using BepInEx;
using R2API;
using R2API.Utils;
using System;

namespace ThunderSurge
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID)]
    [BepInDependency(R2API.LoadoutAPI.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [BepInDependency(R2API.UnlockableAPI.PluginGUID)]
    [BepInDependency(R2API.DamageAPI.PluginGUID)]

    [BepInDependency(JetHack.JetHackPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]
    [R2APISubmoduleDependency(nameof(UnlockableAPI), nameof(LanguageAPI), nameof(LoadoutAPI), nameof(PrefabAPI), nameof(DamageAPI))]
    [BepInPlugin(guid, modName, version)]
    public class ThunderSurgePlugin : BaseUnityPlugin
    {
        public const string guid = "com." + teamName + "." + modName;
        public const string modName = "ThunderSurge";
        public const string teamName = "RiskOfBrainrot";
        public const string version = "1.0.0";
    }
}
