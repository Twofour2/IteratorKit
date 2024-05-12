using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;

namespace IteratorKit.CMOracle
{
    public class CMOracleGraphics : OracleGraphics
    {
        public new CMOracle oracle;
        public OracleJData.OracleBodyJData bodyJson;
        public int sigilSprite;
        private OracleJData.SpriteDataJData defaultSpriteData;
        private Dictionary<string, FShader> rwShaders;


        public CMOracleGraphics(CMOracle oracle) : base(oracle)
        {
            this.oracle = oracle;
            this.bodyJson = this.oracle.oracleJson.body;

            this.CreateSprites();
            this.voiceFreqSamples = new float[64];
            this.eyesOpen = 1f;
        }

        /// <summary>
        /// Dynamically sets the sprite index numbers for each sprite on this oracle and creates a few sub objects.
        /// </summary>
        private void CreateSprites()
        {
            UnityEngine.Random.State state = UnityEngine.Random.state; // store current random state
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
            if (this.bodyJson.gown != null)
            {
                this.gown = new CMOracleGraphics.Gown(this);
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
            this.totalSprites += this.armBase.totalSprites;
            UnityEngine.Random.state = state; // restore random state
        }

        /// <summary>
        /// Generates most of the sprites used by this model. We use CreateSprite to generate a sprite with all the data pre-applied
        /// </summary>
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[this.totalSprites];
            this.rwShaders = rCam.game.rainWorld.Shaders;
            for (int i = 0; i < this.owner.bodyChunks.Length; i++)
            {
                sLeaser.sprites[this.firstBodyChunkSprite + i] = this.CreateSprite(this.bodyJson.body, new OracleJData.SpriteDataJData()
                {
                    sprite = "Circle20",
                    scale = base.owner.bodyChunks[i].rad / 10f
                });
            }
            sLeaser.sprites[this.neckSprite] = this.CreateSprite(this.bodyJson.neck, new OracleJData.SpriteDataJData()
            {
                sprite = "pixel",
                scaleX = 3f,
                anchorX = 0f
            });
            
            sLeaser.sprites[this.HeadSprite] = this.CreateSprite(this.bodyJson.head, new OracleJData.SpriteDataJData()
            {
                sprite = "Circle20",
                scaleX = this.head.rad / 9f,
                scaleY = this.head.rad / 11f
            });
            sLeaser.sprites[this.ChinSprite] = this.CreateSprite(this.bodyJson.chin, new OracleJData.SpriteDataJData()
            {
                sprite = "Circle20",
                scale = this.head.rad / 15f
            });

            // Antenna base sprite left 0
            sLeaser.sprites[this.PhoneSprite(0, 0)] = this.CreateSprite(this.bodyJson.leftAntennaBase, new OracleJData.SpriteDataJData()
            {
                sprite = "Circle20"
            });
            // todo check what the difference between part 0 and part 1 is?
            // Antenna base sprite left 1
            sLeaser.sprites[this.PhoneSprite(0, 1)] = this.CreateSprite(this.bodyJson.leftAntennaBase, new OracleJData.SpriteDataJData()
            {
                sprite = "Circle20"
            });
            // Antenna base sprite right 0
            sLeaser.sprites[this.PhoneSprite(1, 0)] = this.CreateSprite(this.bodyJson.rightAntennaBase, new OracleJData.SpriteDataJData()
            {
                sprite = "Circle20"
            });
            // Antenna base sprite right 1
            sLeaser.sprites[this.PhoneSprite(1, 1)] = this.CreateSprite(this.bodyJson.rightAntennaBase, new OracleJData.SpriteDataJData()
            {
                sprite = "Circle20"
            });
            // Antenna length sprite left
            sLeaser.sprites[this.PhoneSprite(0, 2)] = this.CreateSprite(this.bodyJson.leftAntenna, new OracleJData.SpriteDataJData()
            {
                sprite = "LizardScaleA1",
                scaleX = -0.75f,
                scaleY = 0.8f,
            });
            // Antenna length sprite right
            sLeaser.sprites[this.PhoneSprite(1, 2)] = this.CreateSprite(this.bodyJson.leftAntenna, new OracleJData.SpriteDataJData()
            {
                sprite = "LizardScaleA1",
                scaleX = 0.75f,
                scaleY = 0.8f
            });
            for (int i = 0; i < 2; i++)
            {
                sLeaser.sprites[this.EyeSprite(i)] = this.CreateSprite(this.bodyJson.eyes, new OracleJData.SpriteDataJData()
                {
                    sprite = "pixel",
                    color = new Color(0.02f, 0f, 0f)
                });

                sLeaser.sprites[this.HandSprite(i, 0)] = this.CreateSprite(this.bodyJson.hands, new OracleJData.SpriteDataJData()
                {
                    sprite = "haloGlyph-1"
                });
                sLeaser.sprites[this.HandSprite(i, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
                this.ApplySpriteData(sLeaser.sprites[this.HandSprite(i, 1)], this.bodyJson.feet, new OracleJData.SpriteDataJData()); // apply manually
                sLeaser.sprites[this.FootSprite(i, 0)] = this.CreateSprite(this.bodyJson.feet, new OracleJData.SpriteDataJData()
                {
                    sprite = "haloGlyph-1"
                });
                sLeaser.sprites[this.FootSprite(i, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
                this.ApplySpriteData(sLeaser.sprites[this.FootSprite(i, 1)], this.bodyJson.feet, new OracleJData.SpriteDataJData()); // apply manually
            }

            if (this.bodyJson.sigil?.sprite != null)
            {
                IteratorKit.Log.LogInfo("Loading sigil sprite");
                sLeaser.sprites[this.sigilSprite] = this.CreateSprite(this.bodyJson.sigil, new OracleJData.SpriteDataJData()
                {
                    sprite = "MoonSigil",
                    color = Color.white
                });
            }

            sLeaser.sprites[this.fadeSprite] = this.CreateSprite(this.bodyJson.glowSprite, new OracleJData.SpriteDataJData()
            {
                sprite = "Futile_White",
                scale = 12.5f,
                color = new Color(255f, 255f, 255f),
                a = 0.2f,
                shader = "FlatLightBehindTerrain"
            });


            sLeaser.sprites[this.killSprite] = this.CreateSprite(this.bodyJson.killSprite, new OracleJData.SpriteDataJData()
            {
                sprite = "Futile_White",
                shader = "FlatLight"
            });

            // call to other sprite init handlers
            for (int j = 0; j < this.armJointGraphics.Length; j++)
            {
                this.armJointGraphics[j].InitiateSprites(sLeaser, rCam);
            }
            if (this.gown != null)
            {
                sLeaser.sprites[this.robeSprite] = this.CreateGownSprite(this.gown); // call our own version
            }
            if (this.halo != null)
            {
                this.CreateHaloSprites(this.halo, sLeaser, rCam); // call our own version
            }
            if (this.armBase != null)
            {
                this.armBase.InitiateSprites(sLeaser, rCam);
            }
            if (this.umbCord != null)
            {
                this.umbCord.InitiateSprites(sLeaser, rCam);
            }
            else if (this.discUmbCord != null)
            {
                this.discUmbCord.InitiateSprites(sLeaser, rCam);
            }


            this.AddToContainer(sLeaser, rCam, null);
        }

        /// <summary>
        /// Apply sprite colors, done at a later stage after InitialiseSprites()
        /// </summary>
        /// <param name="sLeaser"></param>
        /// <param name="rCam"></param>
        /// <param name="palette"></param>
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);

            this.defaultSpriteData = this.bodyJson.oracleColor;

            for (int i = 0; i < base.owner.bodyChunks.Length; i++)
            {
                this.ApplySpritePalette(sLeaser.sprites[this.firstBodyChunkSprite + i], this.bodyJson.body);
            }
            this.ApplySpritePalette(sLeaser.sprites[this.neckSprite], this.bodyJson.neck);
            this.ApplySpritePalette(sLeaser.sprites[this.HeadSprite], this.bodyJson.head);
            this.ApplySpritePalette(sLeaser.sprites[this.ChinSprite], this.bodyJson.chin);

            for (int i = 0; i < 2; i++)
            {
                if (this.armJointGraphics.Length == 0)
                {
                    sLeaser.sprites[this.PhoneSprite(i, 0)].color = this.GenericJointBaseColor();
                    sLeaser.sprites[this.PhoneSprite(i, 1)].color = this.GenericJointHighLightColor();
                    sLeaser.sprites[this.PhoneSprite(i, 2)].color = this.GenericJointHighLightColor();
                }
                else
                {
                    sLeaser.sprites[this.PhoneSprite(i, 0)].color = this.armJointGraphics[0].BaseColor(default(Vector2));
                    sLeaser.sprites[this.PhoneSprite(i, 1)].color = this.armJointGraphics[0].HighLightColor(default(Vector2));
                    sLeaser.sprites[this.PhoneSprite(i, 2)].color = this.armJointGraphics[0].HighLightColor(default(Vector2));
                }
                if (this.gown != null && this.bodyJson.hands == null)
                {
                    for (int l = 0; l < 7; l++)
                    {
                        (sLeaser.sprites[this.HandSprite(i, 1)] as TriangleMesh).verticeColors[l * 4] = this.gown.Color(0.4f);
                        (sLeaser.sprites[this.HandSprite(i, 1)] as TriangleMesh).verticeColors[l * 4].a = this.bodyJson.gown.color.a;
                        (sLeaser.sprites[this.HandSprite(i, 1)] as TriangleMesh).verticeColors[l * 4 + 1] = this.gown.Color(0f);
                        (sLeaser.sprites[this.HandSprite(i, 1)] as TriangleMesh).verticeColors[l * 4 + 1].a = this.bodyJson.gown.color.a;
                        (sLeaser.sprites[this.HandSprite(i, 1)] as TriangleMesh).verticeColors[l * 4 + 2] = this.gown.Color(0.4f);
                        (sLeaser.sprites[this.HandSprite(i, 1)] as TriangleMesh).verticeColors[l * 4 + 2].a = this.bodyJson.gown.color.a;
                        (sLeaser.sprites[this.HandSprite(i, 1)] as TriangleMesh).verticeColors[l * 4 + 3] = this.gown.Color(0f);
                        (sLeaser.sprites[this.HandSprite(i, 1)] as TriangleMesh).verticeColors[l * 4 + 3].a = this.bodyJson.gown.color.a;
                    }
                }
                else
                {
                    this.ApplySpritePalette(sLeaser.sprites[this.HandSprite(i, 0)], this.bodyJson.hands);
                    this.ApplySpritePalette(sLeaser.sprites[this.HandSprite(i, 1)], this.bodyJson.hands);
                }
                this.ApplySpritePalette(sLeaser.sprites[this.FootSprite(i, 0)], this.bodyJson.feet);
                this.ApplySpritePalette(sLeaser.sprites[this.FootSprite(i, 1)], this.bodyJson.feet);

                sLeaser.sprites[this.EyeSprite(i)].color = (this.bodyJson.eyes != null) ? this.bodyJson.eyes.color : new Color(0f, 0f, 0f);
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

        /// <summary>
        /// Generates a sprite with pre-applied user sprite data
        /// </summary>
        /// <param name="spriteData">User provided sprite data</param>
        /// <param name="defaultSpriteData">Defaults for this sprite to match regular rainworld code</param>
        /// <returns></returns>
        public FSprite CreateSprite(OracleJData.SpriteDataJData spriteData, OracleJData.SpriteDataJData defaultSpriteData)
        {
            if (spriteData == null)
            {
                spriteData = defaultSpriteData;
            }
            FSprite fSprite = new FSprite(spriteData?.sprite ?? defaultSpriteData.sprite, true);
            fSprite = this.ApplySpriteData(fSprite, spriteData, defaultSpriteData);
            return fSprite;
        }

        /// <summary>
        /// Applies user sprite data to sprite
        /// </summary>
        /// <param name="fSprite">Sprite</param>
        /// <param name="spriteData">User provided sprite data</param>
        /// <param name="defaultSpriteData">Defaults for this sprite to match regular rainworld code</param>
        /// <returns></returns>
        public FSprite ApplySpriteData(FSprite fSprite, OracleJData.SpriteDataJData spriteData, OracleJData.SpriteDataJData defaultSpriteData)
        {
            if (spriteData == null)
            {
                spriteData = defaultSpriteData;
            }
            if (spriteData.scale != 0f)
            {
                fSprite.scale = spriteData.scale;
            }else if (defaultSpriteData.scale != 0f)
            {
                fSprite.scale = defaultSpriteData.scale;
            }
            if (spriteData.scaleX != 0f || defaultSpriteData.scaleX != 0f)
            {
                fSprite.scaleX = (spriteData.scaleX != 0f) ? spriteData.scaleX : defaultSpriteData.scaleX;
            }
            if (spriteData.scaleY != 0f || defaultSpriteData.scaleY != 0f)
            {
                fSprite.scaleY = (spriteData.scaleY != 0f) ? spriteData.scaleY : defaultSpriteData.scaleY;
            }
            if (spriteData.anchorX != 0f || defaultSpriteData.anchorX != 0f)
            {
                fSprite.anchorX = (spriteData.anchorX != 0f) ? spriteData.anchorX : defaultSpriteData.anchorX;
            }
            if (spriteData.anchorY != 0f || defaultSpriteData.anchorY != 0f)
            {
                fSprite.anchorY = (spriteData.anchorY != 0f) ? spriteData.anchorY : defaultSpriteData.anchorY;
            }
           
            fSprite.color = (spriteData.color != new Color()) ? spriteData.color : defaultSpriteData.color;
            fSprite.alpha = (spriteData.a != null) ? spriteData.a.Value : ((defaultSpriteData.a != null) ? defaultSpriteData.a.Value : 255f);

            
            FShader fShader = null;
            if (spriteData.shader != null)
            {
                this.rwShaders.TryGetValue(spriteData.shader ?? "", out fShader);
            }
            if (fShader == null)
            {
                this.rwShaders.TryGetValue(defaultSpriteData.shader ?? "", out fShader);
            }
            if (fShader != null)
            {
                fSprite.shader = fShader;
            }
            

            return fSprite;
        }

        /// <summary>
        /// Applies the sprite palette data. This is handled as a seperate step done after the sprites have been created in InitiateSprites()
        /// Here instead of having a default per each sprite we fall back to just the same default user supplied colors.
        /// </summary>
        /// <param name="sprite">Sprite</param>
        /// <param name="spriteData">User sprite data</param>
        /// <returns></returns>
        public FSprite ApplySpritePalette(FSprite sprite, OracleJData.SpriteDataJData spriteData)
        {
            if (spriteData == null && defaultSpriteData == null)
            {
                IteratorKit.Log.LogWarning("apply sprite given null data");
                return sprite;
            }
            else if (spriteData == null)
            {
                spriteData = defaultSpriteData; // use default instead
            }
            sprite.color = spriteData.color;
            sprite.alpha = (spriteData.a != null) ? spriteData.a.Value : 255f;
            if (spriteData.shader != null)
            {
                if (this.oracle.room.game.rainWorld.Shaders.TryGetValue(spriteData.shader, out FShader shader))
                {
                    IteratorKit.Log.LogInfo($"Applying shader {spriteData.shader}");
                    sprite.shader = shader;
                }
                else
                {
                    IteratorKit.Log.LogError($"cannot get shader named {spriteData.shader}");
                }
            }
            return sprite;
        }

        /// <summary>
        /// Split out version of Gown.InitiateSprite
        /// </summary>
        /// <param name="gown">CMOracleGraphics.gown</param>
        /// <returns>Triangle mesh to assign to sLeaser</returns>
        public TriangleMesh CreateGownSprite(Gown gown)
        {
            IteratorKit.Log.LogWarning(this.bodyJson.gown.sprite);

            TriangleMesh gownMesh = TriangleMesh.MakeGridMesh(this.bodyJson.gown?.sprite ?? "Futile_White", gown.divs - 1);
            for (int i = 0; i < gown.divs; i++)
            {
                for (int j = 0; j < gown.divs; j++)
                {
                    gownMesh.verticeColors[j * gown.divs + i] = gown.Color((float)i / (float)gown.divs - 1);
                }
            }
            return gownMesh;
        }

        /// <summary>
        /// Builds color data for oracle gown (robe)
        /// </summary>
        public static Color GownColor(On.OracleGraphics.Gown.orig_Color orig, OracleGraphics.Gown self, float f)
        {
            if (self.owner is not CMOracleGraphics)
            {
                return orig(self, f);
            }
            OracleJData.OracleBodyJData.OracleGownJData.OracleGownColorDataJData gownColorJData = self.owner.oracle.OracleData()?.oracleJson?.body?.gown?.color;
            if (gownColorJData == null)
            {
                IteratorKit.Log.LogInfo("Using default gown");
                return orig(self, f);
            }

            if (gownColorJData.type == "gradient")
            {
                return Custom.HSL2RGB(
                    Mathf.Lerp(gownColorJData.from.h / 360, gownColorJData.to.h / 360, Mathf.Pow(f, 2)),
                    Mathf.Lerp(gownColorJData.from.s / 100, gownColorJData.to.s / 100, f),
                    Mathf.Lerp(gownColorJData.from.l / 100, gownColorJData.to.l / 100, f)
                );
            }
            else
            {// assume gown type == "solid"
                return new Color(gownColorJData.r / 255f, gownColorJData.g / 255f, gownColorJData.b / 255f, gownColorJData.a / 255f);
            }
        }

        /// <summary>
        /// Split out version of Halo.InitateSprites
        /// </summary>
        /// <param name="halo">Brand new copy of Halo 3</param>
        /// <param name="sLeaser">Sprite Leaser</param>
        /// <param name="rCam">Room camera</param>
        public void CreateHaloSprites(Halo halo, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            OracleJData.OracleBodyJData.OracleHaloJData haloJData = this.bodyJson.halo;
            if (halo == null) // supposedly cannot occur
            {
                return;
            }
            for (int i = 0; i < 2; i++)
            {
                FSprite innerRingSprite = new FSprite(haloJData.innerRing?.sprite ?? "Futile_White", true);
                innerRingSprite.shader = rCam.game.rainWorld.Shaders["VectorCircle"];
                innerRingSprite.color = haloJData.innerRing?.color ?? new Color(0f, 0f, 0f);
                sLeaser.sprites[halo.firstSprite + i] = innerRingSprite;
            }
            for (int i = 0; i < halo.connections.Length; i++)
            {
                TriangleMesh haloSparks = TriangleMesh.MakeLongMesh(20, false, false);
                haloSparks.color = haloJData.sparks?.color ?? new Color(0f, 0f, 0f);
                sLeaser.sprites[halo.firstSprite + 2 + i] = haloSparks;
            }
            for (int i = 0; i < 100; i++)
            {
                FSprite outerRingSprite = new FSprite(haloJData.outerRing?.sprite ?? "pixel", true);
                outerRingSprite.scaleX = (haloJData.outerRing?.scaleX ?? -1f) > 0 ? haloJData.outerRing.scaleX : 4f;
                outerRingSprite.scaleY = (haloJData.outerRing?.scaleY ?? -1f) > 0 ? haloJData.outerRing.scaleY : 4f;
                outerRingSprite.color = haloJData.outerRing?.color ?? new Color(0f, 0f, 0f);
                sLeaser.sprites[halo.firstBitSprite + i] = outerRingSprite;
            }

        }

        /// <summary>
        /// Builds color data for arm base
        /// </summary>
        public static Color ArmBaseColor(On.OracleGraphics.ArmJointGraphics.orig_BaseColor orig, ArmJointGraphics self, Vector2 ps)
        {
            if (self.owner is not CMOracleGraphics)
            {
                return orig(self, ps);
            }

            if (self.owner.oracle.OracleJson()?.body?.arm?.armColor == null) {
                return orig(self, ps);
            }
            return self.owner.oracle.OracleJson().body.arm.armColor.color;
        }

        /// <summary>
        /// Build color data for arm highlight, sets what color most of the arm is
        /// </summary>
        public static Color ArmHighlightColor(On.OracleGraphics.ArmJointGraphics.orig_HighLightColor orig, ArmJointGraphics self, Vector2 ps)
        {
            if (self.owner is not CMOracleGraphics)
            {
                return orig(self, ps);
            }
            if (self.owner.oracle.OracleJson()?.body?.arm?.armHighlight == null)
            {
                return orig(self, ps);
            }
            return self.owner.oracle.OracleJson().body.arm.armHighlight.color;
        }

        public static void CMOracleGraphicsUpdate()
        {

        }

        /// <summary>
        /// handles holdKnees, which calls to this.IsMoon which we can't easily override
        /// </summary>
        public static void CMOracleGraphicsUpdate(On.OracleGraphics.orig_Update orig, OracleGraphics self)
        {
            if (self is not CMOracleGraphics) { orig(self); return; }
            CMOracleGraphics cmOracleGraphics = self as CMOracleGraphics;
            if (cmOracleGraphics.oracle.oracleBehavior is not CMOracleSitBehavior) { orig(self); return; }
            CMOracleSitBehavior cmOracleSitBehavior = cmOracleGraphics.oracle.oracleBehavior as CMOracleSitBehavior;

            // store prev values
            GenericBodyPart[] tmpHands = self.hands;
            Vector2[,] tmpKnees = self.knees;
            orig(self);
            // restore prev values
            self.hands = tmpHands;
            self.knees = tmpKnees;

            if (cmOracleSitBehavior.holdKnees)
            {
                for (int i = 0; i < 2; i++)
                {
                    GenericBodyPart foot = self.feet[i];
                    GenericBodyPart hand = self.hands[i];
                    Vector2? oracleHandTargetPos = SharedPhysics.ExactTerrainRayTracePos(
                    self.oracle.room,
                    self.oracle.firstChunk.pos,
                    self.oracle.firstChunk.pos + new Vector2((i == 0) ? -24f : -14f, -40f)
                    );
                    Vector2 oracleKneeTargetPos;
                    if (oracleHandTargetPos != null)
                    {
                        foot.vel += Vector2.ClampMagnitude(oracleHandTargetPos.Value - foot.pos, 10f) / 2f;

                        // calculate knee target position
                        oracleKneeTargetPos = foot.pos + Custom.DirVec(self.oracle.bodyChunks[1].pos, self.oracle.firstChunk.pos) * 15f;
                        oracleKneeTargetPos += Custom.DirVec(self.oracle.firstChunk.pos, oracleKneeTargetPos) * 5f;
                        oracleKneeTargetPos = Vector2.Lerp(oracleKneeTargetPos,
                            (foot.pos + self.oracle.bodyChunks[1].pos) / 2f,
                            Mathf.InverseLerp(7f, 14f, Vector2.Distance(foot.pos, self.oracle.bodyChunks[1].pos)));
                    }
                    else
                    {
                        oracleKneeTargetPos = Custom.PerpendicularVector(self.oracle.bodyChunks[1].pos, self.oracle.firstChunk.pos) * ((i == 0) ? -1f : 1f) * 5f;
                    }

                    // set the oracle knee position
                    self.knees[i, 0] = Vector2.Lerp(self.knees[i, 0], oracleKneeTargetPos, 0.4f);
                    if (!Custom.DistLess(self.knees[i, 0], oracleKneeTargetPos, 15f))
                    {
                        self.knees[i, 0] = oracleKneeTargetPos + Custom.DirVec(oracleKneeTargetPos, self.knees[i, 0]);
                    }

                    if (!(self.oracle.oracleBehavior.player != null &&
                        self.oracle.Consious &&
                        i == 0 == self.oracle.firstChunk.pos.x > self.oracle.oracleBehavior.player.DangerPos.x &&
                        Custom.DistLess(self.oracle.firstChunk.pos, self.oracle.oracleBehavior.player.DangerPos, 40f))){
                        hand.vel += Vector2.ClampMagnitude(self.knees[i, 0] - hand.pos, 3f) / 7f;
                    }


                }
                
            }
        }
    }
}
