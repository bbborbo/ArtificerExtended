using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using ArtificerExtended.Unlocks;
using UnityEngine;
using ArtificerExtended.States;
using R2API;
using R2API.Utils;
using ArtificerExtended.Modules;

namespace ArtificerExtended.Skills
{
    class _4IceShardsSkill : SkillBase
    {
        //ice shard
        public static GameObject tracerShotgun;
        public static GameObject tracerBuckshot;

        public static float damageCoefficient = 6.4f;
        public static float procCoefficientPoint = 0.5f;
        public static float procCoefficientSpread = 0.7f;
        public static float procCoefficientBuckshot = 0.7f;
        public static float bulletRadius = 0.3f;

        public static int bulletCountPoint = 1;
        public static int bulletCountSpread = 1;
        public static int bulletCountBuckshot = 2;

        public static float recoilAmplitude = 3.25f;
        public static float spreadAmplitude = 1.2f;
        public static float spreadBloomValue = 0.3f;
        public static float spreadShotFraction = 0.4f;
        public override string SkillName => "Icicle Bolts";


        float totalShards = bulletCountBuckshot + bulletCountPoint + bulletCountSpread;
        public override string SkillDescription => $"<style=cIsUtility>Frost</style>. Fire a blast of ice shards for " +
            $"<style=cIsDamage>up to {totalShards}x{Tools.ConvertDecimal(damageCoefficient / totalShards)} damage</style> total. " +
            $"Hold up to 2.";

        public override string TOKEN_IDENTIFIER => "ICESHARDS";

        public override Type RequiredUnlock => (typeof(NoDamageUnlock));


        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(FireIceShard);

        public override SimpleSkillData SkillData => new SimpleSkillData
        ( 
            baseMaxStock: 2,
            rechargeStock: 1,//ArtificerExtendedPlugin.isRiskyModLoaded ? 0 : 1,
            useAttackSpeedScaling: true
        );
        public override Sprite Icon => LoadSpriteFromBundle("IceShardsIcon");
        public override SkillSlot SkillSlot => SkillSlot.Primary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Any;
        public override Type BaseSkillDef => typeof(SteppedSkillDef);
        public override float BaseCooldown => 1.3f;
        public override void Init()
        {
            //FireIceShard.maxRange = config.Bind<float>(
            //    "Skills Config: " + SkillName, "Max Range",
            //    FireIceShard.maxRange,
            //    "Determines the cutoff radius for Ice Shards bullets. Damage falloff still applies."
            //    ).Value;
            //FireIceShard.damageCoefficient = config.Bind<float>(
            //    "Skills Config: " + SkillName, "Damage Per Pellet",
            //    FireIceShard.damageCoefficient / totalShards,
            //    "Determines the max damage coefficient per Ice Shards pellet. Damage falloff still applies."
            //    ).Value * totalShards;

            KeywordTokens = new string[1] { "KEYWORD_FROST" };
            CreateTracerEffects();
            base.Init();
        }

        public override void Hooks()
        {
        }

        private void CreateTracerEffects()
        {
            tracerShotgun = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/tracers/TracerCommandoShotgun").InstantiateClone("tracerMageIceShard", false);
            Tracer shotgunTracer = tracerShotgun.GetComponent<Tracer>();
            shotgunTracer.speed = 100f;
            shotgunTracer.length = 3f;
            shotgunTracer.beamDensity = 5f;
            VFXAttributes shotgunAttributes = tracerShotgun.AddComponent<VFXAttributes>();
            shotgunAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;
            shotgunAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;

            tracerBuckshot = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/tracers/TracerCommandoShotgun").InstantiateClone("tracerMageIceShardBuckshot", false);
            Tracer buckshotTracer = tracerBuckshot.GetComponent<Tracer>();
            buckshotTracer.speed = 60f;
            buckshotTracer.length = 2f;
            buckshotTracer.beamDensity = 3f;
            VFXAttributes buckshotAttributes = tracerBuckshot.AddComponent<VFXAttributes>();
            buckshotAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;
            buckshotAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;

            Content.CreateAndAddEffectDef(tracerShotgun);
            Content.CreateAndAddEffectDef(tracerBuckshot);
        }
    }
}