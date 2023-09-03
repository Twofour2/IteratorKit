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

namespace IteratorMod.SRS_Oracle
{
    public class SRSOracleBehavior : OracleBehavior, Conversation.IOwnAConversation
    {
        public Vector2 currentGetTo, lastPos, nextPos, lastPosHandle, nextPosHandle, baseIdeal;

        public float pathProgression, investigateAngle, invstAngSped, working, getToWorking, discoverCounter, killFac, lastKillFac;

        public bool floatyMovement;

        public int throwOutCounter, playerOutOfRoomCounter;

        public MovementBehavior movementBehavior;

        public SRSOracleBehavior.Action action; // impliment own ver

        public List<SRSOracleSubBehavior> allSubBehaviors;
        public SRSOracleSubBehavior currSubBehavior;

        public new SRSOracle oracle;

        


        public SRSOracleBehavior(SRSOracle oracle) : base (oracle){
            this.oracle = oracle;
            this.currentGetTo = oracle.firstChunk.pos;
            this.lastPos = oracle.firstChunk.pos;
            this.nextPos = oracle.firstChunk.pos;
            this.pathProgression = 1f;
            this.allSubBehaviors = new List<SRSOracleSubBehavior>();
            this.currSubBehavior = new SRSOracleSubBehavior.NoSubBehavior(this);

            this.investigateAngle = UnityEngine.Random.value * 360f;
            this.working = 1f;
            this.getToWorking = 1f;
            this.movementBehavior = SRSOracleBehavior.MovementBehavior.Meditate;
            this.action = SRSOracleBehavior.Action.GeneralIdle;

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
                        this.movementBehavior = SRSOracleBehavior.MovementBehavior.Meditate;
                    }
                    break;
                case MovementBehavior.Meditate:
                    if (this.nextPos != this.oracle.room.MiddleOfTile(24, 17))
                    {
                        this.SetNewDestination(this.oracle.room.MiddleOfTile(24, 17));
                    }
                    this.investigateAngle = 0f;
                    this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
                  //  TestMod.Logger.LogWarning(this.lookPoint);

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
        }
        public float BasePosScore(Vector2 tryPos)
        {
            if (this.movementBehavior == SRSOracleBehavior.MovementBehavior.Meditate || this.player == null)
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
                //TestMod.Logger.LogWarning("oracle get pos");
                //TestMod.LogVector2(v);
                //TestMod.LogVector2(this.ClampToRoom(v));
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
                if (this.movementBehavior == SRSOracleBehavior.MovementBehavior.Idle)
                {
                    return Custom.DegToVec(this.investigateAngle);
                }
                if (this.movementBehavior == SRSOracleBehavior.MovementBehavior.Investigate)
                {
                    return Custom.DegToVec(this.investigateAngle);
                }
                return new Vector2(0f, 1f);
            }
        }

        public void NewAction(SRSOracleBehavior.Action nextAction)
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



    }
}
