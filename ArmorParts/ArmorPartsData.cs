using System.Collections.Generic;
using UnityEngine;

namespace ArmorParts
{
    public class ArmorPartsData
    {
        public string name;
        public string GUID;
        public Transform armor;
        public bool showBra = false;
        public bool showPanties = false;
        public bool showSuspenders = false;
        public List<string> parts = new List<string>();
    }
}