using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using UnityEngine;
using MoreSlugcats;
using static IteratorKit.CMOracle.OracleJSON;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace IteratorKit.CMOracle
{
    public class OracleJSON
    {
        /// <summary>
        /// Unique Identifier for your Iterator/Oracle. It's best you keep this under 5 characters
        /// </summary>
        /// <example>
        ///    "id": "SRS"
        /// </example>
        public string id;
        /// <summary>
        /// Room for your iterator to spawn in
        /// </summary>
        /// <example>
        ///     "roomId": "SU_ai"
        /// </example>
        public string roomId;
        /// <exclude />
        public OracleBodyJson body = new OracleBodyJson();
        /// <exclude />
        public float gravity;
        /// <summary>
        /// Section for some basic room effects.
        /// </summary>
        /// <example>
        ///     "roomEffects": {
        ///         "swarmers": 15,
        ///         "pearls": true
        ///     },
        /// </example>
        public OracleRoomEffectsJson roomEffects;
        /// <summary>
        /// Sets what score the iterator gets annoyed at. When player score is less than it runs the event "oracleAnnoyed"
        /// </summary>
        /// <example>
        ///     "annoyedScore": 10
        /// </example>
        public int annoyedScore = 10;
        /// <summary>
        /// Sets what score the iterator gets angry at. When player score is less than it runs the event "oracleAngry"
        /// </summary>
        /// <example>
        ///     "angryScore": 0
        /// </example>
        public int angryScore = 0;
        /// <summary>
        /// Sets what score the iterator becomes friendly at. When the player score is greater than it runs the event "oracleFriend"
        /// </summary>
        /// <example>
        ///     "friendScore": 20
        /// </example>
        public int friendScore = 20;

        /// <summary>
        /// Set starting score for player relationship
        /// </summary>
        public int playerScore = 20;

        /// <summary>
        /// Determines what is used in place of "<PLAYERNAME>" in dialogs
        /// </summary>
        public string nameForPlayer = "little creature";

        /// <summary>
        /// A list of pearls that the iterator wont pick up. This is best used for pearls spawned in the iterator can so they don't attempt to read it.
        /// </summary>
        /// <example>
        ///     "ignorePearlIds": ["tomato", "Spearmasterpearl"]
        /// </example>
        public List<string> ignorePearlIds = new List<string>();
        /// <exclude />
        public float talkHeight = 250f;
        /// <summary>
        /// Default/Starting position using tile coordinates
        /// </summary>
        /// <example>
        ///     "startPos": {"x": 15, "y": 15}
        /// </example>
        public Vector2 startPos = Vector2.zero;
        /// <summary>
        /// Used when no pearl dialog is found
        /// </summary>
        /// <example>
        ///     "pearlFallback": "pebbles"
        /// </example>
        public string pearlFallback = null;

        /// <summary>
        /// List of tile positions for the arm to consider the corners of the room
        /// </summary>
        /// <example>
        /// "cornerPositions": [
        ///        {"x": 9,"y": 32},
        ///        {"x": 38,"y": 32},
        ///        {"x": 38,"y": 3},
        ///        {"x": 10,"y": 3}
        ///     ]
        /// </example>
        public List<OracleJsonTilePos> cornerPositions = new List<OracleJsonTilePos>();

        /// <exclude />
        public OverseerJson overseers;
        /// <summary>
        /// Sets the default dialog text color
        /// </summary>
        /// <example>
        ///     "dialogColor": {"r": 255, "g": 0, "b": 0, "a": 255}
        /// </example>
        public UnityEngine.Color dialogColor = Color.white;

        /// <exclude />
        public enum OracleType
        {
            normal, sitting
        }
        /// <exclude />
        public OracleType type;

        /// <summary>
        /// Restricts what slugcats this oracle will spawn for
        /// </summary>
        [JsonProperty("for")]
        private List<String> forSlugList;

        /// <exclude />
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

        /// <summary>
        /// Main events section, contains "generic", "pearls" and "items" sections
        /// </summary>
        /// <example>
        /// "events": {
        ///     "generic": [],
        ///     "pearls": [],
        ///     "items": []
        /// }
        /// </example>
        public OracleEventsJson events = new OracleEventsJson();

        /// <exclude/>
        public class OracleRoomEffectsJson
        {
            public int swarmers = 0;
            public string pearls = null;
        }

        /// <exclude/>
        public class OracleArmJson
        {
            public SpriteDataJson armColor = new SpriteDataJson();
            public SpriteDataJson armHighlight = new SpriteDataJson();
        }

        /// <summary>
        /// General options provided for any sprites. Some options may not always be avalible
        /// </summary>
        public class SpriteDataJson
        {
            // generic, used for a lot of things
            // values are not always used, usually just used for colors
            public class SpriteColorDataJson
            {
                public float r, g, b = 0f;
                public float? a = null;
            }

            [JsonProperty("color")]
            public SpriteColorDataJson spriteColorData = new SpriteColorDataJson();

            [JsonProperty("colorObj")]
            public Color color
            {
                get
                {
                    return new Color(spriteColorData.r / 255, spriteColorData.g / 255, spriteColorData.b / 255, (spriteColorData.a ?? 255f) / 255);
                }
                set
                {
                    spriteColorData.r = value.r; spriteColorData.g = value.g; spriteColorData.b = value.b; 
                }
            }

            public float r { get { return spriteColorData.r; } set { spriteColorData.r = value; } }
            public float g { get { return spriteColorData.g; } set { spriteColorData.g = value; } }
            public float b { get { return spriteColorData.b; } set { spriteColorData.b = value; } }
            public float? a { get { return spriteColorData.a; } set { spriteColorData.a = value; } }
            /// <summary>
            /// Specifies the name of the sprite to pass along to the Futile asset loader.
            /// <see href="/iterators.html#sigil"/>
            /// Press "6" with the dev tools open to dump all of the sprites loaded to the console
            /// </summary>
            public string sprite;
            public string shader;

            public float scale, scaleX, scaleY = 0f;
            public float anchorX, anchorY = 0f;

            //public Color color
            //{
            //    get { return new Color(r / 255, g / 255, b / 255, (a ?? 255f) / 255); }
            //    set
            //    {
            //        this.r = value.r * 255;
            //        this.g = value.g * 255;
            //        this.b = value.b * 255;
            //        // !! dont set alpha here, it's read and written to seperately
            //    }
            //}
        }

        public class OracleBodyJson
        {
            public SpriteDataJson oracleColor, body, eyes, head, leftAntennaBase, leftAntenna, rightAntennaBase, rightAntenna, torso, arms, hands, legs, feet, chin, neck, killSprite, glowSprite = new SpriteDataJson();
            /// <summary>
            /// <see href="http://localhost:8080/iterators.html#sigil"/>
            /// </summary>
            public SpriteDataJson sigil = null;
            public OracleGownJson gown = new OracleGownJson();
            /// <summary>
            /// <see href="http://localhost:8080/iterators.html#halos"/>
            /// </summary>
            public OracleHaloJson halo = null;

            /// <summary>
            /// Arm connecting iterator to the wall
            /// </summary>
            /// <example>
            /// "arm": {
            ///    "armColor": {"r": 255, "g": 0, "b": 0},
            ///    "armHighlight": {"r": 255, "g": 0, "b": 0}
            /// }
            /// </example>
            public OracleArmJson arm = null;

            public class OracleGownJson
            {
                public OracleGownColorDataJson color = new OracleGownColorDataJson();

                /// <summary>
                /// Must specify a type of either "solid" or "gradient".
                /// Solid uses rgba values
                /// Gradient uses "from" and "to" using hsl
                /// </summary>
                public class OracleGownColorDataJson
                {

                    public string type;
                    public float r, g, b, a = 255f;

                    public OracleGradientDataJson from;
                    public OracleGradientDataJson to;
                }

                /// <exclude/>
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

        public partial class OracleEventObjectListJson<T>
        {

        }

        /// <summary>
        /// Core events class See <see cref="OracleEventObjectJson"/> for how events work
        /// </summary>
        public class OracleEventsJson
        {
            /// <exclude/>
            public object generic, pearls, items;

            /// <exclude/>
            public Dictionary<string, List<OracleEventObjectJson>> genericEvents, pearlEvents, itemEvents;

            /// <exclude/>
            [OnDeserialized]
            internal void ConvertJsonObjects(StreamingContext content)
            {
                if (generic != null)
                {
                    this.genericEvents = DoFancyJsonConvert(generic);
                }
                if (pearls != null)
                {
                    this.pearlEvents = DoFancyJsonConvert(pearls);
                }

                if (items != null)
                {
                    this.itemEvents = DoFancyJsonConvert(items);
                }

            }

            /// <exclude/>
            private Dictionary<string, List<OracleEventObjectJson>> DoFancyJsonConvert(object eventsObject)
            {
                if (eventsObject is JArray)
                {
                    // support old format style
                    // "generic": [
                    //  {
                    //      "event": "playerEnter"
                    //      ... event code
                    //  }
                    // ]
                    List<OracleEventObjectJson> genericArr = ((JArray)eventsObject).ToObject<List<OracleEventObjectJson>>();
                    Dictionary<string, List<OracleEventObjectJson>> resEvents = new Dictionary<string, List<OracleEventObjectJson>>();
                    foreach (IGrouping<string, OracleEventObjectJson> obj in genericArr.GroupBy(x => x.eventId))
                    {
                        resEvents.Add(obj.Key, obj.ToList());
                    }
                    return resEvents;
                }
                else if (eventsObject is JObject)
                {
                    // support new format style
                    // "generic": {
                    // "playerEnter": {
                    //      ... event code
                    //  }
                    // ]
                    JObject genericObj = (JObject)eventsObject;
                    return genericObj.ToObject<Dictionary<string, List<OracleEventObjectJson>>>();
                }
                else
                {
                    IteratorKit.Log.LogError($"Event object does not use a supported type: {eventsObject.GetType()}");
                    return new Dictionary<string, List<OracleEventObjectJson>>();
                }
            }

            /// <summary>
            /// See the events docs for this: <see href="/events.html">Events</see>
            /// </summary>
            public class OracleEventObjectJson
            {

                /// <summary>
                /// ID for this event <see href="/eventIds.html#custom-oracle-only-events"/>
                /// </summary>
                [JsonProperty("event")]
                public string eventId;

                /// <exclude/>
                public string item
                {
                    set { this.eventId = value; }
                    get { return this.eventId; }
                }

                /// <summary>
                /// List of player relationships that this event will player for.
                /// Leave null to play for all player relationship statuses
                /// </summary>
                /// <example>
                /// "relationship": ["normal", "friend"]
                /// or
                /// "relationship": ["angry", "annoyed"]
                /// </example>
                public List<string> relationship;

                /// <summary>
                /// List of slugcats that this event will play for.
                /// The built in slug cats are: White(Survivor), Yellow(Monk), Red(Hunter)
                /// Downpour DLC: Rivulet, Artificer, Saint, Spear, Gourmand, Slugpup, Inv
                /// </summary>
                /// <example>
                /// "for": ["Yellow", "Spear"]
                /// </example>
                [JsonProperty("for")]
                private List<string> forSlugList;

                /// <summary>
                /// <see href="/eventsIds.html#dialog-creatures"/>
                /// </summary>
                [JsonProperty("creatures")]
                private List<string> creaturesInRoomList;

                /// <exclude/>
                public List<SlugcatStats.Name> forSlugcats
                {
                    get
                    {
                        List<SlugcatStats.Name> nameList = new List<SlugcatStats.Name>();
                        if (this.forSlugList == null || this.forSlugList?.Count <= 0)
                        {
                            return nameList;
                        }
                        foreach (string slugcatName in this.forSlugList)
                        {
                            nameList.Add(new SlugcatStats.Name(slugcatName, false));
                        }
                        return nameList;
                    }
                }

                /// <exclude/>
                public List<CreatureTemplate.Type> creaturesInRoom
                {
                    get
                    {
                        List<CreatureTemplate.Type> creatures = new List<CreatureTemplate.Type>();
                        if (this.creaturesInRoomList == null || this.creaturesInRoomList?.Count <= 0)
                        {
                            return creatures;
                        }
                        foreach (string creature in this.creaturesInRoomList)
                        {
                            switch (creature.ToLower())
                            {
                                case "lizards":
                                    creatures.AddRange(allLizardsList);
                                    break;
                                case "vultures":
                                    creatures.AddRange(allVultures);
                                    break;
                                case "longlegs":
                                    creatures.AddRange(allLongLegsList);
                                    break;
                                case "bigcentipedes":
                                    creatures.AddRange(allBigCentipedes);
                                    break;
                                default:
                                    creatures.Add(new CreatureTemplate.Type(creature, false));
                                    break;
                            }


                        }
                        return creatures;
                    }
                }

                /// <exclude/>
                public List<string> getTexts(SlugcatStats.Name forSlugcat)
                {
                    if (this.forSlugcats != null)
                    {
                        if (this.forSlugcats?.Count > 0 && !this.forSlugcats.Contains(forSlugcat))
                        {
                            return null;
                        }
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


                /// <summary>
                /// Delay and hold times
                /// </summary>
                /// <example>
                /// "delay": 5,
                /// "hold": 15
                /// </example>
                public int delay, hold = 10;

                /// <summary>
                /// Used for just one dialog box
                /// </summary>
                /// <example>
                /// "text": "This is dialog 1"
                /// </example>
                public string text = null;

                /// <summary>
                /// Plays a list of dialogs in order. Unless random mode is enabled.
                /// </summary>
                /// <example>
                /// "texts": ["This is dialog 1", "This is dialog 2"]
                /// </example>
                public List<string> texts;

                /// <exclude/>
                public string translateString = null;
                /// <summary>
                /// Pick one dialog randomly instead of reading them in order
                /// </summary>
                /// <example>
                /// "random": true
                /// </example>
                public bool random = false;
                /// <summary>
                /// Sets the gravity value for the current room, usually only 1 or 0. You need to add a ZeroG effect to the room for this to work.
                /// </summary>
                /// <example>
                /// "gravity": 0
                /// </example>
                public float gravity = -50f; // -50f default value keeps gravity at whatever it already is
                /// <summary>
                /// Plays a sound using rainworlds audio name
                /// </summary>
                /// <example>
                /// "sound": "SS_AI_Exit_Work_Mode"
                /// </example>
                public string sound; // links to SoundID

                /// <summary>
                /// Force iterator to move to this position at the start of this event. Uses world coordinates
                /// </summary>
                /// <example>
                /// "moveTo": {"x": 241, "y": 244}
                /// </example>
                public Vector2 moveTo;

                /// <summary>
                /// <see href="/eventIds.html#actions"/>
                /// </summary>
                public string action;
                /// <summary>
                /// Used with action to specify some additional details <see href="/events.html#action-param"/>
                /// </summary>
                /// <example>
                /// "action": "kickPlayerOut",
                /// "actionParam": "SU_test"
                ///</example>
                public string actionParam;

                public ChangePlayerScoreJson score;

                /// <summary>
                /// Dialog text color
                /// </summary>
                /// <example>
                /// "color": {"r": 255, "g": 0, "b": 0, "a": 0}
                /// </example>
                public UnityEngine.Color color = UnityEngine.Color.white;

                /// <summary>
                /// <see href="/eventIds.html#movements"/>
                /// </summary>
                public string movement;
                public int pauseFrames = 0; // only for pebbles

                /// <summary>
                /// Projection screens <see href="/events.html#screens"/>
                /// </summary>
                public List<OracleScreenJson> screens = new List<OracleScreenJson>();




            }

            public class OracleScreenJson
            {
                public string image;
                public int hold;
                public float alpha = 255f;
                public Vector2 pos;
                public float moveSpeed = 50f;
            }

            public class ChangePlayerScoreJson
            {
                /// <summary>
                /// set, add or subtract
                /// </summary>
                public string action; // set, add, subtract
                /// <summary>
                /// Value to change to/by
                /// </summary>
                public int amount;
            }
        }

        /// <exclude />
        public class OverseerJson
        {
            public SpriteDataJson color;
            public List<string> regions;
            public string guideToRoom;
            public int genMin, genMax;
        }

        private static readonly List<CreatureTemplate.Type> allLizardsList = new List<CreatureTemplate.Type>
    {
        CreatureTemplate.Type.LizardTemplate,
        CreatureTemplate.Type.PinkLizard,
        CreatureTemplate.Type.GreenLizard,
        CreatureTemplate.Type.BlueLizard,
        CreatureTemplate.Type.YellowLizard,
        CreatureTemplate.Type.WhiteLizard,
        CreatureTemplate.Type.RedLizard,
        CreatureTemplate.Type.BlackLizard,
        CreatureTemplate.Type.Salamander,
        CreatureTemplate.Type.CyanLizard,
        MoreSlugcatsEnums.CreatureTemplateType.SpitLizard,
        MoreSlugcatsEnums.CreatureTemplateType.EelLizard,
        MoreSlugcatsEnums.CreatureTemplateType.TrainLizard,
        MoreSlugcatsEnums.CreatureTemplateType.ZoopLizard
    };

        private static readonly List<CreatureTemplate.Type> allVultures = new List<CreatureTemplate.Type>
    {
        CreatureTemplate.Type.Vulture,
        CreatureTemplate.Type.KingVulture,
        MoreSlugcatsEnums.CreatureTemplateType.MirosVulture
    };

        private static readonly List<CreatureTemplate.Type> allLongLegsList = new List<CreatureTemplate.Type>
    {
        CreatureTemplate.Type.BrotherLongLegs,
        CreatureTemplate.Type.DaddyLongLegs,
        MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs,
        MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy
    };

        private static readonly List<CreatureTemplate.Type> allBigCentipedes = new List<CreatureTemplate.Type>
    {
        CreatureTemplate.Type.Centipede,
        CreatureTemplate.Type.Centiwing,
        CreatureTemplate.Type.Centiwing,
        MoreSlugcatsEnums.CreatureTemplateType.AquaCenti
    };
    }




    public class OracleJsonTilePos
    {
        public int x, y;
    }

    //public class DictOrArrayConverter<T> : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return (objectType == typeof(List<T>) || objectType == typeof(Dictionary<string, T>));
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        JToken jToken = JToken.Load(reader);
    //        switch (jToken.Type)
    //        {
    //            case JTokenType.Array:
    //                jToken.ToDictionary<>
    //                return jToken.ToObject<List<T>>();
    //            case JTokenType.Object:
    //                return jToken.ToObject<Dictionary<string, T>>();
    //            case JTokenType.Null:
    //                return null;
    //            default:
    //                throw new JsonReaderException($"Unsupported object type: {jToken.Type}");
    //        }
    //    }

    //    public override bool CanWrite => false;

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}