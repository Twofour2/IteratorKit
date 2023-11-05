using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorMod.CM_Oracle;
using static IteratorMod.CM_Oracle.CMConversation;
using static IteratorMod.CM_Oracle.OracleJSON.OracleEventsJson;
using MoreSlugcats;
using IteratorMod.CustomPearls;
using static IteratorMod.CustomPearls.DataPearlJson;
using System.Text.RegularExpressions;

namespace IteratorMod.SLOracle
{
    public class SLConversation
    {
        public OracleJSON oracleJSON;
        public OracleJSON.OracleEventsJson oracleDialog;
        public CMConversation conversation;

        public static List<string> nonModdedCats = new List<string>()
        {
            SlugcatStats.Name.White.value,
            SlugcatStats.Name.Yellow.value,
            SlugcatStats.Name.Red.value,
            SlugcatStats.Name.Night.value,
            MoreSlugcatsEnums.SlugcatStatsName.Rivulet.value,
            MoreSlugcatsEnums.SlugcatStatsName.Artificer.value,
            MoreSlugcatsEnums.SlugcatStatsName.Saint.value,
            MoreSlugcatsEnums.SlugcatStatsName.Spear.value,
            MoreSlugcatsEnums.SlugcatStatsName.Gourmand.value,
            MoreSlugcatsEnums.SlugcatStatsName.Slugpup.value,
            MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel.value
        };

        Conversation.ID customSlug = new Conversation.ID("customSlug", true);

        public SLConversation(OracleJSON oracleJSON) {
            this.oracleJSON = oracleJSON;
            this.oracleDialog = oracleJSON.events;
            SLConversation.LogAllActionsAndMovements();
        }

        public void ApplyHooks()
        {
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += MoonConversationAddEvents;
            On.SLOracleBehaviorHasMark.InitateConversation += SLInitiateConversaion;
        }

        private void SLInitiateConversaion(On.SLOracleBehaviorHasMark.orig_InitateConversation orig, SLOracleBehaviorHasMark self)
        {
            orig(self);
            // check for custom stuffs
            if (!nonModdedCats.Contains(self.oracle.room.game.StoryCharacter.value))
            {
                IteratorKit.Logger.LogWarning("Non standard slugcat");
                self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(customSlug, self, SLOracleBehaviorHasMark.MiscItemType.NA);
            }
            
        }

        private void MoonConversationAddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            // warning: pebbles in artificer calls MoonConversation. this is why interface owner points to OracleBehvior.
            // this leads to the weird situation where past moon calls this though SSOracleBehavior
            OracleBehavior behaviorHasMark = (self.interfaceOwner as OracleBehavior);

            if (!this.oracleJSON.forSlugcats.Contains(behaviorHasMark.oracle.room.game.StoryCharacter))
            {
                IteratorKit.Logger.LogInfo($"Oracle dialog override not avalible for {behaviorHasMark.oracle.room.game.StoryCharacter.value}");
                orig(self);
                return;
            }

            IteratorKit.Logger.LogWarning("Messing with convo code for moon");
            string eventId = this.convoIdToEventId(self.id.value);
            CMDialogType dialogType = CMDialogType.Generic;
            if (eventId == "moonMiscItem")
            {
                dialogType = CMDialogType.Item;
                eventId = convoIdToEventId(self.describeItem.value);
            }
            if (self.describeItem == SLOracleBehaviorHasMark.MiscItemType.NA && eventId.ToLower().Contains("pearl"))
            {
                IteratorKit.Logger.LogWarning("Detected pearl convo");
                dialogType = CMDialogType.Pearls;
                eventId = eventId.Replace("moonPearl", "");
            }

            bool hasCustomEvents = this.AddCustomEvents(self, dialogType, eventId, behaviorHasMark);
            if (!hasCustomEvents)
            {
                orig(self);
            }
            else
            {
                IteratorKit.Logger.LogInfo($"Overriding conversation {eventId} with custom events");
            }
            
        }

