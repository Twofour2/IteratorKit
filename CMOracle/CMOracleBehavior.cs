using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorKit.Util;
using UnityEngine;
using RWCustom;
using HUD;
using SlugBase.SaveData;
using static IteratorKit.CMOracle.OracleJSON.OracleEventsJson;

namespace IteratorKit.CMOracle
{
    
    public class CMOracleBehavior : SSOracleBehavior, Conversation.IOwnAConversation
    {
        public CMOracle? cmOracle { get { return (this.oracle is CMOracle) ? this.oracle as CMOracle : null; } }

        public new Vector2 currentGetTo, lastPos, nextPos, lastPosHandle, nextPosHandle, baseIdeal, idlePos;
        public new float pathProgression, investigateAngle, invstAngSpeed;
        public CMOracleMovement movement;
        public new CMOracleAction action;
        public PhysicalObject inspectItem;
        public CMConversation cmConversation = null;
        public bool forceGravity = true;
        public new bool floatyMovement = false;
        public float roomGravity = 0.9f;
        public bool hasNoticedPlayer;
        public new int playerOutOfRoomCounter, timeSinceSeenPlayer;
        public int sayHelloDelay = -1;
        private int meditateTick;
        private string actionParam;

        public OracleJSON oracleJson { get { return this.oracle?.OracleData()?.oracleJson; } }
        public bool hadMainPlayerConversation
        {
            get { return ITKUtil.GetSaveDataValue<bool>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "hasHadPlayerConversation", false);}
            set { ITKUtil.SetSaveDataValue<bool>(this.oracle.room.game.session as StoryGameSession, this.oracle.ID, "hasHadPlayerConversation", value);}
        }

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
        public enum CMOracleAction
        {
            none, generalIdle, giveMark, giveKarma, giveMaxKarma, giveFood, startPlayerConversation, kickPlayerOut, killPlayer, redCure, customAction
        }
        public enum CMOracleMovement
        {
            idle, meditate, investigate, keepDistance, talk
        }

        

        public CMOracleBehavior(Oracle oracle) : base(oracle)
        {
            this.oracle = oracle;
            this.oracle.OracleEvents().OnCMEventStart += this.DialogEventActivate;
            
            this.currentGetTo = this.nextPos = this.lastPos = oracle.firstChunk.pos;
            this.pathProgression = 1f;
            this.investigateAngle = UnityEngine.Random.value * 360f;
            this.movement = CMOracleMovement.idle;
            this.action = CMOracleAction.generalIdle;
            this.investigateAngle = 0f;
            this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
            this.idlePos = (this.oracleJson.startPos != Vector2.zero) ? ITKUtil.GetWorldFromTile(this.oracleJson.startPos) : ITKUtil.GetWorldFromTile(this.oracle.room.RandomTile().ToVector2());
            this.forceGravity = true;
            this.roomGravity = this.oracleJson?.gravity ?? 0.9f;
            List<AntiGravity> antiGravEffects = this.oracle.room.updateList.OfType<AntiGravity>().ToList();
            foreach (AntiGravity antiGravEffect in  antiGravEffects)
            {
                antiGravEffect.active = (this.roomGravity >= 1);
            }
            IteratorKit.Log.LogInfo($"Created behavior class for {this.oracle.ID}");
            this.SetNewDestination(new Vector2(313f, 517f));
        }

        

