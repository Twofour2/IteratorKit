using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace IteratorMod.SRS_Oracle
{
    public class SRSOracle: Oracle
    {
        public new SRSOracleArm arm; // todo: fix inheritance/override issues with oracle arm. see oracle graphics line 2525, likely points to the wrong oracle arm copy
        public new SRSOracleBehavior oracleBehavior;

        public new bool Consious = true;

        public class OracleID : Oracle.OracleID
        {
            // SS = Pebbles (inlc. rot pebbles)
            // SL = Moon
            // ST = Straw
            // DM = PastMoon (Alive)
            // CL = Saint Pebbles

            public static readonly OracleID SRS = new OracleID("SRS", register: true);

            public OracleID(string value, bool register = false) : base(value, register)
            {
                // nothing to do here
            }
        }

        public SRSOracle(AbstractPhysicalObject abstractPhysicalObject, Room room) : base(abstractPhysicalObject, room)
        {
            this.room = room;
            base.bodyChunks = new BodyChunk[2];

            
            this.mySwarmers = new List<OracleSwarmer>();
            base.airFriction = 0.99f;
            base.gravity = 0.9f;
            this.bounce = 0.1f;
            this.surfaceFriction = 0.17f;
            this.collisionLayer = 1;
            base.waterFriction = 0.92f;
            base.buoyancy = 0.95f;
            this.ID = OracleID.SRS;
            for (int k = 0; k < base.bodyChunks.Length; k++)
            {
                Vector2 pos = new Vector2(341f, 341f);
                base.bodyChunks[k] = new BodyChunk(this, k, pos, 6f, 0.5f);


            }
            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[1];
            this.bodyChunkConnections[0] = new PhysicalObject.BodyChunkConnection(base.bodyChunks[0], base.bodyChunks[1], 9f, PhysicalObject.BodyChunkConnection.Type.Normal, 1f, 0.5f);
            this.mySwarmers = new List<OracleSwarmer>();
            base.airFriction = 0.99f;
            

            this.oracleBehavior = new SRSOracleBehavior(this);
            this.arm = new SRSOracleArm(this);



        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            this.behaviorTime++;
            this.oracleBehavior.Update(eu);


            if(this.arm != null)
            {
                this.arm.Update();
            }
        }
        public override void InitiateGraphicsModule()
        {
            if (base.graphicsModule == null)
            {
                base.graphicsModule = new SRSOracleGraphics(this, this);
            }
        }
    }
}
