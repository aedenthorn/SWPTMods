using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Transmogrification
{
    [BepInPlugin("aedenthorn.Transmogrification", "Transmogrification", "0.2.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> destroyOriginal;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> modKeyOn;
        public static ConfigEntry<string> modKeyOff;
        public static ConfigEntry<string> appearString;
        public static ConfigEntry<Color> appearStringColor;

        private static EquipmentSlot currentSlot;
        private static Dictionary<int, string> itemAppearances = new Dictionary<int, string>();

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
            nexusID = Config.Bind<int>("General", "NexusID", 42, "Nexus mod ID for updates");

            hotKey = Config.Bind<string>("Options", "HotKey", "t", "Hotkey to transmogrify. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            modKeyOn = Config.Bind<string>("Options", "ModKeyOn", "", "Must be held when pressing the hotkey to transmogrify (optional). Use https://docs.unity3d.com/Manual/class-InputManager.html");
            modKeyOff = Config.Bind<string>("Options", "ModKeyOff", "left shift", "Must be held when pressing the hotkey to untransmogrify (optional). Use https://docs.unity3d.com/Manual/class-InputManager.html");
            
            appearString = Config.Bind<string>("Options", "AppearString", "Appears as {0}", "Text to show in transmogrified item's description. {0} is replaced by the source name. Leave empty to disable.");
            appearStringColor = Config.Bind<Color>("Options", "AppearStringColor", new Color(1,1,1), "Color of appear string text.");

            destroyOriginal = Config.Bind<bool>("Options", "DestroyOriginal", false, "Destroy the item from which the appearance is taken.");
            
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        public static Transform currentDescriptionHolder;

        [HarmonyPatch(typeof(UICombat), nameof(UICombat.CreateLine))]
        static class CreateLine_Patch
        {
            static void Postfix(UICombat __instance)
            {
                if (!modEnabled.Value)
                    return;
                currentDescriptionHolder = __instance.descriptionHolder;
                    
            }
        }
        [HarmonyPatch(typeof(UICombat), nameof(UICombat.ShowInfo))]
        static class ShowInfo_Patch
        {
            static void Postfix(UICombat __instance, Item item)
            {
                if (!modEnabled.Value || !item || !itemAppearances.ContainsKey(item.transform.GetInstanceID()) || appearString.Value.Trim() == "")
                    return;

                Transform transform = UnityEngine.Object.Instantiate<Transform>(__instance.descriptionLinePrefab);
                transform.gameObject.SetActive(true);
                transform.SetParent(currentDescriptionHolder);
                transform.localScale = Vector3.one;
                transform.GetComponent<Text>().text = string.Format(appearString.Value, itemAppearances[item.transform.GetInstanceID()]);
                transform.GetComponent<Text>().color = appearStringColor.Value;
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

        
        [HarmonyPatch(typeof(Mainframe), "SaveItem")]
        static class SaveItem_Patch
        {

            static void Postfix(Mainframe __instance, Transform item)
            {
                try
                {

                    if (!modEnabled.Value || !itemAppearances.ContainsKey(item.GetInstanceID()))
                        return;

                    string name = itemAppearances[item.GetInstanceID()];

                    Dbgl($"saving item {item.name} appearance as {name}");

                    ES2.Save<string>(name, __instance.GetFolderName() + "Items.txt?tag=transmog" + item.GetInstanceID());
                }
                catch { }
            }
        }

        [HarmonyPatch(typeof(Mainframe), "LoadItem")]
        static class LoadItem_Patch
        {

            static void Postfix(Mainframe __instance, int id, Transform __result)
            {
                if (!modEnabled.Value || !ES2.Exists(__instance.GetFolderName() + "Items.txt?tag=transmog" + id))
                    return;

                string name = ES2.Load<string>(__instance.GetFolderName() + "Items.txt?tag=transmog" + id);

                Dbgl($"setting loaded item {__result.name} {id} appearance as {name}");

                Transform source = RM.code.allItems.GetItemWithName(name);
                ReplaceAppearance(source, __result);
                itemAppearances[__result.GetInstanceID()] = name;
            }
        }

        [HarmonyPatch(typeof(UIInventory), "Update")]
        static class UIInventory_Update_Patch
        {
            static void Postfix(UIInventory __instance)
            {
                if (!modEnabled.Value || !Global.code.uiInventory.gameObject.activeSelf || !currentSlot || !currentSlot.item || !AedenthornUtils.CheckKeyDown(hotKey.Value))
                    return;

                Dbgl($"Pressed hotkey on slot with item {currentSlot.item.name}");

                Transform source;
                if (AedenthornUtils.CheckKeyHeld(modKeyOn.Value, false) && !AedenthornUtils.CheckKeyHeld(modKeyOff.Value, true) && Global.code.selectedItem && currentSlot.slotType == Global.code.selectedItem.GetComponent<Item>().slotType && (currentSlot.slotType != SlotType.weapon || currentSlot.item.GetComponent<Item>().itemType == Global.code.selectedItem.GetComponent<Item>().itemType))
                {
                    Dbgl($"Transmogrifying {currentSlot.item.name} into {Global.code.selectedItem.name}");
                    source = Global.code.selectedItem;
                    itemAppearances[currentSlot.item.GetInstanceID()] = Global.code.selectedItem.name;
                }
                else if (AedenthornUtils.CheckKeyHeld(modKeyOff.Value, false) && itemAppearances.ContainsKey(currentSlot.item.GetInstanceID()))
                {
                    Dbgl($"Detransmogrifying {currentSlot.item.name}");
                    source = RM.code.allItems.GetItemWithName(currentSlot.item.name);
                    itemAppearances.Remove(currentSlot.item.GetInstanceID());
                }
                else
                    return;

                ReplaceAppearance(source, currentSlot.item);

                if (destroyOriginal.Value && AedenthornUtils.CheckKeyHeld(modKeyOn.Value, false))
                {
                    Global.code.uiInventory.curStorage.RemoveItem(Global.code.selectedItem);
                    Global.code.selectedItem = null;
                    Destroy(Global.code.selectedItem);
                }

                context.StartCoroutine(RefreshItem(currentSlot.item));
            }
        }

        private static IEnumerator RefreshItem(Transform item)
        {
            Global.code.uiInventory.curCustomization.RemoveItem(item);
            yield return new WaitForEndOfFrame();
            Global.code.uiInventory.curCustomization.AddItem(item, currentSlot.name);
            Global.code.uiInventory.RefreshEquipment();

        }

        private static void ReplaceAppearance(Transform source, Transform destination)
        {
            for (int i = destination.childCount - 1; i >= 0; i--)
            {
                Dbgl($"Removing child {destination.GetChild(i).name}");

                DestroyImmediate(destination.GetChild(i).gameObject);
            }
            if (destination.GetComponent<Appeal>())
            {
                destination.GetComponent<Appeal>().allRenderers.Clear();
            }

            for (int i = 0; i < source.childCount; i++)
            {
                Transform t = Instantiate(source.GetChild(i), destination);
                t.name = source.GetChild(i).name;
                Dbgl($"Adding child {t.name}");
                if (destination.GetComponent<Appeal>() && t.GetComponent<SkinnedMeshRenderer>())
                {
                    destination.GetComponent<Appeal>().allRenderers.Add(t.GetComponent<SkinnedMeshRenderer>());
                }
            }
            destination.GetComponent<Item>().icon = source.GetComponent<Item>().icon;
        }
    }
}
