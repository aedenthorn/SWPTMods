using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace RemoveFromFreePose
{
    [BepInPlugin("aedenthorn.RemoveFromFreePose", "Remove From Free Pose", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<string> modKey;

        public static ConfigEntry<int> nexusID;

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

            modKey = Config.Bind<string>("Options", "ModKey", "left shift", "Modifier key to remove companion.");

            nexusID = Config.Bind<int>("General", "NexusID", 26, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(FreeposeCompanionIcon), nameof(FreeposeCompanionIcon.Click))]
        static class FreeposeCompanionIcon_Click_Patch
        {
            static bool Prefix(FreeposeCompanionIcon __instance)
            {

                if (!modEnabled.Value || !__instance.customization || __instance.customization._Player || !AedenthornUtils.CheckKeyHeld(modKey.Value) || !Global.code.uiFreePose.gameObject.activeSelf || !Global.code.uiFreePose.characters.items.Contains(__instance.customization.transform))
                    return true;

                Dbgl($"Clicked on {__instance.customization.name}");

                Global.code.uiFreePose.LetRuntimeTransformSleep();

                CharacterCustomization component = __instance.customization.GetComponent<CharacterCustomization>();
                Player.code.enabled = true;
                component.anim.runtimeAnimatorController = RM.code.combatController;
                component.anim.avatar = RM.code.flatFeetAvatar;
                __instance.customization.GetComponent<Rigidbody>().isKinematic = false;
                __instance.customization.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                if (__instance.customization.GetComponent<NavMeshAgent>())
                {
                    __instance.customization.GetComponent<NavMeshAgent>().enabled = true;
                }
                component.RefreshClothesVisibility();
                component.characterLightGroup.transform.localEulerAngles = Vector3.zero;

                Global.code.uiFreePose.characters.items.Remove(__instance.customization.transform);
                Scene.code.SpawnCompanion(__instance.customization.transform);

                Global.code.uiFreePose.Refresh();
                RM.code.PlayOneShot(RM.code.sndSelectCompanion);

                Dbgl($"Removed {__instance.customization.name}");

                return false;
            }
        }
    }
}
