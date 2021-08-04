using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Dungeons
{
    [BepInPlugin("aedenthorn.Dungeons", "Dungeons", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> dungeonsResetImmediately;
        public static ConfigEntry<int> dungeonsResetDays;
        public static ConfigEntry<int> dungeonLevelIncreaseOnReset;
        public static ConfigEntry<int> lastDungeonReset;

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

            dungeonsResetImmediately = Config.Bind<bool>("Options", "DungeonsResetImmediately", true, "Cause ordinary dungeons to reset on completion");
            dungeonsResetDays = Config.Bind<int>("Options", "DungeonsResetDays", 1, "Cause ordinary dungeons to reset at after this many days");
            dungeonLevelIncreaseOnReset = Config.Bind<int>("Options", "DungeonLevelIncreaseOnReset", 5, "Ordinary dungeons increase their level by this amount every time they are reset");

            lastDungeonReset = Config.Bind<int>("ZZAuto", "LastDungeonReset", 0, "Days since last dungeon reset.");
            
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }


        private void Update()
        {
            if (!modEnabled.Value || !Global.code || !Player.code)
                return;


        }

        [HarmonyPatch(typeof(UIResult), nameof(UIResult.Open))]
        static class UIResult_Open_Patch
        {
            static void Postfix(UIResult __instance)
            {
                if (!modEnabled.Value || Global.code.curlocation.locationType != LocationType.camp)
                    return;

                if (dungeonsResetImmediately.Value)
                {
                    Dbgl("Resetting dungeon immediately");
                    ResetDungeon(Global.code.curlocation);
                }
            }
        }
        [HarmonyPatch(typeof(Global), nameof(Global.EndDay))]
        static class Global_EndDay_Patch
        {
            static void Postfix(Global __instance)
            {
                if (!modEnabled.Value || dungeonsResetDays.Value <= 0)
                    return;

                lastDungeonReset.Value++;

                Dbgl($"Days since last reset {lastDungeonReset.Value}/{dungeonsResetDays.Value}");

                if (lastDungeonReset.Value >= dungeonsResetDays.Value)
                {
                    Dbgl("Resetting dungeons at end of day");
                    foreach(Transform t in Global.code.locations.items)
                    {
                        Dbgl($"Resetting dungeon {t.name}");
                        Location l = t.GetComponent<Location>();
                        if (l.locationType == LocationType.camp && l.isCleared)
                            ResetDungeon(l);
                    }
                    lastDungeonReset.Value = 0;
                }
            }
        }
        private static void ResetDungeon(Location location)
        {

            location.isCleared = false;
            location.level += dungeonLevelIncreaseOnReset.Value;
        }
    }
}
