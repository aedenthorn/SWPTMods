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

namespace MoreSkinColor
{
    [BepInPlugin("aedenthorn.MoreSkinColor", "More Skin Color", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> modKey;
        public static ConfigEntry<string> delModKey;

        private static string assetPath;
        private static readonly string buttonPrefix = "CustomSkin";
        private static readonly string jsonFile = "skin_colors.json";
        private static List<Color> skinColorList;
        private static Color customSkinColor;
        private static Texture2D saveButtonTexture;

        private static Transform redSlider;
        private static Transform greenSlider;
        private static Transform blueSlider;
        private static Transform saveButton;

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
            
            modKey = Config.Bind<string>("General", "ModKey", "left shift", "Modifier key to overwrite an existing custom color on click");
            delModKey = Config.Bind<string>("General", "DelModKey", "left ctrl", "Modifier key to remove an existing custom color on click (warning, this shifts hair choice for characters with custom hair already!)");

            nexusID = Config.Bind<int>("General", "NexusID", 14, "Nexus mod ID for updates");

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        //[HarmonyPatch(typeof(RM), "LoadResources")]
        static class LoadResources_Patch
        {

            static void Postfix(RM __instance)
            {
                skinColorList = new List<Color>();

                string file = Path.Combine(assetPath, "save_button.png");
                if (File.Exists(file))
                {
                    saveButtonTexture = new Texture2D(2, 2);
                    byte[] data = File.ReadAllBytes(file);
                    saveButtonTexture.LoadImage(data);
                }

                LoadSkinColors();


            }

        }

        private static bool forceSliders = false;

        [HarmonyPatch(typeof(UICustomization), "ButtonCustomizationButtonClicked")]
        static class ButtonCustomizationButtonClicked_Patch
        {
            static bool Prefix(UICustomization __instance, Transform button)
            {
                return true;
                if (!modEnabled.Value || button.parent.name != __instance.skinColorGroup.name || (!AedenthornUtils.CheckKeyHeld(modKey.Value) && !AedenthornUtils.CheckKeyHeld(delModKey.Value)) || !button.name.StartsWith(buttonPrefix) || !int.TryParse(button.name.Substring(buttonPrefix.Length), out int index))
                    return true;

                index--;

                if (AedenthornUtils.CheckKeyHeld(modKey.Value))
                {
                    Dbgl($"changing {button.name} color { button.GetComponent<RawImage>().color } to {customSkinColor}");

                    button.GetComponent<RawImage>().color = customSkinColor;

                    skinColorList[index] = customSkinColor;
                    SaveSkinColors();
                    return false;
                }

                if (AedenthornUtils.CheckKeyHeld(delModKey.Value))
                {
                    Dbgl($"removing color {button.name}");

                    Color color = skinColorList[index];

                    skinColorList.Remove(color);
                    SaveSkinColors();
                    
                    if(customSkinColor == color)
                    {
                        customSkinColor = __instance.skinColorGroup.GetChild(0).GetComponent<RawImage>().color;
                        __instance.curCharacterCustomization.skinColor = customSkinColor;
                        __instance.curCharacterCustomization.RefreshAppearence();
                    }

                    LoadSkinColors();
                    __instance.ButtonSkin();
                    return false;
                }
                return true;
            }
            static void Postfix(UICustomization __instance, Transform button)
            {
                if (!modEnabled.Value || !button || button.parent.name != __instance.skinColorGroup.name || AedenthornUtils.CheckKeyHeld(modKey.Value) || AedenthornUtils.CheckKeyHeld(delModKey.Value))
                    return;
                Color c = button.GetComponent<RawImage>().color;

                forceSliders = true;

                redSlider.GetComponent<Slider>().value = c.r;
                greenSlider.GetComponent<Slider>().value = c.g;
                blueSlider.GetComponent<Slider>().value = c.b;

                forceSliders = false;
            }
        }
        
        [HarmonyPatch(typeof(UICustomization), "ButtonSkin")]
        static class ButtonHair_Patch
        {

            static void Postfix(UICustomization __instance)
            {
                if (!modEnabled.Value || __instance.panelSkin.transform.Find("skin color sliders"))
                    return;
                
                customSkinColor = __instance.skinColorGroup.GetChild(0).GetComponent<RawImage>().color;

                Transform sliderGroup = Instantiate(__instance.panelSkin.transform.Find("slider group"), __instance.panelSkin.transform);
                sliderGroup.GetChild(3).gameObject.SetActive(false);
                Destroy(sliderGroup.GetChild(3).gameObject);

                sliderGroup.name = "skin color sliders";
                sliderGroup.GetComponent<RectTransform>().anchoredPosition = __instance.panelSkin.transform.Find("slider group").GetComponent<RectTransform>().anchoredPosition + new Vector2(0,127);

                redSlider = sliderGroup.GetChild(0);
                redSlider.name = "red";

                Slider rs = redSlider.GetComponent<Slider>();
                rs.minValue = 0;
                rs.maxValue = 1;
                rs.onValueChanged = new Slider.SliderEvent();
                rs.onValueChanged.AddListener(ChangeCurrentColor);

                Transform rt = redSlider.Find("Text");
                Destroy(rt.GetComponent<LocalizationText>());
                rt.GetComponent<Text>().text = "Red";

                redSlider.gameObject.SetActive(true);


                greenSlider = sliderGroup.GetChild(1);
                greenSlider.name = "green";

                Slider gs = greenSlider.GetComponent<Slider>();
                gs.minValue = 0;
                gs.maxValue = 1;
                gs.onValueChanged = new Slider.SliderEvent();
                gs.onValueChanged.AddListener(ChangeCurrentColor);

                Transform gt = greenSlider.Find("Text");
                Destroy(gt.GetComponent<LocalizationText>());
                gt.GetComponent<Text>().text = "Green";

                greenSlider.gameObject.SetActive(true);



                blueSlider = sliderGroup.GetChild(2);
                blueSlider.name = "blue";

                Slider bs = blueSlider.GetComponent<Slider>();
                bs.minValue = 0;
                bs.maxValue = 1;
                bs.onValueChanged = new Slider.SliderEvent();
                bs.onValueChanged.AddListener(ChangeCurrentColor);

                Transform bt = blueSlider.Find("Text");
                Destroy(bt.GetComponent<LocalizationText>());
                bt.GetComponent<Text>().text = "Blue";

                blueSlider.gameObject.SetActive(true);


                /*
                saveButton = Instantiate(__instance.customizationItemButton, __instance.panelSkin.transform);
                saveButton.GetComponent<RectTransform>().anchoredPosition = redSlider.GetComponent<RectTransform>().anchoredPosition - new Vector2(115, 20);
                saveButton.localScale = Vector3.one;
                saveButton.name = "save button";
                saveButton.GetComponent<RawImage>().texture = saveButtonTexture;
                saveButton.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                saveButton.GetComponent<Button>().onClick.AddListener(AddSkinColor);
                */
            }
        }

        private static void ChangeCurrentColor(float arg0)
        {
            if (forceSliders)
                return;
            customSkinColor = new Color(redSlider.GetComponent<Slider>().value, greenSlider.GetComponent<Slider>().value, blueSlider.GetComponent<Slider>().value);
            Global.code.uiCustomization.curCharacterCustomization.skinColor = customSkinColor;
            Global.code.uiCustomization.curCharacterCustomization.RefreshAppearence();
        }

        private static void AddSkinColor()
        {
            Color newColor = customSkinColor;
            Dbgl($"Adding new color {newColor} to skin list");
            skinColorList.Add(newColor);
            SaveSkinColors();
        }
        private static void SaveSkinColors()
        {
            Dbgl("Saving skin file");
            string json = JsonUtility.ToJson(new SkinColorList() { colors = skinColorList });
            File.WriteAllText(Path.Combine(assetPath, jsonFile), json);
            Global.code.uiMakeup.ButtonHair();
        }

        private static void LoadSkinColors()
        {
            string file = Path.Combine(assetPath, jsonFile);
            if (File.Exists(file))
            {
                skinColorList = JsonUtility.FromJson<SkinColorList>(File.ReadAllText(file)).colors;
                Dbgl($"Added {skinColorList.Count} custom skin colors.");
            }
        }

    }
}
