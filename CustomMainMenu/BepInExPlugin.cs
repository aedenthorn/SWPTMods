using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomMainMenu
{
    [BepInPlugin("aedenthorn.CustomMainMenu", "Custom Main Menu", "0.3.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> backgroundChangeInterval;

        public static ConfigEntry<string> saveFolder;
        public static ConfigEntry<string> charOrPresetName;
        public static ConfigEntry<string> armorItem;
        public static ConfigEntry<string> poseName;
        public static ConfigEntry<string> backgroundName;

        public static ConfigEntry<Vector3> characterRotation;
        public static ConfigEntry<Vector3> characterPosition;

        public static ConfigEntry<Vector3> light1Position;
        public static ConfigEntry<Vector3> light2Position;
        public static ConfigEntry<Vector3> light3Position;
        public static ConfigEntry<Vector3> light4Position;

        private static Transform mmCharacter;
        private static Transform kiraCharacter;

        public static BepInExPlugin context;

        public static Vector3[] lightPositions;

        private static float lastBackgroundUpdate;
        private static int lastBackgroundIndex;

        private static List<string> backgroundImages = new List<string>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 46, "Nexus mod ID for updates");

            saveFolder = Config.Bind<string>("Text", "SaveFolder", "", "Name of save folder to load character from. If blank, will load preset name instead.");
            charOrPresetName = Config.Bind<string>("Text", "CharOrPresetName", "", "Name of character if saveFolder is set or preset if not to use.");
            armorItem = Config.Bind<string>("Text", "ArmorItem", "", "Name of armor to use if using a preset (saveFolder is blank).");
            poseName = Config.Bind<string>("Text", "PoseName", "", "Name of pose to use (Random if left blank).");
            backgroundName = Config.Bind<string>("Text", "BackgroundName", "", "Name of background picture to use (Random if left blank).");

            backgroundChangeInterval = Config.Bind<int>("Options", "BackgroundChangeInterval", 10, "Interval to change background images if multiple and backgroundName is left blank.");

            characterRotation = Config.Bind<Vector3>("Positioning", "CharacterRotation", Vector3.zero, "Custom character rotation.");
            characterPosition = Config.Bind<Vector3>("Positioning", "CharacterPosition", Vector3.zero, "Custom character position.");

            light1Position = Config.Bind<Vector3>("Positioning", "Light1Position", Vector3.zero, "Custom light position.");
            light2Position = Config.Bind<Vector3>("Positioning", "Light2Position", Vector3.zero, "Custom light position.");
            light3Position = Config.Bind<Vector3>("Positioning", "Light3Position", Vector3.zero, "Custom light position.");
            light4Position = Config.Bind<Vector3>("Positioning", "Light4Position", Vector3.zero, "Custom light position.");

            armorItem.SettingChanged += SettingChanged;
            poseName.SettingChanged += PoseSettingChanged;
            characterRotation.SettingChanged += SettingChanged;
            characterPosition.SettingChanged += SettingChanged;


            light1Position.SettingChanged += LightSettingChanged;
            light2Position.SettingChanged += LightSettingChanged;
            light3Position.SettingChanged += LightSettingChanged;
            light4Position.SettingChanged += LightSettingChanged;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            if (!Directory.Exists(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace)))
                Directory.CreateDirectory(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace));
            else
                LoadBackgroundImageFiles();

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
        {
            Dbgl($"Scene loaded: {arg0.name}, player {Player.code != null} ID {Player.code?._ID != null} Global {Global.code != null}");
        }

        private void LightSettingChanged(object sender, EventArgs e)
        {
            if (SceneManager.GetActiveScene().name == "Desktop" && mmCharacter?.gameObject.activeSelf == true)
                LoadCustomLightData();
        }

        private void PoseSettingChanged(object sender, EventArgs e)
        {
            if (SceneManager.GetActiveScene().name == "Desktop" && mmCharacter?.gameObject.activeSelf == true)
                LoadPoseData();
        }
        
        private void SettingChanged(object sender, EventArgs e)
        {
            if (SceneManager.GetActiveScene().name == "Desktop" && mmCharacter?.gameObject.activeSelf == true)
                LoadCustomCharacter();
        }
    }
}
