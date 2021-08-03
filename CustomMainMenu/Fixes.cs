using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CustomMainMenu
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        [HarmonyPatch(typeof(UILoading), "LoadScene")]
        static class LoadScene_Patch
        {
            static void Prefix()
            {
                if (!modEnabled.Value || SceneManager.GetActiveScene().name != "Desktop")
                    return;
                Dbgl("Resetting");
                if (mmCharacter)
                {
                    mmCharacter.gameObject.SetActive(false);
                    DestroyImmediate(mmCharacter.gameObject);
                }
                mmCharacter = null;
                if (Player.code)
                    DestroyImmediate(Player.code.gameObject);
                Player.code = null;
                if (Global.code)
                    DestroyImmediate(Global.code.gameObject);
                Global.code = null;
            }
        }
        //[HarmonyPatch(typeof(Merchant), nameof(Merchant.GetMatchLevelItems))]
        static class GetMatchLevelItems_Patch
        {

            static void Prefix(ref List<Transform> list)
            {
                if (!modEnabled.Value)
                    return;


                Dbgl($"list {list != null && list.Count > 0} player {Player.code != null} ID {Player.code?._ID != null} Global {Global.code != null}");
                foreach(Transform t in list)
                {
                    if (t == null)
                        Dbgl("null!");
                    else
                        Dbgl($"name {t.name}");

                }

            }
        }
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.IsWearingHighHeels))]
        static class IsWearingHighHeels_Patch
        {

            static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value || SceneManager.GetActiveScene().name != "Desktop")
                    return true;
                __result = false;
                return false;
            }
        }
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.UpdateStats))]
        static class UpdateStats_Patch
        {

            static bool Prefix()
            {
                if (!modEnabled.Value || SceneManager.GetActiveScene().name != "Desktop")
                    return true;
                return false;
            }
        }
        [HarmonyPatch(typeof(Mainframe), nameof(Mainframe.LoadStorage))]
        static class LoadStorage_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value || SceneManager.GetActiveScene().name != "Desktop")
                    return true;
                return false;
            }
        }
        [HarmonyPatch(typeof(XftWeapon.XWeaponTrail), "OnEnable")]
        static class XWeaponTrail_OnEnable_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value || SceneManager.GetActiveScene().name != "Desktop")
                    return true;
                return false;
            }
        }
        [HarmonyPatch(typeof(XftWeapon.XWeaponTrail), "OnDisable")]
        static class XWeaponTrail_OnDisable_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value || SceneManager.GetActiveScene().name != "Desktop")
                    return true;
                return false;
            }
        }
        [HarmonyPatch(typeof(ID), "OnDisable")]
        static class ID_OnDisable_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value || SceneManager.GetActiveScene().name != "Desktop")
                    return true;
                return false;
            }
        }
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.AddItem))]
        static class AddItem_Patch
        {
            static void Prefix(CharacterCustomization __instance, Transform item, string slotName)
            {
                if (!modEnabled.Value || SceneManager.GetActiveScene().name != "Desktop")
                    return;
                if (item.GetComponent<Item>().itemType == ItemType.lingerie)
                    item.GetComponent<Item>().itemType = ItemType.item;
                Dbgl($"additem {item.name} {slotName}");
            }
        }
    }
}
