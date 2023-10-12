using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Text;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable 
namespace ArtificerExtended
{
    public static class VRStuff
    {
        public static bool VRInstalled = false;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void SetupVR()
        {
            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.DrBibop.VRAPI"))
            {
                VRInstalled = VRAPI.VR.enabled && VRAPI.MotionControls.enabled;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static Ray GetVRHandAimRay(bool dominant)
        {
            return (dominant) ? VRAPI.MotionControls.dominantHand.aimRay : VRAPI.MotionControls.nonDominantHand.aimRay;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AnimateVRHand(bool dominant, string triggerName)
        {
            var hand = (dominant) ? VRAPI.MotionControls.dominantHand : VRAPI.MotionControls.nonDominantHand;
            hand.animator.SetTrigger(triggerName);
        }
    }
}
