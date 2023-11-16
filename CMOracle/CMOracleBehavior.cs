using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.PlayerLoop;
using RWCustom;
using IteratorKit.CMOracle;
using HUD;
using MoreSlugcats;
using System.Runtime.InteropServices;
using DevInterface;
using IL;

namespace IteratorKit.CMOracle
{
    public class CMOracleBehavior : OracleBehavior, Conversation.IOwnAConversation
    {
        public Vector2 currentGetTo, lastPos, nextPos, lastPosHandle, nextPosHandle, baseIdeal;

        public float pathProgression, investigateAngle, invstAngSped, working, getToWorking, discoverCounter, killFac, lastKillFac;

        public bool floatyMovement, hasNoticedPlayer, rainInterrupt;

        public int throwOutCounter, playerOutOfRoomCounter;
        public int sayHelloDelay = -1;
        public int timeSinceSeenPlayer = 0;

        public bool forceGravity;
        public float roomGravity; // enable force gravity to use

        public CMOracleMovement movementBehavior;


        //public List<CMOracleSubBehavior> allSubBehaviors;
        //public CMOracleSubBehavior currSubBehavior;

        public new CMOracle oracle;

        public PhysicalObject inspectItem;
        public CMConversation cmConversation = null;


        public List<OracleJSON.OracleEventsJson.OracleScreenJson> screens;
        public int currScreen = 0;
        public int currScreenCounter = 0;
        public ProjectedImage currImage;
        public OracleJSON.OracleEventsJson.OracleScreenJson currScreenData;
        public Vector2 currImagePos;

        public CMOracleAction action;
        public string actionStr;
        public string actionParam = null;

        public int playerScore = 20;
        public bool oracleAngry = false;
        public bool oracleAnnoyed = false;
        public bool hasSaidByeToPlayer = false;

        private CMConversation actualConversationResumeTo;
        public CMConversation conversationResumeTo
        {
            get { return actualConversationResumeTo; } set
            {
                // avoid replacing this with the "conversationResume" event
                if (!(value?.resumeConvFlag ?? false))
                {
                    actualConversationResumeTo = value;
                }
            }
        }
        public List<EntityID> alreadyDiscussedItems = new List<EntityID>();

        public enum CMOracleAction
        {
            generalIdle,
            giveMark,
            giveKarma,
            giveMaxKarma,
            giveFood,
            startPlayerConversation,
            kickPlayerOut,
            killPlayer,
            customAction
        }

        public enum CMOracleMovement
        {
            idle,
            meditate,
            investigate,
            keepDistance,
            talk
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

        public CMOracleBehavior(CMOracle oracle) : base (oracle){
            this.oracle = oracle;
            this.currentGetTo = oracle.firstChunk.pos;
            this.lastPos = oracle.firstChunk.pos;
            this.nextPos = oracle.firstChunk.pos;
            this.pathProgression = 1f;

            this.investigateAngle = UnityEngine.Random.value * 360f;
            this.working = 1f;
            this.getToWorking = 1f;
            this.movementBehavior = CMOracleMovement.idle;
            this.action = CMOracleAction.generalIdle;
            this.playerOutOfRoomCounter = 1;

            // move?
            if (this.oracle.oracleJson.startPos == Vector2.zero)
            {
                this.SetNewDestination(this.oracle.room.MiddleOfTile(this.oracle.room.TileHeight/2, this.oracle.room.TileWidth/2));
            }
            else
            {
                this.SetNewDestination(this.oracle.oracleJson.startPos);
            }
            
            this.investigateAngle = 0f;
            this.lookPoint = this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);

        }

