using ArtificerExtended.Components;
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
using ThreeEyedGames;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ArtificerExtended.Skills
{
    class _2LavaBoltsSkill : SkillBase<_2LavaBoltsSkill>
    {
        public static GameObject sloshProjectilePrefab;
        public static GameObject lavaProjectilePrefab => CommonAssets.lavaProjectilePrefab;
        public static GameObject lavaGhostPrefab;
        public static GameObject lavaImpactEffect;

        public static int maxStock = 6;
        public static int rechargeStock = 2;
        public static float rechargeInterval = 2f;
        public static float baseDuration = 1.2f;
        public static float visualScale = 0.4f;

        static float pierceDamageCoefficient = 3.8f;
        static float pierceProcCoefficient = 1.0f;
        public static float damageCoefficient = pierceDamageCoefficient * 0.5f;
        public static float procCoefficient = 0.5f;

        public static float sloshProjectileSize = 4f;
        public static float sloshProjectileSpeed = 25;
        public static float maxDistance = 21;
        public static int totalDrops => Mathf.CeilToInt(maxDistance / (CommonAssets.lavaPoolSize * 2)) + 1;
        public static float delayBetweenDrops => (maxDistance / totalDrops) / sloshProjectileSpeed;
        public static float durationBeforeGravity = maxDistance / sloshProjectileSpeed;
        public override string SkillName => "Lava Bolts";

        public override string SkillDescription => $"<style=cIsDamage>Ignite</style>. Lob a molten projectile for " +
            $"<style=cIsDamage>{Tools.ConvertDecimal(pierceDamageCoefficient)} damage</style>, leaving a trail of " +
            $"<style=cIsDamage>molten pools</style> behind.";

        public override string TOKEN_IDENTIFIER => "LAVABOLTS";

        public override Type RequiredUnlock => typeof(StackBurnUnlock);

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(FireLavaBolt);

        public override SimpleSkillData SkillData => new SimpleSkillData 
        { 
            //useAttackSpeedScaling = true,
            stockToConsume = 0,
            baseMaxStock = 1,
            rechargeStock = 1
        };

        public override Sprite Icon => LoadSpriteFromBundle("LavaBoltIcon");
        public override SkillSlot SkillSlot => SkillSlot.Primary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Any;
        public override Type BaseSkillDef => typeof(SteppedSkillDef);
        public override float BaseCooldown => rechargeInterval;
        public override void Init()
        {
            KeywordTokens = new string[] { CommonAssets.lavaPoolKeywordToken, "KEYWORD_IGNITE" };
            //CreateLavaProjectile();
            base.Init();
        }

        public override void Hooks()
        {
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Junk_Commando.FMJ_prefab).Completed += (ctx) => CreateSloshProjectile(ctx.Result);
        }

        private void CreateSloshProjectile(GameObject orig)
        {
            sloshProjectilePrefab = orig.InstantiateClone("MageLavaSloshProjectile", true);

            sloshProjectilePrefab.layer = LayerIndex.projectileWorldOnly.intVal;
            sloshProjectilePrefab.transform.localScale = Vector3.one * sloshProjectileSize;

            ProjectileController pc = sloshProjectilePrefab.GetComponent<ProjectileController>();
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Commando.FMJRampingGhost_prefab).Completed += (ctx) => 
            {
                GameObject sloshGhost = ctx.Result.InstantiateClone("MageLavaSloshGhost");
                pc.ghostPrefab = sloshGhost;

                sloshGhost.transform.localScale = Vector3.one * sloshProjectileSize * visualScale;
                sloshGhost.transform.localPosition = Vector3.zero;


                ParticleSystemRenderer[] psrs = sloshGhost.GetComponentsInChildren<ParticleSystemRenderer>();
                for (int i = 0; i < psrs.Length; i++)
                {
                    ParticleSystemRenderer psr = psrs[i];
                    string name = psr.gameObject.name;
                    Color32 color = Color.white;
                    string matName = "";
                    string rampGuid = "";
                    if (name == "Distortion" || name == "BurstVFX")
                    {
                        psr.gameObject.SetActive(false);
                        psr.enabled = false;
                        continue;
                    }
                    if (name == "Flames")
                    {
                        matName = "matLavaSloshFlames";
                        rampGuid = RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_ColorRamps.texRampParentFire_png;
                        color = new Color32(195, 179, 0, 190);
                    }
                    else if (name == "Trail")
                    {
                        matName = "matLavaSloshTrail";
                        color = new Color32(217, 58, 0, 255);
                    }
                    else if (name == "Core")
                    {
                        matName = "matLavaSloshCore";
                        color = new Color32(113, 9, 0, 255);
                    }

                    if (matName != "")
                    {
                        Material mat = UnityEngine.Object.Instantiate(psr.material);
                        psr.material = mat;
                        mat.name = matName;
                        if(rampGuid != "")
                        {
                            Addressables.LoadAssetAsync<Texture>(rampGuid).Completed += ctx => mat.SetTexture("_RemapTex", ctx.Result);
                        }
                        mat.DisableKeyword("VERTEXCOLOR");
                        mat.SetFloat("_VertexColorOn", 0);
                        mat.SetColor("_TintColor", color);
                    }
                }

                Light light = sloshGhost.GetComponentInChildren<Light>();
                if(light)
                {
                    light.color = new Color32(79, 46, 0, 255);
                }

                Content.CreateAndAddEffectDef(sloshGhost);
            };

            ProjectileMageFirewallWalkerController walkerController = sloshProjectilePrefab.AddComponent<ProjectileMageFirewallWalkerController>();
            walkerController.dropInterval = delayBetweenDrops;
            walkerController.firePillarPrefab = CommonAssets.lavaProjectilePrefab;
            walkerController.totalProjectiles = totalDrops;

            ProjectileOverlapAttack overlapAttack = sloshProjectilePrefab.GetComponent<ProjectileOverlapAttack>();
            if (overlapAttack)
            {
                overlapAttack.damageCoefficient = damageCoefficient > 0 ? pierceDamageCoefficient / damageCoefficient : pierceDamageCoefficient;
                overlapAttack.overlapProcCoefficient = procCoefficient > 0 ? pierceProcCoefficient / procCoefficient : procCoefficient;
            }

            //pierce hitbox
            HitBox hitBox = sloshProjectilePrefab.GetComponentInChildren<HitBox>();
            if (hitBox)
            {
                hitBox.transform.localScale = Vector3.one;
                hitBox.transform.localPosition = Vector3.zero;
            }

            //world impact hitbox
            SphereCollider collider = sloshProjectilePrefab.GetComponent<SphereCollider>();
            if (collider != null)
            {
                collider.radius = 0.2f;
                collider.isTrigger = false;
            }

            ProjectileImpactExplosion pie = sloshProjectilePrefab.AddComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.childrenProjectilePrefab = CommonAssets.lavaPoolPrefab;
                pie.fireChildren = true;
                pie.childrenCount = 1;
                pie.useChildRotation = true;
                pie.impactEffect = CommonAssets.lavaImpactEffect;
                pie.destroyOnEnemy = false;
                pie.destroyOnWorld = true;
                pie.lifetime = 10;
            }

            ProjectileSimple ps = sloshProjectilePrefab.GetComponent<ProjectileSimple>();
            ps.desiredForwardSpeed = sloshProjectileSpeed;

            Rigidbody rb = sloshProjectilePrefab.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.useGravity = true;
                AntiGravityForce agf = sloshProjectilePrefab.AddComponent<AntiGravityForce>();
                agf.rb = rb;
                GravityAfterDuration antiGrav = sloshProjectilePrefab.AddComponent<GravityAfterDuration>();
                antiGrav.antiGrav = agf;
                antiGrav.antiGravCoefficient = -0.5f;
                antiGrav.durationBeforeGravity = durationBeforeGravity;
            }

            Content.AddProjectilePrefab(sloshProjectilePrefab);
        }

        private void CreateLavaProjectile()
        {
            //lavaProjectilePrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/beetlequeenspit").InstantiateClone("LavaProjectile", true);

            Color napalmColor = new Color32(255, 40, 0, 255);

            GameObject ghostPrefab = lavaProjectilePrefab.GetComponent<ProjectileController>().ghostPrefab;
            lavaGhostPrefab = ghostPrefab.InstantiateClone("NapalmSpitGhost", false);
            Tools.GetParticle(lavaGhostPrefab, "SpitCore", napalmColor);

            lavaImpactEffect = RoR2.LegacyResourcesAPI.Load<GameObject>("e184c0c8bc862ff40b9fd07db0b8e98c").InstantiateClone("NapalmSpitExplosion", false); //beetlespitexplosion
            Tools.GetParticle(lavaImpactEffect, "Bugs", Color.clear);
            Tools.GetParticle(lavaImpactEffect, "Flames", napalmColor);
            Tools.GetParticle(lavaImpactEffect, "Flash", Color.yellow);
            Tools.GetParticle(lavaImpactEffect, "Distortion", napalmColor);
            Tools.GetParticle(lavaImpactEffect, "Ring, Mesh", Color.yellow);

            ProjectileImpactExplosion pieNapalm = lavaProjectilePrefab.GetComponent<ProjectileImpactExplosion>();
            if (pieNapalm && CommonAssets.lavaPoolPrefab != null)
            {
                pieNapalm.childrenProjectilePrefab = CommonAssets.lavaPoolPrefab;
                pieNapalm.impactEffect = lavaImpactEffect;
                pieNapalm.blastRadius = CommonAssets.lavaPoolSize;
                //projectilePrefabNapalm.GetComponent<ProjectileImpactExplosion>().destroyOnEnemy = true;
                pieNapalm.blastProcCoefficient = procCoefficient;
                pieNapalm.bonusBlastForce = new Vector3(0, 500, 0);
            }

            ProjectileController pc = lavaProjectilePrefab.GetComponent<ProjectileController>();
            pc.ghostPrefab = lavaGhostPrefab;

            ProjectileDamage pd = lavaProjectilePrefab.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.IgniteOnHit;

            Content.CreateAndAddEffectDef(lavaImpactEffect);
            Content.AddProjectilePrefab(lavaProjectilePrefab);
        }
    }
}
