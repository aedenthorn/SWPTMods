using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    // triggered on increasing a skill level
    public delegate bool SkillValidateIncreaseDelegate(SkillBox skillBox, SkillInfo skillInfo);
    // triggered on decreasing a skill level
    public delegate bool SkillValidateDecreaseDelegate(SkillBox skillBox, SkillInfo skillInfo);

    // triggered on saving a character skills levels
    public delegate void PrefixSaveCharacterCustomizationDelegate(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization);
    // triggered on loading a character skills levels
    public delegate void PostfixLoadCharacterCustomizationDelegate(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization);

    // triggered on updating a character stats
    public delegate void PrefixCharacterCustomizationUpdateStatsDelegate(CharacterCustomization characterCustomization, SkillInfo skillInfo);
    public delegate void PostfixCharacterCustomizationUpdateStatsDelegate(CharacterCustomization characterCustomization, SkillInfo skillInfo);

    public class SkillInfo
    {
        public string id;
        public List<string> name;
        public List<string> description;
        public int category;
        public Texture2D icon;
        public int maxPoints;
        public int reqLevel;
        public bool isActiveSkill;

        #region Skill Increase Validation
        private SkillValidateIncreaseDelegate _increaseDelegate;
        public SkillValidateIncreaseDelegate SetOnIncreaseSkillLevel
        {
            get { return _increaseDelegate; }
            set { _increaseDelegate = value; }
        }
        public bool OnIncreaseSkillLevel(SkillBox skillBox, SkillInfo skillInfo)
        {
            if (_increaseDelegate != null)
                return _increaseDelegate(skillBox, skillInfo);

            return true;
        }
        #endregion

        #region Skill Decrease Validation
        private SkillValidateDecreaseDelegate _decreaseDelegate;
        public SkillValidateDecreaseDelegate SetOnDecreaseSkillLevel
        {
            get { return _decreaseDelegate; }
            set { _decreaseDelegate = value; }
        }
        public bool OnDecreaseSkillLevel(SkillBox skillBox, SkillInfo skillInfo)
        {
            if (_decreaseDelegate != null)
                return _decreaseDelegate(skillBox, skillInfo);

            return true;
        }
        #endregion

        #region Prefix Patch Save Character Customization
        private PrefixSaveCharacterCustomizationDelegate _prefixSaveCharacterCustomizationDelegate;
        public PrefixSaveCharacterCustomizationDelegate SetPrefixSaveCharacterCustomization
        {
            get { return _prefixSaveCharacterCustomizationDelegate; }
            set { _prefixSaveCharacterCustomizationDelegate = value; }
        }
        public void PrefixSaveCharacterCustomization(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization)
        {
            if (_prefixSaveCharacterCustomizationDelegate != null)
                _prefixSaveCharacterCustomizationDelegate(skillInfo, mainFrame, characterCustomization);
        }
        #endregion

        #region Postfix Load Character Customization
        private PostfixLoadCharacterCustomizationDelegate _postfixLoadCharacterCustomizationDelegate;
        public PostfixLoadCharacterCustomizationDelegate SetPostfixLoadCharacterCustomization
        {
            get { return _postfixLoadCharacterCustomizationDelegate; }
            set { _postfixLoadCharacterCustomizationDelegate = value; }
        }
        public void PostfixLoadCharacterCustomization(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization)
        {
            if (_postfixLoadCharacterCustomizationDelegate != null)
                _postfixLoadCharacterCustomizationDelegate(skillInfo, mainFrame, characterCustomization);
        }
        #endregion

        #region Prefix Character Customization Update Stats
        private PrefixCharacterCustomizationUpdateStatsDelegate _prefixCharacterCustomizationUpdateStatsDelegate;
        public PrefixCharacterCustomizationUpdateStatsDelegate SetPrefixCharacterCustomizationUpdateStats
        {
            get { return _prefixCharacterCustomizationUpdateStatsDelegate; }
            set { _prefixCharacterCustomizationUpdateStatsDelegate = value; }
        }
        public void PrefixCharacterCustomizationUpdateStats(CharacterCustomization characterCustomization, SkillInfo skillInfo)
        {
            if (_prefixCharacterCustomizationUpdateStatsDelegate != null)
                _prefixCharacterCustomizationUpdateStatsDelegate(characterCustomization, skillInfo);
        }
        #endregion

        #region Postfix Character Customization Update Stats
        private PostfixCharacterCustomizationUpdateStatsDelegate _postfixCharacterCustomizationUpdateStatsDelegate;
        public PostfixCharacterCustomizationUpdateStatsDelegate SetPostfixCharacterCustomizationUpdateStats
        {
            get { return _postfixCharacterCustomizationUpdateStatsDelegate; }
            set { _postfixCharacterCustomizationUpdateStatsDelegate = value; }
        }
        public void PostfixCharacterCustomizationUpdateStats(CharacterCustomization characterCustomization, SkillInfo skillInfo)
        {
            if (_postfixCharacterCustomizationUpdateStatsDelegate != null)
                _postfixCharacterCustomizationUpdateStatsDelegate(characterCustomization, skillInfo);
        }
        #endregion

    }
}