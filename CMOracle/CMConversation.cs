using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IteratorKit.CMOracle
{
    public class CMConversation : Conversation
    {
        public CMOracleBehavior owner;
        public string eventId;
        public CMDialogCategory eventCategory;
        public OracleJSON.OracleEventsJson dialogJson {
            get { return this.owner.oracleJson.events; }
        }

        public DataPearl.AbstractDataPearl.DataPearlType pearlType;
        public bool resumeConvFlag = false;

        public enum CMDialogCategory
        {
            Generic,
            Pearls,
            Items
        }

        public CMConversation(CMOracleBehavior owner, CMDialogCategory eventCategory, string eventId, DataPearl.AbstractDataPearl.DataPearlType pearlType = null) : base(owner, Conversation.ID.None, owner.dialogBox)
        {
            this.owner = owner;
            this.eventCategory = eventCategory;
            this.eventId = eventId;
            this.pearlType = pearlType;
            this.AddEvents();
        }
    }
}
