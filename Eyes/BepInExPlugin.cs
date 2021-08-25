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

namespace Eyes
{
    [BepInPlugin("aedenthorn.Eyes", "Eyes", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<bool> followHairColor;
        private static string assetPath;

        private static Transform redSlider;
        private static Transform greenSlider;
        private static Transform blueSlider;
        private static Dictionary<string, Color> eyecolors = new Dictionary<string, Color>();

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
            nexusID = Config.Bind<int>("General", "NexusID", 40, "Nexus mod ID for updates");

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
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
                GameObject parent = new GameObject() { name = "CustomEyes" };
                DontDestroyOnLoad(parent);
                foreach(string folder in Directory.GetDirectories(assetPath))
                {
                    Transform eye = Instantiate(RM.code.allEyes.items[0], parent.transform);
                    if (File.Exists(Path.Combine(folder, "texture.png")))
                    {
                        Texture2D tex = new Texture2D(2, 2);
                        byte[] data = File.ReadAllBytes(Path.Combine(folder, "texture.png"));
                        tex.LoadImage(data);
                        eye.GetComponent<CustomizationItem>().eyes = tex;
                    }
                    if (false && File.Exists(Path.Combine(folder, "glow.txt")))
                    {
                        eye = Instantiate(RM.code.allEyes.GetItemWithName("Eye b5 green glow"), parent.transform);
                        if (ColorUtility.TryParseHtmlString(File.ReadAllText(Path.Combine(folder, "glow.txt")), out Color c))
                        {
                            Dbgl($"setting material color {c}, emission color {eye.GetComponent<CustomizationItem>().eyesMat.GetColor("_EmissionColor")}");
                            //eye.GetComponent<CustomizationItem>().eyesMat.DisableKeyword("_EMISSION");
                            Dbgl(string.Join("\n",eye.GetComponent<CustomizationItem>().eyesMat.shader.GetPropertyAttributes(1)));
                            Dbgl(eye.GetComponent<CustomizationItem>().eyesMat.GetColor("_MATERIAL_FEATURE_CLEAR_COAT")+"");
                            eye.GetComponent<CustomizationItem>().eyesMat.SetColor("_EmissionColor", c);
                        }
                    }

                    eye.name = ("CustomEyes_" + Path.GetFileNameWithoutExtension(folder));

                    Texture2D iconTex = null;
                    if (File.Exists(Path.Combine(folder, "icon.png")))
                    {
                        iconTex = new Texture2D(2, 2);
                        byte[] iconData = File.ReadAllBytes(Path.Combine(folder, "icon.png"));
                        iconTex.LoadImage(iconData);
                    }
                    if(iconTex != null)
                        eye.GetComponent<CustomizationItem>().icon = iconTex;

                    RM.code.allEyes.AddItemDifferentName(eye);
                    Dbgl($"Added custom eyes {eye.name} to RM");
                }
            }
        }

        private static bool forceSliders = false;

        //[HarmonyPatch(typeof(Mainframe), "SaveCharacterCustomization")]
        static class SaveCharacterCustomization_Patch
        {
            static void Prefix(Mainframe __instance, CharacterCustomization customization)
            {

                Color color = eyecolors.ContainsKey(customization.characterName) ? eyecolors[customization.characterName] : Color.white;

                Dbgl($"saving eye color {color} for {customization.characterName} as {ColorUtility.ToHtmlStringRGBA(color)}");

                ES2.Save<string>(ColorUtility.ToHtmlStringRGBA(color), __instance.GetFolderName() + customization.name + ".txt?tag=eyeColor");

            }
        }
        
        //[HarmonyPatch(typeof(Mainframe), "LoadCharacterCustomization")]
        static class LoadCharacterCustomization_Patch
        {
            static void Postfix(Mainframe __instance, CharacterCustomization gen)
            {

                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=eyeColor"))
                {
                    string colorCode = "#"+ ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=eyeColor");
                    
                    Dbgl($"got saved eye color: {colorCode} for {gen.characterName}");

                    if (colorCode != "n" && ColorUtility.TryParseHtmlString(colorCode, out Color color))
                    {
                        Dbgl($"loaded eye color: {color} for {gen.characterName}");

                        eyecolors[gen.characterName] = color;
                        //gen.body.materials[6].color = color;
                        gen.body.materials[9].color = color;
                        //gen.body.materials[12].color = color;
                        gen.body.materials[13].color = color;
                        //gen.body.materials[14].color = color;

                    }

                }
                else
                    eyecolors[gen.characterName] = Color.white;
            }
        }
        
