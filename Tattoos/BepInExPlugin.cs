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

namespace Tattoos
{
    [BepInPlugin("aedenthorn.Tattoos", "Tattoos", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static string assetPath;
        public static readonly string buttonPrefix = "Tattoos";

        public static ConfigEntry<int> nexusID;
        public static List<Texture2D> tattooList = new List<Texture2D>();

        private static GameObject tattooGO;
        private static Slider glossSlider;
        private static Slider strengthSlider;

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
            
            nexusID = Config.Bind<int>("General", "NexusID", 62, "Nexus mod ID for updates");

            assetPath = AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace, true);


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.RefreshAppearence))]
        static class RefreshAppearence_Patch
        {

            static void Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (__instance.wombTattoo)
                {
                    __instance.body.materials[0].SetTexture("_MakeUpMask2_RGB", __instance.wombTattoo.GetComponent<CustomizationItem>().eyes);
                    __instance.wombTattooColor.a = __instance.wombTattooStrength;
                    __instance.body.materials[0].SetColor("_Mask2_Rchannel_ColorAmountA", __instance.wombTattooColor);
                    __instance.body.materials[0].SetFloat("_GlossAdjust_Mask2Rchannel", __instance.wombTattooGlossiness);
                }
            }
        }
        [HarmonyPatch(typeof(RM), "LoadResources")]
        static class LoadResources_Patch
        {

            static void Postfix(RM __instance)
            {
                if (!modEnabled.Value)
                    return;

                tattooGO = new GameObject() { name = "CustomTattoos" };
                DontDestroyOnLoad(tattooGO);

                LoadAllTattoos("Womb", ref RM.code.allWombTattoos);
                LoadAllTattoos("Arms", ref RM.code.allArmsTatoos);
                LoadAllTattoos("Legs", ref RM.code.allLegsTatoos);
                LoadAllTattoos("Face", ref RM.code.allFaceTatoos);
                LoadAllTattoos("Body", ref RM.code.allBodyTatoos);

            }
        }
        [HarmonyPatch(typeof(UIColorPick), nameof(UIColorPick.UpdateColor))]
        static class UpdateColor_Patch
        {

            static void Postfix(UIColorPick __instance, string ___curplace)
            {
                if (!modEnabled.Value || ___curplace != "Womb Tatoo Color Picker")
                    return;

                Global.code.uiMakeup.curCustomization.wombTattooColor = __instance.Paint.color;
                Global.code.uiMakeup.curCustomization.RefreshAppearence();
            }
        }
        private static void LoadAllTattoos(string folder, ref CommonArray resources)
        {
            Texture2D templateTex = new Texture2D(1, 1);
            Texture2D template_icon = new Texture2D(1, 1);
            Transform templateT = RM.code.allWombTattoos.items[0];

            try
            {
                foreach (string file in Directory.GetFiles(Path.Combine(assetPath, folder), "*_icon.png"))
                {
                    template_icon.LoadImage(File.ReadAllBytes(file));
                    templateTex.LoadImage(File.ReadAllBytes(file.Replace("_icon.png", ".png")));
                    Transform t = Instantiate(templateT, tattooGO.transform);
                    t.name = (resources.items.Count + 1) + "";
                    t.GetComponent<CustomizationItem>().eyes = templateTex;
                    t.GetComponent<CustomizationItem>().icon = template_icon;
                    resources.AddItem(t);
                }
            }
            catch { }
        }

        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.ButtonPubicHair))]
        static class ButtonPubicHair_Patch
        {

            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value)
                    return;

                for(int i = 0; i < __instance.panelPubicHair.transform.childCount; i++)
                {
                    Transform c = __instance.panelPubicHair.transform.GetChild(i);
                    if (!c.gameObject.activeSelf)
                    {
                        c.gameObject.SetActive(true);
                        if(c.name.Contains("Color Picker"))
                        {
                            Dbgl($"Initializing color picker");
                            c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                            c.GetComponent<Button>().onClick.AddListener(delegate() { Global.code.uiColorPick.Open(__instance.curCustomization.wombTattooColor, c.name); });
                        }
                        else if (c.GetComponentInChildren<Slider>())
                        {
                            c.GetComponent<Slider>().onValueChanged = new Slider.SliderEvent();
                            if (c.name.Contains("trength"))
                            {
                                Dbgl($"Initializing strength slider");
                                c.GetComponentInChildren<LocalizationText>().KEY = "Womb Tattoo Strength";
                                strengthSlider = c.GetComponent<Slider>();
                                strengthSlider.onValueChanged.AddListener(delegate (float arg0) { __instance.curCustomization.wombTattooStrength = arg0; __instance.curCustomization.RefreshAppearence(); });
                            }
                            else if (c.name.Contains("Glossiness"))
                            {
                                Dbgl($"Initializing gloss slider");
                                c.GetComponentInChildren<LocalizationText>().KEY = "Womb Tattoo Glossiness";
                                glossSlider = c.GetComponent<Slider>();
                                glossSlider.onValueChanged.AddListener(delegate (float arg0) { __instance.curCustomization.wombTattooGlossiness = arg0; __instance.curCustomization.RefreshAppearence(); });
                            }
                        }
                    }
                }
                strengthSlider.value = __instance.curCustomization.wombTattooStrength;
                glossSlider.value = __instance.curCustomization.wombTattooGlossiness;
                for (int j = 0; j < __instance.wombTattooGroup.childCount; j++)
                {
                    Transform c = __instance.wombTattooGroup.GetChild(j);
                    if (c)
                    {
                        Dbgl($"clicked womb tattoo");
                        c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        c.GetComponent<Button>().onClick.AddListener(delegate () { __instance.curCustomization.wombTattoo = RM.code.allWombTattoos.GetItemWithName(c.name); __instance.curCustomization.RefreshAppearence(); });
                    }
                }
            }
        }
    }
}
