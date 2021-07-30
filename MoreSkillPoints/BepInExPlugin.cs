using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MoreSkillPoints
{
    [BepInPlugin("aedenthorn.MoreSkillPoints", "More Skill Points", "0.3.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> maxSkillPointsPerSkill;
        public static ConfigEntry<int> extraSkillPointsPerLevel;
        public static ConfigEntry<int> extraAttributePointsPerLevel;
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
            maxSkillPointsPerSkill = Config.Bind<int>("Options", "MaxSkillPointsPerSkill", 10, "Max skill points per skill.");
            extraSkillPointsPerLevel = Config.Bind<int>("Options", "ExtraSkillPointsPerLevel", 2, "Extra skill points per level (added to the default 2).");
            extraAttributePointsPerLevel = Config.Bind<int>("Options", "ExtraAttributePointsPerLevel", 1, "Extra attribute points per level  (added to the default 1).");


            nexusID = Config.Bind<int>("General", "NexusID", 2, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(SkillBox), nameof(SkillBox.Initiate))]
        static class SkillBox_Initiate_Patch
        {
            static void Prefix(SkillBox __instance)
            {
                if (!modEnabled.Value)
                    return;

                __instance.maxPoints = maxSkillPointsPerSkill.Value;
            }
        }
        
        [HarmonyPatch(typeof(ID), nameof(ID.GetNextExp))]
        static class GetNextExp_Patch
        {
            static void Postfix(ID __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (__instance.curExp >= __instance.nextExp)
                {
                    __instance.attributePoints += extraAttributePointsPerLevel.Value;
                    __instance.skillPoints += extraSkillPointsPerLevel.Value;
                }

            }
        }

        [HarmonyPatch(typeof(ID), "Start")]
        static class ID_Start_Patch
        {
            static void Postfix(ID __instance)
            {
                if (!modEnabled.Value)
                    return;

                __instance.attributePoints += __instance.level * extraAttributePointsPerLevel.Value;
                __instance.skillPoints += __instance.level * extraSkillPointsPerLevel.Value;

            }
        }
    }
}
