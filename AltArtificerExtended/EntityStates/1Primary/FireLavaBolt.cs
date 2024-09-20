using ArtificerExtended.Skills;
using EntityStates.Mage.Weapon;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.EntityState
{
    class FireLavaBolt : FireFireBolt, SteppedSkillDef.IStepSetter
    {
        public override void OnEnter()
        {
            this.projectilePrefab = _2LavaBoltsSkill.lavaProjectilePrefab;
            this.muzzleflashEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/MuzzleflashMageIceLarge");
            this.damageCoefficient = _2LavaBoltsSkill.impactDamageCoefficient;
            this.baseDuration = _2LavaBoltsSkill.baseDuration;
            this.attackSoundString = "Play_mage_shift_wall_build";
            this.attackSoundPitch = 10;
            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(true, "Cast");
            base.OnEnter();
        }
    }
}
