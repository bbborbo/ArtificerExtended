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
using ArtificerExtended.Modules;
using UnityEngine.AddressableAssets;
using ArtificerExtended;
using AncientScepter;
using System.Runtime.CompilerServices;
using ArtificerExtended.Unlocks;

namespace ArtificerExtended.Skills
{
    public abstract class SkillBase<T> : SkillBase where T : SkillBase<T>
    {
        public static T instance { get; private set; }

        public SkillBase()
        {
            if (instance != null) throw new InvalidOperationException(
                $"Singleton class \"{typeof(T).Name}\" inheriting {ArtificerExtendedPlugin.modName} {typeof(SkillBase).Name} was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class SkillBase : SharedBase
    {
        public override string BASE_TOKEN => base.BASE_TOKEN + GetElementString(Element);
        public override string TOKEN_PREFIX { get; } = "SKILL_";
        public override AssetBundle assetBundle => ArtificerExtendedPlugin.iconBundle;
        public override string ConfigName => "Skills : " + SkillName;
        public abstract string SkillName { get; }
        public abstract string SkillDescription { get; }

        //public abstract string UnlockString { get; }
        public abstract Sprite Icon { get; }
        public abstract Type ActivationState { get; }
        public abstract Type BaseSkillDef { get; }
        public virtual string CharacterName { get; set; } = "MageBody";
        public abstract SkillSlot SkillSlot { get; }
        public abstract float BaseCooldown { get; }
        public abstract InterruptPriority InterruptPriority { get; }
        public abstract SimpleSkillData SkillData { get; }
        public string[] KeywordTokens;
        public virtual string ActivationStateMachineName { get; set; } = "Weapon";
        public SkillDef SkillDef { 
            get 
            {
                if (_SkillDef == null)
                    _SkillDef = (SkillDef)ScriptableObject.CreateInstance(BaseSkillDef);
                return _SkillDef;
            }
            set
            {
                _SkillDef = value;
            }
        }
        private SkillDef _SkillDef;
        public SkillDef ScepterSkillDef
        {
            get
            {
                return _ScepterSkillDef;
            }
            private set
            {
                _ScepterSkillDef = value;
            }
        }
        private SkillDef _ScepterSkillDef;

        public virtual string ScepterSkillName { get; }
        public virtual string ScepterSkillDesc { get; }
        public virtual Type ScepterActivationState { get; }
        private int variantIndex;
        public UnlockableDef unlockDef;

        public abstract MageElement Element { get; }
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
        public override void Init()
        {
            base.Init();
            CreateSkill();
            if(RequiredUnlock != null)
            {
                unlockDef = UnlockBase.CreateUnlockDef(RequiredUnlock, Icon);
            }
            AddSkillToSkillFamily();

            if (ArtificerExtendedPlugin.isScepterLoaded && ScepterSkillName != null)
            {
                CreateScepterSkill();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void CreateScepterSkill()
        {
            ScepterSkillDef = ArtificerExtendedPlugin.CloneSkillDef(SkillDef);
            ScepterSkillDef.skillNameToken = BASE_TOKEN + "_SCEPTER_NAME";
            ScepterSkillDef.skillDescriptionToken = BASE_TOKEN + "_SCEPTER_DESC";

            ScepterSkillDef = ModifyScepterSkill(ScepterSkillDef);

            LanguageAPI.Add(BASE_TOKEN + "_SCEPTER_NAME", ScepterSkillName);
            LanguageAPI.Add(BASE_TOKEN + "_SCEPTER_DESC", SkillDescription + $"\n<color=#d299ff>SCEPTER: {ScepterSkillDesc}</color>");

            bool b = AncientScepterItem.instance.RegisterScepterSkill(ScepterSkillDef, CharacterName, SkillSlot, variantIndex);
            if (b)
            {
                Content.AddSkillDef(ScepterSkillDef);
            }
        }

        public virtual SkillDef ModifyScepterSkill(SkillDef scepterSkillDef)
        {
            return scepterSkillDef;
        }

        public override void Lang()
        {
            LanguageAPI.Add(BASE_TOKEN + "_NAME", SkillName);
            LanguageAPI.Add(BASE_TOKEN + "_DESCRIPTION", SkillDescription);
        }
        public Sprite LoadSpriteFromBundle(string name) { return assetBundle.LoadAsset<Sprite>(ArtificerExtendedPlugin.iconsPath + name + ".png"); }
        public Sprite LoadSpriteFromRor(string path) { return Addressables.LoadAssetAsync<Sprite>(path).WaitForCompletion(); }
        public Sprite LoadSpriteFromRorSkill(string path) { return Addressables.LoadAssetAsync<SkillDef>(path).WaitForCompletion().icon; }
        private void CreateSkill()
        {
            if(SkillDef == null)
                SkillDef = (SkillDef)ScriptableObject.CreateInstance(BaseSkillDef);

            Content.AddEntityState(ActivationState);
            SkillDef.activationState = new SerializableEntityStateType(ActivationState);

            SkillDef.SetName(SkillDef, BASE_TOKEN.ToLowerInvariant());
            SkillDef.skillNameToken = BASE_TOKEN + "_NAME";
            SkillDef.skillName = SkillName;
            SkillDef.skillDescriptionToken = BASE_TOKEN + "_DESCRIPTION";
            SkillDef.activationStateMachineName = ActivationStateMachineName;

            SkillDef.keywordTokens = KeywordTokens;
            SkillDef.icon = Icon; // assetBundle.LoadAsset<Sprite>(FortunesPlugin.iconsPath + "Skill/" + IconName + ".png");

            #region SkillData
            SkillDef.baseRechargeInterval = Bind(BaseCooldown, "Base Cooldown");
            SkillDef.baseMaxStock = Bind(SkillData.baseMaxStock, "Base Max Stock");
            SkillDef.rechargeStock = Mathf.Min(Bind(SkillData.rechargeStock, "Recharge Stock"), SkillDef.baseMaxStock);
            SkillDef.interruptPriority = this.InterruptPriority;
            SkillDef.beginSkillCooldownOnSkillEnd = SkillData.beginSkillCooldownOnSkillEnd;
            SkillDef.dontAllowPastMaxStocks = SkillData.dontAllowPastMaxStocks;
            SkillDef.fullRestockOnAssign = SkillData.fullRestockOnAssign;
            SkillDef.isCombatSkill = Bind(SkillData.isCombatSkill, "Is Combat Skill");
            SkillDef.mustKeyPress = Bind(SkillData.mustKeyPress, "Must Key Press", "Setting to FALSE will allow the skill to be recast after it ends as long as the button is held.");
            SkillDef.requiredStock = SkillData.requiredStock;
            SkillDef.resetCooldownTimerOnUse = SkillData.resetCooldownTimerOnUse;
            SkillDef.stockToConsume = SkillData.stockToConsume;

            SkillDef.cancelSprintingOnActivation = Bind(SkillData.cancelSprintingOnActivation, "Cancels Sprinting", "Recommended to use HuntressBuffULTIMATE for intended behavior.");
            SkillDef.forceSprintDuringState = Bind(SkillData.forceSprintingDuringState, "Force Sprinting During State", "Used by mobility skills.");
            this.SkillDef.canceledFromSprinting = 
                !(ArtificerExtendedPlugin.autosprintLoaded || !SkillData.cancelSprintingOnActivation || SkillData.forceSprintingDuringState)
                && Bind(SkillData.canceledFromSprinting, "Canceled From Sprinting", 
                "Note: Only set to true if AUTOSPRINT isnt loaded, the skill cancels sprinting, and the skill doesn't force sprinting. " +
                "This avoids situations where the skill can cancel itself without additional input.");
            #endregion

            Content.AddSkillDef(SkillDef);
        }
        protected void AddSkillToSkillFamily()
        {
            //if the skill shouldnt initialize to a character
            if (/*SkillSlot != SkillSlot.None ||*/ string.IsNullOrEmpty(CharacterName))
                return;

            string s = Log.Combine("Skills", SkillName);
            SkillLocator skillLocator;
            string name = CharacterName;
            if (ArtificerExtended.Modules.Skills.characterSkillLocators.ContainsKey(name))
            {
                skillLocator = ArtificerExtended.Modules.Skills.characterSkillLocators[name];
            }
            else
            {
                GameObject body = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/" + name);
                skillLocator = body?.GetComponent<SkillLocator>();

                if (skillLocator)
                {
                    ArtificerExtended.Modules.Skills.characterSkillLocators.Add(name, skillLocator);
                }
                /*
                GameObject body = null;// RalseiSurvivor.instance.bodyPrefab;
                skillLocator = body?.GetComponent<SkillLocator>();
                if (skillLocator)
                {
                    Modules.Skills.characterSkillLocators.Add(name, skillLocator);
                }

                LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/" + name);
                skillLocator = body?.GetComponent<SkillLocator>();

                if (skillLocator)
                {
                    Modules.Skills.characterSkillLocators.Add(name, skillLocator);
                }*/
            }

            if (skillLocator != null)
            {
                SkillFamily skillFamily = null;

                //get skill family from skill slot
                switch (SkillSlot)
                {
                    case SkillSlot.Primary:
                        skillFamily = skillLocator.primary.skillFamily;
                        break;
                    case SkillSlot.Secondary:
                        skillFamily = skillLocator.secondary.skillFamily;
                        break;
                    case SkillSlot.Utility:
                        skillFamily = skillLocator.utility.skillFamily;
                        break;
                    case SkillSlot.Special:
                        skillFamily = skillLocator.special.skillFamily;
                        break;
                    case SkillSlot.None:
                        Log.Warning(s + "Special case!");
                        break;
                }

                if (skillFamily != null)
                {
                    Log.Debug(s + "initializing!");

                    variantIndex = skillFamily.variants.Length;
                    Array.Resize(ref skillFamily.variants, variantIndex + 1);
                    skillFamily.variants[variantIndex] = new SkillFamily.Variant
                    {
                        skillDef = SkillDef,
                        unlockableDef = unlockDef,
                        viewableNode = new ViewablesCatalog.Node(SkillDef.skillNameToken, false, null)
                    };
                    Log.Debug(s + "success!");
                }
                else
                {
                    Log.Error(s + $"No skill family {SkillSlot.ToString()} found from " + CharacterName);
                }
            }
            else
            {
                Log.Error(s + "No skill locator found from " + CharacterName);
            }
        }

        internal UnlockableDef GetUnlockDef(Type type)
        {
            UnlockableDef u = null;

            /*foreach (KeyValuePair<UnlockBase, UnlockableDef> keyValuePair in Main.UnlockBaseDictionary)
            {
                string key = keyValuePair.Key.ToString();
                UnlockableDef value = keyValuePair.Value;
                if (key == type.ToString())
                {
                    u = value;
                    //Debug.Log($"Found an Unlock ID Match {value} for {type.Name}! ");
                    break;
                }
            }*/

            return u;
        }
        public class SimpleSkillData
        {
            public SimpleSkillData(int baseMaxStock = 1, bool beginSkillCooldownOnSkillEnd = false,
                bool canceledFromSprinting = false, bool cancelSprintingOnActivation = true, bool forceSprintingDuringState = false,
                bool dontAllowPastMaxStocks = false, bool fullRestockOnAssign = true, 
                bool isCombatSkill = true, bool mustKeyPress = false, int rechargeStock = 1,
                int requiredStock = 1, bool resetCooldownTimerOnUse = false, int stockToConsume = 1,
                bool useAttackSpeedScaling = false)
            {
                this.baseMaxStock = baseMaxStock;
                this.beginSkillCooldownOnSkillEnd = beginSkillCooldownOnSkillEnd;
                this.canceledFromSprinting = canceledFromSprinting;
                this.cancelSprintingOnActivation = cancelSprintingOnActivation;
                this.forceSprintingDuringState = forceSprintingDuringState;
                this.dontAllowPastMaxStocks = dontAllowPastMaxStocks;
                this.fullRestockOnAssign = fullRestockOnAssign;
                this.isCombatSkill = isCombatSkill;
                this.mustKeyPress = mustKeyPress;
                this.rechargeStock = rechargeStock;
                this.requiredStock = requiredStock;
                this.resetCooldownTimerOnUse = resetCooldownTimerOnUse;
                this.stockToConsume = stockToConsume;
                this.useAttackSpeedScaling = useAttackSpeedScaling;
            }

            internal int baseMaxStock;
            internal bool beginSkillCooldownOnSkillEnd;
            internal bool canceledFromSprinting;
            internal bool cancelSprintingOnActivation;
            internal bool forceSprintingDuringState;
            internal bool dontAllowPastMaxStocks;
            internal bool fullRestockOnAssign;
            internal bool isCombatSkill;
            internal bool mustKeyPress;
            internal int rechargeStock;
            internal int requiredStock;
            internal bool resetCooldownTimerOnUse;
            internal int stockToConsume;
            internal bool useAttackSpeedScaling;
        }
    }
}
