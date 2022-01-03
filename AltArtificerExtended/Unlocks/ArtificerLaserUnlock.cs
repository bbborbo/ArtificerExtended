using BepInEx.Configuration;
using EntityStates;
using RoR2;
using RoR2.Stats;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AltArtificerExtended.Unlocks
{
    class ArtificerLaserUnlock : UnlockBase
    {
        public ulong stunRequirementTotal = 100;
        public StatDef stunCounter = GetStunCounter("artificerTotalEnemiesStunned");

        static StatDef GetStunCounter(string name)
        {
            StatDef stat = StatDef.Find(name);
            if (stat == null)
            {
                stat = StatDef.Register(name, StatRecordType.Sum, StatDataType.ULong, 0.0, null);
            }
            return stat;
        }

        public override string UnlockLangTokenName => "LASERBOLTCAREER";

        public override string UnlockName => "Stunning Precision";

        public override string AchievementName => "Stunning Precision";

        public override string AchievementDesc => $"stun {stunRequirementTotal} enemies.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => GetSpriteProvider("LaserboltIcon");

        public override void Init(ConfigFile config)
        { 
            base.CreateLang();
        }

        public override void OnInstall()
        {
            On.RoR2.SetStateOnHurt.OnTakeDamageServer += AddStunCounter;

            base.OnInstall();
        }

        public override void OnUninstall()
        {
            On.RoR2.SetStateOnHurt.OnTakeDamageServer -= AddStunCounter;

            base.OnUninstall();
        }

        private void AddStunCounter(On.RoR2.SetStateOnHurt.orig_OnTakeDamageServer orig, SetStateOnHurt self, DamageReport damageReport)
        {
            DamageInfo damageInfo = damageReport.damageInfo;

            if (damageInfo.procCoefficient > 0 && damageReport.attackerBodyIndex == LookUpRequiredBodyIndex())
            {
                HealthComponent hc = self.targetStateMachine?.commonComponents.healthComponent;
                bool isStunnedAlready = false;
                if (hc != null)
                {
                    if (hc.isInFrozenState)
                    {
                        isStunnedAlready = true;
                    }
                }

                if (self.canBeStunned && !isStunnedAlready && (damageInfo.damageType.HasFlag(DamageType.Stun1s)))
                {
                    StatSheet currentStats = damageReport.attackerMaster.playerStatsComponent.currentStats;
                    currentStats.PushStatValue(stunCounter, 1UL);
                    if (base.userProfile.statSheet.GetStatValueULong(stunCounter) >= stunRequirementTotal)
                    {
                        base.Grant();
                    }
                }
            }
            orig(self, damageReport);
        }
    }
}