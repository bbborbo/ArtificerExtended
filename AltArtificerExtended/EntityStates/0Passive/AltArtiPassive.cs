using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using EntityStates;

using RoR2;
using RoR2.Projectile;

using UnityEngine;
using UnityEngine.Networking;
using AltArtificerExtended.Components;
using static AltArtificerExtended.Components.ElementCounter;

namespace AltArtificerExtended.Passive
{

    public class AltArtiPassive : BaseState
    {
        #region External Consts
        //External
        public const Single nanoBombInterval = 0.5f;
        private const Single nanoBombMinDelay = 0.3f;
        private const Single nanoBombMaxDelay = 0.7f;
        private const Single prepWallMaxDelay = 0.3f;
        private const Single prepWallMinDelay = 0.7f;
        private const Single flamethrowerInterval = 0.5f;
        private const Int32 nanoBombMaxPerTick = 10;
        private const Int32 flamethrowerMaxPerTick = 10;
        #endregion

        #region Internal Consts
        private const Single nodeYOffset = 1.6f;
        private const Single nodeArcFrac = 0.6f;
        private const Single nodeMinRadius = 1.15f;
        private const Single nodeMaxRadius = 1.6f;
        private const Single nodeFireRadius = 0.25f;
        private const Single nodeFireRate = 0.65f;
        private const Single nodeFireMin = -0.05f;
        private const Single nodeFireMax = 0.05f;
        private const Int32 nodesToCreate = 16;


        #endregion


        #region Public Statics
        public static Single lightningDamageMult = 0.3f;
        public static Single lightningBlastDamageMult = 0.7f;
        public static Single lightningForce = 100f;
        public static Single lightningProcCoef = 0.3f;

        public static Single burnDamageMult = 0.075f;
        public static Single burnBuffDuration = 2f;

        public static Single slowProcChance = 1f;
        public static Single freezeProcCount = 3f;
        public static Single chillProcDuration = 8f;

        public const int novaDebuffThreshold = 10;
        public static Single novaMaxRadius = 8f;
        public static Single novaMinRadius = 3f;

        public static Single targetUpdateFreq = 10f;
        public static Single targetRange = 60f;
        public static Single targetAng = 40f;

        public static BuffIndex fireBuff;

        public static GameObject[] lightningProjectile;
        public static GameObject[] lightningPreFireEffect;

        public static GameObject iceBlast;

        public static Dictionary<GameObject, AltArtiPassive> instanceLookup = new Dictionary<GameObject, AltArtiPassive>();
        #endregion

        #region Private Statics

        #endregion


        #region Public Vars
        public CharacterBody ext_characterBody
        {
            get => base.characterBody;
        }
        public Single ext_attackSpeedStat
        {
            get => base.characterBody.attackSpeed;
        }
        public Single ext_nanoBombInterval
        {
            get => nanoBombInterval;
        }
        public Single ext_nanoBombMinDelay
        {
            get => nanoBombMinDelay;
        }
        public Single ext_nanoBombMaxDelay
        {
            get => nanoBombMaxDelay;
        }
        public Single ext_prepWallMinDelay
        {
            get => prepWallMinDelay;
        }
        public Single ext_prepWallMaxDelay
        {
            get => prepWallMaxDelay;
        }
        public Single ext_flamethrowerInterval
        {
            get => flamethrowerInterval;
        }
        public Int32 ext_nanoBombMaxPerTick
        {
            get => nanoBombMaxPerTick;
        }
        public Int32 ext_flamethrowerMaxPerTick
        {
            get => flamethrowerMaxPerTick;
        }
        #endregion

        #region Private Vars
        private Single searchTimer = 0f;

        public ElementCounter elementPower;

        private Helpers.InstancedRandom random;

        private Transform modelTransform;

        private HurtBox target;

        private readonly List<ProjectileNode> projNodes = new List<ProjectileNode>();
        private readonly BullseyeSearch search = new BullseyeSearch();
        #endregion


