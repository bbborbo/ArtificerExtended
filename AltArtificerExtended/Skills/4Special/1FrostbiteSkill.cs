using ArtificerExtended.EntityState;
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
using ArtificerExtended.CoreModules;
using static R2API.RecalculateStatsAPI;

namespace ArtificerExtended.Skills
{
    class _1FrostbiteSkill : SkillBase
    {
        //whiteout
        public static GameObject blizzardProjectilePrefab;
        public static GameObject blizzardArmorVFX;
        public static BuffDef artiIceShield;

        public static float buffInterval = 1.5f;
        public static int maxBuffStacks = 10;
        public static float maxDuration => buffInterval * maxBuffStacks;

        public override string SkillName => "Polar Vortex";

        public override string SkillDescription => $"Cover yourself in a <style=cIsUtility>protective icy armor</style> for {maxDuration} seconds. " +
            $"Erupts once for <style=cIsDamage>{Tools.ConvertDecimal(Frostbite.blizzardDamageCoefficient)}, " +
            $"</style>then another <style=cIsUtility>Freezing</style> blast " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(Frostbite.novaDamageCoefficient)}.</style>";

        public override string SkillLangTokenName => "FROSTBITE";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerFrostbiteUnlock));

        public override string IconName => "frostbitesketch1";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(PolarVortexStart);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.mageSpecial;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 8,
                interruptPriority: InterruptPriority.Skill,
                mustKeyPress: true,
                canceledFromSprinting: false,
                cancelSprintingOnActivation: false,
                activationStateMachineName: "Body",
                beginSkillCooldownOnSkillEnd: true,
                fullRestockOnAssign: false
            );


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
                if (buffCount < 6)
                    args.moveSpeedMultAdd += 0.12f * (6 - buffCount);
                else if (buffCount > 6)
                    args.moveSpeedReductionMultAdd += 0.25f * (buffCount - 6);
            }
        }

        public override void Init(ConfigFile config)
        {
        
            Frostbite.blizzardDamageCoefficient = config.Bind<float>(
             "Skills Config: " + SkillName, "Primary Damage Coefficient",
             Frostbite.blizzardDamageCoefficient,
             "Determines the damage coefficient of the nova created when Frostbite is cast."
             ).Value;
            Frostbite.blizzardRadius = config.Bind<float>(
             "Skills Config: " + SkillName, "Primary Blast Radius",
             Frostbite.blizzardRadius,
             "Determines the radius of the nova created when Frostbite is cast."
             ).Value;

            Frostbite.novaDamageCoefficient = config.Bind<float>(
            "Skills Config: " + SkillName, "Secondary Damage Coefficient",
            Frostbite.novaDamageCoefficient,
            "Determines the damage coefficient of the nova created after the Frostbite buff expires."
            ).Value;
            Frostbite.novaRadius = config.Bind<float>(
            "Skills Config: " + SkillName, "Secondary Blast Radius",
            Frostbite.novaRadius,
            "Determines the radius of the nova created after the Frostbite buff expires."
            ).Value;
            KeywordTokens = new string[2] { "KEYWORD_FREEZING", ChillRework.ChillRework.chillKeywordToken };
            RegisterBuffWhiteout();
            RegisterArmorEffects();

            Hooks();
            CreateLang();
            CreateSkill();
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
            artiIceShield = ScriptableObject.CreateInstance<BuffDef>();
            {
                artiIceShield.name = "artiIceShield";
                artiIceShield.iconSprite = ArtificerExtendedPlugin.iconBundle.LoadAsset<Sprite>(ArtificerExtendedPlugin.iconsPath + "texBuffFrostbiteShield.png");
                artiIceShield.canStack = true;
                artiIceShield.isDebuff = false;
            }
            Buffs.AddBuff(artiIceShield);

            return;
            On.RoR2.CharacterBody.RecalculateStats += (On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self) =>
            {
                orig(self);
                int iceBuffCount = self.GetBuffCount(artiIceShield);
                if (iceBuffCount > 0)
                {
                    self.armor += 100;
                    self.moveSpeed *= 1.3f;
                }
            };
            On.RoR2.CharacterBody.RemoveBuff_BuffIndex += (On.RoR2.CharacterBody.orig_RemoveBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType) =>
            {
                if (buffType == artiIceShield.buffIndex)
                {
                    CastNova(self);
                }
                orig(self, buffType);
            };
        }

        public void CastNova(CharacterBody self)
        {
            if (NetworkServer.active)
            {
                if (ArtificerExtendedPlugin.AllowBrokenSFX.Value == true)
                    Util.PlaySound(PrepWall.prepWallSoundString, self.gameObject);

                EffectManager.SpawnEffect(EntityState.Frostbite.novaEffectPrefab, new EffectData
                {
                    origin = self.transform.position,
                    scale = EntityState.Frostbite.novaRadius
                }, true);
                BlastAttack blastAttack = new BlastAttack();
                blastAttack.radius = Frostbite.novaRadius;
                blastAttack.procCoefficient = Frostbite.novaProcCoefficient;
                blastAttack.position = self.transform.position;
                blastAttack.attacker = self.gameObject;
                blastAttack.crit = Util.CheckRoll(self.crit, self.master);
                blastAttack.baseDamage = self.damage * Frostbite.novaDamageCoefficient;
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
