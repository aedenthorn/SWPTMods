using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace MoreHomeStorage
{
    [BepInPlugin("aedenthorn.MoreHomeStorage", "More Home Storage", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<int> extraPages;
        public static ConfigEntry<int> pageHeight;
        public static ConfigEntry<string> pageNames;

        private static GameObject scrollObject;

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
            nexusID = Config.Bind<int>("General", "NexusID", 58, "Nexus mod ID for updates");

            extraPages = Config.Bind<int>("Options", "ExtraPages", 10, "Extra home storage pages.");
            pageNames = Config.Bind<string>("Options", "PageNames", "", "Home storage page names, comma separated. Leave blank to use numbers.");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Global), "Awake")]
        static class Global_Awake_Patch
        {

            static void Postfix(Global __instance)
            {
                if (!modEnabled.Value)
                    return;

                Storage temp = __instance.homeStorage[__instance.homeStorage.Length - 1];

                List<Storage> newList = new List<Storage>(__instance.homeStorage);

                for(int i = 1; i <= extraPages.Value; i++)
                {
                    Transform t = Instantiate(temp.transform, temp.transform.parent);
                    t.name = (int.Parse(temp.name) + i) + "";
                    Storage newStorage = t.gameObject.GetComponent<Storage>();
                    newList.Add(newStorage);
                }
                __instance.homeStorage = newList.ToArray();

                if(__instance.uiBox.groupIndexButtons.parent.name != "Mask")
                {
                    scrollObject = new GameObject { name = "ScrollView" };
                    scrollObject.transform.SetParent(__instance.uiBox.groupIndexButtons.parent);

                    ScrollRect sr = scrollObject.AddComponent<ScrollRect>();
                    sr.movementType = ScrollRect.MovementType.Clamped;

                    GameObject mask = new GameObject { name = "Mask" };
                    mask.transform.SetParent(scrollObject.transform);

                    __instance.uiBox.groupIndexButtons.SetParent(mask.transform);
                    __instance.uiBox.groupIndexButtons.localPosition = Vector3.zero;

                    sr.viewport = mask.GetComponent<RectTransform>();
                    sr.content = __instance.uiBox.groupIndexButtons.GetComponent<RectTransform>();

                    scrollObject.GetComponent<RectTransform>().localScale = __instance.uiBox.groupIndexButtons.GetComponent<RectTransform>().localScale;
                    __instance.uiBox.groupIndexButtons.GetComponent<RectTransform>().localScale = Vector3.one;

                    RectTransform rtp = scrollObject.GetComponent<RectTransform>();
                    RectTransform rtc = __instance.uiBox.groupIndexButtons.GetComponent<RectTransform>();

                    rtp.sizeDelta = new Vector2(290, rtc.rect.height);
                    rtp.anchoredPosition = new Vector2(-16, -164);

                    rtp.anchorMin = new Vector2(0, 0.5f);
                    rtp.anchorMax = new Vector2(0, 0.5f);
                    rtp.pivot = new Vector2(0, 0.5f);

                    Texture2D tex = new Texture2D((int)Mathf.Ceil(scrollObject.GetComponent<RectTransform>().rect.width), (int)Mathf.Ceil(scrollObject.GetComponent<RectTransform>().rect.height));

                    Image image = mask.AddComponent<Image>();
                    image.sprite = Sprite.Create(tex, scrollObject.GetComponent<RectTransform>().rect,Vector2.zero);
                    Mask m = mask.AddComponent<Mask>();
                    m.showMaskGraphic = false;

                    RectTransform rtm = mask.GetComponent<RectTransform>();
                    rtm.sizeDelta = rtp.sizeDelta;
                    rtm.anchorMin = new Vector2(0, 0.5f);
                    rtm.anchorMax = new Vector2(0, 0.5f);
                    rtm.pivot = new Vector2(0, 0.5f);

                }
            }
        }
        [HarmonyPatch(typeof(UIBox), "Refresh")]
        static class UIBox_Refresh_Patch
        {
            static void Prefix(UIBox __instance, ref int __state)
            {
                __state = __instance.groupIndexButtons.childCount;
            }
            static void Postfix(UIBox __instance, int __state)
            {

                string[] names = pageNames.Value.Split(',');
                float width = 0;
                for (int i = 0; i < __instance.groupIndexButtons.childCount - __state; i++)
                {
                    Transform child = __instance.groupIndexButtons.GetChild(i + __state);
                    width += child.GetComponent<RectTransform>().rect.width;
                    if (pageNames.Value.Trim().Length > 0 && i >= names.Length)
                    {
                        child.GetComponentInChildren<Text>().text = (i + 1) + "";
                    }
                    else
                        child.GetComponentInChildren<Text>().text = names[i];
                }

                float listWidth = width + __instance.groupIndexButtons.GetComponent<HorizontalLayoutGroup>().spacing * Global.code.homeStorage.Length - 1;

                RectTransform rtc = __instance.groupIndexButtons.GetComponent<RectTransform>();
                rtc.sizeDelta = new Vector2(listWidth, rtc.sizeDelta.y);
                scrollObject.GetComponent<ScrollRect>().horizontalNormalizedPosition = 0;

            }
        }
    }
}
