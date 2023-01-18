using AltArtificerExtended.Unlocks;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using AncientScepter;

namespace AltArtificerExtended.Skills
{
    public abstract class ScepterSkillBase<T> : ScepterSkillBase where T : ScepterSkillBase<T>
    {
        public static T instance { get; private set; }

        public ScepterSkillBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ArtificerExtended SkillBase was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class ScepterSkillBase
    {
        public static Dictionary<string, SkillDef> ScepterDictionary = new Dictionary<string, SkillDef>();

        public virtual string TargetBodyName { get; set; } = "MageBody";
        public virtual SkillSlot TargetSlot { get; set; } = RoR2.SkillSlot.Special;
        public abstract int TargetVariant { get; }

        public static string Token = ArtificerExtendedPlugin.TokenName + "SKILL";
        public abstract string SkillName { get; }
        public abstract string SkillDescription { get; }
        public abstract string SkillLangTokenName { get; }
        public abstract string IconName { get; }

        public abstract MageElement Element { get; }
        public abstract Type ActivationState { get; }
        public abstract SimpleSkillData SkillData { get; }
        public virtual bool isSpecial { get; set; } = false;

        string GetElementString(MageElement type)
        {
            string s = "";

            switch (type)
            {
                case MageElement.Fire:
                    s = "_FIRE";
                    break;
                case MageElement.Lightning:
                    s = "_LIGHTNING";
                    break;
                case MageElement.Ice:
                    s = "_ICE";
                    break;
            }

            return s;
        }

        public abstract void Init(ConfigFile config);

        protected void CreateLang()
        {
            LanguageAPI.Add(Token + SkillLangTokenName + GetElementString(Element), SkillName);
            LanguageAPI.Add(Token + SkillLangTokenName + "_DESCRIPTION", SkillDescription);
        }

        protected void CreateSkill()
        {
            //Debug.Log(s);
            if (TargetBodyName == "" || !ArtificerExtendedPlugin.isScepterLoaded)
                return;

            var skillDef = ScriptableObject.CreateInstance<SkillDef>();

            RegisterEntityState(ActivationState);
            skillDef.activationState = new SerializableEntityStateType(ActivationState);

            skillDef.skillNameToken = Token + SkillLangTokenName + GetElementString(Element);
            skillDef.skillName = SkillName;
            skillDef.skillDescriptionToken = Token + SkillLangTokenName + "_DESCRIPTION";
            skillDef.activationStateMachineName = "Weapon";

            skillDef.icon = ArtificerExtendedPlugin.iconBundle.LoadAsset<Sprite>(ArtificerExtendedPlugin.iconsPath + IconName + ".png");

            #region SkillData
            skillDef.baseMaxStock = SkillData.baseMaxStock;
            skillDef.baseRechargeInterval = SkillData.baseRechargeInterval;
            skillDef.beginSkillCooldownOnSkillEnd = SkillData.beginSkillCooldownOnSkillEnd;
            skillDef.canceledFromSprinting = ArtificerExtendedPlugin.autosprintLoaded ? false : SkillData.canceledFromSprinting;
            skillDef.cancelSprintingOnActivation = SkillData.cancelSprintingOnActivation;
            skillDef.dontAllowPastMaxStocks = SkillData.dontAllowPastMaxStocks;
            skillDef.fullRestockOnAssign = SkillData.fullRestockOnAssign;
            skillDef.interruptPriority = SkillData.interruptPriority;
            skillDef.isCombatSkill = SkillData.isCombatSkill;
            skillDef.mustKeyPress = SkillData.mustKeyPress;
            skillDef.rechargeStock = SkillData.rechargeStock;
            skillDef.requiredStock = SkillData.requiredStock;
            skillDef.resetCooldownTimerOnUse = SkillData.resetCooldownTimerOnUse;
            skillDef.stockToConsume = SkillData.stockToConsume;
            #endregion

            bool b = AncientScepterItem.instance.RegisterScepterSkill(skillDef, TargetBodyName, TargetSlot, TargetVariant);
            if (b)
            {
                ContentPacks.skillDefs.Add(skillDef);
                ScepterDictionary.Add(SkillName, skillDef);
            }
        }

        public abstract void Hooks();

        internal UnlockableDef GetUnlockDef(Type type)
        {
            UnlockableDef u = null;

            foreach (KeyValuePair<UnlockBase, UnlockableDef> keyValuePair in ArtificerExtendedPlugin.UnlockBaseDictionary)
            {
                string key = keyValuePair.Key.ToString();
                UnlockableDef value = keyValuePair.Value;
                if (key == type.ToString())
                {
                    u = value;
                    //Debug.Log($"Found an Unlock ID Match {value} for {type.Name}! ");
                    break;
                }
            }

            return u;
        }
        public static bool RegisterEntityState(Type entityState)
        {
            //Check if the entity state has already been registered, is abstract, or is not a subclass of the base EntityState
            if (ArtificerExtendedPlugin.entityStates.Contains(entityState) || !entityState.IsSubclassOf(typeof(EntityStates.EntityState)) || entityState.IsAbstract)
            {
                //LogCore.LogE(entityState.AssemblyQualifiedName + " is either abstract, not a subclass of an entity state, or has already been registered.");
                //LogCore.LogI("Is Abstract: " + entityState.IsAbstract + " Is not Subclass: " + !entityState.IsSubclassOf(typeof(EntityState)) + " Is already added: " + EntityStateDefinitions.Contains(entityState));
                return false;
            }
            //If not, add it to our EntityStateDefinitions
            ArtificerExtendedPlugin.entityStates.Add(entityState);
            return true;
        }
    }
}
