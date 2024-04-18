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

        public ITKMultiValueDictionary<string, CMOracle.CMOracle> oracles = new ITKMultiValueDictionary<string, CMOracle.CMOracle>();
        public ITKMultiValueDictionary<string, OracleJSON> oracleJsons = new ITKMultiValueDictionary<string, OracleJSON>();

        public static bool debugMode = false;
        public CMOracleDebugUI oracleDebugUI = new CMOracleDebugUI();

        private void OnEnable()
        {
            Log = base.Logger;
            On.RainWorld.PostModsInit += OnPostModsInit;
            On.RainWorldGame.RestartGame += OnRestartGame;
            On.Room.ReadyForAI += OnReadyForAI;
            CMOracle.CMOracle.ApplyHooks();
        }

        private void OnDisable()
        {
            On.RainWorld.PostModsInit -= OnPostModsInit;
            On.RainWorldGame.RestartGame -= OnRestartGame;
            On.Room.ReadyForAI -= OnReadyForAI;
            CMOracle.CMOracle.RemoveHooks();
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

        private void LoadOracleFiles(RainWorld rainWorld, bool isDuringInit = false)
        {
            this.oracles = new ITKMultiValueDictionary<string, CMOracle.CMOracle>();
            this.oracleJsons = new ITKMultiValueDictionary<string, OracleJSON>();
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
                            //todo
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
            List<OracleJSON> oracleJsons = JsonConvert.DeserializeObject<List<OracleJSON>>(File.ReadAllText(file));

            foreach (OracleJSON oracleJson in oracleJsons)
            {
                switch (oracleJson.id)
                {
                    case "SL":
                        //todo
                        break;
                    case "SS":
                    case "DM":
                        // todo
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
                List<OracleJSON> oracleJsons;
                if(!this.oracleJsons.TryGetValue(self.roomSettings?.name, out oracleJsons)){
                    // no oracles for this room
                    return;
                }
                foreach (OracleJSON oracleJson in oracleJsons) {
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