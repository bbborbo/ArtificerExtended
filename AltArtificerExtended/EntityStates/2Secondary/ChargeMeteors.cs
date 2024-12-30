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
using ArtificerExtended.Skills;
using ArtificerExtended.Passive;
using System.Linq;

namespace ArtificerExtended.EntityState
{
    public class ChargeMeteors : BaseSkillState
    {
        public GameObject muzzleflashEffectPrefab;
        public GameObject chargeEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/ChargeMageFireBomb");
        public string chargeSoundString = "Play_mage_m2_charge";

        public float force = 300;

        public static GameObject crosshairOverridePrefab;

        private float minChargeDuration => _1EruptionSkill.minDuration;
        private float maxChargeDuration => _1EruptionSkill.maxDuration;
        private float baseWinddownDuration => _1EruptionSkill.windDownDuration;
        private float minDamage => _1EruptionSkill.minBlastDamage;
        private float maxDamage => _1EruptionSkill.maxBlastDamage;
        private float minMeteorRadius => _1EruptionSkill.minBlastRadius;
        private float maxMeteorRadius => _1EruptionSkill.maxBlastRadius;
        private int minProjectiles => _1EruptionSkill.minClusterProjectiles;
        private int maxProjectiles => _1EruptionSkill.maxClusterProjectiles;

        private float stopwatch;
        private float timer;
        private float windDownDuration;
        private float chargeDuration;
        private bool hasFiredBomb;

        private string muzzleString;
        private Transform muzzleTransform;

        private Animator animator;
        private ChildLocator childLocator;
        private GameObject chargeEffectInstance;
        private GameObject defaultCrosshairPrefab;
        public float minRadius = 0;
        public float maxRadius = 0.5f;
        private uint soundID;

        private AltArtiPassive.BatchHandle handle;

        public static GameObject areaIndicatorPrefab = ChargeMeteor.areaIndicatorPrefab;
        public static GameObject aoeEffect = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/omnieffect/OmniImpactVFXLightningMage");
        public static GameObject muzzleflashEffect = ChargeMeteor.muzzleflashEffect;
        public static GameObject meteorEffect = _1MeteorSkill.meteorImpactPrefab;

        private float radius;
        private GameObject areaIndicatorInstance;
        private Vector3 targetLocation;
        private bool disableIndicator = false;

        public override void OnEnter()
        {
            base.OnEnter();
            if (ChargeMeteor.areaIndicatorPrefab != null)
            {
                this.areaIndicatorInstance = UnityEngine.Object.Instantiate<GameObject>(ChargeMeteor.areaIndicatorPrefab);
                this.UpdateAreaIndicator();
            }
            else
                Debug.Log("Damn, area indicator prefab null?");

            this.handle = new AltArtiPassive.BatchHandle();

            this.windDownDuration = baseWinddownDuration / this.attackSpeedStat;
            this.chargeDuration = maxChargeDuration / this.attackSpeedStat;
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
                    /*this.chargeEffectInstance = UnityEngine.Object.Instantiate<GameObject>(this.chargeEffectPrefab, 
                        this.muzzleTransform.position, this.muzzleTransform.rotation);
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
                    }*/
                }
            }
            //base.PlayAnimation("Gesture, Additive", "ChargeNovaBomb", "ChargeNovaBomb.playbackRate", this.chargeDuration);

