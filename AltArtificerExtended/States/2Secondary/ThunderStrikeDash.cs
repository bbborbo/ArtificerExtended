using ArtificerExtended.Components;
using ArtificerExtended.Passive;
using ArtificerExtended.Skills;
using EntityStates;
using EntityStates.Huntress;
using RoR2;
using RoR2.Networking;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.States
{
	class ThunderStrikeDash : BasicMeleeAttack
	{
		bool hasIncreasedDuration = false;
		public float speedCoefficientOnExit = 1;
		private CharacterModel characterModel;
		float invulDuration = 0.1f;
		public float speedCoefficient => _2ThunderstrikeSkill.speedCoefficient;
		public string endSoundString;
		public float exitSmallHop = 6f;
		public float delayedDamageCoefficient => _2ThunderstrikeSkill.delayDamageCoefficient;
		public float delayedProcCoefficient => _2ThunderstrikeSkill.delayProcCoefficient;
		public float delay = 0.25f;
		public float delayPerHit = 0f;
		public string enterAnimationLayerName = "Gesture, Additive";
		public string enterAnimationStateName = "PrepWall";
		public float enterAnimationCrossfadeDuration = 0.1f;
		public string exitAnimationLayerName = "Gesture, Additive";
		public string exitAnimationStateName = "FireNovaBomb";

		public GameObject delayedEffectPrefab => _2ThunderstrikeSkill.lightningImpactEffect;
		public GameObject orbEffect => _2ThunderstrikeSkill.lightningOrbEffect;

		public GameObject selfOnHitOverlayEffectPrefab;
		private Transform modelTransform;
		private Vector3 dashVector;
		private int originalLayer;
		private int currentHitCount;

		GameObject surgeTrailEffectInstanceL;
		GameObject surgeTrailEffectInstanceR;

		private Vector3 dashVelocity
		{
			get
			{
				return this.dashVector * this.moveSpeedStat * this.speedCoefficient * this.attackSpeedStat;
			}
		}

        public override string GetHitBoxGroupName()
        {
			return _2ThunderstrikeSkill.ThunderStrikeHitBoxGroupName;

		}
        public override void OnEnter()
		{
			this.hitPauseDuration = 0.06f;
			//this.ignoreAttackSpeed = true;
			this.baseDuration = _2ThunderstrikeSkill.baseDuration;
			base.OnEnter();
			this.dashVector = base.inputBank.aimDirection;
			this.originalLayer = base.gameObject.layer;
			base.gameObject.layer = LayerIndex.GetAppropriateFakeLayerForTeam(base.teamComponent.teamIndex).intVal;
			base.characterMotor.Motor.RebuildCollidableLayers();
			base.characterMotor.Motor.ForceUnground(0.1f);
			base.characterMotor.velocity = Vector3.zero;
			this.modelTransform = base.GetModelTransform();
			if (this.modelTransform)
			{
				TemporaryOverlayInstance temporaryOverlay = TemporaryOverlayManager.AddOverlay(this.modelTransform.gameObject);
				temporaryOverlay.duration = 0.6f;
				temporaryOverlay.animateShaderAlpha = true;
				temporaryOverlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlay.destroyComponentOnEnd = true;
				temporaryOverlay.originalMaterial = RoR2.LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashBright");
				temporaryOverlay.AddToCharacterModel(this.modelTransform.GetComponent<CharacterModel>());
				TemporaryOverlayInstance temporaryOverlay2 = TemporaryOverlayManager.AddOverlay(this.modelTransform.gameObject);
				temporaryOverlay2.duration = 0.7f;
				temporaryOverlay2.animateShaderAlpha = true;
				temporaryOverlay2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
				temporaryOverlay2.destroyComponentOnEnd = true;
				temporaryOverlay2.originalMaterial = RoR2.LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashExpanded");
				temporaryOverlay2.AddToCharacterModel(this.modelTransform.GetComponent<CharacterModel>());
			}
			this.modelTransform = base.GetModelTransform();
			if (this.modelTransform)
			{
				this.characterModel = this.modelTransform.GetComponent<CharacterModel>();
			}
			if (this.characterModel)
			{
				this.characterModel.invisibilityCount++;
			}
			base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", this.enterAnimationCrossfadeDuration);
			//base.PlayCrossfade(this.enterAnimationLayerName, this.enterAnimationStateName, this.enterAnimationCrossfadeDuration);
			base.characterDirection.forward = base.characterMotor.velocity.normalized;
			if (NetworkServer.active)
			{
				//base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
			}
			CreateBlinkEffect(Util.GetCorePosition(this.gameObject));
		}
		private void CreateBlinkEffect(Vector3 origin)
		{
			EffectData effectData = new EffectData();
			effectData.rotation = Util.QuaternionSafeLookRotation(this.dashVector);
			effectData.origin = origin;
			EffectManager.SpawnEffect(MiniBlinkState.blinkPrefab, effectData, false);


			Transform muzzleTransformL = base.FindModelChild("MuzzleLeft");
			if (muzzleTransformL)
				surgeTrailEffectInstanceL = UnityEngine.Object.Instantiate<GameObject>(ArtificerExtendedPlugin.muzzleflashIonSurgeTrail, muzzleTransformL);
			Transform muzzleTransformR = base.FindModelChild("MuzzleRight");
			if (muzzleTransformR)
				surgeTrailEffectInstanceR = UnityEngine.Object.Instantiate<GameObject>(ArtificerExtendedPlugin.muzzleflashIonSurgeTrail, muzzleTransformR);
		}

		public override void OnExit()
		{
			if (surgeTrailEffectInstanceL)
				Destroy(surgeTrailEffectInstanceL);
			if (surgeTrailEffectInstanceR)
				Destroy(surgeTrailEffectInstanceR);

			GameObject obj = base.outer.gameObject;
			if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
			{
				passive.SkillCast();
			}
			if((int)ElementCounter.GetPowerLevelFromBody(obj, MageElement.Lightning, passive) >= 4)
            {
				int targetsStruckCount = Mathf.Min(currentHitCount, _2ThunderstrikeSkill.resonantCdrMax);
				GenericSkill secondary = this.skillLocator.secondary;
				secondary.RunRecharge(_2ThunderstrikeSkill.resonantCdrFirst + _2ThunderstrikeSkill.resonantCdr * (targetsStruckCount - 1));
			}

			if (this.characterModel)
			{
				this.characterModel.invisibilityCount--;
			}
			if (NetworkServer.active)
			{
				//base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
			}
			base.characterMotor.velocity *= this.speedCoefficientOnExit;
			base.SmallHop(base.characterMotor, this.exitSmallHop);
			Util.PlaySound(this.endSoundString, base.gameObject);
			this.PlayAnimation(this.exitAnimationLayerName, this.exitAnimationStateName);
			base.gameObject.layer = this.originalLayer;
			base.characterMotor.Motor.RebuildCollidableLayers();
			base.OnExit();
		}

		public override void PlayAnimation()
		{
			base.PlayAnimation();
			base.PlayCrossfade(this.enterAnimationLayerName, this.enterAnimationStateName, this.enterAnimationCrossfadeDuration);
		}

		public override void AuthorityFixedUpdate()
		{
			base.AuthorityFixedUpdate();
			if (!base.authorityInHitPause)
			{
				base.characterMotor.rootMotion += this.dashVelocity * base.GetDeltaTime();
				base.characterDirection.forward = this.dashVelocity;
				base.characterDirection.moveVector = this.dashVelocity;
				base.characterBody.isSprinting = true;
			}
		}

		public override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
		{
			base.AuthorityModifyOverlapAttack(overlapAttack);
			overlapAttack.damage = _2ThunderstrikeSkill.damageCoefficient * this.damageStat;
			overlapAttack.damageType = DamageTypeCombo.GenericSecondary;
		}

        public override void FixedUpdate()
		{
			if (currentHitCount > 0 && !this.hasIncreasedDuration)
			{
				//this.hasIncreasedDuration = true;
				//this.duration *= _2ThunderstrikeSkill.durationMultOnHit;
			}
			base.FixedUpdate();
        }

        public override void OnMeleeHitAuthority()
		{
			base.OnMeleeHitAuthority();
			characterBody.AddTimedBuffAuthority(RoR2Content.Buffs.HiddenInvincibility.buffIndex, invulDuration);

			if (!this.hasIncreasedDuration)
			{
				this.hasIncreasedDuration = true;
				this.duration *= _2ThunderstrikeSkill.durationMultOnHit;
			}

			float num = this.hitPauseDuration / this.attackSpeedStat;
			if (this.selfOnHitOverlayEffectPrefab && num > 0.033333335f)
			{
				EffectData effectData = new EffectData
				{
					origin = base.transform.position,
					genericFloat = this.hitPauseDuration / this.attackSpeedStat
				};
				effectData.SetNetworkedObjectReference(base.gameObject);
				EffectManager.SpawnEffect(this.selfOnHitOverlayEffectPrefab, effectData, true);
			}
			foreach (HurtBox victimHurtBox in this.hitResults)
			{
				this.currentHitCount++;
				float damageValue = base.characterBody.damage * this.delayedDamageCoefficient;
				float num2 = this.delay + this.delayPerHit * (float)this.currentHitCount;
				bool isCrit = base.RollCrit();
				ThunderStrikeDash.HandleHit(base.gameObject, victimHurtBox, damageValue, this.delayedProcCoefficient, isCrit, num2, this.orbEffect, this.delayedEffectPrefab);
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.PrioritySkill;
		}

		private static void HandleHit(GameObject attackerObject, HurtBox victimHurtBox, float damageValue, float procCoefficient, bool isCrit, float delay, GameObject orbEffectPrefab, GameObject orbImpactEffectPrefab)
		{
			if (!NetworkServer.active)
			{
				NetworkWriter networkWriter = new NetworkWriter();
				networkWriter.StartMessage(21206);
				networkWriter.Write(attackerObject);
				networkWriter.Write(HurtBoxReference.FromHurtBox(victimHurtBox));
				networkWriter.Write(damageValue);
				networkWriter.Write(procCoefficient);
				networkWriter.Write(isCrit);
				networkWriter.Write(delay);
				networkWriter.WriteEffectIndex(EffectCatalog.FindEffectIndexFromPrefab(orbEffectPrefab));
				networkWriter.WriteEffectIndex(EffectCatalog.FindEffectIndexFromPrefab(orbImpactEffectPrefab));
				networkWriter.FinishMessage();
				NetworkConnection readyConnection = ClientScene.readyConnection;
				if (readyConnection == null)
				{
					return;
				}
				readyConnection.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
				return;
			}
			else
			{
				if (!victimHurtBox || !victimHurtBox.healthComponent)
				{
					return;
				}
				SetStateOnHurt.SetStunOnObject(victimHurtBox.healthComponent.gameObject, delay);
				OrbManager.instance.AddOrb(new SimpleLightningStrikeOrb
				{
					attacker = attackerObject,
					target = victimHurtBox,
					damageColorIndex = DamageColorIndex.Default,
					damageValue = damageValue,
					isCrit = isCrit,
					procChainMask = default(ProcChainMask),
					procCoefficient = procCoefficient
				});
				return;

				OrbManager.instance.AddOrb(new DelayedHitOrb
				{
					attacker = attackerObject,
					target = victimHurtBox,
					damageColorIndex = DamageColorIndex.Default,
					damageValue = damageValue,
					damageType = new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.Generic, DamageSource.Secondary),
					isCrit = isCrit,
					procChainMask = default(ProcChainMask),
					procCoefficient = procCoefficient,
					delay = delay,
					orbEffect = orbEffectPrefab,
					delayedEffectPrefab = orbImpactEffectPrefab
				});
				return;
			}
		}

		[NetworkMessageHandler(msgType = 21206, client = false, server = true)]
		private static void HandleReportMercFocusedAssaultHitReplaceMeLater(NetworkMessage netMsg)
		{
			GameObject attackerObject = netMsg.reader.ReadGameObject();
			HurtBox victimHurtBox = netMsg.reader.ReadHurtBoxReference().ResolveHurtBox();
			float damageValue = netMsg.reader.ReadSingle();
			float procCoefficient = netMsg.reader.ReadSingle();
			bool isCrit = netMsg.reader.ReadBoolean();
			float num = netMsg.reader.ReadSingle();
			EffectDef effectDef = EffectCatalog.GetEffectDef(netMsg.reader.ReadEffectIndex());
			GameObject orbEffectPrefab = ((effectDef != null) ? effectDef.prefab : null) ?? null;
			EffectDef effectDef2 = EffectCatalog.GetEffectDef(netMsg.reader.ReadEffectIndex());
			GameObject orbImpactEffectPrefab = ((effectDef2 != null) ? effectDef2.prefab : null) ?? null;
			ThunderStrikeDash.HandleHit(attackerObject, victimHurtBox, damageValue, procCoefficient, isCrit, num, orbEffectPrefab, orbImpactEffectPrefab);
		}
	}
}