        //[HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.RefreshAppearence))]
        static class RefreshAppearence_Patch
        {
            static void Postfix(CharacterCustomization __instance)
            {
                if (__instance.characterName == null || !eyecolors.ContainsKey(__instance.characterName))
                    return;
                Color color = eyecolors[__instance.characterName];

                //Dbgl($"refresh {__instance.characterName} eyebrow color: {color}, strength {__instance.eyeBrowsStrength}");

                //__instance.body.materials[6].color = color;
                __instance.body.materials[9].color = color;
                //__instance.body.materials[12].color = color;
                __instance.body.materials[13].color = color;
                //__instance.body.materials[14].color = color;

            }
        }

        //[HarmonyPatch(typeof(UICustomization), nameof(UICustomization.ButtonEyes))]
        static class ButtonEyes_Patch
        {

            static void Postfix(UICustomization __instance)
            {
                if (!modEnabled.Value)
                    return;
                forceSliders = true;

                if (!redSlider)
                {
                    Slider[] sliders = __instance.panelEyes.GetComponentsInChildren<Slider>();

                    Transform template = sliders[sliders.Length - 1].transform;

                    float height = template.GetComponent<RectTransform>().rect.height;

                    redSlider = Instantiate(template, template.parent);
                    redSlider.name = "red slider";
                    redSlider.GetComponent<RectTransform>().anchoredPosition = template.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, height);

                    Slider rs = redSlider.GetComponent<Slider>();
                    rs.minValue = 0;
                    rs.maxValue = 1;
                    rs.onValueChanged = new Slider.SliderEvent();
                    rs.onValueChanged.AddListener(ChangeCurrentColor);

                    Transform rt = redSlider.Find("Text");
                    Destroy(rt.GetComponent<LocalizationText>());
                    rt.GetComponent<Text>().text = "Red";

                    redSlider.gameObject.SetActive(true);


                    greenSlider = Instantiate(template, template.parent);
                    greenSlider.name = "green slider";
                    greenSlider.GetComponent<RectTransform>().anchoredPosition = template.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, height * 2);

                    Slider gs = greenSlider.GetComponent<Slider>();
                    gs.minValue = 0;
                    gs.maxValue = 1;
                    gs.onValueChanged = new Slider.SliderEvent();
                    gs.onValueChanged.AddListener(ChangeCurrentColor);

                    Transform gt = greenSlider.Find("Text");
                    Destroy(gt.GetComponent<LocalizationText>());
                    gt.GetComponent<Text>().text = "Green";

                    greenSlider.gameObject.SetActive(true);



                    blueSlider = Instantiate(template, template.parent);
                    blueSlider.name = "blue slider";

                    blueSlider.GetComponent<RectTransform>().anchoredPosition = template.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, height * 3);

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

                if (!eyecolors.ContainsKey(__instance.curCharacterCustomization.characterName))
                    eyecolors[__instance.curCharacterCustomization.characterName] = new Color(1, 1, 1);

                redSlider.GetComponent<Slider>().value = eyecolors[__instance.curCharacterCustomization.characterName].r;
                greenSlider.GetComponent<Slider>().value = eyecolors[__instance.curCharacterCustomization.characterName].g;
                blueSlider.GetComponent<Slider>().value = eyecolors[__instance.curCharacterCustomization.characterName].b;

                forceSliders = false;
                Dbgl($"open eyes {__instance.curCharacterCustomization.characterName} eye color: {eyecolors[__instance.curCharacterCustomization.characterName]}");
            }
        }

        private static void ChangeCurrentColor(float arg0)
        {
            if (forceSliders)
                return;

            eyecolors[Global.code.uiCustomization.curCharacterCustomization.characterName] = new Color(redSlider.GetComponent<Slider>().value, greenSlider.GetComponent<Slider>().value, blueSlider.GetComponent<Slider>().value);

            Global.code.uiCustomization.curCharacterCustomization.RefreshAppearence();

        }

    }
}
