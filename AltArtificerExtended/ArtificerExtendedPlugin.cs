using System.Security;
using System.Security.Permissions;
using RoR2;
using RoR2.Skills;
using System;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using EntityStates.Mage;
using EntityStates.Mage.Weapon;
using R2API;
using R2API.Utils;
using System.Collections.Generic;
using AltArtificerExtended.Skills;
using AltArtificerExtended.Unlocks;
using System.Reflection;
using System.Linq;
using EntityStates;
using AltArtificerExtended.Components;
using System.Runtime.CompilerServices;
using RoR2.Projectile;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 

namespace AltArtificerExtended
{
    [BepInDependency(R2API.R2API.PluginGUID, "5.0.3")]

    [BepInDependency("com.johnedwa.RTAutoSprintEx", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.HouseOfFruits.RiskierRain", BepInDependency.DependencyFlags.SoftDependency)]
    [R2APISubmoduleDependency(nameof(UnlockableAPI), nameof(LanguageAPI), nameof(LoadoutAPI),  nameof(PrefabAPI))]
    [BepInPlugin(guid, modName, version)]
    public partial class ArtificerExtendedPlugin : BaseUnityPlugin
    {
        public const string guid = "com.HouseOfFruits.ArtificerExtended";
        public const string modName = "ArtificerExtended";
        public const string version = "4.0.0";

        public static AssetBundle iconBundle = Tools.LoadAssetBundle(Properties.Resources.artiskillicons);
        public static string iconsPath = "Assets/AESkillIcons/";
        public static string TokenName = "ARTIFICEREXTENDED_";

        public static bool isScepterLoaded = Tools.isLoaded("com.DestroyedClone.AncientScepter");
        public static bool autosprintLoaded = Tools.isLoaded("com.johnedwa.RTAutoSprintEx");
        public static bool is2r4rLoaded = Tools.isLoaded("com.HouseOfFruits.RiskierRain");

        #region Config
        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> AllowBrokenSFX { get; set; }
        public static ConfigEntry<bool> RecolorMeteor { get; set; }
        public static ConfigEntry<bool> SurgeRework { get; set; }
        #endregion

        public static GameObject mageObject;
        public static CharacterBody mageBody;
        public static SkillLocator mageSkillLocator;

        public static SkillFamily magePassive;
        public static SkillFamily magePrimary;
        public static SkillFamily mageSecondary;
        public static SkillFamily mageUtility;
        public static SkillFamily mageSpecial;

        public static float artiBoltDamage = 2.2f;
        public static float artiNanoDamage = 20;
        public static float artiUtilCooldown = 12;
        public static float meleeRangeChannel = 21; //flamethrower
        public static float meleeRangeSingle = meleeRangeChannel + 4f;

        void Awake()
        {
            mageObject = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/MageBody");
            mageBody = mageObject.GetComponent<CharacterBody>();
            mageSkillLocator = mageObject.GetComponent<SkillLocator>();
            if (mageObject && mageBody && mageSkillLocator)
            {
                Debug.Log("ARTIFICEREXTENDED setup succeeded!");
            }

            magePrimary = mageSkillLocator.primary.skillFamily;
            mageSecondary = mageSkillLocator.secondary.skillFamily;
            mageUtility = mageSkillLocator.utility.skillFamily;
            mageSpecial = mageSkillLocator.special.skillFamily;

            InitializeConfig();
            this.InitializeUnlocks();

            if (is2r4rLoaded)
            {
                artiNanoDamage = 12f;
                artiUtilCooldown = 8f;
            }

            this.ArtiChanges();
            this.InitializeSkills();
            if (isScepterLoaded)
            {
                this.InitializeScepterSkills();
            }
            On.RoR2.CharacterMaster.OnBodyStart += AddAEBodyFX;
            ChillBasic();

            new ContentPacks().Initialize();
        }

        private void ChillBasic()
        {
            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;


            LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_CHILL", "<style=cKeywordName>Chilling</style>" +
                $"<style=cSub>Has a chance to temporarily <style=cIsUtility>slow enemy speed</style> by <style=cIsDamage>80%.</style></style>");

            LanguageAPI.Add("KEYWORD_FREEZING",
                "<style=cKeywordName>Freezing</style>" +
                "<style=cSub>Freeze enemies in place and <style=cIsUtility>Chill</style> them, slowing them by 80% after they thaw. " +
                "Frozen enemies are <style=cIsHealth>instantly killed</style> if below <style=cIsHealth>30%</style> health.");


            LanguageAPI.Add("ITEM_ICICLE_DESC",
                "Killing an enemy surrounds you with an <style=cIsDamage>ice storm</style> " +
                "that deals <style=cIsDamage>600% damage per second</style> and " +
                "<style=cIsUtility>Chills</style> enemies for <style=cIsUtility>1.5s</style>. " +
                "The storm <style=cIsDamage>grows with every kill</style>, " +
                "increasing its radius by <style=cIsDamage>1m</style>. " +
                "Stacks up to <style=cIsDamage>6m</style> <style=cStack>(+6m per stack)</style>.");

            LanguageAPI.Add("ITEM_ICERING_DESC",
                $"Hits that deal <style=cIsDamage>more than 400% damage</style> also blasts enemies with a " +
                $"<style=cIsDamage>runic ice blast</style>, " +
                $"<style=cIsUtility>Chilling</style> them for <style=cIsUtility>3s</style> <style=cStack>(+3s per stack)</style> and " +
                $"dealing <style=cIsDamage>250%</style> <style=cStack>(+250% per stack)</style> TOTAL damage. " +
                $"Recharges every <style=cIsUtility>10</style> seconds.");
        }

        static int freezeProcCount = 3;
        static int chillProcDuration = 8;
        static int slowProcChance = 100;
        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            CharacterBody vBody = self.body;
            CharacterBody aBody = null;
            if (damageInfo.attacker != null)
                aBody = damageInfo.attacker.GetComponent<CharacterBody>();

            if (vBody != null && aBody != null && damageInfo.procCoefficient != 0 && !damageInfo.rejected)
            {
                float debuffDuration = chillProcDuration;

                if (damageInfo.damageType.HasFlag(DamageType.Freeze2s))
                {
                    float chillCount = freezeProcCount;
                    if (damageInfo.damageType.HasFlag(DamageType.AOE))
                    {
                        chillCount -= 1;
                    }
                    for (int i = 0; i < chillCount; i++)
                    {
                        if (Util.CheckRoll(damageInfo.procCoefficient * 100, aBody.master))
                        {
                            vBody.AddTimedBuffAuthority(RoR2Content.Buffs.Slow80.buffIndex, debuffDuration);
                        }
                    }
                }
                else if (damageInfo.damageType.HasFlag(DamageType.SlowOnHit))
                {
                    if (aBody.bodyIndex == BodyCatalog.FindBodyIndex(mageBody))
                    {
                        damageInfo.damageType = damageInfo.damageType & ~DamageType.SlowOnHit;
                        float procChance = slowProcChance * damageInfo.procCoefficient;

                        if (Util.CheckRoll(procChance, aBody.master))
                        {
                            vBody.AddTimedBuffAuthority(RoR2Content.Buffs.Slow80.buffIndex, debuffDuration);
                        }
                    }
                }
            }
            orig(self, damageInfo);
        }

        private void AddAEBodyFX(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);

            AEBodyEffects bodyFX = body.gameObject.AddComponent<AEBodyEffects>();
            bodyFX.body = body;
        }

