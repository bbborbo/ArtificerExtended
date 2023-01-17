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
using AltArtificerExtended.Passive;
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
    [BepInDependency(R2API.R2API.PluginGUID, "4.3.6")]
    [BepInDependency("com.johnedwa.RTAutoSprintEx", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInDependency("com.Borbo.DuckSurvivorTweaks", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.BalanceOverhaulRBO", BepInDependency.DependencyFlags.SoftDependency)]
    [R2APISubmoduleDependency(nameof(UnlockableAPI), nameof(LanguageAPI), nameof(LoadoutAPI),  nameof(PrefabAPI))]
    [BepInPlugin(guid, modName, version)]
    public partial class Main : BaseUnityPlugin
    {
        public const string guid = "com.Borbo.ArtificerExtended";
        public const string modName = "ArtificerExtended";
        public const string version = "3.3.2";

        public static AssetBundle iconBundle = Tools.LoadAssetBundle(Properties.Resources.artiskillicons);
        public static string iconsPath = "Assets/AESkillIcons/";
        public static string TokenName = "ARTIFICEREXTENDED_";

        public static bool isScepterLoaded = Tools.isLoaded("com.DestroyedClone.AncientScepter");
        public static bool autosprintLoaded = Tools.isLoaded("com.johnedwa.RTAutoSprintEx");
        public static bool isDstLoaded = Tools.isLoaded("com.Borbo.DuckSurvivorTweaks");
        public static bool isBorboLoaded = Tools.isLoaded("com.Borbo.BalanceOverhaulRBO");

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
            mageObject.AddComponent<ElementCounter>();
            mageBody = mageObject.GetComponent<CharacterBody>();
            mageSkillLocator = mageObject.GetComponent<SkillLocator>();
            if (mageObject && mageBody && mageSkillLocator)
                Debug.Log("ARTIFICEREXTENDED setup succeeded! Proceeding to allocate skill families!");

            InitializeConfig();
            this.InitializeUnlocks();

            mageSkillLocator.passiveSkill.enabled = false;
            GenericSkill primary = mageSkillLocator.primary;
            GenericSkill secondary = mageSkillLocator.secondary;
            GenericSkill utility = mageSkillLocator.utility;
            GenericSkill special = mageSkillLocator.special;

            magePassive = ScriptableObject.CreateInstance<SkillFamily>();
            ContentPacks.skillFamilies.Add(magePassive);

            magePrimary = primary.skillFamily;
            mageSecondary = secondary.skillFamily;
            mageUtility = utility.skillFamily;
            mageSpecial = special.skillFamily;

            GenericSkill passive = primary;
            primary = secondary;
            secondary = utility;
            utility = special;
            special = mageObject.AddComponent<GenericSkill>();


            mageSkillLocator.primary = primary;
            mageSkillLocator.secondary = secondary;
            mageSkillLocator.utility = utility;
            mageSkillLocator.special = special;

            CreateMagePassives();

            passive._skillFamily = magePassive;
            primary._skillFamily = magePrimary;
            secondary._skillFamily = mageSecondary;
            utility._skillFamily = mageUtility;
            special._skillFamily = mageSpecial;

            if (magePassive && magePrimary && mageSecondary && mageUtility && mageSpecial)
                Debug.Log("ARTIFICEREXTENDED skill families reassigned! Proceeding to make Arti changes!");
            else
            {
                Debug.Log("" + magePassive + magePrimary + mageSecondary + mageUtility + mageSpecial);
            }

            this.CreateBuffs();
            this.CreateLightningSwords();
            this.CreateIceExplosion();
            //CreateNebulaOrbitals();
            this.DoEffects();

            this.ArtiChanges();
            this.InitializeSkills();
            On.RoR2.Skills.SkillCatalog.Init += ReplaceSkillDefs;
            On.RoR2.Skills.SkillCatalog.Init += ReplaceScepterSkillDefs;
            if (isScepterLoaded)
            {
                Debug.Log("Fuck");
                this.InitializeScepterSkills();
            }
            this.FixSkillFlamer(mageSpecial);
            On.RoR2.CharacterMaster.OnBodyStart += AddAAEBodyFX;

            new ContentPacks().Initialize();
        }

        private void ReplaceSkillDefs(On.RoR2.Skills.SkillCatalog.orig_Init orig)
        {
            orig();

            SkillDef surge = RoR2.LegacyResourcesAPI.Load<SkillDef>("skilldefs/magebody/MageBodyFlyUp");
            if (surge != null)
            {
                Debug.Log("Changing ion surge");

                //SkillDef newSurge = CloneSkillDef(surge);
                if (SurgeRework.Value == true)
                {
                    SkillBase.RegisterEntityState(typeof(EntityState.AlternateIonSurge));
                    LanguageAPI.Add(SkillBase.Token + "ALTIONSURGE_DESC", 
                        "Burst forward up to 3 times. <style=cIsDamage>Can attack while dashing.</style> Trigger again to cancel early.");
                    surge.activationState = new SerializableEntityStateType(typeof(EntityState.AlternateIonSurge));
                    surge.baseRechargeInterval = 6f;
                    surge.skillDescriptionToken = SkillBase.Token + "ALTIONSURGE_DESC";
                    surge.keywordTokens = new string[0];
                }
                else
                {
                    SkillBase.RegisterEntityState(typeof(EntityState.VanillaIonSurge));
                    surge.activationState = new SerializableEntityStateType(typeof(EntityState.VanillaIonSurge));
                }
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void ReplaceScepterSkillDefs(On.RoR2.Skills.SkillCatalog.orig_Init orig)
        {
            orig();

            //flamethrower changes only changes the description of the ancient scepter skill for flamethrower
            SkillDef flamer2 = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("Dragon's Breath"));
            if (flamer2 != null)
            {
                LanguageAPI.Add(SkillBase.Token + "FLAMETHROWER2_FIRE", "Dragon's Breath");
                LanguageAPI.Add(SkillBase.Token + "FLAMETHROWER2_DESC",
                    flamethrowerDesc +
                    "\n<color=#d299ff>SCEPTER: Hits leave behind a lingering fire cloud.</color>");
                flamer2.skillNameToken = SkillBase.Token + "FLAMETHROWER2_FIRE";
                flamer2.skillDescriptionToken = SkillBase.Token + "FLAMETHROWER2_DESC";
            }

            SkillDef surge = RoR2.LegacyResourcesAPI.Load<SkillDef>("skilldefs/magebody/MageBodyFlyUp");
            if (SurgeRework.Value == true && surge != null)
            {
                SkillDef surge2 = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName($"{surge.skillName}Scepter"));
                if (surge2 != null)
                {
                    SkillBase.RegisterEntityState(typeof(EntityState.AlternateIonSurge2));

                    LanguageAPI.Add(SkillBase.Token + "ALTANTISURGE_LIGHTNING", "Antimatter Surge");
                    LanguageAPI.Add(SkillBase.Token + "ALTANTISURGE_DESC",
                        "Burst forward up to 3 times. <style=cIsDamage>Can attack while dashing.</style> Trigger again to cancel early." +
                        "\n<color=#d299ff>SCEPTER: Each burst reduces ALL cooldowns.</color>");

                    surge2.activationState = new SerializableEntityStateType(typeof(EntityState.AlternateIonSurge2));
                    surge2.baseRechargeInterval = 6f;
                    surge2.skillDescriptionToken = SkillBase.Token + "ALTANTISURGE_DESC";
                    surge2.skillNameToken = SkillBase.Token + "ALTANTISURGE_LIGHTNING";
                    surge2.keywordTokens = new string[0];
                }
            }
            else
            {
                Debug.LogError("ArtificerExtended could not replace Ancient Scepter's Antimatter Surge. " +
                    "Antimatter Surge WILL break Artificer Extended's alt passives. \n" +
                    "Either turn on ArtificerExtended's Ion Surge rework to use ArtificerExtended's Antimatter Surge, " +
                    "avoid using Antimatter Surge with ArtificerExtended's alt passive, " +
                    "or tell the Ancient Scepter developers to get in contact to fix Antimatter Surge.");
            }
        }

        private void AddAAEBodyFX(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);

            AAEBodyEffects bodyFX = body.gameObject.AddComponent<AAEBodyEffects>();
            bodyFX.body = body;
        }

        void OnEnable()
        {
            this.DoHooks();
        }

        private void CreateMagePassives()
        {
            PassiveSkillDef energeticResonance = ScriptableObject.CreateInstance<PassiveSkillDef>();
            //PassiveSkillDef nebulaSoul = ScriptableObject.CreateInstance<PassiveSkillDef>();
            PassiveSkillDef envSuit = ScriptableObject.CreateInstance<PassiveSkillDef>();


            envSuit.skillNameToken = mageSkillLocator.passiveSkill.skillNameToken;
            envSuit.skillDescriptionToken = mageSkillLocator.passiveSkill.skillDescriptionToken;
            envSuit.icon = mageSkillLocator.passiveSkill.icon;

            envSuit.stateMachineDefaults = new PassiveSkillDef.StateMachineDefaults[1]
            {
                new PassiveSkillDef.StateMachineDefaults
                {
                    machineName = "Body",
                    initalState = new SerializableEntityStateType( typeof( MageCharacterMain ) ),
                    mainState = new SerializableEntityStateType( typeof( MageCharacterMain ) ),
                    defaultInitalState = new SerializableEntityStateType( typeof( GenericCharacterMain ) ),
                    defaultMainState = new SerializableEntityStateType( typeof( GenericCharacterMain ) )
                }
            };

            #region ER
            LanguageAPI.Add("MAGE_PASSIVE_ENERGY_NAME", "Energetic Resonance");
            /*LanguageAPI.Add("MAGE_PASSIVE_ENERGY_DESC",
                "- Selecting <style=cIsDamage>FIRE</style> skills increases the intensity of <style=cIsUtility>Incinerate.</style>" +
                "\n- Selecting <style=cIsDamage>ICE</style> skills increases the power of <style=cIsUtility>Arctic Blasts.</style>" +
                "\n- Selecting <style=cIsDamage>LIGHTNING</style> skills creates additional <style=cIsUtility>Lightning Bolts.</style>");*/
            LanguageAPI.Add("MAGE_PASSIVE_ENERGY_DESC",
                "- <style=cIsUtility>Incinerate</style> increases in intensity for each <style=cIsDamage>FIRE</style> skill." +
                "\n- <style=cIsUtility>Arctic Blasts</style> increase in power for each <style=cIsDamage>ICE</style> skill." +
                "\n- <style=cIsUtility>Lightning Bolts</style> are created for each <style=cIsDamage>LIGHTNING</style> skill.");

            LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_MELT", $"<style=cKeywordName>Incinerate</style>" +
                $"<style=cSub><style=cIsUtility>On ANY Cast:</style> Gain a buff that temporarily " +
                $"increases the <style=cIsDamage>burn damage</style> from Ignite " +
                $"by <style=cIsDamage>{Tools.ConvertDecimal(AltArtiPassive.burnDamageMult)} per stack.</style>");
            LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_ARCTICBLAST", "<style=cKeywordName>Arctic Blast</style>" +
                "<style=cSub><style=cIsUtility>Applying 10 stacks</style> of Chill or <style=cIsUtility>killing Chilled enemies</style> " +
                "causes an <style=cIsUtility>Arctic Blast,</style> " +
                "clearing the effect and <style=cIsDamage>Freezing nearby enemies.</style></style>");
            LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_BOLTS", $"<style=cKeywordName>Lightning Bolts</style>" +
                $"<style=cSub><style=cIsUtility>On ANY Cast:</style> Summon spears of energy that <style=cIsUtility>seek out enemies in front of you</style> " +
                $"for <style=cIsDamage>{Tools.ConvertDecimal(AltArtiPassive.lightningDamageMult + AltArtiPassive.lightningBlastDamageMult)} damage.</style>");


            LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_CHILL_OLD", "<style=cKeywordName>Chill</style>" +
                "<style=cSub><style=cIsUtility>On Hit:</style> Inflicts <style=cIsUtility>Chill.</style>" +
                "\n<style=cIsUtility>Killing Chilled enemies</style> or consuming <style=cIsUtility>10 stacks of Chill</style> " +
                "will create an <style=cIsDamage>Arctic Blast</style>, damaging and <style=cIsUtility>Freezing</style> nearby enemies.</style>");

            energeticResonance.skillNameToken = "MAGE_PASSIVE_ENERGY_NAME";
            energeticResonance.skillDescriptionToken = "MAGE_PASSIVE_ENERGY_DESC";
            energeticResonance.icon = iconBundle.LoadAsset<Sprite>(iconsPath + "ElementalIntensity.png");

            energeticResonance.stateMachineDefaults = new PassiveSkillDef.StateMachineDefaults[1]
            {
                new PassiveSkillDef.StateMachineDefaults
                {
                    /*machineName = "Body",
                    initalState = new SerializableEntityStateType( typeof( GenericCharacterMain ) ),
                    mainState = new SerializableEntityStateType( typeof( GenericCharacterMain ) ),
                    defaultInitalState = new SerializableEntityStateType( typeof( GenericCharacterMain ) ),
                    defaultMainState = new SerializableEntityStateType( typeof( GenericCharacterMain ) )*/
                    machineName = "Jet",
                    initalState = new SerializableEntityStateType( typeof( Passive.AltArtiPassive ) ),
                    mainState = new SerializableEntityStateType( typeof( Passive.AltArtiPassive ) ),
                    defaultInitalState = new SerializableEntityStateType( typeof( Idle ) ),
                    defaultMainState = new SerializableEntityStateType( typeof( Idle ) )
                },
            };
            energeticResonance.keywordTokens = new string[3] { "ARTIFICEREXTENDED_KEYWORD_MELT", "ARTIFICEREXTENDED_KEYWORD_ARCTICBLAST", "ARTIFICEREXTENDED_KEYWORD_BOLTS" };
            #endregion

            magePassive.variants = new SkillFamily.Variant[2]
            {
                new SkillFamily.Variant
                {
                    skillDef = envSuit,
                    unlockableName = "",
                    viewableNode = new ViewablesCatalog.Node( "envSuit" , false )
                },
                new SkillFamily.Variant
                {
                    skillDef = energeticResonance,
                    unlockableDef = energeticResonance.GetUnlockDef(typeof(ArtificerEnergyPassiveUnlock)),
                    viewableNode = new ViewablesCatalog.Node( "energeticResonance" , false )
                }
            };

            Debug.Log("AE State Machines Set Up!");
            SetStateOnHurt stateOnHurt = mageObject.GetComponent<SetStateOnHurt>();
            EntityStateMachine[] idles = stateOnHurt.idleStateMachine;
            Array.Resize<EntityStateMachine>(ref idles, 1);
            stateOnHurt.idleStateMachine = idles;
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
            LanguageAPI.Add("ITEM_ICICLE_DESC",
                "Killing an enemy surrounds you with an <style=cIsDamage>ice storm</style> " +
                "that deals <style=cIsDamage>600% damage per second</style> and " +
                "<style=cIsUtility>Chills</style> enemies for <style=cIsUtility>1.5s</style>. " +
                "The storm <style=cIsDamage>grows with every kill</style>, " +
                "increasing its radius by <style=cIsDamage>1m</style>. " +
                "Stacks up to <style=cIsDamage>6m</style> <style=cStack>(+6m per stack)</style>.");

            if (isBorboLoaded)
            {
                LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_CHILL", "<style=cKeywordName>Chilling</style>" +
                    $"<style=cSub>Has a chance to temporarily <style=cIsUtility>slow enemy speed</style> by <style=cIsDamage>80%.</style></style>");
            }
            else
            {
                LanguageAPI.Add("ITEM_ICERING_DESC",
                    $"Hits that deal <style=cIsDamage>more than 400% damage</style> also blasts enemies with a " +
                    $"<style=cIsDamage>runic ice blast</style>, " +
                    $"<style=cIsUtility>Chilling</style> them for <style=cIsUtility>3s</style> <style=cStack>(+3s per stack)</style> and " +
                    $"dealing <style=cIsDamage>250%</style> <style=cStack>(+250% per stack)</style> TOTAL damage. " +
                    $"Recharges every <style=cIsUtility>10</style> seconds.");

                LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_CHILL", "<style=cKeywordName>Chilling</style>" +
                    $"<style=cSub>Has a chance to temporarily <style=cIsUtility>slow movement speed</style> by <style=cIsDamage>80%.</style></style>");
            }

            LanguageAPI.Add("KEYWORD_FREEZING",
                "<style=cKeywordName>Freezing</style>" +
                "<style=cSub>Freeze enemies in place and <style=cIsUtility>Chill</style> them, slowing them by 80% after they thaw. " +
                "Frozen enemies are <style=cIsHealth>instantly killed</style> if below <style=cIsHealth>30%</style> health.");

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

            if (isDstLoaded)
            {
                artiNanoDamage = 12;
                artiUtilCooldown = 8;
                //artiBoltDamage = 4f;
            }
            else
            {
                On.EntityStates.Mage.Weapon.BaseThrowBombState.OnEnter += (orig, self) =>
                {
                    bool isBomb = self is ThrowIcebomb;
                    if (isBomb)
                    {
                        self.maxDamageCoefficient = artiNanoDamage;
                    }
                    orig(self);
                };

                flamethrowerDesc = "Burn all enemies in front of you for <style=cIsDamage>2000% damage</style>. " +
                    "Each hit has a <style=cIsDamage>50% chance to Ignite</style>.";
                LanguageAPI.Add("MAGE_SPECIAL_FIRE_DESCRIPTION", flamethrowerDesc);
            }
        }
        public static string flamethrowerDesc;

        #region Init
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

        private void FixSkillFlamer(SkillFamily skillFamily)
        {
            SkillDef flamer = RoR2.LegacyResourcesAPI.Load<SkillDef>("skilldefs/magebody/MageBodyFlamethrower");
            if (flamer == null)
            {
                Debug.Log("Could not find Flamethrower by name. Using skillfamily index instead. Oh no!");
                flamer = skillFamily.variants[0].skillDef;
            }

            if (flamer != null)
            {
                flamer.mustKeyPress = true;
            }
        }
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

    public class AAEBodyEffects : MonoBehaviour
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
