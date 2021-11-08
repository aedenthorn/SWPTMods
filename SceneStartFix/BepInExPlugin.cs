using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace SceneStartFix
{
    [BepInPlugin("aedenthorn.SceneStartFix", "Scene Start Fix", "0.1.0")]
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
            //nexusID = Config.Bind<int>("General", "NexusID", 7, "Nexus mod ID for updates");

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


                if (__instance.siegingLocation)
                {
                    if (!__instance.siegingArmy)
                    {
                        Dbgl($"Sieging army is null");
                        __instance.siegingArmy = __instance.siegeArmies.items[Random.Range(0, __instance.siegeArmies.items.Count)].GetComponent<Location>();
                    }
                }
                for(int i = __instance.fieldArmies.items.Count - 1; i >= 0; i--)
                {
                    if (__instance.fieldArmies.items[i] && !__instance.fieldArmies.items[i].GetComponent<Location>())
                    {
                        Dbgl($"fieldarmy broken {__instance.fieldArmies.items[i].name}");
                        __instance.fieldArmies.items.RemoveAt(i);
                    }
                }
            }
        }

    }
}
