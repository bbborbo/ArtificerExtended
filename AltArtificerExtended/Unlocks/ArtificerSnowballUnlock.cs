using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AltArtificerExtended.Unlocks
{
    class ArtificerSnowballUnlock : UnlockBase
    {
        public int freezeRequirementTotal = 20;
        public int freezeCounter = 0;

        public override string UnlockLangTokenName => "SNOWBALL";

        public override string UnlockName => "Freeze Tag!";

        public override string AchievementName => "Freeze Tag!";

        public override string AchievementDesc => $"freeze Glacial Elites {freezeRequirementTotal} times in a single run.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => GetSpriteProvider("SnowballIcon");

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

        public override void OnInstall()
        {
            On.RoR2.SetStateOnHurt.OnTakeDamageServer += AddFreezeCounter;
            Run.onRunStartGlobal += this.ResetFreezeCounter;

            base.OnInstall();
        }

        public override void OnUninstall()
        {
            On.RoR2.SetStateOnHurt.OnTakeDamageServer -= AddFreezeCounter;
            Run.onRunStartGlobal -= this.ResetFreezeCounter;

            base.OnUninstall();
        }

        private void ResetFreezeCounter(Run obj)
        {
            freezeCounter = 0;
        }

        private void AddFreezeCounter(On.RoR2.SetStateOnHurt.orig_OnTakeDamageServer orig, SetStateOnHurt self, DamageReport damageReport)
        {
            DamageInfo damageInfo = damageReport.damageInfo;

            if (damageReport.attackerBodyIndex == LookUpRequiredBodyIndex() && damageReport.victimBody.HasBuff(RoR2Content.Buffs.AffixWhite))
            {
                if (damageInfo.procCoefficient > 0 && self.canBeFrozen && (damageInfo.damageType & DamageType.Freeze2s) != DamageType.Generic)
                {
                    freezeCounter++;

                    if (freezeCounter >= freezeRequirementTotal)
                    {
                        base.Grant();
                    }
                }
            }
            orig(self, damageReport);
        }
    }
}
