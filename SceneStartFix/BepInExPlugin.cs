using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SceneStartFix
{
    [BepInPlugin("aedenthorn.SceneStartFix", "Scene Start Fix", "0.1.3")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> levelBypass;

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
            nexusID = Config.Bind<int>("General", "NexusID", 114, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(Global), "EndDay")]
        static class Global_EndDay_Patch
        {
            static void Prefix(Global __instance)
            {
                if (!modEnabled.Value)
                    return;

                Dbgl("checking scene objects");

                /*
                if (__instance.siegeArmies == null)
                {
                    Dbgl("Siege armies is null");
                    __instance.siegeArmies = new CommonArray();
                }
                Dbgl($"siege armies: {__instance.siegeArmies.items.Count}");
                var list = __instance.locations.items.FindAll(t => t.GetComponent<Location>().locationType == LocationType.city && t.GetComponent<Location>().isCleared);
                Dbgl($"siege locations: {list.Count}");

                foreach(var t in list)
                {
                    Dbgl($"name {t.GetComponent<Location>().name}");
                }
                */
                if (__instance.siegingLocation)
                {
                    if (!__instance.siegingArmy)
                    {
                        Dbgl($"Sieging army is null, removing sieging location");
                        __instance.siegingLocation = null;
                    }
                }

            }
        }
    }
}
