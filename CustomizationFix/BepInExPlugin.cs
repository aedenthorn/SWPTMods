using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace CustomizationFix
{
    [BepInPlugin("aedenthorn.CustomizationFix", "Customization Fix", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static BepInExPlugin context;

        private static Dictionary<string, float> nippleDepth = new Dictionary<string, float>();
        private static Dictionary<string, float> nippleLarge = new Dictionary<string, float>();

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
            }
            static void Postfix(Mainframe __instance, CharacterCustomization gen)
            {
                if (!modEnabled.Value)
                    return;
                nippleDepth[gen.characterName] = Player.code?.nippleDepth ?? 0;
                nippleLarge[gen.characterName] = Player.code?.nippleLarge ?? 0;
            }
        }
        [HarmonyPatch(typeof(Mainframe), "LoadCharacterPreset")]
        static class Mainframe_LoadCharacterPreset_Patch
        {

            static void Postfix(Mainframe __instance, CharacterCustomization gen)
            {
                if (!modEnabled.Value)
                    return;
                nippleDepth[gen.characterName] = Player.code.nippleDepth;
                nippleLarge[gen.characterName] = Player.code.nippleLarge;
            }
        }
        [HarmonyPatch(typeof(Mainframe), "SaveCharacterCustomization")]
        static class Mainframe_SaveCharacterCustomization_Patch
        {
            static void Postfix(Mainframe __instance, CharacterCustomization customization)
            {
                if (!modEnabled.Value)
                    return;
                ES2.Save<float>(customization.body.GetBlendShapeWeight(customization.body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesLarge")), __instance.GetFolderName() + customization.name + ".txt?tag=nippleLarge");
                ES2.Save<float>(customization.body.GetBlendShapeWeight(customization.body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesDepth")), __instance.GetFolderName() + customization.name + ".txt?tag=nippleDepth");
            }
        }
        [HarmonyPatch(typeof(Mainframe), "SaveCharacterPreset")]
        static class Mainframe_SaveCharacterPreset_Patch
        {
            static void Postfix(CharacterCustomization customization, string presetname)
            {
                if (!modEnabled.Value)
                    return;
                ES2.Save<float>(customization.body.GetBlendShapeWeight(customization.body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesLarge")), "Character Presets/" + presetname + "/CharacterPreset.txt?tag=nippleLarge");
                ES2.Save<float>(customization.body.GetBlendShapeWeight(customization.body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesDepth")), "Character Presets/" + presetname + "/CharacterPreset.txt?tag=nippleDepth");
            }
        }
        
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.RefreshClothesVisibility))]
        static class CharacterCustomization_RefreshClothesVisibility_Patch
        {
            static void Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || __instance.characterName == null)
                    return;
                if (!nippleDepth.ContainsKey(__instance.characterName))
                {
                    nippleLarge[__instance.characterName] = __instance.body.GetBlendShapeWeight(Player.code.nipplesLargeIndex);
                    nippleDepth[__instance.characterName] = __instance.body.GetBlendShapeWeight(Player.code.nipplesLargeIndex);
                }
            }
            static void Postfix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || __instance.characterName == null)
                    return;
                //Dbgl($"Player n: {Player.code.nipplesLargeIndex},{Player.code.nipplesDepthIndex}");

                if ((!__instance.armor || !__instance.armor.gameObject.activeSelf) && (!__instance.bra || !__instance.bra.gameObject.activeSelf))
                {
                    __instance.body.SetBlendShapeWeight(__instance.body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesLarge"), nippleLarge[__instance.characterName]);
                    __instance.body.SetBlendShapeWeight(__instance.body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesDepth"), nippleDepth[__instance.characterName]);
                }
                //__instance.wombTattoo = RM.code.allWombTattoos.items[0];
            }
        }
        [HarmonyPatch(typeof(CustomizationSlider), nameof(CustomizationSlider.ValueChange))]
        static class CustomizationSlider_ValueChange_Patch
        {
            static void Postfix(CustomizationSlider __instance, float val)
            {
                if (!modEnabled.Value || __instance.isEmotionController || __instance.presetIndexes.Length > 0 || !Global.code.uiCustomization.gameObject.activeSelf)
                    return;

                if (__instance.index == Global.code.uiCustomization.curCharacterCustomization.body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesLarge"))
                {
                    nippleLarge[Global.code.uiCustomization.curCharacterCustomization.characterName] = val;
                }
                else if (__instance.index == Global.code.uiCustomization.curCharacterCustomization.body.sharedMesh.GetBlendShapeIndex("Genesis8Female__PBMNipplesDepth"))
                {
                    nippleDepth[Global.code.uiCustomization.curCharacterCustomization.characterName] = val;
                }
            }
        }
        [HarmonyPatch(typeof(UICustomization), "ResetAllPanels")]
        static class CustomizationSlider_ResetAllPanels_Patch
        {
            static void Prefix(UICustomization __instance)
            {
                if (!modEnabled.Value)
                    return;
                __instance.SyncSliders();
            }
        }
    }
}
