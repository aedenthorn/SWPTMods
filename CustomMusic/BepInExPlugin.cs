using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace CustomMusic
{
    [BepInPlugin("aedenthorn.CustomMusic", "Custom Music", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        private static ConfigEntry<string> playingNotice;
        private static string assetPath;
        public static ConfigEntry<int> nexusID;

        public static BepInExPlugin context;
        
        public static Dictionary<string, List<AudioClip>> musicDict = new Dictionary<string, List<AudioClip>>();

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind("General", "Enabled", true, "Enable this mod");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug logs");
            nexusID = Config.Bind<int>("General", "NexusID", 66, "Nexus mod ID for updates");

            assetPath = AedenthornUtils.GetAssetPath(this, true);

            //Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            musicDict.Add("CustomMusic", new List<AudioClip>());
            LoadMusicFiles(assetPath, "CustomMusic");

            foreach(string path in Directory.GetDirectories(assetPath))
            {
                TryLoadMusicForScene(Path.GetFileNameWithoutExtension(path));
            }

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
        {
            if (!modEnabled.Value)
                return;

            TryLoadMusicForScene(arg0.name);
            TryPlayMusicForScene(arg0.name);
        }

        private void TryPlayMusicForScene(string scene)
        {
            if (!musicDict.ContainsKey(scene) || !musicDict[scene].Any())
            {
                if (!musicDict["CustomMusic"].Any())
                {
                    Dbgl($"No music for scene {scene}");
                    return;
                }
                scene = "CustomMusic";
            }

            GameObject musicObj = GameObject.Find("Audio Source");
            if (musicObj)
            {
                AudioSource source = musicObj.GetComponent<AudioSource>();
                AudioClip clip;
                if (!musicDict[scene].Contains(source.clip))
                {
                    AedenthornUtils.ShuffleList(musicDict[scene]);
                    clip = musicDict[scene][0];
                }
                else 
                    clip = musicDict[scene][(musicDict[scene].IndexOf(source.clip) + 1) % musicDict[scene].Count];
                    
                Dbgl($"Playing music {clip.name} for scene {scene}");

                source.clip = clip;
                source.loop = false;
                source.Play();
            }
        }

        private void Update()
        {
            if (!modEnabled.Value || !GameObject.Find("Audio Source") || GameObject.Find("Audio Source").GetComponent<AudioSource>().isPlaying)
                return;
            TryPlayMusicForScene(SceneManager.GetActiveScene().name);
        }

        private void TryLoadMusicForScene(string scene)
        {
            Dbgl($"Loading music for scene {scene}");
            string path = Path.Combine(assetPath, scene);
            if (!musicDict.ContainsKey(scene))
            {
                musicDict[scene] = new List<AudioClip>();
            }
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }
            LoadMusicFiles(path, scene);
        }

        private void LoadMusicFiles(string scenePath, string scene)
        {
            var files = Directory.GetFiles(scenePath).ToList();
            for(int i = musicDict[scene].Count - 1; i >= 0; i--)
            {
                if (!files.Exists(f => Path.GetFileNameWithoutExtension(f) == musicDict[scene][i].name))
                    musicDict[scene].RemoveAt(i);
            }
            foreach (string path in files)
            {
                if (!path.ToLower().EndsWith(".wav") && !path.ToLower().EndsWith(".ogg"))
                    continue;
                if (!musicDict[scene].Exists(a => a.name == Path.GetFileNameWithoutExtension(path)))
                {
                    string uri;

                    uri = "file:///" + path.Replace("\\", "/");

                    var www = UnityWebRequestMultimedia.GetAudioClip(uri, path.ToLower().EndsWith(".wav") ? AudioType.WAV : AudioType.OGGVORBIS);
                    www.SendWebRequest();

                    while (!www.isDone) { }

                    //Dbgl($"checking downloaded {filename}");
                    if (www != null)
                    {
                        //Dbgl("www not null. errors: " + www.error);
                        DownloadHandlerAudioClip dac = ((DownloadHandlerAudioClip)www.downloadHandler);
                        if (dac != null)
                        {
                            AudioClip ac = dac.audioClip;
                            if (ac != null)
                            {
                                string name = Path.GetFileNameWithoutExtension(uri);
                                ac.name = name;
                                if (!musicDict[scene].Exists(a => a.name == name))
                                {
                                    musicDict[scene].Add(ac);
                                    Dbgl($"Added audio clip {name} to scene {scene}");
                                }
                            }
                            else
                            {
                                Dbgl("audio clip is null. data: " + dac.text);
                            }
                        }
                        else
                        {
                            Dbgl("DownloadHandler is null. bytes downloaded: " + www.downloadedBytes);
                        }
                    }
                    else
                    {
                        Dbgl("www is null " + www.url);
                    }
                }
            }
        }
    }
}
