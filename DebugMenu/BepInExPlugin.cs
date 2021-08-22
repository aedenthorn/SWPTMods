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
using Object = UnityEngine.Object;

namespace DebugMenu
{
    [BepInPlugin("aedenthorn.DebugMenu", "Debug Menu", "0.4.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> language;
        public static ConfigEntry<string> spawnItemTitle;
        public static ConfigEntry<string> cancelText;
        public static ConfigEntry<string> spawnText;
        public static ConfigEntry<string> levelBypassNotice;
        public static ConfigEntry<string> flyModeNotice;
        
        public static ConfigEntry<float> flyModeDecelerationRate;

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> flyUpKey;
        public static ConfigEntry<string> flyDownKey;
        public static ConfigEntry<string> flyFastKey;
        public static ConfigEntry<string> flyToggleKey;
        public static ConfigEntry<string> flyLockToggleKey;

        public static ConfigEntry<bool> levelBypass;
        public static ConfigEntry<bool> flyMode;
        public static ConfigEntry<bool> flyLocked;

        private static List<string> itemNames;
        public static Transform uiDebug;

        public static Transform lastSelected;

        private static Transform uiSpawnItem;
        private static InputField spawnInput;
        private static InputField spawnPrefixInput;
        private static InputField spawnSuffixInput;
        private static Text spawnHintText;

        private static List<string> wPrefixes = new List<string>();
        private static List<string> wSuffixes = new List<string>();
        private static List<string> aPrefixes = new List<string>();
        private static List<string> aSuffixes = new List<string>();

        private static SlotType[] armorSlotTypes = new SlotType[] { SlotType.armor, SlotType.gloves, SlotType.helmet, SlotType.legging, SlotType.shoes };

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
            nexusID = Config.Bind<int>("General", "NexusID", 48, "Nexus mod ID for updates");
            nexusID.Value = 48;

            language = Config.Bind<string>("Text", "Language", "en", "Name of language file to use for buttons.");
            spawnItemTitle = Config.Bind<string>("Text", "SpawnItemTitle", "Spawn Item", "Title for spawn item ui.");
            cancelText = Config.Bind<string>("Text", "CancelText", "Cancel", "Text for cancel button.");
            spawnText = Config.Bind<string>("Text", "SpawnText", "Spawn", "Text for spawn button.");
            levelBypassNotice = Config.Bind<string>("Text", "LevelBypassNotice", "Level bypass: {0}", "Text for level bypass notice. {0} is replaced with true or false.");
            flyModeNotice = Config.Bind<string>("Text", "FlyModeNotice", "Fly mode: {0}", "Text for fly mode notice. {0} is replaced with true or false.");

            flyModeDecelerationRate = Config.Bind<float>("Options", "FlyModeDecelerationRate", 0.03f, "Deceleration rate in fly mode.");
            
            flyMode = Config.Bind<bool>("Toggles", "FlyMode", false, "Enable fly mode");
            flyLocked = Config.Bind<bool>("Toggles", "FlyLocked", true, "Lock player facing direction to camera direction in fly mode.");
            levelBypass = Config.Bind<bool>("Toggles", "LevelBypass", false, "Enable level bypass for equipment");
            
            hotKey = Config.Bind<string>("Keys", "HotKey", "f4", "Hotkey to toggle debug menu. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            flyToggleKey = Config.Bind<string>("Toggles", "FlyToggleKey", "", "Hotkey to toggle fly mode. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            flyUpKey = Config.Bind<string>("Keys", "FlyUpKey", "space", "Hotkey to increase elevation in fly mode. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            flyDownKey = Config.Bind<string>("Keys", "FlyDownKey", "left ctrl", "Hotkey to decrease elevation in fly mode. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            flyFastKey = Config.Bind<string>("Keys", "FlyFastKey", "left shift", "Hotkey to fly faster in fly mode. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            flyLockToggleKey = Config.Bind<string>("Keys", "FlyLockToggleKey", "l", "Hotkey to toggle locking player facing direction in fly mode. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            Harmony harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
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
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(DumpAffixes);
            count++;

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(DumpPoses);
            count++;

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(DumpEnemies);
            count++;

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(DumpBody);
            count++;

            // Toggle

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(ToggleLevelBypass);
            count++;

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(ToggleFly);
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
