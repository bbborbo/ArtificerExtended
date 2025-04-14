using ArtificerExtended.Modules;
using Assets.RoR2.Scripts.Platform;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Achievements;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(FreezeManySimultaneousUnlock), nameof(FreezeManySimultaneousUnlock), "FreeMage", 5, null)]
    class FreezeManySimultaneousUnlock : UnlockBase<FreezeManySimultaneousUnlock>
    {
        private class FreezeManySimultaneousServerAchievement : BaseServerAchievement
        {
            Dictionary<SetStateOnHurt, float> avalancheUnlockTrackers;

            CharacterBody trackedBody;
            public override void OnInstall()
            {
                base.OnInstall();
                avalancheUnlockTrackers = new Dictionary<SetStateOnHurt, float>();
                RoR2Application.onFixedUpdate += SetTrackedBody;
                On.RoR2.SetStateOnHurt.SetFrozenInternal += AddFreezeTracker;
            }

            private void AddFreezeTracker(On.RoR2.SetStateOnHurt.orig_SetFrozenInternal orig, SetStateOnHurt self, float duration)
            {
                orig(self, duration);

                CharacterBody body;
                if (self.TryGetComponent<CharacterBody>(out body) && body.healthComponent)
                {
                    GameObject lastAttacker = body.healthComponent.lastHitAttacker;
                    CharacterBody attackerBody;
                    if(lastAttacker.TryGetComponent<CharacterBody>(out attackerBody) && attackerBody == trackedBody)
                    {
                        int progress = 1;
                        bool shouldGrant = false;
                        foreach(KeyValuePair<SetStateOnHurt, float> avalancheUnlockTracker in avalancheUnlockTrackers)
                        {
                            if (avalancheUnlockTracker.Key == null)
                                continue;
                            if (Time.time > avalancheUnlockTracker.Value)
                                continue;

                            progress++;
                            if(progress >= FreezeManySimultaneousUnlock.freezeRequirementTotal)
                            {
                                shouldGrant = true;
                                break;
                            }
                        }

                        if (shouldGrant)
                        {
                            base.Grant();
                            base.ServerTryToCompleteActivity();
                        }
                        else
                        {
                            if (avalancheUnlockTrackers.ContainsKey(self))
                            {
                                avalancheUnlockTrackers[self] = Time.time + duration;
                            }
                            else
                            {
                                avalancheUnlockTrackers.Add(self, Time.time + duration);
                            }
                        }
                    }
                }
            }

            private void SetTrackedBody()
            {
                trackedBody = base.GetCurrentBody();
            }

            public override void OnUninstall()
            {
                base.OnUninstall();
                RoR2Application.onFixedUpdate -= SetTrackedBody;
                On.RoR2.SetStateOnHurt.SetFrozenInternal += AddFreezeTracker;
            }
        }

        public override string AchievementName => "Artificer: Ice V Has Arrived";

        public override string AchievementDesc => $"As Artificer, have {freezeRequirementTotal} monsters frozen at once.";

        public override string TOKEN_IDENTIFIER => nameof(FreezeManySimultaneousUnlock).ToUpperInvariant();

        public static int freezeRequirementTotal = 5;

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

        public override void OnInstall()
        {
            base.OnInstall();
        }

        public override void OnUninstall()
        {
            base.OnUninstall();
        }
    }
}
