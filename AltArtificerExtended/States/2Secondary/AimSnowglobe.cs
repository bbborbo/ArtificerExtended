using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtificerExtended.States
{
    class AimSnowglobe : AimThrowableBase
    {
        public override void OnEnter()
        {
            this.damageCoefficient = _3SnowglobeSkill.impactDamageCoefficient;
            this.projectileBaseSpeed = _3SnowglobeSkill.projectileBaseSpeed;
            this.detonationRadius = _3SnowglobeSkill.snowWardRadius;
            this.projectilePrefab = _3SnowglobeSkill.snowglobeDeployProjectilePrefab;
            this.baseMinimumDuration = 0.4f;
            this.maxDistance = 100;
            this.setFuse = true;
            this.arcVisualizerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/BasicThrowableVisualizer.prefab").WaitForCompletion();
            this.endpointVisualizerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Treebot/TreebotMortarAreaIndicator.prefab").WaitForCompletion();
            base.OnEnter();
            this.PlayAnimation("Gesture, Additive", PrepWall.PrepWallStateHash, PrepWall.PrepWallParamHash, this.minimumDuration);
        }
        public override void ModifyProjectile(ref FireProjectileInfo fireProjectileInfo)
        {
            base.ModifyProjectile(ref fireProjectileInfo);
        }
        public override void OnExit()
        {
            this.PlayAnimation("Gesture, Additive", BaseThrowBombState.FireNovaBombStateHash, BaseThrowBombState.FireNovaBombParamHash, this.minimumDuration);
            EffectManager.SimpleMuzzleFlash(_3SnowglobeSkill.muzzleflashEffectPrefab, base.gameObject, "MuzzleLeft", false);
            EffectManager.SimpleMuzzleFlash(_3SnowglobeSkill.muzzleflashEffectPrefab, base.gameObject, "MuzzleRight", false);
            //this.PlayAnimation("Gesture, Additive", PrepWall.FireWallStateHash);
            base.OnExit();
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
