using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Teleportation
{
    [BepInPlugin("aedenthorn.Teleportation", "Teleportation", "0.1.1")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> blinkThroughWalls;
        public static ConfigEntry<string> modKey;
        public static ConfigEntry<string> gatherCompanionKey;
        public static ConfigEntry<string> returnToStartKey;
        public static ConfigEntry<string> returnToHomeKey;
        public static ConfigEntry<string> recallCompanionKey;
        public static ConfigEntry<string> blinkTeleportModKey;
        public static ConfigEntry<float> blinkTeleportDistance;

        private static Vector3 startPosition;
        private static Quaternion startRotation;

        //public static ConfigEntry<int> nexusID;

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
            
            blinkThroughWalls = Config.Bind<bool>("Options", "BlinkThroughWalls", true, "Enable blinking through walls");
            modKey = Config.Bind<string>("Options", "modKey", "", "Modifier key to allow return functions. Can leave blank.");
            gatherCompanionKey = Config.Bind<string>("Options", "GatherCompanionKey", "[*]", "Hotkey to gather your party companions around you.");
            returnToStartKey = Config.Bind<string>("Options", "ReturnToStartKey", "[-]", "Hotkey to return to the starting position in the scene.");
            returnToHomeKey = Config.Bind<string>("Options", "ReturnToHomeKey", "[+]", "Hotkey to return home.");
            recallCompanionKey = Config.Bind<string>("Options", "RecallCompanionKey", "[/]", "Hotkey to return home.");
            blinkTeleportModKey = Config.Bind<string>("Options", "BlinkTeleportModKey", "right shift", "modifier to teleport instead of moving. Leave blank to disable.");
            blinkTeleportDistance = Config.Bind<float>("Options", "BlinkTeleportDistance", 5, "Distance to move when blink teleporting.");

            //nexusID = Config.Bind<int>("General", "NexusID", 1, "Nexus mod ID for updates");

            SceneManager.sceneLoaded += SceneManager_sceneLoaded;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
            Dbgl("Plugin awake");

        }

        private static void SceneManager_sceneLoaded(UnityEngine.SceneManagement.Scene arg0, LoadSceneMode arg1)
        {
            if (!Player.code)
                return;
            startPosition = Player.code.transform.position;
            startRotation = Player.code.transform.rotation;
        }


        [HarmonyPatch(typeof(Player), "Update")]
        static class Player_Update_Patch
        {
            static void Prefix(Player __instance, ref bool ___m_Jump, ThirdPersonCharacter ___m_Character)
            {
                if (!modEnabled.Value)
                    return;

                if (AedenthornUtils.CheckKeyHeld(blinkTeleportModKey.Value))
                {
                    Vector3 forward = new Vector3(__instance.m_Cam.transform.forward.x, 0, __instance.m_Cam.transform.forward.z).normalized;
                    Vector3 right = new Vector3(__instance.m_Cam.transform.right.x, 0, __instance.m_Cam.transform.right.z).normalized;
                    Vector3 move = Vector3.zero;
                    if(PMC_Setting.code.GetKeyDown("Move Left"))
                    {
                        move -= right * blinkTeleportDistance.Value;
                    }
                    if(PMC_Setting.code.GetKeyDown("Move Right"))
                    {
                        move += right * blinkTeleportDistance.Value;
                    }
                    if(PMC_Setting.code.GetKeyDown("Move Forward"))
                    {
                        move += forward * blinkTeleportDistance.Value;
                    }
                    if(PMC_Setting.code.GetKeyDown("Move Back"))
                    {
                        move -= forward * blinkTeleportDistance.Value;
                    }

                    Vector3 newPos = __instance.transform.position + move;

                    if(!blinkThroughWalls.Value && Physics.Raycast(__instance.transform.position, (newPos - __instance.transform.position).normalized, out RaycastHit raycastHit1, blinkTeleportDistance.Value, (int)AccessTools.Field(typeof(ThirdPersonCharacter), "layerMask").GetValue(___m_Character)))
                    {
                        newPos = raycastHit1.point;
                    }

                    if (Physics.Raycast(newPos + new Vector3(0, 2, 0), Vector3.down, out RaycastHit raycastHit2, 8, (int)AccessTools.Field(typeof(ThirdPersonCharacter), "layerMask").GetValue(___m_Character)))
                    {
                        __instance.transform.position = new Vector3(newPos.x, raycastHit2.point.y, newPos.z);
                    }
                }
                else if(AedenthornUtils.CheckKeyHeld(modKey.Value, false))
                {
                    if (AedenthornUtils.CheckKeyDown(returnToStartKey.Value))
                    {
                        __instance.transform.position = startPosition;
                        __instance.transform.rotation = startRotation;

                    }
                    else if (AedenthornUtils.CheckKeyDown(returnToHomeKey.Value) && Global.code.currentHome)
                    {
                        Mainframe.code.uILoading.OpenLoading(Global.code.currentHome.transform);
                    }
                    else if (AedenthornUtils.CheckKeyDown(gatherCompanionKey.Value))
                    {
                        foreach(Transform c in Global.code.playerCombatParty.items)
                        {
                            c.GetComponent<NavMeshAgent>().enabled = false;

                            Vector3 rand = new Vector3(Random.Range(1,2) * (Random.value > 0.5 ? -1 : 1), 0, Random.Range(1, 2) * (Random.value > 0.5 ? -1 : 1));
                            c.position = __instance.transform.position + rand;
                            c.GetComponent<NavMeshAgent>().enabled = true;

                        }
                    }
                    else if (AedenthornUtils.CheckKeyDown(recallCompanionKey.Value))
                    {
                        Dbgl($"Recalling companions");

                        Scene.code.SpawnCompanions();
                    }
                }
            }
        }
    }
}
