using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace Spawn
{
    [BepInPlugin("aedenthorn.Spawn", "Spawn", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<bool> addToCheatMenu;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<string> hotKey;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 45, "Nexus mod ID for updates");
            
            hotKey = Config.Bind<string>("Options", "HotKey", "s", "Hot key to open spawn dialogue. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Global), nameof(Global.ToggleInventory))]
        public static class ToggleInventory_Patch
        {
            public static void Postfix(Global __instance)
            {
                if (!modEnabled.Value || !__instance.uiInventory.gameObject.activeSelf || !addToCheatMenu.Value)
                    return;

            }
        }

    }
}