            base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", this.chargeDuration);
            this.defaultCrosshairPrefab = base.characterBody._defaultCrosshairPrefab;
            if (ChargeMeteors.crosshairOverridePrefab)
            {
                base.characterBody._defaultCrosshairPrefab = ChargeMeteors.crosshairOverridePrefab;
            }
            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(false, "Charge");
        }

        private void UpdateAreaIndicator()
        {
            if (this.areaIndicatorInstance && !disableIndicator)
            {
                this.areaIndicatorInstance.SetActive(true);

                float num = 1000f;
                float num2 = 0f;
                Ray aimRay = (!VRStuff.VRInstalled) ? base.GetAimRay() : VRStuff.GetVRHandAimRay(false);
                RaycastHit raycastHit;
                if (Util.CharacterRaycast(this.gameObject, CameraRigController.ModifyAimRayIfApplicable(aimRay, base.gameObject, out num2),
                        out raycastHit, num + num2, LayerIndex.CommonMasks.bullet, QueryTriggerInteraction.UseGlobal))
                {
                    targetLocation = raycastHit.point;
                    this.areaIndicatorInstance.transform.position = targetLocation;
                    this.areaIndicatorInstance.transform.up = raycastHit.normal;
                }

                this.radius = Util.Remap(this.GetChargeProgressSmooth(), 0, 1, minMeteorRadius, maxMeteorRadius);
                this.areaIndicatorInstance.transform.localScale = new Vector3(this.radius, this.radius, this.radius);
                this.areaIndicatorInstance.SetActive(true);
            }
        }

        public override void Update()
        {
            base.Update();

            base.characterBody.SetSpreadBloom(Util.Remap(this.GetChargeProgressSmooth(), 0, 1, this.minRadius, this.maxRadius), true);

            /*int meteors = Mathf.RoundToInt(Util.Remap(this.GetChargeProgressSmooth(), 0f, 1f, minMeteors, maxMeteors));

            base.characterBody.SetSpreadBloom((float)meteors/(maxMeteors * 2), true);*/

            this.UpdateAreaIndicator();
        }
        private float GetChargeProgressSmooth()
        {
            return Mathf.Clamp01(this.stopwatch / this.chargeDuration);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            this.stopwatch += Time.fixedDeltaTime;
            this.timer += Time.fixedDeltaTime * base.characterBody.attackSpeed;

            
            GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                while (this.timer > AltArtiPassive.nanoBombInterval)
                {
                    this.timer -= AltArtiPassive.nanoBombInterval;
                    passive.SkillCast(handle, true);
                }
            }
            if (!this.hasFiredBomb && (this.stopwatch >= this.chargeDuration || !IsKeyDownAuthority()) &&
                !this.hasFiredBomb && this.stopwatch >= minChargeDuration)
            {
                this.FireMeteor();
            }
            if (this.stopwatch >= this.windDownDuration && this.hasFiredBomb && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        private void FireMeteor()
        {
            this.hasFiredBomb = true;
            base.PlayAnimation("Gesture, Additive", "FireNovaBomb", "FireNovaBomb.playbackRate", this.windDownDuration);
            base.PlayAnimation("Gesture, Additive", "FireWall");
            Ray aimRay = (!VRStuff.VRInstalled) ? base.GetAimRay() : VRStuff.GetVRHandAimRay(false);
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
                Vector3 aimPos = characterBody.corePosition;
                Vector3 aimNormal = Vector3.up;
                if (areaIndicatorInstance != null)
                {
                    aimPos = areaIndicatorInstance.transform.position;
                    aimNormal = areaIndicatorInstance.transform.up;
                }
                else
                {

                    float maxDistance = 1000f;
                    float extraDistance = 0f;
                    Ray aRay = (!VRStuff.VRInstalled) ? base.GetAimRay() : VRStuff.GetVRHandAimRay(false);
                    RaycastHit raycastHit;
                    if (Util.CharacterRaycast(this.gameObject, CameraRigController.ModifyAimRayIfApplicable(aRay, base.gameObject, out extraDistance),
                        out raycastHit, maxDistance + extraDistance, LayerIndex.CommonMasks.bullet, QueryTriggerInteraction.UseGlobal))
                    {
                        aimPos = raycastHit.point;
                        aimNormal = raycastHit.normal;
                    }
                }


                disableIndicator = true;
                this.areaIndicatorInstance.SetActive(false);
                float chargeProgress = this.GetChargeProgressSmooth();
                bool crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
                float damage = Util.Remap(chargeProgress, minChargeDuration / maxChargeDuration, 1, minDamage, maxDamage);

                EffectManager.SpawnEffect(_1EruptionSkill.meteorImpactEffectPrefab, new EffectData
                {
                    origin = targetLocation,
                    scale = this.radius
                }, true);
                BlastAttack blastAttack = new BlastAttack();
                blastAttack.radius = this.radius;
                blastAttack.procCoefficient = 1;
                blastAttack.position = targetLocation;
                blastAttack.attacker = base.gameObject;
                blastAttack.crit = crit;
                blastAttack.baseDamage = base.characterBody.damage * damage;
                blastAttack.falloffModel = BlastAttack.FalloffModel.None;
                blastAttack.baseForce = force;
                blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
                blastAttack.damageType = DamageType.IgniteOnHit;
                blastAttack.damageType.damageSource = DamageSource.Secondary;
                blastAttack.Fire();

                if (aimNormal == Vector3.one)
                    aimNormal += Vector3.forward * 0.1f;
                int projectiles = Mathf.FloorToInt(Util.Remap(chargeProgress, minChargeDuration / maxChargeDuration, 1, minProjectiles, maxProjectiles));
                for(int i = 0; i < projectiles; i++)
                {
                    Vector3 forward = Util.ApplySpread(aimNormal, 20, 70, 1, 1);
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = _2LavaBoltsSkill.lavaProjectilePrefab,
                        position = aimPos + (forward * 1.5f),
                        rotation = Util.QuaternionSafeLookRotation(forward),
                        owner = this.outer.gameObject,
                        damage = _1EruptionSkill.clusterProjectileDamage,
                        force = 300,
                        crit = crit,
                        damageColorIndex = DamageColorIndex.Default,
                        speedOverride = 15f
                    });
                }
            }

            base.characterBody._defaultCrosshairPrefab = this.defaultCrosshairPrefab;
            this.stopwatch = 0f;
            this.timer = 0f;

            global::EntityStates.EntityState.Destroy(this.areaIndicatorInstance.gameObject);
            disableIndicator = true;

        }

        public override void OnExit()
        {
            this.handle.Fire(0f, 0.5f);
            if (!this.outer.destroying && !this.hasFiredBomb)
            {
                base.PlayAnimation("Gesture, Additive", "Empty");
            }
            if (this.chargeEffectInstance)
            {
                global::EntityStates.EntityState.Destroy(this.chargeEffectInstance);
            }
            AkSoundEngine.StopPlayingID(this.soundID);
            base.characterBody._defaultCrosshairPrefab = this.defaultCrosshairPrefab;

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