using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace WeaponSpeed
{
    [BepInPlugin("aedenthorn.WeaponSpeed", "Weapon Speed", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<float> oneHandSpeedMult;
        public static ConfigEntry<float> daggerSpeedMult;
        public static ConfigEntry<float> oneHandHammerSpeedMult;
        public static ConfigEntry<float> bowSpeedMult;
        public static ConfigEntry<float> twoHandSpeedMult;
        public static ConfigEntry<float> spearSpeedMult;
        public static ConfigEntry<float> throwingAxeSpeedMult;
        public static ConfigEntry<float> javelinSpeedMult;
        public static ConfigEntry<float> twoHandAxeSpeedMult;
        public static ConfigEntry<float> oneHandAxeSpeedMult;

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

            oneHandSpeedMult = Config.Bind<float>("Options", "OneHandSpeedMult", 1, "oneHandSpeedMult");
            daggerSpeedMult = Config.Bind<float>("Options", "DaggerSpeedMult", 1, "daggerSpeedMult");
            oneHandHammerSpeedMult = Config.Bind<float>("Options", "OneHandHammerSpeedMult", 1, "oneHandHammerSpeedMult");
            bowSpeedMult = Config.Bind<float>("Options", "BowSpeedMult", 1, "bowSpeedMult");
            twoHandSpeedMult = Config.Bind<float>("Options", "TwoHandSpeedMult", 1, "twoHandSpeedMult");
            spearSpeedMult = Config.Bind<float>("Options", "SpearSpeedMult", 1, "spearSpeedMult");
            throwingAxeSpeedMult = Config.Bind<float>("Options", "ThrowingAxeSpeedMult", 1, "throwingAxeSpeedMult");
            javelinSpeedMult = Config.Bind<float>("Options", "JavelinSpeedMult", 1, "javelinSpeedMult");
            twoHandAxeSpeedMult = Config.Bind<float>("Options", "TwoHandAxeSpeedMult", 1, "twoHandAxeSpeedMult");
            oneHandAxeSpeedMult = Config.Bind<float>("Options", "OneHandAxeSpeedMult", 1, "oneHandAxeSpeedMult");

        nexusID = Config.Bind<int>("General", "NexusID", 16, "Nexus mod ID for updates");

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        public static float origSpeed = 1;

        [HarmonyPatch(typeof(CharacterCustomization), "Attack")]
        static class CharacterCustomization_Attack_Patch
        {
            static void Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || __instance.weaponIndex == 0)
                    return;

                Transform transform = __instance.weaponIndex == 1 ? __instance.weapon : __instance.weapon2;
                if (!transform)
                    return;

                origSpeed = __instance.anim.speed;

                //Dbgl($"Attacking, orig speed {origSpeed}");

                switch (transform.GetComponent<Weapon>().weaponType)
                {
                    case WeaponType.onehand:
                        __instance.anim.speed = oneHandSpeedMult.Value;
                        break;
                    case WeaponType.dagger:
                        __instance.anim.speed = daggerSpeedMult.Value;
                        break;
                    case WeaponType.onehandhammer:
                        __instance.anim.speed = oneHandHammerSpeedMult.Value;
                        break;
                    case WeaponType.bow:
                        __instance.anim.speed = bowSpeedMult.Value;
                        break;
                    case WeaponType.twohand:
                        __instance.anim.speed = twoHandSpeedMult.Value;
                        break;
                    case WeaponType.spear:
                        __instance.anim.speed = spearSpeedMult.Value;
                        break;
                    case WeaponType.throwingaxe:
                        __instance.anim.speed = throwingAxeSpeedMult.Value;
                        break;
                    case WeaponType.javelin:
                        __instance.anim.speed = javelinSpeedMult.Value;
                        break;
                    case WeaponType.twohandaxe:
                        __instance.anim.speed = twoHandAxeSpeedMult.Value;
                        break;
                    case WeaponType.onehandaxe:
                        __instance.anim.speed = oneHandAxeSpeedMult.Value;
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), "ResetAttack")]
        static class CharacterCustomization_ResetAttack_Patch
        {
            static void Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value)
                    return;

                __instance.anim.speed = origSpeed;

                //Dbgl($"Reset attack, orig speed {origSpeed}");

            }
        }
    }
}
