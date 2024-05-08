using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using IL.MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace IteratorKit.CMOracle
{
    public class CMOracleSitBehavior : SLOracleBehaviorHasMark
    {
        public CMOracle? cmOracle { get { return (this.oracle is CMOracle) ? this.oracle as CMOracle : null; } }
        public static Vector2 MoonDefaultPos { get { return new Vector2(1585f, ModManager.MSC ? 200f : 168f); } } // changes is MSC is enabled...
        
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
            this.SetNewDestination(cmOracle?.oracleJson?.startPos ?? MoonDefaultPos);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

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
                if (this.moonActive)
                {
                    Vector2 v = this.currentGetTo;
                    if (this.floatyMovement && Custom.DistLess(this.oracle.firstChunk.pos, this.nextPos, 50f))
                    {
                        v = this.nextPos;
                    }
                    return this.ClampToRoom(v);
                }
                if (this.oracle.room.game.IsMoonActive())
                {
                    return new Vector2(1585f, 160f);
                }
                return MoonDefaultPos;
                
            }
        }

        public override Vector2 GetToDir
        {
            get
            {
                return new Vector2(0f, 1f);
            }
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

    }
}
