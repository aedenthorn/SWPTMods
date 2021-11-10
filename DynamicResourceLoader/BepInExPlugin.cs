using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DynamicResourceLoader
{
    [BepInPlugin("aedenthorn.DynamicResourceLoader", "Dynamic Resource Loader", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                UnityEngine.Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {

            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            //nexusID = Config.Bind<int>("General", "NexusID", 114, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

		public static IEnumerator LoadResources()
        {
			Stopwatch timer = new Stopwatch(); // creating new instance of the stopwatch
			timer.Start();
			//RM.code.allItems.items = Resources.LoadAll("Items", typeof(Transform)).Cast<Transform>().ToList<Transform>();

			RM.code.allDropables.items = Resources.LoadAll("Items/Dropables", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allPotions.items = Resources.LoadAll("Items/Dropables/Potions", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allAmmos.items = Resources.LoadAll("Items/Dropables/Ammos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allMiscs.items = Resources.LoadAll("Items/Dropables/Misc", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.alllMinionScrolls.items = Resources.LoadAll("Items/Dropables/MinionScrolls", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allRings.items = Resources.LoadAll("Items/Dropables/Rings", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allArmors.items = Resources.LoadAll("Items/Dropables/Armors", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allNecklaces.items = Resources.LoadAll("Items/Dropables/Necklaces", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allWeapons.items = Resources.LoadAll("Items/Dropables/Weapons", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allTreasures.items = Resources.LoadAll("Items/Dropables/Treasures", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allBlackmarketItems.items = Resources.LoadAll("Items/RareItems/Blackmarket", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allCollections.items = Resources.LoadAll("Items/Collections", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allLingeries.items = Resources.LoadAll("Items/RareItems/Lingeries", typeof(Transform)).Cast<Transform>().ToList<Transform>();

			foreach (var a in new CommonArray[]{ RM.code.allDropables, RM.code.allPotions, RM.code.allAmmos, RM.code.allMiscs, RM.code.alllMinionScrolls, RM.code.allRings, RM.code.allArmors, RM.code.allNecklaces, RM.code.allWeapons, RM.code.allTreasures, RM.code.allBlackmarketItems, RM.code.allCollections, RM.code.allLingeries })
            {
				for (int k = 0; k < a.items.Count; k++)
				{
					a.items[k].gameObject.SetActive(true);
					Item component2 = a.items[k].GetComponent<Item>();
					component2.occupliedSlots.Clear();
					component2.damageMod = 0f;
					component2.defenceMod = 0f;
				}
			}

			RM.code.allTreasureChests.items = Resources.LoadAll("TreasureChests", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allCompanions.items = Resources.LoadAll("Companions", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allMinions.items = Resources.LoadAll("Minions", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allAchievements.items = Resources.LoadAll("Achievements", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allFreePoses.items = Resources.LoadAll("Poses", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allBuildings.items = Resources.LoadAll("Buildings", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allFieldArmies.items = Resources.LoadAll("Field Armies", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allAffixes.items = Resources.LoadAll("Affixes", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.weaponPrefixes.items = Resources.LoadAll("Affixes/Weapons/Prefixes", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.weaponSurfixes.items = Resources.LoadAll("Affixes/Weapons/Surfixes", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.armorPrefixes.items = Resources.LoadAll("Affixes/Armors/Prefixes", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.armorSurfixes.items = Resources.LoadAll("Affixes/Armors/Surfixes", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			foreach (Transform transform in RM.code.allAffixes.items)
			{
				if (transform)
				{
					RM.code.balancer.GetAffixStats(transform.GetComponent<Item>());
				}
			}
			foreach (Transform transform2 in RM.code.allLingeries.items)
			{
				if (transform2)
				{
					transform2.GetComponent<Item>().itemType = ItemType.lingerie;
					transform2.GetComponent<Item>().isnew = false;
					RM.code.balancer.GetItemStats(transform2, 0);
				}
			}
			foreach (Transform transform3 in RM.code.allAchievements.items)
			{
				if (transform3)
				{
					transform3.GetComponent<Achievement>().Initiate();
				}
			}
			for (int i = 0; i < RM.code.allCollections.items.Count; i++)
			{
				if (RM.code.allCollections.items[i])
				{
					RM.code.allCollections.items[i].GetComponent<Item>().amount = 0;
					RM.code.allCollections.items[i].GetComponent<Item>().itemType = ItemType.resource;
				}
			}
			foreach (Transform transform4 in RM.code.allTreasureChests.items)
			{
				if (transform4)
				{
					TreasureChest component = transform4.GetComponent<TreasureChest>();
					component.purchased = false;
					if (component.rewards.items.Count == 0)
					{
						for (int j = 0; j < transform4.childCount; j++)
						{
							component.rewards.AddItem(transform4.GetChild(j));
						}
					}
				}
			}
			foreach (Transform transform5 in RM.code.allMiscs.items)
			{
				if (transform5)
				{
					RM.code.balancer.GetItemStats(transform5, 0);
				}
			}
			foreach (Transform transform6 in RM.code.alllMinionScrolls.items)
			{
				if (transform6)
				{
					RM.code.balancer.GetItemStats(transform6, 0);
				}
			}
			RM.code.allPresets.items = Resources.LoadAll("Customization/Presets", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allHairs.items = Resources.LoadAll("Customization/Hairs", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allHairColors.items = Resources.LoadAll("Customization/HairColors", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allHeads.items = Resources.LoadAll("Customization/Heads", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allSkins.items = Resources.LoadAll("Customization/Skins", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allEyes.items = Resources.LoadAll("Customization/Eyes", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allLipsticks.items = Resources.LoadAll("Customization/Lipsticks", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allEyesMakeupShapes.items = Resources.LoadAll("Customization/EyesMakeupShapes", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allEyeShadowColors.items = Resources.LoadAll("Customization/EyeShadowColors", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allEyeLinerColors.items = Resources.LoadAll("Customization/EyeLinerColors", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allEyebrows.items = Resources.LoadAll("Customization/Eyebrows", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allBlushColors.items = Resources.LoadAll("Customization/BlushColors", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allHorns.items = Resources.LoadAll("Customization/Horns", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allWings.items = Resources.LoadAll("Customization/Wings", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allTails.items = Resources.LoadAll("Customization/Tail", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allNails.items = Resources.LoadAll("Customization/Nails", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allToeNails.items = Resources.LoadAll("Customization/Toenails", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allPubicHairs.items = Resources.LoadAll("Customization/PubicHairs", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allWombTattoos.items = Resources.LoadAll("Customization/WombTattoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allBodyTatoos.items = Resources.LoadAll("Customization/BodyTatoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allLegsTatoos.items = Resources.LoadAll("Customization/LegsTatoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allArmsTatoos.items = Resources.LoadAll("Customization/ArmsTatoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();
			RM.code.allFaceTatoos.items = Resources.LoadAll("Customization/FaceTatoos", typeof(Transform)).Cast<Transform>().ToList<Transform>();

			Dbgl($"timer: {timer.Elapsed.TotalSeconds}s");
			timer.Stop();

			yield break;
        }

        [HarmonyPatch(typeof(RM), "LoadResources")]
        static class RM_LoadResources_Patch
        {
            static bool Prefix(RM __instance)
            {
                if (!modEnabled.Value)
                    return true;

				//context.StartCoroutine(LoadResources());

				return false;
            }
        }
        [HarmonyPatch(typeof(CommonArray), nameof(CommonArray.GetItemWithName))]
        static class CommonArray_GetItemWithName_Patch
        {
            static bool Prefix(CommonArray __instance, ref Transform __result, string itemName)
            {
                if (!modEnabled.Value || nameof(__instance) != "allItems")
                    return true;
				foreach (var a in new CommonArray[] { RM.code.allItems, RM.code.allDropables, RM.code.allPotions, RM.code.allAmmos, RM.code.allMiscs, RM.code.alllMinionScrolls, RM.code.allRings, RM.code.allArmors, RM.code.allNecklaces, RM.code.allWeapons, RM.code.allTreasures, RM.code.allBlackmarketItems, RM.code.allCollections, RM.code.allLingeries })
				{
					if (a.GetItemWithName(itemName))
                    {
						__result = a.GetItemWithName(itemName);
						return false;
                    }
				}
				Dbgl($"Couldn't find item {itemName}");
				__result = null;
				return false;
            }
        }
    }
}
