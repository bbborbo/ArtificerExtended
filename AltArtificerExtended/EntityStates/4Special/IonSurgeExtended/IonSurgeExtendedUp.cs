using ArtificerExtended.Passive;
using EntityStates;
using EntityStates.Mage;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.EntityState
{
    class IonSurgeExtendedUp : IonSurgeExtendedBase
	{
		public bool halting = false;
		public static float hoverDuration = 2;

		private Vector3 flyVector = Vector3.zero;

		private Transform modelTransform;
		public override void OnEnter()
		{
			base.OnEnter();
			Util.PlaySound(FlyUpState.beginSoundString, base.gameObject);
			this.modelTransform = base.GetModelTransform();
			this.flyVector = Vector3.up;
			this.CreateBlinkEffect(Util.GetCorePosition(base.gameObject));
			base.PlayCrossfade("Body", "FlyUp", "FlyUp.playbackRate", FlyUpState.duration, 0.1f);
			base.characterMotor.Motor.ForceUnground();
			base.characterMotor.velocity = Vector3.zero;
			EffectManager.SimpleMuzzleFlash(FlyUpState.muzzleflashEffect, base.gameObject, "MuzzleLeft", false);
			EffectManager.SimpleMuzzleFlash(FlyUpState.muzzleflashEffect, base.gameObject, "MuzzleRight", false);

			if (AltArtiPassive.instanceLookup.TryGetValue(base.outer.gameObject, out var passive))
			{
				passive.SkillCast();
			}
		}

		public override void HandleMovements()
		{
			base.HandleMovements();
            if (!halting)
            {
				base.characterMotor.rootMotion += this.flyVector * 
					(this.moveSpeedStat * FlyUpState.speedCoefficientCurve.Evaluate(base.fixedAge / FlyUpState.duration) * Time.fixedDeltaTime);
				base.characterMotor.velocity.y = 0f;
			}
            else
			{
				if (base.isAuthority)
				{
					float num = base.characterMotor.velocity.y;
					if(num < 0)
					{
						num = Mathf.MoveTowards(num, JetpackOn.hoverVelocity, JetpackOn.hoverAcceleration * Time.fixedDeltaTime);
						base.characterMotor.velocity = new Vector3(base.characterMotor.velocity.x, num, base.characterMotor.velocity.z);
					}
				}
			}
		}

		public override void UpdateAnimationParameters()
		{
			base.UpdateAnimationParameters();
		}

		private void CreateBlinkEffect(Vector3 origin)
		{
			EffectData effectData = new EffectData();
			effectData.rotation = Util.QuaternionSafeLookRotation(this.flyVector);
			effectData.origin = origin;
			EffectManager.SpawnEffect(FlyUpState.blinkPrefab, effectData, false);
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.fixedAge >= FlyUpState.duration && !halting)
			{
				//this.outer.SetNextStateToMain();
				halting = true;
			}
		}

		public override void OnExit()
		{
			if (!this.outer.destroying)
			{
				Util.PlaySound(FlyUpState.endSoundString, base.gameObject);
			}
			base.OnExit();
		}
	}
}