        #region Public Typedefs
        public class BatchHandle
        {
            public List<ProjectileData> handledProjectiles = new List<ProjectileData>();

            public void Fire(Single minDelay, Single maxDelay)
            {
                foreach (ProjectileData proj in this.handledProjectiles)
                {
                    proj.triggered = true;
                    proj.timerMin = minDelay;
                    proj.timerMax = maxDelay;
                }
            }
        }
        #endregion

        #region Private Typedefs

        public class ProjectileData
        {
            public Boolean isTriggered;
            public Boolean triggered;
            public Boolean timerAssigned;
            public Boolean radiusAssigned;
            public Single timer;
            public Single timerMin = nodeFireMin;
            public Single timerMax = nodeFireMax;
            public Vector3 localPos;
            public Int32 type;
            public Single rotation;
            public BatchHandle handle;

            private readonly Helpers.InstancedRandom random;

            public ProjectileData(Helpers.InstancedRandom random, BatchHandle handle = null)
            {
                this.random = random;
                this.type = Mathf.FloorToInt(this.random.Range(0f, 2f));
                this.rotation = this.random.Range(0f, 360f);
                this.localPos = this.random.InsideUnitSphere();
                this.timerAssigned = true;

                if (handle != null)
                {
                    this.isTriggered = true;
                    this.triggered = false;
                    handle.handledProjectiles.Add(this);
                    this.handle = handle;
                    this.timerAssigned = false;
                }
            }

            public void AssignTimer()
            {
                if (this.timerAssigned)
                {
                    return;
                }

                this.timer = this.random.Range(this.timerMin, this.timerMax);
                this.timerAssigned = true;
            }

            public void AssignRadius(Single radius)
            {
                if (this.radiusAssigned)
                {
                    return;
                }

                this.localPos *= radius;
                this.radiusAssigned = true;
            }
        }

        private class ProjectileNode
        {
            public Transform location;
            public List<ProjectileData> queue;
            public Single fireTime;
            public Single fireRadius;

            private Single timer = 0f;



            public ProjectileData nextProj;

            private GameObject effect;

            private readonly AltArtiPassive passive;

            public ProjectileNode(Vector3 position, Transform parent, AltArtiPassive passive)
            {
                this.location = new GameObject("ProjNode").transform;
                this.location.parent = parent;
                this.location.localPosition = position;
                this.location.localRotation = Quaternion.identity;
                this.location.localScale = Vector3.one;

                this.queue = new List<ProjectileData>();

                this.fireRadius = nodeFireRadius;
                this.fireTime = nodeFireRate;
                this.passive = passive;
            }

            public void AddToQueue(ProjectileData data) => this.queue.Add(data);

            public void AddImmediate(ProjectileData data)
            {
                if (this.nextProj != null)
                {
                    this.queue.Insert(0, this.nextProj);
                }

                this.nextProj = data;
                this.nextProj.AssignRadius(this.fireRadius);
                this.CreateEffect(this.nextProj);
            }

            public void UpdateNode(Single deltaT, HurtBox target, Vector3 direction)
            {
                if (this.nextProj == null)
                {
                    this.nextProj = this.TryGetNextProj();
                }

                if (this.nextProj == null)
                {
                    this.timer = 0f;
                    return;
                }

                if (this.effect)
                {
                    this.effect.transform.rotation = Quaternion.AngleAxis(this.nextProj.rotation, direction) * Util.QuaternionSafeLookRotation(direction);
                }

                if (this.nextProj.isTriggered)
                {
                    if (this.nextProj.triggered)
                    {
                        this.nextProj.AssignTimer();
                        this.timer += deltaT;
                        if (this.timer >= this.nextProj.timer)
                        {
                            this.Fire(target);
                        }
                    }
                }
                else
                {
                    this.timer += deltaT;
                    if (this.timer >= this.fireTime + this.nextProj.timer)
                    {
                        this.Fire(target);
                    }
                }
            }

