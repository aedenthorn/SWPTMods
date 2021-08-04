using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UMod;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BepInExModSupport
{
    [BepInPlugin("aedenthorn.BepInExModSupport", "BepInEx Mod Support", "0.2.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<int> nexusID;

        public static ConfigEntry<bool> checkUpdates;
        public static ConfigEntry<bool> loadImages;
        public static ConfigEntry<bool> checkUpdatesOnLoad;

        public static ConfigEntry<string> checkText;
        public static ConfigEntry<string> visitText;
        public static ConfigEntry<string> updatedText;
        public static ConfigEntry<string> updateText;
        public static ConfigEntry<string> disableText;
        public static ConfigEntry<string> enableText;
        public static ConfigEntry<string> problemText;

        public static ConfigEntry<int> minUpdateInterval;
        public static ConfigEntry<long> lastUpdate;

        public static ConfigDefinition enabledDef = new ConfigDefinition("General", "Enabled");
        public static ConfigDefinition nexusIDDef = new ConfigDefinition("General", "NexusID");
        
        public static string filePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static Transform modCheckImage;
        private static Dictionary<string, PluginUpdateData> pluginUpdateDatas = new Dictionary<string, PluginUpdateData>();
        private static bool isCheckingUpdates;

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
            nexusID = Config.Bind<int>("General", "NexusID", 31, "Nexus mod ID for updates");
            
            checkUpdates = Config.Bind<bool>("General", "CheckUpdates", true, "Check for updates");
            checkUpdatesOnLoad = Config.Bind<bool>("General", "CheckUpdatesOnLoad", true, "Check for updates");

            minUpdateInterval = Config.Bind<int>("Options", "MinUpdateInterval", 60, "Minimum update interval in minutes.");

            checkText = Config.Bind<string>("Text", "CheckText", "Check", "Text to show on update check button");
            disableText = Config.Bind<string>("Text", "DisableText", "Disable", "Text to show instead of Uninstall");
            enableText = Config.Bind<string>("Text", "EnableText", "Enable", "Text to show instead of Uninstall");
            visitText = Config.Bind<string>("Text", "VisitText", "Visit", "Text to show on the visit buttons");
            updateText = Config.Bind<string>("Text", "UpdateText", "Update available!", "Text to show if update available.");
            updatedText = Config.Bind<string>("Text", "UpdatedText", "Latest Version.", "Text to show if latest version installed.");
            problemText = Config.Bind<string>("Text", "ProblemText", "Problem checking update.", "Text to show if problem checking for update.");

            lastUpdate = Config.Bind<long>("ZAuto", "LastUpdate", 0, "Last update time (this is automatically changed on each update).");

            Dbgl("Plugin awake");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            if (!Directory.Exists(Path.Combine(Application.streamingAssetsPath, "DownloadMods")))
                Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "DownloadMods"));

        }

        private void Start()
        {
            if (checkUpdatesOnLoad.Value && ShouldCheckUpdate()) 
            {
                LoadPluginData();
                StartCoroutine(CheckUpdates(true));
            }
        }

        private static void LoadPluginData()
        {
            pluginUpdateDatas.Clear();
            var checkCount = 0;
            foreach (PluginInfo mod in Chainloader.PluginInfos.Values)
            {
                var data = new PluginUpdateData() { pluginInfo = mod };


                if (data.pluginInfo.Instance.Config.ContainsKey(nexusIDDef))
                {
                    checkCount++;
                    data.checkable = true;
                    data.id = (int)mod.Instance.Config[nexusIDDef].BoxedValue;
                }

                pluginUpdateDatas.Add(data.pluginInfo.Metadata.GUID, data);
            }
            Dbgl($"Found {pluginUpdateDatas.Count} plugins, {checkCount} with nexus ids");
        }

        private static bool ShouldCheckUpdate()
        {
            return checkUpdates.Value && DateTimeOffset.Now.ToUnixTimeSeconds() - lastUpdate.Value > minUpdateInterval.Value * 60;
        }

        [HarmonyPatch(typeof(UIDesktop), "Awake")]
        static class UIDesktop_Awake_Patch
        {

            static void Postfix(UIDesktop __instance)
            {
                if (!modEnabled.Value)
                    return;
                modCheckImage = Instantiate(Mainframe.code.uiModBrowse.DownloadPrefab.GetComponent<UIModElement>().loadSign, GameObject.Find("btn mod").transform);
                for (int i = 0; i < modCheckImage.childCount; i++)
                    Destroy(modCheckImage.GetChild(i).gameObject);
                modCheckImage.GetComponent<RectTransform>().anchoredPosition = new Vector2(modCheckImage.parent.GetComponent<RectTransform>().rect.width / 2 - modCheckImage.GetComponent<RectTransform>().rect.width, 0);
                modCheckImage.name = "Mod Check Image";
                modCheckImage.gameObject.SetActive(false);
            }
        }

        [HarmonyPatch(typeof(UIModBrowse), "ShowDownload")]
        static class UIModBrowse_ShowDownload_Patch
        {
            static void Postfix(UIModBrowse __instance)
            {
                if (!modEnabled.Value)
                    return;

                bool shouldCheckUpdate = ShouldCheckUpdate();

                modCheckImage.gameObject.SetActive(false);

                if (!__instance.Uploadbtn.transform.parent.Find("Check Button"))
                {
                    Button checkButton = Instantiate(__instance.Uploadbtn.transform, __instance.Uploadbtn.transform.parent).GetComponent<Button>();
                    checkButton.transform.name = "Check Button";
                    checkButton.GetComponent<RectTransform>().anchoredPosition += new Vector2(checkButton.GetComponent<RectTransform>().rect.width, 0);
                    checkButton.onClick = new Button.ButtonClickedEvent();
                    checkButton.onClick.AddListener(delegate ()
                    {
                        lastUpdate.Value = 0;
                        AccessTools.Method(typeof(UIModBrowse), "ShowPanel").Invoke(__instance, new object[] { 0 });
                    });
                    checkButton.GetComponentInChildren<Text>().text = checkText.Value;
                }

                if (pluginUpdateDatas.Count == 0 || shouldCheckUpdate)
                    LoadPluginData();

                List<string> keys = pluginUpdateDatas.Keys.ToList();

                foreach (string guid in keys)
                {
                    PluginUpdateData data = pluginUpdateDatas[guid];
                    var canEnable = data.pluginInfo.Instance.Config.ContainsKey(enabledDef);

                    var enabled = canEnable ? (bool)data.pluginInfo.Instance.Config[enabledDef].BoxedValue : false;

                    Transform transform = Instantiate(__instance.DownloadPrefab, __instance.DownloadRect.content);
                    transform.gameObject.SetActive(true);
                    UIModElement element = transform.GetComponent<UIModElement>();
                    element.Name = data.pluginInfo.Metadata.Name;
                    element.Version = "V: " + data.pluginInfo.Metadata.Version;
                    element.FilePath = new ModDirectory(filePath);
                    string[] parts = data.pluginInfo.Metadata.GUID.Split('.');
                    element.Author = parts.Length > 1 ? parts[parts.Length - 2] : "";

                    element.icon.gameObject.SetActive(false);

                    /*
                    Texture2D texture2D = this.LoadPictures(modDirPath.Location.FullName);
                    if (texture2D != null)
                    {
                        element.icon.texture = texture2D;
                    }
                    modDirPath.GetModPath(data.pluginInfo.NameInfo.ModName, null).ToString();
                    */
                    element.unloadBtn.GetComponentInChildren<Text>().text = disableText.Value;
                    element.loadBtn.GetComponentInChildren<Text>().text = enableText.Value;
                    element.unloadBtn.onClick.AddListener(delegate ()
                    {
                        var entry = data.pluginInfo.Instance.Config.Bind(new ConfigDefinition("General", "Enabled"), true);
                        entry.Value = false;
                        element.unloadBtn.gameObject.SetActive(false);
                        element.loadBtn.gameObject.SetActive(true);
                        element.loadSign.gameObject.SetActive(false);
                    });
                    element.loadBtn.onClick.AddListener(delegate ()
                    {
                        var entry = data.pluginInfo.Instance.Config.Bind(new ConfigDefinition("General", "Enabled"), true);
                        entry.Value = true;
                        element.unloadBtn.gameObject.SetActive(true);
                        element.loadBtn.gameObject.SetActive(false);
                        element.loadSign.gameObject.SetActive(true);
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
                    if (data.checkable && shouldCheckUpdate)
                    {
                        Dbgl($"{data.pluginInfo.Metadata.Name} has id {data.id}");
                        Button visitButton = Instantiate(element.loadBtn.gameObject, transform).GetComponent<Button>();
                        visitButton.GetComponentInChildren<Text>().text = visitText.Value;
                        visitButton.onClick = new Button.ButtonClickedEvent();
                        visitButton.onClick.AddListener(delegate() {
                            Application.OpenURL($"https://www.nexusmods.com/shewillpunishthem/mods/{data.id}?tab=files");
                        });
                        visitButton.gameObject.SetActive(true);
                        float width = visitButton.GetComponent<RectTransform>().rect.width;
                        element.loadBtn.GetComponent<RectTransform>().anchoredPosition -= new Vector2(width, 0);
                        element.unloadBtn.GetComponent<RectTransform>().anchoredPosition -= new Vector2(width, 0);
                        element.loadSign.GetComponent<RectTransform>().anchoredPosition -= new Vector2(width, 0);
                        pluginUpdateDatas[guid].updateSign = element.loadSign;

                        if (data.remoteVersion == null)
                        {
                            element.loadSign.gameObject.SetActive(false);
                        }
                        else if(data.remoteVersion > data.pluginInfo.Metadata.Version)
                        {
                            element.loadSign.gameObject.SetActive(true);
                            element.loadSign.GetComponentInChildren<Text>().text = updateText.Value;
                            element.loadSign.GetComponent<Image>().color = Color.yellow;
                        }
                        else
                        {
                            element.loadSign.gameObject.SetActive(true);
                            element.loadSign.GetComponentInChildren<Text>().text = updatedText.Value;
                            element.loadSign.GetComponent<Image>().color = Color.green;
                        }
                    }
                }
                if(shouldCheckUpdate)
                    context.StartCoroutine(CheckUpdates(false));
            }
        }
        public static IEnumerator CheckUpdates(bool mainMenu)
        {
            if (isCheckingUpdates)
                yield break;

            Dbgl($"Checking for updates");

            isCheckingUpdates = true;
            
            bool anyUpdates = false;

            modCheckImage.gameObject.SetActive(mainMenu);

            modCheckImage.gameObject.GetComponent<Image>().color = Color.yellow;

            List<string> keys = pluginUpdateDatas.Keys.ToList();

            foreach (var guid in keys)
            {
                PluginUpdateData data = pluginUpdateDatas[guid];

                if (!data.checkable || data.remoteVersion != null)
                    continue;

                Version currentVersion = data.pluginInfo.Metadata.Version;
                string pluginName = data.pluginInfo.Metadata.Name;
                
                int id = data.id;


                Dbgl($"{pluginName} {guid} current version: {currentVersion}");

                WWWForm form = new WWWForm();

                UnityWebRequest uwr = UnityWebRequest.Get($"https://www.nexusmods.com/shewillpunishthem/mods/{id}");
                yield return uwr.SendWebRequest();

                


                if (!mainMenu && Mainframe.code?.uiModBrowse.gameObject.activeSelf != true)
                {
                    isCheckingUpdates = false;
                    yield break;
                }

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

                            Version remoteVersion = new Version(match.Groups[1].Value);
                            Dbgl($"{pluginName} remote version: {remoteVersion}.");

                            pluginUpdateDatas[guid].remoteVersion = remoteVersion;

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

                            if (remoteVersion > currentVersion)
                            {
                                anyUpdates = true;
                                Dbgl($"new remote version: {remoteVersion}!");
                                modCheckImage.gameObject.SetActive(true);
                                modCheckImage.gameObject.GetComponent<Image>().color = Color.red;
                                if (data.updateSign)
                                {
                                    data.updateSign.GetComponentInChildren<Text>().text = updateText.Value;
                                    data.updateSign.GetComponent<Image>().color = Color.yellow;
                                }
                            }
                            else if (data.updateSign)
                            {
                                data.updateSign.GetComponentInChildren<Text>().text = updatedText.Value;
                                data.updateSign.GetComponent<Image>().color = Color.green;
                            }

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
                        data.updateSign.GetComponentInChildren<Text>().text = problemText.Value;
                        data.updateSign.GetComponent<Image>().color = Color.red;
                    }
                    if (data.updateSign)
                        data.updateSign.gameObject.SetActive(true);
                }
            }

            if (!anyUpdates)
            {
                modCheckImage.gameObject.GetComponent<Image>().color = Color.green;
            }
            modCheckImage.gameObject.SetActive(!Mainframe.code.uiModBrowse.gameObject.activeSelf);

            lastUpdate.Value = DateTimeOffset.Now.ToUnixTimeSeconds();
            isCheckingUpdates = false;

        }
    }
}
