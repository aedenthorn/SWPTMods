using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace QuickSwapItems
{
    [BepInPlugin("aedenthorn.QuickSwapItems", "Quick Swap Items", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> hotKeys;
        //public static ConfigEntry<int> nexusID;

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

            hotKeys = Config.Bind<string>("Options", "HotKeys", "1,2,3,4,5,6", "Comma-separated list of hotkeys to switch inventories or send selected item to specific inventory. First entry refers to the player. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(UIInventory), nameof(UIInventory.Update))]
        static class UIInventory_Update_Patch
        {
            static void Postfix(UIInventory __instance)
            {
                if (!modEnabled.Value)
                    return;
                string[] keyarray = hotKeys.Value.Split(',');
                for(int i = 0; i < keyarray.Length; i++)
                {
                    if (AedenthornUtils.CheckKeyDown(keyarray[i]))
                    {
                        if (Global.code.selectedItem)
                        {
                            if (i == 0 && __instance.curCustomization != Player.code.customization && Player.code.customization.storage.AutoAddItem(Global.code.selectedItem, true, true, true))
                            {
                                Global.code.selectedItem = null;
                                __instance.curStorage.inventory.Refresh();
                                Player.code.customization.storage.inventory.Refresh();
                            }
                            else if(i > 0 && Global.code.playerCombatParty.items?.Count >= i && __instance.curCustomization != Global.code.playerCombatParty.items[i - 1].GetComponent<ID>().customization && Global.code.playerCombatParty.items[i - 1].GetComponent<ID>().customization.storage.AutoAddItem(Global.code.selectedItem, true, true, true))
                            {
                                Global.code.selectedItem = null;
                                __instance.curStorage.inventory.Refresh();
                                Global.code.playerCombatParty.items[i - 1].GetComponent<ID>().customization.storage.inventory.Refresh();

                            }
                        }
                        else
                        {
                            if (i == 0 && __instance.curCustomization != Player.code.customization)
                                Global.code.uiInventory.Open(Player.code.customization);
                            else if(i > 0 && Global.code.playerCombatParty.items?.Count >= i && __instance.curCustomization != Global.code.playerCombatParty.items[i - 1].GetComponent<ID>().customization)
                                Global.code.uiInventory.Open(Global.code.playerCombatParty.items[i - 1].GetComponent<ID>().customization);
                        }
                        break;
                    }
                }
            }
        }
    }
}
