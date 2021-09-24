using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AdvancedMerchantTables
{
    [BepInPlugin("bugerry.AdvancedMerchantTables", "Advanced Merchant Tables", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<bool> refreshOnDayEnd;
        public static ConfigEntry<float> levelBasedLootWeighting;
        public static ConfigEntry<float> autoSaveInterval;
        public static ConfigEntry<int> merchantRefreshInterval;
        
        public static List<Merchant> merchants = new List<Merchant>();

        public static ConfigEntry<int> nexusID;

        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");

            levelBasedLootWeighting = Config.Bind("Options", "LevelBasedLootWeighting", 1.0f, "Fraction weighting for preferring loot that matches the player level (1 is vanilla, only match with player level, and 0 means totally not level-matched). Preference is still for items with levels closer to the player.");
            merchantRefreshInterval = Config.Bind("Options", "MerchantRefreshInterval", 1000, "Interval in seconds to refresh merchant inventory.");
            
            nexusID = Config.Bind("General", "NexusID", 21, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }       
               
        [HarmonyPatch(typeof(Merchant), "GetMatchLevelItems")]
        public static class Merchant_GetMatchLevelItems_Patch
        {
            public static void Postfix(List<Transform> list, out List<Transform> __result)
            {
                //if (!modEnabled.Value || levelBasedLootWeighting.Value == 1) return;
                __result = list.FindAll((Transform t) => Random.value < levelBasedLootWeighting.Value);
            }
        }        
            
    }
}
