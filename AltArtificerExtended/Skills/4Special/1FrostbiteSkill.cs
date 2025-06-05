using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ArtificerExtended.Modules;
using static R2API.RecalculateStatsAPI;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;
using RoR2.Achievements;
using static ArtificerExtended.Modules.Language.Styling;

namespace ArtificerExtended.Skills
{
    class _1FrostbiteSkill : SkillBase
    {
        public static GameObject novaEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/AffixWhiteExplosion");

        public static float blizzardDamageCoefficient = 5f;
        public static float blizzardProcCoefficient = 1f;
        public static float blizzardRadius = ArtificerExtendedPlugin.meleeRangeChannel;

        public static float novaDamageCoefficient = 5f;
        public static float novaProcCoefficient = 1f;
        public static float novaRadius = ArtificerExtendedPlugin.meleeRangeChannel;

        public static string frostArmorKeywordToken = ArtificerExtendedPlugin.DEVELOPER_PREFIX + "KEYWORD_FROSTARMOR";
        public static int bonusArmor = 100;
        public static int icicleCount = 3;
        public static float icicleDamage = 0.35f;
        public static int buffsForZeroMovementIncrease = 6;
        public static float movementIncreasePerBuff = 0.12f;
        public static float movementDecreasePerBuff = 0.15f;
        //whiteout
        public static GameObject icicleProjectilePrefab;
        public static GameObject blizzardArmorVFX;
        public static BuffDef artiIceShield;

        public static float buffInterval = 1.2f;
        public static int maxBuffStacks = 10;
        public static float maxDuration => buffInterval * maxBuffStacks;

        public override string SkillName => "Polar Vortex";

        public override string SkillDescription => $"<style=cIsUtility>Agile</style>. <style=cIsUtility>Frost</style>. " +
            $"Create a blast for <style=cIsDamage>{Tools.ConvertDecimal(blizzardDamageCoefficient)} damage</style>, then " +
            $"cover yourself in <style=cIsUtility>Frost Armor</style> for up to {maxDuration} seconds. " +
            $"Press again to cancel.";

        public override string TOKEN_IDENTIFIER => "FROSTBITE";

        public override Type RequiredUnlock => (typeof(AbsoluteZeroUnlock));

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(PolarVortexStart);

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                mustKeyPress: true,
                canceledFromSprinting: false,
                cancelSprintingOnActivation: false,
                beginSkillCooldownOnSkillEnd: true,
                fullRestockOnAssign: false
            );
        public override Sprite Icon => LoadSpriteFromBundle("frostbitesketch1");
        public override SkillSlot SkillSlot => SkillSlot.Special;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;
        public override Type BaseSkillDef => typeof(SkillDef);
        public override float BaseCooldown => 8;
        public override string ActivationStateMachineName => "Body";

        public override string ScepterSkillName => "Cryostasis"; 
        public override string ScepterSkillDesc => "+100 armor, +3 frost crystals."; 
        public override void Init()
        {
            LanguageAPI.Add(frostArmorKeywordToken, $"<style=cKeywordName>Frost Armor</style>" +
                $"<style=cSub>Gain <style=cIsHealing>+{bonusArmor} armor</style>, " +
                $"and <style=cIsUtility>Frost</style> nearby enemies for " +
                $"<style=cIsDamage>{icicleCount}x{Tools.ConvertDecimal(icicleDamage)} damage</style>. " +
                $"When ending, creates a second {UtilityColor("Frost")} blast for {DamageValueText(blizzardDamageCoefficient)}." +
                $"\nWhile armored, move " +
                $"<style=cIsUtility>up to +{Tools.ConvertDecimal(movementIncreasePerBuff * (buffsForZeroMovementIncrease - 1))} faster</style>, " +
                $"gradually decaying to " +
                $"<style=cIsUtility>-{Tools.ConvertDecimal(movementDecreasePerBuff * (10 - buffsForZeroMovementIncrease))} slower</style>. " +
                $"\n{UtilityColor("Frost Armor")} disables hovering, but {UtilityColor("prevents fall damage")}.</style>");

            //  Frostbite.blizzardDamageCoefficient = config.Bind<float>(
            //   "Skills Config: " + SkillName, "Primary Damage Coefficient",
            //   Frostbite.blizzardDamageCoefficient,
            //   "Determines the damage coefficient of the nova created when Frostbite is cast."
            //   ).Value;
            //  Frostbite.blizzardRadius = config.Bind<float>(
            //   "Skills Config: " + SkillName, "Primary Blast Radius",
            //   Frostbite.blizzardRadius,
            //   "Determines the radius of the nova created when Frostbite is cast."
            //   ).Value;
            //  
            //  Frostbite.novaDamageCoefficient = config.Bind<float>(
            //  "Skills Config: " + SkillName, "Secondary Damage Coefficient",
            //  Frostbite.novaDamageCoefficient,
            //  "Determines the damage coefficient of the nova created after the Frostbite buff expires."
            //  ).Value;
            //  Frostbite.novaRadius = config.Bind<float>(
            //  "Skills Config: " + SkillName, "Secondary Blast Radius",
            //  Frostbite.novaRadius,
            //  "Determines the radius of the nova created after the Frostbite buff expires."
            //  ).Value;
            KeywordTokens = new string[3] { "KEYWORD_AGILE", "KEYWORD_FROST", frostArmorKeywordToken };
            RegisterBuffWhiteout();
            RegisterArmorEffects();
            CreateIcicleProjectile();
            base.Init();
        }
        public override SkillDef ModifyScepterSkill(SkillDef scepterSkillDef)
        {
            scepterSkillDef.icon = LoadSpriteFromBundle("frostbitesketch1-2");
            return scepterSkillDef;
        }


