using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace MonsterAITweaks
{
    [BepInPlugin("aedenthorn.MonsterAITweaks", "Monster AI Tweaks", "0.1.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;

        public static ConfigEntry<int> nexusID;

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
            //nexusID = Config.Bind<int>("General", "NexusID", 27, "Nexus mod ID for updates");

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(ID), nameof(ID.AddElementalDamage))]
        static class ID_AddHealth_Patch
        {
            static void Postfix(ID __instance, float pt)
            {
                if (!modEnabled.Value || !__instance.GetComponent<Monster>() || !__instance.damageSource || pt > 0 || __instance.health <= 0)
                    return;

                if(!__instance.GetComponent<Monster>().target)
                    __instance.GetComponent<Monster>().movingToTarget = __instance.damageSource;

                __instance.GetComponent<Monster>().charge = true;
                if (__instance.GetComponent<Monster>().enemySpawner && Random.Range(0, 100) < 50 && __instance.GetComponent<Monster>().enemySpawner.generatedEnemies.items.Count > 0)
                {
                    Transform t = __instance.GetComponent<Monster>().enemySpawner.generatedEnemies.items[Random.Range(0, __instance.GetComponent<Monster>().enemySpawner.generatedEnemies.items.Count)];
                    if (t && t.GetComponent<Monster>().attackDist < 10f)
                    {
                        t.GetComponent<Monster>().charge = true;
                    }
                }
            }
        }
    }
}
