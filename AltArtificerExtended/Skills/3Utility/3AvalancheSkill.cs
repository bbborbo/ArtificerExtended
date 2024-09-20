﻿using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using ArtificerExtended.Unlocks;
using UnityEngine;
using ArtificerExtended.EntityState;
using RoR2.Projectile;

namespace ArtificerExtended.Skills
{
    class _3AvalancheSkill : SkillBase
    {
        public override string SkillName => "Temperature Drop";

        public override string SkillDescription => $"<style=cIsUtility>Freezing.</style> Hold to plummet downwards, " +
            $"creating a blast of <style=cIsDamage>{Tools.ConvertDecimal(Avalanche.damageCoefficient)} damage</style> at your position " +
            $"when released or on impact. Blast radius scales with fall distance.";

        public override string SkillLangTokenName => "TEMPDROP";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(FreezeManySimultaneousUnlock));

        public override string IconName => "AvalancheIcon";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(Avalanche);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.mageUtility;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: ArtificerExtendedPlugin.artiUtilCooldown,
                interruptPriority: InterruptPriority.Skill,
                mustKeyPress: true
            );


        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            return;
            Avalanche.damageCoefficient = config.Bind<float>(
             "Skills Config: " + SkillName, "Damage Coefficient",
             Avalanche.damageCoefficient,
             "Determines the damage coefficient of temperature drop."
             ).Value;
            Avalanche.minRadius = config.Bind<float>(
             "Skills Config: " + SkillName, "Minimum Blast Radius",
             Avalanche.minRadius,
             "Determines the minimum radius of temperature drop."
             ).Value;
            Avalanche.maxRadius = config.Bind<float>(
             "Skills Config: " + SkillName, "Maximum Blast Radius",
             Avalanche.maxRadius,
             "Determines the maximum radius of temperature drop."
             ).Value;

            KeywordTokens = new string[1] { "KEYWORD_FREEZING" };

            CreateLang();
            CreateSkill();
        }
    }
}
