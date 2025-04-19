using ArtificerExtended.Components;
using ArtificerExtended.Modules;
using ArtificerExtended.Passive;
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
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;

namespace ArtificerExtended.Skills
{
    class _3SnowglobeSkill : SkillBase
    {
        public DeployableAPI.GetDeployableSameSlotLimit GetSnowglobeSlotLimit;
        public static DeployableSlot snowglobeDeployableSlot;

        public static GameObject snowglobeDeployProjectilePrefab;
        public static GameObject snowglobeProjectilePrefab;

        public static int maxSnowglobeBase = 2;
        public static int maxSnowglobeUpgrade = 3;
        public static float impactDamageCoefficient = 10f;
        public static float impactProcCoefficient = 1f;
        public static float impactBlastRadius = 14f;
        public static float snowWardRadius = 14f;
        public static float damageCoefficientPerSecond = 0.5f;
        public static float procCoefficientPerTick = 1f;
        public static float damageTicksPerSecond = 0.5f;
        public static int chillStacksPerSnowglobe = 3;
        public static float projectileBaseSpeed = 120f;

        public override string SkillName => "Stasis Field";

        public override string SkillDescription => $"<style=cIsUtility>Resonant</style>. " +
            $"Aim a supercooled projectile for <style=cIsDamage>{Tools.ConvertDecimal(impactDamageCoefficient)}</style> damage, " +
            $"leaving behind a <style=cIsUtility>Chilling</style> stasis field that lasts until replaced. " +
            $"Hold up to {maxSnowglobeBase}.";

        public override string TOKEN_IDENTIFIER => "SNOWGLOBE";

        public override Type RequiredUnlock => (typeof(TankDamageUnlock));

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(AimSnowglobe);

        public override SimpleSkillData SkillData => new SimpleSkillData
        (
            baseMaxStock: maxSnowglobeBase,
            beginSkillCooldownOnSkillEnd: true
        );
        public override Sprite Icon => null;// LoadSpriteFromBundle("meteoricon");
        public override SkillSlot SkillSlot => SkillSlot.Secondary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Any;
        public override Type BaseSkillDef => typeof(SkillDef);
        public override float BaseCooldown => 5;
        public override void Init()
        {
            string resonantKeywordToken = ArtificerExtendedPlugin.DEVELOPER_PREFIX + "KEYWORD_RESONANTSNOWGLOBE";
            CommonAssets.AddResonantKeyword(resonantKeywordToken, "Sustained Stasis",
                $"Max of {maxSnowglobeBase} stasis fields. " +
                $"If only <style=cIsDamage>Ice</style> abilities are equipped, <style=cIsUtility>increase max to {maxSnowglobeUpgrade}</style>.");
            KeywordTokens = new string[] { resonantKeywordToken, ChillRework.ChillRework.chillKeywordToken };
            GetSnowglobeSlotLimit += GetMaxSnowglobes;
            snowglobeDeployableSlot = DeployableAPI.RegisterDeployableSlot(GetSnowglobeSlotLimit);
            CreateSnowglobeProjectile();
            CreateBombProjectile();
            base.Init();
        }

        public override void Hooks()
        {

        }

