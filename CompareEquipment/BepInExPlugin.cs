using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CompareEquipment
{
    [BepInPlugin("aedenthorn.CompareEquipment", "Compare Equipment", "0.3.2")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<string> equippedText;
        public static ConfigEntry<string> weaponModKey;

        public static BepInExPlugin context;
        
        public static bool showingCompare = false;
        public static Transform comparePanel;
        public static Transform compareHolder;
        public static Text compareName;
        public static Text comparePrice;
        public static bool compareAsGoods = false;

        private static Item curItem;
        private static Item comparedItem;
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
            nexusID = Config.Bind<int>("General", "NexusID", 43, "Nexus mod ID for updates");
            equippedText = Config.Bind<string>("Options", "EquippedText", "<color=#FFFF00>Equipped: </color>", "Text to show before equipped item's name.");
            weaponModKey = Config.Bind<string>("Options", "WeaponModKey", "left shift", "Modifier key to compare to off-hand equipped weapon. Use https://docs.unity3d.com/Manual/class-InputManager.html");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(UICombat), "Update")]
        static class UICombat_Update_Patch
        {
            static void Postfix(UICombat __instance)
            {
                if (!modEnabled.Value || !__instance.descriptionsPanel?.gameObject.activeSelf)
                    return;

                if (AedenthornUtils.CheckKeyDown(weaponModKey.Value) || AedenthornUtils.CheckKeyUp(weaponModKey.Value))
                    __instance.ShowInfo(curItem);
            }
        }
        [HarmonyPatch(typeof(UICombat), nameof(UICombat.HideInfo))]
        static class HideInfo_Patch
        {
            static bool Prefix(UICombat __instance)
            {
                if (!modEnabled.Value || !Global.code.selectedItem)
                    return true;
                __instance.ShowInfo(Global.code.selectedItem.GetComponent<Item>());
                return false;
            }
        }
        [HarmonyPatch(typeof(UIInventory), nameof(UIInventory.Open))]
        static class UIInventory_Open_Patch
        {
            static void Prefix()
            {
                if (!modEnabled.Value || !Global.code.selectedItem)
                    return;
                Global.code.uiCombat.ShowInfo(Global.code.selectedItem.GetComponent<Item>());
            }
        }
        [HarmonyPatch(typeof(UICombat), nameof(UICombat.ShowInfo))]
        static class ShowInfo_Patch
        {
            static void Prefix(UICombat __instance, ref Item item)
            {
                if (!modEnabled.Value || !Global.code.uiInventory.gameObject.activeSelf)
                    return;
                if (showingCompare)
                {
                    __instance.descriptionHolder = compareHolder;
                    __instance.lineName = compareName;
                    __instance.linePrice = comparePrice;
                    return;
                }

                if (Global.code.selectedItem && Global.code.selectedItem.GetComponent<Item>().slotType == item.slotType && equipmentSlotTypes.Contains(item.slotType))
                {
                    comparedItem = item;
                    item = Global.code.selectedItem.GetComponent<Item>();
                }
                curItem = item;
            }
            static void Postfix(UICombat __instance, Item item)
            {
                if (!modEnabled.Value || !Global.code.uiInventory.gameObject.activeSelf)
                {
                    comparedItem = null;
                    curItem = null;
                    comparePanel.gameObject.SetActive(false);
                    return;
                }

                if (__instance.descriptionsPanel.transform.Find("compared") == null)
                {
                    Dbgl("Creating compare game objects");
                    comparePanel = Instantiate(__instance.descriptionsPanel.transform.Find("panel"), __instance.descriptionsPanel.transform);
                    comparePanel.name = "compared";
                    compareHolder = comparePanel.Find("group");
                    compareName = comparePanel.Find("name (1)").GetComponent<Text>();
                    comparePrice = comparePanel.Find("line price").GetComponent<Text>();
                    compareName.supportRichText = true;
                }

                if (showingCompare)
                {
                    if(curItem != Global.code.selectedItem?.GetComponent<Item>() || GetComparedItem(item) == item)
                        compareName.text = equippedText.Value + compareName.text;
                    
                    if(compareAsGoods)
                        __instance.linePrice.text = Localization.GetContent("Gold", new object[0]) + " " + item.cost.ToString();
                    __instance.descriptionHolder = __instance.descriptionsPanel.transform.Find("panel/group");
                    __instance.lineName = __instance.descriptionsPanel.transform.Find("panel/name (1)").GetComponent<Text>();
                    __instance.linePrice = __instance.descriptionsPanel.transform.Find("panel/line price").GetComponent<Text>();
                    return;
                }

                Item compared;
                if (comparedItem)
                {
                    compared = comparedItem;
                    comparedItem = null;
                }
                else
                    compared = GetComparedItem(item);

                if (compared == null || compared == item)
                {
                    comparedItem = null;
                    comparePanel.gameObject.SetActive(false);
                    return;

                }

                showingCompare = true;
                compareAsGoods = item.isGoods;
                comparePanel.gameObject.SetActive(true);
                __instance.ShowInfo(compared);
                comparePanel.GetComponent<RectTransform>().anchoredPosition = __instance.descriptionsPanel.transform.Find("panel").GetComponent<RectTransform>().anchoredPosition - new Vector2(0, __instance.descriptionsPanel.transform.Find("panel").GetComponent<RectTransform>().rect.height + 20);
                showingCompare = false;
            }


        }
        private static Item GetComparedItem(Item item, bool offhand = false)
        {
            if (item == null || !Global.code.uiInventory.gameObject.activeSelf || Global.code.uiInventory.curCustomization == null)
                return null;

            Item compared = null;

            switch (item.slotType)
            {
                case SlotType.armor:
                    compared = Global.code.uiInventory.curCustomization.armor?.GetComponent<Item>();
                    break;
                case SlotType.weapon:
                    if (AedenthornUtils.CheckKeyHeld(weaponModKey.Value) || offhand || item == Global.code.uiInventory.curCustomization.weapon?.GetComponent<Item>())
                    {
                        Dbgl("comparing to off-hand weapon");
                        compared = Global.code.uiInventory.curCustomization.weapon2?.GetComponent<Item>();
                    }
                    else
                    {
                        Dbgl("comparing to weapon");

                        compared = Global.code.uiInventory.curCustomization.weapon?.GetComponent<Item>();
                    }
                    break;
                case SlotType.helmet:
                    compared = Global.code.uiInventory.curCustomization.helmet?.GetComponent<Item>();
                    break;
                case SlotType.gloves:
                    compared = Global.code.uiInventory.curCustomization.gloves?.GetComponent<Item>();
                    break;
                case SlotType.legging:
                    compared = Global.code.uiInventory.curCustomization.leggings?.GetComponent<Item>();
                    break;
                case SlotType.shoes:
                    compared = Global.code.uiInventory.curCustomization.shoes?.GetComponent<Item>();
                    break;
                default:
                    break;
            }
            return compared;
        }
    }
}
