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
        public override string SkillName => "Thunderstrike";

        public override string SkillDescription => $"<style=cIsDamage>Stunning</style>. " +
            $"Become a beam of energy, surging forward a short distance. " +
            $"Enemies struck will attract lightning for <style=cIsDamage>{Tools.ConvertDecimal(delayDamageCoefficient)} damage</style>.";

        public override string SkillLangTokenName => "THUNDERDASH";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(OverkillOverloadingUnlock));

        public override string IconName => "shockwaveicon";

        public override MageElement Element => MageElement.Lightning;

        public override Type ActivationState => typeof(ThunderStrikeDash);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.mageSecondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 6,
                interruptPriority: InterruptPriority.Skill,
                mustKeyPress: true
            );

        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[1] { "KEYWORD_STUNNING" };

            lightningOrbEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LightningStrikeOnHit/SimpleLightningStrikeOrbEffect.prefab").WaitForCompletion();
            lightningImpactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LightningStrikeOnHit/SimpleLightningStrikeImpact.prefab").WaitForCompletion();

            CreateLang();
            CreateSkill();

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
                        hitboxTransform.transform.localScale = Vector3.one * 8f;

                        hitBoxGroup.hitBoxes = new HitBox[1] { hitBox };
                    }
                }
            }
        }

        private void RegisterProjectileShockwave()
        {
            shockwaveZapConePrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LoaderZapCone").InstantiateClone("shockwaveZapCone", true);

            ProjectileProximityBeamController ppbc = shockwaveZapConePrefab.GetComponent<ProjectileProximityBeamController>();
            ppbc.attackRange = shockwaveMaxRange - 5;
            ppbc.maxAngleFilter = shockwaveMaxAngleFilter;
            ppbc.damageCoefficient = 0; // 10;
            ppbc.procCoefficient = 1;
            ppbc.attackInterval = 0.15f;

            ShakeEmitter shake = shockwaveZapConePrefab.AddComponent<ShakeEmitter>();
            shake.radius = 80; //40
            shake.duration = 0.3f; //0.2f
            shake.shakeOnEnable = false;
            shake.shakeOnStart = true;
            shake.amplitudeTimeDecay = true;
            shake.scaleShakeRadiusWithLocalScale = false;

            shockwaveZapConePrefab.GetComponent<DestroyOnTimer>().duration = 2f;

            ContentPacks.projectilePrefabs.Add(shockwaveZapConePrefab);
        }
    }
}
