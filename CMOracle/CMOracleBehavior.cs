using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.Util;
using UnityEngine;
using RWCustom;
using HUD;
using SlugBase.SaveData;
using static IteratorKit.CMOracle.OracleJData.OracleEventsJData;

namespace IteratorKit.CMOracle
{
    /// <summary>
    /// Replicates Five Pebbles behavior (SSOracleBehavior)
    /// Makes the oracle come alive.
    /// Triggers conversations, handles movement and more.
    /// This has SSOracleBehavior as a base class only to allow SSOracle to accept using this class in place of its oracleBehavior. 
    /// It is not used at all by this class.
    /// </summary>
    public class CMOracleBehavior : SSOracleBehavior, Conversation.IOwnAConversation
    {
        public CMOracle? cmOracle { get { return (this.oracle is CMOracle) ? this.oracle as CMOracle : null; } }

        public new Vector2 currentGetTo, lastPos, nextPos, lastPosHandle, nextPosHandle, baseIdeal, idlePos;
        public new float pathProgression, investigateAngle, invstAngSpeed;
        public CMOracleMovement movement;
        private new CMOracleAction action;
        private string actionParam;
        public PhysicalObject inspectItem;
        public CMConversation cmConversation, conversationResumeTo = null;
        public new bool floatyMovement = false;
        public float roomGravity = 0.9f;
        public bool hasNoticedPlayer;
        public new int playerOutOfRoomCounter, timeSinceSeenPlayer;
        public int sayHelloDelay = 30;
        private int meditateTick;
        public int playerScore = 20;
        public bool hasSaidByeToPlayer, rainInterrupt, playerRelationshipJustChanged, alreadyDiscussedDeadPlayer = false;
        public CMOracleAction lastAction; // only for debug ui
        public string lastActionParam; // only for debug ui
        public List<AbstractPhysicalObject> alreadyDiscussedItems = new List<AbstractPhysicalObject>();
        public CMOracleScreen cmScreen;
        

