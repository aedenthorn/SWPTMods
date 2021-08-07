using BepInEx;
using HarmonyLib;
using System.Linq;
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

                itemNames = RM.code.allItems.items.Select(t => t.name).ToList();
                itemNames.Sort();
                
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
                if (Input.GetKeyDown(KeyCode.Tab) && spawnInput && spawnInput.GetComponent<InputField>().isFocused)
                {
                    Dbgl("Filling spawn text");
                    spawnInput.GetComponent<InputField>().text = spawnHintText.text;
                    spawnInput.GetComponent<InputField>().caretPosition = spawnInput.GetComponent<InputField>().text.Length;
                    return false;
                }
                return true;
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
        //[HarmonyPatch(typeof(Player), "Update")]
        public static class Player_Update_Patch
        {
            public static void Prefix(Player __instance, ThirdPersonCharacter ___m_Character)
            {
                if (!modEnabled.Value || __instance.customization.isDisplay)
                    return;

                if (Input.GetKey(KeyCode.Q))
                    __instance.transform.position += Vector3.up * 0.01f;
                else if (Input.GetKey(KeyCode.E))
                    __instance.transform.position -= Vector3.up * 0.01f;
                __instance.rigidbody.useGravity = !flyMode.Value;
            }
        }
    }
}
