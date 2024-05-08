using System.Collections.Generic;
using System.Linq;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using IteratorKit.Debug;
using IteratorKit.CMOracle;
using Newtonsoft.Json;
using System;
using IteratorKit.Util;
using IteratorKit.SSOracle;
using UnityEngine;
using IteratorKit.CustomPearls;
using IteratorKit.SLOracle;

namespace IteratorKit
{

    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class IteratorKit : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "twofour2.iteratorKit";
        public const string PLUGIN_NAME = "iteratorKit";
        public const string PLUGIN_VERSION = "0.3.1";
        public static ManualLogSource Log { get; private set; }
        public delegate void OnOracleLoad();
        public static OnOracleLoad? OnOracleLoadEvent;
        public Debug.CMOracleTestManager testManager = new CMOracleTestManager();

        public ITKMultiValueDictionary<string, CMOracle.CMOracle> oracles = new ITKMultiValueDictionary<string, CMOracle.CMOracle>();
        public ITKMultiValueDictionary<string, OracleJData> oracleJsons = new ITKMultiValueDictionary<string, OracleJData>();

        public static bool debugMode = false;
        public CMOracleDebugUI oracleDebugUI = new CMOracleDebugUI();

        private void OnEnable()
        {
            Log = base.Logger;
            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.RainWorld.PostModsInit += OnPostModsInit;
            On.RainWorldGame.RestartGame += OnRestartGame;
            On.Room.ReadyForAI += OnReadyForAI;
            CMOracle.CMOracle.ApplyHooks();
            SSOracleOverride.ApplyHooks();
            SLOracleOverride.ApplyHooks();
            CustomPearls.CustomPearls.ApplyHooks();
        }

        private void OnDisable()
        {
            On.RainWorldGame.RawUpdate -= RainWorldGame_RawUpdate;
            On.RainWorld.PostModsInit -= OnPostModsInit;
            On.RainWorldGame.RestartGame -= OnRestartGame;
            On.Room.ReadyForAI -= OnReadyForAI;
            CMOracle.CMOracle.RemoveHooks();
            SSOracleOverride.RemoveHooks();
            SLOracleOverride.RemoveHooks();
            CustomPearls.CustomPearls.RemoveHooks();
        }

