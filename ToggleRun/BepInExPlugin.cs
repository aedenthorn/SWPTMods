using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ToggleRun
{
    [BepInPlugin("aedenthorn.ToggleRun", "Toggle Run", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<bool> showHeadgear;
        public static ConfigEntry<string> hotKey;
        public static ConfigEntry<string> neverShow;
        public static ConfigEntry<string> alwaysShow;

        public static ConfigEntry<int> nexusID;

        public static bool sprinting;

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
            nexusID = Config.Bind<int>("General", "NexusID", 67, "Nexus mod ID for updates");

            hotKey = Config.Bind<string>("General", "HotKey", "", "Custom toggle hotkey. Leave blank to use default run key");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }

        [HarmonyPatch(typeof(PMC_Input), "Update")]
        static class PMC_Input_Update_Patch
        {
            static void Postfix()
            {
                if (!modEnabled.Value)
                    return;

                bool clicked = hotKey.Value.Length > 0 ? AedenthornUtils.CheckKeyDown(hotKey.Value) : PMC_Setting.code.GetKeyDown("Run");
                if (clicked)
                    sprinting = !sprinting;

                if (sprinting)
                {
                    if (PMC_Input.leftHold || PMC_Input.rightHold || PMC_Input.forwardHold || PMC_Input.backHold)
                    {
                        PMC_Input.Run = true;
                        PMC_Input.RunUp = false;
                    }
                    else
                    {
                        PMC_Input.Run = false;
                        PMC_Input.RunUp = !clicked;
                    }
                }
                else if(clicked)
                {
                    PMC_Input.Run = false;
                    PMC_Input.RunUp = true;
                }
                else
                {
                    PMC_Input.Run = false;
                    PMC_Input.RunUp = false;
                }
            }
        }
    }
}
