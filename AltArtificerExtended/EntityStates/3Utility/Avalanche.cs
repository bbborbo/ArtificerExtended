using AltArtificerExtended.Passive;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AltArtificerExtended.EntityState
{
    class Avalanche : BaseSkillState
    {
        public static float damageCoefficient = 10f;

        public float minRadius = 6;
        public float maxRadius = 25;
        float endHopVelocity = 10;

        public float downForce = -12.5f;
        float downForceScaled = 0;
        bool isFalling = false;
        float fallTimer = 0;
        public float fallTimerMax = 1f;
        float fallTimerMaxScaled;

        public override void OnEnter()
        {
            base.OnEnter();

            this.fallTimer = 0;
            this.fallTimerMaxScaled = this.fallTimerMax / (this.moveSpeedStat / 7);
            this.downForceScaled = downForce * this.moveSpeedStat;

            base.characterMotor.disableAirControlUntilCollision = false;

            if (NetworkServer.active && !base.characterBody.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage))
            {
                base.characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                base.characterMotor.onHitGround += this.CharacterMotor_onHitGround;
            }
        }

        private void CharacterMotor_onHitGround(ref CharacterMotor.HitGroundInfo hitGroundInfo)
        {
            if (base.characterBody.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage))
            {
                base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            }

            // TODO: May need to redo the flag assignment?

            base.characterMotor.onHitGround -= this.CharacterMotor_onHitGround;
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.isAuthority)
            {
                base.characterMotor.velocity.y = this.downForceScaled;
            }
            if(base.characterMotor.velocity.y <= this.downForce)
            {
                isFalling = true;


                fallTimer += Time.fixedDeltaTime;
                if (fallTimer > fallTimerMaxScaled)
                    fallTimer = fallTimerMaxScaled;
            }
            //Debug.Log(isFalling);
            bool flag = base.characterMotor.isGrounded;
            bool flag2 = !base.IsKeyDownAuthority();

            if (flag || flag2)
            {
                if(isFalling == true && fallTimer >= Time.fixedDeltaTime)
                {
                    base.characterMotor.velocity *= 0.1f;
                    base.SmallHop(base.characterMotor, endHopVelocity);
                    if (base.isAuthority)
                    {
                        CastNova(fallTimer);
                    }
                }
                else
                    base.skillLocator.GetSkill(skillLocator.FindSkillSlot(base.activatorSkillSlot)).AddOneStock();

                this.outer.SetNextStateToMain();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void SkillCast()
        {
            GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                passive.SkillCast();
            }
        }

        public void CastNova(float fallDuration)
        {
            GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                passive.SkillCast();
            }

            //Util.PlaySound(PrepWall.prepWallSoundString, base.gameObject);
            float radius = Util.Remap(fallDuration, 0, fallTimerMaxScaled, minRadius, maxRadius);
            //Debug.Log(fallDuration);
            //Debug.Log(radius);

            EffectManager.SpawnEffect(EntityState.Frostbite.novaEffectPrefab, new EffectData
            {
                origin = base.transform.position,
                scale = radius
            }, true);
            BlastAttack blastAttack = new BlastAttack();
            blastAttack.radius = radius;
            blastAttack.procCoefficient = 1;
            blastAttack.position = base.transform.position;
            blastAttack.attacker = base.gameObject;
            blastAttack.crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
            blastAttack.baseDamage = this.damageStat * Avalanche.damageCoefficient;
            blastAttack.falloffModel = BlastAttack.FalloffModel.Linear;
            blastAttack.damageType = DamageType.Freeze2s;
            blastAttack.baseForce = 0;
            blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
            blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
            blastAttack.Fire();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}
