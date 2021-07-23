using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace QuickSwapItems
{
    [BepInPlugin("aedenthorn.QuickSwapItems", "Quick Swap Items", "0.2.0")]
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

            hotKeys = Config.Bind<string>("Options", "HotKeyArray", "1,2,3,4,5,6,7,8", "Comma-separated list of hotkeys to switch inventories or send selected item to specific inventory. First entry refers to the player. Use https://docs.unity3d.com/Manual/class-InputManager.html");

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
                        CharacterCustomization cc;
                        if (i == 0)
                        {
                            cc = Player.code.customization;
                        }
                        else if (Global.code.curlocation.locationType == LocationType.home && i <= Global.code.companions.items.Count)
                        {
                            cc = Global.code.companions.items[i-1].GetComponent<CharacterCustomization>();
                        }
                        else if (Global.code.curlocation.locationType != LocationType.home && i <= Global.code.playerCombatParty.items.Count)
                        {
                            cc = Global.code.playerCombatParty.items[i-1].GetComponent<CharacterCustomization>();
                        }
                        else
                            break;

                        if (__instance.curCustomization == cc)
                            break;

                        if (Global.code.selectedItem)
                        {
                            if (cc.storage.AutoAddItem(Global.code.selectedItem, true, true, true))
                            {
                                Global.code.selectedItem = null;
                                __instance.curStorage.inventory.Refresh();
                                cc.storage.inventory.Refresh();
                            }
                        }
                        else
                        {
                            Global.code.uiInventory.Open(cc);
                        }
                        break;
                    }
                }
            }
        }
    }
}
