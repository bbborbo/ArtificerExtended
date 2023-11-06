using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArtificerExtended.EntityState
{
    class AlternateIonSurge2 : AlternateIonSurge
    {
        internal override void OnMovementDone()
        {
            base.OnMovementDone();
            GenericSkill[] skills = base.characterBody.skillLocator.allSkills;
            foreach (GenericSkill skill in skills)
            {
                if (skill.stock < skill.maxStock)
                {
                    skill.rechargeStopwatch += 0.5f;

                    if (skill.rechargeStopwatch >= skill.finalRechargeInterval)
                    {
                        skill.RestockSteplike();
                    }
                }
            }
        }
    }
}
