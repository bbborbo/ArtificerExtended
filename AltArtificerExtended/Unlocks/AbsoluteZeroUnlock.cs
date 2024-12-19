using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.Unlocks
{
    class AbsoluteZeroUnlock : UnlockBase
    {
        public override string UnlockLangTokenName => "ABSOLUTEZERO";
        public override string UnlockName => "Absolute Zero";
        public override string AchievementName => "Absolute Zero";
        public override string AchievementDesc => "freeze and execute the King of Nothing.";
        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => base.GetSpriteProvider("frostbitesketch1");

        private void ExecuteMithrixCheck(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            bool isMithrixFrozen = false;

            CharacterBody victimBody = self.body;
            BodyIndex victimIndex = victimBody.bodyIndex;
            if ((victimIndex == BodyCatalog.FindBodyIndex("BrotherBody") 
                || victimIndex == BodyCatalog.FindBodyIndex("BrotherGlassBody") 
                || victimIndex == BodyCatalog.FindBodyIndex("BrotherHauntBody") 
                || victimIndex == BodyCatalog.FindBodyIndex("BrotherHurtBody")) 
                && self.isInFrozenState)
            {
                isMithrixFrozen = true;
            }
            
            orig(self, damageInfo);

            if (isMithrixFrozen && !self.alive)
            {
                CharacterBody attackerBody = damageInfo.attacker?.GetComponent<CharacterBody>();
                if (attackerBody != null)
                {
                    BodyIndex attackerIndex = attackerBody.bodyIndex;
                    if(attackerIndex == LookUpRequiredBodyIndex())
                    {
                        base.Grant();
                    }
                }
            }
        }


        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

        public override void OnInstall()
        {
            On.RoR2.HealthComponent.TakeDamage += ExecuteMithrixCheck;

            base.OnInstall();
        }

        public override void OnUninstall()
        {
            On.RoR2.HealthComponent.TakeDamage -= ExecuteMithrixCheck;

            base.OnUninstall();
        }
    }
}
