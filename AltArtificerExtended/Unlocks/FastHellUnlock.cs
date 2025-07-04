using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
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
        static float timeInMinutes = 3f;
        static float gracePeriodInSeconds = 10f;
        private bool stageOk = false;
        private float stageEntryStopwatchValue = float.NegativeInfinity;
        public override void OnInstall()
        {
            base.OnInstall();
        }
        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            Stage.onStageStartGlobal += this.OnStageStart;
        }

        public override void OnBodyRequirementBroken()
        {
            base.OnBodyRequirementBroken();
            Stage.onStageStartGlobal -= this.OnStageStart;
            stageEntryStopwatchValue = float.NegativeInfinity;
            stageOk = false;
        }

        private void OnStageStart(Stage newStageDef)
        {
            SceneDef newSceneDef = newStageDef.sceneDef;
            HandleMostRecentSceneDefChanged(newSceneDef);
        }

        bool CheckSceneRequirement(SceneDef sceneDef)
        {
            Debug.Log(sceneDef.baseSceneName);
            return requiredScenes.Contains(sceneDef.baseSceneName);
        }

        private void HandleMostRecentSceneDefChanged(SceneDef newSceneDef)
        {
            if (stageOk)
            {
                float timeThisStage = Run.instance.GetRunStopwatch() - stageEntryStopwatchValue;
                Debug.Log("seconds this stage: " + timeThisStage);
                if (timeThisStage <= (timeInMinutes * 60) + gracePeriodInSeconds)
                {
                    base.Grant();
                    stageOk = false;
                    return;
                }
            }

            if (CheckSceneRequirement(newSceneDef))
            {
                stageEntryStopwatchValue = Stage.instance.entryStopwatchValue;
                this.stageOk = true;
                return;
            }
            stageEntryStopwatchValue = float.NegativeInfinity;
            stageOk = false;
        }
    }
}
