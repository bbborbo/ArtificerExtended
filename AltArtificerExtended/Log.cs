using BepInEx.Logging;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;

namespace ArtificerExtended
{
    internal static class Log
    {
        public static bool enableDebugging;
        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            enableDebugging = Modules.ConfigManager.DualBindToConfig<bool>(
                "Artificer Extended", Modules.Config.MyConfig, "Enable Debugging", false,
                "Enable debug outputs to the log for troubleshooting purposes. Enabling this will slow down the game.");
            _logSource = logSource;
        }
        internal static string Combine(params string[] parameters)
        {
            string s = $"{ArtificerExtendedPlugin.modName} : ";
            foreach (string s2 in parameters)
            {
                s += $"{s2} : ";
            }
            return s;
        }
        internal static void Debug(object data) 
        {
            if (enableDebugging)
                _logSource.LogDebug(data);
        } 
        internal static void Error(object data) => _logSource.LogError(data);
        internal static void ErrorAssetBundle(string assetName, string bundleName) =>
            Log.Error($"failed to load asset, {assetName}, because it does not exist in asset bundle, {bundleName}");        
        internal static void Fatal(object data) => _logSource.LogFatal(data);
        internal static void Info(object data) => _logSource.LogInfo(data);
        internal static void Message(object data) => _logSource.LogMessage(data);
        internal static void Warning(object data) => _logSource.LogWarning(data);
    }
}