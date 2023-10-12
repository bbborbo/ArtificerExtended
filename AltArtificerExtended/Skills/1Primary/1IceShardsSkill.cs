using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using ArtificerExtended.Unlocks;
using UnityEngine;
using ArtificerExtended.EntityState;
using R2API;
using R2API.Utils;

namespace ArtificerExtended.Skills
{
    class _1IceShardsSkill : SkillBase
    {
        //ice shard
        public static GameObject tracerShotgun;
        public static GameObject tracerBuckshot;

        public override string SkillName => "Ice Shards";


        float totalShards = FireIceShard.bulletCountBuckshot + FireIceShard.bulletCountPoint + FireIceShard.bulletCountSpread;
        public override string SkillDescription => $"Fire a blast of ice shards for " +
                $"<style=cIsDamage>up to {totalShards}x{Tools.ConvertDecimal(FireIceShard.damageCoefficient / totalShards)} damage</style> total, " +
            "which <style=cIsUtility>Chills</style> enemies. Hold up to 4.";

        public override string SkillLangTokenName => "ICESHARDS";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerIceShardsUnlock));

        public override string IconName => "IceShardsIcon";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(FireIceShard);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.magePrimary;

        public override SimpleSkillData SkillData => new SimpleSkillData
        ( 
            baseMaxStock: 4,
            baseRechargeInterval: 1.3f,
            rechargeStock: ArtificerExtendedPlugin.isRiskyModLoaded ? 0 : 1
        );
        public override bool useSteppedDef { get; set; } = true;

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

            ArtificerExtendedPlugin.CreateEffect(tracerShotgun);
            ArtificerExtendedPlugin.CreateEffect(tracerBuckshot);
        }

        public override void Init(ConfigFile config)
        {
            FireIceShard.maxRange = config.Bind<float>(
                "Skills Config: " + SkillName, "Max Range", 
                FireIceShard.maxRange, 
                "Determines the cutoff radius for Ice Shards bullets. Damage falloff still applies."
                ).Value;
            FireIceShard.damageCoefficient = config.Bind<float>(
                "Skills Config: " + SkillName, "Damage Per Pellet",
                FireIceShard.damageCoefficient / totalShards,
                "Determines the max damage coefficient per Ice Shards pellet. Damage falloff still applies."
                ).Value * totalShards;

            KeywordTokens = new string[1] { ChillRework.ChillRework.chillKeywordToken };
            CreateTracerEffects();
            CreateLang();
            CreateSkill();
        }
    }
}