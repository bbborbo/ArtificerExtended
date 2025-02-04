using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.DamageAPI;

namespace ArtificerExtended.Skills
{
    class _1SnowballsSkill : SkillBase
    {
        public static GameObject snowballProjectilePrefab;
        public override string SkillName => "Frost Bolt";

        public override string SkillDescription => $"<style=cIsUtility>Chilling</style>. " +
            $"Throw a snowball for <style=cIsDamage>{Tools.ConvertDecimal(FireSnowBall.damageCoeff)} damage</style>.";

        public override string SkillLangTokenName => "SNOWBALL";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(FreezeManySimultaneousUnlock));

        public override string IconName => "SnowballIcon";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(FireSnowBall);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.magePrimary;

        public override SimpleSkillData SkillData => new SimpleSkillData
        (
            baseRechargeInterval: 0.5f,
            mustKeyPress: false,
            stockToConsume: 1,
            requiredStock: 1,
            useAttackSpeedScaling: true
        );
        public override bool useSteppedDef { get; set; } = true;


        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[1] { ChillRework.ChillRework.chillKeywordToken };
            FixSnowballProjectile();
            CreateLang();
            CreateSkill();
        }

        private void FixSnowballProjectile()
        {
           snowballProjectilePrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIceBolt");

            ProjectileSimple ps = snowballProjectilePrefab.GetComponent<ProjectileSimple>();
            if (ps)
            {
                ps.desiredForwardSpeed = 80f;
            }
            ProjectileDamage pd = snowballProjectilePrefab.GetComponent<ProjectileDamage>();
            if (pd)
            {
                pd.damageType = DamageTypeCombo.GenericPrimary;
            }
            ProjectileController pc = snowballProjectilePrefab.GetComponent<ProjectileController>();
            if (pc)
            {
                pc.procCoefficient = 0.75f;
            }

            ModdedDamageTypeHolderComponent mdthc = snowballProjectilePrefab.AddComponent<ModdedDamageTypeHolderComponent>();
            if (mdthc)
            {
                mdthc.Add(ChillRework.ChillRework.ChillOnHit);
            }
        }
    }
}
