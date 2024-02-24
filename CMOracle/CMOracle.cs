using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using IteratorKit.CMOracle;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using static IteratorKit.CMOracle.CMOracleBehavior;

namespace IteratorKit.CMOracle
{
    public class CMOracle : Oracle
    {

        public OracleJSON oracleJson
        {
            get
            {
                return this.GetOracleData().oracleJson;
            }
        }

        public bool IsSitting
        {
            get { return oracleJson.type == OracleJSON.OracleType.sitting; }
        }

        // public static readonly OracleID CM = new OracleID("CM", register: true);

        // SS = Pebbles (inlc. rot pebbles)
        // SL = Moon
        // ST = Straw
        // DM = PastMoon (Alive)
        // CL = Saint Pebbles

        public delegate OracleGraphics ForceGraphicsModule(CMOracle oracle);
        public static ForceGraphicsModule CMForceGraphicsModule;


        public CMOracle(AbstractPhysicalObject abstractPhysicalObject, Room room, OracleJSON oracleJson) : base(abstractPhysicalObject, room)
        {
            this.GetOracleData().oracleJson = this.oracleJson; // store in CWT

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
            this.ID = new OracleID(oracleJson.id, register: true);
            for (int k = 0; k < base.bodyChunks.Length; k++)
            {
                Vector2 pos = (this.oracleJson.startPos != Vector2.zero) ? GetWorldFromTile(this.oracleJson.startPos) : new Vector2(350f, 350f);
                
                pos.y = pos.y * k;
                base.bodyChunks[k] = new BodyChunk(this, k, pos, 6f, 0.5f);

            }

            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[1];
            this.bodyChunkConnections[0] = new PhysicalObject.BodyChunkConnection(base.bodyChunks[0], base.bodyChunks[1], 9f, PhysicalObject.BodyChunkConnection.Type.Normal, 1f, 0.5f);
            base.airFriction = 0.99f;


            this.oracleBehavior = this.IsSitting ? new CMOracleSitBehavior(this) : new CMOracleBehavior(this);
            this.arm = new CMOracleArm(this);

            this.SetUpSwarmers();

            

            if (this.oracleJson.roomEffects?.pearls != null)
            {
                this.marbles = new List<PebblesPearl>();
                this.SetUpMarbles();
            }
            IteratorKit.Logger.LogWarning("init screen");
            this.myScreen = new OracleProjectionScreen(this.room, this.oracleBehavior);
            this.room.AddObject(this.myScreen);


            

        }

        public static void ApplyHooks()
        {
            On.OracleGraphics.Gown.Color += CMOracleGraphics.CMGown.CMColor;
            On.Oracle.Update += CMOracle.Update;
            On.Oracle.OracleArm.Update += CMOracleArm.ArmUpdate;
            On.OracleGraphics.Halo.InitiateSprites += CMOracleGraphics.HaloInitSprites;
            On.OracleGraphics.ArmJointGraphics.BaseColor += CMOracleGraphics.BaseColor;
            On.OracleGraphics.ArmJointGraphics.HighLightColor += CMOracleGraphics.HighlightColor;
            On.Oracle.SetUpSwarmers += CMOracle.SetUpSwarmers;

        }

        public static void RemoveHooks()
        {
            On.OracleGraphics.Gown.Color -= CMOracleGraphics.CMGown.CMColor;
            On.Oracle.Update -= CMOracle.Update;
            On.Oracle.OracleArm.Update -= CMOracleArm.ArmUpdate;
            On.OracleGraphics.Halo.InitiateSprites -= CMOracleGraphics.HaloInitSprites;
            On.OracleGraphics.ArmJointGraphics.BaseColor -= CMOracleGraphics.BaseColor;
            On.OracleGraphics.ArmJointGraphics.HighLightColor -= CMOracleGraphics.HighlightColor;
            On.Oracle.SetUpSwarmers -= CMOracle.SetUpSwarmers;
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
                OracleGraphics customGraphicsModule = CMForceGraphicsModule?.Invoke(this);
                if (customGraphicsModule != null)
                {
                    IteratorKit.Logger.LogWarning($"IteratorKit is loading a custom graphics module \"{customGraphicsModule.GetType().Name}\" for oracle {this.ID}");
                    base.graphicsModule = customGraphicsModule;
                }
                else
                {
                    base.graphicsModule = new CMOracleGraphics(this, this);
                }
                
            }
        }

        public static void SetUpSwarmers(On.Oracle.orig_SetUpSwarmers orig, Oracle self)
        {
            IteratorKit.Logger.LogWarning("SETUP SWARMERS");
            if (self is CMOracle)
            {
                CMOracle cMOracle = (CMOracle)self;
                if (cMOracle.oracleJson == null)
                {
                    return;
                }
                
                for (int i = 0; i < (cMOracle.oracleJson?.roomEffects?.swarmers ?? 0); i++)
                {
                    SSOracleSwarmer swarmer = new SSOracleSwarmer(
                        new AbstractPhysicalObject(
                            self.room.world,
                            AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer,
                            null,
                            self.room.GetWorldCoordinate(self.oracleBehavior.OracleGetToPos),
                            self.room.game.GetNewID()
                            ), self.room.world);
                    self.room.abstractRoom.entities.Add(swarmer.abstractPhysicalObject);
                    self.room.AddObject(swarmer);
                    self.mySwarmers.Add(swarmer);
                }
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

        public static Vector2 GetWorldFromTile(Vector2 pos)
        {
            // does reverse of the Room GetTilePosition
            return new Vector2(((pos.x + 1) * 20) - 20, ((pos.y + 1) * 20) - 20);
        }
    }
}
