using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace ScalingFieldEncounters
{
    [BepInPlugin("aedenthorn.ScalingFieldEncounters", "Scaling Field Encounters", "0.4.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<float> levelFactor;
        public static ConfigEntry<float> wonFactor;
        public static ConfigEntry<float> enemySpawnMult;
        public static ConfigEntry<float> enemyTotalMult;
        public static ConfigEntry<float> statScaleMult ;
        public static ConfigEntry<float> levelScaleMult;
        public static ConfigEntry<float> damageScaleMult ;
        public static ConfigEntry<float> goldCrystalScaleMult ;
        public static ConfigEntry<float> lootAmountScaleMult;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");

            levelFactor = Config.Bind<float>("Options", "LevelFactor", 0.1f, "Difficulty scale factor based on level (1 = 1-to-1 scaling per player level, set to 0 for no effect).");
            wonFactor = Config.Bind<float>("Options", "WonFactor", 0.1f, "Difficulty scale factor based on skirmishes won (1 = 1-to-1 scaling per player skirmish won, set to 0 for no effect).");
            enemySpawnMult = Config.Bind<float>("Options", "EnemySpawnMult", 1f, "Scale number of enemies per spawn by the scale multiplier times this number.");
            enemyTotalMult  = Config.Bind<float>("Options", "EnemyTotalMult", 1f, "Scale number of total enemies per battle by the scale multiplier times this number.");
            levelScaleMult  = Config.Bind<float>("Options", "LevelScaleMult", 1f, "Scale enemy level by the scale multiplier times this number.");
            statScaleMult  = Config.Bind<float>("Options", "StatScaleMult", 1f, "Scale enemy stats by the scale multiplier times this number.");
            damageScaleMult  = Config.Bind<float>("Options", "DamageScaleMult", 1f, "Scale enemy damage by the scale multiplier times this number.");
            goldCrystalScaleMult = Config.Bind<float>("Options", "GoldCrystalScaleMult", 0.5f, "Scale gold and crystal amounts by the scale multiplier times this number.");
            lootAmountScaleMult = Config.Bind<float>("Options", "LootAmountScaleMult", 0.5f, "Scale frequency and number of loot drops by the scale multiplier times this number.");

            nexusID = Config.Bind<int>("General", "NexusID", 13, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        //[HarmonyPatch(typeof(Weapon), "DealDamage")]
        static class DealDamage_Patch
        {
            static void Prefix(Weapon __instance, Item ____Item, ref float __state)
            {
                if (!modEnabled.Value || Global.code.curlocation?.locationType != LocationType.fieldarmy || !____Item.owner.GetComponent<ID>()?.monster)
                    return;

                float mult = GetMult() * damageScaleMult .Value;

                __state = ____Item.owner.GetComponent<ID>().damage;

                //Dbgl($"damage: {pt} mult: {mult}");

                ____Item.owner.GetComponent<ID>().damage *= mult;
            }
            static void Postfix(Weapon __instance, Item ____Item, float __state)
            {
                if (!modEnabled.Value || Global.code.curlocation?.locationType != LocationType.fieldarmy || !____Item.owner.GetComponent<ID>()?.monster)
                    return;

                ____Item.owner.GetComponent<ID>().damage = __state;
            }
        }

        [HarmonyPatch(typeof(LootDrop), nameof(LootDrop.GetMatchLevelItems))]
        static class LootDrop_GetMatchLevelItems_Patch
        {
            static bool Prefix(LootDrop __instance, List<Transform> list, List<Transform> __result)
            {
                if (!modEnabled.Value || Global.code.curlocation?.locationType != LocationType.fieldarmy || list.Count <= 0)
                    return true;

                List<Transform> list2 = new List<Transform>();
                foreach (Transform transform in list)
                {
                    Item component = transform.GetComponent<Item>();
                    if (component)
                    {
                        if (component.level <= Global.code.curlocation.level && component.level > Random.value * (Global.code.curlocation.level - 2))
                        {
                            list2.Add(transform);
                        }
                    }
                    else
                    {
                        Debug.LogError("错误道具 " + transform.name);
                    }
                }
                __result = list2;
                return false;
            }
        }
        
        [HarmonyPatch(typeof(LootDrop), nameof(LootDrop.Drop))]
        static class LootDrop_Drop_Patch
        {
            static void Prefix(LootDrop __instance)
            {
                if (!modEnabled.Value || Global.code.curlocation?.locationType != LocationType.fieldarmy)
                    return;

                float mult = GetMult();

                //Dbgl($"multiplying dropped rewards by {mult * lootAmountScaleMult.Value}");

                __instance.maxAmount *= Mathf.Max(1, Mathf.RoundToInt(mult * lootAmountScaleMult.Value));
            }
        }

        [HarmonyPatch(typeof(LootDrop), "InstantiateItem")]
        static class LootDrop_InstantiateItem_Patch
        {
            static void Prefix(LootDrop __instance, Transform item)
            {
                if (!modEnabled.Value || Global.code.curlocation?.locationType != LocationType.fieldarmy || (item.name != "Gold" && item.name != "Crystals"))
                    return;

                float mult = GetMult();

                //Dbgl($"multiplying dropped gold and crystals by {mult * goldCrystalScaleMult .Value}");

                item.GetComponent<Item>().amount *= Mathf.Max(1, Mathf.RoundToInt(mult * goldCrystalScaleMult .Value));
            }
        }

        [HarmonyPatch(typeof(UIResult), "GenerateReward")]
        static class UIResult_Open_Patch
        {
            static void Prefix(UIResult __instance)
            {
                if (!modEnabled.Value || Global.code.curlocation?.locationType != LocationType.fieldarmy)
                    return;

                float mult = GetMult();
                Dbgl($"multiplying fixed rewards by {mult * goldCrystalScaleMult .Value}");

                foreach (Transform transform in __instance.finalRewards.items)
                {
                    if (transform)
                    {
                        Reward reward = transform.GetComponent<Reward>();
                        if (reward.rewardItem && (reward.rewardItem.name == "Gold" || reward.rewardItem.name == "Crystals"))
                        {
                            reward.amount *= Mathf.Max(1, Mathf.RoundToInt(mult * goldCrystalScaleMult .Value));
                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(WorldMapIcon), nameof(WorldMapIcon.Initiate))]
        static class WorldMapIcon_Initiate_Patch
        {
            static void Postfix(WorldMapIcon __instance, Location _location)
            {
                if (!modEnabled.Value || _location.locationType != LocationType.fieldarmy)
                    return;
                float mult = GetMult();

                string num = _location.unitsCount.ToString();

                _location.unitsCount = _location.maxUnitsCount * Mathf.RoundToInt(Mathf.Max(1, mult * enemyTotalMult.Value));

                __instance.txtcompliance.text = __instance.txtcompliance.text.Replace(num, _location.unitsCount+"");
                //__instance.txtname.text = __instance.txtname.text.Replace(" lv: " + _location.level, " lv: " + (_location.level * Mathf.Max(1, Mathf.CeilToInt(mult * levelScaleMult.Value))) + "");
                //Dbgl($"{__instance.txtname.text}, scaled {_location.level * Mathf.Max(1, Mathf.CeilToInt(mult * statScale.Value))}, level {_location.level}");
            }
        }
        [HarmonyPatch(typeof(SkirmishSpawner), nameof(SkirmishSpawner.InstantiateEnemy))]
        static class SkirmishSpawner_InstantiateEnemy_Patch
        {
            static void Postfix(SkirmishSpawner __instance, ref Transform __result)
            {
                if (!modEnabled.Value || Global.code.curlocation.locationType != LocationType.fieldarmy)
                    return;

                float countMult = GetMult() * enemySpawnMult.Value;
                while (countMult >= 2)
                {
                    Transform enemy = Utility.Instantiate(__result);
                    enemy.GetComponent<ID>().isFriendly = false;
                    enemy.position = __instance.transform.position;
                    enemy.eulerAngles = new Vector3(0f, Random.Range(0, 360), 0f);
                    enemy.GetComponent<NavMeshAgent>().enabled = false;
                    enemy.GetComponent<NavMeshAgent>().enabled = true;
                    enemy.GetComponent<Monster>().charge = true;
                    Global.code.enemies.AddItemDifferentObject(enemy);

                    countMult -= 1;
                    if (countMult < 2 && countMult - 1 < Random.value)
                        break;
                }


            }
        }
        [HarmonyPatch(typeof(Balancer), "GetMonsterStats")]
        static class Balancer_GetMonsterStats_Patch
        {
            static void Postfix(Balancer __instance, Monster monster)
            {
                if (!modEnabled.Value || Global.code?.curlocation?.locationType != LocationType.fieldarmy || !monster)
                    return;

                float mult = GetMult();

                float statMult = mult * statScaleMult.Value;
                float damageMult = mult * damageScaleMult.Value;
                ID id = monster._ID;

                Dbgl($"enemy {monster.name}: old level {id.level} health {id.maxHealth}, stamina {id.maxStamina }, mana {id.maxMana}, damage {monster._ID.damage}");

                id.level = Mathf.RoundToInt(id.level * levelScaleMult.Value * mult);

                monster._ID.damage = Mathf.RoundToInt(monster._ID.damage * damageMult);
                monster._ID.fireDamage = Mathf.RoundToInt(monster._ID.fireDamage * damageMult);
                monster._ID.coldDamage = Mathf.RoundToInt(monster._ID.coldDamage * damageMult);
                monster._ID.lighteningDamage = Mathf.RoundToInt(monster._ID.lighteningDamage * damageMult);
                monster._ID.poisonDamage = Mathf.RoundToInt(monster._ID.poisonDamage * damageMult);
                monster._ID.fireResist = Mathf.RoundToInt(monster._ID.fireResist * damageMult);
                monster._ID.coldResist = Mathf.RoundToInt(monster._ID.coldResist * damageMult);
                monster._ID.lighteningResist = Mathf.RoundToInt(monster._ID.lighteningResist * damageMult);
                monster._ID.poisonResist = Mathf.RoundToInt(monster._ID.poisonResist * damageMult);

                id.maxHealth *= Mathf.Max(1, statMult);
                id.maxStamina *= Mathf.Max(1, statMult);
                id.maxMana *= Mathf.Max(1, statMult);

                Dbgl($"enemy {monster.name}: new level {id.level} health {id.maxHealth}, stamina {id.maxStamina }, mana {id.maxMana}, damage {monster._ID.damage}");

            }
        }
        [HarmonyPatch(typeof(Scene), "Start")]
        static class Scene_Start_Patch
        {
            static void Prefix(Scene __instance)
            {
                if (!modEnabled.Value || Global.code.curlocation.locationType != LocationType.fieldarmy || Global.code.curlocation.isCleared)
                    return;

                Dbgl("Scaling fieldarmy location");

                float mult = GetMult();

                Dbgl($"Base scaling for location: {mult}");

                int count = Mathf.RoundToInt(Global.code.curlocation.maxUnitsCount * Mathf.Max(1, mult * enemyTotalMult .Value));

                Dbgl($"Vanilla units count: {Global.code.curlocation.maxUnitsCount}, new units count {count}");

                return;
                /*
                Global.code.curlocation.level = Mathf.RoundToInt(Global.code.curlocation.level * mult * statScale.Value);

                ArmyPreset ap = Global.code.curlocation.GetComponent<ArmyPreset>();
                if (ap == null)
                {
                    Dbgl("No army preset");
                    return;
                }

                int count = 0;

                if (ap.bosses != null)
                {
                    ap.bosses = ScaleArray(ap.bosses, mult);
                    count += ap.bosses.Length;
                    //Dbgl($"{ap.bosses.Length} bosses");
                }
                if (ap.minibosses != null)
                {
                    ap.minibosses = ScaleArray(ap.minibosses, mult);
                    count += ap.minibosses.Length;

                    //Dbgl($"{ap.minibosses.Length} minibosses");
                }
                if (ap.superuniques != null)
                {
                    ap.superuniques = ScaleArray(ap.superuniques, mult);
                    count += ap.superuniques.Length;

                    //Dbgl($"{ap.superuniques.Length} superuniques");
                }
                if (ap.rangedSquadPresets != null)
                {
                    for (int i = 0; i < ap.rangedSquadPresets.Length; i++)
                    {
                        if (ap.rangedSquadPresets[i] == null)
                            continue;

                        ap.rangedSquadPresets[i].units = ScaleArray(ap.rangedSquadPresets[i].units, mult);
                        count += ap.rangedSquadPresets[i].units.Length;

                        //Dbgl($"{ap.rangedSquadPresets[i].units.Length} rangedSquadPresets");
                    }
                }
                if (ap.normalSquadPresets != null)
                {
                    for (int i = 0; i < ap.normalSquadPresets.Length; i++)
                    {
                        if (ap.normalSquadPresets[i] == null)
                            continue;

                        ap.normalSquadPresets[i].units = ScaleArray(ap.normalSquadPresets[i].units, mult);
                        count += ap.normalSquadPresets[i].units.Length;

                        //Dbgl($"{ap.normalSquadPresets[i].units.Length} normalSquadPresets");
                    }
                }
                if (ap.hardSquadPresets != null)
                {
                    for (int i = 0; i < ap.hardSquadPresets.Length; i++)
                    {
                        if (ap.hardSquadPresets[i] == null)
                            continue;

                        ap.hardSquadPresets[i].units = ScaleArray(ap.hardSquadPresets[i].units, mult);
                        count += ap.hardSquadPresets[i].units.Length;

                        //Dbgl($"{ap.hardSquadPresets[i].units.Length} hardSquadPresets");
                    }
                }

                Dbgl($"Vanilla units count: {Global.code.curlocation.maxUnitsCount}, new units count {count}");

                Global.code.curlocation.maxUnitsCount = count;
                Global.code.curlocation.unitsCount = count;
                */
            }
        }

        private static float GetMult()
        {
            float mult = 1;

            int won = 0;
            foreach (Transform transform in RM.code.allAchievements.items)
            {
                if (transform && transform.GetComponent<Achievement>().achivementType == AchievementType.skirmishwon)
                {
                    won = transform.GetComponent<Achievement>().curnum;
                    break;
                }
            }
            mult += won * wonFactor.Value;
            mult += Player.code.customization._ID.level * levelFactor.Value;
            return mult;
        }





        private static Transform[] ScaleArray(Transform[] array, float mult)
        {
            float statMult = Mathf.Max(1, mult * statScaleMult .Value);
            foreach (Transform t in array)
            {
                if (!t)
                {
                    //Dbgl($"transform not found");
                    continue;
                }

                ID id = t.GetComponent<ID>();

                if (!id)
                {
                    //Dbgl($"Monster ID not found");
                    continue;
                }
                while(id.level < Mathf.RoundToInt(id.level * levelScaleMult.Value * mult))
                {
                    id.GetNextExp();
                    id.AddExp(id.nextExp - id.curExp);
                }
                //id.maxHealth = id.maxHealth * statMult;
                //id.maxStamina = id.maxStamina * statMult;
                id.maxMana = id.maxMana * statMult;
            }

            List<Transform> list = new List<Transform>();
            while(list.Count < array.Length * mult)
            {
                foreach (Transform t in array)
                {
                    list.Add(t);
                    if (list.Count >= mult)
                        break;
                }
            }
            return list.ToArray();
        }
    }
}
