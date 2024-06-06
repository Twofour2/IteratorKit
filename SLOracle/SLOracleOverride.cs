using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.CMOracle;
using IteratorKit.Util;
using UnityEngine;

namespace IteratorKit.SLOracle
{
    /// <summary>
    /// Replaces SLOracleBehavior(s) with CMOracleMoonBehavior
    /// </summary>
    public class SLOracleOverride
    {

        public static ITKMultiValueDictionary<string, OracleJData> slOracleJsons = new ITKMultiValueDictionary<string, OracleJData>();

        

        public static void ApplyHooks()
        {
            On.Oracle.ctor += Oracle_ctor;
            On.Oracle.HitByWeapon += Oracle_HitByWeapon;
            //On.SLOracleBehaviorHasMark.MoonConversation.ctor += MoonConversation_ctor;
            On.SLOracleBehaviorHasMark.InitateConversation += SLOracleBehavior_InitConversation;
        }

        public static void RemoveHooks()
        {
            On.Oracle.ctor -= Oracle_ctor;
            On.Oracle.HitByWeapon -= Oracle_HitByWeapon;
            //  On.SLOracleBehaviorHasMark.MoonConversation.ctor -= MoonConversation_ctor;
            On.SLOracleBehaviorHasMark.InitateConversation -= SLOracleBehavior_InitConversation;
        }

        private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
        {
            orig(self, abstractPhysicalObject, room);
            IteratorKit.Log.LogInfo(room.roomSettings?.name);
            if (!slOracleJsons.TryGetValue(room.roomSettings?.name, out List<OracleJData> roomSSOracleJsons))
            {
                IteratorKit.Log.LogInfo("Treating SL as normal");
                return;  // no custom oracles for this room
            }
            OracleJData slOracleJson = null;
            foreach (OracleJData checkSLOracleJson in roomSSOracleJsons)
            {
                IteratorKit.Log.LogInfo(self.ID.value);
                if (checkSLOracleJson.forSlugcats.Contains(room.game.StoryCharacter) && self.ID.value == checkSLOracleJson.id)
                {
                    IteratorKit.Log.LogInfo("Loading SSCustomBehavior");
                    slOracleJson = checkSLOracleJson;
                    break;
                }
            }
            if (slOracleJson == null)
            {
                return;
            }
            self.OracleData().oracleJson = slOracleJson;
            self.oracleBehavior = new CMOracleSitBehavior(self);
            IteratorKit.overrideOracles.Add(slOracleJson.roomId, self);

        }

        private static void SLOracleBehavior_InitConversation(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
        {
            if (self.oracle.oracleBehavior is CMOracleSitBehavior)
            {
                throw new NotImplementedException("Not yet finished!");
                return;
            }
            orig(self);
        }


        //private static void MoonConversation_ctor(On.SLOracleBehaviorHasMark.MoonConversation.orig_ctor orig, SLOracleBehaviorHasMark.MoonConversation self, Conversation.ID id, OracleBehavior oracleBehavior, SLOracleBehaviorHasMark.MiscItemType describeItem)
        //{
        //    IteratorKit.Log.LogInfo("ctor");
        //    if (oracleBehavior is CMOracleSitBehavior)
        //    {
        //        IteratorKit.Log.LogWarning("Muting LTTM");
        //        return;
        //    }
        //    orig(self, id, oracleBehavior, describeItem);
        //}

        private static void Oracle_HitByWeapon(On.Oracle.orig_HitByWeapon orig, Oracle self, Weapon weapon)
        {
            if (self.oracleBehavior is CMOracleBehavior)
            {
                (self.oracleBehavior as CMOracleBehavior).cmMixin.ReactToHitByWeapon(weapon);
            }
            else
            {
                orig(self, weapon);
            }
        }
    }
}