        private void OnPostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            LoadOracleFiles(self);
        }


        private void OnRestartGame(On.RainWorldGame.orig_RestartGame orig, RainWorldGame self)
        {
            orig(self);
            LoadOracleFiles(self.rainWorld);
        }

        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (self.devToolsActive)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    RainWorldGame.ForceSaveNewDenLocation(self, self.FirstAnyPlayer.Room.name, false);
                    CMOracleDebugUI.ModWarningText($"Save file forced den location to {self.FirstAlivePlayer.Room.name}! Press \"R\" to reload.", self.rainWorld);
                    ((StoryGameSession)self.session).saveState.deathPersistentSaveData.theMark = true;
                }
                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    Futile.atlasManager.LogAllElementNames();
                    IteratorKit.Log.LogInfo("Logging shader names");
                    foreach (KeyValuePair<string, FShader> shader in self.rainWorld.Shaders)
                    {
                        IteratorKit.Log.LogInfo(shader.Key);
                    }
                }
                if (Input.GetKeyDown(KeyCode.Alpha9))
                {
                    if (!this.oracleDebugUI.debugUIActive)
                    {
                        oracleDebugUI.EnableDebugUI(self.rainWorld, this);
                    }
                    else
                    {
                        oracleDebugUI.DisableDebugUI();
                    }

                }
                if (Input.GetKeyDown(KeyCode.Alpha8))
                {
                    oracleDebugUI.EnableDebugUI(self.rainWorld, this);
                    testManager.EnableTestMode(this, self);
                }
                if (Input.GetKeyDown(KeyCode.Alpha7) && this.testManager.testsActive)
                {
                    testManager.GoToNextOracle(self);
                }
                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    foreach (CMOracle.CMOracle oracle in this.oracles.AllValues())
                    {
                        (oracle.oracleBehavior as CMOracleBehavior).hadMainPlayerConversation = false;
                    }
                    self.GetStorySession.saveState.progression.SaveWorldStateAndProgression(malnourished: false);
                    CMOracleDebugUI.ModWarningText("Removed flag for HasHadMainPlayerConversation and saved game. Reload now.", self.rainWorld);

                }

            }
        }

        private void LoadOracleFiles(RainWorld rainWorld, bool isDuringInit = false)
        {
            this.oracles = new ITKMultiValueDictionary<string, CMOracle.CMOracle>();
            this.oracleJsons = new ITKMultiValueDictionary<string, OracleJData>();
            OnOracleLoadEvent?.Invoke();
            ModManager.Mod currentMod = ModManager.ActiveMods.First();
            string currentFile = "";
            try
            {
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                {
                    currentMod = mod;
                    if (Directory.Exists(Path.Combine(mod.path, "sprites")))
                    {
                        LoadOracleSprites(Path.Combine(mod.path, "sprites"));
                    }
                    foreach (string file in Directory.GetFiles(mod.path))
                    {
                        currentFile = file;
                        if (file.EndsWith("enabledebug"))
                        {
                            this.EnableDebugMode(rainWorld);
                        }
                        else if (file.EndsWith("oracle.json"))
                        {
                            this.LoadOracleFile(file);
                        }
                        else if (file.EndsWith("pearls.json"))
                        {
                            List<DataPearlJson> ojs = JsonConvert.DeserializeObject<List<DataPearlJson>>(File.ReadAllText(file));
                            CustomPearls.CustomPearls.LoadPearlData(ojs);
                            
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (!isDuringInit)
                { // currently this text doesnt work as the screen isn't setup quite right.
                    CMOracleDebugUI.ModWarningText($"Encountered an error while loading data file {currentFile} from mod ${currentMod.name}.\n\n${e.Message}", rainWorld);
                }

                Logger.LogError("EXCEPTION");
                Logger.LogError(e.ToString());
            }
        }

        public void LoadOracleFile(string file)
        {
            List<OracleJData> oracleJsons = JsonConvert.DeserializeObject<List<OracleJData>>(File.ReadAllText(file));

            foreach (OracleJData oracleJson in oracleJsons)
            {
                switch (oracleJson.id)
                {
                    case "SL":
                        Log.LogInfo($"Loading SLConversation {oracleJson.id} data {file}. Targeting room {oracleJson.roomId ?? "SL_AI"}");
                        SLOracleOverride.slOracleJsons.Add(oracleJson.roomId, oracleJson);
                        break;
                    case "SS":
                    case "DM":
                        Log.LogInfo($"Loading override {oracleJson.id} data {file}. Targeting room {oracleJson.roomId ?? "SS_AI"}");
                        SSOracleOverride.ssOracleJsons.Add(oracleJson.roomId ?? "SS_AI", oracleJson);
                        break;
                    default:
                        Log.LogInfo($"Loading {oracleJson.id} data {file}");
                        this.oracleJsons.Add(oracleJson.roomId, oracleJson);
                        break;
                }
            }
        }

        private void LoadOracleSprites(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                if (Path.GetFileName(file).StartsWith("oracle"))
                {
                    Log.LogInfo($"Loading atlas! {file} sprites/{Path.GetFileNameWithoutExtension(file)}");
                    Futile.atlasManager.LoadAtlas($"sprites/{Path.GetFileNameWithoutExtension(file)}");
                }
            }
        }

        private void EnableDebugMode(RainWorld rainWorld)
        {
            if (IteratorKit.debugMode)
            {
                Log.LogWarning("Debug mode already enabled");
                return;
            }
            Log.LogInfo("Iterator kit debug mode enabled");
            IteratorKit.debugMode = true;
            oracleDebugUI.EnableDebugUI(rainWorld, this);
        }

        private void OnReadyForAI(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig(self);
            if (self.game == null)
            {
                return;
            }
            string currentOracle = "";
            try
            {
                List<OracleJData> roomOracleJsons;
                if(!this.oracleJsons.TryGetValue(self.roomSettings?.name, out roomOracleJsons)){
                    // no oracles for this room
                    return;
                }
                foreach (OracleJData oracleJson in roomOracleJsons) {
                    if (oracleJson.forSlugcats.Contains(self.game.StoryCharacter))
                    {
                        Log.LogInfo($"Found matching room, spawning oracle {oracleJson.id}");
                        WorldCoordinate worldCoordinate = new WorldCoordinate(self.abstractRoom.index, 15, 15, -1);
                        AbstractPhysicalObject abstractPhysicalObject = new AbstractPhysicalObject(
                            self.world,
                            global::AbstractPhysicalObject.AbstractObjectType.Oracle,
                            null,
                            worldCoordinate,
                            self.game.GetNewID());
                        CMOracle.CMOracle cmOracle = new CMOracle.CMOracle(abstractPhysicalObject, self, oracleJson);
                        self.AddObject(cmOracle);
                        this.oracles.Add(self.roomSettings?.name, cmOracle);

                    }
                }
            }
            catch (Exception e)
            {
                IteratorKit.Log.LogError(e);
                CMOracleDebugUI.ModWarningText($"{currentOracle} {self.roomSettings?.name} Oracle Initialization Error: {e}", self.game.rainWorld);

            }
        }


    }

}