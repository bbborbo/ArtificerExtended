using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArtificerExtended.EntityState
{
    class AlternateIonSurge2 : AlternateIonSurge
    {
        public static float rechargeIncrement = 0.75f;
        internal override void OnMovementDone()
        {
            base.OnMovementDone();
            GenericSkill[] skills = base.characterBody.skillLocator.allSkills;
            foreach (GenericSkill skill in skills)
            {
                if (skill.stock < skill.maxStock)
                {
                    skill.rechargeStopwatch += rechargeIncrement;

                    if (skill.rechargeStopwatch >= skill.finalRechargeInterval)
                    {
                        skill.RestockSteplike();
                    }
                }
            }
        }
    }
}
