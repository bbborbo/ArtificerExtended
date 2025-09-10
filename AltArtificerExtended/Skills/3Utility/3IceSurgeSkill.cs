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
    class _3IceSurgeSkill : SkillBase<_3IceSurgeSkill>
    {
        public static float blastDamageCoefficient = 3.5f;
        public static float wallDamageCoefficient = 2.0f;
        public static float baseDurationHorizontal = 1.0f;
        public static float baseDurationVertical = 1.4f;

        public override string TOKEN_IDENTIFIER => "ICESURGE";
        public override string SkillName => "Cryoburst";

        public override string SkillDescription => $"{UtilityColor("Agile")}. {UtilityColor("Freezing")}. " +
            $"Cause a glacial burst beneath you for {DamageValueText(blastDamageCoefficient)}, launching you into the air.";

        public override Sprite Icon => LoadSpriteFromBundle("cryoburstAE");

        public override Type ActivationState => typeof(FrostSurgeState);
        public override string ActivationStateMachineName => "Body";

        public override Type BaseSkillDef => typeof(SkillDef);

        public override SkillSlot SkillSlot => SkillSlot.Utility;

        public override float BaseCooldown => 8f;

        public override InterruptPriority InterruptPriority => InterruptPriority.Stun;

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
