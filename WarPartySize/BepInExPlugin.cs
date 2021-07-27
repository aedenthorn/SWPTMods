using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WarPartySize
{
    [BepInPlugin("aedenthorn.WarPartySize", "War Party Size", "0.2.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> partySize;
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

            partySize = Config.Bind<int>("Options", "PartySize", 5, "Maximum number of companions in party.");

            nexusID = Config.Bind<int>("General", "NexusID", 3, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(UICombatParty), nameof(UICombatParty.Refresh))]
        static class Refresh_Patch
        {
            static void Prefix(UICombatParty __instance)
            {
                if (!modEnabled.Value)
                    return;

                foreach(Transform t in __instance.locators)
                {
                    Dbgl($"{t.position}, {t.rotation}");
                }

                List<Transform> locators = new List<Transform>(__instance.locators);
                while(locators.Count <= Global.code.playerCombatParty.items.Count)
                {
                    Transform t = Instantiate(locators[locators.Count - 1], locators[locators.Count - 1].parent);
                    locators.Add(t);
                }
                __instance.locators = locators.ToArray();
            }
            static void Postfix(UICombatParty __instance)
            {
                if (!modEnabled.Value)
                    return;

                for (int k = 3; k < partySize.Value; k++)
                {
                    Transform transform = Instantiate(__instance.iconPrefab);
                    transform.SetParent(__instance.iconHolder);
                    transform.localScale = Vector3.one;
                    if (k < Global.code.playerCombatParty.items.Count)
                    {
                        Transform unit = Global.code.playerCombatParty.items[k];
                        transform.GetComponent<CompanionSelectionIcon>().Initiate(unit, false);
                    }
                    else
                    {
                        transform.GetComponent<CompanionSelectionIcon>().Initiate(null, false);
                    }
                }
            }
        }
        [HarmonyPatch(typeof(Global), nameof(Global.AddCompanionToPlayerArmy))]
        static class AddCompanionToPlayerArmy_Patch
        {
            static void Postfix(Transform companion)
            {
                if (!modEnabled.Value)
                    return;

                if (Global.code.playerCombatParty.items.Count < partySize.Value && !Global.code.playerCombatParty.items.Exists(t => t.name == companion.name))
                {
                    Global.code.playerCombatParty.AddItemDifferentObject(companion);
                }
            }
        }
    }
}
