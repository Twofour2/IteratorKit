using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IteratorKit.CMOracle
{
    public class CMOracleData
    {
        public Oracle owner;
        public OracleJSON oracleJson;
        public CMOracleData(Oracle oracle)
        {
            this.owner = oracle;
        }
    }
}
