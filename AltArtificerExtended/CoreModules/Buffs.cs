using ArtificerExtended.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API;
using R2API.Utils;

namespace ArtificerExtended
{
    public static class Buffs
    {
        public static List<BuffDef> buffDefs = new List<BuffDef>();

        public static DotController.DotIndex burnDot;
        public static DotController.DotIndex strongBurnDot;


        public static void CreateBuffs()
        {
            burnDot = DotController.DotIndex.Burn;
            strongBurnDot = DotController.DotIndex.StrongerBurn;

            AddAAPassiveBuffs();
        }

        internal static void AddBuff(BuffDef buff)
        {
            buffDefs.Add(buff);
        }

        #region EnergeticResonance
        public static BuffDef meltBuff;

        static void AddAAPassiveBuffs()
        {
            Sprite meltSprite = LegacyResourcesAPI.Load<Sprite>("RoR2/DLC1/StrengthenBurn/texBuffStrongerBurnIcon.png");
            meltBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                meltBuff.buffColor = new Color(0.9f, 0.4f, 0.2f);
                meltBuff.canStack = true;
                meltBuff.iconSprite = meltSprite;
                meltBuff.isDebuff = false;
                meltBuff.name = "AltArtiFireBuff";
            }
            AddBuff(meltBuff);
            RoR2Application.onLoad += Fucksadghuderfbghujlaergh;
        }

        private static void Fucksadghuderfbghujlaergh()
        {
            meltBuff.iconSprite = DLC1Content.Buffs.StrongerBurn.iconSprite;
        }
        #endregion
    }
}