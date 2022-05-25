using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DebugMenu
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static void OpenSpawnItemUI()
        {
            Dbgl("Opening Spawn Item UI");
            if (uiSpawnItem == null)
            {
                Dbgl("Creating Spawn Item UI elements");

                Global.code.onGUI = true;
                uiSpawnItem = new GameObject() {name = "Spawn Item UI" }.transform;
                uiSpawnItem.SetParent(Global.code.uiCheat.transform.parent);
                uiSpawnItem.localPosition = Vector3.zero;
                uiSpawnItem.localScale = Vector3.one;

                Transform bkg = Instantiate(Global.code.uiCombat.descriptionsPanel.transform.Find("panel/BG Inventory (3)"), uiSpawnItem);
                bkg.name = "Background";
                bkg.GetComponent<RectTransform>().anchoredPosition *= new Vector2(0, 1);

                Text text = Instantiate(Global.code.uiCombat.lineName.transform, uiSpawnItem).GetComponent<Text>();
                text.text = spawnItemTitle.Value;
                text.gameObject.name = "Title";

                GameObject ti = Instantiate(Global.code.uiNameChanger.nameinput.gameObject, uiSpawnItem);
                ti.name = "Input Field";
                ti.GetComponent<RectTransform>().anchoredPosition *= new Vector2(0,1);
                spawnInput = ti.GetComponent<InputField>();
                spawnInput.text = "";
                spawnInput.onValueChanged = new InputField.OnChangeEvent();
                spawnInput.onValueChanged.AddListener(UpdateSpawnText); 
                spawnInput.placeholder.GetComponent<Text>().text = "Item Name";

                GameObject tp = Instantiate(Global.code.uiNameChanger.nameinput.gameObject, uiSpawnItem);
                tp.name = "Prefix Field";
                tp.GetComponent<RectTransform>().anchoredPosition = spawnInput.GetComponent<RectTransform>().anchoredPosition + new Vector2(0, spawnInput.GetComponent<RectTransform>().rect.height * 2);
                spawnPrefixInput = tp.GetComponent<InputField>();
                spawnPrefixInput.text = "";
                spawnPrefixInput.onValueChanged = new InputField.OnChangeEvent();
                spawnPrefixInput.onValueChanged.AddListener(UpdateSpawnText); 
                spawnPrefixInput.placeholder.GetComponent<Text>().text = "Prefix Name";
                tp.SetActive(false);

                GameObject ts = Instantiate(Global.code.uiNameChanger.nameinput.gameObject, uiSpawnItem);
                ts.name = "Suffix Field";
                ts.GetComponent<RectTransform>().anchoredPosition = spawnInput.GetComponent<RectTransform>().anchoredPosition + new Vector2(0, spawnInput.GetComponent<RectTransform>().rect.height);
                spawnSuffixInput = ts.GetComponent<InputField>();
                spawnSuffixInput.text = "";
                spawnSuffixInput.onValueChanged = new InputField.OnChangeEvent();
                spawnSuffixInput.onValueChanged.AddListener(UpdateSpawnText); 
                spawnSuffixInput.placeholder.GetComponent<Text>().text = "Suffix Name";
                ts.SetActive(false);

                GameObject buttonObj = Instantiate(Mainframe.code.uiConfirmation.groupYesNo, uiSpawnItem);
                buttonObj.name = "Buttons";
                buttonObj.transform.SetParent(uiSpawnItem);
                buttonObj.SetActive(true);
                
                Button cancelB = buttonObj.transform.Find("yes").GetComponent<Button>();
                Destroy(cancelB.GetComponentInChildren<LocalizationText>());
                cancelB.GetComponentInChildren<Text>().text = cancelText.Value;
                cancelB.onClick = new Button.ButtonClickedEvent();
                cancelB.onClick.AddListener(delegate() { Global.code.onGUI = false; uiSpawnItem.gameObject.SetActive(false); });

                Button spawnB = buttonObj.transform.Find("no").GetComponent<Button>();
                Destroy(spawnB.GetComponentInChildren<LocalizationText>());
                spawnB.GetComponentInChildren<Text>().text = spawnText.Value;
                spawnB.onClick = new Button.ButtonClickedEvent();
                spawnB.onClick.AddListener(SpawnItem);

                spawnHintText = Instantiate(Global.code.uiCombat.lineName.transform, uiSpawnItem).GetComponent<Text>();
                spawnHintText.text = "";
                text.gameObject.name = "Hint";
                spawnHintText.GetComponent<RectTransform>().anchoredPosition = spawnInput.GetComponent<RectTransform>().anchoredPosition - new Vector2(0, spawnInput.GetComponent<RectTransform>().rect.height + (spawnInput.GetComponent<RectTransform>().anchoredPosition.y - buttonObj.GetComponent<RectTransform>().anchoredPosition.y) / 2);
                Button spawnHint = spawnHintText.gameObject.AddComponent<Button>();
                spawnHint.onClick.AddListener(SpawnItem);
            }
            uiSpawnItem.gameObject.SetActive(true);
        }

        private static void UpdateSpawnText(string arg0)
        {
            string text = spawnInput.text;
            string itemName = GetNameFromText(text, itemNames);

            if (itemName != null)
            {
                Item item = RM.code.allItems.GetItemWithName(itemName)?.GetComponent<Item>();

                if(item == null)
                {
                    Dbgl($"No item found for name {itemName}");
                    return;
                }

                string affixes = "";

                string prefix = null;
                string suffix = null;

                if (item.slotType == SlotType.weapon)
                {
                    spawnPrefixInput.gameObject.SetActive(true);
                    spawnSuffixInput.gameObject.SetActive(true);
                    prefix = GetNameFromText(spawnPrefixInput.text.Trim(), wPrefixes);
                    suffix = GetNameFromText(spawnSuffixInput.text.Trim(), wSuffixes);
                }
                else if (armorSlotTypes.Contains(item.slotType))
                {
                    spawnPrefixInput.gameObject.SetActive(true);
                    spawnSuffixInput.gameObject.SetActive(true);
                    prefix = GetNameFromText(spawnPrefixInput.text.Trim(), aPrefixes);
                    suffix = GetNameFromText(spawnSuffixInput.text.Trim(), aSuffixes);
                }
                else
                {
                    spawnPrefixInput.gameObject.SetActive(false);
                    spawnSuffixInput.gameObject.SetActive(false);
                }

                if (prefix != null)
                {
                    affixes += prefix + " ";
                }
                if (suffix != null)
                {
                    affixes += suffix + " ";
                }

                spawnHintText.text = affixes + itemName;

            }
            else spawnHintText.text = "";
        }

        private static void SpawnItem()
        {
            Dbgl($"spawnInput: {spawnInput != null}");
            Dbgl($"spawnInput text: {spawnInput.text}");
            string itemName = GetNameFromText(spawnInput.text.Trim(), itemNames);

            if (itemName == null)
            {
                Dbgl($"Couldn't find {spawnInput.text} to spawn");
                return;
            }
            var itemTemp = RM.code.allItems.GetItemWithName(itemName);

            Dbgl($"Got item {itemName}: {itemTemp != null}");
            if(itemTemp == null)
            {
                return;
            }
            Transform item = Utility.Instantiate(itemTemp);
            
            if (item != null)
            {

                SlotType st = item.GetComponent<Item>().slotType;

                string prefix = null;
                string suffix = null;
                if (st == SlotType.weapon)
                {
                    prefix = GetNameFromText(spawnPrefixInput.text.Trim(), wPrefixes);
                    suffix = GetNameFromText(spawnSuffixInput.text.Trim(), wSuffixes);
                    if (prefix != null)
                        item.GetComponent<Item>().prefix = RM.code.weaponPrefixes.GetItemWithName(prefix).GetComponent<Item>();
                    if (suffix != null)
                        item.GetComponent<Item>().surfix = RM.code.weaponSurfixes.GetItemWithName(suffix).GetComponent<Item>();
                }
                else if (armorSlotTypes.Contains(st))
                {
                    prefix = GetNameFromText(spawnPrefixInput.text.Trim(), aPrefixes);
                    suffix = GetNameFromText(spawnSuffixInput.text.Trim(), aSuffixes);
                    if (prefix != null)
                        item.GetComponent<Item>().prefix = RM.code.armorPrefixes.GetItemWithName(prefix).GetComponent<Item>();
                    if (suffix != null)
                        item.GetComponent<Item>().surfix = RM.code.armorSurfixes.GetItemWithName(suffix).GetComponent<Item>();
                }

                Transform parentItem;
                if (!item.GetComponent<Collider>())
                {
                    parentItem = item.GetComponent<Item>().InstantiateModel(null);
                    Destroy(parentItem.GetComponent<Item>());
                    item.SetParent(parentItem);
                    item.localPosition = Vector3.zero;
                }
                else
                {
                    parentItem = item;
                }

                RM.code.balancer.GetItemStats(item, 0);
                parentItem.GetComponent<Item>().autoPickup = false;
                parentItem.position = Player.code.transform.position + new Vector3(0f, 1.2f, 0f);
                parentItem.GetComponent<Collider>().enabled = false;
                parentItem.GetComponent<Collider>().enabled = true;
                parentItem.GetComponent<Rigidbody>().isKinematic = false;
                parentItem.GetComponent<Rigidbody>().useGravity = true;
                parentItem.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                item.GetComponent<Item>().Drop();
                Dbgl($"Spawned {spawnHintText.text}");
                Global.code.uiCombat.ShowHeader($"Spawned {spawnHintText.text}");
            }
            else
            {
                Dbgl($"Couldn't find {itemName} to spawn");
                return;
            }
            Global.code.onGUI = false;
            uiSpawnItem.gameObject.SetActive(false);
        }
        private static string GetNameFromText(string text, List<string> names)
        {
            string rs = null;
            if (text == "")
            {
                return null;
            }
            else if (names.Exists(s => s.ToLower() == text.ToLower()))
            {
                rs = names.Find(s => s.ToLower() == text.ToLower());
            }
            else
            {
                rs = names.Find(s => s.ToLower().StartsWith(text.ToLower()));
            }
            return rs;
        }
    }
}
