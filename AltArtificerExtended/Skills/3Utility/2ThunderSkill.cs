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
using R2API.Utils;
using UnityEngine.AddressableAssets;

namespace ArtificerExtended.Skills
{
    class _2ThunderSkill : SkillBase
    {
        public static int totalStrikes = 8;
        public static float delayBetweenStrikes = 0.5f;
        public static float rollerVelocity = 11f;
        public static float thunderBlastRadius = 6f;

        //thunder
        public static GameObject magnetRollerProjectilePrefab;
        public static GameObject projectilePrefabThunder;
        public static float desiredForwardSpeedMax = 0;
        int maxCharges = 2;
        public override string SkillName => "Rolling Thunder";

        public override string SkillDescription => $"<style=cIsDamage>Stunning.</style> Roll a <style=cIsUtility>magnetic sphere</style> that " +
            $"periodically attracts lightning strikes for <style=cIsDamage>{Tools.ConvertDecimal(CastThunder.damagePerMeatball)} damage</style>.";

        public override string SkillLangTokenName => "THUNDERMEATBALLS";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerThunderUnlock));

        public override string IconName => "thundericon";

        public override MageElement Element => MageElement.Lightning;

        public override Type ActivationState => typeof(CastThunder);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.mageUtility;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseMaxStock: 1,
                baseRechargeInterval: ArtificerExtendedPlugin.artiUtilCooldown,
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
            RegisterMagnetRoller();
            CreateLang();
            CreateSkill();
        }

        private void RegisterMagnetRoller()
        {
            magnetRollerProjectilePrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIcewallWalkerProjectile").InstantiateClone("MagnetRollerProjectile", true);

            ProjectileCharacterController pcc = magnetRollerProjectilePrefab.GetComponent<ProjectileCharacterController>();
            pcc.lifetime = totalStrikes * delayBetweenStrikes + 0.1f;
            pcc.velocity = rollerVelocity;

            ProjectileMageFirewallWalkerController walkerController = magnetRollerProjectilePrefab.GetComponent<ProjectileMageFirewallWalkerController>();
            walkerController.dropInterval = delayBetweenStrikes;
            walkerController.firePillarPrefab = projectilePrefabThunder;
            walkerController.totalProjectiles = totalStrikes;

            ProjectileController pc = magnetRollerProjectilePrefab.GetComponent<ProjectileController>();
            pc.ghostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ElectricWorm/ElectricOrbGhost.prefab").WaitForCompletion();
        }

        private void RegisterProjectileThunder()
        {
            GameObject projectilePrefan = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ElectricWorm/ElectricOrbProjectile.prefab").WaitForCompletion();
            projectilePrefabThunder = projectilePrefan.InstantiateClone("ThunderProjectile", true);
            GameObject impact = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Lightning/LightningStrikeImpact.prefab").WaitForCompletion();
            GameObject impactEffect = impact.InstantiateClone("ThunderImpact", false);
            if(impactEffect != null)
            {
                ShakeEmitter shakeEmitter = impactEffect.GetComponent<ShakeEmitter>();
                if (shakeEmitter)
                {
                    shakeEmitter.radius = 15;
                    shakeEmitter.duration = 0.1f;
                }

                Transform pointLight = impactEffect.transform.Find("Point Light");
                if (pointLight) 
                {
                    Debug.LogWarning("Destroying thunder point light");
                    //GameObject.Destroy(pointLight.gameObject);
                }
                Transform flash = impactEffect.transform.Find("Flash");
                if (flash) 
                {
                    Debug.LogWarning("Destroying thunder flash");
                    GameObject.Destroy(flash.gameObject);
                }
                Transform lines = impactEffect.transform.Find("Flash Lines");
                if (lines) 
                {
                    Debug.LogWarning("Destroying thunder lines");
                    GameObject.Destroy(lines.gameObject);
                }
                Transform pp = impactEffect.transform.Find("PostProcess");
                if (pp) 
                {
                    Debug.LogWarning("Destroying thunder pp");
                    GameObject.Destroy(pp.gameObject);
                }
                Transform sphere = impactEffect.transform.Find("Sphere");
                if (sphere) 
                {
                    Debug.LogWarning("Destroying thunder point light");
                    sphere.localScale = Vector3.one * thunderBlastRadius / 4;
                }
            }

            var pc = projectilePrefabThunder.GetComponent<ProjectileController>();
            pc.procCoefficient = 1f;
            pc.ghostPrefab = null;

            var pd = projectilePrefabThunder.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.Stun1s;

            var ps = projectilePrefabThunder.GetComponent<ProjectileSimple>();
            ps.desiredForwardSpeed = desiredForwardSpeedMax;
            ps.lifetime = 10;

            var pie = projectilePrefabThunder.GetComponent<ProjectileImpactExplosion>();
            pie.childrenCount = 0;
            pie.bonusBlastForce = Vector3.zero;
            pie.blastRadius = thunderBlastRadius;
            pie.falloffModel = BlastAttack.FalloffModel.None;
            pie.blastProcCoefficient = 1f;
            pie.impactEffect = impactEffect;

            float scale = 0.2f;
            projectilePrefabThunder.transform.localScale = new Vector3(scale, scale, scale);

            ContentPacks.projectilePrefabs.Add(projectilePrefabThunder);
            EffectDef newEffect = new EffectDef(impactEffect);
            ContentPacks.effectDefs.Add(newEffect);
        }
    }
}
