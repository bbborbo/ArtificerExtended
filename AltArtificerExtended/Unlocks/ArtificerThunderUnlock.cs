using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    class ArtificerThunderUnlock : UnlockBase
    {
        //public override bool ForceDisable => true;

        public override string UnlockLangTokenName => "THUNDER";

        public override string UnlockName => "Ugorn\u2019s Music";

        public override string AchievementName => "Ugorn\u2019s Music";

        public override string AchievementDesc => "land the killing blow on an Imp Overlord with a powerful lightning strike.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => GetSpriteProvider("thundericon");

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

        public override void OnInstall()
        {
            GlobalEventManager.onCharacterDeathGlobal += ImpBossSmiteCheck;

            base.OnInstall();
        }

        public override void OnUninstall()
        {
            GlobalEventManager.onCharacterDeathGlobal -= ImpBossSmiteCheck;

            base.OnUninstall();
        }

        private void ImpBossSmiteCheck(DamageReport obj)
        {
            CharacterBody attackerBody = obj.attackerBody;
            CharacterBody victimBody = obj.victimBody;
            DamageInfo damageInfo = obj.damageInfo;
            if (attackerBody && victimBody && damageInfo != null)
            {
                bool isImpOverlord = victimBody.bodyIndex == BodyCatalog.FindBodyIndex("ImpBossBody");
                if (attackerBody.bodyIndex == LookUpRequiredBodyIndex() && isImpOverlord)
                {
                    if((damageInfo.force == Vector3.down * 1500 
                        || (damageInfo.force == Vector3.down * 3000 && damageInfo.damageType.HasFlag(DamageType.Stun1s))) 
                        && damageInfo.inflictor == null) //only the orbs use a null inflictor! && damageInfo.inflictor == null) //only the orbs use a null inflictor!
                    {
                        base.Grant();
                    }
                }
            }
        }
    }
}