        public override void Update(bool eu)
        {
            this.floatyMovement = false; // must be before this.Move()
            this.Move();
            this.pathProgression = Mathf.Min(1f, this.pathProgression + 1f / Mathf.Lerp(40f + this.pathProgression * 80f, Vector2.Distance(this.lastPos, this.nextPos) / 5f, 0.5f));
            this.currentGetTo = Custom.Bezier(this.lastPos, this.ClampToRoom(this.lastPos + this.lastPosHandle), this.nextPos, this.ClampToRoom(this.nextPos + this.nextPosHandle), this.pathProgression);
            this.allStillCounter++;
            this.inActionCounter++;
            if (this.pathProgression < 1f || this.consistentBasePosCounter <= 100 || this.oracle.arm.baseMoving) // todo test
            {
                this.allStillCounter = 0;
            }
            CheckActions();
            //ShowScreenImages();
            CheckConversationEvents();

            if (this.player != null && this.player.room == this.oracle.room)
            {
                this.lookPoint = this.player.firstChunk.pos; // look at player
                this.hasNoticedPlayer = true;
                if (this.playerOutOfRoomCounter > 0)
                {
                    // first seeing player
                    this.timeSinceSeenPlayer = 0;
                }
                this.playerOutOfRoomCounter = 0;
            }
            else
            {
                this.playerOutOfRoomCounter++;
            }

            //if (this.inspectItem != null && this.cmConversation == null)
            //{
            //    IteratorKit.Log.LogInfo($"Starting conversation about item {this.inspectItem.abstractPhysicalObject}");
            //   // this.StartItemConversation(this.inspectItem);
            //}
            if (this.inspectItem != null)
            {
                // hold in place logic
                this.HoldObjectInPlace(this.inspectItem);
            }
            if (this.player != null)
            {

            }

            if (this.forceGravity)
            {
                this.oracle.room.gravity = this.roomGravity;
            }
            if (this.cmConversation != null)
            {
                this.cmConversation.Update();
            }

        }

        public new void SetNewDestination(Vector2 dst)
        {
            IteratorKit.Log.LogInfo($"Set new target destination {dst}");
            this.lastPos = this.currentGetTo;
            this.nextPos = dst;
            this.lastPosHandle = Custom.RNV() * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
            this.nextPosHandle = -this.GetToDir * Mathf.Lerp(0.3f, 0.65f, UnityEngine.Random.value) * Vector2.Distance(this.lastPos, this.nextPos);
            this.pathProgression = 0f;
        }

        public override Vector2 OracleGetToPos
        {
            get
            {
                Vector2 v = this.currentGetTo;
                if (this.floatyMovement && Custom.DistLess(this.oracle.firstChunk.pos, this.nextPos, 50f))
                {
                    v = this.nextPos;
                }
                return this.ClampToRoom(v);
            }
        }

        public override Vector2 BaseGetToPos
        {
            get
            {
                return this.baseIdeal;
            }
        }

        public float BasePosScore(Vector2 tryPos)
        {
            if (this.movement == CMOracleMovement.meditate || this.player == null)
            {
                return Vector2.Distance(tryPos, this.oracle.room.MiddleOfTile(24, 5));
            }

            return Mathf.Abs(Vector2.Distance(this.nextPos, tryPos) - 200f) + Custom.LerpMap(Vector2.Distance(this.player.DangerPos, tryPos), 40f, 300f, 800f, 0f);
        }

        public float CommunicatePosScore(Vector2 tryPos)
        {
            if (this.oracle.room.GetTile(tryPos).Solid || this.player == null)
            {
                return float.MaxValue;
            }

            Vector2 dangerPos = this.player.DangerPos;
            float num = Vector2.Distance(tryPos, dangerPos);
            if (this.oracle is CMOracle)
            {
                num -= (tryPos.x + this.oracle.OracleData().oracleJson.talkHeight);
            }
            else
            {
                num -= tryPos.x;
            }
            num -= ((float)this.oracle.room.aimap.getTerrainProximity(tryPos)) * 10f;
            return num;
        }

        public Vector2 ClampToRoom(Vector2 vector)
        {
            vector.x = Mathf.Clamp(vector.x, this.oracle.arm.cornerPositions[0].x + 10f, this.oracle.arm.cornerPositions[1].x - 10f);
            vector.y = Mathf.Clamp(vector.y, this.oracle.arm.cornerPositions[2].y + 10f, this.oracle.arm.cornerPositions[1].y - 10f);
            return vector;
        }

        private void HoldObjectInPlace(PhysicalObject physicalObject)
        {
            Vector2 objectHoldPos = this.oracle.firstChunk.pos - physicalObject.firstChunk.pos;
            float dist = Custom.Dist(this.oracle.firstChunk.pos, physicalObject.firstChunk.pos);
            physicalObject.firstChunk.vel += Vector2.ClampMagnitude(objectHoldPos, 40f) / 40f * Mathf.Clamp(2f - dist / 200f * 2f, 0.5f, 2f);
            if (physicalObject.firstChunk.vel.magnitude < 1f && dist < 16f)
            {
                physicalObject.firstChunk.vel = Custom.RNV() * 8f;
            }
            if (physicalObject.firstChunk.vel.magnitude > 8f)
            {
                physicalObject.firstChunk.vel /= 2f;
            }
        }

