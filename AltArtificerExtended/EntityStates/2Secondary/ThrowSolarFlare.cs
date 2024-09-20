using ArtificerExtended.Skills;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.EntityState
{
    class ThrowSolarFlare : BaseThrowBombState
    {
        GameObject solarFlareProjectilePrefab => _4SolarFlareSkill.projectilePrefab;
        public override void OnEnter()
        {
            this.projectilePrefab = solarFlareProjectilePrefab;
            this.minDamageCoefficient = _4SolarFlareSkill.blastDamage;
            this.maxDamageCoefficient = _4SolarFlareSkill.blastDamage;
            base.OnEnter();
        }

        public override void ModifyProjectile(ref FireProjectileInfo projectileInfo)
        {
            projectileInfo.speedOverride = Util.Remap(this.charge, 
                (_4SolarFlareSkill.minChargeDuration / _4SolarFlareSkill.maxChargeDuration), 1f, 
                _4SolarFlareSkill.minSendSpeed, _4SolarFlareSkill.maxSendSpeed);
            projectileInfo.useSpeedOverride = true;
        }
    }
}
