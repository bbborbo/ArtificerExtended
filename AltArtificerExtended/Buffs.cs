using AltArtificerExtended.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API;
using R2API.Utils;

namespace AltArtificerExtended
{
    public partial class Main
    {
        public static List<BuffDef> buffDefs = new List<BuffDef>();

        public static DotController.DotIndex burnDot;


        void CreateBuffs()
        {
            RoR2Application.onLoad += FixBuffDef;

            burnDot = DotController.DotIndex.Burn;

            AddAAPassiveBuffs();
        }
        void FixBuffDef()
        {
             RoR2Content.Buffs.Slow80.canStack = true;
        }

        internal static void AddBuff(BuffDef buff)
        {
            buffDefs.Add(buff);
        }

        #region AltArtiPassive
        public static BuffDef chillDebuff;
        public static BuffDef meltBuff;

        void AddAAPassiveBuffs()
        {
            chillDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                chillDebuff.buffColor = new Color(0.4f, 0.4f, 0.9f);
                chillDebuff.canStack = true;
                chillDebuff.iconSprite = RoR2.LegacyResourcesAPI.Load<Sprite>("Textures/BuffIcons/texBuffSlow50Icon");
                chillDebuff.isDebuff = false;
                chillDebuff.name = "AltArtiColdDebuff";
            }
            AddBuff(chillDebuff);

            meltBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                meltBuff.buffColor = new Color(0.9f, 0.2f, 0.2f);
                meltBuff.canStack = true;
                meltBuff.iconSprite = RoR2.LegacyResourcesAPI.Load<Sprite>("Textures/BuffIcons/texBuffAffixRed");
                meltBuff.isDebuff = false;
                meltBuff.name = "AltArtiFireBuff";
            }
            AddBuff(meltBuff);
        }
        #endregion


    }
}
