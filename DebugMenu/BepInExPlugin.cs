using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DebugMenu
{
    [BepInPlugin("aedenthorn.DebugMenu", "Debug Menu", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<string> language;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<bool> levelBypass;

        public static Transform uiDebug;

        public static Transform lastSelected;
        private static Transform uiSpawnItem;
        private GameObject spawnInput;

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
            language = Config.Bind<string>("General", "Language", "en", "Name of language file to use.");
            nexusID = Config.Bind<int>("General", "NexusID", 7, "Nexus mod ID for updates");

            levelBypass = Config.Bind<bool>("Options", "LevelBypass", false, "Enable level bypass for equipment");
            hotKey = Config.Bind<string>("Options", "HotKey", "f4", "Hotkey to toggle debug menu. Use https://docs.unity3d.com/Manual/class-InputManager.html");



            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        public void Update()
        {
            if (!modEnabled.Value || !Global.code)
                return;

            if(uiDebug == null && Global.code.uiCheat)
            {
                CreateDebugMenu();
                
            }

            if (AedenthornUtils.CheckKeyDown(hotKey.Value))
            {
                Dbgl("Toggling debug menu");

                if (Global.code.uiCheat.gameObject.activeSelf && !uiDebug.gameObject.activeSelf)
                    Global.code.uiCheat.gameObject.SetActive(false);
                uiDebug.gameObject.SetActive(!uiDebug.gameObject.activeSelf);
            }
        }

        private void CreateDebugMenu()
        {
            Dbgl("Creating debug menu");
            int c = 0;

            uiDebug = Instantiate(Global.code.uiCheat.transform, Global.code.uiCheat.transform.parent);
            uiDebug.name = "Debug Menu";
            Transform buttonList = uiDebug.GetComponentInChildren<VerticalLayoutGroup>().transform;

            string[] names = File.ReadAllLines(Path.Combine(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace), $"{language.Value}.txt"));

            // Dump

            int count = 0;

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(DumpItems);
            count++;

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(DumpPoses);
            count++;

            // Toggle

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(delegate() { levelBypass.Value = !levelBypass.Value; });
            count++;

            // Spawn

            buttonList.GetChild(count).name = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Text>().text = names[count];
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick = new Button.ButtonClickedEvent();
            buttonList.GetChild(count).GetComponentInChildren<Button>().onClick.AddListener(OpenSpawnItemUI);
            count++;

            while(count < buttonList.childCount)
            {
                if (buttonList.GetChild(count))
                    buttonList.GetChild(count).gameObject.SetActive(false);
                count++;
            }
        }

        private void OpenSpawnItemUI()
        {
            if(uiSpawnItem == null)
            {
                Dbgl("Opening Spawn Item UI");
                Global.code.onGUI = true;
                uiSpawnItem = new GameObject() {name = "Spawn Item UI" }.transform;
                uiSpawnItem.SetParent(Global.code.uiCheat.transform.parent);
                uiSpawnItem.localPosition = Vector3.zero;
                uiSpawnItem.localScale = Vector3.one;

                Transform bkg = Instantiate(Global.code.uiCombat.descriptionsPanel.transform.Find("panel/BG Inventory (3)"), uiSpawnItem);
                bkg.name = "Background";

                spawnInput = Instantiate(Global.code.uiNameChanger.nameinput.gameObject, uiSpawnItem);
                spawnInput.name = "Input Field";
                spawnInput.GetComponent<InputField>().onValueChanged = new InputField.OnChangeEvent();

                GameObject buttonGroup = Instantiate(Mainframe.code.uiConfirmation.groupYesNo, uiSpawnItem);
                buttonGroup.name = "Buttons";
                buttonGroup.transform.SetParent(uiSpawnItem);

                Button spawn = buttonGroup.transform.Find("yes").GetComponent<Button>();
                spawn.onClick = new Button.ButtonClickedEvent();
                spawn.onClick.AddListener(SpawnItem);

                Button cancel = buttonGroup.transform.Find("no").GetComponent<Button>();
                cancel.onClick = new Button.ButtonClickedEvent();
                cancel.onClick.AddListener(delegate() { Global.code.onGUI = false; uiSpawnItem.gameObject.SetActive(false); });
            }
        }

        private void SpawnItem()
        {
            string spawnName = spawnInput.GetComponent<InputField>().text;
            Transform item = Utility.Instantiate(RM.code.allItems.GetItemWithName(spawnName));
            if (item != null)
            {
                RM.code.balancer.GetItemStats(item, -1);
                item.GetComponent<Item>().autoPickup = false;
                item.GetComponent<Collider>().enabled = false;
                item.GetComponent<Collider>().enabled = true;
                item.GetComponent<Rigidbody>().isKinematic = false;
                item.GetComponent<Rigidbody>().useGravity = true;
                item.position = Player.code.transform.position + new Vector3(0f, 1f, 0f);
                item.position += Player.code.transform.forward * 1f;
                item.GetComponent<Rigidbody>().AddForce(base.transform.forward * 10f);
                item.GetComponent<Rigidbody>().AddTorque(new Vector3(1000f, 1000f, 1000f));
                item.SetParent(null);
                item.GetComponent<Item>().owner = null;
                item.GetComponent<Item>().Drop();
                item.gameObject.SetActive(true);
                Dbgl($"Spawned {item.name}");
            }
            else
                Dbgl($"Couldn't find {spawnName} to spawn");
            Global.code.onGUI = false;
            uiSpawnItem.gameObject.SetActive(false);
        }

        private void DumpPoses()
        {
            Dictionary<string, List<string>> poses = new Dictionary<string, List<string>>();
            foreach (Transform t in RM.code.allFreePoses.items)
            {
                string cn = t.GetComponent<Pose>().categoryName;
                if (!poses.ContainsKey(cn))
                    poses.Add(cn, new List<string>());
                poses[cn].Add(t.name);
            }

            string output = "Poses:";

            foreach(var kvp in poses)
            {
                output += "\n\n\t" + kvp.Key + "\n\t\t" + string.Join("\n\t\t", kvp.Value);
            }

            string path = Path.Combine(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace), "dump_poses.txt");
            File.WriteAllText(path, output);
            Dbgl($"Dumped poses to {path}");
        }

        private void DumpItems()
        {
            List<string> armors = new List<string>();
            foreach (Transform t in RM.code.allArmors.items)
                armors.Add(t.name);

            List<string> blackMarket = new List<string>();
            foreach (Transform t in RM.code.allBlackmarketItems.items)
                blackMarket.Add(t.name);

            List<string> weapons = new List<string>();
            foreach (Transform t in RM.code.allWeapons.items)
                weapons.Add(t.name);

            List<string> necklaces = new List<string>();
            if(RM.code.allNecklaces?.items != null)
                foreach (Transform t in RM.code.allNecklaces.items)
                    necklaces.Add(t.name);

            List<string> potions = new List<string>();
            foreach (Transform t in RM.code.allPotions.items)
                potions.Add(t.name);

            List<string> rings = new List<string>();
            if (RM.code.allRings?.items != null)
                foreach (Transform t in RM.code.allRings.items)
                    rings.Add(t.name);

            List<string> special = new List<string>();
            if (RM.code.allSpecialItems?.items != null)
                foreach (Transform t in RM.code.allSpecialItems.items)
                    special.Add(t.name);

            List<string> treasures = new List<string>();
            if (RM.code.allTreasures?.items != null)
                foreach (Transform t in RM.code.allTreasures.items)
                    treasures.Add(t.name);

            string output =
                "Armors:\n\n\t" + string.Join("\n\t", armors)
                + "\n\n\nWeapons:\n\t" + string.Join("\n\t", weapons)
                + "\n\n\nBlack Market Items:\n\t" + string.Join("\n\t", blackMarket)
                + "\n\n\nNecklaces:\n\t" + string.Join("\n\t", necklaces)
                + "\n\n\nPotions:\n\t" + string.Join("\n\t", potions)
                + "\n\n\nRings:\n\t" + string.Join("\n\t", rings)
                + "\n\n\nSpecial Items:\n\t" + string.Join("\n\t", special)
                + "\n\n\nTreasures:\n\t" + string.Join("\n\t", treasures);

            string path = Path.Combine(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace), "dump_items.txt");
            File.WriteAllText(path, output);
            Dbgl($"Dumped items to {path}");
        }


        [HarmonyPatch(typeof(Global), "HandleKeys")]
        public static class HandleKeys_Patch
        {
            public static bool Prefix()
            {
                if (!modEnabled.Value || uiSpawnItem?.gameObject.activeSelf != true)
                    return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Global), "CheckOnGUI")]
        public static class CheckOnGUI_Patch
        {
            public static bool Prefix()
            {
                if (!modEnabled.Value || (uiDebug?.gameObject.activeSelf != true && uiSpawnItem?.gameObject.activeSelf != true))
                    return true;

                Global.code.uiCombat.HideHint();
                Global.code.onGUI = true;
                Cursor.SetCursor(RM.code.cursorNormal, Vector2.zero, CursorMode.Auto);
                Global.code.uiCombat.hud.SetActive(false);
                Time.timeScale = 1f;
                return false;
            }
        }

        [HarmonyPatch(typeof(EquipmentSlot), nameof(EquipmentSlot.Click))]
        public static class Click_Patch
        {
            public static void Prefix(EquipmentSlot __instance, ref int __state)
            {
                if (!modEnabled.Value || !levelBypass.Value || !Global.code.selectedItem)
                    return;
                lastSelected = Global.code.selectedItem;
                __state = lastSelected.GetComponent<Item>().levelrequirement;
                lastSelected.GetComponent<Item>().levelrequirement = 0;
            }
            public static void Postfix(EquipmentSlot __instance, int __state)
            {
                if (!modEnabled.Value || !levelBypass.Value || lastSelected == null)
                    return;
                lastSelected.GetComponent<Item>().levelrequirement = __state;
                lastSelected = null;
            }
        }
    }
}
