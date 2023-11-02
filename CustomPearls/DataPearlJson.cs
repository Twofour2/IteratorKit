using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorMod.CM_Oracle;
using UnityEngine;
using static IteratorMod.CM_Oracle.OracleJSON.OracleEventsJson;

namespace IteratorMod.CustomPearls
{
    public class DataPearlJson
    {
        public PearlColor color;
        public PearlColor highlight;
        public string pearl;
        public OracleDialogs dialogs;

        public class OracleDialogs
        {
            public OracleJSON.OracleEventsJson.OracleEventObjectJson moon, pebbles, pastMoon;

            public OracleEventObjectJson getDialogsForOracle(OracleBehavior oracleBehavior)
            {
                switch (oracleBehavior.oracle.ID.value)
                {
                    case "SS":
                        return this.pebbles;
                    case "SL":
                        return this.moon;
                    case "DM":
                        return this.pastMoon;
                    default:
                        return this.moon;
                }
            }
        }




        public class PearlColor {
            public float r, g, b = 1f;
            public float a { get; set; } = 1f;
            public UnityEngine.Color color
            {
                get { return new UnityEngine.Color(r, g, b, a); }
            }
        }

        



    }
}
