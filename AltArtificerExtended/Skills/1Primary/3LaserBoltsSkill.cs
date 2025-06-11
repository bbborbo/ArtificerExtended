using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using ArtificerExtended.Unlocks;
using UnityEngine;
using ArtificerExtended.States;
using RoR2.Projectile;
using R2API;
using ArtificerExtended.Modules;

namespace ArtificerExtended.Skills
{
    class _3LaserBoltsSkill : SkillBase
    {
        public static GameObject tracerLaser;

        public static float damageCoefficient = 2.2f;
        int maxStock = 4;
        public override string SkillName => "Laser Bolt";

        public override string SkillDescription => $"Fire a long-range laser that <style=cIsUtility>chains lightning</style> " +
            $"for <style=cIsDamage>2x{Tools.ConvertDecimal(damageCoefficient)} damage</style>. " +
            $"Hold up to 4, recharging all at once.";

        public override string TOKEN_IDENTIFIER => "LASERBOLTS";

        public override Type RequiredUnlock => (typeof(WoolieRushUnlock));

        public override MageElement Element => MageElement.Lightning;

        public override Type ActivationState => typeof(FireLaserbolts);

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseMaxStock: maxStock,
                rechargeStock: maxStock,
                resetCooldownTimerOnUse: true,
                beginSkillCooldownOnSkillEnd: true,
                useAttackSpeedScaling: true
            );

        public override Sprite Icon => LoadSpriteFromBundle("LaserboltIcon");
        public override SkillSlot SkillSlot => SkillSlot.Primary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Any;
        public override Type BaseSkillDef => typeof(SteppedSkillDef);
        public override float BaseCooldown => 2f;
        public override void Init()
        {
            //FireLaserbolts.maxRange = config.Bind<float>(
            //    "Skills Config: " + SkillName, "Max Range",
            //    FireLaserbolts.maxRange,
            //    "Determines the maximum range laser bolts has. Damage Falloff still applies."
            //    ).Value;
            //FireLaserbolts.damageCoefficient = config.Bind<float>(
            //    "Skills Config: " + SkillName, "Damage Coefficient",
            //    FireLaserbolts.damageCoefficient,
            //    "Determines the damage coefficient laser bolts has. Damage Falloff still applies."
            //    ).Value;
            CreateTracer();
            base.Init();
        }


        public override void Hooks()
        {
        }

        private void CreateTracer()
        {
            tracerLaser = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/tracers/TracerGolem").InstantiateClone("tracerMageLaser", false);
            Tracer buckshotTracer = tracerLaser.GetComponent<Tracer>();
            buckshotTracer.speed = 300f;
            buckshotTracer.length = 15f;
            buckshotTracer.beamDensity = 10f;
            VFXAttributes buckshotAttributes = tracerLaser.AddComponent<VFXAttributes>();
            buckshotAttributes.vfxPriority = VFXAttributes.VFXPriority.Always;
            buckshotAttributes.vfxIntensity = VFXAttributes.VFXIntensity.High;

            Tools.GetParticle(tracerLaser, "SmokeBeam", new Color(0.05f, 0.45f, 1f),  0.66f);
            ParticleSystem.MainModule main = tracerLaser.GetComponentInChildren<ParticleSystem>().main;
            main.startSizeXMultiplier *= 0.4f;
            main.startSizeYMultiplier *= 0.4f;
            main.startSizeZMultiplier *= 2f;

            Content.CreateAndAddEffectDef(tracerLaser);
        }
    }
}
