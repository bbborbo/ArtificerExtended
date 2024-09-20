using BepInEx;
using System;
using System.Security.Permissions;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable 
namespace JetHack
{
    [BepInPlugin(guid, modName, version)]
    public class JetHackPlugin : BaseUnityPlugin
    {
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "JetHack";
        public const string version = "1.0.0";

        public static bool hoverStateCache = false;
        void Awake()
        {
            On.EntityStates.Mage.MageCharacterMain.OnEnter += RestoreHoverState;
            On.EntityStates.Mage.MageCharacterMain.OnExit += CacheHoverState;
            On.EntityStates.Mage.FlyUpState.OnExit += OverrideHoverStateCache;
        }

        private void OverrideHoverStateCache(On.EntityStates.Mage.FlyUpState.orig_OnExit orig, EntityStates.Mage.FlyUpState self)
        {
            orig(self);
            if (self.isAuthority)
            {
                hoverStateCache = true;
            }
        }

        private void CacheHoverState(On.EntityStates.Mage.MageCharacterMain.orig_OnExit orig, EntityStates.Mage.MageCharacterMain self)
        {
            if (self.isAuthority)
            {
                hoverStateCache = self.jumpButtonState;
            }
            orig(self);
        }

        private void RestoreHoverState(On.EntityStates.Mage.MageCharacterMain.orig_OnEnter orig, EntityStates.Mage.MageCharacterMain self)
        {
            if (self.isAuthority)
            {
                self.jumpButtonState = hoverStateCache;
            }
            orig(self);
        }
    }
}
