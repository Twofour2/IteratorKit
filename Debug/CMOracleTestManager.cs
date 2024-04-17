using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.CMOracle;

namespace IteratorKit.Debug
{
    public class CMOracleTestManager
    {
        public List<string> testOracleFiles = new List<string>();
        public int currentTestIdx = 0;
        public static int testTimerMax = 3000;
        public int currentTestTimer = testTimerMax;
        
        public IteratorKit iteratorKit;
        public Room testRoom;
        public bool appliedHooks = false;
        public bool testsActive = false;
        
        public void EnableTestMode(IteratorKit iteratorKit, RainWorldGame rainWorld)
        {
            IteratorKit.Log.LogInfo("Enable test mode");
            this.iteratorKit = iteratorKit;
            this.currentTestTimer = testTimerMax;
            this.currentTestIdx = 0;
            this.testsActive = true;
            
            this.ClearOracles();
            this.testOracleFiles.Clear();

            ModManager.Mod testMod = ModManager.ActiveMods.FirstOrDefault(x => x.name == "IteratorKitTest");
            if (testMod == null)
            {
                IteratorKit.Log.LogError("Cant begin tests as test mod is not loaded.");
                return;
            }
            string testFolder = testMod.path + "/tests";
            foreach (string file in Directory.GetFiles(testFolder))
            {
                this.testOracleFiles.Add(file);
            }
            IteratorKit.Log.LogInfo($"Initilized tests with {this.testOracleFiles.Count} files");
            if (!appliedHooks)
            {
                this.appliedHooks = true;
                On.Player.NewRoom += TestUpdateRoom;
            }
            
            this.testRoom = rainWorld.cameras.FirstOrDefault()?.room;
        }

        public void ClearOracles()
        {
            foreach (CMOracle.CMOracle oracle in iteratorKit.oracles.Values)
            {
                IteratorKit.Log.LogInfo("removing existing oracles");
                oracle.Destroy();
            }
            this.iteratorKit.oracles.Clear();
            this.iteratorKit.oracleJsons.Clear();
        }

        public void GoToNextOracle(RainWorldGame rainWorld)
        {
            if ((this.currentTestIdx + 1) <= this.testOracleFiles.Count)
            {
                this.currentTestIdx++;
            }
            else
            {
                this.currentTestIdx = 0;
            }
            this.LoadNextOracle(rainWorld);
        }

        private void TestUpdateRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            this.testRoom = newRoom;
        }


        public void LoadNextOracle(RainWorldGame rainWorld)
        {
            IteratorKit.Log.LogWarning($"load next oracle file {this.currentTestIdx}");
            this.ClearOracles();
            try
            {
                this.iteratorKit.LoadOracleFile(this.testOracleFiles[this.currentTestIdx]);
                IteratorKit.Log.LogWarning("loaded oracle");
                testRoom.ReadyForAI(); // force oracle to spawn
            }catch(Exception e)
            {
                IteratorKit.Log.LogError(e);
                CMOracleDebugUI.ModWarningText(e.Message, rainWorld.rainWorld);
            }
           
        }
    }
}
