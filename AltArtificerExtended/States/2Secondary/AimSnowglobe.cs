using ArtificerExtended.Skills;
using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtificerExtended.States
{
    class AimSnowglobe : AimThrowableBase
    {
        public override void OnEnter()
        {
            base.damageCoefficient = _3SnowglobeSkill.impactDamageCoefficient;
            base.projectileBaseSpeed = _3SnowglobeSkill.projectileBaseSpeed;
            base.detonationRadius = _3SnowglobeSkill.snowWardRadius;
            base.projectilePrefab = _3SnowglobeSkill.snowglobeDeployProjectilePrefab;
            base.baseMinimumDuration = 0.4f;
            base.maxDistance = 100;
            base.setFuse = true;
            base.arcVisualizerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/BasicThrowableVisualizer.prefab").WaitForCompletion();
            base.endpointVisualizerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Treebot/TreebotMortarAreaIndicator.prefab").WaitForCompletion();
            base.OnEnter();
        }
    }
}
