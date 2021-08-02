using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AutoSave
{
    [BepInPlugin("aedenthorn.AutoSave", "Auto Save", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<float> autoSaveInterval;
        public static ConfigEntry<bool> autoSaveOnHome;
        public static ConfigEntry<bool> saveAwayFromHome;
        public static ConfigEntry<bool> saveInUI;

        public static float timeSinceLastSave = 0;
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

            //saveAwayFromHome = Config.Bind<bool>("Options", "SaveAwayFromHome", true, "Allows the mod to save even when not at home (experimental, may cause problems).");
            saveInUI = Config.Bind<bool>("Options", "SaveInUI", false, "Allow the mod to save when a UI is open (inventory, etc.).");
            hotKey = Config.Bind<string>("Options", "HotKey", "f6", "Hotkey to quick save. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            autoSaveInterval = Config.Bind<float>("Options", "AutoSaveInterval", 10f, "Interval in minutes to auto save (can be decimal, set to 0 to disable timed autosave).");
            autoSaveOnHome = Config.Bind<bool>("Options", "AutoSaveOnHome", true, "Autosave when going home.");

            nexusID = Config.Bind<int>("General", "NexusID", 17, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(UILoading), nameof(UILoading.OpenLoading), new Type[] { typeof(Transform) })]
        static class OpenLoading_Patch1
        {
            static void Prefix(Transform location)
            {
                if (!modEnabled.Value || !Player.code)
                    return;

                if (timeSinceLastSave > 0 && autoSaveOnHome.Value && location.GetComponent<Location>().locationType == LocationType.home)
                {
                    Global.code.curlocation = location.GetComponent<Location>();
                    Dbgl("Autosaving on going home");
                    Mainframe.code.SaveGame();
                    timeSinceLastSave = 0;
                }
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value || Global.code.curlocation.locationType != LocationType.home)
                    return;

                if (AedenthornUtils.CheckKeyDown(hotKey.Value))
                {
                    Dbgl("Saving via hotkey press");
                    Mainframe.code.SaveGame();
                    timeSinceLastSave = 0;
                    return;
                }

                timeSinceLastSave += Time.deltaTime;
                if(autoSaveInterval.Value > 0 && timeSinceLastSave >= autoSaveInterval.Value * 60 && (!Global.code.onGUI || saveInUI.Value))
                {
                    if (Global.code.curlocation.locationType != LocationType.home)
                    {
                        if (!saveAwayFromHome.Value || !Global.code.currentHome)
                            return;
                        Dbgl("autosaving after interval");
                        var curLoc = Global.code.curlocation;
                        Global.code.curlocation = Global.code.currentHome;
                        Mainframe.code.SaveGame();
                        Global.code.curlocation = curLoc;
                        timeSinceLastSave = 0;
                    }
                    else
                    {
                        Dbgl("autosaving after interval");
                        Mainframe.code.SaveGame();
                        timeSinceLastSave = 0;
                    }
                }
            }
        }
    }
}
