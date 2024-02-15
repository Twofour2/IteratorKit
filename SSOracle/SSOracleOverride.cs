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
        public static OracleJSON ssOracleJson = null;

        public static void ApplyHooks()
        {
            On.Oracle.ctor += Oracle_ctor;
        }


        public static void RemoveHooks()
        {
            On.Oracle.ctor -= Oracle_ctor;
        }

        private static void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
        {
            orig(self, abstractPhysicalObject, room);
            self.GetOracleData().oracleJson = ssOracleJson;
            List<SlugcatStats.Name> forSlugcats = self.GetOracleData().oracleJson.forSlugcats;
            if (forSlugcats != null)
            {
                if (forSlugcats.Count > 0 && forSlugcats.Contains(self.room.game.GetStorySession.saveStateNumber))
                {
                    // ah fuck
                    IteratorKit.Logger.LogWarning("Override SS oracle behavior " + self);
                    
                    self.oracleBehavior = new SSCustomBehavior(self);
                    return;
                }
            }
            IteratorKit.Logger.LogInfo("Treating SS oracle as normal");
            
            

        }


    }
}
