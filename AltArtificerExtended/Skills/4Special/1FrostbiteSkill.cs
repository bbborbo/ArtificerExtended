using AltArtificerExtended.EntityState;
using AltArtificerExtended.Unlocks;
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

namespace AltArtificerExtended.Skills
{
    class _1FrostbiteSkill : SkillBase
    {
        //whiteout
        public static GameObject blizzardProjectilePrefab;
        public static GameObject blizzardArmorVFX;
        public static BuffDef artiIceShield;
        public static float blizzardBuffDuration = 3;


        public override string SkillName => "Frostbite";

        public override string SkillDescription => "Cover yourself in a <style=cIsUtility>protective icy armor.</style> " +
                "Erupts once for <style=cIsDamage>300% damage,</style> then another <style=cIsUtility>Freezing</style> blast for <style=cIsDamage>500%.</style>";

        public override string SkillLangTokenName => "FROSTBITE";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerFrostbiteUnlock));

        public override string IconName => "frostbitesketch1";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(Frostbite);

        public override SkillFamily SkillSlot => Main.mageSpecial;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 8,
                interruptPriority: InterruptPriority.Skill,
                mustKeyPress: false,
                canceledFromSprinting: true
            );


        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[2] { "KEYWORD_FREEZING", "ARTIFICEREXTENDED_KEYWORD_CHILL" };
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
                artiIceShield.iconSprite = Main.iconBundle.LoadAsset<Sprite>(Main.iconsPath + "texBuffFrostbiteShield.png");
                artiIceShield.canStack = true;
                artiIceShield.isDebuff = false;
            }
            Main.AddBuff(artiIceShield);

            On.RoR2.CharacterBody.RecalculateStats += (On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self) =>
            {
                orig(self);
                int iceBuffCount = self.GetBuffCount(artiIceShield);
                if (iceBuffCount > 0)
                {
                    self.armor += 150;
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
                if (Main.AllowBrokenSFX.Value == true)
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
                blastAttack.damageType = DamageType.Freeze2s;
                blastAttack.baseForce = 0;
                blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
                blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
                blastAttack.Fire();
            }
        }
    }
}
