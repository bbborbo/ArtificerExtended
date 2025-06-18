using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtificerExtended.States
{
    class FireLavaBolt : FireFireBolt, SteppedSkillDef.IStepSetter
    {
        public void SetStep(int i)
        {
            this.gauntlet = (Gauntlet)i;
        }
        public override void OnEnter()
        {
            this.projectilePrefab = _2LavaBoltsSkill.sloshProjectilePrefab;
            this.muzzleflashEffectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Junk_Mage.MuzzleflashMageFireLarge_prefab).WaitForCompletion();
            this.damageCoefficient = _2LavaBoltsSkill.damageCoefficient;
            this.baseDuration = _2LavaBoltsSkill.baseDuration;
            this.attackSoundString = "Play_mage_shift_wall_build";
            this.attackSoundPitch = 10;
            if (VRStuff.VRInstalled)
                VRStuff.AnimateVRHand(true, "Cast");
            this.muzzleString = this.gauntlet == Gauntlet.Left ? "MuzzleLeft" : "MuzzleRight";
            base.OnEnter();
            this.muzzleString = this.gauntlet == Gauntlet.Left ? "MuzzleLeft" : "MuzzleRight";
        }
        public override void ModifyProjectileInfo(ref FireProjectileInfo fireProjectileInfo)
        {
            base.ModifyProjectileInfo(ref fireProjectileInfo);
            if (muzzleTransform != null)
                fireProjectileInfo.position = muzzleTransform.position;
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            if (this.fixedAge <= this.duration * 0.2f)
                return InterruptPriority.PrioritySkill;
            return base.GetMinimumInterruptPriority();
        }
    }
}
