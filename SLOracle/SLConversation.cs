//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using IteratorKit.CMOracle;
//using static IteratorKit.CMOracle.CMConversation;
//using static IteratorKit.CMOracle.OracleJData.OracleEventsJData;
//using MoreSlugcats;
//using IteratorKit.CustomPearls;
//using static IteratorKit.CustomPearls.DataPearlJson;
//using System.Text.RegularExpressions;

//namespace IteratorKit.SLOracle
//{
//    public class SLConversation
//    {
//        public static List<OracleJData> slOracleJsons;
//      //  public static OracleJData.OracleEventsJData oracleDialog;
//        public static CMConversation conversation;

//        public static List<string> nonModdedCats = new List<string>()
//        {
//            SlugcatStats.Name.White.value,
//            SlugcatStats.Name.Yellow.value,
//            SlugcatStats.Name.Red.value,
//            SlugcatStats.Name.Night.value,
//            MoreSlugcatsEnums.SlugcatStatsName.Rivulet.value,
//            MoreSlugcatsEnums.SlugcatStatsName.Artificer.value,
//            MoreSlugcatsEnums.SlugcatStatsName.Saint.value,
//            MoreSlugcatsEnums.SlugcatStatsName.Spear.value,
//            MoreSlugcatsEnums.SlugcatStatsName.Gourmand.value,
//            MoreSlugcatsEnums.SlugcatStatsName.Slugpup.value,
//            MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel.value
//        };

//        static Conversation.ID customSlug = new Conversation.ID("customSlug", true);


//        public static void ApplyHooks()
//        {
//            IteratorKit.Log.LogInfo("SL Apply hooks");

//            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversationAddEvents;
//            On.SLOracleBehaviorHasMark.InitateConversation += SLInitiateConversaion;
//        }

//        public static void RemoveHooks()
//        {
//            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents -= MoonConversationAddEvents;
//            On.SLOracleBehaviorHasMark.InitateConversation -= SLInitiateConversaion;
//        }

//        private static void SLInitiateConversaion(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
//        {
//            orig(self);
//            // check for custom stuffs
//            if (!nonModdedCats.Contains(self.oracle.room.game.StoryCharacter.value))
//            {
//                IteratorKit.Log.LogWarning("Non standard slugcat");
//                self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(customSlug, self, SLOracleBehaviorHasMark.MiscItemType.NA);
//            }

//        }

//        private static void MoonConversationAddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
//        {
//            // warning: pebbles in artificer calls MoonConversation. this is why interface owner points to OracleBehvior.
//            // this leads to the weird situation where past moon calls this though SSOracleBehavior
//            OracleBehavior behaviorHasMark = (self.interfaceOwner as OracleBehavior);
//            OracleJData oracleJson = null;
//            foreach (OracleJData checkOracleJson in slOracleJsons)
//            {
//                if (checkOracleJson.forSlugcats.Contains(behaviorHasMark.oracle.room.game.StoryCharacter))
//                {
//                    oracleJson = checkOracleJson;
//                    break;
//                }
//            }
//            if (oracleJson.events.)

//            if (!oracleJSON.forSlugcats.Contains(behaviorHasMark.oracle.room.game.StoryCharacter))
//            {
//                IteratorKit.Log.LogInfo($"Oracle dialog override not avalible for {behaviorHasMark.oracle.room.game.StoryCharacter.value}");
//                orig(self);
//                return;
//            }

//            IteratorKit.Log.LogWarning("Messing with convo code for moon");
//            string eventId = convoIdToEventId(self.id.value);
//            CMDialogCategory dialogType = CMDialogCategory.Generic;
//            if (eventId == "moonMiscItem")
//            {
//                dialogType = CMDialogCategory.Items;
//                eventId = convoIdToEventId(self.describeItem.value);
//            }
//            if (self.describeItem == SLOracleBehaviorHasMark.MiscItemType.NA && eventId.ToLower().Contains("pearl"))
//            {
//                IteratorKit.Log.LogWarning("Detected pearl convo");
//                dialogType = CMDialogCategory.Pearls;
//                eventId = eventId.Replace("moonPearl", "");
//            }

//            bool hasCustomEvents = AddCustomEvents(self, dialogType, eventId, behaviorHasMark);
//            if (!hasCustomEvents)
//            {
//                orig(self);
//            }
//            else
//            {
//                IteratorKit.Log.LogInfo($"Overriding conversation {eventId} with custom events");
//            }

//        }

