using ArtificerExtended.Components;
using ArtificerExtended.CoreModules;
using ArtificerExtended.EntityState;
using BepInEx.Configuration;
using EntityStates;
using EntityStates.Mage;
using EntityStates.Seeker;
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
    class _1HeatColumnSkill : SkillBase<_1HeatColumnSkill>
    {
        public static BuffDef HeatWardBuff;
        public static GameObject HeatWardPrefab;
        public static GameObject HeatWardAreaIndicator;

        public static float heatWardRadius = 12f;
        public static float heatWardDuration = 8f;
        public static float maxHeatRiseRate = 9f;
        public static float igniteDuration = 1;
        public static float igniteFrequency = 0.5f;
        public static float igniteDamage = 0.75f;

        public override string SkillName => "Rising Flame";

        public override string SkillDescription => $"Create a column of heat, " +
            $"<style=cIsDamage>igniting</style> enemies inside for <style=cIsDamage>{Tools.ConvertDecimal(igniteDamage / igniteFrequency)} damage per second</style>. " +
            $"Allies inside the column <style=cIsUtility>rise into the air</style> while holding the Jump key.";

        public override string SkillLangTokenName => "HEATCOLUMN";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(CastHeatColumn);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.mageUtility;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseMaxStock: 1,
                baseRechargeInterval: 16f,
                interruptPriority: InterruptPriority.Skill,
                canceledFromSprinting: true
            );

        public override void Hooks()
        {
            On.EntityStates.GenericCharacterMain.FixedUpdate += RiseFromHeat;
        }

        private void RiseFromHeat(On.EntityStates.GenericCharacterMain.orig_FixedUpdate orig, global::EntityStates.GenericCharacterMain self)
        {
            orig(self);
            if (self.hasCharacterMotor && self.hasInputBank && self.isAuthority)
            {
                CharacterBody body = self.characterBody;
                CharacterMotor motor = self.characterMotor;
                if (body && body.HasBuff(HeatWardBuff))
                {
                    bool jumpButtonState = self.inputBank.jump.down;
                    if (jumpButtonState == true)
                    {
                        float verticalVelocity = motor.velocity.y;
                        float maxUpVelocity = Mathf.Clamp(maxHeatRiseRate * (body.moveSpeed / body.baseMoveSpeed), JetpackOn.hoverVelocity, maxHeatRiseRate);

                        float multiplier = verticalVelocity < 0 ? 2f : 1f;
                        verticalVelocity = Mathf.MoveTowards(verticalVelocity, maxUpVelocity, JetpackOn.hoverAcceleration * self.GetDeltaTime() * multiplier);
                        motor.velocity = new Vector3(motor.velocity.x, verticalVelocity, motor.velocity.z);
                    }
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[1] { "KEYWORD_IGNITE" };
            CreateSkill();
            CreateLang();
            Hooks();
            CreateRisingHeatBuff();
            CreateHeatWardPrefab();
        }

        private void CreateRisingHeatBuff()
        {
            HeatWardBuff = ScriptableObject.CreateInstance<BuffDef>();

            HeatWardBuff.name = "RisingHeatBuff";
            HeatWardBuff.canStack = false;
            HeatWardBuff.isDebuff = false;
            HeatWardBuff.iconSprite = ArtificerExtendedPlugin.iconBundle.LoadAsset<Sprite>(ArtificerExtendedPlugin.iconsPath + "texBuffFrostbiteShield.png");
            HeatWardBuff.buffColor = Color.yellow;

            Buffs.AddBuff(HeatWardBuff);
        }

        private void CreateHeatWardPrefab()
        {
            HeatWardAreaIndicator = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Seeker/UnseenHandAreaIndicator.prefab").WaitForCompletion();
            HeatWardAreaIndicator.transform.rotation = Quaternion.identity;

            Material heatWardIndicatorMaterial = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/DLC2/Seeker/matUnseenHandAreaIndicator.mat").WaitForCompletion());
            heatWardIndicatorMaterial.SetColor("_TintColor", new Color32(146, 73, 0, 201)/*(150, 110, 0, 191)*/);
            //encourageWardMaterial.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampLightningYellowOffset.png").WaitForCompletion());

            MeshRenderer mr1 = HeatWardAreaIndicator.GetComponentInChildren<MeshRenderer>();
            mr1.material = heatWardIndicatorMaterial;


            GameObject itSafeWard = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/InfiniteTowerSafeWardAwaitingInteraction.prefab").WaitForCompletion();
            GameObject verticalWard = itSafeWard.transform.Find("Indicator")?.gameObject;
            GameObject encourageWardIndicator = PrefabAPI.InstantiateClone(verticalWard, "EncourageWardIndicatorPrefab");

            Material encourageWardMaterial = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/Base/WardOnLevel/matWarbannerSphereIndicator2.mat").WaitForCompletion());
            encourageWardMaterial.SetColor("_TintColor", new Color32(146, 73, 0, 201)/*(150, 110, 0, 191)*/);
            //encourageWardMaterial.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampLightningYellowOffset.png").WaitForCompletion());

            MeshRenderer mr2 = encourageWardIndicator.GetComponentInChildren<MeshRenderer>();
            mr2.material = encourageWardMaterial;

            HeatWardPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerMineAltDetonated.prefab").WaitForCompletion(), "EncourageWardPrefab", true);
            if (HeatWardPrefab)
            {
                HeatWardPrefab.transform.rotation = Quaternion.identity;

                ProjectileController projectileController = HeatWardPrefab.GetComponent<ProjectileController>();
                if(projectileController != null)
                {
                    projectileController.cannotBeDeleted = true;
                }

                SlowDownProjectiles sdp = HeatWardPrefab.GetComponent<SlowDownProjectiles>();
                if (sdp)
                    GameObject.Destroy(sdp);
                SphereCollider collider = HeatWardPrefab.GetComponent<SphereCollider>();
                if (collider)
                    GameObject.Destroy(collider);
                GameObject areaIndicator = HeatWardPrefab.transform.Find("AreaIndicator").gameObject;
                if (areaIndicator)
                    GameObject.Destroy(areaIndicator);

                encourageWardIndicator.transform.parent = HeatWardPrefab.transform;
                encourageWardIndicator.transform.localScale = new Vector3(heatWardRadius, encourageWardIndicator.transform.localScale.y, heatWardRadius);
                encourageWardIndicator.transform.rotation = Quaternion.identity;
                BuffWard buffWard = HeatWardPrefab.GetComponent<BuffWard>();
                if (buffWard)
                {
                    buffWard.rangeIndicator = verticalWard ? encourageWardIndicator.transform : buffWard.rangeIndicator;
                    buffWard.radius = heatWardRadius;
                    buffWard.buffDef = HeatWardBuff;// Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Common/bdSlow50.asset").WaitForCompletion();//HeatWardBuff;
                    buffWard.buffDuration = 0.5f;
                    buffWard.buffTimer = 0.5f;
                    buffWard.expireDuration = heatWardDuration;
                    buffWard.shape = BuffWard.BuffWardShape.VerticalTube;
                    buffWard.invertTeamFilter = false;
                    buffWard.requireGrounded = false;
                }
                if (projectileController)
                {
                    DotWard dotWard = HeatWardPrefab.AddComponent<DotWard>();
                    if (dotWard)
                    {
                        dotWard.projectileController = projectileController;
                        dotWard.dotIndex = DotController.DotIndex.Burn;
                        dotWard.damageCoefficient = 1;

                        dotWard.rangeIndicator = verticalWard ? encourageWardIndicator.transform : buffWard.rangeIndicator;
                        dotWard.radius = heatWardRadius;
                        dotWard.buffDuration = igniteDuration;
                        dotWard.buffTimer = igniteFrequency;
                        dotWard.expireDuration = heatWardDuration;
                        dotWard.shape = BuffWard.BuffWardShape.VerticalTube;
                        dotWard.invertTeamFilter = true;
                    }
                }
            }
        }
    }
}
