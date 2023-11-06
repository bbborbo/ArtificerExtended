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
        static float zapDistance = 8f;
        static float zapDamageFraction = 1f;
        static float zapDamageCoefficient = 0.2f;
        public static ModdedDamageType ZapOnHit;
        public static void CreateZapDamageType()
        {
            ZapOnHit = ReserveDamageType();
            //On.RoR2.GlobalEventManager.OnHitAll += ZapDamageTypeHook;
        }

        private static void ZapDamageTypeHook(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            if (damageInfo.HasModdedDamageType(ZapOnHit))
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
        public static List<BuffDef> buffDefs = new List<BuffDef>();

        public static DotController.DotIndex burnDot;
        public static DotController.DotIndex strongBurnDot;


        public static void CreateBuffs()
        {
            burnDot = DotController.DotIndex.Burn;
            strongBurnDot = DotController.DotIndex.StrongerBurn;

            AddAAPassiveBuffs();
        }

        internal static void AddBuff(BuffDef buff)
        {
            buffDefs.Add(buff);
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
        public static List<EffectDef> effectDefs = new List<EffectDef>();

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
                        Debug.Log("shit");
                        GameObject newOrb = ps.gameObject.InstantiateClone("NebulaOrb" + i, false);
                        i++;
                        smallOrbs.Add(newOrb);
                    }
                }
                Debug.Log(smallOrbs.Count);
            }
            else
            {
                Debug.Log("SHIT!!!!!");
            }
        }

        private static void CreateLightningPreFire()
        {
            AltArtiPassive.lightningPreFireEffect = new GameObject[3];
            for (Int32 i = 0; i < AltArtiPassive.lightningPreFireEffect.Length; i++)
            {
                GameObject effect = CreateLightningSwordGhost(i);
                GameObject.Destroy(effect.GetComponent<ProjectileGhostController>());
                AltArtiPassive.lightningPreFireEffect[i] = effect;
            }
        }

        internal static GameObject CreateLightningSwordGhost(Int32 meshInd)
        {
            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/ProjectileGhosts/ElectricWormSeekerGhost").InstantiateClone("LightningSwordGhost", false);

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

            //CreateEffect(obj);

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

        internal static GameObject CreateIceDelayEffect()
        {
            CreateIceBombTex();

            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/AffixWhiteDelayEffect").InstantiateClone("iceDelay", false);
            obj.GetComponent<DestroyOnTimer>().duration = 0.2f;

            ParticleSystemRenderer sphere = obj.transform.Find("Nova Sphere").GetComponent<ParticleSystemRenderer>();
            Material mat = UnityEngine.Object.Instantiate<Material>(sphere.material);
            mat.SetTexture("_RemapTex", iceBombTex);
            sphere.material = mat;

            CreateEffect(obj);

            return obj;
        }

        internal static GameObject CreateIceExplosionEffect()
        {
            CreateIceBombTex();

            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/AffixWhiteExplosion").InstantiateClone("IceExplosion", false);
            ParticleSystemRenderer sphere = obj.transform.Find("Nova Sphere").GetComponent<ParticleSystemRenderer>();
            Material mat = UnityEngine.Object.Instantiate<Material>(sphere.material);
            mat.SetTexture("_RemapTex", iceBombTex);
            sphere.material = mat;

            CreateEffect(obj);

            return obj;
        }

        internal static Texture2D iceBombTex;
        internal static void CreateIceBombTex()
        {
            if (iceBombTex != null)
            {
                return;
            }

            var iceGrad = new Gradient
            {
                mode = GradientMode.Blend,
                alphaKeys = new GradientAlphaKey[8]
                {
                    new GradientAlphaKey( 0f, 0f ),
                    new GradientAlphaKey( 0f, 0.14f ),
                    new GradientAlphaKey( 0.22f, 0.46f ),
                    new GradientAlphaKey( 0.22f, 0.61f),
                    new GradientAlphaKey( 0.72f, 0.63f ),
                    new GradientAlphaKey( 0.72f, 0.8f ),
                    new GradientAlphaKey( 0.87f, 0.81f ),
                    new GradientAlphaKey( 0.87f, 1f )
                },
                colorKeys = new GradientColorKey[8]
                {
                    new GradientColorKey( new Color( 0f, 0f, 0f ), 0f ),
                    new GradientColorKey( new Color( 0f, 0f, 0f ), 0.14f ),
                    new GradientColorKey( new Color( 0.179f, 0.278f, 0.250f ), 0.46f ),
                    new GradientColorKey( new Color( 0.179f, 0.278f, 0.250f ), 0.61f ),
                    new GradientColorKey( new Color( 0.5f, 0.8f, 0.75f ), 0.63f ),
                    new GradientColorKey( new Color( 0.5f, 0.8f, 0.75f ), 0.8f ),
                    new GradientColorKey( new Color( 0.6f, 0.9f, 0.85f ), 0.81f ),
                    new GradientColorKey( new Color( 0.6f, 0.9f, 0.85f ), 1f )
                }
            };

            iceBombTex = CreateNewRampTex(iceGrad);
        }

        private static Texture2D CreateNewRampTex(Gradient grad)
        {
            var tex = new Texture2D(256, 8, TextureFormat.RGBA32, false);

            Color tempC;
            var tempCs = new Color[8];

            for (Int32 i = 0; i < 256; i++)
            {
                tempC = grad.Evaluate(i / 255f);
                for (Int32 j = 0; j < 8; j++)
                {
                    tempCs[j] = tempC;
                }

                tex.SetPixels(i, 0, 1, 8, tempCs);
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
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

            effectDefs.Add(def);
            return def;
        }
    }
    public static class Projectiles
    {
        internal static void CreateLightningSwords()
        {
            AltArtiPassive.lightningProjectile = new GameObject[3];
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
            projStick.ignoreCharacters = false;
            projStick.ignoreWorld = false;
            projStick.alignNormals = false;

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
            mdtyhc.Add(Assets.ZapOnHit);


            proj.AddComponent<Components.SoundOnAwake>().sound = "Play_item_proc_dagger_spawn";

            //UnityEngine.Object.DestroyImmediate( proj.GetComponent<ProjectileSingleTargetImpact>() );
            UnityEngine.Object.Destroy(proj.GetComponent<AwakeEvent>());
            UnityEngine.Object.Destroy(proj.GetComponent<DelayedEvent>());

            ContentPacks.projectilePrefabs.Add(proj);
            AltArtiPassive.lightningProjectile[meshInd] = proj;
        }

        internal static void CreateIceExplosion()
        {
            GameObject blast = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GenericDelayBlast").InstantiateClone("IceDelayBlast", false);
            DelayBlast component = blast.GetComponent<DelayBlast>();
            component.crit = false;
            component.procCoefficient = 1.0f;
            component.maxTimer = 0.25f;
            component.falloffModel = BlastAttack.FalloffModel.None;
            component.explosionEffect = Effects.CreateIceExplosionEffect();
            component.delayEffect = Effects.CreateIceDelayEffect();
            component.damageType = DamageType.Freeze2s;
            component.baseForce = 250f;

            AltArtiPassive.iceBlast = blast;
            //projectilePrefabs.Add(blast);
        }
    }
}
