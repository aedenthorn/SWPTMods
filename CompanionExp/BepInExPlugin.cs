using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace CompanionExp
{
    [BepInPlugin("aedenthorn.CompanionExp", "Companion Exp", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> freePoseEnterExp;
        public static ConfigEntry<int> freePosePassiveExp;
        public static ConfigEntry<int> furnitureInteractExp;
        public static ConfigEntry<int> furniturePassiveExp;
        public static ConfigEntry<int> combatPartyExp;
        public static ConfigEntry<float> levelMult;

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
            nexusID = Config.Bind<int>("General", "NexusID", 72, "Nexus mod ID for updates");

            freePosePassiveExp = Config.Bind<int>("Options", "FreePosePassiveExp", 10, "Experience per second while joining the player in free pose.");
            freePoseEnterExp = Config.Bind<int>("Options", "FreePoseEnterExp", 100, "Experience gained when while joining the player in free pose.");
            furniturePassiveExp = Config.Bind<int>("Options", "FurniturePassiveExp", 5, "Experience per second while posing on furniture.");
            furnitureInteractExp = Config.Bind<int>("Options", "FurnitureInteractExp", 50, "Experience gained when interacting with furniture.");
            combatPartyExp = Config.Bind<int>("Options", "CombatPartyExp", 10, "Experience per second while on a combat excursion with the player.");
            levelMult = Config.Bind<float>("Options", "LevelMult", 0.5f, "Mod experience gained is multiplied by 1 + this number times the character's level minus 1.");
            
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(ID), "CS")]
        static class ID_CS_Patch
        {
            static void Postfix(ID __instance)
            {
                if (!modEnabled.Value || !__instance.customization || __instance.player)
                    return;

                if (__instance.customization.interactingObject)
                {
                    AddMultExp(furniturePassiveExp.Value, __instance);
                }
                else if (Global.code.uiFreePose.gameObject.activeSelf && Global.code.uiFreePose.characters.GetItemWithName(__instance.name))
                {
                    AddMultExp(freePosePassiveExp.Value, __instance);
                }
            }
        }

        [HarmonyPatch(typeof(UIFreePose), nameof(UIFreePose.AddCharacter))]
        static class UIFreePose_AddCharacter_Patch
        {
            static void Postfix(Transform character)
            {
                if (!modEnabled.Value || character.GetComponent<Player>())
                    return;

                AddMultExp(freePoseEnterExp.Value, character.GetComponent<ID>());
            }
        }

        [HarmonyPatch(typeof(Companion), "CS")]
        static class Companion_CS_Patch
        {
            static void Postfix(Companion __instance)
            {
                if (!modEnabled.Value || !__instance.movingToTarget)
                    return;
                if(!__instance.movingToTarget.GetComponent<Furniture>().user || __instance.movingToTarget.GetComponent<Furniture>().user == __instance.GetComponent<CharacterCustomization>())
                {
                    if (Vector3.Distance(__instance.transform.position, __instance.movingToTarget.position) < 5f && __instance.movingToTarget.GetComponent<Furniture>() && __instance.GetComponent<Rigidbody>().velocity.magnitude < 0.5f)
                    {
                        __instance.movingToTarget.GetComponent<Furniture>().InteractWithOnlyPoses(__instance.customization);
                    }
                }
                else
                {
                    //Dbgl($"Resetting furniture target for {__instance.name}");
                    __instance.movingToTarget = null;
                    __instance.Stop();
                }
            }
        }

        [HarmonyPatch(typeof(Furniture), "DoInteract")]
        static class Furniture_DoInteract_Patch
        {
            static void Prefix(Furniture __instance, CharacterCustomization customization)
            {
                if (!modEnabled.Value || customization.GetComponent<Player>())
                    return;

                AddMultExp(furnitureInteractExp.Value, customization.GetComponent<ID>());
            }
        }

        private static void AddMultExp(int exp, ID id)
        {
            int mexp = Mathf.RoundToInt(exp * (1 + (id.level - 1) * levelMult.Value));
            id.AddExp(mexp);
            //Dbgl($"{id.name} got {mexp} exp");
        }
    }
}
