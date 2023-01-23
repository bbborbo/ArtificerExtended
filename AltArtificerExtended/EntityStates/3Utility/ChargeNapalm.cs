using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using EntityStates.Mage.Weapon;
using EntityStates.BeetleQueenMonster;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;
//using AlternativeArtificer.States.Main;
using System.Threading.Tasks;
//using AltArtificerExtended.Passive;
using ArtificerExtended.Skills;

namespace ArtificerExtended.EntityState
{
    // Token: 0x020009E0 RID: 2528
    public class ChargeNapalm : BaseSkillState
    {

        //specific attack stuff
        public GameObject projectilePrefab = _1NapalmSkill.projectilePrefabNapalm;
        public GameObject acidPrefab = _1NapalmSkill.acidPrefabNapalm;

        //specific charge stuff
        public GameObject muzzleflashEffectPrefab;
        public GameObject chargeEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/ChargeMageFireBomb");
        public string chargeSoundString = "Play_mage_m2_charge";
        public float baseChargeDuration = 2f;
        public float baseWinddownDuration = 0.5f;

        //generic attack stuff
        public static float maxDamageCoefficient = 3f;
        public static float napalmBurnDamageCoefficient = 1f;

        public static int minRowCount = 2;
        public static int maxRowCount = 2;
        public static float minYawSpread = 5f; //yaw is turn
        public static float maxYawSpread = 25f;
        public static int minProjectileCount = 3; // per row
        public static int maxProjectileCount = 3;
        public static float minPitchSpread = 5f; //pitch is vertical angle
        public static float maxPitchSpread = 10f;

        public static float minSpread = 3f;
        public static float maxSpread = 15f;
        public static float arcAngle = 5f;
        public static float projectileHSpeed = 50f;

        public float force = 5;
        public static float selfForce = 2000f;

        //everything past here is generic charge stuff
        public static float minRadius = 0;
        public static float maxRadius = 0.5f;

        private const float minChargeDuration = 0.2f;
        private float stopwatch;
        private float timer;
        private float frequency = 0.6f;
        private float windDownDuration;
        private float chargeDuration;
        private bool hasFiredBomb;

        private string muzzleString;
        private Transform muzzleTransform;

        private Animator animator;
        private ChildLocator childLocator;
        private GameObject chargeEffectInstance;
        private GameObject defaultCrosshairPrefab;
        public static GameObject crosshairOverridePrefab;
        private uint soundID;

        private Ray aimRay;

        //private AltArtiPassive.BatchHandle handle;

        public override void OnEnter()
        {
            base.OnEnter();
            this.stopwatch = 0f;
            this.timer = 0f;

            //this.handle = new AltArtiPassive.BatchHandle();

            this.windDownDuration = this.baseWinddownDuration / this.attackSpeedStat;
            this.chargeDuration = this.baseChargeDuration / this.attackSpeedStat;
            this.soundID = Util.PlayAttackSpeedSound(this.chargeSoundString, base.gameObject, this.attackSpeedStat);
            base.characterBody.SetAimTimer(this.chargeDuration + this.windDownDuration + 2f);
            this.muzzleString = "MuzzleBetween";
            this.animator = base.GetModelAnimator();
            if (this.animator)
            {
                this.childLocator = this.animator.GetComponent<ChildLocator>();
            }
            if (this.childLocator)
            {
                this.muzzleTransform = this.childLocator.FindChild(this.muzzleString);
                if (this.muzzleTransform && this.chargeEffectPrefab)
                {
                    this.chargeEffectInstance = UnityEngine.Object.Instantiate<GameObject>(this.chargeEffectPrefab, this.muzzleTransform.position, this.muzzleTransform.rotation);
                    this.chargeEffectInstance.transform.parent = this.muzzleTransform;
                    ScaleParticleSystemDuration component = this.chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
                    ObjectScaleCurve component2 = this.chargeEffectInstance.GetComponent<ObjectScaleCurve>();
                    if (component)
                    {
                        component.newDuration = this.chargeDuration;
                    }
                    if (component2)
                    {
                        component2.timeMax = this.chargeDuration;
                    }
                }
            }
            base.PlayAnimation("Gesture, Additive", "ChargeNovaBomb", "ChargeNovaBomb.playbackRate", this.chargeDuration);
            this.defaultCrosshairPrefab = base.characterBody._defaultCrosshairPrefab;
            if (ChargeNapalm.crosshairOverridePrefab)
            {
                base.characterBody._defaultCrosshairPrefab = ChargeNapalm.crosshairOverridePrefab;
            }
        }

        public override void Update()
        {
            base.Update();
            base.characterBody.SetSpreadBloom(Util.Remap(this.GetChargeProgress(), 0f, 1f, maxRadius, minRadius), true);
        }

