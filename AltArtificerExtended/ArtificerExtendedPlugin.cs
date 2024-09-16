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
using ArtificerExtended.Skills;
using ArtificerExtended.Unlocks;
using System.Reflection;
using System.Linq;
using EntityStates;
using ArtificerExtended.Components;
using System.Runtime.CompilerServices;
using RoR2.Projectile;
using MonoMod.Cil;
using RoR2.UI;
using static RoR2.UI.CharacterSelectController;
using Mono.Cecil.Cil;
using ArtificerExtended.Passive;
using ArtificerExtended.CoreModules;

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

    [BepInDependency(ChillRework.ChillRework.guid, BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency("com.johnedwa.RTAutoSprintEx", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.RiskyLives.RiskyMod", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.DrBibop.VRAPI", BepInDependency.DependencyFlags.SoftDependency)]
    [R2APISubmoduleDependency(nameof(UnlockableAPI), nameof(LanguageAPI), nameof(LoadoutAPI),  nameof(PrefabAPI), nameof(DamageAPI))]
    [BepInPlugin(guid, modName, version)]
    public partial class ArtificerExtendedPlugin : BaseUnityPlugin
    {
        public const string guid = "com.Borbo.ArtificerExtended";
        public const string modName = "ArtificerExtended";

        public const string version = "3.7.3";
        
        public static AssetBundle iconBundle = Tools.LoadAssetBundle(Properties.Resources.artiskillicons);
        public static string iconsPath = "Assets/AESkillIcons/";
        public static string TokenName = "ARTIFICEREXTENDED_";

        public static bool isScepterLoaded = Tools.isLoaded("com.DestroyedClone.AncientScepter");
        public static bool autosprintLoaded = Tools.isLoaded("com.johnedwa.RTAutoSprintEx");
        public static bool is2r4rLoaded = Tools.isLoaded("com.HouseOfFruits.RiskierRain");
        public static bool isRiskyModLoaded = Tools.isLoaded("com.RiskyLives.RiskyMod");

        #region Config
        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> AllowBrokenSFX { get; set; }
        public static ConfigEntry<bool> RecolorMeteor { get; set; }
        public static ConfigEntry<bool> SurgeRework { get; set; }
        public static ConfigEntry<bool> ReducedEffectsSurgeRework { get; set; }
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
        public static float meleeRangeSingle = meleeRangeChannel + 4f;

        void Awake()
        {
            mageObject = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/MageBody");
            mageObject.AddComponent<ElementCounter>();
            mageBody = mageObject.GetComponent<CharacterBody>();
            mageSkillLocator = mageObject.GetComponent<SkillLocator>();
            if (mageObject && mageBody && mageSkillLocator)
            {
                Debug.Log("ARTIFICEREXTENDED setup succeeded!");
            }

            magePassive = CreateMagePassiveSlot(mageObject, mageSkillLocator);
            magePassiveFamily = magePassive.skillFamily;
            magePrimary = mageSkillLocator.primary.skillFamily;
            mageSecondary = mageSkillLocator.secondary.skillFamily;
            mageUtility = mageSkillLocator.utility.skillFamily;
            mageSpecial = mageSkillLocator.special.skillFamily;

            InitializeConfig();
            this.InitializeUnlocks();

            Debug.Log("ArtificerExtended setup succeeded!");

            CreateMagePassives(magePassiveFamily);
            On.RoR2.Skills.SkillCatalog.Init += ReplaceSkillDefs;
            On.RoR2.Skills.SkillCatalog.Init += ReplaceScepterSkillDefs;

            if (is2r4rLoaded)
            {
                artiNanoDamage = 12f;
                artiUtilCooldown = 8f;
            }

            AddHooks();
            ArtificerExtended.CoreModules.Assets.CreateZapDamageType();
            Buffs.CreateBuffs();
            Projectiles.CreateLightningSwords();
            Effects.DoEffects();
            this.ArtiChanges();
            this.InitializeSkills();
            if (isScepterLoaded)
            {
                this.InitializeScepterSkills();
            }
            On.RoR2.CharacterMaster.OnBodyStart += AddAEBodyFX;

            new ContentPacks().Initialize();
            VRStuff.SetupVR();
        }

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
            (passiveSkill.skillFamily as ScriptableObject).name = "MageBodyPassive";

            ContentPacks.skillFamilies.Add(passiveFamily);

            skillLocator.passiveSkill.enabled = false;
            foreach (var machine in body.GetComponents<EntityStateMachine>())
            {
                if (machine.customName == "Body")
                {
                    machine.mainStateType = new EntityStates.SerializableEntityStateType(typeof(EntityStates.GenericCharacterMain));
                }
            }

            On.RoR2.UI.LoadoutPanelController.Row.FromSkillSlot += (orig, owner, bodyI, slotI, slot) => {
                LoadoutPanelController.Row row = (LoadoutPanelController.Row)orig(owner, bodyI, slotI, slot);
                if ((slot.skillFamily as ScriptableObject).name.Contains("Passive"))
                {
                    Transform label = row.rowPanelTransform.Find("SlotLabel") ?? row.rowPanelTransform.Find("LabelContainer").Find("SlotLabel");
                    if (label)
                        label.GetComponent<LanguageTextMeshController>().token = "Misc";
                }
                return row;
            };
            IL.RoR2.UI.CharacterSelectController.BuildSkillStripDisplayData += (il) => {
                ILCursor c = new ILCursor(il);
                int skillIndex = -1;
                int defIndex = -1;
                var label = c.DefineLabel();
                if (c.TryGotoNext(x => x.MatchLdloc(out skillIndex), 
                    x => x.MatchLdfld(typeof(GenericSkill).GetField("hideInCharacterSelect")), 
                    x => x.MatchBrtrue(out label)) && skillIndex != (-1) 
                    && c.TryGotoNext(MoveType.After, 
                    x => x.MatchLdfld(typeof(SkillFamily.Variant).GetField("skillDef")), 
                    x => x.MatchStloc(out defIndex)))
                {
                    c.Emit(OpCodes.Ldloc, defIndex);
                    c.EmitDelegate<System.Func<SkillDef, bool>>((def) => def == passiveFamily.variants[0].skillDef);
                    c.Emit(OpCodes.Brtrue, label);
                    if (c.TryGotoNext(x => x.MatchCallOrCallvirt(typeof(List<StripDisplayData>).GetMethod("Add"))))
                    {
                        c.Remove();
                        c.Emit(OpCodes.Ldloc, skillIndex);
                        c.EmitDelegate<System.Action<List<StripDisplayData>, StripDisplayData, GenericSkill>>((list, disp, skill) => {
                            if ((skill.skillFamily as ScriptableObject).name == "MageBodyPassive")
                            {
                                list.Insert(0, disp);
                            }
                            else
                            {
                                list.Add(disp);
                            }
                        });
                    }
                }
            };
            IL.RoR2.UI.LoadoutPanelController.Rebuild += (il) => {
                ILCursor c = new ILCursor(il);
                if (c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(LoadoutPanelController.Row).GetMethod(nameof(LoadoutPanelController.Row.FromSkillSlot), (System.Reflection.BindingFlags)(-1)))))
                {
                    c.EmitDelegate<System.Func<LoadoutPanelController.Row, LoadoutPanelController.Row>>((orig) => {
                        var label = orig.rowPanelTransform.Find("SlotLabel") ?? orig.rowPanelTransform.Find("LabelContainer").Find("SlotLabel");
                        if (label && label.GetComponent<LanguageTextMeshController>().token == "Misc")
                        {
                            orig.rowPanelTransform.SetSiblingIndex(0);
                        }
                        return orig;
                    });
                }
            };
            return passiveSkill;
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
                $"for <style=cIsDamage>{Tools.ConvertDecimal(AltArtiPassive.lightningDamageMult)} damage.</style>");
            #endregion

            PassiveSkillDef resonanceSkillDef = ScriptableObject.CreateInstance<PassiveSkillDef>();
            resonanceSkillDef.skillNameToken = "MAGE_PASSIVE_ENERGY_NAME";
            resonanceSkillDef.skillDescriptionToken = "MAGE_PASSIVE_ENERGY_DESC";
            resonanceSkillDef.icon = iconBundle.LoadAsset<Sprite>(iconsPath + "ElementalIntensity.png");
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
            resonanceSkillDef.keywordTokens = new string[3] { "ARTIFICEREXTENDED_KEYWORD_MELT", "ARTIFICEREXTENDED_KEYWORD_ARCTICBLAST", "ARTIFICEREXTENDED_KEYWORD_BOLTS" };

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
                    unlockableDef = resonanceSkillDef.GetUnlockDef(typeof(ArtificerEnergyPassiveUnlock)),
                    viewableNode = new ViewablesCatalog.Node(resonanceSkillDef.skillNameToken, false, null)
                }
            };
            //
            ContentPacks.skillDefs.Add(hoverSkillDef);
            ContentPacks.skillDefs.Add(resonanceSkillDef);
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
            /*SkillDef flamer2 = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("Dragon's Breath"));
            if (flamer2 != null)
            {
                LanguageAPI.Add(SkillBase.Token + "FLAMETHROWER2_FIRE", "Dragon's Breath");
                LanguageAPI.Add(SkillBase.Token + "FLAMETHROWER2_DESC",
                    flamethrowerDesc +
                    "\n<color=#d299ff>SCEPTER: Hits leave behind a lingering fire cloud.</color>");
                flamer2.skillNameToken = SkillBase.Token + "FLAMETHROWER2_FIRE";
                flamer2.skillDescriptionToken = SkillBase.Token + "FLAMETHROWER2_DESC";
            }*/

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
                Debug.LogError("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                    "ArtificerExtended could not replace Ancient Scepter's Antimatter Surge. " +
                    "Antimatter Surge WILL break Artificer Extended's alt passives. \n" +
                    "Either turn on ArtificerExtended's Ion Surge rework to use ArtificerExtended's Antimatter Surge, " +
                    "avoid using Antimatter Surge with ArtificerExtended's alt passive, " +
                    "or tell the Ancient Scepter developers to get in contact to fix Antimatter Surge. \n" +
                    "This is NOT an error that can be fixed on the ArtificerExtended side.\n" +
                    "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            }
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

            ReducedEffectsSurgeRework = CustomConfigFile.Bind<bool>(
                "Ion Surge", "Reduce Effects",
                false,
                "Setting to TRUE will remove reworked Ion Surge's blink effect, for users who have trouble with the ability's flashing.");
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
