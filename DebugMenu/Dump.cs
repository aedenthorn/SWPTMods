﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

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

            string output =
                "Armors:\n\n\t" + string.Join("\n\t", armors)
                + "\n\n\nWeapons:\n\t" + string.Join("\n\t", weapons)
                + "\n\n\nBlack Market Items:\n\t" + string.Join("\n\t", blackMarket)
                + "\n\n\nNecklaces:\n\t" + string.Join("\n\t", necklaces)
                + "\n\n\nPotions:\n\t" + string.Join("\n\t", potions)
                + "\n\n\nRings:\n\t" + string.Join("\n\t", rings)
                + "\n\n\nSpecial Items:\n\t" + string.Join("\n\t", special)
                + "\n\n\nTreasures:\n\t" + string.Join("\n\t", treasures);

            string path = Path.Combine(AedenthornUtils.GetAssetPath(typeof(BepInExPlugin).Namespace), "dump_items.txt");
            File.WriteAllText(path, output);
            Dbgl($"Dumped items to {path}");
        }

    }
}