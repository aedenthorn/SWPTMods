using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Jiggle
{
    [BepInPlugin("aedenthorn.Jiggle", "Jiggle", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> springStrengthMultArmored;
        public static ConfigEntry<float> centerOfMassInertiaMultArmored;
        public static ConfigEntry<bool> addNoiseArmored;
        public static ConfigEntry<bool> forceAddNoiseArmored;
        public static ConfigEntry<bool> forceDontAddNoiseArmored;
        public static ConfigEntry<float> noiseStrengthMultArmored;
        public static ConfigEntry<float> noiseSpeedMultArmored;
        public static ConfigEntry<float> noiseScaleMultArmored;
        public static ConfigEntry<float> dampeningMultArmored;
        public static ConfigEntry<bool> forceUseSoftLimitArmored;
        public static ConfigEntry<bool> forceDontUseSoftLimitArmored;
        public static ConfigEntry<float> softLimitInfluenceMultArmored;
        public static ConfigEntry<float> softLimitStrengthMultArmored;

        public static ConfigEntry<float> springStrengthMultUnarmored;
        public static ConfigEntry<float> centerOfMassInertiaMultUnarmored;
        public static ConfigEntry<bool> addNoiseUnarmored;
        public static ConfigEntry<bool> forceAddNoiseUnarmored;
        public static ConfigEntry<bool> forceDontAddNoiseUnarmored;
        public static ConfigEntry<float> noiseStrengthMultUnarmored;
        public static ConfigEntry<float> noiseSpeedMultUnarmored;
        public static ConfigEntry<float> noiseScaleMultUnarmored;
        public static ConfigEntry<float> dampeningMultUnarmored;
        public static ConfigEntry<bool> forceUseSoftLimitUnarmored;
        public static ConfigEntry<bool> forceDontUseSoftLimitUnarmored;
        public static ConfigEntry<float> softLimitInfluenceMultUnarmored;
        public static ConfigEntry<float> softLimitStrengthMultUnarmored;


        public static BepInExPlugin context;

        public static Dictionary<Assets.DuckType.Jiggle.Jiggle, JiggleData> jiggleDataDict = new Dictionary<Assets.DuckType.Jiggle.Jiggle, JiggleData>();

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
            nexusID = Config.Bind<int>("General", "NexusID", 55, "Nexus mod ID for updates");

            springStrengthMultArmored = Config.Bind<float>("WithArmor", "SpringStrengthMultArmored", 1.3f, "Spring strength multiplier while wearing armor.");
            centerOfMassInertiaMultArmored = Config.Bind<float>("WithArmor", "CenterOfMassInertiaMultArmored", 1f, "Center of mass inertia multiplier while wearing armor.");
            addNoiseArmored = Config.Bind<bool>("WithArmor", "AddNoiseArmored", true, "Add noise while wearing armor.");
            forceAddNoiseArmored = Config.Bind<bool>("WithArmor", "ForceAddNoiseArmored", true, "Force add noise while wearing armor.");
            forceDontAddNoiseArmored = Config.Bind<bool>("WithArmor", "ForceDontAddNoiseArmored", true, "Force don't add noise while wearing armor.");
            noiseStrengthMultArmored = Config.Bind<float>("WithArmor", "NoiseStrengthMultArmored", 1.0f, "Noise strength mult while wearing armor.");
            noiseSpeedMultArmored = Config.Bind<float>("WithArmor", "NoiseSpeedMultArmored", 1.0f, "Noise speed mult while wearing armor.");
            noiseScaleMultArmored = Config.Bind<float>("WithArmor", "NoiseScaleMultArmored", 1.0f, "Noise scale mult while wearing armor.");
            dampeningMultArmored = Config.Bind<float>("WithArmor", "DampeningMultArmored", 1.0f, "Dampening mult while wearing armor.");
            forceUseSoftLimitArmored = Config.Bind<bool>("WithArmor", "ForceUseSoftLimitArmored", true, "Use soft limit while wearing armor.");
            forceDontUseSoftLimitArmored = Config.Bind<bool>("WithArmor", "ForceDontUseSoftLimitArmored", true, "Use soft limit while wearing armor.");
            softLimitInfluenceMultArmored = Config.Bind<float>("WithArmor", "SoftLimitInfluenceMultArmored", 1.0f, "Soft limit influence mult while wearing armor.");
            softLimitStrengthMultArmored = Config.Bind<float>("WithArmor", "SoftLimitStrengthMultArmored", 1.0f, "Soft limit strength mult while wearing armor.");

            springStrengthMultUnarmored = Config.Bind<float>("WithoutArmor", "SpringStrengthMultUnarmored", 1f, "Spring strength multiplier while not wearing armor.");
            centerOfMassInertiaMultUnarmored = Config.Bind<float>("WithoutArmor", "CenterOfMassInertiaMultUnarmored", 1f, "Center of mass inertia multiplier while not wearing armor.");
            addNoiseUnarmored = Config.Bind<bool>("WithoutArmor", "AddNoiseUnarmored", true, "Add noise while not wearing armor.");
            forceAddNoiseUnarmored = Config.Bind<bool>("WithoutArmor", "forceAddNoiseUnarmored", true, "Force add noise while not wearing armor.");
            forceDontAddNoiseUnarmored = Config.Bind<bool>("WithoutArmor", "forceDontAddNoiseUnarmored", true, "Force don't add noise while not wearing armor.");
            noiseStrengthMultUnarmored = Config.Bind<float>("WithoutArmor", "NoiseStrengthMultUnarmored", 1.0f, "Noise strength mult while not wearing armor.");
            noiseSpeedMultUnarmored = Config.Bind<float>("WithoutArmor", "NoiseSpeedMultUnarmored", 1.0f, "Noise speed mult while not wearing armor.");
            noiseScaleMultUnarmored = Config.Bind<float>("WithoutArmor", "NoiseScaleMultUnarmored", 1.0f, "Noise scale mult while not wearing armor.");
            dampeningMultUnarmored = Config.Bind<float>("WithoutArmor", "DampeningMultUnarmored", 1.0f, "Dampening mult while not wearing armor.");
            forceUseSoftLimitUnarmored = Config.Bind<bool>("WithoutArmor", "ForceUseSoftLimitUnarmored", true, "Use soft limit while wearing not armor.");
            forceDontUseSoftLimitUnarmored = Config.Bind<bool>("WithoutArmor", "ForceDontUseSoftLimitUnarmored", true, "Use soft limit while wearing not armor.");
            softLimitInfluenceMultUnarmored = Config.Bind<float>("WithoutArmor", "SoftLimitInfluenceMultUnarmored", 1.0f, "Soft limit influence mult while not wearing armor.");
            softLimitStrengthMultUnarmored = Config.Bind<float>("WithoutArmor", "SoftLimitStrengthMultUnarmored", 1.0f, "Soft limit strength mult while not wearing armor.");

            springStrengthMultArmored.SettingChanged += SettingChanged;
            centerOfMassInertiaMultArmored.SettingChanged += SettingChanged;
            addNoiseArmored.SettingChanged += SettingChanged;
            noiseStrengthMultArmored.SettingChanged += SettingChanged;
            noiseSpeedMultArmored.SettingChanged += SettingChanged;
            noiseScaleMultArmored.SettingChanged += SettingChanged;
            dampeningMultArmored.SettingChanged += SettingChanged;
            forceUseSoftLimitArmored.SettingChanged += SettingChanged;
            forceDontUseSoftLimitArmored.SettingChanged += SettingChanged;
            softLimitInfluenceMultArmored.SettingChanged += SettingChanged;
            softLimitStrengthMultArmored.SettingChanged += SettingChanged;

            springStrengthMultUnarmored.SettingChanged += SettingChanged;
            centerOfMassInertiaMultUnarmored.SettingChanged += SettingChanged;
            addNoiseUnarmored.SettingChanged += SettingChanged;
            noiseStrengthMultUnarmored.SettingChanged += SettingChanged;
            noiseSpeedMultUnarmored.SettingChanged += SettingChanged;
            noiseScaleMultUnarmored.SettingChanged += SettingChanged;
            dampeningMultUnarmored.SettingChanged += SettingChanged;
            forceUseSoftLimitUnarmored.SettingChanged += SettingChanged;
            forceDontUseSoftLimitUnarmored.SettingChanged += SettingChanged;
            softLimitInfluenceMultUnarmored.SettingChanged += SettingChanged;
            softLimitStrengthMultUnarmored.SettingChanged += SettingChanged;


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void SettingChanged(object sender, EventArgs e)
        {
            if (!modEnabled.Value || !Player.code)
                return;

            foreach(Assets.DuckType.Jiggle.Jiggle jiggle in Player.code.GetComponentsInChildren<Assets.DuckType.Jiggle.Jiggle>())
            {
                SetVariables(jiggle, Player.code.GetComponent<CharacterCustomization>().showArmor);
            }
            foreach(Transform c in Global.code.companions.items)
            {
                foreach (Assets.DuckType.Jiggle.Jiggle jiggle in c.GetComponentsInChildren<Assets.DuckType.Jiggle.Jiggle>())
                {
                    SetVariables(jiggle, c.GetComponent<CharacterCustomization>().showArmor);
                }
            }
        }
        private static void SetVariables(Assets.DuckType.Jiggle.Jiggle jiggle, bool armored)
        {
            if (armored)
            {
                jiggle.SpringStrength = jiggleDataDict[jiggle].SpringStrength * springStrengthMultArmored.Value;
                jiggle.Dampening = jiggleDataDict[jiggle].Dampening * dampeningMultArmored.Value;
                jiggle.NoiseStrength = jiggleDataDict[jiggle].NoiseStrength * noiseStrengthMultArmored.Value;
                jiggle.NoiseSpeed = jiggleDataDict[jiggle].NoiseSpeed * noiseSpeedMultArmored.Value;
                jiggle.NoiseScale = jiggleDataDict[jiggle].NoiseScale * noiseScaleMultArmored.Value;
                jiggle.SoftLimitInfluence = jiggleDataDict[jiggle].SoftLimitInfluence * softLimitInfluenceMultArmored.Value;
                jiggle.SoftLimitStrength = jiggleDataDict[jiggle].SoftLimitStrength * softLimitStrengthMultArmored.Value;

                if (forceAddNoiseArmored.Value)
                    jiggle.AddNoise = true;
                if (forceDontAddNoiseArmored.Value)
                    jiggle.AddNoise = false;
                if (forceUseSoftLimitArmored.Value)
                    jiggle.UseSoftLimit = true;
                if (forceDontUseSoftLimitArmored.Value)
                    jiggle.UseSoftLimit = false;
            }
            else
            {
                jiggle.SpringStrength = jiggleDataDict[jiggle].SpringStrength * springStrengthMultUnarmored.Value;
                jiggle.Dampening = jiggleDataDict[jiggle].Dampening * dampeningMultUnarmored.Value;
                jiggle.NoiseStrength = jiggleDataDict[jiggle].NoiseStrength * noiseStrengthMultUnarmored.Value;
                jiggle.NoiseSpeed = jiggleDataDict[jiggle].NoiseSpeed * noiseSpeedMultUnarmored.Value;
                jiggle.NoiseScale = jiggleDataDict[jiggle].NoiseScale * noiseScaleMultUnarmored.Value;
                jiggle.SoftLimitInfluence = jiggleDataDict[jiggle].SoftLimitInfluence * softLimitInfluenceMultUnarmored.Value;
                jiggle.SoftLimitStrength = jiggleDataDict[jiggle].SoftLimitStrength * softLimitStrengthMultUnarmored.Value;

                if (forceAddNoiseUnarmored.Value)
                    jiggle.AddNoise = true;
                if (forceDontAddNoiseUnarmored.Value)
                    jiggle.AddNoise = false;
                if (forceUseSoftLimitUnarmored.Value)
                    jiggle.UseSoftLimit = true;
                if (forceDontUseSoftLimitUnarmored.Value)
                    jiggle.UseSoftLimit = false;
            }
        }
        [HarmonyPatch(typeof(Assets.DuckType.Jiggle.Jiggle), "Start")]
        public static class Jiggle_Start_Patch
        {
            public static void Postfix(Assets.DuckType.Jiggle.Jiggle __instance)
            {
                if (!modEnabled.Value)
                    return;

                jiggleDataDict.Add(__instance, new JiggleData(__instance));

                bool armored = false;
                Transform t = __instance.transform;
                while (t.parent)
                {
                    if (t.parent.GetComponent<CharacterCustomization>())
                    {
                        armored = t.parent.GetComponent<CharacterCustomization>().armor;
                        break;
                    }
                    t = t.parent;
                }
                SetVariables(__instance, armored);
            }
        }

    }
}
