using BepInEx.Configuration;
using RoR2;
using RoR2.Stats;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(TankDamageUnlock), nameof(TankDamageUnlock), "FreeMage", 5, null)]
    class TankDamageUnlock : UnlockBase
    {
        public static int damageRequirementTotal = 5000;
        float damageTakenCount = 0;
        public ulong killRequirementTotal = 5;
        public StatDef postmortemKillCounter = GetCareerStatTotal("artificerKillsPostMortem");
        public override string TOKEN_IDENTIFIER => nameof(TankDamageUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: Cold Hearted";

        public override string AchievementDesc => $"As Artificer, take more than {damageRequirementTotal} points of damage in a single life.";

        private void ResetDamageCount()
        {
            damageTakenCount = 0;
        }
        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            RoR2.GlobalEventManager.onServerDamageDealt += DamageCounter;
            Run.onClientGameOverGlobal += ClearCheck;
        }

        private void ClearCheck(Run run, RunReport runReport)
        {
            ResetDamageCount();
        }

        public override void OnBodyRequirementBroken()
        {
            base.OnBodyRequirementBroken();
            RoR2.GlobalEventManager.onServerDamageDealt -= DamageCounter;
            //ResetDamageCount();
        }

        private void DamageCounter(DamageReport damageReport)
        {
            if(damageReport.victimBody == localUser.cachedBody)
            {
                if (!damageReport.victimBody.healthComponent.alive)
                {
                    ResetDamageCount();
                    return;
                }

                damageTakenCount += damageReport.damageDealt;
                if(damageTakenCount > damageRequirementTotal)
                {
                    base.Grant();
                }
            }
        }

        private void ColdFusionKillCounter(DamageReport damageReport)
        {
            var e = damageReport.attackerBodyIndex;
            CharacterMaster attackerMaster = damageReport.attackerMaster;
            Debug.Log(e.ToString() + " + " + LookUpRequiredBodyIndex().ToString());

            if (attackerMaster)
            {
                if (e == LookUpRequiredBodyIndex() || e == BodyIndex.None)
                {
                    CharacterBody body = damageReport.attackerBody;
                    if (body == null || !body.healthComponent.alive)
                    {
                        StatSheet currentStats = attackerMaster.playerStatsComponent.currentStats;
                        currentStats.PushStatValue(postmortemKillCounter, 1UL);

                        ulong stat = base.userProfile.statSheet.GetStatValueULong(postmortemKillCounter);
                        Debug.Log(stat);
                        if (stat >= killRequirementTotal)
                        {
                            base.Grant();
                        }
                    }
                }
            }
        }
    }
    
}
