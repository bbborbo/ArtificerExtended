using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    class FreezeManySimultaneousUnlock : UnlockBase
    {
        public int freezeRequirementTotal = 5;
        static BuffDef AvalancheBuff;

        public override string UnlockLangTokenName => "FREEZEMANYSIMULTANEOUS";

        public override string UnlockName => "Ice V Has Arrived";

        public override string AchievementName => "Ice V Has Arrived";

        public override string AchievementDesc => $"have {freezeRequirementTotal} monsters frozen at once.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => base.GetSpriteProvider("SnowballIcon");

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

        private static void CreateBuff(On.RoR2.BuffCatalog.orig_Init orig)
        {
            orig();
            AvalancheBuff = RoR2Content.Buffs.OnFire;

            BuffDef buff = ScriptableObject.CreateInstance<BuffDef>();
            {
                buff.name = "avalancheUnlockTracker";
                buff.iconSprite = RoR2.LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffGenericShield");
                buff.buffColor = Color.red;
                buff.canStack = true;
                buff.isDebuff = false;
            }

            //AvalancheBuff = buff;
            //Main.buffDefs.Add(buff);
            //Buffs.RegisterBuff(buff);
        }

        private void AddFreezeCounter(On.RoR2.SetStateOnHurt.orig_OnTakeDamageServer orig, SetStateOnHurt self, DamageReport damageReport)
        {
            if (damageReport != null && self.targetStateMachine && self.spawnedOverNetwork)
            {
                DamageInfo damageInfo = damageReport.damageInfo;

                if (damageReport.attackerBodyIndex == LookUpRequiredBodyIndex())
                {
                    HealthComponent hc = self.targetStateMachine?.commonComponents.healthComponent;
                    bool isFrozenAlready = false;
                    if (hc != null)
                        isFrozenAlready = hc.isInFrozenState;

                    if (damageInfo.procCoefficient > 0 && self.canBeFrozen && !isFrozenAlready && (damageInfo.damageType & DamageType.Freeze2s) != DamageType.Generic && damageReport.attackerBody != null)
                    {
                        Debug.Log(damageReport.attackerBody.name);
                        damageReport.attackerBody.AddTimedBuffAuthority(RoR2Content.Buffs.OnFire.buffIndex, 2 * damageInfo.procCoefficient);

                        int buffCount = damageReport.attackerBody.GetBuffCount(RoR2Content.Buffs.OnFire);
                        if (buffCount >= freezeRequirementTotal)
                        {
                            base.Grant();
                        }
                    }
                }
            }
            orig(self, damageReport);
        }

        public override void OnInstall()
        {
            On.RoR2.BuffCatalog.Init += CreateBuff;
            On.RoR2.SetStateOnHurt.OnTakeDamageServer += AddFreezeCounter;

            base.OnInstall();
        }

        public override void OnUninstall()
        {
            On.RoR2.BuffCatalog.Init -= CreateBuff;
            On.RoR2.SetStateOnHurt.OnTakeDamageServer -= AddFreezeCounter;

            base.OnUninstall();
        }
    }
}
