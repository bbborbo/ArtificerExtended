using ArtificerExtended.Components;
using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    [RegisterAchievement(nameof(UgornsMusicUnlock), nameof(UgornsMusicUnlock), "FreeMage", 5, null)]
    class UgornsMusicUnlock : UnlockBase
    {
        //public override bool ForceDisable => true;
        public override string TOKEN_IDENTIFIER => nameof(UgornsMusicUnlock).ToUpperInvariant();

        public override string AchievementName => "Artificer: Ugorn\u2019s Music";

        public override string AchievementDesc => $"As Artificer, with at least one Lightning ability equipped, obtain a Charged Perforator and Royal Capacitor, OR become an aspect of lightning.";

        #region implementation
        private Inventory currentInventory;
        public override void OnInstall()
        {
            base.OnInstall();
        }

        public override void OnUninstall()
        {
            this.SetCurrentInventory(null);
            base.OnUninstall();
        }

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

        public override void OnBodyRequirementMet()
        {
            base.OnBodyRequirementMet();
            base.localUser.onMasterChanged += this.UpdateInventory;
            this.UpdateInventory();
        }

        public override void OnBodyRequirementBroken()
        {
            base.localUser.onMasterChanged -= this.UpdateInventory;
            this.SetCurrentInventory(null);
            base.OnBodyRequirementBroken();
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
        #endregion

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
    }
}