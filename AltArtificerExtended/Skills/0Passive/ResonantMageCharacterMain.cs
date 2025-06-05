using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArtificerExtended._0Passive
{
    class ResonantMageCharacterMain : BaseCharacterMain
    {
        private EntityStateMachine jetpackStateMachine;
        public override void OnEnter()
        {
            base.OnEnter();
            this.jetpackStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Jet");
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!jetpackStateMachine.IsInMainState())
            {
                jetpackStateMachine.SetNextStateToMain();
            }
        }
    }
}
