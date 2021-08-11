using System.Collections.Generic;
using UnityEngine;

namespace SkillFramework
{
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
    }
}