using BepInEx.Configuration;
using RoR2;
using RoR2.Stats;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    class KillsPostMortemUnlock : UnlockBase
    {
        public ulong killRequirementTotal = 5;
        public StatDef postmortemKillCounter = GetCareerStatTotal("artificerKillsPostMortem");

        public override string UnlockLangTokenName => "COLDFUSION";

        public override string UnlockName => "When Icicles Die...";

        public override string AchievementName => "When Icicles Die...";

        public override string AchievementDesc => $"kill {killRequirementTotal} enemies post-mortem.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => GetSpriteProvider("ColdFusion");
        public override bool HideUnlock => true;

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

        public override void OnInstall()
        {
            //RoR2.GlobalEventManager.onCharacterDeathGlobal += ColdFusionKillCounter;
            base.OnInstall();
        }

        public override void OnUninstall()
        {
            //RoR2.GlobalEventManager.onCharacterDeathGlobal -= ColdFusionKillCounter;
            base.OnUninstall();
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
