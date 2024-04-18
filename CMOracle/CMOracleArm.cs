using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace IteratorKit.CMOracle
{
    public class CMOracleArm : Oracle.OracleArm
    {
        public CMOracleArm(CMOracle oracle) : base(oracle) {
            this.oracle = oracle;
            IteratorKit.Log.LogInfo($"Created arm class for {this.oracle.ID}");
            this.baseMoveSoundLoop = new StaticSoundLoop(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, oracle.room, 1f, 1f);
            this.cornerPositions = new Vector2[4];
            List<OracleJsonTilePos> cornerPositionsJson = oracle.oracleJson.cornerPositions;
            for (int i = 0; i < 4; i++)
            {
                this.cornerPositions[i] = oracle.room.MiddleOfTile(cornerPositionsJson[i].x, cornerPositionsJson[i].y);
            }
            this.joints = new Joint[4];
            for (int i = 0; i < this.joints.Length; i++)
            {
                this.joints[i] = new Joint(this, i);
                if (i > 0)
                {
                    this.joints[i].previous = this.joints[i - 1];
                    this.joints[i - 1].next = this.joints[i];
                }
            }
            this.framePos = this.lastFramePos = 10002.5f;
        }

        public static void ArmUpdate(On.Oracle.OracleArm.orig_Update orig, Oracle.OracleArm self)
        {
            if (self.oracle is not CMOracle)
            {
                return;
            }
            CMOracle cmOracle = self.oracle as CMOracle;
            self.oracle.bodyChunks[0].vel *= 0.4f;
            self.oracle.bodyChunks[1].vel *= 0.4f;
            self.oracle.bodyChunks[0].vel += Vector2.ClampMagnitude(cmOracle.oracleBehavior.OracleGetToPos - cmOracle.bodyChunks[0].pos, 100f) / 100f * 6.2f;
            self.oracle.bodyChunks[1].vel += Vector2.ClampMagnitude(cmOracle.oracleBehavior.OracleGetToPos - cmOracle.oracleBehavior.GetToDir * cmOracle.bodyChunkConnections[0].distance - cmOracle.bodyChunks[0].pos, 100f) / 100f * 3.2f;

            Vector2 baseGetToPos = cmOracle.oracleBehavior.BaseGetToPos;
            Vector2 baseDistVector = new Vector2(Mathf.Clamp(baseGetToPos.x, self.cornerPositions[0].x, self.cornerPositions[1].x), self.cornerPositions[0].y);
            float baseNum3 = Mathf.InverseLerp(self.cornerPositions[0].x, self.cornerPositions[1].x, baseGetToPos.x);

            self.baseMoving = (Vector2.Distance(self.BasePos(1f), baseDistVector) > (self.baseMoving ? 50f : 350f) && cmOracle.oracleBehavior.consistentBasePosCounter > 30);
            self.lastFramePos = self.framePos;
            if (self.baseMoving)
            {
                self.framePos = Mathf.MoveTowardsAngle(self.framePos * 90f, baseNum3 * 90f, 1f) / 90f;
                if (self.baseMoveSoundLoop != null)
                {
                    self.baseMoveSoundLoop.volume = Mathf.Min(self.baseMoveSoundLoop.volume + 0.1f, 1f);
                    self.baseMoveSoundLoop.pitch = Mathf.Min(self.baseMoveSoundLoop.pitch + 0.025f, 1f);
                }
            }
            else if (self.baseMoveSoundLoop != null)
            {
                self.baseMoveSoundLoop.volume = Mathf.Max(self.baseMoveSoundLoop.volume - 0.1f, 0f);
                self.baseMoveSoundLoop.pitch = Mathf.Max(self.baseMoveSoundLoop.pitch - 0.025f, 0.5f);
            }

            if (self.baseMoveSoundLoop != null)
            {
                self.baseMoveSoundLoop.pos = self.BasePos(1f);
                self.baseMoveSoundLoop.Update();
                if (ModManager.MSC)
                {
                    self.baseMoveSoundLoop.volume *= 1f - self.oracle.noiseSuppress;
                }
            }
            Oracle.OracleID tmpOracleId = self.oracle.ID;
            self.oracle.ID = Oracle.OracleID.SS; // force use pebbles joints code, avoids rewriting it
            for (int j = 0; j < self.joints.Length; j++)
            {
                self.joints[j].Update();
            }
            self.oracle.ID = tmpOracleId; // set back
        }
    }
}
