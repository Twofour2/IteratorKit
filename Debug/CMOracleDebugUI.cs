using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using static System.Net.Mime.MediaTypeNames;
using IteratorKit.CMOracle;
using IteratorKit.Util;

namespace IteratorKit.Debug
{
    public class CMOracleDebugUI
    {

        private static FLabel warningLabel = null;
        private static float warningTimeout = 0f;
        public bool debugUIActive = false;
        private FTextParams fontParams = new FTextParams();
        private IteratorKit iteratorKit;
        private Dictionary<CMOracle.CMOracle, FLabel> debugLabels = new Dictionary<CMOracle.CMOracle, FLabel>();
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
        }


        public void DisableDebugUI()
        {
            this.debugUIActive = false;
            this.ClearDebugLabels();
            On.RainWorldGame.RawUpdate -= Update;
        }

        public FLabel AddDebugLabel(RainWorld rainWorld, CMOracle.CMOracle oracle)
        {
            FLabel debugLabel = new FLabel(Custom.GetFont(), "Loading debug ui...", fontParams);
            if (this.debugLabels.Count > 0)
            {
                debugLabel.y = this.debugLabels.Last().Value.textRect.yMax - 10f;
            }
            else
            {
                debugLabel.y = rainWorld.options.ScreenSize.y - 270;

            }
            debugLabel.x = 10f;


            debugLabel.color = new Color(0.8f, 0.2f, 0.9f);
            debugLabel.alpha = 1f;
            debugLabel.isVisible = true;
            debugLabel.alignment = FLabelAlignment.Left;
            debugLabel.scale = 1.1f;


            Futile.stage.AddChild(debugLabel);
            debugLabel.MoveToFront();
            this.debugLabels.Add(oracle, debugLabel);
            return debugLabel;
        }

        public void ClearDebugLabels()
        {
            foreach (KeyValuePair<CMOracle.CMOracle, FLabel> label in this.debugLabels)
            {
                Futile.stage.RemoveChild(label.Value);
            }
            this.debugLabels = new Dictionary<CMOracle.CMOracle, FLabel>();
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
                if (!this.debugLabels.TryGetValue(cmOracle, out debugLabel)) {
                    debugLabel = this.AddDebugLabel(self.rainWorld, cmOracle);
                };


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
        }
        public string BuildCMOracleSitBehaviorDebug(Oracle cmOracle, CMOracleSitBehavior cmBehavior)
        {
            //  CMOracle.CMConversation cmConversation = cmBehavior.cmConversation;
            // Conversation.DialogueEvent dialogueEvent = cmConversation?.events?.FirstOrDefault();

            IntVector2 tileCoord = cmOracle.room.GetTilePosition(cmOracle.firstChunk.pos);
            string oracleSection = $"\nOracleID: {cmOracle.ID}" +
                $"\nType: {cmOracle.OracleJson().type}" +
                $"\nRoom: {cmOracle.room.abstractRoom.name}" +
                $"\nTilePosX: {tileCoord.x} TilePosY: {tileCoord.y}" +
                $"\nGlobalX: {(int)cmOracle.bodyChunks.First().pos.x} GlobalY: {(int)cmOracle.bodyChunks.First().pos.y}" +
                $"\nTargetX: {cmOracle.oracleBehavior.OracleGetToPos.x} TargetY: {cmOracle.oracleBehavior.OracleGetToPos.y}" +
                $"\nTargetDir: {cmOracle.oracleBehavior.GetToDir.GetAngle()}" +
                $"\nIs In Sitting Position: {cmBehavior.InSitPosition}";
               // $"\nMovement: {cmBehavior.movement}";

            //string actionSection = $"\n----" +
            //    $"\n## Last Action:" +
            //    $"\nLast Action: {cmBehavior.lastAction}" +
            //    $"\nLast Action Param: {cmBehavior.lastActionParam}" +
            //    $"\nAction Timer: {cmBehavior.inActionCounter}" +
            //    $"\nItem: {cmBehavior.inspectItem}" +
            //    $"\nMovement: {cmBehavior.movementBehavior}";

            //string conversationSection = $"\n---" +
            //    $"\n## Conversation:" +
            //    $"\nConversationID: {cmConversation?.id}" +
            //    $"\nEventID: {cmConversation?.eventId}" +
            //    $"\nCurrent Event: {dialogueEvent?.GetType()}" +
            //    $"\nEvent Hold: {dialogueEvent?.initialWait}" +
            //    $"\nEvent Age: {dialogueEvent?.age}" +
            //    $"\nEvent Counter: {cmConversation?.events?.Count ?? 0}" +
            //    $"\nPlayer Score: {cmBehavior?.playerScore}" +
            //    $"\nPaused? {cmConversation?.paused}" +
            //    $"\nResume To: {cmBehavior?.conversationResumeTo?.id}" +
            //    $"\nHas Had Main Player Conversation? [6 Key to remove] {cmBehavior.hadMainPlayerConversation}";

            //string playerSection = $"\n---" +
            //    $"\n## Player: " +
            //    $"\n{cmBehavior.player}" +
            //    $"\nOut Of Room: {cmBehavior.playerOutOfRoomCounter}" +
            //    $"\nPlayer Karma: {cmBehavior.player.Karma}" +
            //    $"\nPosX: {cmBehavior.player.abstractPhysicalObject.pos.x} PosY: {cmBehavior.player.abstractPhysicalObject.pos.y}" +
            //    $"\nGlobalX: {cmBehavior.player.bodyChunks.First().pos.x} GlobalY: {cmBehavior.player.bodyChunks.First().pos.y}" +
            //    $"\nStory Session: {cmOracle.room.game.GetStorySession.saveStateNumber}";


            return $"{oracleSection}";//{playerSection}{actionSection}{conversationSection}";
        }

