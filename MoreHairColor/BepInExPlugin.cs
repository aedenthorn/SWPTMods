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

namespace MoreHairColor
{
    [BepInPlugin("aedenthorn.MoreHairColor", "More Hair Color", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> modKey;
        public static ConfigEntry<string> delModKey;

        public static string assetPath;
        public static readonly string buttonPrefix = "CustomHair";
        public static List<Color> hairColorList;
        public static Transform customHairColor;
        public static Texture2D hairTemplate;
        public static Texture2D saveButtonTexture;

        public static Transform redSlider;
        public static Transform greenSlider;
        public static Transform blueSlider;
        public static Transform saveButton;

        public static ConfigEntry<int> nexusID;

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

            nexusID = Config.Bind<int>("General", "NexusID", 11, "Nexus mod ID for updates");

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(RM), "LoadResources")]
        static class LoadResources_Patch
        {

            static void Postfix(RM __instance)
            {
                hairColorList = new List<Color>();

                string file = Path.Combine(assetPath, "hair_template.png");
                if (File.Exists(file))
                {
                    hairTemplate = new Texture2D(2, 2);
                    byte[] data = File.ReadAllBytes(file);
                    hairTemplate.LoadImage(data);
                }

                file = Path.Combine(assetPath, "save_button.png");
                if (File.Exists(file))
                {
                    saveButtonTexture = new Texture2D(2, 2);
                    byte[] data = File.ReadAllBytes(file);
                    saveButtonTexture.LoadImage(data);
                }

                LoadHairColors();


            }

        }

        public static bool forceSliders = false;

        [HarmonyPatch(typeof(UIMakeup), "ButtonCustomizationButtonClicked")]
        static class ButtonCustomizationButtonClicked_Patch
        {
            static bool Prefix(UIMakeup __instance, Transform button)
            {
                if (!modEnabled.Value || button.parent.name != __instance.hairColorGroup.name || (!AedenthornUtils.CheckKeyHeld(modKey.Value) && !AedenthornUtils.CheckKeyHeld(delModKey.Value)) || !button.name.StartsWith(buttonPrefix) || !int.TryParse(button.name.Substring(buttonPrefix.Length), out int index))
                    return true;

                index--;

                if (AedenthornUtils.CheckKeyHeld(modKey.Value))
                {
                    Dbgl($"changing {button.name} color {RM.code.allHairColors.GetItemWithName(button.name).GetComponent<CustomizationItem>().color} to {customHairColor.GetComponent<CustomizationItem>().color}");

                    customHairColor.name = button.name;

                    hairColorList[index] = customHairColor.GetComponent<CustomizationItem>().color;
                    RM.code.allHairColors.GetItemWithName(button.name).GetComponent<CustomizationItem>().color = hairColorList[index];
                    RM.code.allHairColors.GetItemWithName(button.name).GetComponent<CustomizationItem>().icon = MakeHairTexture(hairColorList[index]);
                    SaveHairColors();
                    return false;
                }

                if (AedenthornUtils.CheckKeyHeld(delModKey.Value))
                {
                    Dbgl($"removing color {button.name}");

                    Color color = RM.code.allHairColors.GetItemWithName(button.name).GetComponent<CustomizationItem>().color;

                    hairColorList.Remove(color);
                    SaveHairColors();
                    
                    if(customHairColor.GetComponent<CustomizationItem>().color == color)
                    {
                        customHairColor.GetComponent<CustomizationItem>().color = RM.code.allHairColors.items[0].GetComponent<CustomizationItem>().color;
                        __instance.curCustomization.hairColor = RM.code.allHairColors.items[0];
                        __instance.curCustomization.RefreshAppearence();
                    }

                    RM.code.allHairColors.items = Resources.LoadAll("Customization/HairColors", typeof(Transform)).Cast<Transform>().ToList();
                    LoadHairColors();
                    __instance.ButtonHair();
                    return false;
                }
                return true;
            }
            static void Postfix(UIMakeup __instance, Transform button)
            {
                if (!modEnabled.Value || !button || button.parent.name != __instance.hairColorGroup.name || AedenthornUtils.CheckKeyHeld(modKey.Value) || AedenthornUtils.CheckKeyHeld(delModKey.Value))
                    return;
                Color c = RM.code.allHairColors.GetItemWithName(button.name).GetComponent<CustomizationItem>().color;

                forceSliders = true;

                redSlider.GetComponent<Slider>().value = c.r;
                greenSlider.GetComponent<Slider>().value = c.g;
                blueSlider.GetComponent<Slider>().value = c.b;

                forceSliders = false;
            }
        }
        
        [HarmonyPatch(typeof(UIMakeup), "ButtonHair")]
        static class ButtonHair_Patch
        {

            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value || __instance.panelHair.transform.Find("save button"))
                    return;

                Dbgl($"Total hair colors: {RM.code.allHairColors.items.Count}");

                /*
                foreach (Transform ac in RM.code.allHairColors.items)
                {
                    Dbgl(ac.name);
                }
                */

