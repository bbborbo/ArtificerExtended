using ArtificerExtended.Components;
using ArtificerExtended.Passive;
using EntityStates;
using EntityStates.Mage;
using JetHack;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.EntityState
{
    class SurgeExtendedImpact : BaseCharacterMain
    {
        public static float baseDuration = 0.1f;
        float duration;

        public Vector3 idealDirection;
        public float damageBoostFromSpeed;
        public bool isCrit;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / this.attackSpeedStat;

            if (NetworkServer.active)
            {
                BlastAttack blastAttack = new BlastAttack
                {
                    radius = SurgeExtendedDash.impactRadius,
                    attacker = base.gameObject,
                    baseDamage = this.damageStat * SurgeExtendedDash.impactDamageCoefficient * this.damageBoostFromSpeed,
                    crit = this.isCrit,
                    procCoefficient = 1f,
                    damageColorIndex = DamageColorIndex.Default,
                    damageType = new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.Generic, DamageSource.Special),
                    position = base.characterBody.corePosition,
                    falloffModel = BlastAttack.FalloffModel.None,
                    baseForce = SurgeExtendedDash.impactBlastForce,
                    teamIndex = TeamComponent.GetObjectTeam(base.gameObject)
                };
                blastAttack.Fire();

                base.healthComponent.TakeDamageForce(/*this.idealDirection*/Vector3.down * -SurgeExtendedDash.impactKnockbackForce, true, false);
                //base.SmallHop(base.characterMotor, 6f);
            }
            if (base.isAuthority)
            {
                base.AddRecoil(-0.5f * SurgeExtendedDash.impactRecoilAmplitude * 3f, 
                    -0.5f * SurgeExtendedDash.impactRecoilAmplitude * 3f, 
                    -0.5f * SurgeExtendedDash.impactRecoilAmplitude * 8f, 
                    0.5f * SurgeExtendedDash.impactRecoilAmplitude * 3f);
                CreateBlinkEffect(base.characterBody.corePosition);
            }
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(base.fixedAge >= duration)
            {
                outer.SetNextStateToMain();
            }
        }
        public override void OnExit()
        {
            base.OnExit();
            if (isAuthority)
            {
                JetHackPlugin.hoverStateCache = true;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Frozen;
        }
        private void CreateBlinkEffect(Vector3 origin)
        {
            EffectData effectData = new EffectData();
            effectData.rotation = Util.QuaternionSafeLookRotation(this.idealDirection);
            effectData.origin = origin;
            EffectManager.SpawnEffect(FlyUpState.blinkPrefab, effectData, false);
        }

        public override void OnSerialize(NetworkWriter writer)
        {
            base.OnSerialize(writer);
            writer.Write(this.idealDirection);
            writer.Write(this.damageBoostFromSpeed);
            writer.Write(this.isCrit);
        }

        public override void OnDeserialize(NetworkReader reader)
        {
            base.OnDeserialize(reader);
            GameObject gameObject = reader.ReadGameObject();
            this.idealDirection = reader.ReadVector3();
            this.damageBoostFromSpeed = reader.ReadSingle();
            this.isCrit = reader.ReadBoolean();
        }
    }
}
