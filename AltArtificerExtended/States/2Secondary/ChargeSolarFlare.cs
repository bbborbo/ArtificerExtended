using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage.Weapon;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.States
{
    class ChargeSolarFlare : BaseChargeBombState
    {
        public static GameObject fireBombChargeEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/ChargeMageFireBomb");
        public override void OnEnter()
        {
            this.chargeEffectPrefab = fireBombChargeEffectPrefab;
            this.minChargeDuration = _4SolarFlareSkill.minChargeDuration;
            this.baseDuration = _4SolarFlareSkill.maxChargeDuration;
            base.OnEnter();
        }
        public override BaseThrowBombState GetNextState()
        {
            return new ThrowSolarFlare();
        }
    }
}
