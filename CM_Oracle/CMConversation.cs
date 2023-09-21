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
            TestMod.Logger.LogWarning(dialogList.Count);

            List<OracleDialogObjectJson> dialogData = dialogList?.FindAll(x => x.eventId == this.eventId);
            TestMod.Logger.LogWarning(dialogData.Count);
            if (dialogData.Count > 0)
            {
                foreach(OracleDialogObjectJson item in dialogData)
                {
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

                    if (item.gravity != -50f) // value not default
                    {
                        this.owner.forceGravity = true;
                        this.owner.roomGravity = item.gravity;
                    }

                    if (item.sound != null)
                    {
                        SoundID soundId = new SoundID(item.sound, false);
                        this.owner.oracle.room.PlaySound(soundId, 0f, 1f, 1f);

                    }

                    if (item.moveTo != null)
                    {
                        this.owner.SetNewDestination(item.moveTo);
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
