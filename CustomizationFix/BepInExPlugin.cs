using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CustomizationFix
{
    [BepInPlugin("bugerry.CustomizationFix", "CustomizationFix", "1.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        private void Awake()
        {
            context = this;
            Config.SaveOnConfigSet = false;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind("General", "NexusID", 60, "Nexus mod ID for updates");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(CharacterCustomization), "Start")]
        public static class CharacterCustomization_Start_Patch
        {
            public static MethodBase TargetMethod()
            {
                return typeof(CharacterCustomization).GetMethod("Start");
            }

            public static void Postfix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value) return;
                for (var i = 0; i < __instance.body.sharedMesh.blendShapeCount; ++i)
				{
                    var name = __instance.body.sharedMesh.GetBlendShapeName(i);
                    var key = new ConfigDefinition(__instance.characterName, i.ToString());
                    if (!context.Config.ContainsKey(key))
                    {
                        context.Config.Bind(__instance.name, i.ToString(), __instance.body.GetBlendShapeWeight(i), name);
                    }
                }
                __instance.RefreshClothesVisibility();
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), "RefreshClothesVisibility")]
        public static class CharacterCustomization_RefreshClothesVisibility_Patch
        {
            public static void Postfix(CharacterCustomization __instance)
			{
                ConfigEntry<float> nippleSize;
                ConfigEntry<float> nippleDepth;
                if (!modEnabled.Value) return;
                if (__instance.armor && __instance.armor.gameObject.activeSelf) return;
                if (__instance.bra && __instance.bra.gameObject.activeSelf) return;
                if (context.Config.TryGetEntry(__instance.name, Player.code.nipplesLargeIndex.ToString(), out nippleSize))
                {
                    __instance.body.SetBlendShapeWeight(Player.code.nipplesLargeIndex, nippleSize.Value);
                }
                if (context.Config.TryGetEntry(__instance.name, Player.code.nipplesDepthIndex.ToString(), out nippleDepth))
                {
                    __instance.body.SetBlendShapeWeight(Player.code.nipplesDepthIndex, nippleDepth.Value);
                }
            }
		}

        [HarmonyPatch(typeof(Appeal), "SyncBlendshape")]
        public static class Appeal_SyncBlendshape_Patch
        {
            public static void Postfix(Appeal __instance)
            {
                if (!modEnabled.Value) return;

                var nippleSizeValue = 100f;
                var nippleDepthValue = 100f;
                var cc = Global.code.uiCustomization.curCharacterCustomization;
                if (cc)
                {
                    ConfigEntry<float> nippleSize;
                    ConfigEntry<float> nippleDepth;
                    if (context.Config.TryGetEntry(cc.name, Player.code.nipplesLargeIndex.ToString(), out nippleSize))
                    {
                        nippleSizeValue = nippleSize.Value;
                    }
                    if (context.Config.TryGetEntry(cc.name, Player.code.nipplesDepthIndex.ToString(), out nippleDepth))
                    {
                        nippleDepthValue = nippleDepth.Value;
                    }
                }

                foreach (SkinnedMeshRenderer skinnedMeshRenderer in __instance.allRenderers)
                {
                    skinnedMeshRenderer.SetBlendShapeWeight(Player.code.nipplesLargeIndex, nippleSizeValue);
                    skinnedMeshRenderer.SetBlendShapeWeight(Player.code.nipplesDepthIndex, nippleDepthValue);
                }
            }
        }

        [HarmonyPatch(typeof(CustomizationSlider), "Start")]
        public static class CustomizationSlider_Start_Patch
        {
            public static MethodBase TargetMethod()
            {
                return typeof(CustomizationSlider).GetMethod("Start");
            }

            public static void Postfix(CustomizationSlider __instance)
            {
                if (!modEnabled.Value) return;
                var cc = Global.code.uiCustomization.curCharacterCustomization;
                if (cc)
				{
                    __instance.GetComponent<Slider>().value = cc.body.GetBlendShapeWeight(__instance.index);
                }
            }
        }

        [HarmonyPatch(typeof(CustomizationSlider), "ValueChange")]
        public static class CustomizationSlider_ValueChange_Patch
        {
            public static void Postfix(float val, CustomizationSlider __instance)
            {
                if (!modEnabled.Value) return;
                var cc = Global.code.uiCustomization.curCharacterCustomization;
                if (cc)
                {
                    ConfigEntry<float> configEntry;
                    if (context.Config.TryGetEntry(cc.name, __instance.index.ToString(), out configEntry))
                    {
                        configEntry.Value = val;
                    }
                    else
                    {
                        context.Config.Bind(cc.name, __instance.index.ToString(), val, __instance.blendshapename);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CustomizationSlider), "Refresh")]
        public static class CustomizationSlider_Refresh_Patch
        {
            public static void Postfix(CustomizationSlider __instance)
            {
                if (!modEnabled.Value) return;
                var cc = Global.code.uiCustomization.curCharacterCustomization;
                if (cc)
                {
                    __instance.GetComponent<Slider>().value = cc.body.GetBlendShapeWeight(__instance.index);
                }
            }
        }

        [HarmonyPatch(typeof(UICustomization), "ResetAllPanels")]
        public static class UICustomization_ResetAllPanels_Patch
		{
            public static MethodBase TargetMethod()
            {
                return typeof(UICustomization).GetMethod("ResetAllPanels");
            }

            public static void Prefix(UICustomization __instance)
			{
                if (!modEnabled.Value) return;
                __instance.panelPresets.SetActive(true);
                __instance.panelFace.SetActive(true);
                __instance.panelEyes.SetActive(true);
                __instance.panelNose.SetActive(true);
                __instance.panelMouth.SetActive(true);
                __instance.panelSkin.SetActive(true);
                __instance.panelBreasts.SetActive(true);
                __instance.panelBody.SetActive(true);
                __instance.panelWings.SetActive(true);
                __instance.panelHorns.SetActive(true);
                __instance.SyncSliders();
            }
        }

        [HarmonyPatch(typeof(UICustomization), "Close")]
        public static class UICustomization_Close_Patch
        {
            public static void Postfix()
            {
                if (!modEnabled.Value) return;
                try
                {
                    context.Config.Save();
                    context.Logger.Log(LogLevel.Message, "Config file stored.");
                }
                catch (IOException ex)
                {
                    context.Logger.Log(LogLevel.Message | LogLevel.Warning, "WARNING: Failed to write to config directory, expect issues!\nError message:" + ex.Message);
                }
                catch (UnauthorizedAccessException ex)
                {
                    context.Logger.Log(LogLevel.Message | LogLevel.Warning, "WARNING: Permission denied to write to config directory, expect issues!\nError message:" + ex.Message);
                }
            }
        }

        [HarmonyPatch(typeof(LoadPresetIcon), "ButtonLoad")]
        public static class LoadPresetIcon_ButtonLoad_Patch
		{
            public static bool Prefix(LoadPresetIcon __instance)
            {
                if (!modEnabled.Value) return true;
                var cc = Global.code.uiCustomization.curCharacterCustomization;
                Mainframe.code.LoadCharacterPreset(cc, __instance.foldername);
                Global.code.uiCustomization.panelLoadPreset.SetActive(false);
                Global.code.uiCombat.ShowHeader("Character Loaded");
                return false;
            }
        }
        
    }
}
