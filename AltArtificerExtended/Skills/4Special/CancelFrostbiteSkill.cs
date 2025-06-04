using BepInEx.Configuration;
using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Skills
{
    class CancelFrostbiteSkill : SkillBase<CancelFrostbiteSkill>
    {
        public override string SkillName => "Shed Ice Armor";

        public override string SkillDescription => "Focus, and shed your ice armor early.";

        public override string TOKEN_IDENTIFIER => "CANCELICEARMOR";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(Idle);

        public override SimpleSkillData SkillData => new SimpleSkillData 
        { 
            requiredStock = 0,
            rechargeStock = 0,
            baseMaxStock = 0,
            stockToConsume = 0,
            fullRestockOnAssign = true,
            mustKeyPress = true,
            isCombatSkill = true
        };
        public override Sprite Icon => LoadSpriteFromBundle("polarvortexreactivationAE");
        public override SkillSlot SkillSlot => SkillSlot.None;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;
        public override Type BaseSkillDef => typeof(SkillDef);
        public override float BaseCooldown => 8;
        public override string ActivationStateMachineName => "Body";
        public override void Init()
        {
            base.Init();
        }

        public override void Hooks()
        {

        }
    }
}
