using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ScalingFieldEncounters
{
    [BepInPlugin("aedenthorn.ScalingFieldEncounters", "Scaling Field Encounters", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<float> levelFactor;
        public static ConfigEntry<float> wonFactor;
        public static ConfigEntry<float> enemyAmountScaleMult ;
        public static ConfigEntry<float> statScaleMult ;
        public static ConfigEntry<float> damageScaleMult ;
        public static ConfigEntry<float> goldCrystalScaleMult ;
        public static ConfigEntry<float> lootAmountScaleMult;
        //public static ConfigEntry<int> nexusID;

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
            enemyAmountScaleMult  = Config.Bind<float>("Options", "EnemyAmountScaleMult", 1f, "Scale amount of enemies by the scale multiplier times this number.");
            statScaleMult  = Config.Bind<float>("Options", "StatScaleMult", 5f, "Scale enemy stats by the scale multiplier times this number.");
            damageScaleMult  = Config.Bind<float>("Options", "DamageScaleMult", 5f, "Scale enemy damage by the scale multiplier times this number.");
            goldCrystalScaleMult = Config.Bind<float>("Options", "GoldCrystalScaleMult", 0.5f, "Scale gold and crystal amounts by the scale multiplier times this number.");
            lootAmountScaleMult = Config.Bind<float>("Options", "LootAmountScaleMult", 0.5f, "Scale frequency and number of loot drops by the scale multiplier times this number.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(ID), "AddHealth")]
        static class AddHealth_Patch
        {
            static void Prefix(ID __instance, ref float pt, Transform source)
            {
                if (!modEnabled.Value || Global.code.curlocation?.locationType != LocationType.fieldarmy || !source.GetComponent<ID>()?.monster)
                    return;

                float mult = GetMult() * damageScaleMult .Value;

                //Dbgl($"damage: {pt} mult: {mult}");
                pt *= mult;
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

                __instance.txtcompliance.text = __instance.txtcompliance.text.Replace(_location.unitsCount.ToString(), _location.unitsCount * Mathf.Max(1, Mathf.CeilToInt(mult * enemyAmountScaleMult .Value))+"");
                __instance.txtname.text = __instance.txtname.text.Replace(" lv: " + _location.level, " lv: " + (_location.level * Mathf.Max(1, Mathf.CeilToInt(mult * statScaleMult .Value))) + "");
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
                float mult = GetMult();
                
                float statMult = mult * statScaleMult .Value;
                float countMult = mult * enemyAmountScaleMult .Value;
                
                ID id = __result.GetComponent<ID>();

                id.level = Mathf.RoundToInt(id.level * statMult);
                id.maxHealth *= Mathf.Max(1, statMult);
                id.maxStamina *= Mathf.Max(1, statMult);
                id.maxMana *= Mathf.Max(1, statMult);

                while(countMult >= 2)
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

                int count = Mathf.RoundToInt(Global.code.curlocation.maxUnitsCount * Mathf.Max(1, mult * enemyAmountScaleMult .Value));

                Dbgl($"Vanilla units count: {Global.code.curlocation.maxUnitsCount}, new units count {count}");

                Global.code.curlocation.maxUnitsCount = count;
                Global.code.curlocation.unitsCount = count;

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
                id.level = Mathf.RoundToInt(id.level * statMult);
                id.maxHealth = id.maxHealth * statMult;
                id.maxStamina = id.maxStamina * statMult;
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
