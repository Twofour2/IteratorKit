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
            On.Oracle.Update += Oracle_Update;
        }

        private static void Oracle_Update(On.Oracle.orig_Update orig, Oracle self, bool eu)
        {
            orig(self, eu);
            IteratorKit.Log.LogInfo(self.oracleBehavior.player.firstChunk.pos);
        }

        public static void RemoveHooks()
        {
            On.Oracle.ctor -= Oracle_ctor;
            On.Oracle.HitByWeapon -= Oracle_HitByWeapon;
            On.Oracle.Update -= Oracle_Update;
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

        }

        private static void Oracle_HitByWeapon(On.Oracle.orig_HitByWeapon orig, Oracle self, Weapon weapon)
        {
            if (self.oracleBehavior is CMOracleBehavior)
            {
                (self.oracleBehavior as CMOracleBehavior).ReactToHitByWeapon(weapon);
            }
            else
            {
                orig(self, weapon);
            }
        }
    }
}
