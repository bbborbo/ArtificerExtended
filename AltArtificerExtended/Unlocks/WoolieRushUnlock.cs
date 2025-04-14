using BepInEx.Configuration;
using RoR2;
using System;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(WoolieRushUnlock), nameof(WoolieRushUnlock), "FreeMage", 5, null)]
    class WoolieRushUnlock : UnlockBase
    {
        public static float timeRequirement = 600;
        public override string TOKEN_IDENTIFIER => nameof(TankDamageUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: Lightning-Fast";

        public override string AchievementDesc => $"As Artificer, fully charge the third teleporter before the timer reaches {timeRequirement / 60} minutes.";

        public override void OnInstall()
        {
            base.OnInstall();
        }
        public override void OnUninstall()
        {
            base.OnUninstall();
        }

        private void OnTeleporterChargedGlobal(TeleporterInteraction teleporterInteraction)
        {
            if (Run.instance.GetRunStopwatch() < timeRequirement && Run.instance.stageClearCount == 2 && base.isUserAlive)
            {
                base.Grant();
            }
        }
        public override void OnBodyRequirementMet()
        {
            TeleporterInteraction.onTeleporterChargedGlobal += this.OnTeleporterChargedGlobal;
            base.OnBodyRequirementMet();
        }
        public override void OnBodyRequirementBroken()
        {
            TeleporterInteraction.onTeleporterChargedGlobal -= this.OnTeleporterChargedGlobal;
            base.OnBodyRequirementBroken();
        }
    }
}
