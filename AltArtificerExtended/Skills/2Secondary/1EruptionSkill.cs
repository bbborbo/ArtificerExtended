using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtificerExtended.Skills
{
    class _1EruptionSkill : SkillBase<_1EruptionSkill>
    {
        public static GameObject meteorImpactEffectPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Items/MeteorAttackOnHighDamage/RunicMeteorStrikeImpact.prefab").WaitForCompletion();
        public static float minDuration = 0.2f;
        public static float maxDuration = 2f;
        public static float windDownDuration = 0.5f;
        public static float minBlastDamage = 3f;
        public static float maxBlastDamage = 8f;
        public static float minBlastRadius = 6f;
        public static float maxBlastRadius = 9f;
        public static int minClusterProjectiles = 2;
        public static int maxClusterProjectiles = 6;
        public static float clusterProjectileDamage = 3f;
        public override string SkillName => "Focused Nano-Eruption";

        public override string SkillDescription => $"<style=cIsDamage>Ignite</style>. Charge up a nano-eruption that " +
            $"deals <style=cIsDamage>{Tools.ConvertDecimal(minBlastDamage)}-{Tools.ConvertDecimal(maxBlastDamage)} damage</style> in an area, " +
            $"plus <style=cIsDamage>{minClusterProjectiles}-{maxClusterProjectiles} molten pools</style> " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(clusterProjectileDamage)} damage</style>.";

        public override string SkillLangTokenName => "ERUPTIONEXTENDED";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(MeteoriteDeathUnlock));

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
            KeywordTokens = new string[2] { "KEYWORD_IGNITE", "ARTIFICEREXTENDED_KEYWORD_LAVAPOOLS" };
            CreateSkill();
            CreateLang();
        }
    }
}
