﻿using ArtificerExtended.Modules;
using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using EntityStates;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static ArtificerExtended.Skills.SkillBase;

namespace ArtificerExtended.Skills
{
    class FrostbiteSkill2 : ScepterSkillBase
    {
        public static BuffDef artiIceShield;
        public override int TargetVariant => 2;

        public override string SkillName => "Cryogenesis";

        public override string SkillDescription => "Cover yourself in a <style=cIsUtility>protective icy armor.</style> " +
                "Erupts once for <style=cIsDamage>300% damage,</style> then another <style=cIsUtility>Freezing</style> blast for <style=cIsDamage>500%.</style>" +
                "\n<color=#d299ff>SCEPTER: 3 Freezing novas - Ice armor is dramatically more powerful.</color>";

        public override string SkillLangTokenName => "FROSTBITE2";

        public override string IconName => "frostbitesketch1-2";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(Frostbite2);

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                mustKeyPress: false,
                canceledFromSprinting: true,
                beginSkillCooldownOnSkillEnd: true
            );

        public override void Hooks()
        {
        }

        public void RegisterBuffWhiteout()
        {
            artiIceShield = Content.CreateAndAddBuff("bdArtiIceShield2",
                ArtificerExtendedPlugin.iconBundle.LoadAsset<Sprite>(ArtificerExtendedPlugin.iconsPath + "texBuffFrostbiteShield.png"),
                Color.white,
                true, false);

            return;
            On.RoR2.CharacterBody.RecalculateStats += (On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self) =>
            {
                orig(self);
                int iceBuffCount = self.GetBuffCount(artiIceShield);
                if (iceBuffCount > 0)
                {
                    self.armor += 250;
                    self.moveSpeed *= 1.5f;
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

        public override void Init(ConfigFile config)
        {
            Hooks();
            RegisterBuffWhiteout();
            CreateLang();
            CreateSkill();
        }

        public void CastNova(CharacterBody self)
        {
            if (NetworkServer.active)
            {

                EffectManager.SpawnEffect(States.Frostbite.novaEffectPrefab, new EffectData
                {
                    origin = self.transform.position,
                    scale = States.Frostbite.novaRadius
                }, true);
                BlastAttack blastAttack = new BlastAttack();
                blastAttack.radius = Frostbite2.novaRadius;
                blastAttack.procCoefficient = Frostbite2.novaProcCoefficient;
                blastAttack.position = self.transform.position;
                blastAttack.attacker = self.gameObject;
                blastAttack.crit = Util.CheckRoll(self.crit, self.master);
                blastAttack.baseDamage = self.damage * Frostbite2.novaDamageCoefficient;
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