        private void HoldPlayerAt(Vector2 holdTarget)
        {
            foreach (Player player in this.PlayersInRoom)
            {
                player.mainBodyChunk.vel += holdTarget;
            }
        }

        private void Move()
        {
            switch (this.movement)
            {
                case CMOracleMovement.idle:
                    // goes to set idle pos, in the json file this is the startPos
                    if (this.nextPos != this.idlePos)
                    {
                        this.SetNewDestination(this.idlePos);
                        this.investigateAngle = 0f;
                    }
                    break;
                case CMOracleMovement.meditate:
                    this.investigateAngle = 0f;
                    this.lookPoint = this.oracle.firstChunk.pos + new Vector2(0f, -40f);
                    this.meditateTick++;
                    for (int i = 0; i < this.oracle.mySwarmers.Count; i++)
                    {
                        OracleSwarmer swarmer = this.oracle.mySwarmers[i];
                        float num = 20f;
                        float num2 = (float)this.meditateTick * 0.035f;
                        num *= (i % 2 == 0) ? Mathf.Sin(num2) : Mathf.Cos(num2);
                        float num3 = (float)i * 6.28f / (float)this.oracle.mySwarmers.Count;
                        num3 += (float)this.meditateTick * 0.0035f;
                        num3 %= 6.28f;
                        float num4 = 90f + num;
                        Vector2 startVec = new Vector2(Mathf.Cos(num3) * num4 + this.oracle.firstChunk.pos.x, -Mathf.Sin(num3) * num4 + this.oracle.firstChunk.pos.y);
                        Vector2 endVec = new Vector2(swarmer.firstChunk.pos.x + (startVec.x - swarmer.firstChunk.pos.x) * 0.05f, swarmer.firstChunk.pos.y + (startVec.y - swarmer.firstChunk.pos.y) * 0.05f);
                        swarmer.firstChunk.HardSetPosition(endVec);
                        swarmer.firstChunk.vel = Vector2.zero;
                        if (swarmer.ping <= 0)
                        {
                            swarmer.rotation = 0f;
                            swarmer.revolveSpeed = 0f;
                            if (this.meditateTick > 120 && (double)UnityEngine.Random.value <= 0.0015)
                            {
                                swarmer.ping = 40;
                                this.oracle.room.AddObject(new Explosion.ExplosionLight(this.oracle.mySwarmers[i].firstChunk.pos, 500f + UnityEngine.Random.value * 400f, 1f, 10, Color.cyan));
                                this.oracle.room.AddObject(new ElectricDeath.SparkFlash(this.oracle.mySwarmers[i].firstChunk.pos, 0.75f + UnityEngine.Random.value));
                                if (this.player != null && this.player.room == this.oracle.room)
                                {
                                    this.oracle.room.PlaySound(SoundID.HUD_Exit_Game, this.player.mainBodyChunk.pos, 1f, 2f + (float)i / (float)this.oracle.mySwarmers.Count * 2f);
                                }
                            }
                        }
                        else
                        {
                            swarmer.ping--;
                        }
                    }
                    break;
                case CMOracleMovement.investigate:
                    if (this.player == null)
                    {
                        this.movement = CMOracleMovement.idle;
                        break;
                    }
                    this.lookPoint = this.player.DangerPos;
                    if (this.investigateAngle < -90f || this.investigateAngle > 90f || (float)this.oracle.room.aimap.getTerrainProximity(this.nextPos) < 2f)
                    {
                        this.investigateAngle = Mathf.Lerp(-70f, 70f, UnityEngine.Random.value);
                        this.invstAngSpeed = Mathf.Lerp(0.4f, 0.8f, UnityEngine.Random.value) * ((UnityEngine.Random.value < 0.5f) ? -1 : 1f);
                    }
                    Vector2 getToVector = this.player.DangerPos + Custom.DegToVec(this.investigateAngle) * 150f;
                    if ((float)this.oracle.room.aimap.getTerrainProximity(getToVector) >= 2f)
                    {
                        if (this.pathProgression > 0.9f)
                        {
                            if (Custom.DistLess(this.oracle.firstChunk.pos, getToVector, 30f))
                            {
                                this.floatyMovement = true;
                            }
                            else if (!Custom.DistLess(this.nextPos, getToVector, 30f))
                            {
                                this.SetNewDestination(getToVector);
                            }
                        }
                        this.nextPos = getToVector;
                    }
                    break;
                case CMOracleMovement.keepDistance:
                    if (this.player == null)
                    {
                        this.movement = CMOracleMovement.idle;
                    }
                    else
                    {
                        this.lookPoint = this.player.DangerPos;
                        Vector2 distancePoint = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
                        if (!this.oracle.room.GetTile(distancePoint).Solid && this.oracle.room.aimap.getTerrainProximity(distancePoint) > 2
                            && Vector2.Distance(distancePoint, this.player.DangerPos) > Vector2.Distance(this.nextPos, this.player.DangerPos) + 100f)
                        {
                            this.SetNewDestination(distancePoint);
                        }
                        this.investigateAngle = 0f;
                    }
                    break;
                case CMOracleMovement.talk:
                    if (this.player == null)
                    {
                        this.movement = CMOracleMovement.idle;
                    }
                    else
                    {
                        this.lookPoint = this.player.DangerPos;
                        Vector2 tryPos = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
                        if (this.CommunicatePosScore(tryPos) + 40f < this.CommunicatePosScore(this.nextPos) && !Custom.DistLess(tryPos, this.nextPos, 30f))
                        {
                            this.SetNewDestination(tryPos);
                        }
                    }
                    break;
            }

            this.consistentBasePosCounter++;

            // try pick a random new base position.
            Vector2 baseTarget = new Vector2(UnityEngine.Random.value * this.oracle.room.PixelWidth, UnityEngine.Random.value * this.oracle.room.PixelHeight);
            if (this.oracle.room.GetTile(baseTarget).Solid || this.BasePosScore(baseTarget) + 40.0 >= this.BasePosScore(this.baseIdeal))
            {
                return;
            }
            this.baseIdeal = baseTarget;
            this.consistentBasePosCounter = 0;
        }

