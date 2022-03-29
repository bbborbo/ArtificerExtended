using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using AltArtificerExtended.Unlocks;
using UnityEngine;
using AltArtificerExtended.EntityState;
using RoR2.Projectile;

namespace AltArtificerExtended.Skills
{
    class _3AvalancheSkill : SkillBase
    {
        public override string SkillName => "Temperature Drop";

        public override string SkillDescription => $"<style=cIsUtility>Freezing.</style> Hold to plummet downwards, " +
            $"creating a blast of <style=cIsDamage>{Tools.ConvertDecimal(Avalanche.damageCoefficient)} damage</style> at your position " +
            $"when released or on impact. Blast radius scales with fall distance.";

        public override string SkillLangTokenName => "TEMPDROP";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerTempDropUnlock));

        public override string IconName => "AvalancheIcon";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(Avalanche);

        public override SkillFamily SkillSlot => Main.mageUtility;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: Main.artiUtilCooldown,
                interruptPriority: InterruptPriority.Skill,
                mustKeyPress: true
            );


        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            Avalanche.damageCoefficient = config.Bind<float>(
             SkillName, "Damage Coefficient",
             Avalanche.damageCoefficient,
             "Determines the damage coefficient of temperature drop."
             ).Value;
            Avalanche.minRadius = config.Bind<float>(
             SkillName, "Minimum Blast Radius",
             Avalanche.minRadius,
             "Determines the minimum radius of temperature drop."
             ).Value;
            Avalanche.maxRadius = config.Bind<float>(
             SkillName, "Maximum Blast Radius",
             Avalanche.maxRadius,
             "Determines the maximum radius of temperature drop."
             ).Value;

            KeywordTokens = new string[1] { "KEYWORD_FREEZING" };

            CreateLang();
            CreateSkill();
        }
    }
}