        public OracleJData oracleJson { get { return this.oracle?.OracleData()?.oracleJson; } }
        public bool hadMainPlayerConversation
        {
            get { return ITKUtil.GetSaveDataValue<bool>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "hasHadPlayerConversation", false);}
            set { ITKUtil.SetSaveDataValue<bool>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "hasHadPlayerConversation", value);}
        }

        public CMPlayerRelationship playerRelationship
        {
            get { return (CMPlayerRelationship)ITKUtil.GetSaveDataValue<int>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "playerRelationship", (int)CMPlayerRelationship.normal); }
            set { ITKUtil.SetSaveDataValue<int>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "playerRelationship", (int)value); }
        }

        public override DialogBox dialogBox
        {
            get
            {
                if (this.oracle.room.game.cameras[0].hud.dialogBox == null)
                {
                    this.oracle.room.game.cameras[0].hud.InitDialogBox();
                    this.oracle.room.game.cameras[0].hud.dialogBox.defaultYPos = -10f;
                }
                return this.oracle.room.game.cameras[0].hud.dialogBox;
            }
        }
        public enum CMOracleAction
        {
            none, generalIdle, giveMark, giveKarma, giveMaxKarma, giveFood, startPlayerConversation, kickPlayerOut, killPlayer, redCure, customAction
        }
        public enum CMOracleMovement
        {
            idle, meditate, investigate, keepDistance, talk
        }
        public enum CMPlayerRelationship
        {
            friend, normal, annoyed, angry 
        }

        public CMOracleBehavior(Oracle oracle) : base(oracle)
        {
            this.oracle = oracle;
            this.oracle.OracleEvents().OnCMEventStart += this.DialogEventActivate;
            
            this.currentGetTo = this.nextPos = this.lastPos = oracle.firstChunk.pos;
            this.pathProgression = 1f;
            this.investigateAngle = UnityEngine.Random.value * 360f;
            this.movement = CMOracleMovement.idle;
            this.action = CMOracleAction.generalIdle;
            this.investigateAngle = 0f;
            this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
            this.idlePos = (this.oracleJson.startPos != Vector2.zero) ? ITKUtil.GetWorldFromTile(this.oracleJson.startPos) : ITKUtil.GetWorldFromTile(this.oracle.room.RandomTile().ToVector2());
            this.cmScreen = new CMOracleScreen(this);
            IteratorKit.Log.LogInfo($"Created behavior class for {this.oracle.ID}");
            this.SetNewDestination(new Vector2(313f, 517f));
        }

        public override void Update(bool eu)
        {
            this.floatyMovement = false; // must be before this.Move()
            this.Move();
            this.pathProgression = Mathf.Min(1f, this.pathProgression + 1f / Mathf.Lerp(40f + this.pathProgression * 80f, Vector2.Distance(this.lastPos, this.nextPos) / 5f, 0.5f));
            this.currentGetTo = Custom.Bezier(this.lastPos, this.ClampToRoom(this.lastPos + this.lastPosHandle), this.nextPos, this.ClampToRoom(this.nextPos + this.nextPosHandle), this.pathProgression);
            this.allStillCounter++;
            this.inActionCounter++;
            if (this.pathProgression < 1f || this.consistentBasePosCounter <= 100 || this.oracle.arm.baseMoving) // todo test
            {
                this.allStillCounter = 0;
            }
            this.CheckActions();
            this.cmScreen.Update();
            this.CheckConversationEvents();

            if (this.player != null && this.player.room == this.oracle.room)
            {
                this.lookPoint = this.player.firstChunk.pos; // look at player
                this.hasNoticedPlayer = true;
                if (this.playerOutOfRoomCounter > 0)
                {
                    // first seeing player
                    this.timeSinceSeenPlayer = 0;
                }
                this.playerOutOfRoomCounter = 0;
            }
            else
            {
                this.playerOutOfRoomCounter++;
            }

            if (this.inspectItem != null)
            {
                this.HoldObjectInPlace(this.inspectItem, this.oracle.firstChunk.pos);
                if (this.cmConversation == null && Custom.Dist(this.oracle.firstChunk.pos, this.inspectItem.firstChunk.pos) < 100f)
                {
                    IteratorKit.Log.LogInfo($"Starting conversation about item {this.inspectItem.abstractPhysicalObject}");
                    this.StartItemConversation(this.inspectItem);
                }
            }
            if (this.player != null)
            {
                this.CheckForConversationItem();
            }
            
            if (this.cmConversation != null)
            {
                this.cmConversation.Update();
            }

            CheckForConversationDelete();
        }

        /// <summary>
        /// Set target position for oracle to move to
        /// </summary>
        /// <param name="dst">Destination</param>
        public new void SetNewDestination(Vector2 dst)
        {
            IteratorKit.Log.LogInfo($"Set new target destination {dst}");
            this.lastPos = this.currentGetTo;
            this.nextPos = dst;
            this.lastPosHandle = Custom.RNV() * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
            this.nextPosHandle = -this.GetToDir * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
            this.pathProgression = 0f;
        }

        /// <summary>
        /// Used by CMOracleArm
        /// </summary>
        public override Vector2 OracleGetToPos
        {
            get
            {
                Vector2 v = this.currentGetTo;
                if (this.floatyMovement && Custom.DistLess(this.oracle.firstChunk.pos, this.nextPos, 50f))
                {
                    v = this.nextPos;
                }
                return this.ClampToRoom(v);
            }
        }

        /// <summary>
        /// Used by CMOracleArm
        /// </summary>
        public override Vector2 BaseGetToPos
        {
            get
            {
                return this.baseIdeal;
            }
        }

        /// <summary>
        /// Used by CMOracleArm
        /// </summary>
        public new float BasePosScore(Vector2 tryPos)
        {
            if (this.movement == CMOracleMovement.meditate || this.player == null)
            {
                return Vector2.Distance(tryPos, this.oracle.room.MiddleOfTile(24, 5));
            }

            return Mathf.Abs(Vector2.Distance(this.nextPos, tryPos) - 200f) + Custom.LerpMap(Vector2.Distance(this.player.DangerPos, tryPos), 40f, 300f, 800f, 0f);
        }

        /// <summary>
        /// Used by CMOracleArm
        /// </summary>
        public new float CommunicatePosScore(Vector2 tryPos)
        {
            if (this.oracle.room.GetTile(tryPos).Solid || this.player == null)
            {
                return float.MaxValue;
            }

            Vector2 dangerPos = this.player.DangerPos;
            float num = Vector2.Distance(tryPos, dangerPos);
            if (this.oracle is CMOracle)
            {
                num -= (tryPos.x + this.oracle.OracleData().oracleJson.talkHeight);
            }
            else
            {
                num -= tryPos.x;
            }
            num -= ((float)this.oracle.room.aimap.getTerrainProximity(tryPos)) * 10f;
            return num;
        }

        /// <summary>
        /// Clamp vector2 to oracle room
        /// </summary>
        public Vector2 ClampToRoom(Vector2 vector)
        {
            vector.x = Mathf.Clamp(vector.x, this.oracle.arm.cornerPositions[0].x + 10f, this.oracle.arm.cornerPositions[1].x - 10f);
            vector.y = Mathf.Clamp(vector.y, this.oracle.arm.cornerPositions[2].y + 10f, this.oracle.arm.cornerPositions[1].y - 10f);
            return vector;
        }

        /// <summary>
        /// Holds an object close to a position, with a orbit effect
        /// </summary>
        /// <param name="physicalObject">Physical object to hold</param>
        /// <param name="holdTarget">Target position</param>
        private void HoldObjectInPlace(PhysicalObject physicalObject, Vector2 holdTarget)
        {
            physicalObject.SetLocalGravity(0f); // apply antigravity so the item actually reaches the oracle if gravity is applied
            float dist = Custom.Dist(holdTarget, physicalObject.firstChunk.pos);
            Vector2 objectHoldPos = holdTarget - physicalObject.firstChunk.pos;
            physicalObject.firstChunk.vel += Vector2.ClampMagnitude(objectHoldPos, 40f) / 40f * Mathf.Clamp(2f - dist / 200f * 2f, 0.5f, 2f);
            if (physicalObject.firstChunk.vel.magnitude < 1f && dist < 16f)
            {
                physicalObject.firstChunk.vel = Custom.RNV() * 8f;
            }
            if (physicalObject.firstChunk.vel.magnitude > 8f)
            {
                physicalObject.firstChunk.vel /= 2f;
            }
        }

        /// <summary>
        /// Oracle hold player by pushing their velocity towards holdTarget
        /// </summary>
        /// <param name="holdTarget">Target position</param>
        private void HoldPlayerAt(Vector2 holdTarget)
        {
            foreach (Player player in this.PlayersInRoom)
            {
                player.mainBodyChunk.vel += holdTarget;
            }
        }

        /// <summary>
        /// Movement handlers
        /// </summary>
        private new void Move()
        {
            switch (this.movement)
            {
                case CMOracleMovement.idle:
                    // goes to set idle pos, in the json file this is the startPos
                    if (this.nextPos != this.idlePos)
                    {
                        this.SetNewDestination(this.idlePos);
                        this.investigateAngle = 0f;
                    }
                    break;
                case CMOracleMovement.meditate:
                    this.investigateAngle = 0f;
                    this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
                    this.meditateTick++;
                    for (int i = 0; i < this.oracle.mySwarmers.Count; i++)
                    {
                        OracleSwarmer swarmer = this.oracle.mySwarmers[i];
                        float num = 20f;
                        float num2 = (float)this.meditateTick * 0.035f;
                        num *= (i % 2 == 0) ? Mathf.Sin(num2) : Mathf.Cos(num2);
                        float num3 = (float)i * 6.28f / (float)this.oracle.mySwarmers.Count;
                        num3 += (float)this.meditateTick * 0.0035f;
                        num3 %= 6.28f;
                        float num4 = 90f + num;
                        Vector2 startVec = new Vector2(Mathf.Cos(num3) * num4 + this.oracle.firstChunk.pos.x, -Mathf.Sin(num3) * num4 + this.oracle.firstChunk.pos.y);
                        Vector2 endVec = new Vector2(swarmer.firstChunk.pos.x + (startVec.x - swarmer.firstChunk.pos.x) * 0.05f, swarmer.firstChunk.pos.y + (startVec.y - swarmer.firstChunk.pos.y) * 0.05f);
                        swarmer.firstChunk.HardSetPosition(endVec);
                        swarmer.firstChunk.vel = Vector2.zero;
                        if (swarmer.ping <= 0)
                        {
                            swarmer.rotation = 0f;
                            swarmer.revolveSpeed = 0f;
                            if (this.meditateTick > 120 && (double)UnityEngine.Random.value <= 0.0015)
                            {
                                swarmer.ping = 40;
                                this.oracle.room.AddObject(new Explosion.ExplosionLight(this.oracle.mySwarmers[i].firstChunk.pos, 500f + UnityEngine.Random.value * 400f, 1f, 10, Color.cyan));
                                this.oracle.room.AddObject(new ElectricDeath.SparkFlash(this.oracle.mySwarmers[i].firstChunk.pos, 0.75f + UnityEngine.Random.value));
                                if (this.player != null && this.player.room == this.oracle.room)
                                {
                                    this.oracle.room.PlaySound(SoundID.HUD_Exit_Game, this.player.mainBodyChunk.pos, 1f, 2f + (float)i / (float)this.oracle.mySwarmers.Count * 2f);
                                }
                            }
                        }
                        else
                        {
                            swarmer.ping--;
                        }
                    }
                    break;
                case CMOracleMovement.investigate:
                    if (this.player == null)
                    {
                        this.movement = CMOracleMovement.idle;
                        break;
                    }
                    this.lookPoint = this.player.DangerPos;
                    if (this.investigateAngle < -90f || this.investigateAngle > 90f || (float)this.oracle.room.aimap.getTerrainProximity(this.nextPos) < 2f)
                    {
                        this.investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
                        this.invstAngSpeed = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * ((UnityEngine.Random.value < 0.5f) ? -1 : 1f);
                    }
                    Vector2 getToVector = this.player.DangerPos + Custom.DegToVec(this.investigateAngle) * 150f;
                    if ((float)this.oracle.room.aimap.getTerrainProximity(getToVector) >= 2f)
                    {
                        if (this.pathProgression > 0.9f)
                        {
                            if (Custom.DistLess(this.oracle.firstChunk.pos, getToVector, 30f))
                            {
                                this.floatyMovement = true;
                            }
                            else if (!Custom.DistLess(this.nextPos, getToVector, 30f))
                            {
                                this.SetNewDestination(getToVector);
                            }
                        }
                        this.nextPos = getToVector;
                    }
                    break;
                case CMOracleMovement.keepDistance:
                    if (this.player == null)
                    {
                        this.movement = CMOracleMovement.idle;
                    }
                    else
                    {
                        this.lookPoint = this.player.DangerPos;
                        Vector2 distancePoint = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
                        if (!this.oracle.room.GetTile(distancePoint).Solid && this.oracle.room.aimap.getTerrainProximity(distancePoint) > 2
                            && Vector2.Distance(distancePoint, this.player.DangerPos) > Vector2.Distance(this.nextPos, this.player.DangerPos) + 100f)
                        {
                            this.SetNewDestination(distancePoint);
                        }
                        this.investigateAngle = 0f;
                    }
                    break;
                case CMOracleMovement.talk:
                    if (this.player == null)
                    {
                        this.movement = CMOracleMovement.idle;
                    }
                    else
                    {
                        this.lookPoint = this.player.DangerPos;
                        Vector2 tryPos = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
                        if (this.CommunicatePosScore(tryPos) + 40f < this.CommunicatePosScore(this.nextPos) && !Custom.DistLess(tryPos, this.nextPos, 30f))
                        {
                            this.SetNewDestination(tryPos);
                        }
                    }
                    break;
            }

            this.consistentBasePosCounter++;

            // try pick a random new base position.
            Vector2 baseTarget = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
            if (this.oracle.room.GetTile(baseTarget).Solid || this.BasePosScore(baseTarget) + 40.0 >= this.BasePosScore(this.baseIdeal))
            {
                return;
            }
            this.baseIdeal = baseTarget;
            this.consistentBasePosCounter = 0;
        }

        /// <summary>
        /// Handler for multi frame actions (any use of this.inActionCounter, or actions that are persistant)
        /// </summary>
        public void CheckActions()
        {
            if (this.player == null)
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
                    foreach (Player player in this.PlayersInRoom)
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
                        this.StunCoopPlayers(20);
                        Vector2 holdTarget = Vector2.ClampMagnitude(this.oracle.room.MiddleOfTile(24, 14) - this.player.mainBodyChunk.pos, 40f) / 40f * 2.8f * Mathf.InverseLerp(30f, 160f, (float)this.inActionCounter);
                        this.HoldPlayerAt(holdTarget);
                    }
                    else if (this.inActionCounter == 300)
                    {
                        this.action = CMOracleAction.generalIdle;
                        this.player.AddFood(10);
                        foreach (Player player in this.PlayersInRoom)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                this.oracle.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                            }
                        }
                        ((StoryGameSession)this.player.room.game.session).saveState.deathPersistentSaveData.theMark = true;
                        this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "afterGiveMark");
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
                    foreach (Player player in base.PlayersInRoom)
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
                    this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerConversation");
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
        /// Checks a variety of conditions to see if we should start a conversation event
        /// </summary>
        public void CheckConversationEvents()
        {
            if (this.player == null)
            {
                return;
            }
            if (this.hasNoticedPlayer)
            {
                if (this.sayHelloDelay < 0)
                {
                    this.sayHelloDelay = 30;
                }
                else
                {
                    if(this.sayHelloDelay > 0)
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
                            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerConversation");
                        }
                        else
                        {
                            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerEnter");
                        }
                        
                    }
                }
                if (this.player.dead && !this.alreadyDiscussedDeadPlayer)
                {
                    this.alreadyDiscussedDeadPlayer = true;
                    this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerDead");
                }
                if (this.player.room != this.oracle.room)
                {
                    if (!this.hasSaidByeToPlayer)
                    {
                        this.hasSaidByeToPlayer = true;
                        if (this.cmConversation != null)
                        {
                            if (this.cmConversation.eventId != "conversationResume") // dont interrupt the interrupt
                            {
                                this.conversationResumeTo = this.cmConversation;
                                this.cmConversation.InterruptQuickHide();
                                this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerLeaveInterrupt");
                            }
                        }
                        else
                        {
                            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerLeave");
                        }
                    }
                }
                else
                {
                    if (this.conversationResumeTo != null && this.player.room == this.oracle.room) // check if we are resuming a conversation
                    {
                        // the player has come back to us, start conversation again
                        if (!(this.cmConversation?.playerLeaveResume ?? false))
                        {
                            this.ResumeConversation();
                        }
                    }else if (this.cmConversation == null && this.playerOutOfRoomCounter > 100)
                    {
                        this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerReturn");
                    }
                    this.hasSaidByeToPlayer = false;
                }
                if (!this.rainInterrupt && this.player.room == this.oracle.room && this.oracle.room.world.rainCycle.TimeUntilRain < 1600 && this.oracle.room.world.rainCycle.pause < 1)
                {
                    if (this.cmConversation != null)
                    {
                        this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "rain");
                        this.rainInterrupt = true;
                    }
                }
                if (this.playerRelationship != CMPlayerRelationship.normal && this.playerRelationshipJustChanged)
                {
                    this.playerRelationshipJustChanged = false;
                    switch (this.playerRelationship)
                    {
                        case CMPlayerRelationship.friend:
                            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "oracleFriend");
                            break;
                        case CMPlayerRelationship.annoyed:
                            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "oracleAnnoyed");
                            break;
                        case CMPlayerRelationship.angry:
                            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "oracleAngry");
                            break;
                    }
                }
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
            if (this.cmConversation.eventId == "conversationResume")
            {
                IteratorKit.Log.LogInfo($"Resuming conversation {this.conversationResumeTo.eventId}");
                this.cmConversation = this.conversationResumeTo;
                this.conversationResumeTo = null;
                return;
            }
            if (this.cmConversation.eventId == "playerEnter" || this.cmConversation.eventId == "afterGiveMark" && !this.hadMainPlayerConversation)
            {
                this.inspectItem?.SetLocalGravity(this.oracle.room.gravity); // remove item no gravity effect
                this.inspectItem = null;
                IteratorKit.Log.LogInfo("Starting main player conversation as it hasn't happened yet.");
                this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerConversation");
                return;
            }
            IteratorKit.Log.LogInfo($"Destroying conversation {this.cmConversation.eventId}");
            if (this.cmConversation.eventId == "playerConversation")
            {
                this.hadMainPlayerConversation = true;
            }
            this.oracle.OracleEvents().OnCMEventEnd?.Invoke(this, this.cmConversation?.eventId ?? "none");
            this.inspectItem?.SetLocalGravity(this.oracle.room.gravity); // remove item no gravity effect
            this.inspectItem = null;
            this.cmConversation = null;
        }

        /// <summary>
        /// Checks to see if there is an item to talk about in the oracles room
        /// </summary>
        public void CheckForConversationItem()
        {
            if (this.player.room != this.oracle.room || this.cmConversation != null || this.sayHelloDelay > 0 || this.inspectItem != null)
            {
                return;
            }
            List<PhysicalObject> physicalObjects = this.oracle.room.physicalObjects.SelectMany(x => x).ToList();
            foreach (PhysicalObject physObject in physicalObjects)
            {
                if (this.alreadyDiscussedItems.Contains(physObject.abstractPhysicalObject) || physObject.grabbedBy.Count > 0){
                    continue;
                }
                this.alreadyDiscussedItems.Add(physObject.abstractPhysicalObject);
                if (physObject is DataPearl)
                {
                    DataPearl pearl = physObject as DataPearl;
                    if (pearl.AbstractPearl.dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl)
                    {
                        if (this.oracle.marbles.Contains(pearl as PebblesPearl))
                        {
                            continue; // avoid talking about any pearls spawned by this oracle
                        }
                    }
                    if (this.oracleJson.ignorePearlIds?.Contains(pearl.AbstractPearl.dataPearlType.value) ?? false){
                        continue; // in ignore list
                    }
                    this.inspectItem = pearl;
                    IteratorKit.Log.LogInfo($"Set inspect pearl to {pearl.AbstractPearl.dataPearlType.value}");
                }
                else
                {
                    if (ExtEnumBase.TryParse(typeof(SLOracleBehaviorHasMark.MiscItemType), physObject.GetType().ToString(), true, out ExtEnumBase result))
                    {
                        IteratorKit.Log.LogInfo($"Found a valid item to discuss {physObject.GetType()}");
                        this.StartItemConversation(physObject);
                        this.inspectItem = physObject;

                    }
                }

            }

        }

        /// <summary>
        /// Runs when a dialog event starts, when it starts displaying text on screen.
        /// This reads out the dialog data and acts on any additional data in it
        /// </summary>
        public void DialogEventActivate(CMOracleBehavior cmBehavior, string eventId, Conversation.DialogueEvent dialogEvent, OracleEventObjectJData eventData)
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
            if (eventData.movement != null)
            {
                if (Enum.TryParse(eventData.movement, out CMOracleMovement tmpMovement))
                {
                    this.movement = tmpMovement;
                    IteratorKit.Log.LogInfo($"Event {eventData.eventId} changed movement type to {eventData.movement}");
                }
                else
                {
                    IteratorKit.Log.LogError($"Event {eventData.eventId} provided an invalid movement option {eventData.movement}");
                }
            }
            if (eventData.screens.Count > 0)
            {
                this.cmScreen.SetScreens(eventData.screens);
            }
            if (eventData.moveTo != Vector2.zero)
            {
                this.SetNewDestination(eventData.moveTo);
            }
            if (eventData.gravity != -50f)
            {
                this.SetGravity(eventData.gravity);
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
                this.conversationResumeTo = this.cmConversation;
                // clear the current dialog box
                this.cmConversation.InterruptQuickHide();
            }
            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerAttack");
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
            }else if (operation == "subtract")
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
            }else if (this.playerScore < this.oracleJson.annoyedScore)
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
            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "conversationResume"); 
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
                this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Pearls, dataPearl.AbstractPearl.dataPearlType.value, dataPearl.AbstractPearl.dataPearlType);
                return;
            }
            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Items, item.GetType().ToString());
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
                antiGravEffect.active = (this.roomGravity < 1);
            }
        }

    }
}
