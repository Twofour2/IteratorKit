using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static IteratorKit.CMOracle.OracleJData.OracleEventsJData;

namespace IteratorKit.CMOracle
{
    public class CMOracleTextEvent : Conversation.TextEvent
    {
        public CMConversation cmOwner;
        public ChangePlayerScoreJData playerScoreData;
        public OracleEventObjectJData eventData;
        public CMOracleTextEvent(CMConversation owner, string text, OracleEventObjectJData eventData) : base(owner, eventData.delay, text, eventData.hold)
        {
            this.owner = owner; this.cmOwner = owner;
            this.playerScoreData = eventData.score;
            this.eventData = eventData;
        }

        public override void Activate()
        {
            base.Activate();
            if (this.eventData.color == UnityEngine.Color.white)
            {
                UnityEngine.Color defaultOracleColor = this.cmOwner?.owner?.oracle?.OracleJson()?.dialogColor ?? UnityEngine.Color.white;
                this.owner.dialogBox.currentColor = defaultOracleColor;
            }
            else
            {
                this.owner.dialogBox.currentColor = this.eventData.color;
            }
            // calls over to event handler in CMOracleBehavior, as well as any custom listeners
            this.cmOwner.owner.oracle.OracleEvents().OnCMEventStart?.Invoke(this.cmOwner.owner, this.eventData.eventId, this, this.eventData);
        }
    }

    public class CMOracleActionEvent : Conversation.DialogueEvent
    {
        public CMConversation cmOwner;
        public string action;
        public string actionParam;
        public ChangePlayerScoreJData playerScoreData;
        public OracleEventObjectJData eventData;
        public CMOracleActionEvent(CMConversation owner, OracleEventObjectJData eventData) : base(owner, eventData.delay)
        {
            this.owner = owner; this.cmOwner = owner;
            this.action = eventData.action;
            this.actionParam = eventData.actionParam;
            this.playerScoreData = eventData.score;
            this.eventData = eventData;
        }

        public override void Activate()
        {
            base.Activate();

            // calls over to event handler in CMOracleBehavior, as well as any custom listeners
            this.cmOwner.owner.oracle.OracleEvents().OnCMEventStart?.Invoke(this.cmOwner.owner, this.eventData.eventId, this, this.eventData);
        }

        public static void LogAllDialogEvents()
        {
            for (int i = 0; i < DataPearl.AbstractDataPearl.DataPearlType.values.Count; i++)
            {
                IteratorKit.Log.LogInfo($"Pearl: {DataPearl.AbstractDataPearl.DataPearlType.values.GetEntry(i)}");
            }
            for (int i = 0; i < AbstractPhysicalObject.AbstractObjectType.values.Count; i++)
            {
                IteratorKit.Log.LogInfo($"Item: {AbstractPhysicalObject.AbstractObjectType.values.GetEntry(i)}");
            }
        }
    }
}
