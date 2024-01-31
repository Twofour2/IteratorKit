using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using IteratorKit.CMOracle;
using System.Runtime.CompilerServices;

namespace IteratorKit.CMOracle
{
    public class CMOracleGraphics : OracleGraphics
    {
        public new CMOracle oracle
        {
            get
            {
                return base.owner as CMOracle;
            }
        }
        public bool IsSuns = true;

        public int sigilSprite;

        public int sunFinL, sunFinR;

        public OracleJSON.OracleBodyJson bodyJson;

        public static ArmBase staticCheckArmBase;

        public CMOracleGraphics(PhysicalObject ow, CMOracle oracle) : base(ow)
        { 
            this.bodyJson = this.oracle.oracleJson.body;

            UnityEngine.Random.State state = UnityEngine.Random.state;
            UnityEngine.Random.InitState(10544);
            this.totalSprites = 0;
            this.armJointGraphics = new OracleGraphics.ArmJointGraphics[this.oracle.arm.joints.Length];
            for (int i = 0; i < this.oracle.arm.joints.Length; i++)
            {
                this.armJointGraphics[i] = new CMOracleGraphics.ArmJointGraphics(this, this.oracle.arm.joints[i], this.totalSprites);
                this.totalSprites += this.armJointGraphics[i].totalSprites;
            }

            this.firstUmbilicalSprite = this.totalSprites;
            this.umbCord = new OracleGraphics.UbilicalCord(this, this.totalSprites);
            this.totalSprites += this.umbCord.totalSprites;

            this.firstBodyChunkSprite = this.totalSprites;
            this.totalSprites += 2;
            this.neckSprite = this.totalSprites;
            this.totalSprites++;
            this.firstFootSprite = this.totalSprites;
            this.totalSprites += 4;

            if (this.bodyJson.halo != null)
            {
                this.halo = new OracleGraphics.Halo(this, this.totalSprites);
                this.totalSprites += this.halo.totalSprites;
            }
            else
            {
                this.halo = null;
            }
           // this.halo = null;// new OracleGraphics.Halo(this, this.totalSprites);

            if (this.bodyJson.gown != null)
            {
                this.gown = new OracleGraphics.Gown(this);
                this.robeSprite = this.totalSprites;
                this.totalSprites++;
            }
            else
            {
                this.gown = null;
            }

            this.firstHandSprite = this.totalSprites;
            this.totalSprites += 4;
            this.head = new GenericBodyPart(this, 5f, 0.5f, 0.995f, this.oracle.firstChunk);
            this.firstHeadSprite = this.totalSprites;
            this.totalSprites += 10;
            this.fadeSprite = this.totalSprites;
            this.totalSprites++;

            if (this.bodyJson.sigil != null)
            {
                this.sigilSprite = this.totalSprites;
                this.totalSprites++;
            }
            

            this.hands = new GenericBodyPart[2];
            for (int i = 0; i < 2; i++)
            {
                this.hands[i] = new GenericBodyPart(this, 2f, 0.5f, 0.98f, this.oracle.firstChunk);
            }
            this.feet = new GenericBodyPart[2];
            for (int i = 0; i < 2; i++)
            {
                this.feet[i] = new GenericBodyPart(this, 2f, 0.5f, 0.98f, this.oracle.firstChunk);
            }

            this.knees = new Vector2[2, 2];
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    this.knees[i, j] = this.oracle.firstChunk.pos;
                }
            }

