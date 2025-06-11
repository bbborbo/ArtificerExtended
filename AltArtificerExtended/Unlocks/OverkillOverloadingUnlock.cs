using Assets.RoR2.Scripts.Platform;
using BepInEx.Configuration;
using RoR2;
using RoR2.Achievements;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(OverkillOverloadingUnlock), nameof(OverkillOverloadingUnlock), "FreeMage", 5, typeof(OverkillOverloadingServerAchievement))]
    class OverkillOverloadingUnlock : UnlockBase
    {
        private class OverkillOverloadingServerAchievement : BaseServerAchievement
        {
            CharacterBody trackedBody;
            public override void OnInstall()
            {
                base.OnInstall();
                RoR2Application.onFixedUpdate += SetTrackedBody;
                GlobalEventManager.onCharacterDeathGlobal += OnDeathOverkillCheck;
                GlobalEventManager.onServerCharacterExecuted += OnExecuteOverkillCheck;
            }

            public override void OnUninstall()
            {
                base.OnUninstall();
                RoR2Application.onFixedUpdate -= SetTrackedBody;
                GlobalEventManager.onCharacterDeathGlobal -= OnDeathOverkillCheck;
                GlobalEventManager.onServerCharacterExecuted -= OnExecuteOverkillCheck;
            }

            private void SetTrackedBody()
            {
                trackedBody = base.GetCurrentBody();
            }

            private void OnDeathOverkillCheck(DamageReport damageReport)
            {
                OnExecuteOverkillCheck(damageReport, 0);
            }
            private void OnExecuteOverkillCheck(DamageReport damageReport, float executionHealthLost)
            {
                CharacterBody victimBody = damageReport.victimBody;
                HealthComponent victimHealthComponent = victimBody.healthComponent;
                bool isVictimOverloading = victimBody.HasBuff(RoR2Content.Buffs.AffixBlue);
                float victimMaxHealth = victimHealthComponent.fullCombinedHealth - executionHealthLost;

                if (isVictimOverloading)
                {
                    float overkillDamage = damageReport.damageDealt - damageReport.combinedHealthBeforeDamage;
                    if (overkillDamage >= victimMaxHealth * overkillAmount)
                    {
                        base.Grant();
                        base.ServerTryToCompleteActivity();
                    }
                }
            }
        }

        static float overkillAmount = 1;
        public override string TOKEN_IDENTIFIER => nameof(OverkillOverloadingUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: Powertrippin\u2019";

        public override string AchievementDesc => $"As Artificer, overkill an Overloading Elite enemy by more than {overkillAmount * 100}% of its combined maximum health.";

        public override void TryToCompleteActivity()
        {
            bool flag = base.localUser.id == LocalUserManager.GetFirstLocalUser().id;
            if (this.shouldGrant && flag)
            {
                BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
                baseActivitySelector.activityAchievementID = nameof(FreezeManySimultaneousUnlock);
                PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector, true, true);
            }
        }
        public override void OnInstall()
        {
            base.OnInstall();
        }

        public override void OnUninstall()
        {
            base.OnUninstall();
        }

        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            base.SetServerTracked(true);
        }
        public override void OnBodyRequirementBroken()
        {
            base.OnBodyRequirementBroken();
            base.SetServerTracked(false);
        }

        private void OverkillCheck(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            CharacterBody victimBody = self.body;

            bool isOverloading = victimBody.HasBuff(RoR2Content.Buffs.AffixBlue);
            float currentHealth = self.combinedHealth;
            float maxHealth = self.fullHealth;

            orig(self, damageInfo);

            if (!self.alive && isOverloading)
            {
                CharacterBody attackerBody = null;
                if (damageInfo.attacker != null)
                {
                    attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                }

                if (attackerBody != null && attackerBody.bodyIndex == LookUpRequiredBodyIndex())
                {
                    if (self.isInFrozenState)
                        maxHealth *= 0.7f;

                    float overkillDamage = damageInfo.damage - currentHealth;
                    if(overkillDamage > maxHealth * overkillAmount)
                    {
                        base.Grant();
                    }
                }
            }
        }
    }
}
