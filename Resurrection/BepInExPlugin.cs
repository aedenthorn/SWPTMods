using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Resurrection
{
    [BepInDependency("aedenthorn.SkillFramework", "0.1.0")]
    [BepInPlugin("aedenthorn.Resurrection", "Resurrection", "0.3.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<int> manaCost;
        public static ConfigEntry<float> resurrectedHealthPercent;
        public static ConfigEntry<int> maxPoints;
        public static ConfigEntry<int> reqLevel;
        public static ConfigEntry<string> castKey;
        public static ConfigEntry<float> range;

        public static string skillId = typeof(BepInExPlugin).Namespace;
        public static List<string> skillName = new List<string>()
        {
            "Resurrection Spell",
            "复活",
            "Воскрешение",
            "復活"
        };

        public static Texture2D skillIcon = new Texture2D(1, 1);
        public static Skill resurrectSkill;
        private static Transform deadCompanion;

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

            resurrectedHealthPercent = Config.Bind<float>("Options", "resurrectedHealthPercent", 20, "Health restored to resurrected companions per skill level.");
            range = Config.Bind<float>("Options", "Range", 10, "Spell range in meters.");
            manaCost = Config.Bind<int>("Options", "ManaConsumption", 50, "Mana required to cast resurrection spell.");
            maxPoints = Config.Bind<int>("Options", "MaxPoints", 5, "Max skill points for this skill.");
            reqLevel = Config.Bind<int>("Options", "ReqLevel", 2, "Character level required for this skill.");
            castKey = Config.Bind<string>("Options", "CastKey", "v", "Key to cast charm spell.");

            //nexusID = Config.Bind<int>("General", "NexusID", 22, "Nexus mod ID for updates");

            if (File.Exists(Path.Combine(AedenthornUtils.GetAssetPath(this), "icon.png")))
                skillIcon.LoadImage(File.ReadAllBytes(Path.Combine(AedenthornUtils.GetAssetPath(this), "icon.png")));

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix(Player __instance)
            {
                if (!modEnabled.Value || Global.code.onGUI)
                    return;
                if (AedenthornUtils.CheckKeyDown(castKey.Value))
                {
                    if (SkillFramework.SkillAPI.GetCharacterSkillLevel(skillId, __instance.name) > 0)
                    {
                        if (!resurrectSkill)
                        {
                            Dbgl("Creating resurrection spell");
                            resurrectSkill = Instantiate(__instance.transform.Find("healaura"), __instance.transform).GetComponent<Skill>();
                            resurrectSkill.gameObject.name = "Resurrection Spell";
                            resurrectSkill.manaConsumption = manaCost.Value;
                        }
                        IEnumerable<Transform> deadCompanions = FindObjectsOfType<Companion>().ToList().FindAll(t => t.GetComponent<Companion>() && t.GetComponent<ID>()?.isFriendly == true && t.GetComponent<ID>()?.isDead == true).Select(c => c.transform);
                        deadCompanion = null;
                        if (deadCompanions.Any())
                        {
                            deadCompanion = Utility.GetNearestObject(deadCompanions.ToList(), __instance.transform);
                            if (deadCompanion && range.Value > 0 && Vector3.Distance(Player.code.transform.position, deadCompanion.position) > range.Value)
                            {
                                deadCompanion = null;
                            }
                        }
                        if (deadCompanion)
                        {
                            Dbgl($"Casting resurrection spell on {deadCompanion.name}");

                            ResurrectCompanion(deadCompanion);
                            resurrectSkill.UseSkill(__instance.customization);
                        }
                        else
                        {
                            Dbgl("No dead companion in range");
                        }
                    }
                    else
                    {
                        Global.code.uiCombat.AddPrompt(Localization.GetContent("Didn't learn resurrection spell", new object[0]));
                        RM.code.PlayOneShot(RM.code.sndNegative);
                    }
                }
                else if (AedenthornUtils.CheckKeyUp(castKey.Value) && SkillFramework.SkillAPI.GetCharacterSkillLevel(skillId, __instance.name) > 0)
                {
                    resurrectSkill.Release();
                }
            }
        }

        private void charmChance_SettingChanged(object sender, EventArgs e)
        {
            AddSkill();
        }

        private void Start()
        {
            AddSkill();
        }

        private void AddSkill()
        {
            List<string> skillDescription = new List<string>()
            {
                string.Format("Resurrect a nearby downed companion, restoring {0}% of health per skill level)", resurrectedHealthPercent.Value),
                string.Format("复活附近死去的同伴，每升一级恢复他们 {0}% 的生命值", resurrectedHealthPercent.Value),
                string.Format("Воскресите ближайшего мертвого товарища, восстанавливая {0}% его здоровья за уровень", resurrectedHealthPercent.Value),
                string.Format("近くの死んだ仲間を復活させ、レベルごとに彼らの健康の{0}％を回復します", resurrectedHealthPercent.Value),
            };
            SkillFramework.SkillAPI.AddSkill(skillId, skillName, skillDescription, 2, skillIcon, maxPoints.Value, reqLevel.Value, true);
        }


        [HarmonyPatch(typeof(CompanionCombatIcon), "Update")]
        static class CompanionCombatIcon_Update_Patch
        {
            static void Postfix(CompanionCombatIcon __instance)
            {
                if (!modEnabled.Value || !__instance.id || __instance.id.gameObject.tag == "D")
                    return;
                __instance.icondeath.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(Skill), "FX")]
        static class Skill_FX_Patch
        {
            static void Postfix(Skill __instance)
            {
                if (!modEnabled.Value || __instance.name != "Resurrection Spell")
                    return;
                __instance.generatedEffect.position = deadCompanion.position;
                deadCompanion = null;
            }
        }

        public static void ResurrectCompanion(Transform companion)
        {
            if (!modEnabled.Value)
                return;

            CharacterCustomization customization = companion.GetComponent<CharacterCustomization>();

            RM.code.PlayOneShot(resurrectSkill.sfxActivate);

            customization.UpdateStats();
            customization._ID.health = Math.Min(customization._ID.maxHealth, customization._ID.maxHealth * resurrectedHealthPercent.Value / 100f * SkillFramework.SkillAPI.GetCharacterSkillLevel(skillId, "Player"));
            customization._ID.tempHealth = customization._ID.health;
            customization._ID.mana = customization._ID.maxMana;
            customization._ID.tempMana = customization._ID.maxMana;
            Global.code.friendlies.AddItemDifferentObject(customization.transform);
            customization.anim.enabled = true;
            customization.GetComponent<Rigidbody>().isKinematic = false;
            if (customization.GetComponent<Collider>())
            {
                customization.GetComponent<Collider>().enabled = true;
            }
            foreach (Transform transform in customization.bones)
            {
                if (transform)
                {
                    transform.GetComponent<Rigidbody>().isKinematic = true;
                    transform.GetComponent<Rigidbody>().useGravity = false;
                    transform.GetComponent<Collider>().enabled = false;
                }
            }
            customization._ID.isDead = false;

            if (customization.GetComponent<MapIcon>())
            {
                Dbgl($"Removing old map icon");
                DestroyImmediate(customization.gameObject.GetComponent<MapIcon>());
            }
            customization.gameObject.AddComponent<MapIcon>();
            customization.GetComponent<MapIcon>().healthBarColor = Color.green;
            customization.GetComponent<MapIcon>().id = customization.GetComponent<ID>();
            customization.GetComponent<MapIcon>().posBias = new Vector3(0f, 2.3f, 0f);
            customization.GetComponent<MapIcon>().visibleRange = 300f;

            Dbgl($"Patient restored to {customization._ID.health}/{customization._ID.maxHealth} health");
        }
    }
}
