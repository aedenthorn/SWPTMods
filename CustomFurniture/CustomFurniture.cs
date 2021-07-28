using System.Collections.Generic;
using UnityEngine;

namespace CustomFurniture
{
    public class CustomFurniture
    {
        public Mesh mesh;
        public GameObject go;
        public Texture2D icon = new Texture2D(2, 2);
        public Texture2D texture;
        public Material material;
        public MeshRenderer mr;
        public FurnitureMeta meta;
        public Dictionary<string, Dictionary<string, Texture2D>> textures = new Dictionary<string, Dictionary<string, Texture2D>>();
    }

    public class FurnitureMeta
    {
        public string buildingName = "";
        public string templateObject = "";
        public string categoryName = "";
        public int rarity = 1;
        public string homeName;
        public string sceneName;
        public Vector3 scale = Vector3.one;
        public string tag;
    }
}