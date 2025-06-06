using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using EntityStates;
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
        public static float snowballBaseDuration = 0.7f;
        public static GameObject snowballProjectilePrefab;
        public override string SkillName => "Cryo Bolt";
        public override string TOKEN_IDENTIFIER => "SNOWBALL";

        public override string SkillDescription => $"<style=cIsUtility>Frost</style>. " +
            $"Fire a bolt for <style=cIsDamage>{Tools.ConvertDecimal(FireSnowBall.damageCoeff)} damage</style>.";

        public override Sprite Icon => LoadSpriteFromBundle("frostbolt");

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(FireSnowBall);

        public override SkillSlot SkillSlot => SkillSlot.Primary;

        public override SimpleSkillData SkillData => new SimpleSkillData
        (
            mustKeyPress: false,
            //stockToConsume: 1,
            requiredStock: 1,
            useAttackSpeedScaling: true
        );
        public override InterruptPriority InterruptPriority => InterruptPriority.Any;

        public override Type BaseSkillDef => typeof(SteppedSkillDef);

        public override float BaseCooldown => 0.5f;
        public override Type RequiredUnlock => typeof(FreezeManySimultaneousUnlock);

        public override void Init()
        {
            KeywordTokens = new string[1] { "KEYWORD_FROST" };
            FixSnowballProjectile();
            base.Init();
        }
        public override void Hooks()
        {
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
            ProjectileController pc = snowballProjectilePrefab.GetComponent<ProjectileController>();
            if (pc)
            {
                pc.procCoefficient = 0.75f;
            }
        }
    }
}
