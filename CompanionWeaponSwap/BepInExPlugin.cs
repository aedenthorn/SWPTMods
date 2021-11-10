using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CompanionWeaponSwap
{
    [BepInPlugin("aedenthorn.CompanionWeaponSwap", "Companion Weapon Swap", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> lastSave;
        public static ConfigEntry<string> hotKey;

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
            nexusID = Config.Bind<int>("General", "NexusID", 115, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(Companion), "CS")]
        static class Companion_CS_Patch
        {
            static void Postfix(Companion __instance)
            {
                if (!modEnabled.Value || !__instance.target)
                    return;

                if (__instance.customization.weaponInHand?.GetComponent<Weapon>().weaponType == WeaponType.bow && __instance.customization.storage.GetItemCount("Arrow") <= 0)
                {
                    if (__instance.customization.weapon && __instance.customization.weapon.GetComponent<Weapon>().weaponType != WeaponType.bow)
                    {
                        Dbgl($"Switching to melee 1, bc no arrows");
                        __instance.customization.DrawWeapon(1);
                    }
                    else if (__instance.customization.weapon2 && __instance.customization.weapon2.GetComponent<Weapon>().weaponType != WeaponType.bow)
                    {
                        Dbgl($"Switching to melee 2, bc no arrows");
                        __instance.customization.DrawWeapon(2);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Companion), "EnemyClose")]
        static class Companion_EnemyClose_Patch
        {
            static bool Prefix(Companion __instance)
            {
                if (!modEnabled.Value || !__instance.target)
                    return true;

                // switch between melee and ranged
                if (__instance.customization.weaponInHand?.GetComponent<Weapon>().weaponType == WeaponType.bow)
                {
                    if (__instance.customization.weapon && __instance.customization.weapon.GetComponent<Weapon>().weaponType != WeaponType.bow)
                    {
                        Dbgl($"Switching to melee 1");
                        __instance.customization.DrawWeapon(1);
                        return false;
                    }
                    else if (__instance.customization.weapon2 && __instance.customization.weapon2.GetComponent<Weapon>().weaponType != WeaponType.bow)
                    {
                        Dbgl($"Switching to melee 2");
                        __instance.customization.DrawWeapon(2);
                        return false;
                    }
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Companion), "EnemyFar")]
        static class Companion_EnemyFar_Patch
        {
            static bool Prefix(Companion __instance)
            {
                if (!modEnabled.Value || !__instance.target)
                    return true;

                Dbgl($"Enemy far");

                // switch between melee and ranged
                if (__instance.customization.weaponInHand?.GetComponent<Weapon>().weaponType != WeaponType.bow && __instance.customization.storage.GetItemCount("Arrow") > 0)
                {
                    if (__instance.customization.weapon?.GetComponent<Weapon>().weaponType == WeaponType.bow)
                    {
                        Dbgl($"Switching to ranged 1");
                        __instance.customization.DrawWeapon(1);
                        return false;
                    }
                    else if (__instance.customization.weapon2?.GetComponent<Weapon>().weaponType == WeaponType.bow)
                    {
                        Dbgl($"Switching to ranged 2");
                        __instance.customization.DrawWeapon(2);
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
