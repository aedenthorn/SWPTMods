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

namespace EyebrowColors
{
    [BepInPlugin("aedenthorn.EyebrowColors", "Eyebrow Colors", "0.3.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<bool> followHairColor;
        private static string assetPath;
        private static Transform strengthSlider;

        private static Transform redSlider;
        private static Transform greenSlider;
        private static Transform blueSlider;
        private static Dictionary<string, Color> eyebrowColors = new Dictionary<string, Color>();

        ConfigEntry<int> nexusID;

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
            nexusID = Config.Bind<int>("General", "NexusID", 34, "Nexus mod ID for updates");

            //followHairColor = Config.Bind<bool>("Options", "FollowHairColor", false, "If set to true, when you change a character's hair colour, their eyebrow color will change as well.");
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                //Directory.CreateDirectory(assetPath);
            }
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        private static bool forceSliders = false;

        [HarmonyPatch(typeof(Mainframe), "SaveCharacterCustomization")]
        static class SaveCharacterCustomization_Patch
        {
            static void Prefix(Mainframe __instance, CharacterCustomization customization)
            {

                Color color = eyebrowColors.ContainsKey(customization.characterName) ? eyebrowColors[customization.characterName] : Color.black;
                color.a = customization.eyeBrowsStrength;

                Dbgl($"saving eyebrow color {color} for {customization.characterName} as {ColorUtility.ToHtmlStringRGBA(color)}");

                ES2.Save<string>(ColorUtility.ToHtmlStringRGBA(color), __instance.GetFolderName() + customization.name + ".txt?tag=eyebrowsColor");

            }
        }
        
        [HarmonyPatch(typeof(Mainframe), "LoadCharacterCustomization")]
        static class LoadCharacterCustomization_Patch
        {
            static void Postfix(Mainframe __instance, CharacterCustomization gen)
            {

                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=eyebrowsColor"))
                {
                    string colorCode = "#"+ ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=eyebrowsColor");
                    
                    Dbgl($"got saved eyebrow color: {colorCode} for {gen.characterName}");

                    if (colorCode != "n" && ColorUtility.TryParseHtmlString(colorCode, out Color color))
                    {
                        color.a = gen.eyeBrowsStrength;
                        Dbgl($"loaded eyebrow color: {color} for {gen.characterName}");

                        eyebrowColors[gen.characterName] = color;

                        gen.body.materials[1].SetColor("_Mask3_Rchannel_ColorAmountA", color);
                        gen.body.materials[2].SetColor("_Mask3_Rchannel_ColorAmountA", color);
                        gen.body.materials[4].SetColor("_Mask3_Rchannel_ColorAmountA", color);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Mainframe), nameof(Mainframe.SaveCharacterPreset))]
        static class SaveCharacterPreset_Patch
        {
            static void Prefix(Mainframe __instance, CharacterCustomization customization, string presetname)
            {

                Color color = eyebrowColors.ContainsKey(customization.characterName) ? eyebrowColors[customization.characterName] : Color.black;
                color.a = customization.eyeBrowsStrength;

                Dbgl($"saving eyebrow color {color} for preset {presetname} as {ColorUtility.ToHtmlStringRGBA(color)}");

                ES2.Save<string>(ColorUtility.ToHtmlStringRGBA(color), "Character Presets/" + presetname + "/CharacterPreset.txt??tag=eyebrowsColor");

            }
        }

        [HarmonyPatch(typeof(Mainframe), nameof(Mainframe.LoadCharacterPreset))]
        static class LoadCharacterPreset_Patch
        {
            static void Postfix(Mainframe __instance, CharacterCustomization gen, string presetname)
            {
                if (ES2.Exists("Character Presets/" + presetname + "/CharacterPreset.txt?tag=eyebrowsColor"))
                {
                    string colorCode = "#"+ ES2.Load<string>("Character Presets/" + presetname + "/CharacterPreset.txt?tag=eyebrowsColor");
                    
                    Dbgl($"got saved eyebrow color: {colorCode} for preset {presetname}");

                    if (colorCode != "n" && ColorUtility.TryParseHtmlString(colorCode, out Color color))
                    {
                        color.a = gen.eyeBrowsStrength;
                        Dbgl($"loaded eyebrow color: {color} for {gen.characterName}");

                        eyebrowColors[gen.characterName] = color;

                        gen.body.materials[1].SetColor("_Mask3_Rchannel_ColorAmountA", color);
                        gen.body.materials[2].SetColor("_Mask3_Rchannel_ColorAmountA", color);
                        gen.body.materials[4].SetColor("_Mask3_Rchannel_ColorAmountA", color);
                    }
                }
            }
        }

        //[HarmonyPatch(typeof(CharacterCustomization), "Awake")]
        static class CharacterCustomization_Awake_Patch
        {
            static void Prefix(CharacterCustomization __instance)
            {
                if(__instance.characterName != null && !eyebrowColors.ContainsKey(__instance.characterName))
                    eyebrowColors[__instance.characterName] = Color.black;
            }
        }
        
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.RefreshAppearence))]
        static class RefreshAppearence_Patch
        {
            static void Postfix(CharacterCustomization __instance)
            {
                if (__instance.characterName == null || !eyebrowColors.ContainsKey(__instance.characterName))
                    return;
                Color color = eyebrowColors[__instance.characterName];
                color.a = __instance.eyeBrowsStrength;
                
                //Dbgl($"refresh {__instance.characterName} eyebrow color: {color}, strength {__instance.eyeBrowsStrength}");
                
                __instance.body.materials[1].SetColor("_Mask3_Rchannel_ColorAmountA", color);
                __instance.body.materials[2].SetColor("_Mask3_Rchannel_ColorAmountA", color);
                __instance.body.materials[4].SetColor("_Mask3_Rchannel_ColorAmountA", color);
            }
        }

        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.ButtonEyebrows))]
        static class ButtonEyebrows_Patch
        {

            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value)
                    return;
                forceSliders = true;

                if (!strengthSlider)
                {
                    strengthSlider = __instance.panelEyebrows.transform.GetComponentInChildren<Slider>().transform;
                    strengthSlider.GetComponent<Slider>().onValueChanged = new Slider.SliderEvent();
                    strengthSlider.GetComponent<Slider>().onValueChanged.AddListener(ChangeCurrentColor);

                    float height = strengthSlider.GetComponent<RectTransform>().rect.height;

                    Transform scrollRect = __instance.panelEyebrows.transform.GetComponentInChildren<ScrollRect>().transform;
                    scrollRect.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, height * 3);

                    redSlider = Instantiate(strengthSlider, strengthSlider.parent);
                    redSlider.name = "red slider";
                    redSlider.GetComponent<RectTransform>().anchoredPosition = strengthSlider.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, height);

                    Slider rs = redSlider.GetComponent<Slider>();
                    rs.minValue = 0;
                    rs.maxValue = 1;
                    rs.onValueChanged = new Slider.SliderEvent();
                    rs.onValueChanged.AddListener(ChangeCurrentColor);

                    Transform rt = redSlider.Find("Text");
                    Destroy(rt.GetComponent<LocalizationText>());
                    rt.GetComponent<Text>().text = "Red";

                    redSlider.gameObject.SetActive(true);


                    greenSlider = Instantiate(strengthSlider, strengthSlider.parent);
                    greenSlider.name = "green slider";
                    greenSlider.GetComponent<RectTransform>().anchoredPosition = strengthSlider.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, height * 2);

                    Slider gs = greenSlider.GetComponent<Slider>();
                    gs.minValue = 0;
                    gs.maxValue = 1;
                    gs.onValueChanged = new Slider.SliderEvent();
                    gs.onValueChanged.AddListener(ChangeCurrentColor);

                    Transform gt = greenSlider.Find("Text");
                    Destroy(gt.GetComponent<LocalizationText>());
                    gt.GetComponent<Text>().text = "Green";

                    greenSlider.gameObject.SetActive(true);



                    blueSlider = Instantiate(strengthSlider, strengthSlider.parent);
                    blueSlider.name = "blue slider";

                    blueSlider.GetComponent<RectTransform>().anchoredPosition = strengthSlider.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, height * 3);

                    Slider bs = blueSlider.GetComponent<Slider>();
                    bs.minValue = 0;
                    bs.maxValue = 1;
                    bs.onValueChanged = new Slider.SliderEvent();
                    bs.onValueChanged.AddListener(ChangeCurrentColor);

                    Transform bt = blueSlider.Find("Text");
                    Destroy(bt.GetComponent<LocalizationText>());
                    bt.GetComponent<Text>().text = "Blue";

                    blueSlider.gameObject.SetActive(true);
                }

                if (!eyebrowColors.ContainsKey(__instance.curCustomization.characterName))
                    eyebrowColors[__instance.curCustomization.characterName] = new Color(0, 0, 0, __instance.curCustomization.eyeBrowsStrength);

                redSlider.GetComponent<Slider>().value = eyebrowColors[__instance.curCustomization.characterName].r;
                greenSlider.GetComponent<Slider>().value = eyebrowColors[__instance.curCustomization.characterName].g;
                blueSlider.GetComponent<Slider>().value = eyebrowColors[__instance.curCustomization.characterName].b;
                strengthSlider.GetComponent<Slider>().value = __instance.curCustomization.eyeBrowsStrength;


                forceSliders = false;
                Dbgl($"open eyebrows {__instance.curCustomization.characterName} eyebrow color: {eyebrowColors[__instance.curCustomization.characterName]}, strength {__instance.curCustomization.eyeBrowsStrength} {strengthSlider.GetComponent<Slider>().value}");
            }
        }

        private static void ChangeCurrentColor(float arg0)
        {
            if (forceSliders)
                return;

            Global.code.uiMakeup.curCustomization.eyeBrowsStrength = strengthSlider.GetComponent<Slider>().value;

            eyebrowColors[Global.code.uiMakeup.curCustomization.characterName] = new Color(redSlider.GetComponent<Slider>().value, greenSlider.GetComponent<Slider>().value, blueSlider.GetComponent<Slider>().value, strengthSlider.GetComponent<Slider>().value);

            //Dbgl($"color changed to {eyebrowColors[Global.code.uiMakeup.curCustomization.characterName]}");
            
            Global.code.uiMakeup.curCustomization.RefreshAppearence();

        }

    }
}
