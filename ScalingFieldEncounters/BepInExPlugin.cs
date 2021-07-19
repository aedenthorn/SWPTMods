using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ScalingFieldEncounters
{
    [BepInPlugin("aedenthorn.ScalingFieldEncounters", "Scaling Field Encounters", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<float> levelScaleFactor;
        public static ConfigEntry<float> skirmishScaleFactor;
        public static ConfigEntry<float> amountScale;
        public static ConfigEntry<float> statScale;
        public static ConfigEntry<float> damageScale;
        public static ConfigEntry<float> lootScale;
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

            levelScaleFactor = Config.Bind<float>("Options", "LevelScaleFactor", 0.05f, "Difficulty scale factor based on level (1 = 1-to-1 scaling per player level, set to 0 for no effect).");
            skirmishScaleFactor = Config.Bind<float>("Options", "SkirmishScaleFactor", 0.05f, "Difficulty scale factor based on skirmishes won (1 = 1-to-1 scaling per player skirmish won, set to 0 for no effect).");
            amountScale = Config.Bind<float>("Options", "AmountScale", 1f, "Scale amount of enemies by the scale multiplier x this number.");
            statScale = Config.Bind<float>("Options", "StatScale", 1.0f, "Scale enemy stats by the scale multiplier x this number.");
            damageScale = Config.Bind<float>("Options", "DamageScale", 0.2f, "Scale enemy damage by the scale multiplier x this number.");
            lootScale = Config.Bind<float>("Options", "LootScale", 1.0f, "Scale loot drops by the scale multiplier x this number.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(Weapon), "DealDamage")]
        static class Weapon_DealDamage_Patch
        {
            static void Prefix(Weapon __instance, ref float multiplier, Item ____Item)
            {
                if (!modEnabled.Value || Global.code.curlocation?.locationType != LocationType.fieldarmy || !____Item.owner?.GetComponent<ID>()?.monster)
                    return;

                float mult = GetMult();

                multiplier *= Mathf.Max(1, mult * damageScale.Value);
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

                Dbgl($"multiplying dropped rewards by {mult * lootScale.Value}");

                __instance.maxAmount *= Mathf.Max(1, Mathf.RoundToInt(mult * lootScale.Value));
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

                Dbgl($"multiplying dropped gold and crystals by {mult * lootScale.Value}");

                item.GetComponent<Item>().amount *= Mathf.Max(1, Mathf.RoundToInt(mult * lootScale.Value));
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
                Dbgl($"multiplying fixed rewards by {mult * lootScale.Value}");

                foreach (Transform transform in __instance.finalRewards.items)
                {
                    if (transform)
                    {
                        Reward reward = transform.GetComponent<Reward>();
                        if (reward.rewardItem && (reward.rewardItem.name == "Gold" || reward.rewardItem.name == "Crystals"))
                        {
                            reward.amount *= Mathf.Max(1, Mathf.RoundToInt(mult * lootScale.Value));
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

                __instance.txtcompliance.text = __instance.txtcompliance.text.Replace(_location.unitsCount.ToString(), _location.unitsCount * Mathf.Max(1, Mathf.CeilToInt(mult * amountScale.Value))+"");
                __instance.txtname.text.Replace(" lv: " + _location.level, " lv: " + _location.level * Mathf.Max(1, Mathf.CeilToInt(mult * statScale.Value)) + "");
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

                Global.code.curlocation.level = Mathf.RoundToInt(Global.code.curlocation.level * mult * statScale.Value);

                ArmyPreset ap = __instance.GetComponent<ArmyPreset>();
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
            }
        }
        [HarmonyPatch(typeof(Scene), "Start")]
        static class SkillBox_ButtonClick_Patch
        {
            static void Prefix(Scene __instance)
            {

                if (!modEnabled.Value || Global.code.curlocation.locationType != LocationType.fieldarmy)
                    return;

                Dbgl("Field army scene start, max units: " + Global.code.curlocation.maxUnitsCount);
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
            mult += won * skirmishScaleFactor.Value;
            mult += Player.code.customization._ID.level * levelScaleFactor.Value;
            return mult;
        }

        private static Transform[] ScaleArray(Transform[] array, float mult)
        {
            float statMult = Mathf.Max(1, mult * statScale.Value);
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
            return array;
        }
    }
}
