using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MegaMorph
{
    [BepInPlugin("bugerry.MegaMorph", "MegaMorph", "1.0.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public enum ValueType
		{
            SCALE,
            POS
		}

        public struct BoneOffset
        {
            public Transform bone;
            public Vector3 value;
            public ValueType type;
        }

        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> expertMode;
        public static ConfigEntry<int> nexusID;

        public ConfigEntry<float> tongue;
        public ConfigEntry<float> tights;
        public ConfigEntry<float> butt;
        public ConfigEntry<float> childish;
        public ConfigEntry<float> elvish;

        public List<ConfigDefinition> presets;
        public Dictionary<ConfigDefinition, BoneOffset> bones;

        private void Awake()
        {
            context = this;
            Config.SaveOnConfigSet = false;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
            expertMode = Config.Bind("General", "Expert Mode", false, "Allows individual modification of every bone by original name.");
            nexusID = Config.Bind("General", "NexusID", 50, "Nexus mod ID for updates");

            tongue = Config.Bind("01_Presets", "Tongue", 0f);
            tights = Config.Bind("01_Presets", "Tights", 0f);
            butt = Config.Bind("01_Presets", "Butt", 0f);
            childish = Config.Bind("01_Presets", "Childish", 0f);
            elvish = Config.Bind("01_Presets", "Elvish", 0f);
            
            Config.Bind("Tongue", "tongue01_scale", new Vector3(1f, 1f, 2f));

			Config.Bind("Elvish", "lEar_scale", new Vector3(5f, 1f, 2f));
            Config.Bind("Elvish", "rEar_scale", new Vector3(5f, 1f, 2f));

            Config.Bind("Butt", "pelvis_scale", new Vector3(1.5f, 1f, 1.5f));

            Config.Bind("Tights", "rThighBend_scale", new Vector3(1.5f, 1f, 1.5f));
            Config.Bind("Tights", "lThighBend_scale", new Vector3(1.5f, 1f, 1.5f));
            Config.Bind("Tights", "rThighTwist_scale", new Vector3(0.5f, 1f, 0.5f));
            Config.Bind("Tights", "lThighTwist_scale", new Vector3(0.5f, 1f, 0.5f));

            Config.Bind("Childish", "hip_scale", new Vector3(0.9f, 0.9f, 0.9f));
            Config.Bind("Childish", "neckUpper_scale", new Vector3(1.2f, 1.2f, 1.2f));
            Config.Bind("Childish", "lForearmTwist_scale", new Vector3(1f, 0.9f, 1f));
            Config.Bind("Childish", "rForearmTwist_scale", new Vector3(1f, 0.9f, 1f));
            Config.Bind("Childish", "rThighTwist_scale", new Vector3(1f, 0.9f, 1f));
            Config.Bind("Childish", "lThighTwist_scale", new Vector3(1f, 0.9f, 1f));

            bones = new Dictionary<ConfigDefinition, BoneOffset>();
            presets = new List<ConfigDefinition>();
            foreach (var config in Config.Keys)
			{
                if (config.Section == "01_Presets")
				{
                    presets.Add(config);
				}
			}

            context.Config.SettingChanged += context.OnSettingChanged;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        /*
        private void OnDisable()
		{
            try
            {
                Config.Save();
                Logger.Log(LogLevel.Message, "Config file stored.");
            }
            catch (IOException ex)
            {
                Logger.Log(LogLevel.Message | LogLevel.Warning, "WARNING: Failed to write to config directory, expect issues!\nError message:" + ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Log(LogLevel.Message | LogLevel.Warning, "WARNING: Permission denied to write to config directory, expect issues!\nError message:" + ex.Message);
            }
        }
        */

        public void ScanModel(CharacterCustomization cc)
        {
            if (cc == null) return;
            if (expertMode.Value)
            {
                foreach (var bone in cc.body.bones)
                {
                    var key = new ConfigDefinition("03_Expert Options", bone.name + "_scale");
                    if (!Config.ContainsKey(key))
                    {
                        Config.Bind(key, Vector3.one * 100f);
                    }
                    key = new ConfigDefinition("03_Expert Options", bone.name + "_pos");
                    if (!Config.ContainsKey(key))
                    {
                        Config.Bind(key, bone.localPosition * 100f);
                    }
                }
            }
        }

        public void ApplyConfig(CharacterCustomization cc, SettingChangedEventArgs args)
        {
            ConfigEntry <float> weight;
            ConfigEntry<Vector3> vec;
            BoneOffset boneOffset;
            if (cc == null) return;

            var key = new ConfigDefinition(cc.characterName, args.ChangedSetting.Definition.Key);
            if (bones.TryGetValue(key, out boneOffset))
			{
                boneOffset.value = (Vector3)args.ChangedSetting.BoxedValue;
                return;
			}
            
            foreach (var bone in cc.body.bones)
            {
                bone.localScale = Vector3.one;
                if (bone.name.StartsWith(args.ChangedSetting.Definition.Key))
                {
                    boneOffset = new BoneOffset
                    {
                        bone = bone,
                        value = (Vector3)args.ChangedSetting.BoxedValue,
                        type = bone.name.StartsWith("scale") ? ValueType.SCALE : ValueType.POS
                    };
                    return;
                }

                if (Config.TryGetEntry("03_Expert Options", bone.name + "_scale", out vec))
                {
                    bone.localScale = vec.Value / 100f;
                }

                if (bone.name == "hip_pos")
				{
                    //skip
				}
                else if (Config.TryGetEntry("03_Expert Options", bone.name + "_pos", out vec))
                {
                    bone.localPosition = vec.Value / 100f;
                }

                foreach (var preset in presets)
                {
                    if (Config.TryGetEntry(preset.Key, bone.name + "_scale", out vec) && Config.TryGetEntry(preset, out weight))
                    {
                        bone.localScale = Vector3.Scale(bone.localScale, Vector3.LerpUnclamped(Vector3.one, vec.Value, weight.Value / 100f));
                    }
                }
            }
        }

        public void ApplyPreset()
		{

		}

        public void OnSettingChanged(object source, SettingChangedEventArgs args)
        {
            if (!modEnabled.Value) return;

            if (args.ChangedSetting.Definition.Key == "Expert Mode")
			{
                if (Global.code.uiFreePose && Global.code.uiFreePose.selectedCharacter)
                {
                    ScanModel(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>());
                }
                else if (Global.code.uiCustomization && Global.code.uiCustomization.curCharacterCustomization)
                {
                    ScanModel(Global.code.uiCustomization.curCharacterCustomization);
                }
                else if (Player.code.customization)
                {
                    ScanModel(Player.code.customization);
                }
            }
            else if (Global.code.uiFreePose && Global.code.uiFreePose.selectedCharacter)
			{
                ApplyConfig(Global.code.uiFreePose.selectedCharacter.GetComponent<CharacterCustomization>(), args);
            }
            else if (Global.code.uiCustomization && Global.code.uiCustomization.curCharacterCustomization)
			{
                ApplyConfig(Global.code.uiCustomization.curCharacterCustomization, args);
            }
            else if (Player.code.customization)
			{
                ApplyConfig(Player.code.customization, args);
            }
        }

        public static class CharacterCustomization_LateUpdate_Patch
        {
            public static MethodBase TargetMethod()
            {
                return typeof(CharacterCustomization).GetMethod("LateUpdate");
            }

            public static void Postfix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value) return;
                
            }
        }
    }
}
