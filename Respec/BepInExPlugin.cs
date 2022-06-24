using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace Respec
{
    [BepInPlugin("aedenthorn.Respec", "Respec", "0.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<string> modKey;
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

            modKey = Config.Bind<string>("Options", "ModKey", "left shift", "Modifier key to subtract point from skills. Use https://docs.unity3d.com/Manual/class-InputManager.html");

            nexusID = Config.Bind<int>("General", "NexusID", 4, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }


        [HarmonyPatch(typeof(SkillBox), nameof(SkillBox.ButtonClick))]
        static class SkillBox_ButtonClick_Patch
        {
            static bool Prefix(SkillBox __instance)
            {
                if (!modEnabled.Value || !AedenthornUtils.CheckKeyHeld(modKey.Value))
                    return true;

                if (__instance.points == 0)
                    return false;

                switch (__instance.transform.name)
                {
                    case "frostbite":
                        Global.code.uiCharacter.curCustomization.frostbite--;
                        break;
                    case "ignorpain":
                        Global.code.uiCharacter.curCustomization.ignorpain--;
                        break;
                    case "focus":
                        Global.code.uiCharacter.curCustomization.focus--;
                        break;
                    case "recreation":
                        Global.code.uiCharacter.curCustomization.recreation--;
                        break;
                    case "bowproficiency":
                        Global.code.uiCharacter.curCustomization.bowproficiency--;
                        break;
                    case "swordproficiency":
                        Global.code.uiCharacter.curCustomization.swordproficiency--;
                        break;
                    case "healaura":
                        Global.code.uiCharacter.curCustomization.healaura--;
                        break;
                    case "axeproficiency":
                        Global.code.uiCharacter.curCustomization.axeproficiency--;
                        break;
                    case "atheletic":
                        Global.code.uiCharacter.curCustomization.atheletic--;
                        break;
                    case "daggerproficiency":
                        Global.code.uiCharacter.curCustomization.daggerproficiency--;
                        break;
                    case "phatomslashes":
                        Global.code.uiCharacter.curCustomization.phatomslashes--;
                        break;
                    case "maceproficiency":
                        Global.code.uiCharacter.curCustomization.maceproficiency--;
                        break;
                    case "fireball":
                        Global.code.uiCharacter.curCustomization.fireball--;
                        break;
                    case "crystalhunter":
                        Global.code.uiCharacter.curCustomization.crystalhunter--;
                        break;
                    case "elementalresistence":
                        Global.code.uiCharacter.curCustomization.elementalresistence--;
                        break;
                    case "scavenger":
                        Global.code.uiCharacter.curCustomization.scavenger--;
                        break;
                    case "trading":
                        Global.code.uiCharacter.curCustomization.trading--;
                        break;
                    case "painfulscream":
                        Global.code.uiCharacter.curCustomization.painfulscream--;
                        break;
                    case "blocking":
                        Global.code.uiCharacter.curCustomization.blocking--;
                        break;
                    case "ironbody":
                        Global.code.uiCharacter.curCustomization.ironbody--;
                        break;
                    case "fastlearner":
                        Global.code.uiCharacter.curCustomization.fastlearner--;
                        break;
                    case "hardenedskin":
                        Global.code.uiCharacter.curCustomization.hardenedskin--;
                        break;
                    case "dodging":
                        Global.code.uiCharacter.curCustomization.dodging--;
                        break;
                    case "alchemist":
                        Global.code.uiCharacter.curCustomization.alchemist--;
                        break;
                    case "commanding":
                        Global.code.uiCharacter.curCustomization.commanding--;
                        break;
                    case "spearproficiency":
                        Global.code.uiCharacter.curCustomization.spearproficiency--;
                        break;
                    case "goldenhand":
                        Global.code.uiCharacter.curCustomization.goldenhand--;
                        break;
                }
                Global.code.uiCharacter.curCustomization._ID.skillPoints++;

                Global.code.uiCharacter.Refresh();
                RM.code.PlayOneShot(RM.code.sndUpgradeSkill);

                return false;
            }

        }

        [HarmonyPatch(typeof(UICharacter), nameof(UICharacter.Refresh))]
        static class UICharacter_Refresh_Patch
        {
            static void Postfix(UICharacter __instance)
            {
                if (!modEnabled.Value)
                    return;
                if(!__instance.addStrength.transform.parent.Find("Sub strength"))
                {
                    GameObject subStrength = Instantiate(__instance.addStrength, __instance.addStrength.transform.parent);
                    subStrength.name = "Sub strength";
                    subStrength.GetComponent<RectTransform>().anchoredPosition = __instance.addStrength.GetComponent<RectTransform>().anchoredPosition - new Vector2(__instance.addStrength.GetComponent<RectTransform>().rect.width + __instance.addStrength.transform.parent.GetComponent<RectTransform>().rect.width - 2, 0);
                    subStrength.transform.GetComponentInChildren<Text>().text = "-";
                    subStrength.transform.GetComponentInChildren<Text>().GetComponent<RectTransform>().anchoredPosition += new Vector2(0,1);
                    subStrength.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                    subStrength.GetComponent<Button>().onClick.AddListener(SubtractStrength);
                    
                    GameObject subAgility = Instantiate(__instance.addAgility, __instance.addAgility.transform.parent);
                    subAgility.name = "Sub agility";
                    subAgility.GetComponent<RectTransform>().anchoredPosition = __instance.addAgility.GetComponent<RectTransform>().anchoredPosition - new Vector2(__instance.addAgility.GetComponent<RectTransform>().rect.width + __instance.addAgility.transform.parent.GetComponent<RectTransform>().rect.width - 2, 0);
                    subAgility.transform.GetComponentInChildren<Text>().text = "-";
                    subAgility.transform.GetComponentInChildren<Text>().GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 1);
                    subAgility.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                    subAgility.GetComponent<Button>().onClick.AddListener(SubtractAgility);
                    
                    GameObject subVitality = Instantiate(__instance.addVitality, __instance.addVitality.transform.parent);
                    subVitality.name = "Sub vitality";
                    subVitality.GetComponent<RectTransform>().anchoredPosition = __instance.addVitality.GetComponent<RectTransform>().anchoredPosition - new Vector2(__instance.addVitality.GetComponent<RectTransform>().rect.width + __instance.addVitality.transform.parent.GetComponent<RectTransform>().rect.width - 2, 0);
                    subVitality.transform.GetComponentInChildren<Text>().text = "-";
                    subVitality.transform.GetComponentInChildren<Text>().GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 1);
                    subVitality.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                    subVitality.GetComponent<Button>().onClick.AddListener(SubtractVitality);
                    
                    GameObject subPower = Instantiate(__instance.addPower, __instance.addPower.transform.parent);
                    subPower.name = "Sub power";
                    subPower.GetComponent<RectTransform>().anchoredPosition = __instance.addPower.GetComponent<RectTransform>().anchoredPosition - new Vector2(__instance.addPower.GetComponent<RectTransform>().rect.width + __instance.addPower.transform.parent.GetComponent<RectTransform>().rect.width - 2, 0);
                    subPower.transform.GetComponentInChildren<Text>().text = "-";
                    subPower.transform.GetComponentInChildren<Text>().GetComponent<RectTransform>().anchoredPosition += new Vector2(0, 1);
                    subPower.GetComponent<Button>().onClick = new Button.ButtonClickedEvent();
                    subPower.GetComponent<Button>().onClick.AddListener(SubtractPower);
                }
                __instance.addStrength.transform.parent.Find("Sub strength").gameObject.SetActive(__instance.curCustomization._ID.strength > 0);
                __instance.addAgility.transform.parent.Find("Sub agility").gameObject.SetActive(__instance.curCustomization._ID.agility > 0);
                __instance.addVitality.transform.parent.Find("Sub vitality").gameObject.SetActive(__instance.curCustomization._ID.vitality > 0);
                __instance.addPower.transform.parent.Find("Sub power").gameObject.SetActive(__instance.curCustomization._ID.power > 0);
            }

        }
        private static void SubtractStrength()
        {
            if (Global.code.uiCharacter.curCustomization._ID.strength == 0)
                return;
            Global.code.uiCharacter.curCustomization._ID.attributePoints++;
            Global.code.uiCharacter.curCustomization._ID.strength--;
            Global.code.uiCharacter.curCustomization.UpdateStats();
            RM.code.PlayOneShot(RM.code.sndAddAttribute);
            Global.code.uiCharacter.Refresh();
        }
        private static void SubtractAgility()
        {
            if (Global.code.uiCharacter.curCustomization._ID.agility == 0)
                return;
            Global.code.uiCharacter.curCustomization._ID.attributePoints++;
            Global.code.uiCharacter.curCustomization._ID.agility--;
            Global.code.uiCharacter.curCustomization.UpdateStats();
            RM.code.PlayOneShot(RM.code.sndAddAttribute);
            Global.code.uiCharacter.Refresh();
        }
        private static void SubtractVitality()
        {
            if (Global.code.uiCharacter.curCustomization._ID.vitality == 0)
                return;
            Global.code.uiCharacter.curCustomization._ID.attributePoints++;
            Global.code.uiCharacter.curCustomization._ID.vitality--;
            Global.code.uiCharacter.curCustomization.UpdateStats();
            RM.code.PlayOneShot(RM.code.sndAddAttribute);
            Global.code.uiCharacter.Refresh();
        }
        private static void SubtractPower()
        {
            if (Global.code.uiCharacter.curCustomization._ID.power == 0)
                return;
            Global.code.uiCharacter.curCustomization._ID.attributePoints++;
            Global.code.uiCharacter.curCustomization._ID.power--;
            Global.code.uiCharacter.curCustomization.UpdateStats();
            RM.code.PlayOneShot(RM.code.sndAddAttribute);
            Global.code.uiCharacter.Refresh();
        }
    }
}