        public override void Update(bool eu)
        {
            // for the most part seems to handle changing states, i.e if player enters the room
            this.Move();
            base.Update(eu);

            this.pathProgression = Mathf.Min(1f, this.pathProgression + 1f / Mathf.Lerp(40f + this.pathProgression * 80f, Vector2.Distance(this.lastPos, this.nextPos) / 5f, 0.5f));

            this.currentGetTo = Custom.Bezier(this.lastPos, this.ClampToRoom(this.lastPos + this.lastPosHandle), this.nextPos, this.ClampToRoom(this.nextPos + this.nextPosHandle), this.pathProgression);

            
            this.floatyMovement = false;

            if (this.pathProgression >= 1f && this.consistentBasePosCounter > 100 && !this.oracle.arm.baseMoving)
            {
                this.allStillCounter++;
            }
            else
            {
                this.allStillCounter = 0;
            }

            this.inActionCounter++;
            CheckActions(); // runs actions like giveMark. moved out of update to avoid mess. 
            ShowScreenImages();

            if (this.player != null && this.player.room == this.oracle.room)
            {
                // look at player
                this.lookPoint = this.player.firstChunk.pos;
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
            if (this.inspectItem != null && this.cmConversation == null)
            {
                IteratorKit.Logger.LogWarning("Starting convo about pearl");
                this.StartItemConversation(this.inspectItem);
            }
            if (this.inspectItem != null)
            {
                Vector2 pearlHoldPos = this.oracle.firstChunk.pos - this.inspectItem.firstChunk.pos;
                float dist = Custom.Dist(this.oracle.firstChunk.pos, this.inspectItem.firstChunk.pos);
                this.inspectItem.firstChunk.vel += Vector2.ClampMagnitude(pearlHoldPos, 40f) / 40f * Mathf.Clamp(2f - dist / 200f * 2f, 0.5f, 2f);
                if (this.inspectItem.firstChunk.vel.magnitude < 1f && dist < 16f)
                {
                    this.inspectItem.firstChunk.vel = Custom.RNV() * 8f;
                }
                if (this.inspectItem.firstChunk.vel.magnitude > 8f)
                {
                    this.inspectItem.firstChunk.vel /= 2f;
                }

            }

            if (this.player != null && this.player.room == this.oracle.room && this.cmConversation == null)
            {
                List<PhysicalObject>[] physicalObjects = this.oracle.room.physicalObjects;
                foreach (List<PhysicalObject> physicalObject in physicalObjects)
                {

                    foreach (PhysicalObject physObject in physicalObject)
                    {
                        if (this.alreadyDiscussedItems.Contains(physObject.abstractPhysicalObject.ID))
                        {
                            continue;
                        }
                        if (physObject.grabbedBy.Count > 0)
                        { // dont count held objects
                            continue;
                        }

                            IteratorKit.Logger.LogInfo("building conversation");
                        if (this.inspectItem == null && this.cmConversation == null)
                        {
                            this.alreadyDiscussedItems.Add(physObject.abstractPhysicalObject.ID);
                            if (physObject is DataPearl)
                            {
                                IteratorKit.Logger.LogInfo("Found data pearl");
                                DataPearl pearl = (DataPearl)physObject;
                                if (pearl.AbstractPearl.dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl)
                                {
                                    if (this.oracle.marbles.Contains(pearl as PebblesPearl))
                                    {
                                        continue; // avoid talking about any pearls that were spawned by this oracle
                                    }
                                }
                                this.inspectItem = pearl;
                                IteratorKit.Logger.LogInfo($"Set inspect pearl to {pearl.AbstractPearl.ID}");
                            }
                            else
                            {
                                IteratorKit.Logger.LogInfo("Found other item");
                                this.inspectItem = physObject;
                            }
                           

                        }
                        else if (this.cmConversation == null)
                        {
                            this.alreadyDiscussedItems.Add(physObject.abstractPhysicalObject.ID);
                            SLOracleBehaviorHasMark.MiscItemType msc = new SLOracleBehaviorHasMark.MiscItemType("NA", false);
                            if(SLOracleBehaviorHasMark.MiscItemType.TryParse(msc.enumType, physicalObject.GetType().ToString(), true, out ExtEnumBase result)){
                                IteratorKit.Logger.LogInfo("Found a valid item");
                                this.StartItemConversation(physObject);
                            }
                            else
                            {
                                IteratorKit.Logger.LogInfo($"Failed to find match for {physicalObject.GetType().ToString()}");
                            }
                        }
                    }
                }
            }

            CheckConversationEvents();

            if (this.forceGravity == true)
            {
                this.oracle.room.gravity = this.roomGravity;
            }
            if (this.cmConversation != null)
            {
                this.cmConversation.Update();
            }
            if (this.conversationResumeTo != null && this.player.room == this.oracle.room)// check if we are resuming
            {
                if (!(this.cmConversation?.resumeConvFlag ?? false))
                {
                    this.ResumeConversation();
                }

            }
            if ((this.cmConversation != null && this.cmConversation.slatedForDeletion && this.action == CMOracleAction.generalIdle)) {
                if (this.cmConversation.resumeConvFlag) // special case to resume conversation
                {
                    this.cmConversation = this.conversationResumeTo;
                    this.conversationResumeTo = null;
                }
                else
                {
                    IteratorKit.Logger.LogWarning("Destroying convo");
                    this.inspectItem = null;
                    this.cmConversation = null;
                }
               
            }

        }

        public void Move()
        {
            switch (this.movementBehavior)
            {
                case CMOracleMovement.idle:
                    // usually just looks at marbles, for now just sit still
                    break;
                case CMOracleMovement.meditate:
                    //if (this.nextPos != this.oracle.room.MiddleOfTile(24, 17))
                    //{
                    //    this.SetNewDestination(this.oracle.room.MiddleOfTile(24, 17));
                    //}
                    this.investigateAngle = 0f;
                    this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
                    break;
                //  TestMod.Logger.LogWarning(this.lookPoint);
                case CMOracleMovement.investigate:
                    if (this.player == null)
                    {
                        this.movementBehavior = CMOracleMovement.idle;
                        break;
                    }
                    this.lookPoint = this.player.DangerPos;
                    if (this.investigateAngle < -90f || this.investigateAngle > 90f || (float)this.oracle.room.aimap.getAItile(this.nextPos).terrainProximity < 2f)
                    {
                        this.investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
                        this.invstAngSped = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * ((UnityEngine.Random.value < 0.5f) ? -1 : 1f);
                    }
                    Vector2 getToVector = this.player.DangerPos + Custom.DegToVec(this.investigateAngle) * 150f;
                    if ((float)this.oracle.room.aimap.getAItile(getToVector).terrainProximity >= 2f)
                    {
                        if (this.pathProgression > 0.9f)
                        {
                            if (Custom.DistLess(this.oracle.firstChunk.pos, getToVector, 30f)){
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
                        this.movementBehavior = CMOracleMovement.idle;
                    }
                    else
                    {
                        this.lookPoint = this.player.DangerPos;
                        Vector2 distancePoint = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
                        if (!this.oracle.room.GetTile(distancePoint).Solid && this.oracle.room.aimap.getAItile(distancePoint).terrainProximity > 2 
                            && Vector2.Distance(distancePoint, this.player.DangerPos) > Vector2.Distance(this.nextPos, this.player.DangerPos) + 100f)
                        {
                            this.SetNewDestination(distancePoint);
                        }
                    }
                    break;
                case CMOracleMovement.talk:
                    if (this.player == null)
                    {
                        this.movementBehavior = CMOracleMovement.idle;
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
            Vector2 vector2 = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
            if (this.oracle.room.GetTile(vector2).Solid || this.BasePosScore(vector2) + 40.0 >= this.BasePosScore(this.baseIdeal))
            {
                return;
            }
            this.baseIdeal = vector2;
            this.consistentBasePosCounter = 0;


        }

        public float CommunicatePosScore(Vector2 tryPos)
        {
            if (this.oracle.room.GetTile(tryPos).Solid || this.player == null)
            {
                return float.MaxValue;
            }
            
            Vector2 dangerPos = this.player.DangerPos;
            //dangerPos *= this.oracle.oracleJson.talkHeight;
            float num = Vector2.Distance(tryPos, dangerPos);
            num -= (tryPos.x + this.oracle.oracleJson.talkHeight);
            num -= ((float)this.oracle.room.aimap.getAItile(tryPos).terrainProximity) * 10f;
            return num;
        }

        public float BasePosScore(Vector2 tryPos)
        {
            if (this.movementBehavior == CMOracleMovement.meditate || this.player == null)
            {
                return Vector2.Distance(tryPos, this.oracle.room.MiddleOfTile(24, 5));
            }

             return Mathf.Abs(Vector2.Distance(this.nextPos, tryPos) - 200f) + Custom.LerpMap(Vector2.Distance(this.player.DangerPos, tryPos), 40f, 300f, 800f, 0f);
        }

        public void SetNewDestination(Vector2 dst)
        {
            this.lastPos = this.currentGetTo;
            this.nextPos = dst;
            this.lastPosHandle = Custom.RNV() * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
            this.nextPosHandle = -this.GetToDir * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
            this.pathProgression = 0f;
        }

        public Vector2 ClampToRoom(Vector2 vector)
        {
            vector.x = Mathf.Clamp(vector.x, this.oracle.arm.cornerPositions[0].x + 10f, this.oracle.arm.cornerPositions[1].x - 10f);
            vector.y = Mathf.Clamp(vector.y, this.oracle.arm.cornerPositions[2].y + 10f, this.oracle.arm.cornerPositions[1].y - 10f);
            return vector;
        }

        public Vector2 RandomRoomPoint()
        {
            return this.ClampToRoom(new Vector2(UnityEngine.Random.value, UnityEngine.Random.value));
        }

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

        public override Vector2 BaseGetToPos
        {
            get
            {
                return this.baseIdeal;
            }
        }

        public override Vector2 GetToDir
        {
            get
            {
                if (this.movementBehavior == CMOracleMovement.idle)
                {
                    return Custom.DegToVec(this.investigateAngle);
                }
                if (this.movementBehavior == CMOracleMovement.keepDistance)
                {
                    return Custom.DegToVec(this.investigateAngle);
                }
                return Custom.DegToVec(0);// new Vector2(0f, 1f);
            }
        }

        public void NewAction(string nextAction, string actionParam)
        {
            IteratorKit.Logger.LogInfo($"new action: {nextAction} (from: {this.action})");
            if (nextAction == this.actionStr)
            {
                return;
            }
            if (Enum.TryParse(nextAction, out CMOracleBehavior.CMOracleAction tmpAction))
            {
                this.action = tmpAction; // assign our enum action
            }
            else
            {
                this.action = CMOracleAction.customAction; // trigger special event instead
            }
            this.inActionCounter = 0;
            this.actionStr = nextAction;
            this.actionParam = actionParam;
         }



        public enum Action
        {
            GeneralIdle,
            MeetPlayer
        }

        public enum MovementBehavior
        {
            Idle,
            Meditate,
            KeepDistance,
            Talk,
            Investigate,
            ShowMedia
        }


        public void CheckConversationEvents()
        {
            if (this.hasNoticedPlayer)
            {
                if (this.sayHelloDelay < 0 && this.oracle.room.world.rainCycle.TimeUntilRain + this.oracle.room.world.rainCycle.pause > 2000)
                {
                    this.sayHelloDelay = 30;
                }
                else
                {
                    if(this.sayHelloDelay > 0)
                    {
                        this.sayHelloDelay--;
                    }
                    if(this.sayHelloDelay == 1)
                    {
                        this.oracle.room.game.cameras[0].EnterCutsceneMode(this.player.abstractCreature, RoomCamera.CameraCutsceneType.Oracle);
                        // now we can start calling player dialogs!
                        this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "playerEnter");
                        
                    }
                }
                if (this.player.dead)
                {
                    this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "playerDead");
                }
                if (this.player.room != this.oracle.room )
                {
                    this.playerOutOfRoomCounter++;
                    if (!this.hasSaidByeToPlayer)
                    {
                        this.hasSaidByeToPlayer = true;
                        if (this.cmConversation != null)
                        {
                            if ((this.cmConversation?.eventId ?? "") != "conversationResume") // dont interrupt the interrupt
                            {
                                this.conversationResumeTo = this.cmConversation;
                                this.cmConversation.InterruptQuickHide();
                                this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "playerLeaveInterrupt");
                            }
                        }
                        else
                        {
                            this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "playerLeave");
                        }
                    }
                    
                }
                else
                {
                    this.hasSaidByeToPlayer = false;
                    this.playerOutOfRoomCounter = 0;
                }
                if (!this.rainInterrupt && this.player.room == this.oracle.room && this.oracle.room.world.rainCycle.TimeUntilRain < 1600 && this.oracle.room.world.rainCycle.pause < 1)
                {
                    if (this.cmConversation != null)
                    {
                        this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "rain");
                        this.rainInterrupt = true;
                    }
                }
            }
            if (this.oracleAngry)
            {
                this.conversationResumeTo = this.cmConversation;
                this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "oracleAngry");
            }
            else if (this.oracleAnnoyed)
            {
                this.conversationResumeTo = this.cmConversation;
                this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "oracleAnnoyed");
            }

        }


        public void ResumeConversation()
        {
            if (this.conversationResumeTo == null)
            {
                IteratorKit.Logger.LogInfo("No conversation to resume to.");
                return;
            }
            IteratorKit.Logger.LogInfo($"Resuming conversation {this.conversationResumeTo}");
            this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "conversationResume");
            this.cmConversation.resumeConvFlag = true; // when this is flagged for deletion, this flag will cause it to instead replace the conversation with conversationResumeTo
         
        }


        public void StartItemConversation(PhysicalObject item)
        {
            if (item is DataPearl)
            {
                DataPearl dataPearl = (DataPearl)item;
                Conversation.ID id = Conversation.DataPearlToConversation(dataPearl.AbstractPearl.dataPearlType);
                this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Pearls, dataPearl.AbstractPearl.dataPearlType.value, dataPearl.AbstractPearl.dataPearlType);
            }
            else
            {
                this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Item, item.GetType().ToString());
            }
            
        }

        public void ChangePlayerScore(string operation, int amount)
        {
            SlugBase.SaveData.SlugBaseSaveData saveData = SlugBase.SaveData.SaveDataExtension.GetSlugBaseData(((StoryGameSession)this.oracle.room.game.session).saveState.miscWorldSaveData);
            if (!saveData.TryGet($"{this.oracle.ID}_playerScore", out int playerScore))
            {
                playerScore = 0;
            }
            this.playerScore = playerScore;
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
            saveData.Set($"{this.oracle.ID}_playerScore", this.playerScore);
            IteratorKit.Logger.LogInfo($"Changed player score to {this.playerScore}");
            if (this.playerScore < this.oracle.oracleJson.annoyedScore)
            {
                this.oracleAnnoyed = true;
            }
            if (this.playerScore < this.oracle.oracleJson.angryScore)
            {
                this.oracleAngry = true;
            }
        }

        public void ReactToHitByWeapon(Weapon weapon)
        {
            IteratorKit.Logger.LogWarning("oracle hit by weapon");
            if (UnityEngine.Random.value < 0.5f)
            {
                this.oracle.room.PlaySound(SoundID.SS_AI_Talk_1, this.oracle.firstChunk).requireActiveUpkeep = false;
            }
            else
            {
                this.oracle.room.PlaySound(SoundID.SS_AI_Talk_4, this.oracle.firstChunk).requireActiveUpkeep = false;
            }
            IteratorKit.Logger.LogWarning("Player attack convo");
            this.conversationResumeTo = this.cmConversation;
            // clear the current dialog box
            if (this.dialogBox.messages.Count > 0)
            {
                this.dialogBox.messages = new List<DialogBox.Message>
                {
                    this.dialogBox.messages[0] 
                };
                this.dialogBox.lingerCounter = this.dialogBox.messages[0].linger + 1;
                this.dialogBox.showCharacter = this.dialogBox.messages[0].text.Length + 2;
            }
            this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "playerAttack");
        }

        public void CheckActions()
        {
            if (this.player == null)
            { // for dealing with other mods that somehow set this to null
                return;
            }

            // actions should reset back to CMOracleAction.generalIdle if they wish for future conversations to continue working.
            // actions such as kill/kickOut dont allow any further actions to take place after they have occurred
            switch (this.action)
            {
                case CMOracleAction.generalIdle:
                    if (this.player != null && this.player.room == this.oracle.room)
                    {
                        this.discoverCounter++;
                        
                    }
                    break;
                case CMOracleAction.giveMark:
                    IteratorKit.Logger.LogWarning("GIVING MARK TO PLAYER");
                    IteratorKit.Logger.LogWarning(((StoryGameSession)this.player.room.game.session).saveState.deathPersistentSaveData.theMark);
                    if (((StoryGameSession)this.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                    {
                        IteratorKit.Logger.LogInfo("Player already has mark!");
                        this.action = CMOracleAction.generalIdle;
                        return;
                    }
                    if (this.inActionCounter > 30 && this.inActionCounter < 300)
                    {
                        if (this.inActionCounter < 300)
                        {
                            if (ModManager.CoopAvailable)
                            {
                                base.StunCoopPlayers(20);
                            }
                            else
                            {
                                this.player.Stun(20);
                            }
                        }
                        Vector2 holdPlayerAt = Vector2.ClampMagnitude(this.oracle.room.MiddleOfTile(24, 14) - this.player.mainBodyChunk.pos, 40f) / 40f * 2.8f * Mathf.InverseLerp(30f, 160f, (float)this.inActionCounter);

                        foreach (Player player in base.PlayersInRoom)
                        {
                            player.mainBodyChunk.vel += holdPlayerAt;
                        }

                    }
                    if (this.inActionCounter == 30)
                    {
                        this.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
                    }
                    if (this.inActionCounter == 300)
                    {
                        this.action = CMOracleAction.generalIdle;
                        this.player.AddFood(10);
                        foreach (Player player in base.PlayersInRoom)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                this.oracle.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                            }
                        }

                        ((StoryGameSession)this.player.room.game.session).saveState.deathPersistentSaveData.theMark = true;
                        this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "afterGiveMark");
                    }
                    break;
                case CMOracleAction.giveKarma:
                    // set player to max karma level
                    if (Int32.TryParse(this.actionParam, out int karmaCap))
                    {
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
                        
                    }
                    else
                    {
                        IteratorKit.Logger.LogError($"Failed to convert action param {this.actionParam} to integer");
                    }
                    this.action = CMOracleAction.generalIdle;

                    break;
                case CMOracleAction.giveFood:
                    if (!Int32.TryParse(this.actionParam, out int playerFood))
                    {
                        playerFood = this.player.MaxFoodInStomach;
                    }
                    this.player.AddFood(playerFood);
                    this.action = CMOracleAction.generalIdle;
                    break;
                case CMOracleAction.startPlayerConversation:
                    this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "playerConversation");
                    this.action = CMOracleAction.generalIdle;
                    break;
                case CMOracleAction.kickPlayerOut:
                    IteratorKit.Logger.LogWarning("kick player out");
                    ShortcutData? shortcut = this.GetShortcutToRoom(this.actionParam);
                    if (shortcut == null)
                    {
                        IteratorKit.Logger.LogError("Cannot kick player out as destination does not exist!");
                        this.action = CMOracleAction.generalIdle;
                        return;
                    }

                    Vector2 vector2 = this.oracle.room.MiddleOfTile(shortcut.Value.startCoord);

                    foreach(Player player in this.PlayersInRoom)
                    {
                        player.mainBodyChunk.vel += Custom.DirVec(player.mainBodyChunk.pos, vector2);
                    }
                    this.ChangePlayerScore("set", -10);
                    break;
                case CMOracleAction.killPlayer:
                    if (!this.player.dead && this.player.room == this.oracle.room)
                    {
                        IteratorKit.Logger.LogInfo("Oracle killing player");
                        this.player.mainBodyChunk.vel += Custom.RNV() * 12f;
                        for (int i = 0; i < 20; i++)
                        {
                            this.oracle.room.AddObject(new Spark(this.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                            this.player.Die();
                        }
                    }
                    break;
                case CMOracleAction.customAction:
                    base.SpecialEvent(this.actionStr);
                    if (this.inActionCounter < 0) // allows custom action to signal it wants to quit
                    {
                        IteratorKit.Logger.LogInfo("Custom action signalled it was done");
                        this.action = CMOracleAction.generalIdle;
                        this.actionStr = null;
                    }
                    break;
            }

        }

        private ShortcutData? GetShortcutToRoom(string roomId)
        {
            foreach(ShortcutData shortcut in this.oracle.room.shortcuts)
            {
                IntVector2 destTile = shortcut.connection.DestTile;
                AbstractRoom destRoom = this.oracle.room.WhichRoomDoesThisExitLeadTo(destTile);
                if (destRoom != null)
                {
                    if (destRoom.name == roomId)
                    {
                        return shortcut;
                    }
                }
            }
            IteratorKit.Logger.LogInfo($"Failed to find shortcut with room to {roomId}");
            return null;
        }

        public void ShowScreens(List<OracleJSON.OracleEventsJson.OracleScreenJson> screens)
        {
            if (this.oracle.myScreen == null)
            {
                IteratorKit.Logger.LogError("got request to show image, but oracle has no screen.");
            }
            this.currScreen = 0;
            this.screens = screens;
            
        }

        public void ShowScreenImages()
        {
            if (this.screens == null)
            {
                return;
            }
            if (this.currImage != null)
            {
                if (this.currScreenData.moveSpeed > 0)
                {
                    this.currImage.setPos = Custom.MoveTowards(this.currImage.pos, this.currScreenData.pos, this.currScreenData.moveSpeed);
                }
                else
                {
                    this.currImage.setPos = this.currScreenData.pos;
                }
                
            }
            

            if (this.currScreenCounter > 0 && this.currScreen != 0)
            {
                this.currScreenCounter--;
            }
            else 
            { // move to next screen
                if (this.currScreen >= this.screens.Count()) // are we out of screens?
                {
                    IteratorKit.Logger.LogInfo("destroy image");
                    this.currScreen = 0;
                    this.screens = null;
                    this.currImage.Destroy();
                    this.currImage = null;
                    return;
                }
                else
                { // show next screen
                    
                    OracleJSON.OracleEventsJson.OracleScreenJson screen = this.screens[this.currScreen];
                    IteratorKit.Logger.LogWarning($"next image {screen.image} at pos {screen.pos} with alpha {screen.alpha}");
                    if (screen.image != null)
                    {
                        if (this.currImage != null)
                        {
                            this.currImage.Destroy();
                        }
                        this.currImage = this.oracle.myScreen.AddImage(screen.image);
                    }
                    this.currScreenData = screen;
                    if (this.currScreenData.moveSpeed <= 0)
                    {
                        this.currImage.pos = this.currScreenData.pos;
                        this.currImage.setPos = this.currScreenData.pos;
                    }
                    //this.currImagePos = screen.pos;
                    //this.currImageMoveSpeed = screen.
                    this.currImage.setAlpha = (screen.alpha / 255);
                    this.currScreenCounter = screen.hold;
                    this.currScreen += 1;
                }
                
            }
        }


}
}
