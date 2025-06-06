using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ArtificerExtended.Components
{
    [RequireComponent(typeof(ProjectileController), typeof(Rigidbody))]
    class ProjectileRecallToOwner : NetworkBehaviour
    {
        public float delay;
        public float transitionDuration;
        public float returnSpeed;

		ProjectileController projectileController;
		Rigidbody rigidbody;

		float startSpeed;
        float stopwatch;

        [SyncVar]
        BoomerangProjectile.BoomerangState boomerangState = BoomerangProjectile.BoomerangState.FlyOut;
		#region networking i stole from ror2 code
		private void UNetVersion()
		{
		}
		public BoomerangProjectile.BoomerangState NetworkboomerangState
		{
			get
			{
				return this.boomerangState;
			}
			[param: In]
			set
			{
				ulong newValueAsUlong = (ulong)((long)value);
				ulong fieldValueAsUlong = (ulong)((long)this.boomerangState);
				base.SetSyncVarEnum<BoomerangProjectile.BoomerangState>(value, newValueAsUlong, ref this.boomerangState, fieldValueAsUlong, 1U);
			}
		}
		public override bool OnSerialize(NetworkWriter writer, bool forceAll)
		{
			if (forceAll)
			{
				writer.Write((int)this.boomerangState);
				return true;
			}
			bool flag = false;
			if ((base.syncVarDirtyBits & 1U) != 0U)
			{
				if (!flag)
				{
					writer.WritePackedUInt32(base.syncVarDirtyBits);
					flag = true;
				}
				writer.Write((int)this.boomerangState);
			}
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
			}
			return flag;
		}
		public override void OnDeserialize(NetworkReader reader, bool initialState)
		{
			if (initialState)
			{
				this.boomerangState = (BoomerangProjectile.BoomerangState)reader.ReadInt32();
				return;
			}
			int num = (int)reader.ReadPackedUInt32();
			if ((num & 1) != 0)
			{
				this.boomerangState = (BoomerangProjectile.BoomerangState)reader.ReadInt32();
			}
		}
		public override void PreStartClient()
		{
		}
        #endregion

        void Start()
        {
            projectileController = GetComponent<ProjectileController>();
			rigidbody = GetComponent<Rigidbody>();
			startSpeed = rigidbody.velocity.magnitude;
        }
        void FixedUpdate()
        {
            switch (this.boomerangState)
            {
                case BoomerangProjectile.BoomerangState.FlyOut:
                    stopwatch += Time.fixedDeltaTime;
                    if(stopwatch >= delay)
                    {
						stopwatch = 0;
						this.NetworkboomerangState = BoomerangProjectile.BoomerangState.Transition;
						return;
                    }
                    break;

                case BoomerangProjectile.BoomerangState.Transition:
                    stopwatch += Time.fixedDeltaTime;
					float delta = this.stopwatch / this.transitionDuration;
					Vector3 pullDirection = CalculatePullDirection();
					rigidbody.velocity = Vector3.Lerp(startSpeed * transform.forward, returnSpeed * pullDirection, delta);
					if (stopwatch >= transitionDuration)
					{
						this.NetworkboomerangState = BoomerangProjectile.BoomerangState.Transition;
						return;
					}
					break;

                case BoomerangProjectile.BoomerangState.FlyBack:
					Vector3 pullDirection2 = CalculatePullDirection();
					rigidbody.velocity = returnSpeed * pullDirection2;
					break;

				default:
                    return;
            }

            Vector3 CalculatePullDirection()
            {
                if (this.projectileController.owner)
                {
                    return (this.projectileController.owner.transform.position - base.transform.position).normalized;
                }
                return base.transform.forward;
            }
        }
    }
}