        public override void Hooks()
        {
            GetStatCoefficients += FrostArmorStats;
        }

        private void FrostArmorStats(CharacterBody sender, StatHookEventArgs args)
        {
            int buffCount = sender.GetBuffCount(artiIceShield);
            if(buffCount > 0)
            {
                args.armorAdd += 100;
                if (buffCount < buffsForZeroMovementIncrease)
                    args.moveSpeedMultAdd += 0.12f * (buffsForZeroMovementIncrease - buffCount);
                else if (buffCount > buffsForZeroMovementIncrease)
                    args.moveSpeedReductionMultAdd += 0.15f * (buffCount - buffsForZeroMovementIncrease);
            }
        }

        private void CreateIcicleProjectile()
        {
            icicleProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/SoulSpiralProjectile.prefab").WaitForCompletion().InstantiateClone("ArtiOrbitIcicle", true);
            GameObject icicleGhostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Junk/Mage/MageIceboltExpandedGhost.prefab").WaitForCompletion().InstantiateClone("ArtiOrbitIcicleGhost");
            AnimateShaderAlpha asa = icicleGhostPrefab.GetComponentInChildren<AnimateShaderAlpha>();
            if (asa)
            {
                asa.timeMax = maxDuration;
            }
            ObjectScaleCurve osc = icicleGhostPrefab.GetComponentInChildren<ObjectScaleCurve>();
            if (osc)
            {
                osc.timeMax = maxDuration;
            }

            ProjectileController pc = icicleProjectilePrefab.GetComponent<ProjectileController>();
            if (pc)
            {
                pc.ghostPrefab = icicleGhostPrefab;
                pc.procCoefficient = 0.1f;
            }
            ProjectileOwnerOrbiter poo = icicleProjectilePrefab.GetComponent<ProjectileOwnerOrbiter>();
            if (poo)
            {
                poo.degreesPerSecond = -90;
            }
            ProjectileDamage pd = icicleProjectilePrefab.GetComponent<ProjectileDamage>();
            if (pd)
            {
                pd.damageType = new DamageTypeCombo(DamageType.Frost, DamageTypeExtended.Generic, DamageSource.Special);
            }
            UnseenHandHealingProjectile uhhp = icicleProjectilePrefab.GetComponent<UnseenHandHealingProjectile>();
            if(uhhp)
            {
                GameObject.Destroy(uhhp);
            }
        }

        private void RegisterArmorEffects()
        {
            GameObject baseShieldFx = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/TemporaryVisualEffects/ElephantDefense");

            blizzardArmorVFX = baseShieldFx.InstantiateClone("ArtificerExtendedBlizzardArmor", false);

            RotateObject blizzardArmorRotation = blizzardArmorVFX.GetComponent<RotateObject>();
            if(blizzardArmorRotation != null)
            {
                blizzardArmorRotation.rotationSpeed = new Vector3(0, 75, 0);
            }

            //Tools.DebugMaterial(blizzardArmorVFX);
            Material blizzardArmorMaterial = null;
            Tools.GetMaterial(blizzardArmorVFX, "ShieldMesh", Color.cyan, ref blizzardArmorMaterial, 2.5f, true);

            //blizzardArmorVFX.AddComponent<NetworkIdentity>();
            //TemporaryVisualEffect tempEffect = blizzardArmorVFX.GetComponent<TemporaryVisualEffect>();

            //Main.CreateEffect(blizzardArmorVFX);
        }

        public void RegisterBuffWhiteout()
        {
            artiIceShield = Content.CreateAndAddBuff("bdArtiIceShield",
                ArtificerExtendedPlugin.iconBundle.LoadAsset<Sprite>(ArtificerExtendedPlugin.iconsPath + "texBuffFrostbiteShield.png"),
                Color.white,
                true, false);
        }

        public void CastNova(CharacterBody self)
        {
            if (NetworkServer.active)
            {

                EffectManager.SpawnEffect(novaEffectPrefab, new EffectData
                {
                    origin = self.transform.position,
                    scale = novaRadius
                }, true);
                BlastAttack blastAttack = new BlastAttack();
                blastAttack.radius = novaRadius;
                blastAttack.procCoefficient = novaProcCoefficient;
                blastAttack.position = self.transform.position;
                blastAttack.attacker = self.gameObject;
                blastAttack.crit = Util.CheckRoll(self.crit, self.master);
                blastAttack.baseDamage = self.damage * novaDamageCoefficient;
                blastAttack.falloffModel = BlastAttack.FalloffModel.SweetSpot;
                blastAttack.baseForce = 0;
                blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
                blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;

                blastAttack.damageType = DamageType.Freeze2s;

                blastAttack.Fire();
            }
        }
    }
}
