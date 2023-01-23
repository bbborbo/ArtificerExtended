using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ArtificerExtended.Helpers
{
    public class InstancedRandom
    {
        private UnityEngine.Random.State localState;
        private UnityEngine.Random.State globalState;

        public InstancedRandom(Int32 seed)
        {
            this.globalState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            this.localState = UnityEngine.Random.state;
            UnityEngine.Random.state = this.globalState;
        }
        public Vector2 InsideUnitCircle()
        {
            this.CacheState();
            Vector2 temp = UnityEngine.Random.insideUnitCircle;
            this.RestoreState();
            return temp;
        }
        public Vector3 InsideUnitSphere()
        {
            this.CacheState();
            Vector3 temp = UnityEngine.Random.insideUnitSphere;
            this.RestoreState();
            return temp;
        }
        public Vector3 OnUnitSphere()
        {
            this.CacheState();
            Vector3 temp = UnityEngine.Random.onUnitSphere;
            this.RestoreState();
            return temp;
        }
        public Quaternion Rotation()
        {
            this.CacheState();
            Quaternion temp = UnityEngine.Random.rotation;
            this.RestoreState();
            return temp;
        }
        public Quaternion RotationUniform()
        {
            this.CacheState();
            Quaternion temp = UnityEngine.Random.rotationUniform;
            this.RestoreState();
            return temp;
        }
        public Single Value()
        {
            this.CacheState();
            Single temp = UnityEngine.Random.value;
            this.RestoreState();
            return temp;
        }
        public Single Range(Single min, Single max)
        {
            this.CacheState();
            Single temp = UnityEngine.Random.Range(min, max);
            this.RestoreState();
            return temp;
        }
        public Color ColorHSV()
        {
            this.CacheState();
            Color temp = UnityEngine.Random.ColorHSV();
            this.RestoreState();
            return temp;
        }
        public Color ColorHSV(Single hueMin, Single hueMax)
        {
            this.CacheState();
            Color temp = UnityEngine.Random.ColorHSV(hueMin, hueMax);
            this.RestoreState();
            return temp;
        }
        public Color ColorHSV(Single hueMin, Single hueMax, Single saturationMin, Single saturationMax)
        {
            this.CacheState();
            Color temp = UnityEngine.Random.ColorHSV(hueMin, hueMax, saturationMin, saturationMax);
            this.RestoreState();
            return temp;
        }
        public Color ColorHSV(Single hueMin, Single hueMax, Single saturationMin, Single saturationMax, Single valueMin, Single valueMax)
        {
            this.CacheState();
            Color temp = UnityEngine.Random.ColorHSV(hueMin, hueMax, saturationMin, saturationMax, valueMin, valueMax);
            this.RestoreState();
            return temp;
        }
        public Color ColorHSV(Single hueMin, Single hueMax, Single saturationMin, Single saturationMax, Single valueMin, Single valueMax, Single alphaMin, Single alphaMax)
        {
            this.CacheState();
            Color temp = UnityEngine.Random.ColorHSV(hueMin, hueMax, saturationMin, saturationMax, valueMin, valueMax, alphaMin, alphaMax);
            this.RestoreState();
            return temp;
        }

        private void CacheState()
        {
            this.globalState = UnityEngine.Random.state;
            UnityEngine.Random.state = this.localState;
        }

        private void RestoreState()
        {
            this.localState = UnityEngine.Random.state;
            UnityEngine.Random.state = this.globalState;
        }
    }
}
