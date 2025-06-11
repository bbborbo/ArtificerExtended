using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Components
{
    [RequireComponent(typeof(AntiGravityForce))]
    class GravityAfterDuration : MonoBehaviour
    {
        public float antiGravCoefficient = 0;
        public float durationBeforeGravity = 0f;
        internal AntiGravityForce antiGrav;
        float stopwatch = 0;
        void Awake()
        {
            if(!antiGrav)
                antiGrav = GetComponent<AntiGravityForce>();
            if (antiGrav)
                antiGrav.antiGravityCoefficient = 1;
        }
        void FixedUpdate()
        {
            stopwatch += Time.fixedDeltaTime;
            if(stopwatch >= durationBeforeGravity && antiGrav)
            {
                antiGrav.antiGravityCoefficient = antiGravCoefficient;
            }
        }
        void OnEnable()
        {
            stopwatch = 0;
        }
        void OnDisable()
        {
            stopwatch = 0;
        }
    }
}
