using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CustomMainMenu
{
    [BepInPlugin("aedenthorn.CustomMainMenu", "Custom Main Menu", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<string> saveName;
        public static ConfigEntry<string> charName;
        public static ConfigEntry<bool> useSave;

        public static BepInExPlugin context;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            //nexusID = Config.Bind<int>("General", "NexusID", 38, "Nexus mod ID for updates");
            
            useSave = Config.Bind<bool>("Options", "UseSave", false, "Use character from specified save file. Otherwise use preset character.");
            saveName = Config.Bind<string>("Options", "SaveName", "", "Name of save to get character from.");
            charName = Config.Bind<string>("Options", "CharName", "Arisha", "Name of character to use.");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }
        [HarmonyPatch(typeof(Mainframe), "LoadCharacterCustomization")]
        static class Mainframe_LoadCharacterCustomization_Patch
        {
            static void Prefix(CharacterCustomization __instance, ref bool __state)
            {
                if (!modEnabled.Value)
                    return;
                __state = false;
                if (!Player.code)
                {
                    Dbgl("no player");
                    Player.code = new Player();
                    __state = true;
                }
            }
            static void Postfix(Mainframe __instance, CharacterCustomization gen)
            {
                if (!modEnabled.Value)
                    return;
            }
        }
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.RefreshClothesVisibility))]
        static class CharacterCustomization_RefreshClothesVisibility_Patch
        {
            static void Prefix(CharacterCustomization __instance, ref bool __state)
            {
                if (!modEnabled.Value)
                    return;
                __state = false;
                if (!Player.code)
                {
                    Dbgl("no player");
                    Player.code = new Player();
                    __state = true;
                }
            }
            static void Postfix(bool __state)
            {
                if (!modEnabled.Value)
                    return;
                if (__state)
                {
                    Player.code = null;
                }
            }
        }
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.IsWearingHighHeels))]
        static class IsWearingHighHeels_Patch
        {

            static bool Prefix(ref bool __result)
            {
                if (!modEnabled.Value || Global.code)
                    return true;
                __result = false;
                return false;
            }
        }
        [HarmonyPatch(typeof(Mainframe), nameof(Mainframe.LoadStorage))]
        static class LoadStorage_Patch
        {

            static bool Prefix()
            {
                if (!modEnabled.Value || Global.code)
                    return true;
                return false;
            }
        }
        [HarmonyPatch(typeof(XftWeapon.XWeaponTrail), "OnEnable")]
        static class XWeaponTrail_OnEnable_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value || Global.code)
                    return true;
                return false;
            }
        }
        [HarmonyPatch(typeof(XftWeapon.XWeaponTrail), "OnDisable")]
        static class XWeaponTrail_OnDisable_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value || Global.code)
                    return true;
                return false;
            }
        }
        [HarmonyPatch(typeof(ID), "OnDisable")]
        static class ID_OnDisable_Patch
        {
            static bool Prefix()
            {
                if (!modEnabled.Value || Global.code)
                    return true;
                return false;
            }
        }
        [HarmonyPatch(typeof(UIDesktop), "Awake")]
        static class UIDesktop_Awake_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                Global.code = new Global();
                Global.code.uiPose = new GameObject().AddComponent<UIPose>();
                Global.code.uiPose.gameObject.SetActive(false);

                Transform template;
                if (charName.Value == "Player")
                    template = RM.code.allCompanions.GetItemWithName("Kira");
                else
                    template = RM.code.allCompanions.GetItemWithName(charName.Value);

                if(template == null)
                {
                    Dbgl("Error getting customization");
                    return;
                }
                Transform character = Instantiate(template, GameObject.Find("Kira").transform.parent);
                character.name = template.name;
                character.localPosition = template.localPosition;
                character.GetComponent<CharacterCustomization>().isDisplay = true;
                Destroy(character.GetComponent<Interaction>());
                Destroy(GameObject.Find("Kira"));

                if (useSave.Value)
                {
                    Mainframe.code.foldername = saveName.Value;
                    AccessTools.Method(typeof(Mainframe), "LoadCharacterCustomization").Invoke(Mainframe.code, new object[] { character.GetComponent<CharacterCustomization>() });
                }
                else
                {

                }
                Destroy(character.GetComponent<CharacterCustomization>());
                Destroy(character.GetComponent<Companion>());
                Destroy(character.GetComponent<ID>());
                character.GetComponent<Rigidbody>().isKinematic = true;
                Destroy(character.GetComponent<CapsuleCollider>());
            }
        }
    }
}
