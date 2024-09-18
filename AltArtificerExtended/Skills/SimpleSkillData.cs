using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArtificerExtended.Skills
{
    public class SimpleSkillData
    {
        public SimpleSkillData(int baseMaxStock = 1, float baseRechargeInterval = 1f, bool beginSkillCooldownOnSkillEnd = false,
            bool canceledFromSprinting = false, bool cancelSprintingOnActivation = true, bool dontAllowPastMaxStocks = true, 
            bool fullRestockOnAssign = true, InterruptPriority interruptPriority = InterruptPriority.Any, 
            bool isCombatSkill = true, bool mustKeyPress = false, int rechargeStock = 1, 
            int requiredStock = 1, bool resetCooldownTimerOnUse = false, int stockToConsume = 1,
            bool useAttackSpeedScaling = false, string activationStateMachineName = "Weapon")
        {
            this.baseMaxStock = baseMaxStock;
            this.baseRechargeInterval = baseRechargeInterval;
            this.beginSkillCooldownOnSkillEnd = beginSkillCooldownOnSkillEnd;
            this.canceledFromSprinting = canceledFromSprinting;
            this.cancelSprintingOnActivation = cancelSprintingOnActivation;
            this.dontAllowPastMaxStocks = dontAllowPastMaxStocks;
            this.fullRestockOnAssign = fullRestockOnAssign;
            this.interruptPriority = interruptPriority;
            this.isCombatSkill = isCombatSkill;
            this.mustKeyPress = mustKeyPress;
            this.rechargeStock = rechargeStock;
            this.requiredStock = requiredStock;
            this.resetCooldownTimerOnUse = resetCooldownTimerOnUse;
            this.stockToConsume = stockToConsume;
            this.useAttackSpeedScaling = useAttackSpeedScaling;
            this.activationStateMachineName = activationStateMachineName;
        }

        internal int baseMaxStock;
        internal float baseRechargeInterval;
        internal bool beginSkillCooldownOnSkillEnd;
        internal bool canceledFromSprinting;
        internal bool cancelSprintingOnActivation;
        internal bool dontAllowPastMaxStocks;
        internal bool fullRestockOnAssign;
        internal InterruptPriority interruptPriority;
        internal bool isCombatSkill;
        internal bool mustKeyPress;
        internal int rechargeStock;
        internal int requiredStock;
        internal bool resetCooldownTimerOnUse;
        internal int stockToConsume;
        internal bool useAttackSpeedScaling;
        internal string activationStateMachineName;
    }
}
