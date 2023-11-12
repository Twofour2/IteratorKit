using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IteratorKit.CMOracle
{
    public class CMOverseer
    {
        public static void ApplyHooks()
        {
           // On.WorldLoader.GeneratePopulation += WorldLoader_GeneratePopulation;
        }

        public static List<OracleJSON.OverseerJson> overseeerDataList = new List<OracleJSON.OverseerJson>();
        public static List<string> regionList = new List<string>();

        private static void WorldLoader_GeneratePopulation(On.WorldLoader.orig_GeneratePopulation orig, WorldLoader self, bool fresh)
        {
            orig(self, fresh);
            if (regionList.Contains(self.world.region.name))
            {
                IteratorKit.Logger.LogInfo($"CMOverseer match region {self.world.region.name}");
                OracleJSON.OverseerJson overseerData = overseeerDataList.Find(x => x.regions.Contains(self.world.region.name));
                if (overseerData == null)
                {
                    IteratorKit.Logger.LogError($"Failed to find overseer data for {self.world.region.name}");
                    return;
                }
                int randOverseerCount = UnityEngine.Random.Range(overseerData.genMin, overseerData.genMax);
                for (int i = 0; i < randOverseerCount; i++)
                {
                    // todo: create the overseer object?
                    //https://github.com/Dark-Gran/KarmaAppetite2/blob/main/src/KAWorld.cs#L187
                }
            } 
        }
    }
}
