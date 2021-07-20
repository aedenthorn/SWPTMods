using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace RescueMissions
{
    [BepInPlugin("aedenthorn.RescueMissions", "Rescue Missions", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        //public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> rescueMissionChance;

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
            
            rescueMissionChance = Config.Bind<float>("General", "RescueMissionChance", 0.5f, "Chance of a rescue mission being activated if it exists on the map (fraction between 0 and 1).");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(Global), nameof(Global.EndDay))]
        static class Global_EndDay_Patch
        {
            static void Postfix(Global __instance)
            {
                if (!modEnabled.Value)
                    return;
                foreach (Transform transform in __instance.locations.items)
                {
                    if (transform)
                    {
                        Location location = transform.GetComponent<Location>();
                        if (location && location.locationType == LocationType.rescue && !location.companionPrisoner && location.level <= Player.code._ID.level && transform.parent?.gameObject.activeSelf == true && Random.value < rescueMissionChance.Value)
                        {
                            Dbgl("Trying to add rescue companion");

                            List<Transform> list = new List<Transform>(RM.code.allCompanions.items);

                            //ShuffleList(list);

                            foreach (Transform c in list)
                            {
                                if (c && !(Transform)AccessTools.Method(typeof(Global), "CompanionRescueGenerated").Invoke(__instance, new object[]{ c }) && !__instance.companions.GetItemWithName(c.name))
                                {
                                    Dbgl($"Adding rescue companion {c.name}");
                                    location.companionPrisoner = c;
                                    location.GenerateRewards();
                                }
                            }
                        }
                    }
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
    }
}
