using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using IteratorMod.CM_Oracle;
using IteratorMod.SRS_Oracle;
using UnityEngine;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RWCustom;
using System.Text;
using On.Menu;
using Menu;
using MoreSlugcats;
using IteratorMod.SLOracle;
using IteratorMod.CustomPearls;
using System.Linq.Expressions;

namespace IteratorMod
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class IteratorKit : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "twofour2.iteratorKit";
        public const string PLUGIN_NAME = "iteratorKit";
        public const string PLUGIN_DESC = "Toolkit for making custom iterators and adding custom dialogs";
        public const string PLUGIN_VERSION = "1.0.0";

        private bool oracleHasSpawned = false;
        public CMOracle oracle;

        public static new ManualLogSource Logger { get; private set; }

        public List<string> oracleRoomIds = new List<string>();
        public List<OracleJSON> oracleJsonData = new List<OracleJSON>();


        private void OnEnable()
        {
            Logger = base.Logger;

            On.Player.NewRoom += SpawnOracle;
            On.DebugMouse.Update += DebugMouse_Update;
            
            On.Menu.HoldButton.Update += HoldButton_Update;
            On.ShelterDoor.Update += ShelterDoor_Update;
            On.OracleGraphics.Gown.Color += CMOracleGraphics.SRSGown.SRSColor;
            On.RainWorld.PostModsInit += AfterModsInit;
            On.RainWorldGame.RestartGame += OnRestartGame;
            On.Oracle.SetUpSwarmers += CMOracle.SetUpSwarmers;
            SlugBase.SaveData.SaveDataHooks.Apply();
            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            
        }

        private static FLabel warningLabel = null;
        private static float warningTimeout = 0f;

        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (self.devToolsActive) {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    RainWorldGame.ForceSaveNewDenLocation(self, self.FirstAnyPlayer.Room.name, false);
                    ModWarningText($"Save file forced den location to {self.FirstAlivePlayer.Room.name}! Press \"R\" to reload.", self.rainWorld);
                }
            }
            if (warningLabel != null)
            {
                warningTimeout += dt;
                warningLabel.color = new Color(1f, 0f, 0f);
                warningLabel.alpha = 1f;
                if (warningTimeout > 12f)
                {
                    Futile.stage.RemoveChild(warningLabel);
                }
            }
        }

        public static void ModWarningText(string text, RainWorld rainWorld)
        {
            if (warningLabel != null)
            {
                Futile.stage.RemoveChild(warningLabel);
            }
            FTextParams fontParams = new FTextParams();
            fontParams.lineHeightOffset = 2f;
            warningLabel = new FLabel(Custom.GetFont(), text, fontParams);
            warningLabel.x = rainWorld.options.ScreenSize.x / 2f + 0.01f + 1f;
            warningLabel.y = 755.01f;
            warningLabel.color = new Color(1f, 0f, 0f);
            warningLabel.alpha = 1f;
            warningLabel.isVisible = true;
            warningLabel.SetAnchor(warningLabel.anchorX, warningLabel.anchorY * 4f);
            
            Futile.stage.AddChild(warningLabel);
            warningLabel.MoveToFront();
            warningTimeout = 0f;
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
                oracleRoomIds = new List<string>();
                oracleJsonData = new List<OracleJSON>();
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                {
                    string[] files = Directory.GetFiles(mod.path);
                    foreach (string file in files)
                    {
                        try
                        {
                            if (file.EndsWith("oracle.json"))
                            {
                                List<OracleJSON> ojs = JsonConvert.DeserializeObject<List<OracleJSON>>(File.ReadAllText(file));

                                foreach (OracleJSON oracleData in ojs)
                                {
                                    Logger.LogWarning(oracleData);
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
                                }
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
                                ModWarningText($"Encountered an error while loading data file {file} from mod ${mod.name}.\n\n${e.Message}", rainWorld);
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



        private void ShelterDoor_Update(On.ShelterDoor.orig_Update orig, ShelterDoor self, bool eu)
        {
            
            orig(self, eu);
            self.openTime = 1;
            self.openUpTicks = 10;
        }

        private void HoldButton_Update(On.Menu.HoldButton.orig_Update orig, Menu.HoldButton self)
        {
            orig(self);
            if (self.held)
            {
                self.Singal(self, self.signalText);
                self.hasSignalled = true;
                self.menu.ResetSelection();
            }
        }

        private void SpawnOracle(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            foreach (string roomId in this.oracleRoomIds)
            {
                Logger.LogWarning(roomId);
            }
            try
            {
                if (this.oracleRoomIds.Contains(newRoom.roomSettings.name))
                {
                    OracleJSON oracleJson = this.oracleJsonData.Find(x => x.roomId == newRoom.roomSettings.name);

                    if (oracleJson.forSlugcats.Contains(newRoom.game.GetStorySession.saveStateNumber))
                    {
                        newRoom.oracleWantToSpawn = Oracle.OracleID.SL;

                        IteratorKit.Logger.LogWarning($"Found matching room, spawning oracle {oracleJson.id}");
                        newRoom.loadingProgress = 3;
                        newRoom.readyForNonAICreaturesToEnter = true;
                        WorldCoordinate worldCoordinate = new WorldCoordinate(newRoom.abstractRoom.index, 15, 15, -1);
                        AbstractPhysicalObject abstractPhysicalObject = new AbstractPhysicalObject(
                            newRoom.world,
                            global::AbstractPhysicalObject.AbstractObjectType.Oracle,
                            null,
                            worldCoordinate,
                            newRoom.game.GetNewID());

                        oracle = new CMOracle(abstractPhysicalObject, newRoom, oracleJson);
                        newRoom.AddObject(oracle);
                        newRoom.waitToEnterAfterFullyLoaded = Math.Max(newRoom.waitToEnterAfterFullyLoaded, 20);
                    }
                    else
                    {
                        Logger.LogWarning($"{oracleJson.id} Oracle is not avalible for the current slugcat");
                    }

                }
            }catch (Exception e)
            {
                ModWarningText($"Iterator Kit Initialization Error: {e}", newRoom.game.rainWorld);
                IteratorKit.Logger.LogError(e);
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

    }
}