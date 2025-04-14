using ArtificerExtended.Modules;
using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Skills
{
    class _3ColdFusionSkill : SkillBase
    {
        public static GameObject fusionTracer;
        public override string SkillName => "Cold Fusion";

        public override string SkillDescription => $"<style=cIsUtility>Chilling.</style> Channel a blast of cold spikes " +
            $"for <style=cIsDamage>{ColdFusion.minBulletCount}-{ColdFusion.maxBulletCount}x" +
            $"{Tools.ConvertDecimal(ColdFusion.totalDamageCoefficient / ColdFusion.maxBulletCount)} damage.</style>";

        public override string TOKEN_IDENTIFIER => "COLDFUSION";

        public override Type RequiredUnlock => (typeof(TankDamageUnlock));


        public override MageElement Element => MageElement.Ice;

        public override Type ActivationState => typeof(ColdFusion);

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                beginSkillCooldownOnSkillEnd: true
            );
        public override Sprite Icon => LoadSpriteFromBundle("FusionIcon");
        public override SkillSlot SkillSlot => SkillSlot.Secondary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;
        public override Type BaseSkillDef => typeof(SkillDef);
        public override float BaseCooldown => 5;
        public override void Init()
        {
            return;
            //ColdFusion.maxRange = config.Bind<float>(
            //    "Skills Config: " + SkillName, "Max Range",
            //    ColdFusion.maxRange,
            //    "Determines the maximum range Cold Fusion has. Damage Falloff still applies."
            //    ).Value;
            //ColdFusion.totalDamageCoefficient = config.Bind<float>(
            //    "Skills Config: " + SkillName, "Damage Coefficient",
            //    ColdFusion.totalDamageCoefficient,
            //    "Determines the TOTAL damage coefficient Cold Fusion has. Damage Falloff still applies."
            //    ).Value;
            //ColdFusion.minBulletCount = config.Bind<int>(
            //    "Skills Config: " + SkillName, "Spear Count",
            //    ColdFusion.minBulletCount,
            //    "Determines the amount of spears/bullets Cold Fusion has."
            //    ).Value;
            //ColdFusion.freezeChance = config.Bind<float>(
            //    "Skills Config: " + SkillName, "Freeze Chance",
            //    ColdFusion.freezeChance,
            //    "Determines the chance per spear/bullet has to freeze on hit."
            //    ).Value;
            //KeywordTokens = new string[2] { ChillRework.ChillRework.chillKeywordToken, "KEYWORD_FREEZING" };
            //
            //CreateFusionTracer();
            base.Init();
        }

        public override void Hooks()
        {

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

            Content.CreateAndAddEffectDef(fusionTracer);
        }
    }
}
