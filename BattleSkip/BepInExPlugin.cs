using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace BattleSkip
{
    [BepInPlugin("aedenthorn.BattleSkip", "Battle Skip", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> endDayOnSkip;
        public static ConfigEntry<bool> useAverageLevel;
        public static ConfigEntry<float> reqLevelDiffFactor;
        public static ConfigEntry<string> skipButtonText;
        public static ConfigEntry<int> nexusID;

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
            
            endDayOnSkip = Config.Bind<bool>("Options", "EndDayOnSkip", false, "If true, will end the day each time you skip the battle, just as though you had completed it manually.");
            useAverageLevel = Config.Bind<bool>("Options", "IncludeCompanionLevels", true, "If true, will take the average of levels for all party members. If false, will just consider player's level.");

            reqLevelDiffFactor = Config.Bind<float>("Options", "ReqLevelDiffFactor", 0.5f, "Difference as fraction of player level required between player level and location level in order to skip, rounded up (e.g. 0.25 = >25% level difference, i.e. enemy's level is less than 75% of the player's level).");
            skipButtonText = Config.Bind<string>("Options", "SkipButtonText", "Skip", "Text to show on skip button.");

            nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(UIWorldMap), nameof(UIWorldMap.Close))]
        static class UIWorldMap_Close_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value || !Global.code.curlocation || !Global.code.currentHome || Global.code.curlocation == Global.code.currentHome)
                    return;
                context.StartCoroutine(CheckLoadHome());
            }
        }

        private static IEnumerator CheckLoadHome()
        {
            yield return new WaitForEndOfFrame();

            if(!Global.code.onGUI)
                Mainframe.code.uILoading.OpenLoading(Global.code.currentHome.transform);

            yield break;
        }


        [HarmonyPatch(typeof(UIResult), nameof(UIResult.ButtonOpenWorldMap))]
        static class UIResult_ButtonOpenWorldMap_Patch
        {
            static void Prefix(UIResult __instance)
            {
                if (!modEnabled.Value)
                    return;
                Global.code.curlocation = Global.code.currentHome;
            }
        }

        [HarmonyPatch(typeof(UICombatParty), nameof(UICombatParty.Open))]
        static class UICombatParty_Open_Patch
        {
            static void Postfix(UICombatParty __instance)
            {
                if (!modEnabled.Value)
                    return;
                Location loc = Global.code.uiWorldMap.focusedLocation;

                var oldSkip = __instance.gameObject.transform.Find("button down group").Find("btn skip");

                if (loc.locationType != LocationType.fieldarmy)
                {
                    Dbgl($"Not a field army. Location type: {loc.locationType}");
                    if (oldSkip)
                        Destroy(oldSkip.gameObject);
                    return;
                }

                int level = Player.code._ID.level;
                if (useAverageLevel.Value)
                {
                    foreach (Transform companion in Global.code.playerCombatParty.items)
                    {
                        level += companion.GetComponent<ID>().level;
                    }
                    level = (int)Mathf.Round(level / (float)Global.code.playerCombatParty.items.Count + 1);
                }

                if (reqLevelDiffFactor.Value < 1 && level - loc.level < level * reqLevelDiffFactor.Value)
                {
                    Dbgl($"Level too low to skip: {level} vs. {loc.level}");
                    if (oldSkip)
                        Destroy(oldSkip.gameObject);
                    return;
                }
                Dbgl($"Player can skip: {level} vs. {loc.level}");
                if (oldSkip)
                    return;

                Transform attack = __instance.gameObject.transform.Find("button down group").Find("btn attack");
                if (!attack)
                {
                    Dbgl("attack button not found! aborting");
                    return;
                }
                Transform skip = Instantiate(attack, attack.parent);
                skip.name = "btn skip";

                __instance.StartCoroutine(AddButton(attack, skip));

            }
        }
        private static IEnumerator AddButton(Transform attack, Transform skip)
        {
            yield return new WaitForEndOfFrame();

            //skip.GetComponent<RectTransform>().anchoredPosition = new Vector2(attack.GetComponent<RectTransform>().anchoredPosition.x, attack.GetComponent<RectTransform>().anchoredPosition.y + attack.GetComponent<RectTransform>().rect.height + 5);
            skip.GetComponentInChildren<Text>().text = skipButtonText.Value;
            Button btn = skip.GetComponent<Button>();
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(SkipBattle);

            yield break;
        }
        private static void SkipBattle()
        {
            Global.code.uiCombatParty.gameObject.SetActive(false);
            Global.code.QuitInteraction();
            Global.code.uiCombatParty.scene.SetActive(false);
            if (Global.code.curlocation.locationType == LocationType.home)
            {
                Global.code.currentHome = Global.code.curlocation;
            }
            AccessTools.Method(typeof(UICombatParty), "ClearDisplay").Invoke(Global.code.uiCombatParty, null);
            Global.code.curlocation = Global.code.uiWorldMap.focusedLocation;
            RM.code.PlayOneShot(Global.code.uiCombatParty.sndAttack);

            Global.code.CloseAllUI();
            Global.code.uiWorldMap.Close();

            if(endDayOnSkip.Value)
                Global.code.EndDay();

            int exp = GetExperience();
            Dbgl($"Total experience from encounter: {exp}");
            int party = 1 + Global.code.playerCombatParty.items.Count;
            int perExp = exp / party;
            Player.code._ID.AddExp(perExp);
            Dbgl($"Gave {perExp} to player");

            foreach (Transform companion in Global.code.playerCombatParty.items)
            {
                companion.GetComponent<ID>().AddExp(perExp);
                Dbgl($"Gave {perExp} to {companion.GetComponent<ID>().name}");
            }

            Scene.code.BattleFinished();
            /*
            Scene.code.battlefinished = true;
            Scene.code.CancelInvoke("CheckCombat");
            Global.code.friendlies.ArrangeItems();
            Global.code.enemies.ArrangeItems();
            Global.code.companions.ArrangeItems();
            Global.code.playerCombatParty.ArrangeItems();
            */
            Scene.code.won = true;

            if (Global.code.dynamicDifficultyModifier < 1.3f)
            {
                Global.code.dynamicDifficultyModifier += 0.1f;
                Dbgl($"Difficulty modifier increased to {Global.code.dynamicDifficultyModifier}");
            }

            Global.code.curlocation.isCleared = true;
            Global.code.curlocation.gameObject.SetActive(false);
            Global.code.curlocation.GetComponent<Location>().disableMapIcon = true;
            Global.code.uiAchievements.AddPoint(AchievementType.skirmishwon, 1);
            //Global.code.uiCombat.ShowHeader(Localization.GetContent("Scene_2", Array.Empty<object>()));
            //Global.code.uiCombat.blinkingPrompt.text = Localization.GetContent("Global_2", Array.Empty<object>());
            Global.code.uiResult.rewardClaimed = false;
            Global.code.uiResult.Open();
            Scene.code.won = false;
        }

        private static int GetExperience()
        {
            int totalExp = 0;
            ArmyPreset ap = Global.code.uiWorldMap.focusedLocation.GetComponent<ArmyPreset>();
            if (ap == null)
            {
                Dbgl("No army preset");
                return 0;
            }
            List<Transform> enemies = new List<Transform>();
            if (ap.bosses != null)
                enemies.AddRange(ap.bosses);
            if (ap.minibosses != null)
                enemies.AddRange(ap.minibosses);
            if (ap.superuniques != null)
                enemies.AddRange(ap.superuniques);
            if (ap.rangedSquadPresets != null)
            {
                foreach (SquadPreset sp in ap.rangedSquadPresets)
                {
                    if (sp == null)
                        continue;

                    Dbgl($"Got {sp.units.Length} ranged units");
                    enemies.AddRange(sp.units);
                }

            }
            if (ap.normalSquadPresets != null)
            {
                foreach (SquadPreset sp in ap.normalSquadPresets)
                {
                    if (sp == null)
                        continue;
                    Dbgl($"Got {sp.units.Length} normal units");
                    enemies.AddRange(sp.units);
                }
            }
            if (ap.hardSquadPresets != null)
            {
                foreach (SquadPreset sp in ap.hardSquadPresets)
                {
                    if (sp == null)
                        continue;
                    Dbgl($"Got {sp.units.Length} hard units");
                    enemies.AddRange(sp.units);
                }
            }

            Scene.code.kills = enemies.Count;
            Scene.code.time = 0;
            Scene.code.loses = 0;

            foreach (Transform t in enemies)
            {
                if (!t)
                {
                    Dbgl($"transform not found");
                    continue;
                }

                Monster monster = t.GetComponent<Monster>();

                if (!monster)
                {
                    Dbgl($"Monster not found");
                    continue;
                }

                ID id = t.GetComponent<ID>();

                Dbgl($"killed monster {id.name}");

                int exp = 50;
                exp += 20 * id.level;
                exp *= (int)(monster.enemyRarity + 1);
                totalExp += exp;

                Dbgl($"monster xp {exp}");

                // etc.

                Global.code.uiAchievements.AddPoint(AchievementType.totalkills, 1);

                LootDrop ld = t.GetComponent<LootDrop>();

                if (!ld)
                {
                    Dbgl($"loot drop not found");
                    continue;
                }

                ld.Start();

                if (ld.sureItems != null && ld.sureItems.Length != 0)
                {
                    Dbgl($"has {ld.sureItems.Length} sure items");
                    foreach (Transform transform in ld.sureItems)
                    {
                        if (transform)
                        {
                            TryGetItem(ld, transform);
                        }
                    }
                }
                else
                {
                    Dbgl("Getting random loot");

                    List<Transform> matchLevelWeapons = ld.GetMatchLevelItems(RM.code.allWeapons.items);
                    List<Transform> matchLevelArmors = ld.GetMatchLevelItems(RM.code.allArmors.items);

                    Dbgl($"counts: {RM.code.allWeapons.items.Count} {RM.code.allArmors.items.Count} {matchLevelWeapons?.Count} {matchLevelArmors?.Count}");

                    for (int i = 0; i < ld.maxAmount; i++)
                    {
                        int num = Random.Range(0, 100);
                        if (num < 7)
                        {
                            if (matchLevelWeapons?.Count > 0)
                            {
                                TryGetItem(ld, matchLevelWeapons[Random.Range(0, matchLevelWeapons.Count)]);
                            }
                        }
                        else if (num < 15)
                        {
                            if (matchLevelArmors?.Count > 0)
                            {
                                TryGetItem(ld, matchLevelArmors[Random.Range(0, matchLevelArmors.Count)]);
                            }
                        }
                        else if (num < 25)
                        {
                            if (RM.code.allPotions.items.Count > 0)
                            {
                                TryGetItem(ld, RM.code.allPotions.items[Random.Range(0, RM.code.allPotions.items.Count)]);
                            }
                        }
                        else if (num < 27)
                        {
                            if (RM.code.allAmmos.items.Count > 0)
                            {
                                TryGetItem(ld, RM.code.allAmmos.items[Random.Range(0, RM.code.allAmmos.items.Count)]);
                            }
                        }
                        else if (num < 32)
                        {
                            if (RM.code.allTreasures.items.Count > 0)
                            {
                                TryGetItem(ld, RM.code.allTreasures.items[Random.Range(0, RM.code.allTreasures.items.Count)]);
                            }
                        }
                        else if (num < 36 && RM.code.allMiscs.items.Count > 0)
                        {
                            TryGetItem(ld, RM.code.allMiscs.items[Random.Range(0, RM.code.allMiscs.items.Count)]);
                        }
                    }

                    //Dbgl("Getting random gold 1");

                    for (int j = 0; j < ld.maxAmount; j++)
                    {
                        if (Random.Range(0, 100) < 20 + Player.code.customization.goldenhand * 5)
                        {
                            GetGold(ld, id);
                        }
                    }

                    //Dbgl("Getting random lingerie");

                    int numLingerieTries = 0;
                    while (numLingerieTries < ld.maxAmount * 0.5f)
                    {
                        if (Random.Range(0, 100) < 2 && RM.code.allLingeries.items.Count > 0)
                        {
                            TryGetItem(ld, RM.code.allLingeries.items[Random.Range(0, RM.code.allLingeries.items.Count)]);
                        }
                        numLingerieTries++;
                    }

                    //Dbgl("Getting random gold 2");

                    if (Random.Range(0, 100) < 3)
                    {
                        int amount = Random.Range(10, 17);
                        for (int k = 0; k < amount; k++)
                        {
                            GetGold(ld, id);
                        }
                    }

                    //Dbgl("Getting random crystals");

                    int crystalChance = 2;
                    int crystals = 1;
                    switch (ld.rarity)
                    {
                        case Rarity.one:
                            crystalChance = 2;
                            crystals = 1;
                            break;
                        case Rarity.two:
                            crystalChance = 5;
                            crystals = 3;
                            break;
                        case Rarity.three:
                            crystalChance = 8;
                            crystals = 5;
                            break;
                        case Rarity.four:
                            crystalChance = 15;
                            crystals = 8;
                            break;
                        case Rarity.five:
                            crystalChance = 20;
                            crystals = 10;
                            break;
                        case Rarity.six:
                            crystalChance = 70;
                            crystals = 20;
                            break;
                    }
                    if (Random.Range(0, 100) < crystalChance + Player.code.customization.crystalhunter * 3)
                    {
                        //Transform crystalItem = Utility.Instantiate(RM.code.crystal);
                        //crystalItem.GetComponent<Item>().amount = amount;
                        //Dbgl($"Got {crystals} crystals");
                        Global.code.AddCrystals(crystals);
                    }
                }

            }
            return totalExp;
        }

        private static void GetGold(LootDrop ld, ID id)
        {
            int gold = 0;
            switch (ld.rarity)
            {
                case Rarity.one:
                    gold = 3;
                    break;
                case Rarity.two:
                    gold = 5;
                    break;
                case Rarity.three:
                    gold = 8;
                    break;
                case Rarity.four:
                    gold = 9;
                    break;
                case Rarity.five:
                    gold = 12;
                    break;
                case Rarity.six:
                    gold = 20;
                    break;
            }
            gold += (int)(id.level * 0.25f);
            gold = (int)(gold * Random.Range(0.7f, 1f));
            //Transform goldItem = Utility.Instantiate(RM.code.gold);
            //goldItem.GetComponent<Item>().amount = gold;
            //Dbgl($"Got {gold} gold, rarity {ld.rarity}, level {id.level}");
            Global.code.AddGold(gold);
        }

        private static void TryGetItem(LootDrop ld, Transform prefab)
        {

            if (!prefab || (float)Random.Range(0, 100) > prefab.GetComponent<Item>().GetDropRate(ld.rarity) + (Player.code.customization.scavenger * 2))
                return;

            Transform item = Utility.Instantiate(prefab);
            RM.code.balancer.GetItemStats(item, 0);

            //AccessTools.Method(typeof(Item), "Start").Invoke(item.GetComponent<Item>(), null);
            if (Player.code.customization.storage.AutoAddItem(item, true, true, true))
            {
                Dbgl($"Added item {item.GetComponent<Item>().name} ({item.GetComponent<Item>().amount}) to player inventory");
                return;
            }
            if (Global.code.playerCombatParty.items.Count > 0)
            {
                foreach (Transform transform in Global.code.playerCombatParty.items)
                {
                    if (transform && transform.GetComponent<CharacterCustomization>().storage.AutoAddItem(item, true, true, true))
                    {
                        Dbgl($"Added item {item.GetComponent<Item>().name} ({item.GetComponent<Item>().amount}) to {transform.GetComponent<CharacterCustomization>()._ID.name} inventory");
                        return;
                    }
                }
            }

            Dbgl($"No room for {item.GetComponent<Item>().name}  ({item.GetComponent<Item>().amount}), discarded");
            Destroy(item.gameObject);
        }
    }
}
