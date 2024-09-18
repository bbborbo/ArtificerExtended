using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using ArtificerExtended.Unlocks;
using UnityEngine;
using ArtificerExtended.EntityState;
using R2API;
using R2API.Utils;
using ArtificerExtended.CoreModules;

namespace ArtificerExtended.Skills
{
    class _1MeteorSkill : SkillBase
    {
        //meteor
        public static GameObject meteorImpactPrefab;

        public override string SkillName => "Channeled Nano-Inferno";

        public override string SkillDescription => $"<style=cIsDamage>Ignite</style>. Charge up a storm of 1-{ChargeMeteors.maxMeteors} nano-meteors that each deal " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(ChargeMeteors.meteorDamageCoefficient)} damage</style>.";

        public override string SkillLangTokenName => "METEORS";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(ArtificerMeteorUnlock));

        public override string IconName => "meteoricon";

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(ChargeMeteors);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.mageSecondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 5,
                beginSkillCooldownOnSkillEnd: true,
                mustKeyPress: true
            );


        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            RegisterProjectileMeteor(config);
            CreateLang();
            CreateSkill();
        }
        private void RegisterProjectileMeteor(ConfigFile config)
        {
            meteorImpactPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/MeteorStrikeImpact").InstantiateClone("NanometeorImpact", false);
            float newScale = 2f;
            meteorImpactPrefab.transform.localScale = new Vector3(newScale, newScale, newScale);

            bool recolorMeteor = config.Bind<bool>(
                "Skills Config: " + SkillName,
                "Recolor Nano-Meteor",
                true,
                "Choose to have either the ugly red nano-meteor or the ugly blue nano-meteor that is definitely not glowing meteorite."
                ).Value;

            KeywordTokens = new string[1] { "KEYWORD_IGNITE" };

            if (recolorMeteor)
            {
                Color napalmColor = new Color32(255, 52, 0, 255);
                Color dustColor = new Color32(112, 11, 7, 255);
                Color dustColor2 = new Color32(148, 40, 6, 255);
                //Tools.DebugMaterial(meteorImpactPrefab);
                //Tools.DebugLight(meteorImpactPrefab);
                //Tools.DebugParticleSystem(meteorImpactPrefab);

                Tools.GetLight(meteorImpactPrefab, "Point Light", napalmColor);

                Tools.GetParticle(meteorImpactPrefab, "Debris", Color.red);
                Tools.GetParticle(meteorImpactPrefab, "Dust", dustColor);
                Tools.GetParticle(meteorImpactPrefab, "Dust, Directional", dustColor2);
                Tools.GetParticle(meteorImpactPrefab, "Flash", Color.red);
                Tools.GetParticle(meteorImpactPrefab, "Sparks", Color.yellow);
                Tools.GetParticle(meteorImpactPrefab, "Flash Lines", Color.red);
                Tools.GetParticle(meteorImpactPrefab, "Flash Lines, Fire", napalmColor);
                Tools.GetParticle(meteorImpactPrefab, "Fire", napalmColor);
            }
            Effects.CreateEffect(meteorImpactPrefab);
        }
    }
}
