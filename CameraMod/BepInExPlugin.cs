using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityStandardAssets.Cameras;
using Assets.DuckType.Jiggle;

namespace CameraMod
{
    [BepInPlugin("bugerry.CameraMod", "Camera Mod", "1.0.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> defaultDistance;
        public static ConfigEntry<float> minFov;
        public static ConfigEntry<float> maxFov;
        public static ConfigEntry<Vector3> targetOffset;

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind("General", "NexusID", 102, "Nexus mod ID for updates");

            defaultDistance = Config.Bind("Camera", "Default Distance", 2f, "Default distance between camera and target [0..4.6)");
            minFov = Config.Bind("Camera", "FOV Min", 45f, "Min Field of View");
            maxFov = Config.Bind("Camera", "FOV Max", 45f, "Max Field of View");
            targetOffset = context.Config.Bind("Camera", "Offset", new Vector3(0f, 1.5f, 0f), "Offset between pivot and target");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(ProtectCameraFromWallClip), "Start")]
        public static class ProtectCameraFromWallClip_Start_Patch
        {
            public static MethodBase TargetMethod()
            {
                return typeof(ProtectCameraFromWallClip).GetMethod("Start");
            }

            public static void Postfix(ProtectCameraFromWallClip __instance)
            {
                if (!modEnabled.Value) return;
                __instance.targetdist = defaultDistance.Value;
                __instance.targetdistCombat = defaultDistance.Value;
            }
        }

        [HarmonyPatch(typeof(FreeLookCam), "Update")]
        public static class FreeLookCam_Update_Patch
        {
            public static MethodBase TargetMethod()
            {
                return typeof(FreeLookCam).GetMethod("Update");
            }

            public static void Prefix(FreeLookCam __instance)
			{
                if (!modEnabled.Value) return;
                var right = __instance.cam.transform.right * targetOffset.Value.x;
                var up = Vector3.up * targetOffset.Value.y;
                var forward = __instance.cam.transform.forward * targetOffset.Value.z;
                __instance.cam.transform.parent.position = __instance.cam.transform.parent.parent.position + right + up + forward;
                __instance.cam.fieldOfView = minFov.Value - (maxFov.Value - minFov.Value) * __instance.cam.transform.localPosition.z / 4.3f;
            }
        }
    }
}
