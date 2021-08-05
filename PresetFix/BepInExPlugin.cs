using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace PresetFix
{
    [BepInPlugin("aedenthorn.PresetFix", "Preset Fix", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

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
            enableWorldPeace = Config.Bind<bool>("Options", "EnableWorldPeace", true, "Enable world peace (not really).");
            //nexusID = Config.Bind<int>("General", "NexusID", 42, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(LoadPresetIcon), nameof(LoadPresetIcon.ButtonLoad))]
        static class LoadPresetIcon_ButtonLoad_Patch
        {
            static bool Prefix(LoadPresetIcon __instance)
            {
                if (!modEnabled.Value)
                    return true;
                Dbgl($"Setting preset {__instance.foldername} for {Global.code.uiCustomization.curCharacterCustomization.characterName}");
                Mainframe.code.LoadCharacterPreset(Global.code.uiCustomization.curCharacterCustomization, __instance.foldername);
                Global.code.uiCustomization.panelLoadPreset.SetActive(false);
                Global.code.uiCombat.ShowHeader("Character Loaded");
                return false;
            }
        }
    }
}
