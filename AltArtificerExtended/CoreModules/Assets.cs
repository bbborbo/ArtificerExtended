using ArtificerExtended.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API;
using R2API.Utils;
using RoR2.Projectile;
using ArtificerExtended.Passive;
using RoR2.EntityLogic;
using static R2API.DamageAPI;
using RoR2.Orbs;

namespace ArtificerExtended.CoreModules
{
    public static class Assets
    {
        static float zapDistance = 15f;
        static float zapDamageFraction = 1f;
        static float zapDamageCoefficient = 0.2f;
        public static ModdedDamageType ChainLightning;
        public static void CreateZapDamageType()
        {
            ChainLightning = ReserveDamageType();
            //On.RoR2.GlobalEventManager.OnHitAll += ChainLightningHook;
        }

        private static void ChainLightningHook(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            if (damageInfo.HasModdedDamageType(ChainLightning))
            {
                LightningOrb lightningOrb2 = new LightningOrb();
                lightningOrb2.origin = damageInfo.position;
                lightningOrb2.damageValue = damageInfo.damage * zapDamageFraction;
                lightningOrb2.isCrit = damageInfo.crit;
                lightningOrb2.bouncesRemaining = 0;
                lightningOrb2.teamIndex = TeamComponent.GetObjectTeam(damageInfo.attacker);
                lightningOrb2.attacker = damageInfo.attacker;
                lightningOrb2.bouncedObjects = new List<HealthComponent>();
                HealthComponent victimHealthComponent = hitObject.GetComponent<HealthComponent>();
                if (victimHealthComponent)
                    lightningOrb2.bouncedObjects.Add(victimHealthComponent);
                lightningOrb2.procChainMask = damageInfo.procChainMask;
                lightningOrb2.procCoefficient = 0.2f;
                lightningOrb2.lightningType = LightningOrb.LightningType.Ukulele;
                lightningOrb2.damageColorIndex = DamageColorIndex.Default;
                lightningOrb2.range = zapDistance;
                lightningOrb2.canBounceOnSameTarget = false;
                HurtBox hurtBox2 = lightningOrb2.PickNextTarget(damageInfo.position);
                if (hurtBox2)
                {
                    lightningOrb2.target = hurtBox2;
                    OrbManager.instance.AddOrb(lightningOrb2);
                }
            }
            orig(self, damageInfo, hitObject);
        }
    }
    public static class Buffs
    {
        public static DotController.DotIndex burnDot;
        public static DotController.DotIndex strongBurnDot;


        public static void CreateBuffs()
        {
            burnDot = DotController.DotIndex.Burn;
            strongBurnDot = DotController.DotIndex.StrongerBurn;

            AddAAPassiveBuffs();
        }

        public static void AddBuff(BuffDef buffDef)
        {
            ContentPacks.buffDefs.Add(buffDef);
        }

        #region EnergeticResonance
        public static BuffDef meltBuff;

        static void AddAAPassiveBuffs()
        {
            Sprite meltSprite = LegacyResourcesAPI.Load<Sprite>("RoR2/DLC1/StrengthenBurn/texBuffStrongerBurnIcon.png");
            meltBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                meltBuff.buffColor = new Color(0.9f, 0.4f, 0.2f);
                meltBuff.canStack = true;
                meltBuff.iconSprite = meltSprite;
                meltBuff.isDebuff = false;
                meltBuff.name = "AltArtiFireBuff";
            }
            AddBuff(meltBuff);
            RoR2Application.onLoad += Fucksadghuderfbghujlaergh;
        }

