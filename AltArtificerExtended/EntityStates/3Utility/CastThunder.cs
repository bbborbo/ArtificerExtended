using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ArtificerExtended.Passive;
using ArtificerExtended.Skills;
//using AlternativeArtificer.States.Main;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace ArtificerExtended.EntityState
{
    public class CastThunder : BaseSkillState
    {
        public static float baseDuration = 0.3f;
        float baseSpeed = 15f;

        public static GameObject areaIndicatorPrefab = ChargeMeteor.areaIndicatorPrefab;
        public static GameObject aoeEffect = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/omnieffect/OmniImpactVFXLightningMage");
        public static GameObject muzzleflashEffect = ChargeMeteor.muzzleflashEffect;

        public static float minMeteorRadius = _2ThunderSkill.thunderBlastRadius;
        public static float damagePerMeatball = 3f;

        public static int meatballCount = 3;
        public static float meatballForce = 250;
        public static GameObject meatballProjectile = _2ThunderSkill.projectilePrefabThunder;

        private float stopwatch;
        private float duration;

        private GameObject cachedCrosshairPrefab;


        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = CastThunder.baseDuration / this.attackSpeedStat;
            base.characterBody.SetAimTimer(this.duration + 2f);
            this.cachedCrosshairPrefab = base.characterBody._defaultCrosshairPrefab;
            base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", this.duration);

            if (ArtificerExtendedPlugin.AllowBrokenSFX.Value == true)
                Util.PlaySound(PrepWall.prepWallSoundString, base.gameObject);

            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Charge");
            base.OnEnter();
        }

        public override void Update()
        {
            base.Update();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            if (this.stopwatch >= this.duration && !IsKeyDownAuthority() && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            base.PlayAnimation("Gesture, Additive", "FireWall");
            EffectManager.SimpleMuzzleFlash(CastThunderOld.muzzleflashEffect, base.gameObject, "Muzzle", false);
            if (!this.outer.destroying && base.isAuthority)
            {
                GameObject obj = base.outer.gameObject;
                if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
                {
                    passive.SkillCast();
                }

                Ray aimRay = base.GetAimRay();

                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    projectilePrefab = _2ThunderSkill.magnetRollerProjectilePrefab,
                    position = aimRay.origin,
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                    owner = base.gameObject,
                    damage = this.characterBody.damage * damagePerMeatball,
                    force = meatballForce,
                    crit = Util.CheckRoll(this.characterBody.crit, this.characterBody.master),
                    damageColorIndex = DamageColorIndex.Default
                });
            }
            base.characterBody._defaultCrosshairPrefab = this.cachedCrosshairPrefab;
            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Cast");
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }
    }
}
