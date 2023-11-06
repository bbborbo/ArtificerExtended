using ArtificerExtended.Components;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static ArtificerExtended.Components.ElementCounter;
using static ArtificerExtended.Passive.AltArtiPassive;

namespace ArtificerExtended.Unlocks
{
    class ArtificerEnergyPassiveUnlock : UnlockBase
    {
        public override string UnlockLangTokenName => "ENERGYPASSIVE";

        public override string UnlockName => "Elemental Intensity";

        public override string AchievementName => "Elemental Intensity";

        public override string AchievementDesc => "equip 4 abilities of a single element at once.";

        public override string PrerequisiteUnlockableIdentifier => "FreeMage";

        public override Sprite Sprite => GetSpriteProvider("ElementalIntensity");

        public override void Init(ConfigFile config)
        {
            base.CreateLang();
        }

        public override void OnInstall()
        {
            On.EntityStates.Mage.MageCharacterMain.OnEnter += PowerCheck;
            base.OnInstall();
        }

        public override void OnUninstall()
        {
            On.EntityStates.Mage.MageCharacterMain.OnEnter -= PowerCheck;
            base.OnUninstall();
        }


        private void PowerCheck(On.EntityStates.Mage.MageCharacterMain.orig_OnEnter orig, global::EntityStates.Mage.MageCharacterMain self)
        {
            orig(self);
            ElementCounter power = self.characterBody?.GetComponent<ElementCounter>();
            if (power != null)
            {
                if (power.firePower >= Power.Extreme || power.icePower >= Power.Extreme || power.lightningPower >= Power.Extreme)
                {
                    base.Grant();
                }
            }
        }
    }
}