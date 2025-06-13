using ArtificerExtended.Modules;
using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtificerExtended.Skills
{
    class _2ThunderstrikeSkill : SkillBase
    {
        public static GameObject shockwaveZapConePrefab;
        public static int shockwaveMaxAngleFilter = 35;
        public static float shockwaveMaxRange = 20;


        public const string ThunderStrikeHitBoxGroupName = "Strike";
        public static float baseDuration = 0.36f;
        public static float durationMultOnHit = 1.3f;
        public static GameObject lightningOrbEffect;
        public static GameObject lightningImpactEffect;
        public static Material enterOverlayMaterial;
        public static float speedCoefficient = 9f;
        public static float damageCoefficient = 1;
        public static float procCoefficient = 0.25f;
        public static float delayDamageCoefficient = 9;
        public static float delayProcCoefficient = 1;
        public static float resonantCdrFirst = 1f;
        public static float resonantCdr = 0.5f;
        public static int resonantCdrMax = 15;
        public override string SkillName => "Pulse Strike";

        public override string SkillDescription => $"<style=cIsUtility>Resonant</style>. <style=cIsDamage>Stunning</style>. " +
            $"Become a beam of energy, surging forward a short distance. " +
            $"Enemies struck will attract lightning for <style=cIsDamage>{Tools.ConvertDecimal(damageCoefficient + delayDamageCoefficient)} damage</style>.";

        public override string TOKEN_IDENTIFIER => "THUNDERDASH";

        public override Type RequiredUnlock => (typeof(OverkillOverloadingUnlock));


        public override MageElement Element => MageElement.Lightning;

        public override Type ActivationState => typeof(ThunderStrikeDash);

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                mustKeyPress: true
            );
        public override Sprite Icon => LoadSpriteFromBundle("pulsestrikeAE2");
        public override SkillSlot SkillSlot => SkillSlot.Secondary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;
        public override Type BaseSkillDef => typeof(SkillDef);
        public override float BaseCooldown => 8;
        public override void Init()
        {
            string resonantKeywordToken = ArtificerExtendedPlugin.DEVELOPER_PREFIX + "KEYWORD_RESONANTTHUNDERDASH";
            CommonAssets.AddResonantKeyword(resonantKeywordToken, "Pulse Conduit",
                $"If only <style=cIsDamage>Lightning</style> abilities are equipped, striking an enemy reduces {SkillName} cooldown by <style=cIsUtility>{resonantCdrFirst} second, </style>" +
                $"plus <style=cIsUtility>{resonantCdr} seconds</style> for every additional enemy (up to <style=cIsUtility>{resonantCdrMax}</style> times).");
            KeywordTokens = new string[] { resonantKeywordToken, "KEYWORD_STUNNING" };

            lightningOrbEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LightningStrikeOnHit/SimpleLightningStrikeOrbEffect.prefab").WaitForCompletion();
            lightningImpactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LightningStrikeOnHit/SimpleLightningStrikeImpact.prefab").WaitForCompletion();
            base.Init();

            // hitbox setup
            ModelLocator modelLocator = ArtificerExtendedPlugin.mageBody.GetComponent<ModelLocator>();
            Transform modelTransform = modelLocator?.modelTransform;
            if (modelTransform)
            {
                HitBoxGroup hitBoxGroup = modelTransform.gameObject.AddComponent<HitBoxGroup>();
                hitBoxGroup.groupName = ThunderStrikeHitBoxGroupName;

                ChildLocator childLocator = modelTransform.GetComponent<ChildLocator>();
                if (childLocator)
                {
                    Transform rootTransform = childLocator.FindChild("Base")?.parent;
                    if (rootTransform)
                    {
                        GameObject hitboxTransform = new GameObject();
                        HitBox hitBox = hitboxTransform.AddComponent<HitBox>();
                        hitboxTransform.transform.parent = rootTransform;
                        hitboxTransform.layer = LayerIndex.projectile.intVal;
                        hitboxTransform.transform.localPosition = new Vector3(0, 1.564f, 0);
                        hitboxTransform.transform.localRotation = Quaternion.identity;
                        hitboxTransform.transform.localScale = Vector3.one * 10f;

                        hitBoxGroup.hitBoxes = new HitBox[1] { hitBox };
                    }
                }
            }
        }

        public override void Hooks()
        {
        }
    }
}