//        public static void CustomPearlAddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
//        {
//            CustomPearls.CustomPearls.DataPearlRelationStore dataPearlRelation = CustomPearls.CustomPearls.pearlJsonDict.FirstOrDefault(x => x.Value.convId == self.id).Value;
//            if (dataPearlRelation != null)
//            {
//                // this handles events for moon, pastMoon and pebbles since all of the iterator code converges here.
//                OracleEventObjectJData pearlJson = dataPearlRelation.pearlJson.dialogs.getDialogsForOracle(self.interfaceOwner as OracleBehavior);
//                if (pearlJson == null && dataPearlRelation.pearlJson.dialogs.defaultDiags != null)
//                {
//                    pearlJson = dataPearlRelation.pearlJson.dialogs.defaultDiags;
//                }

//                if (pearlJson != null)
//                {
//                    foreach (string text in pearlJson.getTexts((self.interfaceOwner as OracleBehavior).oracle.room.game.GetStorySession.saveStateNumber))
//                    {
//                        self.events.Add(new Conversation.TextEvent(self, pearlJson.delay, text, pearlJson.hold));
//                    }
//                }
//                else
//                {
//                    IteratorKit.Log.LogError($"Failed to load dialog texts for this oracle.");
//                }

//            }
//            else
//            {
//                orig(self);
//            }
//        }

//        private static string convoIdToEventId(string convoId)
//        {
//            convoId = convoId.Replace("_", "");
//            convoId = convoId.Substring(0, 1).ToLower() + convoId.Substring(1);
//            IteratorKit.Log.LogInfo($"Converted convo id to {convoId}");
//            return convoId;
//        }

//        private static bool AddCustomEvents(SLOracleBehaviorHasMark.MoonConversation self, CMDialogCategory eventType, string eventId, OracleBehavior oracleBehavior)
//        {
//            IteratorKit.Log.LogWarning($"Adding events for {eventType}: {eventId}");

//            Dictionary<string, List<OracleEventObjectJData>> dialogList = oracleDialog.genericEvents;

//            switch (eventType)
//            {
//                case CMDialogCategory.Generic:
//                    dialogList = oracleDialog.genericEvents;
//                    break;
//                case CMDialogCategory.Pearls:
//                    dialogList = oracleDialog.pearlEvents;
//                    break;
//                case CMDialogCategory.Items:
//                    dialogList = oracleDialog.itemEvents;
//                    break;
//                default:
//                    IteratorKit.Log.LogError("Tried to get non-existant dialog type. using generic");
//                    dialogList = oracleDialog.genericEvents;
//                    break;
//            }
//            List<OracleEventObjectJData> dialogData = dialogList[eventId.ToLower()];

//            if (dialogData.Count > 0)
//            {
//                foreach (OracleEventObjectJData item in dialogData)
//                {
//                    if (item.forSlugcats != null)
//                    {
//                        if (item.forSlugcats.Count > 0 && !item.forSlugcats.Contains(oracleBehavior.oracle.room.game.GetStorySession.saveStateNumber))
//                        {
//                            continue; // skip as this one isnt for us
//                        }
//                    }

//                    foreach (string text in item.getTexts((self.interfaceOwner as OracleBehavior).oracle.room.game.GetStorySession.saveStateNumber))
//                    {
//                        self.events.Add(new Conversation.TextEvent(self, item.delay, text, item.hold));
//                    }
//                }
//                return true;
//            }
//            return false;
//        }

//        public static string camelCaseToPascal(string input)
//        {
//            string camelized = Regex.Replace(input, @"\b\p{Ll}", match => match.Value.ToUpper());
//            return Regex.Replace(camelized, @"_\p{Ll}", match => match.Value.ToUpper());
//        }
//        public static string pascalToCamel(string input)
//        {
//            string camelized = Regex.Replace(input, @"\b\p{Lu}", match => match.Value.ToLower());
//            return Regex.Replace(camelized, @"_\p{Lu}", match => match.Value.ToLower());
//        }

//        public static void LogAllActionsAndMovements()
//        {
//            IteratorKit.Log.LogInfo("All Events:");
//            foreach (string value in Conversation.ID.values.entries)
//            {
//                IteratorKit.Log.LogInfo(pascalToCamel(value));
//            }
//            IteratorKit.Log.LogInfo("Movements:");
//            foreach (string value in SLOracleBehavior.MovementBehavior.values.entries)
//            {
//                IteratorKit.Log.LogInfo(pascalToCamel(value));
//            }
//        }

//    }
//}
