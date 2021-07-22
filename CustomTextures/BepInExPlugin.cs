using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace CustomTextures
{
    [BepInPlugin("aedenthorn.CustomTextures", "Custom Textures", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<string> hotKey;
        private string assetPath;

        public static Dictionary<string, string> customTextures = new Dictionary<string, string>();
        public static Dictionary<string, DateTime> fileWriteTimes = new Dictionary<string, DateTime>();
        public static List<string> texturesToLoad = new List<string>();
        public static List<string> layersToLoad = new List<string>();
        public static Dictionary<string, Texture2D> cachedTextures = new Dictionary<string, Texture2D>();

        //public static ConfigEntry<int> nexusID;

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
            
            hotKey = Config.Bind<string>("Options", "HotKey", "page up", "Hotkey to reload textures.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Dbgl("Creating mod folder");
                Directory.CreateDirectory(assetPath);
            }
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            GetTextureFiles();
        }

        private void GetTextureFiles()
        {
            texturesToLoad.Clear();

            foreach (string file in Directory.GetFiles(assetPath, "*.*", SearchOption.AllDirectories))
            {
                string fileName = Path.GetFileName(file);
                string id = Path.GetFileNameWithoutExtension(fileName);


                if (!fileWriteTimes.ContainsKey(id) || (cachedTextures.ContainsKey(id) && !DateTime.Equals(File.GetLastWriteTimeUtc(file), fileWriteTimes[id])))
                {
                    cachedTextures.Remove(id);
                    texturesToLoad.Add(id);
                    layersToLoad.Add(Regex.Replace(id, @"_[^_]+\.", "."));
                    fileWriteTimes[id] = File.GetLastWriteTimeUtc(file);
                    Dbgl($"adding new custom texture {id}.");
                }

                customTextures[id] = file;
            }
        }

        private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
        {
            List<string> dump = new List<string>();
            GameObject[] allGOs = FindObjectsOfType<GameObject>();
            Dictionary<string, SkinnedMeshRenderer> namedRenderers = new Dictionary<string, SkinnedMeshRenderer>();
            foreach(GameObject go in allGOs)
            {
                var smr = go.GetComponent<SkinnedMeshRenderer>();
                var mr = go.GetComponent<MeshRenderer>();

                if (!smr && !mr)
                    continue;
                string name = null;
                try
                {
                    if (customTextures.ContainsKey($"object_{go.name}_MainTex"))
                        name = $"object_{go.name}_MainTex";
                    else if (customTextures.ContainsKey($"renderer_{mr?.material?.mainTexture?.name}_MainTex"))
                        name = $"renderer_{mr?.material?.mainTexture?.name}_MainTex";
                    else if (customTextures.ContainsKey($"renderer_{smr?.material?.mainTexture?.name}_MainTex"))
                        name = $"renderer_{smr?.material?.mainTexture?.name}_MainTex";

                    if (name != null)
                    {
                        Dbgl($"Found custom texture for {go.name}: {name}");

                        Texture2D tex = new Texture2D(2, 2);
                        if (cachedTextures.ContainsKey(name))
                        {
                            tex = cachedTextures[name];
                        }
                        else
                        {
                            var bytes = File.ReadAllBytes(customTextures[name]);
                            tex.LoadImage(bytes);
                            tex.name = name;
                            tex.Apply();
                            cachedTextures[name] = tex;
                        }
                        if(smr)
                            smr.material.mainTexture = tex;
                        else
                            mr.material.mainTexture = tex;
                    }
                    dump.Add($"{go.name}, smr: {smr?.name} {smr?.material?.mainTexture?.name}, mr: {mr?.name} {mr?.material?.mainTexture?.name}");
                }
                catch
                {
                    continue;
                }

            }
            if (!File.Exists(Path.Combine(assetPath, arg0.name + "_dump.txt")))
                File.WriteAllLines(Path.Combine(assetPath, arg0.name +"_dump.txt"), dump.ToArray());
        }
    }
}
