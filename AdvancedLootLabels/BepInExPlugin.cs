using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace AdvancedLootLabels
{
    [BepInPlugin("aedenthorn.AdvancedLootLabels", "Advanced Loot Labels", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> distanceBias;
        public static ConfigEntry<float> scaleBias;
        public static ConfigEntry<float> baseMaxDistance;
        public static ConfigEntry<float> maxScale;
        //public static ConfigEntry<float> distanceToShowItemType;
        public static ConfigEntry<Rarity> minRarityToShow;

        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 65, "Nexus mod ID for updates");
            
            baseMaxDistance = Config.Bind<float>("Options", "BaseMaxDistance", 30f, "Max distance to show label.");
            distanceBias = Config.Bind<float>("Options", "DistanceBias", 0.8f, "Max display distance bias based on rarity.");
            maxScale = Config.Bind<float>("Options", "MaxScale", 1.2f, "Max scale.");
            scaleBias = Config.Bind<float>("Options", "ScaleBias", 0.5f, "Scale bias based on rarity.");
            minRarityToShow = Config.Bind<Rarity>("Options", "MinRarityToShow", Rarity.one, "Minimum rarity to show label.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(MapIcon), "CS")]
        static class MapIcon_LateUpdate_Patch
        {
            static void Postfix(MapIcon __instance)
            {
                if (!modEnabled.Value || !__instance.GetComponent<Item>())
                    return;

                float r = ((int)__instance.GetComponent<Item>().rarity - 1) / 5f;

                float distance = Vector3.Distance(__instance.transform.position, Player.code.transform.position);
                float maxDistance = baseMaxDistance.Value * (1 - (1 - r) * distanceBias.Value);
                if (distance < maxDistance && __instance.GetComponent<Item>().rarity >= minRarityToShow.Value)
                {
                    __instance.inrange = true;
                }
                else
                {
                    __instance.inrange = false;
                }
                /*
                if(distance > __instance.visibleRange * distanceToShowItemType.Value)
                {
                    __instance.groundItemText.gameObject.SetActive(false);
                    __instance.objectIndicator.gameObject.SetActive(true);
                }
                else
                */

                __instance.groundItemText.transform.localScale = Vector3.one * maxScale.Value * (1 - (1 - r) * scaleBias.Value);
            }
        }
    }
}
