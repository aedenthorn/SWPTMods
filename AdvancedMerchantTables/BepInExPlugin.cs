using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AdvancedMerchantTables
{
    [BepInPlugin("aedenthorn.AdvancedMerchantTables", "Advanced Merchant Tables", "0.1.1")]
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

            levelBasedLootWeighting = Config.Bind<float>("Options", "LevelBasedLootWeighting", 1.0f, "Fraction weighting for preferring loot that matches the player level (1 is vanilla, only match with player level, and 0 means totally not level-matched). Preference is still for items with levels closer to the player.");
            merchantRefreshInterval = Config.Bind<int>("Options", "MerchantRefreshInterval", 1000, "Interval in seconds to refresh merchant inventory.");
            //refreshOnDayEnd = Config.Bind<bool>("Options", "RefreshOnDayEnd", false, "Refresh inventory at the end of every day.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Merchant), "Refresh")]
        static class Merchant_Refresh_Patch
        {
            static void Prefix(Merchant __instance)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl($"Clearing storage. sure {__instance.sureItems.Length}, weapons {__instance.weaponAmount}, armor {__instance.armorAmount}, misc {__instance.miscAmount}, potion {__instance.potionAmount}, scroll {__instance.minionScrollAmount}");
                __instance._Storage.ClearStorage();
            }
            static void Postfix(Merchant __instance)
            {
                if (!modEnabled.Value)
                    return;
                Dbgl($"Setting refresh timer to {merchantRefreshInterval.Value}");
                __instance.refreshTimer = merchantRefreshInterval.Value;
            }
        }        
               
        [HarmonyPatch(typeof(Merchant), "GetMatchLevelItems")]
        static class Merchant_GetMatchLevelItems_Patch
        {
            static void Postfix(List<Transform> list, List<Transform> __result)
            {
                if (!modEnabled.Value || levelBasedLootWeighting.Value == 1)
                    return;

                Dbgl($"count: {__result.Count}");

                List<Transform> armor = new List<Transform>();
                List<Transform> extra = new List<Transform>();
                foreach (Transform t in list)
                {
                    if (!__result.Contains(t))
                    {
                        float add;
                        if (Player.code.customization._ID.level > t.GetComponent<Item>().level)
                        {
                            add = (Player.code.customization._ID.level / 2f - t.GetComponent<Item>().level) / Player.code.customization._ID.level;
                        }
                        else
                        {
                            add = -(Player.code.customization._ID.level * 1.5f - t.GetComponent<Item>().level) / t.GetComponent<Item>().level; // 6 12, 9 - 12 / 12 = -0.25 // 10 12, 15 - 12 / 12 = 0.25
                        }
                        //Dbgl($"Item level {t.GetComponent<Item>().level}, extra chance {add}");

                        if (Random.value > levelBasedLootWeighting.Value * (1 + add))
                        {
                            if (t.GetComponent<Item>().slotType == SlotType.armor)
                                armor.Add(t);
                            else
                                extra.Add(t);
                        }
                    }
                }
                if (armor.Any())
                {
                    var oarmor = __result.FindAll(t => t.GetComponent<Item>().slotType == SlotType.armor);
                    var oextra = __result.FindAll(t => t.GetComponent<Item>().slotType != SlotType.armor);
                    armor.AddRange(oarmor);
                    extra.AddRange(oextra);
                    AedenthornUtils.ShuffleList(armor);
                    AedenthornUtils.ShuffleList(extra);
                    __result = armor.Take(oarmor.Count).ToList();
                    __result.AddRange(extra.Take(oextra.Count));
                    //Dbgl($"result with armor {oarmor.Count} {oextra.Count} {__result.Count}");
                }
                else
                {
                    int count = __result.Count;
                    extra.AddRange(__result);
                    AedenthornUtils.ShuffleList(extra);
                    __result = extra.Take(count).ToList();
                    //Dbgl($"result without armor {count} {__result.Count}");
                }
            }
        }        
            
    }
}
