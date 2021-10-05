using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Assets.DuckType.Jiggle;

namespace BreastPhysics
{
	[BepInPlugin("bugerry.BreastPhysics", "Breast Physics", "1.0.1")]
	public partial class BepInExPlugin : BaseUnityPlugin
	{
		private static BepInExPlugin context;
		public static ConfigEntry<bool> modEnabled;
		public static ConfigEntry<bool> isDebug;
		public static ConfigEntry<int> nexusID;

		public readonly string[] names = new string [] {
			"Jelly", "Spring", "Hold", "Mass", "Angle", "Limit"
		};
		public readonly Dictionary<string, float> values = new Dictionary<string, float>();
		public readonly Dictionary<string, Slider> sliders = new Dictionary<string, Slider>();
		public Transform viewport = null;

		public static void ApplyValue(Jiggle jiggle, string key, float val)
		{
			switch (key)
			{
				case "Jelly": jiggle.Dampening = 1f - val; break;
				case "Spring": jiggle.SpringStrength = val; break;
				case "Hold": jiggle.SoftLimitInfluence = 1f - val; break;
				case "Mass": jiggle.SoftLimitStrength = 1f - val; break;
				case "Angle": jiggle.HingeAngle = 150f * val; break;
				case "Limit": jiggle.AngleLimit = 90f * val; break;
				default: break;
			}
		}

		public static float GetValue(Jiggle jiggle, string key)
		{
			switch (key)
			{
				case "Jelly": return 1f - jiggle.Dampening;
				case "Spring": return jiggle.SpringStrength;
				case "Hold": return 1f - jiggle.SoftLimitInfluence;
				case "Mass": return 1f - jiggle.SoftLimitStrength;
				case "Angle": return jiggle.HingeAngle / 150f;
				case "Limit": return jiggle.AngleLimit / 90f;
				default: return 0f;
			}
		}

		private void Awake()
		{
			context = this;
			modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
			isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
			nexusID = Config.Bind("General", "NexusID", 106, "Nexus mod ID for updates");
			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
		}

		public void ApplyValue(string key, float val)
		{
			var cc = Global.code.uiCustomization.curCharacterCustomization;
			if (!cc) return;

			foreach (var jiggle in cc.body.rootBone.GetComponentsInChildren<Jiggle>())
			{
				if (jiggle.name.EndsWith("Pectoral"))
				{
					ApplyValue(jiggle, key, val);
				}
			}
			values[string.Format("{0}/{1}", cc.name, key)] = val;
		}

		public void AddSlider(UICustomization __instance, Transform viewport, string key, float init)
		{
			var slider = __instance.panelSkin.GetComponentInChildren<Slider>();
			var name = key.Replace('_', ' ');
			slider = Instantiate(slider, viewport);
			slider.onValueChanged = new Slider.SliderEvent();
			slider.name = name;
			slider.minValue = 0f;
			slider.maxValue = 1f;
			slider.value = init;
			slider.onValueChanged.AddListener((float val) => ApplyValue(key, val));
			slider.transform.GetComponentInChildren<Text>().text = name;
			Destroy(slider.GetComponentInChildren<LocalizationText>());
			sliders[key] = slider;
		}

		[HarmonyPatch(typeof(UICustomization), "Start")]
		public static class UICustomization_Start_Patch
		{
			public static MethodBase TargetMethod()
			{
				return typeof(UICustomization).GetMethod("Start");
			}

			public static void Postfix(UICustomization __instance)
			{
				var cc = __instance.curCharacterCustomization;
				if (!modEnabled.Value || cc == null) return;

				context.viewport = __instance.panelBreasts.transform.GetChild(0).GetChild(0).GetChild(0);
				var title = Instantiate(context.viewport.GetChild(0), context.viewport);
				title.GetComponent<Text>().text = "Breast Physics";
				title.name = "Breast Physics";
				Destroy(title.GetComponent<LocalizationText>());

				foreach (var jiggle in cc.body.rootBone.GetComponentsInChildren<Jiggle>())
				{
					if (jiggle.name.EndsWith("Pectoral"))
					{
						context.AddSlider(__instance, context.viewport, "Jelly", 1f - jiggle.Dampening);
						context.AddSlider(__instance, context.viewport, "Spring", jiggle.SpringStrength);
						context.AddSlider(__instance, context.viewport, "Hold", 1f - jiggle.SoftLimitInfluence);
						context.AddSlider(__instance, context.viewport, "Mass", 1f - jiggle.SoftLimitStrength);
						context.AddSlider(__instance, context.viewport, "Angle", jiggle.HingeAngle / 150f);
						context.AddSlider(__instance, context.viewport, "Limit", jiggle.AngleLimit / 90f);
						break;
					}
				}
			}
		}

