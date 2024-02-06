using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using static IteratorKit.CMOracle.OracleJSON;

namespace IteratorKit.CMOracle
{
    public class OracleJSON
    {
        public string id;
        public string roomId;
        public OracleBodyJson body = new OracleBodyJson();
        public float gravity;
        public float airFriction;
        public OracleRoomEffectsJson roomEffects;
        public int annoyedScore, angryScore;
        public float talkHeight = 250f;
        public Vector2 startPos = Vector2.zero;
        public string pearlFallback = null;
        public List<OracleJsonTilePos> cornerPositions = new List<OracleJsonTilePos>();
        public OverseerJson overseers;

        public enum OracleType
        {
            normal, sitting
        }
        public OracleType type;
    
        [JsonProperty("for")]
        private List<String> forSlugList; 

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

        public class OracleRoomEffectsJson
        {
            public int swarmers = 0;
            public string pearls = null;
        }

        public class OracleArmJson
        {
            public SpriteDataJson armColor = new SpriteDataJson();
            public SpriteDataJson armHighlight = new SpriteDataJson();
        }

        public class SpriteDataJson
        {
            // generic, used for a lot of things
            // values are not always used, usually just used for colors
            public float r, g, b = 0f;

            public float a { get; set; } = 255f;

            public string sprite, shader;

            public float scaleX, scaleY = -1f;

            public Color color
            {
                get { return new Color(r / 255, g / 255, b / 255, a / 255); }
            }
        }

        public class OracleBodyJson
        {
            public SpriteDataJson oracleColor, eyes, head, torso, arms, hands, legs, feet, chin, neck = new SpriteDataJson();
            public SpriteDataJson sigil = null;
            public OracleGownJson gown = new OracleGownJson();
            public OracleHaloJson halo = null;
            public OracleArmJson arm = null; 

            public class OracleGownJson
            {
                public OracleGownColorDataJson color = new OracleGownColorDataJson();

                public class OracleGownColorDataJson
                {
                    public string type;
                    public float r, g, b, a = 255f;

                    public OracleGradientDataJson from;
                    public OracleGradientDataJson to;
                }

                public class OracleGradientDataJson
                {
                    public float h, s, l = 0f;
                }

            }

            public class OracleHaloJson
            {
                public SpriteDataJson innerRing, outerRing, sparks = new SpriteDataJson();
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

                public List<string> getTexts(SlugcatStats.Name forSlugcat)
                {
                    if (!this.forSlugcats.Contains(forSlugcat))
                    {
                        return null;
                    }
                    if ((this.texts?.Count ?? 0) == 0)
                    {
                        return new List<string>() { this.text };
                    }
                    if (this.random)
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
                public UnityEngine.Color color = UnityEngine.Color.white;

                public string movement;
                public int pauseFrames = 0; // only for pebbles

                // projection screen
                public List<OracleScreenJson> screens = new List<OracleScreenJson>();


            }

            public class OracleScreenJson {
                public string image;
                public int hold;
                public float alpha = 255f;
                public Vector2 pos;
                public float moveSpeed = 50f;
            }

            public class ChangePlayerScoreJson
            {
                public string action; // set, add, subtract
                public int amount;
            }
        }

        public class OverseerJson
        {
            public SpriteDataJson color;
            public List<string> regions;
            public string guideToRoom;
            public int genMin, genMax;
        }
    }
    
    
    public class OracleJsonTilePos
    {
        public int x, y;
    }
}
