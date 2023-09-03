using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IteratorMod.SRS_Oracle
{
    public class SRSOracleSubBehavior
    {

        public SubBehavID ID;

        // Token: 0x040010DE RID: 4318
        public SRSOracleBehavior owner;


        public SRSOracleSubBehavior(SRSOracleBehavior owner, SubBehavID ID)
        {
            this.owner = owner;
            this.ID = ID;
        }

        public class SubBehavID : ExtEnum<SRSOracleSubBehavior.SubBehavID>
        {
            // Token: 0x06001244 RID: 4676 RVA: 0x000F9176 File Offset: 0x000F7376
            public SubBehavID(string value, bool register = false) : base(value, register)
            {
            }

            // Token: 0x040010DF RID: 4319
            public static readonly SRSOracleSubBehavior.SubBehavID General = new SRSOracleSubBehavior.SubBehavID("General", true);

        }

        public class NoSubBehavior : SRSOracleSubBehavior
        {
            // Token: 0x06001246 RID: 4678 RVA: 0x000F91ED File Offset: 0x000F73ED
            public NoSubBehavior(SRSOracleBehavior owner) : base(owner, SRSOracleSubBehavior.SubBehavID.General)
            {
            }
        }
    }

    
}
