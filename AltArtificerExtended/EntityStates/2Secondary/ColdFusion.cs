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
using ArtificerExtended.Skills;
using R2API;
using ArtificerExtended.Passive;

namespace ArtificerExtended.States
{
    class ColdFusion : BaseSkillState
    {
        //specific attack stuff
        public GameObject projectilePrefab = _4NapalmSkill.projectilePrefabNapalm;
        public GameObject acidPrefab = _4NapalmSkill.acidPrefabNapalm;

        public GameObject muzzleflashEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/MuzzleflashMageIceLarge");
        public GameObject chargeEffectPrefab = new EntityStates.Mage.Weapon.ChargeIcebomb().chargeEffectPrefab;
        //FrozenImpactEffect, IceCullExplosion, IceRingExplosion, MageIceExplosion
        public GameObject coldImpactPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/FrozenImpactEffect");
        public GameObject freezeImpactPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/MageIceExplosion");
        public string chargeSoundString = "Play_mage_m2_charge";
        public float baseChargeDuration = 2.5f;
        public float baseWinddownDuration = 0.25f;
        public string attackSoundString = new EntityStates.Toolbot.FireSpear().fireSoundString;

        //generic attack stuff
        public static float totalDamageCoefficient = 20;
        public static int maxBulletCount = 10;
        public static int minBulletCount = 2;
        public static float freezeChance = 0;

        public static float pitchSpread = 0.25f;//pitch is vertical angle
        public static float spread = 1f;
        public static float baseRecoilAmplitude = 7f;
        public static float maxRecoilBias = 0.2f;

        public float force = 600f;
        public static float selfForce = 150f;
        public static float maxRange = ArtificerExtendedPlugin.meleeRangeSingle;

        //everything past here is generic charge stuff
        public float minRadius = 0;
        public float maxRadius = 0.5f;

        private float stopwatch;
        private float timer;
        private float frequency = AltArtiPassive.nanoBombInterval;
        private float windDownDuration;
        private float chargeDuration;
        private bool hasFiredBomb;
        private float endChargeProgress = 1;

        private string muzzleString;
        private Transform muzzleTransform;

        private Animator animator;
        private ChildLocator childLocator;
        private GameObject chargeEffectInstance;
        private GameObject defaultCrosshairPrefab;
        public static GameObject crosshairOverridePrefab;
        private uint soundID;

        private Ray aimRay;

        private AltArtiPassive.BatchHandle handle;

        public override void OnEnter()
        {
            base.OnEnter();
            this.stopwatch = 0f;
            this.timer = 0f;

            this.handle = new AltArtiPassive.BatchHandle();

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
            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Charge");
        }

        public override void Update()
        {
            base.Update();
            if (hasFiredBomb)
            {
                base.characterBody.SetSpreadBloom(Util.Remap(this.GetChargeProgress(windDownDuration - stopwatch), 0f, windDownDuration, this.minRadius, this.maxRadius * endChargeProgress), true);
                return;
            }
            base.characterBody.SetSpreadBloom(Util.Remap(this.GetChargeProgress(stopwatch), 0f, 1f, this.minRadius, this.maxRadius), true);
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

            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Cast");
            base.OnExit();
        }

        private async void FireFusionBolts()
        {
            this.hasFiredBomb = true;

            base.PlayAnimation("Gesture, Additive", "FireNovaBomb", "FireNovaBomb.playbackRate", this.windDownDuration);


            if (this.chargeEffectInstance)
            {
                Destroy(this.chargeEffectInstance);
            }
            if (this.muzzleflashEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(this.muzzleflashEffectPrefab, base.gameObject, "MuzzleLeft", false);
                EffectManager.SimpleMuzzleFlash(this.muzzleflashEffectPrefab, base.gameObject, "MuzzleRight", false);
            }
            if (base.isAuthority)
            {
                float projectileDamageCoeff = totalDamageCoefficient / maxBulletCount;
                bool isCrit = Util.CheckRoll(this.critStat, base.characterBody.master);
                float recoil = -baseRecoilAmplitude / this.attackSpeedStat;
                float recoilBias = Random.Range(-maxRecoilBias, maxRecoilBias);
                int bulletsToFire = Mathf.FloorToInt(Util.Remap(this.GetChargeProgress(this.fixedAge), 0f, 1f, minBulletCount, (float)maxBulletCount));

                for (int i = 0; i < bulletsToFire; i++)
                {
                    this.aimRay = (!VRStuff.VRInstalled) ? base.GetAimRay() : VRStuff.GetVRHandAimRay(false);
                    Vector3 direction = aimRay.direction;
                    float bonusPitch = ((float)maxBulletCount / pitchSpread - (float)i) * pitchSpread;
                    Vector3 forward = Util.ApplySpread(aimRay.direction, 0, spread, 0, 1f, 0, bonusPitch - 6f);

                    if (base.characterMotor)
                    {
                        base.characterMotor.ApplyForce(aimRay.direction * -selfForce, false, false);
                    }


                    BulletAttack ba = new BulletAttack
                    {
                        owner = base.gameObject,
                        weapon = base.gameObject,
                        origin = aimRay.origin,
                        aimVector = forward,
                        minSpread = 0f,
                        maxSpread = 0f,
                        damage = projectileDamageCoeff * this.damageStat,
                        procCoefficient = 0.75f,
                        force = force,
                        tracerEffectPrefab = _3ColdFusionSkill.fusionTracer,
                        muzzleName = this.muzzleString,
                        isCrit = isCrit,
                        radius = 0.4f,
                        falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                        maxDistance = maxRange,
                        smartCollision = true
                    };

                    ba.hitEffectPrefab = coldImpactPrefab;
                    ba.AddModdedDamageType(ChillRework.ChillRework.ChillOnHit);

                    ba.Fire();

                    Util.PlaySound(attackSoundString, base.gameObject);
                    //base.AddRecoil(recoil, recoil, recoil * recoilBias * 0.75f, recoil * recoilBias);
                    await Task.Delay(35);
                }
            }

            base.characterBody._defaultCrosshairPrefab = this.defaultCrosshairPrefab;
            this.stopwatch = 0f;
            this.timer = 0f;
            this.handle.Fire(0f, 0.5f);
        }

        private float GetChargeProgress(float charge)
        {
            return Mathf.Clamp01(charge / this.chargeDuration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            this.stopwatch += Time.fixedDeltaTime;
            this.timer += Time.fixedDeltaTime * base.characterBody.attackSpeed;

            GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                while (this.timer > this.frequency)
                {
                    this.timer -= this.frequency;
                    passive.SkillCast(handle);
                }
            }

            if (!this.hasFiredBomb)
            {
                if (!IsKeyDownAuthority() || this.stopwatch >= this.chargeDuration)
                {
                    this.FireFusionBolts();
                }
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
