using AltArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AltArtificerExtended.EntityState
{
    class FireLaserbolts : BaseSkillState, SteppedSkillDef.IStepSetter
    {
        public GameObject muzzleflashEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/MuzzleflashMageLightningLarge");

        public float procCoefficient = 1.0f;

        public static float damageCoefficient = ArtificerExtendedPlugin.artiBoltDamage;

        public float force = 20f;

        public static float attackSpeedAltAnimationThreshold = FireFireBolt.attackSpeedAltAnimationThreshold;

        public float baseDuration = 0.45f;

        public string attackSoundString = FireLaserbolt.attackString;
        public string attackSoundString2 = "Play_mage_m2_impact";

        private float duration;

        private bool hasFiredGauntlet = false;

        private string muzzleString;

        private Transform muzzleTransform;

        private Animator animator;

        private ChildLocator childLocator;

        private Gauntlet gauntlet;
        public static float baseRecoilAmplitude = 1.6f;
        public  static float maxRange = 45;

        public enum Gauntlet
        {
            Left,
            Right
        }
        public void SetStep(int i)
        {
            this.gauntlet = (Gauntlet)i;
        }

        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / this.attackSpeedStat;
            Util.PlaySound(this.attackSoundString, base.gameObject);
            Util.PlaySound(this.attackSoundString2, base.gameObject);
            float recoil = FireLaserbolts.baseRecoilAmplitude / this.attackSpeedStat;
            base.AddRecoil(-recoil, -2f * recoil, -recoil, recoil);
            base.characterBody.SetAimTimer(2f);
            this.animator = base.GetModelAnimator();
            if (this.animator)
            {
                this.childLocator = this.animator.GetComponent<ChildLocator>();
            }
            Gauntlet gauntlet = this.gauntlet;
            if (gauntlet != Gauntlet.Left)
            {
                if (gauntlet != Gauntlet.Right)
                {
                    return;
                }
                this.muzzleString = "MuzzleRight";
                if (this.attackSpeedStat < FireFireBolt.attackSpeedAltAnimationThreshold)
                {
                    base.PlayCrossfade("Gesture, Additive", "Cast1Right", "FireGauntlet.playbackRate", this.duration, 0.1f);
                    base.PlayAnimation("Gesture Left, Additive", "Empty");
                    base.PlayAnimation("Gesture Right, Additive", "Empty");
                    return;
                }
                base.PlayAnimation("Gesture Right, Additive", "FireGauntletRight", "FireGauntlet.playbackRate", this.duration);
                base.PlayAnimation("Gesture, Additive", "HoldGauntletsUp", "FireGauntlet.playbackRate", this.duration);
                this.FireGauntlet();
                return;
            }
            else
            {
                this.muzzleString = "MuzzleLeft";
                if (this.attackSpeedStat < FireFireBolt.attackSpeedAltAnimationThreshold)
                {
                    base.PlayCrossfade("Gesture, Additive", "Cast1Left", "FireGauntlet.playbackRate", this.duration, 0.1f);
                    base.PlayAnimation("Gesture Left, Additive", "Empty");
                    base.PlayAnimation("Gesture Right, Additive", "Empty");
                    return;
                }
                base.PlayAnimation("Gesture Left, Additive", "FireGauntletLeft", "FireGauntlet.playbackRate", this.duration);
                base.PlayAnimation("Gesture, Additive", "HoldGauntletsUp", "FireGauntlet.playbackRate", this.duration);
                this.FireGauntlet();
                return;
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void FireGauntlet()
        {
            if (this.hasFiredGauntlet)
            {
                return;
            }
            //SetStep((int)gauntlet + 1);
            base.characterBody.AddSpreadBloom(FireFireBolt.bloom);
            this.hasFiredGauntlet = true;

            Ray aimRay = base.GetAimRay();
            if (this.childLocator)
            {
                this.muzzleTransform = this.childLocator.FindChild(this.muzzleString);
            }
            if (this.muzzleflashEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(this.muzzleflashEffectPrefab, base.gameObject, this.muzzleString, false);
            }
            if (base.isAuthority)
            {
                new BulletAttack
                {
                    owner = base.gameObject,
                    weapon = base.gameObject,
                    origin = aimRay.origin,
                    aimVector = aimRay.direction,
                    minSpread = 0f,
                    maxSpread = 0f,
                    damage = damageCoefficient * this.damageStat,
                    force = FireLaserbolt.force,
                    tracerEffectPrefab = _3LaserBoltsSkill.tracerLaser,
                    muzzleName = this.muzzleString,
                    hitEffectPrefab = FireLaserbolt.impactEffectPrefab,
                    isCrit = Util.CheckRoll(this.critStat, base.characterBody.master),
                    radius = 0.25f,
                    falloffModel = BulletAttack.FalloffModel.None,
                    //maxDistance = maxRange,
                    smartCollision = true
                }.Fire();
            }

            /*GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                passive.SkillCast();
            }*/
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (this.animator.GetFloat("FireGauntlet.fire") > 0f && !this.hasFiredGauntlet)
            {
                this.FireGauntlet();
            }
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write((byte)this.gauntlet);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            this.gauntlet = (Gauntlet)reader.ReadByte();
        }
    }
}
