using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace HealCompanions
{
    [BepInPlugin("aedenthorn.HealCompanions", "Heal Companions", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> modKey;
        public static ConfigEntry<string> healText;
        public static ConfigEntry<Color> healTextColor;
        public static ConfigEntry<float> healMult;
        public static ConfigEntry<bool> splitAmount;
        public static ConfigEntry<float> manaMult;
        public static ConfigEntry<bool> immediateHeal;


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
            nexusID = Config.Bind<int>("General", "NexusID", 37, "Nexus mod ID for updates");

            modKey = Config.Bind<string>("Options", "ModKey", "left shift", "Modifier key to heal companions with healing spell.");
            healText = Config.Bind<string>("Options", "HealText", "Healed companions for {0} hp.", "Text to show when healing companions. {0} is replaced by the HP amount received per companion.");
            healTextColor = Config.Bind<Color>("Options", "HealTextColor", Color.white, "Color of HealText.");
            healMult = Config.Bind<float>("Options", "HealFraction", 1, "Multiplier for total heal amount based on ordinary heal spell healing amount.");
            splitAmount = Config.Bind<bool>("Options", "SplitAmount", true, "If true, split the total healing amount between companions. If false, heal each companion for the total amount.");
            manaMult = Config.Bind<float>("Options", "ManaMult", 1, "Multiplier for mana cost based on ordinary heal spell mana cost.");
            immediateHeal = Config.Bind<bool>("Options", "ImmediateHeal", true, "If false, companions heal over time as with the ordinary healing aura spell.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Skill), "Cast")]
        static class Skill_Cast_Patch
        {
            static bool Prefix(Skill __instance, ref bool ___canRelease)
            {
                if (!modEnabled.Value || !AedenthornUtils.CheckKeyHeld(modKey.Value) || __instance.transform.name != "healaura" || !__instance.caster._Player)
                    return true;

                __instance.caster._ID.AddMana(-__instance.manaConsumption * manaMult.Value);
                ___canRelease = false;
                if (__instance.generatedHandfx)
                {
                    Destroy(__instance.generatedHandfx.gameObject);
                }
                __instance.Invoke("FX", __instance.FXdelayTime);
                __instance.Invoke("CancelAnimation", 0.1f);
                __instance.Invoke("Reset", 0.5f);

                float healAmount = (50 * __instance.caster.healaura + __instance.caster._ID.power * 10) * healMult.Value;
                if (splitAmount.Value)
                    healAmount /= Global.code.playerCombatParty.items.Count;

                Dbgl($"Healing {Global.code.playerCombatParty.items.Count} companions for {healAmount}");

                foreach (Transform t in Global.code.playerCombatParty.items)
                {
                    if (t.gameObject.tag == "D")
                        continue;

                    if (immediateHeal.Value)
                        t.GetComponent<ID>().health = Mathf.Min(t.GetComponent<ID>().maxHealth, t.GetComponent<ID>().health + healAmount);
                    else
                        t.GetComponent<ID>().AddHealth(healAmount, __instance.caster.transform);
                }

                Global.code.uiCombat.AddRollHint(string.Format(healText.Value, healAmount), healTextColor.Value);


                return false;
            }
        }
    }
}