                customHairColor = Instantiate(RM.code.allHairColors.items[0]);
                customHairColor.GetComponent<CustomizationItem>().color = Color.black;
                DontDestroyOnLoad(customHairColor);
                redSlider = Instantiate(__instance.panelLips.transform.Find("lip gloss"), __instance.panelHair.transform);
                Slider rs = redSlider.GetComponent<Slider>();
                rs.minValue = 0;
                rs.maxValue = 1;
                rs.onValueChanged = new Slider.SliderEvent();
                rs.onValueChanged.AddListener(ChangeCurrentColor);

                redSlider.name = "red";

                Transform rt = redSlider.Find("Text");
                Destroy(rt.GetComponent<LocalizationText>());
                rt.GetComponent<Text>().text = "Red";
                rt.GetComponent<RectTransform>().anchoredPosition += new Vector2(60,0);

                redSlider.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, 110);


                float height = 20;

                greenSlider = Instantiate(redSlider, __instance.panelHair.transform);
                greenSlider.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, height);
                greenSlider.name = "green";
                greenSlider.Find("Text").GetComponent<Text>().text = "Green";

                Slider gs = greenSlider.GetComponent<Slider>();
                gs.minValue = 0;
                gs.maxValue = 1;
                gs.onValueChanged = new Slider.SliderEvent();
                gs.onValueChanged.AddListener(ChangeCurrentColor);


                blueSlider = Instantiate(redSlider, __instance.panelHair.transform);
                blueSlider.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, height * 2);
                blueSlider.name = "blue";
                blueSlider.Find("Text").GetComponent<Text>().text = "Blue";

                Slider bs = blueSlider.GetComponent<Slider>();
                bs.minValue = 0;
                bs.maxValue = 1;
                bs.onValueChanged = new Slider.SliderEvent();
                bs.onValueChanged.AddListener(ChangeCurrentColor);

                saveButton = Instantiate(__instance.customizationItemButton, __instance.panelHair.transform);
                saveButton.GetComponent<RectTransform>().anchoredPosition = redSlider.GetComponent<RectTransform>().anchoredPosition - new Vector2(115, 20);
                saveButton.localScale = Vector3.one;
                saveButton.name = "save button";
                saveButton.GetComponent<RawImage>().texture = saveButtonTexture;
                saveButton.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                saveButton.GetComponent<Button>().onClick.AddListener(AddHairColor);

            }
        }

        public static void ChangeCurrentColor(float arg0)
        {
            if (forceSliders)
                return;
            customHairColor.GetComponent<CustomizationItem>().color = new Color(redSlider.GetComponent<Slider>().value, greenSlider.GetComponent<Slider>().value, blueSlider.GetComponent<Slider>().value);
            Global.code.uiMakeup.curCustomization.hairColor = customHairColor;
            Global.code.uiMakeup.curCustomization.RefreshAppearence();
        }

        public static void AddHairColor()
        {
            Color newColor = customHairColor.GetComponent<CustomizationItem>().color;
            Dbgl($"Adding new color {newColor} to hair list");
            hairColorList.Add(newColor);
            customHairColor.name = buttonPrefix + hairColorList.Count;
            Transform newTransform = Instantiate(customHairColor);
            DontDestroyOnLoad(newTransform);
            newTransform.GetComponent<CustomizationItem>().icon = MakeHairTexture(newColor);
            newTransform.name = buttonPrefix + hairColorList.Count;
            RM.code.allHairColors.AddItem(newTransform);
            SaveHairColors();
        }
        public static void SaveHairColors()
        {
            Dbgl("Saving hair file");
            string json = JsonUtility.ToJson(new HairColorList() { colors = hairColorList });
            File.WriteAllText(Path.Combine(assetPath, "hair_colors.json"), json);
            Global.code.uiMakeup.ButtonHair();
        }

        public static Texture2D MakeHairTexture(Color color)
        {
            Texture2D temp = new Texture2D(hairTemplate.width, hairTemplate.height);
            
            Color[] data = hairTemplate.GetPixels();
            for(int i = 0; i < data.Length; i++)
            {
                data[i] = Color.Lerp(data[i], color, 0.50f);
            }
            temp.SetPixels(data);
            temp.Apply();
            return temp;
        }
        public static void LoadHairColors()
        {
            string file = Path.Combine(assetPath, "hair_colors.json");
            if (File.Exists(file))
            {
                hairColorList = JsonUtility.FromJson<HairColorList>(File.ReadAllText(file)).colors;
                GameObject parent = new GameObject() { name = "CustomHair" };
                for (int i = 0; i < hairColorList.Count; i++)
                {
                    DontDestroyOnLoad(parent);
                    Transform t = Instantiate(RM.code.allHairColors.items[0], parent.transform);
                    t.GetComponent<CustomizationItem>().color = hairColorList[i];
                    t.GetComponent<CustomizationItem>().icon = MakeHairTexture(hairColorList[i]);
                    t.name = buttonPrefix + (i + 1);
                    RM.code.allHairColors.AddItem(t);
                }
                Dbgl($"Added {hairColorList.Count} custom hair colors. Total hair colors: {RM.code.allHairColors.items.Count} ({RM.code.allHairColors.items.Count}); last one {RM.code.allHairColors.items[RM.code.allHairColors.items.Count - 1].name}");
            }
        }

    }
}
