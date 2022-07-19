using BepInEx;
using BepInEx.Configuration;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SkillFramework.BaseSkill
{
    public abstract class BaseSkill : ISkill
    {
        #region Default properties
        protected BaseUnityPlugin plugin;
        // name and description of the skill used in Localization added in order:
        // english, chinese, russian, japanese, ...
        protected List<string> skillName;
        protected List<string> skillDescription;
        // represent the category the skill will be in
        public int skillCategory = SkillCategories.Combat;
        // plugin section name
        protected string configSection;
        // skill icon texture
        protected Texture2D skillIcon = new Texture2D(1, 1);
        // skill icon file name (without file extension, we only use .png)
        public string iconName = "frame";
        // config default properties
        public int defaultMaxPoints = 5;
        public int defaultReqLevel = 2;
        // default config entries
        public static ConfigEntry<int> maxPoints;
        public static ConfigEntry<int> reqLevel;
        #endregion

        #region Getters/Setters
        protected abstract string skillId
        {
            get;
            set;
        }
        public string GetSkillID()
        {
            return skillId;
        }

        public override string ToString()
        {
            return iconName;
        }
        #endregion

        public ISkill Build(BaseUnityPlugin defaultPlugin, string sectionName)
        {
            plugin = defaultPlugin;
            configSection = sectionName;

            // bind default skill settings
            maxPoints = plugin.Config.Bind<int>(configSection, "MaxPoints", defaultMaxPoints, "Maximum skill points for this skill");
            reqLevel = plugin.Config.Bind<int>(configSection, "RequiredLevel", defaultReqLevel, "Character level required for this skill");

            // bind skill information
            SetConfig();
            SetSkillDescription();
            SetSkillName();
            SetSettingChanged();
            SetSkillIcon();

            BepInExPlugin.Log($"Setup Skill using: {configSection} - {skillCategory} - {defaultMaxPoints} - {defaultReqLevel} - {iconName}");

            // add skill to the list
            AddSkill();

            return this;
        }

        /// <summary>
        /// This method is called whenever the BepInEx configuration is updated
        /// </summary>
        public virtual void Update()
        {
            SetSkillDescription();

            // initialize increase/decrease delegate
            SkillInfo skillInfo = SkillAPI.GetSkill(skillId);

            // setup delegate handler if skill is defined
            if (skillInfo != null)
            {
                skillInfo.SetOnDecreaseSkillLevel = OnDecreaseSkillLevel;
                skillInfo.SetOnIncreaseSkillLevel = OnIncreaseSkillLevel;
                skillInfo.SetPostfixLoadCharacterCustomization = PostfixLoadCharacterCustomization;
                skillInfo.SetPrefixSaveCharacterCustomization = PrefixSaveCharacterCustomization;
                skillInfo.SetPrefixCharacterCustomizationUpdateStats = PrefixCharacterCustomizationUpdateStats;
                skillInfo.SetPostfixCharacterCustomizationUpdateStats = PostfixCharacterCustomizationUpdateStats;
            }
        }

        /// <summary>
        /// This method is called when the skill should be added to the game
        /// </summary>
        public virtual void AddSkill()
        {
            // initialize skill using SkillFramework API
            SkillAPI.AddSkill(skillId, skillName, skillDescription, skillCategory, skillIcon, maxPoints.Value, reqLevel.Value, false);
        }

        public virtual void SetSkillIcon()
        {
            string path = Path.Combine(AedenthornUtils.GetAssetPath(plugin), $"{iconName}.png");
            BepInExPlugin.Log($"Setup skill icon {path}");
            if (File.Exists(path))
                skillIcon.LoadImage(File.ReadAllBytes(path));
        }

        #region BepInEx & Basic Skill configuration method to override
        public abstract void SetConfig();
        public abstract void SetSkillDescription();
        public abstract void SetSkillName();
        public abstract void SetSettingChanged();
        #endregion

        #region Events Delegates

        public virtual bool OnDecreaseSkillLevel(SkillBox skillBox, SkillInfo skillInfo)
        {
            // cannot handle skill increase
            if (!CanHandleSkillIncreaseDecrease(skillBox, skillId))
                return true;

            // TODO Custom code here

            return true;
        }
        public virtual bool OnIncreaseSkillLevel(SkillBox skillBox, SkillInfo skillInfo)
        {
            // cannot handle skill increase
            if (!CanHandleSkillIncreaseDecrease(skillBox, skillId))
                return true;

            // TODO Custom code here

            return true;
        }
        public virtual void PrefixSaveCharacterCustomization(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization)
        { }
        public virtual void PostfixLoadCharacterCustomization(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization)
        { }
        public virtual void PrefixCharacterCustomizationUpdateStats(CharacterCustomization characterCustomization, SkillInfo skillInfo)
        { }
        public virtual void PostfixCharacterCustomizationUpdateStats(CharacterCustomization characterCustomization, SkillInfo skillInfo)
        { }

        #endregion

        #region Helpers

        public virtual bool CanHandleSkillIncreaseDecrease(SkillBox skillBox, string skillId)
        {
            // procceed to next call if not in the current skill
            if (skillId != skillBox.name)
                return false;

            return true;
        }

        #endregion

    }
}
