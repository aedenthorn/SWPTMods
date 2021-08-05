using BepInEx;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DebugMenu
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {
       
        private static void DumpPoses()
        {
            Dictionary<string, List<string>> poses = new Dictionary<string, List<string>>();
            foreach (Transform t in RM.code.allFreePoses.items)
            {
                string cn = t.GetComponent<Pose>().categoryName;
                if (!poses.ContainsKey(cn))
                    poses.Add(cn, new List<string>());
                poses[cn].Add(t.name);
            }

            string output = "Poses:";

            foreach(var kvp in poses)
            {
                output += "\n\n\t" + kvp.Key + "\n\t\t" + string.Join("\n\t\t", kvp.Value);
            }

            string path = Path.Combine(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace), "dump_poses.txt");
            File.WriteAllText(path, output);
            Dbgl($"Dumped poses to {path}");
            Global.code.uiCombat.ShowHeader($"Poses dumped to {path}");
        }

        private static void DumpItems()
        {
            List<string> armors = new List<string>();
            foreach (Transform t in RM.code.allArmors.items)
                if(t)
                    armors.Add(t.name);


            List<string> blackMarket = new List<string>();
            foreach (Transform t in RM.code.allBlackmarketItems.items)
                if (t)
                    blackMarket.Add(t.name);

            List<string> weapons = new List<string>();
            foreach (Transform t in RM.code.allWeapons.items)
                if (t)
                    weapons.Add(t.name);

            List<string> lingerie = new List<string>();
            foreach (Transform t in RM.code.allLingeries.items)
                if (t)
                    lingerie.Add(t.name);

            List<string> necklaces = new List<string>();
            if(RM.code.allNecklaces?.items != null)
                foreach (Transform t in RM.code.allNecklaces.items)
                    if (t)
                        necklaces.Add(t.name);

            List<string> potions = new List<string>();
            foreach (Transform t in RM.code.allPotions.items)
                if (t)
                    potions.Add(t.name);

            List<string> rings = new List<string>();
            if (RM.code.allRings?.items != null)
                foreach (Transform t in RM.code.allRings.items)
                    if (t)
                        rings.Add(t.name);

            List<string> special = new List<string>();
            if (RM.code.allSpecialItems?.items != null)
                foreach (Transform t in RM.code.allSpecialItems.items)
                    if (t)
                        special.Add(t.name);

            List<string> treasures = new List<string>();
            if (RM.code.allTreasures?.items != null)
                foreach (Transform t in RM.code.allTreasures.items)
                    if (t)
                        treasures.Add(t.name);

            List<string> misc = new List<string>();
            foreach (Transform t in RM.code.allItems.items)
                if (t && !armors.Contains(t.name) && !weapons.Contains(t.name) && !blackMarket.Contains(t.name) && !necklaces.Contains(t.name) && !lingerie.Contains(t.name) && !potions.Contains(t.name) && !rings.Contains(t.name) && !special.Contains(t.name) && !treasures.Contains(t.name))
                    misc.Add(t.name);


            string output =
                "Armors:\n\n\t" + string.Join("\n\t", armors)
                + "\n\n\nWeapons:\n\t" + string.Join("\n\t", weapons)
                + "\n\n\nBlack Market Items:\n\t" + string.Join("\n\t", blackMarket)
                + "\n\n\nLingerie:\n\t" + string.Join("\n\t", lingerie)
                + "\n\n\nNecklaces:\n\t" + string.Join("\n\t", necklaces)
                + "\n\n\nPotions:\n\t" + string.Join("\n\t", potions)
                + "\n\n\nRings:\n\t" + string.Join("\n\t", rings)
                + "\n\n\nSpecial Items:\n\t" + string.Join("\n\t", special)
                + "\n\n\nTreasures:\n\t" + string.Join("\n\t", treasures)
                + "\n\n\nMisc:\n\t" + string.Join("\n\t", misc);

            string path = Path.Combine(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace), "dump_items.txt");
            File.WriteAllText(path, output);
            Dbgl($"Dumped items to {path}");
            Global.code.uiCombat.ShowHeader($"Items dumped to {path}");
        }

    }
}
