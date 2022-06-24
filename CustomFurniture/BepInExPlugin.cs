using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace CustomFurniture
{
    [BepInPlugin("aedenthorn.CustomFurniture", "Custom Furniture", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static List<CustomFurniture> furnitureList = new List<CustomFurniture>();
        private static BepInExPlugin context;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        private static string assetPath;

        public static Mesh customMesh { get; set; }

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug.Value)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            context = this;

            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 33, "Nexus id for update checking");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");

            if (!modEnabled.Value)
                return;

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            PreloadBuildings();
        }

        private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name != "The Palace")
                return;
            var gos = FindObjectsOfType<GameObject>();
            var names = gos.Select(g => g.name);
            var file = Path.Combine(assetPath, arg0.name + "_scenedump.txt");
            if (!File.Exists(file))
                File.WriteAllLines(file, names);

        }

        private static void PreloadBuildings()
        {
            furnitureList.Clear();

            Dbgl($"Importing buildings");



            foreach (string folder in Directory.GetDirectories(assetPath))
            {
                string name = Path.GetFileName(folder);

                if (!File.Exists(Path.Combine(folder, "data.json")))
                {
                    Dbgl($"building {name} is missing data.json!");
                    continue;
                }
                Dbgl($"creating building data for {name}");

                CustomFurniture building = new CustomFurniture();


                string json = File.ReadAllText(Path.Combine(folder, "data.json"));
                building.meta = JsonUtility.FromJson<FurnitureMeta>(json);

                if (File.Exists(Path.Combine(folder, "icon.png")))
                {
                    var icon = File.ReadAllBytes(Path.Combine(folder, "icon.png"));
                    building.icon.LoadImage(icon);
                }

                foreach(string subfolder in Directory.GetDirectories(folder))
                {
                    building.textures[Path.GetFileName(subfolder)] = new Dictionary<string, Texture2D>();
                    foreach (string texFile in Directory.GetFiles(subfolder, "*.png"))
                    {
                        Dbgl($"adding texture {Path.GetFileName(subfolder)} {Path.GetFileNameWithoutExtension(texFile)}");

                        var texture = File.ReadAllBytes(texFile);
                        building.textures[Path.GetFileName(subfolder)][Path.GetFileNameWithoutExtension(texFile)] = new Texture2D(2, 2);
                        building.textures[Path.GetFileName(subfolder)][Path.GetFileNameWithoutExtension(texFile)].LoadImage(texture);
                    }
                }

                if (File.Exists(Path.Combine(folder, "texture.png")))
                {
                    var texture = File.ReadAllBytes(Path.Combine(folder, "texture.png"));
                    building.texture = new Texture2D(2, 2);
                    building.texture.LoadImage(texture);
                }

                furnitureList.Add(building);
                /*

                //DontDestroyOnLoad(building.go);
                asset = AssetBundle.LoadFromFile(Path.Combine(folder, "bundle"));
                //building.go = smith;
                //Dbgl($"building.go: {building.go?.name}");

                //buildings.Add(building);

                //continue;

                var texture = File.ReadAllBytes(Path.Combine(folder, "texture.png"));
                building.texture.LoadImage(texture);






                try
                {
                    GameObject imported = MeshImporter.Load(Path.Combine(folder, "mesh.fbx"));
                    //GameObject parent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    //parent.GetComponent<MeshRenderer>().material.mainTexture = building.texture;
                    //parent.name = name;
                    DontDestroyOnLoad(imported);
                    /*
                    if (imported.GetComponent<MeshFilter>())
                    {
                        parent.GetComponent<MeshFilter>().mesh = imported.GetComponent<MeshFilter>().mesh;
                        building.mesh = parent.GetComponent<MeshFilter>().mesh;
                    }
                    else
                    {
                        parent.GetComponent<MeshFilter>().mesh = imported.GetComponentInChildren<MeshFilter>().mesh;
                        building.mesh = parent.GetComponentInChildren<MeshFilter>().mesh;
                    }
                    building.go = imported;
                    /*
                    Mesh mesh = new ObjImporter().ImportFile(Path.Combine(folder, "mesh.obj"));
                    if (mesh != null)
                        building.mesh = mesh;
                    Dbgl($"Imported fbx as go");
                }
                catch (Exception ex)
                {
                    Dbgl($"Error importing fbx as game object:\n\n{ex}");
                }
                buildings.Add(building);
                */
            }
        }

        [HarmonyPatch(typeof(RM), "LoadResources")]
        static class RM_LoadResources_Patch
        {
            static void Postfix(RM __instance)
            {
                if (!modEnabled.Value)
                    return;


                foreach (CustomFurniture cb in furnitureList)
                {

                    Dbgl($"Adding building {cb.meta.buildingName}, cat {cb.meta.categoryName}, rarity {cb.meta.rarity}");

                    GameObject gameObject = new GameObject() { name = cb.meta.buildingName };
                    DontDestroyOnLoad(gameObject);
                    RM.code.allBuildings.AddItem(gameObject.transform);
                    Dbgl($"Added building {gameObject.name} to allBuildings");

                    //GameObject cbgo = Instantiate(cb.go, gameObject.transform);
                    //cbgo.transform.localPosition = Vector3.zero;
                    //gameObject.AddComponent<Furniture>();

                    //cbgo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    /*
                    gameObject.AddComponent<MapIcon>();
                    gameObject.AddComponent<Interaction>();
                    Building building = gameObject.AddComponent<Building>();
                    building.rarity = (Rarity)cb.building.rarity;
                    building.categoryName = cb.building.categoryName;
                    building.icon = cb.icon;

                    //GameObject full = Instantiate(cb.go, parent);
                    //DontDestroyOnLoad(full);


                    Building building = mainObj.GetComponent<Building>();
                    building.rarity = cb.building.rarity;
                    building.homeName = cb.building.homeName;
                    building.categoryName = cb.building.categoryName;
                    building.icon = cb.icon;

                    
                    //Transform newObj2 = Instantiate(__instance.allBuildings.GetItemWithName("Chair").Find("classic_armchair_prefab (2)"), mainObj.transform);
                    //newObj2.GetComponent<MeshFilter>().mesh = cb.mesh;

                    //newObj2.GetComponent<MeshRenderer>().material. = cb.material.shader;
                    //newObj2.GetComponent<MeshRenderer>().material.mainTexture = cb.texture;
                    //newObj2.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);


                    GameObject newObj = Instantiate(cb.go, mainObj.transform);
                    newObj.name = cb.building.buildingName;
                    newObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);


                                        foreach (string prop in newObj2.GetComponent<MeshRenderer>().material.GetTexturePropertyNames())
                    {
                        newObj2.GetComponent<MeshRenderer>().material.SetTexture(prop, cb.texture);
                    }

                    newObj2.GetComponent<MeshRenderer>().material = cb.material;

                    Mesh mesh = Array.Find(Resources.FindObjectsOfTypeAll<Mesh>(), m => m.name == "Stand_3");

                    Dbgl($"got mesh? {mesh != null}");

                    foreach(string prop in go.GetComponent<MeshRenderer>().material.GetTexturePropertyNames())
                    {
                        go.GetComponent<MeshRenderer>().material.SetTexture(prop, null);
                    }
                    go.GetComponent<MeshRenderer>().material.SetTexture("_MainTex", cb.texture);
                    Transform furnitureTemplate = __instance.allBuildings.GetItemWithName(cb.building.templateFurniture);
                    if (furnitureTemplate)
                    {
                        Dbgl($"Got furniture template {cb.building.templateFurniture} for {cb.building.buildingName}");
                        Furniture furnitureT = furnitureTemplate.GetComponent<Furniture>();
                        if(!mainObj.gameObject.GetComponent<Furniture>())
                            mainObj.gameObject.AddComponent<Furniture>();

                        Furniture furniture = mainObj.GetComponent<Furniture>();

                        furniture.posesGroup = furnitureT.posesGroup;
                        furniture.camerasGroup = furnitureT.camerasGroup;
                    }
                    __instance.allBuildings.items.Add(mainObj.transform);
                    */
                }
            }
        }
        [HarmonyPatch(typeof(UIBuildingMode), nameof(UIBuildingMode.Open))]
        static class UIBuilding_Open_Patch
        {
            static void Prefix()
            {
                foreach (CustomFurniture cb in furnitureList)
                {
                    if (cb.meta.sceneName != SceneManager.GetActiveScene().name)
                    {
                        continue;
                    }

                    var item = RM.code.allBuildings.GetItemWithName(cb.meta.buildingName);
                    if (!item)
                    {
                        Dbgl($"building {cb.meta.buildingName} not found in RM!");
                        continue;
                    }

                    Transform temp = GameObject.Find("/Scene").transform.Find(cb.meta.templateObject);
                    
                    Dbgl($"Adding model to menu item {cb.meta.buildingName}, cat {cb.meta.categoryName}, model {temp?.name}, scale {cb.meta.scale}");

                    //if (cb.building.buildingName == "Bartender")
                    //    File.WriteAllText(Path.Combine(assetPath, "test.json"), JsonUtility.ToJson(cb.building));

                    if (!item.Find(temp.name))
                    {
                        AddModel(temp, item, cb);

                    }

                    //var plane = Instantiate(asset.LoadAsset("Assets/Tomcat/Tomcat.prefab") as GameObject, item);
                    //plane.transform.localPosition = Vector3.zero;

                }
                foreach (Transform t in RM.code.allBuildings.items)
                {
                    //Dbgl($"building {t.name}");

                }
            }

        }
        [HarmonyPatch(typeof(UIBuildingMode), nameof(UIBuildingMode.RefreshBuildingList))]
        static class UIBuilding_Refresh_Patch
        {
            static void Postfix(UIBuildingMode __instance)
            {
                bool refresh = false;
                for (int i = __instance.iconGroup.childCount - 1; i >= 0; i--)
                {
                    if (furnitureList.Exists(b => b.meta.buildingName == __instance.iconGroup.GetChild(i).name && b.meta.categoryName == __instance.curCategory && b.meta.sceneName != SceneManager.GetActiveScene().name) && !furnitureList.Exists(b => b.meta.buildingName == __instance.iconGroup.GetChild(i).name && b.meta.categoryName == __instance.curCategory && b.meta.sceneName == SceneManager.GetActiveScene().name))
                    {
                        Dbgl($"Removing {__instance.iconGroup.GetChild(i).name}");
                        Destroy(__instance.iconGroup.GetChild(i).gameObject);
                    }
                }
                for (int i = __instance.categoryGroup.childCount - 1; i >= 0; i--)
                {
                    if (!RM.code.allBuildings.items.Exists(t => t.GetComponent<Building>().categoryName == __instance.categoryGroup.GetChild(i).name && (!furnitureList.Exists(b => b.meta.buildingName == t.name && b.meta.categoryName == __instance.curCategory) || furnitureList.Find(b => b.meta.buildingName == t.name && b.meta.categoryName == __instance.curCategory).meta.sceneName == SceneManager.GetActiveScene().name)))
                    {
                        Dbgl($"Removing cat {__instance.categoryGroup.GetChild(i).name}");
                        if (__instance.categoryGroup.GetChild(i).name == __instance.curCategory)
                            __instance.curCategory = "Bedroom";
                        Destroy(__instance.categoryGroup.GetChild(i).gameObject);
                        refresh = true;
                    }
                }
                if(refresh)
                    __instance.RefreshBuildingList();

            }
        }
        [HarmonyPatch(typeof(UIConfirmation), nameof(UIConfirmation.Confirm))]
        static class UIConfirmation_Confirm_Patch
        {
            static void Prefix(string ___stats)
            {
                if (!modEnabled.Value || ___stats != "BuyFurniture")
                    return;
                Collider[] colliders = ((Transform)AccessTools.Field(typeof(Global), "placingFurniture").GetValue(Global.code)).GetComponentsInChildren<Collider>();
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliders[i].enabled = true;
                }
            }
        }
        [HarmonyPatch(typeof(Mainframe), "LoadFurniture")]
        static class Mainframe_LoadFurniture_Patch
        {
            static void Postfix(Transform __result)
            {
                CustomFurniture cb = furnitureList.Find(b => b.meta.buildingName == __result.name);
                if (!modEnabled.Value || cb == null)
                    return;

                Transform temp = GameObject.Find("/Scene").transform.Find(cb.meta.templateObject);

                Dbgl($"Adding model to existing furniture {cb.meta.buildingName}, cat {cb.meta.categoryName}, model {temp?.name}");
                if (!__result.Find(temp.name))
                {
                    AddModel(temp, __result, cb);
                }
            }
        }
        private static void AddModel(Transform template, Transform item, CustomFurniture cb)
        {
            Transform t = Instantiate(template, item);
            if (cb.meta.tag != null)
                t.tag = cb.meta.tag;
            t.localScale = cb.meta.scale;
            t.name = template.name;
            t.localPosition = Vector3.zero;
            t.gameObject.SetActive(true);
            Furniture f = item.gameObject.AddComponent<Furniture>();
            f.cameras = new CommonArray();
            f.poses = new CommonArray();
            item.gameObject.AddComponent<MapIcon>();
            item.gameObject.AddComponent<Interaction>();
            Building building = item.gameObject.AddComponent<Building>();
            building.rarity = (Rarity)cb.meta.rarity;
            building.categoryName = cb.meta.categoryName;
            building.icon = cb.icon;

            MeshRenderer[] mrs = t.GetComponentsInChildren<MeshRenderer>();

            foreach (var kvp in cb.textures)
            {
                Dbgl($"checking for mr with name {kvp.Key}");

                MeshRenderer mr = Array.Find(mrs, m => m.gameObject.name == kvp.Key);
                if (mr)
                {
                    /*
                    foreach (string prop in mr.material.GetTexturePropertyNames())
                    {
                        Dbgl($"prop {mr.gameObject.name}:{prop}");
                    }
                    */
                    foreach (var kvp2 in kvp.Value)
                    {
                        Dbgl($"replacing texture {kvp.Key}:{kvp2.Key}");
                        mr.material.SetTexture(kvp2.Key, kvp2.Value);
                    }
                }
            }
            /*
            if (cb.texture)
            {
                cb.texture.name = cb.building.buildingName;
                MeshRenderer[] mrs = t.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < mrs.Length; i++)
                {
                    mrs[i].material.SetTexture("_MainTex", cb.texture);
                    mrs[i].material.SetTexture("_BaseColorMap", cb.texture);
                }
            }
            */
        }

    }
}
