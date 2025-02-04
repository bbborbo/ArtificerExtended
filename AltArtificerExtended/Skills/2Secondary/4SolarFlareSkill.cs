using ArtificerExtended.Components;
using ArtificerExtended.States;
using ArtificerExtended.Unlocks;
using BepInEx.Configuration;
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

namespace ArtificerExtended.Skills
{
    class _4SolarFlareSkill : SkillBase<_4SolarFlareSkill>
    {
        public static GameObject projectilePrefab;

        public static float minChargeDuration = 0.2f;
        public static float maxChargeDuration = 2f;
        public static float minSendSpeed = 5;
        public static float maxSendSpeed = 25;

        public static float returnSpeed = 5f;
        public static float returnDelay = 1.5f;
        public static float returnTransitionDelay = 0.5f;

        public static float tornadoHitFrequency = 4;
        public static float tornadoDPS = 1.5f;
        public static float tornadoLifetime = 8;
        public static float tornadoProcCoefficient = 0.5f;
        public static float tornadoRadius = 8;
        public static float blastDamage = 5;
        public static float blastProcCoefficient = 1;
        public static float blastRadius = 14;
        public override string SkillName => "Solar Flare";

        public override string SkillDescription => $"<style=cIsDamage>Ignite</style>. Send out a <style=cIsDamage>roaming</style> solar flare that deals " +
            $"<style=cIsDamage>{Tools.ConvertDecimal(tornadoDPS)}</style> damage over time. " +
            $"Dissipates after <style=cIsUtility>{tornadoLifetime}</style> seconds " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(blastDamage)} damage</style>.";

        public override string SkillLangTokenName => "SOLARFLARE";

        public override UnlockableDef UnlockDef => GetUnlockDef(typeof(KillBlazingWithFireUnlock));

        public override string IconName => "";

        public override MageElement Element => MageElement.Fire;

        public override Type ActivationState => typeof(ChargeSolarFlare);

        public override SkillFamily SkillSlot => ArtificerExtendedPlugin.mageSecondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 8,
                beginSkillCooldownOnSkillEnd: true,
                mustKeyPress: true
            );

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateSkill();
            CreateLang();

            CreateTornadoProjectile();
        }

        private void CreateTornadoProjectile()
        {
            projectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/LunarSkillReplacements/LunarSecondaryProjectile.prefab")
                .WaitForCompletion().InstantiateClone("ArtiSolarFlareProjectile", true);

            GameObject ghostPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ElementalRings/FireTornadoGhost.prefab")
                .WaitForCompletion().InstantiateClone("ArtiSolarFlareGhost");

            if (ghostPrefab)
            {
                GameObject initialBurst = ghostPrefab.transform.Find("InitialBurst")?.gameObject;
                if (initialBurst)
                {
                    GameObject.Destroy(initialBurst);
                }

                Transform embers = ghostPrefab.transform.Find("Embers");
                if (embers)
                {
                    ParticleSystem ps = embers.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule main = ps.main;
                    main.duration = tornadoLifetime;
                }

                Transform tmcw = ghostPrefab.transform.Find("TornadoMeshCore, Wide");
                if (tmcw)
                {
                    ParticleSystem ps = tmcw.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule main = ps.main;
                    main.duration = tornadoLifetime;
                    tmcw.localScale = Vector3.one * tornadoRadius / 14;
                }

                Transform tmc = ghostPrefab.transform.Find("TornadoMeshCore");
                if (tmc)
                {
                    ParticleSystem ps = tmc.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule main = ps.main;
                    main.duration = tornadoLifetime;
                }

                Transform smoke = ghostPrefab.transform.Find("Smoke");
                if (smoke)
                {
                    ParticleSystem ps = smoke.GetComponent<ParticleSystem>();
                    ParticleSystem.MainModule main = ps.main;
                    main.duration = tornadoLifetime;
                }

                ShakeEmitter shakeEmitter = ghostPrefab.GetComponent<ShakeEmitter>();
                if (shakeEmitter)
                {
                    GameObject.Destroy(shakeEmitter);
                }
            }

            ProjectileController pc = projectilePrefab.GetComponent<ProjectileController>();
            if (pc && ghostPrefab)
            {
                pc.ghostPrefab = ghostPrefab;
            }

            ProjectileSimple simple = projectilePrefab.GetComponent<ProjectileSimple>();
            if (simple)
            {
                simple.lifetime = tornadoLifetime + 1;
            }

            ProjectileDamage pd = projectilePrefab.GetComponent<ProjectileDamage>();
            if (pd)
            {
                pd.damageType = DamageType.IgniteOnHit;
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
