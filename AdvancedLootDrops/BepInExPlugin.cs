using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace AdvancedLootDrops
{
    [BepInPlugin("aedenthorn.AdvancedLootDrops", "Advanced Loot Drops", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> levelBasedLootWeighting;
        public static ConfigEntry<float> goldMult;
        public static ConfigEntry<float> crystalMult;
        public static ConfigEntry<float> lootAmountMult;
        
        public static ConfigEntry<bool> enableForCamps;
        public static ConfigEntry<bool> enableForRescue;
        public static ConfigEntry<bool> enableForFieldEncounters;
        public static ConfigEntry<bool> enableForSurvival;
        public static ConfigEntry<bool> enableForCity;
        public static ConfigEntry<bool> enableForHelllegion;
        public static ConfigEntry<bool> enableForSiege;
        public static ConfigEntry<bool> enableForFixedRewards;

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
            nexusID = Config.Bind<int>("General", "NexusID", 51, "Nexus mod ID for updates");

            levelBasedLootWeighting = Config.Bind<float>("Options", "LevelBasedLootWeighting", 0, "Fraction weighting for preferring loot that matches the player level (1 is vanilla, only match with player level, and 0 means totally not level-matched). Preference is still for items with levels closer to the player.");
            enableForFixedRewards = Config.Bind<bool>("Options", "EnableForFixedRewards", false, "Enable multipliers for fixed, end of mission rewards.");

            goldMult = Config.Bind<float>("Multipliers", "GoldMult", 1f, "Scale gold amounts by this number.");
            crystalMult = Config.Bind<float>("Multipliers", "CrystalMult", 1f, "Scale crystal amounts by this number.");
            lootAmountMult = Config.Bind<float>("Multipliers", "LootAmountMult", 1f, "Scale frequency and number of loot drops by this number.");

            enableForCamps = Config.Bind<bool>("Toggles", "EnableForCamps", true, "Enable for set checkmark locations.");
            enableForRescue = Config.Bind<bool>("Toggles", "EnableForRescue", true, "Enable for rescue missions.");
            enableForFieldEncounters = Config.Bind<bool>("Toggles", "EnableForFieldEncounters", true, "Enable for the random battle field locations.");
            enableForSurvival = Config.Bind<bool>("Toggles", "EnableForSurvival", true, "Enable for endless survival (comming soon).");
            enableForCity = Config.Bind<bool>("Toggles", "enableForCity", true, "Enable for city locations.");
            enableForHelllegion = Config.Bind<bool>("Toggles", "enableForHelllegion", true, "Enable for hell legion locations.");
            enableForSiege = Config.Bind<bool>("Toggles", "enableForSiege", true, "Enable for siege locations.");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }
        private static bool CheckEnabledForLocation()
        {
            switch (Global.code.curlocation?.locationType)
            {
                case LocationType.camp:
                    return enableForCamps.Value;
                case LocationType.city:
                    return enableForCity.Value;
                case LocationType.fieldarmy:
                    return enableForFieldEncounters.Value;
                case LocationType.rescue:
                    return enableForRescue.Value;
                case LocationType.survival:
                    return enableForSurvival.Value;
                case LocationType.helllegion:
                    return enableForHelllegion.Value;
                case LocationType.siegearmy:
                    return enableForSiege.Value;
                default:
                    return false;
            }
        }

        [HarmonyPatch(typeof(LootDrop), nameof(LootDrop.GetMatchLevelItems))]
        static class LootDrop_GetMatchLevelItems_Patch
        {
            static bool Prefix(List<Transform> list, ref List<Transform> __result)
            {
                if (!modEnabled.Value || !CheckEnabledForLocation() || list.Count <= 0)
                    return true;

                List<Transform> list2 = new List<Transform>();
                foreach (Transform transform in list)
                {
                    Item component = transform.GetComponent<Item>();
                    if (component)
                    {
                        float add;
                        if (Player.code.customization._ID.level > component.level)
                        {
                            add = (Player.code.customization._ID.level / 2f - component.level) / Player.code.customization._ID.level;
                        }
                        else
                        {
                            add = -(Player.code.customization._ID.level * 1.5f - component.level) / component.level; 
                        }
                        //Dbgl($"Item level {t.GetComponent<Item>().level}, extra chance {add}");

                        if (Random.value > levelBasedLootWeighting.Value * (1 + add))
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
                if (!modEnabled.Value || !CheckEnabledForLocation())
                    return;

                //Dbgl($"multiplying dropped rewards by {mult * lootAmountScaleMult.Value}");

                __instance.maxAmount = Mathf.RoundToInt(__instance.maxAmount * Random.Range(0, lootAmountMult.Value));
            }
        }

        [HarmonyPatch(typeof(LootDrop), "InstantiateItem")]
        static class LootDrop_InstantiateItem_Patch
        {
            static void Prefix(LootDrop __instance, Transform item)
            {
                if (!modEnabled.Value || !CheckEnabledForLocation()|| (item.name != "Gold" && item.name != "Crystals"))
                    return;

                //Dbgl($"multiplying dropped gold and crystals by {mult * goldCrystalScaleMult .Value}");

                item.GetComponent<Item>().amount = Mathf.RoundToInt(item.GetComponent<Item>().amount * (item.name == "Gold" ? Random.Range(0, goldMult.Value) : Random.Range(0, crystalMult.Value)));
            }
        }

        [HarmonyPatch(typeof(UIResult), "GenerateReward")]
        static class UIResult_Open_Patch
        {
            static void Prefix(UIResult __instance)
            {
                if (!modEnabled.Value || !CheckEnabledForLocation())
                    return;

                foreach (Transform transform in __instance.finalRewards.items)
                {
                    if (transform)
                    {
                        Reward reward = transform.GetComponent<Reward>();
                        if (reward.rewardItem)
                        {
                            if(reward.rewardItem.name == "Gold")
                                reward.amount = Mathf.RoundToInt(reward.amount * Random.Range(0,goldMult.Value));
                            else if (reward.rewardItem.name == "Crystals")
                                reward.amount = Mathf.RoundToInt(reward.amount * Random.Range(0, crystalMult.Value));
                        }
                    }
                }
            }
        }
    }
}
