using System;
using System.Collections.Generic;
using System.Linq;
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
        public int annoyedScore, angryScore;
        public float talkHeight = 250f;


        [JsonProperty("for")]
        private List<String> forSlugList; //= // SlugcatStats.Name slugcatName = this.oracle.room.game.GetStorySession.saveStateNumber;

        public List<SlugcatStats.Name> forSlugcats
        {
            get
            {
                List<SlugcatStats.Name> nameList = SlugcatStats.getSlugcatTimelineOrder().ToList();
                if (this.forSlugList != null && this.forSlugList.Count > 0)
                {
                    return new List<SlugcatStats.Name>(nameList.Where(x => forSlugList.Contains(x.value)));
                }
                else
                {
                    return nameList;
                }
            }
        }

        public OracleDialogJson dialogs = new OracleDialogJson();

        

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


        public class OracleDialogJson
        {
            public List<OracleDialogObjectJson> generic = new List<OracleDialogObjectJson>();
            public List<OracleDialogObjectJson> pearls = new List<OracleDialogObjectJson>();
            public List<OracleDialogObjectJson> items = new List<OracleDialogObjectJson>();


            public class OracleDialogObjectJson
            {
                [JsonProperty("event")]
                public string eventId;

                public string item
                {
                    set { this.eventId = value; }
                    get { return this.eventId; }
                }

                [JsonProperty("for")]
                private List<String> forSlugList; //= // SlugcatStats.Name slugcatName = this.oracle.room.game.GetStorySession.saveStateNumber;

                public List<SlugcatStats.Name> forSlugcats
                {
                    get
                    {
                        List<SlugcatStats.Name> nameList = Expedition.ExpeditionData.GetPlayableCharacters();
                        IteratorMod.Logger.LogWarning(nameList.Count);

                        if (this.forSlugList != null && this.forSlugList.Count > 0)
                        {
                            return new List<SlugcatStats.Name>(nameList.Where(x => forSlugList.Contains(x.value)));
                        }
                        else
                        {
                            return nameList;
                        }
                    }
                }

                public int delay, hold = 10;

                public string text = null;
                public List<string> texts;

                public string translateString = null;
                public bool random = false;
                public float gravity = -50f; // -50f default value keeps gravity at whatever it already is
                public string sound; // links to SoundID

                public Vector2 moveTo;

                public string action;
                public string actionParam;

                public ChangePlayerScoreJson score;

                public string movement;


            }

            public class ChangePlayerScoreJson
            {
                public string action; // set, add, subtract
                public int amount;
            }
        }
    }
}
