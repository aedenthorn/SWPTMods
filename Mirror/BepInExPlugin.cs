using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace MirrorMod
{
    [BepInPlugin("aedenthorn.MirrorMod", "MirrorMod", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 38, "Nexus mod ID for updates");
            
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(Furniture), "Start")]
        static class Furniture_Start_Patch
        {
            static void Prefix(Furniture __instance)
            {
                if (!modEnabled.Value || !__instance.GetComponent<Mirror>())
                    return;
                RenderTexture rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
                rt.Create();

                Transform mirrorSelf = new GameObject().transform;
                mirrorSelf.SetParent(__instance.transform);
                mirrorSelf.name = "MirrorSelf";

                Transform ct = new GameObject().transform;
                ct.SetParent(__instance.transform);
                Camera c = mirrorSelf.gameObject.AddComponent<Camera>();
                c.nearClipPlane = 0.5f;
                mirrorSelf.position -= new Vector3(0, 0, 0.5f);

                var mr = mirrorSelf.gameObject.AddComponent<MeshRenderer>();

                var mScript = __instance.gameObject.AddComponent<MirrorScript>();
                mScript.playerCam = Player.code.m_Cam;
                mScript.mirrorCam = c.transform;
            }
        }
    }
}
