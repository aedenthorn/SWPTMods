using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace EnhancedFreePose
{
    [BepInPlugin("aedenthorn.EnhancedFreePose", "Enhanced Free Pose", "0.4.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<int> maxModels;

        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<float> freeCameraSpeed;
        public static ConfigEntry<float> freeCameraBoostMult;
        public static ConfigEntry<string> xRotateModKey;
        public static ConfigEntry<string> zRotateModKey;

        private static Vector3 lastCursorPoint = new Vector3(-1, -1, -1);

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
            nexusID = Config.Bind<int>("General", "NexusID", 18, "Nexus mod ID for updates");

            maxModels = Config.Bind<int>("Options", "MaxModels", 8, "Maximum number of models to allow.");
            freeCameraSpeed = Config.Bind<float>("Options", "FreeCameraSpeed", 2f, "Free camera move speed.");
            freeCameraBoostMult = Config.Bind<float>("Options", "FreeCameraBoostMult", 3f, "Multiply camera move speed by this when holding down game's camera boost key (left shift).");
            xRotateModKey = Config.Bind<string>("Options", "XRotateModKey", "left shift", "Modifier key to rotate around X-axis. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            zRotateModKey = Config.Bind<string>("Options", "ZRotateModKey", "left ctrl", "Modifier key to rotate around Z-axis. Use https://docs.unity3d.com/Manual/class-InputManager.html");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(UIFreePose), "Refresh")]
        public static class Update_Patch
        {
            public static void Postfix(UIFreePose __instance)
            {
                if (!modEnabled.Value)
                    return;
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

        [HarmonyPatch(typeof(CustomizationSlider), nameof(CustomizationSlider.ValueChange))]
        public static class CustomizationSlider_ValueChange_Patch
        {
            public static bool Prefix(CustomizationSlider __instance, float val)
            {
                if (!modEnabled.Value || !Global.code.uiFreePose.gameObject.activeSelf || !__instance.isEmotionController)
                    return true;

                Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().body.SetBlendShapeWeight(__instance.index, val);
                Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>().eyelash.SetBlendShapeWeight(__instance.index, val);

                return false;

            }
        }
        [HarmonyPatch(typeof(FreelookCamera), nameof(FreelookCamera.FixedUpdate))]
        public static class FreelookCamera_FixedUpdate_Patch
        {
            public static void Prefix(FreelookCamera __instance)
            {
                if (!modEnabled.Value || Global.code?.uiFreePose.gameObject.activeSelf != true)
                    return;
                __instance.Speed = freeCameraSpeed.Value;
                __instance.BoostSpeed = freeCameraSpeed.Value * freeCameraBoostMult.Value;
            }
        }
        [HarmonyPatch(typeof(MoveObject), nameof(MoveObject.LetObjectGo))]
        public static class LetObjectGo_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value || !Global.code.uiFreePose.gameObject.activeSelf)
                    return;
                lastCursorPoint = new Vector3(-1, -1, 0);

            }
        }
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.LateUpdate))]
        public static class CharacterCustomization_LateUpdate_Patch
        {
            public static void Prefix(Transform ___mytransform, ref Vector3 __state)
            {
                if (!modEnabled.Value || Global.code?.uiFreePose.gameObject.activeSelf != true)
                    return;

                __state = ___mytransform.eulerAngles;
            }
            public static void Postfix(Transform ___mytransform, Vector3 __state)
            {
                if (!modEnabled.Value || Global.code?.uiFreePose.gameObject.activeSelf != true)
                    return;

                ___mytransform.eulerAngles = __state;
            }

        }
        [HarmonyPatch(typeof(MoveObject), "Update")]
        public static class MoveObject_Update_Patch
        {

            public static void Prefix(MoveObject __instance, ref Vector3 __state)
            {
                if (!modEnabled.Value || !__instance.canGo || !Global.code.uiFreePose.selectedCharacter)
                {
                    return;
                }
                if(__instance.rotate)
                    __state = Global.code.uiFreePose.selectedCharacter.transform.eulerAngles;
                else if (__instance.move)
                    __state = Global.code.uiFreePose.selectedCharacter.transform.position;
            }
            public static void Postfix(MoveObject __instance, Vector3 __state)
            {
                if (!modEnabled.Value || !__instance.canGo || !Global.code.uiFreePose.selectedCharacter)
                    return;

                Vector3 cursorPoint = Input.mousePosition;
                float deltaX = cursorPoint.x - lastCursorPoint.x;
                float deltaY = cursorPoint.y - lastCursorPoint.y;


                if (__instance.move)
                {

                    if (lastCursorPoint.x >= 0 && lastCursorPoint.y >= 0 && Mathf.Abs(Screen.width - Mathf.Abs(deltaX)) > 100 && Mathf.Abs(Screen.height - Mathf.Abs(deltaY)) > 100)
                    {

                        Quaternion q = Global.code.freeCamera.transform.rotation;
                        q.z = 0;
                        //q.eulerAngles = new Vector3(q.eulerAngles.x, 0, q.eulerAngles.z).normalized;
                        Vector3 a = q * new Vector3(deltaX, 0, deltaY);
                        a = new Vector3(a.x, 0, a.z).normalized * a.magnitude;
                        //Dbgl($"q {q}, a {a}");
                        __instance.mover.transform.position = Vector3.Lerp(__state, __state + a, Time.deltaTime / 2f);
                    }
                    else
                    {
                        __instance.mover.transform.position = __state;
                    }
                    lastCursorPoint = cursorPoint;

                }
                else if (__instance.rotate)
                {
                    if (lastCursorPoint.x >= 0 && lastCursorPoint.y >= 0 && Mathf.Abs(Screen.width - Mathf.Abs(deltaX)) > 100 && Mathf.Abs(Screen.height - Mathf.Abs(deltaY)) > 100)
                    {
                        if (AedenthornUtils.CheckKeyHeld(xRotateModKey.Value))
                        {
                            //Dbgl($"trying to rotate {__instance.mover.transform.eulerAngles} x by {__instance.deltaX}");
                            __instance.mover.transform.eulerAngles = Vector3.Lerp(__state, __state + new Vector3(deltaX * __instance.speed_Rotate, 0f, 0f), Time.deltaTime / 20f);
                        }
                        else if (AedenthornUtils.CheckKeyHeld(zRotateModKey.Value))
                        {
                            //Dbgl($"trying to rotate {__instance.mover.transform.eulerAngles} z by {__instance.deltaX}");
                            __instance.mover.transform.eulerAngles = Vector3.Lerp(__state, __state + new Vector3(0f, 0f, deltaX * __instance.speed_Rotate), Time.deltaTime / 20f);
                        }
                        else
                        {
                            __instance.mover.transform.eulerAngles = Vector3.Lerp(__state, __state + new Vector3(0f, -deltaX * __instance.speed_Rotate, 0f), Time.deltaTime / 20f);
                        }
                    }
                    else
                    {
                        __instance.mover.transform.eulerAngles = __state;
                    }
                    lastCursorPoint = cursorPoint;
                }

                //Dbgl($"New rotation {__instance.mover.transform.eulerAngles}");
            }
        }
    }
}
