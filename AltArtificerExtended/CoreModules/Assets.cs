using System.Reflection;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.IO;
using System.Collections.Generic;
using RoR2.UI;
using RoR2.Projectile;
using Path = System.IO.Path;
using RoR2.Skills;
using EntityStates;
using System;
using RoR2.CharacterAI;
using System.Linq;
using UnityEngine.AddressableAssets;
using ThreeEyedGames;
using RoR2.ExpansionManagement;
using ArtificerExtended.Passive;
using static R2API.DamageAPI;
using RoR2.EntityLogic;
using static ArtificerExtended.Modules.Language.Styling;

namespace ArtificerExtended.Modules
{
    public static class CommonAssets
    {
        private static AssetBundle _mainAssetBundle;
        public static AssetBundle mainAssetBundle
        {
            get
            {
                if (_mainAssetBundle == null)
                    _mainAssetBundle = Assets.LoadAssetBundle("itmightbebad");
                return _mainAssetBundle;
            }
            set
            {
                _mainAssetBundle = value;
            }
        }

        public static string dropPrefabsPath = "Assets/Models/DropPrefabs";
        public static string iconsPath = "Assets/Textures/Icons/";
        public static string eliteMaterialsPath = "Assets/Textures/Materials/Elite/";

        public static void AddResonantKeyword(string keywordToken, string resonantAbilityName, string resonantDesc)
        {
            LanguageAPI.Add(keywordToken, $"<style=cKeywordName>Resonance: {resonantAbilityName}</style>" +
                $"<style=cSub>{resonantDesc}</style>");
        }
        public static void Init()
        {
            CreateZapDamageType();
            AddAAPassiveBuffs();
            AddAAKeywords();

            CreateLavaPool();
            CreateLavaProjectile();
            CreateLightningPreFire();
            CreateLightningSwords();
            CreateFlameAuraMaterial();
        }

        public static Material matMageFlameAura;
        private static void CreateFlameAuraMaterial()
        {
            matMageFlameAura = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/Base/WardOnLevel/matWarbannerSphereIndicator2.mat").WaitForCompletion());
            matMageFlameAura.name = "matMageFlameAura";
            matMageFlameAura.SetColor("_TintColor", new Color32(146, 73, 0, 201)/*(150, 110, 0, 191)*/);
            matMageFlameAura.SetFloat("_RimPower", 2);
            matMageFlameAura.SetFloat("_RimStrength", 0.58f);
        }
        #region nebula passive
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
        #endregion
        #region lightning swords
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
            GameObject ghost = CreateLightningSwordGhost(meshInd);
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
            mdtyhc.Add(ChainLightningDamageType);


            proj.AddComponent<Components.SoundOnAwake>().sound = "Play_item_proc_dagger_spawn";

            //UnityEngine.Object.DestroyImmediate( proj.GetComponent<ProjectileSingleTargetImpact>() );
            UnityEngine.Object.Destroy(proj.GetComponent<AwakeEvent>());
            UnityEngine.Object.Destroy(proj.GetComponent<DelayedEvent>());

            Content.AddProjectilePrefab(proj);
            AltArtiPassive.lightningProjectile[meshInd] = proj;
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

            Assets.DoMesh(model, meshInd, color1, color2);
            _ = GameObject.Instantiate<Material>(trailRen.material);
            model.GetComponent<MeshRenderer>().material = trailRen.material;

            return obj;
        }
        #endregion
        #region zap damage type
        public static float chainLightningZapDistance = 25f;
        public static float chainLightningZapDamageFraction = 1f;
        public static float chainLightningZapDamageCoefficient = 0.2f;
        public static ModdedDamageType ChainLightningDamageType;
        public static void CreateZapDamageType()
        {
            ChainLightningDamageType = ReserveDamageType();
        }
        #endregion

        #region EnergeticResonance
        public static BuffDef meltBuff;
        public static string meltKeywordToken = ArtificerExtendedPlugin.DEVELOPER_PREFIX + "KEYWORD_RESONANTMELT";
        public static string arcticBlastKeywordToken = ArtificerExtendedPlugin.DEVELOPER_PREFIX + "KEYWORD_RESONANTARCTICBLAST";
        public static string lightningBoltKeywordToken = ArtificerExtendedPlugin.DEVELOPER_PREFIX + "KEYWORD_RESONANTBOLTS";
        public static string magePassiveDescToken = "MAGE_PASSIVE_ENERGY_DESC";

