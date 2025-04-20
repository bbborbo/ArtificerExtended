using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.Components
{
    [RequireComponent(typeof(ProjectileController))]
    class SolarFlareMissileComponent : MonoBehaviour
    {
        bool shouldFireMissiles = false;
        internal ProjectileController pc;
        internal ProjectileDamage pd;
        CharacterBody ownerBody;
        bool crit;

        public float fireMissileDamageCoefficient = 1;
        public float fireMissileProcCoefficient = 1;
        public float fireMissileBaseInterval = 1;
        float fireMissileInterval;
        float fireMissileTimer = 0;

        public GameObject missilePrefab;

        void Start()
        {
            if (!NetworkServer.active)
                return;

            if(pc == null)
                pc = this.GetComponent<ProjectileController>();
            if (pc && pc.owner)
            {
                if((int)ElementCounter.GetPowerLevelFromBody(pc.owner, RoR2.MageElement.Fire) >= 4)
                {
                    shouldFireMissiles = true;
                    ownerBody = pc.owner.GetComponent<CharacterBody>();
                    ResetMissileTimer();
                    if (pd == null)
                        pd = this.GetComponent<ProjectileDamage>();
                }
            }
        }
        void ResetMissileTimer()
        {
            fireMissileTimer += fireMissileBaseInterval / GetAttackSpeed();
        }
        float GetAttackSpeed()
        {
            if (!ownerBody)
                return 1;
            return ownerBody.attackSpeed;
        }
        void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;
            if (!shouldFireMissiles)
                return;

            int missilesToFire = 0;
            while(fireMissileTimer <= 0)
            {
                ResetMissileTimer();
                missilesToFire++;
            }
            FireSolarFlareMissile(missilesToFire);

            fireMissileTimer -= Time.fixedDeltaTime * GetAttackSpeed();
        }
        public void FireSolarFlareMissile(int count)
        {
            if (!NetworkServer.active)
                return;
            if (count <= 0)
                return;

            Vector3 origin = this.transform.position;

            GameObject target = null;
            BullseyeSearch bullseyeSearch = new BullseyeSearch();
            bullseyeSearch.searchOrigin = origin;
            bullseyeSearch.filterByLoS = false;
            bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
            bullseyeSearch.maxDistanceFilter = 40;
            if (ownerBody.teamComponent)
            {
                bullseyeSearch.teamMaskFilter.RemoveTeam(ownerBody.teamComponent.teamIndex);
            }
            bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
            bullseyeSearch.RefreshCandidates();
            HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();

            for (int i = 0; i < count; i++)
            {
                FireProjectileInfo fpi = new FireProjectileInfo()
                {
                    force = 0,
                    damage = fireMissileDamageCoefficient * ownerBody.damage,
                    crit = pd ? pd.crit : Util.CheckRoll(ownerBody.crit, ownerBody.master),
                    position = origin,
                    projectilePrefab = missilePrefab,
                    target = hurtBox ? hurtBox.gameObject : null,
                    damageColorIndex = DamageColorIndex.Default,
                    owner = ownerBody.gameObject,
                    rotation = Util.QuaternionSafeLookRotation(UnityEngine.Random.insideUnitSphere)
                };
                ProjectileManager.instance.FireProjectile(fpi);
            }
        }
    }
}
