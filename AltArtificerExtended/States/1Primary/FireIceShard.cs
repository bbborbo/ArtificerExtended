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
using static R2API.DamageAPI;
using ArtificerExtended.Passive;

namespace ArtificerExtended.States
{
    class FireIceShard : BaseSkillState, SteppedSkillDef.IStepSetter
    {
        public static GameObject effectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/MuzzleflashMageLightningLarge");
        public static GameObject hitEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/HitsparkCommandoShotgun");
        public GameObject muzzleflashEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/MuzzleflashMageIceLarge");
        public static float damageCoefficient = ArtificerExtendedPlugin.artiBoltDamage + 1.4f;
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


            GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                passive.SkillCast();
            }

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
                CreateIceShardSpread(aimRay, 0, 0, 
                    (uint)((FireIceShard.bulletCountPoint > 0) ? FireIceShard.bulletCountPoint : 1), 
                    procCoefficientPoint, BulletAttack.FalloffModel.None, crit, true).Fire();

                //spread
                CreateIceShardSpread(aimRay, baseSpread * 0.1f, baseSpread * spreadShotFraction, 
                    (uint)((FireIceShard.bulletCountSpread > 0) ? FireIceShard.bulletCountSpread : 0), 
                    procCoefficientSpread, BulletAttack.FalloffModel.DefaultBullet, crit, true).Fire();

                //buckshot
                CreateIceShardSpread(aimRay, baseSpread * 0.5f, baseSpread, 
                    (uint)((FireIceShard.bulletCountBuckshot > 0) ? FireIceShard.bulletCountBuckshot : 0), 
                    procCoefficientBuckshot, BulletAttack.FalloffModel.Buckshot, crit, false).Fire();
            }
        }
        internal BulletAttack CreateIceShardSpread(Ray aimRay, float minSpread, float maxSpread, 
            uint bulletsPerSpread, float procCoefficient, BulletAttack.FalloffModel falloffModel, bool isCrit, bool useChill = true)
        {
            BulletAttack bulletAttack = new BulletAttack();

            bulletAttack.damageType = DamageType.Generic;
            bulletAttack.owner = base.gameObject;
            bulletAttack.weapon = base.gameObject;
            bulletAttack.origin = aimRay.origin;
            bulletAttack.aimVector = aimRay.direction;
            bulletAttack.minSpread = minSpread;
            bulletAttack.maxSpread = maxSpread;
            bulletAttack.bulletCount = bulletsPerSpread;
            bulletAttack.procCoefficient = procCoefficient;
            bulletAttack.damage = FireIceShard.damageCoefficient * this.damageStat / bulletCount;
            bulletAttack.force = FireIceShard.force;
            bulletAttack.falloffModel = falloffModel;
            bulletAttack.tracerEffectPrefab = _4IceShardsSkill.tracerShotgun;
            bulletAttack.muzzleName = muzzleString;
            bulletAttack.hitEffectPrefab = hitEffectPrefab;
            bulletAttack.isCrit = isCrit;
            bulletAttack.HitEffectNormal = false;
            bulletAttack.radius = bulletRadius;
            bulletAttack.maxDistance = maxRange;
            bulletAttack.damageType = DamageTypeCombo.GenericPrimary;

            if(useChill)
                bulletAttack.AddModdedDamageType(ChillRework.ChillRework.ChillOnHit);

            return bulletAttack;
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (this.animator != null && this.animator.GetFloat("FireGauntlet.fire") > 0f && !this.hasFiredGauntlet)
            {
                this.FireGauntlet();
            }
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                if (!this.hasFiredGauntlet)
                {
                    this.FireGauntlet();
                }
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
            this.gauntlet = (FireIceShard.Gauntlet)reader.ReadByte();
        }
    }
}