        public static void CustomPearlAddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            CustomPearls.CustomPearls.DataPearlRelationStore dataPearlRelation = CustomPearls.CustomPearls.pearlJsonDict.FirstOrDefault(x => x.Value.convId == self.id).Value;
            if (dataPearlRelation != null)
            {
                // this handles events for moon, pastMoon and pebbles since all of the iterator code converges here.
                OracleEventObjectJson pearlJson = dataPearlRelation.pearlJson.dialogs.getDialogsForOracle(self.interfaceOwner as OracleBehavior);
                if (pearlJson == null && dataPearlRelation.pearlJson.dialogs.defaultDiags != null)
                {
                    pearlJson = dataPearlRelation.pearlJson.dialogs.defaultDiags;
                }

                if (pearlJson != null)
                {
                    foreach (string text in pearlJson.getTexts((self.interfaceOwner as OracleBehavior).oracle.room.game.GetStorySession.saveStateNumber))
                    {
                        self.events.Add(new Conversation.TextEvent(self, pearlJson.delay, text, pearlJson.hold));
                    }
                }
                else
                {
                    IteratorKit.Logger.LogError($"Failed to load dialog texts for this oracle.");
                }
                
            }
            else
            {
                orig(self);
            }
        }

        private string convoIdToEventId(string convoId)
        {
            convoId = convoId.Replace("_", "");
            convoId = convoId.Substring(0, 1).ToLower() + convoId.Substring(1);
            IteratorKit.Logger.LogInfo($"Converted convo id to {convoId}");
            return convoId;
        }

        private bool AddCustomEvents(SLOracleBehaviorHasMark.MoonConversation self, CMDialogType eventType, string eventId, OracleBehavior oracleBehavior)
        {
            IteratorKit.Logger.LogWarning($"Adding events for {eventType}: {eventId}");
            List<OracleEventObjectJson> dialogList = this.oracleDialog.generic;

            switch (eventType)
            {
                case CMDialogType.Generic:
                    dialogList = this.oracleDialog.generic;
                    break;
                case CMDialogType.Pearls:
                    dialogList = this.oracleDialog.pearls;
                    break;
                case CMDialogType.Item:
                    dialogList = this.oracleDialog.items;
                    break;
                default:
                    IteratorKit.Logger.LogError("Tried to get non-existant dialog type. using generic");
                    dialogList = this.oracleDialog.generic;
                    break;
            }

            List<OracleEventObjectJson> dialogData = dialogList?.FindAll(x => x.eventId == eventId);
            if (dialogData.Count > 0)
            {
                foreach (OracleEventObjectJson item in dialogData)
                {
                    if (!item.forSlugcats.Contains(oracleBehavior.oracle.room.game.GetStorySession.saveStateNumber))
                    {
                        continue; // skip as this one isnt for us
                    }

                    foreach (string text in item.getTexts((self.interfaceOwner as OracleBehavior).oracle.room.game.GetStorySession.saveStateNumber))
                    {
                        self.events.Add(new Conversation.TextEvent(self, item.delay, text, item.hold));
                    }
                }
                return true;
            }
            return false;
        }

        public static string camelCaseToPascal(string input)
        {
            string camelized = Regex.Replace(input, @"\b\p{Ll}", match => match.Value.ToUpper());
            return Regex.Replace(camelized, @"_\p{Ll}", match => match.Value.ToUpper());
        }
        public static string pascalToCamel(string input)
        {
            string camelized = Regex.Replace(input, @"\b\p{Lu}", match => match.Value.ToLower());
            return Regex.Replace(camelized, @"_\p{Lu}", match => match.Value.ToLower());
        }

        public static void LogAllActionsAndMovements()
        {
            IteratorKit.Logger.LogInfo("All Events:");
            foreach(string value in Conversation.ID.values.entries)
            {
                IteratorKit.Logger.LogInfo(pascalToCamel(value));
            }
            IteratorKit.Logger.LogInfo("Movements:");
            foreach (string value in SLOracleBehavior.MovementBehavior.values.entries)
            {
                IteratorKit.Logger.LogInfo(pascalToCamel(value));
            }
        }

    }
}
