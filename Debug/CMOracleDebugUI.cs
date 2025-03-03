﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using static System.Net.Mime.MediaTypeNames;
using IteratorKit.CMOracle;
using IteratorKit.Util;
using IteratorKit.SSOracle;

namespace IteratorKit.Debug
{
    public class CMOracleDebugUI
    {

        private static FLabel warningLabel = null;
        private static float warningTimeout = 0f;
        public bool debugUIActive = false;
        private FTextParams fontParams = new FTextParams();
        private IteratorKit iteratorKit;
        private Dictionary<Oracle.OracleID, FLabel> debugLabels = new Dictionary<Oracle.OracleID, FLabel>();
        private float debugLabelWidth;
        public CMOracleDebugUI()
        {
        }
        public void EnableDebugUI(RainWorld rainWorld, IteratorKit iteratorKit)
        {
            IteratorKit.Log.LogWarning("enable debug ui");
            this.debugUIActive = true;
            this.iteratorKit = iteratorKit;
            fontParams.lineHeightOffset = 2f;
            On.RainWorldGame.RawUpdate += Update;
            On.Room.ReadyForAI += RoomReady;
        }

        public void DisableDebugUI()
        {
            this.debugUIActive = false;
            this.ClearDebugLabels();
            On.RainWorldGame.RawUpdate -= Update;
            On.Room.ReadyForAI -= RoomReady;
        }

        public FLabel AddDebugLabel(RainWorld rainWorld, Oracle oracle)
        {
            FLabel debugLabel = new FLabel(Custom.GetFont(), "Loading debug ui...", fontParams);
            
            debugLabel.y = rainWorld.options.ScreenSize.y - 350;
            debugLabel.x = 10f;
            if (debugLabels.Count > 0)
            {
                debugLabel.x = (debugLabels.Last().Value.x + 10f);
            }

            if (oracle is CMOracle.CMOracle)
            {
                debugLabel.color = new Color(0.8f, 0.2f, 0.9f);
            }else if (oracle.ID == Oracle.OracleID.SL)
            {
                debugLabel.color = new Color(0.84f, 0.72f, 0.49f);
            }else if (oracle.ID == Oracle.OracleID.SS)
            {
                debugLabel.color = new Color(0f, 0f, 255f);
            }
            else
            {
                debugLabel.color = Color.white;
            }
            
            debugLabel.alpha = 1f;
            debugLabel.isVisible = true;
            debugLabel.alignment = FLabelAlignment.Left;
            debugLabel.scale = 1.1f;


            Futile.stage.AddChild(debugLabel);
            debugLabel.MoveToFront();
            this.debugLabels.Add(oracle.ID, debugLabel);
            return debugLabel;
        }

        public void ClearDebugLabels()
        {
            foreach (KeyValuePair<Oracle.OracleID, FLabel> label in this.debugLabels)
            {
                Futile.stage.RemoveChild(label.Value);
            }
            this.debugLabels = new Dictionary<Oracle.OracleID, FLabel>();
        }

        private void RoomReady(On.Room.orig_ReadyForAI orig, Room self)
        {
            orig(self);
            foreach (FLabel fLabel in this.debugLabels.Values)
            {
                fLabel.MoveToFront(); // prevent from being placed behind the room when the rain cycle is reset
            }
        }


