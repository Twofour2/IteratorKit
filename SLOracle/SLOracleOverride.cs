using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.CMOracle;
using IteratorKit.Util;
using UnityEngine;
using static IteratorKit.CMOracle.OracleJData.OracleEventsJData;

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
            On.SLOracleBehaviorHasMark.InterruptPlayerHoldNeuron += SLOracleBehaviorHasMark_InterruptPlayerHoldNeuron;
            On.SLOracleBehaviorHasMark.PlayerReleaseNeuron += SLOracleBehaviorHasMark_PlayerReleaseNeuron;
            On.Oracle.Collide += Oracle_Collide;
        }

        

        public static void RemoveHooks()
        {
            On.Oracle.ctor -= Oracle_ctor;
            On.Oracle.HitByWeapon -= Oracle_HitByWeapon;
            //  On.SLOracleBehaviorHasMark.MoonConversation.ctor -= MoonConversation_ctor;
            On.SLOracleBehaviorHasMark.InitateConversation -= SLOracleBehavior_InitConversation;
            On.SLOracleBehaviorHasMark.InterruptPlayerHoldNeuron -= SLOracleBehaviorHasMark_InterruptPlayerHoldNeuron;
            On.SLOracleBehaviorHasMark.PlayerReleaseNeuron -= SLOracleBehaviorHasMark_PlayerReleaseNeuron;
            On.Oracle.Collide -= Oracle_Collide;
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
                return;
            }
            orig(self);
        }


        private static void Oracle_HitByWeapon(On.Oracle.orig_HitByWeapon orig, Oracle self, Weapon weapon)
        {
            if (self.oracleBehavior is CMOracleSitBehavior)
            {
                (self.oracleBehavior as CMOracleSitBehavior).cmMixin.ReactToHitByWeapon(weapon);
            }
            else
            {
                orig(self, weapon);
            }
        }

        private static void SLOracleBehaviorHasMark_InterruptPlayerHoldNeuron(On.SLOracleBehaviorHasMark.orig_InterruptPlayerHoldNeuron orig, SLOracleBehaviorHasMark self)
        {
            if (self is CMOracleSitBehavior)
            {
                if ((self as CMOracleSitBehavior).cmMixin.HasEvent("playerTakeNeuron"))
                {
                    return;
                }
            }
            orig(self);
        }

        private static void SLOracleBehaviorHasMark_PlayerReleaseNeuron(On.SLOracleBehaviorHasMark.orig_PlayerReleaseNeuron orig, SLOracleBehaviorHasMark self)
        {
            if (self is CMOracleSitBehavior)
            {
                if ((self as CMOracleSitBehavior).cmMixin.HasEvent("playerReleaseNeuron"))
                {
                    return;
                }
            }
            orig(self);
        }

        private static void Oracle_Collide(On.Oracle.orig_Collide orig, Oracle self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (self.oracleBehavior is not CMOracleSitBehavior)
            {
                orig(self, otherObject, myChunk, otherChunk);
                return;
            }
            (self.oracleBehavior as CMOracleSitBehavior).Collide(otherObject, myChunk, otherChunk);
        }



    }
}
