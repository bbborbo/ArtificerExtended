using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    class NoDamageUnlock : UnlockBase<NoDamageUnlock>
	{
		private HealthComponent healthComponent;
		private bool failed;
		private bool characterOk;
		private ToggleAction healthCheck;
		private ToggleAction teleporterCheck;

		public override string UnlockLangTokenName => "NODAMAGEUNLOCK";

        public override string UnlockName => "Flawless Execution";

        public override string AchievementName => "Flawless Execution";

        public override string AchievementDesc => "start and finish any stage without falling below 100% health.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => GetSpriteProvider("IceShardsIcon");

        public override void Init(ConfigFile config)
        {
			CreateLang();
        }

		private void SubscribeHealthCheck()
		{
			RoR2Application.onFixedUpdate += this.CheckHealth;
		}

		private void UnsubscribeHealthCheck()
		{
			RoR2Application.onFixedUpdate -= this.CheckHealth;
		}

		private void SubscribeTeleporterCheck()
		{
			TeleporterInteraction.onTeleporterChargedGlobal += this.CheckTeleporter;
		}

		private void UnsubscribeTeleporterCheck()
		{
			TeleporterInteraction.onTeleporterChargedGlobal -= this.CheckTeleporter;
		}

		private void CheckTeleporter(TeleporterInteraction teleporterInteraction)
		{
			if (this.characterOk && !this.failed)
			{
				base.Grant();
			}
		}

		public override void OnInstall()
		{
			base.OnInstall();
			this.healthCheck = new ToggleAction(new Action(this.SubscribeHealthCheck), new Action(this.UnsubscribeHealthCheck));
			this.teleporterCheck = new ToggleAction(new Action(this.SubscribeTeleporterCheck), new Action(this.UnsubscribeTeleporterCheck));
			SceneCatalog.onMostRecentSceneDefChanged += this.OnMostRecentSceneDefChanged;
			base.localUser.onBodyChanged += this.OnBodyChanged;
		}

		public override void OnUninstall()
		{
			base.localUser.onBodyChanged -= this.OnBodyChanged;
			SceneCatalog.onMostRecentSceneDefChanged -= this.OnMostRecentSceneDefChanged;
			this.healthCheck.Dispose();
			this.teleporterCheck.Dispose();
			base.OnUninstall();
		}

		private void OnBodyChanged()
		{
			if (this.characterOk && !this.failed && base.localUser.cachedBody)
			{
				this.healthComponent = base.localUser.cachedBody.healthComponent;
				this.healthCheck.SetActive(true);
				this.teleporterCheck.SetActive(true);
			}
		}

		private void OnMostRecentSceneDefChanged(SceneDef sceneDef)
		{
			this.failed = false;
		}

		public override void OnBodyRequirementMet()
		{
			base.OnBodyRequirementMet();
			this.characterOk = true;
		}

		public override void OnBodyRequirementBroken()
		{
			this.characterOk = false;
			this.Fail();
			base.OnBodyRequirementBroken();
		}

		private void Fail()
		{
			this.failed = true;
			this.healthCheck.SetActive(false);
			this.teleporterCheck.SetActive(false);
		}

		private void CheckHealth()
		{
			if (this.healthComponent && this.healthComponent.combinedHealth < this.healthComponent.fullCombinedHealth)
			{
				this.Fail();
			}
		}
	}
}
