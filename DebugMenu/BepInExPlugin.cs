using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DebugMenu
{
    [BepInPlugin("aedenthorn.DebugMenu", "Debug Menu", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> language;
        public static ConfigEntry<string> spawnItemTitle;
        public static ConfigEntry<string> cancelText;
        public static ConfigEntry<string> levelBypassNotice;
        public static ConfigEntry<string> flyModeNotice;

        public static ConfigEntry<string> hotKey;

        public static ConfigEntry<bool> levelBypass;
        public static ConfigEntry<bool> flyMode;

        private static List<string> itemNames;
        public static Transform uiDebug;

        public static Transform lastSelected;

        private static Transform uiSpawnItem;
        private static GameObject spawnInput;
        private static Text spawnHintText;

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
            
            language = Config.Bind<string>("Text", "Language", "en", "Name of language file to use for buttons.");
            spawnItemTitle = Config.Bind<string>("Text", "SpawnItemTitle", "Spawn Item", "Title for spawn item ui.");
            cancelText = Config.Bind<string>("Text", "CancelText", "Cancel", "Text for cancel button.");
            levelBypassNotice = Config.Bind<string>("Text", "LevelBypassNotice", "Level bypass: {0}", "Text for level bypass notice. {0} is replaced with true or false.");
            //flyModeNotice = Config.Bind<string>("Text", "FlyModeNotice", "Fly mode: {0}", "Text for fly mode notice. {0} is replaced with true or false.");
            
            nexusID = Config.Bind<int>("General", "NexusID", 48, "Nexus mod ID for updates");
            nexusID.Value = 48;
            //flyMode = Config.Bind<bool>("Options", "FlyMode", false, "Enable fly mode");
            levelBypass = Config.Bind<bool>("Options", "LevelBypass", false, "Enable level bypass for equipment");
            hotKey = Config.Bind<string>("Options", "HotKey", "f4", "Hotkey to toggle debug menu. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        private static void CreateDebugMenu()
        {
            Dbgl("Creating debug menu");

            uiDebug = Instantiate(Global.code.uiCheat.transform, Global.code.uiCheat.transform.parent);
            uiDebug.name = "Debug Menu";
            Transform buttonList = uiDebug.GetComponentInChildren<VerticalLayoutGroup>().transform;

            string[] names = File.ReadAllLines(Path.Combine(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace), $"{language.Value}.txt"));

            // Dump

            int count = 0;

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(DumpItems);
            count++;

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(DumpPoses);
            count++;

            // Toggle

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(ToggleLevelBypass);
            count++;

            // Spawn

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(OpenSpawnItemUI);
            count++;

            while(count < buttonList.childCount)
            {
                if (buttonList.GetChild(count))
                    buttonList.GetChild(count).gameObject.SetActive(false);
                count++;
            }
        }
    }
}
