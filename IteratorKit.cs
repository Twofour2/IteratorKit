using System.Collections.Generic;
using System.Linq;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using IteratorKit.Debug;
using IteratorKit.CMOracle;
using Newtonsoft.Json;
using System;

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

        public Dictionary<string, CMOracle.CMOracle> oracles = new Dictionary<string, CMOracle.CMOracle>();
        public Dictionary<string, OracleJSON> oracleJsons = new Dictionary<string, OracleJSON>();

        public static bool debugMode = false;
        public CMOracleDebugUI oracleDebugUI = new CMOracleDebugUI();

        private void OnEnable()
        {
            Log = base.Logger;
            Log.LogWarning("is loaded!");
            On.RainWorld.PostModsInit += OnPostModsInit;
            On.RainWorldGame.RestartGame += OnRestartGame;

        }

        private void OnDisable()
        {
            Logger.LogWarning("itk on remove!");
            On.RainWorld.PostModsInit -= OnPostModsInit;
            On.RainWorldGame.RestartGame -= OnRestartGame;
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
            this.oracles = new Dictionary<string, CMOracle.CMOracle>();
            this.oracleJsons = new Dictionary<string, OracleJSON>();
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
                            //todo
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

    }

}