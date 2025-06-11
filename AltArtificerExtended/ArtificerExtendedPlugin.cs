using System.Security;
using System.Security.Permissions;
using RoR2;
using RoR2.Skills;
using System;
using UnityEngine;
using BepInEx;
using EntityStates.Mage;
using EntityStates.Mage.Weapon;
using R2API;
using R2API.Utils;
using System.Collections.Generic;
using ArtificerExtended.Skills;
using ArtificerExtended.Unlocks;
using System.Reflection;
using System.Linq;
using EntityStates;
using ArtificerExtended.Components;
using System.Runtime.CompilerServices;
using RoR2.Projectile;
using RoR2.UI;
using static RoR2.UI.CharacterSelectController;
using ArtificerExtended.Passive;
using ArtificerExtended.Modules;
using BepInEx.Configuration;
using MonoMod.Cil;
using Mono.Cecil.Cil;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 

namespace ArtificerExtended
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID)]
    [BepInDependency(R2API.LoadoutAPI.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [BepInDependency(R2API.UnlockableAPI.PluginGUID)]
    [BepInDependency(R2API.DamageAPI.PluginGUID)]
    [BepInDependency(R2API.SkillsAPI.PluginGUID)]

    [BepInDependency(RainrotSharedUtils.SharedUtilsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency("xyz.yekoc.PassiveAgression", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("prodzpod.MinerSkillReturns", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInDependency("com.johnedwa.RTAutoSprintEx", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.RiskyLives.RiskyMod", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.DrBibop.VRAPI", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(JetHack.JetHackPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]
    [R2APISubmoduleDependency(nameof(UnlockableAPI), nameof(LanguageAPI), nameof(LoadoutAPI),  nameof(PrefabAPI), nameof(DamageAPI), nameof(DeployableAPI))]
    [BepInPlugin(guid, modName, version)]
    public partial class ArtificerExtendedPlugin : BaseUnityPlugin
    {
        public const string guid = "com." + teamName + "." + modName;
        public const string modName = "ArtificerExtended";
        public const string teamName = "Borbo";
        public const string version = "4.0.9";
        public static ArtificerExtendedPlugin instance;

        public static AssetBundle iconBundle => Tools.mainAssetBundle;
        public const string iconsPath = "Assets/Icons/";
        public const string DEVELOPER_PREFIX = "AE_";
        public const string achievementIdentifier = "_ACHIEVEMENT_NAME";

        public static bool isJethackLoaded = Tools.isLoaded("JetHack.JetHackPlugin.guid");
        public static bool isScepterLoaded = Tools.isLoaded("com.DestroyedClone.AncientScepter");
        public static bool autosprintLoaded = Tools.isLoaded("com.johnedwa.RTAutoSprintEx");
        public static bool is2r4rLoaded = Tools.isLoaded("com.HouseOfFruits.RiskierRain");
        public static bool isRiskyModLoaded = Tools.isLoaded("com.RiskyLives.RiskyMod");

        #region Config
        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> RecolorMeteor { get; set; }
        public static bool ShouldReworkIonSurge { get; set; }
        #endregion

        public static GameObject mageObject;
        public static CharacterBody mageBody;
        public static SkillLocator mageSkillLocator;

        public static GenericSkill magePassive;
        public static SkillFamily magePassiveFamily;
        public static SkillFamily magePrimary;
        public static SkillFamily mageSecondary;
        public static SkillFamily mageUtility;
        public static SkillFamily mageSpecial;

        public static float artiBoltDamage = 2.8f;
        public static float artiNanoDamage = 20;
        public static float artiUtilCooldown = 12;
        public static float meleeRangeChannel = 21; //flamethrower
        public static float meleeRangeSingle = meleeRangeChannel + 7f;

        void Awake()
        {
            instance = this;

            Modules.Config.Init();
            InitializeConfig();
            Log.Init(Logger);

            Modules.Language.Init();

            mageObject = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/MageBody");
            mageObject.AddComponent<ElementCounter>();
            mageBody = mageObject.GetComponent<CharacterBody>();
            mageSkillLocator = mageObject.GetComponent<SkillLocator>();
            if (mageObject && mageBody && mageSkillLocator)
            {
                Modules.Skills.characterSkillLocators.Add("MageBody", mageSkillLocator);
                Log.Debug("AE Skill Setup succeeded!");
            }

            magePassive = CreateMagePassiveSlot(mageObject, mageSkillLocator);
            magePassiveFamily = magePassive.skillFamily;
            magePrimary = mageSkillLocator.primary.skillFamily;
            mageSecondary = mageSkillLocator.secondary.skillFamily;
            mageUtility = mageSkillLocator.utility.skillFamily;
            mageSpecial = mageSkillLocator.special.skillFamily;

            Log.Debug("ArtificerExtended setup succeeded!");

            CreateMagePassives(magePassiveFamily);
            if (ShouldReworkIonSurge)
            {
                DoSurgeReworkAssetSetup();
            }
            On.RoR2.Skills.SkillCatalog.Init += ReplaceSkillDefs;

            if (is2r4rLoaded)
            {
                artiNanoDamage = 12f;
                artiUtilCooldown = 8f;
            }

            Modules.CommonAssets.Init();
            AddHooks();
            this.ArtiChanges();
            InitializeContent();
            On.RoR2.CharacterMaster.OnBodyStart += AddAEBodyFX;

            new ContentPacks().Initialize();
            //VRStuff.SetupVR();
        }

        public static bool BodyHasAncientScepterItem(CharacterBody body)
        {
            if (!body || !body.inventory)
                return false;
            if (!isScepterLoaded)
                return false;
            return _BodyHasAncientScepterItem(body);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool _BodyHasAncientScepterItem(CharacterBody body)
        {
            return body.inventory.GetItemCount(AncientScepter.AncientScepterItem.instance.ItemDef) > 0;
        }

        private void InitializeContent()
        {
            Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

            BeginInitializing<SkillBase>(allTypes, "SwanSongSkills.txt");
        }

        #region content initialization
        private void BeginInitializing<T>(Type[] allTypes, string fileName = "") where T : SharedBase
        {
            Type baseType = typeof(T);
            //base types must be a base and not abstract
            if (!baseType.IsAbstract)
            {
                Log.Error(Log.Combine() + "Incorrect BaseType: " + baseType.Name);
                return;
            }

            IEnumerable<Type> objTypesOfBaseType = allTypes.Where(type => !type.IsAbstract && type.IsSubclassOf(baseType));

            if (objTypesOfBaseType.Count() <= 0)
                return;

            Log.Debug(Log.Combine(baseType.Name) + "Initializing");

            foreach (var objType in objTypesOfBaseType)
            {
                string s = Log.Combine(baseType.Name, objType.Name);
                Log.Debug(s);
                T obj = (T)System.Activator.CreateInstance(objType);
                if (ValidateBaseType(obj as SharedBase))
                {
                    Log.Debug(s + "Validated");
                    InitializeBaseType(obj as SharedBase);
                    Log.Debug(s + "Initialized");
                }
            }

            if (!string.IsNullOrEmpty(fileName))
                Modules.Language.TryPrintOutput(fileName);
        }

        bool ValidateBaseType(SharedBase obj)
        {
            bool enabled = obj.isEnabled;
            if (obj.lockEnabled)
                return enabled;
            return obj.Bind(enabled, "Should This Content Be Enabled");
        }
        void InitializeBaseType(SharedBase obj)
        {
            obj.Init();
        }
        #endregion

        private GenericSkill CreateMagePassiveSlot(GameObject body, SkillLocator skillLocator)
        {
            foreach (var skill in body.GetComponents<GenericSkill>())
                if ((skill.skillFamily as ScriptableObject).name.ToLower().Contains("passive"))
                    return skill;

            //

            SkillFamily passiveFamily = ScriptableObject.CreateInstance<SkillFamily>();
            passiveFamily.variants = new SkillFamily.Variant[1];

            GenericSkill passiveSkill = body.gameObject.AddComponent<GenericSkill>();
            passiveSkill._skillFamily = passiveFamily;
            passiveSkill.SetOrderPriority(-1);
            passiveSkill.SetLoadoutTitleTokenOverride("MAGE_LOADOUT_PASSIVE");
            passiveSkill._skillFamily = passiveFamily;
            (passiveSkill.skillFamily as ScriptableObject).name = "MageBodyPassive";

            LanguageAPI.Add("MAGE_LOADOUT_PASSIVE", "Passive");

            Content.AddSkillFamily(passiveFamily);

            skillLocator.passiveSkill.enabled = false;
            foreach (var machine in body.GetComponents<EntityStateMachine>())
            {
                if (machine.customName == "Body")
                {
                    machine.mainStateType = new EntityStates.SerializableEntityStateType(typeof(EntityStates.GenericCharacterMain));
                }
            }

            return passiveSkill;
        }

        private void CharacterSelectController_BuildSkillStripDisplayData(On.RoR2.UI.CharacterSelectController.orig_BuildSkillStripDisplayData orig, CharacterSelectController self, Loadout loadout, ValueType bodyInfo, object dest)
        {
            throw new NotImplementedException();
        }

        public void CreateMagePassives(SkillFamily passiveFamily)
        {
            PassiveSkillDef hoverSkillDef = ScriptableObject.CreateInstance<PassiveSkillDef>();
            hoverSkillDef.skillNameToken = mageSkillLocator.passiveSkill.skillNameToken;
            (hoverSkillDef as ScriptableObject).name = mageSkillLocator.passiveSkill.skillNameToken;
            hoverSkillDef.skillDescriptionToken = mageSkillLocator.passiveSkill.skillDescriptionToken;
            hoverSkillDef.icon = mageSkillLocator.passiveSkill.icon;
            hoverSkillDef.canceledFromSprinting = false;
            hoverSkillDef.cancelSprintingOnActivation = false;
            hoverSkillDef.stateMachineDefaults = new PassiveSkillDef.StateMachineDefaults[1]
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

            #region lang
            LanguageAPI.Add("MAGE_PASSIVE_ENERGY_NAME", "Energetic Resonance");
            /*LanguageAPI.Add("MAGE_PASSIVE_ENERGY_DESC",
                "- Selecting <style=cIsDamage>FIRE</style> skills increases the intensity of <style=cIsUtility>Incinerate.</style>" +
                "\n- Selecting <style=cIsDamage>ICE</style> skills increases the power of <style=cIsUtility>Arctic Blasts.</style>" +
                "\n- Selecting <style=cIsDamage>LIGHTNING</style> skills creates additional <style=cIsUtility>Lightning Bolts.</style>");*/
            //LanguageAPI.Add("MAGE_PASSIVE_ENERGY_DESC",
            //    "- <style=cIsUtility>Incinerate</style> increases in intensity for each <style=cIsDamage>FIRE</style> skill." +
            //    "\n- <style=cIsUtility>Arctic Blasts</style> increase in radius for each <style=cIsDamage>ICE</style> skill." +
            //    "\n- <style=cIsUtility>Lightning Bolts</style> increase in number for each <style=cIsDamage>LIGHTNING</style> skill.");
            #endregion

            Sprite altPassiveIcon = iconBundle.LoadAsset<Sprite>(iconsPath + "passiveilyborbo.png");
            PassiveSkillDef resonanceSkillDef = ScriptableObject.CreateInstance<PassiveSkillDef>();
            resonanceSkillDef.skillNameToken = "MAGE_PASSIVE_ENERGY_NAME";
            resonanceSkillDef.skillDescriptionToken = CommonAssets.magePassiveDescToken;
            resonanceSkillDef.icon = altPassiveIcon;
            resonanceSkillDef.canceledFromSprinting = false;
            resonanceSkillDef.cancelSprintingOnActivation = false;
            resonanceSkillDef.stateMachineDefaults = new PassiveSkillDef.StateMachineDefaults[1]
            {
                new PassiveSkillDef.StateMachineDefaults
                {
                    machineName = "Jet",
                    initalState = new SerializableEntityStateType( typeof( Passive.AltArtiPassive ) ),
                    mainState = new SerializableEntityStateType( typeof( Passive.AltArtiPassive ) ),
                    defaultInitalState = new SerializableEntityStateType( typeof( Idle ) ),
                    defaultMainState = new SerializableEntityStateType( typeof( Idle ) )
                },
            };
            resonanceSkillDef.keywordTokens = new string[3] { CommonAssets.meltKeywordToken, CommonAssets.arcticBlastKeywordToken, CommonAssets.lightningBoltKeywordToken };

            passiveFamily.variants = new SkillFamily.Variant[2]
            {
                new SkillFamily.Variant
                {
                    skillDef = hoverSkillDef,
                    unlockableName = "",
                    viewableNode = new ViewablesCatalog.Node(hoverSkillDef.skillNameToken, false, null)
                },
                new SkillFamily.Variant
                {
                    skillDef = resonanceSkillDef,
                    unlockableDef = UnlockBase.CreateUnlockDef(typeof(FullKitElementUnlock),  altPassiveIcon),
                    viewableNode = new ViewablesCatalog.Node(resonanceSkillDef.skillNameToken, false, null)
                }
            };
            //
            Content.AddSkillDef(hoverSkillDef);
            Content.AddSkillDef(resonanceSkillDef);
        }
        private void ReplaceSkillDefs(On.RoR2.Skills.SkillCatalog.orig_Init orig)
        {
            orig();

            ReplaceVanillaIonSurge(ShouldReworkIonSurge);
        }
        private void AddAEBodyFX(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            orig(self, body);

            AEBodyEffects bodyFX = body.gameObject.AddComponent<AEBodyEffects>();
            bodyFX.body = body;
        }

        private void InitializeConfig()
        {
            ShouldReworkIonSurge = ConfigManager.DualBind<bool>("(A0) ArtificerExtended : Ion Surge", "Enable Ion Surge Rework", true,
                "Determines whether Ion Surge gets reworked. Note that vanilla Ion Surge is INCOMPATIBLE with ALL alt-passives. Use at your own risk.");
        }

        private void ArtiChanges()
        {

            //LanguageAPI.Add("MAGE_OUTRO_FLAVOR", "..and so she left, her heart fixed on new horizons.");
            //LanguageAPI.Add("MAGE_OUTRO_FLAVOR", "..and so she left, in search of a heaven that no longer exists.");
            LanguageAPI.Add("MAGE_OUTRO_FLAVOR", "..and so she left, still searching for a heaven that no longer exists.");

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


        public static List<Type> entityStates = new List<Type>();
        public static List<SkillBase> Skills = new List<SkillBase>();
        public static Dictionary<SkillBase, bool> SkillStatusDictionary = new Dictionary<SkillBase, bool>();

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
                body.GetBuffCount(_1FrostbiteSkill.artiIceShield), "");
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
