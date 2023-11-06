using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.EntityState
{
    public class FireSnowBall : FireFireBolt, SteppedSkillDef.IStepSetter
    {
        public static float damageCoeff = Mathf.Ceil((ArtificerExtendedPlugin.artiBoltDamage* 0.8f) * 10) / 10;
        public override void OnEnter()
        {
            this.projectilePrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIceBolt");
            this.muzzleflashEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/MuzzleflashMageIceLarge");
            this.damageCoefficient = damageCoeff;
            this.baseDuration = 0.55f;
            this.attackSoundString = "Play_mage_shift_wall_build";
            this.attackSoundPitch = 10;
            if(VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(true, "Cast");
            base.OnEnter();
        }
    }
}
