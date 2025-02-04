using BepInEx.Configuration;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArtificerExtended.Skills
{
    class _3IceSurgeSkill : SkillBase
    {
        public override string SkillName => "Avalanche";

        public override string SkillDescription => "Freezing. Burst forward, leaving an icicle at your feet for X% damage.";

        public override string SkillLangTokenName => "Arctic Surge";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => throw new NotImplementedException();

        public override MageElement Element => throw new NotImplementedException();

        public override Type ActivationState => throw new NotImplementedException();

        public override SkillFamily SkillSlot => throw new NotImplementedException();

        public override SimpleSkillData SkillData => throw new NotImplementedException();

        public override void Hooks()
        {
            throw new NotImplementedException();
        }

        public override void Init(ConfigFile config)
        {
            throw new NotImplementedException();
        }
    }
}
