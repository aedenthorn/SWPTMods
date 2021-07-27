using UnityEngine;

namespace CustomBuildings
{
    public class CustomBuilding
    {
        public Mesh mesh;
        public GameObject go;
        public Texture2D icon = new Texture2D(2, 2);
        public Texture2D texture = new Texture2D(2, 2);
        public Material material;
        public MeshRenderer mr;
        public BuildingData building;
    }

    public class BuildingData
    {
        public string buildingName = "";
        public string templateObject = "";
        public string categoryName = "";
        public Rarity rarity = Rarity.one;
        public string homeName;
    }
}