        private static void Fucksadghuderfbghujlaergh()
        {
            meltBuff.iconSprite = DLC1Content.Buffs.StrongerBurn.iconSprite;
        }
        #endregion
    }
    public class Effects
    {
        public static void DoEffects() => CreateLightningPreFire();

        public static void CreateNebulaOrbitals()
        {
            GameObject teleporterPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/teleporters/Teleporter1");
            if (teleporterPrefab != null)
            {
                List<GameObject> smallOrbs = new List<GameObject>();
                int i = 0;
                foreach (FlickerLight ps in teleporterPrefab.GetComponentsInChildren<FlickerLight>())
                {
                    Debug.Log(ps.gameObject.name);
                    if (ps.gameObject.name == "SmallOrb")
                    {
                        GameObject newOrb = ps.gameObject.InstantiateClone("NebulaOrb" + i, false);
                        i++;
                        smallOrbs.Add(newOrb);
                    }
                }
                Debug.Log(smallOrbs.Count);
            }
        }

        private static void CreateLightningPreFire()
        {
            AltArtiPassive.lightningPreFireEffect = new GameObject[AltArtiPassive.lightningSwordEffectCount];
            for (Int32 i = 0; i < AltArtiPassive.lightningPreFireEffect.Length; i++)
            {
                GameObject effect = CreateLightningSwordGhost(i, true);
                GameObject.Destroy(effect.GetComponent<ProjectileGhostController>());

                //EffectComponent effectComponent = effect.AddComponent<EffectComponent>();
                //DestroyOnTimer dot = effect.AddComponent<DestroyOnTimer>();
                //dot.duration = 15;
                //dot.resetAgeOnDisable = true;
                //CreateEffect(effect);

                AltArtiPassive.lightningPreFireEffect[i] = effect;
            }
        }

        internal static GameObject CreateLightningSwordGhost(Int32 meshInd, bool isPreFire = false)
        {
            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/ProjectileGhosts/ElectricWormSeekerGhost").InstantiateClone("LightningBolt" + (isPreFire ? "Prefire" : "ProjectileGhost") + meshInd, false);

            GameObject model = obj.transform.Find("mdlRock").gameObject;
            GameObject trail = obj.transform.Find("Trail").gameObject;
            TrailRenderer trailRen = trail.GetComponent<TrailRenderer>();
            trail.SetActive(false);
            model.SetActive(true);
            GameObject.Destroy(model.GetComponent<RotateObject>());

            Color color1 = trailRen.startColor;
            Color color2 = trailRen.endColor;

            DoMesh(model, meshInd, color1, color2);
            _ = GameObject.Instantiate<Material>(trailRen.material);
            model.GetComponent<MeshRenderer>().material = trailRen.material;

            return obj;
        }

        private static void DoMesh(GameObject model, Int32 meshInd, Color color1, Color color2)
        {
            Mesh mesh;
            Vector2 baseUV;
            Color[] colors;
            Vector2[] uvs;
            switch (meshInd)
            {
                default:
                    Debug.LogError("Mesh index for sword out of range");
                    break;
                case 0:
                    model.transform.localScale = new Vector3(0.5f, 0.5f, 1.5f);

                    mesh = GameObject.Instantiate<Mesh>(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/PickupModels/PickupTriTip").transform.Find("mdlTriTip").GetComponent<MeshFilter>().sharedMesh);
                    model.GetComponent<MeshFilter>().sharedMesh = mesh;
                    baseUV = new Vector2(0.5f, 0.5f);
                    uvs = mesh.uv;
                    colors = new Color[uvs.Length];

                    for (Int32 i = 0; i < uvs.Length; i++)
                    {
                        Vector2 uv = uvs[i];
                        Single t = Mathf.Pow(uv.x - 0.5f, 2) + Mathf.Pow(uv.y - 0.5f, 2);
                        colors[i] = Color.Lerp(color1, color2, Mathf.Sqrt(t));
                        uvs[i] = baseUV;
                        //colors[i] = 
                    }
                    mesh.uv = uvs;
                    mesh.colors = colors;
                    break;

                case 1:
                    model.transform.localScale = new Vector3(1f, 1f, 1f);
                    mesh = GameObject.Instantiate<Mesh>(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/MercBody").transform.Find("ModelBase/mdlMerc/MercSwordMesh").GetComponent<SkinnedMeshRenderer>().sharedMesh);
                    model.GetComponent<MeshFilter>().sharedMesh = mesh;
                    baseUV = new Vector2(0.5f, 0.5f);
                    uvs = mesh.uv;
                    colors = new Color[uvs.Length];

                    for (Int32 i = 0; i < uvs.Length; i++)
                    {
                        Vector2 uv = uvs[i];
                        Single t = Mathf.Pow(uv.x - 0.5f, 2) + Mathf.Pow(uv.y - 0.5f, 2);
                        colors[i] = Color.Lerp(color1, color2, Mathf.Sqrt(t));
                        uvs[i] = baseUV;
                        //colors[i] = 
                    }
                    mesh.uv = uvs;
                    mesh.colors = colors;
                    break;

                case 2:
                    model.transform.localScale = new Vector3(0.06f, 0.06f, 0.15f);
                    model.transform.eulerAngles = new Vector3(0f, 180f, 0f);
                    mesh = GameObject.Instantiate<Mesh>(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/TitanGoldBody").transform.Find("ModelBase/mdlTitan/TitanArmature/ROOT/base/stomach/chest/upper_arm.r/lower_arm.r/hand.r/RightFist/Sword").GetComponent<MeshFilter>().sharedMesh);
                    model.GetComponent<MeshFilter>().sharedMesh = mesh;
                    baseUV = new Vector2(0.5f, 0.5f);
                    uvs = mesh.uv;
                    colors = new Color[uvs.Length];

                    for (Int32 i = 0; i < uvs.Length; i++)
                    {
                        Vector2 uv = uvs[i];
                        Single t = Mathf.Pow(uv.x - 0.5f, 2) + Mathf.Pow(uv.y - 0.5f, 2);
                        colors[i] = Color.Lerp(color1, color2, Mathf.Sqrt(t));
                        uvs[i] = baseUV;
                        //colors[i] = 
                    }
                    mesh.uv = uvs;
                    mesh.colors = colors;
                    break;
            }
        }
        public static EffectDef CreateEffect(GameObject effect)
        {
            if (effect == null)
            {
                Debug.LogError("Effect prefab was null");
                return null;
            }

            var effectComp = effect.GetComponent<EffectComponent>();
            if (effectComp == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have an EffectComponent.", effect.name);
                return null;
            }

            var vfxAttrib = effect.GetComponent<VFXAttributes>();
            if (vfxAttrib == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have a VFXAttributes component.", effect.name);
                return null;
            }

            var def = new EffectDef
            {
                prefab = effect,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAttrib,
                prefabName = effect.name,
                spawnSoundEventName = effectComp.soundName
            };

            ContentPacks.effectDefs.Add(def);
            return def;
        }
    }
    public static class Projectiles
    {
        internal static void CreateLightningSwords()
        {
            AltArtiPassive.lightningProjectile = new GameObject[AltArtiPassive.lightningSwordEffectCount];
            for (Int32 i = 0; i < AltArtiPassive.lightningProjectile.Length; i++)
            {
                CreateLightningSword(i);
            }
        }

        private static void CreateLightningSword(Int32 meshInd)
        {
            GameObject ghost = Effects.CreateLightningSwordGhost(meshInd);
            GameObject proj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarNeedleProjectile").InstantiateClone("LightningBoltProjectile" + meshInd.ToString(), false);

            UnityEngine.Networking.NetworkIdentity netID = proj.GetComponent<UnityEngine.Networking.NetworkIdentity>();
            netID.localPlayerAuthority = true;


            ProjectileDamage projDamage = proj.GetComponent<ProjectileDamage>();
            projDamage.damage = 1;

            ProjectileController projController = proj.GetComponent<ProjectileController>();
            projController.ghostPrefab = ghost;
            projController.procCoefficient = AltArtiPassive.lightningProcCoef;
            projController.allowPrediction = true;

            ProjectileSimple projSimple = proj.GetComponent<ProjectileSimple>();
            projSimple.enabled = true;
            projSimple.enableVelocityOverLifetime = false;
            projSimple.desiredForwardSpeed = 80f;


            ProjectileDirectionalTargetFinder projTargetFind = proj.GetComponent<ProjectileDirectionalTargetFinder>();
            projTargetFind.enabled = true;
            projTargetFind.lookRange = 150;
            projTargetFind.lookCone = 25;

            ProjectileSteerTowardTarget projSteering = proj.GetComponent<ProjectileSteerTowardTarget>();
            projSteering.enabled = true;
            projSteering.rotationSpeed = 120f;

            ProjectileStickOnImpact projStick = proj.GetComponent<ProjectileStickOnImpact>();
            GameObject.Destroy(projStick);
            /*projStick.ignoreCharacters = false;
            projStick.ignoreWorld = false;
            projStick.alignNormals = false;*/

            ProjectileImpactExplosion projExpl = proj.GetComponent<ProjectileImpactExplosion>();
            projExpl.impactEffect = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/LightningStakeNova");
            projExpl.explosionSoundString = "Play_item_proc_dagger_impact";// "Play_item_lunar_primaryReplace_impact";
            projExpl.lifetimeExpiredSoundString = "";
            projExpl.offsetForLifetimeExpiredSound = 0f;
            projExpl.destroyOnEnemy = true;
            projExpl.destroyOnWorld = true;
            projExpl.falloffModel = BlastAttack.FalloffModel.None;
            projExpl.lifetime = 10f;
            projExpl.lifetimeRandomOffset = 0f;
            projExpl.blastRadius = 0.1f;
            projExpl.blastDamageCoefficient = AltArtiPassive.lightningBlastDamageMult;
            projExpl.blastProcCoefficient = AltArtiPassive.lightningProcCoef;
            projExpl.bonusBlastForce = Vector3.zero;
            projExpl.fireChildren = false;
            projExpl.childrenProjectilePrefab = null;
            projExpl.childrenCount = 0;
            projExpl.childrenDamageCoefficient = 0f;
            projExpl.minAngleOffset = Vector3.zero;
            projExpl.maxAngleOffset = Vector3.zero;
            projExpl.transformSpace = ProjectileImpactExplosion.TransformSpace.World;
            projExpl.projectileHealthComponent = null;

            /*ProjectileSingleTargetImpact projStimp = proj.GetComponent<ProjectileSingleTargetImpact>();
            projStimp.destroyOnWorld = false;
            projStimp.hitSoundString = "Play_item_proc_dagger_impact";
            projStimp.enemyHitSoundString = "Play_item_proc_dagger_impact";*/

            ModdedDamageTypeHolderComponent mdtyhc = proj.AddComponent<ModdedDamageTypeHolderComponent>();
            mdtyhc.Add(Assets.ChainLightning);

            ParticleSystem FUCKYOU = proj.GetComponentInChildren<ParticleSystem>();
            if(FUCKYOU != null)
            {
                UnityEngine.Object.Destroy(FUCKYOU.gameObject);
            }

            proj.AddComponent<Components.SoundOnAwake>().sound = "Play_item_proc_dagger_spawn";

            //UnityEngine.Object.DestroyImmediate( proj.GetComponent<ProjectileSingleTargetImpact>() );
            UnityEngine.Object.Destroy(proj.GetComponent<AwakeEvent>());
            UnityEngine.Object.Destroy(proj.GetComponent<DelayedEvent>());

            ContentPacks.projectilePrefabs.Add(proj);
            AltArtiPassive.lightningProjectile[meshInd] = proj;
        }
    }
}
