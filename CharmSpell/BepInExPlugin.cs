using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace CharmSpell
{
    [BepInDependency("aedenthorn.SkillFramework", "0.1.0")]
    [BepInPlugin("aedenthorn.CharmSpell", "Charm Spell", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> castKey;
        public static ConfigEntry<string> charmedNotice;
        public static ConfigEntry<string> failNotice;
        public static ConfigEntry<int> charmChance;
        public static ConfigEntry<int> charmDuration;
        public static ConfigEntry<int> maxPoints;
        public static ConfigEntry<int> reqLevel;
        public static ConfigEntry<int> manaCost;
        public static ConfigEntry<float> levelResistanceMult;

        public static string skillId = typeof(BepInExPlugin).Namespace;
        public static List<string> skillName = new List<string>()
        {
            "Charm Spell",
            "魅力咒语",
            "Быстрый ход",
            "Заклинание обаяния"
        };

        public static Texture2D skillIcon = new Texture2D(1, 1);
        public static Skill charmSkill;
        
        public static Dictionary<Transform, float> charmedEnemies = new Dictionary<Transform, float>();

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
            nexusID = Config.Bind<int>("General", "NexusID", 88, "Nexus mod ID for updates");

            charmedNotice = Config.Bind<string>("Options", "CharmedNotice", "Charmed {0}!", "Notification to display on successful charm.");
            failNotice = Config.Bind<string>("Options", "FailNotice", "Failed to charm {0} ({1}% chance).", "Notification to display on charm failure.");
            castKey = Config.Bind<string>("Options", "CastKey", "v", "Key to cast charm spell.");
            charmChance = Config.Bind<int>("Options", "CharmChance", 10, "Charm chance percent per skill level.");
            charmDuration = Config.Bind<int>("Options", "CharmDuration", 10, "Charm duration in seconds per skill level.");
            maxPoints = Config.Bind<int>("Options", "MaxPoints", 5, "Max skill points for this skill.");
            reqLevel = Config.Bind<int>("Options", "ReqLevel", 2, "Character level required for this skill.");
            manaCost = Config.Bind<int>("Options", "ManaCost", 20, "Mana cost for charm spell.");
            levelResistanceMult = Config.Bind<float>("Options", "LevelResistanceMult", 1.5f, "Percent chance reduction per level of enemy.");

            charmChance.SettingChanged += charmChance_SettingChanged;

            if (File.Exists(Path.Combine(AedenthornUtils.GetAssetPath(this), "icon.png")))
                skillIcon.LoadImage(File.ReadAllBytes(Path.Combine(AedenthornUtils.GetAssetPath(this), "icon.png")));

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

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
                string.Format("Cast projectile to charm target ({0}% chance per level)", charmChance.Value),
                string.Format("向目标施放投射物（每级 {0}% 几率）", charmChance.Value),
                string.Format("Использовать снаряд для очарования цели (шанс {0}% за уровень) ", charmChance.Value),
                string.Format("発射物をチャームターゲットにキャストします（レベルごとに{0}％の確率）", charmChance.Value),
            };
            SkillFramework.SkillAPI.AddSkill(skillId, skillName, skillDescription, 2, skillIcon, maxPoints.Value, reqLevel.Value, true);
        }

        private void Update()
        {
            if (charmedEnemies.Any())
            {
                List<Transform> keys = new List<Transform>(charmedEnemies.Keys);
                foreach (Transform key in keys)
                {
                    charmedEnemies[key] -= Time.deltaTime;
                    if (charmedEnemies[key] <= 0)
                    {
                        key.GetComponent<ID>().isFriendly = false;
                        charmedEnemies.Remove(key);
                        Dbgl($"Charm wore off for {key.name}");
                    }
                }
            }
        }
       
        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix(Player __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (AedenthornUtils.CheckKeyDown(castKey.Value))
                {
                    if (SkillFramework.SkillAPI.GetCharacterSkillLevel(skillId, __instance.name) > 0)
                    {
                        if (!charmSkill)
                        {
                            charmSkill = Instantiate(__instance.transform.Find("fireball"), __instance.transform).GetComponent<Skill>();
                            charmSkill.gameObject.name = "Charm Spell";
                            //charmSkill.sfxActivate = __instance.GetComponent<CharacterCustomization>().skillHealingAura.sfxActivate;
                            charmSkill.manaConsumption = manaCost.Value;
                        }

                        charmSkill.UseSkill(__instance.customization);
                    }
                    else
                    {
                        Global.code.uiCombat.AddPrompt(Localization.GetContent("Didn't learn charm spell", new object[0]));
                        RM.code.PlayOneShot(RM.code.sndNegative);
                    }
                }
                else if (AedenthornUtils.CheckKeyUp(castKey.Value) && SkillFramework.SkillAPI.GetCharacterSkillLevel(skillId, __instance.name) > 0)
                {
                    charmSkill.Release();
                }
            }
        }
        [HarmonyPatch(typeof(Skill), nameof(Skill.UseSkill))]
        static class Skill_UseSkill_Patch
        {
            static void Postfix(Skill __instance)
            {
                if (!modEnabled.Value || __instance.name != "Charm Spell" || !__instance.generatedHandfx)
                    return;

                foreach (ParticleSystemRenderer r in __instance.generatedHandfx.GetComponentsInChildren<ParticleSystemRenderer>(true))
                {
                    if (r.name == "Fire")
                    {
                        r.material.SetColor("_TintColor", new Color(0, 1, 0.374f, 0.635f));
                    }
                    else if (r.name == "Particles")
                    {
                        r.material.SetColor("_TintColor", new Color(1.152f, 2.702f, 1.473f, 0.08f));
                    }
                    else if (r.name == "Distortion")
                    {
                        r.material.SetColor("_TintColor", new Color(0.297f, 0.860f, 0.589f, 0.559f));
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Skill), "FX")]
        static class Skill_FX_Patch
        {
            static void Postfix(Skill __instance)
            {
                if (!modEnabled.Value || __instance.name != "Charm Spell")
                    return;
                foreach (Renderer r in __instance.generatedEffect.GetComponentsInChildren<Renderer>(true))
                {
                    if (r.name == "FireTrail")
                    {
                        r.material.SetColor("_TintColor", new Color(0, 1.087f, 0.428f, 1));
                    }
                    else if (r.name == "Particles")
                    {
                        r.material.SetColor("_TintColor", new Color(0.842f, 1.877f, 1.549f, 0.423f));
                    }
                    else if (r.name == "Core")
                    {
                        r.material.SetColor("_TintColor", new Color(0.798f, 0.853f, 1.938f, 1));
                    }
                }
                __instance.generatedEffect.GetComponentInChildren<RFX4_PhysicsMotion>().EffectOnCollision = null;
                __instance.generatedEffect.transform.name = "Charm Spell";

            }
        }

        [HarmonyPatch(typeof(RFX4_PhysicsMotion), "OnCollisionEnter")]
        static class RFX4_PhysicsMotion_OnCollisionEnter_Patch
        {
            static bool Prefix(RFX4_PhysicsMotion __instance, Collision collision, Rigidbody ___rigid, SphereCollider ___collid)
            {
                if (!modEnabled.Value || __instance.transform.parent.name != "Charm Spell")
                    return true;

                Dbgl($"Hit {collision.collider.name}!");

                Transform t = collision.collider.transform;
                while (true)
                {
                    if(t.GetComponent<ID>() && !t.GetComponent<ID>().isFriendly)
                    {
                        int level = SkillFramework.SkillAPI.GetCharacterSkillLevel(skillId, "Player");
                        float chance = charmChance.Value * level - levelResistanceMult.Value * t.GetComponent<ID>().level;
                        Dbgl($"Hit enemy {t.name}, charm chance {Mathf.RoundToInt(chance)}%");
                        if(Random.value < chance * 0.01f)
                        {
                            Transform effect = Utility.Instantiate(Player.code.customization.skillHealingAura.fx);
                            effect.position = t.position;
                            RM.code.PlayOneShot(Player.code.customization.skillHealingAura.sfxActivate);

                            charmedEnemies.Add(t, charmDuration.Value);
                            t.GetComponent<ID>().isFriendly = true;
                            Dbgl($"Charm success!");
                            if (charmedNotice.Value.Contains("{0}"))
                                Global.code.uiCombat.AddPrompt(string.Format(charmedNotice.Value, t.name));
                        }
                        else
                        {
                            if (failNotice.Value.Contains("{0}"))
                                Global.code.uiCombat.AddPrompt(string.Format(failNotice.Value, t.name, chance));
                        }
                        break;
                    }
                    if (!t.parent)
                        break;
                    t = t.parent;
                }
                if (___rigid != null)
                {
                    Destroy(___rigid);
                }
                if (___collid != null)
                {
                    Destroy(___collid);
                }
                return false;
            }
        }
    }
}
