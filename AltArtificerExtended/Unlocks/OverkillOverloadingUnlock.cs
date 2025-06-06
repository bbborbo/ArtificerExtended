using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(OverkillOverloadingUnlock), nameof(OverkillOverloadingUnlock), "FreeMage", 5, null)]
    class OverkillOverloadingUnlock : UnlockBase
    {
        static float overkillAmount = 1;
        public override string TOKEN_IDENTIFIER => nameof(OverkillOverloadingUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: Powertrippin\u2019";

        public override string AchievementDesc => $"As Artificer, overkill an Overloading Elite enemy by more than {overkillAmount * 100}% of its total maximum health.";

        public override void OnInstall()
        {
            On.RoR2.HealthComponent.TakeDamage += OverkillCheck;

            base.OnInstall();
        }

        public override void OnUninstall()
        {
            On.RoR2.HealthComponent.TakeDamage -= OverkillCheck;

            base.OnUninstall();
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
