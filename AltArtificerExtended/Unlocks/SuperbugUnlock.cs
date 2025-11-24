using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(SuperbugUnlock), nameof(SuperbugUnlock), "FreeMage", 5, null)]
    class SuperbugUnlock : UnlockBase
    {
        public override string TOKEN_IDENTIFIER => "SUPERBUG";

        public override string AchievementName => "Artificer: Death is in Your Blood";

        public override string AchievementDesc => "As Artificer, apply 1 stack of bleed to a single target.";
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
            orig(self, damageInfo, victim);

            CharacterBody victimBody = victim.GetComponent<CharacterBody>();
            CharacterBody attackerBody = null;
            if (damageInfo.attacker != null)
            {
                attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            }

            if (victimBody != null && attackerBody != null)
            {
                if (attackerBody.bodyIndex == LookUpRequiredBodyIndex())
                {
                    int burnCount = victimBody.GetBuffCount(RoR2Content.Buffs.Bleeding) + victimBody.GetBuffCount(RoR2Content.Buffs.SuperBleed);

                    if (burnCount >= 1)
                    {
                        base.Grant();
                    }
                }
            }

        }
    }
}
