using AltArtificerExtended.Passive;
using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AltArtificerExtended.Components
{
    public class ElementCounter : MonoBehaviour
    {
        public enum Power
        {
            None = 0,
            Low,
            Medium,
            High,
            Extreme,
            Unfathomable
        }

        internal bool useIceAspect = false;
        internal bool useFireAspect = false;
        internal bool useLightningAspect = false;
        public Power firePower;
        public Power icePower;
        public Power lightningPower;
        SkillLocator loc = null;

        public void GetPowers(EquipmentIndex equipIndex, bool updateEquipment = false, SkillLocator skillLocator = null)
        {
            if(loc == null)
            {
                if(skillLocator == null)
                {
                    Debug.Log("Element Counter has no state assigned!");
                    return;
                }
                loc = skillLocator;
            }

            this.firePower = Power.None;
            this.icePower = Power.None;
            this.lightningPower = Power.None;

            this.GetSkillPower(loc.primary);
            this.GetSkillPower(loc.secondary);
            this.GetSkillPower(loc.utility);
            this.GetSkillPower(loc.special);
            if(updateEquipment)
                this.GetEquipPower(equipIndex);

            Debug.Log($"Fire: {this.firePower}\nIce: {this.icePower}\nLightning: {this.lightningPower}");
        }

        private void GetEquipPower(EquipmentIndex equipIndex)
        {
            if (equipIndex == RoR2Content.Equipment.AffixRed.equipmentIndex)
            {
                this.firePower++;
                //Debug.Log("Adding Fire power from affix!");
            }
            if (equipIndex == RoR2Content.Equipment.AffixWhite.equipmentIndex)
            {
                this.icePower++;
                //Debug.Log("Adding Ice power from affix!");
            }
            if (equipIndex == RoR2Content.Equipment.AffixBlue.equipmentIndex)
            {
                this.lightningPower++;
                //Debug.Log("Adding Lightning power from affix!");
            }

            return;
            /*
            if (body.HasBuff(RoR2Content.Buffs.AffixRed))
            {
                this.firePower++;
                Debug.Log("Adding Fire power from affix!");
            }
            if (body.HasBuff(RoR2Content.Buffs.AffixWhite))
            {
                this.icePower++;
                Debug.Log("Adding Ice power from affix!");
            }
            if (body.HasBuff(RoR2Content.Buffs.AffixBlue))
            {
                this.lightningPower++;
                Debug.Log("Adding Lightning power from affix!");
            }*/
        }

        private void GetSkillPower(GenericSkill skill)
        {
            bool isProperToken = skill.baseSkill.skillNameToken.Contains("_");
            if (isProperToken)
            {
                String name = skill.baseSkill.skillNameToken.Split('_')[2].ToLower();
                switch (name)
                {
                    default:
                        Debug.Log($"Element: {name} is not handled");
                        break;
                    case "fire":
                        this.firePower++;
                        break;
                    case "ice":
                        this.icePower++;
                        break;
                    case "lightning":
                        this.lightningPower++;
                        break;
                }
            }

            EquipmentIndex equipIndex = EquipmentIndex.None;
            if(skill.characterBody.equipmentSlot != null)
            {
                equipIndex = skill.characterBody.equipmentSlot.equipmentIndex;
            }

            skill.onSkillChanged += (s) => this.GetPowers(equipIndex);
        }

        #region static methods
        public static Power GetIcePowerLevelFromBody(CharacterBody body, ElementCounter powerComponent = null)
        {
            Power power = Power.None;

            if (body == null)
                return power;
            if (powerComponent == null)
            {
                powerComponent = GetPowerComponentFromBody(body);
            }

            bool useAspect = true;
            BuffDef aspect = RoR2Content.Buffs.AffixWhite;
            if (powerComponent != null)
            {
                power = powerComponent.icePower;
                useAspect = powerComponent.useIceAspect;
            }

            return GetPowerLevelFromBody(body, power, aspect, useAspect);
        }

        public static Power GetFirePowerLevelFromBody(CharacterBody body, ElementCounter powerComponent = null)
        {
            Power power = Power.None;

            if (body == null)
                return power;
            if (powerComponent == null)
            {
                powerComponent = GetPowerComponentFromBody(body);
            }

            bool useAspect = true;
            BuffDef aspect = RoR2Content.Buffs.AffixRed;
            if (powerComponent != null)
            {
                power = powerComponent.firePower;
                useAspect = powerComponent.useFireAspect;
            }

            return GetPowerLevelFromBody(body, power, aspect, useAspect);
        }

        public static Power GetLightningPowerLevelFromBody(CharacterBody body, ElementCounter powerComponent = null)
        {
            Power power = Power.None;

            if (body == null)
                return power;
            if (powerComponent == null)
            {
                powerComponent = GetPowerComponentFromBody(body);
            }

            bool useAspect = true;
            BuffDef aspect = RoR2Content.Buffs.AffixBlue;
            if (powerComponent != null)
            {
                power = powerComponent.lightningPower;
                useAspect = powerComponent.useLightningAspect;
            }

            return GetPowerLevelFromBody(body, power, aspect, useAspect);
        }

        public static Power GetPowerLevelFromBody(CharacterBody body, Power skillPower, BuffDef aspectDef = null, bool useAspect = false)
        {
            Power power = Power.None;
            if (AltArtiPassive.instanceLookup.ContainsKey(body.gameObject))
            {
                power = skillPower;
            }
            if (useAspect == true && power < Power.Unfathomable)
            {
                if (aspectDef != null && body.HasBuff(aspectDef))
                {
                    power++;
                }
            }

            return power;
        }

        public static ElementCounter GetPowerComponentFromBody(CharacterBody body)
        {
            ElementCounter powerComponent = null;
            if (AltArtiPassive.instanceLookup.ContainsKey(body.gameObject))
            {
                AltArtiPassive AApassive = AltArtiPassive.instanceLookup[body.gameObject];
                powerComponent = AApassive.elementPower;
            }
            else
            {
                powerComponent = body.gameObject.GetComponent<ElementCounter>();
            }
            return powerComponent;
        }
        #endregion
    }
}
