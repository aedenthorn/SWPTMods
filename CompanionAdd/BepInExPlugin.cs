using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CompanionAdd
{
    [BepInPlugin("aedenthorn.CompanionAdd", "Companion Add", "0.3.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> allowDuplicates;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> recallHotKey;
        public static ConfigEntry<float> rewardChance;
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
            
            allowDuplicates = Config.Bind<bool>("General", "AllowDuplicates", false, "Allow adding a companion you already have.");

            hotKey = Config.Bind<string>("Options", "HotKey", "f2", "Hotkey to add a random companion. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            recallHotKey = Config.Bind<string>("Options", "RecallHotKey", "f3", "Hotkey to call companions to player's location. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            nexusID = Config.Bind<int>("General", "NexusID", 8, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                if (AedenthornUtils.CheckKeyDown(hotKey.Value))
                {
                    Dbgl("Trying to add companion");

                    List<Transform> list = new List<Transform>(RM.code.allCompanions.items);

                    ShuffleList(list);

                    Transform c = null;
                    foreach (Transform transform in list)
                    {
                        if (allowDuplicates.Value || !Global.code.companions.items.Exists(t => t.name == transform.name))
                        {
                            c = transform;
                            break;
                        }
                        //Dbgl($"name: {transform.name}");
                    }

                    if (c != null)
                    {
                        Transform companion = Utility.Instantiate(c);
                        if(Global.code.companions.items.Exists(t => t.name == c.name))
                        {
                            GiveNewName(companion);
                        }

                        Dbgl($"adding {companion.name} to army");
                        Global.code.AddCompanionToPlayerArmy(companion);
                    }
                    Scene.code.SpawnCompanions();
                }
                else if (AedenthornUtils.CheckKeyDown(recallHotKey.Value))
                {
                    Dbgl($"Recalling companions");

                    Scene.code.SpawnCompanions();
                }
            }
        }
        public static void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        [HarmonyPatch(typeof(Mainframe), "LoadCompanions")]
        static class LoadCompanions_Patch
        {
            static void Prefix(Mainframe __instance)
            {
                if (!modEnabled.Value || RM.code?.allCompanions == null || RM.code.allCompanions.items.Count == 0)
                    return;
                List<string> list = ES2.LoadList<string>(__instance.GetFolderName() + "Global.txt?tag=companionlist");
                if (list.Count > 0)
                {
                    foreach (string text in list)
                    {
                        if (text != "")
                        {
                            if (!RM.code.allCompanions.GetItemWithName(text))
                            {
                                Transform companion = Utility.Instantiate(RM.code.allCompanions.items[0]);
                                companion.name = text;
                                RM.code.allCompanions.AddItem(companion);
                            }

                        }
                    }
                }
            }
        }
        [HarmonyPatch(typeof(UICombat), nameof(UICombat.HidePanels))]
        static class HidePanels_Patch
        {
            static void Postfix(UICombat __instance)
            {
                if (!modEnabled.Value || !Global.code || !scrollObject)
                    return;
                scrollObject.SetActive(false);
            }
        }
        [HarmonyPatch(typeof(Global), nameof(Global.AddCompanionToPlayerArmy))]
        static class AddCompanionToPlayerArmy_Patch
        {
            static void Prefix(Global __instance, Transform companion)
            {
                if (!modEnabled.Value)
                    return;

                if (Global.code.companions.items.Exists(t => t.name == companion.name))
                {
                    GiveNewName(companion);
                }

            }
        }

        private static void GiveNewName(Transform companion)
        {
            string name = null;

            List<string> names = new List<string>(Global.code.uiPose.namelist);
            AedenthornUtils.ShuffleList(names);
            foreach (string n in names)
            {
                if (!Global.code.companions.items.Exists(t => t.name == n))
                {
                    name = n;
                    break;
                }
            }
            if (name == null)
            {
                name = companion.name + "_";
                while (Global.code.companions.items.Exists(t => t.name == name))
                    name += "_";
            }

            companion.name = name;
            companion.GetComponent<CharacterCustomization>().characterName = companion.name;
        }

        private static Texture2D tex;
        private static GameObject scrollObject;

        //[HarmonyPatch(typeof(UICombat), "ShowSuccubusIcons")]
        static class ShowSuccubusIcons_Patch
        {
            static void Postfix(UICombat __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (!scrollObject)
                {
                    Dbgl("Adding scroll view");
                    /*
                    tex = new Texture2D(2, 2);
                    Color[] color = new Color[tex.width * tex.height];
                    for (int i = 0; i < color.Length; i++)
                    {
                        color[i] = Color.white;

                    }
                    tex.SetPixels(color);
                    tex.Apply();
                    */

                    scrollObject = new GameObject() { name = "ScrollView" };
                    scrollObject.transform.SetParent(__instance.succubusIconGroup.parent);

                    ScrollRect sr = scrollObject.AddComponent<ScrollRect>();
                    sr.movementType = ScrollRect.MovementType.Clamped;

                    //Image image = scrollObject.AddComponent<Image>();
                    //image.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), Vector2.zero);

                    GameObject mask = new GameObject() { name = "Mask" };
                    mask.transform.SetParent(scrollObject.transform);


                    //Image image = mask.AddComponent<Image>();
                    //image.sprite = Sprite.Create(tex, sv.GetComponent<RectTransform>().rect,Vector2.zero);
                    //Mask m = mask.AddComponent<Mask>();
                    //m.showMaskGraphic = false;

                    __instance.succubusIconGroup.SetParent(mask.transform);


                    sr.viewport = mask.GetComponent<RectTransform>();
                    sr.content = __instance.succubusIconGroup.GetComponent<RectTransform>();

                    //scrollObject.GetComponent<RectTransform>().localScale = __instance.succubusIconGroup.GetComponent<RectTransform>().localScale;
                    //__instance.succubusIconGroup.GetComponent<RectTransform>().localScale = Vector3.one;

                    Dbgl("Added scroll view");

                }

                scrollObject.SetActive(true);

                //Dbgl("Adjusting layout");

                float listWidth = __instance.succubusIconGroup.GetChild(0).GetComponent<RectTransform>().rect.width * (Global.code.companions.items.Count + 1) + __instance.succubusIconGroup.GetComponent<HorizontalLayoutGroup>().spacing * Global.code.companions.items.Count;


                RectTransform rtp = scrollObject.GetComponent<RectTransform>();
                RectTransform rtc = __instance.succubusIconGroup.GetComponent<RectTransform>();

                float maxWidth = rtp.transform.parent.GetComponent<RectTransform>().rect.width / rtp.localScale.x;

                rtc.sizeDelta = new Vector2(listWidth, rtc.sizeDelta.y);
                rtp.sizeDelta = new Vector2(Mathf.Min(maxWidth, rtc.rect.width), rtc.rect.height);

                rtp.anchoredPosition = rtc.anchoredPosition;
                rtc.anchoredPosition = Vector2.zero;

                //Dbgl($"list width {listWidth}, max width {maxWidth}, parent size {scrollObject.transform.parent.GetComponent<RectTransform>().rect.size} rtp size {rtp.rect.size}, rtc size {rtc.rect.size}");

                rtp.anchorMin = new Vector2(1, 0);
                rtp.anchorMax = new Vector2(1, 0);
                rtp.pivot = new Vector2(1, 0);
            }
        }
        //[HarmonyPatch(typeof(UIResult), "Open")]
        static class UIResult_Open_Patch
        {
            static void Postfix(UIResult __instance)
            {
                if (!modEnabled.Value || Global.code.curlocation.companionPrisoner || Random.value > rewardChance.Value)
                    return;

                Dbgl("Trying to add companion reward");

                int tries = 0;
                while (tries++ < 10)
                {
                    Transform t = RM.code.allCompanions.items[Random.Range(0, RM.code.allCompanions.items.Count - 1)];
                    if (t)
                    {
                        Dbgl("Adding companion reward");
                        Global.code.curlocation.companionPrisoner = t;
                        AccessTools.Method(typeof(Location), "GenerateCompanionReward").Invoke(Global.code.curlocation, new object[] { Global.code.curlocation.rewards });
                        return;
                    }
                }
            }
        }
    }
}
