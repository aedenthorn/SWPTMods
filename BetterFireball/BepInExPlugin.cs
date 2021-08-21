using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterFireball
{
    [BepInPlugin("aedenthorn.BetterFireball", "Better Fireball", "0.4.0")]
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

        [HarmonyPatch(typeof(PSMeshRendererUpdater), "OnEnable")]
        static class PSMeshRendererUpdater_OnEnable_Patch
        {
            static void Prefix(PSMeshRendererUpdater __instance)
            {
                if (!modEnabled.Value)
                    return;
                if (!__instance.MeshObject)
                {
                    __instance.MeshObject = new GameObject();
                    __instance.MeshObject.transform.SetParent(__instance.transform);
                }
                foreach (ParticleSystem ps in __instance.GetComponentsInChildren<ParticleSystem>(true))
                {
                    if (ps.shape.shapeType == ParticleSystemShapeType.MeshRenderer && !ps.shape.meshRenderer)
                    {
                        Dbgl("no mesh renderer, fixing");
                        AccessTools.Method(typeof(ParticleSystem.ShapeModule), "set_shapeType_Injected").Invoke(null, new object[] { ps.shape, ParticleSystemShapeType.Sphere });
                        AccessTools.Method(typeof(ParticleSystem.ShapeModule), "set_radius_Injected").Invoke(null, new object[] { ps.shape, 0.2f });
                    }
                }
            }
        }
        
        [HarmonyPatch(typeof(PSMeshRendererUpdater), "UpdateVisibleStatus")]
        static class PSMeshRendererUpdater_UpdateVisibleStatus_Patch
        {
            static void Prefix(PSMeshRendererUpdater __instance)
            {
                if (!modEnabled.Value)
                    return;
            }
        }
        
        [HarmonyPatch(typeof(Object), "Destroy", new Type[] { typeof(Object), typeof(float) })]
        static class Object_Destroy_Patch
        {
            static void Prefix(ref Object obj)
            {
                if (!modEnabled.Value)
                    return;
                if (obj is Transform)
                    obj = (obj as Transform).gameObject;
            }
        }

        [HarmonyPatch(typeof(MagicExplosion), "Start")]
        static class MagicExplosion_Start_Patch
        {
            static bool Prefix(MagicExplosion __instance)
            {
                if (!modEnabled.Value || (__instance.name != "FireBall" &&__instance.name != "Frostbite"))
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
                if (!modEnabled.Value || (__instance.name != "FireBall" && __instance.name != "Frostbite") || !spellsGoThroughFriends.Value || !___collid || !__instance.root.caster.GetComponent<CharacterCustomization>())
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
    }
}
