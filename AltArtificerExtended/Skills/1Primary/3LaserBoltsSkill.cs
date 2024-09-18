using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using ArtificerExtended.Unlocks;
using UnityEngine;
using ArtificerExtended.EntityState;
using RoR2.Projectile;
using R2API;
using ArtificerExtended.CoreModules;

namespace ArtificerExtended.Skills
{
    class _3LaserBoltsSkill : SkillBase
    {
        public static GameObject tracerLaser;

        int maxStock = 4;
        public override string SkillName => "Laser Bolts";

        public override string SkillDescription => $"Fire a long-range laser that <style=cIsUtility>chains lightning</style> " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(FireLaserbolts.damageCoefficient)} damage</style>. " +
            $"Hold up to 4, recharging all at once.";

        public override string SkillLangTokenName => "LASERS";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerLaserUnlock));

        public override string IconName => "LaserboltIcon";

        public override MageElement Element => MageElement.Lightning;

        public override Type ActivationState => typeof(FireLaserbolts);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.magePrimary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseMaxStock: maxStock,
                baseRechargeInterval: 2f,
                interruptPriority: InterruptPriority.Any,
                rechargeStock: maxStock,
                resetCooldownTimerOnUse: true,
                beginSkillCooldownOnSkillEnd: true
            );
        public override bool useSteppedDef { get; set; } = true;


        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            FireLaserbolts.maxRange = config.Bind<float>(
                "Skills Config: " + SkillName, "Max Range",
                FireLaserbolts.maxRange,
                "Determines the maximum range laser bolts has. Damage Falloff still applies."
                ).Value;
            FireLaserbolts.damageCoefficient = config.Bind<float>(
                "Skills Config: " + SkillName, "Damage Coefficient",
                FireLaserbolts.damageCoefficient,
                "Determines the damage coefficient laser bolts has. Damage Falloff still applies."
                ).Value;


            CreateLang();
            CreateSkill();
            CreateTracer();
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

            Effects.CreateEffect(tracerLaser);
        }
    }
}
