using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityStandardAssets.Cameras;

namespace CameraMod
{
    [BepInPlugin("bugerry.CameraMod", "Camera Mod", "1.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public static readonly Vector3 XY = new Vector3(1f, 1f, 0f);
        public static readonly Vector3 XZ = new Vector3(1f, 0f, 1f);
        public static readonly Vector3 YZ = new Vector3(0f, 1f, 1f);

        private static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> defaultDistance;
        public static ConfigEntry<float> minFov;
        public static ConfigEntry<float> maxFov;
        public static ConfigEntry<float> minFocal;
        public static ConfigEntry<float> maxFocal;
        public static ConfigEntry<Vector3> offset;

        public float fov = 45f;

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind("General", "NexusID", 102, "Nexus mod ID for updates");

            defaultDistance = Config.Bind("Camera", "Default Distance", 2f, "Default distance between camera and target [0..4.6)");
            minFov = Config.Bind("Camera", "FOV Min", 45f, "Min Field of View");
            maxFov = Config.Bind("Camera", "FOV Max", 45f, "Max Field of View");
            minFocal = Config.Bind("Camera", "Focal Length Min", 15f, "Min focal length of pose mode camera");
            maxFocal = Config.Bind("Camera", "Focal Length Max", 90f, "Max focal length of pose mode camera");
            offset = context.Config.Bind("Camera", "Offset", new Vector3(0f, 1.5f, 0f), "Offset between pivot and target");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void LateUpdate()
        {
            if (!modEnabled.Value) return;
            if (FreeLookCam.code && FreeLookCam.code.isActiveAndEnabled)
            {
                Camera.main.fieldOfView = fov;
            }
            else if (Global.code && Global.code.freeCamera && Global.code.freeCamera.activeInHierarchy)
            {
                Camera.main.fieldOfView = Mathf.Clamp(Camera.main.fieldOfView - Input.GetAxis("Mouse ScrollWheel"), minFocal.Value, maxFocal.Value);
            }
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
                var right = __instance.cam.transform.right * offset.Value.x;
                var up = Vector3.up * offset.Value.y;
                var forward = __instance.cam.transform.forward * offset.Value.z;
                __instance.cam.transform.parent.position = __instance.cam.transform.parent.parent.position + right + up + forward;
                context.fov = minFov.Value - (maxFov.Value - minFov.Value) * __instance.cam.transform.localPosition.z / 4.3f;
            }
        }

        [HarmonyPatch(typeof(UISettings), "Awake")]
        public static class UISettings_Awake_Patch
        {
            public static MethodBase TargetMethod()
            {
                return typeof(UISettings).GetMethod("Awake");
            }

            public static Transform AddToggle(
                UISettings __instance,
                string name,
                UnityAction<bool> action,
                bool current = false)
            {
                var panel = Instantiate(__instance.FullScreen.transform.parent, __instance.FullScreen.transform.parent.parent);
                var toggle = panel.GetComponentInChildren<Toggle>();
                toggle.isOn = current;
                panel.GetComponentInChildren<Text>().text = name;
                toggle.onValueChanged = new Toggle.ToggleEvent();
                toggle.onValueChanged.AddListener(action);
                Destroy(panel.GetComponentInChildren<LocalizationText>());
                return panel;
            }

            public static Slider AddSlider(
                UISettings __instance,
                string name,
                UnityAction<float> action,
                float current = 0f,
                float min = 0f,
                float max = 1f,
                string precision = "f1")
            {
                var panel = Instantiate(__instance.FullScreen.transform.parent, __instance.FullScreen.transform.parent.parent);
                var toggle = panel.GetComponentInChildren<Toggle>();
                toggle.transform.parent = null;
                Destroy(panel.GetComponentInChildren<LocalizationText>());
                Destroy(toggle);
                panel.GetComponentInChildren<Text>().text = name;
                var slider = Instantiate(__instance.JoySensitivitySlider, panel);
                slider.minValue = min;
                slider.maxValue = max;
                slider.onValueChanged = new Slider.SliderEvent();
                slider.onValueChanged.AddListener(action);
                var comp = slider.transform.GetComponentInChildren<Text>();
                if (comp)
				{
                    slider.onValueChanged.AddListener((float val) => comp.text = slider.value.ToString(precision));
                }
                slider.value = current;
                return slider;
            }

            public static void SetOffsetX(float val)
			{
                offset.Value = Vector3.Scale(offset.Value, YZ) + Vector3.right * val;
			}

            public static void SetOffsetY(float val)
            {
                offset.Value = Vector3.Scale(offset.Value, XZ) + Vector3.up * val;
            }

            public static void SetOffsetZ(float val)
            {
                offset.Value = Vector3.Scale(offset.Value, XY) + Vector3.forward * val;
            }

            public static void Postfix(UISettings __instance)
            {
                if (!modEnabled.Value) return;
                AddToggle(__instance, "Camera Mod", (bool tog) => modEnabled.Value = tog, true);
                AddSlider(__instance, "Default Distance", (float val) => defaultDistance.Value = val, defaultDistance.Value, 0f, 4.6f);
                AddSlider(__instance, "Shoulder Shot (Left/Right)", SetOffsetX, offset.Value.x, -0.5f, 0.5f);
                AddSlider(__instance, "Panty Shot (Up/Down)", SetOffsetY, offset.Value.y, 0.6f, 2f).direction = Slider.Direction.RightToLeft;
                AddSlider(__instance, "Panorama Shot (FOV)", (float val) => maxFov.Value = val, maxFov.Value, 45f, 60f, "f0");
            }
        }
    }
}
