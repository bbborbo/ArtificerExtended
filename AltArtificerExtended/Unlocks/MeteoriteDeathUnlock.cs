using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(MeteoriteDeathUnlock), nameof(MeteoriteDeathUnlock), "FreeMage", 5, null)]
    class MeteoriteDeathUnlock : UnlockBase
    {
        GameObject meteorGameObject = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm");

        public override string TOKEN_IDENTIFIER => nameof(MeteoriteDeathUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: Cloudy, With A Risk Of...";

        public override string AchievementDesc => $"As Artificer, kill yourself with a meteor strike.";

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
                    if(attackerBody.equipmentSlot.equipmentIndex == RoR2Content.Equipment.Meteor.equipmentIndex)
                    {
                        base.Grant();
                    }
                }
            }
        }
    }
}