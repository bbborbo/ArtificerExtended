using ArtificerExtended.Components;
using ArtificerExtended.Modules;
using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
using EntityStates;
using EntityStates.Mage.Weapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static ArtificerExtended.Modules.Language.Styling;

namespace ArtificerExtended.Skills
{
    class _4SolarFlareSkill : SkillBase<_4SolarFlareSkill>
    {
        public static GameObject projectilePrefab;
        public static GameObject missileProjectilePrefab;

        public static float minChargeDuration = 0.2f;
        public static float maxChargeDuration = 2f;
        public static float minSendSpeed = 5;
        public static float maxSendSpeed = 25;

        public static float returnSpeed = 5f;
        public static float returnDelay = 1.5f;
        public static float returnTransitionDelay = 0.5f;

        public static float tornadoHitFrequency = 3;
        public static float tornadoDPS = 2f;
        public static float tornadoLifetime = 7;
        public static float tornadoProcCoefficient = 0.7f;
        public static float tornadoRadius = 8;
        public static float blastDamage = 8;
        public static float blastProcCoefficient = 1;
        public static float blastRadius = 14;
        public static float missileDamageCoefficient = 0.8f;
        public static float missileProcCoefficient = 0.5f;
        public static float missileFireInterval = 0.5f;
        public override string SkillName => "Solar Flare";

        public override string SkillDescription => $"<style=cIsUtility>Resonant</style>. " +
            $"<style=cIsDamage>Ignite</style>. Send out a <style=cIsDamage>movement tracking</style> solar flare that deals " +
            $"<style=cIsDamage>{Tools.ConvertDecimal(tornadoDPS)}</style> damage over time. " +
            $"Dissipates after <style=cIsUtility>{tornadoLifetime}</style> seconds " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(blastDamage)} damage</style>.";

        public override string TOKEN_IDENTIFIER => "STAR";

