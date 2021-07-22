using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityStandardAssets.Cameras;
using Random = UnityEngine.Random;

namespace FieldOfView
{
    [BepInPlugin("aedenthorn.FieldOfView", "Field Of View", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<float> FOV;
        public static ConfigEntry<float> zoomedFOV;
        public static ConfigEntry<bool> useScrollWheel;
        public static ConfigEntry<string> scrollModKey;

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

            useScrollWheel = Config.Bind<bool>("Options", "ScrollWheel", true, "Use scroll wheel to change FOV");
            scrollModKey = Config.Bind<string>("Settings", "ScrollModKey", "", "Modifer key to allow scroll wheel change. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            FOV = Config.Bind<float>("Options", "FOV", 50, "Ordinary FOV.");
            zoomedFOV = Config.Bind<float>("Options", "ZoomedFOV", 100, "Zoomed FOV.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(Player), "Update")]
        static class Update_Patch
        {
            static void Prefix(Player __instance)
            {
                if (!modEnabled.Value || __instance.zooming)
                    return;

                if (useScrollWheel.Value && AedenthornUtils.CheckKeyHeld(scrollModKey.Value, false) && Input.mouseScrollDelta.y != 0)
                {
                    FOV.Value += Input.mouseScrollDelta.y;
                    Dbgl($"FOV {FOV.Value}");
                }

            }
            static void Postfix(Player __instance, float __state)
            {
                if (!modEnabled.Value || __instance.zooming)
                    return;

                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, FOV.Value, Time.deltaTime * 6f);

            }
        }
    }
}
