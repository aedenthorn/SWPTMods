using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CustomPortrait
{
    [BepInPlugin("aedenthorn.CustomPortrait", "Custom Portrait", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<string> hotkey;
        public static ConfigEntry<string> portraitModkey;
        public static ConfigEntry<string> screenshotModkey;
        public static ConfigEntry<string> screenshotSaved;
        public static ConfigEntry<string> portraitSaved;
        public static ConfigEntry<Color> portraitMaskColor;

        private static string assetPath;
        private static Image imageObject;
        private static Texture2D borderTexture;
        private static Vector3 mouseDownPos;
        private static int captureStage;
        private static float ratio;
        private static GameObject rectImage;
        private static Dictionary<string, Texture2D> cachedPortraits = new Dictionary<string, Texture2D>();

        //public static ConfigEntry<int> nexusID;

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

            screenshotSaved = Config.Bind<string>("Options", "ScreenshotSaved", "Screenshot saved to {0}", "Text to show when a screenshot is saved.");
            portraitSaved = Config.Bind<string>("Options", "PortraitSaved", "Portrait saved for {0}", "Text to show when a portrait is saved.");
            portraitMaskColor = Config.Bind<Color>("Options", "PortraitMaskColor", new Color(0.5f,0.5f,1,0.05f), "Color of guide showing portrait area.");
            hotkey = Config.Bind<string>("Options", "Hotkey", "insert", "Hotkey to make a portrait or take screenshot. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            portraitModkey = Config.Bind<string>("Options", "PortraitModkey", "left shift", "Modifier key to hold in order to create a portrait when pressing the hotkey. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            screenshotModkey = Config.Bind<string>("Options", "ScreenshotModkey", "", "Modifier key to hold in order to take a screenshot when pressing the hotkey. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
                Directory.CreateDirectory(Path.Combine(assetPath,"Screenshots"));
                Directory.CreateDirectory(Path.Combine(assetPath,"Portraits"));
            }

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            borderTexture = new Texture2D(1, 1);

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        
        private void Update()
        {
            if (!modEnabled.Value || !Global.code || !Player.code)
                return;

            if (AedenthornUtils.CheckKeyDown(hotkey.Value))
            {
                if ((AedenthornUtils.CheckKeyHeld(portraitModkey.Value, true) || (AedenthornUtils.CheckKeyHeld(portraitModkey.Value, false) && !AedenthornUtils.CheckKeyHeld(screenshotModkey.Value, true))) && (Global.code.uiInventory.gameObject.activeSelf == true || Global.code.uiPose.gameObject.activeSelf == true || Global.code.uiFreePose.gameObject.activeSelf == true))
                {
                    Dbgl("Pressed portrait hotkey");

                    if (captureStage > 0)
                    {
                        Dbgl("Cancelling portrait");
                        ExitCapture();
                    }
                    else
                        context.StartCoroutine(SnapPhoto(true));
                }
                else if (AedenthornUtils.CheckKeyHeld(screenshotModkey.Value, false))
                {
                    Dbgl("Pressed screenshot hotkey");
                    context.StartCoroutine(SnapPhoto(false));

                }

            }
            if (captureStage == 2)
            {
                Rect rect = GetPortraitRect(mouseDownPos, Input.mousePosition);
                rectImage.SetActive(true);
                rectImage.transform.position = rect.position;
                rectImage.GetComponent<Image>().rectTransform.sizeDelta = rect.size / rectImage.GetComponent<RectTransform>().lossyScale;
            }
        }

        [HarmonyPatch(typeof(Player), "Start")]
        static class Player_Start_Patch
        {
            static void Postfix(Player __instance)
            {
                if (!modEnabled.Value)
                    return;

                ratio = __instance.customization.icon.width / (float)__instance.customization.icon.height;
                Dbgl($"Portrait ratio: {ratio}");
            }
        }

        [HarmonyPatch(typeof(Global), "Start")]
        static class Global_Start_Patch
        {
            static void Prefix(Global __instance)
            {
                if (!modEnabled.Value || rectImage != null)
                    return;

                rectImage = Instantiate(new GameObject(), Global.code.uiCanvas.transform);
                rectImage.name = "Portrait Outline";
                RectTransform rt = rectImage.AddComponent<RectTransform>();
                rt.pivot = new Vector2(0, 0);

                Texture2D tex = new Texture2D(1,1);
                tex.SetPixel(0, 0, portraitMaskColor.Value);
                tex.Apply();

                Image image = rectImage.AddComponent<Image>();
                image.sprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), Vector2.zero);
                rectImage.SetActive(false);
            }
        }
        
        [HarmonyPatch(typeof(CompanionIcon), nameof(CompanionIcon.Initiate))]
        static class CompanionIcon_Initiate_Patch
        {
            static void Prefix(CharacterCustomization _customization)
            {
                if (!modEnabled.Value)
                    return;
                if (cachedPortraits.ContainsKey(_customization.characterName))
                {
                    _customization.icon = cachedPortraits[_customization.characterName];
                }
                else if(File.Exists(Path.Combine(assetPath, "Portraits", _customization.characterName + ".png")))
                {
                    byte[] bytes = File.ReadAllBytes(Path.Combine(assetPath, "Portraits", _customization.characterName + ".png"));
                    _customization.icon.LoadImage(bytes);

                }
            }
        }

        private static IEnumerator SnapPhoto(bool portrait)
        {
            yield return new WaitForEndOfFrame();

            RenderTexture rt = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default);
            Texture2D background = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);

            foreach (Camera cam in Camera.allCameras)
            {
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
            }

            RenderTexture.active = rt;
            background.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            background.Apply();
            Camera.main.targetTexture = null;
            RenderTexture.active = null;

            yield return 0;
            if (!Global.code.uiCanvas.GetComponent<Image>())
                imageObject = Global.code.uiCanvas.AddComponent<Image>();
            else imageObject = Global.code.uiCanvas.GetComponent<Image>();

            Global.code.uiCanvas.SetActive(true);

            imageObject.sprite = Sprite.Create(background, new Rect(0.0f, 0.0f, background.width, background.height), Vector2.zero, 100.0f);
            //GameCamera.instance.gameObject.GetComponent<Camera>().enabled = false;
            Dbgl($"Created scene texture.");
            
            Time.timeScale = 0;

            if (!portrait)
            {

                string fileName = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";

                Dbgl($"Saving to {fileName}");

                var bytes = background.EncodeToPNG();

                if (!Directory.Exists(Path.Combine(assetPath, "Screenshots")))
                {
                    Directory.CreateDirectory(Path.Combine(assetPath, "Screenshots"));
                }

                File.WriteAllBytes(Path.Combine(assetPath, "ScreenShots", fileName), bytes);

                Global.code.uiCombat.ShowHeader(string.Format(screenshotSaved.Value, fileName));

                ExitCapture();
                yield break;
            }

            captureStage = 1;
            while (!Input.GetKeyDown(KeyCode.Mouse0))
                yield return 0;
            Dbgl("Mouse down");
            captureStage = 2;
            mouseDownPos = Input.mousePosition;

            while (Input.GetKey(KeyCode.Mouse0))
                yield return 0;
            Dbgl("Mouse up");
            captureStage = 3;

            var mouseUpPos = Input.mousePosition;

            if (mouseUpPos.x == mouseDownPos.x || mouseUpPos.y == mouseDownPos.y)
            {
                Dbgl($"invalid size {mouseDownPos}, {mouseUpPos}");
                ExitCapture();

                yield break;
            }


            Rect portraitRect = GetPortraitRect(mouseDownPos, mouseUpPos);

            Dbgl($"creating portrait at {portraitRect.position}, size {portraitRect.size}");

            Color[] portraitData = background.GetPixels((int)portraitRect.x, (int)portraitRect.y, (int)portraitRect.size.x, (int)portraitRect.size.y);

            Texture2D outTexture = new Texture2D((int)portraitRect.width, (int)portraitRect.height);
            outTexture.SetPixels(portraitData);
            outTexture.Apply();

            string name = null;

            if (Global.code.uiInventory.gameObject.activeSelf)
            {
                name = Global.code.uiInventory.curCustomization.characterName;
            }
            else if (Global.code.uiPose.gameObject.activeSelf)
            {
                name = Global.code.uiPose.curCustomization.characterName;
            }
            else if (Global.code.uiFreePose.gameObject.activeSelf)
            {
                name = Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().characterName;
            }

            var bytes2 = outTexture.EncodeToPNG();

            if (!Directory.Exists(Path.Combine(assetPath, "Portraits")))
            {
                Directory.CreateDirectory(Path.Combine(assetPath, "Portraits"));
            }

            File.WriteAllBytes(Path.Combine(assetPath, "Portraits", name + ".png"), bytes2);

            cachedPortraits[name] = outTexture;

            if(Global.code.uiCombat.succubusIconGroup.gameObject.activeSelf)
                Global.code.uiCombat.ShowSuccubusIcons();
            if(Global.code.uiFreePose.gameObject.activeSelf)
                Global.code.uiCombat.ShowSuccubusIcons();

            Global.code.uiCombat.ShowHeader(string.Format(portraitSaved.Value, name));

            ExitCapture();
        }

        private static void ExitCapture()
        {
            DestroyImmediate(imageObject);
            imageObject = null;
            captureStage = 0;
            Time.timeScale = 1;
            rectImage.SetActive(false);
        }

        private static Rect GetPortraitRect(Vector2 v1, Vector2 v2)
        {
            Vector2 startV = new Vector2(v1.x > v2.x ? v2.x : v1.x, v1.y > v2.y ? v2.y : v1.y);
            Vector2 endV = new Vector2(v1.x < v2.x ? v2.x : v1.x, v1.y < v2.y ? v2.y : v1.y);

            Vector2 size = endV - startV;
            if(ratio > size.x / size.y) // thinner
            {
                size = new Vector2(size.y * ratio, size.y);
            }
            else if(ratio < size.x / size.y) // wider
            {
                float newY = size.x / ratio;
                startV.y = Mathf.Max(0, startV.y - (newY - size.y)); 
                size = new Vector2(size.x, newY);
            }

            if(size.x + startV.x > Screen.width)
            {
                startV.x = Screen.width - size.x;
            }
            if(size.y + startV.y > Screen.height)
            {
                startV.y = Screen.height - size.y;
            }

            return new Rect(startV, size);
        }
    }
}
