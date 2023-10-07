using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx.Logging;
using HUD;
using IteratorMod.SRS_Oracle;
using static IteratorMod.CM_Oracle.OracleJSON.OracleDialogJson;
using static IteratorMod.SRS_Oracle.CMOracleBehavior;

namespace IteratorMod.CM_Oracle
{
    public class CMConversation : Conversation
    {
        public CMOracleBehavior owner;
        public string eventId;
        public CMDialogType eventType;
        public OracleJSON.OracleDialogJson oracleDialogJson;

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
            this.oracleDialogJson = this.owner.oracle.oracleJson.dialogs;
            this.AddEvents();
        }

        public override void AddEvents()
        {
            // read this.id
            IteratorMod.Logger.LogWarning($"Adding events for {this.eventId}");
            List<OracleDialogObjectJson> dialogList = this.oracleDialogJson.generic;
            
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
                    IteratorMod.Logger.LogError("Tried to get non-existant dialog type. using generic");
                    dialogList = this.oracleDialogJson.generic;
                    break;
            }

            List<OracleDialogObjectJson> dialogData = dialogList?.FindAll(x => x.eventId == this.eventId);
            if (dialogData.Count > 0)
            {
                foreach(OracleDialogObjectJson item in dialogData)
                {
                    if (!item.forSlugcats.Contains(this.owner.oracle.room.game.GetStorySession.saveStateNumber)){
                        continue; // skip as this one isnt for us
                    }

                    IteratorMod.Logger.LogWarning(item.forSlugcats);
                    if (item.action != null)
                    {
                        if (Enum.TryParse(item.action, out CMOracleBehavior.CMOracleAction tmpAction))
                        {
                            this.events.Add(new CMOracleActionEvent(this, tmpAction, item));
                        }
                        else
                        {
                            IteratorMod.Logger.LogError($"Given JSON action not valid. {item.action}");
                        }
                    }

                    if (!((StoryGameSession)this.owner.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                    {
                        // dont run any dialogs until we have given the player the mark.
                        return;
                    }


                    if (item.random)
                    {
                        // todo: fix cause this is not random
                        this.events.Add(new CMOracleTextEvent(this, item));
                    }
                    else if ((item.texts?.Count() ?? 0) > 0)
                    {
                        foreach(string text in item.texts)
                        {
                            this.events.Add(new CMOracleTextEvent(this, item));
                        }
                    }
                    else
                    {
                        this.events.Add(new CMOracleTextEvent(this, item));
                    }

                }
                
            }
            else
            {
                IteratorMod.Logger.LogError($"Failed to find dialog {this.eventId} of type {this.eventType}");
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

        private string GetRandomDialog(OracleDialogObjectJson dialogData)
        {
            return dialogData.texts[UnityEngine.Random.Range(0, dialogData.texts.Count())];
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

        public void OnEventActivate(DialogueEvent dialogueEvent, OracleDialogObjectJson dialogData)
        {
            if (dialogData.score != null)
            {
                this.owner.ChangePlayerScore(dialogData.score.action, dialogData.score.amount);
            }
            if (dialogData.movement != null)
            {
                IteratorMod.Logger.LogWarning($"Change movement to {dialogData.movement}");
                if (Enum.TryParse(dialogData.movement, out CMOracleMovement tmpMovement))
                {
                    this.owner.movementBehavior = tmpMovement;
                }
                else
                {
                    IteratorMod.Logger.LogError($"Invalid movement option provided: {dialogData.movement}");
                }

            }
        }

        public class CMOracleTextEvent : TextEvent
        {
            public new CMConversation owner;
            public ChangePlayerScoreJson playerScoreData;
            public OracleDialogObjectJson dialogData;
            public CMOracleTextEvent(CMConversation owner, OracleDialogObjectJson dialogData) : base(owner, dialogData.delay, dialogData.text, dialogData.hold)
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
            public OracleDialogObjectJson dialogData;

            public CMOracleActionEvent(CMConversation owner, CMOracleBehavior.CMOracleAction action, OracleDialogObjectJson dialogData) : base(owner, dialogData.delay)
            {
                IteratorMod.Logger.LogWarning("Adding custom event");
                this.owner = owner;
                this.action = action;
                this.actionParam = dialogData.actionParam;
                this.playerScoreData = dialogData.score;
                this.dialogData = dialogData;
            }

            public override void Activate()
            {
                base.Activate();
                IteratorMod.Logger.LogInfo($"Triggering action ${action}");
                this.owner.owner.NewAction(action, this.actionParam); // this passes the torch over to CMOracleBehavior to run the rest of this shite
                this.owner.OnEventActivate(this, dialogData); // get owner to run addit checks

            }

            public static void LogAllDialogEvents()
            {
                for (int i = 0; i < DataPearl.AbstractDataPearl.DataPearlType.values.Count; i++)
                {
                    IteratorMod.Logger.LogInfo($"Pearl: {DataPearl.AbstractDataPearl.DataPearlType.values.GetEntry(i)}");
                }
                for (int i = 0; i < AbstractPhysicalObject.AbstractObjectType.values.Count; i++)
                {
                    IteratorMod.Logger.LogInfo($"Item: {AbstractPhysicalObject.AbstractObjectType.values.GetEntry(i)}");
                }
            }


            //public abstract class ConversationBehavior : CMOracleBehavior.TalkBehavior
            //{
            //    public ConversationBehavior(CMOracleBehavior owner, CMOracleBehavior.SubBehavior.SubBehavID ID, Conversation.ID convoID) : base(owner, ID)
            //    {
            //        this.convoID = convoID;
            //    }

            //    public Conversation.ID convoID;
            //}
        }
    }
}