            this.firstArmBaseSprite = this.totalSprites;
            this.armBase = new OracleGraphics.ArmBase(this, this.firstArmBaseSprite);
            staticCheckArmBase = this.armBase;
            this.totalSprites += this.armBase.totalSprites;
            this.voiceFreqSamples = new float[64];
            this.eyesOpen = 1f;
            UnityEngine.Random.state = state;


        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);

            OracleJSON.SpriteDataJson defaultBodySpriteData = this.bodyJson.oracleColor;
            for (int j = 0; j < base.owner.bodyChunks.Length; j++)
            {
                this.ApplySpriteColor(sLeaser.sprites[this.firstBodyChunkSprite + j], this.bodyJson.oracleColor, defaultBodySpriteData);
            }
            this.ApplySpriteColor(sLeaser.sprites[this.neckSprite], this.bodyJson.neck, defaultBodySpriteData);
            this.ApplySpriteColor(sLeaser.sprites[this.HeadSprite], this.bodyJson.head, defaultBodySpriteData);
            this.ApplySpriteColor(sLeaser.sprites[this.ChinSprite], this.bodyJson.chin, defaultBodySpriteData);

            for (int k = 0; k < 2; k++)
            {
                if (this.armJointGraphics.Length == 0)
                {
                    sLeaser.sprites[this.PhoneSprite(k, 0)].color = this.GenericJointBaseColor();
                    sLeaser.sprites[this.PhoneSprite(k, 1)].color = this.GenericJointHighLightColor();
                    sLeaser.sprites[this.PhoneSprite(k, 2)].color = this.GenericJointHighLightColor();
                }
                else
                {
                    sLeaser.sprites[this.PhoneSprite(k, 0)].color = this.armJointGraphics[0].BaseColor(default(Vector2));
                    sLeaser.sprites[this.PhoneSprite(k, 1)].color = this.armJointGraphics[0].HighLightColor(default(Vector2));
                    sLeaser.sprites[this.PhoneSprite(k, 2)].color = this.armJointGraphics[0].HighLightColor(default(Vector2));
                }
                if (this.gown != null && this.bodyJson.hands == null)
                {
                    for (int l = 0; l < 7; l++)
                    {
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4] = this.gown.Color(0.4f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4].a = this.bodyJson.gown.color.a;
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 1] = this.gown.Color(0f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 1].a = this.bodyJson.gown.color.a;
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 2] = this.gown.Color(0.4f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 2].a = this.bodyJson.gown.color.a;
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 3] = this.gown.Color(0f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 3].a = this.bodyJson.gown.color.a;
                    }
                }
                else
                {
                    this.ApplySpriteColor(sLeaser.sprites[this.HandSprite(k, 0)], this.bodyJson.hands, defaultBodySpriteData);
                    this.ApplySpriteColor(sLeaser.sprites[this.HandSprite(k, 1)], this.bodyJson.hands, defaultBodySpriteData);
                }
                this.ApplySpriteColor(sLeaser.sprites[this.FootSprite(k, 0)], this.bodyJson.feet, defaultBodySpriteData);
                this.ApplySpriteColor(sLeaser.sprites[this.FootSprite(k, 1)], this.bodyJson.feet, defaultBodySpriteData);

                sLeaser.sprites[this.EyeSprite(k)].color = (this.bodyJson.eyes != null) ? this.bodyJson.eyes.color : new Color(0f, 0f, 0f);


            }
            


            if (this.umbCord != null)
            {
                this.umbCord.ApplyPalette(sLeaser, rCam, palette);
                sLeaser.sprites[this.firstUmbilicalSprite].color = palette.blackColor;
            }
            if (this.armBase != null)
            {
                this.armBase.ApplyPalette(sLeaser, rCam, palette);
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[this.totalSprites];
            for (int i = 0; i < base.owner.bodyChunks.Length; i++)
            {
                sLeaser.sprites[this.firstBodyChunkSprite + i] = new FSprite("Circle20", true);
                sLeaser.sprites[this.firstBodyChunkSprite + i].scale = base.owner.bodyChunks[i].rad / 10f;
               // sLeaser.sprites[this.firstBodyChunkSprite + i].color = new Color(1f, (i == 0) ? 0.5f : 0f, (i == 0) ? 0.5f : 0f);
                
            }
            for (int j = 0; j < this.armJointGraphics.Length; j++)
            {
                this.armJointGraphics[j].InitiateSprites(sLeaser, rCam);
            }
            if (this.gown != null)
            {
                this.gown.InitiateSprite(this.robeSprite, sLeaser, rCam);
            }
            if (this.halo != null)
            {
                this.halo.InitiateSprites(sLeaser, rCam);
            }
            if (this.armBase != null)
            {
                this.armBase.InitiateSprites(sLeaser, rCam);
            }
            sLeaser.sprites[this.neckSprite] = new FSprite((this.bodyJson.neck?.sprite != null) ? this.bodyJson.neck.sprite : "pixel", true);
            sLeaser.sprites[this.neckSprite].scaleX = 3f;
            sLeaser.sprites[this.neckSprite].anchorY = 0f;
            sLeaser.sprites[this.HeadSprite] = new FSprite((this.bodyJson.head?.sprite != null) ? this.bodyJson.head.sprite : "Circle20", true);
            sLeaser.sprites[this.ChinSprite] = new FSprite((this.bodyJson.chin?.sprite != null) ? this.bodyJson.chin.sprite : "Circle20", true);
            for (int k = 0; k < 2; k++)
            {
                sLeaser.sprites[this.EyeSprite(k)] = new FSprite((this.bodyJson.eyes?.sprite != null) ? this.bodyJson.eyes.sprite : "pixel", true);
                sLeaser.sprites[this.EyeSprite(k)].color = (this.bodyJson.eyes != null) ? this.bodyJson.eyes.color : new Color(0.02f, 0f, 0f);

                sLeaser.sprites[this.PhoneSprite(k, 0)] = new FSprite("Circle20", true);
                sLeaser.sprites[this.PhoneSprite(k, 1)] = new FSprite("Circle20", true);
                sLeaser.sprites[this.PhoneSprite(k, 2)] = new FSprite("LizardScaleA1", true);
                sLeaser.sprites[this.PhoneSprite(k, 2)].anchorY = 0f;
                sLeaser.sprites[this.PhoneSprite(k, 2)].scaleY = 0.8f;
                sLeaser.sprites[this.PhoneSprite(k, 2)].scaleX = ((k == 0) ? -1f : 1f) * 0.75f;
                sLeaser.sprites[this.HandSprite(k, 0)] = new FSprite("haloGlyph-1", true);
                sLeaser.sprites[this.HandSprite(k, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
                sLeaser.sprites[this.FootSprite(k, 0)] = new FSprite("haloGlyph-1", true);
                sLeaser.sprites[this.FootSprite(k, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
            }

            if (this.bodyJson.sigil?.sprite != null)
            {
                IteratorKit.Logger.LogWarning("loading sigil sprite");
                sLeaser.sprites[this.sigilSprite] = new FSprite((this.bodyJson.sigil.sprite != null) ? this.bodyJson.sigil.sprite : "MoonSigil", true); // sigil
                sLeaser.sprites[this.sigilSprite].color = this.bodyJson.sigil.color;
            }
            

            if (this.umbCord != null)
            {
                this.umbCord.InitiateSprites(sLeaser, rCam);
            }
            else if (this.discUmbCord != null)
            {
                this.discUmbCord.InitiateSprites(sLeaser, rCam);
            }

            sLeaser.sprites[this.HeadSprite].scaleX = this.head.rad / 9f;
            sLeaser.sprites[this.HeadSprite].scaleY = this.head.rad / 11f;
            sLeaser.sprites[this.ChinSprite].scale = this.head.rad / 15f;
            sLeaser.sprites[this.fadeSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.fadeSprite].scale = 12.5f;
            sLeaser.sprites[this.fadeSprite].color = new Color(0f, 0f, 0f);
            sLeaser.sprites[this.fadeSprite].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
            sLeaser.sprites[this.fadeSprite].alpha = 0.2f;

            sLeaser.sprites[this.killSprite] = new FSprite("Futile_White", true);
            sLeaser.sprites[this.killSprite].shader = rCam.game.rainWorld.Shaders["FlatLight"];

            this.AddToContainer(sLeaser, rCam, null);
            
        }

        public override void Update()
        {
            Room tmpRoom = this.oracle.room;

            this.oracle.room = null;// hide so base.Update() doesnt do anything aside from calling base.Update(), this has a side effect of that base.Update not having access to oracle.room but I dont think it uses it
            base.Update();
            this.oracle.room = tmpRoom;

            if (this.oracle == null || this.oracle.room == null)
            {
                return;
            }

            this.breathe += 1f / Mathf.Lerp(10f, 60f, this.oracle.health);
            this.lastBreatheFac = this.breathFac;
            this.breathFac = Mathf.Lerp(0.5f + 0.5f * Mathf.Sin(this.breathe * 3.1415927f * 2f), 1f, Mathf.Pow(this.oracle.health, 2f));

            if (this.gown != null)
            {
                this.gown.Update();
            }
            if (this.halo != null)
            {
                this.halo.Update();
            }

            if (this.armBase != null)
            {
                this.armBase.Update();
            }
            // may want to add flag?
            this.lastLookDir = this.lookDir;
            if (this.oracle.Consious)
            {
                Vector2 tmpVector2 = Vector2.ClampMagnitude(this.oracle.oracleBehavior.lookPoint - this.oracle.firstChunk.pos, 100f) / 100f;
                this.lookDir = Vector2.ClampMagnitude(tmpVector2 + this.randomTalkVector * this.averageVoice * 0.3f, 1f);

            }

            this.head.Update();
            this.head.ConnectToPoint(this.oracle.firstChunk.pos + Custom.DirVec(this.oracle.bodyChunks[1].pos, this.oracle.firstChunk.pos) * 6f, 8f, true, 0f, this.oracle.firstChunk.vel, 0.5f, 0.01f);
            var torso = this.oracle.bodyChunks[1]; // is torso-ish i guess

            if (this.oracle.Consious)
            {
                this.head.vel += Custom.DirVec(torso.pos, this.oracle.firstChunk.pos) * this.breathFac;
                this.head.vel += this.lookDir * 0.5f * this.breathFac;
            }
            else
            {
                this.head.vel += Custom.DirVec(torso.pos, this.oracle.firstChunk.pos) * 0.75f;
                this.head.vel.y = this.head.vel.y - 0.7f;
            }

            for (int i = 0; i < 2; i++)
            {
                var foot = this.feet[i];
                foot.Update();
                foot.ConnectToPoint(torso.pos, 10f, false, 0f, torso.vel, 0.3f, 0.01f);
                foot.vel += Custom.DirVec(this.oracle.firstChunk.pos, torso.pos) * 0.3f;
                foot.vel += Custom.PerpendicularVector(Custom.DirVec(this.oracle.firstChunk.pos, torso.pos)) * 0.15f * ((i == 0) ? -1f : 1f);

                var hand = this.hands[i];
                hand.Update();
                hand.ConnectToPoint(this.oracle.firstChunk.pos, 15f, false, 0f, this.oracle.firstChunk.vel, 0.3f, 0.01f);
                hand.vel.y = hand.vel.y - 0.5f;

                hand.vel += Custom.DirVec(this.oracle.firstChunk.pos, torso.pos) * 0.3f;
                hand.vel += Custom.PerpendicularVector(Custom.DirVec(this.oracle.firstChunk.pos, torso.pos)) * 0.3f * ((i == 0) ? -1f : 1f);
                this.knees[i, 1] = this.knees[i, 0];

                hand.vel += this.randomTalkVector * this.averageVoice * 0.8f;
                this.knees[i, 1] = this.knees[i, 0];
                this.knees[i, 0] = (foot.pos + torso.pos) / 2f + 
                Custom.PerpendicularVector(Custom.DirVec(this.oracle.firstChunk.pos, torso.pos)) * 4f * ((i == 0) ? -1f : 1f);
                // after end of big if block

                for (int j = 0; j < this.armJointGraphics.Length; j++)
                {
                    this.armJointGraphics[j].Update();
                }
                if (this.umbCord != null)
                {
                    this.umbCord.Update();
                }

                // voice?
            }

        }

        public FSprite ApplySpriteColor(FSprite sprite, OracleJSON.SpriteDataJson spriteData, OracleJSON.SpriteDataJson defaultSpriteData = null)
        {
            if (spriteData == null && defaultSpriteData == null)
            {
                IteratorKit.Logger.LogWarning("apply sprite given null data");
                return sprite;
            }else if (spriteData == null)
            {
                spriteData = defaultSpriteData; // use default instead
            }

            sprite.color = spriteData.color;
            if (spriteData.shader != null)
            {
                if (this.oracle.room.game.rainWorld.Shaders.TryGetValue(spriteData.shader, out FShader shader))
                {
                    IteratorKit.Logger.LogInfo($"Applying shader {spriteData.shader}");
                    sprite.shader = shader;
                }
                else
                {
                    IteratorKit.Logger.LogError($"cannot get shader named {spriteData.shader}");
                }
            }
            return sprite;
            
            
            
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            //this.armBase.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            // this function lets the orig draw sprites function do its thing, then we fix its issues here
            // sLeaser.sprites[this.killSprite].isVisible = false;
            Vector2 sunSpritePos = new Vector2(sLeaser.sprites[this.firstHeadSprite].x, sLeaser.sprites[this.firstHeadSprite].y);


            Vector2 bodyVector = Vector2.Lerp(base.owner.firstChunk.lastPos, base.owner.firstChunk.pos, timeStacker);
            Vector2 headVector = Vector2.Lerp(this.head.lastPos, this.head.pos, timeStacker);
            Vector2 vector6 = Custom.DirVec(headVector, bodyVector); // subtracts both vectors?
            Vector2 vector7 = Custom.PerpendicularVector(vector6); // flips vector across-wise (horizontally?)
            Vector2 lookVector = this.RelativeLookDir(timeStacker); // direction oracle is looking

            if (this.bodyJson.sigil != null)
            {
                Vector2 sunVector = headVector + vector7 * lookVector.x * 2.5f + vector6 * (-2f - lookVector.y * 1.5f);
                sLeaser.sprites[this.sigilSprite].x = sunVector.x - camPos.x;
                sLeaser.sprites[this.sigilSprite].y = sunVector.y - camPos.y;
                sLeaser.sprites[this.sigilSprite].rotation = Custom.AimFromOneVectorToAnother(sunVector, headVector - vector6 * 10f);
                sLeaser.sprites[this.sigilSprite].scaleX = Mathf.Lerp(0.2f, 0.45f, Mathf.Abs(lookVector.x));//Custom.LerpMap(lookVector.x - 0.5f, 0.8f, 0f, 0.4f, 0f);
                sLeaser.sprites[this.sigilSprite].scaleY = Custom.LerpMap(lookVector.y, 0f, 1, 0.4f, 0.1f);

            }
            // moon sigil graphic scale y works because it is above the zero point of the lookVector.y (aka middle of oracle head)
            // we offset the scaleY calcs by 1f so we get what we want

           // sLeaser.sprites[this.sunFinL].x = (sunVector.x - camPos.x);
           // sLeaser.sprites[this.sunFinL].y = (sunVector.y - camPos.y) + 1f;
           // sLeaser.sprites[this.sunFinL].rotation = Custom.AimFromOneVectorToAnother(sunVector, headVector - vector6 * 10f);
           //// sLeaser.sprites[this.sunFinL].scaleX = Custom.LerpMap(lookVector.x + 0.5f, 0.8f, 0f, 0.4f, 0f);
           // //TestMod.Logger.LogWarning(lookVector.x);
           //// sLeaser.sprites[this.sunFinL].scaleY = Custom.LerpMap(lookVector.y + 0.2f, 0.8f, 0f, 1f, 0f); 

           // sLeaser.sprites[this.sunFinR].x = (sunVector.x - camPos.x);
           // sLeaser.sprites[this.sunFinR].y = (sunVector.y - camPos.y) + 1f;
           // sLeaser.sprites[this.sunFinR].rotation = Custom.AimFromOneVectorToAnother(sunVector, headVector - vector6 * 10f);
           // sLeaser.sprites[this.sunFinR].scaleX = -Mathf.Lerp(1f, 0f, lookVector.x);
           // float scaleY = Mathf.Lerp(0f, 1f, lookVector.y + 0.5f);
           // sLeaser.sprites[this.sunFinR].scaleY = (scaleY >= 0f) ? scaleY : 0f;
           // TestMod.Logger.LogWarning(lookVector.y);
           // TestMod.Logger.LogWarning(scaleY);

            // looking up
            // 1f = full scale 1f
            // 0.9f = 

        }

        public class CMGown
        {
            public static Color CMColor(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f)
            {
                Color origRes = orig(self, f);
                //
                if (self.owner.oracle is CMOracle) {
                    try
                    {
                        CMOracle cmOracle = (CMOracle)self.owner.oracle;
                        OracleJSON.OracleBodyJson.OracleGownJson.OracleGownColorDataJson gownColor = cmOracle.oracleJson?.body?.gown?.color;
                        if (gownColor == null)
                        {
                            return origRes;
                        }

                        if (gownColor.type == "gradient")
                        {
                            return Custom.HSL2RGB(
                                Mathf.Lerp(gownColor.from.h / 360, gownColor.to.h / 360, Mathf.Pow(f, 2)),
                                Mathf.Lerp(gownColor.from.s / 100, gownColor.to.s / 100, f), 
                                Mathf.Lerp(gownColor.from.l / 100, gownColor.to.l / 100, f)
                            );
                        } 
                        else
                        { // gown type == "solid"
                            return new Color(gownColor.r / 255, gownColor.g / 255, gownColor.b / 255, gownColor.a / 255);
                        }

                    }
                    catch (InvalidCastException e)
                    {
                        IteratorKit.Logger.LogError(e.Message);
                        return origRes;
                    }
                    
                }
                else
                {
                    return origRes;
                }
                
            }

        }

        public static void HaloInitSprites(On.OracleGraphics.Halo.orig_InitiateSprites orig, OracleGraphics.Halo self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (self.owner is CMOracleGraphics)
            {
                CMOracleGraphics oracleGraphics = (CMOracleGraphics)self.owner;
                OracleJSON.OracleBodyJson.OracleHaloJson haloJson = oracleGraphics.bodyJson.halo;
                if (haloJson == null)
                {
                    return;
                }
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[self.firstSprite + i] = new FSprite(haloJson.innerRing?.sprite ?? "Futile_White", true);
                    sLeaser.sprites[self.firstSprite + i].shader = rCam.game.rainWorld.Shaders["VectorCircle"];
                    sLeaser.sprites[self.firstSprite + i].color = haloJson.innerRing?.color ?? new Color(0, 0, 0);
                }
                for (int i = 0; i < self.connections.Length; i++)
                {
                    sLeaser.sprites[self.firstSprite + 2 + i] = TriangleMesh.MakeLongMesh(20, false, false);
                    sLeaser.sprites[self.firstSprite + 2 + i].color = haloJson.sparks?.color ?? new Color(0f, 0f, 0f);
                }
                for (int i = 0; i < 100; i++)
                {
                    sLeaser.sprites[self.firstBitSprite + i] = new FSprite(haloJson.outerRing?.sprite ?? "pixel", true);
                    sLeaser.sprites[self.firstBitSprite + i].scaleX = (haloJson.outerRing?.scaleX ?? -1f) > 0 ? haloJson.outerRing.scaleX : 4f;
                    sLeaser.sprites[self.firstBitSprite + i].scaleY = (haloJson.outerRing?.scaleY ?? -1f) > 0 ? haloJson.outerRing.scaleY : 4f;
                    sLeaser.sprites[self.firstBitSprite + i].color = haloJson.outerRing?.color ?? new Color(0f, 0f, 0f);
                }
            }
            else
            {
                orig(self, sLeaser, rCam);
            }
        }

        public static Color BaseColor(On.OracleGraphics.ArmJointGraphics.orig_BaseColor orig, ArmJointGraphics self, Vector2 ps)
        {
            if (self.owner is CMOracleGraphics)
            {
                CMOracleGraphics cMOracleGraphics = self.owner as CMOracleGraphics;
                if (cMOracleGraphics.bodyJson.arm != null)
                {
                    return cMOracleGraphics.bodyJson.arm.armColor.color;
                }
                
            }
            return orig(self, ps);
        }

        internal static Color HighlightColor(On.OracleGraphics.ArmJointGraphics.orig_HighLightColor orig, ArmJointGraphics self, Vector2 ps)
        {
            if (self.owner is CMOracleGraphics)
            {
                CMOracleGraphics cMOracleGraphics = self.owner as CMOracleGraphics;
                if (cMOracleGraphics.bodyJson.arm != null)
                {
                    return cMOracleGraphics.bodyJson.arm.armHighlight.color;
                }

            }
            return orig(self, ps);
        }
    }

    
}
