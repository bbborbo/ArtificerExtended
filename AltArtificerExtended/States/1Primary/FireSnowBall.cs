using System;
using System.Collections.Generic;
using System.Text;
using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.States
{
    public class FireSnowBall : FireFireBolt, SteppedSkillDef.IStepSetter
    {
        public void SetStep(int i)
        {
            this.gauntlet = (Gauntlet)i;
        }
        public static float damageCoeff = _1SnowballsSkill.snowballBaseDamage;//Mathf.Ceil((ArtificerExtendedPlugin.artiBoltDamage * 0.8f) * 10) / 10;
        public override void OnEnter()
        {
            this.projectilePrefab = _1SnowballsSkill.snowballProjectilePrefab;
            this.muzzleflashEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/MuzzleflashMageIceLarge");
            this.damageCoefficient = damageCoeff;
            this.baseDuration = _1SnowballsSkill.snowballBaseDuration;
            this.attackSoundString = "Play_mage_shift_wall_build";
            this.attackSoundPitch = 10;
            if(VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(true, "Cast");
            this.muzzleString = this.gauntlet == Gauntlet.Left ? "MuzzleLeft" : "MuzzleRight";
            base.OnEnter();
            this.muzzleString = this.gauntlet == Gauntlet.Left ? "MuzzleLeft" : "MuzzleRight";
        }
        public override void ModifyProjectileInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            base.ModifyProjectileInfo(ref fireProjectileInfo);
            fireProjectileInfo.damageTypeOverride = new DamageTypeCombo(DamageType.Frost, DamageTypeExtended.Generic, DamageSource.Primary);
            if (muzzleTransform != null)
                fireProjectileInfo.position = muzzleTransform.position;
        }
    }
}