        public void Update(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
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
            if (this.iteratorKit?.oracles == null && !this.debugUIActive)
            {
                return;
            }
            foreach (CMOracle.CMOracle cmOracle in this.iteratorKit.oracles.AllValues())
            {
                FLabel debugLabel;
                if (!this.debugLabels.TryGetValue(cmOracle.ID, out debugLabel)) {
                    debugLabel = this.AddDebugLabel(self.rainWorld, cmOracle);
                }

                if (cmOracle.oracleBehavior is CMOracleBehavior)
                {
                    CMOracleBehavior cmBehavior = cmOracle.oracleBehavior as CMOracleBehavior;
                    debugLabel.text = BuildCMOracleBehaviorDebug(cmOracle, cmBehavior);
                } else if (cmOracle.oracleBehavior is CMOracleSitBehavior)
                {
                    CMOracleSitBehavior cmOracleSitBehavior = cmOracle.oracleBehavior as CMOracleSitBehavior;
                    debugLabel.text = BuildCMOracleSitBehaviorDebug(cmOracle, cmOracleSitBehavior);
                }

            }
            foreach (Oracle overrideOracle in IteratorKit.overrideOracles.AllValues())
            {
                FLabel debugLabel;
                if (!this.debugLabels.TryGetValue(overrideOracle.ID, out debugLabel))
                {
                    debugLabel = this.AddDebugLabel(self.rainWorld, overrideOracle);
                }
                if (overrideOracle.oracleBehavior is CMOracleBehavior)
                {
                    CMOracleBehavior cmBehavior = overrideOracle.oracleBehavior as CMOracleBehavior;
                    debugLabel.text = BuildCMOracleBehaviorDebug(overrideOracle, cmBehavior);
                }
                else if (overrideOracle.oracleBehavior is CMOracleSitBehavior)
                {
                    CMOracleSitBehavior cmOracleSitBehavior = overrideOracle.oracleBehavior as CMOracleSitBehavior;
                    debugLabel.text = BuildCMOracleSitBehaviorDebug(overrideOracle, cmOracleSitBehavior);
                }
            }
        }
        public string BuildCMOracleSitBehaviorDebug(Oracle cmOracle, CMOracleSitBehavior cmBehavior)
        {
            CMOracle.CMConversation cmConversation = cmBehavior.cmMixin.cmConversation;
            Conversation.DialogueEvent dialogueEvent = cmConversation?.events?.FirstOrDefault();
            string conversationQueue = String.Join(", ", cmBehavior.cmMixin.cmConversationQueue.Select(x => x.eventId));

            IntVector2 tileCoord = cmOracle.room.GetTilePosition(cmOracle.firstChunk.pos);
            string oracleSection = $"\nOracleID: {cmOracle.ID}" +
                $"\nType: {cmOracle.OracleJson().type}" +
                $"\nRoom: {cmOracle.room.abstractRoom.name}. FPS: {cmOracle.room.game.framesPerSecond}" +
                $"\nTilePosX: {tileCoord.x} TilePosY: {tileCoord.y}" +
                $"\nGlobalX: {(int)cmOracle.bodyChunks.First().pos.x} GlobalY: {(int)cmOracle.bodyChunks.First().pos.y}" +
                $"\nTargetX: {cmOracle.oracleBehavior.OracleGetToPos.x} TargetY: {cmOracle.oracleBehavior.OracleGetToPos.y}" +
                $"\nTargetDir: {cmOracle.oracleBehavior.GetToDir.GetAngle()}" +
                $"\nIs In Sitting Position: {cmBehavior.InSitPosition}";
            // $"\nMovement: {cmBehavior.movement}";

            string playerSection = "";
            if (cmBehavior.player != null)
            {
               playerSection = $"\n## Player: " +
               $"\n{cmBehavior.player}" +
               $"\nOut Of Room: {cmBehavior.cmMixin.playerOutOfRoomCounter}" +
               $"\nPlayer Karma: {cmBehavior.player.Karma}" +
               $"\nPosX: {cmBehavior.player.abstractPhysicalObject.pos.x} PosY: {cmBehavior.player.abstractPhysicalObject.pos.y}" +
               $"\nGlobalX: {cmBehavior.player.bodyChunks.First().pos.x} GlobalY: {cmBehavior.player.bodyChunks.First().pos.y}" +
               $"\nStory Session: {cmOracle.room.game.GetStorySession.saveStateNumber}";
            }
            else
            {
                playerSection = "\n---\nNo player!";
            }


            string actionSection = $"\n----" +
                $"\n## Last Action:" +
                $"\nLast Action: {cmBehavior.cmMixin.lastAction}" +
                $"\nLast Action Param: {cmBehavior.cmMixin.lastActionParam}" +
                $"\nAction Timer: {cmBehavior.inActionCounter}" +
                $"\nItem: {cmBehavior.inspectItem}" +
                $"\nMovement: {cmBehavior.movementBehavior}";

            string conversationSection = $"\n---" +
                $"\n## Conversation:" +
                $"\n# Queue: {conversationQueue}" +
                $"\nConversationID: {cmConversation?.id}" +
                $"\nEventID: {cmConversation?.eventId}" +
                $"\nCurrent Event: {dialogueEvent?.GetType()}" +
                $"\nEvent Hold: {dialogueEvent?.initialWait}" +
                $"\nEvent Age: {dialogueEvent?.age}" +
                $"\nEvent Counter: {cmConversation?.events?.Count ?? 0}" +
                $"\nPlayer Score: {cmBehavior?.cmMixin.playerScore}" +
                $"\nPaused? {cmConversation?.paused}" +
                $"\nResume To: {cmBehavior?.cmMixin.conversationResumeTo?.id}" +
                $"\nHas Had Main Player Conversation? [6 Key to remove] {cmBehavior.cmMixin.hadMainPlayerConversation}";

            return $"{oracleSection}{playerSection}{actionSection}{conversationSection}";
        }

