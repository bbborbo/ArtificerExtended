using AltArtificerExtended.Passive;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.EntityLogic;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AltArtificerExtended
{
    public partial class Main
    {
        private void CreateLightningSwords()
        {
            AltArtiPassive.lightningProjectile = new GameObject[3];
            for (Int32 i = 0; i < AltArtiPassive.lightningProjectile.Length; i++)
            {
                this.CreateLightningSword(i);
            }
        }

        [Obsolete]
        private void CreateLightningSword(Int32 meshInd)
        {
            GameObject ghost = this.CreateLightningSwordGhost(meshInd);
            GameObject proj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarNeedleProjectile").InstantiateClone("LightningSwordProjectile" + meshInd.ToString(), false);

            UnityEngine.Networking.NetworkIdentity netID = proj.GetComponent<UnityEngine.Networking.NetworkIdentity>();
            netID.localPlayerAuthority = true;


            ProjectileDamage projDamage = proj.GetComponent<ProjectileDamage>();
            projDamage.damage = AltArtiPassive.lightningDamageMult;

            ProjectileController projController = proj.GetComponent<ProjectileController>();
            projController.ghostPrefab = ghost;
            projController.procCoefficient = AltArtiPassive.lightningProcCoef;
            projController.allowPrediction = true;

            ProjectileSimple projSimple = proj.GetComponent<ProjectileSimple>();
            projSimple.enabled = true;
            projSimple.enableVelocityOverLifetime = false;
            projSimple.desiredForwardSpeed = 80f;


            ProjectileDirectionalTargetFinder projTargetFind = proj.GetComponent<ProjectileDirectionalTargetFinder>();
            projTargetFind.enabled = true;
            projTargetFind.lookRange = 150;
            projTargetFind.lookCone = 25;

            ProjectileSteerTowardTarget projSteering = proj.GetComponent<ProjectileSteerTowardTarget>();
            projSteering.enabled = true;
            projSteering.rotationSpeed = 120f;

            ProjectileStickOnImpact projStick = proj.GetComponent<ProjectileStickOnImpact>();
            //projStick.ignoreCharacters = false;
            //projStick.ignoreWorld = false;
            projStick.alignNormals = false;

            ProjectileImpactExplosion projExpl = proj.GetComponent<ProjectileImpactExplosion>();
            projExpl.impactEffect = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/LightningStakeNova");
            projExpl.explosionSoundString = "Play_item_lunar_primaryReplace_impact";
            projExpl.lifetimeExpiredSoundString = "";
            projExpl.offsetForLifetimeExpiredSound = 0f;
            projExpl.destroyOnEnemy = false;
            projExpl.destroyOnWorld = false;
            projExpl.timerAfterImpact = true;
            projExpl.falloffModel = BlastAttack.FalloffModel.None;
            projExpl.lifetime = 10f;
            projExpl.lifetimeAfterImpact = 1f;
            projExpl.lifetimeRandomOffset = 0f;
            projExpl.blastRadius = 1f;
            projExpl.blastDamageCoefficient = AltArtiPassive.lightningBlastDamageMult;
            projExpl.blastProcCoefficient = AltArtiPassive.lightningProcCoef;
            projExpl.bonusBlastForce = Vector3.zero;
            projExpl.fireChildren = false;
            projExpl.childrenProjectilePrefab = null;
            projExpl.childrenCount = 0;
            projExpl.childrenDamageCoefficient = 0f;
            projExpl.minAngleOffset = Vector3.zero;
            projExpl.maxAngleOffset = Vector3.zero;
            projExpl.transformSpace = ProjectileImpactExplosion.TransformSpace.World;
            projExpl.projectileHealthComponent = null;

            ProjectileSingleTargetImpact projStimp = proj.GetComponent<ProjectileSingleTargetImpact>();
            projStimp.destroyOnWorld = false;
            projStimp.hitSoundString = "Play_item_proc_dagger_impact";
            projStimp.enemyHitSoundString = "Play_item_proc_dagger_impact";


            proj.AddComponent<Components.SoundOnAwake>().sound = "Play_item_proc_dagger_spawn";

            //UnityEngine.Object.DestroyImmediate( proj.GetComponent<ProjectileSingleTargetImpact>() );
            UnityEngine.Object.Destroy(proj.GetComponent<AwakeEvent>());
            UnityEngine.Object.Destroy(proj.GetComponent<DelayedEvent>());

            ContentPacks.projectilePrefabs.Add(proj);
            AltArtiPassive.lightningProjectile[meshInd] = proj;
        }

        private void CreateIceExplosion()
        {
            GameObject blast = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GenericDelayBlast").InstantiateClone("IceDelayBlast", false);
            DelayBlast component = blast.GetComponent<DelayBlast>();
            component.crit = false;
            component.procCoefficient = 1.0f;
            component.maxTimer = 0.25f;
            component.falloffModel = BlastAttack.FalloffModel.None;
            component.explosionEffect = this.CreateIceExplosionEffect();
            component.delayEffect = this.CreateIceDelayEffect();
            component.damageType = DamageType.Freeze2s;
            component.baseForce = 250f;

            AltArtiPassive.iceBlast = blast;
            //projectilePrefabs.Add(blast);
        }
    }
}
