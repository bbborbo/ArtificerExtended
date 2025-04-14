using R2API;
using System;
using System.Collections.Generic;
using System.IO;

namespace ArtificerExtended.Modules {
    internal static class Language
    {
        public static class Styling
        {
            public static string ConvertDecimal(float value)
            {
                return (value * 100).ToString() + "%";
            }
            public static string DamageColor(string text)
            {
                return $"<style=cIsDamage>{text}</style>";
            }
            public static string HealingColor(string text)
            {
                return $"<style=cIsHealing>{text}</style>";
            }
            public static string DamageValueText(float value)
            {
                return DamageColor(ConvertDecimal(value) + " damage");
            }
            public static string UtilityColor(string text)
            {
                return $"<style=cIsUtility>{text}</style>";
            }
            public static string RedText(string text) => HealthColor(text);
            public static string HealthColor(string text)
            {
                return $"<style=cIsHealth>{text}</style>";
            }
            public static string KeywordText(string keyword, string sub)
            {
                return $"<style=cKeywordName>{keyword}</style><style=cSub>{sub}</style>";
            }
            public static string ScepterDescription(string desc)
            {
                return $"\n<color=#d299ff>SCEPTER: {desc}</color>";
            }
            public static string VoidColor(string text)
            {
                return $"<style=cIsVoid>{text}</style>";
            }
            public static string StackText(string text)
            {
                return StackColor($"({text} per stack)");
            }
            public static string StackColor(string text)
            {
                return $"<style=cStack>{text}</style>";
            }

            public static string GetAchievementNameToken(string identifier)
            {
                return $"ACHIEVEMENT_{identifier.ToUpperInvariant()}_NAME";
            }
            public static string GetAchievementDescriptionToken(string identifier)
            {
                return $"ACHIEVEMENT_{identifier.ToUpperInvariant()}_DESCRIPTION";
            }

            public static string NumToAdj(int num)
            {
                switch (num)
                {
                    default:
                        return num + "th";
                    case 1:
                        return num + "st";
                    case 2:
                        return num + "nd";
                    case 3:
                        return num + "rd";
                }
            }
        }

        public static string TokensOutput = "";

        public static bool usingLanguageFolder = false;

        public static bool printingEnabled = false;

        public static void Init() {
            if (usingLanguageFolder) {
                RoR2.Language.collectLanguageRootFolders += Language_collectLanguageRootFolders;
            }
        }

        private static void Language_collectLanguageRootFolders(List<string> obj) {
            string path = Path.Combine(Path.GetDirectoryName(ArtificerExtendedPlugin.instance.Info.Location), "Language");
            if (Directory.Exists(path)) {
                obj.Add(path);
            }
        }

        public static void Add(string token, string text) {
            if (!usingLanguageFolder) {
                LanguageAPI.Add(token, text);
            }

            if (!printingEnabled) return;

            //add a token formatted to language file
            TokensOutput += $"\n    \"{token}\" : \"{text.Replace(Environment.NewLine, "\\n").Replace("\n", "\\n")}\",";
        }

        public static void TryPrintOutput(string fileName = "")
        {
            if (usingLanguageFolder && printingEnabled)
            {
                PrintOutput(fileName);
            }
        }
        public static void PrintOutput(string fileName = "") {
            if (!printingEnabled) return;

            //wrap all tokens in a properly formatted language file
            string strings = $"{{\n    strings:\n    {{{TokensOutput}\n    }}\n}}";

            //spit out language dump in console for copy paste if you want
            Log.Message($"{fileName}: \n{strings}");

            //write a language file next to your mod. must have a folder called Language next to your mod dll.
            if (!string.IsNullOrEmpty(fileName)) {
                string path = Path.Combine(Directory.GetParent(ArtificerExtendedPlugin.instance.Info.Location).FullName, "Language", "en", fileName);
                File.WriteAllText(path, strings);
            }

            //empty the output each time this is printed, so you can print multiple language files
            TokensOutput = "";
        }
    }
}