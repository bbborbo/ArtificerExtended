using System;
using System.Collections.Generic;
using System.Text;
using AltArtificerExtended.Skills;
//using AlternativeArtificer.States.Main;
using EntityStates;
using EntityStates.Huntress;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Audio;

namespace AltArtificerExtended.EntityState
{
    
    public class CastSnowstorm : BaseSkillState
    {
        public static GameObject impactEffectPrefab = IceNova.impactEffectPrefab;
        public static GameObject novaEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/AffixWhiteExplosion");
        public static GameObject areaIndicatorPrefab = ArrowRain.areaIndicatorPrefab;
        public static GameObject projectilePrefab = _1FrostbiteSkill.blizzardProjectilePrefab;

        public static float baseStartDuration = 0.25f;
        public static float baseEndDuration = 0.9f;
        public static float novaDamageCoefficient = 4f;
        public static float blizzardDamageCoefficient = 4f;
        public static float novaProcCoefficient = 0.75f;
        public static float blizzardProcCoefficient = 0.2f;

        public static float force = 0f;
        public static float minRadius = 15f;
        public static float maxRadius = 40f;

        private float stopwatch;
        private float startDuration;

        private bool hasCastNova;
        private GameObject areaIndicatorInstance;

        //private AltArtiPassive.BatchHandle handle;
        private bool shouldFireBlizzard;


        private Transform modelTransform;
        public static GameObject blinkPrefab = BlinkState.blinkPrefab;
        private Vector3 blinkVector = Vector3.zero;
        public float speedCoefficient = 7.5f;
        public static string beginSoundString = PrepWall.prepWallSoundString;
        public static string endSoundString = PrepWall.fireSoundString;
        private CharacterModel characterModel;
        private HurtBoxGroup hurtboxGroup;

        public override void OnEnter()
        {
            base.OnEnter();
            this.stopwatch = 0f;
            this.startDuration = CastSnowstorm.baseStartDuration / this.attackSpeedStat;
            base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", CastSnowstorm.baseStartDuration);
            if (CastSnowstorm.areaIndicatorPrefab)
            {
                this.areaIndicatorInstance = UnityEngine.Object.Instantiate<GameObject>(CastSnowstorm.areaIndicatorPrefab);
                this.areaIndicatorInstance.transform.localScale = new Vector3(CastSnowstorm.maxRadius, CastSnowstorm.maxRadius, CastSnowstorm.maxRadius);
            }

            //blink
            Util.PlaySound(CastSnowstorm.beginSoundString, base.gameObject);
            this.modelTransform = base.GetModelTransform();
            if (this.modelTransform)
            {
                this.characterModel = this.modelTransform.GetComponent<CharacterModel>();
                this.hurtboxGroup = this.modelTransform.GetComponent<HurtBoxGroup>();
            }
            if (this.characterModel)
            {
                this.characterModel.invisibilityCount++;
            }
            //THIS DOES IFRAMES
            /*if (this.hurtboxGroup)
            {
                HurtBoxGroup hurtBoxGroup = this.hurtboxGroup;
                int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
                hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
            }*/
            this.blinkVector = this.GetBlinkVector();
            this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
        }

        protected virtual Vector3 GetBlinkVector()
        {
            return base.inputBank.aimDirection;
        }
        private void CreateBlinkEffect(Vector3 origin)
        {
            EffectData effectData = new EffectData();
            effectData.rotation = Util.QuaternionSafeLookRotation(this.blinkVector);
            effectData.origin = origin;
            EffectManager.SpawnEffect(BlinkState.blinkPrefab, effectData, false);
        }

        private void UpdateAreaIndicator()
        {
            if (this.areaIndicatorInstance)
            {
                this.areaIndicatorInstance.transform.position = base.transform.position;
                this.areaIndicatorInstance.transform.up = Vector3.up;
            }
        }

