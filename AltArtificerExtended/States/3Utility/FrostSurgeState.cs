using ArtificerExtended.Passive;
using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Mage;
using JetHack;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace ArtificerExtended.States
{
	public class FrostSurgeState : GenericCharacterMain
	{
		static GameObject muzzleflashEffect => PolarVortexBase.muzzleflashEffect;//FlyUpState.muzzleflashEffect;
		public static float blastDamageCoefficient => _3IceSurgeSkill.blastDamageCoefficient;
		public static float wallDamageCoefficient => _3IceSurgeSkill.wallDamageCoefficient;
		bool crit = false;

		private Vector3 flyVector = Vector3.zero;
		bool flyingUp = false;

		private Transform modelTransform;

		private Vector3 blastPosition;

		public float duration = 0.8f;

		private GameObject iceExplosionEffectPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/MageIceExplosion");

		public GameObject wallPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIcewallPillarProjectile");

		public GameObject bigWallPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/MageIcewallPillarProjectile");

		public override void OnEnter()
		{
			base.OnEnter();

			GameObject obj = base.outer.gameObject;
			if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
			{
				passive.SkillCast();
			}


			flyingUp = this.inputBank.moveVector.sqrMagnitude < 0.1f || this.inputBank.jump.down;
			if (flyingUp)
			{
				this.flyVector = Vector3.up; //Vector3.Normalize(base.characterDirection.forward + Vector3.up / 1.15f) * 0.8f;
				this.duration = _3IceSurgeSkill.baseDurationVertical;
			}
			else
			{
				this.flyVector = Vector3.Normalize(inputBank.moveVector.normalized + Vector3.up / 2f) * 0.8f;
				this.duration = _3IceSurgeSkill.baseDurationHorizontal;
			}
			crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
			Util.PlaySound(FlyUpState.beginSoundString, base.gameObject);
			this.modelTransform = base.GetModelTransform();
			this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
			base.PlayCrossfade("Body", "FlyUp", "FlyUp.playbackRate", FlyUpState.duration, 0.1f);
			base.characterMotor.Motor.ForceUnground();
			base.characterMotor.velocity = Vector3.zero;
			EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleLeft", false);
			EffectManager.SimpleMuzzleFlash(muzzleflashEffect, base.gameObject, "MuzzleRight", false);
			if (base.isAuthority)
			{
				this.blastPosition = base.characterBody.corePosition;
			}
            if (NetworkServer.active)
            {
                float damage = characterBody.damage * 3.5f;
				RainrotSharedUtils.Frost.FrostUtilsModule.CreateIceBlast(characterBody, 
					FlyUpState.blastAttackForce, damage, FlyUpState.blastAttackProcCoefficient, 
					FlyUpState.blastAttackRadius * 1.3f, crit, blastPosition, true, DamageSource.Special);

				FireProjectileInfo fpi = new FireProjectileInfo()
                {
                    projectilePrefab = this.bigWallPrefab,
                    position = base.transform.position,
                    rotation = Util.QuaternionSafeLookRotation(flyVector),
                    owner = base.gameObject,
                    damage = this.damageStat * wallDamageCoefficient,
                    force = 0,
                    crit = crit
                };
                ProjectileManager.instance.FireProjectile(fpi);
                if (base.isGrounded)
                {
                    InstantiateCircle();
                }
            }

            void InstantiateCircle()
			{
				float d = 2f;
				int num = 6;
				float num2 = 360f / (float)num;
				for (int i = 0; i < num; i++)
				{
					Quaternion rotation = Quaternion.AngleAxis((float)i * num2, Vector3.up);
					Vector3 a = rotation * (Vector3.forward * 0.3f);
					Vector3 a2 = base.transform.position + a * d;
					FireProjectileInfo fpi = new FireProjectileInfo()
					{
						projectilePrefab = this.wallPrefab,
						position = a2 + Vector3.down * 3f,
						rotation = Util.QuaternionSafeLookRotation(Vector3.Normalize(a + Vector3.up)),
						owner = base.gameObject,
						damage = this.damageStat * wallDamageCoefficient,
						force = 0,
						crit = crit
					};
					ProjectileManager.instance.FireProjectile(fpi);
				}
			}
		}

        public override void OnExit()
		{
			bool flag = !this.outer.destroying;
			if (flag)
			{
				Util.PlaySound(FlyUpState.endSoundString, base.gameObject);
			}
			base.OnExit();
			if (isAuthority && ArtificerExtendedPlugin.isJethackLoaded)
            {
                SetHoverStateCache();
            }
        }

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private static void SetHoverStateCache()
        {
            JetHackPlugin.hoverStateCache = true;
        }

        public override void FixedUpdate()
		{
			base.FixedUpdate();
			bool flag = base.fixedAge >= duration && base.isAuthority;
			if (flag)
			{
				this.outer.SetNextStateToMain();
			}
		}

		private void CreateBlinkEffect(Vector3 origin)
		{
			EffectData effectData = new EffectData();
			effectData.rotation = Util.QuaternionSafeLookRotation(this.flyVector);
			effectData.origin = origin;
			EffectManager.SpawnEffect(FlyUpState.blinkPrefab, effectData, false);
		}

		public override void OnSerialize(NetworkWriter writer)
		{
			base.OnSerialize(writer);
			writer.Write(this.blastPosition);
		}

		public override void OnDeserialize(NetworkReader reader)
		{
			base.OnDeserialize(reader);
			this.blastPosition = reader.ReadVector3();
		}
		public override void HandleMovements()
		{
			base.HandleMovements();
			Vector3 rootMotion = this.flyVector * (FlyUpState.speedCoefficientCurve.Evaluate(base.fixedAge / duration) * Time.fixedDeltaTime);
			if (!flyingUp)
				rootMotion *= moveSpeedStat;
			base.characterMotor.rootMotion += rootMotion;
			base.characterMotor.velocity.y = 0f;
		}
		public override void UpdateAnimationParameters()
		{
			base.UpdateAnimationParameters();
		}
	}
}
