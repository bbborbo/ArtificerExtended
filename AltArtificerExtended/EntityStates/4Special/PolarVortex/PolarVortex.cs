using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage;
using EntityStates.Toolbot;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.EntityState
{
    class PolarVortex : PolarVortexBase
    {
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
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (isAuthority)
            {
                float delta = (ending ? Time.fixedDeltaTime * endingSpeedMultiplier : Time.fixedDeltaTime);
                stopwatch += delta;
                armorAddStopwatch += delta;

                if (stopwatch >= _1FrostbiteSkill.maxDuration)
                {
                    if(!ending)
                        base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", 0.3f / this.attackSpeedStat);
                    this.SetNextState();
                    return;
                }

                while (armorAddStopwatch > _1FrostbiteSkill.buffInterval)
                {
                    armorAddStopwatch -= _1FrostbiteSkill.buffInterval;
                    AddIceArmorBuff();
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