        static void AddAAPassiveBuffs()
        {
            meltBuff = Content.CreateAndAddBuff(
                "bdResonanceMelt",
                Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/StrengthenBurn/texBuffStrongerBurnIcon.tif").WaitForCompletion(),
                new Color(0.9f, 0.4f, 0.2f), 
                true, false);
        }
        private static void AddAAKeywords()
        {
            LanguageAPI.Add(magePassiveDescToken,
                $"- {DamageColor("FIRE Resonance")} increases {UtilityColor("Incinerate")} intensity.\n" +
                $"- {DamageColor("ICE Resonance")} increases {UtilityColor("Arctic Blast")} range.\n" +
                $"- {DamageColor("LIGHTNING Resonance")} increases {UtilityColor("Lightning Bolts")} fired.");

            AddResonantKeyword(meltKeywordToken, "Incinerate",
                $"On {UtilityColor("FIRE SKILL Cast")}, gain 1 stack of {UtilityColor("Incinerate")} " +
                $"for each {DamageColor("Fire")} ability equipped. " +
                $"{UtilityColor("Incinerate")} increases {DamageColor("attack speed")} on ALL skills " +
                $"by {DamageColor(Tools.ConvertDecimal(AltArtiPassive.meltAspdIncrease))} per stack.");
            AddResonantKeyword(arcticBlastKeywordToken, "Arctic Blast",
                $"On {UtilityColor($"Freezing")} or {UtilityColor("killing Frosted enemies")}, " +
                $"cause a {UtilityColor("Frost")} blast that extends {UtilityColor(AltArtiPassive.novaRadiusPerPower.ToString() + "m")} " +
                $"for each {DamageColor("Ice")} ability equipped. " +
                $"{UtilityColor("Arctic Blasts")} deal {DamageValueText(AltArtiPassive.novaBaseDamage)} to nearby enemies.");
            AddResonantKeyword(lightningBoltKeywordToken, "Lightning Bolts",
                $"On {UtilityColor("ANY SKILL Cast")}, fire a spear of energy " +
                $"for each {DamageColor("Lightning")} ability equipped. " +
                $"Each {UtilityColor("Lightning Bolt")} seeks out enemies in front of you " +
                $"for {DamageColor("2x" + Tools.ConvertDecimal(AltArtiPassive.lightningDamageMult) + " damage")}.");

            return;
            LanguageAPI.Add(magePassiveDescToken,
                "- <style=cIsUtility>Incinerate</style> increases in intensity for each <style=cIsDamage>FIRE</style> skill." +
                "\n- <style=cIsUtility>Arctic Blasts</style> increase in radius for each <style=cIsDamage>ICE</style> skill." +
                "\n- <style=cIsUtility>Lightning Bolts</style> increase in number for each <style=cIsDamage>LIGHTNING</style> skill.");
            LanguageAPI.Add(meltKeywordToken, $"<style=cKeywordName>Incinerate</style>" +
                $"<style=cSub><style=cIsUtility>On FIRE SKILL Cast:</style> Gain a buff that " +
                $"increases your <style=cIsDamage>attack speed</style> on all skills " +
                $"by <style=cIsDamage>{Tools.ConvertDecimal(AltArtiPassive.meltAspdIncrease)}</style> per stack.");
            LanguageAPI.Add(arcticBlastKeywordToken, $"<style=cKeywordName>Arctic Blast</style>" +
                $"<style=cSub><style=cIsUtility>Applying 10 stacks</style> of Chill or <style=cIsUtility>killing Chilled enemies</style> " +
                $"causes an <style=cIsUtility>Arctic Blast,</style> " +
                $"<style=cIsUtility>Chilling</style> nearby enemies " +
                $"for <style=cIsDamage>{Tools.ConvertDecimal(AltArtiPassive.novaBaseDamage)} damage</style> " +
                $"and <style=cIsUtility>Freezing</style> the target.</style>");
            LanguageAPI.Add(lightningBoltKeywordToken, $"<style=cKeywordName>Lightning Bolts</style>" +
                $"<style=cSub><style=cIsUtility>On ANY Cast:</style> Summon spears of energy that <style=cIsUtility>seek out enemies in front of you</style> " +
                $"for <style=cIsDamage>2x{Tools.ConvertDecimal(AltArtiPassive.lightningDamageMult)} damage.</style>");
        }
        #endregion

