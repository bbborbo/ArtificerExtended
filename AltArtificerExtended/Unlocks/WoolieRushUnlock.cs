using BepInEx.Configuration;
using RoR2;
using System;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    class WoolieRushUnlock : UnlockBase
    {
        public static float timeRequirement = 600;
        public override bool ForceDisable => false;

        public override string UnlockLangTokenName => "WOOLIERUSH";

        public override string UnlockName => "Lightning-Fast";

        public override string AchievementName => "Lightning-Fast";

        public override string AchievementDesc => $"fully charge the third teleporter before the timer reaches {timeRequirement / 60} minutes.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => base.GetSpriteProvider("LaserboltIcon");

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

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
