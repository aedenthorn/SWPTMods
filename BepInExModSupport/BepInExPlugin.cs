using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UMod;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BepInExModSupport
{
    [BepInPlugin("aedenthorn.BepInExModSupport", "BepInEx Mod Support", "0.1.2")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> checkUpdates;
        public static ConfigEntry<bool> loadImages;
        public static ConfigEntry<string> visitText;
        public static ConfigEntry<string> updatedText;
        public static ConfigEntry<string> updateText;
        public static ConfigEntry<string> disableText;
        public static ConfigEntry<string> enableText;
        public static ConfigEntry<string> problemText;

        public static ConfigEntry<int> minUpdateInterval;
        public static ConfigEntry<long> lastUpdate;
        
        private static Dictionary<int, Transform> updateSigns = new Dictionary<int, Transform>();

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

            minUpdateInterval = Config.Bind<int>("Options", "MinUpdateInterval", 0, "Minimum update interval in minutes.");
            
            disableText = Config.Bind<string>("Text", "DisableText", "Disable", "Text to show instead of Uninstall");
            enableText = Config.Bind<string>("Text", "EnableText", "Enable", "Text to show instead of Uninstall");
            visitText = Config.Bind<string>("Text", "VisitText", "Visit", "Text to show on the visit buttons");
            updateText = Config.Bind<string>("Text", "UpdateText", "Update available!", "Text to show if update available.");
            updatedText = Config.Bind<string>("Text", "UpdatedText", "Latest Version.", "Text to show if latest version installed.");
            problemText = Config.Bind<string>("Text", "ProblemText", "Problem checking update.", "Text to show if problem checking for update.");
            
            lastUpdate = Config.Bind<long>("ZAuto", "LastUpdate", 0, "Last update time (this is automatically changed on each update).");

            nexusID = Config.Bind<int>("General", "NexusID", 31, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

            //assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);

            if (!Directory.Exists(Path.Combine(Application.streamingAssetsPath, "DownloadMods")))
                Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "DownloadMods"));

        }

        [HarmonyPatch(typeof(UIModBrowse), "ShowDownload")]
        static class GetMods_Patch
        {
            static void Postfix(UIModBrowse __instance)
            {
                if (!modEnabled.Value)
                    return;

                updateSigns.Clear();

                string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                ConfigDefinition enabledDef = new ConfigDefinition("General", "Enabled");
                ConfigDefinition nexusIDDef = new ConfigDefinition("General", "NexusID");

                Dictionary<int, PluginInfo> updateableMods = new Dictionary<int, PluginInfo>();

                bool shouldUpdate = DateTimeOffset.Now.ToUnixTimeSeconds() - lastUpdate.Value > minUpdateInterval.Value * 60;

                Dbgl($"Last update was {(DateTimeOffset.Now.ToUnixTimeSeconds() - lastUpdate.Value)/60} minutes ago. Updating: {false}");

                foreach (PluginInfo mod in Chainloader.PluginInfos.Values)
                {
                    var canEnable = mod.Instance.Config.ContainsKey(enabledDef);

                    var enabled = canEnable ? (bool)mod.Instance.Config[enabledDef].BoxedValue : false;

                    Transform transform = Instantiate(__instance.DownloadPrefab, __instance.DownloadRect.content);
                    transform.gameObject.SetActive(true);
                    UIModElement element = transform.GetComponent<UIModElement>();
                    element.Name = mod.Metadata.Name;
                    element.Version = "V: " + mod.Metadata.Version;
                    element.FilePath = new ModDirectory(filePath);
                    string[] parts = mod.Metadata.GUID.Split('.');
                    element.Author = parts.Length > 1 ? parts[parts.Length - 2] : "";

                    element.icon.gameObject.SetActive(false);

                    /*
                    Texture2D texture2D = this.LoadPictures(modDirPath.Location.FullName);
                    if (texture2D != null)
                    {
                        element.icon.texture = texture2D;
                    }
                    modDirPath.GetModPath(mod.NameInfo.ModName, null).ToString();
                    */
                    element.unloadBtn.GetComponentInChildren<Text>().text = disableText.Value;
                    element.loadBtn.GetComponentInChildren<Text>().text = enableText.Value;
                    element.unloadBtn.onClick.AddListener(delegate ()
                    {
                        var entry = mod.Instance.Config.Bind(new ConfigDefinition("General", "Enabled"), true);
                        entry.Value = false;
                        element.unloadBtn.gameObject.SetActive(false);
                        element.loadBtn.gameObject.SetActive(true);
                    });
                    element.loadBtn.onClick.AddListener(delegate ()
                    {
                        var entry = mod.Instance.Config.Bind(new ConfigDefinition("General", "Enabled"), true);
                        entry.Value = true;
                        element.unloadBtn.gameObject.SetActive(true);
                        element.loadBtn.gameObject.SetActive(false);
                    });

                    if (enabled)
                    {
                        element.unloadBtn.gameObject.SetActive(true);
                        element.loadSign.gameObject.SetActive(true);
                        element.loadBtn.gameObject.SetActive(false);
                    }
                    else if(canEnable)
                    {
                        element.unloadBtn.gameObject.SetActive(false);
                        element.loadSign.gameObject.SetActive(false);
                        element.loadBtn.gameObject.SetActive(true);
                    }
                    else
                    {
                        element.loadSign.gameObject.SetActive(true);
                        element.unloadBtn.gameObject.SetActive(false);
                        element.loadBtn.gameObject.SetActive(false);
                    }
                    if (mod.Instance.Config.ContainsKey(nexusIDDef))
                    {

                        int id = (int)mod.Instance.Config[nexusIDDef].BoxedValue;
                        Dbgl($"{mod.Metadata.Name} has id {id}");
                        Button visitButton = Instantiate(element.loadBtn.gameObject, transform).GetComponent<Button>();
                        visitButton.GetComponentInChildren<Text>().text = visitText.Value;
                        visitButton.onClick = new Button.ButtonClickedEvent();
                        visitButton.onClick.AddListener(delegate() {
                            Application.OpenURL($"https://www.nexusmods.com/shewillpunishthem/mods/{id}?tab=files");
                        });
                        visitButton.gameObject.SetActive(true);
                        float width = visitButton.GetComponent<RectTransform>().rect.width;
                        element.loadBtn.GetComponent<RectTransform>().anchoredPosition -= new Vector2(width, 0);
                        element.unloadBtn.GetComponent<RectTransform>().anchoredPosition -= new Vector2(width, 0);
                        element.loadSign.GetComponent<RectTransform>().anchoredPosition -= new Vector2(width, 0);

                        if (shouldUpdate)
                        {
                            updateSigns.Add(id, element.loadSign);
                            updateableMods.Add(id, mod);
                            element.loadSign.gameObject.SetActive(false);
                        }
                    }
                }
                if(updateableMods.Count > 0)
                {
                    context.StartCoroutine(CheckUpdates(updateableMods));
                }
            }
        }
        public static IEnumerator CheckUpdates(Dictionary<int, PluginInfo> mods)
        {
            //Dictionary<string, string> ignores = GetIgnores();
            foreach (var kvp in mods)
            {

                Version currentVersion = kvp.Value.Metadata.Version;
                string pluginName = kvp.Value.Metadata.Name;
                string guid = kvp.Value.Metadata.GUID;
                
                int id = kvp.Key;


                Dbgl($"{pluginName} {kvp.Key} current version: {currentVersion}");

                WWWForm form = new WWWForm();

                UnityWebRequest uwr = UnityWebRequest.Get($"https://www.nexusmods.com/shewillpunishthem/mods/{id}");
                yield return uwr.SendWebRequest();

                if (uwr.isNetworkError)
                {
                    Debug.Log("Error While Sending: " + uwr.error);
                }
                else
                {
                    //Dbgl($"entire text: {uwr.downloadHandler.text}.");

                    string[] lines = uwr.downloadHandler.text.Split(
                        new[] { "\r\n", "\r", "\n" },
                        StringSplitOptions.None
                    );
                    bool found = false;
                    bool check = false;
                    foreach (string line in lines)
                    {
                        if (check)
                        {
                            Match match = Regex.Match(line, "\"([0-9.]+)\"");
                            if (!match.Success)
                                break;

                            found = true;

                            Version version = new Version(match.Groups[1].Value);
                            Dbgl($"remote version: {version}.");

                            /*
                            if (ignores.ContainsKey("" + id))
                            {
                                if (ignores["" + id] == version.ToString())
                                {
                                    if (!showAllManagedMods.Value)
                                    {
                                        Dbgl($"ignoring {pluginName} {id} version: {version}");
                                        break;
                                    }
                                }
                                else
                                {
                                    Dbgl($"new version {version}, removing ignore {ignores["" + id]}");
                                    RemoveIgnore("" + id);
                                }
                            }
                            */

                            if (version > currentVersion)
                            {
                                Dbgl($"new remote version: {version}!");
                                updateSigns[id].GetComponentInChildren<Text>().text = updateText.Value;
                                updateSigns[id].GetComponent<Image>().color = Color.yellow;
                            }
                            else
                                updateSigns[id].GetComponentInChildren<Text>().text = updatedText.Value;

                            break;
                        }
                        if (line.Contains("<meta property") && line.Contains("Version"))
                        {
                            check = true;
                        }
                    }
                    if (found == false)
                    {
                        Dbgl("Mod version string not found on page!");
                        //File.WriteAllLines(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), id + "_page.txt"), lines);
                        updateSigns[id].GetComponentInChildren<Text>().text = problemText.Value;
                        updateSigns[id].GetComponent<Image>().color = Color.red;
                    }
                    updateSigns[id].gameObject.SetActive(true);
                }
            }
            lastUpdate.Value = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
    }
}