        public string BuildCMOracleBehaviorDebug(Oracle cmOracle, CMOracleBehavior cmBehavior) 
        {

            CMOracle.CMConversation cmConversation = cmBehavior.cmMixin.cmConversation;
            Conversation.DialogueEvent dialogueEvent = cmConversation?.events?.FirstOrDefault();


            string oracleSection = $"\nOracleID: {cmOracle.ID}" +
                $"\nRoom: {cmOracle.room.abstractRoom.name}. FPS: {cmOracle.room.game.framesPerSecond}" +
                $"\nPosX: {cmOracle.abstractPhysicalObject.pos.x} PosY: {cmOracle.abstractPhysicalObject.pos.x}" +
                $"\nGlobalX: {(int)cmOracle.bodyChunks.First().pos.x} GlobalY: {(int)cmOracle.bodyChunks.First().pos.y}" +
                $"\nTargetX: {cmOracle.oracleBehavior.OracleGetToPos.x} TargetY: {cmOracle.oracleBehavior.OracleGetToPos.y}" +
                $"\nMovement: {cmBehavior.movement}";

            string playerSection = "";
            if (cmBehavior.player != null)
            {
                playerSection = $"\n---" +
                $"\n## Player: " +
                $"\n{cmBehavior.player}" +
                $"\nOut Of Room: {cmBehavior.playerOutOfRoomCounter}" +
                $"\nPlayer Karma: {cmBehavior.player?.Karma}" +
                $"\nPosX: {cmBehavior.player.abstractPhysicalObject.pos.x} PosY: {cmBehavior.player.abstractPhysicalObject.pos.y}" +
                $"\nGlobalX: {cmBehavior.player.bodyChunks.First().pos.x} GlobalY: {cmBehavior.player.bodyChunks.First().pos.y}" +
                $"\nStory Session: {cmOracle.room.game.GetStorySession?.saveStateNumber}";
            }
            else
            {
                playerSection = "\n---\nNo player!";
            }
            
            string actionSection = $"\n----" +
                $"\n## Last Action:" +
                $"\nLast Action: {cmBehavior.cmMixin.lastAction}" +
                $"\nLast Action Param: {cmBehavior.cmMixin.lastActionParam}" +
                $"\nAction Timer: {cmBehavior.cmMixin.inActionCounter}" +
                $"\nItem: {cmBehavior.cmMixin.inspectItem}" +
                $"\nMovement: {cmBehavior.movementBehavior}";

            string conversationSection = $"\n---" +
                $"\n## Conversation:" +
                $"\nConversationID: {cmConversation?.id}" +
                $"\nEventID: {cmConversation?.eventId}" +
                $"\nCurrent Event: {dialogueEvent?.GetType()}" +
                $"\nEvent Hold: {dialogueEvent?.initialWait}" +
                $"\nEvent Age: {dialogueEvent?.age}" +
                $"\nEvent Counter: {cmConversation?.events?.Count ?? 0}" +
                $"\nPlayer Score: {cmBehavior.cmMixin?.playerScore}" +
                $"\nPaused? {cmConversation?.paused}" +
                $"\nResume To: {cmBehavior.cmMixin?.conversationResumeTo?.id}" +
                $"\nHas Had Main Player Conversation? [6 Key to remove] {cmBehavior.cmMixin.hadMainPlayerConversation}";

            return $"{oracleSection}{playerSection}{actionSection}{conversationSection}";
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
    }

    
}
