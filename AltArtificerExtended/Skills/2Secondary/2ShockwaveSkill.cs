using System;
using EntityStates;
using RoR2;
using RoR2.Skills;
using BepInEx.Configuration;
using ArtificerExtended.Unlocks;
using UnityEngine;
using ArtificerExtended.States;
using RoR2.Projectile;
using R2API;
using R2API.Utils;
using ArtificerExtended.Modules;

namespace ArtificerExtended.Skills
{
    class _2ShockwaveSkill : SkillBase
    {
        public static GameObject shockwaveZapConePrefab;
        public static int shockwaveMaxAngleFilter = 35;
        public static float shockwaveMaxRange = 20;

        public override string SkillName => "Shockwave";

        public override string SkillDescription => $"<style=cIsDamage>Stunning.</style> Burst forward, producing a powerful shockwave " +
            $"that chains lightning for <style=cIsDamage>2x{Tools.ConvertDecimal(FireShockwave.damage)} damage.</style>";

        public override string TOKEN_IDENTIFIER => "SHOCKWAVE";

        public override Type RequiredUnlock => (typeof(OverkillOverloadingUnlock));

        public override MageElement Element => MageElement.Lightning;

        public override Type ActivationState => typeof(CastShockwave);

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                mustKeyPress: true
            );
        public override Sprite Icon => LoadSpriteFromBundle("shockwaveicon");
        public override SkillSlot SkillSlot => SkillSlot.Secondary;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;
        public override Type BaseSkillDef => typeof(SkillDef);
        public override float BaseCooldown => 5;
        public override void Init()
        {
            return;
            //KeywordTokens = new string[1] { "KEYWORD_STUNNING" };
            //
            //RegisterEntityState(typeof(FireShockwave));
            //RegisterEntityState(typeof(FireShockwaveVisuals));
            //
            //RegisterProjectileShockwave();
            base.Init();
        }


        //public override string[] KeywordTokens = new string[1] { "KEYWORD_STUNNING" };

        public override void Hooks()
        {
        }

        private void RegisterProjectileShockwave()
        {
            shockwaveZapConePrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LoaderZapCone").InstantiateClone("shockwaveZapCone", true);

            ProjectileProximityBeamController ppbc = shockwaveZapConePrefab.GetComponent<ProjectileProximityBeamController>();
            ppbc.attackRange = shockwaveMaxRange - 5;
            ppbc.maxAngleFilter = shockwaveMaxAngleFilter;
            ppbc.damageCoefficient = 0; // 10;
            ppbc.procCoefficient = 1;
            ppbc.attackInterval = 0.15f;

            ShakeEmitter shake = shockwaveZapConePrefab.AddComponent<ShakeEmitter>();
            shake.radius = 80; //40
            shake.duration = 0.3f; //0.2f
            shake.shakeOnEnable = false;
            shake.shakeOnStart = true;
            shake.amplitudeTimeDecay = true;
            shake.scaleShakeRadiusWithLocalScale = false;

            shockwaveZapConePrefab.GetComponent<DestroyOnTimer>().duration = 2f;

            Content.AddProjectilePrefab(shockwaveZapConePrefab);
        }
    }
}
