using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(StackBurnUnlock), nameof(StackBurnUnlock), "FreeMage", 5, null)]
    class StackBurnUnlock : UnlockBase
    {
        public static int burnRequirementTotal = 25;
        public override string TOKEN_IDENTIFIER => nameof(StackBurnUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: The Ultimate Stonecutter";

        public override string AchievementDesc => $"As Artificer, apply {burnRequirementTotal} stacks of burn to a single target.";
        public override void OnInstall()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += CountBurn;

            base.OnInstall();
        }

        public override void OnUninstall()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy -= CountBurn;

            base.OnUninstall();
        }

        private void CountBurn(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.procCoefficient == 0f || damageInfo.rejected)
            {
                return;
            }
            if (!NetworkServer.active)
            {
                return;
            }

            CharacterBody victimBody = victim.GetComponent<CharacterBody>();
            CharacterBody attackerBody = null;
            if(damageInfo.attacker != null)
            {
                attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            }

            if(victimBody != null && attackerBody != null)
            {
                if (attackerBody.bodyIndex == LookUpRequiredBodyIndex())
                {
                    int burnCount = victimBody.GetBuffCount(RoR2Content.Buffs.OnFire) + victimBody.GetBuffCount(DLC1Content.Buffs.StrongerBurn) + 1;

                    if (burnCount >= burnRequirementTotal)
                    {
                        base.Grant();
                    }
                }
            }

            orig(self, damageInfo, victim);
        }
    }
}
