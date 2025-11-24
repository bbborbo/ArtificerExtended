using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage;
using EntityStates.Toolbot;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ArtificerExtended.States
{
    class PolarVortexBase : GenericCharacterMain, ISkillState
    {
        public static GameObject muzzleflashEffect => Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Mage.MuzzleflashMageIceLarge_prefab).WaitForCompletion();//FlyUpState.muzzleflashEffect;

        internal bool continuing = false;
        internal bool addedFallImmunity = false;
        internal bool crit = false;
        public GenericSkill activatorSkillSlot { get; set; }

        protected virtual void SetNextState()
        {
            continuing = false;
            outer.SetNextStateToMain();
        }
        public override void OnEnter()
        {
            base.OnEnter();

            if (NetworkServer.active && !base.characterBody.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage))
            {
                base.characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                addedFallImmunity = true;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.characterBody)
            {
                base.characterBody.SetAimTimer(1f);
                if (base.isAuthority && base.characterBody.HasBuff(DLC2Content.Buffs.DisableAllSkills.buffIndex))
                {
                    outer.SetNextStateToMain();
                }
            }
        }
        public override void OnExit()
        {
            base.OnExit();
            if (!continuing)
            {
                if (NetworkServer.active)
                {
                    //clear buffs
                    while (characterBody.HasBuff(_1FrostbiteSkill.artiIceShield))
                        characterBody.RemoveBuff(_1FrostbiteSkill.artiIceShield);
                }

                //clear spiral projectiles

                if (addedFallImmunity)
                {
                    base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
                }
            }
        }

        protected void StopHover()
        {
            if (base.isAuthority)
            {
                /*EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Weapon");
                if (entityStateMachine != null)
                {
                    entityStateMachine.SetNextStateToMain();
                }*/

                EntityStateMachine entityStateMachine2 = EntityStateMachine.FindByCustomName(base.gameObject, "Jet");
                if (entityStateMachine2 == null || entityStateMachine2.state.GetType() != typeof(JetpackOn))
                {
                    return;
                }
                entityStateMachine2.SetNextStateToMain();
            }
        }

        public void InflictSnow()
        {
            EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleLeft", false);
            EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleRight", false);

            float damage = characterBody.damage * _1FrostbiteSkill.blizzardDamageCoefficient;
            RainrotSharedUtils.Frost.FrostUtilsModule.CreateIceBlast(characterBody,
                FlyUpState.blastAttackForce, damage, _1FrostbiteSkill.blizzardProcCoefficient,
                _1FrostbiteSkill.blizzardRadius, this.crit, base.transform.position, true, DamageSource.Special);

            return;
            EffectManager.SpawnEffect(_1FrostbiteSkill.novaEffectPrefab, new EffectData
            {
                origin = base.transform.position,
                scale = _1FrostbiteSkill.blizzardRadius
            }, true);
            BlastAttack blastAttack = new BlastAttack();
            blastAttack.radius = _1FrostbiteSkill.blizzardRadius;
            blastAttack.procCoefficient = _1FrostbiteSkill.blizzardProcCoefficient;
            blastAttack.position = base.transform.position;
            blastAttack.attacker = base.gameObject;
            blastAttack.crit = crit;
            blastAttack.baseDamage = base.characterBody.damage * _1FrostbiteSkill.blizzardDamageCoefficient;
            blastAttack.falloffModel = BlastAttack.FalloffModel.None;
            blastAttack.damageType = new DamageTypeCombo(DamageTypeExtended.Generic, DamageTypeExtended.Frost, DamageSource.Special);
            blastAttack.baseForce = 1500;
            blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
            blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;

            blastAttack.Fire();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }
    }
}
