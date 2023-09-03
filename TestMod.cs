using System;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Logging;
using IteratorMod.SRS_Oracle;
using UnityEngine;

namespace IteratorMod
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
    public class TestMod : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "twofour2.testmod";
        public const string PLUGIN_NAME = "Test mod";
        public const string PLUGIN_VERSION = "1.0.0";

        private bool oracleHasSpawned = false;
        public SRSOracle oracle;

        public static new ManualLogSource Logger { get; private set; }


        private void OnEnable()
        {
            Logger = base.Logger;
            Debug.Log("demo mod load2");
            Logger.LogWarning("Mod loaded2");

            On.Player.NewRoom += SpawnOracle;
           // On.DebugMouse.Update += DebugMouse_Update;
            On.Menu.HoldButton.Update += HoldButton_Update;
            On.ShelterDoor.Update += ShelterDoor_Update;
            On.OracleGraphics.Gown.Color += SRSOracleGraphics.SRSGown.SRSColor;
        }

        private void ShelterDoor_Update(On.ShelterDoor.orig_Update orig, ShelterDoor self, bool eu)
        {
            self.openTime = 1;
            self.openUpTicks = 10;

        }

        private void HoldButton_Update(On.Menu.HoldButton.orig_Update orig, Menu.HoldButton self)
        {
            //self.fillTime = 0.01f;

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
            Logger.LogInfo(newRoom.roomSettings.name);
            
            if (newRoom.roomSettings.name == "SU_ai" && !oracleHasSpawned)
            {
                //Logger.LogInfo("Spawning oracle?");
                oracleHasSpawned = true;
                newRoom.abstractRoom.name = "SU_ai";
                newRoom.loadingProgress = 3;
                newRoom.readyForNonAICreaturesToEnter = true;
                newRoom.oracleWantToSpawn = Oracle.OracleID.SL;
                for (int j = 0; j < newRoom.updateList.Count; j++)
                {
                    if (newRoom.updateList[j] is INotifyWhenRoomIsReady)
                    {
                        (newRoom.updateList[j] as INotifyWhenRoomIsReady).AIMapReady();
                    }
                }

                WorldCoordinate worldCoordinate = new WorldCoordinate(newRoom.abstractRoom.index, 15, 15, -1);
                AbstractPhysicalObject abstractPhysicalObject = new AbstractPhysicalObject(
                    newRoom.world,
                    global::AbstractPhysicalObject.AbstractObjectType.Oracle,
                    null,
                    worldCoordinate,
                    newRoom.game.GetNewID());

                oracle = new SRSOracle(abstractPhysicalObject, newRoom);
                newRoom.AddObject(oracle);
                newRoom.waitToEnterAfterFullyLoaded = Math.Max(newRoom.waitToEnterAfterFullyLoaded, 20);

                newRoom.abstractRoom.name = "SU_ai";

                //Logger.LogWarning(oracle.ToString());
                //Logger.LogInfo(newRoom.oracleWantToSpawn);
                //orig(self, newRoom);

            }
            else
            {
                Logger.LogInfo("Not spawning oracle");
            }
           
        }

        public void DebugMouse_Update(On.DebugMouse.orig_Update orig, DebugMouse self, bool eu)
        {
            
            orig(self, eu);
            if (oracleHasSpawned)
            {
                TestMod.Logger.LogWarning("set oracle dest");
                TestMod.LogVector2(self.pos);
                oracle.oracleBehavior.SetNewDestination(self.pos);
            }
            
        }

        public static void LogVector2(Vector2 vector)
        {
            Logger.LogInfo($"x: {vector.x} y: {vector.y}");
        }

    }
}