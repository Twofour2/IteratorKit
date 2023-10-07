using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorMod.CM_Oracle;
using static IteratorMod.CM_Oracle.CMConversation;
using static IteratorMod.CM_Oracle.OracleJSON.OracleDialogJson;
using MoreSlugcats;

namespace IteratorMod.SLOracle
{
    public class SLConversation
    {
        public OracleJSON oracleJSON;
        public OracleJSON.OracleDialogJson oracleDialog;
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
            this.oracleDialog = oracleJSON.dialogs;
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
            IteratorMod.Logger.LogWarning(self.oracle.room.game.StoryCharacter.value);
            if (!nonModdedCats.Contains(self.oracle.room.game.StoryCharacter.value))
            {
                IteratorMod.Logger.LogWarning("Non standard slugcat");
                self.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(customSlug, self, SLOracleBehaviorHasMark.MiscItemType.NA);
            }
            
        }

        private void MoonConversationAddEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            SLOracleBehaviorHasMark behaviorHasMark = (self.interfaceOwner as SLOracleBehaviorHasMark);

            if (!this.oracleJSON.forSlugcats.Contains(behaviorHasMark.oracle.room.game.StoryCharacter))
            {
                IteratorMod.Logger.LogInfo($"Oracle dialog override not avalible for {behaviorHasMark.oracle.room.game.StoryCharacter.value}");
                orig(self);
                return;
            }

            IteratorMod.Logger.LogWarning("Messing with convo code for moon");
            string eventId = this.convoIdToEventId(self.id.value);
            CMDialogType dialogType = CMDialogType.Generic;
            if (eventId == "moonMiscItem")
            {
                dialogType = CMDialogType.Item;
                eventId = convoIdToEventId(self.describeItem.value);
            }
            if (self.describeItem == SLOracleBehaviorHasMark.MiscItemType.NA && eventId.ToLower().Contains("pearl"))
            {
                IteratorMod.Logger.LogWarning("Detected pearl convo");
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
                IteratorMod.Logger.LogInfo($"Overriding conversation {eventId} with custom events");
            }
            
        }

        private string convoIdToEventId(string convoId)
        {
            convoId = convoId.Replace("_", "");
            convoId = convoId.Substring(0, 1).ToLower() + convoId.Substring(1);
            IteratorMod.Logger.LogInfo($"Converted convo id to {convoId}");
            return convoId;
        }

        private bool AddCustomEvents(SLOracleBehaviorHasMark.MoonConversation self, CMDialogType eventType, string eventId, OracleBehavior oracleBehavior)
        {
            IteratorMod.Logger.LogWarning($"Adding events for {eventType}: {eventId}");
            List<OracleDialogObjectJson> dialogList = this.oracleDialog.generic;

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
                    IteratorMod.Logger.LogError("Tried to get non-existant dialog type. using generic");
                    dialogList = this.oracleDialog.generic;
                    break;
            }

            List<OracleDialogObjectJson> dialogData = dialogList?.FindAll(x => x.eventId == eventId);
            if (dialogData.Count > 0)
            {
                foreach (OracleDialogObjectJson item in dialogData)
                {
                    if (!item.forSlugcats.Contains(oracleBehavior.oracle.room.game.GetStorySession.saveStateNumber))
                    {
                        continue; // skip as this one isnt for us
                    }

                    if (item.random)
                    {
                        int rand = UnityEngine.Random.Range(0, item.texts.Count);
                        self.events.Add(new Conversation.TextEvent(self, item.delay, item.texts[rand], item.hold));
                    }
                    else if ((item.texts?.Count() ?? 0) > 0)
                    {
                        foreach (string text in item.texts)
                        {
                            self.events.Add(new Conversation.TextEvent(self, item.delay, text, item.hold));
                        }
                    }
                    else
                    {
                        self.events.Add(new Conversation.TextEvent(self, item.delay, item.text, item.hold));
                    }
                }
                return true;
            }
            return false;
        }

    }
}
