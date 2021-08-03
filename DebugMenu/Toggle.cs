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
    public partial class BepInExPlugin : BaseUnityPlugin
    {

        private static void ToggleFly()
        {
            flyMode.Value = !flyMode.Value;
            if (flyModeNotice.Value.Contains("{0}"))
                Global.code.uiCombat.ShowHeader(string.Format(flyModeNotice.Value, flyMode.Value));

        }

        private static void ToggleLevelBypass()
        {
            levelBypass.Value = !levelBypass.Value;
            if (levelBypassNotice.Value.Contains("{0}"))
                Global.code.uiCombat.ShowHeader(string.Format(levelBypassNotice.Value, levelBypass.Value));

        }
    }
}
