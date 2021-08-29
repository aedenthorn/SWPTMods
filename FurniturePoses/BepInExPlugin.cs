using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace FurniturePoses
{
    [BepInPlugin("aedenthorn.FurniturePoses", "Furniture Poses", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<int> nexusID;
        public static bool opening = false;

        public static Dictionary<string, Transform> allPoses = new Dictionary<string, Transform>();
        private static GameObject parentContainer;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 27, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
            parentContainer = new GameObject();
            parentContainer.name = "AllFurniturePoses";
            DontDestroyOnLoad(parentContainer);
        }
        [HarmonyPatch(typeof(UIPose), nameof(UIPose.Open))]
        static class UIPose_Open_Patch
        {
            static void Prefix()
            {
                if (!modEnabled.Value)
                    return;
                opening = true;
            }
            static void Postfix(UIPose __instance, Furniture furniture, CommonArray ___poses)
            {
                if (!modEnabled.Value)
                    return;

                foreach (Transform t in RM.code.allBuildings.items)
                {
                    if (t?.GetComponent<Furniture>() && t.GetComponent<Furniture>().poses != null)
                    {
                        foreach (Transform p in t.GetComponent<Furniture>().poses.items)
                        {
                            if (p && !allPoses.ContainsKey(t.name + "_" + p.name))
                                allPoses[t.name + "_" + p.name] = Instantiate(p, parentContainer.transform);
                        }
                    }
                }

                foreach (var kvp in allPoses)
                {
                    if(!___poses.items.Exists(t => kvp.Key.StartsWith(furniture.name +"_") && t.name == kvp.Key.Substring((furniture.name + "_").Length)))
                    {
                        ___poses.AddItem(kvp.Value);
                    }
                }

                opening = false;
                __instance.Refresh();
            }
        }
        [HarmonyPatch(typeof(UIPose), nameof(UIPose.Refresh))]
        static class UIPose_Refresh_Patch
        {
            static bool Prefix()
            {
                return !modEnabled.Value || !opening;
            }
        }
    }
}
