using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using IteratorMod.CMOracle;
using RWCustom;
using UnityEngine;
using static IL.MoreSlugcats.MoreSlugcatsEnums;

namespace IteratorMod.CMOracle
{
    public class CMOracleArm : Oracle.OracleArm
    {

        public CMOracleArm(CMOracle oracle) : base(oracle)
        {
            this.oracle = oracle;
            this.baseMoveSoundLoop = new StaticSoundLoop(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, oracle.room, 1f, 1f);

            this.cornerPositions = new Vector2[4];
            List<OracleJsonTilePos> cornerPositionsJson = oracle.oracleJson.cornerPositions;

            this.cornerPositions[0] = oracle.room.MiddleOfTile(cornerPositionsJson[0].x, cornerPositionsJson[0].y);
            this.cornerPositions[1] = oracle.room.MiddleOfTile(cornerPositionsJson[1].x, cornerPositionsJson[1].y);
            this.cornerPositions[2] = oracle.room.MiddleOfTile(cornerPositionsJson[2].x, cornerPositionsJson[2].y);
            this.cornerPositions[3] = oracle.room.MiddleOfTile(cornerPositionsJson[3].x, cornerPositionsJson[3].y);
            IteratorKit.LogVector2(oracle.room.MiddleOfTile(10, 32));
            IteratorKit.LogVector2(this.cornerPositions[0]);

            this.joints = new Oracle.OracleArm.Joint[4];
            for (int k = 0; k < this.joints.Length; k++)
            {
                this.joints[k] = new Oracle.OracleArm.Joint(this, k);
                if (k > 0)
                {
                    this.joints[k].previous = this.joints[k - 1];
                    this.joints[k - 1].next = this.joints[k];
                }
            }
            this.framePos = 10002.5f;
            this.lastFramePos = this.framePos;

        }

        public static void ArmUpdate(On.Oracle.OracleArm.orig_Update orig, Oracle.OracleArm self)
        {
            if (self.oracle is CMOracle)
            {
                CMOracle cMOracle = (CMOracle)self.oracle;
                float num = 1f / 240f;
                self.oracle.bodyChunks[1].vel *= 0.4f;
                self.oracle.bodyChunks[0].vel *= 0.4f;
                self.oracle.bodyChunks[0].vel += Vector2.ClampMagnitude(cMOracle.oracleBehavior.OracleGetToPos - cMOracle.bodyChunks[0].pos, 100f) / 100f * 6.2f;
                self.oracle.bodyChunks[1].vel += Vector2.ClampMagnitude(cMOracle.oracleBehavior.OracleGetToPos - cMOracle.oracleBehavior.GetToDir * cMOracle.bodyChunkConnections[0].distance - cMOracle.bodyChunks[0].pos, 100f) / 100f * 3.2f * num;

                Vector2 baseGetToPos = cMOracle.oracleBehavior.BaseGetToPos;

                Vector2 vector = new Vector2(Mathf.Clamp(baseGetToPos.x, self.cornerPositions[0].x, self.cornerPositions[1].x), self.cornerPositions[0].y);

                float num2 = Vector2.Distance(vector, baseGetToPos);
                float num3 = Mathf.InverseLerp(self.cornerPositions[0].x, self.cornerPositions[1].x, baseGetToPos.x);

                self.baseMoving = (Vector2.Distance(self.BasePos(1f), vector) > (self.baseMoving ? 50f : 350f) && cMOracle.oracleBehavior.consistentBasePosCounter > 30);
                self.lastFramePos = self.framePos;
                if (self.baseMoving)
                {

                    self.framePos = Mathf.MoveTowardsAngle(self.framePos * 90f, num3 * 90f, 1f) / 90f;
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
                self.oracle.ID = Oracle.OracleID.SS; // force use pebbles joints code, avoids rewriting it
                for (int j = 0; j < self.joints.Length; j++)
                {
                    self.joints[j].Update();
               }
                self.oracle.ID = CMOracle.SRS; // set back
            }
            else
            {
                orig(self);
            }
        }


    }

        
    
}
