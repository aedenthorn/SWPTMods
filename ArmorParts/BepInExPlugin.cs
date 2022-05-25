using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace ArmorParts
{
    [BepInPlugin("aedenthorn.ArmorParts", "Armor Parts", "0.3.1")]
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

        private static Dictionary<Transform, ArmorPartsData> partsTransformDict = new Dictionary<Transform, ArmorPartsData>();
        private static Dictionary<string, ArmorPartsData> partsGUIDDict = new Dictionary<string, ArmorPartsData>();
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
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Item), nameof(Item.InstantiateModel))]
        static class InstantiateModel_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Dbgl($"Transpiling Item.InstantiateModel");

                var codes = new List<CodeInstruction>(instructions);
                var newCodes = new List<CodeInstruction>();
                for (int i = 0; i < codes.Count; i++)
                {
                    if (i > 1 && codes[i - 2].opcode == OpCodes.Ldarg_0 && codes[i - 1].opcode == OpCodes.Ldloc_0 && codes[i].opcode == OpCodes.Call && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(Utility), nameof(Utility.Instantiate)))
                    {
                        Dbgl($"Changing instantiate method");
                        newCodes.Add(new CodeInstruction(OpCodes.Ldarg_0));
                        newCodes.Add(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BepInExPlugin), nameof(BepInExPlugin.GetArmorPartsModel))));
                    }
                    else
                        newCodes.Add(codes[i]);
                }
                return newCodes.AsEnumerable();
            }
        }

        private static Transform GetArmorPartsModel(Transform transform, Item item)
        {
            Transform t = Utility.Instantiate(transform);
            if (modEnabled.Value && partsTransformDict.ContainsKey(item.transform))
            {
                Dbgl($"Setting armor parts for model {t.name}");
                SetArmorParts(t, partsTransformDict[item.transform], item.itemType);
            }
            return t;
        }

        [HarmonyPatch(typeof(Mainframe), "SaveItem")]
        static class SaveItem_Patch
        {
            static void Postfix(Mainframe __instance, Transform item)
            {
                if (!modEnabled.Value || !item.GetComponent<Item>() || item.GetComponent<Item>().slotType != SlotType.armor || !partsTransformDict.ContainsKey(item))
                    return;

                string GUID = partsTransformDict[item].GUID;

                Dbgl($"saving item {item.name} id {item.GetInstanceID()} GUID as {GUID}");

                ES2.Save(GUID, __instance.GetFolderName() + "Items.txt?tag=armorpartsmod_" + item.GetInstanceID());
            }
        }

        [HarmonyPatch(typeof(Mainframe), "LoadItem")]
        static class LoadItem_Patch
        {

            static void Postfix(Mainframe __instance, int id, Transform __result)
            {
                if (!modEnabled.Value || __result.GetComponent<Item>().slotType != SlotType.armor || !ES2.Exists(__instance.GetFolderName() + "Items.txt?tag=armorpartsmod_" + id))
                    return;

                string GUID = ES2.Load<string>(__instance.GetFolderName() + "Items.txt?tag=armorpartsmod_" + id);

                if (!partsGUIDDict.ContainsKey(GUID))
                {
                    Dbgl($"armor data with {GUID} for loaded item {__result.name} not found!");
                    return;
                }

                Dbgl($"got guid {GUID} for loaded item {__result.name}");

                partsTransformDict[__result] = partsGUIDDict[GUID];
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
                if (!modEnabled.Value || !__instance.showArmor || !__instance.armor || !Global.code?.uiInventory)
                    return;
                Transform t;
                
                if(__instance.isDisplay && Global.code.uiInventory.gameObject.activeSelf)
                {
                    t = Global.code.uiInventory.curCustomization.armor;
                }
                else
                    t = __instance.armor;

                if (!partsTransformDict.ContainsKey(t))
                    return;

                if (partsTransformDict[t].showBra && __instance.bra)
                {
                    __instance.bra.gameObject.SetActive(true);
                }
                if (partsTransformDict[t].showPanties && __instance.panties)
                {
                    __instance.panties.gameObject.SetActive(true);
                }
                if (partsTransformDict[t].showSuspenders && __instance.suspenders)
                {
                    __instance.suspenders.gameObject.SetActive(true);
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

                Transform item = GetCurrentItem();
                if (!item)
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
                    data.GUID = partsTransformDict[item.parent].GUID;
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
                    context.StartCoroutine(RefreshItem(item.parent));
                    Dbgl($"Reset data for {item.name}");
                }
                else
                {
                    LoadFromFiles();
                    if (partsTransformDict.ContainsKey(item.parent))
                    {
                        Dbgl($"loaded data for {item.name}");
                        context.StartCoroutine(RefreshItem(item.parent));
                    }
                }
            }
        }
        private static Transform GetCurrentItem()
        {
            Transform item = null;
            if (currentSlot?.item && currentSlot.slotType == SlotType.armor)
                item = currentSlot.item;
            else if (currentIcon?.item && currentIcon.item.GetComponentInChildren<Item>().slotType == SlotType.armor)
                item = currentIcon.item;

            if (!item)
                return null;

            if (!item.Find(item.name))
            {
                Dbgl($"Child transform not found!");
                return null;
            }

            return item.Find(item.name);
        }

        private static void LoadFromFiles()
        {

            partsGUIDDict.Clear();
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
                    partsGUIDDict.Add(apd.GUID, apd);
                }
                catch (Exception ex)
                {
                    Dbgl($"error loading parts for {Path.GetFileNameWithoutExtension(file)}. \n\n{ex}");
                }
            }
            var keys = partsTransformDict.Keys.ToArray();
            foreach(Transform key in keys)
            {
                if (!partsGUIDDict.ContainsKey(partsTransformDict[key].GUID))
                    partsTransformDict.Remove(key);
                else
                    partsTransformDict[key] = partsGUIDDict[partsTransformDict[key].GUID];
            }
        }

        private static void SaveData(Transform childItem)
        {
            ArmorPartsData data = new ArmorPartsData();
            data.name = childItem.name;
            if (partsTransformDict.ContainsKey(childItem.parent))
                data.GUID = partsTransformDict[childItem.parent].GUID;
            else
                data.GUID = Guid.NewGuid().ToString();
            for (int i = 0; i < childItem.childCount; i++)
            {
                if (childItem.GetChild(i).gameObject.activeSelf)
                    data.parts.Add(childItem.GetChild(i).name);
            }
            SaveData(childItem, data);
        }

        private static void SaveData(Transform childItem, ArmorPartsData data)
        {
            File.WriteAllText(Path.Combine(assetPath, $"{childItem.name}_{data.GUID}.json"), JsonUtility.ToJson(data));
            Dbgl($"saved data to {Path.Combine(assetPath, $"{childItem.name}_{data.GUID}.json")}");
            partsTransformDict[childItem.parent] = data;
        }

        private static IEnumerator RefreshItem(Transform item)
        {
            Dbgl($"Refreshing item {item.name}");

            Global.code.uiInventory.curCustomization.RemoveItem(item);
            yield return new WaitForEndOfFrame();
            Global.code.uiInventory.curCustomization.AddItem(item, currentSlot.name);
            Global.code.uiInventory.RefreshEquipment();

        }

        private static void SetArmorParts(Transform childItem, ArmorPartsData data, ItemType itemType)
        {
            for (int i = 0; i < childItem.childCount; i++)
            {
                if (childItem.GetChild(i).name.Contains(":"))
                {
                    if (!data.parts.Contains(childItem.GetChild(i).name))
                        Destroy(childItem.GetChild(i).gameObject);
                    else
                        childItem.GetChild(i).gameObject.SetActive(true);
                }
                else
                    childItem.GetChild(i).gameObject.SetActive(data.parts.Contains(childItem.GetChild(i).name));
            }
            foreach(string part in data.parts)
            {
                if (!part.Contains(":") || childItem.Find(part))
                    continue;
                string[] tp = part.Split(':');
                Dbgl($"Adding external part {part}");
                Transform t;
                if (itemType == ItemType.lingerie)
                {
                    t = Resources.Load<Transform>("Clothes Prefabs/Lingeries/" + tp[0]);
                }
                else
                {
                    t = Resources.Load<Transform>("Clothes Prefabs/Armors/" + tp[0]);
                }
                if (t)
                {
                    Transform p = t.Find(tp[1]);
                    if (p)
                    {
                        Transform added = Utility.Instantiate(p);
                        added.SetParent(childItem);
                        added.name = part;
                    }
                }
            }
        }
    }
}
