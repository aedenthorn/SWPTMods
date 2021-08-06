using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Dungeons
{
    [BepInPlugin("aedenthorn.Dungeons", "Dungeons", "0.2.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> fullLootForClearedDungeons;
        public static ConfigEntry<int> dungeonLevelIncreaseOnReset;
        
        public static List<string> clearedDungeons = new List<string>();

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
            nexusID = Config.Bind<int>("General", "NexusID", 49, "Nexus mod ID for updates");

            dungeonLevelIncreaseOnReset = Config.Bind<int>("Options", "DungeonLevelIncreaseOnReset", 5, "Dungeons increase their level by this amount every time they are cleared");
            fullLootForClearedDungeons = Config.Bind<bool>("Options", "FullLootForClearedDungeons", true, "If false, the default behaviour of the game to reduce loot in cleared dungeons to 1/4 is maintained.");
            
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }



        [HarmonyPatch(typeof(UIResult), nameof(UIResult.Open))]
        static class UIResult_Open_Patch
        {
            static void Postfix(UIResult __instance)
            {
                if (!modEnabled.Value || Global.code.curlocation.locationType != LocationType.camp)
                    return;

                ResetDungeon(Global.code.curlocation);
            }
        }

        [HarmonyPatch(typeof(LootDrop), nameof(LootDrop.Start))]
        static class LootDrop_Start_Patch
        {
            static void Prefix(LootDrop __instance, ref bool __state)
            {
                __state = false;
                if (!modEnabled.Value || !fullLootForClearedDungeons.Value || !Global.code.curlocation || Global.code.curlocation.locationType != LocationType.camp || !Global.code.curlocation.isCleared)
                    return;

                __state = true;
                Global.code.curlocation.isCleared = false;
            }
            static void Postfix(LootDrop __instance, bool __state)
            {
                if (!modEnabled.Value || !fullLootForClearedDungeons.Value || !Global.code.curlocation || Global.code.curlocation.locationType != LocationType.camp || !__state)
                    return;

                Global.code.curlocation.isCleared = true;
            }
        }
        
        [HarmonyPatch(typeof(WorldMapIcon), nameof(WorldMapIcon.Initiate))]
        static class WorldMapIcon_Initiate_Patch
        {
            static void Postfix(WorldMapIcon __instance, Location ___location)
            {
                if (!modEnabled.Value || ___location.locationType != LocationType.camp || !___location.isCleared)
                    return;

                __instance.txtname.text +=" lv: " + ___location.level;
            }
        }

        [HarmonyPatch(typeof(UIWorldMap), nameof(UIWorldMap.FocusOnLocation))]
        static class UIWorldMap_FocusOnLocation_Patch
        {
            static void Postfix(UIWorldMap __instance, Location location)
            {
                if (!modEnabled.Value || location.locationType != LocationType.camp || !location.isCleared)
                    return;

                __instance.txtAttackButton.text = Localization.GetContent("UIWorldMap_3", Array.Empty<object>());
            }
        }

        private static void ResetDungeon(Location location)
        {
            location.level += dungeonLevelIncreaseOnReset.Value;
        }
    }
}