        private void InitializeConfig()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\ArtificerExtended.cfg", true);

            AllowBrokenSFX = CustomConfigFile.Bind<bool>(
                "Cosmetic",
                "Allow Broken SFX",
                false,
                "Some SFX (the snapfreeze cast sound, specifically) create an unstoppable droning/ringing sound. \n" +
                "They are disabled by default, but if you would like to have SFX and dont mind the bug, then you may enable them."
                );

            SurgeRework = CustomConfigFile.Bind<bool>(
                "Ion Surge", "Enable Rework",
                true,
                "Determines whether Ion Surge gets reworked. Note that vanilla Ion Surge is INCOMPATIBLE with ALL alt-passives. Use at your own risk.");
        }

        private void ArtiChanges()
        {

            LanguageAPI.Add("MAGE_OUTRO_FLAVOR", "..and so she left, her heart fixed on new horizons.");

            GameObject iceWallPillarPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIcewallPillarProjectile");
            ProjectileImpactExplosion pie = iceWallPillarPrefab.GetComponentInChildren<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.destroyOnEnemy = false;
            }
            LanguageAPI.Add("MAGE_UTILITY_ICE_DESCRIPTION",
                "<style=cIsUtility>Freezing</style>. Create a barrier that hurts enemies for " +
                "up to <style=cIsDamage>12x100% damage</style>.");


            SkillDef flamer = mageSpecial.variants[0].skillDef;
            if (flamer != null)
            {
                flamer.mustKeyPress = true;
            }
        }

        #region Init
        public static void AddBuff(BuffDef buffDef)
        {
            ContentPacks.buffDefs.Add(buffDef);
        }
        public static EffectDef CreateEffect(GameObject effect)
        {
            if (effect == null)
            {
                Debug.LogError("Effect prefab was null");
                return null;
            }

            var effectComp = effect.GetComponent<EffectComponent>();
            if (effectComp == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have an EffectComponent.", effect.name);
                return null;
            }

            var vfxAttrib = effect.GetComponent<VFXAttributes>();
            if (vfxAttrib == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have a VFXAttributes component.", effect.name);
                return null;
            }

            var def = new EffectDef
            {
                prefab = effect,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAttrib,
                prefabName = effect.name,
                spawnSoundEventName = effectComp.soundName
            };

            ContentPacks.effectDefs.Add(def);
            return def;
        }
        public static Dictionary<UnlockBase, UnlockableDef> UnlockBaseDictionary = new Dictionary<UnlockBase, UnlockableDef>();
        private void InitializeUnlocks()
        {
            var UnlockTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(UnlockBase)));
            var baseMethod = typeof(UnlockableAPI).GetMethod("AddUnlockable", new Type[] { typeof(bool) });

            Debug.Log("ARTIFICEREXTENDED Initializing unlocks!:");

            foreach (Type unlockType in UnlockTypes)
            {
                UnlockBase unlock = (UnlockBase)System.Activator.CreateInstance(unlockType);
                Debug.Log(unlockType);

                if (!unlock.HideUnlock)
                {
                    unlock.Init(CustomConfigFile);

                    UnlockableDef unlockableDef = (UnlockableDef)baseMethod.MakeGenericMethod(new Type[] { unlockType }).Invoke(null, new object[] { true });

                    bool forceUnlock = unlock.ForceDisable;

                    if (!forceUnlock)
                    {
                        forceUnlock = CustomConfigFile.Bind<bool>("Config: UNLOCKS", "Force Unlock Achievement: " + unlock.UnlockName,
                        false, $"Force this achievement to unlock: {unlock.UnlockName}?").Value;
                    }

                    if (!forceUnlock)
                        UnlockBaseDictionary.Add(unlock, unlockableDef);
                    else
                        UnlockBaseDictionary.Add(unlock, ScriptableObject.CreateInstance<UnlockableDef>());
                }
            }
        }

        public static List<Type> entityStates = new List<Type>();
        public static List<SkillBase> Skills = new List<SkillBase>();
        public static List<ScepterSkillBase> ScepterSkills = new List<ScepterSkillBase>();
        public static Dictionary<SkillBase, bool> SkillStatusDictionary = new Dictionary<SkillBase, bool>();

        private void InitializeSkills()
        {
            var SkillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(SkillBase)));

            foreach (var skillType in SkillTypes)
            {
                SkillBase skill = (SkillBase)System.Activator.CreateInstance(skillType);

                if (ValidateSkill(skill))
                { 
                    skill.Init(CustomConfigFile);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void InitializeScepterSkills()
        {
            var SkillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ScepterSkillBase)));

            foreach (var skillType in SkillTypes)
            {
                ScepterSkillBase skill = (ScepterSkillBase)System.Activator.CreateInstance(skillType);

                if (ValidateScepterSkill(skill))
                {
                    skill.Init(CustomConfigFile);
                }
            }
        }

        bool ValidateSkill(SkillBase item)
        {
            var forceUnlock = true;

            if (forceUnlock)
            {
                Skills.Add(item);
            }
            SkillStatusDictionary.Add(item, forceUnlock);

            return forceUnlock;
        }

        bool ValidateScepterSkill(ScepterSkillBase item)
        {
            var forceUnlock = isScepterLoaded;

            if (forceUnlock)
            {
                ScepterSkills.Add(item);
            }

            return forceUnlock;
        }
        #endregion

        public static SkillDef CloneSkillDef(SkillDef oldDef)
        {
            SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
            skillDef.skillName = oldDef.skillName;
            skillDef.skillNameToken = oldDef.skillNameToken;
            skillDef.skillDescriptionToken = oldDef.skillDescriptionToken;
            skillDef.icon = oldDef.icon;
            skillDef.activationStateMachineName = oldDef.activationStateMachineName;
            skillDef.activationState = oldDef.activationState;
            skillDef.interruptPriority = oldDef.interruptPriority;
            skillDef.baseRechargeInterval = oldDef.baseRechargeInterval;
            skillDef.baseMaxStock = oldDef.baseMaxStock;
            skillDef.rechargeStock = oldDef.rechargeStock;
            skillDef.requiredStock = oldDef.requiredStock;
            skillDef.stockToConsume = oldDef.stockToConsume;
            skillDef.beginSkillCooldownOnSkillEnd = oldDef.beginSkillCooldownOnSkillEnd;
            skillDef.fullRestockOnAssign = oldDef.fullRestockOnAssign;
            skillDef.dontAllowPastMaxStocks = oldDef.dontAllowPastMaxStocks;
            skillDef.resetCooldownTimerOnUse = oldDef.resetCooldownTimerOnUse;
            skillDef.isCombatSkill = oldDef.isCombatSkill;
            skillDef.cancelSprintingOnActivation = oldDef.cancelSprintingOnActivation;
            skillDef.canceledFromSprinting = oldDef.canceledFromSprinting;
            skillDef.forceSprintDuringState = oldDef.forceSprintDuringState;
            skillDef.mustKeyPress = oldDef.mustKeyPress;
            skillDef.keywordTokens = oldDef.keywordTokens;
            return skillDef;
        }
    }

    public class AEBodyEffects : MonoBehaviour
    {
        public CharacterBody body;
        private TemporaryVisualEffect blizzardArmorTempEffect;


        void Update()
        {
            UpdateSingleTemporaryVisualEffect(ref blizzardArmorTempEffect, 
                _1FrostbiteSkill.blizzardArmorVFX, body.radius * 0.5f, 
                body.GetBuffCount(_1FrostbiteSkill.artiIceShield) + body.GetBuffCount(FrostbiteSkill2.artiIceShield), "");
        }

        private void UpdateSingleTemporaryVisualEffect(ref TemporaryVisualEffect tempEffect, GameObject obj, float effectRadius, int count, string childLocatorOverride = "")
        {
            bool flag = tempEffect != null;
            if (flag != (count > 0))
            {
                if (count > 0)
                {
                    if (!flag)
                    {
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(obj, body.corePosition, Quaternion.identity);
                        tempEffect = gameObject.GetComponent<TemporaryVisualEffect>();
                        tempEffect.parentTransform = body.coreTransform;
                        tempEffect.visualState = TemporaryVisualEffect.VisualState.Enter;
                        tempEffect.healthComponent = body.healthComponent;
                        tempEffect.radius = effectRadius;
                        LocalCameraEffect component = gameObject.GetComponent<LocalCameraEffect>();
                        if (component)
                        {
                            component.targetCharacter = base.gameObject;
                        }
                        if (!string.IsNullOrEmpty(childLocatorOverride))
                        {
                            ModelLocator modelLocator = body.modelLocator;
                            ChildLocator childLocator;
                            if (modelLocator == null)
                            {
                                childLocator = null;
                            }
                            else
                            {
                                Transform modelTransform = modelLocator.modelTransform;
                                childLocator = ((modelTransform != null) ? modelTransform.GetComponent<ChildLocator>() : null);
                            }
                            ChildLocator childLocator2 = childLocator;
                            if (childLocator2)
                            {
                                Transform transform = childLocator2.FindChild(childLocatorOverride);
                                if (transform)
                                {
                                    tempEffect.parentTransform = transform;
                                    return;
                                }
                            }
                        }
                    }
                }
                else if (tempEffect)
                {
                    tempEffect.visualState = TemporaryVisualEffect.VisualState.Exit;
                }
            }
        }
    }
}
