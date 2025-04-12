using ArtificerExtended.Passive;
using EntityStates;
using EntityStates.Toolbot;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.States
{
    class PolarVortexStart : PolarVortexBase
    {
        public static float baseDurationEnter = 1f;
        public static float baseDurationExit = 0.6f;
        float duration;
        public bool exiting = false;

        public override void OnEnter()
        {
            base.OnEnter();
            // determine duration
            duration = (exiting ? baseDurationExit : baseDurationEnter) / this.attackSpeedStat;
            base.StopSkills();

            GameObject obj = base.outer.gameObject;
            if (AltArtiPassive.instanceLookup.TryGetValue(obj, out var passive))
            {
                passive.SkillCast();
            }
            // play animation
            if (!exiting)
                base.PlayAnimation("Gesture, Additive", "PrepWall", "PrepWall.playbackRate", this.duration);
        }
        public override void OnExit()
        {
            base.OnExit();
            // cast nova
            if(NetworkServer.active)
                InflictSnow();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(fixedAge >= duration)
            {
                //play exit animation
                base.PlayAnimation("Gesture, Additive", "FireWall");
                if (isAuthority)
                {
                    //InflictSnow();
                    SetNextState();
                }
            }
        }

        protected override void SetNextState()
        {
            if(exiting)
            {
                base.SetNextState();
                return;
            }

            continuing = true;
            outer.SetNextState(new PolarVortex
            {
                addedFallImmunity = this.addedFallImmunity,
                activatorSkillSlot = this.activatorSkillSlot
            });
        }
    }
}
