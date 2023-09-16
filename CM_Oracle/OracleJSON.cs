using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using static IteratorMod.CM_Oracle.OracleJSON;

namespace IteratorMod.CM_Oracle
{
    public class OracleJSON
    {
        public string id;
        public string roomId;
        public OracleBodyJson body;
        public float gravity;
        public float airFriction;
        public int swarmers = 0;

        

        public class OracleArmJson
        {
            public List<Vector2> corners;
        }

        public class OracleBodyChunkJson
        {
            public float r, g, b = 1f;

            public float a { get; set; } = 1f;

            public string sprite;

            public Color color
            {
                get { return new Color(r, g, b, a); }
            }
        }
        
        public class OracleBodyJson
        {
            public OracleBodyChunkJson oracleColor, eyes, head, torso, arms, hands, legs, feet, chin, neck, sigil;
            public OracleGownJson gown;

            public class OracleGownJson
            {
                public OracleGownColorDataJson color;

                public class OracleGownColorDataJson
                {
                    public string type;
                    public float r, g, b, a = 1f;

                    public OracleGradientDataJson from;
                    public OracleGradientDataJson to;
                }

                public class OracleGradientDataJson
                {
                    public float h, s, l;
                }

            }

            
        }
    }
}
