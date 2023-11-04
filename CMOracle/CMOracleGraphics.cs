using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using IteratorMod.CM_Oracle;
using System.Runtime.CompilerServices;

namespace IteratorMod.SRS_Oracle
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

            this.halo = null;// new OracleGraphics.Halo(this, this.totalSprites);

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
            this.SLArmBaseColA = new Color(0.52156866f, 0.52156866f, 0.5137255f);
            this.SLArmHighLightColA = new Color(0.5686275f, 0.5686275f, 0.54901963f);
            this.SLArmBaseColB = palette.texture.GetPixel(5, 1);
            this.SLArmHighLightColB = palette.texture.GetPixel(5, 2);

            Color oracleColor = this.bodyJson?.oracleColor?.color ?? new Color(1f, 0f, 0f);// this.bodyJson.oracleColor.color;
            for (int j = 0; j < base.owner.bodyChunks.Length; j++)
            {
                sLeaser.sprites[this.firstBodyChunkSprite + j].color = (this.bodyJson.torso != null) ? this.bodyJson.torso.color : oracleColor;
            }
            sLeaser.sprites[this.neckSprite].color = (this.bodyJson.neck != null) ? this.bodyJson.neck.color : oracleColor;
            sLeaser.sprites[this.HeadSprite].color = (this.bodyJson.head != null) ? this.bodyJson.head.color : oracleColor;
            sLeaser.sprites[this.ChinSprite].color = (this.bodyJson.chin != null) ? this.bodyJson.chin.color : oracleColor;

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
                sLeaser.sprites[this.HandSprite(k, 0)].color = oracleColor;
                if (this.gown != null)
                {
                    for (int l = 0; l < 7; l++)
                    {
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4] = this.gown.Color(0.4f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 1] = this.gown.Color(0f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 2] = this.gown.Color(0.4f);
                        (sLeaser.sprites[this.HandSprite(k, 1)] as TriangleMesh).verticeColors[l * 4 + 3] = this.gown.Color(0f);
                    }
                }
                else
                {
                    sLeaser.sprites[this.HandSprite(k, 1)].color = (this.bodyJson.hands != null) ? this.bodyJson.hands.color : oracleColor;
                }
                sLeaser.sprites[this.FootSprite(k, 0)].color = (this.bodyJson.feet != null) ? this.bodyJson.feet.color : oracleColor;
                sLeaser.sprites[this.FootSprite(k, 1)].color = (this.bodyJson.feet != null) ? this.bodyJson.feet.color : oracleColor;

                sLeaser.sprites[this.EyeSprite(k)].color = (this.bodyJson.eyes != null) ? this.bodyJson.eyes.color : new Color(0f, 0f, 0f);


            }


            if (this.bodyJson.sigil != null)
            {
                sLeaser.sprites[this.sigilSprite].color = new Color(0.92f, 0.25f, 0.20f);
            }
            

            sLeaser.sprites[this.sunFinL].color = new Color(1f, 0f, 0f);
            sLeaser.sprites[this.sunFinR].color = new Color(1f, 0f, 0f);

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
           // Futile.atlasManager.LogAllElementNames();
            sLeaser.sprites = new FSprite[this.totalSprites];
            for (int i = 0; i < base.owner.bodyChunks.Length; i++)
            {
                sLeaser.sprites[this.firstBodyChunkSprite + i] = new FSprite("Circle20", true);
                sLeaser.sprites[this.firstBodyChunkSprite + i].scale = base.owner.bodyChunks[i].rad / 10f;
                sLeaser.sprites[this.firstBodyChunkSprite + i].color = new Color(1f, (i == 0) ? 0.5f : 0f, (i == 0) ? 0.5f : 0f);
                
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

            if (this.bodyJson.sigil != null)
            {
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
                if (this.oracle.oracleBehavior.player != null && i == 0 && false)
                {
                    // <--- hand towards player stuff goes here! must also fix above cond.

                }
                this.knees[i, 1] = this.knees[i, 0];
                this.knees[i, 0] = (foot.pos + torso.pos) / 2f + 
                Custom.PerpendicularVector(Custom.DirVec(this.oracle.firstChunk.pos, torso.pos)) * 4f * ((i == 0) ? -1f : 1f);
               // TestMod.LogVector2(this.knees[i, 0]);
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
                sLeaser.sprites[this.sigilSprite].scaleX = Custom.LerpMap(lookVector.x - 0.5f, 0.8f, 0f, 0.4f, 0f);
                sLeaser.sprites[this.sigilSprite].scaleY = Custom.LerpMap(lookVector.y, 0f, 0.4f, 0.4f, 0.1f);
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

        public class SRSGown
        {
            public static Color SRSColor(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f)
            {
                Color origRes = orig(self, f);
                //
                if (self.owner.oracle is CMOracle) {
                    // #4f3068
                    // 79, 48, 104
                    // 273°, 37%, 30%
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
                                Mathf.Lerp(gownColor.from.h, gownColor.to.h, Mathf.Pow(f, 2f)), 
                                Mathf.Lerp(gownColor.from.s, gownColor.to.s, f), 
                                Mathf.Lerp(gownColor.from.s, gownColor.to.s, f)
                            );
                        } 
                        else
                        { // gown type == "solid"
                            return new Color(gownColor.r, gownColor.g, gownColor.b, gownColor.a);
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
    }

    
}
