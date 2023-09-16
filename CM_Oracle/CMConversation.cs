using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using IteratorMod.SRS_Oracle;

namespace IteratorMod.CM_Oracle
{
    public class CMConversation : Conversation
    {
        public CMOracleBehavior owner;
       // public ConversationBehavior convBehav;
        public CMConversation(CMOracleBehavior owner, Conversation.ID id, DialogBox dialogBox) : base(owner, id, dialogBox)
        {
            this.owner = owner;
           // this.convBehav = convBehav;
            this.AddEvents();
        }

        public override void AddEvents()
        {

            this.events.Add(new Conversation.TextEvent(this, 0, "This is a test", 0));
            this.events[0].Activate();
            //if (!this.owner.playerEnteredWithMark)
            //{
            //    this.events.Add(new Conversation.TextEvent(this, 0, ". . .", 0));
            //    this.events.Add(new Conversation.TextEvent(this, 0, this.Translate("...is this reaching you?"), 0));
            //}
        }

        public string Translate(string s)
        {
            return this.owner.Translate(s);
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
