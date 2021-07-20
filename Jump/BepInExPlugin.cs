using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Jump
{
    [BepInPlugin("aedenthorn.Jump", "Jump", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> multiJump;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<float> jumpPower;
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
            
            multiJump = Config.Bind<bool>("General", "MultiJump", true, "Enable in air jumping");

            hotKey = Config.Bind<string>("Options", "HotKey", "space", "Hotkey to jump.");
            jumpPower = Config.Bind<float>("Options", "JumpPower", 6, "Player jump power.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(ThirdPersonCharacter), "Snap")]
        static class Snap_Patch
        {
            static bool Prefix(ThirdPersonCharacter __instance)
            {
                if (!modEnabled.Value)
                    return true;

                AccessTools.Method(typeof(ThirdPersonCharacter), "CheckGroundStatus").Invoke(__instance, null);

                return false;
            }
        }

        [HarmonyPatch(typeof(ThirdPersonCharacter), "CheckGroundStatus")]
        static class ThirdPersonCharacter_CheckGroundStatus_Patch
        {
            static bool Prefix(ThirdPersonCharacter __instance, ref Vector3 ___m_GroundNormal, int ___layerMask, CharacterCustomization ___customization, Rigidbody ___m_Rigidbody)
            {
                if (!modEnabled.Value)
                    return true;
                __instance.m_IsGrounded = true;
                RaycastHit raycastHit;
                if (___m_Rigidbody.velocity.y < 5f && Physics.Raycast(__instance.transform.position + Vector3.up * 0.1f, Vector3.down, out raycastHit, 0.5f, ___layerMask))
                {
                    ___m_GroundNormal = raycastHit.normal;
                    __instance.m_IsGrounded = true;
                    if (!Player.code.focusedInteraction)
                    {
                        __instance.transform.position = new Vector3(__instance.transform.position.x, raycastHit.point.y, __instance.transform.position.z);
                    }
                    ___customization.curSurfaceTag = raycastHit.collider.tag;
                    return false;
                }
                __instance.m_IsGrounded = false;
                ___m_GroundNormal = Vector3.up;
                return false;
            }
        }

        [HarmonyPatch(typeof(ThirdPersonCharacter), "Move")]
        static class ThirdPersonCharacter_Move_Patch
        {
            static void Postfix(ThirdPersonCharacter __instance, bool crouch, bool jump, Vector3 move)
            {
                if (!modEnabled.Value || !jump)
                    return;

                Dbgl($"character move, grounded {__instance.m_IsGrounded}");

                if (multiJump.Value)
                {
                    AccessTools.Method(typeof(ThirdPersonCharacter), "HandleGroundedMovement").Invoke(__instance, new object[] { crouch, jump });
                    AccessTools.Method(typeof(ThirdPersonCharacter), "UpdateAnimator").Invoke(__instance, new object[] { move });

                }

            }
        }

        private static int frames = 0;

        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Prefix(Player __instance, ref bool ___m_Jump, ThirdPersonCharacter ___m_Character)
            {
                if (!modEnabled.Value || !AedenthornUtils.CheckKeyDown(hotKey.Value))
                    return;
                Dbgl($"Jumping");
                //frames = 1;
                AccessTools.Field(typeof(ThirdPersonCharacter), "m_JumpPower").SetValue(___m_Character, jumpPower.Value);
                ___m_Jump = true;
            }
            static void Postfix(Player __instance, ref bool ___m_Jump, ThirdPersonCharacter ___m_Character)
            {
                if (frames == 0)
                    return;
                if (frames < 10)
                {
                    Dbgl($"player velocity {__instance.rigidbody.velocity}");
                }
                else
                {
                    frames = 0;
                    return;
                }
                frames++;

            }
        }
        [HarmonyPatch(typeof(ThirdPersonCharacter), "HandleGroundedMovement")]
        static class HandleGroundedMovement_Patch
        {
            static bool Prefix(ThirdPersonCharacter __instance, bool crouch, bool jump, Rigidbody ___m_Rigidbody, ref float ___m_JumpPower, ref bool ___m_IsGrounded, ref float ___m_GroundCheckDistance, Animator ___m_Animator)
            {
                if (!modEnabled.Value || !jump)
                    return true;

                Dbgl($"handle grounded movement");

                if (jump && !crouch)
                {
                    Dbgl("Applying jump vel");
                    ___m_Rigidbody.velocity = new Vector3(___m_Rigidbody.velocity.x, ___m_JumpPower, ___m_Rigidbody.velocity.z);
                    ___m_IsGrounded = false;
                    ___m_GroundCheckDistance = 0.5f;
                    //___m_Animator.SetTrigger("Jump")
                }
                return false;
            }
            static void Postfix(ThirdPersonCharacter __instance, bool crouch, bool jump, Rigidbody ___m_Rigidbody, float ___m_JumpPower)
            {
                if (!modEnabled.Value || !jump)
                    return;
                Dbgl($"Jumping {jump}, power {___m_JumpPower}, vel: {___m_Rigidbody.velocity}");
            }
        }
    }
}
