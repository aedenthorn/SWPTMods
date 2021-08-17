using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace MegaMorph
{
    [BepInPlugin("bugerry.MegaMorph", "MegaMorph", "1.0.5")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        public struct Offset
		{
            public Transform bone;
            public Vector3 offset;
            public bool isScale;

            public void Apply()
			{
                if (isScale)
				{
                    bone.localScale = offset / 100f;
				}
                else if (bone.name == "hip")
                {
                    if (!Global.code.uiPose.isActiveAndEnabled && !Global.code.uiFreePose.isActiveAndEnabled)
                    {
                        bone.localPosition += offset / 100f;
                    }
                }
                else
				{
                    bone.localPosition = offset / 100f;
                }
			}
		}

        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public Dictionary<string, Offset> offsets;
        private bool isScanning = false;

        private void Awake()
        {
            context = this;
            Config.SaveOnConfigSet = false;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind("General", "NexusID", 50, "Nexus mod ID for updates");
            offsets = new Dictionary<string, Offset>();

            context.Config.SettingChanged += context.OnSettingChanged;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private void LateUpdate()
        {
            foreach (var offset in offsets)
			{
                offset.Value.Apply();
			}
        }

        public void ScanModel(CharacterCustomization cc)
        {
            if (cc == null) return;
            isScanning = true;
            foreach (var bone in cc.body.bones)
            {
                var key = new ConfigDefinition("Bones", bone.name + "_scale");
                if (!Config.ContainsKey(key))
                {
                    Config.Bind(key, Vector3.one * 100f);
                }
                key = new ConfigDefinition("Bones", bone.name + "_pos");
                if (!Config.ContainsKey(key))
                {
                    Config.Bind(key, bone.name == "hip" ? Vector3.zero : bone.localPosition * 100f);
                }
            }
            isScanning = false;
        }

        public void ApplyConfig(CharacterCustomization cc, SettingChangedEventArgs args)
        {
            if (cc == null) return;
            var key = string.Format("{0}/{1}", cc.name, args.ChangedSetting.Definition.Key);
            foreach (var bone in cc.body.bones)
			{
                if (args.ChangedSetting.Definition.Key.StartsWith(bone.name))
				{
                    offsets[key] = new Offset
                    {
                        bone = bone,
                        offset = (Vector3)args.ChangedSetting.BoxedValue,
                        isScale = args.ChangedSetting.Definition.Key.EndsWith("scale")
                    };
                    return;
				}
			}
        }

        public void OnSettingChanged(object source, SettingChangedEventArgs args)
        {
            if (!modEnabled.Value || isScanning) return;

            if (Global.code.uiFreePose && Global.code.uiFreePose.selectedCharacter)
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

        [HarmonyPatch(typeof(Player), "Awake")]
        public static class Player_Awake_Patch
        {
            public static MethodBase TargetMethod()
            {
                return typeof(Player).GetMethod("Awake");
            }

            public static void Postfix()
            {
                if (!modEnabled.Value) return;
                context.ScanModel(Player.code.customization);
            }
        }

        [HarmonyPatch(typeof(Mainframe), "SaveGame")]
        public static class Mainframe_SaveGame_Patch
        {
            public static void Postfix(Mainframe __instance)
            {
                try
                {
                    Directory.CreateDirectory(__instance.GetFolderName() + "MegaMorph");
                    foreach (var offset in context.offsets)
				    {
                        var key = offset.Key.Split('/');
                        ES2.Save(offset.Value.offset, string.Format("{0}MegaMorph/{1}.txt?tag={2}", __instance.GetFolderName(), key[0], key[1]));
                    }
                }
                catch (Exception e)
                {
                    context.Logger.LogError("OnSave: " + e.Message);
                }
            }
        }

        [HarmonyPatch(typeof(Mainframe), "LoadCharacterCustomization")]
        public static class Mainframe_LoadCharacterCustomization_Patch
        {
            static Texture texture = null;

            public static MethodBase TargetMethod()
            {
                return typeof(Mainframe).GetMethod("LoadCharacterCustomization");
            }

            public static void Postfix(Mainframe __instance, CharacterCustomization gen)
            {
                try
                {
                    var data = ES2.LoadAll(string.Format("{0}MegaMorph/{1}.txt", __instance.GetFolderName(), gen.name));
                    foreach (var d in data.loadedData)
                    {
                        var key = string.Format("{0}/{1}", gen.name, d.Key);
                        foreach (var bone in gen.body.bones)
                        {
                            if (d.Key.StartsWith(bone.name))
                            {
                                context.offsets[key] = new Offset
                                {
                                    bone = bone,
                                    offset = (Vector3)d.Value,
                                    isScale = d.Key.EndsWith("scale")
                                };
                                break;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    context.Logger.LogWarning("OnLoad: " + e.Message);
                }
            }
        }
    }
}
