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
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;
using RoR2BepInExPack.GameAssetPaths;
using UnityEngine.ResourceManagement.AsyncOperations;
using RoR2.ContentManagement;
using RainrotSharedUtils.Components;
using ArtificerExtended.Modules;

namespace ArtificerExtended.Skills
{
    class _1SnowballsSkill : SkillBase
    {
        const string CryoBoltHitBoxGroupName = "CryoBoltPierce";
        [AutoConfig("Base Attack Duration", 0.8f)]
        public static float snowballBaseDuration = 0.8f;
        [AutoConfig("Pierce Hit Limit", 3)]
        public static int snowballPierceLimit = 3;
        [AutoConfig("Pierce Hitbox Size", 3f)]
        public static float snowballHitBoxSize = 3f;
        [AutoConfig("Pierce Damage Decay (First Hit)", 0.5f)]
        public static float snowballPierceDamageDecayFirstHit = 0.5f;
        [AutoConfig("Pierce Damage Decay (Later Hits)", "1.0 means pierce damage will not continue to decay after the first hit.", 1.0f)]
        public static float snowballPierceDamageDecay = 1.0f;
        public static GameObject snowballProjectilePrefab;
        public override string SkillName => "Cryo Bolt";
        public override string TOKEN_IDENTIFIER => "SNOWBALL";

        public override string SkillDescription => $"<style=cIsUtility>Frost</style>. " +
            $"Fire a bolt for <style=cIsDamage>{Tools.ConvertDecimal(FireSnowBall.damageCoeff)} damage</style> " +
            $"that pierces up to <style=cIsUtility>{snowballPierceLimit}</style> times.";

        public override Sprite Icon => LoadSpriteFromBundle("frostbolt");

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(FireSnowBall);

        public override SkillSlot SkillSlot => SkillSlot.Primary;

        public override SimpleSkillData SkillData => new SimpleSkillData
        (
            stockToConsume: 0,
            //requiredStock: 1,
            //useAttackSpeedScaling: true,
            mustKeyPress: false
        );
        public override InterruptPriority InterruptPriority => InterruptPriority.Any;

        public override Type BaseSkillDef => typeof(SteppedSkillDef);

        public override float BaseCooldown => 0.5f;
        public override Type RequiredUnlock => typeof(FreezeManySimultaneousUnlock);

        public override void Init()
        {
            KeywordTokens = new string[1] { "KEYWORD_FROST" };
            //FixSnowballProjectile();
            base.Init();
        }

        public override void Hooks()
        {
            AssetReferenceT<GameObject> refSnowballProjectile = new AssetReferenceT<GameObject>(RoR2_Junk_Mage.MageIceBolt_prefab);
            AssetAsyncReferenceManager<GameObject>.LoadAsset(refSnowballProjectile).Completed += LoadSnowballProjectile;
            //Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Junk_Mage.MageIceBolt_prefab).Completed += LoadSnowballProjectile;
        }

        private void LoadSnowballProjectile(AsyncOperationHandle<GameObject> obj)
        {
            snowballProjectilePrefab = obj.Result;

            FixSnowballProjectile();
        }

        private void FixSnowballProjectile()
        {
            //snowballProjectilePrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIceBolt");

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


            BoxCollider boxCollider = snowballProjectilePrefab.GetComponent<BoxCollider>();
            if (boxCollider != null)
            {
                snowballProjectilePrefab.transform.localScale = Vector3.one;
                boxCollider.size = Vector3.one * 0.2f;
                //boxCollider.isTrigger = true;
            }

            //get child transform and fix to pierce instead of prox. detonate
            HitBox hitBox = null;
            Transform childTransform = null;
            MineProximityDetonator proximityDetonator = snowballProjectilePrefab.GetComponentInChildren<MineProximityDetonator>();
            if (proximityDetonator)
            {
                childTransform = proximityDetonator.transform;
                childTransform.localScale = Vector3.one * snowballHitBoxSize;
                childTransform.localPosition = Vector3.zero;
                childTransform.gameObject.layer = LayerIndex.projectile.intVal;

                hitBox = childTransform.gameObject.AddComponent<HitBox>();

                #region destroy old components
                SphereCollider sphereCollider = proximityDetonator.GetComponent<SphereCollider>();
                if (sphereCollider)
                {
                    UnityEngine.Object.Destroy(sphereCollider);
                }
                UnityEngine.Object.Destroy(proximityDetonator);
                #endregion
            }

            if (hitBox != null)
            {
                HitBoxGroup hitBoxGroup = snowballProjectilePrefab.AddComponent<HitBoxGroup>();
                hitBoxGroup.groupName = CryoBoltHitBoxGroupName;
                hitBoxGroup.hitBoxes = new HitBox[1] { hitBox };

                ProjectileOverlapAttack projectileOverlap = snowballProjectilePrefab.AddComponent<ProjectileOverlapAttack>();
                projectileOverlap.damageCoefficient = 1;
                projectileOverlap.maximumOverlapTargets = 100;
                projectileOverlap.fireFrequency = 60;

                AssetReferenceT<GameObject> ref1 = new AssetReferenceT<GameObject>(RoR2_Junk_Mage.MuzzleflashMageIce_prefab);
                AssetAsyncReferenceManager<GameObject>.LoadAsset(ref1).Completed += (obj) => projectileOverlap.impactEffect = obj.Result;
                //projectileOverlap.impactEffect = ;

                ProjectileOverlapDecayDamage overlapDecayDamage = snowballProjectilePrefab.AddComponent<ProjectileOverlapDecayDamage>();
                overlapDecayDamage.firstHitDamageMultiplier = snowballPierceDamageDecayFirstHit;
                overlapDecayDamage.hitLimit = snowballPierceLimit;

                if(boxCollider != null)
                {
                    boxCollider.gameObject.layer = LayerIndex.projectileWorldOnly.intVal;
                    childTransform.gameObject.layer = LayerIndex.projectile.intVal;

                    ProjectileSingleTargetImpact psti = snowballProjectilePrefab.AddComponent<ProjectileSingleTargetImpact>();
                    psti.destroyOnWorld = true;
                    psti.destroyWhenNotAlive = true;

                    AssetReferenceT<GameObject> ref2 = new AssetReferenceT<GameObject>(RoR2_Junk_Mage.MuzzleflashMageIce_prefab);
                    AssetAsyncReferenceManager<GameObject>.LoadAsset(ref2).Completed += (obj) => psti.impactEffect = obj.Result;
                    //psti.impactEffect = ;
                }

                #region destroy old components
                ProjectileImpactExplosion pie = snowballProjectilePrefab.GetComponent<ProjectileImpactExplosion>();
                if (pie)
                {
                    UnityEngine.Object.Destroy(pie);
                }
                #endregion
            }
        }
    }
}
