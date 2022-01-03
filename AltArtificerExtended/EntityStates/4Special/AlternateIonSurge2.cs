using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using EntityStates.Mage;
using R2API;
using R2API.Utils;
using RoR2;

using UnityEngine;
using UnityEngine.Networking;
using AltArtificerExtended.Passive;

namespace AltArtificerExtended.EntityState
{
    public class AlternateIonSurge2 : GenericCharacterMain
    {
        // TODO: Disable jumping during surge
        private const Int32 steps = 3;
        private const Single stepHalt = 0.5f;
        private const Single dashTime = 0.35f;
        private const Single speedConst = 0.25f;

        private Int32 stepCounter = 0;

        private Single duration;
        private Single stepTime;
        private readonly Single[] stepTimes = new Single[steps + 1];

        private Boolean halting = true;
        private Boolean haltingFirst = false;

        private Vector3 flyVector;

        private Transform inputSpace;

        private Components.Rotator rotator;
        private Transform modelTransform;
        private AltArtiPassive passive;
        private readonly AltArtiPassive.BatchHandle[] handles = new AltArtiPassive.BatchHandle[steps];

        public override void OnEnter()
        {
            base.OnEnter();

            this.inputSpace = new GameObject("inputSpace").transform;
            this.inputSpace.position = Vector3.zero;
            this.inputSpace.rotation = Quaternion.identity;
            this.inputSpace.localScale = Vector3.one;

            this.stepTime = stepHalt + dashTime;
            this.duration = (this.stepTime * steps) - stepHalt;
            for (Int32 i = 0; i < steps; i++)
            {
                this.stepTimes[i] = (this.stepTime * i) - stepHalt;
            }
            this.stepTimes[steps] = this.duration;
            this.stepTime = (this.duration / steps) - stepHalt;

            this.modelTransform = base.GetModelTransform();
            this.rotator = this.modelTransform.Find("MageArmature").GetComponent<Components.Rotator>();
            if(rotator == null)
                this.rotator = this.modelTransform.Find("MageArmature").gameObject.AddComponent<Components.Rotator>();

            //base.characterMotor.useGravity = false;
            base.cameraTargetParams.aimMode = CameraTargetParams.AimType.Aura;

            if (AltArtiPassive.instanceLookup.ContainsKey(base.gameObject))
            {
                this.passive = AltArtiPassive.instanceLookup[base.gameObject];
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            bool earlyCancel = stepCounter > 0 && inputBank.skill4.justPressed;
            
            if ((base.fixedAge >= this.duration || earlyCancel) && base.isAuthority)
            {
                base.outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            this.rotator.ResetRotation(0.5f);
            //Reflection.SetPropertyValue<float>(base.characterMotor, "useGravity", true);
            base.cameraTargetParams.aimMode = CameraTargetParams.AimType.Standard;

            if (this.inputSpace)
            {
                UnityEngine.Object.Destroy(this.inputSpace.gameObject);
            }

            if (NetworkServer.active && !base.characterBody.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage))
            {
                base.characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                base.characterMotor.onHitGround += this.CharacterMotor_onHitGround;
            }

            for (Int32 i = 0; i < this.handles.Length; i++)
            {
                if (this.handles[i] != null)
                {
                    this.handles[i].Fire(0f, 0f);
                }
            }

            base.OnExit();
        }

        private void CharacterMotor_onHitGround(ref CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            if (base.characterBody.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage))
            {
                base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            }

            // TODO: May need to redo the flag assignment?

            base.characterMotor.onHitGround -= this.CharacterMotor_onHitGround;
        }

        public override void HandleMovements()
        {
            base.HandleMovements();

            if (this.halting)
            {
                if (this.haltingFirst && base.fixedAge >= this.stepTimes[this.stepCounter] + stepHalt)
                {
                    //base.passive.SkillCast( skillLocator.special );
                    this.haltingFirst = false;
                }
                if (base.fixedAge >= this.stepTimes[this.stepCounter] + stepHalt)
                {
                    Vector3 aimDir = base.GetAimRay().direction;
                    Vector3 moveDir = this.inputBank.moveVector;
                    var aimOrientation = new Vector3(aimDir.x, 0f, aimDir.z);
                    aimOrientation = Vector3.Normalize(aimOrientation);
                    this.inputSpace.rotation = Quaternion.LookRotation(aimOrientation, Vector3.up);

                    aimDir = this.inputSpace.InverseTransformDirection(aimDir);
                    moveDir = this.inputSpace.InverseTransformDirection(moveDir);

                    Vector3 forward;
                    if (moveDir.sqrMagnitude != 0)
                    {
                        forward = aimDir * moveDir.z;
                        forward.x = moveDir.x;

                    }
                    else
                    {
                        forward = aimDir;
                    }

                    forward.y += this.inputBank.jump.down ? 2f : 0f;
                    forward = Vector3.Normalize(forward);
                    this.flyVector = this.inputSpace.TransformDirection(forward);

                    _ = Util.PlaySound(FlyUpState.beginSoundString, base.gameObject);
                    this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
                    base.PlayCrossfade("Body", "FlyUp", "FlyUp.playbackRate", dashTime, 0.1f);
                    base.characterMotor.Motor.ForceUnground();
                    base.characterMotor.velocity = Vector3.zero;
                    EffectManager.SimpleMuzzleFlash(FlyUpState.muzzleflashEffect, base.gameObject, "MuzzleLeft", false);
                    EffectManager.SimpleMuzzleFlash(FlyUpState.muzzleflashEffect, base.gameObject, "MuzzleRight", false);

                    this.rotator.SetRotation(Quaternion.LookRotation(this.flyVector, Vector3.up), dashTime);

                    base.characterBody.isSprinting = true;
                    this.halting = false;
                    this.stepCounter++;
                    GenericSkill[] skills = base.characterBody.skillLocator.allSkills;
                    foreach(GenericSkill skill in skills)
                    {
                        if (skill.stock < skill.maxStock)
                        {
                            skill.rechargeStopwatch += 0.5f;

                            if (skill.rechargeStopwatch >= skill.finalRechargeInterval)
                            {
                                skill.RestockSteplike();
                            }
                        }
                    }

                    this.handles[this.stepCounter - 1] = new AltArtiPassive.BatchHandle();
                    if (this.passive != null)
                    {
                        this.passive.SkillCast(this.handles[this.stepCounter - 1]);
                    }
                }
            }
            else
            {
                if (base.fixedAge >= this.stepTimes[this.stepCounter])
                {
                    if (this.stepCounter < steps)
                    {
                        this.halting = true;
                        _ = Util.PlaySound(FlyUpState.endSoundString, base.gameObject);
                        this.rotator.ResetRotation(stepHalt);
                        this.haltingFirst = true;
                        base.characterBody.isSprinting = false;
                    }
                }
                Single speedCoef = base.moveSpeedStat * speedConst * FlyUpState.speedCoefficientCurve.Evaluate((base.fixedAge - this.stepTimes[this.stepCounter]) / dashTime) / dashTime;
                base.characterMotor.rootMotion += this.flyVector * speedCoef * Time.fixedDeltaTime;

            }
            base.characterMotor.velocity.y = 0f;
        }

        //public override void UpdateAnimationParameters() => base.UpdateAnimationParameters();

        private void CreateBlinkEffect(Vector3 origin)
        {
            var data = new EffectData
            {
                rotation = Util.QuaternionSafeLookRotation(this.flyVector),
                origin = origin
            };
            EffectManager.SpawnEffect(FlyUpState.blinkPrefab, data, false);
        }
    }
}
