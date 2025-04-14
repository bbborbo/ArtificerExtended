using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace ArtificerExtended.Modules
{
    //thanks to hifu/pseudopulse for config attribute
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AutoConfigAttribute : Attribute
    {
        public string name;
        public string desc;
        public object defaultValue;

        public AutoConfigAttribute(string name, object defaultValue) => Init(name, string.Empty, defaultValue);
        public AutoConfigAttribute(string name, string desc, object defaultValue) => Init(name, desc, defaultValue);
        public void Init(string name, string desc, object defaultValue)
        {
            this.name = name;
            this.desc = desc;
            this.defaultValue = defaultValue;
        }
    }

    public static class Config
    {
        public static ConfigFile MyConfig;
        public static ConfigFile BackupConfig;

        public static void Init()
        {
            MyConfig = new ConfigFile(Paths.ConfigPath + $"\\{ArtificerExtendedPlugin.guid}.cfg", true); //FortunesPlugin.instance.Config;
            BackupConfig = new ConfigFile(Paths.ConfigPath + $"\\{ArtificerExtendedPlugin.guid}.Backup.cfg", true);
            BackupConfig.Bind(": DO NOT MODIFY THIS FILES CONTENTS :", ": DO NOT MODIFY THIS FILES CONTENTS :", ": DO NOT MODIFY THIS FILES CONTENTS :", ": DO NOT MODIFY THIS FILES CONTENTS :");
        }

        /// <summary>
        /// automatically makes config entries for disabling survivors
        /// </summary>
        /// <param name="section"></param>
        /// <param name="characterName"></param>
        /// <param name="description"></param>
        /// <param name="enabledByDefault"></param>
        public static ConfigEntry<bool> CharacterEnableConfig(string section, string characterName, string description = "", bool enabledByDefault = true)
        {

            if (string.IsNullOrEmpty(description))
            {
                description = "Set to false to disable this character and as much of its code and content as possible";
            }
            return BindAndOptions<bool>(section,
                                        "Enable " + characterName,
                                        enabledByDefault,
                                        description,
                                        true);
        }

        public static ConfigEntry<T> BindAndOptions<T>(string section, string name, T defaultValue, string description = "", bool restartRequired = false) =>
            BindAndOptions<T>(section, name, defaultValue, 0, 20, description, restartRequired);
        public static ConfigEntry<T> BindAndOptions<T>(string section, string name, T defaultValue, float min, float max, string description = "", bool restartRequired = false)
        {
            if (string.IsNullOrEmpty(description))
            {
                description = name;
            }

            if (restartRequired)
            {
                description += " (restart required)";
            }
            ConfigEntry<T> configEntry = MyConfig.Bind(section, name, defaultValue, description);

            if (BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.rune580.riskofoptions"))
            {
                TryRegisterOption(configEntry, min, max, restartRequired);
            }

            return configEntry;
        }

        //back compat
        public static ConfigEntry<float> BindAndOptionsSlider(string section, string name, float defaultValue, string description, float min = 0, float max = 20, bool restartRequired = false) =>
            BindAndOptions<float>(section, name, defaultValue, min, max, description, restartRequired);

        //add risk of options dll to your project libs and uncomment this for a soft dependency
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void TryRegisterOption<T>(ConfigEntry<T> entry, float min, float max, bool restartRequired)
        {
            //if (entry is ConfigEntry<float>)
            //{
            //    ModSettingsManager.AddOption(new SliderOption(entry as ConfigEntry<float>, new SliderConfig() { min = min, max = max, formatString = "{0:0.00}", restartRequired = restartRequired }));
            //}
            //if (entry is ConfigEntry<int>)
            //{
            //    ModSettingsManager.AddOption(new IntSliderOption(entry as ConfigEntry<int>, new IntSliderConfig() { min = (int)min, max = (int)max, restartRequired = restartRequired }));
            //}
            //if (entry is ConfigEntry<bool>)
            //{
            //    ModSettingsManager.AddOption(new CheckBoxOption(entry as ConfigEntry<bool>, restartRequired));
            //}
            //if (entry is BepInEx.Configuration.ConfigEntry<KeyboardShortcut>)
            //{
            //    ModSettingsManager.AddOption(new KeyBindOption(entry as ConfigEntry<KeyboardShortcut>, restartRequired));
            //}
        }

        //Taken from https://github.com/ToastedOven/CustomEmotesAPI/blob/main/CustomEmotesAPI/CustomEmotesAPI/CustomEmotesAPI.cs
        public static bool GetKeyPressed(KeyboardShortcut entry)
        {
            foreach (var item in entry.Modifiers)
            {
                if (!Input.GetKey(item))
                {
                    return false;
                }
            }
            return Input.GetKeyDown(entry.MainKey);
        }
    }

    //thanks to hifu/pseudopulse for config manager
    public class ConfigManager
    {
        internal static bool ConfigChanged = false;
        internal static bool VersionChanged = false;
        public static void HandleConfigAttributes(Type type, string section, ConfigFile config)
        {
            TypeInfo info = type.GetTypeInfo();

            foreach (FieldInfo field in info.GetFields())
            {
                //only works on static fields
                if (!field.IsStatic) continue;

                Type t = field.FieldType;
                AutoConfigAttribute configattr = field.GetCustomAttribute<AutoConfigAttribute>();
                //ignore fields that arent marked as autoconfigurable
                if (configattr == null) continue;

                string configName = configattr.name;
                object defaultValue = configattr.defaultValue;
                string configDesc = configattr.desc;

                field.SetValue(null, DualBindToConfig(t, section, config, configName, defaultValue, configDesc));
            }
        }

        private static object DualBindToConfig(Type t, string section, ConfigFile config, string configName, object defaultValue, string configDesc)
        {
            if (string.IsNullOrWhiteSpace(section) || string.IsNullOrWhiteSpace(configName))
                return defaultValue;
            ConfigDescription desc = new ConfigDescription(configDesc);

            //get the method for binding to a config file
            MethodInfo method = typeof(ConfigFile).GetMethods().Where(x => x.Name == nameof(ConfigFile.Bind)).First();
            method = method.MakeGenericMethod(t);

            //bind to chosen config file
            ConfigEntryBase configValue = (ConfigEntryBase)method.Invoke(config,
                new object[] {
                        new ConfigDefinition(section, configName),
                        defaultValue,
                        desc
                });

            //bind to backup config for versioning
            ConfigEntryBase backupValue = (ConfigEntryBase)method.Invoke(Config.BackupConfig,
                new object[] {
                        new ConfigDefinition(Regex.Replace(config.ConfigFilePath, "\\W", "") + " : " + section, configName),
                        defaultValue,
                        desc
                });
            
            //if the backup config's (new) default value is different from its current (set) value
            if (!ConfigEqual(backupValue.DefaultValue, backupValue.BoxedValue))
            {
                if (true)//VersionChanged)
                {
                    Log.Warning("Syncing config to new version");
                    //reset set values to new defaults
                    configValue.BoxedValue = configValue.DefaultValue;
                    backupValue.BoxedValue = backupValue.DefaultValue;
                }
            }
            if (!ConfigEqual(configValue.DefaultValue, configValue.BoxedValue)) ConfigChanged = true;
            return configValue.BoxedValue;
        }

        public static T DualBind<T>(string section, string configName, T defaultValue, string configDesc)
        {
            return (T)DualBindToConfig(typeof(T), section, Config.MyConfig, configName, defaultValue, configDesc);
        }
        public static T DualBindToConfig<T>(string section, ConfigFile config, string configName, T defaultValue, string configDesc)
        {
            return (T)DualBindToConfig(typeof(T), section, config, configName, defaultValue, configDesc);
        }

        private static bool ConfigEqual(object a, object b)
        {
            if (a.Equals(b)) return true;
            float fa, fb;
            if (float.TryParse(a.ToString(), out fa) && float.TryParse(b.ToString(), out fb) && Mathf.Abs(fa - fb) < 0.0001) return true;
            return false;
        }
    }
}
