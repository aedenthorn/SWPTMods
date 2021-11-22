using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace SkillFramework
{
    [BepInPlugin("aedenthorn.SkillFramework", "Skill Framework", "0.3.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static BepInExPlugin context;

        public static Dictionary<string, Dictionary<string, int>> characterSkillLevels = new Dictionary<string, Dictionary<string, int>>();
        public static Dictionary<string, SkillInfo> customSkills = new Dictionary<string, SkillInfo>();

        public static void Log(string message)
        {
            if (isDebug.Value)
                Debug.Log(typeof(BepInExPlugin).Namespace + " - " + message);
        }

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");

            nexusID = Config.Bind<int>("General", "NexusID", 69, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        public static int GetCharacterSkillLevel(string charName, string skillname)
        {
            if (!characterSkillLevels.ContainsKey(charName))
                characterSkillLevels[charName] = new Dictionary<string, int>();
            if (!characterSkillLevels[charName].ContainsKey(skillname))
                characterSkillLevels[charName][skillname] = 0;
            return characterSkillLevels[charName][skillname];
        }
        public static void SetCharacterSkillLevel(string charName, string skillname, int level)
        {
            if (!characterSkillLevels.ContainsKey(charName))
                characterSkillLevels[charName] = new Dictionary<string, int>();
            characterSkillLevels[charName][skillname] = level;
        }

        [HarmonyPatch(typeof(UICharacter), nameof(UICharacter.Refresh))]
        static class UICharacter_Refresh_Patch
        {
            static void Postfix(UICharacter __instance)
            {
                // this draw the custom skills in their respective categories
                // after each time the UICharacter.Refresh method is called in the game

                if (!modEnabled.Value || !customSkills.Any())
                    return;

                // get categories transform elements
                Transform[] parents = AedenthornUtils.GetUICharacterSkillCategoriesParents(__instance);

                foreach (SkillInfo skillInfo in customSkills.Values)
                {
                    // get current skill transform element
                    Transform skillTransform = parents[skillInfo.category].Find(skillInfo.id);

                    // check if it's defined
                    if (!skillTransform)
                    {
                        // check if we should place the skill in a new line
                        if (parents[skillInfo.category].childCount % 6 == 0)
                        {
                            // decrease y of lower categories
                            float height = parents[skillInfo.category].GetComponent<GridLayoutGroup>().spacing.y + __instance.daggerproficiency.transform.GetComponent<RectTransform>().sizeDelta.y;
                            for (int i = skillInfo.category + 1; i < parents.Length; i++)
                            {
                                parents[i].parent.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, height);
                                foreach (Transform category in parents[i])
                                {
                                    foreach (Transform cc in category)
                                    {
                                        if (cc.name.StartsWith("plus"))
                                            cc.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                                    }
                                }
                            }
                            parents[0].parent.parent.GetComponent<RectTransform>().sizeDelta += new Vector2(0, height);
                        }
                        // instanciate skillbox
                        skillTransform = Instantiate(__instance.daggerproficiency.transform, parents[skillInfo.category]);
                        skillTransform.name = skillInfo.id;
                        skillTransform.GetComponentInChildren<Button>().GetComponent<RawImage>().texture = skillInfo.icon;
                    }

                    // setup localization
                    Localization.LocalizationDic[skillInfo.id] = skillInfo.name;
                    Localization.LocalizationDic[skillInfo.id + "DESC"] = skillInfo.description;

                    // get current skill level
                    int points = GetCharacterSkillLevel(__instance.curCustomization.name, skillInfo.id);

                    // create SkillBox and setup properties
                    SkillBox skillBox = skillTransform.GetComponent<SkillBox>();
                    skillBox.maxPoints = skillInfo.maxPoints;
                    skillBox.isActiveSkill = skillInfo.isActiveSkill;
                    skillBox.Initiate(points);
                    __instance.curCustomization.UpdateStats();
                }
            }
        }

        [HarmonyPatch(typeof(SkillBox), nameof(SkillBox.ButtonClick))]
        static class SkillBox_ButtonClick_Patch
        {
            static bool Prefix(SkillBox __instance)
            {
                // this handle click on a skill box
                // it handle the increase / decrease of the current clicked skill level

                if (!modEnabled.Value)
                    return true;

                // check if the aedenthorn.Respec plugin is loaded so we can handle this plugin behaviour to reduce the skill level
                bool reduce = Chainloader.PluginInfos.ContainsKey("aedenthorn.Respec") && AedenthornUtils.CheckKeyHeld(Chainloader.PluginInfos["aedenthorn.Respec"].Instance.Config[new ConfigDefinition("Options", "ModKey")].BoxedValue as string);
                SkillInfo skillInfo = SkillAPI.GetSkill(__instance.name);

                // skip to default method if not a custom skill
                if (skillInfo == null)
                    return true;

                // either there's no available points or at max points for the skill
                if (!reduce && Global.code.uiCharacter.curCustomization._ID.skillPoints <= 0 || !reduce && __instance.points >= __instance.maxPoints)
                    return false;

                if (reduce)
                {
                    // check if we can proceed to decrease skill level
                    if (skillInfo.OnDecreaseSkillLevel(__instance, skillInfo))
                    {
                        SkillAPI.DecreaseSkillLevel(__instance.name, Global.code.uiCharacter.curCustomization.name);
                        Global.code.uiCharacter.Refresh();
                    }
                    // return to avoid running default code that increase the skill level
                    return false;
                }

                // check if we can proceed to increase skill level
                if (!reduce && skillInfo.OnIncreaseSkillLevel(__instance, skillInfo))
                {
                    SkillAPI.IncreaseSkillLevel(__instance.name, Global.code.uiCharacter.curCustomization.name);
                    return true;
                }
                
                return true;
            }
        }

        [HarmonyPatch(typeof(Mainframe), "SaveCharacterCustomization")]
        static class SaveCharacterCustomization_Patch
        {
            static void Prefix(Mainframe __instance, CharacterCustomization customization)
            {
                // this save the current character skills levels
                if (!modEnabled.Value)
                    return;

                foreach (SkillInfo info in customSkills.Values)
                {
                    ES2.Save<int>(GetCharacterSkillLevel(customization.name, info.id), $"{__instance.GetFolderName()}{customization.name}.txt?tag=CustomSkill{info.id}");
                    // trigger delegate
                    info.PrefixSaveCharacterCustomization(info, __instance, customization);
                }
            }
        }

        [HarmonyPatch(typeof(Mainframe), "LoadCharacterCustomization")]
        static class LoadCharacterCustomization_Patch
        {
            static void Postfix(Mainframe __instance, CharacterCustomization gen)
            {
                // this load the current character skills levels
                if (!modEnabled.Value)
                    return;

                foreach (SkillInfo info in customSkills.Values)
                {
                    if (ES2.Exists($"{__instance.GetFolderName()}{gen.name}.txt?tag=CustomSkill{info.id}"))
                    {
                        SetCharacterSkillLevel(gen.name, info.id, ES2.Load<int>($"{__instance.GetFolderName()}{gen.name}.txt?tag=CustomSkill{info.id}"));
                        // trigger delegate
                        info.PostfixLoadCharacterCustomization(info, __instance, gen);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.UpdateStats))]
        static class CharacterCustomization_UpdateStats_Patch
        {
            static bool Prefix(CharacterCustomization __instance)
            {
                // iterate each skill and run the delegate
                foreach (SkillInfo skillInfo in customSkills.Values)
                {
                    skillInfo.PrefixCharacterCustomizationUpdateStats(__instance, skillInfo);
                }

                return true; // always returning true to continue through the main code
            }

            static void Postfix(CharacterCustomization __instance)
            {
                // iterate each skill and run the delegate
                foreach (SkillInfo skillInfo in customSkills.Values)
                {
                    skillInfo.PostfixCharacterCustomizationUpdateStats(__instance, skillInfo);
                }
            }
        }
    }
}
