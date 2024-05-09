using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace IteratorKit.CMOracle
{
    public class CMOracleArm : Oracle.OracleArm
    {
        public OracleJData.OracleType oracleType;
        public CMOracleArm(CMOracle oracle, OracleJData.OracleType oracleType) : base(oracle) {
            this.oracle = oracle;
            this.oracleType = oracleType;
            IteratorKit.Log.LogInfo($"Created arm class for {this.oracle.ID}");
            this.baseMoveSoundLoop = new StaticSoundLoop(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, oracle.room, 1f, 1f);
            this.cornerPositions = new Vector2[4];
            List<OracleJDataTilePos> cornerPositionsJson = oracle.oracleJson.cornerPositions;
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
            if (self is not CMOracleArm)
            {
                orig(self);
                return;
            }
            CMOracleArm cmOracleArm = self as CMOracleArm;
            Oracle.OracleID armJointOracleId = Oracle.OracleID.SS;
            if (cmOracleArm.oracleType == OracleJData.OracleType.sitting)
            {
                armJointOracleId = Oracle.OracleID.SL;
                SittingArmUpdate(cmOracleArm);
            }
            else
            {
                armJointOracleId = Oracle.OracleID.SS;
                ActiveArmUpdate(cmOracleArm);
            }
            Oracle.OracleID tmpOracleId = self.oracle.ID;
            self.oracle.ID = armJointOracleId; // force use pebbles/lttm joints code, avoids rewriting it
            for (int j = 0; j < self.joints.Length; j++)
            {
                self.joints[j].Update();
            }
            self.oracle.ID = tmpOracleId; // set back
        }

        public static void ActiveArmUpdate(CMOracleArm self)
        {
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
        }

        public static void SittingArmUpdate(CMOracleArm self)
        {
            self.joints[1].vel += Vector2.ClampMagnitude(new Vector2(1774f, 66f) - self.joints[1].pos, 50f) / 50f * 5f;
            self.oracle.bodyChunks[0].vel *= 0.9f; //apply gravity
            self.oracle.bodyChunks[1].vel *= 0.9f;
            OracleBehavior oracleBehavior = self.oracle.oracleBehavior;

            // dead or player too close
            IteratorKit.Log.LogInfo(oracleBehavior.player?.DangerPos.x);
            if (!self.oracle.Consious || ((oracleBehavior.player?.DangerPos.x ?? 999f) < self.oracle.firstChunk.pos.x))
            {
                self.oracle.bodyChunks[0].vel += Vector2.ClampMagnitude(oracleBehavior.OracleGetToPos - self.oracle.bodyChunks[0].pos, 100f) / 100f * 0.5f;
                self.oracle.bodyChunks[0].vel += Vector2.ClampMagnitude(
                    oracleBehavior.OracleGetToPos - oracleBehavior.GetToDir * self.oracle.bodyChunkConnections[0].distance - self.oracle.bodyChunks[0].pos
                    , 100f) / 100f * 0.2f;
            }
        }
    }
}
