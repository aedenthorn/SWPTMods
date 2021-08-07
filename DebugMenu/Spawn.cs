using BepInEx;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DebugMenu
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static void OpenSpawnItemUI()
        {
            Dbgl("Opening Spawn Item UI");
            if (uiSpawnItem == null)
            {
                Global.code.onGUI = true;
                uiSpawnItem = new GameObject() {name = "Spawn Item UI" }.transform;
                uiSpawnItem.SetParent(Global.code.uiCheat.transform.parent);
                uiSpawnItem.localPosition = Vector3.zero;
                uiSpawnItem.localScale = Vector3.one;

                Transform bkg = Instantiate(Global.code.uiCombat.descriptionsPanel.transform.Find("panel/BG Inventory (3)"), uiSpawnItem);
                bkg.name = "Background";

                Text text = Instantiate(Global.code.uiCombat.lineName.transform, uiSpawnItem).GetComponent<Text>();
                text.text = spawnItemTitle.Value;
                text.gameObject.name = "Title";

                spawnInput = Instantiate(Global.code.uiNameChanger.nameinput.gameObject, uiSpawnItem);
                spawnInput.name = "Input Field";
                spawnInput.GetComponent<RectTransform>().anchoredPosition *= new Vector2(0,1);
                spawnInput.GetComponent<InputField>().onValueChanged = new InputField.OnChangeEvent();
                spawnInput.GetComponent<InputField>().onValueChanged.AddListener(UpdateSpawnText); 
                spawnInput.GetComponent<InputField>().placeholder.GetComponent<Text>().text = "";

                GameObject buttonObj = Instantiate(Mainframe.code.uiConfirmation.groupClose, uiSpawnItem);
                buttonObj.name = "Button";
                buttonObj.transform.SetParent(uiSpawnItem);
                buttonObj.SetActive(true);
                
                Button cancel = buttonObj.transform.Find("yes").GetComponent<Button>();
                Destroy(cancel.GetComponentInChildren<LocalizationText>());
                cancel.GetComponentInChildren<Text>().text = cancelText.Value;
                cancel.onClick = new Button.ButtonClickedEvent();
                cancel.onClick.AddListener(delegate() { Global.code.onGUI = false; uiSpawnItem.gameObject.SetActive(false); });

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
            if(arg0 == "")
            {
                spawnHintText.text = "";
                return;
            }

            if (itemNames.Exists(s => s.ToLower() == arg0.ToLower()) || itemNames.Contains(spawnInput.GetComponent<InputField>().placeholder.GetComponent<Text>().text))
            {
                spawnHintText.text = itemNames.Find(s => s.ToLower() == arg0.ToLower());
                return;
            }

            string item = itemNames.Find(s => s.ToLower().StartsWith(arg0.ToLower()));
            if (item != null)
            {
                spawnHintText.text = item;
            }
            else spawnHintText.text = "Item not found";
        }

        private static void SpawnItem()
        {
            string spawnName = spawnHintText.text;
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
    }
}
