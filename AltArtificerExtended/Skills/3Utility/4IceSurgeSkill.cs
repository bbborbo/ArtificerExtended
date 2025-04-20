using ArtificerExtended.States;
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
        public static float blastDamageCoefficient = 3.5f;
        public static float wallDamageCoefficient = 1.2f;
        public static float baseDurationHorizontal = 1f;
        public static float baseDurationVertical = 1.5f;

        public override string TOKEN_IDENTIFIER => "ICESURGE";
        public override string SkillName => "Cryoburst";

        public override string SkillDescription => $"Agile. Freezing. Cause a glacial burst beneath you for {DamageValueText(blastDamageCoefficient)}, launching you into the air.";

        public override Sprite Icon => null;

        public override Type ActivationState => typeof(FrostSurgeState);
        public override string ActivationStateMachineName => "Body";

        public override Type BaseSkillDef => typeof(SkillDef);

        public override SkillSlot SkillSlot => SkillSlot.Utility;

        public override float BaseCooldown => 8f;

        public override InterruptPriority InterruptPriority => InterruptPriority.Any;

        public override SimpleSkillData SkillData => new SimpleSkillData
        ( 
            baseMaxStock: 1,
            rechargeStock: 1,
            beginSkillCooldownOnSkillEnd: true,
            forceSprintingDuringState: false,
            cancelSprintingOnActivation: false,
            isCombatSkill: false,
            mustKeyPress: true
        );

        public override MageElement Element => MageElement.Ice;

        public override void Init()
        {
            KeywordTokens = new string[] { "KEYWORD_AGILE", "KEYWORD_FREEZING" };
            base.Init();
        }
        public override void Hooks()
        {
            
        }
    }
}
