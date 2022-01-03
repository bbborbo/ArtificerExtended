using System.Security;
using System.Security.Permissions;
using RoR2;
using RoR2.Skills;
using System;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using EntityStates.Mage;
using EntityStates.Mage.Weapon;
using R2API;
using R2API.Utils;
using System.Collections.Generic;
using AltArtificerExtended.Skills;
using AltArtificerExtended.Unlocks;
using System.Reflection;
using System.Linq;
using EntityStates;
using AltArtificerExtended.Passive;
using AltArtificerExtended.Components;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 

namespace AltArtificerExtended.Borbo
{
    [BepInDependency("com.Borbo.BORBO", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.ArtificerExtended", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("com.Borbo.ArtificerExtendedStringsAttachment", "This Loads An Extension For ArtificerExtended That Makes Some Strings Change If You Dont Have BalanceOverhaulRBO Installed", "0.0.0")]
    public class BorboDependentLangStrings : BaseUnityPlugin
    { 
        void Awake()
        {
            if (!Tools.isLoaded("com.Borbo.BORBO"))
            {
                LanguageAPI.Add("ITEM_ICERING_DESC",
                    $"Hits that deal <style=cIsDamage>more than 400% damage</style> also blasts enemies with a " +
                    $"<style=cIsDamage>runic ice blast</style>, " +
                    $"<style=cIsUtility>Chilling</style> them for <style=cIsUtility>3s</style> <style=cStack>(+3s per stack)</style> and " +
                    $"dealing <style=cIsDamage>250%</style> <style=cStack>(+250% per stack)</style> TOTAL damage. " +
                    $"Recharges every <style=cIsUtility>10</style> seconds.");

                LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_CHILL", "<style=cKeywordName>Chilling</style>" +
                    $"<style=cSub>Has a chance to temporarily <style=cIsUtility>slow movement speed</style> by <style=cIsDamage>80%.</style></style>");
                return;
            }
        }
    }
}