using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static ArtificerExtended.Modules.Language.Styling;

namespace ArtificerExtended.Skills
{
    class _4IceSurgeSkill : SkillBase<_4IceSurgeSkill>
    {
        public static float damageCoefficient = 1.2f;

        public override string TOKEN_IDENTIFIER => "ICESURGE";
        public override string SkillName => "Ice Surge";

        public override string SkillDescription => $"Freezing. Cause a glacial burst beneath you for {DamageValueText(damageCoefficient)}, launching you into the air.";

        public override Sprite Icon => null;

        public override Type ActivationState => typeof(Idle);

        public override Type BaseSkillDef => typeof(SkillDef);

        public override SkillSlot SkillSlot => SkillSlot.Utility;

        public override float BaseCooldown => 8f;

        public override InterruptPriority InterruptPriority => InterruptPriority.Any;

        public override SimpleSkillData SkillData => new SimpleSkillData
        ( 
            baseMaxStock: 1,
            rechargeStock: 1,
            beginSkillCooldownOnSkillEnd: true,
            forceSprintingDuringState: true,
            isCombatSkill: false,
            mustKeyPress: true
        );

        public override MageElement Element => MageElement.Ice;

        public override void Hooks()
        {
            
        }
    }
}
