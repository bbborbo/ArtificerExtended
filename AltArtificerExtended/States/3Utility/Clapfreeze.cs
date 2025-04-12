using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
//using AlternativeArtificer.States.Main;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.States
{
    public class Clapfreeze : BaseSkillState
    {
        public static GameObject areaIndicatorPrefab = PrepWall.areaIndicatorPrefab;
        public static GameObject projectilePrefab;
        public static GameObject muzzleflashEffect;
        public static GameObject goodCrosshairPrefab;
        public static GameObject badCrosshairPrefab;
        private GameObject areaIndicatorInstance;
        private GameObject cachedCrosshairPrefab;
        public static float damageCoefficient;

        public static float baseDuration = PrepWall.baseDuration;
        private float duration;
        public static float maxDistance;
        public static float maxSlopeAngle;
        private bool goodPlacement;

        public static string prepWallSoundString = "Play_mage_shift_start";
        public static string fireSoundString = "Play_mage_shift_stop";
        private float stopwatch;


        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = Clapfreeze.baseDuration / this.attackSpeedStat;
            base.characterBody.SetAimTimer(this.duration + 2f);
            this.cachedCrosshairPrefab = base.characterBody._defaultCrosshairPrefab;
            base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", this.duration);
            Util.PlaySound(Clapfreeze.prepWallSoundString, base.gameObject);
            this.areaIndicatorInstance = UnityEngine.Object.Instantiate<GameObject>(PrepWall.areaIndicatorPrefab);
            this.UpdateAreaIndicator();
            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Charge");
        }

        private void UpdateAreaIndicator()
        {
            this.goodPlacement = false;
            this.areaIndicatorInstance.SetActive(true);
            if (this.areaIndicatorInstance)
            {
                float num = PrepWall.maxDistance;
                float num2 = 0f;
                Ray aimRay = (!VRStuff.VRInstalled) ? base.GetAimRay() : VRStuff.GetVRHandAimRay(false);
                RaycastHit raycastHit;
                if (Physics.Raycast(CameraRigController.ModifyAimRayIfApplicable(aimRay, base.gameObject, out num2), 
                    out raycastHit, num + num2, LayerIndex.world.mask))
                {
                    this.areaIndicatorInstance.transform.position = raycastHit.point;
                    this.areaIndicatorInstance.transform.up = raycastHit.normal;
                    this.areaIndicatorInstance.transform.right = -aimRay.direction;
                    this.goodPlacement = (Vector3.Angle(Vector3.up, raycastHit.normal) < PrepWall.maxSlopeAngle);
                }
                base.characterBody._defaultCrosshairPrefab = (this.goodPlacement ? PrepWall.goodCrosshairPrefab : PrepWall.badCrosshairPrefab);
            }
            this.areaIndicatorInstance.SetActive(this.goodPlacement);
        }

        public override void Update()
        {
            base.Update();
            this.UpdateAreaIndicator();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            if (this.stopwatch >= this.duration && !base.inputBank.skill3.down && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            if (!this.outer.destroying)
            {
                if (this.goodPlacement)
                {
                    
                    /*GameObject obj = base.outer.gameObject;
                    if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
                    {
                        passive.SkillCast();
                    }*/

                    base.PlayAnimation("Gesture, Additive", "FireWall");
                    Util.PlaySound(PrepWall.fireSoundString, base.gameObject);
                    if (this.areaIndicatorInstance && base.isAuthority)
                    {
                        EffectManager.SimpleMuzzleFlash(PrepWall.muzzleflashEffect, base.gameObject, "MuzzleLeft", true);
                        EffectManager.SimpleMuzzleFlash(PrepWall.muzzleflashEffect, base.gameObject, "MuzzleRight", true);
                        Vector3 forward = this.areaIndicatorInstance.transform.forward;
                        forward.y = 0f;
                        forward.Normalize();
                        Vector3 vector = Vector3.Cross(Vector3.up, forward);
                        bool crit = Util.CheckRoll(this.critStat, base.characterBody.master);

                        ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                        {
                            projectilePrefab = PrepWall.projectilePrefab,
                            position = this.areaIndicatorInstance.transform.position + Vector3.up,
                            rotation = Util.QuaternionSafeLookRotation(vector),
                            owner = base.gameObject,
                            damage = this.damageStat * PrepWall.damageCoefficient,
                            force = 0f,
                            crit = crit,
                            damageColorIndex = DamageColorIndex.Default,
                            target = null
                        });
                        ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                        {
                            projectilePrefab = PrepWall.projectilePrefab,
                            position = this.areaIndicatorInstance.transform.position + Vector3.up,
                            rotation = Util.QuaternionSafeLookRotation(-vector),
                            owner = base.gameObject,
                            damage = this.damageStat * PrepWall.damageCoefficient,
                            force = 0f,
                            crit = crit,
                            damageColorIndex = DamageColorIndex.Default,
                            target = null
                        });
                    }
                }
                else
                {
                    base.skillLocator.utility.AddOneStock();
                    base.PlayCrossfade("Gesture, Additive", "BufferEmpty", 0.2f);
                }
            }
            global::EntityStates.EntityState.Destroy(this.areaIndicatorInstance.gameObject);
            base.characterBody._defaultCrosshairPrefab = this.cachedCrosshairPrefab;
            base.OnExit();
            if(VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Cast");
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }
    }
}
