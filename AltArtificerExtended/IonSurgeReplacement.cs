using ArtificerExtended.Components;
using ArtificerExtended.States;
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
using ArtificerExtended.Modules;

namespace ArtificerExtended
{
    partial class ArtificerExtendedPlugin
    {
        public const string ThunderSurgeHitBoxGroupName = "IonCharge";
        public static Material ionSurgePowerOverlay;
        public static BuffDef ionSurgePower;
        public static GameObject muzzleflashIonSurgeTrail;

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
                    Content.AddEntityState(typeof(States.VanillaIonSurge));
                    surge.activationState = new SerializableEntityStateType(typeof(States.VanillaIonSurge));
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
                        hitboxTransform.transform.localPosition = new Vector3(0, 1.5f, 0);
                        hitboxTransform.transform.localRotation = Quaternion.identity;
                        hitboxTransform.transform.localScale = new Vector3(4,4,4);

                        hitBoxGroup.hitBoxes = new HitBox[1] { hitBox };
                    }
                }
            }

            // effects
            ionSurgePower = Content.CreateAndAddBuff("bdIonSurgePower", null, Color.black, false, false);
            ionSurgePower.isHidden = true;

            ionSurgePowerOverlay = UnityEngine.Object.Instantiate(Addressables.LoadAssetAsync<Material>("RoR2/DLC1/VoidSurvivor/matVoidSurvivorCorruptOverlay.mat").WaitForCompletion());
            ionSurgePowerOverlay.name = "matIonSurgePowerOverlay";
            ionSurgePowerOverlay.SetColor("_TintColor", new Color32(0, 210, 255, 255));

            On.RoR2.CharacterModel.UpdateOverlays += SurgeOverlay;

            muzzleflashIonSurgeTrail = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mage/MuzzleflashMageLightningLargeWithTrail.prefab")
                .WaitForCompletion().InstantiateClone("MuzzleflashIonSurgeTrail");

            ParticleSystem[] pss = muzzleflashIonSurgeTrail.GetComponentsInChildren<ParticleSystem>();
            foreach(ParticleSystem ps in pss)
            {
                Debug.Log(ps.gameObject.name);
                ParticleSystem.MainModule main = ps.main;
                main.duration = SurgeExtendedDash.flightDuration;
                //main.loop = true;
            }
            TrailRenderer tr = muzzleflashIonSurgeTrail.GetComponentInChildren<TrailRenderer>();
            if (tr)
            {
                tr.time = SurgeExtendedDash.flightDuration + 0.5f;
            }

            Transform trail = muzzleflashIonSurgeTrail.transform.Find("Particles").Find("Trail");
            if (trail)
            {
                TrailRenderer t = trail.GetComponent<TrailRenderer>();
                t.time = SurgeExtendedDash.flightDuration + 0.5f;
            }

            Transform matrix = muzzleflashIonSurgeTrail.transform.Find("Particles").Find("Matrix, Mesh");
            if (matrix)
            {
                ParticleSystem ps = matrix.GetComponent<ParticleSystem>();
                ParticleSystem.MainModule main = ps.main;
                main.duration = SurgeExtendedDash.flightDuration;
                main.loop = true;
            }

            Transform light = muzzleflashIonSurgeTrail.transform.Find("Particles").Find("Point Light");
            if (light)
            {
                LightIntensityCurve lit = light.GetComponent<LightIntensityCurve>();
                lit.timeMax = 2f;
            }

            Transform smoke = muzzleflashIonSurgeTrail.transform.Find("Particles").Find("Smoke");
            if (smoke)
            {
                ParticleSystem ps = smoke.GetComponent<ParticleSystem>();
                ParticleSystem.MainModule main = ps.main;
                main.duration = SurgeExtendedDash.flightDuration;
                main.loop = true;
            }
            // attack prefabs
        }

        private void SurgeOverlay(On.RoR2.CharacterModel.orig_UpdateOverlays orig, CharacterModel self)
        {
            orig(self);
            if (self.visibility == VisibilityLevel.Invisible || self.body == null)
            {
                return;
            }

            AddOverlay(ionSurgePowerOverlay, self.body.HasBuff(ionSurgePower));

            void AddOverlay(Material overlayMaterial, bool condition)
            {
                if (self.activeOverlayCount < CharacterModel.maxOverlays && condition)
                {
                    self.currentOverlays[self.activeOverlayCount++] = overlayMaterial;
                }
            }
        }

        public static string surgeFullDesc =
            $"<style=cIsDamage>Stunning</style>. Surge forward, dealing " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(SurgeExtendedDash.grazeDamageCoefficient)} damage</style> to enemies in your path. " +
                $"Upon impact, create an explosion for <style=cIsDamage>{Tools.ConvertDecimal(SurgeExtendedDash.impactDamageCoefficient)} damage</style>.";
        public void ModifyVanillaIonSurge(SkillDef surge)
        {
            Content.AddEntityState(typeof(States.SurgeExtendedDash));
            Content.AddEntityState(typeof(States.SurgeExtendedImpact));
            LanguageAPI.Add("MAGE_SPECIAL_LIGHTNING_DESCRIPTION", surgeFullDesc);
                //"Burst forward up to 3 times. <style=cIsDamage>Can attack while dashing.</style> Trigger again to cancel early.");
            surge.activationState = new SerializableEntityStateType(typeof(States.SurgeExtendedDash));
            surge.baseRechargeInterval = 8f;
            //surge.keywordTokens = new string[0];
        }

        public void ModifyScepterSurge(SkillDef surge2)
        {
            //Debug.LogError("Scepter Surge Shit Not Available!!!!!!! Report to Borbo Immediatelly!!!!!!!!!!");
            LanguageAPI.Add("MAGE_ANTISURGE_LIGHTNING_DESC",
                surgeFullDesc +
                "\n<color=#d299ff>SCEPTER: 30% longer flight time, 200% impact damage.</color>");
            surge2.activationState = new SerializableEntityStateType(typeof(States.SurgeExtendedDash));
            //Content.AddEntityState(typeof(States.AlternateIonSurge2));
            //
            //LanguageAPI.Add("MAGE_ANTISURGE_LIGHTNING_NAME", "Antimatter Surge");
            //LanguageAPI.Add("MAGE_ANTISURGE_LIGHTNING_DESC",
            //    "Burst forward up to 3 times. <style=cIsDamage>Can attack while dashing.</style> Trigger again to cancel early." +
            //    "\n<color=#d299ff>SCEPTER: Each burst reduces ALL cooldowns.</color>");
            //
            //surge2.baseRechargeInterval = 8f;
            //surge2.skillDescriptionToken = "MAGE_ANTISURGE_LIGHTNING_DESC";
            //surge2.skillNameToken = "MAGE_ANTISURGE_LIGHTNING_NAME";
            //surge2.keywordTokens = new string[0];
        }
    }
}
