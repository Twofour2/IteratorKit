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
        public CMOracleGraphics(CMOracle oracle) : base(oracle) {
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
    }
}
