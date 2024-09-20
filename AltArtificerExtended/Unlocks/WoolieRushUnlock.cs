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

        public override string UnlockLangTokenName => "ICESHARDS";

        public override string UnlockName => "Flawless Execution";

        public override string AchievementName => "Flawless Execution";

        public override string AchievementDesc => "fully charge the third teleporter before the timer reaches 10 minutes.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => base.GetSpriteProvider("IceShardsIcon");

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
