﻿using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage;
using EntityStates.Toolbot;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.EntityState
{
    class PolarVortexBase : GenericCharacterMain, ISkillState
    {
        internal bool continuing = false;
        internal bool addedFallImmunity = false;
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
                //clear buffs
                while (characterBody.HasBuff(_1FrostbiteSkill.artiIceShield))
                    characterBody.RemoveBuff(_1FrostbiteSkill.artiIceShield);

                //clear spiral projectiles

                if (addedFallImmunity)
                {
                    base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
                }
            }
        }

        protected void StopSkills()
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
            EffectManager.SpawnEffect(Frostbite.novaEffectPrefab, new EffectData
            {
                origin = base.transform.position,
                scale = Frostbite.blizzardRadius
            }, true);
            BlastAttack blastAttack = new BlastAttack();
            blastAttack.radius = Frostbite.blizzardRadius;
            blastAttack.procCoefficient = Frostbite.blizzardProcCoefficient;
            blastAttack.position = base.transform.position;
            blastAttack.attacker = base.gameObject;
            blastAttack.crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
            blastAttack.baseDamage = base.characterBody.damage * Frostbite.blizzardDamageCoefficient;
            blastAttack.falloffModel = BlastAttack.FalloffModel.None;
            blastAttack.damageType = DamageType.Generic;
            blastAttack.baseForce = 1500;
            blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
            blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;

            blastAttack.AddModdedDamageType(ChillRework.ChillRework.ChillOnHit);

            blastAttack.Fire();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
    }
}