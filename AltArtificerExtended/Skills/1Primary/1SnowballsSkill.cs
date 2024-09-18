using ArtificerExtended.EntityState;
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
            $"Fire a snowball for <style=cIsDamage>{Tools.ConvertDecimal(FireSnowBall.damageCoeff)} damage</style>. " +
            $"Has no cooldown.";

        public override string SkillLangTokenName => "SNOWBALL";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerSnowballUnlock));

        public override string IconName => "SnowballIcon";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(FireSnowBall);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.magePrimary;

        public override SimpleSkillData SkillData => new SimpleSkillData
        (
            baseRechargeInterval: 1.2f,
            mustKeyPress: false,
            stockToConsume: 1,
            requiredStock: 1
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

            snowballProjectilePrefab.GetComponent<ProjectileSimple>().desiredForwardSpeed = 80f;
            snowballProjectilePrefab.GetComponent<ProjectileDamage>().damageType = DamageType.Generic;
            snowballProjectilePrefab.GetComponent<ProjectileController>().procCoefficient = 0.75f;
            snowballProjectilePrefab.AddComponent<ModdedDamageTypeHolderComponent>().Add(ChillRework.ChillRework.ChillOnHit);
        }
    }
}
