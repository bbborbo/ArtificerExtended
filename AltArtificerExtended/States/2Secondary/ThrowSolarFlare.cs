﻿using ArtificerExtended.Skills;
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
    class ThrowSolarFlare : BaseThrowBombState
    {
        GameObject solarFlareProjectilePrefab => _4SolarFlareSkill.projectilePrefab;
        public override void OnEnter()
        {
            this.muzzleflashEffectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Junk_Mage.MuzzleflashMageFireLarge_prefab).WaitForCompletion();
            this.projectilePrefab = solarFlareProjectilePrefab;
            this.minDamageCoefficient = _4SolarFlareSkill.blastDamage;
            this.maxDamageCoefficient = _4SolarFlareSkill.blastDamage;
            this.baseDuration = 1f;
            base.OnEnter();
        }

        public override void ModifyProjectile(ref FireProjectileInfo projectileInfo)
        {
            projectileInfo.speedOverride = Util.Remap(this.charge, 
                (_4SolarFlareSkill.minChargeDuration / _4SolarFlareSkill.maxChargeDuration), 1f, 
                _4SolarFlareSkill.minSendSpeed, _4SolarFlareSkill.maxSendSpeed);
            projectileInfo.useSpeedOverride = true;
            projectileInfo.damageTypeOverride = new DamageTypeCombo?(new DamageTypeCombo(DamageType.IgniteOnHit, DamageTypeExtended.Generic, DamageSource.Secondary));
            base.ModifyProjectile(ref projectileInfo);
        }
    }
}
