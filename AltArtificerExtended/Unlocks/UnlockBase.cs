using ArtificerExtended;
using ArtificerExtended.Modules;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Achievements;
using RoR2.Stats;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ArtificerExtended.Unlocks
{
    public abstract class UnlockBase<T> : UnlockBase where T : UnlockBase<T>
    {
        public static T instance { get; private set; }

        public UnlockBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting UnlockBase was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class UnlockBase : BaseAchievement
    {
        public static UnlockableDef CreateUnlockDef(Type RequiredUnlock, Sprite icon)
        {
            string name = RequiredUnlock.Name;
            string nameUpper = name.ToUpperInvariant();
            UnlockableDef unlockDef = Content.CreateAndAddUnlockbleDef(name, name, icon);

            string nameToken = "ACHIEVEMENT_" + nameUpper + "_NAME";
            string descToken = "ACHIEVEMENT_" + nameUpper + "_DESCRIPTION";
            LanguageAPI.Add(nameToken, RequiredUnlock.GetPropertyValue<string>(nameof(UnlockBase.AchievementName)));
            LanguageAPI.Add(descToken, RequiredUnlock.GetPropertyValue<string>(nameof(UnlockBase.AchievementDesc)));
            unlockDef.getHowToUnlockString = (() => RoR2.Language.GetStringFormatted("UNLOCK_VIA_ACHIEVEMENT_FORMAT", new object[]
                            {
                                RoR2.Language.GetString(nameToken),
                                RoR2.Language.GetString(descToken)
                            }));
            unlockDef.getUnlockedString = (() => RoR2.Language.GetStringFormatted("UNLOCKED_FORMAT", new object[]
                            {
                                RoR2.Language.GetString(nameToken),
                                RoR2.Language.GetString(descToken)
                            }));             
            
            //RequiredUnlock.InvokeMethod(nameof(AddLang));
            //MethodInfo baseMethod = typeof(UnlockBase).GetMethod(nameof(UnlockBase.AddLang));
            //baseMethod.Invoke(RequiredUnlock, new object[] { });
            //baseMethod.MakeGenericMethod(new Type[] { RequiredUnlock }).Invoke(null, new object[] { });

            return unlockDef;
        }
        public abstract string TOKEN_IDENTIFIER { get; }
        public abstract string AchievementName { get; }
        public abstract string AchievementDesc { get; }
        public void AddLang()
        {
            LanguageAPI.Add("ACHIEVEMENT_" + TOKEN_IDENTIFIER + "_NAME", AchievementName);
            LanguageAPI.Add("ACHIEVEMENT_" + TOKEN_IDENTIFIER + "_DESCRIPTION", AchievementDesc);
        }
        public override BodyIndex LookUpRequiredBodyIndex()
        {
            return BodyCatalog.FindBodyIndex("MageBody");
        }

        public static StatDef GetCareerStatTotal(string name)
        {
            StatDef stat = StatDef.Find(name);
            if (stat == null)
            {
                stat = StatDef.Register(name, StatRecordType.Sum, StatDataType.ULong, 0.0, null);
            }
            return stat;
        }
    }
}
