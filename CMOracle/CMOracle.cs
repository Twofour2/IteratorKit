using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IteratorKit.CMOracle
{

    public class CMOracle : Oracle
    {
        public CMOracle(AbstractPhysicalObject abstractPhysicalObject, Room room, OracleJSON oracleJson) : base(abstractPhysicalObject, room)
        {
            IteratorKit.Log.LogInfo("Loaded!");
        }
    }

}