using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using IteratorMod.CMOracle;
using MoreSlugcats;
using RWCustom;
using UnityEngine;

namespace IteratorMod.CMOracle
{
    public class CMOracle : Oracle
    {

        public OracleJSON oracleJson;

        public static readonly OracleID CM = new OracleID("CM", register: true);

        // SS = Pebbles (inlc. rot pebbles)
        // SL = Moon
        // ST = Straw
        // DM = PastMoon (Alive)
        // CL = Saint Pebbles

        public CMOracle(AbstractPhysicalObject abstractPhysicalObject, Room room, OracleJSON oracleJson) : base(abstractPhysicalObject, room)
        {
            this.oracleJson = oracleJson;

            this.room = room;
            base.bodyChunks = new BodyChunk[2];

            this.mySwarmers = new List<OracleSwarmer>();
            base.airFriction = this.oracleJson.airFriction;
            base.gravity = this.oracleJson.gravity;
            this.bounce = 0.1f;
            this.surfaceFriction = 0.17f;
            this.collisionLayer = 1;
            base.waterFriction = 0.92f;
            this.health = 10f;
            this.stun = 0;
            base.buoyancy = 0.95f;
            this.ID = CMOracle.CM;
            for (int k = 0; k < base.bodyChunks.Length; k++)
            {
                Vector2 pos = new Vector2(350f, 350f);
                base.bodyChunks[k] = new BodyChunk(this, k, pos, 6f, 0.5f);

            }
            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[1];
            this.bodyChunkConnections[0] = new PhysicalObject.BodyChunkConnection(base.bodyChunks[0], base.bodyChunks[1], 9f, PhysicalObject.BodyChunkConnection.Type.Normal, 1f, 0.5f);
            this.mySwarmers = new List<OracleSwarmer>();
            base.airFriction = 0.99f;
            

            this.oracleBehavior = new CMOracleBehavior(this);
            this.arm = new CMOracleArm(this);

        }

        public static void ApplyHooks()
        {
            On.OracleGraphics.Gown.Color += CMOracleGraphics.CMGown.CMColor;
            On.Oracle.Update += CMOracle.Update;
            On.Oracle.OracleArm.Update += CMOracleArm.ArmUpdate;
            On.Oracle.SetUpSwarmers += CMOracle.SetUpSwarmers;
        }
        public static void Update(On.Oracle.orig_Update orig, Oracle self, bool eu)
        {
            if (self is CMOracle)
            {
                CMOracle cMOracle = (CMOracle)self;
                OracleArm tmpOracleArm = self.arm;
                float tmpHealth = self.health;
                self.arm = null;
                self.health = -10;
                orig(self, eu);
                self.arm = tmpOracleArm;
                self.health = tmpHealth;
                if (self.Consious)
                {
                    self.behaviorTime++;
                    cMOracle.oracleBehavior.Update(eu);
                }

                if (self.arm != null)
                {
                    cMOracle.arm.Update();
                }

            }
            else
            {
                orig(self, eu);
            }
        }

        public override void InitiateGraphicsModule()
        {
            if (base.graphicsModule == null)
            {
                base.graphicsModule = new CMOracleGraphics(this, this);
            }
        }

        public static void SetUpSwarmers(On.Oracle.orig_SetUpSwarmers orig, Oracle self)
        {
            if (self is CMOracle)
            {
                return;
            }
            else
            {
                orig(self);
                return;
            }
        }

        public override void HitByWeapon(Weapon weapon)
        {
            base.HitByWeapon(weapon);
            (this.oracleBehavior as CMOracleBehavior).ReactToHitByWeapon(weapon);
        }
    }
}
