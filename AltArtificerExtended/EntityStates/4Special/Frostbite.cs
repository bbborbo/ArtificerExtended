using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ArtificerExtended.Passive;
using ArtificerExtended.Skills;
//using AlternativeArtificer.States.Main;
using EntityStates;
using EntityStates.Huntress;
using EntityStates.Mage.Weapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.EntityState
{
    public class Frostbite : BaseSkillState
    {
        public static GameObject novaEffectPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/AffixWhiteExplosion");

        public static float blizzardDamageCoefficient = 3f;
        public static float blizzardProcCoefficient = 1f;
        public static float blizzardRadius = ArtificerExtendedPlugin.meleeRangeChannel;

        public static float novaDamageCoefficient = 5f;
        public static float novaProcCoefficient = 1f;
        public static float novaRadius = ArtificerExtendedPlugin.meleeRangeChannel;

        private static float buffduration = _1FrostbiteSkill.blizzardBuffDuration;
        public static float baseDuration = 0.4f;
        public static float force = 1500;
        private float duration;

        public static string beginSoundString = PrepWall.prepWallSoundString;
        public static string endSoundString = "Play_mage_shift_wall_pre_explode_rumble";
        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = Frostbite.baseDuration / this.attackSpeedStat;

            if(ArtificerExtendedPlugin.AllowBrokenSFX.Value == true)
                Util.PlaySound(CastSnowstorm.beginSoundString, base.gameObject);
            base.PlayAnimation("Gesture, Additive", "PrepFlamethrower", "Flamethrower.playbackRate", this.duration);
        }

        public override void OnExit()
        {
            GameObject obj = base.outer.gameObject;

            //this.CastBlizzard();
            
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                passive.SkillCast();
            }

            base.PlayAnimation("Gesture, Additive", "FireWall");
            base.characterBody.AddTimedBuffAuthority(_1FrostbiteSkill.artiIceShield.buffIndex, buffduration);
            if (NetworkServer.active)
                InflictSnow();

            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration)
            {
                if (base.isAuthority)
                {
                    outer.SetNextStateToMain();
                    return;
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }

        public void InflictSnow()
        {
            EffectManager.SpawnEffect(CastSnowstorm.novaEffectPrefab, new EffectData
            {
                origin = base.transform.position,
                scale = Frostbite.blizzardRadius
            }, true);
            BlastAttack blastAttack = new BlastAttack();
            blastAttack.radius = Frostbite.blizzardRadius;
            blastAttack.procCoefficient = Frostbite.blizzardProcCoefficient;
            blastAttack.position = base.transform.position;
            blastAttack.attacker = base.gameObject;
            blastAttack.crit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
            blastAttack.baseDamage = base.characterBody.damage * Frostbite.blizzardDamageCoefficient;
            blastAttack.falloffModel = BlastAttack.FalloffModel.None;
            blastAttack.damageType = DamageType.Generic;
            blastAttack.baseForce = force;
            blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
            blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;

            blastAttack.AddModdedDamageType(ChillRework.ChillRework.ChillOnHit);

            blastAttack.Fire();
        }
    }
}
