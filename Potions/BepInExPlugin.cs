using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Potions
{
    [BepInPlugin("aedenthorn.Potions", "Potions", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static Dictionary<string, PotionData> potionDataDict = new Dictionary<string, PotionData>();

        private static string assetPath;

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
            nexusID = Config.Bind<int>("General", "NexusID", 36, "Nexus mod ID for updates");

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
            }
            else
            {
                LoadPotionData();
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        private void LoadPotionData()
        {
            foreach (string file in Directory.GetFiles(assetPath, "*.json"))
            {
                PotionData pd = JsonUtility.FromJson<PotionData>(File.ReadAllText(file));
                string filename = Path.Combine(assetPath, Path.GetFileNameWithoutExtension(file));
                if (File.Exists(filename + "_icon.png"))
                {
                    pd.icon = new Texture2D(1, 1);
                    pd.icon.LoadImage(File.ReadAllBytes(filename + "_icon.png"));
                }
                if (File.Exists(filename + "_3D.png"))
                {
                    pd.texture = new Texture2D(1, 1);
                    pd.texture.LoadImage(File.ReadAllBytes(filename + "_3D.png"));
                }
                if (File.Exists(filename + "_E.png"))
                {
                    pd.emission = new Texture2D(1, 1);
                    pd.emission.LoadImage(File.ReadAllBytes(filename + "_E.png"));
                }
                potionDataDict.Add(pd.id, pd);
                Dbgl($"Added potion {pd.id}");
            }
        }

        [HarmonyPatch(typeof(RM), "LoadResources")]
        static class LoadResources_Patch
        {
            static void Postfix(RM __instance)
            {
                if (!modEnabled.Value)
                    return;
                Transform template = __instance.allPotions.items[0];
                foreach(PotionData pd in potionDataDict.Values)
                {
                    Transform t = Instantiate(template);
                    t.name = pd.id;
                    DontDestroyOnLoad(t.gameObject);

                    DestroyImmediate(t.GetComponent<Potion>());
                    t.gameObject.AddComponent<Potion>();
                    Item item = t.GetComponent<Item>();
                    item.nameEN = pd.nameEN;
                    item.nameCN = pd.nameCN;
                    item.nameRU = pd.nameRU;
                    item.itemType = ItemType.item;
                    item.itemName = pd.id;
                    item.autoPickup = true;
                    item.rarity = (Rarity) pd.rarity;
                    item.cost = pd.cost;

                    if (pd.icon != null)
                        item.icon = pd.icon;
                    if(pd.texture != null)
                    {
                        Dbgl("Adding custom texture for " + pd.id);
                        MeshRenderer mr = t.GetComponent<MeshRenderer>();

                        foreach(string prop in mr.material.GetTexturePropertyNames())
                        {
                            mr.material.SetTexture(prop, null);
                        }

                        mr.material.mainTexture = pd.texture;
                        if (pd.emission != null)
                        {
                            mr.material.SetTexture("_EmissiveColorMap", pd.texture);
                        }
                    }

                    __instance.allPotions.AddItemDifferentName(t);

                    Dbgl($"Added potion to RM: {pd.id}");
                }
            }
        }
        //[HarmonyPatch(typeof(LootDrop), "DropRandomItems")]
        static class DropRandomItems_Patch
        {
            static void Postfix(LootDrop __instance)
            {
                if (!modEnabled.Value)
                    return;

                foreach(Transform t in RM.code.allPotions.items)
                {
                    Transform transform = Utility.Instantiate(t);
                    RM.code.balancer.GetItemStats(transform, 0);
                    transform.position = __instance.transform.position + new Vector3(0f, 1.2f, 0f);
                    transform.GetComponent<Collider>().enabled = false;
                    transform.GetComponent<Collider>().enabled = true;
                    transform.GetComponent<Rigidbody>().isKinematic = false;
                    transform.GetComponent<Rigidbody>().useGravity = true;
                    transform.GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(-160, 160), Random.Range(0, 200), Random.Range(-160, 160)));
                    transform.GetComponent<Rigidbody>().AddTorque(new Vector3(Random.Range(-160, 160), Random.Range(-160, 160), Random.Range(-160, 160)));
                    transform.GetComponent<Item>().Drop();
                }
            }
        }
        //[HarmonyPatch(typeof(LootDrop), "DropItem")]
        static class DropItem_Patch
        {
            static void Prefix(Transform item)
            {
                if (!modEnabled.Value || !item.GetComponent<Potion>())
                    return;
                Dbgl($"trying to drop {item.name}");
            }
        }

        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Postfix(Player __instance)
            {
                if (!modEnabled.Value || __instance.gameObject.tag == "D")
                    return;

                foreach (PotionData pd in potionDataDict.Values)
                {
                    if (AedenthornUtils.CheckKeyDown(pd.hotKey))
                    {
                        Dbgl($"Pressed {pd.hotKey}, using potion {pd.id}");

                        foreach (Transform transform in __instance.customization.storage.items.items)
                        {
                            if (transform && transform.GetComponent<Potion>() && transform.name == pd.id)
                            {
                                UsePotion(__instance.customization, transform.GetComponent<Item>(), pd);
                                Player.code.customization.storage.RemoveItem(transform);
                                return;
                            }
                        }

                    }
                }
            }
        }   
        
        [HarmonyPatch(typeof(Balancer), nameof(Balancer.GetPotionStats))]
        static class Balancer_GetPotionStats_Patch
        {
            static bool Prefix(Item item)
            {
                if (!modEnabled.Value || !potionDataDict.ContainsKey(item.name))
                    return true;
                item.cost = potionDataDict[item.name].cost;
                return false;
            }
        }
                
        [HarmonyPatch(typeof(Item), nameof(Item.Use))]
        static class Item_Use_Patch
        {
            static bool Prefix(Item __instance, CharacterCustomization user)
            {
                if (!modEnabled.Value || !__instance.GetComponent<Potion>() || !potionDataDict.ContainsKey(__instance.name))
                    return true;

                UsePotion(user, __instance, potionDataDict[__instance.name]);
                Player.code.customization.storage.RemoveItem(__instance.transform);
                return false;
            }
        }

        private static void UsePotion(CharacterCustomization customization, Item item, PotionData pd)
        {
            if (item.sndUse)
                RM.code.PlayOneShot(item.sndUse);
            else
                RM.code.PlayOneShot(RM.code.sndPickupGenerics[Random.Range(0, RM.code.sndPickupGenerics.Length - 1)]); 
            
            float mult = 1f + customization.alchemist * 0.15f;

            if (pd.health > 0)
            {
                customization._ID.AddHealth(pd.health * mult, Player.code.transform);
                Dbgl($"Potion added {pd.health * mult} health");
            }
            if (pd.mana > 0)
            {
                customization._ID.AddMana(pd.mana * mult);
                Dbgl($"Potion added {pd.mana * mult} mana");
            }
            if (pd.stamina > 0)
            {
                customization._ID.AddStamina(pd.stamina * mult, 0);
                Dbgl($"Potion added {pd.stamina * mult} stamina");
            }
            if (pd.rage > 0)
            {
                customization._ID.AddRage(pd.rage * mult);
                Dbgl($"Potion added {pd.rage * mult} rage");
            }
            if (pd.skillPoints > 0)
            {
                customization._ID.skillPoints += Mathf.RoundToInt(pd.skillPoints * mult);
                Dbgl($"Potion added {Mathf.RoundToInt(pd.skillPoints * mult)} skill points");
            }
            if (pd.attributePoints > 0)
            {
                customization._ID.attributePoints += Mathf.RoundToInt(pd.attributePoints * mult);
                Dbgl($"Potion added {Mathf.RoundToInt(pd.attributePoints * mult)} attribute points");
            }
        }
    }
}
