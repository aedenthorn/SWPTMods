using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
    public static class SkillAPI
    {
        public static void AddSkill(string id, List<string> name, List<string> description, int category, Texture2D icon, int maxPoints, int reqLevel, bool isActiveSkill)
        {
            if (!BepInExPlugin.customSkills.Exists(s => s.id == id))
            {
                BepInExPlugin.customSkills.Add(new SkillInfo()
                {
                    id = id,
                    name = name,
                    description = description,
                    category = category,
                    icon = icon,
                    maxPoints = maxPoints,
                    reqLevel = reqLevel,
                    isActiveSkill = isActiveSkill
                });
                BepInExPlugin.Dbgl($"Added skill {id}");
            }
            else
                BepInExPlugin.Dbgl($"Skill {id} already exists");
        }
        public static SkillInfo GetSkill(string id)
        {
            return BepInExPlugin.customSkills.Find(i => i.id == id);
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