using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IteratorMod.CMOracle;
using static IteratorMod.CMOracle.CMConversation;
using static IteratorMod.CMOracle.OracleJSON.OracleEventsJson;
using MoreSlugcats;
using IteratorMod.CMOracle;
using static IteratorMod.CMOracle.CMOracleBehavior;
using System.Text.RegularExpressions;
using static IteratorMod.CustomPearls.DataPearlJson;
using static System.Net.Mime.MediaTypeNames;

namespace IteratorMod.SLOracle
{
    public class SSConversation
    {
        public OracleJSON oracleJSON;
        public OracleJSON.OracleEventsJson oracleEvents;
        public CMConversation conversation;
        public SSOracleBehavior owner;

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

        public static List<String> specialEvents = new List<String>() { "karma", "panic", "resync", "tag", "unlock" };

        Conversation.ID customSlug = new Conversation.ID("customSlug", true);

        public SSConversation(OracleJSON oracleJSON) {
            this.oracleJSON = oracleJSON;
            this.oracleEvents = oracleJSON.events;
        }

        public void ApplyHooks()
        {
            IteratorKit.Logger.LogWarning("applying pebbles hooks");
            On.SSOracleBehavior.PebblesConversation.AddEvents += PebblesConversationAddEvents;
            On.SSOracleBehavior.InitateConversation += SSInitiateConversation;
        }


        private void SSInitiateConversation(On.SSOracleBehavior.orig_InitateConversation orig, SSOracleBehavior self, Conversation.ID convoId, SSOracleBehavior.ConversationBehavior convBehav)
        {
            orig(self, convoId, convBehav);
            // check for custom stuffs
            if (!nonModdedCats.Contains(self.oracle.room.game.StoryCharacter.value))
            {
                IteratorKit.Logger.LogWarning($"Non standard slugcat {self.oracle.room.game.StoryCharacter.value}");
                self.conversation = new SSOracleBehavior.PebblesConversation(self, convBehav, customSlug, self.dialogBox);
            }

        }

        private void PebblesConversationAddEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            IteratorKit.Logger.LogWarning("Messing with convo code for pebbles");
            SSOracleBehavior behaviorHasMark = (self.interfaceOwner as SSOracleBehavior);
            this.owner = behaviorHasMark;

            if (!this.oracleJSON.forSlugcats.Contains(behaviorHasMark.oracle.room.game.StoryCharacter))
            {
                IteratorKit.Logger.LogInfo($"Oracle dialog override not availible for {behaviorHasMark.oracle.room.game.StoryCharacter.value}. Is availible for {String.Join(", ", this.oracleJSON.forSlugcats.ConvertAll<string>(x => x.value))}");
                List<SlugcatStats.Name> nameList = Expedition.ExpeditionData.GetPlayableCharacters();
                foreach (SlugcatStats.Name name in nameList)
                {
                    IteratorKit.Logger.LogInfo(name);
                }
                orig(self);
                return;
            }

            IteratorKit.Logger.LogWarning("Messing with convo code for pebbles");
            string eventId = this.convoIdToEventId(self.id.value);
            CMDialogType dialogType = CMDialogType.Generic;


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

        private string convoIdToEventId(string convoId)
        {
            convoId = convoId.Replace("_", "");
            convoId = convoId.Substring(0, 1).ToLower() + convoId.Substring(1);
            IteratorKit.Logger.LogInfo($"Converted convo id to {convoId}");
            return convoId;
        }

        private bool AddCustomEvents(SSOracleBehavior.PebblesConversation self, CMDialogType eventType, string eventId, SSOracleBehavior oracleBehavior)
        {
            IteratorKit.Logger.LogWarning($"Adding events for {eventType}: {eventId}");
            List<OracleEventObjectJson> dialogList = this.oracleEvents.generic;

            switch (eventType)
            {
                case CMDialogType.Generic:
                    dialogList = this.oracleEvents.generic;
                    break;
                case CMDialogType.Pearls:
                    dialogList = this.oracleEvents.pearls;
                    break;
                case CMDialogType.Item:
                    dialogList = this.oracleEvents.items;
                    break;
                default:
                    IteratorKit.Logger.LogError("Tried to get non-existant dialog type. using generic");
                    dialogList = this.oracleEvents.generic;
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

                    if (item.pauseFrames > 0)
                    {
                        self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, item.pauseFrames));
                    }

