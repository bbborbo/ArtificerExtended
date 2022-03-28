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
        public static DotController.DotIndex strongBurnDot;


        void CreateBuffs()
        {
            RoR2Application.onLoad += FixBuffDef;

            burnDot = DotController.DotIndex.Burn;
            strongBurnDot = DotController.DotIndex.StrongerBurn;

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
        //public static BuffDef chillDebuff;
        public static BuffDef meltBuff;

        void AddAAPassiveBuffs()
        {
            /*chillDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                chillDebuff.buffColor = new Color(0.4f, 0.4f, 0.9f);
                chillDebuff.canStack = true;
                chillDebuff.iconSprite = RoR2.LegacyResourcesAPI.Load<Sprite>("RoR2/Base/Common/texBuffSlow50Icon.png");
                chillDebuff.isDebu*/

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

        private void Fucksadghuderfbghujlaergh()
        {
            meltBuff.iconSprite = DLC1Content.Buffs.StrongerBurn.iconSprite;
        }
        #endregion
    }
}
