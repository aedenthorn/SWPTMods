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
        //public static ConfigEntry<int> nexusID;

        public static BepInExPlugin context;
        
        public static bool showingCompare = false;
        public static Transform comparePanel;
        public static Transform compareHolder;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 169, "Nexus mod ID for updates");


            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

        }

        [HarmonyPatch(typeof(UICombat), nameof(UICombat.ShowInfo))]
        static class ShowInfo_Patch
        {
            static void Prefix(UICombat __instance, Item item)
            {
                if (!modEnabled.Value || !showingCompare)
                    return;
                __instance.descriptionHolder = compareHolder;
            }
            static void Postfix(UICombat __instance, Item item)
            {
                if (!modEnabled.Value || !Global.code.uiInventory.gameObject.activeSelf)
                    return;

                if(__instance.descriptionsPanel.transform.Find("compared") == null)
                {
                    comparePanel = Instantiate(__instance.descriptionsPanel.transform.Find("panel"), __instance.descriptionsPanel.transform);
                    comparePanel.name = "compared";
                    comparePanel.GetComponent<RectTransform>().anchoredPosition -= new Vector2(0, __instance.descriptionsPanel.transform.Find("panel").GetComponent<RectTransform>().rect.height);
                    compareHolder = comparePanel.Find("group");
                }

                if (showingCompare)
                {
                    __instance.descriptionHolder = __instance.descriptionsPanel.transform.Find("panel/group");
                    return;
                }

                Item compared;

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
                        return;
                }

                if (compared == null)
                {
                    comparePanel.gameObject.SetActive(false);
                    return;
                }

                Dbgl($"name: {compareHolder?.name}");

                showingCompare = true;
                comparePanel.gameObject.SetActive(true);
                __instance.ShowInfo(compared);
                showingCompare = false;


            }
        }
    }
}
