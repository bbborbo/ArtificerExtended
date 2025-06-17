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
        private SceneDef depthsSceneDef;
        private SceneDef hatcherySceneDef;
        public static float timeInMinutes = 3f;
        private bool stageOk = false;
        private float stageEnterTime;
        public override void OnInstall()
        {
            base.OnInstall();
            this.depthsSceneDef = SceneCatalog.GetSceneDefFromSceneName("dampcavesimple");
            this.hatcherySceneDef = SceneCatalog.GetSceneDefFromSceneName("helminthroost");
        }
        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            //Stage.onStageStartGlobal += this.OnStageStart;
            SceneCatalog.onMostRecentSceneDefChanged += this.HandleMostRecentSceneDefChanged;
        }

        public override void OnBodyRequirementBroken()
        {
            base.OnBodyRequirementBroken();
            //Stage.onStageStartGlobal -= this.OnStageStart;
            SceneCatalog.onMostRecentSceneDefChanged -= this.HandleMostRecentSceneDefChanged;
            stageOk = false;
            stageEnterTime = float.NegativeInfinity;
        }

        bool CheckSceneRequirement(SceneDef sceneDef)
        {
            return sceneDef == hatcherySceneDef || sceneDef == depthsSceneDef;
        }

        private void OnStageStart(Stage obj)
        {
            if(CheckSceneRequirement(obj.sceneDef))
            {
                stageOk = true;
                stageEnterTime = Run.instance.GetRunStopwatch();
            }
        }

        private void HandleMostRecentSceneDefChanged(SceneDef newSceneDef)
        {
            if (stageOk && stageEnterTime >= 0)
            {
                if (Run.instance.GetRunStopwatch() <= stageEnterTime + (timeInMinutes * 60 + 5));
                {
                    base.Grant();
                    stageOk = false;
                    stageEnterTime = float.NegativeInfinity;
                    return;
                }
            }
            if (CheckSceneRequirement(newSceneDef))
            {
                this.stageOk = true;
                stageEnterTime = Run.instance.GetRunStopwatch();
                return;
            }
            stageOk = false;
            stageEnterTime = float.NegativeInfinity;
        }
    }
}
