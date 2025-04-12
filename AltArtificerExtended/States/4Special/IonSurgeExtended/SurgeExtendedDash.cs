using ArtificerExtended.Components;
using ArtificerExtended.Passive;
using EntityStates;
using EntityStates.Mage;
using EntityStates.Mage.Weapon;
using EntityStates.Toolbot;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.States
{
    /// <summary>
    /// Reworked Ion Surge
    /// 
    /// First disables gravity, then has a brief period in which Artificer charges a surge of energy
    /// After the charging period, Artificer releases the surge of energy, bursting forward in flight
    /// If Artificer is grounded, the initial flight will have a small vertical boost in order to avoid hitting the ground
    /// Otherwise, Artificer flies in the direction of aiming
    /// For a short period after surging begins, anti-gravity continues, then gravity resumes
    /// After a long period of flight, the surging speed ends, and Artificer will fall to the ground
    /// 
    /// The flight can be interrupted at any point by crashing into a surface or a heavy enemy
    /// On impact, deal a large amount of stunning damage in an aoe
    /// Impact leaves behind several waves of aftershocks, which continue to deal considerable damage and stun in an area
    /// </summary>
    class SurgeExtendedDash : BaseCharacterMain
    {
        public static float baseSurgeSkillCastInterval = 0.7f;

        public static float grazeDamageCoefficient = 3f;
        public static float grazeProcCoefficient = 0.5f;
        public static float grazeUpForceMagnitude = ToolbotDash.upwardForceMagnitude;
        public static float grazeAwayForceMagnitude = ToolbotDash.awayForceMagnitude;
        public static float grazeHitPauseDuration = ToolbotDash.hitPauseDuration;
        public static float zapRange = 13f;
        public static int zapFireCount = 3;
        public static float baseZapAttackInterval = 0.25f;
        public static float baseZapClearInterval = 1f;

        public static float impactDamageCoefficient = 9f;
        public static float impactProcCoefficient = 1f;
        public static float impactRadius = 21f;
        public static float impactBlastForce = 1250f;
        public static float impactKnockbackForce = 750f;
        public static float impactRecoilAmplitude = ToolbotDash.recoilAmplitude;

        public static float aftershockDamageCoefficient = 3f;
        public static float aftershockProcCoefficient = 1f;
        public static float aftershockRadius = 18f;
        public static float aftershockFrequency = 2f;
        public static float aftershockTotalWaves = 3f;

        public static float massThresholdForImpact = 250;
        public static float baseWindupDuration = 0.6f; // duration to wind up before surge
        public static float antiGravityDuration = 2; // duration after beginning to ignore gravity
        public static float flightDuration = 3.5f; // duration after gravity begins before surging ends
        public static float minFlightDuration = 0.2f; // duration after gravity begins before surging ends
        public static float startingSurgeSpeed = 5f; // movement speed multiplier while surging
        public static float endingSurgeSpeed = 2f; // movement speed multiplier while surging
        public static float baseSurgeDrag = 0.6f; // the time in seconds it should take for the surge direction to respond to changes in aim direction
        public static float surgeJumpFactor = 0.3f; // how much the initial direction should be adjusted if casted while grounded
        public static float gravityStrengthOverTime = 11f;

        GameObject surgeTrailEffectInstanceL;
        GameObject surgeTrailEffectInstanceR;

        CharacterModel model;
        Rotator rotator;
        Vector3 idealDirection;
        float windupDuration;
        bool hasStartedFlight;
        bool isInFlight;
        bool isAntiGravity;
        bool detonateNextFrame;
        bool isCrit;

        public AltArtiPassive.BatchHandle handle;
        float skillCastInterval;
        float skillCastTimer;

        List<HurtBox> victimsStruck = new List<HurtBox>();
        OverlapAttack attack;
        bool inHitPause;
        float hitPauseTimer;

        BullseyeSearch search = new BullseyeSearch();
        List<HealthComponent> previousTargets = new List<HealthComponent>();
        float zapAttackInterval;
        float zapAttackTimer;
        float zapClearInterval;
        float zapClearTimer;

        public override void OnEnter()
        {
            base.OnEnter();

            windupDuration = Mathf.Max(baseWindupDuration / attackSpeedStat, 0);
            skillCastInterval = baseSurgeSkillCastInterval / attackSpeedStat;
            zapAttackInterval = baseZapAttackInterval / attackSpeedStat;
            zapClearInterval = baseZapClearInterval / attackSpeedStat; 
            skillCastTimer = 0;
            this.handle = new AltArtiPassive.BatchHandle();

            base.characterBody.SetAimTimer(windupDuration + flightDuration + 5f);
            BeginCharging();

            HitBoxGroup hitBoxGroup = null;
            Transform modelTransform = base.GetModelTransform();
            if (modelTransform)
            {
                model = modelTransform.GetComponent<CharacterModel>();

                hitBoxGroup = Array.Find<HitBoxGroup>(modelTransform.GetComponents<HitBoxGroup>(), 
                    (HitBoxGroup element) => element.groupName == ArtificerExtendedPlugin.ThunderSurgeHitBoxGroupName);

                GameObject mageArmature = modelTransform.Find("MageArmature")?.gameObject;
                if (mageArmature)
                {
                    rotator = mageArmature.GetComponent<Rotator>();
                    if (rotator == null)
                        rotator = mageArmature.AddComponent<Rotator>();
                }
            }

            this.attack = new OverlapAttack();
            this.attack.attacker = base.gameObject;
            this.attack.inflictor = base.gameObject;
            this.attack.teamIndex = base.GetTeam();
            this.attack.damage = grazeDamageCoefficient * this.damageStat;
            this.attack.procCoefficient = grazeProcCoefficient;
            this.attack.hitEffectPrefab = ToolbotDash.impactEffectPrefab;
            this.attack.forceVector = Vector3.up * grazeUpForceMagnitude;
            this.attack.pushAwayForce = grazeAwayForceMagnitude;
            this.attack.hitBoxGroup = hitBoxGroup;
            this.attack.damageType = DamageType.Stun1s;
            this.attack.damageType.damageSource = DamageSource.Special;
            isCrit = base.RollCrit();
            this.attack.isCrit = isCrit;
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //begin flight after windup
            if(!isInFlight && 
                base.fixedAge >= windupDuration && 
                base.fixedAge < windupDuration + flightDuration)
            {
                BeginFlight();
            }

            if (hasStartedFlight)
            {
                //do impact if grounded
                if (base.isAuthority &&
                    (detonateNextFrame ||
                        (base.characterMotor.Motor.GroundingStatus.FoundAnyGround
                        && !base.characterMotor.Motor.LastGroundingStatus.FoundAnyGround)))
                {
                    Debug.Log("Environment detonation");
                    OnSurgeImpact();
                    return;
                }


                if (base.isAuthority)
                {
                    if (base.characterBody)
                    {
                        base.characterBody.isSprinting = true;
                    }
                    if (!this.inHitPause)
                    {
                        if (base.characterDirection)
                        {
                            base.characterDirection.moveVector = Vector3.zero;
                            if (base.characterMotor)
                            {
                                base.characterMotor.rootMotion += this.GetIdealVelocity() * base.GetDeltaTime();
                            }
                        }
                        ProcessHits();
                    }
                    else
                    {
                        //hitpause
                        base.characterMotor.velocity = Vector3.zero;
                        this.hitPauseTimer -= base.GetDeltaTime();
                        if (this.hitPauseTimer < 0f)
                        {
                            this.inHitPause = false;
                        }
                    }
                }

                //do flight movement and hits
                if (isInFlight)
                {
                    base.characterMotor.disableAirControlUntilCollision = false;
                    if (base.fixedAge >= windupDuration + flightDuration)
                    {
                        EndFlight();
                        return;
                    }
                    skillCastTimer -= base.GetDeltaTime();
                    if (skillCastTimer <= 0)
                    {
                        AddSkillCast();
                    }

                    this.UpdateDirection();
                }
            }
            
            //do antigravity
            if (isAntiGravity)
            {
                if (base.fixedAge >= antiGravityDuration + windupDuration)
                {
                    SetAntiGravity(false);
                }
            }
        }
        private void ProcessHits()
        {
            this.attack.damage = this.damageStat * (ToolbotDash.chargeDamageCoefficient * this.GetDamageBoostFromSpeed());
            if (this.attack.Fire(this.victimsStruck))
            {
                Util.PlaySound(ToolbotDash.impactSoundString, base.gameObject);
                this.inHitPause = true;
                this.hitPauseTimer = grazeHitPauseDuration;
                //base.AddRecoil(-0.5f * ToolbotDash.recoilAmplitude, -0.5f * ToolbotDash.recoilAmplitude, -0.5f * ToolbotDash.recoilAmplitude, 0.5f * ToolbotDash.recoilAmplitude);
                for (int i = 0; i < this.victimsStruck.Count; i++)
                {
                    float mass = 0f;
                    HurtBox hurtBox = this.victimsStruck[i];
                    HealthComponent hc = hurtBox.healthComponent;
                    if (hc)
                    {
                        this.previousTargets.Add(hc);

                        CharacterMotor component = hc.GetComponent<CharacterMotor>();
                        if (component)
                        {
                            mass = component.mass;
                        }
                        else
                        {
                            Rigidbody component2 = hc.GetComponent<Rigidbody>();
                            if (component2)
                            {
                                mass = component2.mass;
                            }
                        }

                        if (mass >= massThresholdForImpact)
                        {
                            OnSurgeImpact();
                            return;
                        }
                    }
                }
                return;
            }

            if (!this.isInFlight)
                return;

            this.zapClearTimer -= base.GetDeltaTime();
            if (this.zapClearTimer <= 0f)
            {
                this.ClearList();
                this.zapClearTimer = this.zapClearInterval;
            }
            this.zapAttackTimer -= base.GetDeltaTime();
            if (this.zapAttackTimer <= 0f)
            {
                this.zapAttackTimer += this.zapAttackInterval;
                Vector3 position = base.transform.position;
                Vector3 forward = base.transform.forward;
                for (int i = 0; i < zapFireCount; i++)
                {
                    HurtBox hurtBox = this.FindNextTarget(position, forward);
                    if (hurtBox)
                    {
                        OnZapTargetFound(position, hurtBox);
                    }
                    else
                        break;
                }
            }
        }

        public virtual void OnZapTargetFound(Vector3 position, HurtBox hurtBox)
        {
            this.previousTargets.Add(hurtBox.healthComponent);

            LightningOrb lightningOrb = new LightningOrb();
            lightningOrb.bouncedObjects = new List<HealthComponent>();
            lightningOrb.attacker = base.gameObject;
            lightningOrb.inflictor = base.gameObject;
            lightningOrb.teamIndex = this.attack.teamIndex;
            lightningOrb.damageValue = this.attack.damage;
            lightningOrb.isCrit = this.isCrit;
            lightningOrb.origin = position;
            lightningOrb.bouncesRemaining = 1;
            lightningOrb.lightningType = LightningOrb.LightningType.MageLightning;
            lightningOrb.procCoefficient = grazeProcCoefficient;
            lightningOrb.target = hurtBox;
            lightningOrb.damageColorIndex = DamageColorIndex.Default;
            lightningOrb.damageType = new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.Generic, DamageSource.Special);

            OrbManager.instance.AddOrb(lightningOrb);
        }

        public HurtBox FindNextTarget(Vector3 position, Vector3 forward)
        {
            this.search.searchOrigin = position;
            this.search.searchDirection = forward;
            this.search.sortMode = BullseyeSearch.SortMode.Distance;
            this.search.teamMaskFilter = TeamMask.allButNeutral;
            this.search.teamMaskFilter.RemoveTeam(base.GetTeam());
            this.search.filterByLoS = false;
            this.search.minAngleFilter = 0;
            this.search.maxAngleFilter = 180;
            this.search.maxDistanceFilter = zapRange;
            this.search.RefreshCandidates();
            return this.search.GetResults().FirstOrDefault((HurtBox hurtBox) => !this.previousTargets.Contains(hurtBox.healthComponent));
        }

        private void ClearList()
        {
            this.previousTargets.Clear();
        }

        private void OnHitGroundAuthority(ref CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            detonateNextFrame = true;
        }

        public override void OnExit()
        {
            base.OnExit();

            rotator.ResetRotation(0.2f);
            this.handle.Fire(0.5f, 1f);
            SetAntiGravity(false);
            EndFlight();

            if (NetworkServer.active)
            {
                base.characterBody.RemoveBuff(JunkContent.Buffs.IgnoreFallDamage.buffIndex);
            }
            if (base.isAuthority)
            {
                base.characterMotor.onHitGroundAuthority -= OnHitGroundAuthority;
            }
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }

        private Vector3 GetIdealVelocity(bool useGravity = true)
        {
            if (!isInFlight)
            {
                return this.idealDirection * base.characterBody.moveSpeed * endingSurgeSpeed;
            }

            Vector3 idealVelocity = this.idealDirection * base.characterBody.moveSpeed * 
                Util.Remap(1 - ((base.fixedAge - windupDuration) / flightDuration), 0, 1, endingSurgeSpeed, startingSurgeSpeed);
                //FlyUpState.speedCoefficientCurve.Evaluate(Mathf.Clamp((base.fixedAge - windupDuration) / flightDuration, 0.3f, 0.8f));
            if (!useGravity)
                return idealVelocity;

            float timeSinceAntiGravEnd = fixedAge - (windupDuration + antiGravityDuration);
            float gravityFactor = Mathf.Max(0, timeSinceAntiGravEnd) * gravityStrengthOverTime;

            return idealVelocity + gravityFactor * Vector3.down;
        }
        private float GetDamageBoostFromSpeed()
        {
            return 1;
            return Mathf.Max(1f, base.characterBody.moveSpeed / base.characterBody.baseMoveSpeed);
        }
        public void OnSurgeImpact()
        {
            EndFlight();
            this.outer.SetNextState(new SurgeExtendedImpact
            {
                idealDirection = this.idealDirection,
                damageBoostFromSpeed = this.GetDamageBoostFromSpeed(),
                isCrit = isCrit
            });
        }
        private void UpdateDirection()
        {
            if (base.inputBank)
            {
                Vector3 vector = base.inputBank.aimDirection;
                if (vector != Vector3.zero)
                {
                    vector.Normalize();
                    this.idealDirection = Vector3.Lerp(this.idealDirection, vector, (1 / baseSurgeDrag) * base.GetDeltaTime());
                }
            }

            if (isInFlight && this.idealDirection != null)
            {
                this.rotator.SetRotation(Quaternion.LookRotation(this.idealDirection, Vector3.up), base.GetDeltaTime());
            }
            else
            {
                this.rotator.SetRotation(Quaternion.LookRotation(this.characterMotor.velocity.normalized, Vector3.up), base.GetDeltaTime());
            }
        }

        #region substate changes
        public void BeginCharging()
        {
            base.characterMotor.useGravity = false;
            SetAntiGravity(true);

            if (windupDuration <= 0)
            {
                BeginFlight();
            }
            else
            {
                base.characterMotor.velocity = Vector3.zero;
                base.characterBody.previousPosition = base.characterBody.transform.position;
                base.characterBody.notMovingStopwatch = 1;
                base.PlayAnimation("Gesture, Additive", PrepWall.PrepWallStateHash, PrepWall.PrepWallParamHash, this.windupDuration);
            }
        }

        private void SetAntiGravity(bool notGravity)
        {
            if(notGravity == true)
            {
                base.characterMotor.velocity *= 0.5f;
                //base.characterMotor.velocity.y = 0;
            }
            isAntiGravity = notGravity;
        }

        /// <summary>
        /// Begins the flight state and sets initial flight properties
        /// </summary>
        public void BeginFlight()
        {
            if (hasStartedFlight)
                return;
            base.characterMotor.disableAirControlUntilCollision = false;
            hasStartedFlight = true;
            isInFlight = true;
            Util.PlaySound(FlyUpState.beginSoundString, base.gameObject);
            base.PlayAnimation("Gesture, Additive", "FireWall");
            base.PlayCrossfade("Body", "FlyUp", "FlyUp.playbackRate", flightDuration, 0.1f);
            //EffectManager.SimpleMuzzleFlash(FlyUpState.muzzleflashEffect, base.gameObject, "MuzzleLeft", false);
            //EffectManager.SimpleMuzzleFlash(FlyUpState.muzzleflashEffect, base.gameObject, "MuzzleRight", false);

            Transform muzzleTransformL = base.FindModelChild("MuzzleLeft");
            if (muzzleTransformL)
                surgeTrailEffectInstanceL = UnityEngine.Object.Instantiate<GameObject>(ArtificerExtendedPlugin.muzzleflashIonSurgeTrail, muzzleTransformL);
            Transform muzzleTransformR = base.FindModelChild("MuzzleRight");
            if (muzzleTransformR)
                surgeTrailEffectInstanceR = UnityEngine.Object.Instantiate<GameObject>(ArtificerExtendedPlugin.muzzleflashIonSurgeTrail, muzzleTransformR);

            characterBody.AddBuff(ArtificerExtendedPlugin.ionSurgePower);
            if (model)
                model.forceUpdate = true;

            if (base.isAuthority)
            {
                if (base.inputBank)
                {
                    this.idealDirection = base.inputBank.aimDirection;
                    if (base.characterMotor.isGrounded)
                    {
                        this.idealDirection += Vector3.up * surgeJumpFactor;
                        this.idealDirection.Normalize();
                    }
                }
                this.UpdateDirection();

                base.characterMotor.onHitGroundAuthority += OnHitGroundAuthority;
            }
            if (NetworkServer.active)
            {
                base.characterBody.AddBuff(JunkContent.Buffs.IgnoreFallDamage.buffIndex);
            }
            AddSkillCast();
            base.characterMotor.Motor.ForceUnground(minFlightDuration);
        }

        private void AddSkillCast()
        {
            skillCastTimer += skillCastInterval;
            if (AltArtiPassive.instanceLookup.TryGetValue(base.outer.gameObject, out var passive))
            {
                passive.SkillCast(handle);
            }
        }

        public void EndFlight()
        {
            if(characterBody.HasBuff(ArtificerExtendedPlugin.ionSurgePower))
                characterBody.RemoveBuff(ArtificerExtendedPlugin.ionSurgePower);
            if (model)
                model.forceUpdate = true;

            if (surgeTrailEffectInstanceL)
                Destroy(surgeTrailEffectInstanceL);
            if (surgeTrailEffectInstanceR)
                Destroy(surgeTrailEffectInstanceR);

            if (isInFlight && !detonateNextFrame)
            {
                base.healthComponent.TakeDamageForce(GetIdealVelocity(false), true, false);
                //base.characterMotor.velocity = GetIdealVelocity();
            }
            isInFlight = false;
            base.characterMotor.useGravity = true;
        }
        #endregion
    }
}
