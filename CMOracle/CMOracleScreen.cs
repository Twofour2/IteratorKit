using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using UnityEngine;
using static IteratorKit.CMOracle.OracleJData.OracleEventsJData;

namespace IteratorKit.CMOracle
{
    public class CMOracleScreen
    {
        public CMOracleBehavior cmBehavior;
        public List<OracleScreenJData> screenData = null;
        public int currScreen = 0;
        public int currScreenCounter = 0;
        public ProjectedImage currImage;
        public OracleScreenJData currScreenData;
        public Vector2 currImagePos;
        public CMOracleScreen(CMOracleBehavior cmBehavior)
        {
            this.cmBehavior = cmBehavior;
        }

        public void SetScreens(List<OracleScreenJData> screenData)
        {
            this.screenData = screenData;
        }

        public void Update()
        {
            if (screenData == null)
            {
                return;
            }
            if (this.currImage != null)
            {
                if (this.currScreenData.moveSpeed > 0)
                {
                    this.currImage.setPos = Custom.MoveTowards(this.currImage.pos, this.currScreenData.pos, this.currScreenData.moveSpeed);
                }
                else
                {
                    this.currImage.setPos = this.currScreenData.pos;
                }
            }
            if (this.currScreenCounter > 0 && this.currScreen != 0)
            {
                this.currScreenCounter--;
            }
            else
            {
                if (this.currScreen >= this.screenData.Count())
                {
                    // out of screens
                    IteratorKit.Log.LogInfo("Destroying screen");
                    this.currScreen = 0;
                    this.screenData = null;
                    this.currImage.Destroy();
                    this.currImage = null;
                    return;
                }
                // show next screen
                this.currScreenData = this.screenData[this.currScreen];
                IteratorKit.Log.LogInfo($"Next image {this.currScreenData.image} at pos {this.currScreenData.pos} with alpha {this.currScreenData.alpha}");
                if (this.currScreenData.image != null)
                {
                    if (this.currImage != null)
                    {
                        this.currImage.Destroy();
                    }
                    this.currImage = this.cmBehavior.oracle.myScreen.AddImage(this.currScreenData.image);
                }
                if (this.currScreenData.moveSpeed <= 0)
                {
                    this.currImage.pos = this.currScreenData.pos;
                    this.currImage.setPos = this.currScreenData.pos;
                }
                this.currImage.setAlpha = this.currScreenData.alpha / 255;
                this.currScreenCounter = this.currScreenData.hold;
                this.currScreen += 1;
            }
        }
        
    }
}
