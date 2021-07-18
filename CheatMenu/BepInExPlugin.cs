using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CheatMenu
{
    [BepInPlugin("aedenthorn.CheatMenu", "Cheat Menu", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> hotKey;
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

            hotKey = Config.Bind<string>("Options", "HotKey", "f5", "Hotkey to toggle cheat menu. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(Global), "Awake")]
        static class Global_Awake_Patch
        {
            static void Postfix(Global __instance)
            {
                if (!modEnabled.Value)
                    return;

                Dbgl("Fixing spelling errors in cheat menu");

                Text[] textList = __instance.uiCheat.transform.GetComponentsInChildren<Text>();
                foreach(Text text in textList)
                {
                    text.text = text.text.Replace("Invinsible", "Invincible").Replace("Lingeries", "Lingerie");
                }

            }
        }
        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                if (AedenthornUtils.CheckKeyDown(hotKey.Value))
                {
                    Dbgl("Toggling cheat menu");

                    Global.code.uiCheat.gameObject.SetActive(!Global.code.uiCheat.gameObject.activeSelf);
                }

            }
        }
    }
}
