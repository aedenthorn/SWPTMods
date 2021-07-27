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

namespace CustomBuildings
{
    [BepInPlugin("aedenthorn.CustomBuildings", "Custom Buildings", "0.1.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static List<CustomBuilding> buildings = new List<CustomBuilding>();
        private static BepInExPlugin context;

        public static ConfigEntry<int> nexusID;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        private static string assetPath;
        private static AssetBundle asset;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 184, "Nexus id for update checking");
            isDebug = Config.Bind<bool>("General", "IsDebug", true, "Enable debug");

            if (!modEnabled.Value)
                return;

            assetPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), typeof(BepInExPlugin).Namespace);
            if (!Directory.Exists(assetPath))
            {
                Directory.CreateDirectory(assetPath);
                return;
            }

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);

            PreloadBuildings();
        }

        private void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
        {
            if (arg0.name != "The Palace")
                return;
            //var gos = FindObjectsOfType<GameObject>();
            //var names = gos.Select(g => g.name);
            //File.WriteAllLines(Path.Combine(assetPath, arg0.name + "_scenedump.txt"), names);

        }

        private static void PreloadBuildings()
        {
            buildings.Clear();

            Dbgl($"Importing buildings");

            

            foreach (string folder in Directory.GetDirectories(assetPath))
            {
                string name = Path.GetFileName(folder);
                
                Dbgl($"Trying to create building data for {name}");


                if(!File.Exists(Path.Combine(folder, "data.json")))
                {
                    Dbgl($"building {name} is missing data.json!");
                    continue;
                }

                CustomBuilding building = new CustomBuilding();

                var icon = File.ReadAllBytes(Path.Combine(folder, "icon.png"));
                building.icon.LoadImage(icon);

                string json = File.ReadAllText(Path.Combine(folder, "data.json"));
                building.building = JsonUtility.FromJson<BuildingData>(json);

                //DontDestroyOnLoad(building.go);
                buildings.Add(building);

                asset = AssetBundle.LoadFromFile(Path.Combine(folder, "bundle"));

                continue;
                //building.go = smith;
                //Dbgl($"building.go: {building.go?.name}");

                //buildings.Add(building);

                //continue;

                //var texture = File.ReadAllBytes(Path.Combine(folder, "texture.png"));
                //building.texture.LoadImage(texture);






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
                    */
                    building.go = imported;
                    /*
                    Mesh mesh = new ObjImporter().ImportFile(Path.Combine(folder, "mesh.obj"));
                    if (mesh != null)
                        building.mesh = mesh;
                    */
                    Dbgl($"Imported fbx as go");
                }
                catch (Exception ex)
                {
                    Dbgl($"Error importing fbx as game object:\n\n{ex}");
                }
                buildings.Add(building); 
            }
        }

        [HarmonyPatch(typeof(RM), "LoadResources")]
        static class RM_LoadResources_Patch
        {
            static void Postfix(RM __instance)
            {
                if (!modEnabled.Value || customMesh)
                    return;


                foreach(CustomBuilding cb in buildings)
                {

                    Dbgl($"Adding building {cb.building.buildingName}, cat {cb.building.categoryName}");

                    GameObject gameObject = new GameObject() { name = cb.building.buildingName };
                    DontDestroyOnLoad(gameObject);
                    //GameObject cbgo = Instantiate(cb.go, gameObject.transform);
                    //cbgo.transform.localPosition = Vector3.zero;
                    //gameObject.AddComponent<Furniture>();
                    gameObject.AddComponent<MapIcon>();
                    gameObject.AddComponent<Interaction>();
                    Building building = gameObject.AddComponent<Building>();
                    building.rarity = cb.building.rarity;
                    building.categoryName = cb.building.categoryName;
                    building.icon = cb.icon;
                    RM.code.allBuildings.AddItem(gameObject.transform);

                    //cbgo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
                    /*

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
                    Dbgl($"Added building {gameObject.name} to allBuildings");
                }
            }
        }
        [HarmonyPatch(typeof(UIBuilding), nameof(UIBuilding.Open))]
        static class UIBuilding_Open_Patch
        {
            static void Prefix()
            {
                foreach (CustomBuilding cb in buildings)
                {
                    var item = RM.code.allBuildings.GetItemWithName(cb.building.buildingName);
                    if (!item)
                    {
                        Dbgl($"building {cb.building.buildingName} not found in RM!");
                        continue;
                    }

                    Transform temp = GameObject.Find("/Scene").transform.Find(cb.building.templateObject);

                    Dbgl($"Adding building {cb.building.buildingName}, cat {cb.building.categoryName}, model {temp?.name}");

                    Transform t = Instantiate(temp, item);
                    t.localPosition = Vector3.zero;
                    t.gameObject.SetActive(true);
                    //gameObject.AddComponent<Furniture>();
                    item.gameObject.AddComponent<MapIcon>();
                    item.gameObject.AddComponent<Interaction>();
                    Building building = item.gameObject.AddComponent<Building>();
                    building.rarity = cb.building.rarity;
                    building.categoryName = cb.building.categoryName;
                    building.icon = cb.icon;

                    var plane = Instantiate(asset.LoadAsset("Assets/Tomcat/Tomcat.prefab") as GameObject, item);
                    plane.transform.localPosition = Vector3.zero;

                }
                foreach (Transform t in RM.code.allBuildings.items)
                {
                    //Dbgl($"building {t.name}");

                }
            }
        }
        [HarmonyPatch(typeof(Mainframe), "LoadFurniture")]
        static class Mainframe_LoadFurniture_Patch
        {
            static void Postfix(Transform __result)
            {
                CustomBuilding cb = buildings.Find(b => b.building.buildingName == __result.name);
                if (!modEnabled.Value || cb == null)
                    return;

                Transform temp = GameObject.Find("/Scene").transform.Find(cb.building.templateObject);

                Dbgl($"Adding building {cb.building.buildingName}, cat {cb.building.categoryName}, model {temp?.name}");

                Transform t = Instantiate(temp, __result);
                t.localPosition = Vector3.zero;
                t.gameObject.SetActive(true);
                __result.gameObject.AddComponent<Furniture>();
                __result.gameObject.AddComponent<MapIcon>();
                __result.gameObject.AddComponent<Interaction>();
                Building building = __result.gameObject.AddComponent<Building>();
                building.rarity = cb.building.rarity;
                building.categoryName = cb.building.categoryName;
                building.icon = cb.icon;
            }
        }
    }
}
