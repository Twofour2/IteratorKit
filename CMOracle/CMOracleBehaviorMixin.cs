using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.Util;
using Newtonsoft.Json.Linq;
using RWCustom;
using UnityEngine;
using static IteratorKit.CMOracle.CMOracleBehavior;
using static IteratorKit.CMOracle.OracleJData.OracleEventsJData;

namespace IteratorKit.CMOracle
{
    /// <summary>
    /// Shared Custom Oracle Behavior Logic (Mixin)
    /// Enables logic to be shared between CMOracleBehavior (Pebbles type) and CMOracleSitBehavior (moon type)
    /// We do this via a conditional weak table defined in CMOracleModule. This is stored alongside the shared base class OracleBehavior
    /// This done so that the other two classes can inherit the behaviors of their subclasses (SSOracleBehavior, SLOracleBehavior + derivatives)
    /// 
    /// Call any oracleBehavior.OracleBehaviorMixin() to get access to this
    /// 
    /// </summary>
    public class CMOracleBehaviorMixin
    {
        /// <summary>
        /// Reference back to owner, usually CMOracleBehavior or CMOracleSitBehavior
        /// </summary>
        public OracleBehavior owner;
        public int playerScore = 20;
        public int sayHelloDelay = 30;
        public float roomGravity = 0.9f;
        public bool hasSaidByeToPlayer, rainInterrupt, playerRelationshipJustChanged, alreadyDiscussedDeadPlayer, playerAlreadyHoldingSLNeuron, hasConvToResumeTo = false;
        public bool hasNoticedPlayer;
        public int playerOutOfRoomCounter, timeSinceSeenPlayer;
        public LinkedList<CMConversation> cmConversationQueue;
        public CMConversation conversationResumeTo = null;

        public CMConversation cmConversation
        {
            get
            {
                return this.cmConversationQueue.FirstOrDefault();
            }
        }

        public CMOracleAction action;
        private string actionParam;
        public int inActionCounter, playerAnnoyingCounter = 0;
        public PhysicalObject inspectItem;
        public List<AbstractPhysicalObject> alreadyDiscussedItems = new List<AbstractPhysicalObject>();
        public CMOracleScreen cmScreen;

        public CMOracleAction lastAction; // only for debug ui
        public string lastActionParam; // only for debug ui

        public Oracle oracle { get { return owner.oracle; } set { owner.oracle = value; } }
        public Player player { get { return owner.player; } set { owner.player = value; } }

        public OracleJData oracleJson { get { return owner.oracle.OracleJson(); } }

        public bool hadMainPlayerConversation
        {
            get { return ITKUtil.GetSaveDataValue<bool>(owner.oracle.room.game.session as StoryGameSession, owner.oracle.ID, "hasHadPlayerConversation", false); }
            set { ITKUtil.SetSaveDataValue<bool>(owner.oracle.room.game.session as StoryGameSession, owner.oracle.ID, "hasHadPlayerConversation", value); }
        }

        public CMPlayerRelationship playerRelationship
        {
            get { return (CMPlayerRelationship)ITKUtil.GetSaveDataValue<int>(owner.oracle.room.game.session as StoryGameSession, owner.oracle.ID, "playerRelationship", (int)CMPlayerRelationship.normal); }
            set { ITKUtil.SetSaveDataValue<int>(owner.oracle.room.game.session as StoryGameSession, owner.oracle.ID, "playerRelationship", (int)value); }
        }

        public CMOracleBehaviorMixin(OracleBehavior oracleBehavior) {
            this.owner = oracleBehavior;
            this.cmConversationQueue = new LinkedList<CMConversation>();
            this.oracle.OracleEvents().OnCMEventStart += this.DialogEventActivate;
            this.cmScreen = new CMOracleScreen(this);
        }

