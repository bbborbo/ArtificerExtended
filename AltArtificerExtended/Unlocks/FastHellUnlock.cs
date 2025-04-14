using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(FastHellUnlock), nameof(FastHellUnlock), "FreeMage", 5, null)]
    class FastHellUnlock : UnlockBase
    {
        public override string TOKEN_IDENTIFIER => nameof(FastHellUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: God, It\u2019s Pretty Hot Down Here";

        public override string AchievementDesc => $"As Artificer, leave the Abyssal Depths or Helminth Hatchery within {timeInMinutes} minutes of entering.";
        private static readonly string[] requiredScenes = new string[]
        {
            "dampcavesimple",
            "helminthroost"
        };
        public static float timeInMinutes = 3f;
        private bool stageOk = false;
        private float stageEnterTime;
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
