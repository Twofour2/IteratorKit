using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using static System.Net.Mime.MediaTypeNames;
using IteratorKit.CMOracle;

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
        // private float debugLabelWidth;
        public CMOracleDebugUI()
        {
        }
        public void EnableDebugUI(RainWorld rainWorld, IteratorKit iteratorKit)
        {
            IteratorKit.Logger.LogWarning("enable debug ui");
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
            if (this.iteratorKit?.oracleList == null && !this.debugUIActive)
            {
                return;
            }
            foreach (CMOracle.CMOracle cMOracle in this.iteratorKit.oracleList)
            {
                FLabel debugLabel;
                if (!this.debugLabels.TryGetValue(cMOracle, out debugLabel)){
                    debugLabel = this.AddDebugLabel(self.rainWorld, cMOracle);
                };

                CMOracle.CMOracleBehavior cMBehavior = cMOracle.oracleBehavior as CMOracle.CMOracleBehavior;
                CMOracle.CMConversation cMConversation = cMBehavior.cmConversation;
                Conversation.DialogueEvent dialogueEvent = cMConversation?.events?.FirstOrDefault();

                string oracleSection = $"\nOracleID: {cMOracle.ID}" +
                    $"\nRoom: {cMOracle.room.abstractRoom.name}" +
                    $"\nPosX: {cMOracle.abstractPhysicalObject.pos.x} PosY: {cMOracle.abstractPhysicalObject.pos.x}" +
                    $"\nGlobalX: {(int)cMOracle.bodyChunks.First().pos.x} GlobalY: {(int)cMOracle.bodyChunks.First().pos.y}";

                string actionSection = $"\n----" +
                    $"\n## Action:" +
                    $"\nAction: {cMBehavior.action} ({cMBehavior.actionStr})" +
                    $"\nAction Param: {cMBehavior.actionParam}" +
                    $"\nAction Timer: {cMBehavior.inActionCounter}" +
                    $"\nItem: {cMBehavior.inspectItem}" +
                    $"\nMovement: {cMBehavior.movementBehavior}";

                string conversationSection = $"\n---" +
                    $"\n## Conversation:" +
                    $"\nConversationID: {cMConversation?.id}" +
                    $"\nEventID: {cMConversation?.eventId}" +
                    $"\nCurrent Event: {dialogueEvent?.GetType()}" +
                    $"\nEvent Hold: {dialogueEvent?.initialWait}" +
                    $"\nEvent Age: {dialogueEvent?.age}" +
                    $"\nEvent Counter: {cMConversation?.events?.Count ?? 0}" +
                    $"\nPlayer Score: {cMBehavior?.playerScore}" +
                    $"\nPaused? {cMConversation?.paused}" +
                    $"\nResume To: {cMBehavior?.conversationResumeTo?.id}" +
                    $"\nHas Had Main Player Conversation? [6 Key to remove] {cMBehavior.HasHadMainPlayerConversation()}";

                string playerSection = $"\n---" +
                    $"\n## Player: " +
                    $"\n{cMBehavior.player}" +
                    $"\nOut Of Room: {cMBehavior.playerOutOfRoomCounter}" +
                    $"\nPlayer Karma: {cMBehavior.player.Karma}" +
                    $"\nPosX: {cMBehavior.player.abstractPhysicalObject.pos.x} PosY: {cMBehavior.player.abstractPhysicalObject.pos.y}" +
                    $"\nGlobalX: {cMBehavior.player.bodyChunks.First().pos.x} GlobalY: {cMBehavior.player.bodyChunks.First().pos.y}" +
                    $"\nStory Session: {cMOracle.room.game.GetStorySession.saveStateNumber}";


                debugLabel.text = $"{oracleSection}{actionSection}{conversationSection}{playerSection}";
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
    }

    
}
