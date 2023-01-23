using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Treebot.Weapon;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.EntityState
{
    public class FireShockwaveVisuals : BaseState
    {

        public string sound;

        public string muzzle;

        public GameObject fireEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/TreebotShockwaveEffect");

        public float baseDuration = 0.7f;

        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration;
            base.PlayAnimation("Gesture, Additive", "FireSonicBoom");
            Util.PlaySound(this.sound, base.gameObject);

            var aimRay = base.GetAimRay();

            if (base.isAuthority)
            {
                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                fireProjectileInfo.crit = base.RollCrit();
                fireProjectileInfo.damage = 1 * this.damageStat;
                fireProjectileInfo.damageTypeOverride = DamageType.Stun1s;
                fireProjectileInfo.owner = base.gameObject;
                fireProjectileInfo.force = 0;
                fireProjectileInfo.projectilePrefab = _2ShockwaveSkill.shockwaveZapConePrefab;
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }/*
            if (base.isAuthority)
            {
                FireZapCone(aimRay, 0, 0, 10, aimRay.direction, aimRay.origin);
            }*/
        }

        private void FireZapCone(Ray aimRay, float bonusPitch, float bonusYaw, float projectileDamageCoeff, Vector3 direction, Vector3 origin)
        {
            Vector3 forward = Util.ApplySpread(direction, 0, 0, 1f, 1f, bonusYaw, bonusPitch - 6f);
            ProjectileManager.instance.FireProjectile(_2ShockwaveSkill.shockwaveZapConePrefab, origin,
                Util.QuaternionSafeLookRotation(forward), base.gameObject, this.damageStat * projectileDamageCoeff,
                0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
