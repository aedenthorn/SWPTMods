using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace SortInventory
{
    [BepInPlugin("aedenthorn.SortInventory", "Sort Inventory", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<bool> autoSort;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<string> hotKey;

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
            nexusID = Config.Bind<int>("General", "NexusID", 45, "Nexus mod ID for updates");
            
            autoSort = Config.Bind<bool>("Options", "AutoSort", false, "Enable auto-sorting");
            hotKey = Config.Bind<string>("Options", "HotKey", "s", "Hot key to sort current inventory. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }


        //[HarmonyPatch(typeof(Storage), nameof(Storage.AutoAddItem))]
        public static class Storage_AutoAddItem_Patch
        {
            public static void Postfix(Storage __instance)
            {
                if (!modEnabled.Value || !autoSort.Value)
                    return;

                SortStorage(__instance);
            }
        }

        //[HarmonyPatch(typeof(Storage), nameof(Storage.RemoveItem))]
        public static class Storage_RemoveItem_Patch
        {
            public static void Postfix(Storage __instance)
            {
                if (!modEnabled.Value || !autoSort.Value)
                    return;

                SortStorage(__instance);
            }
        }

        [HarmonyPatch(typeof(Global), nameof(Global.ToggleInventory))]
        public static class ToggleInventory_Patch
        {
            public static void Postfix(Global __instance)
            {
                if (!modEnabled.Value || !__instance.uiInventory.gameObject.activeSelf || !autoSort.Value)
                    return;

                SortStorage(Player.code.customization.storage);
                foreach(var t in Global.code.companions.items)
                {
                    if(t && t.GetComponent<CharacterCustomization>()?.storage)
                        SortStorage(t.GetComponent<CharacterCustomization>().storage);
                }
            }
        }

        [HarmonyPatch(typeof(UIInventory), nameof(UIInventory.Update))]
        public static class UIInventory_Update_Patch
        {
            public static void Postfix(UIInventory __instance)
            {
                if (!modEnabled.Value || __instance.curStorage == null)
                    return;

                if (AedenthornUtils.CheckKeyDown(hotKey.Value))
                {
                    SortStorage(__instance.curStorage);
                }
                if (false && AedenthornUtils.CheckKeyDown("p"))
                {
                    __instance.curStorage.AutoAddItem(RM.code.allItems.GetItemWithName("Lascivious Huntress Armor"), false, false, false);
                    __instance.curStorage.AutoAddItem(RM.code.allItems.GetItemWithName("Black Priestess Outfit"), false, false, false);
                }
            }
        }


        private static void SortStorage(Storage storage)
        {
            var watch = new Stopwatch();
            watch.Start();
            if (storage.items.items.Count == 0)
                return;

            Dbgl("sorting storage");

            List<Transform> items = new List<Transform>();
            for(int i = storage.items.items.Count - 1; i >= 0; i--)
            {

                items.Add(storage.items.items[i]);
                RemoveItem(storage, i);
            }
            storage.inventory.Refresh();

            items.Sort(delegate (Transform a, Transform b) {

                if (a.name == b.name)
                {
                    return 0;
                }
                if(a.GetComponent<Item>().x == b.GetComponent<Item>().x && a.GetComponent<Item>().y == b.GetComponent<Item>().y)
                {
                    if(a.GetComponent<Item>().slotType == b.GetComponent<Item>().slotType)
                    {
                        if(a.GetComponent<Potion>() && b.GetComponent<Potion>())
                        {
                            if(a.GetComponent<Potion>().addExp != b.GetComponent<Potion>().addExp)
                                return a.GetComponent<Potion>().addExp.CompareTo(b.GetComponent<Potion>().addExp);
                            if (a.GetComponent<Item>().cost == b.GetComponent<Item>().cost)
                                return a.name.CompareTo(b.name);
                            return a.GetComponent<Item>().cost.CompareTo(b.GetComponent<Item>().cost);
                        }
                        else if (a.GetComponent<Potion>())
                            return 1;
                        else if (b.GetComponent<Potion>())
                            return -1;
                        if (a.GetComponent<Item>().cost == b.GetComponent<Item>().cost)
                            return a.name.CompareTo(b.name);
                        return a.GetComponent<Item>().cost.CompareTo(b.GetComponent<Item>().cost);
                    }
                    return a.GetComponent<Item>().slotType.CompareTo(b.GetComponent<Item>().slotType);
                }
                if(a.GetComponent<Item>().y == b.GetComponent<Item>().y)
                {
                    return b.GetComponent<Item>().x.CompareTo(a.GetComponent<Item>().x);
                }
                return b.GetComponent<Item>().y.CompareTo(a.GetComponent<Item>().y);
            });

            foreach (Transform item in items)
            {
                /*
                if (item.GetComponent<Item>().y == 1)
                {
                    for (int i = 0; i < storage.x; i++)
                    {
                        int j = storage.y - 1;
                        if (storage.AddItemToSlot(item, new Vector2Int(i, j), false, false))
                        {
                            item.GetComponent<Item>().posX = i;
                            item.GetComponent<Item>().posY = j;
                            //Dbgl($"Added {item.name} to {i},{j}");
                            goto cont;
                        }
                    }
                }
                else if (item.GetComponent<Item>().y == 2)
                {
                    for (int i = 0; i < storage.x; i++)
                    {
                        int j = storage.y - 2;
                        if (storage.AddItemToSlot(item, new Vector2Int(i, j), false, false))
                        {
                            item.GetComponent<Item>().posX = i;
                            item.GetComponent<Item>().posY = j;
                            //Dbgl($"Added {item.name} to {i},{j}");
                            goto cont;
                        }
                    }
                }
                */
                for (int i = 0; i < storage.x; i++)
                {
                    for (int j = 0; j < storage.y; j++)
                    {
                        if (AddItemToSlot(storage, item, new Vector2Int(i, j)))
                        {
                            item.GetComponent<Item>().posX = i;
                            item.GetComponent<Item>().posY = j;
                            //Dbgl($"Added {item.name} to {i},{j}");
                            goto cont;
                        }
                    }
                }
                cont:
                continue;
            }
            storage.inventory.Refresh();
            watch.Stop();
            Dbgl($"sorted in {watch.Elapsed.TotalSeconds} seconds");
        }

        private static bool AddItemToSlot(Storage storage, Transform item, Vector2Int vec)
        {
            Item component = item.GetComponent<Item>();
            List<Vector2Int> list = new List<Vector2Int>();
            if (item.GetComponent<Item>().itemType == ItemType.lingerie)
            {
                MonoBehaviour.print(item.name);
                Global.code.playerLingerieStorage.AddItemToCollection(item, true, true);
                UnityEngine.Object.Destroy(item.gameObject);
                return true;
            }
            for (int i = 0; i < item.GetComponent<Item>().x; i++)
            {
                for (int j = 0; j < item.GetComponent<Item>().y; j++)
                {
                    list.Add(vec + new Vector2Int(i, j));
                }
            }
            List<InventorySlot> list2 = new List<InventorySlot>();
            foreach (Vector2Int vector2Int in list)
            {
                if (vector2Int.x >= storage.x || vector2Int.y >= storage.y)
                {
                    return false;
                }
                InventorySlot component2 = storage.inventory.slots[vector2Int.x, vector2Int.y].GetComponent<InventorySlot>();
                list2.Add(component2);
                if (!component2 || component2.item)
                {
                    return false;
                }
            }
            foreach (InventorySlot inventorySlot in list2)
            {
                inventorySlot.item = item;
                inventorySlot.GetComponent<RawImage>().raycastTarget = false;
                component.occupliedSlots.Add(inventorySlot);
            }
            component.isGoods = storage.GetComponent<Merchant>();
            component.owner = storage.transform;
            component.dropImpactPlayed = false;
            component.posX = vec.x;
            component.posY = vec.y;
            if (item.GetComponent<Interaction>())
            {
                Destroy(item.GetComponent<Interaction>());
            }
            if (item.GetComponent<MapIcon>())
            {
                Destroy(item.GetComponent<MapIcon>());
            }
            storage.items.AddItem(item);
            item.gameObject.SetActive(false);
            item.SetParent(storage.transform);
            return true;
        }

        private static void RemoveItem(Storage storage, int i)
        {
            Transform item = storage.items.items[i];
            foreach (InventorySlot inventorySlot in item.GetComponent<Item>().occupliedSlots)
            {
                if (inventorySlot)
                {
                    inventorySlot.item = null;
                    inventorySlot.GetComponent<RawImage>().raycastTarget = true;
                }
            }
            storage.items.RemoveItem(item);
            item.GetComponent<Item>().occupliedSlots.Clear();
        }
    }
}
