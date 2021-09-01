using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace EnhancedFreePose
{
    [BepInPlugin("aedenthorn.EnhancedFreePose", "Enhanced Free Pose", "0.2.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> maxModels;
        public static ConfigEntry<int> nexusID;
        
        public static Dictionary<MoveObject, Vector3> lastPositions = new Dictionary<MoveObject, Vector3>();

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
            maxModels = Config.Bind("Options", "MaxModels", 8, "Maximum number of models to allow.");
            nexusID = Config.Bind("General", "NexusID", 18, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(UIFreePose), "Refresh")]
        public static class Update_Patch
        {
            public static void Postfix(UIFreePose __instance)
            {
                if (!modEnabled.Value) return;

                __instance.transform.Find("Left").Find("group pose").Find("tools bg").GetComponent<RectTransform>().anchoredPosition = new Vector2(171, -51);
                for (int j = 4; j < maxModels.Value; j++)
                {
                    Transform transform = Instantiate(__instance.companionIconPrefab);
                    transform.SetParent(__instance.companionIconHolder);
                    transform.localScale = Vector3.one;
                    if (j < __instance.characters.items.Count)
                    {
                        Transform transform2 = __instance.characters.items[j];
                        transform.GetComponent<FreeposeCompanionIcon>().Initiate(transform2.GetComponent<CharacterCustomization>());
                    }
                    else
                    {
                        transform.GetComponent<FreeposeCompanionIcon>().Initiate(null);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(UIFreePose), "AddCharacter")]
        public static class UIFreePose_AddCharacter_Patch
		{
            public static void Postfix(Transform character, UIFreePose __instance)
			{
                if (!modEnabled.Value) return;
                __instance.selectedCharacter = character;
                ThirdPersonCharacter component = character.GetComponent<ThirdPersonCharacter>();
                if (component)
                {
                    component.enabled = false;
                }
            }
		}

        [HarmonyPatch(typeof(UIFreePose), "Open")]
        public static class UIFreePose_Open_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value) return;
                Global.code.freeCameraCollider.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(UIFreePose), "Close")]
        public static class UIFreePose_Close_Patch
        {
            public static void Prefix(UIFreePose __instance)
            {
                if (!modEnabled.Value) return;

                foreach (Transform character in __instance.characters.items)
                {
                    if (!character) continue;

                    ThirdPersonCharacter component = character.GetComponent<ThirdPersonCharacter>();
                    if (component)
                    {
                        component.enabled = true;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MoveObject), "Update")]
        public static class MoveObject_Update_Patch
		{
            public static MethodBase TargetMethod()
			{
                return typeof(MoveObject).GetMethod("Update");
            }

            public static bool Prefix(MoveObject __instance)
			{
                if (!modEnabled.Value) return true;
                Vector3 lastPos;
                Vector3 mousePosition = Input.mousePosition;
                if (__instance.canGo && Global.code.uiFreePose.selectedCharacter && lastPositions.TryGetValue(__instance, out lastPos))
                {
                    int width = Screen.width;
                    int height = Screen.height;
                    __instance.deltaX = (float)(mousePosition.x - lastPos.x) / width;
                    __instance.deltaY = (float)(mousePosition.y - lastPos.y) / height;

                    CharacterCustomization component = Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>();
                    __instance.mover = Global.code.uiFreePose.selectedCharacter;
                    if (__instance.move)
                    {
                        Vector3 normalized = Vector3.Scale(Global.code.freeCamera.transform.forward, new Vector3(1f, 0f, 1f)).normalized;
                        Vector3 a = __instance.deltaY * normalized + __instance.deltaX * Global.code.freeCamera.transform.right;
                        __instance.mover.transform.position += a * __instance.speed_Move * Time.deltaTime;
                    }
                    else if (__instance.moveY)
                    {
                        __instance.mover.transform.position += new Vector3(0f, __instance.deltaY * __instance.speed_MoveY * Time.deltaTime * 20f, 0f);
                    }
                    else if (__instance.rotate)
                    {
                        __instance.mover.transform.eulerAngles += new Vector3(0f, -__instance.deltaX * __instance.speed_Rotate * Time.deltaTime * 20f, 0f);
                    }
                    else if (__instance.rotatelighting)
                    {
                        component.characterLightGroup.transform.eulerAngles += new Vector3(0f, -__instance.deltaX * __instance.speed_Rotate * Time.deltaTime * 20f, 0f);
                    }
                    MoveObject.CursorPoint cursorPoint;
                    MoveObject.GetCursorPos(out cursorPoint);
                    if (cursorPoint.X <= 0)
                    {
                        MoveObject.SetCursorPos(width - 2, cursorPoint.Y);
                        mousePosition.x = (float)(width - 2);
                    }
                    if (cursorPoint.X > width - 2)
                    {
                        MoveObject.SetCursorPos(1, cursorPoint.Y);
                        mousePosition.x = 1f;
                    }
                    if (cursorPoint.Y <= 0)
                    {
                        MoveObject.SetCursorPos(cursorPoint.X, height - 2);
                        mousePosition.y = 1f;
                    }
                    if (cursorPoint.Y > height - 2)
                    {
                        MoveObject.SetCursorPos(cursorPoint.X, 1);
                        mousePosition.y = (float)(height - 2);
                    }
                }
                lastPositions[__instance] = mousePosition;
                return false;
			}
        }

        [HarmonyPatch(typeof(CustomizationSlider), nameof(CustomizationSlider.ValueChange))]
        public static class CustomizationSlider_ValueChange_Patch
        {
            public static bool Prefix(CustomizationSlider __instance, float val)
            {
                if (!modEnabled.Value || !Global.code.uiFreePose.gameObject.activeSelf || !__instance.isEmotionController) return true;
                Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().body.SetBlendShapeWeight(__instance.index, val);
                Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().eyelash.SetBlendShapeWeight(__instance.index, val);
                return false;
            }
        }

        [HarmonyPatch(typeof(ThirdPersonCharacter), "Snap")]
        public static class ThirdPersonCharacter_Snap_Patch
        {
            public static MethodBase TargetMethod()
            {
                return typeof(ThirdPersonCharacter).GetMethod("Snap");
            }

            public static bool Prefix(ThirdPersonCharacter __instance)
            {
                if (!modEnabled.Value || __instance.enabled) return true;
                __instance.m_IsGrounded = true;
                return true;
            }
        }

        [HarmonyPatch(typeof(ThirdPersonCharacter), "CheckGroundStatus")]
        public static class ThirdPersonCharacter_CheckGroundStatus_Patch
        {
            public static MethodBase TargetMethod()
            {
                return typeof(ThirdPersonCharacter).GetMethod("CheckGroundStatus");
            }

            public static bool Prefix(ThirdPersonCharacter __instance)
            {
                if (!modEnabled.Value || __instance.enabled) return true;
                __instance.m_IsGrounded = true;
                return true;
            }
        }
    }
}
