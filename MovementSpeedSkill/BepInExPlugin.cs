using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MovementSpeedSkill
{
    [BepInPlugin("aedenthorn.MovementSpeedSkill", "Movement Speed Skill", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        
        public static ConfigEntry<int> increasePercent;
        public static ConfigEntry<int> maxPoints;
        public static ConfigEntry<int> reqLevel;

        public static string skillId = typeof(BepInExPlugin).Namespace;
        public static List<string> skillName = new List<string>()
        {
            "Fleet Foot",
            "快速移动",
            "Быстрый ход",
            "クイックムーブ"
        };

        public static List<string> skillDescription;
        public static Texture2D skillIcon = new Texture2D(1, 1);

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
            //nexusID = Config.Bind<int>("General", "NexusID", 4, "Nexus mod ID for updates");
            
            increasePercent = Config.Bind<int>("Options", "IncreasePercent", 5, "Increase movement speed by this percent per skill level.");
            maxPoints = Config.Bind<int>("Options", "MaxPoints", 5, "Max skill points for this skill.");
            reqLevel = Config.Bind<int>("Options", "ReqLevel", 2, "Character level required for this skill.");

            SetSkillDescription();

            increasePercent.SettingChanged += IncreasePercent_SettingChanged;

            if(File.Exists(Path.Combine(AedenthornUtils.GetAssetPath(this), "icon.png")))
                skillIcon.LoadImage(File.ReadAllBytes(Path.Combine(AedenthornUtils.GetAssetPath(this),"icon.png")));

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        private void IncreasePercent_SettingChanged(object sender, EventArgs e)
        {
            SetSkillDescription();
        }

        private void Start()
        {
            SkillFramework.SkillAPI.AddSkill(skillId, skillName, skillDescription, 1, skillIcon, maxPoints.Value, reqLevel.Value, false);
        }

        private void SetSkillDescription()
        {
            skillDescription = new List<string>()
            {
                string.Format("Increase movement speed by {0}% per level", increasePercent.Value),
                string.Format("每级增加 {0}% 移动速度", increasePercent.Value),
                string.Format("Увеличивайте скорость передвижения на {0}% за уровень", increasePercent.Value),
                string.Format("レベルごとに移動速度を{0}％増加させます", increasePercent.Value),
            };
        }

        [HarmonyPatch(typeof(ThirdPersonCharacter), "Update")]
        static class ThirdPersonCharacter_Update_Patch
        {
            static void Postfix(ThirdPersonCharacter __instance, ref float ___m_MoveSpeedMultiplier)
            {
                if (!modEnabled.Value || !__instance.GetComponent<CharacterCustomization>())
                    return;
                ___m_MoveSpeedMultiplier += SkillFramework.SkillAPI.GetCharacterSkillLevel(skillId, __instance.name) * increasePercent.Value / 100f;
            }
        }
    }
}
