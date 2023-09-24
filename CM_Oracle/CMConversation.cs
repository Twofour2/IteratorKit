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
            TestMod.Logger.LogWarning($"Adding events for {this.eventId}");
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
                    TestMod.Logger.LogError("Tried to get non-existant dialog type. using generic");
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

                    TestMod.Logger.LogWarning(item.forSlugcats);
                    if (item.action != null)
                    {
                        if (Enum.TryParse(item.action, out CMOracleBehavior.CMOracleAction tmpAction))
                        {
                            this.events.Add(new CMOracleActionEvent(this, item.delay, tmpAction));
                        }
                        else
                        {
                            TestMod.Logger.LogError($"Given JSON action not valid. {item.action}");
                        }
                    }

                    if (!((StoryGameSession)this.owner.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                    {
                        // dont run any dialogs until we have given the player the mark.
                        return;
                    }


                    if (item.random)
                    {
                        this.events.Add(new Conversation.TextEvent(this, item.delay, this.ReplaceParts(GetRandomDialog(item)), item.hold));
                    }
                    else if ((item.texts?.Count() ?? 0) > 0)
                    {
                        foreach(string text in item.texts)
                        {
                            this.events.Add(new Conversation.TextEvent(this, item.delay, this.ReplaceParts(text), item.hold));
                        }
                    }
                    else
                    {
                        this.events.Add(new Conversation.TextEvent(this, item.delay, this.ReplaceParts(item.text), item.hold));
                    }

                    

                }
                
            }
            else
            {
                TestMod.Logger.LogError($"Failed to find dialog {this.eventId} of type {this.eventType}");
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

        public class CMOracleActionEvent : DialogueEvent
        {

            public new CMConversation owner;
            CMOracleBehavior.CMOracleAction action;

            public CMOracleActionEvent(CMConversation owner, int initialWait, CMOracleBehavior.CMOracleAction action) : base(owner, initialWait)
            {
                TestMod.Logger.LogWarning("Adding custom event");
                this.owner = owner;
                this.action = action;
            }

            public override void Activate()
            {
                base.Activate();
                TestMod.Logger.LogInfo($"Triggering action ${action}");
                this.owner.owner.NewAction(action); // this passes the torch over to CMOracleBehavior to run the rest of this shite
            }
        }

        public static void LogAllDialogEvents()
        {
            for (int i = 0; i < DataPearl.AbstractDataPearl.DataPearlType.values.Count; i++)
            {
                TestMod.Logger.LogInfo($"Pearl: {DataPearl.AbstractDataPearl.DataPearlType.values.GetEntry(i)}");
            }
            for (int i = 0; i < AbstractPhysicalObject.AbstractObjectType.values.Count; i++)
            {
                TestMod.Logger.LogInfo($"Item: {AbstractPhysicalObject.AbstractObjectType.values.GetEntry(i)}");
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
