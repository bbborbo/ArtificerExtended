//using AlternativeArtificer.States.Main;

//using AltArtificerExtended.Passive;
using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Huntress;
using EntityStates.Mage;
using EntityStates.Mage.Weapon;
using EntityStates.Treebot.Weapon;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.EntityState
{
    public class CastShockwave : BaseState
    {
        public static GameObject impactEffectPrefab = IceNova.impactEffectPrefab;
        public static GameObject novaEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/AffixWhiteExplosion");
        public static GameObject areaIndicatorPrefab = ArrowRain.areaIndicatorPrefab;

        public static float totalDuration = 0.75f;
        public static float baseDuration = 0.1f;
        public static float speedCoefficient = 9f;

        private float stopwatch;
        private float duration;
        private float speed;

        private Ray blinkAimRay;
        private Transform modelTransform;
        public static GameObject blinkPrefab = BlinkState.blinkPrefab;
        private Vector3 blinkVector = Vector3.zero;
        public static string beginSoundString = FlyUpState.beginSoundString;
        public static string endSoundString = PrepWall.fireSoundString;
        private CharacterModel characterModel;
        private HurtBoxGroup hurtboxGroup;

        public override void OnEnter()
        {
            base.OnEnter();
            this.stopwatch = 0f;
            this.duration = CastShockwave.baseDuration / this.attackSpeedStat;
            this.speed = CastShockwave.speedCoefficient * this.attackSpeedStat;
            base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", CastShockwave.baseDuration);

            //blink
            Util.PlaySound(beginSoundString, base.gameObject);
            this.modelTransform = base.GetModelTransform();
            if (this.modelTransform)
            {
                this.characterModel = this.modelTransform.GetComponent<CharacterModel>();
                this.hurtboxGroup = this.modelTransform.GetComponent<HurtBoxGroup>();
            }
            if (this.characterModel)
            {
                this.characterModel.invisibilityCount++;
            }
            //THIS DOES IFRAMES
            /*if (this.hurtboxGroup)
            {
                HurtBoxGroup hurtBoxGroup = this.hurtboxGroup;
                int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
                hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
            }*/
            this.blinkVector = this.GetBlinkVector();
            this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
        }

        protected virtual Vector3 GetBlinkVector()
        {
            blinkAimRay = base.GetAimRay();
            return blinkAimRay.direction;
        }
        private void CreateBlinkEffect(Vector3 origin)
        {
            EffectData effectData = new EffectData();
            effectData.rotation = Util.QuaternionSafeLookRotation(this.blinkVector);
            effectData.origin = origin;
            EffectManager.SpawnEffect(MiniBlinkState.blinkPrefab, effectData, false);
        }

        public override void OnExit()
        {
            base.OnExit();
            if (!this.outer.destroying)
            {
                Util.PlaySound(beginSoundString, base.gameObject);
                this.modelTransform = base.GetModelTransform();
                if (this.modelTransform)
                {
                    TemporaryOverlay temporaryOverlay = this.modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                    temporaryOverlay.duration = 0.6f;
                    temporaryOverlay.animateShaderAlpha = true;
                    temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlay.destroyComponentOnEnd = true;
                    temporaryOverlay.originalMaterial = RoR2.LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashBright");
                    temporaryOverlay.AddToCharacerModel(this.modelTransform.GetComponent<CharacterModel>());
                    TemporaryOverlay temporaryOverlay2 = this.modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                    temporaryOverlay2.duration = 0.7f;
                    temporaryOverlay2.animateShaderAlpha = true;
                    temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlay2.destroyComponentOnEnd = true;
                    temporaryOverlay2.originalMaterial = RoR2.LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashExpanded");
                    temporaryOverlay2.AddToCharacerModel(this.modelTransform.GetComponent<CharacterModel>());
                }
            }
            if (this.characterModel)
            {
                this.characterModel.invisibilityCount--;
            }
            //THIS DOES IFRAMES
            /*if (this.hurtboxGroup)
            {
                HurtBoxGroup hurtBoxGroup = this.hurtboxGroup;
                int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
                hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
            }*/
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            if (this.stopwatch >= this.duration)
            {
                /*GameObject obj = base.outer.gameObject;
                if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
                {
                    passive.SkillCast();
                }*/

                base.PlayAnimation("Gesture, Additive", "FireWall");

                this.outer.SetNextState(this.GetNextState());
            }
            if (base.characterMotor && base.characterDirection)
            {
                base.characterMotor.velocity = Vector3.zero;
                base.characterMotor.rootMotion += this.blinkVector * (this.moveSpeedStat * this.speed * Time.fixedDeltaTime);
            }
        }
        protected virtual EntityStates.EntityState GetNextState()
        {
            float remainingDuration = (CastShockwave.totalDuration - CastShockwave.baseDuration) / this.attackSpeedStat;
            base.PlayAnimation("Gesture, Additive", "FireNovaBomb", "FireNovaBomb.playbackRate", remainingDuration - (0.2f / this.attackSpeedStat));
            return new FireShockwave()
            {
                baseDuration = remainingDuration,
                maxAngleFraction = (float)_2ShockwaveSkill.shockwaveMaxAngleFilter / 100,
                maxDistance = _2ShockwaveSkill.shockwaveMaxRange,
                burstAimRay = this.blinkAimRay
            };
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}
