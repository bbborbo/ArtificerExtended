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
using ThreeEyedGames;

namespace ArtificerExtended.CoreModules
{
    public static class Assets
    {
        public static float zapDistance = 25f;
        public static float zapDamageFraction = 1f;
        public static float zapDamageCoefficient = 0.2f;
        public static ModdedDamageType ChainLightning;
        public static void CreateZapDamageType()
        {
            ChainLightning = ReserveDamageType();
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
                GameObject effect = CreateLightningSwordGhost(i);
                GameObject.Destroy(effect.GetComponent<ProjectileGhostController>());

                //EffectComponent effectComponent = effect.AddComponent<EffectComponent>();
                //DestroyOnTimer dot = effect.AddComponent<DestroyOnTimer>();
                //dot.duration = 15;
                //dot.resetAgeOnDisable = true;
                //CreateEffect(effect);

                AltArtiPassive.lightningPreFireEffect[i] = effect;
            }
        }

        internal static GameObject CreateLightningSwordGhost(Int32 meshInd)
        {
            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/ProjectileGhosts/ElectricWormSeekerGhost").InstantiateClone("LightningPrefireGhost" + meshInd, false);

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
        public static float napalmFireFrequency = 2f;
        public static float napalmDamageCoefficient = 0.25f;
        public static float napalmDuration = 3f;
        public static float napalmProcCoefficient = 0.25f;
        public static float lavaPoolSize = 3.5f;
        public static GameObject lavaPoolPrefab;
        internal static void CreateLavaPool()
        {
            lavaPoolPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/beetlequeenacid").InstantiateClone("LavaPool", true);

            Color napalmColor = new Color32(255, 40, 0, 255);
            Transform pDotObjDecal = lavaPoolPrefab.transform.Find("FX/Decal");
            Material napalmDecalMaterial = new Material(pDotObjDecal.GetComponent<Decal>().Material);
            napalmDecalMaterial.SetColor("_Color", napalmColor);
            pDotObjDecal.GetComponent<Decal>().Material = napalmDecalMaterial;

            ProjectileDotZone pdz = lavaPoolPrefab.GetComponent<ProjectileDotZone>();
            pdz.lifetime = napalmDuration;
            pdz.resetFrequency = napalmFireFrequency;
            pdz.damageCoefficient = napalmDamageCoefficient;
            pdz.overlapProcCoefficient = napalmProcCoefficient;
            pdz.attackerFiltering = AttackerFiltering.Default;
            lavaPoolPrefab.GetComponent<ProjectileDamage>().damageType = DamageType.IgniteOnHit;
            lavaPoolPrefab.GetComponent<ProjectileController>().procCoefficient = 1f;


            lavaPoolPrefab.transform.localScale = new Vector3(lavaPoolSize, lavaPoolSize, lavaPoolSize);

            Transform fxTransform = lavaPoolPrefab.transform.Find("FX");
            fxTransform.Find("Spittle").gameObject.SetActive(false);

            GameObject FireTrail = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/FireTrail");
            GameObject fireTrailSegmentPrefab = FireTrail?.GetComponent<DamageTrail>()?.segmentPrefab;
            if (fireTrailSegmentPrefab)
            {
                GameObject fireEffect = UnityEngine.Object.Instantiate<GameObject>(fireTrailSegmentPrefab, fxTransform.transform);
                ParticleSystem.MainModule main = fireEffect.GetComponent<ParticleSystem>().main;
                main.duration = 8f;
                main.gravityModifier = -0.075f;
                ParticleSystem.MinMaxCurve startSizeX = main.startSizeX;
                startSizeX.constantMin *= 0.6f;
                startSizeX.constantMax *= 0.8f;
                ParticleSystem.MinMaxCurve startSizeY = main.startSizeY;
                startSizeY.constantMin *= 0.8f;
                startSizeY.constantMax *= 1f;
                ParticleSystem.MinMaxCurve startSizeZ = main.startSizeZ;
                startSizeZ.constantMin *= 0.6f;
                startSizeZ.constantMax *= 0.8f;
                ParticleSystem.MinMaxCurve startLifetime = main.startLifetime;
                startLifetime.constantMin = 0.9f;
                startLifetime.constantMax = 1.1f;
                fireEffect.GetComponent<DestroyOnTimer>().enabled = false;
                fireEffect.transform.localPosition = Vector3.zero;
                fireEffect.transform.localPosition = Vector3.zero;
                fireEffect.transform.localScale = Vector3.one;
                ParticleSystem.ShapeModule shape = fireEffect.GetComponent<ParticleSystem>().shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.scale = Vector3.one * 0.5f;
            }

            GameObject gameObject2 = fxTransform.Find("Point Light").gameObject;
            Light component2 = gameObject2.GetComponent<Light>();
            component2.color = new Color(1f, 1f, 0f);
            component2.intensity = 4f;
            component2.range = 7.5f;

            ContentPacks.projectilePrefabs.Add(lavaPoolPrefab);
        }
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
            GameObject proj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarNeedleProjectile").InstantiateClone("LightningSwordProjectile" + meshInd.ToString(), false);

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


            proj.AddComponent<Components.SoundOnAwake>().sound = "Play_item_proc_dagger_spawn";

            //UnityEngine.Object.DestroyImmediate( proj.GetComponent<ProjectileSingleTargetImpact>() );
            UnityEngine.Object.Destroy(proj.GetComponent<AwakeEvent>());
            UnityEngine.Object.Destroy(proj.GetComponent<DelayedEvent>());

            ContentPacks.projectilePrefabs.Add(proj);
            AltArtiPassive.lightningProjectile[meshInd] = proj;
        }
    }
}
