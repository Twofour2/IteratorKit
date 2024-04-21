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
        public OracleJSON.OracleBodyJson bodyJson;
        public int sigilSprite;
        private OracleJSON.SpriteDataJson defaultSpriteData;
        private Dictionary<string, FShader> rwShaders;

        public CMOracleGraphics(CMOracle oracle) : base(oracle)
        {
            this.oracle = oracle;
            this.bodyJson = this.oracle.oracleJson.body;

            this.CreateSprites();
            this.voiceFreqSamples = new float[64];
            this.eyesOpen = 1f;
        }

        private void CreateSprites()
        {
            UnityEngine.Random.State state = UnityEngine.Random.state; // store random state
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

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[this.totalSprites];
            this.rwShaders = rCam.game.rainWorld.Shaders;
            for (int i = 0; i < this.owner.bodyChunks.Length; i++)
            {
                sLeaser.sprites[this.firstBodyChunkSprite + i] = this.CreateSprite(this.bodyJson.body, new OracleJSON.SpriteDataJson()
                {
                    sprite = "Circle20",
                    scale = this.owner.bodyChunks[i].rad / 10f
                });
            }
            sLeaser.sprites[this.neckSprite] = this.CreateSprite(this.bodyJson.neck, new OracleJSON.SpriteDataJson()
            {
                sprite = "pixel",
                scaleX = 3f,
                anchorX = 0f
            });
            
            sLeaser.sprites[this.HeadSprite] = this.CreateSprite(this.bodyJson.head, new OracleJSON.SpriteDataJson()
            {
                sprite = "Circle20",
                scaleX = this.head.rad / 9f,
                scaleY = this.head.rad / 11f
            });
            sLeaser.sprites[this.ChinSprite] = this.CreateSprite(this.bodyJson.chin, new OracleJSON.SpriteDataJson()
            {
                sprite = "Circle20",
                scale = this.head.rad / 15f
            });

            // Antenna base sprite left 0
            sLeaser.sprites[this.PhoneSprite(0, 0)] = this.CreateSprite(this.bodyJson.leftAntennaBase, new OracleJSON.SpriteDataJson()
            {
                sprite = "Circle20"
            });
            // todo check what the difference between part 0 and part 1 is?
            // Antenna base sprite left 1
            sLeaser.sprites[this.PhoneSprite(0, 1)] = this.CreateSprite(this.bodyJson.leftAntennaBase, new OracleJSON.SpriteDataJson()
            {
                sprite = "Circle20"
            });
            // Antenna base sprite right 0
            sLeaser.sprites[this.PhoneSprite(1, 0)] = this.CreateSprite(this.bodyJson.rightAntennaBase, new OracleJSON.SpriteDataJson()
            {
                sprite = "Circle20"
            });
            // Antenna base sprite right 1
            sLeaser.sprites[this.PhoneSprite(1, 1)] = this.CreateSprite(this.bodyJson.rightAntennaBase, new OracleJSON.SpriteDataJson()
            {
                sprite = "Circle20"
            });
            // Antenna length sprite left
            sLeaser.sprites[this.PhoneSprite(0, 2)] = this.CreateSprite(this.bodyJson.leftAntenna, new OracleJSON.SpriteDataJson()
            {
                sprite = "LizardScaleA1",
                scaleX = -0.75f,
                scaleY = 0.8f,
            });
            // Antenna length sprite right
            sLeaser.sprites[this.PhoneSprite(1, 2)] = this.CreateSprite(this.bodyJson.leftAntenna, new OracleJSON.SpriteDataJson()
            {
                sprite = "LizardScaleA1",
                scaleX = 0.75f,
                scaleY = 0.8f
            });
            for (int i = 0; i < 2; i++)
            {
                sLeaser.sprites[this.EyeSprite(i)] = this.CreateSprite(this.bodyJson.eyes, new OracleJSON.SpriteDataJson()
                {
                    sprite = "pixel",
                    color = new Color(0.02f, 0f, 0f)
                });

                sLeaser.sprites[this.HandSprite(i, 0)] = this.CreateSprite(this.bodyJson.hands, new OracleJSON.SpriteDataJson()
                {
                    sprite = "haloGlyph-1"
                });
                sLeaser.sprites[this.HandSprite(i, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
                this.ApplySpriteData(sLeaser.sprites[this.HandSprite(i, 1)], this.bodyJson.feet, new OracleJSON.SpriteDataJson()); // apply manually
                sLeaser.sprites[this.FootSprite(i, 0)] = this.CreateSprite(this.bodyJson.feet, new OracleJSON.SpriteDataJson()
                {
                    sprite = "haloGlyph-1"
                });
                sLeaser.sprites[this.FootSprite(i, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
                this.ApplySpriteData(sLeaser.sprites[this.FootSprite(i, 1)], this.bodyJson.feet, new OracleJSON.SpriteDataJson()); // apply manually
            }

            if (this.bodyJson.sigil?.sprite != null)
            {
                IteratorKit.Log.LogInfo("Loading sigil sprite");
                sLeaser.sprites[this.sigilSprite] = this.CreateSprite(this.bodyJson.sigil, new OracleJSON.SpriteDataJson()
                {
                    sprite = "MoonSigil",
                    color = Color.white
                });
            }
           // IteratorKit.Log.LogWarning(sLeaser.sprites[this.sigilSprite]?.element?.name ?? "NULL");

            sLeaser.sprites[this.fadeSprite] = this.CreateSprite(this.bodyJson.glowSprite, new OracleJSON.SpriteDataJson()
            {
                sprite = "Futile_White",
                scale = 12.5f,
                color = new Color(0f, 0f, 0f, 0.2f),
                shader = "FlatLightBehindTerrain"
            });


            sLeaser.sprites[this.killSprite] = this.CreateSprite(this.bodyJson.killSprite, new OracleJSON.SpriteDataJson()
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
        

        public FSprite CreateSprite(OracleJSON.SpriteDataJson spriteData, OracleJSON.SpriteDataJson defaultSpriteData)
        {
            if (spriteData == null)
            {
                spriteData = defaultSpriteData;
            }
            FSprite fSprite = new FSprite(spriteData?.sprite ?? defaultSpriteData.sprite, true);
            fSprite = this.ApplySpriteData(fSprite, spriteData, defaultSpriteData);
            return fSprite;
        }

        public FSprite ApplySpriteData(FSprite fSprite, OracleJSON.SpriteDataJson spriteData, OracleJSON.SpriteDataJson defaultSpriteData)
        {
            if (spriteData == null)
            {
                spriteData = defaultSpriteData;
            }
            if (spriteData.scale != 0f)
            {
                fSprite.scale = spriteData.scale;
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
            fSprite.alpha = (spriteData.a != 0f) ? spriteData.a : defaultSpriteData.a;

            
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

        public FSprite ApplySpritePalette(FSprite sprite, OracleJSON.SpriteDataJson spriteData)
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
            sprite.alpha = spriteData.a;
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