        // Token: 0x0600331D RID: 13085 RVA: 0x000300F3 File Offset: 0x0002E2F3
        public override void OnExit()
        {
            base.OnExit();
            if (!this.outer.destroying)
            {
                Util.PlaySound(CastSnowstorm.endSoundString, base.gameObject);
                this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
                this.modelTransform = base.GetModelTransform();
                if (this.modelTransform)
                {
                    TemporaryOverlay temporaryOverlay = this.modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                    temporaryOverlay.duration = 0.6f;
                    temporaryOverlay.animateShaderAlpha = true;
                    temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlay.destroyComponentOnEnd = true;
                    temporaryOverlay.originalMaterial = Resources.Load<Material>("Materials/matHuntressFlashBright");
                    temporaryOverlay.AddToCharacerModel(this.modelTransform.GetComponent<CharacterModel>());
                    TemporaryOverlay temporaryOverlay2 = this.modelTransform.gameObject.AddComponent<TemporaryOverlay>();
                    temporaryOverlay2.duration = 0.7f;
                    temporaryOverlay2.animateShaderAlpha = true;
                    temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                    temporaryOverlay2.destroyComponentOnEnd = true;
                    temporaryOverlay2.originalMaterial = Resources.Load<Material>("Materials/matHuntressFlashExpanded");
                    temporaryOverlay2.AddToCharacerModel(this.modelTransform.GetComponent<CharacterModel>());
                }

                if (this.shouldFireBlizzard)
                {
                    this.CastBlizzard();
                }
            }
            if (this.characterModel)
            {
                this.characterModel.invisibilityCount--;
            }
            //THIS DOES IFRAMES
            /*if (this.hurtboxGroup)
            {
                HurtBoxGroup hurtBoxGroup = this.hurtboxGroup;
                int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
                hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
            }*/

            if (this.areaIndicatorInstance)
            {
                global::EntityStates.EntityState.Destroy(this.areaIndicatorInstance.gameObject);
            }
        }

        // Token: 0x0600331E RID: 13086 RVA: 0x000D4508 File Offset: 0x000D2708
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += Time.fixedDeltaTime;
            if (this.stopwatch >= this.startDuration && !this.hasCastNova)
            {
                /*
                GameObject obj = base.outer.gameObject;
                if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
                {
                    passive.SkillCast();
                }*/

                base.PlayAnimation("Gesture, Additive", "FireWall");
                CastNova();
                this.shouldFireBlizzard = true;
                this.outer.SetNextStateToMain();
            }
            if (base.characterMotor && base.characterDirection)
            {
                base.characterMotor.velocity = Vector3.zero;
                base.characterMotor.rootMotion += this.blinkVector * (this.moveSpeedStat * this.speedCoefficient * Time.fixedDeltaTime);
            }
			this.UpdateAreaIndicator();
        }
        public override void Update()
        {
            base.Update();
            this.UpdateAreaIndicator();
        }

        public void CastNova()
        {
            this.hasCastNova = true;
            EffectManager.SpawnEffect(CastSnowstorm.novaEffectPrefab, new EffectData
            {
                origin = base.transform.position,
                scale = CastSnowstorm.minRadius
            }, true);
            BlastAttack blastAttack = new BlastAttack();
            blastAttack.radius = CastSnowstorm.minRadius;
            blastAttack.procCoefficient = CastSnowstorm.novaProcCoefficient;
            blastAttack.position = base.transform.position;
            blastAttack.attacker = base.gameObject;
            blastAttack.crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
            blastAttack.baseDamage = base.characterBody.damage * CastSnowstorm.novaDamageCoefficient;
            blastAttack.falloffModel = BlastAttack.FalloffModel.None;
            blastAttack.damageType = DamageType.Freeze2s;
            blastAttack.baseForce = CastSnowstorm.force;
            blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
            blastAttack.Fire();
        }

        public void CastBlizzard()
        {
            if (this.areaIndicatorInstance && this.shouldFireBlizzard)
			{
				ProjectileManager.instance.FireProjectile(CastSnowstorm.projectilePrefab, 
                    this.areaIndicatorInstance.transform.position, this.areaIndicatorInstance.transform.rotation, 
                    base.gameObject, this.damageStat * CastSnowstorm.blizzardDamageCoefficient, 0f, 
                    Util.CheckRoll(this.critStat, base.characterBody.master), 
                    DamageColorIndex.Default, null, -1f);
			}
        }
        /*
        private void OnIciclesActivated()
        {
            Util.PlaySound("Play_item_proc_icicle", base.gameObject);
            foreach (ParticleSystem particleSystem in this.auraParticles)
            {
                particleSystem.main.loop = true;
                particleSystem.Play();
            }
        }
        private void OnIciclesDectivated()
        {
            Util.PlaySound("Stop_item_proc_icicle", base.gameObject);
            ParticleSystem[] array = this.auraParticles;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].main.loop = false;
            }
            if (this.cachedOwnerInfo.cameraTargetParams)
            {
                this.cachedOwnerInfo.cameraTargetParams.aimMode = CameraTargetParams.AimType.Standard;
            }
        }*/

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
    
}
