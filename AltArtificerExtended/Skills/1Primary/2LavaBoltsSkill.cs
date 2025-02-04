using ArtificerExtended.CoreModules;
using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using ThreeEyedGames;
using UnityEngine;

namespace ArtificerExtended.Skills
{
    class _2LavaBoltsSkill : SkillBase<_2LavaBoltsSkill>
    {
        public static GameObject lavaProjectilePrefab;
        public static GameObject lavaGhostPrefab;
        public static GameObject lavaImpactEffect;

        public static int maxStock = 6;
        public static int rechargeStock = 2;
        public static float rechargeInterval = 2f;
        public static float baseDuration = 0.2f;

        public static float impactDamageCoefficient = 1.2f;
        public static float impactProcCoefficient = 1.0f;
        public override string SkillName => "Lava Bolts";

        public override string SkillDescription => $"<style=cIsDamage>Ignite</style>. Lob molten projectiles for " +
            $"<style=cIsDamage>{Tools.ConvertDecimal(impactDamageCoefficient)} damage</style>, leaving " +
            $"<style=cIsDamage>molten pools</style> on impact. " +
            $"Hold up to {maxStock}.";

        public override string SkillLangTokenName => "LAVABOLTS";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(StackBurnUnlock));

        public override string IconName => "napalmicon";//"Fireskill2icon";//

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(FireLavaBolt);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.magePrimary;

        public override SimpleSkillData SkillData => new SimpleSkillData 
        { 
            baseMaxStock = maxStock,
            baseRechargeInterval = rechargeInterval,
            rechargeStock = rechargeStock,
            useAttackSpeedScaling = true
        };

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[2] { "KEYWORD_IGNITE", "ARTIFICEREXTENDED_KEYWORD_LAVAPOOLS" };
            CreateSkill();
            CreateLang();

            CreateLavaProjectile();
        }

        private void CreateLavaProjectile()
        {
            lavaProjectilePrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/beetlequeenspit").InstantiateClone("LavaProjectile", true);

            Color napalmColor = new Color32(255, 40, 0, 255);

            GameObject ghostPrefab = lavaProjectilePrefab.GetComponent<ProjectileController>().ghostPrefab;
            lavaGhostPrefab = ghostPrefab.InstantiateClone("NapalmSpitGhost", false);
            Tools.GetParticle(lavaGhostPrefab, "SpitCore", napalmColor);

            lavaImpactEffect = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/BeetleSpitExplosion").InstantiateClone("NapalmSpitExplosion", false);
            Tools.GetParticle(lavaImpactEffect, "Bugs", Color.clear);
            Tools.GetParticle(lavaImpactEffect, "Flames", napalmColor);
            Tools.GetParticle(lavaImpactEffect, "Flash", Color.yellow);
            Tools.GetParticle(lavaImpactEffect, "Distortion", napalmColor);
            Tools.GetParticle(lavaImpactEffect, "Ring, Mesh", Color.yellow);

            ProjectileImpactExplosion pieNapalm = lavaProjectilePrefab.GetComponent<ProjectileImpactExplosion>();
            if (pieNapalm && Projectiles.lavaPoolPrefab != null)
            {
                pieNapalm.childrenProjectilePrefab = Projectiles.lavaPoolPrefab;
                pieNapalm.impactEffect = lavaImpactEffect;
                pieNapalm.blastRadius = Projectiles.lavaPoolSize;
                //projectilePrefabNapalm.GetComponent<ProjectileImpactExplosion>().destroyOnEnemy = true;
                pieNapalm.blastProcCoefficient = impactProcCoefficient;
                pieNapalm.bonusBlastForce = new Vector3(0, 500, 0);
            }

            ProjectileController pc = lavaProjectilePrefab.GetComponent<ProjectileController>();
            pc.ghostPrefab = lavaGhostPrefab;

            ProjectileDamage pd = lavaProjectilePrefab.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.IgniteOnHit;

            Effects.CreateEffect(lavaImpactEffect);
            ContentPacks.projectilePrefabs.Add(lavaProjectilePrefab);
        }
    }
}
