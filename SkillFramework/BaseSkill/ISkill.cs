using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkillFramework.BaseSkill
{
    public interface ISkill
    {

        ISkill Build(BaseUnityPlugin defaultPlugin, string sectionName);

        void SetConfig();

        void SetSettingChanged();

        void SetSkillDescription();

        void SetSkillIcon();

        void SetSkillName();

        void AddSkill();

        void Update();

        bool OnIncreaseSkillLevel(SkillBox skillBox, SkillInfo skillInfo);

        bool OnDecreaseSkillLevel(SkillBox skillBox, SkillInfo skillInfo);

        void PrefixSaveCharacterCustomization(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization);

        void PostfixLoadCharacterCustomization(SkillInfo skillInfo, Mainframe mainFrame, CharacterCustomization characterCustomization);

        void PrefixCharacterCustomizationUpdateStats(CharacterCustomization characterCustomization, SkillInfo skillInfo);

        void PostfixCharacterCustomizationUpdateStats(CharacterCustomization characterCustomization, SkillInfo skillInfo);

    }
}
