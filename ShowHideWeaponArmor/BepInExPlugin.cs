using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShowHideWeaponArmor
{
    [BepInPlugin("aedenthorn.ShowHideWeaponArmor", "Show / Hide Weapons and Armor", "0.4.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<bool> hideWeaponsWithArmor;
        public static ConfigEntry<bool> hideWeaponsNotArmor;
        public static ConfigEntry<bool> forceShowArmor;
        public static ConfigEntry<bool> forceHideArmor;
        public static ConfigEntry<bool> hideArmorAtHome;
        public static ConfigEntry<bool> showArmorAtHome;
        public static ConfigEntry<bool> hideArmorAway;
        public static ConfigEntry<bool> showArmorAway;
        public static ConfigEntry<bool> hideArmorPosing;
        public static ConfigEntry<bool> showArmorPosing;
        public static ConfigEntry<bool> hideArmorInventory;
        public static ConfigEntry<bool> showArmorInventory;
        
        public static ConfigEntry<string> hotKey;


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
            nexusID = Config.Bind<int>("General", "NexusID", 39, "Nexus mod ID for updates");

            hideWeaponsWithArmor = Config.Bind<bool>("Options", "HideWeaponsWithArmor", true, "Hide weapons when armor is hidden.");
            hideWeaponsNotArmor = Config.Bind<bool>("Options", "HideWeaponsNotArmor", false, "Hide weapons instead when armor is set to be hidden (overrides HideWeaponsWithArmor).");
            forceShowArmor = Config.Bind<bool>("Options", "ForceShowArmor", false, "Override other settings, always show armor (overrides ForceHideArmor).");
            forceHideArmor = Config.Bind<bool>("Options", "ForceHideArmor", false, "Override other settings, always hide armor.");
            
            hideArmorAtHome = Config.Bind<bool>("Toggles", "AtHomeHideArmor", true, "Override vanilla settings, always hide armor at home.");
            showArmorAtHome = Config.Bind<bool>("Toggles", "AtHomeShowArmor", true, "Override vanilla settings, always show armor at home (overrides AtHomeHideArmor).");
            hideArmorAway = Config.Bind<bool>("Toggles", "AwayHideArmor", true, "Override vanilla settings, always hide armor away from home.");
            showArmorAway = Config.Bind<bool>("Toggles", "AwayShowArmor", true, "Override vanilla settings, always show armor away from home (overrides AwayHideArmor).");
            hideArmorPosing = Config.Bind<bool>("Toggles", "PosingHideArmor", true, "Override vanilla settings, always hide armor when posing.");
            showArmorPosing = Config.Bind<bool>("Toggles", "PosingShowArmor", true, "Override vanilla settings, always show armor when posing (overrides PosingHideArmor).");
            hideArmorInventory = Config.Bind<bool>("Toggles", "InventoryHideArmor", true, "Override vanilla settings, always hide armor when posing.");
            showArmorInventory = Config.Bind<bool>("Toggles", "InventoryShowArmor", true, "Override vanilla settings, always show armor when posing (overrides PosingHideArmor).");

            hotKey = Config.Bind<string>("Options", "HotKey", "u", "Hotkey to toggle armor (force hide => force show => don't force).");

            hideWeaponsWithArmor.SettingChanged += SettingChanged;
            hideWeaponsNotArmor.SettingChanged += SettingChanged;
            forceShowArmor.SettingChanged += SettingChanged;
            forceHideArmor.SettingChanged += SettingChanged;

            hideArmorAtHome.SettingChanged += SettingChanged;
            showArmorAtHome.SettingChanged += SettingChanged;
            hideArmorAway.SettingChanged += SettingChanged;
            showArmorAway.SettingChanged += SettingChanged;
            hideArmorPosing.SettingChanged += SettingChanged;
            showArmorPosing.SettingChanged += SettingChanged;
            hideArmorInventory.SettingChanged += SettingChanged;
            showArmorInventory.SettingChanged += SettingChanged;

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
        {
            RefreshAllCharacters();
        }

        private void SettingChanged(object sender, EventArgs e)
        {
            RefreshAllCharacters();
        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.RefreshClothesVisibility))]
        static class CharacterCustomization_RefreshClothesVisibility_Patch
        {
            static void Prefix(CharacterCustomization __instance, ref bool __state)
            {
                if (!modEnabled.Value || !Global.code)
                    return;

                //Dbgl($"Refresh clothes pose {Global.code.uiPose.gameObject.activeSelf} freepose {Global.code.uiFreePose.gameObject.activeSelf} inv {Global.code.uiInventory.gameObject.activeSelf} home {Global.code.curlocation.locationType == LocationType.home} ");

                __state = __instance.showArmor;
                if(Global.code.uiInventory.gameObject.activeSelf && Global.code.uiInventory.lingerieGroup.activeSelf)
                {
                    __instance.showArmor = false;

                }
                else if(Global.code.uiCustomization.gameObject.activeSelf || Global.code.uiMakeup.gameObject.activeSelf)
                {
                    __instance.showArmor = false;

                }
                else if (forceShowArmor.Value)
                {
                    __instance.showArmor = true;
                }
                else if (forceHideArmor.Value)
                {
                    __instance.showArmor = false;
                }
                else if ((Global.code.uiPose.gameObject.activeSelf || Global.code.uiFreePose.gameObject.activeSelf) && showArmorPosing.Value)
                {
                    __instance.showArmor = true;
                }
                else if ((Global.code.uiPose.gameObject.activeSelf || Global.code.uiFreePose.gameObject.activeSelf) && hideArmorPosing.Value)
                {
                    __instance.showArmor = false;
                }
                else if (Global.code.uiInventory.gameObject.activeSelf && showArmorInventory.Value)
                {
                    __instance.showArmor = true;
                }
                else if (Global.code.uiInventory.gameObject.activeSelf && hideArmorInventory.Value)
                {
                    __instance.showArmor = false;
                }
                else if (Global.code.uiCombatParty.gameObject.activeSelf && showArmorAway.Value)
                {
                    __instance.showArmor = true;
                }
                else if (Global.code.uiCombatParty.gameObject.activeSelf && hideArmorAway.Value)
                {
                    __instance.showArmor = false;
                }
                else if (Global.code.curlocation.locationType == LocationType.home && showArmorAtHome.Value)
                {
                    __instance.showArmor = true;
                }
                else if (Global.code.curlocation.locationType == LocationType.home && hideArmorAtHome.Value)
                {
                    __instance.showArmor = false;
                }
                else if (Global.code.curlocation.locationType != LocationType.home && showArmorAway.Value)
                {
                    __instance.showArmor = true;
                }
                else if (Global.code.curlocation.locationType != LocationType.home && hideArmorAway.Value)
                {
                    __instance.showArmor = false;
                }

                if (hideWeaponsNotArmor.Value)
                {
                    if (!__instance.showArmor)
                    {
                        if (!__instance.weaponInHand)
                        {
                            __instance.weapon?.gameObject.SetActive(false);
                            __instance.weapon2?.gameObject.SetActive(false);
                        }
                    }
                    if((!Global.code.uiInventory.gameObject.activeSelf || !Global.code.uiInventory.lingerieGroup.activeSelf) && !Global.code.uiCustomization.gameObject.activeSelf && !Global.code.uiMakeup.gameObject.activeSelf)
                        __instance.showArmor = true;
                }
                else if (hideWeaponsWithArmor.Value)
                {
                        if (!__instance.weaponInHand)
                        {
                            __instance.weapon?.gameObject.SetActive(false);
                            __instance.weapon2?.gameObject.SetActive(false);
                        }
                }
                else
                {
                    __instance.weapon?.gameObject.SetActive(true);
                    __instance.weapon2?.gameObject.SetActive(true);
                }

            }
            static void Postfix(CharacterCustomization __instance, ref bool __state)
            {
                if (!modEnabled.Value)
                    return;

                __instance.showArmor = __state;
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value || !AedenthornUtils.CheckKeyDown(hotKey.Value) || Global.code.uiNameChanger.gameObject.activeSelf)
                    return;

                Dbgl("Pressed hotkey");
                if (forceShowArmor.Value)
                {
                    Dbgl("Forcing hide");
                    forceShowArmor.Value = false;
                    forceHideArmor.Value = true;
                }
                else if (forceHideArmor.Value)
                {
                    Dbgl("not forcing hide or show");
                    forceShowArmor.Value = false;
                    forceHideArmor.Value = false;
                }
                else
                {
                    Dbgl("forcing show");
                    forceShowArmor.Value = true;
                    forceHideArmor.Value = false;
                }
                RefreshAllCharacters();
            }

        }
        
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.Holster))]
        static class CharacterCustomization_Holster_Patch
        {
            static void Postfix(CharacterCustomization __instance, Transform weapon)
            {
                if (!modEnabled.Value || !hideWeaponsWithArmor.Value)
                    return;
                __instance.RefreshClothesVisibility();
            }
        }
                
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.Draw))]
        static class CharacterCustomization_Draw_Patch
        {
            static void Postfix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || !hideWeaponsWithArmor.Value)
                    return;
                __instance.weaponInHand?.gameObject.SetActive(true);
            }
        }
        
        [HarmonyPatch(typeof(Scene), "SpawnPlayerSoldiers")]
        static class SpawnPlayerSoldiers_Patch
        {
            static void Postfix()
            {
                RefreshAllCharacters();
            }
        }
        
        [HarmonyPatch(typeof(UIInventory), nameof(UIInventory.Close))]
        static class UIInventory_Close_Patch
        {
            static void Postfix()
            {
                RefreshAllCharacters();
            }
        }
        
        [HarmonyPatch(typeof(Furniture), "DoInteract")]
        static class Furniture_DoInteract_Patch
        {
            static void Postfix(CharacterCustomization customization)
            {
                if (!modEnabled.Value)
                    return;
                customization.RefreshClothesVisibility();
            }
        }
        
        [HarmonyPatch(typeof(UIFreePose), nameof(UIFreePose.Open))]
        static class UIFreePose_Open_Patch
        {
            static void Postfix(UIFreePose __instance)
            {
                RefreshAllCharacters();
            }
        }
        
        [HarmonyPatch(typeof(UICombatParty), nameof(UICombatParty.Refresh))]
        static class UICombatParty_Refresh_Patch
        {
            static void Postfix(UICombatParty __instance)
            {
                RefreshAllCharacters();
            }
        }
        private static void RefreshAllCharacters()
        {
            if (!modEnabled.Value || !Player.code || !Global.code)
                return;


            Player.code.customization.RefreshClothesVisibility();
            for (int i = 0; i < Global.code.companions.items.Count; i++)
            {
                if (Global.code.companions.items[i].GetComponent<CharacterCustomization>().anim == null)
                    continue;
                Global.code.companions.items[i].GetComponent<CharacterCustomization>().RefreshClothesVisibility();
            }
        }
    }
}
