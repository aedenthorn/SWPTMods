using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    public static class SkillAPI
    {
        public static void AddSkill(string id, List<string> name, List<string> description, int category, Texture2D icon, int maxPoints, int reqLevel, bool isActiveSkill)
        {
            if (BepInExPlugin.customSkills.ContainsKey(id))
            {
                BepInExPlugin.Dbgl($"Updating skill {id}");
            }
            else
            {
                BepInExPlugin.Dbgl($"Adding skill {id}");
            }
            BepInExPlugin.customSkills[id] = new SkillInfo()
            {
                id = id,
                name = name,
                description = description,
                category = category,
                icon = icon,
                maxPoints = maxPoints,
                reqLevel = reqLevel,
                isActiveSkill = isActiveSkill
            };
            if (Global.code?.uiCharacter?.gameObject.activeSelf == true)
                Global.code.uiCharacter.Refresh();
        }
        public static SkillInfo GetSkill(string id)
        {
            return BepInExPlugin.customSkills.ContainsKey(id) ? BepInExPlugin.customSkills[id] : null;
        }
        public static int GetCharacterSkillLevel(string id, string name)
        {
            if (!BepInExPlugin.characterSkillLevels.ContainsKey(name) || !BepInExPlugin.characterSkillLevels[name].ContainsKey(id))
            {
                return 0;
            }
            return BepInExPlugin.characterSkillLevels[name][id];
        }
    }
}