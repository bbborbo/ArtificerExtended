using System;
using System.Collections.Generic;
using System.Text;

using RoR2;

using UnityEngine;

namespace AltArtificerExtended.Components
{

    public class SoundOnAwake : MonoBehaviour
    {
        public String sound;
        public void Awake() => Util.PlaySound(this.sound, base.gameObject);
    }
}
