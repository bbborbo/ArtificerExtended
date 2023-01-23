using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using EntityStates.Mage.Weapon;
using EntityStates.Commando.CommandoWeapon;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;
using ArtificerExtended.Skills;
using System.Runtime.CompilerServices;
using RiskyMod.Survivors.Mage.Components;
//using AlternativeArtificer.States.Main;

namespace ArtificerExtended.EntityState
{
    class FireIceShard : BaseSkillState, SteppedSkillDef.IStepSetter
    {
        public static GameObject effectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/MuzzleflashMageLightningLarge");
        public static GameObject hitEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/HitsparkCommandoShotgun");
        public GameObject muzzleflashEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/MuzzleflashMageIceLarge");
        public static float damageCoefficient = ArtificerExtendedPlugin.artiBoltDamage + 2f;
        public static float procCoefficientPoint = 0.5f;
        public static float procCoefficientSpread = 0.5f;
        public static float procCoefficientBuckshot = 0.7f;
        public static float bulletRadius = 0.15f;
        public static float maxRange = ArtificerExtendedPlugin.meleeRangeSingle;
        public static float force = 0f;
        private int bulletCount;
        public static int bulletCountPoint = 1;
        public static int bulletCountSpread = 2;
        public static int bulletCountBuckshot = 3;

        public float baseDuration = 0.3f;
        private float duration;
        public static float attackSpeedAltAnimationThreshold = FireFireBolt.attackSpeedAltAnimationThreshold;

        public string attackSoundString = "Play_mage_m2_iceSpear_shoot";
        public string attackSoundString2 = "Play_mage_shift_wall_build";

        public static float recoilAmplitude = 3.25f;
        public static float spreadAmplitude = 1.2f;
        public static float spreadBloomValue = 0.3f;
        public static float spreadShotFraction = 0.4f;

        public static float bloom = 0;
        private bool hasFiredGauntlet;

        private Animator animator;
        private string muzzleString;
        private Transform muzzleTransform;
        private FireIceShard.Gauntlet gauntlet;
        private ChildLocator childLocator;
        public enum Gauntlet
        {
            Left,
            Right
        }

        //everything else
        public void SetStep(int i)
        {
            this.gauntlet = (FireIceShard.Gauntlet)i;
        }

