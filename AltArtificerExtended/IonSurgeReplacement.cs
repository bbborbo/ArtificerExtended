using ArtificerExtended.Components;
using ArtificerExtended.EntityState;
using ArtificerExtended.Skills;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ArtificerExtended
{
    partial class ArtificerExtendedPlugin
    {
        public const string ThunderSurgeHitBoxGroupName = "Charge";

        #region replacement initialization
        public void ReplaceVanillaIonSurge(bool shouldReworkSurge)
        {
            SkillDef surge = Addressables.LoadAssetAsync<SkillDef>("RoR2/Base/Mage/MageBodyFlyUp.asset").WaitForCompletion();
            if (surge != null)
            {
                Debug.Log("Changing ion surge");

                //SkillDef newSurge = CloneSkillDef(surge);
                if (shouldReworkSurge)
                {
                    ModifyVanillaIonSurge(surge);
                }
                else
                {
                    SkillBase.RegisterEntityState(typeof(EntityState.VanillaIonSurge));
                    surge.activationState = new SerializableEntityStateType(typeof(EntityState.VanillaIonSurge));
                }
            }

            if (isScepterLoaded)
            {
                ReplaceScepterIonSurge(shouldReworkSurge, surge);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public void ReplaceScepterIonSurge(bool shouldReworkSurge, SkillDef surgeSkillDef)
        {
            if(!shouldReworkSurge || surgeSkillDef == null)
            {
                Debug.LogError("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n" +
                    "ArtificerExtended could not replace Ancient Scepter's Antimatter Surge. " +
                    "Antimatter Surge WILL break Artificer Extended's alt passives. \n" +
                    "Either turn on ArtificerExtended's Ion Surge rework to use ArtificerExtended's Antimatter Surge, " +
                    "avoid using Antimatter Surge with ArtificerExtended's alt passive, " +
                    "or tell the Ancient Scepter developers to get in contact to fix Antimatter Surge. \n" +
                    "This is NOT an error that can be fixed on the ArtificerExtended side.\n" +
                    "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                return;
            }

            SkillDef surge2 = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName($"{surgeSkillDef.skillName}Scepter"));
            if (surge2 != null)
            {
                ModifyScepterSurge(surge2);
            }
        }
        #endregion

        private void DoSurgeReworkAssetSetup()
        {
            // hitbox setup
            ModelLocator modelLocator = mageBody.GetComponent<ModelLocator>();
            Transform modelTransform = modelLocator?.modelTransform;
            if (modelTransform)
            {
                HitBoxGroup hitBoxGroup = modelTransform.gameObject.AddComponent<HitBoxGroup>();
                hitBoxGroup.groupName = ThunderSurgeHitBoxGroupName;

                ChildLocator childLocator = modelTransform.GetComponent<ChildLocator>();
                if (childLocator)
                {
                    Transform rootTransform = childLocator.FindChild("Base")?.parent;
                    if (rootTransform)
                    {
                        GameObject hitboxTransform = new GameObject();
                        HitBox hitBox = hitboxTransform.AddComponent<HitBox>();
                        hitboxTransform.transform.parent = rootTransform;
                        hitboxTransform.layer = LayerIndex.projectile.intVal;
                        hitboxTransform.transform.localPosition = new Vector3(0, 0.564f, 0);
                        hitboxTransform.transform.localRotation = Quaternion.identity;
                        hitboxTransform.transform.localScale = Vector3.one * 3f;

                        hitBoxGroup.hitBoxes = new HitBox[1] { hitBox };
                    }
                }
            }

            // body effects
            // attack prefabs
        }

        public void ModifyVanillaIonSurge(SkillDef surge)
        {
            SkillBase.RegisterEntityState(typeof(EntityState.SurgeExtendedDash));
            SkillBase.RegisterEntityState(typeof(EntityState.SurgeExtendedImpact));
            LanguageAPI.Add(SkillBase.Token + "ALTIONSURGE_DESC",
                $"<style=cIsDamage>Stunning</style>. Surge forward, dealing " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(SurgeExtendedDash.grazeDamageCoefficient)} damage</style> to enemies in your path. " +
                $"Upon impact, create an explosion for <style=cIsDamage>{Tools.ConvertDecimal(SurgeExtendedDash.impactDamageCoefficient)} damage</style>.");
                //"Burst forward up to 3 times. <style=cIsDamage>Can attack while dashing.</style> Trigger again to cancel early.");
            surge.activationState = new SerializableEntityStateType(typeof(EntityState.SurgeExtendedDash));
            surge.baseRechargeInterval = 9f;
            surge.skillDescriptionToken = SkillBase.Token + "ALTIONSURGE_DESC";
            //surge.keywordTokens = new string[0];
        }

        public void ModifyScepterSurge(SkillDef surge2)
        {
            SkillBase.RegisterEntityState(typeof(EntityState.AlternateIonSurge2));

            LanguageAPI.Add(SkillBase.Token + "ALTANTISURGE_LIGHTNING", "Antimatter Surge");
            LanguageAPI.Add(SkillBase.Token + "ALTANTISURGE_DESC",
                "Burst forward up to 3 times. <style=cIsDamage>Can attack while dashing.</style> Trigger again to cancel early." +
                "\n<color=#d299ff>SCEPTER: Each burst reduces ALL cooldowns.</color>");

            surge2.activationState = new SerializableEntityStateType(typeof(EntityState.AlternateIonSurge2));
            surge2.baseRechargeInterval = 6f;
            surge2.skillDescriptionToken = SkillBase.Token + "ALTANTISURGE_DESC";
            surge2.skillNameToken = SkillBase.Token + "ALTANTISURGE_LIGHTNING";
            surge2.keywordTokens = new string[0];
        }
    }
}
