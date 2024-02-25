using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.CMOracle;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace IteratorKit.SSOracle
{
    public class SSOracleOverride
    {
        public static List<OracleJSON> ssOracleJsonData = new List<OracleJSON>();

        public static void ApplyHooks()
        {
            On.Oracle.ctor += Oracle_ctor;
            On.Oracle.HitByWeapon += Oracle_HitByWeapon;
        }

        

        public static void RemoveHooks()
        {
            On.Oracle.ctor -= Oracle_ctor;
            On.Oracle.HitByWeapon -= Oracle_HitByWeapon;
        }

        private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
        {
            orig(self, abstractPhysicalObject, room);
            OracleJSON oracleJSON = null;
            foreach (OracleJSON oracleJSONData in ssOracleJsonData)
            {
                IteratorKit.Logger.LogWarning(self.ID.value);
                if (oracleJSONData.forSlugcats.Contains(room.game.StoryCharacter) && self.ID.value == oracleJSONData.id)
                {
                    IteratorKit.Logger.LogInfo("Loading SSCustomBehavior");
                    oracleJSON = oracleJSONData;
                    break;
                }
            }
            if (oracleJSON == null)
            {
                return;
            }

            self.GetOracleData().oracleJson = oracleJSON;
            List<SlugcatStats.Name> forSlugcats = self.GetOracleData().oracleJson.forSlugcats;
            if (forSlugcats != null)
            {
                if (forSlugcats.Count > 0 && forSlugcats.Contains(self.room.game.GetStorySession.saveStateNumber))
                {
                    IteratorKit.Logger.LogWarning("Override SS oracle behavior " + self);
                    self.oracleBehavior = new SSCustomBehavior(self);
                    return;
                }
            }
            IteratorKit.Logger.LogInfo("Treating SS oracle as normal");
        }

        private static void Oracle_HitByWeapon(On.Oracle.orig_HitByWeapon orig, Oracle self, Weapon weapon)
        {
            if (self.oracleBehavior is SSCustomBehavior)
            {
                (self.oracleBehavior as SSCustomBehavior).ReactToHitByWeapon(weapon);
            }
            else
            {
                orig(self, weapon);
            }
        }


    }
}
