using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Resurrection
{
    [BepInPlugin("aedenthorn.Resurrection", "Resurrection", "0.1.5")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<int> healingSpellLevel;
        public static ConfigEntry<float> resurrectedHealthPercent;
        public static ConfigEntry<float> manaConsumptionMult;

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

            healingSpellLevel = Config.Bind<int>("Options", "HealingSpellLevel", 5, "Required healing aura level in order to resurrect companion.");
            resurrectedHealthPercent = Config.Bind<float>("Options", "resurrectedHealthPercent", 20, "Health restored to resurrected companions.");
            manaConsumptionMult = Config.Bind<float>("Options", "ManaConsumptionMult", 2f, "Multiplier for mana required, based on healing aura cost.");

            nexusID = Config.Bind<int>("General", "NexusID", 22, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        /*
        HarmonyPatch(typeof(Utility), nameof(Utility.GetNearestObject))]
        static class Utility_GetNearestObject_Patch
        {
            static void Prefix(ref List<Transform> _object, Transform origin)
            {
                if (!modEnabled.Value || Global.code.curlocation.locationType == LocationType.home || !origin || !origin.GetComponent<CharacterCustomization>())
                    return;

                _object = new List<Transform>(_object);
                for(int i = _object.Count - 1; i >= 0; i--)
                {
                    if(_object[i].GetComponent<Interaction>() && _object[i].GetComponent<CharacterCustomization>() && 
                }
            }
        }
        */
        [HarmonyPatch(typeof(CompanionCombatIcon), "Update")]
        static class CompanionCombatIcon_Update_Patch
        {
            static void Postfix(CompanionCombatIcon __instance)
            {
                if (!modEnabled.Value || !__instance.customization || __instance.customization._ID.health <= 0f || !__instance.icondeath.activeSelf)
                    return;

                __instance.icondeath.SetActive(false);

            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.Die))]
        static class CharacterCustomization_Die_Patch
        {
            static void Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || __instance._Player || !__instance.GetComponent<ID>().isFriendly)
                    return;

                if(!__instance.gameObject.GetComponent<Interaction>())
                    __instance.gameObject.AddComponent<Interaction>();
            }
        }

        [HarmonyPatch(typeof(Interaction), nameof(Interaction.Interact))]
        static class Interaction_Interact_Patch
        {
            static bool Prefix(Interaction __instance, CharacterCustomization customization)
            {
                if (!modEnabled.Value || !__instance.GetComponent<CharacterCustomization>() || __instance.gameObject.tag != "D" || !__instance.GetComponent<ID>() || !__instance.GetComponent<ID>().isFriendly)
                    return true;

                CharacterCustomization patient = __instance.GetComponent<CharacterCustomization>();


                if (customization.healaura < healingSpellLevel.Value)
                {
                    Dbgl($"Healing spell level too low!");
                    return true;
                }

                Skill heal = customization.skillHealingAura;

                if (customization._ID.mana < heal.manaConsumption * manaConsumptionMult.Value || heal.cd > 0)
                {
                    Dbgl($"Spell not ready or not enough mana!");
                    RM.code.PlayOneShot(RM.code.sndManaDepletion);
                    return true;
                }

                //DestroyImmediate(patient.GetComponent<Interaction>());

                customization._ID.AddMana(-customization.skillHealingAura.manaConsumption * manaConsumptionMult.Value);

                RM.code.PlayOneShot(heal.sfxActivate);
                heal.generatedEffect = Utility.Instantiate(heal.fx);
                heal.generatedEffect.position = patient.transform.position;

                patient.gameObject.tag = "Player";
                patient.UpdateStats();
                patient._ID.health = patient._ID.maxHealth * resurrectedHealthPercent.Value / 100f;
                patient._ID.tempHealth = patient._ID.maxHealth * resurrectedHealthPercent.Value / 100f;
                patient._ID.mana = patient._ID.maxMana;
                patient._ID.tempMana = patient._ID.maxMana;
                Global.code.friendlies.AddItemDifferentObject(patient.transform);
                patient.anim.enabled = true;
                patient.GetComponent<Rigidbody>().isKinematic = false;
                if (patient.GetComponent<Collider>())
                {
                    patient.GetComponent<Collider>().enabled = true;
                }
                foreach (Transform transform in patient.bones)
                {
                    if (transform)
                    {
                        transform.GetComponent<Rigidbody>().isKinematic = true;
                        transform.GetComponent<Rigidbody>().useGravity = false;
                        transform.GetComponent<Collider>().enabled = false;
                    }
                }

                if (patient.GetComponent<MapIcon>())
                {
                    Dbgl($"Removing old map icon");
                    DestroyImmediate(patient.gameObject.GetComponent<MapIcon>());
                }
                patient.gameObject.AddComponent<MapIcon>();
                patient.GetComponent<MapIcon>().healthBarColor = Color.green;
                patient.GetComponent<MapIcon>().id = patient.GetComponent<ID>();
                patient.GetComponent<MapIcon>().posBias = new Vector3(0f, 2.3f, 0f);
                patient.GetComponent<MapIcon>().visibleRange = 300f;

                Dbgl($"Patient restored to {patient._ID.health}/{patient._ID.maxHealth} health");

                Global.code.curInteractingCustomization = null;
                return false;
            }
        }
    }
}
