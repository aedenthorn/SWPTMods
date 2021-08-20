using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BetterFireball
{
    [BepInPlugin("aedenthorn.BetterFireball", "Better Fireball", "0.3.0")]
    public partial class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;

        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> isDebug;
        public static ConfigEntry<bool> spellsGoThroughFriends;
        public static ConfigEntry<bool> spellsHurtFriends;
        public static ConfigEntry<float> radiusMultiplier;

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

            nexusID = Config.Bind<int>("General", "NexusID", 10, "Nexus mod ID for updates");

            spellsGoThroughFriends = Config.Bind<bool>("Options", "SpellsGoThroughFriends", true, "Spells will pass through friends.");
            spellsHurtFriends = Config.Bind<bool>("Options", "SpellsHurtFriends", false, "Spells will damage friends.");
            radiusMultiplier = Config.Bind<float>("Options", "RadiusMultiplier", 1, "Explosion radius multiplier.");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), Info.Metadata.GUID);
            Dbgl("Plugin awake");

        }

        [HarmonyPatch(typeof(MagicExplosion), "Start")]
        static class MagicExplosion_Start_Patch
        {
            static bool Prefix(MagicExplosion __instance)
            {
                if (!modEnabled.Value)
                    return true;

                __instance.radius *= radiusMultiplier.Value;

                Dbgl($"radius: {__instance.radius}");

                //Dbgl($"Explosion: {__instance.transform.name}, caster: {__instance.owner?.name}");


                bool friendlyDamage = true;
                bool enemyDamage = true;
                if (__instance.owner)
                {
                    if (__instance.owner.GetComponent<ID>().isFriendly || __instance.owner.GetComponent<ID>().player)
                    {
                        //Dbgl("Will not damage friends");
                        friendlyDamage = false;
                    }
                    else
                    {
                        //Dbgl("Will not damage enemies");
                        enemyDamage = false;
                    }
                }

                int enemyCount = 0;
                int friendCount = 0;

                if (enemyDamage || (friendlyDamage && spellsHurtFriends.Value))
                {
                    List<Transform> enemies = new List<Transform>(Global.code.enemies.items);
                    for (int i = enemies.Count - 1; i >= 0; i--)
                    {
                        Transform transform = enemies[i];
                        if (transform && Vector3.Distance(__instance.transform.position, transform.position) < __instance.radius)
                        {
                            enemyCount++;

                            AccessTools.Method(typeof(MagicExplosion), "DealElementalDamage").Invoke(__instance, new object[] { transform });
                        }
                    }
                }
                if (friendlyDamage || (enemyDamage && spellsHurtFriends.Value))
                {
                    List<Transform> friends = new List<Transform>(Global.code.friendlies.items);
                    for (int i = friends.Count - 1; i >= 0; i--)
                    {
                        Transform transform = friends[i];
                        if (transform && Vector3.Distance(__instance.transform.position, transform.position) < __instance.radius)
                        {
                            friendCount++;

                            AccessTools.Method(typeof(MagicExplosion), "DealElementalDamage").Invoke(__instance, new object[] { transform });
                        }
                    }
                }

                Dbgl($"Magic explosion, caster {__instance.owner.name}, position {__instance.transform.position}, radius {__instance.radius}, enemies hurt: {enemyCount}, friends hurt: {friendCount}");
                return false;
            }
        }
        [HarmonyPatch(typeof(RFX4_PhysicsMotion), "InitializeRigid")]
        static class InitializeRigid_Patch
        {
            static void Postfix(RFX4_PhysicsMotion __instance, SphereCollider ___collid)
            {
                if (!modEnabled.Value || !spellsGoThroughFriends.Value || !___collid || !__instance.root.caster.GetComponent<CharacterCustomization>())
                    return;

                Dbgl("Friendly firing spell");

                foreach (Transform t in Global.code.playerCombatParty.items)
                {
                    //Dbgl($"Ignoring collision with {t.name}");
                    Physics.IgnoreCollision(___collid, t.GetComponent<Collider>());

                    foreach (Collider c in t.GetComponentsInChildren<Collider>())
                    {
                        //Dbgl($"Ignoring collision between {__instance.name} and {c.name}");
                        Physics.IgnoreCollision(___collid, c);
                    }
                }

            }
        }
        [HarmonyPatch(typeof(ID), nameof(ID.AddElementalDamage))]
        static class AddElementalDamage_Patch
        {
            static void Prefix(ID __instance, ElementalDamageType damagetype, float pt, int duration, Transform _damageSource)
            {
                if (!modEnabled.Value || damagetype != ElementalDamageType.fire || !Environment.StackTrace.Contains("MagicExplosion"))
                    return;

                Dbgl($"Fire damage, source {_damageSource.name}, victim {__instance.name}, source {_damageSource.name}, amount {pt}, resistance {__instance.fireResist}");

            }
        }
        [HarmonyPatch(typeof(PSMeshRendererUpdater), "UpdateVisibleStatus")]
        static class PSMeshRendererUpdater_UpdateVisibleStatus_Patch
        {
            static bool Prefix(PSMeshRendererUpdater __instance, float ___alpha, Dictionary<string, float> ___startAlphaColors)
            {
                if (!modEnabled.Value)
                    return true;

                MethodInfo UpdateAlphaByProperties = AccessTools.Method(typeof(PSMeshRendererUpdater), "UpdateAlphaByProperties");
                foreach (Renderer renderer in __instance.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer.materials == null)
                        continue;
                    Material[] materials = renderer.materials;
                    for (int j = 0; j < materials.Length; j++)
                    {
                        if (materials[j].name.Contains("MeshEffect"))
                        {
                            UpdateAlphaByProperties.Invoke(__instance, new object[] { renderer.GetHashCode().ToString(), j, materials[j], ___alpha });
                        }
                    }
                }
                foreach (Renderer renderer2 in __instance.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer2.materials == null)
                        continue;
                    Material[] materials2 = renderer2.materials;
                    for (int k = 0; k < materials2.Length; k++)
                    {
                        if (materials2[k].name.Contains("MeshEffect"))
                        {
                            UpdateAlphaByProperties.Invoke(__instance, new object[] { renderer2.GetHashCode().ToString(), k, materials2[k], ___alpha });
                        }
                    }
                }
                if (__instance.MeshObject)
                {
                    foreach (Renderer renderer3 in __instance.MeshObject.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer3.materials == null)
                            continue;
                        Material[] materials3 = renderer3.materials;
                        for (int l = 0; l < materials3.Length; l++)
                        {
                            if (materials3[l].name.Contains("MeshEffect"))
                            {
                                UpdateAlphaByProperties.Invoke(__instance, new object[] { renderer3.GetHashCode().ToString(), l, materials3[l], ___alpha });
                            }
                        }
                    }
                    foreach (Renderer renderer4 in __instance.MeshObject.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer4.materials == null)
                            continue;

                        Material[] materials4 = renderer4.materials;
                        for (int m = 0; m < materials4.Length; m++)
                        {
                            if (materials4[m].name.Contains("MeshEffect"))
                            {
                                UpdateAlphaByProperties.Invoke(__instance, new object[] { renderer4.GetHashCode().ToString(), m, materials4[m], ___alpha });
                            }
                        }
                    }
                }
                //Dbgl("5");
                ME_LightCurves[] componentsInChildren2 = __instance.GetComponentsInChildren<ME_LightCurves>(true);
                for (int i = 0; i < componentsInChildren2.Length; i++)
                {
                    componentsInChildren2[i].enabled = __instance.IsActive;
                }
                //Dbgl("6");
                Light[] componentsInChildren3 = __instance.GetComponentsInChildren<Light>(true);
                if(componentsInChildren3.Length > 0)
                {
                    for (int n = 0; n < componentsInChildren3.Length; n++)
                    {
                        if (!__instance.IsActive)
                        {
                            float num = ___startAlphaColors[componentsInChildren3[n].GetHashCode().ToString() + n];
                            componentsInChildren3[n].intensity = ___alpha * num;
                        }
                    }
                }
                //Dbgl("7");
                foreach (ParticleSystem particleSystem in __instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (!__instance.IsActive && !particleSystem.isStopped)
                    {
                        particleSystem.Stop();
                    }
                    if (__instance.IsActive && particleSystem.isStopped)
                    {
                        particleSystem.Play();
                    }
                }
                //Dbgl("8");
                ME_TrailRendererNoise[] componentsInChildren5 = __instance.GetComponentsInChildren<ME_TrailRendererNoise>();
                if (componentsInChildren5.Length > 0)
                {
                    for (int i = 0; i < componentsInChildren5.Length; i++)
                    {
                        componentsInChildren5[i].IsActive = __instance.IsActive;
                    }
                }

                return false;
            }
        }
    }
}
