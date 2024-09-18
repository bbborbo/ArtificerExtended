using ArtificerExtended.Unlocks;
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

namespace ArtificerExtended.Skills
{
    public abstract class SkillBase<T> : SkillBase where T : SkillBase<T>
    {
        public static T instance { get; private set; }

        public SkillBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ArtificerExtended SkillBase was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class SkillBase
    {
        public static string Token = ArtificerExtendedPlugin.TokenName + "SKILL";
        public abstract string SkillName { get; }
        public abstract string SkillDescription { get; }
        public abstract string SkillLangTokenName { get; }

        //public abstract string UnlockString { get; }
        public abstract UnlockableDef UnlockDef { get; }
        public abstract string IconName { get; }

        public abstract MageElement Element { get; }
        public abstract Type ActivationState { get; }
        public abstract SkillFamily SkillSlot { get; }
        public abstract SimpleSkillData SkillData { get; }
        public string[] KeywordTokens;
        public virtual bool useSteppedDef { get; set; } = false;

        string GetElementString(MageElement type)
        {
            string s = "";

            switch (type)
            {
                default:
                    s = "_MAGIC";
                    break;
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
            string s = $"ArtificerExtended: {SkillName} initializing to unlock {(UnlockDef != null ? UnlockDef.cachedName : "(null)")}!";
            //Debug.Log(s);

            var skillDef = ScriptableObject.CreateInstance<SkillDef>();
            if (useSteppedDef)
            {
                skillDef = ScriptableObject.CreateInstance<SteppedSkillDef>();
            }

            RegisterEntityState(ActivationState);
            skillDef.activationState = new SerializableEntityStateType(ActivationState);

            skillDef.skillNameToken = Token + SkillLangTokenName + GetElementString(Element);
            skillDef.skillName = SkillName;
            skillDef.skillDescriptionToken = Token + SkillLangTokenName + "_DESCRIPTION";

            skillDef.keywordTokens = KeywordTokens;
            if(IconName != "")
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
            skillDef.attackSpeedBuffsRestockSpeed = SkillData.useAttackSpeedScaling;
            skillDef.activationStateMachineName = SkillData.activationStateMachineName;
            #endregion

            ContentPacks.skillDefs.Add(skillDef);
            Array.Resize(ref SkillSlot.variants, SkillSlot.variants.Length + 1);
            SkillSlot.variants[SkillSlot.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = skillDef,
                unlockableDef = UnlockDef,
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null)
            };
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
