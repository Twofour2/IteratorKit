using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.CMOracle;
using IteratorKit.Util;
using static IteratorKit.CMOracle.CMOracle;

namespace IteratorKit.SSOracle
{
    /// <summary>
    /// Replaces SSOracleBehavior with CMOracle behavior when the right conditions are met
    /// </summary>
    public class SSOracleOverride
    {
        public static ITKMultiValueDictionary<string, OracleJData> ssOracleJsons = new ITKMultiValueDictionary<string, OracleJData>();

        public static void ApplyHooks()
        {
            On.Oracle.ctor += Oracle_ctor;
            On.Oracle.HitByWeapon += Oracle_HitByWeapon;
            //On.Oracle.Update += Oracle_Update;
        }

        //private static void Oracle_Update(On.Oracle.orig_Update orig, Oracle self, bool eu)
        //{

        //}

        public static void RemoveHooks()
        {
            On.Oracle.ctor -= Oracle_ctor;
            On.Oracle.HitByWeapon -= Oracle_HitByWeapon;
        }

        private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
        {
            orig(self, abstractPhysicalObject, room);
            IteratorKit.Log.LogInfo(room.roomSettings?.name);
            if (!ssOracleJsons.TryGetValue(room.roomSettings?.name, out List<OracleJData>roomSSOracleJsons))
            {
                IteratorKit.Log.LogInfo("Treating SS as normal");
                return;  // no custom oracles for this room
            }
            OracleJData ssOracleJson = null;
            foreach (OracleJData checkSSOracleJson in roomSSOracleJsons)
            {
                IteratorKit.Log.LogInfo(self.ID.value);
                if (checkSSOracleJson.forSlugcats.Contains(room.game.StoryCharacter) && self.ID.value == checkSSOracleJson.id)
                {
                    IteratorKit.Log.LogInfo("Loading SSCustomBehavior");
                    ssOracleJson = checkSSOracleJson;
                    break;
                }
            }
            if (ssOracleJson == null)
            {
                return;
            }
            self.OracleData().oracleJson = ssOracleJson;
            self.oracleBehavior = new CMOracleBehavior(self);

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
