using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    class FastHellUnlock : UnlockBase<FastHellUnlock>
    {
        private static readonly string[] requiredScenes = new string[]
        {
            "dampcavesimple",
            "helminthroost"
        };
        public static float timeInMinutes = 3f;
        private bool stageOk = false;
        private float stageEnterTime;

        public override string UnlockLangTokenName => "FASTHELL";

        public override string UnlockName => "God, It's Pretty Hot Down Here";

        public override string AchievementName => "God, It's Pretty Hot Down Here";

        public override string AchievementDesc => $"Leave the Abyssal Depths or Helminth Hatchery within {timeInMinutes} of entering.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => base.GetSpriteProvider("");

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }
        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            SceneCatalog.onMostRecentSceneDefChanged += this.HandleMostRecentSceneDefChanged;
        }

        public override void OnBodyRequirementBroken()
        {
            base.OnBodyRequirementBroken();
            SceneCatalog.onMostRecentSceneDefChanged -= this.HandleMostRecentSceneDefChanged;
            stageOk = false;
            stageEnterTime = float.NegativeInfinity;
        }

        private void HandleMostRecentSceneDefChanged(SceneDef sceneDef)
        {
            if (stageOk && stageEnterTime > 0)
            {
                if (Run.instance.GetRunStopwatch() <= stageEnterTime + (timeInMinutes * 60 + 5));
                {
                    base.Grant();
                    stageOk = false;
                    stageEnterTime = float.NegativeInfinity;
                    return;
                }
            }
            this.stageOk = (Array.IndexOf<string>(FastHellUnlock.requiredScenes, sceneDef.baseSceneName) != -1);
            if (this.stageOk)
            {
                stageEnterTime = Run.instance.GetRunStopwatch();
            }
            stageOk = false;
            stageEnterTime = float.NegativeInfinity;
        }
    }
}
