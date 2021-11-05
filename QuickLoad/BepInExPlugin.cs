using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CheatMenu
{
    [BepInPlugin("aedenthorn.CheatMenu", "Cheat Menu", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> levelBypass;

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
            nexusID = Config.Bind<int>("General", "NexusID", 7, "Nexus mod ID for updates");

            levelBypass = Config.Bind<bool>("Options", "LevelBypass", false, "Enable level bypass for equipment");
            hotKey = Config.Bind<string>("Options", "HotKey", "f5", "Hotkey to toggle cheat menu. Use https://docs.unity3d.com/Manual/class-InputManager.html");


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
        public static Transform lastSelected;
        [HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.Click))]
        public static class Click_Patch
        {
            public static void Prefix(EquipmentSlot __instance, ref int __state)
            {
                if (!modEnabled.Value || !levelBypass.Value || !Global.code.selectedItem)
                    return;
                lastSelected = Global.code.selectedItem;
                __state = lastSelected.GetComponent<Item>().levelrequirement;
                lastSelected.GetComponent<Item>().levelrequirement = 0;
            }
            public static void Postfix(EquipmentSlot __instance, int __state)
            {
                if (!modEnabled.Value || !levelBypass.Value || lastSelected == null)
                    return;
                lastSelected.GetComponent<Item>().levelrequirement = __state;
                lastSelected = null;
            }
        }
    }
}
