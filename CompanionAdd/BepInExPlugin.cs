using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CompanionAdd
{
    [BepInPlugin("aedenthorn.CompanionAdd", "Companion Add", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> allowDuplicates;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> recallHotKey;
        public static ConfigEntry<float> rewardChance;
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
            
            allowDuplicates = Config.Bind<bool>("General", "AllowDuplicates", false, "Allow adding a companion you already have (seems to be problematic, resetting levels and stuff).");

            hotKey = Config.Bind<string>("Options", "HotKey", "f2", "Hotkey to add a random companion. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            recallHotKey = Config.Bind<string>("Options", "RecallHotKey", "f3", "Hotkey to call companions to player's location. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

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
                            string name = null;
    
                            List<string> names = new List<string>(Global.code.uiNameChanger.namelist);
                            AedenthornUtils.ShuffleList(names);
                            foreach(string n in names)
                            {
                                if (!Global.code.companions.items.Exists(t => t.name == n))
                                {
                                    name = n;
                                    break;
                                }
                            }
                            if(name == null)
                            {
                                name = c.name + "_";
                                while (Global.code.companions.items.Exists(t => t.name == name))
                                    name += "_";
                            }

                            companion.name = name;
                            companion.GetComponent<CharacterCustomization>().characterName = companion.name;

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
                if (!modEnabled.Value)
                    return;
                List<string> list = ES2.LoadList<string>(__instance.GetFolderName() + "Global.txt?tag=companionlist");
                if (list.Count > 0)
                {
                    foreach (string text in list)
                    {
                        if (text != "")
                        {
                            if (!RM.code.allCompanions.CheckItemByName(text))
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
        [HarmonyPatch(typeof(UICombat), "ShowSuccubusIcons")]
        static class ShowSuccubusIcons_Patch
        {
            static void Prefix(UICombat __instance)
            {
                if (!modEnabled.Value || __instance.succubusIconGroup.parent.name == "ScrollView")
                    return;

                Dbgl("Adding scroll view");

                //GameObject content = new GameObject() { name = "Content" };
                GameObject sv = Instantiate(new GameObject(), __instance.succubusIconGroup.parent);
                ScrollRect sr = sv.AddComponent<ScrollRect>();
                sv.name = "ScrollView";
                sv.GetComponent<RectTransform>().anchoredPosition = __instance.succubusIconGroup.GetComponent<RectTransform>().anchoredPosition;

                GameObject mask = Instantiate(new GameObject(), sv.transform);
                mask.name = "Mask";

                __instance.succubusIconGroup.SetParent(mask.transform);
                __instance.succubusIconGroup.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                sr.viewport = mask.GetComponent<RectTransform>();
                sr.content = __instance.succubusIconGroup.GetComponent<RectTransform>();

                //GameObject cc = Instantiate(content, sv.transform);
                //cc.AddComponent<HorizontalLayoutGroup>();
                //__instance.succubusIconGroup.gameObject.SetActive(false);
                //Destroy(__instance.succubusIconGroup.gameObject);
                //__instance.succubusIconGroup = cc.transform;
                //__instance.succubusIconGroup.SetParent(sv.transform);


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
