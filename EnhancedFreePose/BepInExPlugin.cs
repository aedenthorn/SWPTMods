using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace EnhancedFreePose
{
    [BepInPlugin("aedenthorn.EnhancedFreePose", "Enhanced Free Pose", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<int> maxModels;

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

            maxModels = Config.Bind<int>("Options", "MaxModels", 8, "Maximum number of models to allow.");

            nexusID = Config.Bind<int>("General", "NexusID", 18, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(UIFreePose), "Refresh")]
        public static class Update_Patch
        {
            public static void Postfix(UIFreePose __instance)
            {
                __instance.transform.Find("Left").Find("group pose").Find("tools bg").GetComponent<RectTransform>().anchoredPosition = new Vector2(171, -51);
                for (int j = 4; j < maxModels.Value; j++)
                {
                    Transform transform = Instantiate(__instance.companionIconPrefab);
                    transform.SetParent(__instance.companionIconHolder);
                    transform.localScale = Vector3.one;
                    if (j < __instance.characters.items.Count)
                    {
                        Transform transform2 = __instance.characters.items[j];
                        transform.GetComponent<FreeposeCompanionIcon>().Initiate(transform2.GetComponent<CharacterCustomization>());
                    }
                    else
                    {
                        transform.GetComponent<FreeposeCompanionIcon>().Initiate(null);
                    }
                }
            }

        }
        [HarmonyPatch(typeof(FreeposeCompanionIcon), "Initiate")]
        public static class Initiate_Patch
        {
            public static void Prefix(CharacterCustomization _customization)
            {
                Dbgl($"initiating {_customization?.characterName}");
            }
        }
    }
}
