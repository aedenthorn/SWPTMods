using System;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CustomizationFix
{
	[BepInPlugin("bugerry.CustomizationFix", "CustomizationFix", "1.2.0")]
	public partial class BepInExPlugin : BaseUnityPlugin
	{
		private static BepInExPlugin context;
		public static ConfigEntry<bool> modEnabled;
		public static ConfigEntry<bool> isDebug;
		public static ConfigEntry<int> nexusID;

		public readonly Dictionary<string, float> stats = new Dictionary<string, float>();

		private void Awake()
		{
			context = this;
			Config.SaveOnConfigSet = false;
			modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
			isDebug = Config.Bind("General", "IsDebug", true, "Enable debug logs");
			nexusID = Config.Bind("General", "NexusID", 60, "Nexus mod ID for updates");
			Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
		}

		[HarmonyPatch(typeof(CharacterCustomization), "RefreshClothesVisibility")]
		public static class CharacterCustomization_RefreshClothesVisibility_Patch
		{
			public static void Postfix(CharacterCustomization __instance)
			{
				if (!modEnabled.Value) return;
				if (__instance.armor && __instance.armor.gameObject.activeSelf) return;
				if (__instance.bra && __instance.bra.gameObject.activeSelf) return;

				var name = __instance.body.sharedMesh.GetBlendShapeName(Player.code.nipplesLargeIndex);
				var key = string.Format("{0}/{1}", __instance.name, name);
				if (context.stats.TryGetValue(key, out float nippleSize))
				{
					__instance.body.SetBlendShapeWeight(Player.code.nipplesLargeIndex, nippleSize);
				}

				name = __instance.body.sharedMesh.GetBlendShapeName(Player.code.nipplesDepthIndex);
				key = string.Format("{0}/{1}", __instance.name, name);
				if (context.stats.TryGetValue(key, out float nippleDepth))
				{
					__instance.body.SetBlendShapeWeight(Player.code.nipplesDepthIndex, nippleDepth);
				}
			}
		}

		[HarmonyPatch(typeof(Appeal), "SyncBlendshape")]
		public static class Appeal_SyncBlendshape_Patch
		{
			public static void Postfix(Appeal __instance)
			{
				if (!modEnabled.Value) return;

				var nippleSize = 100f;
				var nippleDepth = 100f;
				var cc = Global.code.uiCustomization.curCharacterCustomization;
				if (cc)
				{
					var name = cc.body.sharedMesh.GetBlendShapeName(Player.code.nipplesLargeIndex);
					context.stats.TryGetValue(string.Format("{0}/{1}", cc.name, name), out nippleSize);
					name = cc.body.sharedMesh.GetBlendShapeName(Player.code.nipplesLargeIndex);
					context.stats.TryGetValue(string.Format("{0}/{1}", cc.name, name), out nippleDepth);
				}

				foreach (SkinnedMeshRenderer skinnedMeshRenderer in __instance.allRenderers)
				{
					skinnedMeshRenderer.SetBlendShapeWeight(Player.code.nipplesLargeIndex, nippleSize);
					skinnedMeshRenderer.SetBlendShapeWeight(Player.code.nipplesDepthIndex, nippleDepth);
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

			public static bool Prefix(CustomizationSlider __instance)
			{
				if (modEnabled.Value)
				{
					__instance.Refresh();
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(CustomizationSlider), "Refresh")]
		public static class CustomizationSlider_Refresh_Patch
		{
			public static bool Prefix(CustomizationSlider __instance)
			{
				if (!modEnabled.Value) return true;
				var cc = Global.code.uiCustomization.curCharacterCustomization;
				if (cc && __instance.blendshapename.Length > 0)
				{
					var name = string.Format("{0}/{1}", cc.name, __instance.blendshapename);
					if (context.stats.TryGetValue(name, out float val))
					{
						__instance.GetComponent<Slider>().value = val;
					}
					else
					{
						val = cc.body.GetBlendShapeWeight(__instance.index);
						context.stats[name] = val;
						__instance.GetComponent<Slider>().value = val;
					}
				}
				return false;
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
					var name = cc.body.sharedMesh.GetBlendShapeName(__instance.index);
					name = string.Format("{0}/{1}", cc.name, name);
					context.stats[name] = val;
				}
			}
		}

		[HarmonyPatch(typeof(UICustomization), "Open")]
		public static class UICustomization_Open_Patch
		{
			public static void Postfix(UICustomization __instance, CharacterCustomization customization, bool isOpenChangeName = true)
			{
				if (!modEnabled.Value) return;
				var slider = __instance.panelSkin.GetComponentInChildren<Slider>();
				if (slider && customization)
				{
					slider.minValue = -1f;
					slider.maxValue = -0.25f;
					slider.value = customization.skinGlossiness;
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

				try
				{
					var key = "";
					var shapes = ES2.LoadList<float>(__instance.GetFolderName() + gen.name + ".txt?tag=blendshapes");
					for (var i = 0; i < shapes.Count; ++i)
					{
						key = gen.body.sharedMesh.GetBlendShapeName(i);
						key = string.Format("{0}/{1}", gen.name, key);
						if (i == Player.code.nipplesLargeIndex)
						{
							context.stats[key] = ES2.Load<float>(__instance.GetFolderName() + gen.name + ".txt?tag=nippleLarge");
						}
						else if (i == Player.code.nipplesDepthIndex)
						{
							context.stats[key] = ES2.Load<float>(__instance.GetFolderName() + gen.name + ".txt?tag=nippleDepth");
						}
						else
						{
							context.stats[key] = shapes[i];
						}
					}
				}
				catch (Exception e)
				{
					context.Logger.LogError(e);
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

			public static void Postfix(Mainframe __instance, CharacterCustomization customization)
			{
				if (!modEnabled.Value) return;

				try
				{
					var name = customization.body.sharedMesh.GetBlendShapeName(Player.code.nipplesLargeIndex);
					var key = string.Format("{0}/{1}", customization.name, name);
					if (context.stats.TryGetValue(key, out float nippleLarge))
					{
						ES2.Save(nippleLarge, __instance.GetFolderName() + customization.name + ".txt?tag=nippleLarge");
					}

					name = customization.body.sharedMesh.GetBlendShapeName(Player.code.nipplesDepthIndex);
					key = string.Format("{0}/{1}", customization.name, name);
					if (context.stats.TryGetValue(key, out float nippleDepth))
					{
						ES2.Save(nippleDepth, __instance.GetFolderName() + customization.name + ".txt?tag=nippleDepth");
					}
				}
				catch (Exception e)
				{
					context.Logger.LogError(e);
				}
			}
		}
	}
}
