using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace QuickSwapItems
{
    [BepInPlugin("aedenthorn.QuickSwapItems", "Quick Swap", "0.3.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> hotKeys;
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

            hotKeys = Config.Bind<string>("Options", "HotKeyArray", "1,2,3,4,5,6,7,8", "Comma-separated list of hotkeys to switch inventories or send selected item to specific inventory. First entry refers to the player. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            nexusID = Config.Bind<int>("General", "NexusID", 5, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(Global), "Update")]
        static class Global_Update_Patch
        {
            static void Postfix(Global __instance)
            {
                if (!modEnabled.Value || !Player.code)
                    return;

                string[] keyarray = hotKeys.Value.Split(',');
                for(int i = 0; i < keyarray.Length; i++)
                {
                    if (AedenthornUtils.CheckKeyDown(keyarray[i]))
                    {
                        Dbgl($"Pressed {keyarray[i]}");
                        CharacterCustomization cc;
                        if (i == 0)
                        {
                            cc = Player.code.customization;
                        }
                        else if (__instance.curlocation.locationType == LocationType.home && i <= __instance.companions.items.Count)
                        {
                            cc = __instance.companions.items[i-1].GetComponent<CharacterCustomization>();
                        }
                        else if (__instance.curlocation.locationType != LocationType.home && i <= __instance.playerCombatParty.items.Count)
                        {
                            cc = __instance.playerCombatParty.items[i-1].GetComponent<CharacterCustomization>();
                        }
                        else
                            break;

                        if (__instance.uiInventory.gameObject.activeSelf)
                        {
                            if (__instance.uiInventory.curCustomization == cc)
                                break;

                            if (__instance.selectedItem && !__instance.selectedItem.GetComponent<Item>().isGoods)
                            {
                                Dbgl("Trying to move item");
                                if (cc.storage.AutoAddItem(__instance.selectedItem, true, true, true))
                                {
                                    __instance.selectedItem = null;
                                    __instance.uiInventory.curStorage.inventory.Refresh();
                                    cc.storage.inventory.Refresh();
                                    Dbgl("Item moved");
                                }
                            }
                            else
                            {
                                __instance.uiInventory.Open(cc);
                                Dbgl("Switched inventory");
                            }

                        }
                        else if (__instance.uiCustomization.gameObject.activeSelf)
                        {
                            if (__instance.uiCustomization.curCharacterCustomization != cc)
                            {
                                __instance.uiCustomization.curCharacterCustomization = cc;
                                __instance.uiCustomization.SwitchToCustomization();
                                Dbgl("Switched character");
                            }
                        }
                        else if (__instance.uiMakeup.gameObject.activeSelf)
                        {
                            if (__instance.uiMakeup.curCustomization != cc)
                            {
                                __instance.uiMakeup.curCustomization = cc;
                                __instance.uiMakeup.SwitchToMakeup();
                                Dbgl("Switched character");
                            }
                        }
                        else if (__instance.uiNameChanger.gameObject.activeSelf)
                        {
                            if (__instance.uiNameChanger.curcustomization != cc)
                            {
                                __instance.uiNameChanger.curcustomization = cc;
                                __instance.uiNameChanger.SwitchToCharacter();
                                Dbgl("Switched character");
                            }
                        }
                        else if (__instance.uiCharacter.gameObject.activeSelf)
                        {
                            if (__instance.uiCharacter.curCustomization != cc)
                            {
                                __instance.uiCharacter.Open(cc);
                                Dbgl("Switched character");
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
