using System.Collections.Generic;

namespace ArmorParts
{
    public class ArmorPartsData
    {
        public string name;
        public int id;
        public bool showBra = false;
        public bool showPanties = false;
        public bool showSuspenders = false;
        public List<string> parts = new List<string>();
    }
}