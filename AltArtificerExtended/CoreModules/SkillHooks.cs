﻿using System;
using System.Collections.Generic;
using System.Text;

using EntityStates.Mage.Weapon;

using Mono.Cecil.Cil;

using MonoMod.Cil;

using RoR2;
using RoR2.Skills;

using ArtificerExtended.Passive;

using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using R2API;
using R2API.Utils;
using ArtificerExtended;
using ArtificerExtended.Components;
using static ArtificerExtended.Passive.AltArtiPassive;
using static ArtificerExtended.Components.ElementCounter;
using static RainrotSharedUtils.Frost.FrostUtilsModule;
using static R2API.RecalculateStatsAPI;
using ArtificerExtended.Modules;
using ArtificerExtended.States;
using RoR2.Projectile;
using RoR2.Orbs;
using EntityStates;
using RoR2BepInExPack.GameAssetPaths;
using UnityEngine.AddressableAssets;
using RoR2.ContentManagement;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ArtificerExtended
{
    public partial class ArtificerExtendedPlugin
    {
        public delegate TCheese GiveCheese<TCheese>();

        public void DoHooks() => this.AddHooks();

        void RemoveHooks()
        {

        }

        void AddHooks()
        {
            On.EntityStates.Mage.Weapon.FireFireBolt.FireGauntlet += this.FireFireBolt_FireGauntlet;
            On.EntityStates.Mage.Weapon.BaseChargeBombState.OnEnter += this.BaseChargeBombState_OnEnter;
            On.EntityStates.Mage.Weapon.BaseChargeBombState.FixedUpdate += this.BaseChargeBombState_FixedUpdate;
            //On.EntityStates.Mage.Weapon.BaseThrowBombState.OnEnter += this.BaseChargeBombState_GetNextState;
            On.EntityStates.Mage.Weapon.BaseChargeBombState.OnExit += this.BaseChargeBombState_OnExit;
            On.EntityStates.Mage.Weapon.PrepWall.OnEnter += this.PrepWall_OnEnter;
            On.EntityStates.Mage.Weapon.PrepWall.OnExit += this.PrepWall_OnExit;
            On.EntityStates.Mage.Weapon.Flamethrower.OnEnter += this.Flamethrower_OnEnter;
            On.EntityStates.Mage.Weapon.Flamethrower.FixedUpdate += this.Flamethrower_FixedUpdate;
            On.EntityStates.Mage.Weapon.Flamethrower.OnExit += this.Flamethrower_OnExit;
            On.RoR2.HealthComponent.TakeDamage += this.HealthComponent_TakeDamage;
            GlobalEventManager.onCharacterDeathGlobal += this.GlobalEventManager_OnCharacterDeath;
            //On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            //On.RoR2.CharacterBody.AddBuff_BuffIndex += CharacterBody_AddBuff_BuffIndex;
            On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;
            On.RoR2.CharacterMaster.OnBodyDestroyed += CharacterMaster_OnBodyDestroyed;
            //IL.RoR2.GlobalEventManager.ProcessHitEnemy += MaxFrostHook;
            //OnMaxChill += FrostNovaOnMaxChill;
            GetStatCoefficients += MeltAttackSpeedBuff;
            On.EntityStates.FrozenState.OnEnter += FrostNovaOnFreeze;
            On.EntityStates.FrozenState.FixedUpdate += RemoveFrostWhileFrozen;
            On.RoR2.SetStateOnHurt.SetFrozenInternal += FixSetFrozen;
            On.RoR2.CharacterBody.AddTimedBuff_BuffIndex_float += FixFrostStacks;
            On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += FixFrostStacks2;

            On.RoR2.GlobalEventManager.OnHitAll += ChainLightningHook;


            AssetReferenceT<GameObject> ref1 = new AssetReferenceT<GameObject>(RoR2_Base_Mage.MageIceBombProjectile_prefab);
            AssetAsyncReferenceManager<GameObject>.LoadAsset(ref1).Completed += FixIceSpear;
            AssetReferenceT<GameObject> ref2 = new AssetReferenceT<GameObject>(RoR2_Base_Lightning.LightningStrikeImpact_prefab);
            AssetAsyncReferenceManager<GameObject>.LoadAsset(ref2).Completed += (ctx) => FixLightningStrike(ctx.Result);
            AssetReferenceT<GameObject> ref3 = new AssetReferenceT<GameObject>(RoR2_Base_LightningStrikeOnHit.SimpleLightningStrikeImpact_prefab);
            AssetAsyncReferenceManager<GameObject>.LoadAsset(ref3).Completed += (ctx) => FixLightningStrike(ctx.Result);

            //On.RoR2.SeekerSoulSpiralManager.DiscoverUnassignedSpirals += FixSoulSpiralNRE;
        }

        private void FixLightningStrike(GameObject lightningStrikeImpactPrefab)
        {
            if (!lightningStrikeImpactPrefab.TryGetComponent(out EffectComponent effectComponent))
            {
                Log.Error("Lightning strike impact is missing EffectComponent");
                return;
            }

            if (string.IsNullOrEmpty(effectComponent.soundName))
            {
                effectComponent.soundName = "Play_item_use_lighningArm";
            }
        }

        private void RemoveFrostWhileFrozen(On.EntityStates.FrozenState.orig_FixedUpdate orig, FrozenState self)
        {
            if (self.characterBody)
            {
                self.characterBody.SetBuffCount(DLC2Content.Buffs.Frost.buffIndex, 0);
            }
            orig(self);
        }

        private void FixFrostStacks2(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
        {
            if (self.healthComponent.isInFrozenState && buffDef == DLC2Content.Buffs.Frost)
            {
                return;
            }
            orig(self, buffDef, duration);
        }

        private void FixFrostStacks(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffIndex_float orig, CharacterBody self, BuffIndex buffDef, float duration)
        {
            if (self.healthComponent.isInFrozenState && buffDef == DLC2Content.Buffs.Frost.buffIndex)
            {
                return;
            }
            orig(self, buffDef, duration);
        }

        private void FixSoulSpiralNRE(On.RoR2.SeekerSoulSpiralManager.orig_DiscoverUnassignedSpirals orig, SeekerSoulSpiralManager self)
        {
            if(self == null || self.seekerController == null || SoulSpiralProjectile.unassignedSoulSpirals == null || SoulSpiralProjectile.unassignedSoulSpirals.Count <= 0)
            {
                self.StopListeningForUnassignedSpirals();
                return;
            }    
            orig(self);
        }

        private void FixIceSpear(AsyncOperationHandle<GameObject> iceSpearPrefab)
        {
            iceSpearPrefab.Result.layer = LayerIndex.projectileWorldOnly.intVal;
        }

        private void FixSetFrozen(On.RoR2.SetStateOnHurt.orig_SetFrozenInternal orig, SetStateOnHurt self, float duration)
        {
            if (self.targetStateMachine)
            {
                FrozenState frozenState = new FrozenState();
                frozenState.freezeDuration = duration;
                self.targetStateMachine.SetInterruptState(frozenState, InterruptPriority.Frozen);
            }
            EntityStateMachine[] array = self.idleStateMachine;
            for (int i = 0; i < array.Length; i++)
            {
                array[i].SetNextStateToMain();
            }
        }

        private void FrostNovaOnFreeze(On.EntityStates.FrozenState.orig_OnEnter orig, EntityStates.FrozenState self)
        {
            if (NetworkServer.active)
            {
                CharacterBody attackerBody = null;
                GameObject lastHitAttacker = self.healthComponent.lastHitAttacker;
                bool crit = false;
                if (lastHitAttacker)
                    attackerBody = lastHitAttacker.GetComponent<CharacterBody>();
                if (attackerBody)
                    crit = Util.CheckRoll(attackerBody.crit, attackerBody.master);

                FrostNovaOnMaxChill(attackerBody, self.characterBody, crit);
            }

            orig(self);
        }

        private static void ChainLightningHook(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            if (damageInfo.HasModdedDamageType(CommonAssets.ChainLightningDamageType))
            {
                LightningOrb lightningOrb2 = new LightningOrb();
                lightningOrb2.origin = damageInfo.position;
                lightningOrb2.damageValue = damageInfo.damage * CommonAssets.chainLightningZapDamageFraction;
                lightningOrb2.isCrit = damageInfo.crit;
                lightningOrb2.teamIndex = TeamComponent.GetObjectTeam(damageInfo.attacker);
                lightningOrb2.attacker = damageInfo.attacker;

                lightningOrb2.bouncesRemaining = 0; //will connect to one new target, no bounce
                lightningOrb2.canBounceOnSameTarget = false;
                lightningOrb2.bouncedObjects = new List<HealthComponent>();
                HurtBox victim = hitObject.GetComponent<HurtBox>();
                if (victim && victim.healthComponent)
                    lightningOrb2.bouncedObjects.Add(victim.healthComponent);
                else
                {
                    HealthComponent victimHealthComponent = hitObject.GetComponent<HealthComponent>();
                    if (victimHealthComponent)
                        lightningOrb2.bouncedObjects.Add(victimHealthComponent);
                }

                lightningOrb2.procChainMask = damageInfo.procChainMask;
                lightningOrb2.procCoefficient = CommonAssets.chainLightningZapDamageCoefficient;
                lightningOrb2.lightningType = LightningOrb.LightningType.Ukulele;
                lightningOrb2.damageColorIndex = DamageColorIndex.Default;
                lightningOrb2.range = CommonAssets.chainLightningZapDistance;
                HurtBox hurtBox2 = lightningOrb2.PickNextTarget(damageInfo.position);
                if (hurtBox2)
                {
                    lightningOrb2.target = hurtBox2;
                    OrbManager.instance.AddOrb(lightningOrb2);
                }
            }
            orig(self, damageInfo, hitObject);
        }

        private void MeltAttackSpeedBuff(CharacterBody sender, StatHookEventArgs args)
        {
            int meltBuffCount = sender.GetBuffCount(CommonAssets.meltBuff);
            if(meltBuffCount > 0)
            {
                args.baseAttackSpeedAdd += AltArtiPassive.meltAspdIncrease * meltBuffCount;
            }
        }

        private void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
        {
            ElementCounter elements = body.GetComponent<ElementCounter>();
            if (elements != null)
            {
                //Debug.Log("Element counter on body start");
                elements.OnBodyStart(body.skillLocator);
                //elements.GetPowers(body.skillLocator);
            }

            orig(self, body);
        }

        private void CharacterMaster_OnBodyDestroyed(On.RoR2.CharacterMaster.orig_OnBodyDestroyed orig, CharacterMaster self, CharacterBody body)
        {
            ElementCounter elements = body.GetComponent<ElementCounter>();
            if (elements != null)
            {
                elements.OnBodyEnd();
            }

            orig(self, body);
        }

        #region IceStuff + FireStuff

        private void GlobalEventManager_OnCharacterDeath(DamageReport damageReport)
        {
            if (NetworkServer.active)
            {
                if(damageReport != null)
                {
                    CharacterBody aBody = damageReport.attackerBody;
                    CharacterBody vBody = damageReport.victimBody;
                    if (vBody && aBody && vBody.healthComponent && AltArtiPassive.instanceLookup.TryGetValue(aBody.gameObject, out var passive))
                    {
                        Power icePower = GetPowerLevelFromBody(aBody.gameObject, MageElement.Ice, passive);

                        int chillDebuffCount = vBody.GetBuffCount(DLC2Content.Buffs.Frost);
                        if(vBody.healthComponent.isInFrozenState)
                            chillDebuffCount = Mathf.Min(chillDebuffCount + 3, 5);
                        int chillLimitCount = 0;// vBody.GetBuffCount(ChillRework.ChillRework.ChillLimitBuff);
                        int minChillForBlast = chillLimitCount > 0 ? 3 : 1;

                        if (chillDebuffCount >= minChillForBlast && icePower > 0) 
                        {
                            float chillFraction = (float)chillDebuffCount / chillStacksMax;
                            float chillFractionInverse = 1 - chillFraction;
                            float procChanceFractionInverse = Mathf.Pow(chillFractionInverse, 2);
                            float procChanceFraction = 1 - procChanceFractionInverse;
                            if (Util.CheckRoll(procChanceFraction * 100, damageReport.attackerMaster))
                            {
                                //Arctic Blast
                                AltArtiPassive.DoArcticBlast(aBody, icePower, damageReport.victim.transform.position, damageReport.damageInfo.crit, chillDebuffCount);
                            }
                        }
                        #region old stuff
                        /*if (damageReport.victimBody.healthComponent.isInFrozenState)
                        {
                            if (this.frozenBy.ContainsKey(damageReport.victim.gameObject))
                            {
                                GameObject body = this.frozenBy[damageReport.victim.gameObject];
                                if (AltArtiPassive.instanceLookup.ContainsKey(body))
                                {
                                    AltArtiPassive passive = AltArtiPassive.instanceLookup[body];
                                    passive.DoExecute(damageReport);
                                }
                            }
                        }
                        else if (damageReport.damageInfo.damageType.HasFlag(DamageType.Freeze2s))
                        {
                            if (AltArtiPassive.instanceLookup.ContainsKey(damageReport.attacker))
                            {
                                AltArtiPassive.instanceLookup[damageReport.attacker].DoExecute(damageReport);
                            }
                        }*/
                        #endregion
                    }
                }
            }
        }
        private void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            /*if (damageInfo.dotIndex == Buffs.burnDot || damageInfo.dotIndex == Buffs.strongBurnDot)
            {
                if (damageInfo.attacker)
                {
                    CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    if (attackerBody)
                    {
                        Int32 buffCount = attackerBody.GetBuffCount(Buffs.meltBuff);

                        if (buffCount >= 0)
                        {
                            damageInfo.damage *= 1f + (AltArtiPassive.burnDamageMult * buffCount);

                            if (Util.CheckRoll((buffCount / 15) * 100, attackerBody.master))
                            {
                                EffectManager.SimpleImpactEffect(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/MagmaOrbExplosion"), damageInfo.position, Vector3.up, true);
                                //ImpactWispEmber MagmaOrbExplosion IgniteDirectionalExplosionVFX IgniteExplosionVFX FireMeatBallExplosion
                            }
                        }
                    }
                }
            }*/
        }

        private void MaxFrostHook(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<DamageTypeCombo>(nameof(DamageTypeCombo.IsChefFrostDamage)))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<SetStateOnHurt>(nameof(SetStateOnHurt.SetFrozen)));
            if (b)
            {
                c.Emit(OpCodes.Ldarg_0, 0); //damageInfo
                c.Emit(OpCodes.Ldloc, 0); //attackerBody
                c.Emit(OpCodes.Ldloc, 1); //victimBody
                c.EmitDelegate<Action<DamageInfo, CharacterBody, CharacterBody>>((damageInfo, attackerBody, victimBody) =>
                {
                    if (NetworkServer.active)
                    {
                        FrostNovaOnMaxChill(attackerBody, victimBody, damageInfo.crit);
                    }
                });
            }
            else
            {
            }
        }

        private static void FrostNovaOnMaxChill(CharacterBody aBody, CharacterBody vBody, bool isCrit)
        {
            if (aBody != null && AltArtiPassive.instanceLookup.TryGetValue(aBody.gameObject, out AltArtiPassive passive))
            {
                Power icePower = GetPowerLevelFromBody(aBody.gameObject, MageElement.Ice, passive);
                if (icePower > Power.None) //Arctic Blast
                {
                    AltArtiPassive.DoArcticBlast(aBody, icePower, vBody.corePosition, isCrit);
                }
            }
        }

        #endregion
        #region Flamethrower
        private class FlamethrowerContext
        {
            public AltArtiPassive passive;
            public Single timer;

            public FlamethrowerContext(AltArtiPassive passive)
            {
                this.passive = passive;
                this.timer = 0f;
            }
        }
        private readonly Dictionary<Flamethrower, FlamethrowerContext> flamethrowerContext = new Dictionary<Flamethrower, FlamethrowerContext>();
        private void Flamethrower_OnEnter(On.EntityStates.Mage.Weapon.Flamethrower.orig_OnEnter orig, Flamethrower self)
        {
            orig(self);
            GameObject obj = self.outer.gameObject;
            if (AltArtiPassive.instanceLookup.ContainsKey(obj))
            {
                AltArtiPassive passive = AltArtiPassive.instanceLookup[obj];
                var context = new FlamethrowerContext(passive);
                passive.SkillCast(isFire: true);
                this.flamethrowerContext[self] = context;
            }
        }
        private void Flamethrower_FixedUpdate(On.EntityStates.Mage.Weapon.Flamethrower.orig_FixedUpdate orig, Flamethrower self)
        {
            orig(self);
            if (this.flamethrowerContext.ContainsKey(self))
            {
                FlamethrowerContext context = this.flamethrowerContext[self];
                context.timer += Time.fixedDeltaTime * context.passive.attackSpeedStat;
                Int32 count = 0;
                while (context.timer >= context.passive.ext_flamethrowerInterval && count <= context.passive.ext_flamethrowerMaxPerTick)
                {
                    context.passive.SkillCast(isFire: true);
                    count++;
                    context.timer -= context.passive.ext_flamethrowerInterval;
                }
            }
        }
        private void Flamethrower_OnExit(On.EntityStates.Mage.Weapon.Flamethrower.orig_OnExit orig, Flamethrower self)
        {
            orig(self);
            if (this.flamethrowerContext.ContainsKey(self))
            {
                FlamethrowerContext context = this.flamethrowerContext[self];
                context.passive.SkillCast(isFire: true);
                _ = this.flamethrowerContext.Remove(self);
            }
        }
        #endregion
        #region Ice Wall
        private class PrepWallContext
        {
            public AltArtiPassive passive;
            public AltArtiPassive.BatchHandle handle;

            public PrepWallContext(AltArtiPassive passive, AltArtiPassive.BatchHandle handle)
            {
                this.passive = passive;
                this.handle = handle;
            }
        }
        private readonly Dictionary<PrepWall, PrepWallContext> prepWallContext = new Dictionary<PrepWall, PrepWallContext>();
        private void PrepWall_OnEnter(On.EntityStates.Mage.Weapon.PrepWall.orig_OnEnter orig, PrepWall self)
        {
            orig(self);
            GameObject obj = self.outer.gameObject;
            if (AltArtiPassive.instanceLookup.ContainsKey(obj))
            {
                AltArtiPassive passive = AltArtiPassive.instanceLookup[obj];
                var handle = new AltArtiPassive.BatchHandle();
                passive.SkillCast(handle);
                var context = new PrepWallContext(passive, handle);
                this.prepWallContext[self] = context;
            }
        }
        private void PrepWall_OnExit(On.EntityStates.Mage.Weapon.PrepWall.orig_OnExit orig, PrepWall self)
        {
            orig(self);
            if (this.prepWallContext.ContainsKey(self))
            {
                PrepWallContext context = this.prepWallContext[self];
                context.handle.Fire(context.passive.ext_prepWallMinDelay, context.passive.ext_prepWallMaxDelay);
                _ = this.prepWallContext.Remove(self);
            }
        }
        #endregion
        #region Nano Bomb/Spear
        private class NanoBombContext
        {
            public AltArtiPassive passive;
            public AltArtiPassive.BatchHandle handle;
            public Single timer;
            public NanoBombContext(AltArtiPassive passive, AltArtiPassive.BatchHandle handle)
            {
                this.passive = passive;
                this.handle = handle;
                this.timer = 0f;
            }
        }
        private readonly Dictionary<BaseChargeBombState, NanoBombContext> nanoBombContext = new Dictionary<BaseChargeBombState, NanoBombContext>();
        private void BaseChargeBombState_OnEnter(On.EntityStates.Mage.Weapon.BaseChargeBombState.orig_OnEnter orig, BaseChargeBombState self)
        {
            //Debug.Log(self.chargeEffectPrefab.name);
            orig(self);
            GameObject obj = self.outer.gameObject;
            if (AltArtiPassive.instanceLookup.ContainsKey(obj))
            {
                AltArtiPassive passive = AltArtiPassive.instanceLookup[obj];
                var handle = new AltArtiPassive.BatchHandle();
                var context = new NanoBombContext(passive, handle);
                this.nanoBombContext[self] = context;
                passive.SkillCast(handle, (self is ChargeSolarFlare ? true : false));
            }
        }
        private void BaseChargeBombState_FixedUpdate(On.EntityStates.Mage.Weapon.BaseChargeBombState.orig_FixedUpdate orig, BaseChargeBombState self)
        {
            orig(self);
            if (this.nanoBombContext.ContainsKey(self))
            {
                NanoBombContext context = this.nanoBombContext[self];
                context.timer += Time.fixedDeltaTime * context.passive.ext_attackSpeedStat;
                Int32 count = 0;
                while (context.timer >= context.passive.ext_nanoBombInterval && count <= context.passive.ext_nanoBombMaxPerTick)
                {
                    count++;
                    context.passive.SkillCast(context.handle, (self is ChargeSolarFlare ? true : false));
                    context.timer -= context.passive.ext_nanoBombInterval;
                }
            }
        }
        /*private void BaseThrowBombState_OnEnter(On.EntityStates.Mage.Weapon.BaseThrowBombState.orig_OnEnter orig, BaseThrowBombState self)
        {
            orig(self);
            if (this.nanoBombContext.ContainsKey(self))
            {
                NanoBombContext context = this.nanoBombContext[self];

                Int32 count = 0;
                while (context.timer >= context.passive.ext_nanoBombInterval && count <= context.passive.ext_nanoBombMaxPerTick)
                {
                    count++;
                    context.passive.SkillCast(context.handle);
                    context.timer -= context.passive.ext_nanoBombInterval;
                }

                context.handle.Fire(context.passive.ext_nanoBombMinDelay, context.passive.ext_nanoBombMaxDelay);
                _ = this.nanoBombContext.Remove(self);
            }
        }*/
        private void BaseChargeBombState_OnExit(On.EntityStates.Mage.Weapon.BaseChargeBombState.orig_OnExit orig, BaseChargeBombState self)
        {
            orig(self);
            if (this.nanoBombContext.ContainsKey(self))
            {
                NanoBombContext context = this.nanoBombContext[self];

                Int32 count = 0;
                while (context.timer >= context.passive.ext_nanoBombInterval && count <= context.passive.ext_nanoBombMaxPerTick)
                {
                    count++;
                    context.passive.SkillCast(context.handle, (self is ChargeSolarFlare ? true : false));
                    context.timer -= context.passive.ext_nanoBombInterval;
                }

                context.handle.Fire(context.passive.ext_nanoBombMinDelay, context.passive.ext_nanoBombMaxDelay);
                _ = this.nanoBombContext.Remove(self);
            }
        }
        #endregion
        #region Fire/Lightning Bolt
        private void FireFireBolt_FireGauntlet(On.EntityStates.Mage.Weapon.FireFireBolt.orig_FireGauntlet orig, FireFireBolt self)
        {
            orig(self);
            GameObject obj = self.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out AltArtiPassive passive))
            {
                passive.SkillCast(isFire: !(self is FireLightningBolt) && !(self is FireSnowBall));
            }
        }
        #endregion
    }
}