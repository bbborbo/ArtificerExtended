using ArtificerExtended.Passive;
using EntityStates;
using EntityStates.Mage;
using JetHack;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.EntityState
{
	public class VanillaIonSurge : GenericCharacterMain
	{
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
			if (base.isAuthority)
			{
				this.blastPosition = base.characterBody.corePosition;
			}
			if (NetworkServer.active)
			{
				BlastAttack blastAttack = new BlastAttack();
				blastAttack.radius = FlyUpState.blastAttackRadius;
				blastAttack.procCoefficient = FlyUpState.blastAttackProcCoefficient;
				blastAttack.position = this.blastPosition;
				blastAttack.attacker = base.gameObject;
				blastAttack.crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
				blastAttack.baseDamage = base.characterBody.damage * FlyUpState.blastAttackDamageCoefficient;
				blastAttack.falloffModel = BlastAttack.FalloffModel.None;
				blastAttack.baseForce = FlyUpState.blastAttackForce;
				blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
				blastAttack.damageType = DamageType.Stun1s;
				blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
				blastAttack.Fire();
			}

			if (AltArtiPassive.instanceLookup.TryGetValue(base.outer.gameObject, out var passive))
			{
				passive.SkillCast();
			}
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
			base.characterMotor.rootMotion += this.flyVector * (this.moveSpeedStat * FlyUpState.speedCoefficientCurve.Evaluate(base.fixedAge / FlyUpState.duration) * Time.fixedDeltaTime);
			base.characterMotor.velocity.y = 0f;
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
			if (base.fixedAge >= FlyUpState.duration && base.isAuthority)
			{
				this.outer.SetNextStateToMain();
			}
		}

		public override void OnExit()
		{
			if (!this.outer.destroying)
			{
				Util.PlaySound(FlyUpState.endSoundString, base.gameObject);
			}
			base.OnExit();
            if (isAuthority)
            {
				JetHackPlugin.hoverStateCache = true;
            }
		}

		private Vector3 flyVector = Vector3.zero;

		private Transform modelTransform;

		private CharacterModel characterModel;

		private HurtBoxGroup hurtboxGroup;

		private Vector3 blastPosition;
	}
}