using System;
using System.Collections.Generic;
using System.Text;
using AltArtificerExtended.Passive;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace AltArtificerExtended.EntityState
{
    public class FireSnowBall : FireFireBolt, SteppedSkillDef.IStepSetter
    {
        public static float damageCoeff = Mathf.Ceil((Main.artiBoltDamage* 0.8f) * 10) / 10;
        public override void OnEnter()
        {
            this.projectilePrefab = Resources.Load<GameObject>("prefabs/projectiles/MageIceBolt");
            this.muzzleflashEffectPrefab = Resources.Load<GameObject>("prefabs/effects/MuzzleflashMageIceLarge");
            this.damageCoefficient = damageCoeff;
            this.baseDuration = 0.55f;
            this.attackSoundString = "Play_mage_shift_wall_build";
            this.attackSoundPitch = 10;
            base.OnEnter();

            /*GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                passive.SkillCast();
            }*/
        }
    }
}
