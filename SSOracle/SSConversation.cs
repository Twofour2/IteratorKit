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
    public class SSConversation
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

        public SSConversation(OracleJSON oracleJSON) {
            this.oracleJSON = oracleJSON;
            this.oracleDialog = oracleJSON.dialogs;
        }

        public void ApplyHooks()
        {
            IteratorMod.Logger.LogWarning("applying pebbles hooks");
            On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversationAddEvents;
            On.SSOracleBehavior.InitateConversation += SSInitiateConversation;
        }


        private void SSInitiateConversation(On.SSOracleBehavior.orig_InitateConversation orig, SSOracleBehavior self, Conversation.ID convoId, SSOracleBehavior.ConversationBehavior convBehav)
        {
            orig(self, convoId, convBehav);
            // check for custom stuffs
            IteratorMod.Logger.LogWarning(self.oracle.room.game.StoryCharacter.value);
            if (!nonModdedCats.Contains(self.oracle.room.game.StoryCharacter.value))
            {
                IteratorMod.Logger.LogWarning("Non standard slugcat");
               // self.conversation = new SSOracleBehavior.PebblesConversation(customSlug, self);
            }
            
        }

        private void PebblesConversationAddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            IteratorMod.Logger.LogWarning("Messing with convo code for pebbles");
            SSOracleBehavior behaviorHasMark = (self.interfaceOwner as SSOracleBehavior);

            if (!this.oracleJSON.forSlugcats.Contains(behaviorHasMark.oracle.room.game.StoryCharacter))
            {
                IteratorMod.Logger.LogInfo($"Oracle dialog override not avalible for {behaviorHasMark.oracle.room.game.StoryCharacter.value}");
                orig(self);
                return;
            }

            IteratorMod.Logger.LogWarning("Messing with convo code for pebbles");
            string eventId = this.convoIdToEventId(self.id.value);
            CMDialogType dialogType = CMDialogType.Generic;


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

        private bool AddCustomEvents(SSOracleBehavior.PebblesConversation self, CMDialogType eventType, string eventId, OracleBehavior oracleBehavior)
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
