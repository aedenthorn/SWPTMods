using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace StayThatWay
{
    [BepInPlugin("bugerry.StayThatWay", "Stay That Way", "1.0.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        private static readonly HashSet<Transform> backup = new HashSet<Transform>();

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind("General", "NexusID", 81, "Nexus mod ID for updates");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(UIFreePose), "AddCharacter")]
        public static class UIFreePose_AddCharacter_Patch
		{
            public static void Postfix(Transform character)
			{
                if (!modEnabled.Value) return;
                character.GetComponent<ThirdPersonCharacter>().enabled = false;
                character.GetComponent<Animator>().enabled = false;
            }
		}

        [HarmonyPatch(typeof(UIFreePose), "Close")]
        public static class UIFreePose_Close_Patch
        {
            public static void Prefix(UIFreePose __instance)
            {
                if (!modEnabled.Value) return;

                if (Global.code.curlocation.locationType == LocationType.home)
                {
                    foreach (var t in __instance.characters.items)
					{
                        backup.Add(t);
                    }
                    __instance.characters.ClearItems();
                    __instance.characters.AddItem(Player.code.transform);
                }

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

            public static void Postfix()
			{
                if (!modEnabled.Value) return;
                foreach (var transform in backup)
                {
                    transform.GetComponent<Animator>().enabled = false;
                }
                Player.code.GetComponent<Animator>().enabled = true;
            }
        }

        [HarmonyPatch(typeof(UICombatParty), "Open")]
        public static class UICombatParty_Start_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value) return;

                foreach (var transform in backup)
				{
                    if (!transform) continue;
                    CharacterCustomization component = transform.GetComponent<CharacterCustomization>();
                    component.anim.runtimeAnimatorController = RM.code.combatController;
                    component.anim.avatar = RM.code.flatFeetAvatar;
                    component.anim.enabled = true;
                    transform.GetComponent<Rigidbody>().isKinematic = false;
                    transform.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;


                    if (transform.GetComponent<NavMeshAgent>())
                    {
                        transform.GetComponent<NavMeshAgent>().enabled = true;
                    }

                    if (transform.GetComponent<ThirdPersonCharacter>())
                    {
                        transform.GetComponent<ThirdPersonCharacter>().enabled = true;
                    }

                    component.RefreshClothesVisibility();
                    component.characterLightGroup.transform.localEulerAngles = Vector3.zero;
                }
                backup.Clear();
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
                return false;
            }
        }
    }
}