        public void StartConversation(CMConversation.CMDialogCategory category, string eventId, bool interrupt = false, DataPearl.AbstractDataPearl.DataPearlType pearlType = null) {
            IteratorKit.Log.LogInfo($"Add conversation {eventId} to queue. Interrupt: ${interrupt}");
            if (this.cmConversationQueue.Any(x => x.eventId == eventId))
            {
                IteratorKit.Log.LogInfo($"Not adding conversation {eventId} as it already exists in the queue");
                return;
            }
            if (interrupt && this.cmConversationQueue.Count > 0)
            {
                // already waiting for a conversation to resume to, dont stack any futher
                if (this.cmConversationQueue.Any(x => x.eventId == "conversationResume"))
                {
                    return;
                }
                this.hasConvToResumeTo = true; // removed once conversationResume is done
                this.conversationResumeTo = this.cmConversation; // store current conversation
                this.cmConversation.InterruptQuickHide(); // quick hide current diag
                // queue will look like -> ["interrupt", "conversationResume", "origEvent"]
                this.cmConversationQueue.AddFirst(new CMConversation(owner, CMConversation.CMDialogCategory.Generic, "conversationResume"));
                this.cmConversationQueue.AddFirst(new CMConversation(owner, category, eventId, pearlType));
                return;
            }
            this.cmConversationQueue.AddLast(new CMConversation(owner, category, eventId, pearlType));
        }

