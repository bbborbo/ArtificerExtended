using ArtificerExtended.Components;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static ArtificerExtended.Components.ElementCounter;
using static ArtificerExtended.Passive.AltArtiPassive;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(FullKitElementUnlock), nameof(FullKitElementUnlock), "FreeMage", 5, null)]
    class FullKitElementUnlock : UnlockBase
    {
        public override string TOKEN_IDENTIFIER => nameof(FullKitElementUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: Elemental Intensity";

        public override string AchievementDesc => $"As Artificer, win with 4 abilities of a single element equipped at once.";

        public override void OnInstall()
        {
            //On.EntityStates.Mage.MageCharacterMain.OnEnter += PowerCheck;
            base.OnInstall();
        }
        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            Run.onClientGameOverGlobal += ClearCheck;
        }
        public override void OnBodyRequirementBroken()
        {
            base.OnBodyRequirementBroken();
            Run.onClientGameOverGlobal -= ClearCheck;
        }

        private void ClearCheck(Run run, RunReport runReport)
        {
            if (run is null) return;
            if (runReport is null) return;

            if (!runReport.gameEnding) return;


            if (runReport.gameEnding.isWin)
            {
                CharacterBody localBody = this.localUser.cachedBody;
                if (localBody)
                {
                    //ElementCounter power = localBody.GetComponent<ElementCounter>();
                    //if (power != null &&
                    //    (power.firePower >= Power.Extreme || power.icePower >= Power.Extreme || power.lightningPower >= Power.Extreme))
                    //{
                    //    base.Grant();
                    //}
                }
                else
                {
                    Log.Error("FullKitElementUnlock : Local Body destroyed, unable to access element counter! This may be a result of the ending destroying the body or dying before the ending!");
                }

                if(ElementCounter.localUserFirePower >= Power.Extreme || ElementCounter.localUserIcePower >= Power.Extreme || ElementCounter.localUserLightningPower >= Power.Extreme)
                {
                    base.Grant();
                }
            }
        }


        private void PowerCheck(On.EntityStates.Mage.MageCharacterMain.orig_OnEnter orig, global::EntityStates.Mage.MageCharacterMain self)
        {
            orig(self);
            ElementCounter power = self.characterBody?.GetComponent<ElementCounter>();
            if (power != null)
            {
                if (power.firePower >= Power.Extreme || power.icePower >= Power.Extreme || power.lightningPower >= Power.Extreme)
                {
                    base.Grant();
                }
            }
        }
    }
}