using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace FurnitureCost
{
    [BepInPlugin("aedenthorn.FurnitureCost", "Furniture Cost", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> costMult;

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
            nexusID = Config.Bind<int>("General", "NexusID", 45, "Nexus mod ID for updates");

            costMult = Config.Bind<float>("Options", "CostMult", 1f, "Multiply cost of furniture by this amount.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Balancer), nameof(Balancer.GetFurnitureStats))]
        public static class Balancer_GetFurnitureStats_Patch
        {
            public static void Postfix(Transform item)
            {
                if (!modEnabled.Value)
                    return;
                Building component = item.GetComponent<Building>();
                component.cost = Mathf.RoundToInt(component.cost * costMult.Value);
            }
        }
    }
}
