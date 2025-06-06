using BepInEx.Configuration;
using BepInEx.Logging;
using ArtificerExtended.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;
using RoR2;
using ArtificerExtended.Unlocks;
using System.Reflection;

namespace ArtificerExtended
{
    public abstract class SharedBase
    {
        public virtual string BASE_TOKEN => TOKEN_PREFIX + TOKEN_IDENTIFIER;
        public abstract string TOKEN_IDENTIFIER { get; }
        public abstract string TOKEN_PREFIX { get; }
        public virtual bool lockEnabled { get; } = false;
        public abstract string ConfigName { get; }
        public virtual bool isEnabled { get; } = true;
        public virtual ConfigFile configFile { get; } = Config.MyConfig;
        public static ManualLogSource Logger => Log._logSource;
        public abstract AssetBundle assetBundle { get; }

        public virtual Type RequiredUnlock { get; }

        public abstract void Hooks();
        public abstract void Lang();

        public virtual void Init()
        {
            ConfigManager.HandleConfigAttributes(GetType(), ConfigName, configFile);
            Hooks();
            Lang();
        }

        public T Bind<T>(T defaultValue, string configName, string configDesc = "")
        {
            return ConfigManager.DualBindToConfig<T>(ConfigName, configFile, configName, defaultValue, configDesc);
        }
        public static float GetHyperbolic(float firstStack, float cap, float chance) // Util.ConvertAmplificationPercentageIntoReductionPercentage but Better :zanysoup:
        {
            if (firstStack >= cap) return cap * (chance / firstStack); // should not happen, but failsafe
            float count = chance / firstStack;
            float coeff = 100 * firstStack / (cap - firstStack); // should be good
            return cap * (1 - (100 / ((count * coeff) + 100)));
        }
    }
}
