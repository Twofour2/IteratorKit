using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IteratorMod.SRS_Oracle
{
    public class CMOracleSubBehavior
    {

        public SubBehavID ID;

        // Token: 0x040010DE RID: 4318
        public CMOracleBehavior owner;


        public CMOracleSubBehavior(CMOracleBehavior owner, SubBehavID ID)
        {
            this.owner = owner;
            this.ID = ID;
        }

        public class SubBehavID : ExtEnum<CMOracleSubBehavior.SubBehavID>
        {
            // Token: 0x06001244 RID: 4676 RVA: 0x000F9176 File Offset: 0x000F7376
            public SubBehavID(string value, bool register = false) : base(value, register)
            {
            }

            // Token: 0x040010DF RID: 4319
            public static readonly CMOracleSubBehavior.SubBehavID General = new CMOracleSubBehavior.SubBehavID("General", true);

        }

        public class NoSubBehavior : CMOracleSubBehavior
        {
            // Token: 0x06001246 RID: 4678 RVA: 0x000F91ED File Offset: 0x000F73ED
            public NoSubBehavior(CMOracleBehavior owner) : base(owner, CMOracleSubBehavior.SubBehavID.General)
            {
            }
        }
    }

    
}
