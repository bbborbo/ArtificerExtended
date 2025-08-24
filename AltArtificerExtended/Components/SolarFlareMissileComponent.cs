using ArtificerExtended.Skills;
using RoR2;
using RoR2.Orbs;
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
        float fireMissileTimer = 0;
        public float resetTargetsBaseInterval = 1;
        float resetTargetsTimer = 0;

        public GameObject missilePrefab;

        void Start()
        {
            if (!NetworkServer.active)
                return;

            this.previousTargets = new List<HealthComponent>();
            this.search = new BullseyeSearch();
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

            fireMissileTimer -= Time.fixedDeltaTime;
        }
        public void FireSolarFlareMissile(int count)
        {
            if (!NetworkServer.active)
                return;
            if (!shouldFireMissiles)
                return;
            if (count <= 0)
                return;

            Vector3 origin = this.transform.position;
            Vector3 forward = UnityEngine.Random.insideUnitSphere;

            for (int i = 0; i < count; i++)
            {
                this.previousTargets.Clear();
                HurtBox hurtBox = this.FindNextTarget(origin, forward);
                if (hurtBox)
                {
                    this.previousTargets.Add(hurtBox.healthComponent);
                    LightningOrb lightningOrb = new LightningOrb();
                    lightningOrb.bouncedObjects = new List<HealthComponent>();
                    lightningOrb.attacker = ownerBody.gameObject;
                    lightningOrb.inflictor = base.gameObject;
                    lightningOrb.teamIndex = ownerBody.teamComponent.teamIndex;
                    lightningOrb.damageValue = fireMissileDamageCoefficient * ownerBody.damage;
                    lightningOrb.isCrit = pd ? pd.crit : Util.CheckRoll(ownerBody.crit, ownerBody.master);
                    lightningOrb.origin = origin;
                    lightningOrb.bouncesRemaining = 0;
                    lightningOrb.lightningType = LightningOrb.LightningType.BFG;
                    lightningOrb.procCoefficient = _4SolarFlareSkill.missileProcCoefficient;
                    lightningOrb.target = hurtBox;
                    lightningOrb.damageColorIndex = DamageColorIndex.Default;
                    lightningOrb.damageType = new DamageTypeCombo(DamageType.IgniteOnHit, DamageTypeExtended.Generic, DamageSource.Secondary);
                    OrbManager.instance.AddOrb(lightningOrb);
                }
            }
        }

        List<HealthComponent> previousTargets = new List<HealthComponent>();
        private BullseyeSearch search;
        public HurtBox FindNextTarget(Vector3 position, Vector3 forward)
        {
            if (this.search == null)
                return null;
            this.search.searchOrigin = position;
            this.search.searchDirection = forward;
            this.search.sortMode = BullseyeSearch.SortMode.Distance;
            this.search.teamMaskFilter = TeamMask.allButNeutral;
            if (ownerBody.teamComponent)
            {
                this.search.teamMaskFilter.RemoveTeam(ownerBody.teamComponent.teamIndex);
            }
            this.search.filterByLoS = false;
            this.search.maxDistanceFilter = 40;
            this.search.RefreshCandidates();

            HurtBox result = this.search.GetResults().FirstOrDefault((HurtBox hurtBox) => !this.previousTargets.Contains(hurtBox.healthComponent));
            if (!result)
                result = this.search.GetResults().FirstOrDefault();
            return result;
        }
    }
}