        public override Type RequiredUnlock => (typeof(KillBlazingWithFireUnlock));

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(ChargeSolarFlare);

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                beginSkillCooldownOnSkillEnd: true,
                mustKeyPress: true
            );
        public override Sprite Icon => LoadSpriteFromBundle("solarflareAE");
        public override SkillSlot SkillSlot => SkillSlot.Secondary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;
        public override Type BaseSkillDef => typeof(SkillDef);
        public override float BaseCooldown => 8;
        public override void Init()
        {
            string resonantKeywordToken = ArtificerExtendedPlugin.DEVELOPER_PREFIX + "KEYWORD_RESONANTSTAR";
            CommonAssets.AddResonantKeyword(resonantKeywordToken, "Coronal Mass Ejection",
                $"If only <style=cIsDamage>Fire</style> abilities are equipped, periodically launches superheated plasma for {DamageValueText(missileDamageCoefficient)}.");
            CreateMissileProjectile();
            CreateTornadoProjectile();
            KeywordTokens = new string[] { resonantKeywordToken, "KEYWORD_IGNITE" };
            base.Init();
        }

        private void CreateMissileProjectile()
        {
            missileProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/MissileProjectile.prefab").WaitForCompletion().InstantiateClone("MageMassEjectionMissile", true);
            ProjectileDamage pd = missileProjectilePrefab.GetComponent<ProjectileDamage>();
            if (pd)
            {
                pd.damageType.damageType = DamageType.IgniteOnHit;
                pd.damageType.damageSource = DamageSource.NoneSpecified;
            }
            
            Content.AddProjectilePrefab(missileProjectilePrefab);
        }

        public override void Hooks()
        {

        }

        private void CreateTornadoProjectile()
        {
            projectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarSkillReplacements/LunarSecondaryProjectile.prefab")
                .WaitForCompletion().InstantiateClone("ArtiSolarFlareProjectile", true);
            Content.AddProjectilePrefab(projectilePrefab);

            GameObject ghostPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Mage.MageFireboltGhost_prefab)
                .WaitForCompletion().InstantiateClone("ArtiSolarFlareGhost");

            if (ghostPrefab)
            {

                //FindAndDestroy(ghostPrefab, "InitialBurst");//, "Embers", "TornadoMeshCore, Wide", "TornadoMeshCore", "Smoke" });
                //FindAndDestroy(ghostPrefab, "Embers");//, "Embers", "TornadoMeshCore, Wide", "TornadoMeshCore", "Smoke" });
                //FindAndDestroy(ghostPrefab, "TornadoMeshCore");//, "Embers", "TornadoMeshCore, Wide", "TornadoMeshCore", "Smoke" });
                //FindAndDestroy(ghostPrefab, "TornadoMeshCore, Wide");//, "Embers", "TornadoMeshCore, Wide", "TornadoMeshCore", "Smoke" });
                //FindAndDestroy(ghostPrefab, "Smoke");//, "Embers", "TornadoMeshCore, Wide", "TornadoMeshCore", "Smoke" });
                void FindAndDestroy(Transform gameObject, string name)
                {
                    Transform t = gameObject.Find(name);
                    if (t && t.gameObject != gameObject)
                    {
                        UnityEngine.Object.Destroy(t.gameObject);
                    }
                    else
                    {
                        Debug.LogError($"Could not find transform [{name}] to destroy");
                        
                    }
                }

                //ShakeEmitter shakeEmitter = ghostPrefab.GetComponent<ShakeEmitter>();
                //if (shakeEmitter.gameObject)
                //{
                //    GameObject.Destroy(shakeEmitter.gameObject);
                //}

                GameObject gpaSun = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Grandparent.GrandParentSun_prefab).WaitForCompletion();
                Transform vfxRoot = gpaSun.transform.Find("VfxRoot");
                if (vfxRoot)
                {
                    GameObject sunEffect = vfxRoot.gameObject.InstantiateClone("ArtiSolarFlareSunEffect");
                    sunEffect.transform.parent = ghostPrefab.transform;
                    sunEffect.transform.localPosition = Vector3.zero;

                    FindAndDestroy(sunEffect.transform.Find("Mesh").Find("SunMesh"), "MoonMesh");
                    FindAndDestroy(sunEffect.transform.Find("Particles"), "Sparks");
                    FindAndDestroy(sunEffect.transform.Find("Particles"), "Goo, Drip");
                    FindAndDestroy(sunEffect.transform.Find("Particles"), "HeatDistortionEmitter");
                    FindAndDestroy(sunEffect.transform, "PP");
                    FindAndDestroy(sunEffect.transform, "LightSpinner");
                    FindAndDestroy(sunEffect.transform.Find("Mesh"), "AreaIndicator");
                    FindAndResize(sunEffect, "Mesh", Vector3.one * 0.4f);
                    FindAndResize(sunEffect, "Particles", Vector3.one * 2.5f);
                    void FindAndResize(GameObject gameObject, string name, Vector3 size)
                    {
                        Transform t = gameObject.transform.Find(name);
                        if (t)
                        {
                            t.localScale = size;
                        }
                        else
                        {
                            Debug.LogError($"Could not find transform [{name}] to resize");
                        }
                    }

                    Transform sunMesh = sunEffect.transform.Find("Mesh").Find("SunMesh");
                    if (sunMesh)
                    {
                        MeshRenderer renderer = sunMesh.GetComponent<MeshRenderer>();
                        if (renderer)
                        {
                            Material mat = UnityEngine.Object.Instantiate(renderer.material);
                            mat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>("RoR2/DLC2/Scorchling/texRampScorchling.png").WaitForCompletion());
                            renderer.material = mat;
                        }
                    }
                }

                GameObject warbannerWard = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/WardOnLevel/WarbannerWard.prefab").WaitForCompletion();
                GameObject gah = GameObject.Instantiate(warbannerWard);

                MeshRenderer[] mrs = gah.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer meshRenderer in mrs)
                {
                    if (meshRenderer.gameObject.name == "IndicatorSphere")
                    {
                        meshRenderer.material = CommonAssets.matMageFlameAura;
                        meshRenderer.transform.parent = ghostPrefab.transform;
                        meshRenderer.transform.localScale = Vector3.one * tornadoRadius * 2;
                        meshRenderer.transform.localPosition = Vector3.zero;

                        Material[] sm = meshRenderer.sharedMaterials;
                        Array.Resize(ref sm, 2);
                        sm[1] = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/GameModes/InfiniteTowerRun/InfiniteTowerAssets/matITSafeWardAreaIndicator2.mat").WaitForCompletion();
                        meshRenderer.sharedMaterials = sm;
                        break;
                    }
                }
                Transform.Destroy(gah);
            }


            ProjectileDamage pd = projectilePrefab.GetComponent<ProjectileDamage>();
            if (pd)
            {
                pd.damageType = DamageType.IgniteOnHit;
            }

            ProjectileController pc = projectilePrefab.GetComponent<ProjectileController>();
            if (pc)
            {
                if (ghostPrefab)
                {
                    pc.ghostPrefab = ghostPrefab;
                }

                SolarFlareMissileComponent missileComponent = projectilePrefab.AddComponent<SolarFlareMissileComponent>();
                missileComponent.pc = pc;
                missileComponent.fireMissileBaseInterval = missileFireInterval;
                missileComponent.fireMissileDamageCoefficient = missileDamageCoefficient;
                missileComponent.fireMissileProcCoefficient = missileProcCoefficient;
                missileComponent.missilePrefab = missileProjectilePrefab;
                if (pd)
                    missileComponent.pd = pd;
            }

            ProjectileSimple simple = projectilePrefab.GetComponent<ProjectileSimple>();
            if (simple)
            {
                simple.lifetime = tornadoLifetime + 1;
            }

            ProjectileFuse fuse = projectilePrefab.GetComponent<ProjectileFuse>();
            if (fuse)
            {
                fuse.fuse = tornadoLifetime;
            }

            ProjectileDotZone pdz = projectilePrefab.GetComponent<ProjectileDotZone>();
            if (pdz)
            {
                pdz.damageCoefficient = tornadoDPS / (blastDamage * tornadoHitFrequency);
                pdz.resetFrequency = tornadoHitFrequency;
                pdz.lifetime = tornadoLifetime;
                pdz.overlapProcCoefficient = tornadoProcCoefficient;
                pdz.impactEffect = Flamethrower.impactEffectPrefab;
            }

            ProjectileExplosion pe = projectilePrefab.GetComponent<ProjectileExplosion>();
            if (pe)
            {
                pe.blastDamageCoefficient = blastDamage;
                pe.blastProcCoefficient = blastProcCoefficient;
                pe.falloffModel = BlastAttack.FalloffModel.SweetSpot;
                //pe.explosionEffect = ;
                //pe.explosionSoundString = ;
            }

            Rigidbody rb = projectilePrefab.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.drag = 0;
            }

            ProjectileRecallToOwner boomerang = projectilePrefab.AddComponent<ProjectileRecallToOwner>();
            if (boomerang)
            {
                boomerang.returnSpeed = returnSpeed;
                boomerang.delay = returnDelay;
                boomerang.transitionDuration = returnTransitionDelay;
            }

            HitBox[] hitboxes = projectilePrefab.GetComponentsInChildren<HitBox>();
            foreach(HitBox hb in hitboxes)
            {
                hb.transform.localScale = Vector3.one * tornadoRadius;
            }
        }
    }
}
