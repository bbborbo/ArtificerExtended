using AltArtificerExtended.EntityState;
using BepInEx.Configuration;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AltArtificerExtended.Skills
{
    class _3ColdFusionSkill : SkillBase
    {
        public static GameObject fusionTracer;
        public override string SkillName => "Cold Fusion";

        public override string SkillDescription => $"<style=cIsUtility>Chilling.</style> Channel a blast of cold spikes " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(ColdFusion.totalDamageCoefficient)} total damage.</style> " +
            $"Each hit has a <style=cIsUtility>{ColdFusion.freezeChance}% chance to Freeze.</style> " +
            $"Releasing the attack before fully charging returns a stock and doesn't fire.";

        public override string SkillLangTokenName => "COLDFUSION";

        public override UnlockableDef UnlockDef => ScriptableObject.CreateInstance<UnlockableDef>();//GetUnlockDef(typeof(ArtificerColdFusionUnlock));

        public override string IconName => "FusionIcon";

        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(ColdFusion);

        public override SkillFamily SkillSlot => Main.mageSecondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 5,
                interruptPriority: InterruptPriority.Skill,
                beginSkillCooldownOnSkillEnd: true
            );

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {

            ColdFusion.maxRange = config.Bind<float>(
                "Skills Config: " + SkillName, "Max Range",
                ColdFusion.maxRange,
                "Determines the maximum range Cold Fusion has. Damage Falloff still applies."
                ).Value;
            ColdFusion.totalDamageCoefficient = config.Bind<float>(
                "Skills Config: " + SkillName, "Damage Coefficient",
                ColdFusion.totalDamageCoefficient,
                "Determines the TOTAL damage coefficient Cold Fusion has. Damage Falloff still applies."
                ).Value;
            ColdFusion.bulletCount = config.Bind<int>(
                "Skills Config: " + SkillName, "Spear Count",
                ColdFusion.bulletCount,
                "Determines the amount of spears/bullets Cold Fusion has."
                ).Value;
            ColdFusion.freezeChance = config.Bind<float>(
                "Skills Config: " + SkillName, "Freeze Chance",
                ColdFusion.freezeChance,
                "Determines the chance per spear/bullet has to freeze on hit."
                ).Value;
            KeywordTokens = new string[2] { "ARTIFICEREXTENDED_KEYWORD_CHILL", "KEYWORD_FREEZING" };

            CreateFusionTracer();
            CreateLang();
            CreateSkill();
        }

        private void CreateFusionTracer()
        {
            fusionTracer = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/tracers/TracerHuntressSnipe").InstantiateClone("tracerMageFusion", false); //TracerCommandoDefault, TracerGoldGat
            Tracer shotgunTracer = fusionTracer.GetComponent<Tracer>();
            //shotgunTracer.length = 35f;
            shotgunTracer.speed = 75f;
            VFXAttributes shotgunAttributes = fusionTracer.AddComponent<VFXAttributes>();
            shotgunAttributes.vfxPriority = VFXAttributes.VFXPriority.Medium;
            shotgunAttributes.vfxIntensity = VFXAttributes.VFXIntensity.Medium;

            Main.CreateEffect(fusionTracer);
        }
    }
}
