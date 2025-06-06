using ArtificerExtended.Skills;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.Components
{
    [RequireComponent(typeof(ProjectileController))]
    class SnowglobeDeployProjectile : MonoBehaviour, IProjectileImpactBehavior
    {
        internal ProjectileController pc;
        static DeployableSlot deployableSlot => _3SnowglobeSkill.snowglobeDeployableSlot;
        public GameObject snowglobeProjectilePrefab;
        private bool isAlive = true;
        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            if (this.isAlive && snowglobeProjectilePrefab != null)
            {
                this.SpawnSnowglobe(impactInfo);
                this.isAlive = false;
            }
        }

        private void SpawnSnowglobe(ProjectileImpactInfo impactInfo)
        {
            if (!NetworkServer.active)
                return;

            if(pc == null)
                pc = base.GetComponent<ProjectileController>();
            if (!pc || !pc.owner)
                return;

            CharacterBody ownerBody = pc.owner.GetComponent<CharacterBody>();
            if (!ownerBody )
                return;
            CharacterMaster ownerMaster = ownerBody.master;
            if (!ownerMaster /*|| characterMaster.IsDeployableLimited(deployableSlot)*/)
                return;

            ProjectileDamage projectileDamage = base.GetComponent<ProjectileDamage>();
            if (!projectileDamage)
                return;

            GameObject snowglobeInstance = UnityEngine.Object.Instantiate<GameObject>(snowglobeProjectilePrefab, impactInfo.estimatedPointOfImpact, Quaternion.identity);
            snowglobeInstance.GetComponent<TeamFilter>().teamIndex = ownerBody.teamComponent.teamIndex;
            snowglobeInstance.GetComponent<GenericOwnership>().ownerObject = ownerBody.gameObject;
            Deployable deployableComponent = snowglobeInstance.GetComponent<Deployable>();
            if (deployableComponent && ownerMaster)
            {
                deployableComponent.onUndeploy.AddListener(deployableComponent.DestroyGameObject);
                ownerMaster.AddDeployable(deployableComponent, deployableSlot);
            }
            ProjectileDamage component2 = snowglobeInstance.GetComponent<ProjectileDamage>();
            if (component2)
            {
                component2.crit = projectileDamage.crit;
                component2.damage = projectileDamage.damage;
                component2.damageColorIndex = DamageColorIndex.Default;
                component2.force = 0;
                component2.damageType = DamageType.Generic;
                component2.damageType.damageSource = DamageSource.Secondary;
            }
            NetworkServer.Spawn(snowglobeInstance);
        }
    }
}
