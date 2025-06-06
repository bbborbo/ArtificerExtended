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
using ThreeEyedGames;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtificerExtended.Skills
{
    class _1EruptionSkill : SkillBase<_1EruptionSkill>
    {
        public static GameObject meteorImpactEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Items/MeteorAttackOnHighDamage/RunicMeteorStrikeImpact.prefab").WaitForCompletion();
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

        public override string TOKEN_IDENTIFIER => "ERUPTIONEXTENDED";

        public override Type RequiredUnlock => (typeof(MeteoriteDeathUnlock));

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(ChargeMeteors);

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                beginSkillCooldownOnSkillEnd: true,
                mustKeyPress: true
            );
        public override Sprite Icon => LoadSpriteFromBundle("eruptionAE");
        public override SkillSlot SkillSlot => SkillSlot.Secondary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;
        public override Type BaseSkillDef => typeof(SkillDef);
        public override float BaseCooldown => 5;
        public override void Init()
        {
            KeywordTokens = new string[2] { CommonAssets.lavaPoolKeywordToken, "KEYWORD_IGNITE" };
            CreateMeteorImpactEffect();
            base.Init();
        }

        private void CreateMeteorImpactEffect()
        {
            meteorImpactEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikeImpact.prefab").WaitForCompletion().InstantiateClone("MageEruptionImpact", false);
            float newScale = 2f;
            meteorImpactEffectPrefab.transform.localScale = Vector3.one * newScale;
            Light light = meteorImpactEffectPrefab.GetComponentInChildren<Light>();
            if (light)
            {
                light.color = new Color32(255, 96, 102, 255);
            }

            ParticleSystemRenderer[] psrs = meteorImpactEffectPrefab.GetComponentsInChildren<ParticleSystemRenderer>();
            for (int i = 0; i < psrs.Length; i++)
            {
                ParticleSystemRenderer psr = psrs[i];
                string name = psr.gameObject.name;
                Color32 color = Color.white;
                Texture remapTex = null;
                bool disableVertexColor = false;
                string matName = "";
                switch (name)
                {
                    case "Flash":
                    case "Sparks":
                        matName = "matEruptionTracerBright";
                        disableVertexColor = true;
                        color = new Color32(199, 39, 0, 213);
                        break;
                    case "Flash Lines, Fire":
                    case "Fire":
                        matName = "matEruptionFire";
                        remapTex = Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampMagmaWorm.png").WaitForCompletion();
                        color = new Color32(134, 51, 45, 255);
                        disableVertexColor = true;
                        break;
                    default:
                        continue;
                }

                if (matName != "")
                {
                    Material mat = UnityEngine.Object.Instantiate(psr.material);
                    psr.material = mat;
                    mat.name = matName;
                    mat.SetColor("_TintColor", color);
                    if(remapTex != null)
                    {
                        mat.SetTexture("_RemapTex", remapTex);
                    }
                    if (disableVertexColor)
                    {
                        mat.DisableKeyword("VERTEXCOLOR");
                        mat.SetFloat("_VertexColorOn", 0);
                    }
                }
            }

            GameObject guh = Addressables.LoadAssetAsync<GameObject>("d9cbb9db8a4992e49b933ab13eea4f9c").WaitForCompletion(); //beetleguardgroundslam.prefab
            GameObject gah = GameObject.Instantiate(guh);
            Decal decal = gah.GetComponentInChildren<Decal>();// decalTransform.GetComponent<Decal>();

            //Decal decal = decalTransform.GetComponent<Decal>();
            if (decal)
            {
                Transform decalTransform = decal.transform;// gah.transform.Find("Decal");
                decalTransform.parent = meteorImpactEffectPrefab.transform;
                decalTransform.localPosition = Vector3.zero;

                Material decalMaterial = new Material(decal.Material/*Addressables.LoadAssetAsync<Material>("RoR2/Base/Beetle/matBeetleGuardSlamDecal.mat").WaitForCompletion()*/);
                decalMaterial.SetColor("_Color", new Color32(205, 74, 0, 143)); //
                decalMaterial.SetTexture("_MaskTex", Addressables.LoadAssetAsync<Texture>("RoR2/Base/CrippleWard/texLunarWardImpactMask.png").WaitForCompletion());
                decalMaterial.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampCloth.jpg").WaitForCompletion());

                decal.Material = decalMaterial;

                AnimateShaderAlpha asa = decalTransform.GetComponent<AnimateShaderAlpha>();
                if (asa)
                {
                    asa.timeMax = 2;
                }
                //decal.Fade = 0.5f;
                //decal.RenderMode = Decal.DecalRenderMode.Deferred;
                //decal.DrawAlbedo = true;
                //decal.DrawNormalAndGloss = true;
                //decal.UseLightProbes = true;
                //decal.HighQualityBlending = false;
            }
            GameObject.Destroy(gah);

            Content.CreateAndAddEffectDef(meteorImpactEffectPrefab);
        }

        public override void Hooks()
        {

        }
    }
}
