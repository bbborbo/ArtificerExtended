using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AltArtificerExtended.Components
{
    public class Rotator : MonoBehaviour
    {
        private Single rotationTime;
        private Single rotationTimer;

        private Boolean rotating;
        private Boolean useTarget;

        private Vector3 basePosition;
        private Vector3 baseOffset;
        private Vector3 centerPoint = new Vector3(0f, 2f, 0f);

        private Quaternion baseRotation;
        private Quaternion target;
        private Quaternion rotStart;
        private Quaternion internalTarget;

        public void SetRotation(Quaternion target, Single time)
        {
            this.rotating = true;
            this.useTarget = true;
            this.rotationTime = time;
            this.rotationTimer = time;
            this.target = target;
            this.rotStart = base.transform.rotation;
        }

        public void ResetRotation(Single time)
        {
            this.rotating = true;
            this.useTarget = false;
            this.rotationTime = time;
            this.rotationTimer = time;
            this.rotStart = base.transform.rotation;
        }

        public void Awake()
        {
            this.baseRotation = base.transform.localRotation;
            this.basePosition = base.transform.localPosition;

            this.baseOffset = base.transform.InverseTransformPoint(base.transform.parent.TransformPoint(this.centerPoint));
        }

        public void LateUpdate()
        {
            if (!this.rotating)
            {
                return;
            }

            Single start = this.rotationTimer;
            this.rotationTimer -= Time.deltaTime;
            this.rotationTimer = Math.Max(0f, this.rotationTimer);
            _ = start - this.rotationTimer;

            this.internalTarget = this.useTarget ? this.target : base.transform.parent.rotation * this.baseRotation;

            base.transform.rotation = Quaternion.Lerp(this.rotStart, this.internalTarget, 1f - (this.rotationTimer / this.rotationTime));

            Vector3 idealCenter = base.transform.parent.TransformPoint(this.centerPoint);
            Vector3 currentCenter = base.transform.TransformPoint(this.baseOffset);
            Vector3 diff = idealCenter - currentCenter;

            base.transform.position += diff;

            if (this.rotationTimer <= 0.0f)
            {
                this.rotating = false;
                if (!this.useTarget)
                {
                    base.transform.localRotation = this.baseRotation;
                    base.transform.localPosition = this.basePosition;
                }
            }
        }
    }
}