        public string BuildCMOracleBehaviorDebug(Oracle cmOracle, CMOracleBehavior cmBehavior) 
        {

            CMOracle.CMConversation cmConversation = cmBehavior.cmConversation;
            Conversation.DialogueEvent dialogueEvent = cmConversation?.events?.FirstOrDefault();

            string oracleSection = $"\nOracleID: {cmOracle.ID}" +
                $"\nRoom: {cmOracle.room.abstractRoom.name}" +
                $"\nPosX: {cmOracle.abstractPhysicalObject.pos.x} PosY: {cmOracle.abstractPhysicalObject.pos.x}" +
                $"\nGlobalX: {(int)cmOracle.bodyChunks.First().pos.x} GlobalY: {(int)cmOracle.bodyChunks.First().pos.y}" +
                $"\nTargetX: {cmOracle.oracleBehavior.OracleGetToPos.x} TargetY: {cmOracle.oracleBehavior.OracleGetToPos.y}" +
                $"\nMovement: {cmBehavior.movement}";

            string actionSection = $"\n----" +
                $"\n## Last Action:" +
                $"\nLast Action: {cmBehavior.lastAction}" +
                $"\nLast Action Param: {cmBehavior.lastActionParam}" +
                $"\nAction Timer: {cmBehavior.inActionCounter}" +
                $"\nItem: {cmBehavior.inspectItem}" +
                $"\nMovement: {cmBehavior.movementBehavior}";

            string conversationSection = $"\n---" +
                $"\n## Conversation:" +
                $"\nConversationID: {cmConversation?.id}" +
                $"\nEventID: {cmConversation?.eventId}" +
                $"\nCurrent Event: {dialogueEvent?.GetType()}" +
                $"\nEvent Hold: {dialogueEvent?.initialWait}" +
                $"\nEvent Age: {dialogueEvent?.age}" +
                $"\nEvent Counter: {cmConversation?.events?.Count ?? 0}" +
                $"\nPlayer Score: {cmBehavior?.playerScore}" +
                $"\nPaused? {cmConversation?.paused}" +
                $"\nResume To: {cmBehavior?.conversationResumeTo?.id}" +
                $"\nHas Had Main Player Conversation? [6 Key to remove] {cmBehavior.hadMainPlayerConversation}";

            string playerSection = $"\n---" +
                $"\n## Player: " +
                $"\n{cmBehavior.player}" +
                $"\nOut Of Room: {cmBehavior.playerOutOfRoomCounter}" +
                $"\nPlayer Karma: {cmBehavior.player.Karma}" +
                $"\nPosX: {cmBehavior.player.abstractPhysicalObject.pos.x} PosY: {cmBehavior.player.abstractPhysicalObject.pos.y}" +
                $"\nGlobalX: {cmBehavior.player.bodyChunks.First().pos.x} GlobalY: {cmBehavior.player.bodyChunks.First().pos.y}" +
                $"\nStory Session: {cmOracle.room.game.GetStorySession.saveStateNumber}";


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
