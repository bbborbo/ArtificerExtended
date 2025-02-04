using System;
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
using static ChillRework.ChillRework;
using static R2API.RecalculateStatsAPI;
using ArtificerExtended.CoreModules;
using ArtificerExtended.EntityState;
using RoR2.Projectile;
using RoR2.Orbs;

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
            OnMaxChill += FrostNovaOnMaxChill;
            GetStatCoefficients += MeltAttackSpeedBuff;

            On.RoR2.GlobalEventManager.OnHitAll += ChainLightningHook;
        }

        private static void ChainLightningHook(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            if (damageInfo.HasModdedDamageType(CoreModules.Assets.ChainLightning))
            {
                LightningOrb lightningOrb2 = new LightningOrb();
                lightningOrb2.origin = damageInfo.position;
                lightningOrb2.damageValue = damageInfo.damage * CoreModules.Assets.zapDamageFraction;
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
                lightningOrb2.procCoefficient = 0.2f;
                lightningOrb2.lightningType = LightningOrb.LightningType.Ukulele;
                lightningOrb2.damageColorIndex = DamageColorIndex.Default;
                lightningOrb2.range = CoreModules.Assets.zapDistance;
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
            int meltBuffCount = sender.GetBuffCount(Buffs.meltBuff);
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
        private struct FreezeInfo
        {
            public GameObject frozenBy;
            public Vector3 frozenAt;

            public FreezeInfo(GameObject frozenBy, Vector3 frozenAt)
            {
                this.frozenAt = frozenAt;
                this.frozenBy = frozenBy;
            }
        }

        private readonly Dictionary<GameObject, GameObject> frozenBy = new Dictionary<GameObject, GameObject>();

        private void GlobalEventManager_OnCharacterDeath(DamageReport damageReport)
        {
            if (NetworkServer.active)
            {
                if(damageReport != null)
                {
                    CharacterBody aBody = damageReport.attackerBody;
                    CharacterBody vBody = damageReport.victimBody;
                    if (vBody && aBody && vBody.healthComponent)
                    {
                        Power icePower = GetIcePowerLevelFromBody(aBody);

                        int chillDebuffCount = vBody.GetBuffCount(RoR2Content.Buffs.Slow80);
                        int chillLimitCount = vBody.GetBuffCount(ChillRework.ChillRework.ChillLimitBuff);
                        int minChillForBlast = chillLimitCount > 0 ? 5 : 1;

                        if (chillDebuffCount >= minChillForBlast && icePower > 0) 
                        {
                            float novaChance = Mathf.Pow(chillDebuffCount / 10, 1) * 100;
                            if (Util.CheckRoll(novaChance, damageReport.attackerMaster))
                            {
                                //Arctic Blast
                                AltArtiPassive.DoNova(aBody, icePower, damageReport.victim.transform.position, chillDebuffCount);
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
            if (damageInfo.damageType.damageType.HasFlag(DamageType.Freeze2s))
            {
                this.frozenBy[self.gameObject] = damageInfo.attacker;
            }
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

        private void FrostNovaOnMaxChill(CharacterBody aBody, CharacterBody vBody)
        {
            if(aBody != null)
            {
                Power icePower = GetIcePowerLevelFromBody(aBody);
                if (icePower > Power.None) //Arctic Blast
                {
                    AltArtiPassive.DoNova(aBody, icePower, vBody.corePosition);
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
                context.timer += Time.fixedDeltaTime * context.passive.ext_attackSpeedStat;
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
                    context.passive.SkillCast(context.handle);
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