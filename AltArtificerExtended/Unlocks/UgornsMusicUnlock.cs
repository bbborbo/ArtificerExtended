using ArtificerExtended.Components;
using ArtificerExtended.Passive;
using Assets.RoR2.Scripts.Platform;
using BepInEx.Configuration;
using RoR2;
using RoR2.Achievements;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(UgornsMusicUnlock), nameof(UgornsMusicUnlock), "FreeMage", 5, typeof(UgornsMusicServerAchievement))]
    class UgornsMusicUnlock : UnlockBase
    {
        private class UgornsMusicServerAchievement : BaseServerAchievement
        {
            CharacterBody trackedBody;
            public override void OnInstall()
            {
                base.OnInstall();
                GlobalEventManager.onCharacterDeathGlobal += OnDeathResonanceCheck;
            }

            public override void OnUninstall()
            {
                base.OnUninstall();
                GlobalEventManager.onCharacterDeathGlobal -= OnDeathResonanceCheck;
            }

            private void OnDeathResonanceCheck(DamageReport damageReport)
            {
                CharacterBody currentBody = base.networkUser.GetCurrentBody();
                if (!currentBody)
                {
                    return;
                }

                CharacterBody victimBody = damageReport.victimBody;
                if (victimBody.bodyIndex == BodyCatalog.FindBodyIndex("ElectricWormBody")
                    && (int)ElementCounter.GetPowerLevelFromBody(currentBody.gameObject, MageElement.Lightning) >= 1)
                {
                    base.Grant();
                    base.ServerTryToCompleteActivity();
                }
            }
        }
        //public override bool ForceDisable => true;
        public override string TOKEN_IDENTIFIER => nameof(UgornsMusicUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: Ugorn\u2019s Music";

        public override string AchievementDesc => $"As Artificer, with at least one Lightning ability equipped, triumph over an Overloading Worm.";

        #region implementation

        public override void TryToCompleteActivity()
        {
            bool flag = base.localUser.id == LocalUserManager.GetFirstLocalUser().id;
            if (this.shouldGrant && flag)
            {
                BaseActivitySelector baseActivitySelector = new BaseActivitySelector();
                baseActivitySelector.activityAchievementID = nameof(FreezeManySimultaneousUnlock);
                PlatformSystems.activityManager.TryToCompleteActivity(baseActivitySelector, true, true);
            }
        }

        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            base.SetServerTracked(true);
        }
        public override void OnBodyRequirementBroken()
        {
            base.OnBodyRequirementBroken();
            base.SetServerTracked(false);
        }

        public override void OnInstall()
        {
            base.OnInstall();
        }

        public override void OnUninstall()
        {
            base.OnUninstall();
        }
        #endregion

        #region legacy

        private Inventory currentInventory;
        private void UpdateInventory()
        {
            Inventory inventory = null;
            if (base.localUser.cachedMasterController)
            {
                inventory = base.localUser.cachedMasterController.master.inventory;
            }
            this.SetCurrentInventory(inventory);
        }

        private void SetCurrentInventory(Inventory newInventory)
        {
            if (this.currentInventory == newInventory)
            {
                return;
            }
            if (this.currentInventory != null)
            {
                this.currentInventory.onInventoryChanged -= this.OnInventoryChanged;
            }
            this.currentInventory = newInventory;
            if (this.currentInventory != null)
            {
                this.currentInventory.onInventoryChanged += this.OnInventoryChanged;
                this.OnInventoryChanged();
            }
        }

        private void OnInventoryChanged()
        {
            //if the inventory belongs to the local user and the local user meets the body requirement
            GameObject localBodyObject = this.localUser.cachedBodyObject;
            if (currentInventory.gameObject == localBodyObject && base.meetsBodyRequirement)
            {
                ElementCounter elementCounter = localBodyObject.GetComponent<ElementCounter>();
                if (elementCounter != null && elementCounter.lightningPower >= ElementCounter.Power.Low)
                {
                    if (currentInventory.currentEquipmentIndex == RoR2Content.Equipment.AffixBlue.equipmentIndex)
                    {
                        base.Grant();
                        return;
                    }
                    if (currentInventory.currentEquipmentIndex == RoR2Content.Equipment.Lightning.equipmentIndex)
                    {
                        if (currentInventory.GetItemCount(RoR2Content.Items.LightningStrikeOnHit) > 0)
                        {
                            base.Grant();
                        }
                    }
                }
            }
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
                    if ((damageInfo.force == Vector3.down * 1500
                        || (damageInfo.force == Vector3.down * 3000 && damageInfo.damageType.damageType.HasFlag(DamageType.Stun1s)))
                        && damageInfo.inflictor == null) //only the orbs use a null inflictor! && damageInfo.inflictor == null) //only the orbs use a null inflictor!
                    {
                        base.Grant();
                    }
                }
            }
        }
        #endregion
    }
}