using BepInEx.Configuration;
using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArtificerExtended.Skills
{
    class CancelFrostbiteSkill : SkillBase<CancelFrostbiteSkill>
    {
        public override string SkillName => "Shed Ice Armor";

        public override string SkillDescription => "Focus, and shed your ice armor early.";

        public override string SkillLangTokenName => "CANCELICEARMOR";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "frostbitesketch2";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(GenericCharacterMain);

        public override SkillFamily SkillSlot => null;

        public override SimpleSkillData SkillData => new SimpleSkillData 
        { 
            requiredStock = 1,
            rechargeStock = 0,
            baseMaxStock = 0,
            baseRechargeInterval = 0,
            stockToConsume = 0,
            fullRestockOnAssign = true,
            interruptPriority = InterruptPriority.Any,
            mustKeyPress = true,
            isCombatSkill = true,
            activationStateMachineName = "Body"
        };

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateSkill();
            CreateLang();
        }
    }
}
