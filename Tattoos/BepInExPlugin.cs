using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Tattoos
{
    [BepInPlugin("aedenthorn.Tattoos", "Tattoos", "0.8.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;


        public static ConfigEntry<bool> sameVariablesAllTattoos;
        public static ConfigEntry<bool> storeByName;
        public static ConfigEntry<string> reloadKey;

        public static string assetPath;
        public static readonly string buttonPrefix = "Tattoos";

        public static ConfigEntry<int> nexusID;
        public static List<Texture2D> tattooTextureList = new List<Texture2D>();

        private static GameObject tattooGO;
        private static Slider glossSlider;
        private static Slider strengthSlider;

        private static Texture2D defaultWombIcon;
        private static readonly Dictionary<Transform, string> pathRegistere = new Dictionary<Transform, string>();
        private static readonly Dictionary<string, Transform> tattooRegister = new Dictionary<string, Transform>();

        //Why not using context.Logger.LogInfo / .LogWarning / .LogError ?
        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");

            sameVariablesAllTattoos = Config.Bind("Options", "SameVariablesAllTattoos", true, "Use color, gloss, and strength from womb tattoo for all tattoos. This is necessary for now.");
            reloadKey = Config.Bind("Options", "ReloadKey", "page down", "Key to reload tattoos from disk.");
            storeByName = Config.Bind("Options", "Store by Name", false, "Whether to store the tattoos by name or (default) by number");

            nexusID = Config.Bind("General", "NexusID", 62, "Nexus mod ID for updates");

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
            if (AedenthornUtils.CheckKeyDown(reloadKey.Value))
            {
                RegisterAllTattoos();
                if (Global.code.uiMakeup.gameObject.activeSelf)
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
        private static void RegisterAllTattoos()
        {
            RM.code.allPubicHairs.items = Resources.LoadAll("Customization/PubicHairs", typeof(Transform)).Cast<Transform>().ToList();
            RM.code.allWombTattoos.items = Resources.LoadAll("Customization/WombTattoos", typeof(Transform)).Cast<Transform>().ToList();
            RM.code.allBodyTatoos.items = Resources.LoadAll("Customization/BodyTatoos", typeof(Transform)).Cast<Transform>().ToList();
            RM.code.allLegsTatoos.items = Resources.LoadAll("Customization/LegsTatoos", typeof(Transform)).Cast<Transform>().ToList();
            RM.code.allArmsTatoos.items = Resources.LoadAll("Customization/ArmsTatoos", typeof(Transform)).Cast<Transform>().ToList();
            RM.code.allFaceTatoos.items = Resources.LoadAll("Customization/FaceTatoos", typeof(Transform)).Cast<Transform>().ToList();
            RegisterTattoos("Pubic", ref RM.code.allPubicHairs);
            RegisterTattoos("Womb", ref RM.code.allWombTattoos);
            RegisterTattoos("Arms", ref RM.code.allArmsTatoos);
            RegisterTattoos("Legs", ref RM.code.allLegsTatoos);
            RegisterTattoos("Face", ref RM.code.allFaceTatoos);
            RegisterTattoos("Body", ref RM.code.allBodyTatoos);
        }

        private static void RegisterTattoos(string folder, ref CommonArray resources)
        {
            Transform templateT = RM.code.allWombTattoos.items[0];
            int count = 0;
            try
            {
                foreach (string texPath in Directory.GetFiles(Path.Combine(assetPath, folder), "*.png"))
                {
                    //string texPath = iconPath.Replace("_icon.png", ".png");
                    if (texPath.EndsWith("_icon.png"))
                        continue;

                    if (tattooRegister.TryGetValue(texPath, out Transform t))
                    {
                        //Lazy loading!!!
                        //textureDict[texPath].LoadImage(File.ReadAllBytes(texPath));
                        //textureDict[iconPath].LoadImage(File.ReadAllBytes(iconPath));
                        resources.AddItem(t);
                        count++;
                        continue;
                    }

                    //Lazy loading!!!
                    //Texture2D tex = new Texture2D(1, 1);
                    //Texture2D icon = new Texture2D(1, 1);
                    //tex.LoadImage(File.ReadAllBytes(texPath));
                    //icon.LoadImage(File.ReadAllBytes(iconPath));
                    t = Instantiate(templateT, tattooGO.transform);
                    if (storeByName.Value)
                    {
                        t.name = texPath.Replace('/', '_').Replace('\\', '_').Replace(':', '_').Replace('.', '_');
                    }
                    else
                    {
                        t.name = resources.items.Count + 1 + "";
                    }
                    t.GetComponent<CustomizationItem>().eyes = null; //tex;
                    t.GetComponent<CustomizationItem>().icon = null; //icon;
                    resources.AddItem(t);
                    count++;

                    pathRegistere.Add(t, texPath);
                    tattooRegister.Add(texPath, t);
                }
                Dbgl($"Got {count} {folder} tattoos");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error while trying to get {folder} tattoos: \n\n {ex.StackTrace}");
            }
            RM.code.allWombTattoos.items[0].GetComponent<CustomizationItem>().icon = defaultWombIcon;
        }

        private static Texture2D LoadIcon(Transform t)
        {
            if (t && t.TryGetComponent(out CustomizationItem ci))
            {
                if (ci.icon)
                {
                    return ci.icon;
                }
                else
                {
                    ci.icon = new Texture2D(1, 1);
                    if (pathRegistere.TryGetValue(t, out string path))
                    {
                        var icon_path = path.Replace(".png", "_icon.png");
                        if (File.Exists(icon_path))
						{
                            ci.icon.LoadImage(File.ReadAllBytes(icon_path));
                        }
                        else
						{
                            ci.icon.LoadImage(File.ReadAllBytes(path));
                        }
                        return ci.icon;
                    }
                    else
                    {
                        context.Logger.LogError("Error: Tattoo was not registered!");
                    }
                }
            }
            return null;
        }

        private static Transform LoadTattoo(Transform t)
        {
            if (t && t.TryGetComponent(out CustomizationItem ci) && !ci.eyes)
            {
                ci.eyes = new Texture2D(1, 1);
                if (pathRegistere.TryGetValue(t, out string path))
                {
                    ci.eyes.LoadImage(File.ReadAllBytes(path));
                }
                else
                {
                    context.Logger.LogError("Error: Tattoo was not registered!");
                }
            }
            return t;
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

                if (File.Exists(Path.Combine(assetPath, "wombIcon.png")))
                    defaultWombIcon.LoadImage(File.ReadAllBytes(Path.Combine(assetPath, "wombIcon.png")));

                RegisterAllTattoos();
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


        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.Open))]
        static class UIMakeup_Open_Patch
        {
            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value)
                    return;

                Sprite legsSprite = Global.code.uiInventory.transform.Find("Left (1)/Options Group/Button Skills/Button Lips (1)")?.GetComponent<Image>()?.sprite;
                Transform legsButton = __instance.panelPubicHair.transform.parent.Find("Category (1)/Button Legs");

                if (!legsButton || !legsSprite)
                {
                    context.Logger.LogError($"Error while trying to get button {legsButton?.name} and sprite {legsSprite?.name}");
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
            static IEnumerator CoroutineLeg(UIMakeup __instance)
            {
                for (var i = 4; i < RM.code.allPubicHairs.items.Count; ++i)
                {
                    yield return new WaitForSeconds(0.1f);
                    var t = RM.code.allPubicHairs.items[i];
                    __instance.pubicHairGroup.GetChild(i).GetComponent<RawImage>().texture = LoadIcon(t);
                }
                yield return null;
            }

            static IEnumerator CoroutineWomb(UIMakeup __instance)
            {
                //__instance.wombTattooGroup.GetChild(1).GetComponent<RawImage>().texture = defaultWombIcon;
                for (var i = 1; i < RM.code.allWombTattoos.items.Count; ++i)
                {
                    yield return new WaitForSeconds(0.1f);
                    var t = RM.code.allWombTattoos.items[i];
                    __instance.wombTattooGroup.GetChild(i + 1).GetComponent<RawImage>().texture = LoadIcon(t);
                }
                yield return null;
            }

            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value)
                    return;

                for (int i = 0; i < __instance.panelPubicHair.transform.childCount; i++)
                {
                    Transform c = __instance.panelPubicHair.transform.GetChild(i);
                    if (!c.gameObject.activeSelf)
                    {
                        c.gameObject.SetActive(true);
                        if (c.name.Contains("Color Picker"))
                        {
                            Dbgl($"Initializing color picker");
                            c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                            c.GetComponent<Button>().onClick.AddListener(() =>
                            {
                                Global.code.uiColorPick.Open(__instance.curCustomization.wombTattooColor, c.name);
                            }
                            );
                        }
                        else if (c.GetComponentInChildren<Slider>())
                        {
                            c.GetComponent<Slider>().onValueChanged = new Slider.SliderEvent();
                            if (c.name.Contains("trength"))
                            {
                                Dbgl($"Initializing strength slider");
                                c.GetComponentInChildren<LocalizationText>().KEY = "Womb Tattoo Strength";
                                strengthSlider = c.GetComponent<Slider>();
                                strengthSlider.onValueChanged.AddListener((float arg0) =>
                                {
                                    __instance.curCustomization.wombTattooStrength = arg0; __instance.curCustomization.RefreshAppearence();
                                });
                            }
                            else if (c.name.Contains("Glossiness"))
                            {
                                Dbgl($"Initializing gloss slider");
                                c.GetComponentInChildren<LocalizationText>().KEY = "Womb Tattoo Glossiness";
                                glossSlider = c.GetComponent<Slider>();
                                glossSlider.onValueChanged.AddListener((float arg0) =>
                                {
                                    __instance.curCustomization.wombTattooGlossiness = arg0;
                                    __instance.curCustomization.RefreshAppearence();
                                });
                            }
                        }
                    }
                    else if (c.name == "Scroll View Pubic")
                    {
                        c.GetComponent<RectTransform>().sizeDelta = new Vector2(212.3437f, 58.978f);
                    }
                }

                for (int j = 4; j < __instance.pubicHairGroup.childCount; j++)
                {
                    Transform c = __instance.pubicHairGroup.GetChild(j);
                    if (c)
                    {
                        c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        c.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            __instance.curCustomization.pubicHair = LoadTattoo(RM.code.allPubicHairs.GetItemWithName(c.name));
                            __instance.curCustomization.RefreshAppearence();
                        });
                    }
                }

                strengthSlider.value = __instance.curCustomization.wombTattooStrength;
                glossSlider.value = __instance.curCustomization.wombTattooGlossiness;
                for (int j = 0; j < __instance.wombTattooGroup.childCount; j++)
                {
                    Transform c = __instance.wombTattooGroup.GetChild(j);
                    if (c)
                    {
                        c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        c.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            __instance.curCustomization.wombTattoo = LoadTattoo(RM.code.allWombTattoos.GetItemWithName(c.name));
                            __instance.curCustomization.RefreshAppearence();
                        });
                    }
                }

                //cancel button
                Transform t = Instantiate(__instance.customizationItemButton, __instance.wombTattooGroup);
                t.name = "0";
                t.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t.SetAsFirstSibling();
                t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.wombTattoo = null;
                    __instance.curCustomization.body.materials[0].SetTexture("_MakeUpMask2_RGB", null);
                });

                __instance.StartCoroutine(CoroutineLeg(__instance));
                __instance.StartCoroutine(CoroutineWomb(__instance));
            }
        }

        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.ButtonTatooLegs))]
        static class ButtonTatooLegs_Patch
        {
            static IEnumerator Coroutine(UIMakeup __instance)
            {
                var i = 0;
                foreach (var t in RM.code.allLegsTatoos.items)
                {
                    yield return new WaitForSeconds(0.1f);
                    __instance.legsTatooGroup.GetChild(++i).GetComponent<RawImage>().texture = LoadIcon(t);
                }
                yield return null;
            }

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
                        c.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            __instance.curCustomization.legsTatoos = LoadTattoo(RM.code.allLegsTatoos.GetItemWithName(c.name));
                            __instance.curCustomization.RefreshAppearence();
                        });
                    }
                }

                //cancel button
                Transform t = Instantiate(__instance.customizationItemButton, __instance.legsTatooGroup);
                t.name = "0";
                t.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t.SetAsFirstSibling();
                t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.legsTatoos = null;
                    __instance.curCustomization.body.materials[5].SetTexture("_MakeUpMask2_RGB", null);
                });

                __instance.StartCoroutine(Coroutine(__instance));
            }
        }

        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.ButtonTatooBody))]
        static class ButtonTatooBody_Patch
        {
            static IEnumerator CoroutineBody(UIMakeup __instance)
            {
                var i = 0;
                foreach (var t in RM.code.allBodyTatoos.items)
                {
                    yield return new WaitForSeconds(0.1f);
                    __instance.bodyTatooGroup.GetChild(++i).GetComponent<RawImage>().texture = LoadIcon(t);
                }
                yield return null;
            }

            static IEnumerator CoroutineArm(UIMakeup __instance)
            {
                var i = 0;
                foreach (var t in RM.code.allArmsTatoos.items)
                {
                    yield return new WaitForSeconds(0.1f);
                    __instance.armsTatooGroup.GetChild(++i).GetComponent<RawImage>().texture = LoadIcon(t);
                }
                yield return null;
            }

            static void Postfix(UIMakeup __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (__instance.bodyTatooGroup.parent == __instance.armsTatooGroup.parent)
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
                        c.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            __instance.curCustomization.armsTatoos = LoadTattoo(RM.code.allArmsTatoos.GetItemWithName(c.name));
                            __instance.curCustomization.RefreshAppearence();
                        });
                    }
                }
                for (int j = 0; j < __instance.bodyTatooGroup.childCount; j++)
                {
                    Transform c = __instance.bodyTatooGroup.GetChild(j);
                    if (c)
                    {
                        Dbgl(c.name);
                        c.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                        c.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            __instance.curCustomization.bodyTatoos = LoadTattoo(RM.code.allBodyTatoos.GetItemWithName(c.name));
                            __instance.curCustomization.RefreshAppearence();
                        });
                    }
                }

                Transform t = Instantiate(__instance.customizationItemButton, __instance.armsTatooGroup);
                t.name = "0";
                t.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t.SetAsFirstSibling();
                t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t.GetComponent<Button>().onClick.AddListener(() =>
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
                t2.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.bodyTatoos = null;
                    if (__instance.curCustomization.wombTattoo == null)
                        __instance.curCustomization.body.materials[0].SetTexture("_MakeUpMask2_RGB", null);
                    else
                        __instance.curCustomization.body.materials[0].SetTexture("_MakeUpMask2_RGB", __instance.curCustomization.wombTattoo.GetComponent<CustomizationItem>().eyes);
                });

                __instance.StartCoroutine(CoroutineBody(__instance));
                __instance.StartCoroutine(CoroutineArm(__instance));
            }
        }

        [HarmonyPatch(typeof(UIMakeup), nameof(UIMakeup.ButtonFace))]
        static class ButtonFace_Patch
        {
            static IEnumerator Coroutine(UIMakeup __instance)
            {
                var i = 0;
                foreach (var t in RM.code.allFaceTatoos.items)
                {
                    yield return new WaitForSeconds(0.1f);
                    __instance.faceTatooGroup.GetChild(++i).GetComponent<RawImage>().texture = LoadIcon(t);
                }
                yield return null;
            }

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
                        c.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            __instance.curCustomization.faceTatoos = LoadTattoo(RM.code.allFaceTatoos.GetItemWithName(c.name));
                            __instance.curCustomization.RefreshAppearence();
                        });
                    }
                }

                Transform t = Instantiate(__instance.customizationItemButton, __instance.faceTatooGroup);
                t.name = "0";
                t.GetComponent<RawImage>().texture = RM.code.allWings.items[0].GetComponent<CustomizationItem>().icon;
                t.SetAsFirstSibling();
                t.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                t.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Dbgl("Clicked");
                    __instance.curCustomization.faceTatoos = null;
                    __instance.curCustomization.body.materials[1].SetTexture("_MakeUpMask2_RGB", __instance.curCustomization.blushColor.GetComponent<CustomizationItem>().eyes);
                });

                __instance.StartCoroutine(Coroutine(__instance));
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
                    ES2.Save(customization.wombTattoo.name, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattoo");
                }
                ES2.Save(customization.wombTattooStrength, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooStrength");
                ES2.Save(customization.wombTattooGlossiness, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooGlossiness");
                ES2.Save(ColorUtility.ToHtmlStringRGBA(customization.wombTattooColor), __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooColor");
                ES2.Save(ColorUtility.ToHtmlStringRGBA(customization.pubicHairColor), __instance.GetFolderName() + customization.name + ".txt?tag=pubicHairColor");
            }
        }

        [HarmonyPatch(typeof(Mainframe), "LoadCharacterCustomization")]
        static class LoadCharacterCustomization_Patch
        {
            static void Postfix(Mainframe __instance, CharacterCustomization gen)
            {
                if (!modEnabled.Value)
                    return;

                LoadTattoo(gen.pubicHair);
                gen.blushColor.GetComponent<CustomizationItem>().eyes = (Texture2D)gen.body.materials[1].GetTexture("_MakeUpMask2_RGB");
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattoo"))
                {
                    Transform t = RM.code.allWombTattoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattoo"));
                    if (t) gen.wombTattoo = LoadTattoo(t);
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=faceTatoos"))
                {
                    Transform t = RM.code.allFaceTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=faceTatoos"));
                    if (t) gen.faceTatoos = LoadTattoo(t);
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=bodyTatoos"))
                {
                    Transform t = RM.code.allBodyTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=bodyTatoos"));
                    if (t) gen.bodyTatoos = LoadTattoo(t);
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=legsTatoos"))
                {
                    Transform t = RM.code.allLegsTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=legsTatoos"));
                    if (t) gen.legsTatoos = LoadTattoo(t);
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=armsTatoos"))
                {
                    Transform t = RM.code.allArmsTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=armsTatoos"));
                    if (t) gen.armsTatoos = LoadTattoo(t);
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
                    }
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=pubicHairColor"))
                {
                    string colorCode = "#" + ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=pubicHairColor");

                    if (colorCode != "n" && ColorUtility.TryParseHtmlString(colorCode, out Color color))
                    {
                        gen.pubicHairColor = color;
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
                    ES2.Save(customization.wombTattoo.name, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattoo");
                }
                ES2.Save(customization.wombTattooStrength, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooStrength");
                ES2.Save(customization.wombTattooGlossiness, __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooGlossiness");
                ES2.Save(ColorUtility.ToHtmlStringRGBA(customization.wombTattooColor), __instance.GetFolderName() + customization.name + ".txt?tag=wombTattooColor");
            }
        }

        [HarmonyPatch(typeof(Mainframe), nameof(Mainframe.LoadCharacterPreset))]
        static class LoadCharacterPreset_Patch
        {
            static void Postfix(Mainframe __instance, CharacterCustomization gen, string presetname)
            {
                LoadTattoo(gen.pubicHair);
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattoo"))
                {
                    Transform t = RM.code.allWombTattoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=wombTattoo"));
                    if (t) gen.wombTattoo = LoadTattoo(t);
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=faceTatoos"))
                {
                    Transform t = RM.code.allFaceTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=faceTatoos"));
                    if (t) gen.faceTatoos = LoadTattoo(t);
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=bodyTatoos"))
                {
                    Transform t = RM.code.allBodyTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=bodyTatoos"));
                    if (t) gen.bodyTatoos = LoadTattoo(t);
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=legsTatoos"))
                {
                    Transform t = RM.code.allLegsTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=legsTatoos"));
                    if (t) gen.legsTatoos = LoadTattoo(t);
                }
                if (ES2.Exists(__instance.GetFolderName() + gen.name + ".txt?tag=armsTatoos"))
                {
                    Transform t = RM.code.allArmsTatoos.GetItemWithName(ES2.Load<string>(__instance.GetFolderName() + gen.name + ".txt?tag=armsTatoos"));
                    if (t) gen.armsTatoos = LoadTattoo(t);
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
