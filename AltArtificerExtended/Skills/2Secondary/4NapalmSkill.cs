using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using ArtificerExtended.Unlocks;
using UnityEngine;
using ArtificerExtended.EntityState;
using RoR2.Projectile;
using ThreeEyedGames;
using R2API;
using R2API.Utils;
using ArtificerExtended.CoreModules;

namespace ArtificerExtended.Skills
{
    class _4NapalmSkill : SkillBase
    {
        //napalm
        public static GameObject projectilePrefabNapalm;
        public static GameObject acidPrefabNapalm;
        public static GameObject projectileNapalmImpact;
        public static GameObject projectileNapalmFX;

        public static float napalmDotFireFrequency = 2f;
        public static int napalmMaxProjectiles = ChargeNapalm.maxProjectileCount * ChargeNapalm.maxRowCount;
        public static float napalmBurnDPS => (ChargeNapalm.napalmBurnDamageCoefficient * ChargeNapalm.totalImpactDamageCoefficient * napalmDotFireFrequency) / napalmMaxProjectiles;
        public override string SkillName => "Napalm Cascade";

        public override string SkillDescription => $"Charge up a barrage of fiery napalm, creating flaming pools that " +
            $"<style=cIsDamage>continuously Ignite</style> enemies " +
            $"for <style=cIsDamage>{napalmMaxProjectiles}x{Tools.ConvertDecimal(napalmBurnDPS)} damage per second</style>. " +
            $"Charging focuses the cone of fire.";

        public override string SkillLangTokenName => "NAPALM";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerNapalmUnlock));

        public override string IconName => "napalmicon";

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(ChargeNapalm);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.mageSecondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 8,
                interruptPriority: InterruptPriority.Skill,
                beginSkillCooldownOnSkillEnd: true
            );


        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            ChargeNapalm.totalImpactDamageCoefficient = config.Bind<float>(
                "Skills Config: " + SkillName, "Primary Damage Coefficient",
                ChargeNapalm.totalImpactDamageCoefficient,
                "Determines the total damage dealt by each Napalm impact. This is divided by six, for each projectile."
                ).Value;
            ChargeNapalm.napalmBurnDamageCoefficient = config.Bind<float>(
                "Skills Config: " + SkillName, "Secondary Damage Coefficient",
                ChargeNapalm.napalmBurnDamageCoefficient,
                "Determines the damage per tick of napalm, expressed as a fraction of the Primary damage coefficient. " +
                "Ex - if the Primary damage coefficient is 0.7, and the Secondary damage coefficient is 0.5, " +
                "then each tick of damage will have a coefficient of 0.35."
                ).Value;
            ChargeNapalm.projectileHSpeed = config.Bind<float>(
                "Skills Config: " + SkillName, "Projectile Speed",
                ChargeNapalm.projectileHSpeed,
                "Determines the speed of napalm projectiles."
                ).Value;
            napalmDotFireFrequency = config.Bind<float>(
                "Skills Config: " + SkillName, "DOT frequency",
                napalmDotFireFrequency,
                "Determines the amount of times each Napalm pool ticks each second."
                ).Value;

            KeywordTokens = new string[1] { "KEYWORD_IGNITE" };

            CreateSkill();
            CreateLang();
            RegisterProjectileNapalm();
        }
        private void RegisterProjectileNapalm()
        {
            projectilePrefabNapalm = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/beetlequeenspit").InstantiateClone("NapalmSpit", true);
            acidPrefabNapalm = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/beetlequeenacid").InstantiateClone("NapalmFire", true);

            Color napalmColor = new Color32(255, 40, 0, 255);


            Transform pDotObjDecal = acidPrefabNapalm.transform.Find("FX/Decal");
            Material napalmDecalMaterial = new Material(pDotObjDecal.GetComponent<Decal>().Material);
            napalmDecalMaterial.SetColor("_Color", napalmColor);
            pDotObjDecal.GetComponent<Decal>().Material = napalmDecalMaterial;

            GameObject ghostPrefab = projectilePrefabNapalm.GetComponent<ProjectileController>().ghostPrefab;
            projectileNapalmFX = ghostPrefab.InstantiateClone("NapalmSpitGhost", false);
            Tools.GetParticle(projectileNapalmFX, "SpitCore", napalmColor);

            projectileNapalmImpact = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/BeetleSpitExplosion").InstantiateClone("NapalmSpitExplosion", false);
            Tools.GetParticle(projectileNapalmImpact, "Bugs", Color.clear);
            Tools.GetParticle(projectileNapalmImpact, "Flames", napalmColor);
            Tools.GetParticle(projectileNapalmImpact, "Flash", Color.yellow);
            Tools.GetParticle(projectileNapalmImpact, "Distortion", napalmColor);
            Tools.GetParticle(projectileNapalmImpact, "Ring, Mesh", Color.yellow);

            ProjectileImpactExplosion pieNapalm = projectilePrefabNapalm.GetComponent<ProjectileImpactExplosion>();
            pieNapalm.childrenProjectilePrefab = acidPrefabNapalm;
            projectilePrefabNapalm.GetComponent<ProjectileController>().ghostPrefab = projectileNapalmFX;
            pieNapalm.impactEffect = projectileNapalmImpact;
            //projectilePrefabNapalm.GetComponent<ProjectileImpactExplosion>().destroyOnEnemy = true;
            pieNapalm.blastProcCoefficient = 0.5f;
            pieNapalm.bonusBlastForce = new Vector3(0, 500, 0);

            ProjectileDamage pd = projectilePrefabNapalm.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.IgniteOnHit;

            ProjectileDotZone pdz = acidPrefabNapalm.GetComponent<ProjectileDotZone>();
            pdz.lifetime = 4f;
            pdz.resetFrequency = napalmDotFireFrequency;
            pdz.damageCoefficient = ChargeNapalm.napalmBurnDamageCoefficient;
            pdz.overlapProcCoefficient = 0.5f;
            pdz.attackerFiltering = AttackerFiltering.Default;
            acidPrefabNapalm.GetComponent<ProjectileDamage>().damageType = DamageType.IgniteOnHit;
            acidPrefabNapalm.GetComponent<ProjectileController>().procCoefficient = 1f;

            float decalScale = 3.5f;
            acidPrefabNapalm.transform.localScale = new Vector3(decalScale, decalScale, decalScale);

            Transform transform = acidPrefabNapalm.transform.Find("FX");
            transform.Find("Spittle").gameObject.SetActive(false);

            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(
                RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/FireTrail").GetComponent<DamageTrail>().segmentPrefab, transform.transform);
            ParticleSystem.MainModule main = gameObject.GetComponent<ParticleSystem>().main;
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
            gameObject.GetComponent<DestroyOnTimer>().enabled = false;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
            ParticleSystem.ShapeModule shape = gameObject.GetComponent<ParticleSystem>().shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.scale = Vector3.one * 0.5f;

            GameObject gameObject2 = transform.Find("Point Light").gameObject;
            Light component2 = gameObject2.GetComponent<Light>();
            component2.color = new Color(1f, 1f, 0f);
            component2.intensity = 4f;
            component2.range = 7.5f;

            Effects.CreateEffect(projectileNapalmImpact);
            ContentPacks.projectilePrefabs.Add(projectilePrefabNapalm);
            ContentPacks.projectilePrefabs.Add(acidPrefabNapalm);
        }
    }
}
