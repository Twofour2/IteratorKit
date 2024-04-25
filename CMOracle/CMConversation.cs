using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HUD;
using static IteratorKit.CMOracle.OracleJSON.OracleEventsJson;

namespace IteratorKit.CMOracle
{
    public class CMConversation : Conversation
    {
        public CMOracleBehavior owner;
        public string eventId;
        public CMDialogCategory eventCategory;
        public OracleJSON.OracleEventsJson dialogJson {
            get { return this.owner.oracleJson.events; }
        }

        public DataPearl.AbstractDataPearl.DataPearlType pearlType;
        public bool resumeConvFlag = false;


        public enum CMDialogCategory
        {
            Generic,
            Pearls,
            Items
        }

        public CMConversation(CMOracleBehavior owner, CMDialogCategory eventCategory, string eventId, DataPearl.AbstractDataPearl.DataPearlType pearlType = null) : base(owner, Conversation.ID.None, owner.dialogBox)
        {
            this.owner = owner;
            this.eventCategory = eventCategory;
            this.eventId = eventId;
            this.pearlType = pearlType;
            this.AddEvents();
        }

        public override void AddEvents()
        {
            IteratorKit.Log.LogInfo($"Adding events for {this.eventCategory} {this.eventId}");
            Dictionary<string, List<OracleEventObjectJson>> eventList = this.dialogJson.genericEvents;
            switch (this.eventCategory)
            {
                case CMDialogCategory.Generic:
                    eventList = this.dialogJson.genericEvents;
                    break;
                case CMDialogCategory.Pearls:
                    eventList = this.dialogJson.pearlEvents;
                    break;
                case CMDialogCategory.Items:
                    eventList = this.dialogJson.itemEvents;
                    break;
            }

            if (!eventList.TryGetValue(this.eventId, out List<OracleEventObjectJson> eventDataList))
            {
                if (this.eventCategory == CMDialogCategory.Pearls)
                {
                    IteratorKit.Log.LogInfo($"Fallback to collections code for {this.eventId}");
                    //if (this.TryLoadCustomPearls())
                    //{
                    //    return;
                    //}
                    if (this.TryLoadFallbackPearls())
                    {
                        return;
                    }
                    IteratorKit.Log.LogWarning($"Failed to find event {this.eventCategory} {this.eventId}");
                }
            }

            if (eventDataList.Count == 0)
            {
                IteratorKit.Log.LogWarning($"Provided with empty event list for {this.eventId}");
                return;
            }

            foreach(OracleEventObjectJson eventData in  eventDataList)
            {
                if (!HasMatchingCreatureInRoom(eventData.creaturesInRoom)) { // returns true on empty/null list (no check), or matching creature existing in room
                    IteratorKit.Log.LogInfo($"Skipping event {eventData.eventId} due to creature requirement.");
                    continue;
                }

                if(eventData.forSlugcats != null && eventData.forSlugcats?.Count > 0)
                {
                    if (!eventData.forSlugcats.Contains(this.owner.oracle.room.game.GetStorySession.saveStateNumber))
                    {
                        IteratorKit.Log.LogInfo($"Skipping event {eventData.eventId} as it is not for the current story session");
                        continue;
                    }
                }

                if (eventData.action != null)
                {
                    this.events.Add(new CMOracleActionEvent(this, eventData)); // insert action event so it runs before the dialog starts
                }

                if (!((StoryGameSession)this.owner.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                {
                    return; // dont run any dialogs until we have given the player the mark.
                }

                IteratorKit.Log.LogInfo($"Event {eventData.eventId} passed all dialog checks");

                foreach (string text in eventData.getTexts(this.owner.oracle.room.game.StoryCharacter))
                {
                    if (text != null)
                    {
                        this.events.Add(new CMOracleTextEvent(this, this.ReplaceParts(text), eventData));
                    }
                }

                // I think this prevents a crash if we don't add an event? not sure.
                if (eventData.texts == null && eventData.text == null && eventData.action == null)
                {
                    IteratorKit.Log.LogInfo($"Event {eventData.eventId} has no provided action/text. adding dummy event");
                    this.events.Add(new CMOracleActionEvent(this, eventData));
                }


            }
        }

        public override void Update()
        {
            if (this.paused) return;
            if (this.events.Count == 0)
            {
                this.Destroy();
                return;
            }
            this.events[0].Update(); // run update on the current dialog
            if (this.events[0].IsOver)
            {
                this.events.RemoveAt(0); // delete the event
            }
        }

        public void InterruptQuickHide()
        {
            this.dialogBox.messages = new List<DialogBox.Message>();
        }

        public bool TryLoadCustomPearls()
        {
            throw new NotImplementedException();
        }

        public bool TryLoadFallbackPearls()
        {
            if (this.pearlType == null || this.owner.oracle.OracleJson().pearlFallback == null)
            {
                return false;
            }
            // is not a custom pearl. switch which set of pearl dialogs to use, null save file uses default moon dialogs, so any value except below will use moons dialogs.
            SlugcatStats.Name saveFileName = null;
            switch (this.owner.oracle.OracleJson().pearlFallback.ToLower())
            {
                case "pebbles":
                    saveFileName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
                    break;
                case "pastmoon":
                    saveFileName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear;
                    break;
                case "futuremoon":
                    saveFileName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint;
                    break;
            }
            int id = MoreSlugcats.CollectionsMenu.DataPearlToFileID(this.pearlType); // very useful method
            if (this.pearlType == MoreSlugcats.MoreSlugcatsEnums.DataPearlType.Spearmasterpearl)
            {
                id = 106;
            }
            this.LoadEventsFromFile(id, saveFileName, false, 0);
            return true;
        }


        public bool HasMatchingCreatureInRoom(List<CreatureTemplate.Type> creaturesInRoom)
        {
            if (creaturesInRoom == null || creaturesInRoom.Count == 0) return true; // given empty list, skip this check
            foreach (AbstractCreature abstractCreature in this.owner.oracle.room.abstractRoom.creatures)
            {
                if (!abstractCreature.state.alive)
                {
                    continue; // skip if dead
                }
                if (creaturesInRoom.Contains(abstractCreature.creatureTemplate.type))
                {
                    IteratorKit.Log.LogInfo($"Found creature in room {abstractCreature.creatureTemplate.type}");
                    return true;
                }
            }
            return false;
        }

        public string ReplaceParts(string s)
        {
            s = Regex.Replace(s, "<PLAYERNAME>", this.NameForPlayer(false));
            s = Regex.Replace(s, "<CAPPLAYERNAME>", this.NameForPlayer(true));
            s = Regex.Replace(s, "<PlayerName>", this.NameForPlayer(false));
            s = Regex.Replace(s, "<CapPlayerName>", this.NameForPlayer(true));
            return s;
        }

        public string NameForPlayer(bool capitalized)
        {
            return "little creature";
        }
    }
}