        #region lava pools
        public static string lavaPoolKeywordToken = ArtificerExtendedPlugin.DEVELOPER_PREFIX + "KEYWORD_LAVAPOOLS";
        public static float napalmFireFrequency = 2f;
        public static float napalmDamageCoefficient = 0.25f;
        public static float napalmDuration = 3f;
        public static float napalmProcCoefficient = 0.25f;
        public static float lavaPoolSize = 3;
        public static GameObject lavaPoolPrefab;
        public static GameObject lavaProjectilePrefab;
        internal static void CreateLavaProjectile()
        {
            lavaProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("862783bd9da988641bbcdc1606415b09").WaitForCompletion().InstantiateClone("MageLavaProjectile", true); //beetlequeenspit.prefab
            GameObject lavaPoolGhostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Drones/PaladinRocketGhost.prefab").WaitForCompletion().InstantiateClone("MageLavaProjectileGhost", false);

            Color napalmColor = new Color32(255, 40, 0, 255);
            GameObject lavaImpactEffect = Addressables.LoadAssetAsync<GameObject>("e184c0c8bc862ff40b9fd07db0b8e98c").WaitForCompletion().InstantiateClone("NapalmSpitExplosion", false); //beetlespitexplosion
            Tools.GetParticle(lavaImpactEffect, "Bugs", Color.clear);
            Tools.GetParticle(lavaImpactEffect, "Flames", napalmColor);
            Tools.GetParticle(lavaImpactEffect, "Flash", Color.yellow);
            Tools.GetParticle(lavaImpactEffect, "Distortion", napalmColor);
            Tools.GetParticle(lavaImpactEffect, "Ring, Mesh", Color.yellow);

            ProjectileImpactExplosion pieNapalm = lavaProjectilePrefab.GetComponent<ProjectileImpactExplosion>();
            if (pieNapalm && lavaPoolPrefab != null)
            {
                pieNapalm.blastDamageCoefficient = 1;
                pieNapalm.childrenProjectilePrefab = lavaPoolPrefab;
                pieNapalm.impactEffect = lavaImpactEffect;
                pieNapalm.blastRadius = CommonAssets.lavaPoolSize;
                //projectilePrefabNapalm.GetComponent<ProjectileImpactExplosion>().destroyOnEnemy = true;
                pieNapalm.blastProcCoefficient = 1;
                pieNapalm.bonusBlastForce = new Vector3(0, 500, 0);
            }

            ProjectileController pc = lavaProjectilePrefab.GetComponent<ProjectileController>();
            pc.ghostPrefab = lavaPoolGhostPrefab;

            ProjectileDamage pd = lavaProjectilePrefab.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.IgniteOnHit;

            Content.CreateAndAddEffectDef(lavaImpactEffect);
            Content.AddProjectilePrefab(lavaProjectilePrefab);
        }
        internal static void CreateLavaPool()
        {
            LanguageAPI.Add(lavaPoolKeywordToken, $"<style=cKeywordName>Molten Pools</style>" +
                $"<style=cSub>Creates a <style=cIsDamage>lingering pool</style> on impact with any surface, " +
                $"which <style=cIsDamage>ignites</style> enemies and deals " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(CommonAssets.napalmFireFrequency * CommonAssets.napalmDamageCoefficient)}</style> TOTAL damage per second.</style>");

            //lavaPoolPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/beetlequeenacid").InstantiateClone("LavaPool", true);
            string path = "21516298ba7220c41bc56ab2e0215f92";//beetlequeenacid; "RoR2/DLC1/Molotov/MolotovProjectileDotZone.prefab";//
            lavaPoolPrefab = Addressables.LoadAssetAsync<GameObject>(path).WaitForCompletion().InstantiateClone("MageLavaPool", true);

            string path2 = "ee66af398b28cda46b311d0be4f9ca0a";//beetlequeenacidghost; "RoR2/DLC1/Molotov/MolotovProjectileDotZone.prefab";//
            GameObject lavaPoolGhostPrefab = Addressables.LoadAssetAsync<GameObject>(path2).WaitForCompletion().InstantiateClone("MageLavaPoolGhost", false);


            lavaPoolPrefab.transform.localScale = new Vector3(lavaPoolSize, lavaPoolSize, lavaPoolSize);

            ProjectileDotZone pdz = lavaPoolPrefab.GetComponent<ProjectileDotZone>();
            if (pdz)
            {
                pdz.lifetime = napalmDuration;
                pdz.resetFrequency = napalmFireFrequency;
                pdz.damageCoefficient = napalmDamageCoefficient;
                pdz.overlapProcCoefficient = napalmProcCoefficient;
                pdz.attackerFiltering = AttackerFiltering.Default;
            }

            ProjectileDamage pd = lavaPoolPrefab.GetComponent<ProjectileDamage>();
            if (pd)
            {
                pd.damageType = DamageType.IgniteOnHit;
            }
            ProjectileController pc = lavaPoolPrefab.GetComponent<ProjectileController>();
            if (pc)
            {
                pc.procCoefficient = 1;
                pc.ghostPrefab = lavaPoolGhostPrefab;
            }


            Color napalmColor = new Color32(255, 100, 50, 255);

            Transform fxTransform = lavaPoolGhostPrefab.transform.Find("FX");
            if (fxTransform)
            {
                fxTransform.transform.localScale = Vector3.one * lavaPoolSize;
                fxTransform.Find("Spittle").gameObject.SetActive(false);

                Decal decal = fxTransform.GetComponentInChildren<Decal>();
                if (decal)
                {
                    decal.transform.localScale = Vector3.one * lavaPoolSize;
                    Material newMat = new Material(decal.Material);
                    newMat.name = "matMageLavaPoolDecal";
                    newMat.SetColor("_TintColor", napalmColor);
                    newMat.SetColor("_Color", napalmColor);
                    newMat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>("RoR2/DLC2/Scorchling/texRampScorchling.png").WaitForCompletion());
                    decal.Material = newMat;
                }

                GameObject FireTrail = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/FireTrail");
                GameObject fireTrailSegmentPrefab = FireTrail?.GetComponent<DamageTrail>()?.segmentPrefab;
                if (fireTrailSegmentPrefab)
                {
                    GameObject fireEffect = UnityEngine.Object.Instantiate<GameObject>(fireTrailSegmentPrefab, fxTransform.transform);
                    ParticleSystem ps = fireEffect.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule main = ps.main;
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
            }

            Content.AddProjectilePrefab(lavaPoolPrefab);
        }
        #endregion
    }

    // for simplifying rendererinfo creation
    public class CustomRendererInfo
    {
        //the childname according to how it's set up in your childlocator
        public string childName;
        //the material to use. pass in null to use the material in the bundle
        public Material material = null;
        //don't set the hopoo shader on the material, and simply use the material from your prefab, unchanged
        public bool dontHotpoo = false;
        //ignores shields and other overlays. use if you're not using a hopoo shader
        public bool ignoreOverlays = false;
    }

    internal static class Assets
    {
        //cache bundles if multiple characters use the same one
        internal static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();


        internal static void DoMesh(GameObject model, Int32 meshInd, Color color1, Color color2)
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

                    //pickuptritip.prefab
                    mesh = GameObject.Instantiate<Mesh>(Addressables.LoadAssetAsync<GameObject>("a931fe3391939d84383a030b3098d78b").WaitForCompletion().transform.Find("mdlTriTip").GetComponent<MeshFilter>().sharedMesh);
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
                    //mercbody.prefab
                    mesh = GameObject.Instantiate<Mesh>(Addressables.LoadAssetAsync<GameObject>("c9898f15e54a0194dbd2ab62ad507bd4").WaitForCompletion().transform.Find("ModelBase/mdlMerc/MercSwordMesh").GetComponent<SkinnedMeshRenderer>().sharedMesh);
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
                    mesh = GameObject.Instantiate<Mesh>(Addressables.LoadAssetAsync<GameObject>("41f30d571bf74fd4ab6e76601054e7ca").WaitForCompletion().transform.Find("ModelBase/mdlTitan/TitanArmature/ROOT/base/stomach/chest/upper_arm.r/lower_arm.r/hand.r/RightFist/Sword").GetComponent<MeshFilter>().sharedMesh);
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
        internal static AssetBundle LoadAssetBundle(string bundleName)
        {
            if (loadedBundles.ContainsKey(bundleName))
            {
                return loadedBundles[bundleName];
            }

            AssetBundle assetBundle = null;
            assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(ArtificerExtendedPlugin.instance.Info.Location), bundleName));

            loadedBundles[bundleName] = assetBundle;

            return assetBundle;

        }

        internal static GameObject CloneTracer(string originalTracerName, string newTracerName)
        {
            if (RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/" + originalTracerName) == null) 
                return null;

            GameObject newTracer = PrefabAPI.InstantiateClone(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/" + originalTracerName), newTracerName, true);

            if (!newTracer.GetComponent<EffectComponent>()) newTracer.AddComponent<EffectComponent>();
            if (!newTracer.GetComponent<VFXAttributes>()) newTracer.AddComponent<VFXAttributes>();
            if (!newTracer.GetComponent<NetworkIdentity>()) newTracer.AddComponent<NetworkIdentity>();
            
            newTracer.GetComponent<Tracer>().speed = 250f;
            newTracer.GetComponent<Tracer>().length = 50f;

            Modules.Content.CreateAndAddEffectDef(newTracer);

            return newTracer;
        }

        internal static void ConvertAllRenderersToHopooShader(GameObject objectToConvert)
        {
            if (!objectToConvert) return;

            foreach (MeshRenderer i in objectToConvert.GetComponentsInChildren<MeshRenderer>())
            {
                if (i)
                {
                    if (i.sharedMaterial)
                    {
                        i.sharedMaterial.ConvertDefaultShaderToHopoo();
                    }
                }
            }

            foreach (SkinnedMeshRenderer i in objectToConvert.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (i)
                {
                    if (i.sharedMaterial)
                    {
                        i.sharedMaterial.ConvertDefaultShaderToHopoo();
                    }
                }
            }
        }

        internal static GameObject LoadCrosshair(string crosshairName)
        {
            GameObject loadedCrosshair = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Crosshair/" + crosshairName + "Crosshair");
            if (loadedCrosshair == null)
            {
                Log.Error($"could not load crosshair with the name {crosshairName}. defaulting to Standard");

                return RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Crosshair/StandardCrosshair");
            }

            return loadedCrosshair;
        }

        internal static GameObject LoadEffect(this AssetBundle assetBundle, string resourceName, bool parentToTransform) => LoadEffect(assetBundle, resourceName, "", parentToTransform);
        internal static GameObject LoadEffect(this AssetBundle assetBundle, string resourceName, string soundName = "", bool parentToTransform = false)
        {
            GameObject newEffect = assetBundle.LoadAsset<GameObject>(resourceName);

            if (!newEffect)
            {
                Log.ErrorAssetBundle(resourceName, assetBundle.name);
                return null;
            }

            newEffect.AddComponent<DestroyOnTimer>().duration = 12;
            newEffect.AddComponent<NetworkIdentity>();
            newEffect.AddComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
            EffectComponent effect = newEffect.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.parentToReferencedTransform = parentToTransform;
            effect.positionAtReferencedTransform = true;
            effect.soundName = soundName;

            Modules.Content.CreateAndAddEffectDef(newEffect);

            return newEffect;
        }

        internal static GameObject CreateProjectileGhostPrefab(this AssetBundle assetBundle, string ghostName)
        {
            GameObject ghostPrefab = assetBundle.LoadAsset<GameObject>(ghostName);
            if (ghostPrefab == null)
            {
                Log.Error($"Failed to load ghost prefab {ghostName}");
            }
            if (!ghostPrefab.GetComponent<NetworkIdentity>()) ghostPrefab.AddComponent<NetworkIdentity>();
            if (!ghostPrefab.GetComponent<ProjectileGhostController>()) ghostPrefab.AddComponent<ProjectileGhostController>();

            Modules.Assets.ConvertAllRenderersToHopooShader(ghostPrefab);

            return ghostPrefab;
        }

        internal static GameObject CreateProjectileGhostPrefab(GameObject ghostObject, string newName)
        {
            if (ghostObject == null)
            {
                Log.Error($"Failed to load ghost prefab {ghostObject.name}");
            }
            GameObject go = PrefabAPI.InstantiateClone(ghostObject, newName);
            if (!go.GetComponent<NetworkIdentity>()) go.AddComponent<NetworkIdentity>();
            if (!go.GetComponent<ProjectileGhostController>()) go.AddComponent<ProjectileGhostController>();

            //Modules.Assets.ConvertAllRenderersToHopooShader(go);

            return go;
        }

        internal static GameObject CloneProjectilePrefab(string prefabName, string newPrefabName)
        {
            GameObject newPrefab = PrefabAPI.InstantiateClone(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/" + prefabName), newPrefabName);
            return newPrefab;
        }

        internal static GameObject LoadAndAddProjectilePrefab(this AssetBundle assetBundle, string newPrefabName)
        {
            GameObject newPrefab = assetBundle.LoadAsset<GameObject>(newPrefabName);
            if(newPrefab == null)
            {
                Log.ErrorAssetBundle(newPrefabName, assetBundle.name);
                return null;
            }

            Content.AddProjectilePrefab(newPrefab);
            return newPrefab;
        }
    }
    internal static class Materials
    {
        internal static void GetMaterial(GameObject model, string childObject, Color color, ref Material material, float scaleMultiplier = 1, bool replaceAll = false)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Renderer smr = renderer;

                if (string.Equals(renderer.name, childObject))
                {
                    if (color == Color.clear)
                    {
                        UnityEngine.GameObject.Destroy(renderer);
                        return;
                    }

                    if (material == null)
                    {
                        material = new Material(renderer.material);
                        material.mainTexture = renderer.material.mainTexture;
                        material.shader = renderer.material.shader;
                        material.color = color;
                    }
                    renderer.material = material;
                    renderer.transform.localScale *= scaleMultiplier;
                    if (!replaceAll)
                        break;
                }
            }
        }
        internal static void DebugMaterial(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Renderer smr = renderer;
                Debug.Log("Material: " + smr.name.ToString());
            }
        }

        #region shaders lol

        public static void SwapShadersFromMaterialsInBundle(AssetBundle bundle)
        {
            if (bundle.isStreamedSceneAssetBundle)
            {
                Debug.LogWarning($"Cannot swap material shaders from a streamed scene assetbundle.");
                return;
            }

            Material[] assetBundleMaterials = bundle.LoadAllAssets<Material>().Where(mat => mat.shader.name.StartsWith("Stubbed")).ToArray();

            for (int i = 0; i < assetBundleMaterials.Length; i++)
            {
                var material = assetBundleMaterials[i];
                if (!material.shader.name.StartsWith("Stubbed"))
                {
                    Debug.LogWarning($"The material {material} has a shader which's name doesnt start with \"Stubbed\" ({material.shader.name}), this is not allowed for stubbed shaders for MSU. not swapping shader.");
                    continue;
                }
                try
                {
                    SwapShader(material);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to swap shader of material {material}: {ex}");
                }
            }
        }
        private static void SwapShader(Material material)
        {
            var shaderName = material.shader.name.Substring("Stubbed".Length);
            var adressablePath = $"{shaderName}.shader";
            Shader shader = Addressables.LoadAssetAsync<Shader>(adressablePath).WaitForCompletion();
            material.shader = shader;
            MaterialsWithSwappedShaders.Add(material);
        }
        public static List<Material> MaterialsWithSwappedShaders { get; } = new List<Material>();
        #endregion

        private static List<Material> cachedMaterials = new List<Material>();

        internal static Shader hotpoo = RoR2.LegacyResourcesAPI.Load<Shader>("Shaders/Deferred/HGStandard");

        public static Material LoadMaterial(this AssetBundle assetBundle, string materialName) => CreateHopooMaterialFromBundle(assetBundle, materialName);
        public static Material CreateHopooMaterialFromBundle(this AssetBundle assetBundle, string materialName)
        {
            Material tempMat = cachedMaterials.Find(mat =>
            {
                materialName.Replace(" (Instance)", "");
                return mat.name.Contains(materialName);
            });
            if (tempMat)
            {
                Log.Debug($"{tempMat.name} has already been loaded. returning cached");
                return tempMat;
            }
            tempMat = assetBundle.LoadAsset<Material>(materialName);

            if (!tempMat)
            {
                Log.ErrorAssetBundle(materialName, assetBundle.name);
                return new Material(hotpoo);
            }

            return tempMat.ConvertDefaultShaderToHopoo();
        }

        public static Material SetHopooMaterial(this Material tempMat) => ConvertDefaultShaderToHopoo(tempMat);
        public static Material ConvertDefaultShaderToHopoo(this Material tempMat)
        {
            if (cachedMaterials.Contains(tempMat))
            {
                Log.Debug($"{tempMat.name} has already been loaded. returning cached");
                return tempMat;
            }

            float? bumpScale = null;
            Color? emissionColor = null;

            //grab values before the shader changes
            if (tempMat.IsKeywordEnabled("_NORMALMAP"))
            {
                bumpScale = tempMat.GetFloat("_BumpScale");
            }
            if (tempMat.IsKeywordEnabled("_EMISSION"))
            {
                emissionColor = tempMat.GetColor("_EmissionColor");
            }

            //set shader
            tempMat.shader = hotpoo;

            //apply values after shader is set
            tempMat.SetTexture("_EmTex", tempMat.GetTexture("_EmissionMap"));
            tempMat.EnableKeyword("DITHER");

            if (bumpScale != null)
            {
                tempMat.SetFloat("_NormalStrength", (float)bumpScale);
                tempMat.SetTexture("_NormalTex", tempMat.GetTexture("_BumpMap"));
            }
            if (emissionColor != null)
            {
                tempMat.SetColor("_EmColor", (Color)emissionColor);
                tempMat.SetFloat("_EmPower", 1);
            }

            //set this keyword in unity if you want your model to show backfaces
            //in unity, right click the inspector tab and choose Debug
            if (tempMat.IsKeywordEnabled("NOCULL"))
            {
                tempMat.SetInt("_Cull", 0);
            }
            //set this keyword in unity if you've set up your model for limb removal item displays (eg. goat hoof) by setting your model's vertex colors
            if (tempMat.IsKeywordEnabled("LIMBREMOVAL"))
            {
                tempMat.SetInt("_LimbRemovalOn", 1);
            }

            cachedMaterials.Add(tempMat);
            return tempMat;
        }

        /// <summary>
        /// Makes this a unique material if we already have this material cached (i.e. you want an altered version). New material will not be cached
        /// <para>If it was not cached in the first place, simply returns as it is already unique.</para>
        /// </summary>
        public static Material MakeUnique(this Material material)
        {

            if (cachedMaterials.Contains(material))
            {
                return new Material(material);
            }
            return material;
        }

        public static Material SetColor(this Material material, Color color)
        {
            material.SetColor("_Color", color);
            return material;
        }

        public static Material SetNormal(this Material material, float normalStrength = 1)
        {
            material.SetFloat("_NormalStrength", normalStrength);
            return material;
        }

        public static Material SetEmission(this Material material) => SetEmission(material, 1);
        public static Material SetEmission(this Material material, float emission) => SetEmission(material, emission, Color.white);
        public static Material SetEmission(this Material material, float emission, Color emissionColor)
        {
            material.SetFloat("_EmPower", emission);
            material.SetColor("_EmColor", emissionColor);
            return material;
        }
        public static Material SetCull(this Material material, bool cull = false)
        {
            material.SetInt("_Cull", cull ? 1 : 0);
            return material;
        }

        public static Material SetSpecular(this Material material, float strength)
        {
            material.SetFloat("_SpecularStrength", strength);
            return material;
        }
        public static Material SetSpecular(this Material material, float strength, float exponent)
        {
            material.SetFloat("_SpecularStrength", strength);
            material.SetFloat("SpecularExponent", exponent);
            return material;
        }
    }
    internal static class Particles
    {
        internal static void GetParticle(GameObject model, string childObject, Color color, float sizeMultiplier = 1, bool replaceAll = false)
        {
            ParticleSystem[] partSystems = model.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem partSys in partSystems)
            {
                ParticleSystem ps = partSys;
                var main = ps.main;
                var lifetime = ps.colorOverLifetime;
                var speed = ps.colorBySpeed;

                if (string.Equals(ps.name, childObject))
                {
                    main.startColor = color;
                    main.startSizeMultiplier *= sizeMultiplier;
                    lifetime.color = color;
                    speed.color = color;
                    if (!replaceAll)
                        break;
                }
            }
        }
        internal static void DebugParticleSystem(GameObject model)
        {
            ParticleSystem[] partSystems = model.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem partSys in partSystems)
            {
                ParticleSystem ps = partSys;
                Debug.Log("Particle: " + ps.name.ToString());
            }
        }
    }
    internal class Content
    {
        //consolidate contentaddition here in case something breaks and/or want to move to r2api
        internal static void AddExpansionDef(ExpansionDef expansion)
        {
            ContentPacks.expansionDefs.Add(expansion);
        }

        internal static void AddCharacterBodyPrefab(GameObject bprefab)
        {
            ContentPacks.bodyPrefabs.Add(bprefab);
        }

        internal static void AddMasterPrefab(GameObject prefab)
        {
            ContentPacks.masterPrefabs.Add(prefab);
        }

        internal static void AddProjectilePrefab(GameObject prefab)
        {
            ContentPacks.projectilePrefabs.Add(prefab);
        }

        internal static void AddSurvivorDef(SurvivorDef survivorDef)
        {

            ContentPacks.survivorDefs.Add(survivorDef);
        }
        internal static void AddItemDef(ItemDef itemDef)
        {
            ContentPacks.itemDefs.Add(itemDef);
        }
        internal static void AddEliteDef(EliteDef eliteDef)
        {
            ContentPacks.eliteDefs.Add(eliteDef);
        }
        internal static void AddArtifactDef(ArtifactDef artifactDef)
        {
            ContentPacks.artifactDefs.Add(artifactDef);
        }

        internal static void AddNetworkedObjectPrefab(GameObject prefab)
        {
            ContentPacks.networkedObjectPrefabs.Add(prefab);
        }

        internal static void AddUnlockableDef(UnlockableDef unlockableDef)
        {
            ContentPacks.unlockableDefs.Add(unlockableDef);
        }
        internal static UnlockableDef CreateAndAddUnlockbleDef(string identifier, string nameToken, Sprite achievementIcon)
        {
            UnlockableDef unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();
            unlockableDef.cachedName = identifier;
            unlockableDef.nameToken = nameToken;
            unlockableDef.achievementIcon = achievementIcon;

            AddUnlockableDef(unlockableDef);

            return unlockableDef;
        }

        internal static void AddSkillDef(SkillDef skillDef)
        {
            ContentPacks.skillDefs.Add(skillDef);
        }

        internal static void AddSkillFamily(SkillFamily skillFamily)
        {
            ContentPacks.skillFamilies.Add(skillFamily);
        }

        internal static void AddEntityState(Type entityState)
        {
            ContentPacks.entityStates.Add(entityState);
        }

        internal static void AddBuffDef(BuffDef buffDef)
        {
            ContentPacks.buffDefs.Add(buffDef);
        }
        internal static BuffDef CreateAndAddBuff(string buffName, Sprite buffIcon, Color buffColor, bool canStack, bool isDebuff)
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.name = buffName;
            buffDef.buffColor = buffColor;
            buffDef.canStack = canStack;
            buffDef.isDebuff = isDebuff;
            buffDef.eliteDef = null;
            buffDef.iconSprite = buffIcon;

            AddBuffDef(buffDef);

            return buffDef;
        }

        internal static void AddEffectDef(EffectDef effectDef)
        {
            ContentPacks.effectDefs.Add(effectDef);
        }
        internal static EffectDef CreateAndAddEffectDef(GameObject effectPrefab)
        {
            if (effectPrefab == null)
            {
                Debug.LogError("Effect prefab was null");
                return null;
            }

            EffectComponent effectComp = effectPrefab.GetComponent<EffectComponent>();
            if (effectComp == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have an EffectComponent.", effectPrefab.name);
                return null;
            }

            VFXAttributes vfxAttrib = effectPrefab.GetComponent<VFXAttributes>();
            if (vfxAttrib == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have a VFXAttributes component.", effectPrefab.name);
                return null;
            }

            EffectDef def = new EffectDef
            {
                prefab = effectPrefab,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAttrib,
                prefabName = effectPrefab.name,
                spawnSoundEventName = effectComp.soundName
            };

            AddEffectDef(def);

            return def;
        }

        internal static void AddNetworkSoundEventDef(NetworkSoundEventDef networkSoundEventDef)
        {
            ContentPacks.networkSoundEventDefs.Add(networkSoundEventDef);
        }
        internal static NetworkSoundEventDef CreateAndAddNetworkSoundEventDef(string eventName)
        {
            NetworkSoundEventDef networkSoundEventDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            networkSoundEventDef.akId = AkSoundEngine.GetIDFromString(eventName);
            networkSoundEventDef.eventName = eventName;

            AddNetworkSoundEventDef(networkSoundEventDef);

            return networkSoundEventDef;
        }
    }
    internal static class Skills
    {
        public static Dictionary<string, SkillLocator> characterSkillLocators = new Dictionary<string, SkillLocator>();

        #region genericskills
        public static void CreateSkillFamilies(GameObject targetPrefab) => CreateSkillFamilies(targetPrefab, SkillSlot.Primary, SkillSlot.Secondary, SkillSlot.Utility, SkillSlot.Special);
        /// <summary>
        /// Create in order the GenericSkills for the skillslots desired, and create skillfamilies for them.
        /// </summary>
        /// <param name="targetPrefab">Body prefab to add GenericSkills</param>
        /// <param name="slots">Order of slots to add to the body prefab.</param>
        public static void CreateSkillFamilies(GameObject targetPrefab, params SkillSlot[] slots)
        {
            SkillLocator skillLocator = targetPrefab.GetComponent<SkillLocator>();

            for (int i = 0; i < slots.Length; i++)
            {
                switch (slots[i])
                {
                    case SkillSlot.Primary:
                        skillLocator.primary = CreateGenericSkillWithSkillFamily(targetPrefab, "Primary");
                        break;
                    case SkillSlot.Secondary:
                        skillLocator.secondary = CreateGenericSkillWithSkillFamily(targetPrefab, "Secondary");
                        break;
                    case SkillSlot.Utility:
                        skillLocator.utility = CreateGenericSkillWithSkillFamily(targetPrefab, "Utility");
                        break;
                    case SkillSlot.Special:
                        skillLocator.special = CreateGenericSkillWithSkillFamily(targetPrefab, "Special");
                        break;
                    case SkillSlot.None:
                        break;
                }
            }
        }

        public static void ClearGenericSkills(GameObject targetPrefab)
        {
            foreach (GenericSkill obj in targetPrefab.GetComponentsInChildren<GenericSkill>())
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        public static GenericSkill CreateGenericSkillWithSkillFamily(GameObject targetPrefab, SkillSlot skillSlot, bool hidden = false)
        {
            SkillLocator skillLocator = targetPrefab.GetComponent<SkillLocator>();
            switch (skillSlot)
            {
                case SkillSlot.Primary:
                    return skillLocator.primary = CreateGenericSkillWithSkillFamily(targetPrefab, "Primary", hidden);
                case SkillSlot.Secondary:
                    return skillLocator.secondary = CreateGenericSkillWithSkillFamily(targetPrefab, "Secondary", hidden);
                case SkillSlot.Utility:
                    return skillLocator.utility = CreateGenericSkillWithSkillFamily(targetPrefab, "Utility", hidden);
                case SkillSlot.Special:
                    return skillLocator.special = CreateGenericSkillWithSkillFamily(targetPrefab, "Special", hidden);
                case SkillSlot.None:
                    Log.Error("Failed to create GenericSkill with skillslot None. If making a GenericSkill outside of the main 4, specify a familyName, and optionally a genericSkillName");
                    return null;
            }
            return null;
        }
        public static GenericSkill CreateGenericSkillWithSkillFamily(GameObject targetPrefab, string familyName, bool hidden = false) => CreateGenericSkillWithSkillFamily(targetPrefab, familyName, familyName, hidden);
        public static GenericSkill CreateGenericSkillWithSkillFamily(GameObject targetPrefab, string genericSkillName, string familyName, bool hidden = false)
        {
            GenericSkill skill = targetPrefab.AddComponent<GenericSkill>();
            skill.skillName = genericSkillName;
            skill.hideInCharacterSelect = hidden;

            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            (newFamily as ScriptableObject).name = targetPrefab.name + familyName + "Family";
            newFamily.variants = new SkillFamily.Variant[0];

            skill._skillFamily = newFamily;

            Content.AddSkillFamily(newFamily);
            return skill;
        }
        #endregion

        #region skillfamilies

        //everything calls this
        public static void AddSkillToFamily(SkillFamily skillFamily, SkillDef skillDef, UnlockableDef unlockableDef = null)
        {
            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);

            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = skillDef,
                unlockableDef = unlockableDef,
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null)
            };
        }

        public static void AddSkillsToFamily(SkillFamily skillFamily, params SkillDef[] skillDefs)
        {
            foreach (SkillDef skillDef in skillDefs)
            {
                AddSkillToFamily(skillFamily, skillDef);
            }
        }

        public static void AddPrimarySkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            AddSkillsToFamily(targetPrefab.GetComponent<SkillLocator>().primary.skillFamily, skillDefs);
        }
        public static void AddSecondarySkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            AddSkillsToFamily(targetPrefab.GetComponent<SkillLocator>().secondary.skillFamily, skillDefs);
        }
        public static void AddUtilitySkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            AddSkillsToFamily(targetPrefab.GetComponent<SkillLocator>().utility.skillFamily, skillDefs);
        }
        public static void AddSpecialSkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            AddSkillsToFamily(targetPrefab.GetComponent<SkillLocator>().special.skillFamily, skillDefs);
        }

        /// <summary>
        /// pass in an amount of unlockables equal to or less than skill variants, null for skills that aren't locked
        /// <code>
        /// AddUnlockablesToFamily(skillLocator.primary, null, skill2UnlockableDef, null, skill4UnlockableDef);
        /// </code>
        /// </summary>
        public static void AddUnlockablesToFamily(SkillFamily skillFamily, params UnlockableDef[] unlockableDefs)
        {
            for (int i = 0; i < unlockableDefs.Length; i++)
            {
                SkillFamily.Variant variant = skillFamily.variants[i];
                variant.unlockableDef = unlockableDefs[i];
                skillFamily.variants[i] = variant;
            }
        }
        #endregion

        #region entitystates
        public static ComboSkillDef.Combo ComboFromType(Type t)
        {
            ComboSkillDef.Combo combo = new ComboSkillDef.Combo();
            combo.activationStateType = new SerializableEntityStateType(t);
            return combo;
        }
        #endregion
    }
}