using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    class MeteoriteDeathUnlock : UnlockBase
    {
        GameObject meteorGameObject = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm");

        public override string UnlockLangTokenName => "METEOR";

        public override string UnlockName => "Cloudy, With A Chance Of...";

        public override string AchievementName => "Cloudy, With A Chance Of...";

        public override string AchievementDesc => "kill yourself with a Glowing Meteorite.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => GetSpriteProvider("meteoricon");

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

        public override void OnInstall()
        {
            GlobalEventManager.onCharacterDeathGlobal += MeteorCheck;

            base.OnInstall();
        }

        public override void OnUninstall()
        {
            GlobalEventManager.onCharacterDeathGlobal -= MeteorCheck;

            base.OnUninstall();
        }

        private void MeteorCheck(DamageReport obj)
        {
            CharacterBody attackerBody = obj.attackerBody;
            CharacterBody victimBody = obj.victimBody;
            if(attackerBody == victimBody && attackerBody.bodyIndex == LookUpRequiredBodyIndex())
            {
                GameObject inflictor = obj.damageInfo.inflictor;
                MeteorStormController msc = inflictor.GetComponent<MeteorStormController>();
                if (msc != null)
                {
                    base.Grant();
                }
            }
        }
    }
}