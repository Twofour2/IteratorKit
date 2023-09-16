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
using IteratorMod.CM_Oracle;
using HUD;

namespace IteratorMod.SRS_Oracle
{
    public class CMOracleBehavior : OracleBehavior, Conversation.IOwnAConversation
    {
        public Vector2 currentGetTo, lastPos, nextPos, lastPosHandle, nextPosHandle, baseIdeal;

        public float pathProgression, investigateAngle, invstAngSped, working, getToWorking, discoverCounter, killFac, lastKillFac;

        public bool floatyMovement;

        public int throwOutCounter, playerOutOfRoomCounter;

        public MovementBehavior movementBehavior;

        public CMOracleBehavior.Action action; // impliment own ver

        public List<CMOracleSubBehavior> allSubBehaviors;
        public CMOracleSubBehavior currSubBehavior;

        public new CMOracle oracle;

        public DataPearl inspectPearl;
        public CMConversation conversation = null;

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
            this.allSubBehaviors = new List<CMOracleSubBehavior>();
            this.currSubBehavior = new CMOracleSubBehavior.NoSubBehavior(this);

            this.investigateAngle = UnityEngine.Random.value * 360f;
            this.working = 1f;
            this.getToWorking = 1f;
            this.movementBehavior = CMOracleBehavior.MovementBehavior.Idle;
            this.action = CMOracleBehavior.Action.GeneralIdle;

            // move?
            this.SetNewDestination(this.oracle.room.RandomPos());
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
            //this.currentGetTo = this.nextPos;// Custom.Bezier(this.lastPos, this.ClampToRoom(this.lastPos + this.lastPosHandle), this.nextPos, this.ClampToRoom(this.nextPos + this.nextPosHandle), this.pathProgression);
            this.inActionCounter++;

            if (this.pathProgression >= 1f && this.consistentBasePosCounter > 100 && !this.oracle.arm.baseMoving)
            {
                this.allStillCounter++;
            }
            else
            {
                this.allStillCounter = 0;
            }

            switch (this.action)
            {
                case Action.GeneralIdle:
                    if (this.player != null && this.player.room == this.oracle.room)
                    {
                        this.discoverCounter++;
                        // see player code?
                    }
                    break;
            }
            // tmp test look point
            this.lookPoint = this.player.firstChunk.pos;

            // pearl code
            if (this.inspectPearl == null)
            {

            }

        }

        public void Move()
        {
            switch (this.movementBehavior)
            {
                case MovementBehavior.Idle:
                    // usually just looks at marbles, for now just sit still
                    if (UnityEngine.Random.value < 0.9f)
                    {
                        TestMod.Logger.LogWarning("Changing to meditate");
                        this.movementBehavior = CMOracleBehavior.MovementBehavior.Meditate;
                        
                      //  this.dialogBox.NewMessage(this.Translate("test content"), 10);
                       
                    }
                    break;
                case MovementBehavior.Meditate:
                    if (this.nextPos != this.oracle.room.MiddleOfTile(24, 17))
                    {
                        this.SetNewDestination(this.oracle.room.MiddleOfTile(24, 17));
                    }
                    this.investigateAngle = 0f;
                    this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
                    break;
                //  TestMod.Logger.LogWarning(this.lookPoint);
                case MovementBehavior.Investigate:
                    if (this.player == null)
                    {
                        this.movementBehavior = MovementBehavior.Idle;
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
            }

            this.consistentBasePosCounter++;
            Vector2 vector2 = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
            if (!this.oracle.room.GetTile(vector2).Solid && this.BasePosScore(vector2) + 40f < this.BasePosScore(this.baseIdeal))
            {
                this.baseIdeal = vector2;
                this.consistentBasePosCounter = 0;
                return;
            }

            if (this.inspectPearl != null && this.conversation == null)
            {
                TestMod.Logger.LogWarning("Starting convo about pearl");
                this.StartItemConversation(this.inspectPearl);
            }

            if (this.player != null && this.player.room == this.oracle.room)
            {
                List<PhysicalObject>[] physicalObjects = this.oracle.room.physicalObjects;
                foreach (List<PhysicalObject> physicalObject in physicalObjects)
                {
                    foreach (PhysicalObject physObject in physicalObject)
                    {
                        if (this.inspectPearl == null && this.conversation == null && physObject is DataPearl)
                        {
                            DataPearl pearl = (DataPearl)physObject;
                            TestMod.Logger.LogWarning(pearl.grabbedBy.Count);
                            if (pearl.grabbedBy.Count == 0)
                            {
                                this.inspectPearl = pearl;
                                TestMod.Logger.LogInfo("Set inspect pearl.");
                            }
                            
                        }
                    }
                }
            }

            
        }
        public float BasePosScore(Vector2 tryPos)
        {
            if (this.movementBehavior == CMOracleBehavior.MovementBehavior.Meditate || this.player == null)
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
                if (this.movementBehavior == CMOracleBehavior.MovementBehavior.Idle)
                {
                    return Custom.DegToVec(this.investigateAngle);
                }
                if (this.movementBehavior == CMOracleBehavior.MovementBehavior.Investigate)
                {
                    return Custom.DegToVec(this.investigateAngle);
                }
                return new Vector2(0f, 1f);
            }
        }

        public void NewAction(CMOracleBehavior.Action nextAction)
        {
            TestMod.Logger.LogInfo($"new action: {nextAction.ToString()} (from: {this.action.ToString()}");
            if (nextAction == this.action)
            {
                return;
            }

            this.action = nextAction;
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

        public void InitiateConversation(Conversation.ID convoId)
        {
            if (this.conversation != null)
            {
                this.conversation.Interrupt("...", 0);
                this.conversation.Destroy();
            }
            this.conversation = new CMConversation(this, convoId, this.dialogBox);
        }

        public void StartItemConversation(DataPearl item)
        {
            Conversation.ID id = Conversation.DataPearlToConversation(item.AbstractPearl.dataPearlType);
            this.conversation = new CMConversation(this, id, this.dialogBox);


        }



    }
}
