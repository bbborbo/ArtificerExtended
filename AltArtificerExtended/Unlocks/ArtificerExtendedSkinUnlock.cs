using BepInEx.Configuration;
using HG;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    class ArtificerExtendedSkinUnlock : UnlockBase
    {
        public override bool HideUnlock => true;
        public override bool ForceDisable => true;
        public override string UnlockLangTokenName => "ARTIEXTENDED";

        public override string UnlockName => "...To Explore";

        public override string AchievementName => "...To Explore";

        public override string AchievementDesc => "visit all but 2 stages in a single run, and win.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => base.GetSpriteProvider("");

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }
		public override void OnBodyRequirementMet()
		{
			base.OnBodyRequirementMet();
			this.visitedScenes = CollectionPool<SceneDef, List<SceneDef>>.RentCollection();
			SceneCatalog.onMostRecentSceneDefChanged += this.HandleMostRecentSceneDefChanged;
		}

		public override void OnBodyRequirementBroken()
		{
			SceneCatalog.onMostRecentSceneDefChanged -= this.HandleMostRecentSceneDefChanged;
			this.visitedScenes = CollectionPool<SceneDef, List<SceneDef>>.ReturnCollection(this.visitedScenes);
			base.OnBodyRequirementBroken();
		}

		private void HandleMostRecentSceneDefChanged(SceneDef newSceneDef)
		{
			if (!this.visitedScenes.Contains(newSceneDef))
			{
				this.visitedScenes.Add(newSceneDef);
			}
        }

        public void ClearCheck(Run run, RunReport runReport)
        {
            return;
            if (run is null) return;
            if (runReport is null) return;

            if (!runReport.gameEnding) return;


            if (runReport.gameEnding.isWin)
            {
                if (this.visitedScenes.Count >= ArtificerExtendedSkinUnlock.requirement && base.meetsBodyRequirement)
                {
                    base.Grant();
                }
            }
        }

        public override void OnInstall()
        {
            base.OnInstall();

            Run.onClientGameOverGlobal += this.ClearCheck;
        }

        public override void OnUninstall()
        {
            base.OnUninstall();

            Run.onClientGameOverGlobal -= this.ClearCheck;
        }

        private static readonly int requirement = 10;

		private List<SceneDef> visitedScenes;
	}
}
