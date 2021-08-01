using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ArmorParts
{
    [BepInPlugin("aedenthorn.ArmorParts", "Armor Parts", "0.1.0")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> modKeySave;
        public static ConfigEntry<string> modKeyReset;

        public static BepInExPlugin context;

        private static EquipmentSlot currentSlot;
        private static ItemIcon currentIcon;

        private static Dictionary<int, ArmorPartsData> partsData = new Dictionary<int, ArmorPartsData>();
        private static string assetPath;

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
            hotKey = Config.Bind<string>("Options", "HotKey", "e", "Hot key to import. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            modKeySave = Config.Bind<string>("Options", "ModKeySave", "left shift", "Modifier key to export instead of import. Use https://docs.unity3d.com/Manual/class-InputManager.html");
            modKeyReset = Config.Bind<string>("Options", "ModKeyReset", "left alt", "Modifier key to reset instead of import. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            assetPath = AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace);

            LoadFromFiles();

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

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.RefreshClothesVisibility))]
        [HarmonyPriority(Priority.Last)]
        static class CharacterCustomization_RefreshClothesVisibility_Patch
        {

            static void Postfix(CharacterCustomization __instance)
            {
                if (!modEnabled.Value || !__instance.showArmor || !__instance.armor)
                    return;
                int id;
                
                if(__instance.isDisplay && Global.code.uiInventory.gameObject.activeSelf)
                {
                    id = Global.code.uiInventory.curCustomization.armor.GetInstanceID();
                }
                else
                    id = __instance.armor.GetInstanceID();

                if (!partsData.ContainsKey(id))
                    return;

                Dbgl("checking clothing");

                if (partsData[id].showBra && __instance.bra)
                {
                    Dbgl("showing bra");
                    __instance.bra.gameObject.SetActive(true);
                }
                if (partsData[id].showPanties && __instance.panties)
                {
                    Dbgl("showing panties");
                    __instance.panties.gameObject.SetActive(true);
                }
                if (partsData[id].showSuspenders && __instance.suspenders)
                {
                    Dbgl("showing suspenders");
                    __instance.suspenders.gameObject.SetActive(true);
                }
            }
        }

        [HarmonyPatch(typeof(CharacterCustomization), nameof(CharacterCustomization.AddItem))]
        static class CharacterCustomization_AddItem_Patch
        {

            static void Prefix(CharacterCustomization __instance, Transform item, string slotName)
            {
                if (!modEnabled.Value || slotName != "armor")
                    return;

                if (partsData.ContainsKey(item.GetInstanceID()))
                {
                    SetArmorParts(item, partsData[item.GetInstanceID()]);
                    Dbgl($"loaded data for {item.GetInstanceID()}");
                }
            }
        }

        [HarmonyPatch(typeof(UIInventory), "Update")]
        static class UIInventory_Update_Patch
        {
            static void Postfix(UIInventory __instance)
            {
                if (!modEnabled.Value || !Global.code.uiInventory.gameObject.activeSelf || (!currentSlot?.item && !currentIcon?.item) || !AedenthornUtils.CheckKeyDown(hotKey.Value))
                    return;

                Transform item;
                if (currentSlot?.item && currentSlot.slotType == SlotType.armor)
                    item = currentSlot.item;
                else if (currentIcon?.item && currentIcon.item.GetComponent<Item>().slotType == SlotType.armor)
                    item = currentIcon.item;
                else return;

                if (item == null)
                    return;

                Dbgl($"Pressed hotkey on slot with armor {item.name}");

                if (AedenthornUtils.CheckKeyHeld(modKeySave.Value))
                {
                    SaveData(item);
                }
                else if (AedenthornUtils.CheckKeyHeld(modKeyReset.Value))
                {
                    ArmorPartsData data = new ArmorPartsData();
                    data.name = item.name;
                    data.id = item.GetInstanceID();

                    for (int i = 0; i < item.childCount; i++)
                    {
                        if (item.GetChild(i).name.Contains(":"))
                            Destroy(item.GetChild(i).gameObject);
                        else
                        {
                            item.GetChild(i).gameObject.SetActive(true);
                            data.parts.Add(item.GetChild(i).name);
                        }
                    }
                    SaveData(item, data);

                    Dbgl($"Reset data for {item.name}");
                }
                else
                {
                    LoadFromFiles();
                    if (partsData.ContainsKey(item.GetInstanceID()))
                    {
                        SetArmorParts(item, partsData[item.GetInstanceID()]);
                        Dbgl($"loaded data for {item.GetInstanceID()}");
                        context.StartCoroutine(RefreshItem(item));
                    }
                }
            }
        }


        private static void LoadFromFiles()
        {

            partsData.Clear();
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
                return;
            }
            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                try
                {
                    ArmorPartsData apd = JsonUtility.FromJson<ArmorPartsData>(File.ReadAllText(file));
                    partsData.Add(apd.id, apd);
                }
                catch (Exception ex)
                {
                    Dbgl($"error loading parts for {Path.GetFileNameWithoutExtension(file)}. \n\n{ex}");
                }
            }
        }

        private static void SaveData(Transform item)
        {
            ArmorPartsData data = new ArmorPartsData();
            data.name = item.name;
            data.id = item.GetInstanceID();
            for (int i = 0; i < item.childCount; i++)
            {
                if (item.GetChild(i).gameObject.activeSelf)
                    data.parts.Add(item.GetChild(i).name);
            }
            SaveData(item, data);
        }
        private static void SaveData(Transform item, ArmorPartsData data)
        {
            File.WriteAllText(Path.Combine(assetPath, $"{item.name}_{item.GetInstanceID()}.json"), JsonUtility.ToJson(data));
            Dbgl($"saved data to {Path.Combine(assetPath, $"{item.name}_{item.GetInstanceID()}.json")}");
            partsData[item.GetInstanceID()] = data;
        }
        private static IEnumerator RefreshItem(Transform item)
        {
            Global.code.uiInventory.curCustomization.RemoveItem(item);
            yield return new WaitForEndOfFrame();
            Global.code.uiInventory.curCustomization.AddItem(item, currentSlot.name);
            Global.code.uiInventory.RefreshEquipment();

        }

        private static void SetArmorParts(Transform item, ArmorPartsData data)
        {
            for (int i = 0; i < item.childCount; i++)
            {
                if (item.GetChild(i).name.Contains(":"))
                {
                    if (!data.parts.Contains(item.GetChild(i).name))
                        Destroy(item.GetChild(i).gameObject);
                    else
                        item.GetChild(i).gameObject.SetActive(true);
                }
                else
                    item.GetChild(i).gameObject.SetActive(data.parts.Contains(item.GetChild(i).name));
            }
            foreach(string part in data.parts)
            {
                if (!part.Contains(":") || item.Find(part))
                    continue;
                string[] tp = part.Split(':');
                Transform t = RM.code.allItems.GetItemWithName(tp[0]);
                if (!t)
                    continue;
                Transform p = t.Find(tp[1]);
                if (p)
                {
                    Transform added = Instantiate(p, item);
                    added.name = part;
                }
            }
        }
    }
}