		[HarmonyPatch(typeof(UICustomization), "Open")]
		public static class UICustomization_Open_Patch
		{
			public static void Postfix(CharacterCustomization customization, bool isOpenChangeName = true)
			{
				if (!modEnabled.Value || customization == null) return;

				foreach (var jiggle in customization.body.rootBone.GetComponentsInChildren<Jiggle>())
				{
					if (jiggle.name.EndsWith("Pectoral"))
					{
						foreach (var slider in context.sliders)
						{
							slider.Value.value = GetValue(jiggle, slider.Key);
						}
						break;
					}
				}
			}
		}

		[HarmonyPatch(typeof(Mainframe), "SaveCharacterCustomization")]
		public static class Mainframe_SaveCharacterCustomization_Patch
		{
			public static MethodBase TargetMethod()
			{
				return typeof(Mainframe).GetMethod("SaveCharacterCustomization");
			}

			public static void Postfix(Mainframe __instance)
			{
				if (!modEnabled.Value) return;
				try
				{
					Directory.CreateDirectory(__instance.GetFolderName() + "BreastPhysics");
					foreach (var entry in context.values)
					{
						var key = entry.Key.Split('/');
						var id = string.Format("{0}BreastPhysics/{1}.txt?tag={2}", __instance.GetFolderName(), key[0], key[1]);
						ES2.Save(entry.Value, id);
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
			public static MethodBase TargetMethod()
			{
				return typeof(Mainframe).GetMethod("LoadCharacterCustomization");
			}

			public static void Postfix(Mainframe __instance, CharacterCustomization gen)
			{
				if (!modEnabled.Value) return;

				var item = string.Format("{0}BreastPhysics/{1}.txt", __instance.GetFolderName(), gen.name);
				if (ES2.Exists(item))
				{
					var data = ES2.LoadAll(string.Format("{0}BreastPhysics/{1}.txt", __instance.GetFolderName(), gen.name));
					foreach (var jiggle in gen.body.rootBone.GetComponentsInChildren<Jiggle>())
					{
						if (jiggle.name.EndsWith("Pectoral"))
						{
							foreach (var d in data.loadedData)
							{
								ApplyValue(jiggle, d.Key, (float)d.Value);
								context.values[string.Format("{0}/{1}", gen.name, d.Key)] = (float)d.Value;
							}
						}
					}
				}
				else
				{
					context.Logger.LogWarning("Missing: " + item);
				}
			}
		}

		[HarmonyPatch(typeof(Mainframe), "SaveCharacterPreset")]
		public static class Mainframe_SaveCharacterPreset_Patch
		{
			public static void Postfix(CharacterCustomization customization, string presetname, string creator, Texture2D profile)
			{
				if (!modEnabled.Value) return;
				try
				{
					foreach (var jiggle in customization.body.rootBone.GetComponentsInChildren<Jiggle>())
					{
						if (jiggle.name.EndsWith("Pectoral"))
						{
							foreach (var name in context.names)
							{
								var val = GetValue(jiggle, name);
								ES2.Save(val, string.Format("Character Presets/{0}/BreastPhysics.txt?tag={1}", presetname, name));
							}
							break;
						}
					}
				}
				catch (Exception e)
				{
					context.Logger.LogError(e);
				}
			}
		}

		[HarmonyPatch(typeof(Mainframe), "LoadCharacterPreset")]
		public static class Mainframe_LoadCharacterPreset_Patch
		{
			public static void Postfix(CharacterCustomization gen, string presetname)
			{
				if (!modEnabled.Value) return;
				var item = string.Format("Character Presets/{0}/BreastPhysics.txt", presetname);
				if (ES2.Exists(item))
				{
					var data = ES2.LoadAll(item);
					foreach (var jiggle in gen.body.rootBone.GetComponentsInChildren<Jiggle>())
					{
						if (jiggle.name.EndsWith("Pectoral"))
						{
							foreach (var d in data.loadedData)
							{
								ApplyValue(jiggle, d.Key, (float)d.Value);
								if (context.sliders.TryGetValue(d.Key, out Slider slider))
								{
									slider.value = (float)d.Value;
								}
							}
						}
					}
				}
				else
				{
					context.Logger.LogWarning("Missing: " + item);
				}
			}
		}

		[HarmonyPatch(typeof(Jiggle), "Start")]
		public static class Jiggle_Start_Patch
		{
			public static MethodBase TargetMethod()
			{
				return typeof(Jiggle).GetMethod("Start");
			}

			public static void Prefix(Jiggle __instance)
			{
				if (!modEnabled.Value) return;
				var cc = __instance.GetComponentInParent<CharacterCustomization>();
				if (cc)
				{
					if (__instance.name.EndsWith("Pectoral"))
					{
						foreach (var name in context.names)
						{
							var key = string.Format("{0}/{1}", cc.name, name);
							if (context.values.TryGetValue(key, out float val))
							{
								ApplyValue(__instance, name, val);
							}
							else
							{
								context.values[key] = GetValue(__instance, name);
							}
						}
					}
				}
			}
		}
	}
}
