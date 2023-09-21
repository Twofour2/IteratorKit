using System;
using System.Collections.Generic;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace IteratorMod.SRS_Oracle
{
    public class CMOracleArm : Oracle.OracleArm
    {
        public new CMOracle oracle;

        public CMOracleArm(CMOracle oracle) : base(oracle)
        {
            this.oracle = oracle;
            this.baseMoveSoundLoop = new StaticSoundLoop(SoundID.SS_AI_Base_Move_LOOP, oracle.firstChunk.pos, oracle.room, 1f, 1f);

            this.cornerPositions = new Vector2[4];


            this.cornerPositions[0] = oracle.room.MiddleOfTile(10, 33);
            this.cornerPositions[1] = oracle.room.MiddleOfTile(38, 33);
            this.cornerPositions[2] = oracle.room.MiddleOfTile(38, 3);
            this.cornerPositions[3] = oracle.room.MiddleOfTile(10, 3);


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


        public new void Update()
        { 
            if (!this.oracle.Consious)
            {
                return;
            }
            float num = 1f / 240f;

            this.oracle.bodyChunks[1].vel *= 0.4f;
            this.oracle.bodyChunks[0].vel *= 0f;
            this.oracle.bodyChunks[0].vel += Vector2.ClampMagnitude(this.oracle.oracleBehavior.OracleGetToPos - this.oracle.bodyChunks[0].pos, 100f) / 100f * 6.2f;
            this.oracle.bodyChunks[1].vel += Vector2.ClampMagnitude(this.oracle.oracleBehavior.OracleGetToPos - this.oracle.oracleBehavior.GetToDir * this.oracle.bodyChunkConnections[0].distance - this.oracle.bodyChunks[0].pos, 100f) / 100f * 3.2f * num;



            Vector2 baseGetToPos = this.oracle.oracleBehavior.BaseGetToPos;
            
            Vector2 vector = new Vector2(Mathf.Clamp(baseGetToPos.x, this.cornerPositions[0].x, this.cornerPositions[1].x), this.cornerPositions[0].y);

            float num2 = Vector2.Distance(vector, baseGetToPos);
            float num3 = Mathf.InverseLerp(this.cornerPositions[0].x, this.cornerPositions[1].x, baseGetToPos.x);


            baseMoving = Vector2.Distance(BasePos(1f), vector) > (baseMoving ? 50f : 350f) && oracle.oracleBehavior.consistentBasePosCounter > 30;
            lastFramePos = framePos;
            if (baseMoving)
            {
                framePos = Mathf.MoveTowardsAngle(framePos * 90f, num3 * 90f, 1f) / 90f;
                if (baseMoveSoundLoop != null)
                {
                    baseMoveSoundLoop.volume = Mathf.Min(baseMoveSoundLoop.volume + 0.1f, 1f);
                    baseMoveSoundLoop.pitch = Mathf.Min(baseMoveSoundLoop.pitch + 0.025f, 1f);
                }
            }
            else if (baseMoveSoundLoop != null)
            {
                baseMoveSoundLoop.volume = Mathf.Max(baseMoveSoundLoop.volume - 0.1f, 0f);
                baseMoveSoundLoop.pitch = Mathf.Max(baseMoveSoundLoop.pitch - 0.025f, 0.5f);
            }

            if (baseMoveSoundLoop != null)
            {
                baseMoveSoundLoop.pos = BasePos(1f);
                baseMoveSoundLoop.Update();
                if (ModManager.MSC)
                {
                    baseMoveSoundLoop.volume *= 1f - oracle.noiseSuppress;
                }
            }

            this.oracle.ID = Oracle.OracleID.SS; // force use pebbles joints code, avoids rewriting it
            for (int j = 0; j < joints.Length; j++)
            {
                joints[j].Update();
            }
            this.oracle.ID = CMOracle.OracleID.SRS; // set back

        }

        public new Vector2 BaseDir(float timeStacker)
        {
            float num = Mathf.Lerp(this.lastFramePos, this.framePos, timeStacker) % 4f;
            float num2 = 0.1f;
            if (num < num2)
            {
                return Vector3.Slerp(new Vector2(1f, 0f), new Vector2(0f, -1f), 0.5f + Mathf.InverseLerp(0f, num2, num) * 0.5f);
            }
            if (num < 1f - num2)
            {
                return new Vector2(0f, -1f);
            }
            if (num < 1f + num2)
            {
                return Vector3.Slerp(new Vector2(0f, -1f), new Vector2(-1f, 0f), Mathf.InverseLerp(1f - num2, 1f + num2, num));
            }
            if (num < 2f - num2)
            {
                return new Vector2(-1f, 0f);
            }
            if (num < 2f + num2)
            {
                return Vector3.Slerp(new Vector2(-1f, 0f), new Vector2(0f, 1f), Mathf.InverseLerp(2f - num2, 2f + num2, num));
            }
            if (num < 3f - num2)
            {
                return new Vector2(0f, 1f);
            }
            if (num < 3f + num2)
            {
                return Vector3.Slerp(new Vector2(0f, 1f), new Vector2(1f, 0f), Mathf.InverseLerp(3f - num2, 3f + num2, num));
            }
            if (num < 4f - num2)
            {
                return new Vector2(1f, 0f);
            }
            return Vector3.Slerp(new Vector2(1f, 0f), new Vector2(0f, -1f), Mathf.InverseLerp(4f - num2, 4f, num) * 0.5f);
        }

        public new Vector2 OnFramePos(float timeStacker)
        {
            float num = Mathf.Lerp(this.lastFramePos, this.framePos, timeStacker) % 4f;
            float num2 = 0.1f;
            float num3 = Mathf.Abs(this.cornerPositions[0].x - this.cornerPositions[1].x) * num2;
            Vector2 vector = default(Vector2);
            float ang;
            if (num < num2)
            {
                vector = new Vector2(this.cornerPositions[0].x + num3, this.cornerPositions[1].y - num3);
                ang = -45f + Mathf.InverseLerp(0f, num2, num) * 45f;
            }
            else
            {
                if (num < 1f - num2)
                {
                    return Vector2.Lerp(this.cornerPositions[0], this.cornerPositions[1], Mathf.InverseLerp(0f, 1f, num));
                }
                if (num < 1f + num2)
                {
                    vector = new Vector2(this.cornerPositions[1].x - num3, this.cornerPositions[1].y - num3);
                    ang = Mathf.InverseLerp(1f - num2, 1f + num2, num) * 90f;
                }
                else
                {
                    if (num < 2f - num2)
                    {
                        return Vector2.Lerp(this.cornerPositions[1], this.cornerPositions[2], Mathf.InverseLerp(1f, 2f, num));
                    }
                    if (num < 2f + num2)
                    {
                        vector = new Vector2(this.cornerPositions[2].x - num3, this.cornerPositions[2].y + num3);
                        ang = 90f + Mathf.InverseLerp(2f - num2, 2f + num2, num) * 90f;
                    }
                    else
                    {
                        if (num < 3f - num2)
                        {
                            return Vector2.Lerp(this.cornerPositions[2], this.cornerPositions[3], Mathf.InverseLerp(2f, 3f, num));
                        }
                        if (num < 3f + num2)
                        {
                            vector = new Vector2(this.cornerPositions[3].x + num3, this.cornerPositions[3].y + num3);
                            ang = 180f + Mathf.InverseLerp(3f - num2, 3f + num2, num) * 90f;
                        }
                        else
                        {
                            if (num < 4f - num2)
                            {
                                return Vector2.Lerp(this.cornerPositions[3], this.cornerPositions[0], Mathf.InverseLerp(3f, 4f, num));
                            }
                            vector = new Vector2(this.cornerPositions[0].x + num3, this.cornerPositions[0].y - num3);
                            ang = 270f + Mathf.InverseLerp(4f - num2, 4f, num) * 45f;
                        }
                    }
                }
            }
            return vector + Custom.DegToVec(ang) * num3;
        }
        }

        
    
}
