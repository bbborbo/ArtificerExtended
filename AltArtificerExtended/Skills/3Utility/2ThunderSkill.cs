using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using AltArtificerExtended.Unlocks;
using UnityEngine;
using AltArtificerExtended.EntityState;
using RoR2.Projectile;
using ThreeEyedGames;
using R2API;
using R2API.Utils;

namespace AltArtificerExtended.Skills
{
    class _2ThunderSkill : SkillBase
    {
        //thunder
        public static GameObject projectilePrefabThunder;
        public static float desiredForwardSpeedMax = 22.5f;
        public static float thunderBlastRadius = 8;
        int maxCharges = 2;
        public override string SkillName => "Rolling Thunder";

        public override string SkillDescription => $"<style=cIsDamage>Stunning.</style> Summon a rain of<style=cIsDamage> explosive</style> lightning bolts " +
            $"for <style=cIsDamage>{CastThunder.meatballCount}x{Tools.ConvertDecimal(CastThunder.damagePerMeatball)} damage</style>. " +
            $"Can hold up to {maxCharges} charges.";

        public override string SkillLangTokenName => "THUNDERMEATBALLS";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerThunderUnlock));

        public override string IconName => "thundericon";

        public override MageElement Element => MageElement.Lightning;

        public override Type ActivationState => typeof(CastThunder);

        public override SkillFamily SkillSlot => Main.mageUtility;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseMaxStock: maxCharges,
                baseRechargeInterval: Main.artiUtilCooldown / 2,
                interruptPriority: InterruptPriority.Skill,
                canceledFromSprinting: true

                /*Thunder.baseMaxStock = 2;
                Thunder.baseRechargeInterval = 5f;
                Thunder.beginSkillCooldownOnSkillEnd = snapfreeze.beginSkillCooldownOnSkillEnd;
                Thunder.canceledFromSprinting = snapfreeze.canceledFromSprinting;
                Thunder.fullRestockOnAssign = snapfreeze.fullRestockOnAssign;
                Thunder.interruptPriority = snapfreeze.interruptPriority;
                Thunder.isBullets = snapfreeze.isBullets;
                Thunder.isCombatSkill = snapfreeze.isCombatSkill;
                Thunder.mustKeyPress = snapfreeze.mustKeyPress;
                Thunder.noSprint = snapfreeze.noSprint;
                Thunder.rechargeStock = 1;
                Thunder.requiredStock = 1;
                Thunder.shootDelay = snapfreeze.shootDelay;
                Thunder.stockToConsume = 1;
                Thunder.keywordTokens = new string[1];
                Thunder.keywordTokens[0] = "KEYWORD_STUNNING";*/
            );


        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[1] { "KEYWORD_STUNNING" };

            RegisterProjectileThunder();
            CreateLang();
            CreateSkill();
        }
        private void RegisterProjectileThunder()
        {
            projectilePrefabThunder = Resources.Load<GameObject>("prefabs/projectiles/ElectricOrbProjectile").InstantiateClone("ThunderProjectile", true);

            var pc = projectilePrefabThunder.GetComponent<ProjectileController>();
            var pd = projectilePrefabThunder.GetComponent<ProjectileDamage>();
            var ps = projectilePrefabThunder.GetComponent<ProjectileSimple>();
            var pie = projectilePrefabThunder.GetComponent<ProjectileImpactExplosion>();

            float scale = 0.2f;
            projectilePrefabThunder.transform.localScale = new Vector3(scale, scale, scale);
            pd.damageType = DamageType.Stun1s;
            ps.desiredForwardSpeed = desiredForwardSpeedMax;
            ps.lifetime = 10;
            pie.childrenCount = 0;
            pie.bonusBlastForce = Vector3.zero;
            pie.blastRadius = thunderBlastRadius;
            pie.falloffModel = BlastAttack.FalloffModel.None;
            pie.blastProcCoefficient = 1f;
            pc.procCoefficient = 0.6f;

            ContentPacks.projectilePrefabs.Add(projectilePrefabThunder);
        }
    }
}
