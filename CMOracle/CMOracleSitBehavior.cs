using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using IteratorKit.Util;
using RWCustom;
using UnityEngine;

namespace IteratorKit.CMOracle
{
    public class CMOracleSitBehavior : SLOracleBehaviorHasMark
    {
        public CMOracle? cmOracle { get { return (this.oracle is CMOracle) ? this.oracle as CMOracle : null; } }
        public OracleJData oracleJson { get { return this.oracle?.OracleData()?.oracleJson; } }
        public static Vector2 MoonDefaultPos { get { return new Vector2(1585f, ModManager.MSC ? 200f : 168f); } } // changes is MSC is enabled...

        public float roomGravity = 0.9f;
        
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
        public CMOracleSitBehavior(Oracle oracle) : base(oracle)
        {
            this.oracle = oracle;
            this.investigateAngle = 0;
            this.moonActive = false;
            this.SetNewDestination(cmOracle?.oracleJson?.startPos ?? MoonDefaultPos);
            this.SetGravity(1f);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (!this.hasNoticedPlayer)
            {
                if (this.player != null && this.player.room == this.oracle.room)
                {
                    if (this.oracleJson.playerNoticeDistance == -1 || Custom.Dist(this.oracle.firstChunk.pos, this.player.firstChunk.pos) < (this.oracleJson.playerNoticeDistance))
                    {
                        this.hasNoticedPlayer = true;
                        // note: moon here has some velocity to make her sit up.
                    }
                }
            }
            if (this.holdingObject != null)
            {
                this.TryHoldObject(eu);
            }
            this.SittingMove();

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
        }

        public void TryHoldObject(bool eu)
        {
            if (this.holdingObject != null)
            {
                return;
            }
            if (!this.oracle.Consious)
            {
                this.holdingObject = null; // drop it if we die
                return;
            }
            if (this.holdingObject.grabbedBy.Count > 0)
            {
                // todo: interrupt conversation from player theft
                this.holdingObject = null;
                return;
            }
            // move object to "hand" position, guess: does arm move to this position?
            this.holdingObject.firstChunk.MoveFromOutsideMyUpdate(eu, this.oracle.firstChunk.pos + new Vector2(-18f, -7f)); 
            this.holdingObject.firstChunk.vel *= 0f; // remove any velocity given by the above function call
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
