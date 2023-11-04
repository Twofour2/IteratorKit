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
        public OracleBodyJson body = new OracleBodyJson();
        public float gravity;
        public float airFriction;
        public int swarmers = 0;
        public int annoyedScore, angryScore;
        public float talkHeight = 250f;
        public Vector2 startPos = Vector2.zero;


        [JsonProperty("for")]
        private List<String> forSlugList; //= // SlugcatStats.Name slugcatName = this.oracle.room.game.GetStorySession.saveStateNumber;

        public List<SlugcatStats.Name> forSlugcats
        {
            get
            {
                List<SlugcatStats.Name> nameList = Expedition.ExpeditionData.GetPlayableCharacters();

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

        public OracleEventsJson events = new OracleEventsJson();

        

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
            public OracleBodyChunkJson oracleColor, eyes, head, torso, arms, hands, legs, feet, chin, neck, sigil = new OracleBodyChunkJson();
            public OracleGownJson gown = new OracleGownJson();

            public class OracleGownJson
            {
                public OracleGownColorDataJson color = new OracleGownColorDataJson();

                public class OracleGownColorDataJson
                {
                    public string type;
                    public float r, g, b, a = 1f;

                    public OracleGradientDataJson from;
                    public OracleGradientDataJson to;
                }

                public class OracleGradientDataJson
                {
                    public float h, s, l = 0f;
                }

            }

            
        }


        public class OracleEventsJson
        {
            public List<OracleEventObjectJson> generic = new List<OracleEventObjectJson>();
            public List<OracleEventObjectJson> pearls = new List<OracleEventObjectJson>();
            public List<OracleEventObjectJson> items = new List<OracleEventObjectJson>();


            public class OracleEventObjectJson
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
                        IteratorKit.Logger.LogWarning(nameList.Count);

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

                public List<string> getTexts(SlugcatStats.Name forSlugcat, bool random = false)
                {
                    if (!this.forSlugcats.Contains(forSlugcat))
                    {
                        return null;
                    }
                    if ((this.texts?.Count ?? 0) == 0)
                    {
                        return new List<string>() { this.text };
                    }
                    if (random)
                    {
                        return new List<string>() { this.texts[UnityEngine.Random.Range(0, this.texts.Count())] };
                    }
                    return this.texts;

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
                public int pauseFrames = 0; // only for pebbles


            }

            public class ChangePlayerScoreJson
            {
                public string action; // set, add, subtract
                public int amount;
            }
        }
    }
}
