using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using EntityStates.Mage;
using R2API;
using R2API.Utils;
using RoR2;

using UnityEngine;
using UnityEngine.Networking;
using ArtificerExtended.Passive;

namespace ArtificerExtended.EntityState
{
    public class IonSurgeExtendedBase : GenericCharacterMain
    {
        public override void OnEnter()
        {
            base.OnEnter();
            if (!base.characterBody.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage))
            {
                base.characterBody.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
            }
        }
        public override void OnExit()
        {
            base.OnExit();
            if (base.characterBody.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage))
            {
                base.characterBody.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
            }
        }
    }
}
