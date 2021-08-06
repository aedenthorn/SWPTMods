using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace LootVacuum
{
    [BepInPlugin("aedenthorn.LootVacuum", "Loot Vacuum", "0.1.3")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<float> maxVacuumDistance;
        public static ConfigEntry<float> vacuumVelocity;
        public static ConfigEntry<bool> vacuumWeapons;
        public static ConfigEntry<bool> vacuumArmor;
        public static ConfigEntry<bool> vacuumItems;
        public static ConfigEntry<bool> vacuumGold;
        public static ConfigEntry<bool> vacuumLingerie;
        public static ConfigEntry<bool> vacuumCrystals;
        public static ConfigEntry<bool> vacuumPotions;

        public static BepInExPlugin context;
        public static List<Transform> movingTransformList = new List<Transform>();
        public static List<Transform> ignoreTransformList = new List<Transform>();

        private static SlotType[] armorSlotTypes = new SlotType[] { SlotType.armor, SlotType.shield, SlotType.gloves, SlotType.helmet, SlotType.legging, SlotType.shoes };

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
            nexusID = Config.Bind<int>("General", "NexusID", 55, "Nexus mod ID for updates");

            vacuumVelocity = Config.Bind<float>("Options", "VacuumVelocity", 3f, "Vacuum veloctiy.");
            maxVacuumDistance = Config.Bind<float>("Options", "MaxVacuumDistance", 10f, "Max vacuum distance.");

            vacuumWeapons = Config.Bind<bool>("Toggles", "VacuumWeapons", true, "Vacuum weapons.");
            vacuumArmor = Config.Bind<bool>("Toggles", "VacuumArmor", true, "Vacuum armour.");
            vacuumLingerie = Config.Bind<bool>("Toggles", "VacuumLingerie", true, "Vacuum lingerie.");
            vacuumPotions = Config.Bind<bool>("Toggles", "VacuumPotions", true, "Vacuum misc. items.");
            vacuumItems = Config.Bind<bool>("Toggles", "VacuumItems", true, "Vacuum misc. items.");
            vacuumGold = Config.Bind<bool>("Toggles", "VacuumGold", true, "Vacuum gold.");
            vacuumCrystals = Config.Bind<bool>("Toggles", "VacuumCrystals", true, "Vacuum crystals.");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }
        private void Update()
        {
            if (!modEnabled.Value || !Player.code || movingTransformList.Count == 0)
                return;
            for (int i = movingTransformList.Count - 1; i >= 0; i--)
            {
                if (!movingTransformList[i] || ignoreTransformList.Contains(movingTransformList[i]))
                {
                    movingTransformList.RemoveAt(i);
                    continue;
                }
                movingTransformList[i].position = Vector3.MoveTowards(movingTransformList[i].position, Player.code.transform.position, Time.deltaTime * vacuumVelocity.Value);
                if (Vector3.Distance(movingTransformList[i].position, Player.code.transform.position) < 0.05f)
                {
                    if (movingTransformList[i].name == "Gold" || movingTransformList[i].name == "Crystals")
                    {
                        movingTransformList[i].GetComponent<Item>().InitiateInteract();
                        return;
                    }
                    else
                        ignoreTransformList.Add(movingTransformList[i]);
                    movingTransformList.RemoveAt(i);
                }
            }
            for (int i = ignoreTransformList.Count - 1; i >= 0; i--)
            {
                if(!ignoreTransformList[i])
                    ignoreTransformList.RemoveAt(i);
            }
        }
        [HarmonyPatch(typeof(Player), nameof(Player.CS))]
        static class Player_CS_Patch
        {
            static void Postfix(Player __instance)
            {
                if (!modEnabled.Value || Global.code.onGUI)
                    return;

                foreach (Transform t in Global.code.interactions.items)
                {
                    Item item = t.GetComponent<Item>();

                    if (t && t.gameObject.activeSelf && item && !ignoreTransformList.Contains(t))
                    {
                        if (Vector3.Distance(t.position, Player.code.transform.position) <= maxVacuumDistance.Value && t != Player.code.transform)
                        {
                            if (item.slotType == SlotType.weapon)
                            {
                                if (!vacuumWeapons.Value)
                                    continue;
                            }
                            else if (armorSlotTypes.Contains(item.slotType))
                            {
                                if (!vacuumArmor.Value)
                                    continue;
                            }
                            else if (item.GetComponent<Potion>())
                            {
                                if (!vacuumPotions.Value)
                                    continue;
                            }
                            else if (item.itemType == ItemType.lingerie)
                            {
                                if (!vacuumLingerie.Value)
                                    continue;
                            }
                            else if (item.name == "Gold")
                            {
                                if (!vacuumGold.Value)
                                    continue;
                            }
                            else if (item.name == "Crystals")
                            {
                                if (!vacuumCrystals.Value)
                                    continue;
                            }
                            else if (!vacuumItems.Value)
                                continue;

                            movingTransformList.Add(t);
                        }
                    }
                }
            }
        }
    }
}