        public override void OnEnter()
        {
            base.OnEnter();

            if (ArtificerExtendedPlugin.isRiskyModLoaded)
                FireSkill();

            bulletCount = bulletCountPoint + bulletCountSpread + bulletCountBuckshot;

            base.AddRecoil(-1f * FireIceShard.recoilAmplitude, -2f * FireIceShard.recoilAmplitude, -0.5f * FireIceShard.recoilAmplitude, 0.5f * FireIceShard.recoilAmplitude);
            base.characterBody.AddSpreadBloom(FireIceShard.spreadBloomValue);
            this.duration = this.baseDuration / this.attackSpeedStat;
            Util.PlaySound(this.attackSoundString, base.gameObject);
            Util.PlaySound(this.attackSoundString2, base.gameObject);
            base.characterBody.SetAimTimer(2f);
            this.animator = base.GetModelAnimator();
            if (this.animator)
            {
                this.childLocator = this.animator.GetComponent<ChildLocator>();
            }
            FireIceShard.Gauntlet gauntlet = this.gauntlet;
            if (gauntlet != FireIceShard.Gauntlet.Left)
            {
                if (gauntlet != FireIceShard.Gauntlet.Right)
                {
                    return;
                }
                this.muzzleString = "MuzzleRight";
                if (this.attackSpeedStat < FireIceShard.attackSpeedAltAnimationThreshold)
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
                if (this.attackSpeedStat < FireIceShard.attackSpeedAltAnimationThreshold)
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
        public static bool hasAssignedToRiskyModReload = false;
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void FireSkill()
        {
            if (!hasAssignedToRiskyModReload)
            {
                MageStockController.StatePairs.Add(typeof(FireIceShard), MageStockController.iceMuzzleflashEffectPrefab);
                hasAssignedToRiskyModReload = true;
            }

            if (hasAssignedToRiskyModReload)
            {
                var msc = this.gameObject.GetComponent<MageStockController>();
                if (msc)
                {
                    msc.FireSkill(this.duration);
                }
            }
        }
        private void FireGauntlet()
        {
            if (this.hasFiredGauntlet)
            {
                return;
            }


            /*GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                passive.SkillCast();
            }*/

            base.characterBody.AddSpreadBloom(FireIceShard.bloom);
            this.hasFiredGauntlet = true;
            Ray aimRay;
            if (VRStuff.VRInstalled)
            {
                this.muzzleString = "MuzzleRight";
                aimRay = VRStuff.GetVRHandAimRay(true);
                VRStuff.AnimateVRHand(true, "Cast");
            } 
            else
                aimRay = base.GetAimRay();
            if (this.childLocator)
            {
                this.muzzleTransform = this.childLocator.FindChild(this.muzzleString);
            }
            if (this.muzzleflashEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(this.muzzleflashEffectPrefab, base.gameObject, this.muzzleString, false);
            }
            base.StartAimMode(aimRay, 2f, false);/*
            if (FireIceShard.effectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(FireIceShard.effectPrefab, base.gameObject, this.muzzleString, false);
            }*/
            if (base.isAuthority)
            {
                float baseSpread = base.characterBody.spreadBloomAngle * spreadAmplitude;
                bool crit = Util.CheckRoll(this.critStat, base.characterBody.master);
                //point
                new BulletAttack
                {
                    damageType = DamageType.SlowOnHit,
                    owner = base.gameObject,
                    weapon = base.gameObject,
                    origin = aimRay.origin,
                    aimVector = aimRay.direction,
                    minSpread = 0f,
                    maxSpread = 0f,
                    bulletCount = (uint)((FireIceShard.bulletCountPoint > 0) ? FireIceShard.bulletCountPoint : 0),
                    procCoefficient = FireIceShard.procCoefficientPoint,
                    damage = FireIceShard.damageCoefficient * this.damageStat / bulletCount,
                    force = FireIceShard.force,
                    falloffModel = BulletAttack.FalloffModel.None,
                    tracerEffectPrefab = _1IceShardsSkill.tracerShotgun,
                    muzzleName = muzzleString,
                    hitEffectPrefab = hitEffectPrefab,
                    isCrit = crit,
                    HitEffectNormal = false,
                    radius = bulletRadius,
                    maxDistance = maxRange * 1.1f
                }.Fire();
                //spread
                new BulletAttack
                {
                    damageType = DamageType.SlowOnHit,
                    owner = base.gameObject,
                    weapon = base.gameObject,
                    origin = aimRay.origin,
                    aimVector = aimRay.direction,
                    minSpread = baseSpread * 0.1f,
                    maxSpread = baseSpread * spreadShotFraction,
                    bulletCount = (uint)((FireIceShard.bulletCountSpread > 0) ? FireIceShard.bulletCountSpread : 0),
                    procCoefficient = FireIceShard.procCoefficientSpread,
                    damage = FireIceShard.damageCoefficient * this.damageStat / bulletCount,
                    force = FireIceShard.force,
                    falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                    tracerEffectPrefab = _1IceShardsSkill.tracerShotgun,
                    muzzleName = muzzleString,
                    hitEffectPrefab = FireIceShard.hitEffectPrefab,
                    isCrit = crit,
                    HitEffectNormal = false,
                    radius = bulletRadius,
                    maxDistance = maxRange
                }.Fire();
                //buckshot
                int a =(int)UnityEngine.Random.Range(1, 5);
                float buckshotcount = (float)a / 2;

                new BulletAttack
                {
                    damageType = DamageType.Generic,
                    owner = base.gameObject,
                    weapon = base.gameObject,
                    origin = aimRay.origin,
                    aimVector = aimRay.direction,
                    minSpread = baseSpread * 0.5f,
                    maxSpread = baseSpread,
                    bulletCount = (uint)((FireIceShard.bulletCountBuckshot > 0) ? FireIceShard.bulletCountBuckshot : 0),
                    procCoefficient = FireIceShard.procCoefficientBuckshot,
                    damage = (FireIceShard.damageCoefficient * this.damageStat) / bulletCount,
                    force = FireIceShard.force,
                    falloffModel = BulletAttack.FalloffModel.Buckshot,
                    tracerEffectPrefab = _1IceShardsSkill.tracerBuckshot,
                    muzzleName = muzzleString,
                    hitEffectPrefab = FireIceShard.hitEffectPrefab,
                    isCrit = crit,
                    HitEffectNormal = false,
                    radius = bulletRadius,
                    maxDistance = maxRange
                }.Fire();
            }
        }

        // Token: 0x060037D7 RID: 14295 RVA: 0x000300F3 File Offset: 0x0002E2F3
        public override void OnExit()
        {
            base.OnExit();
        }

        // Token: 0x060037D8 RID: 14296 RVA: 0x000ED2EC File Offset: 0x000EB4EC
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

        // Token: 0x060037D9 RID: 14297 RVA: 0x000ED341 File Offset: 0x000EB541
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
            this.gauntlet = (FireIceShard.Gauntlet)reader.ReadByte();
        }
    }
}
