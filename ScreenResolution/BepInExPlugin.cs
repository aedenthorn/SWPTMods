using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace ScreenResolution
{
    [BepInPlugin("aedenthorn.ScreenResolution", "Screen Resolution", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        
        public static ConfigEntry<string> customResolutions;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 45, "Nexus mod ID for updates");

            customResolutions = Config.Bind<string>("Options", "CustomResolutions", "", "List of custom resolutions, comma-separated use format xxx*yyy");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(UISettings), nameof(UISettings.Open))]
        public static class UISettings_Open_Patch
        {
            public static void Postfix(UISettings __instance)
            {
                if (!modEnabled.Value)
                    return;

                foreach(Resolution r in Screen.resolutions)
                {
                    if(!__instance.ResolutionDropDown.options.Exists(s => s.text == r.width + "*" + r.height))
                    {
                        Dbgl($"Adding resolution {r.width}*{r.height}");
                        __instance.ResolutionDropDown.options.Add(new Dropdown.OptionData(r.width + "*" + r.height));
                    }
                }

                if(customResolutions.Value.Trim().Length > 0)
                {
                    string[] reslist = customResolutions.Value.Split(',');
                    foreach (string r in reslist)
                    {
                        Match m = Regex.Match(r, @"([0-9]+\*[0-9]+)");
                        if (!m.Success)
                            continue;
                        string res = m.Groups[1].Value;
                        if (!__instance.ResolutionDropDown.options.Exists(s => s.text == res))
                        {
                            Dbgl($"Adding resolution {res}");
                            __instance.ResolutionDropDown.options.Add(new Dropdown.OptionData(res));
                        }
                    }
                }
            }
        }

    }
}