        public void CheckActions()
        {
            if (this.player == null)
            {
                return;
            }
            // actions should reset back to CMOracleAction.generalIdle if they wish for future conversations to continue working.
            // actions such as kill/kickOut dont allow any further actions to take place after they have occurred
            switch (this.action)
            {
                case CMOracleAction.generalIdle:
                    // nothing
                    break;
                case CMOracleAction.giveMark:
                    if (((StoryGameSession)this.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                    {
                        IteratorKit.Log.LogWarning("Player already has mark!");
                        this.action = CMOracleAction.generalIdle;
                        return;
                    }
                    if (this.inActionCounter == 30)
                    {
                        this.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Telekenisis, 0f, 1f, 1f);
                    }else if (this.inActionCounter > 30 && this.inActionCounter < 300)
                    {
                        this.StunCoopPlayers(20);
                        Vector2 holdTarget = Vector2.ClampMagnitude(this.oracle.room.MiddleOfTile(24, 14) - this.player.mainBodyChunk.pos, 40f) / 40f * 2.8f * Mathf.InverseLerp(30f, 160f, (float)this.inActionCounter);
                        this.HoldPlayerAt(holdTarget);
                    }else if (this.inActionCounter == 300)
                    {
                        this.action = CMOracleAction.generalIdle;
                        this.player.AddFood(10);
                        foreach (Player player in this.PlayersInRoom)
                        {
                            for (int i = 0; i < 20; i++)
                            {
                                this.oracle.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                            }
                        }
                        ((StoryGameSession)this.player.room.game.session).saveState.deathPersistentSaveData.theMark = true;
                        //this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "afterGiveMark");
                    }

                    break;
                case CMOracleAction.giveKarma:
                    int karmaCap = 0;
                    if (!Int32.TryParse(this.actionParam, out karmaCap))
                    {
                        IteratorKit.Log.LogError($"Failed to convert action param {this.actionParam} to integer");
                        break;
                    }
                    StoryGameSession session = ((StoryGameSession)this.oracle.room.game.session);
                    if (karmaCap >= 0)
                    {
                        session.saveState.deathPersistentSaveData.karmaCap = karmaCap;
                        session.saveState.deathPersistentSaveData.karma = karmaCap;
                    }
                    else
                    { // -1 passed, set to current max
                        session.saveState.deathPersistentSaveData.karma = karmaCap;
                    }

                    this.oracle.room.game.manager.rainWorld.progression.SaveDeathPersistentDataOfCurrentState(false, false);
                    foreach (RoomCamera camera in this.oracle.room.game.cameras)
                    {

                        if (camera.hud.karmaMeter != null)
                        {
                            camera.hud.karmaMeter.forceVisibleCounter = 80;
                            camera.hud.karmaMeter.UpdateGraphic();
                            camera.hud.karmaMeter.reinforceAnimation = 1;
                            ((StoryGameSession)this.oracle.room.game.session).AppendTimeOnCycleEnd(true);
                        }
                    }
                    foreach (Player player in base.PlayersInRoom)
                    {
                        for (int i = 0; i < 20; i++)
                        {
                            this.oracle.room.AddObject(new Spark(player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        }
                    }
                    break;
                case CMOracleAction.giveFood:
                    if (!Int32.TryParse(this.actionParam, out int playerFood))
                    {
                        playerFood = this.player.MaxFoodInStomach;
                    }
                    this.player.AddFood(playerFood);
                    this.action = CMOracleAction.generalIdle;
                    break;
                case CMOracleAction.startPlayerConversation:
                    //this.cmConversation = new CMConversation(this, CMConversation.CMDialogType.Generic, "playerConversation");
                    //this.action = CMOracleAction.generalIdle;
                    //SetHasHadMainPlayerConversation(true);
                    break;
                case CMOracleAction.kickPlayerOut:
                    ShortcutData? shortcut = ITKUtil.GetShortcutToRoom(this.oracle.room, this.actionParam);
                    if (shortcut == null)
                    {
                        IteratorKit.Log.LogError("Cannot kick player out as destination does not exist!");
                        this.action = CMOracleAction.generalIdle;
                        return;
                    }
                    Vector2 shortcutVector = this.oracle.room.MiddleOfTile(shortcut.Value.startCoord);
                    foreach (Player player in this.PlayersInRoom)
                    {
                        player.mainBodyChunk.vel += Custom.DirVec(player.mainBodyChunk.pos, shortcutVector);
                    }
                    break;
                case CMOracleAction.killPlayer:
                    if (this.player.dead || this.player.room != this.oracle.room)
                    {
                        break;
                    }
                    IteratorKit.Log.LogInfo("Oracle killing player");
                    this.player.mainBodyChunk.vel += Custom.RNV() * 12f;
                    for (int i = 0; i < 20; i++)
                    {
                        this.oracle.room.AddObject(new Spark(this.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                        this.player.Die();
                    }
                    break;
                case CMOracleAction.redCure:
                    this.oracle.room.game.GetStorySession.saveState.redExtraCycles = true;
                    if (this.oracle.room.game.cameras[0].hud != null)
                    {
                        if (this.oracle.room.game.cameras[0].hud.textPrompt != null)
                        {
                            this.oracle.room.game.cameras[0].hud.textPrompt.cycleTick = 0;
                        }
                        if (this.oracle.room.game.cameras[0].hud.map != null && this.oracle.room.game.cameras[0].hud.map.cycleLabel != null)
                        {
                            this.oracle.room.game.cameras[0].hud.map.cycleLabel.UpdateCycleText();
                        }
                    }
                    this.player.redsIllness?.GetBetter();
                    if (ModManager.CoopAvailable)
                    {
                        foreach (AbstractCreature abstractCreature in this.oracle.room.game.AlivePlayers)
                        {
                            if (abstractCreature.Room != this.oracle.room.abstractRoom)
                            {
                                continue;
                            }
                            RedsIllness playerIllness = (abstractCreature.realizedCreature as Player)?.redsIllness;
                            if (playerIllness == null)
                            {
                                continue;
                            }
                            playerIllness.GetBetter();
                        }
                    }
                    break;
            }
        }

        public void CheckConversationEvents()
        {
            if (this.player == null)
            {
                return;
            }
            if (this.hasNoticedPlayer)
            {
                if (this.sayHelloDelay < 0)
                {
                    this.sayHelloDelay = 30;
                }
                else
                {
                    if(this.sayHelloDelay > 0)
                    {
                        this.sayHelloDelay--;
                    }
                    if (this.sayHelloDelay == 1)
                    {
                        this.oracle.room.game.cameras[0].EnterCutsceneMode(this.player.abstractCreature, RoomCamera.CameraCutsceneType.Oracle);
                        IteratorKit.Log.LogInfo($"Has had main conversation? {this.hadMainPlayerConversation}");
                        if (!this.hadMainPlayerConversation && (this.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark)
                        {
                            IteratorKit.Log.LogInfo("Starting main player conversation");
                            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerConversation");
                        }
                        else
                        {
                            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerEnter");
                        }
                        
                    }
                }
            }
        }

        /// <summary>
        /// Runs when a dialog event starts, when it starts displaying text on screen.
        /// This reads out the dialog data and acts on any additional data in it
        /// </summary>
        public void DialogEventActivate(CMOracleBehavior cmBehavior, string eventId, Conversation.DialogueEvent dialogEvent, OracleEventObjectJson eventData)
        {
            IteratorKit.Log.LogWarning("On dialog event activate invoked!");
            if (eventData.score != null)
            {
               // this.ChangePlayerScore(eventData.score.action, eventData.score.amount); todo
            }
            if (eventData.movement != null)
            {
                if (Enum.TryParse(eventData.movement, out CMOracleMovement tmpMovement))
                {
                    this.movement = tmpMovement;
                    IteratorKit.Log.LogInfo($"Event {eventData.eventId} changed movement type to {eventData.movement}");
                }
                else
                {
                    IteratorKit.Log.LogError($"Event {eventData.eventId} provided an invalid movement option {eventData.movement}");
                }
            }
            if (eventData.screens.Count > 0)
            {
               // this.ShowScreens(eventData.screens); todo
            }
            if (eventData.moveTo != UnityEngine.Vector2.zero)
            {
                this.SetNewDestination(eventData.moveTo);
            }
            if (eventData.gravity != -50f)
            {
                this.forceGravity = true;
                this.roomGravity = eventData.gravity;
                List<AntiGravity> antiGravEffects = this.oracle.room.updateList.OfType<AntiGravity>().ToList();
                foreach (AntiGravity antiGravEffect in antiGravEffects)
                {
                    antiGravEffect.active = (this.roomGravity >= 1);
                }
            }
        }

        public void ReactToHitByWeapon(Weapon weapon)
        {
            IteratorKit.Log.LogWarning("oracle hit by weapon");
            if (UnityEngine.Random.value < 0.5f)
            {
                this.oracle.room.PlaySound(SoundID.SS_AI_Talk_1, this.oracle.firstChunk).requireActiveUpkeep = false;
            }
            else
            {
                this.oracle.room.PlaySound(SoundID.SS_AI_Talk_4, this.oracle.firstChunk).requireActiveUpkeep = false;
            }
            IteratorKit.Log.LogWarning("Player attack convo");
          //  this.conversationResumeTo = this.cmConversation;
            // clear the current dialog box
            if (this.dialogBox.messages.Count > 0)
            {
                this.dialogBox.messages = new List<DialogBox.Message>
                {
                    this.dialogBox.messages[0]
                };
                this.dialogBox.lingerCounter = this.dialogBox.messages[0].linger + 1;
                this.dialogBox.showCharacter = this.dialogBox.messages[0].text.Length + 2;
            }
            this.cmConversation = new CMConversation(this, CMConversation.CMDialogCategory.Generic, "playerAttack");
        }
    }
}
