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
    [BepInPlugin("aedenthorn.Tattoos", "Tattoos", "0.6.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<bool> sameVariablesAllTattoos;
        public static ConfigEntry<string> reloadKey;

        public static string assetPath;
        public static readonly string buttonPrefix = "Tattoos";

        public static ConfigEntry<int> nexusID;
        public static List<Texture2D> tattooTextureList = new List<Texture2D>();

        private static GameObject tattooGO;
        private static Slider glossSlider;
        private static Slider strengthSlider;

        private static Texture2D defaultWombIcon;
        private static Dictionary<string, Texture2D> textureDict = new Dictionary<string, Texture2D>();
        private static Dictionary<string, Transform> tattooDict = new Dictionary<string, Transform>();

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
            
            sameVariablesAllTattoos = Config.Bind<bool>("Options", "SameVariablesAllTattoos", true, "Use color, gloss, and strength from womb tattoo for all tattoos. This is necessary for now.");
            reloadKey = Config.Bind<string>("Options", "ReloadKey", "page down", "Key to reload tattoos from disk.");
            
            nexusID = Config.Bind<int>("General", "NexusID", 62, "Nexus mod ID for updates");

            assetPath = AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace, true);
            if (!Directory.Exists(Path.Combine(assetPath, "Womb")))
                Directory.CreateDirectory(Path.Combine(assetPath, "Womb"));
            if (!Directory.Exists(Path.Combine(assetPath, "Face")))
                Directory.CreateDirectory(Path.Combine(assetPath, "Face"));
            if (!Directory.Exists(Path.Combine(assetPath, "Body")))
                Directory.CreateDirectory(Path.Combine(assetPath, "Body"));
            if (!Directory.Exists(Path.Combine(assetPath, "Arms")))
                Directory.CreateDirectory(Path.Combine(assetPath, "Arms"));
            if (!Directory.Exists(Path.Combine(assetPath, "Legs")))
                Directory.CreateDirectory(Path.Combine(assetPath, "Legs"));
            if (!Directory.Exists(Path.Combine(assetPath, "Pubic")))
                Directory.CreateDirectory(Path.Combine(assetPath, "Pubic"));

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        private void Update()
        {
            if(AedenthornUtils.CheckKeyDown(reloadKey.Value))
            {
                LoadAllTattoos();
                if(Global.code.uiMakeup.gameObject.activeSelf)
                {
                    if (Global.code.uiMakeup.panelBodyTatoos.activeSelf)
                        Global.code.uiMakeup.ButtonTatooBody();
                    else if (Global.code.uiMakeup.panelLegTatoos.activeSelf)
                        Global.code.uiMakeup.ButtonTatooLegs();
                    else if (Global.code.uiMakeup.panelFace.activeSelf)
                        Global.code.uiMakeup.ButtonFace();
                    else if (Global.code.uiMakeup.panelPubicHair.activeSelf)
                        Global.code.uiMakeup.ButtonPubicHair();
                }
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.RefreshAppearence))]
        static class RefreshAppearence_Patch
        {

            static void Postfix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (__instance.wombTattoo && !__instance.bodyTatoos)
                {
                    __instance.body.materials[0].SetTexture("_MakeUpMask2_RGB", __instance.wombTattoo.GetComponent<CustomizationItem>().eyes);
                    __instance.wombTattooColor.a = __instance.wombTattooStrength;
                    __instance.body.materials[0].SetColor("_Mask2_Rchannel_ColorAmountA", __instance.wombTattooColor);
                    __instance.body.materials[0].SetFloat("_GlossAdjust_Mask2Rchannel", __instance.wombTattooGlossiness);
                }
                if (__instance.faceTatoos && sameVariablesAllTattoos.Value)
                {
                    __instance.body.materials[1].SetColor("_Mask2_Rchannel_ColorAmountA", __instance.wombTattooColor);
                    __instance.body.materials[1].SetFloat("_GlossAdjust_Mask2Rchannel", __instance.wombTattooGlossiness);
                }
                if (__instance.bodyTatoos && sameVariablesAllTattoos.Value)
                {
                    __instance.body.materials[0].SetColor("_Mask2_Rchannel_ColorAmountA", __instance.wombTattooColor);
                    __instance.body.materials[0].SetFloat("_GlossAdjust_Mask2Rchannel", __instance.wombTattooGlossiness);
                }
                if (__instance.armsTatoos && sameVariablesAllTattoos.Value)
                {
                    __instance.body.materials[8].SetColor("_Mask2_Rchannel_ColorAmountA", __instance.wombTattooColor);
                    __instance.body.materials[8].SetFloat("_GlossAdjust_Mask2Rchannel", __instance.wombTattooGlossiness);
                }
                if (__instance.legsTatoos && sameVariablesAllTattoos.Value)
                {
                    __instance.body.materials[5].SetColor("_Mask2_Rchannel_ColorAmountA", __instance.wombTattooColor);
                    __instance.body.materials[5].SetFloat("_GlossAdjust_Mask2Rchannel", __instance.wombTattooGlossiness);
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

                defaultWombIcon = new Texture2D(1, 1);

                if(File.Exists(Path.Combine(assetPath, "wombIcon.png")))
                    defaultWombIcon.LoadImage(File.ReadAllBytes(Path.Combine(assetPath, "wombIcon.png")));

                LoadAllTattoos();
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
        private static void LoadAllTattoos()
        {
            RM.code.allPubicHairs.items = Resources.LoadAll("Customization/PubicHairs", typeof(Transform)).Cast<Transform>().ToList<Transform>();
            RM.code.allWombTattoos.items = Resources.LoadAll("Customization/WombTattoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
            RM.code.allBodyTatoos.items = Resources.LoadAll("Customization/BodyTatoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
            RM.code.allLegsTatoos.items = Resources.LoadAll("Customization/LegsTatoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
            RM.code.allArmsTatoos.items = Resources.LoadAll("Customization/ArmsTatoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
            RM.code.allFaceTatoos.items = Resources.LoadAll("Customization/FaceTatoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
            LoadTattoos("Pubic", ref RM.code.allPubicHairs);
            LoadTattoos("Womb", ref RM.code.allWombTattoos);
            LoadTattoos("Arms", ref RM.code.allArmsTatoos);
            LoadTattoos("Legs", ref RM.code.allLegsTatoos);
            LoadTattoos("Face", ref RM.code.allFaceTatoos);
            LoadTattoos("Body", ref RM.code.allBodyTatoos);
        }
        private static void LoadTattoos(string folder, ref CommonArray resources)
        {
            Transform templateT = RM.code.allWombTattoos.items[0];
            int count = 0;
            try
            {
                foreach (string iconPath in Directory.GetFiles(Path.Combine(assetPath, folder), "*_icon.png"))
                {
                    string texPath = iconPath.Replace("_icon.png", ".png");
                    if (!File.Exists(texPath))
                        continue;

                    if (textureDict.ContainsKey(iconPath))
                    {
                        textureDict[texPath].LoadImage(File.ReadAllBytes(texPath));
                        textureDict[iconPath].LoadImage(File.ReadAllBytes(iconPath));
                        resources.AddItem(tattooDict[texPath]);
                        count++;
                        continue;
                    }

                    Texture2D tex = new Texture2D(1, 1);
                    Texture2D icon = new Texture2D(1, 1);
                    tex.LoadImage(File.ReadAllBytes(texPath));
                    icon.LoadImage(File.ReadAllBytes(iconPath));
                    Transform t = Instantiate(templateT, tattooGO.transform);
                    t.name = (resources.items.Count + 1) + "";
                    t.GetComponent<CustomizationItem>().eyes = tex;
                    t.GetComponent<CustomizationItem>().icon = icon;
                    resources.AddItem(t);
                    count++;

                    textureDict.Add(iconPath, icon);
                    textureDict.Add(texPath, tex);
                    tattooDict.Add(texPath, t);
                }
                Dbgl($"Got {count} {folder} tattoos");
            }
            catch (Exception ex)
            {
                Dbgl($"Error getting {folder} tattoos: \n\n {ex.StackTrace}");
            }
        }

        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.Open))]
        static class UIMakeup_Open_Patch
        {
            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value)
                    return;

                Sprite legsSprite = Global.code.uiInventory.transform.Find("Left (1)/Options Group/Button Skills/Button Lips (1)")?.GetComponent<Image>()?.sprite;
                Transform legsButton = __instance.panelPubicHair.transform.parent.Find("Category (1)/Button Legs");

                if(!legsButton || !legsSprite)
                {
                    Dbgl($"Error getting button {legsButton?.name} and sprite {legsSprite?.name}");
                    return;
                }

                if (legsButton.gameObject.activeSelf)
                    return;

                legsButton.gameObject.SetActive(true);

                legsButton.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(-178, legsButton.parent.GetComponent<RectTransform>().anchoredPosition.y);

                Transform t = Instantiate(legsButton, legsButton.parent);
                t.name = "Button Body";
                t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t.GetComponent<Button>().onClick.AddListener(__instance.ButtonTatooBody);

                legsButton.GetComponent<Image>().sprite = legsSprite;
                legsButton.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                legsButton.GetComponent<Button>().onClick.AddListener(__instance.ButtonTatooLegs);

                if (!__instance.legsTatooGroup)
                {
                    __instance.legsTatooGroup = Instantiate(__instance.wombTattooGroup, __instance.panelLegTatoos.transform);
                    __instance.legsTatooGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, __instance.panelLegTatoos.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition.y - __instance.panelLegTatoos.transform.GetChild(0).GetComponent<RectTransform>().rect.height);
                }
                if (!__instance.bodyTatooGroup)
                {
                    __instance.bodyTatooGroup = Instantiate(__instance.wombTattooGroup, __instance.panelBodyTatoos.transform);
                    __instance.bodyTatooGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, __instance.panelBodyTatoos.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition.y - __instance.panelBodyTatoos.transform.GetChild(0).GetComponent<RectTransform>().rect.height);

                }
                if (!__instance.armsTatooGroup)
                {
                    __instance.armsTatooGroup = Instantiate(__instance.wombTattooGroup, __instance.panelBodyTatoos.transform);
                    __instance.armsTatooGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, __instance.panelBodyTatoos.transform.GetChild(1).GetComponent<RectTransform>().anchoredPosition.y - __instance.panelBodyTatoos.transform.GetChild(1).GetComponent<RectTransform>().rect.height);
                }
            }
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
                    else if (c.name == "Scroll View Pubic")
                    {
                        c.GetComponent<RectTransform>().sizeDelta = new Vector2(212.3437f, 58.978f);
                    }
                }
                strengthSlider.value = __instance.curCustomization.wombTattooStrength;
                glossSlider.value = __instance.curCustomization.wombTattooGlossiness;
                for (int j = 0; j < __instance.wombTattooGroup.childCount; j++)
                {
                    Transform c = __instance.wombTattooGroup.GetChild(j);
                    if (c)
                    {
                        if(c.name == "1")
                        {
                            
                            c.GetComponent<RawImage>().texture = defaultWombIcon;
                        }
                        c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        c.GetComponent<Button>().onClick.AddListener(delegate () {
                            __instance.curCustomization.wombTattoo = RM.code.allWombTattoos.GetItemWithName(c.name); 
                            __instance.curCustomization.RefreshAppearence();
                        });
                    }
                }

                // cancel

                Transform t = Instantiate(__instance.customizationItemButton, __instance.wombTattooGroup);
                t.name = "0";
                t.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t.SetAsFirstSibling();
                t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t.GetComponent<Button>().onClick.AddListener(delegate()
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.wombTattoo = null;
                    __instance.curCustomization.body.materials[0].SetTexture("_MakeUpMask2_RGB", null);
                });

                Transform t2 = Instantiate(__instance.customizationItemButton, __instance.pubicHairGroup);
                t2.name = "0";
                t2.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t2.SetAsFirstSibling();
                t2.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t2.GetComponent<Button>().onClick.AddListener(delegate()
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.pubicHair = null;
                    __instance.curCustomization.body.materials[0].SetTexture("_MakeUpMask1_RGB", null);
                    __instance.curCustomization.body.materials[16].SetTexture("_MakeUpMask1_RGB", null);
                    __instance.curCustomization.body.materials[17].SetTexture("_MakeUpMask1_RGB", null);
                });
            }
        }
        
        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.ButtonTatooLegs))]
        static class ButtonTatooLegs_Patch
        {
            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (!__instance.legsTatooGroup.parent.GetComponent<Mask>())
                {
                    //Destroy(__instance.bodyTatooGroup.GetComponent<ContentSizeFitter>());
                    Transform svl = Instantiate(__instance.blushColorGroup.parent.parent, __instance.legsTatooGroup.parent);
                    svl.name = "Legs Tattoo Scroll View";
                    Destroy(svl.GetComponentInChildren<Mask>().transform.GetChild(0).gameObject);
                    __instance.legsTatooGroup.SetParent(svl.GetComponentInChildren<Mask>().transform);

                    svl.GetComponent<ScrollRect>().viewport = svl.GetComponentInChildren<Mask>().GetComponent<RectTransform>();
                    svl.GetComponent<ScrollRect>().content = __instance.legsTatooGroup.GetComponent<RectTransform>();
                    svl.GetComponent<ScrollRect>().verticalNormalizedPosition = 0;

                    svl.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0);
                    svl.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
                    svl.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, svl.parent.GetChild(0).GetComponent<RectTransform>().sizeDelta.y);
                    __instance.legsTatooGroup.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    svl.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, (__instance.customizationItemButton.GetComponent<RectTransform>().rect.height + __instance.legsTatooGroup.GetComponent<GridLayoutGroup>().spacing.y) * 5);

                    __instance.legsTatooGroup.name = "Legs Tattoo Group";
                }


                Global.code.uiPose.ButtonCamera("Hidden Camera 1");
                Global.code.uiPose.ButtonCamera("Free Camera");
                Global.code.uiPose.PoseButtonClicked(__instance.curMakeupTable.pubicPose);
                __instance.curCustomization.lipstickModel.SetActive(false);

                for (int j = 0; j < __instance.legsTatooGroup.childCount; j++)
                {
                    Transform c = __instance.legsTatooGroup.GetChild(j);
                    if (c)
                    {
                        c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        c.GetComponent<Button>().onClick.AddListener(delegate () { __instance.curCustomization.legsTatoos = RM.code.allLegsTatoos.GetItemWithName(c.name); __instance.curCustomization.RefreshAppearence(); });
                    }
                }
                Transform t = Instantiate(__instance.customizationItemButton, __instance.legsTatooGroup);
                t.name = "0";
                t.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t.SetAsFirstSibling();
                t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t.GetComponent<Button>().onClick.AddListener(delegate ()
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.legsTatoos = null;
                    __instance.curCustomization.body.materials[5].SetTexture("_MakeUpMask2_RGB", null);
                });
            }
        }
        
        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.ButtonTatooBody))]
        static class ButtonTatooBody_Patch
        {

            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value)
                    return;

                if(__instance.bodyTatooGroup.parent == __instance.armsTatooGroup.parent)
                {
                    //Destroy(__instance.bodyTatooGroup.GetComponent<ContentSizeFitter>());
                    Transform svb = Instantiate(__instance.blushColorGroup.parent.parent, __instance.bodyTatooGroup.parent);
                    svb.name = "Body Tattoo Scroll View";
                    Destroy(svb.GetComponentInChildren<Mask>().transform.GetChild(0).gameObject);
                    __instance.bodyTatooGroup.SetParent(svb.GetComponentInChildren<Mask>().transform);

                    svb.GetComponent<ScrollRect>().viewport = svb.GetComponentInChildren<Mask>().GetComponent<RectTransform>();
                    svb.GetComponent<ScrollRect>().content = __instance.bodyTatooGroup.GetComponent<RectTransform>();
                    svb.GetComponent<ScrollRect>().verticalNormalizedPosition = 0;

                    __instance.bodyTatooGroup.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    __instance.bodyTatooGroup.name = "Body Tattoo Group";

                    //Destroy(__instance.armsTatooGroup.GetComponent<ContentSizeFitter>());
                    Transform sva = Instantiate(__instance.faceTatooGroup.parent.parent, __instance.armsTatooGroup.parent);
                    sva.name = "Arms Tattoo Scroll View";
                    Destroy(sva.GetComponentInChildren<Mask>().transform.GetChild(0).gameObject);
                    __instance.armsTatooGroup.SetParent(sva.GetComponentInChildren<Mask>().transform);

                    sva.GetComponent<ScrollRect>().viewport = sva.GetComponentInChildren<Mask>().GetComponent<RectTransform>();
                    sva.GetComponent<ScrollRect>().content = __instance.armsTatooGroup.GetComponent<RectTransform>();
                    sva.GetComponent<ScrollRect>().verticalNormalizedPosition = 0;

                    __instance.armsTatooGroup.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                    __instance.armsTatooGroup.name = "Arms Tattoo Group";

                }

                Global.code.uiPose.ButtonCamera("Hidden Camera 1");
                Global.code.uiPose.ButtonCamera("Free Camera");
                Global.code.uiPose.PoseButtonClicked(__instance.curMakeupTable.pubicPose);
                __instance.curCustomization.lipstickModel.SetActive(false);

                for (int j = 0; j < __instance.armsTatooGroup.childCount; j++)
                {
                    Transform c = __instance.armsTatooGroup.GetChild(j);
                    if (c)
                    {
                        Dbgl(c.name);
                        c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        c.GetComponent<Button>().onClick.AddListener(delegate () { __instance.curCustomization.armsTatoos = RM.code.allArmsTatoos.GetItemWithName(c.name); __instance.curCustomization.RefreshAppearence(); });
                    }
                }
                for (int j = 0; j < __instance.bodyTatooGroup.childCount; j++)
                {
                    Transform c = __instance.bodyTatooGroup.GetChild(j);
                    if (c)
                    {
                        Dbgl(c.name);
                        c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        c.GetComponent<Button>().onClick.AddListener(delegate () { __instance.curCustomization.bodyTatoos = RM.code.allBodyTatoos.GetItemWithName(c.name); __instance.curCustomization.RefreshAppearence(); });
                    }
                }

                Transform t = Instantiate(__instance.customizationItemButton, __instance.armsTatooGroup);
                t.name = "0";
                t.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t.SetAsFirstSibling();
                t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t.GetComponent<Button>().onClick.AddListener(delegate ()
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.armsTatoos = null;
                    __instance.curCustomization.body.materials[8].SetTexture("_MakeUpMask2_RGB", null);
                });

                Transform t2 = Instantiate(__instance.customizationItemButton, __instance.bodyTatooGroup);
                t2.name = "0";
                t2.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t2.SetAsFirstSibling();
                t2.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t2.GetComponent<Button>().onClick.AddListener(delegate ()
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.bodyTatoos = null;
                    if(__instance.curCustomization.wombTattoo == null)
                        __instance.curCustomization.body.materials[0].SetTexture("_MakeUpMask2_RGB", null);
                    else
                        __instance.curCustomization.body.materials[0].SetTexture("_MakeUpMask2_RGB", __instance.curCustomization.wombTattoo.GetComponent<CustomizationItem>().eyes);
                });

            }
        }
        
        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.ButtonFace))]
        static class ButtonFace_Patch
        {

            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value)
                    return;

                for (int j = 0; j < __instance.faceTatooGroup.childCount; j++)
                {
                    Transform c = __instance.faceTatooGroup.GetChild(j);
                    if (c)
                    {
                        c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        c.GetComponent<Button>().onClick.AddListener(delegate () { __instance.curCustomization.faceTatoos = RM.code.allFaceTatoos.GetItemWithName(c.name); __instance.curCustomization.RefreshAppearence(); });
                    }
                }
                Transform t = Instantiate(__instance.customizationItemButton, __instance.faceTatooGroup);
                t.name = "0";
                t.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t.SetAsFirstSibling();
                t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t.GetComponent<Button>().onClick.AddListener(delegate ()
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.faceTatoos = null;
                    __instance.curCustomization.body.materials[1].SetTexture("_MakeUpMask2_RGB", null);
                });

            }
        }


        [HarmonyPatch(typeof(Mainframe), "SaveCharacterCustomization")]
        static class SaveCharacterCustomization_Patch
        {
            static void Prefix(Mainframe __instance, CharacterCustomization customization)
            {
                if (!modEnabled.Value)
                    return;

                if (customization.wombTattoo)
                {
                    ES2.Save<string>(customization.wombTattoo.name, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattoo");
                }
                ES2.Save<float>(customization.wombTattooStrength, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooStrength");
                ES2.Save<float>(customization.wombTattooGlossiness, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooGlossiness");
                ES2.Save<string>(ColorUtility.ToHtmlStringRGBA(customization.wombTattooColor), __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooColor");
            }
        }

        [HarmonyPatch(typeof(Mainframe), "LoadCharacterCustomization")]
        static class LoadCharacterCustomization_Patch
        {
            static void Postfix(Mainframe __instance, CharacterCustomization gen)
            {
                if (!modEnabled.Value)
                    return;

                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattoo"))
                {
                    Transform t = RM.code.allWombTattoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattoo"));
                    if (t)
                        gen.wombTattoo = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=faceTatoos"))
                {
                    Transform t = RM.code.allFaceTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=faceTatoos"));
                    if (t)
                        gen.faceTatoos = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=bodyTatoos"))
                {
                    Transform t = RM.code.allBodyTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=bodyTatoos"));
                    if (t)
                        gen.bodyTatoos = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=legsTatoos"))
                {
                    Transform t = RM.code.allLegsTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=legsTatoos"));
                    if (t)
                        gen.legsTatoos = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=armsTatoos"))
                {
                    Transform t = RM.code.allArmsTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=armsTatoos"));
                    if (t)
                        gen.armsTatoos = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooStrength"))
                {
                    gen.wombTattooStrength = ES2.Load<float>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooStrength");
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooGlossiness"))
                {
                    gen.wombTattooGlossiness = ES2.Load<float>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooGlossiness");
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooColor"))
                {
                    string colorCode = "#" + ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooColor");

                    if (colorCode != "n" && ColorUtility.TryParseHtmlString(colorCode, out Color color))
                    {
                        color.a = gen.wombTattooStrength;
                        gen.wombTattooColor = color;
                        //Dbgl($"loaded womb tattoo color: {color} for {gen.characterName}");
                    }
                }
                gen.RefreshAppearence();
                gen.SyncBlendshape();
                Global.code.uiCustomization.SyncSliders();
            }
        }

        [HarmonyPatch(typeof(Mainframe), nameof(Mainframe.SaveCharacterPreset))]
        static class SaveCharacterPreset_Patch
        {
            static void Prefix(Mainframe __instance, CharacterCustomization customization, string presetname)
            {
                if (!modEnabled.Value)
                    return;

                if (customization.wombTattoo)
                {
                    ES2.Save<string>(customization.wombTattoo.name, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattoo");
                }
                ES2.Save<float>(customization.wombTattooStrength, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooStrength");
                ES2.Save<float>(customization.wombTattooGlossiness, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooGlossiness");
                ES2.Save<string>(ColorUtility.ToHtmlStringRGBA(customization.wombTattooColor), __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooColor");

            }
        }

        [HarmonyPatch(typeof(Mainframe), nameof(Mainframe.LoadCharacterPreset))]
        static class LoadCharacterPreset_Patch
        {
            static void Postfix(Mainframe __instance, CharacterCustomization gen, string presetname)
            {
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattoo"))
                {
                    Transform t = RM.code.allWombTattoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattoo"));
                    if (t)
                        gen.wombTattoo = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=faceTatoos"))
                {
                    Transform t = RM.code.allFaceTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=faceTatoos"));
                    if (t)
                        gen.faceTatoos = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=bodyTatoos"))
                {
                    Transform t = RM.code.allBodyTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=bodyTatoos"));
                    if (t)
                        gen.bodyTatoos = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=legsTatoos"))
                {
                    Transform t = RM.code.allLegsTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=legsTatoos"));
                    if (t)
                        gen.legsTatoos = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=armsTatoos"))
                {
                    Transform t = RM.code.allArmsTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=armsTatoos"));
                    if (t)
                        gen.armsTatoos = t;
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooStrength"))
                {
                    gen.wombTattooStrength = ES2.Load<float>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooStrength");
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooGlossiness"))
                {
                    gen.wombTattooGlossiness = ES2.Load<float>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooGlossiness");
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooColor"))
                {
                    string colorCode = "#" + ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattooColor");

                    if (colorCode != "n" && ColorUtility.TryParseHtmlString(colorCode, out Color color))
                    {
                        color.a = gen.wombTattooStrength;
                        gen.wombTattooColor = color;
                        //Dbgl($"loaded womb tattoo color: {color} for {gen.characterName}");
                    }
                }
                gen.RefreshAppearence();
                gen.SyncBlendshape();
                Global.code.uiCustomization.SyncSliders();
            }
        }

    }
}
