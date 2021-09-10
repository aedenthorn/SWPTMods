using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace DebugMenu
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        
        [HarmonyPatch(typeof(Global), "Awake")]
        public static class Global_Awake_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                itemNames = RM.code.allItems.items.Select(t => t?.name).ToList();
                itemNames.Sort();
                
                wPrefixes = RM.code.weaponPrefixes.items.Select(t => t?.name).ToList();
                wSuffixes = RM.code.weaponSurfixes.items.Select(t => t?.name).ToList();
                aPrefixes = RM.code.armorPrefixes.items.Select(t => t?.name).ToList();
                aSuffixes = RM.code.armorSurfixes.items.Select(t => t?.name).ToList();
                wPrefixes.Sort();
                wSuffixes.Sort();
                aPrefixes.Sort();
                aSuffixes.Sort();
                
                CreateDebugMenu();
            }
        }
                      
        [HarmonyPatch(typeof(Global), "Update")]
        public static class Global_Update_Patch
        {
            public static bool Prefix()
            {
                if (!modEnabled.Value)
                    return true;

                if (PMC_Input.IsMenu() || Input.GetKeyDown(KeyCode.Escape))
                {
                    if (uiSpawnItem && uiSpawnItem.gameObject.activeSelf)
                        uiSpawnItem.gameObject.SetActive(false);
                    else if (uiDebug && uiDebug.gameObject.activeSelf)
                        uiDebug.gameObject.SetActive(false);
                    else
                        return true;
                    return false;
                }

                if (AedenthornUtils.CheckKeyDown(hotKey.Value))
                {
                    Dbgl("Toggling debug menu");

                    if (uiDebug.gameObject.activeSelf)
                    {
                        if (uiSpawnItem && uiSpawnItem.gameObject.activeSelf)
                            uiSpawnItem.gameObject.SetActive(false);
                        uiDebug.gameObject.SetActive(false);
                    }
                    else
                    {
                        Global.code.uiCheat.gameObject.SetActive(false);
                        uiDebug.gameObject.SetActive(!uiDebug.gameObject.activeSelf);
                    }
                    return false;
                }
                if (Input.GetKeyDown(KeyCode.Tab) && spawnInput && spawnHintText.text.Length > 0)
                {
                    if (spawnInput.GetComponent<InputField>().isFocused)
                    {
                        Dbgl("Filling spawn text");
                        spawnInput.GetComponent<InputField>().text = spawnHintText.text;
                        spawnInput.GetComponent<InputField>().caretPosition = spawnInput.GetComponent<InputField>().text.Length;
                    }
                    else if (spawnPrefixInput.GetComponent<InputField>().isFocused)
                    {
                        Dbgl("Filling spawn prefix text");
                        string item = GetNameFromText(spawnInput.text, itemNames);
                        if (item == null)
                            return false;
                        SlotType st = RM.code.allItems.GetItemWithName(item).GetComponent<Item>().slotType;
                        string prefix = null;
                        if (st == SlotType.weapon)
                        {
                            prefix = GetNameFromText(spawnPrefixInput.text.Trim(), wPrefixes);
                        }
                        else if (armorSlotTypes.Contains(st))
                        {
                            prefix = GetNameFromText(spawnPrefixInput.text.Trim(), aPrefixes);
                        }
                        if(prefix != null)
                        {
                            spawnPrefixInput.GetComponent<InputField>().text = prefix;
                            spawnPrefixInput.GetComponent<InputField>().caretPosition = spawnPrefixInput.GetComponent<InputField>().text.Length;
                        }
                    }
                    else if (spawnSuffixInput.GetComponent<InputField>().isFocused)
                    {
                        Dbgl("Filling spawn suffix text");
                        string item = GetNameFromText(spawnInput.text, itemNames);
                        if (item == null)
                            return false;
                        SlotType st = RM.code.allItems.GetItemWithName(item).GetComponent<Item>().slotType;
                        string suffix = null;
                        if (st == SlotType.weapon)
                        {
                            suffix = GetNameFromText(spawnSuffixInput.text.Trim(), wSuffixes);
                        }
                        else if (armorSlotTypes.Contains(st))
                        {
                            suffix = GetNameFromText(spawnSuffixInput.text.Trim(), aSuffixes);
                        }
                        if(suffix != null)
                        {
                            spawnSuffixInput.GetComponent<InputField>().text = suffix;
                            spawnSuffixInput.GetComponent<InputField>().caretPosition = spawnSuffixInput.GetComponent<InputField>().text.Length;
                        }
                    }
                    return false;
                }
                return true;
            }
        }
                
        [HarmonyPatch(typeof(ES2Reader), "ProcessHeader", new Type[] { typeof(ES2Keys.Key), typeof(ES2Type), typeof(ES2Type), typeof(string) })]
        public static class ES2Reader_ProcessHeader_Patch
        {
            public static void Prefix(ES2Reader __instance, ref ES2Type expectedValue, string tag)
            {
                if (!modEnabled.Value)
                    return;
                if (tag.StartsWith("rarity-"))
                {
                    expectedValue = ES2TypeManager.GetES2Type(typeof(Rarity));
                }
            }
        }

                        
        [HarmonyPatch(typeof(Global), "HandleKeys")]
        public static class HandleKeys_Patch
        {
            public static bool Prefix()
            {
                if (!modEnabled.Value || uiSpawnItem?.gameObject.activeSelf != true)
                    return true;
                return false;
            }
        }

        
        [HarmonyPatch(typeof(Global), "CheckOnGUI")]
        public static class CheckOnGUI_Patch
        {
            public static bool Prefix()
            {
                if (!modEnabled.Value || (uiDebug?.gameObject.activeSelf != true && uiSpawnItem?.gameObject.activeSelf != true))
                    return true;

                Global.code.uiCombat.HideHint();
                Global.code.onGUI = true;
                Cursor.SetCursor(RM.code.cursorNormal, Vector2.zero, CursorMode.Auto);
                Global.code.uiCombat.hud.SetActive(false);
                Time.timeScale = 1f;
                return false;
            }
        }

        [HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.Click))]
        public static class EquipmentSlot_Click_Patch
        {
            public static void Prefix(EquipmentSlot __instance, ref int __state)
            {
                if (!modEnabled.Value || !levelBypass.Value || !Global.code.selectedItem)
                    return;
                lastSelected = Global.code.selectedItem;
                __state = lastSelected.GetComponent<Item>().levelrequirement;
                lastSelected.GetComponent<Item>().levelrequirement = 0;
            }
            public static void Postfix(EquipmentSlot __instance, int __state)
            {
                if (!modEnabled.Value || !levelBypass.Value || lastSelected == null)
                    return;
                lastSelected.GetComponent<Item>().levelrequirement = __state;
                lastSelected = null;
            }
        }

        [HarmonyPatch(typeof(ThirdPersonCharacter), "CheckGroundStatus")]
        public static class ThirdPersonCharacter_CheckGroundStatus_Patch
        {
            public static bool Prefix(ThirdPersonCharacter __instance)
            {
                if (!modEnabled.Value || !flyMode.Value)
                    return true;
                __instance.m_IsGrounded = false;
                return false;
            }
        }

        [HarmonyPatch(typeof(ThirdPersonCharacter), "Move")]
        public static class ThirdPersonCharacter_Move_Patch
        {
            public static bool Prefix(ThirdPersonCharacter __instance)
            {
                return !modEnabled.Value || !flyMode.Value;
            }
        }
        
        [HarmonyPatch(typeof(ThirdPersonCharacter), "UpdateAnimator")]
        public static class ThirdPersonCharacter_UpdateAnimator_Patch
        {
            public static bool Prefix(ThirdPersonCharacter __instance)
            {
                return !modEnabled.Value || !flyMode.Value;
            }
        }

        [HarmonyPatch(typeof(ThirdPersonCharacter), "HandleGroundedMovement")]
        public static class ThirdPersonCharacter_HandleGroundedMovement_Patch
        {
            public static bool Prefix(ThirdPersonCharacter __instance)
            {
                return !modEnabled.Value || !flyMode.Value;
            }
        }
        
        [HarmonyPatch(typeof(ThirdPersonCharacter), "HandleAirborneMovement")]
        public static class ThirdPersonCharacter_HandleAirborneMovement_Patch
        {
            public static bool Prefix(ThirdPersonCharacter __instance)
            {
                return !modEnabled.Value || !flyMode.Value;
            }
        }

        [HarmonyPatch(typeof(ThirdPersonCharacter), "Snap")]
        public static class ThirdPersonCharacter_Snap_Patch
        {
            public static bool Prefix(ThirdPersonCharacter __instance)
            {
                return !modEnabled.Value || !flyMode.Value;
            }
        }
        [HarmonyPatch(typeof(Player), "Update")]
        public static class Player_Update_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(Player), "rigidbody") && codes[i + 1].opcode == OpCodes.Ldc_I4_1)
                    {
                        codes[i + 1].opcode = OpCodes.Ldc_I4_0;
                    }
                }
                return codes.AsEnumerable();
            }

            public static void Postfix(Player __instance, ThirdPersonCharacter ___m_Character, float ___h, float ___v)
            {
                if (!modEnabled.Value || __instance.customization.isDisplay)
                    return;
                
                __instance.rigidbody.useGravity = !flyMode.Value;
                __instance.GetComponent<CapsuleCollider>().enabled = !flyMode.Value;

                if (!Global.code.onGUI && spawnInput?.gameObject.activeSelf != true && AedenthornUtils.CheckKeyDown(flyToggleKey.Value))
                    flyMode.Value = !flyMode.Value;

                if (flyMode.Value)
                {
                    if (AedenthornUtils.CheckKeyDown(flyLockToggleKey.Value))
                        flyLocked.Value = !flyLocked.Value;

                    ___m_Character.m_IsGrounded = false;
                    __instance.rigidbody.isKinematic = false;
                    Quaternion q = __instance.m_Cam.transform.rotation;

                    Vector3 velocity = Vector3.zero;
                    if (Input.GetKey(KeyCode.Space))
                        velocity += Vector3.up * 0.1f;
                    if (Input.GetKey(KeyCode.LeftControl))
                        velocity -= Vector3.up * 0.1f;
                    if (PMC_Input.leftHold)
                        velocity -= q * Vector3.right * 0.1f;
                    if (PMC_Input.rightHold)
                        velocity += q * Vector3.right * 0.1f;
                    if (PMC_Input.backHold)
                        velocity -= q * Vector3.forward * 0.1f;
                    if (PMC_Input.forwardHold)
                        velocity += q * Vector3.forward * 0.1f;

                    if (AedenthornUtils.CheckKeyHeld(flyFastKey.Value))
                        velocity *= 10;

                    __instance.rigidbody.velocity *= (1 - flyModeDecelerationRate.Value);

                    __instance.rigidbody.velocity += velocity;

                    if(flyLocked.Value)
                        __instance.transform.forward = Vector3.LerpUnclamped(__instance.transform.forward, Vector3.Scale(__instance.m_Cam.forward, new Vector3(1f, 0f, 1f)).normalized, Time.deltaTime * 20f);


                }
                else if (___h == 0f && ___v == 0f && __instance.anim.velocity.magnitude < 0.1f)
                {
                    __instance.rigidbody.isKinematic = true;
                }
            }
        }

    }
}
