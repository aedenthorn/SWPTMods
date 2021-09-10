using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace OrcMaiden
{
    [BepInPlugin("aedenthorn.OrcMaiden", "Orc Maiden", "0.2.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<int> nexusID;

        private static Transform orcMaiden;

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
            
            nexusID = Config.Bind<int>("General", "NexusID", 94, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");
        }


        [HarmonyPatch(typeof(RM), "LoadResources")]
        static class RM_LoadResources_Patch
        {
            static void Postfix(RM __instance)
            {
                if (!modEnabled.Value)
                    return;
                orcMaiden = __instance.alllMinionScrolls.GetItemWithName("Orc War Maiden Scroll").GetComponent<Potion>().summonMonster;

                string bodyPath = Path.Combine(AedenthornUtils.GetAssetPath(context), "body.png");
                string headPath = Path.Combine(AedenthornUtils.GetAssetPath(context), "head.png");
                if (File.Exists(bodyPath) && File.Exists(headPath))
                {
                    Dbgl("Replacing skin texture");
                    Texture2D bodyTexture = new Texture2D(1, 1);
                    bodyTexture.LoadImage(File.ReadAllBytes(bodyPath));
                    Texture2D headTexture = new Texture2D(1, 1);
                    headTexture.LoadImage(File.ReadAllBytes(headPath));
                    orcMaiden.Find("Body1").GetComponent<SkinnedMeshRenderer>().material.SetTexture("_BaseColorMap", bodyTexture);
                    orcMaiden.Find("Head4").GetComponent<SkinnedMeshRenderer>().material.SetTexture("_BaseColorMap", headTexture);
                }
                if (!__instance.allMinions.items.Contains(orcMaiden))
                {
                    Dbgl("Adding orc maiden to RM");
                    __instance.allMinions.AddItem(orcMaiden);
                }
            }
        }
        [HarmonyPatch(typeof(Scene), "SpawnPlayerSoldiers")]
        static class Scene_SpawnPlayerSoldiers_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                var newCodes = new List<CodeInstruction>();
                bool start = false;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (start)
                    {
                        newCodes.Add(codes[i]);
                    }
                    else if (codes[i].opcode == OpCodes.Ret)
                    {
                        start = true;
                    }
                }
                if (start)
                    return newCodes.AsEnumerable();
                else
                    return codes.AsEnumerable();
            }
        }
        [HarmonyPatch(typeof(Slaver), "Start")]
        static class Slaver_Start_Patch
        {
            static void Prefix(Slaver __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (!__instance.possibleSlaves.Contains(orcMaiden))
                {
                    Dbgl($"slaves: {__instance.possibleSlaves.Length}");
                    Dbgl($"Adding {orcMaiden.name} to slaver");
                    List<Transform> ps = __instance.possibleSlaves.ToList();
                    ps.Add(orcMaiden);
                    __instance.possibleSlaves = ps.ToArray();
                    Dbgl($"slaves: {__instance.possibleSlaves.Length}");
                }
            }
        }
    }
}
