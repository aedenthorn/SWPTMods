using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CompareEquipment
{
    [BepInPlugin("aedenthorn.CompareEquipment", "Compare Equipment", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static BepInExPlugin context;
        
        public static bool showingCompare = false;
        public static Transform comparePanel;
        public static Transform compareHolder;
        public static Text compareName;
        public static Text comparePrice;
        public static bool compareAsGoods = false;

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
            nexusID = Config.Bind<int>("General", "NexusID", 38, "Nexus mod ID for updates");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(UICombat), "Update")]
        static class UICombat_Update_Patch
        {
            static void Postfix(UICombat __instance)
            {
                if (!modEnabled.Value || !comparePanel || !comparePanel.gameObject.activeSelf)
                    return;
                //comparePanel.GetComponent<RectTransform>().anchoredPosition = __instance.descriptionsPanel.transform.Find("panel").GetComponent<RectTransform>().anchoredPosition - new Vector2(0, __instance.descriptionsPanel.transform.Find("panel").GetComponent<RectTransform>().rect.height);
            }
        }
        [HarmonyPatch(typeof(UICombat), nameof(UICombat.ShowInfo))]
        static class ShowInfo_Patch
        {
            static void Prefix(UICombat __instance)
            {
                if (!modEnabled.Value || !showingCompare)
                    return;
                __instance.descriptionHolder = compareHolder;
                __instance.lineName = compareName;
                __instance.linePrice = comparePrice;
            }
            static void Postfix(UICombat __instance, Item item)
            {
                if (!modEnabled.Value || !Global.code.uiInventory.gameObject.activeSelf)
                    return;

                if(__instance.descriptionsPanel.transform.Find("compared") == null)
                {
                    comparePanel = Instantiate(__instance.descriptionsPanel.transform.Find("panel"), __instance.descriptionsPanel.transform);
                    comparePanel.name = "compared";
                    compareHolder = comparePanel.Find("group");
                    compareName = comparePanel.Find("name (1)").GetComponent<Text>();
                    comparePrice = comparePanel.Find("line price").GetComponent<Text>();
                }

                if (showingCompare)
                {
                    if(compareAsGoods)
                        __instance.linePrice.text = Localization.GetContent("Gold", new object[0]) + " " + item.cost.ToString();
                    __instance.descriptionHolder = __instance.descriptionsPanel.transform.Find("panel/group");
                    __instance.lineName = __instance.descriptionsPanel.transform.Find("panel/name (1)").GetComponent<Text>();
                    __instance.linePrice = __instance.descriptionsPanel.transform.Find("panel/line price").GetComponent<Text>();
                    return;
                }

                Item compared = null;

                switch (item.slotType)
                {
                    case SlotType.armor:
                        compared = Global.code.uiInventory.curCustomization.armor?.GetComponent<Item>();
                        break;
                    case SlotType.weapon:
                        compared = Global.code.uiInventory.curCustomization.weapon?.GetComponent<Item>();
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

                if (compared == null || compared == item)
                {
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
    }
}
