using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IteratorKit.CMOracle
{
    public class CMOracleSitBehavior : CMOracleBehavior
    {
        public CMOracleSitBehavior(CMOracle oracle) : base(oracle)
        {
            IteratorKit.Logger.LogWarning("Init as sitting");
        }

        public override void Update(bool eu)
        {
            IteratorKit.Logger.LogWarning("Sitting update!");
        }
    }
}