        public override void OnExit()
        {
            if (!this.outer.destroying && !this.hasFiredBomb)
            {
                base.PlayAnimation("Gesture, Additive", "Empty");
            }
            if (this.chargeEffectInstance)
            {
                Destroy(this.chargeEffectInstance);
            }
            AkSoundEngine.StopPlayingID(this.soundID);
            base.characterBody._defaultCrosshairPrefab = this.defaultCrosshairPrefab;
            base.characterBody.AddSpreadBloom(4);

            base.OnExit();
        }

        private async void FireNapalm()
        {
            this.hasFiredBomb = true;

            base.PlayAnimation("Gesture, Additive", "FireNovaBomb", "FireNovaBomb.playbackRate", this.windDownDuration);

            this.aimRay = base.GetAimRay();

            if (this.chargeEffectInstance)
            {
                global::EntityStates.EntityState.Destroy(this.chargeEffectInstance);
            }
            if (this.muzzleflashEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(this.muzzleflashEffectPrefab, base.gameObject, "MuzzleLeft", false);
                EffectManager.SimpleMuzzleFlash(this.muzzleflashEffectPrefab, base.gameObject, "MuzzleRight", false);
            }
            if (base.isAuthority)
            {
                float chargeProgress = this.GetChargeProgress();

                if (base.characterMotor)
                {
                    base.characterMotor.ApplyForce(aimRay.direction * (-ChargeNapalm.selfForce * chargeProgress * 0.4f - ChargeNapalm.selfForce * 0.6f), false, false);
                }
                if (this.projectilePrefab != null)
                {
                    float rows = Util.Remap(chargeProgress, 0f, 1f, ChargeNapalm.minRowCount, ChargeNapalm.maxRowCount);
                    float projectilesPerRow = Util.Remap(chargeProgress, 0f, 1f, ChargeNapalm.minProjectileCount, ChargeNapalm.maxProjectileCount);
                    float totalDamage = ChargeNapalm.maxDamageCoefficient;
                    float pitchSpread = Util.Remap(chargeProgress, 0f, 1f, ChargeNapalm.maxPitchSpread, ChargeNapalm.minPitchSpread);
                    float yawSpread = Util.Remap(chargeProgress, 0f, 1f, ChargeNapalm.maxYawSpread, ChargeNapalm.minYawSpread);

                    Ray aimRay2 = base.GetAimRay();
                    Vector3 direction = aimRay2.direction;
                    Vector3 origin = aimRay2.origin;

                    float yawPerRow = (yawSpread * 2) / (rows + 1);
                    float pitchPerProjectile = (pitchSpread * 2) / (projectilesPerRow + 1);

                    int totalProjectiles = ChargeNapalm.maxRowCount * (int)projectilesPerRow;
                    float projectileDamageCoeff = totalDamage / (int)totalProjectiles;
                    for (int n = 0; n < ChargeNapalm.maxRowCount; n++)
                    {
                        for (int i = 0; i < projectilesPerRow; i++)
                        {
                            this.FireBlob(this.aimRay, (pitchPerProjectile * (i + 1)) - pitchSpread,
                                (yawPerRow * (n + 1)) - yawSpread, projectileDamageCoeff, direction, origin);
                            await Task.Delay(50);
                        }
                    }
                }
            }

            base.characterBody._defaultCrosshairPrefab = this.defaultCrosshairPrefab;
            this.stopwatch = 0f;
            this.timer = 0f;
            //this.handle.Fire(0f, 0.5f);
        }

        private void FireBlob(Ray aimRay, float bonusPitch, float bonusYaw, float projectileDamageCoeff, Vector3 direction, Vector3 origin)
        {
            float spread = Util.Remap(this.GetChargeProgress(), 0f, 1f, ChargeNapalm.maxSpread, ChargeNapalm.minSpread);

            Vector3 forward = Util.ApplySpread(direction, 0, spread, 1f, 1f, bonusYaw, bonusPitch - 6f);
            //Vector3 forward = Util.ApplySpread(direction, 0, 0, 1f, 1f, bonusYaw, bonusPitch - 6f);
            ProjectileManager.instance.FireProjectile(this.projectilePrefab, origin,
                Util.QuaternionSafeLookRotation(forward), base.gameObject, this.damageStat * projectileDamageCoeff,
                0f, Util.CheckRoll(this.critStat, base.characterBody.master), DamageColorIndex.Default, null, -1);
        }

        private float GetChargeProgress()
        {
            return Mathf.Clamp01(this.stopwatch / this.chargeDuration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            this.stopwatch += Time.fixedDeltaTime;
            this.timer += Time.fixedDeltaTime * base.characterBody.attackSpeed;
            
            /*GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                while (this.timer > this.frequency)
                {
                    this.timer -= this.frequency;
                    passive.SkillCast(handle);
                }
            }*/

            if (!this.hasFiredBomb && (this.stopwatch >= this.chargeDuration || !IsKeyDownAuthority()) 
                && this.stopwatch >= ChargeNapalm.minChargeDuration)
            {
                this.FireNapalm();
            }
            if (this.stopwatch >= this.windDownDuration && this.hasFiredBomb && base.isAuthority)
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

