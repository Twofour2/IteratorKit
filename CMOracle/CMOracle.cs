using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.Util;
using UnityEngine;
using static IteratorKit.CMOracle.OracleJSON.OracleEventsJson;

namespace IteratorKit.CMOracle
{
    /// <summary>
    /// Core Oracle class.
    /// This mod replicates structure names present in the base game. They are:
    /// CMOracle -> Core, links the classes together
    /// CMOracleArm -> Movement
    /// CMOracleGraphics -> Visuals
    /// CMConversation -> Dialogs
    /// Plus CMOracleData -> Weak Ref Table
    /// 
    /// Oracle IDs:
    /// SS = Pebbles (incl. rot pebbles)
    /// SL = Moon
    /// ST = Straw
    /// DM = PastMoon (Alive)
    /// CL = Saint Pebbles
    /// 
    /// This mod is built to re-use as much built in Oracle code whilst avoiding IL Hooking.
    /// The built in Oracle code makes heavy use of hard coding (i.e. "if pebbles do this"). As such we have to re-impliment code where this happens.
    /// </summary>
    public class CMOracle : Oracle
    {
        public OracleJSON oracleJson { get { return this.OracleData().oracleJson; } }

        public delegate OracleGraphics ForceGraphicsModule(CMOracle oracle);
        public ForceGraphicsModule CMForceGraphicsModule;
        public delegate void OnOracleInit(CMOracle oracle);
        public static OnOracleInit OnCMOracleInit;
        

        public static void ApplyHooks()
        {
            On.Oracle.Update += Update;
            On.Oracle.OracleArm.Update += CMOracleArm.ArmUpdate;
            On.Oracle.SetUpSwarmers += CMOracleSetupSwarmers;
        }

        
        public static void RemoveHooks()
        {
            On.Oracle.Update -= Update;
            On.Oracle.OracleArm.Update -= CMOracleArm.ArmUpdate;
            On.Oracle.SetUpSwarmers -= CMOracleSetupSwarmers;
           
        }

        public CMOracle(AbstractPhysicalObject abstractPhysicalObject, Room room, OracleJSON oracleJson) : base(abstractPhysicalObject, room)
        {
            this.OracleData().oracleJson = oracleJson;
            // most of these likely arent used here, but 5p defines them we do to.
            this.bounce = 0.1f; this.surfaceFriction = 0.17f; this.collisionLayer = 1; this.airFriction = 0.99f; this.waterFriction = 0.92f; this.health = 10f; this.stun = 0; this.buoyancy = 0.95f;
            this.ID = new OracleID(oracleJson.id, register: true);
            this.room = room;
            this.bodyChunks = new BodyChunk[2];
            this.mySwarmers = new List<OracleSwarmer>();
            for (int i = 0; i < this.bodyChunks.Length; i++)
            {
                Vector2 pos = (this.oracleJson.startPos != Vector2.zero) ? ITKUtil.GetWorldFromTile(this.oracleJson.startPos) : new Vector2(350f, 350f);
                pos.y *= i;
                this.bodyChunks[i] = new BodyChunk(this, i, pos, 6f, 0.5f);
            }
            this.bodyChunkConnections = new BodyChunkConnection[1];
            this.bodyChunkConnections[0] = new BodyChunkConnection(this.bodyChunks[0], this.bodyChunks[1], 9f, BodyChunkConnection.Type.Normal, 1f, 0.5f);
            this.oracleBehavior = new CMOracleBehavior(this);
            this.arm = new CMOracleArm(this);
            this.CMSetupSwarmers();

            if (this.oracleJson.roomEffects?.pearls != null)
            {
                this.marbles = new List<PebblesPearl>();
                this.SetUpMarbles();
            }
            this.myScreen = new OracleProjectionScreen(this.room, this.oracleBehavior);
            this.room.AddObject(this.myScreen);
            OnCMOracleInit?.Invoke(this);
            IteratorKit.Log.LogInfo($"Initialized oracle {this.ID}");
        }


        public override void InitiateGraphicsModule()
        {
            if (this.graphicsModule != null)
            {
                return;
            }
            OracleGraphics customGraphicsModule = this.CMForceGraphicsModule?.Invoke(this);
            if (customGraphicsModule == null) {
                this.graphicsModule = new CMOracleGraphics(this);
                return;
            }
            IteratorKit.Log.LogWarning($"IteratorKit is loading a custom graphics module \"{customGraphicsModule.GetType().Name}\" for oracle {this.ID}");
            this.graphicsModule = customGraphicsModule;
        }

        
        public static void Update(On.Oracle.orig_Update orig, Oracle self, bool eu)
        {
            if (self is not CMOracle)
            {
                orig(self, eu);
                return;
            }
            CMOracle cmOracle = self as CMOracle;

            // we need to force the orig method to think the oracle is dead so it runs less code
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
                cmOracle.oracleBehavior.Update(eu);
            }
            if (self.arm != null)
            {
                cmOracle.arm.Update();
            }
        }

        /// <summary>
        /// Block Oracle.SetupSwarmers() from running as we use our own method. 
        /// </summary>
        private static void CMOracleSetupSwarmers(On.Oracle.orig_SetUpSwarmers orig, Oracle self)
        {
            if (self is CMOracle) return;
            orig(self);
        }

        public void CMSetupSwarmers()
        {
            IteratorKit.Log.LogInfo("Setup swarmers");
            for(int i = 0; i < (this.oracleJson?.roomEffects?.swarmers ?? 0); i++)
            {
                SSOracleSwarmer swarmer = new SSOracleSwarmer(
                    new AbstractPhysicalObject(
                            this.room.world,
                            AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer,
                            null,
                            this.room.GetWorldCoordinate(this.oracleBehavior.OracleGetToPos),
                            this.room.game.GetNewID()
                            ), this.room.world);
                this.room.abstractRoom.entities.Add(swarmer.abstractPhysicalObject);
                this.room.AddObject(swarmer);
                this.mySwarmers.Add(swarmer);
            }
        }

        public override void HitByWeapon(Weapon weapon)
        {
            base.HitByWeapon(weapon);
           // (this.oracleBehavior as CMOracleBehavior).ReactToHitByWeapon(weapon);
        }
    }

}