using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.Util;
using UnityEngine;
using RWCustom;

namespace IteratorKit.CMOracle
{
    public class CMOracleBehavior : OracleBehavior, Conversation.IOwnAConversation
    {
        public Vector2 currentGetTo, lastPos, nextPos, lastPosHandle, nextPosHandle, baseIdeal, idlePos;
        public float pathProgression, investigateAngle;
        public CMOracleMovement movement;
        public CMOracleAction action;
        public PhysicalObject inspectItem;
       // public CMConversation cmConversation = null;
        public bool forceGravity = true;
        public float roomGravity = 0.9f;
        public bool hasNoticedPlayer;
        public int playerOutOfRoomCounter, timeSinceSeenPlayer = 0;

        public OracleJSON oracleJson { get { return this.oracle?.GetOracleData()?.oracleJson; } }
        public enum CMOracleAction
        {
            none, generalIdle, giveMark, giveKarma, giveMaxKarma, giveFood, startPlayerConversation, kickPlayerOut, killPlayer, redCure, customAction
        }
        public enum CMOracleMovement
        {
            idle, meditate, investigate, keepDistance, talk
        }

        public delegate void CMEventStart(CMOracleBehavior cmBehavior, string eventName, OracleJSON.OracleEventsJson.OracleEventObjectJson eventData);
        public delegate void CMEventEnd(CMOracleBehavior cmBehavior, string eventName);
        public static CMEventStart OnEventStart;
        public static CMEventEnd OnEventEnd;

        public CMOracleBehavior(Oracle oracle) : base(oracle)
        {
            this.oracle = oracle;
            this.currentGetTo = this.nextPos = this.lastPos = oracle.firstChunk.pos;
            this.pathProgression = 1f;
            this.investigateAngle = UnityEngine.Random.value * 360f;
            this.movement = CMOracleMovement.idle;
            this.action = CMOracleAction.generalIdle;
            this.investigateAngle = 0f;
            this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
            this.idlePos = (this.oracleJson.startPos != Vector2.zero) ? ITKUtil.GetWorldFromTile(this.oracleJson.startPos) : ITKUtil.GetWorldFromTile(this.oracle.room.RandomTile().ToVector2());
            this.forceGravity = true;
            this.roomGravity = this.oracleJson?.gravity ?? 0.9f;
            List<AntiGravity> antiGravEffects = this.oracle.room.updateList.OfType<AntiGravity>().ToList();
            foreach (AntiGravity antiGravEffect in  antiGravEffects)
            {
                antiGravEffect.active = (this.roomGravity >= 1);
            }
            IteratorKit.Log.LogInfo($"Created behavior class for {this.oracle.ID}");
            this.SetNewDestination(new Vector2(313f, 517f));
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            // this.Move();
            this.pathProgression = Mathf.Min(1f, this.pathProgression + 1f / Mathf.Lerp(40f + this.pathProgression * 80f, Vector2.Distance(this.lastPos, this.nextPos) / 5f, 0.5f));
            this.currentGetTo = Custom.Bezier(this.lastPos, this.ClampToRoom(this.lastPos + this.lastPosHandle), this.nextPos, this.ClampToRoom(this.nextPos + this.nextPosHandle), this.pathProgression);
            this.allStillCounter++;
            this.inActionCounter++;
            if (this.pathProgression < 1f || this.consistentBasePosCounter <= 100 || this.oracle.arm.baseMoving) // todo test
            {
                this.allStillCounter = 0;
            }
            //CheckActions(); // runs actions like giveMark. moved out of update to avoid mess. 
            //ShowScreenImages();
            //CheckConversationEvents();

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

            //if (this.inspectItem != null && this.cmConversation == null)
            //{
            //    IteratorKit.Log.LogInfo($"Starting conversation about item {this.inspectItem.abstractPhysicalObject}");
            //   // this.StartItemConversation(this.inspectItem);
            //}
            if (this.inspectItem != null)
            {
                // hold in place logic
                this.HoldObjectInPlace(this.inspectItem);
            }
            if (this.player != null)
            {

            }

        }



        public void SetNewDestination(Vector2 dst)
        {
            IteratorKit.Log.LogInfo($"Set new target destination {dst}");
            this.lastPos = this.currentGetTo;
            this.nextPos = dst;
            this.lastPosHandle = Custom.RNV() * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
            this.nextPosHandle = -this.GetToDir * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
            this.pathProgression = 0f;
        }

        public override Vector2 OracleGetToPos
        {
            get
            {
                Vector2 v = this.currentGetTo;
                if (Custom.DistLess(this.oracle.firstChunk.pos, this.nextPos, 50f))
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

        public Vector2 ClampToRoom(Vector2 vector)
        {
            vector.x = Mathf.Clamp(vector.x, this.oracle.arm.cornerPositions[0].x + 10f, this.oracle.arm.cornerPositions[1].x - 10f);
            vector.y = Mathf.Clamp(vector.y, this.oracle.arm.cornerPositions[2].y + 10f, this.oracle.arm.cornerPositions[1].y - 10f);
            return vector;
        }

        private void HoldObjectInPlace(PhysicalObject physicalObject)
        {
            Vector2 objectHoldPos = this.oracle.firstChunk.pos - physicalObject.firstChunk.pos;
            float dist = Custom.Dist(this.oracle.firstChunk.pos, physicalObject.firstChunk.pos);
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
    }
}
