using ArtificerExtended.States;
using ArtificerExtended.Passive;
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
    class CastHeatColumn : BaseSkillState
    {
        public static float baseDuration = 0.3f;
        float baseSpeed = 15f;

        public static GameObject projectilePrefab => _1HeatColumnSkill.HeatWardPrefab;
        public static GameObject areaIndicatorPrefab => _1HeatColumnSkill.HeatWardAreaIndicator;
        public static GameObject aoeEffect = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Mage.OmniImpactVFXLightningMage_prefab).WaitForCompletion();
        public static GameObject muzzleflashEffect = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Junk_Mage.MuzzleflashMageFireLarge_prefab).WaitForCompletion();

        public static float damagePerMeatball = 1.8f;

        public static int meatballCount = 3;
        public static float meatballAngleMin = 1f;
        public static float meatballAngleMax = 7f;
        public static float meatballForce = 250;

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

            //if (ArtificerExtendedPlugin.AllowBrokenSFX.Value == true)
            //    Util.PlaySound(PrepWall.prepWallSoundString, base.gameObject);
            this.areaIndicatorInstance = UnityEngine.Object.Instantiate<GameObject>(areaIndicatorPrefab);
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
                    out raycastHit, num + num2, LayerIndex.world.mask | LayerIndex.enemyBody.mask, QueryTriggerInteraction.UseGlobal))
                {
                    this.areaIndicatorInstance.transform.position = raycastHit.point;
                    this.areaIndicatorInstance.transform.up = Vector3.one;// raycastHit.normal;
                }
            }
            this.radius = _1HeatColumnSkill.heatWardRadius;
            this.areaIndicatorInstance.transform.localScale = new Vector3(this.radius, this.areaIndicatorInstance.transform.localScale.y, this.radius);
            this.areaIndicatorInstance.transform.rotation = Quaternion.identity;
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
            if (muzzleflashEffect)
            {
                EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleLeft", false);
                EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleRight", false);
            }
            if (this.areaIndicatorInstance)
            {
                base.PlayAnimation("Gesture, Additive", "FireWall");
                if (!this.outer.destroying && base.isAuthority)
                {
                    GameObject obj = base.outer.gameObject;
                    if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
                    {
                        passive.SkillCast(isFire: true);
                    }

                    EffectManager.SpawnEffect(aoeEffect, new EffectData
                    {
                        origin = this.areaIndicatorInstance.transform.position,
                    }, true);

                    Vector3 surfaceNormal = this.areaIndicatorInstance.transform.up;
                    SummonHeatColumn(this.areaIndicatorInstance.transform.position,
                        meatballCount, meatballForce);

                }
                global::EntityStates.EntityState.Destroy(this.areaIndicatorInstance.gameObject);
            }
            base.characterBody._defaultCrosshairPrefab = this.cachedCrosshairPrefab;
            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Cast");
            base.OnExit();
        }

        private void SummonHeatColumn(Vector3 impactPosition, int meatballCount, float meatballForce)
        {
            ProjectileManager.instance.FireProjectile(new FireProjectileInfo
            {
                projectilePrefab = projectilePrefab,
                position = impactPosition,
                rotation = Quaternion.identity,
                owner = this.outer.gameObject,
                damage = this.characterBody.damage,
                force = meatballForce,
                crit = false,
                damageColorIndex = DamageColorIndex.Default
            });
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Pain;
        }
    }
}
