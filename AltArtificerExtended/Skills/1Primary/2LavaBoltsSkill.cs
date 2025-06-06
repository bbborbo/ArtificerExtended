using ArtificerExtended.Modules;
using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using EntityStates;
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
        public static GameObject lavaProjectilePrefab => CommonAssets.lavaProjectilePrefab;
        public static GameObject lavaGhostPrefab;
        public static GameObject lavaImpactEffect;

        public static int maxStock = 6;
        public static int rechargeStock = 2;
        public static float rechargeInterval = 2f;
        public static float baseDuration = 0.2f;

        public static float impactDamageCoefficient = 1.8f;
        public static float impactProcCoefficient = 1.0f;
        public override string SkillName => "Lava Bolts";

        public override string SkillDescription => $"<style=cIsDamage>Ignite</style>. Lob molten projectiles for " +
            $"<style=cIsDamage>{Tools.ConvertDecimal(impactDamageCoefficient)} damage</style>, leaving " +
            $"<style=cIsDamage>molten pools</style> on impact. " +
            $"Hold up to {maxStock}.";

        public override string TOKEN_IDENTIFIER => "LAVABOLTS";

        public override Type RequiredUnlock => typeof(StackBurnUnlock);

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(FireLavaBolt);

        public override SimpleSkillData SkillData => new SimpleSkillData 
        { 
            baseMaxStock = maxStock,
            rechargeStock = rechargeStock,
            useAttackSpeedScaling = true
        };

        public override Sprite Icon => LoadSpriteFromBundle("LavaBoltIcon");
        public override SkillSlot SkillSlot => SkillSlot.Primary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Any;
        public override Type BaseSkillDef => typeof(SteppedSkillDef);
        public override float BaseCooldown => rechargeInterval;
        public override void Init()
        {
            KeywordTokens = new string[] { CommonAssets.lavaPoolKeywordToken, "KEYWORD_IGNITE" };
            //CreateLavaProjectile();
            base.Init();
        }

        public override void Hooks()
        {

        }

        private void CreateLavaProjectile()
        {
            //lavaProjectilePrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/beetlequeenspit").InstantiateClone("LavaProjectile", true);

            Color napalmColor = new Color32(255, 40, 0, 255);

            GameObject ghostPrefab = lavaProjectilePrefab.GetComponent<ProjectileController>().ghostPrefab;
            lavaGhostPrefab = ghostPrefab.InstantiateClone("NapalmSpitGhost", false);
            Tools.GetParticle(lavaGhostPrefab, "SpitCore", napalmColor);

            lavaImpactEffect = RoR2.LegacyResourcesAPI.Load<GameObject>("e184c0c8bc862ff40b9fd07db0b8e98c").InstantiateClone("NapalmSpitExplosion", false); //beetlespitexplosion
            Tools.GetParticle(lavaImpactEffect, "Bugs", Color.clear);
            Tools.GetParticle(lavaImpactEffect, "Flames", napalmColor);
            Tools.GetParticle(lavaImpactEffect, "Flash", Color.yellow);
            Tools.GetParticle(lavaImpactEffect, "Distortion", napalmColor);
            Tools.GetParticle(lavaImpactEffect, "Ring, Mesh", Color.yellow);

            ProjectileImpactExplosion pieNapalm = lavaProjectilePrefab.GetComponent<ProjectileImpactExplosion>();
            if (pieNapalm && CommonAssets.lavaPoolPrefab != null)
            {
                pieNapalm.childrenProjectilePrefab = CommonAssets.lavaPoolPrefab;
                pieNapalm.impactEffect = lavaImpactEffect;
                pieNapalm.blastRadius = CommonAssets.lavaPoolSize;
                //projectilePrefabNapalm.GetComponent<ProjectileImpactExplosion>().destroyOnEnemy = true;
                pieNapalm.blastProcCoefficient = impactProcCoefficient;
                pieNapalm.bonusBlastForce = new Vector3(0, 500, 0);
            }

            ProjectileController pc = lavaProjectilePrefab.GetComponent<ProjectileController>();
            pc.ghostPrefab = lavaGhostPrefab;

            ProjectileDamage pd = lavaProjectilePrefab.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.IgniteOnHit;

            Content.CreateAndAddEffectDef(lavaImpactEffect);
            Content.AddProjectilePrefab(lavaProjectilePrefab);
        }
    }
}