                    if (item.action != null)
                    {
                        IteratorKit.Logger.LogInfo($"Check for pebs action {item.action}");
                        if (SSConversation.specialEvents.Contains(item.action))
                        {
                            IteratorKit.Logger.LogInfo("Action is special event.");
                            self.events.Add(new Conversation.SpecialEvent(self, item.delay, item.action));
                        }
                        else
                        {
                            string action = camelCaseToPascal(item.action);
                            IteratorKit.Logger.LogInfo($"Action is action event {action}");
                            if(SSOracleBehavior.Action.TryParse(oracleBehavior.action.enumType, action, true, out ExtEnumBase result))
                            {
                                IteratorKit.Logger.LogInfo("action is valid");
                                self.events.Add(new SSOracleActionEvent(this, self, item.delay, (SSOracleBehavior.Action)result, oracleBehavior, item));
                            }
                            else
                            {
                                IteratorKit.Logger.LogError($"Failed to parse action {item.action}");
                            }
                            
                        }
                        
                    }

                    if (item.random)
                    {
                        int rand = UnityEngine.Random.Range(0, item.texts.Count);
                        self.events.Add(new SSOracleTextEvent(self, GetRandomDialog(item), item, oracleBehavior));
                    }
                    else if ((item.texts?.Count() ?? 0) > 0)
                    {
                        foreach (string text in item.texts)
                        {
                            self.events.Add(new SSOracleTextEvent(self, text, item, oracleBehavior));
                        }
                    }
                    else
                    {
                        self.events.Add(new SSOracleTextEvent(self, item.text, item, oracleBehavior));
                    }
                }
                return true;
            }
            return false;
        }

        private string GetRandomDialog(OracleEventObjectJson dialogData)
        {
            return dialogData.texts[UnityEngine.Random.Range(0, dialogData.texts.Count())];
        }

        public static void OnEventActivate(SSOracleBehavior owner, Conversation.DialogueEvent dialogueEvent, OracleEventObjectJson dialogData)
        {
            if (dialogData.movement != null)
            {
                string movement = camelCaseToPascal(dialogData.movement);
                IteratorKit.Logger.LogWarning($"Change movement to {movement}");
                if (ExtEnumBase.TryParse(owner.movementBehavior.enumType, movement, true, out ExtEnumBase tmpMovement))
                {
                    owner.movementBehavior = (SSOracleBehavior.MovementBehavior)tmpMovement;
                }
                else
                {
                    IteratorKit.Logger.LogError($"Invalid movement option provided: {movement}");
                }

            }
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
            IteratorKit.Logger.LogInfo("Actions:");
            foreach (string value in SSOracleBehavior.Action.values.entries)
            {
                IteratorKit.Logger.LogInfo(pascalToCamel(value));
            }
            IteratorKit.Logger.LogInfo("Movements:");
            foreach (string value in SSOracleBehavior.MovementBehavior.values.entries)
            {
                IteratorKit.Logger.LogInfo(pascalToCamel(value));
            }
        }

    }

    public class SSOracleActionEvent : Conversation.DialogueEvent
    {
        SSOracleBehavior behavior;
        SSOracleBehavior.Action action;
        SSConversation customOwner;
        OracleEventObjectJson dialogData;

        public SSOracleActionEvent(SSConversation customOwner, Conversation owner, int initialWait, SSOracleBehavior.Action action, SSOracleBehavior oracleBehavior, OracleEventObjectJson dialogData) : base(owner, initialWait)
        {
            this.behavior = oracleBehavior;
            this.action = action;
            this.customOwner = customOwner;
            this.dialogData = dialogData;
        }

        public override void Activate()
        {
            base.Activate();
            IteratorKit.Logger.LogInfo($"Triggering action ${action}");
            this.behavior.NewAction(action);
            SSConversation.OnEventActivate(behavior, this, dialogData); // get owner to run addit checks
        }
    }

    public class SSOracleTextEvent : Conversation.TextEvent
    {
        OracleEventObjectJson dialogData;
        SSOracleBehavior behavior;

        public SSOracleTextEvent(Conversation owner, string text, OracleEventObjectJson dialogData, SSOracleBehavior behavior) : base(owner, dialogData.delay, text, dialogData.hold)
        {
            this.dialogData = dialogData;
            this.behavior = behavior;
        }

        public override void Activate()
        {
            base.Activate();
            SSConversation.OnEventActivate(behavior, this, dialogData); // get owner to run addit checks
        }
    }
}