        public void CheckConversationEvents()
        {
            if (this.player == null || !this.hasNoticedPlayer)
            {
                return;
            }
            if (this.sayHelloDelay < 0)
            {
                this.sayHelloDelay = 30;
            }
            else
            {
                if (this.sayHelloDelay > 0)
                {
                    this.sayHelloDelay--;
                }
                if (this.sayHelloDelay == 1)
                {
                    this.oracle.room.game.cameras[0].EnterCutsceneMode(this.player.abstractCreature, RoomCamera.CameraCutsceneType.Oracle);
                    IteratorKit.Log.LogInfo($"Has had main conversation? {this.hadMainPlayerConversation}");
                    if (!this.hadMainPlayerConversation && (this.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark)
                    {
                        IteratorKit.Log.LogInfo("Starting main player conversation");
                        this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerConversation");
                    }
                    else
                    {
                        this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerEnter");
                    }
                }
            }

            if (this.player.dead && !this.alreadyDiscussedDeadPlayer)
            {
                this.alreadyDiscussedDeadPlayer = true;
                this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerDead");
            }
            if (!this.PlayerInRoom())
            {
                this.playerOutOfRoomCounter++;
                if (!this.hasSaidByeToPlayer)
                {
                    this.hasSaidByeToPlayer = true;
                    if (this.cmConversation != null)
                    {
                        if (this.cmConversation.eventId != "conversationResume") // dont interrupt the interrupt
                        {
                            this.cmConversation.InterruptQuickHide();
                            this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerLeaveInterrupt", true);
                        }
                    }
                    else
                    {
                        this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerLeave", true);
                    }
                }

                if (this.owner is CMOracleSitBehavior)
                {
                    if (Custom.DistLess(this.player.mainBodyChunk.pos, this.oracle.firstChunk.pos, 100) && !Custom.DistLess(this.player.mainBodyChunk.lastPos, this.player.mainBodyChunk.pos, 1f)){
                        this.playerAnnoyingCounter ++;
                    }
                    else
                    {
                        this.playerAnnoyingCounter--;
                    }
                }
                this.playerAnnoyingCounter = Custom.IntClamp(this.playerAnnoyingCounter, 0, 150);
            }
            else
            {
                if (this.cmConversation == null && this.playerOutOfRoomCounter > 100)
                {
                    this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerReturn");
                }
                this.playerOutOfRoomCounter = 0;
                this.hasSaidByeToPlayer = false;
            }
            if (!this.rainInterrupt && this.PlayerInRoom() && this.oracle.room.world.rainCycle.TimeUntilRain < 1600 && this.oracle.room.world.rainCycle.pause < 1)
            {
                if (this.cmConversation != null)
                {
                    this.StartConversation(CMConversation.CMDialogCategory.Generic, "rain");
                    this.rainInterrupt = true;
                }
            }
            bool playerHoldSLNeuron = this.player.grasps.Any(x => x != null && x.grabbed is SLOracleSwarmer);
            if (playerHoldSLNeuron && !this.playerAlreadyHoldingSLNeuron)
            {
                if (this.HasEvent("playerTakeNeuron")) // dont play unless we actually have this event
                {
                    this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerTakeNeuron", true);
                    this.playerAlreadyHoldingSLNeuron = true;
                }
                
            }
            else if(!playerHoldSLNeuron)
            {
                if (this.playerAlreadyHoldingSLNeuron)
                {
                    if (this.HasEvent("playerReleaseNeuron") && this.conversationResumeTo != null) // only plays if LTTM doesn't want to return to a conversation
                    {
                        if (this.cmConversation.eventId == "playerTakeNeuron")
                        {
                            this.cmConversationQueue.RemoveFirst(); // remove playerTakeNeuron event
                        }
                        if (this.cmConversation.eventId != "playerReleaseNeuron")
                        {
                            this.cmConversationQueue.AddFirst(new CMConversation(owner, CMConversation.CMDialogCategory.Generic, "playerReleaseNeuron"));
                        }
                        else
                        {
                            this.cmConversation.RestartCurrent();
                        }
                    }
                }
                this.playerAlreadyHoldingSLNeuron = false;
            }
            if (this.playerRelationship != CMPlayerRelationship.normal && this.playerRelationshipJustChanged)
            {
                this.playerRelationshipJustChanged = false;
                switch (this.playerRelationship)
                {
                    case CMPlayerRelationship.friend:
                        this.StartConversation(CMConversation.CMDialogCategory.Generic, "oracleFriend");
                        break;
                    case CMPlayerRelationship.annoyed:
                        this.StartConversation(CMConversation.CMDialogCategory.Generic, "oracleAnnoyed");
                        break;
                    case CMPlayerRelationship.angry:
                        this.StartConversation(CMConversation.CMDialogCategory.Generic, "oracleAngry");
                        break;
                }
            }

            
        }

        public void Update()
        {
            bool playerHoldSLNeuron = this.player.grasps.Any(x => x != null && x.grabbed is SLOracleSwarmer);
            if (playerHoldSLNeuron)
            {
                // dont continue talking until player releases the neuron
                return;
            }
            if (this.cmConversation != null && (this.playerOutOfRoomCounter == 0 || this.cmConversation.eventId.StartsWith("playerLeave")))
            {
                this.cmConversation.Update();
            }
            this.CheckForConversationDelete();
        }

        /// <summary>
        /// Handler for multi frame actions (any use of this.inActionCounter, or actions that are persistant)
        /// </summary>
        public void CheckActions()
        {
            if (owner.player == null)
            {
                return;
            }
            switch (this.action)
            {
                case CMOracleAction.kickPlayerOut:
                    ShortcutData? shortcut = ITKUtil.GetShortcutToRoom(this.oracle.room, actionParam);
                    if (shortcut == null)
                    {
                        IteratorKit.Log.LogError("Cannot kick player out as destination does not exist!");
                        this.action = CMOracleAction.generalIdle;
                        return;
                    }
                    Vector2 shortcutVector = this.oracle.room.MiddleOfTile(shortcut.Value.startCoord);
                    foreach (Player player in owner.PlayersInRoom)
                    {
                        player.mainBodyChunk.vel += Custom.DirVec(player.mainBodyChunk.pos, shortcutVector);
                        // flag the shortcut as being entered by the player, otherwise they just get stuck being pushed against it
                        if (this.oracle.room.GetTilePosition(player.mainBodyChunk.pos) == this.oracle.room.GetTilePosition(shortcutVector) && player.enteringShortCut == null)
                        {
                            player.enteringShortCut = shortcut.Value.StartTile;
                        }
                    }
                    break;
                case CMOracleAction.giveMark:
                    if (((StoryGameSession)this.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                    {
                        IteratorKit.Log.LogWarning("Player already has mark!");
                        this.action = CMOracleAction.generalIdle;
                        return;
                    }
                    if (this.inActionCounter == 30)
                    {
                        this.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
                    }
                    else if (this.inActionCounter > 30 && this.inActionCounter < 300)
                    {
                        owner.StunCoopPlayers(20);
                        Vector2 holdTarget = Vector2.ClampMagnitude(this.oracle.room.MiddleOfTile(24, 14) - this.player.mainBodyChunk.pos, 40f) / 40f * 2.8f * Mathf.InverseLerp(30f, 160f, (float)this.inActionCounter);
                        this.HoldPlayerAt(holdTarget);
                    }
                    else if (this.inActionCounter == 300)
                    {
                        this.action = CMOracleAction.generalIdle;
                        this.player.AddFood(10);
                        foreach (Player player in owner.PlayersInRoom)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                this.oracle.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                            }
                        }
                        ((StoryGameSession)this.player.room.game.session).saveState.deathPersistentSaveData.theMark = true;
                        this.StartConversation(CMConversation.CMDialogCategory.Generic, "afterGiveMark");
                    }
                    break;
            }
        }

        /// <summary>
        /// Triggers a single frame action or sets up multi frame actions to be run by CheckAction()
        /// </summary>
        /// <param name="action">CMOracleAction to run</param>
        /// <param name="actionParam">See documentation for this event</param>
        public void RunAction(CMOracleAction action, string actionParam = null)
        {
            if (this.player == null)
            {
                return;
            }
            this.lastAction = action;
            this.lastActionParam = actionParam;
            // actions should reset back to CMOracleAction.generalIdle if they wish for future conversations to continue working.
            // actions such as kill/kickOut dont allow any further actions to take place after they have occurred
            switch (action)
            {
                case CMOracleAction.generalIdle:
                    // nothing
                    break;
                case CMOracleAction.giveMark: // multiframe
                    this.action = CMOracleAction.giveMark;
                    this.actionParam = actionParam;
                    break;
                case CMOracleAction.giveKarma:
                    int karmaCap = 0;
                    if (!Int32.TryParse(actionParam, out karmaCap))
                    {
                        IteratorKit.Log.LogError($"Failed to convert action param {actionParam} to integer");
                        break;
                    }
                    StoryGameSession session = ((StoryGameSession)this.oracle.room.game.session);
                    if (karmaCap >= 0)
                    {
                        session.saveState.deathPersistentSaveData.karmaCap = karmaCap;
                        session.saveState.deathPersistentSaveData.karma = karmaCap;
                    }
                    else
                    { // -1 passed, set to current max
                        session.saveState.deathPersistentSaveData.karma = karmaCap;
                    }

                    this.oracle.room.game.manager.rainWorld.progression.SaveDeathPersistentDataOfCurrentState(false, false);
                    foreach (RoomCamera camera in this.oracle.room.game.cameras)
                    {

                        if (camera.hud.karmaMeter != null)
                        {
                            camera.hud.karmaMeter.forceVisibleCounter = 80;
                            camera.hud.karmaMeter.UpdateGraphic();
                            camera.hud.karmaMeter.reinforceAnimation = 1;
                            ((StoryGameSession)this.oracle.room.game.session).AppendTimeOnCycleEnd(true);
                        }
                    }
                    foreach (Player player in owner.PlayersInRoom)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            this.oracle.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        }
                    }
                    break;
                case CMOracleAction.giveFood:
                    if (!Int32.TryParse(actionParam, out int playerFood))
                    {
                        playerFood = this.player.MaxFoodInStomach;
                    }
                    this.player.AddFood(playerFood);
                    this.action = CMOracleAction.generalIdle;
                    break;
                case CMOracleAction.startPlayerConversation:
                    this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerConversation");
                    this.action = CMOracleAction.generalIdle;
                    this.hadMainPlayerConversation = true;
                    break;
                case CMOracleAction.kickPlayerOut: // multiframe
                    this.action = CMOracleAction.kickPlayerOut;
                    this.actionParam = actionParam;
                    break;
                case CMOracleAction.killPlayer:
                    if (this.player.dead || this.player.room != this.oracle.room)
                    {
                        break;
                    }
                    IteratorKit.Log.LogInfo("Oracle killing player");
                    this.player.mainBodyChunk.vel += Custom.RNV() * 12f;
                    for (int i = 0; i < 20; i++)
                    {
                        this.oracle.room.AddObject(new Spark(this.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        this.player.Die();
                    }
                    break;
                case CMOracleAction.redCure:
                    this.oracle.room.game.GetStorySession.saveState.redExtraCycles = true;
                    if (this.oracle.room.game.cameras[0].hud != null)
                    {
                        if (this.oracle.room.game.cameras[0].hud.textPrompt != null)
                        {
                            this.oracle.room.game.cameras[0].hud.textPrompt.cycleTick = 0;
                        }
                        if (this.oracle.room.game.cameras[0].hud.map != null && this.oracle.room.game.cameras[0].hud.map.cycleLabel != null)
                        {
                            this.oracle.room.game.cameras[0].hud.map.cycleLabel.UpdateCycleText();
                        }
                    }
                    this.player.redsIllness?.GetBetter();
                    if (ModManager.CoopAvailable)
                    {
                        foreach (AbstractCreature abstractCreature in this.oracle.room.game.AlivePlayers)
                        {
                            if (abstractCreature.Room != this.oracle.room.abstractRoom)
                            {
                                continue;
                            }
                            RedsIllness playerIllness = (abstractCreature.realizedCreature as Player)?.redsIllness;
                            if (playerIllness == null)
                            {
                                continue;
                            }
                            playerIllness.GetBetter();
                        }
                    }
                    break;
            }
        }


        /// <summary>
        /// See if a conversation should be removed, if so run additional code when needed
        /// </summary>
        public void CheckForConversationDelete()
        {
            if (this.cmConversation == null || !this.cmConversation.slatedForDeletion)
            {
                return; // not relevant for deletion
            }
            // special case for conversationResume to replay conversation stored in conversationResumeTo
            if (this.cmConversation.eventId == "conversationResume" && this.conversationResumeTo != null)
            {
                IteratorKit.Log.LogInfo($"Resuming conversation {this.conversationResumeTo?.eventId}");
                this.hasConvToResumeTo = false; // was set by this.StartConversation
                this.conversationResumeTo.RestartCurrent(); // restart dialog
                this.conversationResumeTo = null;
                return;
            }
            if (this.cmConversation.eventId == "playerEnter" || this.cmConversation.eventId == "afterGiveMark" && !this.hadMainPlayerConversation)
            {
                this.inspectItem?.SetLocalGravity(this.oracle.room.gravity); // remove item no gravity effect
                this.inspectItem = null;
                IteratorKit.Log.LogInfo("Starting main player conversation as it hasn't happened yet.");
                this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerConversation");
                return;
            }
            
            
            if (this.cmConversation.eventId == "playerConversation")
            {
                this.hadMainPlayerConversation = true;
            }

            this.oracle.OracleEvents().OnCMEventEnd?.Invoke(owner, this.cmConversation?.eventId ?? "none");
            if (this.cmConversation.eventCategory == CMConversation.CMDialogCategory.Pearls)
            {
                this.inspectItem?.SetLocalGravity(this.oracle.room.gravity); // remove item no gravity effect
                this.inspectItem = null;
                if (this.owner is CMOracleSitBehavior)
                {
                    (this.owner as CMOracleSitBehavior).inspectItem = null; // put pearl back down
                }
            }
            IteratorKit.Log.LogInfo($"Removing conversation {this.cmConversation.eventId} from queue");
            this.cmConversationQueue.RemoveFirst();
            this.cmConversation.RestartCurrent();
        }
        /// <summary>
        /// Oracle hold player by pushing their velocity towards holdTarget
        /// </summary>
        /// <param name="holdTarget">Target position</param>
        public void HoldPlayerAt(Vector2 holdTarget)
        {
            foreach (Player player in owner.PlayersInRoom)
            {
                player.mainBodyChunk.vel += holdTarget;
            }
        }

        /// <summary>
        /// Changes the stored player score and updates the oracle/player relationship
        /// </summary>
        /// <param name="operation">add/subtract/set</param>
        /// <param name="amount">change amount</param>
        public void ChangePlayerScore(string operation, int amount)
        {
            StoryGameSession storyGameSession = this.oracle.room.game.session as StoryGameSession;
            this.playerScore = ITKUtil.GetSaveDataValue<int>(storyGameSession, this.oracle.ID, "playerScore", 20);
            if (operation == "add")
            {
                this.playerScore += amount;
            }
            else if (operation == "subtract")
            {
                this.playerScore -= amount;
            }
            else
            {
                this.playerScore = amount;
            }
            ITKUtil.SetSaveDataValue(storyGameSession, this.oracle.ID, "playerScore", this.playerScore);
            CMPlayerRelationship prevPlayerRelationship = this.playerRelationship;
            if (this.playerScore < this.oracleJson.angryScore)
            {
                this.playerRelationship = CMPlayerRelationship.angry;
            }
            else if (this.playerScore < this.oracleJson.annoyedScore)
            {
                this.playerRelationship = CMPlayerRelationship.annoyed;
            }
            else if (this.playerScore > this.oracleJson.friendScore)
            {
                this.playerRelationship = CMPlayerRelationship.friend;
            }
            else
            {
                this.playerRelationship = CMPlayerRelationship.normal;
            }
            // If relationship changed, set a flag so code in CheckConversation can trigger the dialogs
            if (this.playerRelationship != prevPlayerRelationship)
            {
                this.playerRelationshipJustChanged = true;
            }
        }

        /// <summary>
        /// Restart conversation from conversationResumeTo
        /// </summary>
        public void ResumeConversation()
        {
            if (this.conversationResumeTo == null)
            {
                IteratorKit.Log.LogWarning("No conversation to resume to.");
                return;
            }
            IteratorKit.Log.LogInfo($"Resuming conversation {this.conversationResumeTo.eventId}");
            // special: when conversationResume is flagged for deletion, it will start playing what is stored in conversationResumeTo
            this.StartConversation(CMConversation.CMDialogCategory.Generic, "conversationResume");
        }

        /// <summary>
        /// Start a conversation event about a PhysicalObject
        /// </summary>
        /// <param name="item">PhysicalObject</param>
        public void StartItemConversation(PhysicalObject item)
        {
            if (item is DataPearl)
            {
                DataPearl dataPearl = item as DataPearl;
                this.StartConversation(CMConversation.CMDialogCategory.Pearls, dataPearl.AbstractPearl.dataPearlType.value, false, dataPearl.AbstractPearl.dataPearlType);
                return;
            }
            this.StartConversation(CMConversation.CMDialogCategory.Items, item.GetType().ToString());
        }

        /// <summary>
        /// Runs when a dialog event starts, when it starts displaying text on screen.
        /// This reads out the dialog data and acts on any additional data in it
        /// </summary>
        public void DialogEventActivate(OracleBehavior cmBehavior, string eventId, Conversation.DialogueEvent dialogueEvent, OracleEventObjectJData eventData)
        {
            if (eventData.score != null)
            {
                this.ChangePlayerScore(eventData.score.action, eventData.score.amount);
            }
            if (eventData.action != null)
            {
                if (Enum.TryParse(eventData.action, out CMOracleAction tmpAction))
                {
                    this.RunAction(tmpAction, eventData.actionParam);
                }
            }
            if (eventData.screens.Count > 0)
            {
                this.cmScreen?.SetScreens(eventData.screens);
            }
            if (eventData.gravity != -50f)
            {
                this.SetGravity(eventData.gravity);
            }
        }

        /// <summary>
        /// Modify oracle room gravity, set to 0.9f for regular gravity
        /// Requires an AntiGravity effect be present in the room
        /// </summary>
        /// <param name="gravity">Gravity amount</param>
        public void SetGravity(float gravity)
        {
            this.roomGravity = gravity;
            this.oracle.room.gravity = gravity;
            List<AntiGravity> antiGravEffects = this.oracle.room.updateList.OfType<AntiGravity>().ToList();
            foreach (AntiGravity antiGravEffect in antiGravEffects)
            {
                antiGravEffect.active = (this.roomGravity <= 0);
            }
        }

        /// <summary>
        /// Checks if the player is in the current room. Handles specific logic for LTTM since their room is double size.
        /// </summary>
        public bool PlayerInRoom()
        {
            if (this.oracle.ID == Oracle.OracleID.SL)
            {
                if (this.player.room == this.oracle.room && this.player.mainBodyChunk.pos.x > 1160f)
                {
                    return true;
                }
                return false;
            }
            else
            {
                return this.player.room == this.oracle.room;
            }
        }

        /// <summary>
        /// Called by oracle
        /// </summary>
        /// <param name="weapon"></param>
        public void ReactToHitByWeapon(Weapon weapon)
        {
            IteratorKit.Log.LogWarning("oracle hit by weapon");
            if (UnityEngine.Random.value < 0.5f)
            {
                this.oracle.room.PlaySound(SoundID.SS_AI_Talk_1, this.oracle.firstChunk).requireActiveUpkeep = false;
            }
            else
            {
                this.oracle.room.PlaySound(SoundID.SS_AI_Talk_4, this.oracle.firstChunk).requireActiveUpkeep = false;
            }
            IteratorKit.Log.LogWarning("Player attack convo");
            if (this.cmConversation != null)
            {
                // clear the current dialog box
                this.cmConversation.InterruptQuickHide();
            }
            this.StartConversation(CMConversation.CMDialogCategory.Generic, "playerAttack");
        }

        public bool HasEvent(string eventId)
        {
            return this.oracleJson.events.genericEvents.ContainsKey(eventId);
        }

    }
    
}
