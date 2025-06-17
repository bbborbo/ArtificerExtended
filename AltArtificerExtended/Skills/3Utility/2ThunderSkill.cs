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
using R2API.Utils;
using UnityEngine.AddressableAssets;
using ArtificerExtended.Modules;

namespace ArtificerExtended.Skills
{
    class _2ThunderSkill : SkillBase
    {
        public static int totalStrikes = 8;
        public static float delayBetweenStrikes = 0.5f;
        public static float rollerVelocity = 12f;
        public static float thunderBlastRadius = 8f;

        //thunder
        public static GameObject magnetRollerProjectilePrefab;
        public static GameObject projectilePrefabThunder;
        public static float desiredForwardSpeedMax = 0;
        int maxCharges = 2;
        public override string SkillName => "Rolling Thunder";

        public override string SkillDescription => $"<style=cIsDamage>Stunning.</style> Roll a <style=cIsUtility>magnetic sphere</style> that " +
            $"periodically attracts lightning for " +
            $"<style=cIsDamage>{totalStrikes}x{Tools.ConvertDecimal(CastThunder.damagePerMeatball)} damage</style>.";

        public override string TOKEN_IDENTIFIER => "THUNDERMEATBALLS";

        public override Type RequiredUnlock => (typeof(UgornsMusicUnlock));

        public override MageElement Element => MageElement.Lightning;

        public override Type ActivationState => typeof(CastThunder);

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseMaxStock: 1,
                canceledFromSprinting: true
            );
        public override Sprite Icon => LoadSpriteFromBundle("thundericon");
        public override SkillSlot SkillSlot => SkillSlot.Utility;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;
        public override Type BaseSkillDef => typeof(SkillDef);
        public override float BaseCooldown => ArtificerExtendedPlugin.artiUtilCooldown;
        public override void Init()
        {
            KeywordTokens = new string[1] { "KEYWORD_STUNNING" };

            RegisterProjectileThunder();
            RegisterMagnetRoller();
            base.Init();
        }


        public override void Hooks()
        {
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

            Content.AddProjectilePrefab(magnetRollerProjectilePrefab);
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

            Content.AddProjectilePrefab(projectilePrefabThunder);
            Content.CreateAndAddEffectDef(impactEffect);
        }
    }
}
