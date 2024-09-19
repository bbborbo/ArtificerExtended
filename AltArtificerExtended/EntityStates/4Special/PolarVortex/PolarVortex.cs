using ArtificerExtended.Components;
using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage;
using EntityStates.Seeker;
using EntityStates.Toolbot;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.EntityState
{
    class PolarVortex : PolarVortexBase
    {
        SeekerController orbitProjectileManager;

        bool ending = false;
        public static float endingSpeedMultiplier = 10f;
        bool keyReleased;
        float armorAddStopwatch;
        float stopwatch;
        public override void OnEnter()
        {
            base.OnEnter();

            if (activatorSkillSlot && ToolbotDualWieldBase.cancelSkillDef != null)
            {
                activatorSkillSlot.SetSkillOverride(this, CancelFrostbiteSkill.instance.SkillDef, GenericSkill.SkillOverridePriority.Contextual);
            }

            // add ice armor
            AddIceArmorBuff();
            // create spiral projectiles
            if(!outer.gameObject.TryGetComponent(out orbitProjectileManager))
            {
                orbitProjectileManager = outer.gameObject.AddComponent<SeekerController>();
            }
            if(orbitProjectileManager != null)
            {
                FireProjectileInfo projectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = _1FrostbiteSkill.icicleProjectilePrefab,
                    owner = base.gameObject,
                    damage = this.damageStat * _1FrostbiteSkill.icicleDamage,
                    force = SoulSpiral.projectileForce,
                    position = base.characterBody.corePosition,
                    crit = Util.CheckRoll(this.critStat, base.characterBody.master)
                };
                orbitProjectileManager.FireSoulSpiral(projectileInfo);
                orbitProjectileManager.FireSoulSpiral(projectileInfo);
                orbitProjectileManager.FireSoulSpiral(projectileInfo);
            }
            //ProjectileManager.instance.FireProjectile(SoulSpiral.projectilePrefab, )
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority)
            {
                armorAddStopwatch += (ending ? Time.fixedDeltaTime * endingSpeedMultiplier : Time.fixedDeltaTime); 

                while (armorAddStopwatch > _1FrostbiteSkill.buffInterval)
                {
                    armorAddStopwatch -= _1FrostbiteSkill.buffInterval;
                    AddIceArmorBuff();

                    int buffCount = characterBody.GetBuffCount(_1FrostbiteSkill.artiIceShield);
                    if (buffCount >= _1FrostbiteSkill.maxBuffStacks)
                    {
                        if (!ending)
                            base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", 0.3f / this.attackSpeedStat);
                        this.SetNextState();
                        return;
                    }
                }

                if (!ending)
                {
                    bool flag = this.IsKeyDownAuthority(base.skillLocator, base.inputBank);
                    this.keyReleased |= !flag;
                    if (this.keyReleased && flag)
                    {
                        ending = true;
                        base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", 0.3f / this.attackSpeedStat);
                    }
                }
            }
        }

        void AddIceArmorBuff()
        {
            characterBody.AddBuff(_1FrostbiteSkill.artiIceShield);
        }

        public override void OnExit()
        {
            base.OnExit();
            if (activatorSkillSlot && ToolbotDualWieldBase.cancelSkillDef != null)
            {
                activatorSkillSlot.UnsetSkillOverride(this, CancelFrostbiteSkill.instance.SkillDef, GenericSkill.SkillOverridePriority.Contextual);
            }
            //clear spiral projectiles
            if(orbitProjectileManager != null)
            {
                Destroy(orbitProjectileManager);
            }
        }
        protected override void SetNextState()
        {
            continuing = true;
            outer.SetNextState(new PolarVortexStart
            {
                addedFallImmunity = this.addedFallImmunity,
                exiting = true,
                activatorSkillSlot = this.activatorSkillSlot
            });
        }
    }
}
