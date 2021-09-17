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

namespace CustomEyebrows
{
    [BepInPlugin("aedenthorn.CustomEyebrows", "CustomEyebrows", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<string> reloadKey;

        public static string assetPath;

        public static ConfigEntry<int> nexusID;

        private static GameObject eyebrowsGO;

        private static Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
        private static Dictionary<string, Transform> eyebrowDict = new Dictionary<string, Transform>();

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
            
            reloadKey = Config.Bind<string>("Options", "ReloadKey", "", "Key to reload tattoos from disk.");
            
            nexusID = Config.Bind<int>("General", "NexusID", 98, "Nexus mod ID for updates");
            nexusID.Value = 98;
            assetPath = AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace, true);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        private void Update()
        {
            if(reloadKey.Value.Length > 0 && AedenthornUtils.CheckKeyDown(reloadKey.Value))
            {
                LoadAllEyebrows();
                if(Global.code.uiMakeup.gameObject.activeSelf && Global.code.uiMakeup.panelEyebrows.activeSelf)
                    Global.code.uiMakeup.ButtonEyebrows();
            }
        }
        private static void LoadAllEyebrows()
        {
            RM.code.allEyebrows.items = Resources.LoadAll("Customization/Eyebrows", typeof(Transform)).Cast<Transform>().ToList();
            Transform templateT = RM.code.allEyebrows.items[0];
            int count = 0;
            try
            {
                foreach (string iconPath in Directory.GetFiles(assetPath, "*_icon.png"))
                {
                    string texPath = iconPath.Replace("_icon.png", ".png");
                    if (!File.Exists(texPath))
                        continue;

                    if (textureDict.ContainsKey(iconPath))
                    {
                        textureDict[texPath].LoadImage(File.ReadAllBytes(texPath));
                        textureDict[iconPath].LoadImage(File.ReadAllBytes(iconPath));
                        RM.code.allEyebrows.AddItem(eyebrowDict[texPath]);
                        count++;
                        continue;
                    }

                    Texture2D tex = new Texture2D(1, 1);
                    Texture2D icon = new Texture2D(1, 1);
                    tex.LoadImage(File.ReadAllBytes(texPath));
                    icon.LoadImage(File.ReadAllBytes(iconPath));
                    Transform t = Instantiate(templateT, eyebrowsGO.transform);
                    t.name = (RM.code.allEyebrows.items.Count + 1) + "";
                    t.GetComponent<CustomizationItem>().eyes = tex;
                    t.GetComponent<CustomizationItem>().icon = icon;
                    RM.code.allEyebrows.AddItem(t);
                    count++;

                    textureDict.Add(iconPath, icon);
                    textureDict.Add(texPath, tex);
                    eyebrowDict.Add(texPath, t);
                }
                Dbgl($"Got {count} eyebrows");
            }
            catch (Exception ex)
            {
                Dbgl($"Error getting eyebrows: \n\n {ex.StackTrace}");
            }
        }
        [HarmonyPatch(typeof(RM), "LoadResources")]
        static class LoadResources_Patch
        {

            static void Postfix(RM __instance)
            {
                if (!modEnabled.Value)
                    return;

                eyebrowsGO = new GameObject() { name = "CustomEyebrows" };
                DontDestroyOnLoad(eyebrowsGO);

                LoadAllEyebrows();
            }
        }
    }
}
