using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AltArtificerExtended.Unlocks
{
    class ArtificerFlameBurstUnlock : UnlockBase
    {
        public int burnRequirementTotal = 15;
        public int burnCounter = 0;

        public override string UnlockLangTokenName => "FLAMEBURST";

        public override string UnlockName => "To Fight Fire";

        public override string AchievementName => "To Fight Fire";

        public override string AchievementDesc => $"kill {burnRequirementTotal} Blazing Elites with burn damage in a single run.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => GetSpriteProvider("Fireskill2icon");

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

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
                    bool isBurnDamage = (damageInfo.damageType.HasFlag(DamageType.IgniteOnHit) || damageInfo.damageType.HasFlag(DamageType.PercentIgniteOnHit));
                    bool isBurnDot = (damageInfo.dotIndex == DotController.DotIndex.Burn || damageInfo.dotIndex == DotController.DotIndex.PercentBurn);

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
