using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HideHeadgear
{
    [BepInPlugin("aedenthorn.HideHeadgear", "HideHeadgear", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<bool> showHeadgear;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> neverShow;
        public static ConfigEntry<string> alwaysShow;


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
            
            showHeadgear = Config.Bind<bool>("Options", "showHeadgear", true, "Show headgear");

            hotKey = Config.Bind<string>("Options", "HotKey", "h", "Hotkey to toggle headgear.");
            
            neverShow = Config.Bind<string>("Options", "NeverShow", "", "Comma-separated list of names of characters to never show headgear.");
            alwaysShow = Config.Bind<string>("Options", "AlwaysShow", "", "Comma-separated list of names of characters to always show headgear.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || !AedenthornUtils.CheckKeyDown(hotKey.Value) || Global.code.uiNameChanger.gameObject.activeSelf)
                    return;
                showHeadgear.Value = !showHeadgear.Value;
                Dbgl($"Show headgear: {showHeadgear.Value}");
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.FixedUpdate))]
        static class CharacterCustomization_FixedUpdate_Patch
        {
            static void Postfix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || !__instance.helmet)
                    return;
                if(neverShow.Value.Length > 0 && neverShow.Value.Split(',').Contains(__instance.characterName))
                    __instance.helmet.gameObject.SetActive(false);
                else if(alwaysShow.Value.Length > 0 && alwaysShow.Value.Split(',').Contains(__instance.characterName))
                    __instance.helmet.gameObject.SetActive(true);
                else
                    __instance.helmet.gameObject.SetActive(showHeadgear.Value);
            }
        }
    }
}
