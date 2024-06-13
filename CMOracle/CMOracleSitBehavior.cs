using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using IteratorKit.Util;
using RWCustom;
using UnityEngine;
using static IteratorKit.CMOracle.CMOracleBehavior;

namespace IteratorKit.CMOracle
{
    public class CMOracleSitBehavior : SLOracleBehaviorHasMark, Conversation.IOwnAConversation
    {
        public CMOracle? cmOracle { get { return (this.oracle is CMOracle) ? this.oracle as CMOracle : null; } }

        public CMOracleBehaviorMixin cmMixin;

        public PhysicalObject inspectItem { get { return this.holdingObject; } set { this.holdingObject = value; this.cmMixin.inspectItem = value; } }
        public PhysicalObject cmMoveToAndPickUpItem = null;
        public OracleJData oracleJson { get { return this.oracle?.OracleData()?.oracleJson; } }
        public static Vector2 MoonDefaultPos { get { return new Vector2(1585f, ModManager.MSC ? 200f : 168f); } } // changes is MSC is enabled...

        public float roomGravity = 0.9f;
        public CMConversation cmConversation, conversationResumeTo = null;
        public int playerScore;
        public bool playerRelationshipJustChanged;

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

        public bool hadMainPlayerConversation
        {
            get { return ITKUtil.GetSaveDataValue<bool>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "hasHadPlayerConversation", false); }
            set { ITKUtil.SetSaveDataValue<bool>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "hasHadPlayerConversation", value); }
        }

        public CMPlayerRelationship playerRelationship
        {
            get { return (CMPlayerRelationship)ITKUtil.GetSaveDataValue<int>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "playerRelationship", (int)CMPlayerRelationship.normal); }
            set { ITKUtil.SetSaveDataValue<int>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "playerRelationship", (int)value); }
        }

        public CMOracleSitBehavior(Oracle oracle) : base(oracle)
        {
            this.oracle = oracle;
            this.cmMixin = this.OracleBehaviorShared();
            this.investigateAngle = 0;
            this.moonActive = false;
            this.SetNewDestination(cmOracle?.oracleJson?.startPos ?? MoonDefaultPos);
            this.cmMixin.SetGravity(1f);
        }

        public override void Update(bool eu)
        {
            if (!this.hasNoticedPlayer)
            {
                if (this.player != null && this.cmMixin.PlayerInRoom())
                {
                    if (this.oracleJson.playerNoticeDistance == -1 || Custom.Dist(this.oracle.firstChunk.pos, this.player.firstChunk.pos) < (this.oracleJson.playerNoticeDistance))
                    {
                        if (this.oracle.ID == Oracle.OracleID.SL)
                        {
                            // LTTM: Player must be on second screen
                            if (this.player.mainBodyChunk.pos.x > 1160f)
                            {
                                this.hasNoticedPlayer = true;
                                this.cmMixin.hasNoticedPlayer = true;
                            }
                        }
                        else
                        {
                            this.hasNoticedPlayer = true;
                            this.cmMixin.hasNoticedPlayer = true;
                        }
                        
                    }
                }
            }

            // moon uses holdingObject to do the same thing pebbles uses inspectItem for. We use pebbles variable name here.
            if (this.inspectItem != null)
            {
                this.TryHoldObject(eu);
            }
            if (this.oracle.ID != Oracle.OracleID.SL)
            {
                this.SittingMove();
            }
            else
            {
                base.Update(eu);
                this.currentConversation = null; // for LTTM, block any attempts to set this
            }

            
            this.cmMixin.CheckConversationEvents();
            this.CheckForConversationItem();
            this.cmMixin.cmScreen?.Update();

            cmMixin.Update();
            

            

        }

        public void SittingMove()
        {
            if (this.InSitPosition)
            {
                // Big pile of conditions needed to meet for oracle to hold knees
                if (
                    this.holdingObject == null &&
                    this.dontHoldKnees < 1 &&
                    UnityEngine.Random.value < 0.025f &&
                    (this.player == null || !Custom.DistLess(this.oracle.firstChunk.pos, this.player.DangerPos, 50f)) &&
                    !this.protest && this.oracle.health > 1f
                    )
                {
                    this.holdKnees = true;
                }
            }
            else
            {
                this.oracle.firstChunk.vel.x += ((this.oracle.firstChunk.pos.x < this.OracleGetToPos.x) ? 1f : -1f) * 0.6f * this.CrawlSpeed;
            }
            // constantly try to get the oracle to sit straight up
            this.oracle.WeightedPush(0, 1, new Vector2(0f, 1f), 4f * Mathf.InverseLerp(60f, 20f, Mathf.Abs(this.OracleGetToPos.x - this.oracle.firstChunk.pos.x)));
        }


        public void TryHoldObject(bool eu)
        {
            if (this.inspectItem == null)
            {
                return;
            }
            if (!this.oracle.Consious)
            {
                this.inspectItem = null; // drop it if we die
                return;
            }
            if (this.inspectItem.grabbedBy.Count > 0)
            {
                // todo: interrupt conversation from player theft
                this.inspectItem = null;
                return;
            }
            // move object to "hand" position, oracle graphics code will move the arm here
            this.inspectItem.firstChunk.MoveFromOutsideMyUpdate(eu, this.oracle.firstChunk.pos + new Vector2(-13f, -7f));
            this.inspectItem.firstChunk.vel *= 0f; // remove any velocity given by the above function call
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
                if (this.oracle is not CMOracle)
                {
                    return base.OracleGetToPos;
                }
                if (this.cmMoveToAndPickUpItem != null && this.moveToItemDelay > 40)
                {
                    return this.cmMoveToAndPickUpItem.firstChunk.pos;
                }
                return ITKUtil.GetWorldFromTile(this.cmOracle.oracleJson.startPos);

            }
        }

        public override Vector2 GetToDir
        {
            get
            {
                if (this.InSitPosition)
                {
                    return new Vector2(0f, 0f);
                }
                return new Vector2(0f, 0f);
            }
        }

        public delegate bool orig_InSitPosition(SLOracleBehavior self);
        public static bool CMInSitPosition(orig_InSitPosition orig, SLOracleBehavior self)
        {
            if (self.oracle is not CMOracle)
            {
                return orig(self);
            }
            if (self.oracle.OracleJson().type != OracleJData.OracleType.sitting)
            {
                return orig(self);
            }
            if (self.oracle.room.GetTilePosition(self.oracle.firstChunk.pos).x == (int)self.oracle.OracleJson().startPos.x)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clamp vector2 to oracle room
        /// </summary>
        public Vector2 ClampToRoom(Vector2 vector)
        {
            vector.x = Mathf.Clamp(vector.x, this.oracle.arm.cornerPositions[0].x + 100f, this.oracle.arm.cornerPositions[1].x - 100f);
            vector.y = Mathf.Clamp(vector.y, this.oracle.arm.cornerPositions[2].y + 100f, this.oracle.arm.cornerPositions[1].y - 100f);
            return vector;
        }

        /// <summary>
        /// Checks to see if there is an item to talk about in the oracles room. Sitting verison to mirror lttm
        /// </summary>
        public void CheckForConversationItem()
        {
            foreach (SocialEventRecognizer.OwnedItemOnGround ownedItem in this.oracle.room.socialEventRecognizer.ownedItemsOnGround)
            {
                if (
                    Custom.DistLess(ownedItem.item.firstChunk.pos, this.oracle.firstChunk.pos, 60f) &&
                    ownedItem.item.firstChunk.vel.magnitude < 5f &&
                    this.CMWillingToInspectItem(ownedItem.item))
                {
                    this.cmMixin.alreadyDiscussedItems.Add(ownedItem.item.abstractPhysicalObject);
                    this.cmMoveToAndPickUpItem = ownedItem.item;
                    IteratorKit.Log.LogInfo($"Moving to pickup {this.cmMoveToAndPickUpItem}");
                    if (this.oracle.ID == Oracle.OracleID.SL)
                    {
                        // moon trigger dialogs about player putting the item down
                        this.PlayerPutItemOnGround();
                    }
                    break;
                }
            }
            if (this.cmMoveToAndPickUpItem != null)
            {
                this.moveToItemDelay++;
                if (this.CMWillingToInspectItem(this.cmMoveToAndPickUpItem) || this.cmMoveToAndPickUpItem.grabbedBy.Count > 0)
                {
                    IteratorKit.Log.LogWarning($"No longer willing to pickup item {this.cmMoveToAndPickUpItem}! WillingToInspect: {this.CMWillingToInspectItem(this.moveToAndPickUpItem)} GrabbedBy: {this.moveToAndPickUpItem.grabbedBy.Count}");
                    this.cmMoveToAndPickUpItem = null;
                }else if (this.moveToItemDelay > 40 && 
                    Custom.DistLess(this.cmMoveToAndPickUpItem.firstChunk.pos, this.oracle.firstChunk.pos, 40f) || 
                    (this.moveToItemDelay < 20 && !Custom.DistLess(this.cmMoveToAndPickUpItem.firstChunk.lastPos, this.cmMoveToAndPickUpItem.firstChunk.pos, 5f) &&
                    Custom.DistLess(this.cmMoveToAndPickUpItem.firstChunk.pos, this.oracle.firstChunk.pos, 20f)))
                {
                    IteratorKit.Log.LogInfo($"Grabbed item {this.moveToAndPickUpItem}");
                    this.GrabObject(this.cmMoveToAndPickUpItem);
                    this.cmMoveToAndPickUpItem = null;

                    // start talking about it
                    this.cmMixin.StartItemConversation(this.inspectItem);
                }
            }
            else
            {
                this.moveToItemDelay = 0;
            }
        }

        public bool CMWillingToInspectItem(PhysicalObject item)
        {
            if (this.cmMixin.alreadyDiscussedItems.Contains(item.abstractPhysicalObject))
            {
                return false;
            }

            if (this.oracle.ID == Oracle.OracleID.SL)
            {
                return base.WillingToInspectItem(item);
            }
            // todo: maybe add logic here?
            if (item is Player)
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Called by oracle
        /// </summary>
        /// <param name="weapon"></param>
        public void ReactToHitByWeapon(Weapon weapon)
        {
            IteratorKit.Log.LogInfo("HIT BY WEAPON");
        }

        public void Collide(PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (otherObject is Player && this.cmConversation == null)
            {
                Player player = otherObject as Player;
                foreach (Creature.Grasp grasp in player.grasps)
                {
                    if (grasp == null) continue;
                    if (!this.CMWillingToInspectItem(grasp.grabbed)) continue; // not willing to talk about item
                    if (this.pickedUpItemsThisRealization.Any(x => x == grasp.grabbed.abstractPhysicalObject.ID)) continue; // have already discussed this item
                    // met all conditions
                    this.GrabObject(grasp.grabbed);
                    this.cmMixin.StartItemConversation(grasp.grabbed); // start talking about it
                   // this.inspectItem = grasp.grabbed; // rainworld doesn't do this here. but we do just to be safe.
                    player.ReleaseGrasp(player.grasps.IndexOf(grasp)); // tell the player to drop it
                    return; // flag = false
                }

            }
            // no return condition met, player is just being annoying
            this.playerAnnoyingCounter++;
        }
    }
}