        private void CreateSnowglobeProjectile()
        {
            snowglobeProjectilePrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerMineAltDetonated.prefab").WaitForCompletion(), "HeatWardPrefab", true);
            if (snowglobeProjectilePrefab)
            {
                Content.AddProjectilePrefab(snowglobeProjectilePrefab);
                snowglobeProjectilePrefab.transform.rotation = Quaternion.identity;

                Deployable deployableComponent = snowglobeProjectilePrefab.AddComponent<Deployable>();
                GenericOwnership genericOwnership = snowglobeProjectilePrefab.AddComponent<GenericOwnership>();

                ProjectileController projectileController = snowglobeProjectilePrefab.GetComponent<ProjectileController>();
                if (projectileController != null)
                {
                    projectileController.cannotBeDeleted = true;
                }

                SlowDownProjectiles sdp = snowglobeProjectilePrefab.GetComponent<SlowDownProjectiles>();
                if (sdp)
                    GameObject.Destroy(sdp);
                SphereCollider collider = snowglobeProjectilePrefab.GetComponent<SphereCollider>();
                if (collider)
                    GameObject.Destroy(collider);
                GameObject areaIndicator = snowglobeProjectilePrefab.transform.Find("AreaIndicator").gameObject;
                if (areaIndicator)
                {
                    //GameObject.Destroy(areaIndicator);
                }

                //encourageWardIndicator.transform.parent = HeatWardPrefab.transform;
                //encourageWardIndicator.transform.localScale = new Vector3(heatWardRadius, encourageWardIndicator.transform.localScale.y, heatWardRadius);
                //encourageWardIndicator.transform.rotation = Quaternion.identity;
                BuffWard buffWard = snowglobeProjectilePrefab.GetComponent<BuffWard>();
                if (buffWard)
                {
                    //buffWard.rangeIndicator = verticalWard ? encourageWardIndicator.transform : buffWard.rangeIndicator;
                    buffWard.radius = snowWardRadius;
                    buffWard.buffDef = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Common/bdSlow80.asset").WaitForCompletion();//HeatWardBuff;
                    buffWard.buffDuration = damageTicksPerSecond * chillStacksPerSnowglobe;
                    buffWard.interval = damageTicksPerSecond;
                    buffWard.expireDuration = 9999;
                    buffWard.shape = BuffWard.BuffWardShape.Sphere;
                    buffWard.invertTeamFilter = true;
                    buffWard.requireGrounded = false;
                }
            }
        }

        private int GetMaxSnowglobes(CharacterMaster self, int deployableCountMultiplier)
        {
            GameObject body = self.GetBodyObject();
            if (body)
            {
                if ((int)ElementCounter.GetPowerLevelFromBody(body, MageElement.Ice) >= 4)
                    return maxSnowglobeUpgrade;
            }
            return maxSnowglobeBase /*+ Mathf.CeilToInt(self.inventory.GetItemCount(RoR2Content.Items.SecondarySkillMagazine) * maxSnowglobeUpgrade)*/;
        }

        private static void CreateBombProjectile()
        {
            //highly recommend setting up projectiles in editor, but this is a quick and dirty way to prototype if you want
            snowglobeDeployProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/CryoCanisterProjectile.prefab").WaitForCompletion();

            //remove their ProjectileImpactExplosion component and start from default values
            //UnityEngine.Object.Destroy(snowglobeDeployProjectilePrefab.GetComponent<ProjectileImpactExplosion>());
            ProjectileImpactExplosion pie = snowglobeDeployProjectilePrefab.GetComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.blastRadius = impactBlastRadius;
                pie.blastProcCoefficient = impactProcCoefficient;
                pie.childrenCount = 0;
                pie.falloffModel = BlastAttack.FalloffModel.SweetSpot;
            }
            ModdedDamageTypeHolderComponent mdthc = snowglobeDeployProjectilePrefab.AddComponent<ModdedDamageTypeHolderComponent>();
            if (mdthc)
            {
                mdthc.Add(ChillRework.ChillRework.ChillOnHit);
            }

            ProjectileController bombController = snowglobeDeployProjectilePrefab.GetComponent<ProjectileController>();

            SnowglobeDeployProjectile bombImpactExplosion = snowglobeDeployProjectilePrefab.AddComponent<SnowglobeDeployProjectile>();
            bombImpactExplosion.snowglobeProjectilePrefab = snowglobeProjectilePrefab;
            bombImpactExplosion.pc = bombController;

            GameObject ghostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoSpitGhost.prefab").WaitForCompletion();
            if (ghostPrefab/*_assetBundle.LoadAsset<GameObject>("HenryBombGhost")*/ != null)
                bombController.ghostPrefab = ghostPrefab;//Assets.CreateProjectileGhostPrefab("HenryBombGhost");

            bombController.startSound = "";
            Content.AddProjectilePrefab(snowglobeDeployProjectilePrefab);
        }
    }
}
