using ArtificerExtended.Passive;
using EntityStates;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Components
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

        public void OnBodyStart(SkillLocator skillLocator = null)
        {
            if (loc == null)
            {
                if (skillLocator == null)
                {
                    Debug.Log("Element Counter has no state assigned!");
                    return;
                }
                loc = skillLocator;
            }

            loc.primary.onSkillChanged += (s) => RecalculatePowers();
            loc.secondary.onSkillChanged += (s) => RecalculatePowers();
            loc.utility.onSkillChanged += (s) => RecalculatePowers();
            loc.special.onSkillChanged += (s) => RecalculatePowers();

            RecalculatePowers();
        }

        public void OnBodyEnd()
        {
            if (loc == null)
                return;

            loc.primary.onSkillChanged -= (s) => RecalculatePowers();
            loc.secondary.onSkillChanged -= (s) => RecalculatePowers();
            loc.utility.onSkillChanged -= (s) => RecalculatePowers();
            loc.special.onSkillChanged -= (s) => RecalculatePowers();
        }

        public void RecalculatePowers()
        {
            this.firePower = Power.None;
            this.icePower = Power.None;
            this.lightningPower = Power.None;

            this.GetPowerFromSkill(loc.primary);
            this.GetPowerFromSkill(loc.secondary);
            this.GetPowerFromSkill(loc.utility);
            this.GetPowerFromSkill(loc.special);


            Debug.Log($"Fire: {this.firePower}\nIce: {this.icePower}\nLightning: {this.lightningPower}");
        }

        private void GetPowerFromSkill(GenericSkill skill)
        {
            bool isProperToken = skill.baseSkill.skillNameToken.Contains("_");
            if (isProperToken)
            {
                string[] s = skill.baseSkill.skillNameToken.Split('_');
                String name = s.Length > 2 ? s[2].ToLower() : "";
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
        }

        #region static methods

        public static Power GetPowerLevelFromBody(GameObject body, MageElement element, AltArtiPassive altArtiPassive)
        {
            ElementCounter elementCounter = null;
            if(altArtiPassive != null)
            {
                elementCounter = altArtiPassive.elementPower;
            }
            return GetPowerLevelFromBody(body, element, elementCounter);
        }
        public static Power GetPowerLevelFromBody(GameObject body, MageElement element, ElementCounter powerComponent = null)
        {
            Power power = Power.None;

            if (body == null)
                return power;
            if (powerComponent == null)
            {
                powerComponent = GetPowerComponentFromBody(body);
            }

            if (powerComponent != null)
            {
                bool useAspect = false;
                switch (element)
                {
                    case MageElement.Fire:
                        power = powerComponent.firePower;
                        useAspect = powerComponent.useFireAspect;
                        break;
                    case MageElement.Lightning:
                        power = powerComponent.lightningPower;
                        useAspect = powerComponent.useLightningAspect;
                        break;
                    case MageElement.Ice:
                        power = powerComponent.icePower;
                        useAspect = powerComponent.useIceAspect;
                        break;
                }
                if (useAspect == true && power < Power.Unfathomable)
                {

                }
            }

            return power;
        }

        public static ElementCounter GetPowerComponentFromBody(GameObject body)
        {
            ElementCounter powerComponent = null;
            if (AltArtiPassive.instanceLookup.ContainsKey(body))
            {
                AltArtiPassive AApassive = AltArtiPassive.instanceLookup[body];
                powerComponent = AApassive.elementPower;
            }
            else
            {
                powerComponent = body.GetComponent<ElementCounter>();
            }
            return powerComponent;
        }
        #endregion
    }
}