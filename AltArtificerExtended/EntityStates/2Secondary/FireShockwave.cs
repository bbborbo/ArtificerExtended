using AltArtificerExtended.Skills;
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

namespace AltArtificerExtended.EntityState
{
    public class FireShockwave : BaseState
    {
        public GameObject fireEffectPrefab = Resources.Load<GameObject>("prefabs/effects/muzzleflashes/TreebotShockwaveEffect");
        public Ray burstAimRay;
        public static float damage = Main.artiNanoDamage;
        public float procCoefficient = 1.0f;
        public float baseDuration = 0.7f;

        public float backupDistance = 2.5f;
        public float maxDistance = 20;
        public static float smallHopVelocity = 7;
        public static float recoilAmplitude = 3.5f;


        public string sound;
        public string muzzle;
        public float fieldOfView = 100;
        public float maxAngleFraction = 0.45f;

        public float idealDistanceToPlaceTargets = 20;
        public float liftVelocity = 20;
        public float slowDuration;
        public float groundKnockbackDistance;
        public float airKnockbackDistance;
        public static AnimationCurve shoveSuitabilityCurve = FireSonicBoom.shoveSuitabilityCurve;
        private float duration;

        public override void OnEnter()
        {
            base.OnEnter();
            //Util.PlaySound(this.sound, base.gameObject);
            Ray aimRay = base.GetAimRay();

            base.SmallHop(base.characterMotor, smallHopVelocity);
            base.AddRecoil(-1f * recoilAmplitude, -2f * recoilAmplitude, -0.5f * recoilAmplitude, 0.5f * recoilAmplitude);
            FireZapCone(aimRay.origin);
            aimRay.origin -= aimRay.direction * this.backupDistance;

            if (NetworkServer.active)
            {
                BullseyeSearch bullseyeSearch = new BullseyeSearch();
                bullseyeSearch.teamMaskFilter = TeamMask.all;
                bullseyeSearch.maxAngleFilter = this.fieldOfView * this.maxAngleFraction;
                bullseyeSearch.maxDistanceFilter = this.maxDistance + this.backupDistance;
                bullseyeSearch.searchOrigin = aimRay.origin;
                bullseyeSearch.searchDirection = burstAimRay.direction;
                bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
                bullseyeSearch.filterByLoS = false;
                bullseyeSearch.RefreshCandidates();
                bullseyeSearch.FilterOutGameObject(base.gameObject);
                IEnumerable<HurtBox> enumerable = bullseyeSearch.GetResults().Where(new Func<HurtBox, bool>(Util.IsValid)).Distinct(default(HurtBox.EntityEqualityComparer));
                TeamIndex team = base.GetTeam();
                foreach (HurtBox hurtBox in enumerable)
                {
                    if (FriendlyFireManager.ShouldSplashHitProceed(hurtBox.healthComponent, team))
                    {
                        Vector3 vector = hurtBox.transform.position - aimRay.origin;
                        float magnitude = vector.magnitude;
                        float num = 1f;
                        CharacterBody body = hurtBox.healthComponent.body;
                        if (body.characterMotor)
                        {
                            num = body.characterMotor.mass;
                        }
                        else if (hurtBox.healthComponent.GetComponent<Rigidbody>())
                        {
                            num = base.rigidbody.mass;
                        }
                        float num2 = 0.4f;//FireShockwave.shoveSuitabilityCurve.Evaluate(num);
                        //this.AddDebuff(body);
                        body.RecalculateStats();
                        float acceleration = body.acceleration;
                        Vector3 a = vector / magnitude;
                        float d = Trajectory.CalculateInitialYSpeedForHeight(Mathf.Abs(this.idealDistanceToPlaceTargets - magnitude), -acceleration) 
                            * Mathf.Sign(this.idealDistanceToPlaceTargets - magnitude);
                        a *= d;
                        //a.y = this.liftVelocity;
                        DamageInfo damageInfo = new DamageInfo
                        {
                            attacker = base.gameObject,
                            damage = this.CalculateDamage(),
                            crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master),
                            position = hurtBox.transform.position,
                            procCoefficient = this.CalculateProcCoefficient()
                            //, damageType = DamageType.Stun1s
                        };
                        AddDebuff(body);
                        hurtBox.healthComponent.TakeDamageForce(a * (num * num2), true, true);
                        hurtBox.healthComponent.TakeDamage(damageInfo);
                        GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtBox.healthComponent.gameObject);
                    }
                }
            }
            if (base.isAuthority && base.characterBody && base.characterBody.characterMotor)
            {
                /*
                float height = base.characterBody.characterMotor.isGrounded ? this.groundKnockbackDistance : this.airKnockbackDistance;
                float num3 = base.characterBody.characterMotor ? base.characterBody.characterMotor.mass : 1f;
                float acceleration2 = base.characterBody.acceleration;
                float num4 = Trajectory.CalculateInitialYSpeedForHeight(height, -acceleration2);
                base.characterBody.characterMotor.ApplyForce(-num4 * num3 * aimRay.direction, false, false);
                */
            }
        }

        private void FireZapCone(Vector3 origin)
        {
            FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
            fireProjectileInfo.position = origin;
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(burstAimRay.direction);
            fireProjectileInfo.owner = base.gameObject;
            fireProjectileInfo.projectilePrefab = _2ShockwaveSkill.shockwaveZapConePrefab;
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }

        protected virtual void AddDebuff(CharacterBody body)
        {
            //body.AddTimedBuff(BuffIndex.Weak, this.slowDuration);
            SetStateOnHurt component = body.healthComponent.GetComponent<SetStateOnHurt>();
            if (component == null)
            {
                return;
            }
            component.SetStun(1f * procCoefficient);
        }

        protected virtual float CalculateDamage()
        {
            return damage * this.damageStat;
        }

        protected virtual float CalculateProcCoefficient()
        {
            return this.procCoefficient;
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

        protected virtual EntityStates.EntityState GetNextState()
        {
            float remainingDuration = (CastShockwave.totalDuration - CastShockwave.baseDuration) / this.attackSpeedStat;

            return new FireShockwaveVisuals()
            {
                baseDuration = remainingDuration
            };
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
