using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}
