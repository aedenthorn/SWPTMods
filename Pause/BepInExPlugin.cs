using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Pause
{
    [BepInPlugin("aedenthorn.Pause", "Pause", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<string> pauseKey;

        public static float lastTimeScale = 0;
        public static bool paused = false;

        public static ConfigEntry<bool> enableWorldPeace;
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
            pauseKey = Config.Bind<string>("Options", "PauseKey", "pause", "Hotkey to toggle pause.");
            //nexusID = Config.Bind<int>("General", "NexusID", 42, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }
        private void Update()
        {
            if (AedenthornUtils.CheckKeyDown(pauseKey.Value))
            {
                Dbgl("Pressed pause hotkey");
                paused = !paused;
            }
        }
        [HarmonyPatch(typeof(Global), "CheckOnGUI")]
        static class Global_CheckOnGUI_Patch
        {
            static void Postfix(Global __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (paused)
                    Time.timeScale = 0;
            }
        }
    }
}
