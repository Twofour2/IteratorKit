using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx.Logging;
using HUD;
using IteratorMod.SRS_Oracle;
using static IteratorMod.CM_Oracle.OracleJSON.OracleEventsJson;
using static IteratorMod.SRS_Oracle.CMOracleBehavior;

namespace IteratorMod.CM_Oracle
{
    public class CMConversation : Conversation
    {
        public CMOracleBehavior owner;
        public string eventId;
        public CMDialogType eventType;
        public OracleJSON.OracleEventsJson oracleDialogJson;

        public enum CMDialogType
        {
            Generic,
            Pearls,
            Item
        }
       // public ConversationBehavior convBehav;
        public CMConversation(CMOracleBehavior owner, CMDialogType eventType, string eventId) : base(owner, Conversation.ID.None, owner.dialogBox)
        {
            this.owner = owner;
            // this.convBehav = convBehav;
            this.eventType = eventType;
            this.eventId = eventId;
            this.oracleDialogJson = this.owner.oracle.oracleJson.events;
            this.AddEvents();
        }

        public override void AddEvents()
        {
            // read this.id
            IteratorKit.Logger.LogWarning($"Adding events for {this.eventId}");
            List<OracleEventObjectJson> dialogList = this.oracleDialogJson.generic;
            
            switch (eventType)
            {
                case CMDialogType.Generic:
                    dialogList = this.oracleDialogJson.generic;
                    break;
                case CMDialogType.Pearls:
                    dialogList = this.oracleDialogJson.pearls;
                    break;
                case CMDialogType.Item:
                    dialogList = this.oracleDialogJson.items;
                    break;
                default:
                    IteratorKit.Logger.LogError("Tried to get non-existant dialog type. using generic");
                    dialogList = this.oracleDialogJson.generic;
                    break;
            }

            List<OracleEventObjectJson> dialogData = dialogList?.FindAll(x => x.eventId == this.eventId);
            if (dialogData.Count > 0)
            {
                foreach(OracleEventObjectJson item in dialogData)
                {
                    if (!item.forSlugcats.Contains(this.owner.oracle.room.game.GetStorySession.saveStateNumber)){
                        continue; // skip as this one isnt for us
                    }

                    IteratorKit.Logger.LogWarning(item.forSlugcats);
                    if (item.action != null)
                    {
                        if (Enum.TryParse(item.action, out CMOracleBehavior.CMOracleAction tmpAction))
                        {
                            this.events.Add(new CMOracleActionEvent(this, tmpAction, item));
                        }
                        else
                        {
                            IteratorKit.Logger.LogError($"Given JSON action not valid. {item.action}");
                        }
                    }

                    if (!((StoryGameSession)this.owner.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                    {
                        // dont run any dialogs until we have given the player the mark.
                        return;
                    }

                    // add the texts. get texts handles randomness
                    foreach (string text in item.getTexts(this.owner.oracle.room.game.GetStorySession.saveStateNumber))
                    {
                        this.events.Add(new CMOracleTextEvent(this, text, item));
                    }
                   

                }
                
            }
            else
            {
                IteratorKit.Logger.LogError($"Failed to find dialog {this.eventId} of type {this.eventType}");
            }


        }

        public string Translate(string s)
        {
            return this.owner.Translate(s);
        }

        public string ReplaceParts(string s)
        {
            s = Regex.Replace(s, "<PLAYERNAME>", this.NameForPlayer(false));
            s = Regex.Replace(s, "<CAPPLAYERNAME>", this.NameForPlayer(true));
            s = Regex.Replace(s, "<PlayerName>", this.NameForPlayer(false));
            s = Regex.Replace(s, "<CapPlayerName>", this.NameForPlayer(true));
            return s;
        }


        public string NameForPlayer(bool capitalized)
        {
            return "little creature";

        }

        public override void Update()
        {
            if (this.paused)
            {
                return;
            }
            if (this.events.Count == 0)
            {
                this.Destroy();
                return;
            }
            this.events[0].Update();
            if (this.events[0].IsOver)
            {
                this.events.RemoveAt(0);
            }
        }

        public void OnEventActivate(DialogueEvent dialogueEvent, OracleEventObjectJson dialogData)
        {
            if (dialogData.score != null)
            {
                this.owner.ChangePlayerScore(dialogData.score.action, dialogData.score.amount);
            }
            if (dialogData.movement != null)
            {
                IteratorKit.Logger.LogWarning($"Change movement to {dialogData.movement}");
                if (Enum.TryParse(dialogData.movement, out CMOracleMovement tmpMovement))
                {
                    this.owner.movementBehavior = tmpMovement;
                }
                else
                {
                    IteratorKit.Logger.LogError($"Invalid movement option provided: {dialogData.movement}");
                }

            }
        }

        public class CMOracleTextEvent : TextEvent
        {
            public new CMConversation owner;
            public ChangePlayerScoreJson playerScoreData;
            public OracleEventObjectJson dialogData;
            public CMOracleTextEvent(CMConversation owner, string text, OracleEventObjectJson dialogData) : base(owner, dialogData.delay, text, dialogData.hold)
            {
                this.owner = owner;
                this.playerScoreData = dialogData.score;
                this.dialogData = dialogData;
            }

            public override void Activate()
            {
                base.Activate();
                this.owner.OnEventActivate(this, dialogData); // get owner to run addit checks
            }
        }


        public class CMOracleActionEvent : DialogueEvent
        {

            public new CMConversation owner;
            CMOracleBehavior.CMOracleAction action;
            public string actionParam;
            public ChangePlayerScoreJson playerScoreData;
            public OracleEventObjectJson dialogData;

            public CMOracleActionEvent(CMConversation owner, CMOracleBehavior.CMOracleAction action, OracleEventObjectJson dialogData) : base(owner, dialogData.delay)
            {
                IteratorKit.Logger.LogWarning("Adding custom event");
                this.owner = owner;
                this.action = action;
                this.actionParam = dialogData.actionParam;
                this.playerScoreData = dialogData.score;
                this.dialogData = dialogData;
            }

            public override void Activate()
            {
                base.Activate();
                IteratorKit.Logger.LogInfo($"Triggering action ${action}");
                this.owner.owner.NewAction(action, this.actionParam); // this passes the torch over to CMOracleBehavior to run the rest of this shite
                this.owner.OnEventActivate(this, dialogData); // get owner to run addit checks
            }

            public static void LogAllDialogEvents()
            {
                for (int i = 0; i < DataPearl.AbstractDataPearl.DataPearlType.values.Count; i++)
                {
                    IteratorKit.Logger.LogInfo($"Pearl: {DataPearl.AbstractDataPearl.DataPearlType.values.GetEntry(i)}");
                }
                for (int i = 0; i < AbstractPhysicalObject.AbstractObjectType.values.Count; i++)
                {
                    IteratorKit.Logger.LogInfo($"Item: {AbstractPhysicalObject.AbstractObjectType.values.GetEntry(i)}");
                }
            }


        }
    }
}
