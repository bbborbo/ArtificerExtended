using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using AltArtificerExtended.Unlocks;
using UnityEngine;
using AltArtificerExtended.EntityState;
using RoR2.Projectile;
using R2API;
using R2API.Utils;

namespace AltArtificerExtended.Skills
{
    class _2FireSkill2Skill : SkillBase
    {
        //fireskill2
        public static GameObject outerFireball;
        public static GameObject innerFireball;

        public override string SkillName => "Flame Burst";

        public override string SkillDescription => $"Ignite. Charge a spread of 3 fireballs for " +
            $"<style=cIsDamage>{Tools.ConvertDecimal(ChargeFireBlast.minDamageCoefficient)}-{Tools.ConvertDecimal(ChargeFireBlast.maxDamageCoefficient)} " +
            $"damage each</style> that converge on a point in front of you.";

        public override string SkillLangTokenName => "FIREBALLS";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerFlameBurstUnlock));

        public override string IconName => "Fireskill2icon";

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(ChargeFireBlast);

        public override SkillFamily SkillSlot => Main.magePrimary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 2,
                beginSkillCooldownOnSkillEnd: true
            );

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {

            ChargeFireBlast.minDamageCoefficient = config.Bind<float>(
                "Skills Config: " + SkillName, "Minimum Damage Coefficient",
                ChargeFireBlast.minDamageCoefficient,
                "Determines the minimum damage of Fire Blast."
                ).Value;
            ChargeFireBlast.maxDamageCoefficient = config.Bind<float>(
                "Skills Config: " + SkillName, "Max Damage Coefficient",
                ChargeFireBlast.maxDamageCoefficient,
                "Determines the max damage of Fire Blast. "
                ).Value;


            KeywordTokens = new string[1] { "KEYWORD_IGNITE" };

            CreateProjectiles();
            CreateLang();
            CreateSkill();
        }

        private void CreateProjectiles()
        {
            float blastRadius = 4;
            float scale = 0.5f;

            outerFireball = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageFireBombProjectile").InstantiateClone("mageFireballOuter", true);
            outerFireball.transform.localScale = Vector3.one * scale;
            var ps1 = outerFireball.GetComponent<ProjectileSimple>();
            ps1.desiredForwardSpeed = 80;
            ps1.lifetime = 0.3f;
            ps1.updateAfterFiring = true;
            ps1.oscillate = true;
            ps1.oscillateSpeed = 25f;
            ps1.oscillateMagnitude = 60f;
            var pie1 = outerFireball.GetComponent<ProjectileImpactExplosion>();
            pie1.blastRadius = blastRadius;
            pie1.falloffModel = BlastAttack.FalloffModel.None;
            pie1.bonusBlastForce = Vector3.zero;
            pie1.lifetime = 0.275f;

            innerFireball = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageFireBombProjectile").InstantiateClone("mageFireballInner", true);
            innerFireball.transform.localScale = Vector3.one * scale;
            var ps2 = innerFireball.GetComponent<ProjectileSimple>();
            ps2.desiredForwardSpeed = ps1.desiredForwardSpeed;
            ps2.lifetime = ps1.lifetime + 0.05f;
            ps2.updateAfterFiring = ps1.updateAfterFiring;
            var pie2 = innerFireball.GetComponent<ProjectileImpactExplosion>();
            pie2.blastRadius = blastRadius;
            pie2.falloffModel = BlastAttack.FalloffModel.None;
            pie2.bonusBlastForce = Vector3.zero;
            pie2.lifetime = pie1.lifetime + 0.05f;

            ContentPacks.projectilePrefabs.Add(outerFireball);
            ContentPacks.projectilePrefabs.Add(innerFireball);
        }
    }
}
