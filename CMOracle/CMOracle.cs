using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.Util;
using MonoMod.RuntimeDetour;
using UnityEngine;
using static IteratorKit.CMOracle.OracleJData.OracleEventsJData;

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
        public OracleJData oracleJson { get { return this.OracleData().oracleJson; } }

        public delegate void OnOracleSetupGraphicsModule(CMOracle oracle);

        /// <summary>
        /// Use this to modify oracle graphics. i.e. oracle.
        /// Necassary as graphics setup happens at a different stage.
        /// </summary>
        public OnOracleSetupGraphicsModule OnCMOracleSetupGraphicsModule;

        public delegate void OnOracleSetupModules(CMOracle oracle);

        /// <summary>
        /// Use this to setup your own code. i.e. oracle.oracleBehavior = new MyBehaviorClass()
        /// Use OnCMOracleSetupGraphicsModule for the graphics module.
        /// </summary>
        public static OnOracleSetupModules OnCMOracleSetupModules;

        
        public delegate void OnOracleInit(CMOracle oracle);
        /// <summary>
        /// Triggers when initialization is finished
        /// </summary>
        /// <param name="oracle"></param>
        public static OnOracleInit OnCMOracleInit;

        public static Hook oracleBehaviorInSitPositionHook;
        public static Hook oracleGraphicsIsMoonHook;
        public static void ApplyHooks()
        {
            On.Oracle.Update += Update;
            On.Oracle.OracleArm.Update += CMOracleArm.ArmUpdate;
            On.Oracle.SetUpSwarmers += CMOracleSetupSwarmers;
            On.OracleGraphics.Gown.Color += CMOracleGraphics.GownColor;
            On.OracleGraphics.ArmJointGraphics.BaseColor += CMOracleGraphics.ArmBaseColor;
            On.OracleGraphics.ArmJointGraphics.HighLightColor += CMOracleGraphics.ArmHighlightColor;
            On.OracleGraphics.Update += CMOracleGraphics.CMOracleGraphicsUpdate;
            On.Oracle.OracleArm.BasePos += CMOracleArm.BasePos;
            On.Oracle.OracleArm.BaseDir += CMOracleArm.BaseDir;
            oracleBehaviorInSitPositionHook = new Hook(
                typeof(SLOracleBehavior).GetMethod("get_InSitPosition"),
                typeof(CMOracleSitBehavior).GetMethod("CMInSitPosition"));
            oracleGraphicsIsMoonHook = new Hook(
                typeof(OracleGraphics).GetMethod("get_IsMoon"),
                typeof(CMOracleGraphics).GetMethod("CMIsMoon")
                );

        }

        public static void RemoveHooks()
        {
            On.Oracle.Update -= Update;
            On.Oracle.OracleArm.Update -= CMOracleArm.ArmUpdate;
            On.Oracle.SetUpSwarmers -= CMOracleSetupSwarmers;
            On.OracleGraphics.Gown.Color -= CMOracleGraphics.GownColor;
            On.OracleGraphics.ArmJointGraphics.BaseColor -= CMOracleGraphics.ArmBaseColor;
            On.OracleGraphics.ArmJointGraphics.HighLightColor -= CMOracleGraphics.ArmHighlightColor;
            On.OracleGraphics.Update -= CMOracleGraphics.CMOracleGraphicsUpdate;
            On.Oracle.OracleArm.BasePos -= CMOracleArm.BasePos;
            On.Oracle.OracleArm.BaseDir -= CMOracleArm.BaseDir;
            oracleBehaviorInSitPositionHook.Dispose();
            oracleGraphicsIsMoonHook.Dispose();
        }

        public CMOracle(AbstractPhysicalObject abstractPhysicalObject, Room room, OracleJData oracleJson) : base(abstractPhysicalObject, room)
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
                this.bodyChunks[i] = new BodyChunk(this, i, pos, 6f, 0.5f);
            }
            this.bodyChunkConnections = new BodyChunkConnection[1];
            // body chunks is reversed here, stops em' spawning upside down
            this.bodyChunkConnections[0] = new BodyChunkConnection(this.bodyChunks[1], this.bodyChunks[0], 9f, BodyChunkConnection.Type.Normal, 1f, 0.5f);
            
            this.oracleBehavior = null; // base() set this to SLOracleBehaviorHasMark
            this.arm = null;
            // check for custom modules
            OnCMOracleSetupModules?.Invoke(this);

            if (this.oracleBehavior == null)
            {
                this.oracleBehavior = (this.oracleJson.type == OracleJData.OracleType.normal) ? new CMOracleBehavior(this) : new CMOracleSitBehavior(this);
            }

            if (this.arm == null)
            {
                this.arm = new CMOracleArm(this, this.oracleJson.type);
            }
            
            this.gravity = (this.oracleJson.type == OracleJData.OracleType.normal) ? 0f : 0.9f;
            this.CMSetupSwarmers();

            if (this.oracleJson.roomEffects?.pearls != null)
            {
                this.marbles = new List<PebblesPearl>();
                this.SetUpMarbles();
            }
            this.myScreen = new OracleProjectionScreen(this.room, this.oracleBehavior);
            this.room.AddObject(this.myScreen);
            
            IteratorKit.Log.LogInfo($"Initialized oracle {this.ID}");
        }



        public override void InitiateGraphicsModule()
        {
            OnCMOracleSetupGraphicsModule?.Invoke(this); // expected set this.graphicsModule if using custom
            if (this.graphicsModule == null)
            {
                this.graphicsModule = new CMOracleGraphics(this);
                return;
            }
            IteratorKit.Log.LogWarning($"IteratorKit is loading a custom graphics module \"{this.graphicsModule.GetType().Name}\" for oracle {this.ID}");

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
            int? numNeurons = ITKUtil.GetSaveDataValue(this.room.game.session as StoryGameSession, this.ID, "neurons", this.oracleJson.neurons);
            if (numNeurons == null)
            {
                return;
            }
            for (int i = 0; i < (numNeurons ?? 0); i++)
            {
                SLOracleSwarmer swarmer = new SLOracleSwarmer(
                    new AbstractPhysicalObject(
                            this.room.world,
                            AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer,
                            null,
                            this.room.GetWorldCoordinate(this.firstChunk.pos),
                            this.room.game.GetNewID()
                            ), this.room.world);
                this.room.abstractRoom.entities.Add(swarmer.abstractPhysicalObject);
                swarmer.firstChunk.HardSetPosition(this.firstChunk.pos);
                this.room.AddObject(swarmer);
                this.mySwarmers.Add(swarmer);
            }
        }

        public override void HitByWeapon(Weapon weapon)
        {
            if (this.ID == OracleID.SL || this.ID == OracleID.SS)
            {
                base.HitByWeapon(weapon);
                return;
            }
            if (this.oracleBehavior is CMOracleBehavior)
            {
                (this.oracleBehavior as CMOracleBehavior).cmMixin.ReactToHitByWeapon(weapon);
                return;
            }
            if (this.oracleBehavior is CMOracleSitBehavior)
            {
                (this.oracleBehavior as CMOracleSitBehavior).cmMixin.ReactToHitByWeapon(weapon);
                return;
            }
            
           // 
        }
    }

}