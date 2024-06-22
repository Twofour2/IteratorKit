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
    /// This has SSOracleBehavior as a base class to allow SSOracle to accept using this class in place of its oracleBehavior. 
    /// It is not used at all by this class.
    /// CMOracleBehaviorMixin (here defined as cmMixin) is used to called shared mix-in logic
    /// </summary>
    public class CMOracleBehavior : SSOracleBehavior, Conversation.IOwnAConversation
    {
        public CMOracle? cmOracle { get { return (this.oracle is CMOracle) ? this.oracle as CMOracle : null; } }
        public new Oracle oracle { get { return base.oracle; } set { base.oracle = value; } }

        public CMOracleBehaviorMixin cmMixin = null;

        public new Vector2 currentGetTo, lastPos, nextPos, lastPosHandle, nextPosHandle, baseIdeal, idlePos;
        public new float pathProgression, investigateAngle, invstAngSpeed;
        public CMOracleMovement movement;
        public new bool floatyMovement = false;
        private int meditateTick;

        public OracleJData oracleJson { get { return this.oracle?.OracleData()?.oracleJson; } }
        public new Player player { get { return base.player; } }

        // hotfix section: stop mods that used this from crashing.
        public delegate void CMEventStart(CMOracleBehavior cmBehavior, string eventName, OracleEventObjectJData eventData);
        public delegate void CMEventEnd(CMOracleBehavior cmBehavior, string eventName);
        public static CMEventStart OnEventStart;
        public static CMEventEnd OnEventEnd;
        public float roomGravity { set { this.cmMixin.roomGravity = value; } get { return this.cmMixin.roomGravity; } }
        // end hotfix

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
            this.cmMixin = this.OracleBehaviorShared();
            this.oracle.OracleEvents().OnCMEventStart += this.DialogEventActivate;
            this.currentGetTo = this.nextPos = this.lastPos = oracle.firstChunk.pos;
            this.pathProgression = 1f;
            this.investigateAngle = (this.oracleJson.type == OracleJData.OracleType.normal) ? UnityEngine.Random.value * 360f : 0f;
            this.movement = CMOracleMovement.idle;
            this.cmMixin.action = CMOracleAction.generalIdle;
            this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
            this.idlePos = (this.oracleJson.startPos != Vector2.zero) ? ITKUtil.GetWorldFromTile(this.oracleJson.startPos) : ITKUtil.GetWorldFromTile(this.oracle.room.RandomTile().ToVector2());
            
            IteratorKit.Log.LogInfo($"Created behavior class for {this.oracle.ID}");
            this.SetNewDestination(this.idlePos);
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
            this.cmMixin.CheckActions();
            this.cmMixin.cmScreen.Update();
            this.cmMixin.CheckConversationEvents();

            if (this.player != null && this.player.room == this.oracle.room)
            {
                this.lookPoint = this.player.firstChunk.pos; // look at player
                cmMixin.hasNoticedPlayer = true;
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

            if (this.cmMixin.inspectItem != null)
            {
                this.HoldObjectInPlace(this.cmMixin.inspectItem, this.oracle.firstChunk.pos);
                if (cmMixin.cmConversation == null && Custom.Dist(this.oracle.firstChunk.pos, this.cmMixin.inspectItem.firstChunk.pos) < 100f)
                {
                    IteratorKit.Log.LogInfo($"Starting conversation about item {this.cmMixin.inspectItem.abstractPhysicalObject}");
                    this.cmMixin.StartItemConversation(this.cmMixin.inspectItem);
                }
            }
            if (this.player != null)
            {
                this.CheckForConversationItem();
            }
            this.cmMixin.Update();
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
                Vector2 targetPos = this.currentGetTo;
                if (this.floatyMovement && Custom.DistLess(this.oracle.firstChunk.pos, this.nextPos, 50f))
                {
                    targetPos = this.nextPos;
                }
                return this.ClampToRoom(targetPos);
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
        /// Runs when a dialog event starts, when it starts displaying text on screen.
        /// This reads out the dialog data and acts on any additional data in it
        /// </summary>
        public void DialogEventActivate(OracleBehavior cmBehavior, string eventId, Conversation.DialogueEvent dialogEvent, OracleEventObjectJData eventData)
        {
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
            if (eventData.moveTo != Vector2.zero)
            {
                this.SetNewDestination(eventData.moveTo);
            }
        }



        

        /// <summary>
        /// Checks to see if there is an item to talk about in the oracles room. Normal version to mirror 5p
        /// </summary>
        public void CheckForConversationItem()
        {
            if (this.player.room != this.oracle.room || this.cmMixin.cmConversation != null || this.cmMixin.sayHelloDelay > 0 || this.cmMixin.inspectItem != null)
            {
                return;
            }
            List<PhysicalObject> physicalObjects = this.oracle.room.physicalObjects.SelectMany(x => x).ToList();
            foreach (PhysicalObject physObject in physicalObjects)
            {
                if (this.cmMixin.alreadyDiscussedItems.Contains(physObject.abstractPhysicalObject) || physObject.grabbedBy.Count > 0)
                {
                    continue;
                }
                this.cmMixin.alreadyDiscussedItems.Add(physObject.abstractPhysicalObject);
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
                    if (this.oracleJson.ignorePearlIds?.Contains(pearl.AbstractPearl.dataPearlType.value) ?? false)
                    {
                        continue; // in ignore list
                    }
                    this.cmMixin.inspectItem = pearl;
                    IteratorKit.Log.LogInfo($"Set inspect pearl to {pearl.AbstractPearl.dataPearlType.value}");
                }
                else
                {
                    if (ExtEnumBase.TryParse(typeof(SLOracleBehaviorHasMark.MiscItemType), physObject.GetType().ToString(), true, out ExtEnumBase result))
                    {
                        IteratorKit.Log.LogInfo($"Found a valid item to discuss {physObject.GetType()}");
                        this.cmMixin.StartItemConversation(physObject);
                        this.cmMixin.inspectItem = physObject;

                    }
                }

            }

        }
    }
}
