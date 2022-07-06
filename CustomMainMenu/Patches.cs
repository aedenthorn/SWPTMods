using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace CustomMainMenu
{
    public partial class BepInExPlugin : BaseUnityPlugin
    {

        public static bool loaded = false;

        [HarmonyPatch(typeof(UIDesktop), "Awake")]
        static class UIDesktop_Awake_Patch
        {

            static void Postfix(UIDesktop __instance)
            {
                if (!modEnabled.Value)
                    return;
                loaded = false;
            }
        }

        [HarmonyPatch(typeof(UIDesktop), "Update")]
        static class UIDesktop_Update_Patch
        {

            static void Postfix(UIDesktop __instance)
            {
                if (!modEnabled.Value)
                    return;

                if (!loaded)
                {
                    kiraCharacter = GameObject.Find("Kira").transform;

                    if (poseName.Value.Trim().Length == 0)
                    {
                        var poses = RM.code.allFreePoses.items.FindAll(t => t.GetComponent<Pose>().categoryName == "Standing");
                        Dbgl($"Standing Poses:");
                        foreach (var pose in poses)
                        {
                            Dbgl($"\t{pose.name}");
                        }
                    }

                    lightPositions = new Vector3[]
                    {
                        GameObject.Find("Inventory Scene/hide (2)/Point Light (13)").transform.localPosition,
                        GameObject.Find("Inventory Scene/hide (2)/Point Light (20)").transform.localPosition,
                        GameObject.Find("Inventory Scene/hide (2)/Point Light (21)").transform.localPosition,
                        GameObject.Find("Inventory Scene/hide (2)/Point Light (22)").transform.localPosition,
                    };


                    LoadCustomLightData();

                    LoadCustomCharacter();

                    LoadPoseData();

                    LoadBackgroundImage();
                    loaded = true;
                }


                if (backgroundName.Value.Trim().Length != 0)
                {
                    lastBackgroundUpdate = 0;
                    return;
                }



                lastBackgroundUpdate += Time.deltaTime;
                MeshRenderer mr = GameObject.Find("Inventory Scene/hide (2)/Plane").GetComponent<MeshRenderer>();
                if (lastBackgroundUpdate >= backgroundChangeInterval.Value)
                {

                    if (mr.material.color.a > 0)
                        mr.material.color -= new Color(0.01f, 0.01f, 0.01f, 0.01f);
                    else
                    {
                        LoadBackgroundImage();
                        lastBackgroundUpdate = 0;
                    }
                }
                else if (mr.material.color.a < 1)
                    mr.material.color += new Color(0.01f, 0.01f, 0.01f, 0.01f);

            }
        }
    }
}
