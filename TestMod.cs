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

namespace IteratorMod
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class TestMod : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "twofour2.testmod";
        public const string PLUGIN_NAME = "Test mod";
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
            On.RainWorld.PostModsInit += TestOnLoad;
            On.Oracle.SetUpSwarmers += CMOracle.SetUpSwarmers;

        }


        private void TestOnLoad(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            orig(self);
            EncryptDialogFiles();
            try
            {
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                {
                    string[] files = Directory.GetFiles(mod.path);
                    foreach (string file in files)
                    {
                        if (file.EndsWith("oracle.json"))
                        {
                            List<OracleJSON> ojs = JsonConvert.DeserializeObject<List<OracleJSON>>(File.ReadAllText(file));

                            foreach (OracleJSON oracleData in ojs)
                            {
                                Logger.LogWarning(oracleData);
                                oracleJsonData.Add(oracleData);
                                oracleRoomIds.Add(oracleData.roomId);
                            }
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

            if (this.oracleRoomIds.Contains(newRoom.roomSettings.name))
            {
                if (newRoom.oracleWantToSpawn == Oracle.OracleID.SL)
                {
                    // avoid spawning the oracle multiple times
                    Logger.LogWarning("oracle already spawned");
                    return;
                }
                OracleJSON oracleJson = this.oracleJsonData.Find(x => x.roomId == newRoom.roomSettings.name);

                newRoom.oracleWantToSpawn = Oracle.OracleID.SL;

                TestMod.Logger.LogWarning($"Found matching room, spawning oracle {oracleJson.id}");
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


                //// spawn test pearl
                //AbstractPhysicalObject pearlPhsyObject = new AbstractPhysicalObject(
                //    newRoom.world,
                //    global::AbstractPhysicalObject.AbstractObjectType.DataPearl,
                //    null,
                //    worldCoordinate,
                //    newRoom.game.GetNewID());
                //DataPearl dataPearl = new DataPearl(pearlPhsyObject, newRoom.world);
                //dataPearl.PlaceInRoom(newRoom);
                //dataPearl.AbstractPearl.dataPearlType = DataPearl.AbstractDataPearl.DataPearlType.LF_west;


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
                TestMod.Logger.LogWarning("Encrypting all dialog files");
                foreach (ModManager.Mod mod in ModManager.ActiveMods)
                {
                    string[] dirs = Directory.GetDirectories(mod.path);
                    foreach (string dir in dirs)
                    {
                        if (dir.EndsWith("text_raw"))
                        {
                            TestMod.Logger.LogInfo("got raw dir file");
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
                TestMod.Logger.LogInfo("Encypting text files");
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
                TestMod.Logger.LogInfo($"Checking lang dir {langDir}");
                TestMod.Logger.LogWarning(Directory.Exists(langDir));
                if (Directory.Exists(langDir))
                {
                    string[] files = Directory.GetFiles(langDir, "*.txt", SearchOption.AllDirectories);
                    foreach (string file in files)
                    {
                        TestMod.Logger.LogInfo($"Encrypting file at ${file}");
                        string result = InGameTranslator.EncryptDecryptFile(file, true, true);
                        TestMod.Logger.LogInfo(result);
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