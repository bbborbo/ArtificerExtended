using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(KillBlazingWithFireUnlock), nameof(KillBlazingWithFireUnlock), "FreeMage", 5, null)]
    class KillBlazingWithFireUnlock : UnlockBase
    {
        public static int burnRequirementTotal = 15;
        public int burnCounter = 0;
        public override string TOKEN_IDENTIFIER => nameof(KillBlazingWithFireUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: Sinners For Dinner";

        public override string AchievementDesc => $"As Artificer, kill {burnRequirementTotal} Blazing Elites with burn damage in a single run.";

        public override void OnInstall()
        {
            GlobalEventManager.onCharacterDeathGlobal += AddBurnCounter;
            Run.onRunStartGlobal += this.ResetBurnCounter;

            base.OnInstall();
        }

        public override void OnUninstall()
        {
            GlobalEventManager.onCharacterDeathGlobal -= AddBurnCounter;
            Run.onRunStartGlobal -= this.ResetBurnCounter;

            base.OnUninstall();
        }

        private void ResetBurnCounter(Run obj)
        {
            burnCounter = 0;
        }

        private void AddBurnCounter(DamageReport obj)
        {
            CharacterBody attackerBody = obj.attackerBody;
            CharacterBody victimBody = obj.victimBody;
            if(attackerBody && victimBody)
            {
                if (attackerBody.bodyIndex == LookUpRequiredBodyIndex() && victimBody.HasBuff(RoR2Content.Buffs.AffixRed))
                {
                    DamageInfo damageInfo = obj.damageInfo;
                    bool isBurnDamage = (damageInfo.damageType.damageType.HasFlag(DamageType.IgniteOnHit) || damageInfo.damageType.damageType.HasFlag(DamageType.PercentIgniteOnHit));
                    bool isBurnDot = (damageInfo.dotIndex == DotController.DotIndex.Burn 
                        || damageInfo.dotIndex == DotController.DotIndex.PercentBurn 
                        || damageInfo.dotIndex == DotController.DotIndex.StrongerBurn);

                    if (isBurnDamage || isBurnDot)
                    {
                        burnCounter++;

                        if (burnCounter >= burnRequirementTotal)
                        {
                            base.Grant();
                        }
                    }
                }
            }
        }
    }
}
