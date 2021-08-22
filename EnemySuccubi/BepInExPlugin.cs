using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace EnemySuccubi
{
    [BepInPlugin("aedenthorn.EnemySuccubi", "Enemy Succubi", "0.2.6")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> replaceOrdinaryChance;
        public static ConfigEntry<float> bossGangChance;
        public static ConfigEntry<float> gearEnchantmentChance;
        public static ConfigEntry<int> bossGangMax;
        public static ConfigEntry<string> succubusName;
        public static ConfigEntry<bool> dropEquipment;
        
        public static ConfigEntry<Rarity> lootLevel;

        public static List<GameObject> toDestroy = new List<GameObject>();


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
            nexusID = Config.Bind<int>("General", "NexusID", 87, "Nexus mod ID for updates");
            nexusID.Value = 87;

            bossGangMax = Config.Bind<int>("Options", "BossGangMax", 4, "Maximum number of succubi surrounding a boss.");
            bossGangChance = Config.Bind<float>("Options", "BossGangChance", 0.3f, "Chance of boss having succubi gang (0.0 to 1.0).");
            replaceOrdinaryChance = Config.Bind<float>("Options", "ReplaceOrdinaryChance", 0.1f, "Chance of a succubus replacing an ordinary enemy (0.0 to 1.0).");
            gearEnchantmentChance = Config.Bind<float>("Options", "GearEnchantmentChance", 0.5f, "Chance of each piece of a succubus' gear being enchanted (0.0 to 1.0).");
            succubusName = Config.Bind<string>("Options", "SuccubusName", "Enemy Succubus", "Name of enemy succubi.");
            lootLevel = Config.Bind<Rarity>("Options", "LootLevel", Rarity.three, "Loot level of random succubus loot.");
            dropEquipment = Config.Bind<bool>("Options", "DropEquipment", true, "Cause succubi to drop their equipment on death.");

            InvokeRepeating("DestroySuccubi", 5f, 5f);


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(EnemySpawner), "InstantiateEnemy")]
        public static class EnemySpawner_InstantiateEnemy_Patch
        {
            public static void Postfix(EnemySpawner __instance, ref Transform __result)
            {
                if (!modEnabled.Value || __result.GetComponent<Monster>()?.isElite == true)
                    return;

                if (TrySpawnOrdinary(__result))
                {
                    Global.code.enemies.RemoveItem(__result);
                    __result = null;
                }
            }
        }
        [HarmonyPatch(typeof(SkirmishSpawner), "InstantiateEnemy")]
        public static class SkirmishSpawner_InstantiateEnemy_Patch
        {
            public static void Postfix(EnemySpawner __instance, ref Transform __result)
            {
                if (!modEnabled.Value)
                    return;
                Transform s = TrySpawnOrdinary(__result);
                if (s)
                {
                    Global.code.enemies.RemoveItem(__result);
                    __result = null;
                    s.GetComponent<Companion>().charge = true;
                }
            }
        }
        [HarmonyPatch(typeof(Weapon), "DealDamage")]
        public static class Weapon_DealDamage_Patch
        {
            public static bool Prefix(Weapon __instance, Bodypart bodypart, float multiplier, bool playStaggerAnimation)
            {
                if (!modEnabled.Value || __instance.GetComponent<Item>().owner.GetComponent<ID>().monster || __instance.GetComponent<Item>().owner.GetComponent<ID>().player || __instance.GetComponent<Item>().owner.GetComponent<ID>().isFriendly)
                    return true;

                ID component = __instance.GetComponent<Item>().owner.GetComponent<ID>();
                ID component2 = bodypart.root.GetComponent<ID>();

                float num = component.damage;
                float num2 = 0f;
                switch (PMC_Setting.code.CurDifficulty)
                {
                    case Difficulty.Easy:
                        num2 = 0.5f;
                        break;
                    case Difficulty.Normal:
                        num2 = 0.7f;
                        break;
                    case Difficulty.Hard:
                        num2 = 1f;
                        break;
                    case Difficulty.VerHard:
                        num2 = 2f;
                        break;
                    case Difficulty.Insane:
                        num2 = 4f;
                        break;
                }
                num *= Global.code.dynamicDifficultyModifier;
                num *= num2;

                if (num <= 0f)
                {
                    num = Random.Range(1, 3);
                }

                num -= component2.defence;
                num *= multiplier;
                if (num < 0f)
                {
                    num = Random.Range(0, 5);
                }
                component2.AddHealth(-num, component.transform);
                component2.AddBalance(-num);

                return false;

            }
        }
        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.Die))]
        public static class CharacterCustomization_Die_Patch
        {
            public static bool Prefix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || __instance.GetComponent<ID>().isFriendly || __instance._Player)
                    return true;
                if (__instance.gameObject.tag == "D")
                {
                    return false;
                }
                __instance.gameObject.tag = "D";
                RM.code.PlayOneShot(RM.code.femaleDeathes[Random.Range(0, RM.code.femaleDeathes.Length)]);
                if (__instance.GetComponent<Collider>())
                {
                    __instance.GetComponent<Collider>().enabled = false;
                }
                __instance.Invoke("ResetAnimation", 0.5f);
                if (__instance.arrowInHand)
                {
                    Destroy(__instance.arrowInHand.gameObject);
                }
                __instance.CancelPullBow();
                __instance.DisableWeapon();
                __instance.anim.SetTrigger("FireArrow");
                Global.code.enemies.RemoveItemWithName(__instance.transform.name);
                Global.code.friendlies.RemoveItem(__instance.transform);
                __instance.isPullingBow = false;
                __instance.anim.enabled = false;
                if (__instance.GetComponent<Rigidbody>())
                {
                    __instance.GetComponent<Rigidbody>().isKinematic = true;
                }
                foreach (Transform transform in __instance.bones)
                {
                    if (transform)
                    {
                        transform.GetComponent<Rigidbody>().isKinematic = false;
                        transform.GetComponent<Rigidbody>().useGravity = true;
                        transform.GetComponent<Collider>().enabled = true;
                    }
                }
                Instantiate(RM.code.bigBloodFXs[Random.Range(0, RM.code.bigBloodFXs.Length)], __instance.transform.position, __instance.transform.rotation).GetComponent<BFX_BloodSettings>().GroundHeight = __instance.transform.position.y;
                Instantiate(RM.code.bigBloodFXs[Random.Range(0, RM.code.bigBloodFXs.Length)], __instance.transform.position, __instance.transform.rotation).GetComponent<BFX_BloodSettings>().GroundHeight = __instance.transform.position.y;

                if (__instance._ID.damageSource && __instance._ID.damageSource.GetComponent<ID>())
                {
                    int num = 50;
                    num += 20 * __instance._ID.level;
                    num *= __instance._ID.level + 1;
                    __instance._ID.damageSource.GetComponent<ID>().AddExp(num);
                    Global.code.uiAchievements.AddPoint(AchievementType.totalkills, 1);
                    if (__instance._ID.damageSource.GetComponent<CharacterCustomization>()._Player && Player.code.customization.weaponInHand)
                    {
                        switch (Player.code.customization.weaponInHand.GetComponent<Weapon>().weaponType)
                        {
                            case WeaponType.onehand:
                                Global.code.uiAchievements.AddPoint(AchievementType.killwithswords, 1);
                                break;
                            case WeaponType.twohand:
                                Global.code.uiAchievements.AddPoint(AchievementType.killwithswords, 1);
                                break;
                            case WeaponType.spear:
                                Global.code.uiAchievements.AddPoint(AchievementType.killwithspears, 1);
                                break;
                            case WeaponType.onehandaxe:
                                Global.code.uiAchievements.AddPoint(AchievementType.killwithaxe, 1);
                                break;
                            case WeaponType.bow:
                                Global.code.uiAchievements.AddPoint(AchievementType.killwithbows, 1);
                                break;
                            case WeaponType.dagger:
                                Global.code.uiAchievements.AddPoint(AchievementType.killwithdaggers, 1);
                                break;
                            case WeaponType.onehandhammer:
                                Global.code.uiAchievements.AddPoint(AchievementType.killwithhammers, 1);
                                break;
                        }
                    }
                }

                Scene.code.kills++;
                if (Scene.code.enemiesLeft > 0)
                {
                    Scene.code.enemiesLeft--;
                }

                if (__instance.curCastingMagic && __instance.curCastingMagic.generatedHandfx)
                {
                    Destroy(__instance.curCastingMagic.generatedHandfx.gameObject);
                }

                __instance.GetComponent<LootDrop>().Drop();


                toDestroy.Add(__instance.gameObject);
                
                return false;
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), "Start")]
        public static class CharacterCustomization_Start_Patch
        {
            public static void Postfix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || !__instance.GetComponent<ID>())
                    return;

                if (!__instance.GetComponent<ID>().isFriendly && Global.code.friendlies.items.Contains(__instance.transform))
                {
                    Global.code.friendlies.items.Remove(__instance.transform);
                }
            }
        }
        
        [HarmonyPatch(typeof(Scene), "Start")]
        public static class Scene_Start_Patch
        {
            public static void Postfix(Scene __instance)
            {
                if (!modEnabled.Value || !__instance.boss)
                    return;

                TrySpawnGang(__instance.boss);
            }
        }
        
        [HarmonyPatch(typeof(Monster), "Awake")]
        public static class Monster_Awake_Patch
        {
            public static void Postfix(Monster __instance)
            {
                if (!modEnabled.Value || !__instance.isElite)
                    return;

                TrySpawnGang(__instance.transform);
            }
        }

        private static Transform TrySpawnOrdinary(Transform result)
        {
            if (Random.value < replaceOrdinaryChance.Value)
            {
                int idx = Random.Range(0, RM.code.allCompanions.items.Count - 1);
                Transform succubus = CreateSuccubus(result.position, idx);
                Global.code.enemies.RemoveItemWithName(result.name);
                Destroy(result.gameObject);
                return succubus;
            }
            return null;
        }
        
        private static void TrySpawnGang(Transform boss)
        {
            for (int i = 0; i < bossGangMax.Value; i++)
            {
                if (Random.value > bossGangChance.Value)
                    continue;

                Dbgl($"Adding gang succubus for boss {boss.name}");

                int idx = Random.Range(0, RM.code.allCompanions.items.Count - 1);

                CreateSuccubus(boss.position + new Vector3(Random.Range(-3f, 3f), 0, Random.Range(-3f, 3f)).normalized * 3, idx);
            }
        }

        private static Transform CreateSuccubus(Vector3 position, int idx)
        {
            Transform succubus = Utility.Instantiate(RM.code.allCompanions.items[idx]);
            if (!succubus)
                return null;
            succubus.GetComponent<NavMeshAgent>().enabled = false;
            succubus.position = position;
            succubus.eulerAngles = new Vector3(0f, Random.Range(0, 360), 0f);
            Global.code.Snap(succubus);
            succubus.GetComponent<NavMeshAgent>().enabled = true;
            succubus.name = succubusName.Value + " " + (idx + 1);
            succubus.GetComponent<ID>().isFriendly = false;
            //succubus.GetComponent<Companion>().charge = true;

            LootDrop ld = succubus.gameObject.AddComponent<LootDrop>();
            ld.rarity = lootLevel.Value;

            if (Global.code.friendlies.items.Contains(succubus))
                Global.code.friendlies.items.Remove(succubus);

            context.StartCoroutine(EquipSuccubus(succubus, Global.code.curlocation.level));
            return succubus;
        }

        private static IEnumerator EquipSuccubus(Transform succubus, int level)
        {
            int eChance = Mathf.RoundToInt(gearEnchantmentChance.Value * 100);

            while (succubus.GetComponent<ID>().level < level)
            {
                succubus.GetComponent<ID>().GetNextExp();
                succubus.GetComponent<ID>().AddExp(succubus.GetComponent<ID>().nextExp - succubus.GetComponent<ID>().curExp);
            }

            yield return new WaitForEndOfFrame();


            if (ES2.Exists("Character Presets/" + succubus.name + "/CharacterPreset.txt"))
            {
                Dbgl($"Loading Preset for {succubus.name}");
                Mainframe.code.LoadCharacterPreset(succubus.GetComponent<CharacterCustomization>(), succubus.name);
                succubus.GetComponent<CharacterCustomization>().RefreshAppearence();
            }

            succubus.GetComponent<CharacterCustomization>().weapon2 = null;

            Transform weapon = Utility.Instantiate(GetMatchLevelItem(level, RM.code.allWeapons.items.FindAll(i => i.GetComponent<Item>().slotType == SlotType.weapon && i.GetComponent<Weapon>() && i.GetComponent<Weapon>().weaponType != WeaponType.bow)));
            if (weapon)
            {
                RM.code.balancer.GetItemStats(weapon, eChance);
                succubus.GetComponent<CharacterCustomization>().AddItem(weapon, "weapon");
                if (dropEquipment.Value)
                    succubus.GetComponent<LootDrop>().sureItems.AddItem(weapon);
            }

            if(weapon && new List<WeaponType>() { WeaponType.dagger, WeaponType.onehand, WeaponType.onehandaxe, WeaponType.onehandhammer, WeaponType.onehandspear }.Contains(weapon.GetComponent<Weapon>().weaponType))
            {
                Transform shield = Utility.Instantiate(GetMatchLevelItem(level, RM.code.allWeapons.items.FindAll(i => i.GetComponent<Item>().slotType == SlotType.shield)));
                if (shield)
                {
                    succubus.GetComponent<CharacterCustomization>().AddItem(shield, "shield");
                    if (dropEquipment.Value)
                        succubus.GetComponent<LootDrop>().sureItems.AddItem(shield);
                }

            }

            List<Transform> armors = RM.code.allArmors.items.FindAll(t => t.GetComponent<Item>().slotType == SlotType.armor);
            Transform armor = Utility.Instantiate(GetMatchLevelItem(level, armors));
            if (armor)
            {
                RM.code.balancer.GetItemStats(armor, eChance);
                succubus.GetComponent<CharacterCustomization>().AddItem(armor, "armor");
                if (dropEquipment.Value)
                    succubus.GetComponent<LootDrop>().sureItems.AddItem(armor);
            }

            List<Transform> leggings = RM.code.allArmors.items.FindAll(t => t.GetComponent<Item>().slotType == SlotType.legging);
            Transform legging = Utility.Instantiate(GetMatchLevelItem(level, leggings));
            if (legging)
            {
                RM.code.balancer.GetItemStats(legging, eChance);
                succubus.GetComponent<CharacterCustomization>().AddItem(legging, "leggings");
                if (dropEquipment.Value)
                    succubus.GetComponent<LootDrop>().sureItems.AddItem(legging);
            }
            List<Transform> gloves = RM.code.allArmors.items.FindAll(t => t.GetComponent<Item>().slotType == SlotType.gloves);
            Transform glove = Utility.Instantiate(GetMatchLevelItem(level, gloves));
            if (glove)
            {
                RM.code.balancer.GetItemStats(glove, eChance);
                succubus.GetComponent<CharacterCustomization>().AddItem(glove, "gloves");
                if (dropEquipment.Value)
                    succubus.GetComponent<LootDrop>().sureItems.AddItem(glove);
            }
            List<Transform> helmets = RM.code.allArmors.items.FindAll(t => t.GetComponent<Item>().slotType == SlotType.helmet);
            Transform helmet = Utility.Instantiate(GetMatchLevelItem(level, helmets));
            if (helmet)
            {
                RM.code.balancer.GetItemStats(helmet, eChance);
                succubus.GetComponent<CharacterCustomization>().AddItem(helmet, "helmet");
                if (dropEquipment.Value)
                    succubus.GetComponent<LootDrop>().sureItems.AddItem(helmet);
            }
            List<Transform> shoes = RM.code.allArmors.items.FindAll(t => t.GetComponent<Item>().slotType == SlotType.shoes);
            Transform shoe = Utility.Instantiate(GetMatchLevelItem(level, shoes));
            if (shoe)
            {
                RM.code.balancer.GetItemStats(shoe, eChance);
                succubus.GetComponent<CharacterCustomization>().AddItem(shoe, "shoes");
                if (dropEquipment.Value)
                    succubus.GetComponent<LootDrop>().sureItems.AddItem(shoe);
            }

            AddRandomStatsAndSkills(succubus, level);

        }

        private static void AddRandomStatsAndSkills(Transform succubus, int level)
        {
            ID id = succubus.GetComponent<ID>();
            CharacterCustomization c = succubus.GetComponent<CharacterCustomization>();
            int which = Random.Range(1,4);
            switch (which)
            {
                case 1: // magic
                    id.vitality += Mathf.CeilToInt(level / 6f);
                    id.agility += Mathf.CeilToInt(level / 6f);
                    id.power += Mathf.CeilToInt(level / 2f);
                    id.strength += Mathf.CeilToInt(level / 6f);
                    c.fireball += Mathf.Min(Global.code.uiCharacter.fireball.maxPoints, Mathf.CeilToInt(level * 2 / 3f ));
                    c.healaura += Mathf.Min(Global.code.uiCharacter.healaura.maxPoints, Mathf.CeilToInt(level * 2 / 3f ));
                    c.frostbite += Mathf.Min(Global.code.uiCharacter.frostbite.maxPoints, Mathf.CeilToInt(level * 2 / 3f ));
                    break;
                case 2: // defence
                    id.vitality += Mathf.CeilToInt(level / 3f);
                    id.agility += Mathf.CeilToInt(level / 6f);
                    id.power += Mathf.CeilToInt(level / 6f);
                    id.strength += Mathf.CeilToInt(level / 3f);
                    c.elementalresistence += Mathf.Min(Global.code.uiCharacter.elementalresistence.maxPoints, Mathf.CeilToInt(level * 2 / 5f));
                    c.ironbody += Mathf.Min(Global.code.uiCharacter.ironbody.maxPoints, Mathf.CeilToInt(level * 2 / 5f));
                    c.hardenedskin += Mathf.Min(Global.code.uiCharacter.hardenedskin.maxPoints, Mathf.CeilToInt(level * 2 / 5f));
                    c.ignorpain += Mathf.Min(Global.code.uiCharacter.ignorpain.maxPoints, Mathf.CeilToInt(level * 2 / 5f));
                    c.blocking += Mathf.Min(Global.code.uiCharacter.blocking.maxPoints, Mathf.CeilToInt(level * 2 / 5f));
                    break;
                case 3: // offence
                    id.vitality += Mathf.CeilToInt(level / 6f);
                    id.agility += Mathf.CeilToInt(level / 3f);
                    id.power += Mathf.CeilToInt(level / 6f);
                    id.strength += Mathf.CeilToInt(level / 3f);
                    c.atheletic += Mathf.Min(Global.code.uiCharacter.atheletic.maxPoints, Mathf.CeilToInt(level * 2 / 4f));
                    c.recreation += Mathf.Min(Global.code.uiCharacter.recreation.maxPoints, Mathf.CeilToInt(level * 2 / 4f));
                    c.phatomslashes += Mathf.Min(Global.code.uiCharacter.phatomslashes.maxPoints, Mathf.CeilToInt(level * 2 / 4f));
                    c.painfulscream += Mathf.Min(Global.code.uiCharacter.painfulscream.maxPoints, Mathf.CeilToInt(level * 2 / 4f));
                    break;
                case 4: //balanced
                    id.vitality += Mathf.CeilToInt(level / 4f);
                    id.agility += Mathf.CeilToInt(level / 4f);
                    id.power += Mathf.CeilToInt(level / 4f);
                    id.strength += Mathf.CeilToInt(level / 4f);

                    c.fireball += Mathf.Min(Global.code.uiCharacter.fireball.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    c.healaura += Mathf.Min(Global.code.uiCharacter.healaura.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    c.frostbite += Mathf.Min(Global.code.uiCharacter.frostbite.maxPoints, Mathf.CeilToInt(level * 2 / 8f));

                    c.elementalresistence += Mathf.Min(Global.code.uiCharacter.elementalresistence.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    c.ironbody += Mathf.Min(Global.code.uiCharacter.ironbody.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    c.hardenedskin += Mathf.Min(Global.code.uiCharacter.hardenedskin.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    c.ignorpain += Mathf.Min(Global.code.uiCharacter.ignorpain.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    c.blocking += Mathf.Min(Global.code.uiCharacter.blocking.maxPoints, Mathf.CeilToInt(level * 2 / 8f));

                    c.atheletic += Mathf.Min(Global.code.uiCharacter.atheletic.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    c.recreation += Mathf.Min(Global.code.uiCharacter.recreation.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    c.phatomslashes += Mathf.Min(Global.code.uiCharacter.phatomslashes.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    c.painfulscream += Mathf.Min(Global.code.uiCharacter.painfulscream.maxPoints, Mathf.CeilToInt(level * 2 / 8f));
                    break;
            }
            Weapon weapon = c.weapon?.GetComponent<Weapon>();
            if (weapon)
            {
                switch (weapon.weaponType)
                {
                    case WeaponType.onehand:
                        c.swordproficiency += Mathf.Min(Global.code.uiCharacter.swordproficiency.maxPoints, Mathf.CeilToInt(level * 2 / 6f));
                        break;
                    case WeaponType.twohand:
                        c.swordproficiency += Mathf.Min(Global.code.uiCharacter.swordproficiency.maxPoints, Mathf.CeilToInt(level * 2 / 6f));
                        break;
                    case WeaponType.throwingaxe:
                        c.axeproficiency += Mathf.Min(Global.code.uiCharacter.axeproficiency.maxPoints, Mathf.CeilToInt(level * 2 / 6f));
                        break;
                    case WeaponType.twohandaxe:
                        c.axeproficiency += Mathf.Min(Global.code.uiCharacter.axeproficiency.maxPoints, Mathf.CeilToInt(level * 2 / 6f));
                        break;
                    case WeaponType.onehandaxe:
                        c.axeproficiency += Mathf.Min(Global.code.uiCharacter.axeproficiency.maxPoints, Mathf.CeilToInt(level * 2 / 6f));
                        break;
                    case WeaponType.bow:
                        c.bowproficiency += Mathf.Min(Global.code.uiCharacter.bowproficiency.maxPoints, Mathf.CeilToInt(level * 2 / 6f));
                        break;
                    case WeaponType.dagger:
                        c.daggerproficiency += Mathf.Min(Global.code.uiCharacter.daggerproficiency.maxPoints, Mathf.CeilToInt(level * 2 / 6f));
                        break;
                    case WeaponType.onehandhammer:
                        c.maceproficiency += Mathf.Min(Global.code.uiCharacter.maceproficiency.maxPoints, Mathf.CeilToInt(level * 2 / 6f));
                        break;
                }
            }

        }

        public static Transform GetMatchLevelItem(int level, List<Transform> list)
        {
            if (list.Count <= 0)
            {
                return null;
            }
            List<Transform> list2 = new List<Transform>();
            foreach (Transform transform in list)
            {
                Item component = transform.GetComponent<Item>();
                if (component)
                {
                    if (level > 13)
                    {
                        if (component.level > 10)
                        {
                            list2.Add(transform);
                        }
                    }
                    else if (component.level <= level + 2 && component.level > level - 3)
                    {
                        list2.Add(transform);
                    }
                }
                else
                {
                    Debug.LogError("wrong item " + transform.name);
                }
            }
            if (list2.Count <= 0)
            {
                return null;
            }
            return list2[Random.Range(0, list2.Count - 1)];
        }

        public void DestroySuccubi()
        {
            if (toDestroy.Count > 0)
            {
                for (int i = toDestroy.Count - 1; i >= 0; i--)
                {
                    if (Vector3.Distance(toDestroy[i].transform.position, Player.code.transform.position) > 10f)
                    {
                        Destroy(toDestroy[i]);
                        toDestroy.RemoveAt(i);
                    }
                }
            }
        }

    }
}
