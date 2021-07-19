using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BowAnimationFix
{
    [BepInPlugin("aedenthorn.BowAnimationFix", "Bow Animation Fix", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        //ConfigEntry<int> nexusID;

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

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.PullBow))]
        static class PullBow_Patch
        {
            static void Prefix(CharacterCustomization __instance)
            {
                //Dbgl($"storage {__instance.storage != null}, weapon {__instance.weaponInHand != null}, weapon weapon {__instance.weaponInHand?.GetComponent<Weapon>() != null}, animator {__instance.weaponInHand?.GetComponent<Weapon>()?.bowAnimator != null}");

                if (__instance.weaponInHand.GetComponent<Weapon>().bowAnimator == null)
                {
                    Dbgl("bow animator is null, replacing");
                    __instance.weaponInHand.GetComponent<Weapon>().bowAnimator = RM.code.allWeapons.items.First(i => i.GetComponent<Weapon>()?.bowAnimator != null).GetComponent<Weapon>().bowAnimator;
                }
            }
        }
        [HarmonyPatch(typeof(UIWorldMap), nameof(UIWorldMap.RefreshStats))]
        static class UIWorldmap_RefreshStats_Patch
        {
            static void Prefix(UIWorldMap __instance)
            {
                Dbgl($"fieldarmies {Global.code.fieldArmies != null}, locations {Global.code.locations != null}, textprogress {__instance.txtProgress != null}, progressBar {__instance.progressBar != null}");

                foreach (Transform transform in Global.code.locations.items)
                {
                    if (transform.GetComponent<Location>() == null)
                        Dbgl("location is null");
                    if (transform.parent?.gameObject == null)
                        Dbgl("location parent is null");
                }
            }
        }
    }
}
