using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SaveBackup
{
    [BepInPlugin("aedenthorn.SaveBackup", "Save Backup", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 39, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(Mainframe), "DeleteAllSavedGames")]
        static class SaveGame_Patch
        {

            static void Prefix(Mainframe __instance)
            {
                if (!modEnabled.Value)
                    return;

                string dest = Path.Combine(Application.persistentDataPath, __instance.foldername + "_bkp");
                if (Directory.Exists(dest))
                {
                    foreach (string file in Directory.GetFiles(dest))
                        File.Delete(file);
                    Directory.Delete(dest);
                }
                Directory.Move(Path.Combine(Application.persistentDataPath, __instance.foldername), dest);
            }
        }
    }
}
