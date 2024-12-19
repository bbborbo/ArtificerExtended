using BepInEx.Configuration;
using RoR2;
using RoR2.Stats;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    class TankDamageUnlock : UnlockBase
    {
        public int damageRequirementTotal = 2000;
        float damageTakenCount = 0;
        public ulong killRequirementTotal = 5;
        public StatDef postmortemKillCounter = GetCareerStatTotal("artificerKillsPostMortem");

        public override string UnlockLangTokenName => "TANKDAMAGE";

        public override string UnlockName => "Cold Hearted";

        public override string AchievementName => "Cold Hearted";

        public override string AchievementDesc => $"take more than {damageRequirementTotal} points of damage without dying.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => GetSpriteProvider("ColdFusion");
        public override bool HideUnlock => true;

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

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
