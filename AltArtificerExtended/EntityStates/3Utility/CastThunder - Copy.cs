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
    public class CastThunderOld : BaseSkillState
    {
        public static float baseDuration = 0.3f;
        float baseSpeed = 15f;

        public static GameObject areaIndicatorPrefab = ChargeMeteor.areaIndicatorPrefab;
        public static GameObject aoeEffect = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/omnieffect/OmniImpactVFXLightningMage");
        public static GameObject muzzleflashEffect = ChargeMeteor.muzzleflashEffect;

        public static float minMeteorRadius = _2ThunderSkill.thunderBlastRadius;
        public static float damagePerMeatball = 1.8f;

        public static int meatballCount = 3;
        public static float meatballAngleMin = 1f;
        public static float meatballAngleMax = 7f;
        public static float meatballForce = 250;
        public static GameObject meatballProjectile = _2ThunderSkill.projectilePrefabThunder;

        private float stopwatch;
        private float radius;
        private float duration;

        private GameObject cachedCrosshairPrefab;
        private GameObject areaIndicatorInstance;


        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = CastThunderOld.baseDuration / this.attackSpeedStat;
            base.characterBody.SetAimTimer(this.duration + 2f);
            this.cachedCrosshairPrefab = base.characterBody._defaultCrosshairPrefab;
            base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", this.duration);

            if (ArtificerExtendedPlugin.AllowBrokenSFX.Value == true)
                Util.PlaySound(PrepWall.prepWallSoundString, base.gameObject);
            this.areaIndicatorInstance = UnityEngine.Object.Instantiate<GameObject>(ChargeMeteor.areaIndicatorPrefab);
            this.UpdateAreaIndicator();

            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Charge");
            base.OnEnter();
        }

        private void UpdateAreaIndicator()
        {
            this.areaIndicatorInstance.SetActive(true);
            if (this.areaIndicatorInstance)
            {
                float num = 1000f;
                float num2 = 0f;
                Ray aimRay = (!VRStuff.VRInstalled) ? base.GetAimRay() : VRStuff.GetVRHandAimRay(false);
                RaycastHit raycastHit;
                if (Util.CharacterRaycast(this.gameObject, CameraRigController.ModifyAimRayIfApplicable(aimRay, base.gameObject, out num2),
                    out raycastHit, num + num2, LayerIndex.CommonMasks.bullet, QueryTriggerInteraction.UseGlobal))
                {
                    this.areaIndicatorInstance.transform.position = raycastHit.point;
                    this.areaIndicatorInstance.transform.up = Vector3.one;// raycastHit.normal;
                }
            }
            this.radius = CastThunderOld.minMeteorRadius;
            this.areaIndicatorInstance.transform.localScale = new Vector3(this.radius, this.radius, this.radius);
            this.areaIndicatorInstance.SetActive(true);
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
            if (this.stopwatch >= this.duration && !IsKeyDownAuthority() && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            if (this.areaIndicatorInstance)
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

                    EffectManager.SpawnEffect(CastThunderOld.aoeEffect, new EffectData
                    {
                        origin = this.areaIndicatorInstance.transform.position,
                    }, true);

                    Vector3 surfaceNormal = this.areaIndicatorInstance.transform.up;
                    FireMeatballs(surfaceNormal, this.areaIndicatorInstance.transform.position + Vector3.up * 0.2f,
                        meatballCount, meatballForce);

                }
                global::EntityStates.EntityState.Destroy(this.areaIndicatorInstance.gameObject);
            }
            base.characterBody._defaultCrosshairPrefab = this.cachedCrosshairPrefab;
            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Cast");
            base.OnExit();
        }

        private async void FireMeatballs(Vector3 impactNormal, Vector3 impactPosition, int meatballCount, float meatballForce)
        {
            int delay = 100;

            float rotationInverval = 360f / (float)meatballCount;
            float rotationExtra = UnityEngine.Random.Range(0, 360);
            Vector3 normalized = Vector3.ProjectOnPlane(Vector3.up, impactNormal).normalized;
            for (int i = 0; i < meatballCount; i++)
            {
                float speedOverride = baseSpeed + (((_2ThunderSkill.desiredForwardSpeedMax - baseSpeed) * (i + 1)) / meatballCount);
                float angle = UnityEngine.Random.Range(CastThunderOld.meatballAngleMin, CastThunderOld.meatballAngleMax);
                Vector3 point = Vector3.RotateTowards(Vector3.up, normalized, angle * 0.0174532924f, float.PositiveInfinity);

                Vector3 forward2 = Quaternion.AngleAxis(rotationExtra + rotationInverval * (float)i, Vector3.up) * point;

                ProjectileManager.instance.FireProjectile(meatballProjectile, impactPosition, Util.QuaternionSafeLookRotation(forward2),
                    base.gameObject, this.characterBody.damage * damagePerMeatball, meatballForce, 
                    Util.CheckRoll(this.characterBody.crit, this.characterBody.master), DamageColorIndex.Default, null, speedOverride);
                await Task.Delay(delay);
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }
    }
}