            private ProjectileData TryGetNextProj()
            {
                if (this.queue.Count <= 0)
                {
                    return null;
                }

                ProjectileData temp = this.queue[0];
                this.queue.RemoveAt(0);

                if (temp != null)
                {
                    temp.AssignTimer();
                    temp.AssignRadius(this.fireRadius);
                    this.CreateEffect(temp);
                }


                return temp;
            }

            private void Fire(HurtBox target)
            {
                this.timer = 0f;
                if (this.effect == null)
                {
                    this.CreateEffect(this.nextProj);
                }

                if (this.passive.isAuthority)
                {
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        crit = this.passive.characterBody.RollCrit(),
                        damage = this.passive.characterBody.damage * AltArtiPassive.lightningDamageMult,
                        damageColorIndex = DamageColorIndex.Default,
                        force = AltArtiPassive.lightningForce,
                        owner = this.passive.gameObject,
                        position = this.effect.transform.position,
                        procChainMask = default,
                        projectilePrefab = AltArtiPassive.lightningProjectile[this.nextProj.type],
                        rotation = this.effect.transform.rotation,
                        target = target?.gameObject
                    });
                }
                UnityEngine.Object.Destroy(this.effect);

                this.nextProj = null;
            }

            private void CreateEffect(ProjectileData proj)
            {
                if (this.effect != null)
                {
                    UnityEngine.Object.Destroy(this.effect);
                }
                this.effect = UnityEngine.Object.Instantiate(AltArtiPassive.lightningPreFireEffect[proj.type], this.location);
                this.effect.transform.localScale = Vector3.one;
                this.effect.transform.localPosition = proj.localPos;
                this.effect.transform.localRotation = Quaternion.identity;
            }
        }
        #endregion


        #region External Methods
        public void SkillCast(BatchHandle handle = null)
        {
            if(elementPower == null)
            {
                Debug.Log("Energetic Resonance passive has no element counter component!");
                return;
            }
            this.DoLightning(elementPower.lightningPower, handle);
            this.DoFire(elementPower.firePower);
            this.DoIce(elementPower.icePower);
        }

        public static void DoNova(CharacterBody attacker, Power currentPower, Vector3 position, int strength = novaDebuffThreshold)
        {
            if (attacker == null || currentPower == Power.None || strength == 0)
                return;

            float radiusByPower = 1 + (1 * (int)currentPower);
            float radiusByBuffs = Util.Remap((float)strength, 0, novaDebuffThreshold, novaMinRadius, novaMaxRadius);
            CreateIceBlast(attacker, currentPower, position, radiusByPower + radiusByBuffs);
        }
        #endregion

        #region Internal Methods
        private void DoLightning(Power power, BatchHandle handle)
        {
            for (Int32 i = 0; i < (Int32)power; i++)
            {
                var proj = new ProjectileData(this.random, handle);

                this.AddProjectileToRandomNode(proj, handle != null);
            }
        }

        private void DoFire(Power power)
        {
            if (NetworkServer.active)
            {
                for (Int32 i = 0; i < (Int32)power; i++)
                {
                    base.characterBody.AddTimedBuff(Main.meltBuff, burnBuffDuration + 0.1f * i);
                }
            }
        }

        private void DoIce(Power power)
        {
            /*if (NetworkServer.active)
            {
                BuffIndex ice = Main.chillBuff.buffIndex;
                for (Int32 i = 0; i < (Int32)power; i++)
                {
                    //base.characterBody.AddTimedBuff(ice, iceBuffDuration);
                }
            }*/
        }

        public static void CreateIceBlast(CharacterBody attacker, Power icePowerToUse, Vector3 position, Single radius)
        {
            if (NetworkServer.active)
            {
                GameObject blast = UnityEngine.Object.Instantiate<GameObject>(iceBlast, position, Quaternion.identity);
                blast.transform.localScale = new Vector3(radius, radius, radius);
                DelayBlast delay = blast.GetComponent<DelayBlast>();
                delay.maxTimer += UnityEngine.Random.Range(-0.1f, 0.1f);
                delay.position = position;
                delay.baseDamage = attacker.damage * (1 + 0.5f * (int)icePowerToUse);
                delay.procCoefficient = 0.3f + (0.1f * (int)icePowerToUse);
                delay.attacker = attacker.gameObject;
                delay.radius = radius;
                blast.GetComponent<TeamFilter>().teamIndex = attacker.teamComponent.teamIndex;
            }
        }


        private void GenerateNodes()
        {
            for (Int32 i = 0; i < nodesToCreate; i++)
            {
                Single randRot = this.random.Value();
                Single randRad = this.random.Value();
                Single radius = nodeMinRadius + (randRad * (nodeMaxRadius - nodeMinRadius));
                Single ang = randRot - 0.5f;
                ang *= Mathf.PI * 2f;
                ang *= nodeArcFrac;
                Single x = Mathf.Sin(ang);
                Single y = Mathf.Cos(ang);
                var localPos = new Vector3(x, y, 0f);
                localPos = Vector3.Normalize(localPos);
                localPos *= radius;
                localPos += new Vector3(0f, nodeYOffset, 0f);

                this.projNodes.Add(new ProjectileNode(localPos, this.modelTransform, this));
            }
        }

        private HurtBox GetTarget()
        {
            Ray aimRay = base.GetAimRay();
            this.search.teamMaskFilter = TeamMask.all;
            this.search.teamMaskFilter.RemoveTeam(this.teamComponent.teamIndex);
            this.search.filterByLoS = true;
            this.search.searchOrigin = aimRay.origin;
            this.search.searchDirection = aimRay.direction;
            this.search.sortMode = BullseyeSearch.SortMode.Angle;
            this.search.maxDistanceFilter = targetRange;
            this.search.maxAngleFilter = targetAng;
            this.search.RefreshCandidates();
            return this.search.GetResults().FirstOrDefault<HurtBox>();
        }

        private void AddProjectileToRandomNode(ProjectileData proj, Boolean immediate)
        {
            var counts = new List<Int32>();
            for (Int32 i = 0; i < this.projNodes.Count; i++)
            {
                counts.Add(this.projNodes[i].queue.Count + (this.projNodes[i].nextProj != null ? 5 : 0));
            }

            Int32 min = counts.Min();
            var minInds = new List<Int32>();
            for (Int32 i = 0; i < counts.Count; i++)
            {
                if (counts[i] == min)
                {
                    minInds.Add(i);
                }
            }

            Int32 finalIndex = 0;

            if (minInds.Count > 1)
            {
                finalIndex = Mathf.FloorToInt(this.random.Range(0, minInds.Count));
            }

            if (immediate)
            {
                this.projNodes[minInds[finalIndex]].AddImmediate(proj);
            }
            else
            {
                this.projNodes[minInds[finalIndex]].AddToQueue(proj);
            }
        }
        #endregion

        #region Hooked Methods
        public override void OnEnter()
        {
            base.OnEnter();

            this.random = new Helpers.InstancedRandom((Int32)base.characterBody.netId.Value);

            this.modelTransform = base.GetModelTransform();

            instanceLookup[base.gameObject] = this;

            this.elementPower = base.characterBody.gameObject.GetComponent<ElementCounter>();
            if(elementPower == null)
            {
                Debug.Log("Energetic Resonance passive failed to find an element counter component!");
            }

            this.GenerateNodes();
        }

        public override void Update() => base.Update();

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            Single deltaT = Time.fixedDeltaTime;

            this.searchTimer += deltaT;
            if (this.searchTimer >= 1f / targetUpdateFreq)
            {
                this.target = this.GetTarget();
                this.searchTimer = 0f;
            }


            Vector3 direction = base.GetAimRay().direction;

            foreach (ProjectileNode node in this.projNodes)
            {
                node.UpdateNode(deltaT, this.target, direction);
            }
        }

        public override void OnExit()
        {
            if (instanceLookup.ContainsKey(base.gameObject))
            {
                _ = instanceLookup.Remove(base.gameObject);
            }

            base.OnExit();
        }
        #endregion
    }
}
