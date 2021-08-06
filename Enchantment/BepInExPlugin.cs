using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Enchantment
{
    [BepInPlugin("aedenthorn.Enchantment", "Enchantment", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> baseSuccessChance;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> successText;
        public static ConfigEntry<string> failText;
        public static ConfigEntry<string> noEnchantmentText;
        public static ConfigEntry<string> alreadyEnchantedText;

        public static BepInExPlugin context;

        private static EquipmentSlot currentSlot;
        private static ItemIcon currentIcon;

        private static SlotType[] equipmentSlotTypes = new SlotType[] { SlotType.armor, SlotType.weapon, SlotType.shield, SlotType.gloves, SlotType.helmet, SlotType.legging, SlotType.shoes };

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 53, "Nexus mod ID for updates");


            baseSuccessChance = Config.Bind<float>("Options", "BaseSuccessChance", 0.1f, "Percent chance of enchantment success per gold price of enchanting object.");
            hotKey = Config.Bind<string>("Options", "HotKey", "e", "Key to initiate enchantment. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            
            successText = Config.Bind<string>("Text", "SuccessText", "Success! Added enchantment {0}.", "Text to show when enchantment is a success.");
            failText = Config.Bind<string>("Text", "FailText", "Enchantment failed ({0}% chance).", "Text to show when enchantment fails.");
            noEnchantmentText = Config.Bind<string>("Text", "NoEnchantmentText", "No enchantments available for this item.", "Text to show when no enchantments are available.");
            alreadyEnchantedText = Config.Bind<string>("Text", "AlreadyEnchantedText", "Item already fully enchanted.", "Text to show when no item already fully enchanted.");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(UIInventory), "Update")]
        static class UIInventory_Update_Patch
        {
            static void Postfix(UIInventory __instance)
            {
                if (!modEnabled.Value || 
                    !Global.code.selectedItem || 
                    Global.code.selectedItem.GetComponent<Item>()?.itemType != ItemType.treasure || 
                    (
                        (!currentSlot || !equipmentSlotTypes.Contains(currentSlot.slotType)) && 
                        (!currentIcon?.item || !equipmentSlotTypes.Contains(currentIcon.item.GetComponent<Item>().slotType))
                    )
                )
                {
                    return;
                }

                if (AedenthornUtils.CheckKeyDown(hotKey.Value))
                {
                    Enchant();
                }
            }
        }
        private static void Enchant()
        {
            Item item = currentSlot ? currentSlot.item.GetComponent<Item>() : currentIcon.item.GetComponent<Item>();

            if (item.prefix && item.surfix)
            {
                Dbgl($"Already fully enchanted.");
                Global.code.uiCombat.ShowHeader(alreadyEnchantedText.Value);
                return;
            }

            List<Transform> prefixes = RM.code.balancer.GetMatchLevelAffixes(item, item.slotType == SlotType.weapon ? RM.code.weaponPrefixes.items : RM.code.armorPrefixes.items);
            List<Transform> suffixes = RM.code.balancer.GetMatchLevelAffixes(item, item.slotType == SlotType.weapon ? RM.code.weaponSurfixes.items : RM.code.armorSurfixes.items);

            if(prefixes.Count == 0 && suffixes.Count == 0 || (item.prefix && suffixes.Count == 0) || (item.surfix && prefixes.Count == 0))
            {
                Dbgl($"no enchantment available");
                Global.code.uiCombat.ShowHeader(noEnchantmentText.Value);
                return;
            }

            float chance = Global.code.selectedItem.GetComponent<Item>().cost * baseSuccessChance.Value;
            Dbgl($"Item cost {item.cost}, Chance of success {chance}%");
            if(Random.value < chance / 100f)
            {
                Item enchantment;
                Dbgl($"Success!");

                if (prefixes.Count == 0)
                {
                    item.surfix = suffixes[Random.Range(0, suffixes.Count - 1)].GetComponent<Item>();
                    enchantment = item.surfix;
                }
                else if (suffixes.Count == 0)
                {
                    item.prefix = prefixes[Random.Range(0, prefixes.Count - 1)].GetComponent<Item>();
                    enchantment = item.prefix;
                }
                else if (!item.prefix  && !item.surfix)
                {
                    if(Random.value < 0.5f)
                    {
                        item.prefix = prefixes[Random.Range(0, prefixes.Count - 1)].GetComponent<Item>();
                        enchantment = item.prefix;
                    }
                    else
                    {
                        item.surfix = suffixes[Random.Range(0, suffixes.Count - 1)].GetComponent<Item>();
                        enchantment = item.surfix;
                    }

                }
                else if (item.prefix)
                {
                    item.surfix = suffixes[Random.Range(0, suffixes.Count - 1)].GetComponent<Item>();
                    enchantment = item.surfix;
                }
                else
                {
                    item.prefix = prefixes[Random.Range(0, prefixes.Count - 1)].GetComponent<Item>();
                    enchantment = item.prefix;
                }

                RM.code.balancer.GetAffixStats(enchantment);
                AccessTools.Method(typeof(Balancer), "ApplyAffixStats").Invoke(RM.code.balancer, new object[] { item, enchantment });

                Destroy(Global.code.selectedItem.gameObject);
                Global.code.selectedItem = null;

                Global.code.uiCombat.ShowHeader(string.Format(successText.Value, enchantment.name));
                RM.code.PlayOneShot(Player.code.customization.skillHealingAura.sfxActivate);
                Global.code.uiCombat.ShowInfo(item);
            }
            else
            {
                Destroy(Global.code.selectedItem.gameObject);
                Global.code.selectedItem = null;

                Global.code.uiCombat.ShowHeader(string.Format(failText.Value, chance));
                RM.code.PlayOneShot(Player.code.customization.skillFireball.sfxActivate);
            }
        }

        [HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.OnPointerEnter))]
        static class EquipmentSlot_OnPointerEnter_Patch
        {
            static void Postfix(EquipmentSlot __instance)
            {
                if (!modEnabled.Value)
                    return;
                currentSlot = __instance;
            }
        }
        [HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.OnPointerExit))]
        static class EquipmentSlot_OnPointerExit_Patch
        {

            static void Postfix(EquipmentSlot __instance)
            {
                if (!modEnabled.Value)
                    return;
                currentSlot = null;
            }
        }

        [HarmonyPatch(typeof(ItemIcon), nameof(ItemIcon.OnPointerEnter))]
        static class ItemIcon_OnPointerEnter_Patch
        {

            static void Postfix(ItemIcon __instance)
            {
                if (!modEnabled.Value)
                    return;
                currentIcon = __instance;
            }
        }
        [HarmonyPatch(typeof(ItemIcon), nameof(ItemIcon.OnPointerExit), new Type[] { })]
        static class ItemIcon_OnPointerExit_Patch
        {

            static void Postfix(EquipmentSlot __instance)
            {
                if (!modEnabled.Value)
                    return;
                currentIcon = null;
            }
        }

    }
}
