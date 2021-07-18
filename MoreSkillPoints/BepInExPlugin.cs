using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MoreSkillPoints
{
    [BepInPlugin("aedenthorn.MoreSkillPoints", "More Skill Points", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> extraSkillPointsPerLevel;
        public static ConfigEntry<int> extraAttributePointsPerLevel;
        //public static ConfigEntry<int> nexusID;

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
            extraSkillPointsPerLevel = Config.Bind<int>("Options", "ExtraSkillPointsPerLevel", 2, "Extra skill points per level (added to the default 2).");
            extraAttributePointsPerLevel = Config.Bind<int>("Options", "ExtraAttributePointsPerLevel", 1, "Extra attribute points per level  (added to the default 1).");


            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

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
    }
}
