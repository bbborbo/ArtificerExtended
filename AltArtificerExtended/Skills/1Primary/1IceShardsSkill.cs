using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using AltArtificerExtended.Unlocks;
using UnityEngine;
using AltArtificerExtended.EntityState;
using R2API;
using R2API.Utils;

namespace AltArtificerExtended.Skills
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

        public override SkillFamily SkillSlot => Main.magePrimary;

        public override SimpleSkillData SkillData => new SimpleSkillData
        ( 
            baseMaxStock: 4,
            baseRechargeInterval: 1.3f
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

            Main.CreateEffect(tracerShotgun);
            Main.CreateEffect(tracerBuckshot);
        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[1] { "ARTIFICEREXTENDED_KEYWORD_CHILL" };
            CreateTracerEffects();
            CreateLang();
            CreateSkill();
        }
    }
}