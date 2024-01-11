using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using IteratorKit.CMOracle;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RWCustom;
using System.Text;
using On.Menu;
using Menu;
using MoreSlugcats;
using IteratorKit.SLOracle;
using IteratorKit.CustomPearls;
using System.Linq.Expressions;
using IteratorKit.Debug;
using System.Runtime.ExceptionServices;
using SlugBase.SaveData;

namespace IteratorKit
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class IteratorKit : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "twofour2.iteratorKit";
        public const string PLUGIN_NAME = "iteratorKit";
        public const string PLUGIN_DESC = "Framework for creating custom iterators and making dialogs for existing iterators.<LINE> <LINE>For mod developers, please see the github page: https://github.com/Twofour2/IteratorKit/.";
        public const string PLUGIN_VERSION = "0.2.9";

        private bool oracleHasSpawned = false;
        public CMOracle.CMOracle oracle;

        public static new ManualLogSource Logger { get; private set; }

        public List<string> oracleRoomIds = new List<string>();
        public List<OracleJSON> oracleJsonData = new List<OracleJSON>();
        public CMOracleDebugUI oracleDebugUI = new CMOracleDebugUI();
        public List<CMOracle.CMOracle> oracleList = new List<CMOracle.CMOracle>();
        public static bool debugMode = false;
        public Debug.CMOracleTestManager testManager = new CMOracleTestManager();

        private void OnEnable()
        {
            Logger = base.Logger;
            Logger.LogWarning("LOaded new dllnew? 4");

            On.Room.ReadyForAI += SpawnOracle;

            CMOracle.CMOracle.ApplyHooks();
            CMOverseer.ApplyHooks();

            On.RainWorld.PostModsInit += AfterModsInit;
            On.RainWorldGame.RestartGame += OnRestartGame;
            
            SlugBase.SaveData.SaveDataHooks.Apply();
            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.ShortcutHandler.Update += InformOfInvalidShortcutError;
        }


        private void OnDisable()
        {
            On.Room.ReadyForAI -= SpawnOracle;

            CMOracle.CMOracle.RemoveHooks();
           // CMOverseer.ApplyHooks();

            On.RainWorld.PostModsInit -= AfterModsInit;
            On.RainWorldGame.RestartGame -= OnRestartGame;

            SlugBase.SaveData.SaveDataHooks.UnApply();

            On.RainWorldGame.RawUpdate -= RainWorldGame_RawUpdate;
            On.ShortcutHandler.Update -= InformOfInvalidShortcutError;
        }

        

        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
          //  Logger.LogWarning("LOaded new dll?9");
            if (self.devToolsActive) {
                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    RainWorldGame.ForceSaveNewDenLocation(self, self.FirstAnyPlayer.Room.name, false);
                    CMOracleDebugUI.ModWarningText($"Save file forced den location to {self.FirstAlivePlayer.Room.name}! Press \"R\" to reload.", self.rainWorld);
                }
                if (Input.GetKeyDown(KeyCode.Alpha6))
                {
                    Futile.atlasManager.LogAllElementNames();
                    IteratorKit.Logger.LogWarning("Logging shader names");
                    foreach(KeyValuePair<string, FShader> shader in self.rainWorld.Shaders)
                    {
                        IteratorKit.Logger.LogInfo(shader.Key);
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
                    foreach (CMOracle.CMOracle oracle in oracleList)
                    {
                        (oracle.oracleBehavior as CMOracleBehavior).SetHasHadMainPlayerConversation(false);
                    }
                    self.GetStorySession.saveState.progression.SaveWorldStateAndProgression(malnourished: false);
                    CMOracleDebugUI.ModWarningText("Removed flag for HasHadMainPlayerConversation and saved game. Reload now.", self.rainWorld);

                }
                
            }
        }

        private void OnRestartGame(On.RainWorldGame.orig_RestartGame orig, RainWorldGame self)
        {
            this.LoadOracleFiles(self.rainWorld);
            orig(self);
        }

        private void AfterModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            LoadOracleFiles(self, true);
        }


        private void LoadOracleFiles(RainWorld rainWorld, bool isDuringInit = false)
        {
            EncryptDialogFiles();
            try
            {
                this.oracleList = new List<CMOracle.CMOracle>();
                this.oracleDebugUI.ClearDebugLabels();
                oracleRoomIds = new List<string>();
                oracleJsonData = new List<OracleJSON>();
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                {
                    if (Directory.Exists(mod.path + "/sprites"))
                    {
                        IteratorKit.Logger.LogWarning("hunting for atlases in " + mod.path + "/sprites");
                        foreach (string file in Directory.GetFiles(mod.path + "/sprites"))
                        {
                            IteratorKit.Logger.LogInfo(file);
                            
                            if (Path.GetFileName(file).StartsWith("oracle"))
                            {
                                IteratorKit.Logger.LogWarning($"Loading atlas! sprites/{Path.GetFileNameWithoutExtension(file)}");
                                Futile.atlasManager.LoadAtlas($"sprites/{Path.GetFileNameWithoutExtension(file)}");
                            }
                        }
                    }
                    
                    foreach (string file in Directory.GetFiles(mod.path))
                    {
                        try
                        {
                            if (file.EndsWith("enabledebug"))
                            {
                                this.EnableDebugMode(rainWorld);
                            }
                            if (file.EndsWith("oracle.json"))
                            {
                                this.LoadOracleFile(file);
                            }
                            if (file.EndsWith("pearls.json"))
                            {
                                List<DataPearlJson> ojs = JsonConvert.DeserializeObject<List<DataPearlJson>>(File.ReadAllText(file));
                                CustomPearls.CustomPearls.LoadPearlData(ojs);
                                CustomPearls.CustomPearls.ApplyHooks();

                            }
                        }catch(Exception e)
                        {
                            if (!isDuringInit)
                            { // currently this text doesnt work as the screen isn't setup quite right.
                                CMOracleDebugUI.ModWarningText($"Encountered an error while loading data file {file} from mod ${mod.name}.\n\n${e.Message}", rainWorld);
                            }
                            
                            Logger.LogWarning("EXCEPTION");
                            Logger.LogWarning(e.ToString());
                        }
                    }
                }
            }
            catch (Exception e)
            {
               
                Logger.LogWarning("EXCEPTION");
                Logger.LogWarning(e.ToString());
            }
            return;
        }

        public void LoadOracleFile(string file)
        {
            List<OracleJSON> ojs = JsonConvert.DeserializeObject<List<OracleJSON>>(File.ReadAllText(file));

            foreach (OracleJSON oracleData in ojs)
            {
                oracleJsonData.Add(oracleData);
                oracleRoomIds.Add(oracleData.roomId);
                switch (oracleData.id)
                {
                    case "SL":
                        SLConversation slConvo = new SLConversation(oracleData);
                        slConvo.ApplyHooks();
                        break;
                    case "SS": // includes DM
                        SSConversation ssConvo = new SSConversation(oracleData);
                        SSConversation.LogAllActionsAndMovements();
                        ssConvo.ApplyHooks();
                        break;
                }
                if (oracleData.overseers != null)
                {
                    CMOverseer.overseeerDataList.Add(oracleData.overseers);
                    CMOverseer.regionList.AddRange(oracleData.overseers.regions);
                }

            }
        }

        private void EnableDebugMode(RainWorld rainWorld)
        {
            if (IteratorKit.debugMode)
            {
                IteratorKit.Logger.LogInfo("Debug mode already enabled");
                return;  
            }
            IteratorKit.Logger.LogInfo("Iterator kit debug mode enabled");
            IteratorKit.debugMode = true;
            On.Menu.HoldButton.Update += HoldButton_Update;
            oracleDebugUI.EnableDebugUI(rainWorld, this);

        }

        private void ShelterDoor_Update(On.ShelterDoor.orig_Update orig, ShelterDoor self, bool eu)
        {
            
            orig(self, eu);
            self.openTime = 1;
            self.openUpTicks = 10;
        }

        private static void HoldButton_Update(On.Menu.HoldButton.orig_Update orig, Menu.HoldButton self)
        {
            orig(self);
            if (self.held)
            {
                self.Singal(self, self.signalText);
                self.hasSignalled = true;
                self.menu.ResetSelection();
            }
        }

        private void SpawnOracle(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig(self);
            if (self.game == null)
            {
                return;
            }
            try
            {
                if (this.oracleRoomIds.Contains(self.roomSettings.name))
                {
                    IEnumerable<OracleJSON> oracleJsons = this.oracleJsonData.Where(x => x.roomId == self.roomSettings.name);
                    foreach (OracleJSON oracleJson in oracleJsons)
                    {

                        if (oracleJson.forSlugcats.Contains(self.game.StoryCharacter))
                        {
                            IteratorKit.Logger.LogWarning($"Found matching room, spawning oracle {oracleJson.id}");
                            self.loadingProgress = 3;
                            self.readyForNonAICreaturesToEnter = true;
                            WorldCoordinate worldCoordinate = new WorldCoordinate(self.abstractRoom.index, 15, 15, -1);
                            AbstractPhysicalObject abstractPhysicalObject = new AbstractPhysicalObject(
                                self.world,
                                global::AbstractPhysicalObject.AbstractObjectType.Oracle,
                                null,
                                worldCoordinate,
                                self.game.GetNewID());

                            oracle = new CMOracle.CMOracle(abstractPhysicalObject, self, oracleJson);
                            self.AddObject(oracle);
                            self.waitToEnterAfterFullyLoaded = Math.Max(self.waitToEnterAfterFullyLoaded, 20);
                            this.oracleList.Add(oracle);
                        }
                        else
                        {
                            Logger.LogWarning($"{oracleJson.id} Oracle is not avalible for the current slugcat");
                        }
                    }

                }
            }catch (Exception e)
            {
                IteratorKit.Logger.LogError(e);
                CMOracleDebugUI.ModWarningText($"Iterator Kit Initialization Error: {e}", self.game.rainWorld);
                
            }

            

        }

        public void DebugMouse_Update(On.DebugMouse.orig_Update orig, DebugMouse self, bool eu)
        {
            
            orig(self, eu);
            if (oracleHasSpawned)
            {
                //oracle.oracleBehavior.SetNewDestination(self.pos);
                oracle.oracleBehavior.lookPoint = self.pos;
            }
        }

        public static void LogVector2(Vector2 vector)
        {
            Logger.LogInfo($"x: {vector.x} y: {vector.y}");
        }

        public void EncryptDialogFiles()
        {
            try
            {
                IteratorKit.Logger.LogWarning("Encrypting all dialog files");
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                {
                    string[] dirs = Directory.GetDirectories(mod.path);
                    foreach (string dir in dirs)
                    {
                        if (dir.EndsWith("text_raw"))
                        {
                            IteratorKit.Logger.LogInfo("got raw dir file");
                            ProcessUnencryptedTexts(dir, mod.path);
                        }
                    }
                    
                }
            }catch (Exception e)
            {
                Logger.LogWarning(e.Message);
            }
        }

        private void ProcessUnencryptedTexts(string dir, string modDir)
        {
            for (int i = 0; i < ExtEnum<InGameTranslator.LanguageID>.values.Count; i++)
            {
                IteratorKit.Logger.LogInfo("Encypting text files");
                InGameTranslator.LanguageID languageID = InGameTranslator.LanguageID.Parse(i);
                string langDir = Path.Combine(dir, $"Text_{LocalizationTranslator.LangShort(languageID)}").ToLowerInvariant();
                //string langDir = string.Concat(new string[]
                //   {
                //    dir,
                //    Path.DirectorySeparatorChar.ToString(),
                //    "Text",
                //    Path.DirectorySeparatorChar.ToString(),
                //    "Text_",
                //    LocalizationTranslator.LangShort(languageID),
                //    Path.DirectorySeparatorChar.ToString()
                //   }).ToLowerInvariant();
                IteratorKit.Logger.LogInfo($"Checking lang dir {langDir}");
                IteratorKit.Logger.LogWarning(Directory.Exists(langDir));
                if (Directory.Exists(langDir))
                {
                    string[] files = Directory.GetFiles(langDir, "*.txt", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        IteratorKit.Logger.LogInfo($"Encrypting file at ${file}");
                        string result = InGameTranslator.EncryptDecryptFile(file, true, true);
                        IteratorKit.Logger.LogInfo(result);
                        SaveEncryptedText(modDir, languageID, result, Path.GetFileName(file));
                    }

                }
            }
        }

        private void SaveEncryptedText(string modDir, InGameTranslator.LanguageID langId, string encryptedText, string origFileName)
        {
            string modTexts = Path.Combine(modDir, "text", $"Text_{LocalizationTranslator.LangShort(langId)}").ToLowerInvariant();
            if (!Directory.Exists(modTexts))
            {
                Logger.LogWarning($"Creating texts directory for mod dir {modTexts}");
                Directory.CreateDirectory(modTexts);
            }
            string encryptedLangFilePath = Path.Combine(modTexts, origFileName).ToLowerInvariant();
            Logger.LogInfo($"Writing file to: {encryptedLangFilePath}");
            File.WriteAllText(encryptedLangFilePath, encryptedText, encoding: Encoding.UTF8);
            Logger.LogInfo("Wrote encrypted text file.");
            
        }

        private void InformOfInvalidShortcutError(On.ShortcutHandler.orig_Update orig, ShortcutHandler self)
        {
            try
            {
                orig(self);
            }
            catch (IndexOutOfRangeException e)
            {
                CMOracleDebugUI.ModWarningText("ROOM SHORTCUTS ARE NOT SETUP CORRECTLY. this is a kind message just to let you know from iteratorkit :).", self.game.rainWorld);
                ExceptionDispatchInfo.Capture(e).Throw(); // re-throw the error
            }

        }

    }
}