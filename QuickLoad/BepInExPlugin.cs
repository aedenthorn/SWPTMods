using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace QuickLoad
{
    [BepInPlugin("aedenthorn.QuickLoad", "Quick Load", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> lastSave;
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
            //nexusID = Config.Bind<int>("General", "NexusID", 7, "Nexus mod ID for updates");

            hotKey = Config.Bind<string>("Options", "HotKey", "f7", "Hotkey to toggle cheat menu. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            lastSave = Config.Bind<string>("ZZAuto", "LastSave", "", "Last save loaded (set automatically)");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(UIDesktop), "Update")]
        static class UIDesktop_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                if (AedenthornUtils.CheckKeyDown(hotKey.Value) && lastSave.Value.Length > 0)
                {
                    Dbgl($"loading last save {lastSave.Value}");
                    Mainframe.code.uiLoadGame.Close();
                    Mainframe.code.uiConfirmation.gameObject.SetActive(false);
                    Mainframe.code.uiModBrowse.Close();
                    Mainframe.code.uiNotice.Close();
                    Mainframe.code.uISettings.Close();
                    Mainframe.code.LoadGame(lastSave.Value);
                }

            }
        }
        [HarmonyPatch(typeof(LoadGameIcon), "ConfirmLoadGame")]
        static class LoadGameIcon_ConfirmLoadGame_Patch
        {
            static void Postfix(LoadGameIcon __instance)
            {
                if (!modEnabled.Value)
                    return;

                lastSave.Value = __instance.foldername;
            }
        }
    }
}
