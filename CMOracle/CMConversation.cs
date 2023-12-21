using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BepInEx.Logging;
using HUD;
using IL.MoreSlugcats;
using IteratorKit.CMOracle;
using static IteratorKit.CMOracle.OracleJSON.OracleEventsJson;
using static IteratorKit.CMOracle.CMOracleBehavior;

namespace IteratorKit.CMOracle
{
    public class CMConversation : Conversation
    {
        public CMOracleBehavior owner;
        public string eventId;
        public CMDialogType eventType;
        public OracleJSON.OracleEventsJson oracleDialogJson;
        public DataPearl.AbstractDataPearl.DataPearlType pearlType;
        public bool resumeConvFlag = false;

        public enum CMDialogType
        {
            Generic,
            Pearls,
            Item
        }

        public CMConversation(CMOracleBehavior owner, CMDialogType eventType, string eventId, DataPearl.AbstractDataPearl.DataPearlType pearlType = null) : base(owner, Conversation.ID.None, owner.dialogBox)
        {
            this.owner = owner;
            // this.convBehav = convBehav;
            this.eventType = eventType;
            this.eventId = eventId;
            this.oracleDialogJson = this.owner.oracle.oracleJson.events;
            this.pearlType = pearlType;
            this.AddEvents();
        }

        public override void AddEvents()
        {
            // read this.id
            IteratorKit.Logger.LogInfo($"Adding events for {this.eventId}");
            List<OracleEventObjectJson> dialogList = this.oracleDialogJson.generic;
            
            switch (this.eventType)
            {
                case CMDialogType.Generic:
                    dialogList = this.oracleDialogJson.generic;
                    break;
                case CMDialogType.Pearls:
                    dialogList = this.oracleDialogJson.pearls;
                    break;
                case CMDialogType.Item:
                    dialogList = this.oracleDialogJson.items;
                    break;
                default:
                    IteratorKit.Logger.LogError("Tried to get non-existant dialog type. using generic");
                    dialogList = this.oracleDialogJson.generic;
                    break;
            }
            foreach (OracleEventObjectJson item in dialogList)
            {
                IteratorKit.Logger.LogWarning(item.eventId);
            }
            List<OracleEventObjectJson> dialogData = dialogList?.FindAll(x => x.eventId.ToLower() == this.eventId.ToLower());

            if (dialogData.Count > 0)
            {
                foreach(OracleEventObjectJson item in dialogData)
                {
                    foreach (SlugcatStats.Name name in item.forSlugcats)
                    {
                        IteratorKit.Logger.LogWarning(name);
                    }
                    if (!item.forSlugcats.Contains(this.owner.oracle.room.game.GetStorySession.saveStateNumber)){
                        continue; // skip as this one isnt for us
                    }
                    IteratorKit.Logger.LogWarning(item.action);

                    if (item.action != null)
                    {
                        this.events.Add(new CMOracleActionEvent(this, item.action, item));
                    }

                    if (!((StoryGameSession)this.owner.oracle.room.game.session).saveState.deathPersistentSaveData.theMark)
                    {
                        // dont run any dialogs until we have given the player the mark.
                        return;
                    }

                    // add the texts. get texts handles randomness
                    foreach (string text in item.getTexts(this.owner.oracle.room.game.StoryCharacter))
                    {
                        IteratorKit.Logger.LogWarning("got texts");
                        IteratorKit.Logger.LogWarning(text);
                        if (text != null)
                        {
                            this.events.Add(new CMOracleTextEvent(this, this.ReplaceParts(text), item));
                        }
                        
                    }

                    if (item.texts == null && item.text == null && item.action == null)
                    {
                        IteratorKit.Logger.LogWarning("No provided action/text. adding dummy event");
                        this.events.Add(new CMOracleActionEvent(this, "none", item));
                    }
                }
                
            }
            else
            {
                IteratorKit.Logger.LogInfo("Fallback to collections code for "+this.pearlType);
                if (this.TryLoadCustomPearls())
                {
                    return;
                }else if (this.TryLoadFallbackPearls())
                {
                    return;
                }
                else
                {
                    IteratorKit.Logger.LogWarning($"Failed to find dialog {this.eventId} of type {this.eventType}");
                    return;
                }
                
            }


        }

        private bool TryLoadCustomPearls()
        {
            CustomPearls.CustomPearls.DataPearlRelationStore dataPearlRelation = CustomPearls.CustomPearls.pearlJsonDict.FirstOrDefault(x => x.Value.pearlType == this.pearlType).Value;
            if (dataPearlRelation != null)
            {
                OracleEventObjectJson pearlJson = null;  ;
                switch (this.owner.oracle.oracleJson.pearlFallback?.ToLower() ?? "default")
                {
                    case "pebbles":
                        pearlJson = dataPearlRelation.pearlJson.dialogs.pebbles;
                        break;
                    case "moon":
                        pearlJson = dataPearlRelation.pearlJson.dialogs.moon;
                        break;
                    case "pastmoon":
                        pearlJson = dataPearlRelation.pearlJson.dialogs.pastMoon;
                        break;
                    case "futuremoon":
                        pearlJson = dataPearlRelation.pearlJson.dialogs.futureMoon;
                        break;
                }
                if (pearlJson == null && dataPearlRelation.pearlJson.dialogs.defaultDiags != null)
                {
                    // use default as fallback
                    pearlJson = dataPearlRelation.pearlJson.dialogs.defaultDiags;
                }

                if (pearlJson != null)
                {
                    foreach (string text in pearlJson.getTexts((this.interfaceOwner as OracleBehavior).oracle.room.game.GetStorySession.saveStateNumber))
                    {
                        this.events.Add(new Conversation.TextEvent(this, pearlJson.delay, this.ReplaceParts(text), pearlJson.hold));
                    }
                    return true;
                }
                else
                {
                    IteratorKit.Logger.LogError($"Failed to load dialog texts for this oracle.");
                }
            }
            return false;
        }

        private bool TryLoadFallbackPearls()
        {
            if (this.pearlType != null && this.owner.oracle.oracleJson.pearlFallback != null)
            {
                // is not a custom pearl. switch which set of pearl dialogs to use, null save file uses default moon dialogs, so any value except below will use moons dialogs.
                SlugcatStats.Name saveFileName = null;
                switch (this.owner.oracle.oracleJson.pearlFallback.ToLower())
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
            return false;
        }

        public string Translate(string s)
        {
            return this.owner.Translate(s);
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

        public void FallbackPearlConvo(PhysicalObject physicalObject)
        {
            base.LoadEventsFromFile(38, true, physicalObject.abstractPhysicalObject.ID.RandomSeed);
        } 

        public override void Update()
        {
            if (this.paused)
            {
                return;
            }
            if (this.events.Count == 0)
            {
                this.Destroy();
                return;
            }
            this.events[0].Update();
            if (this.events[0].IsOver)
            {
                this.events.RemoveAt(0);
            }
        }

        public void InterruptQuickHide()
        {
            this.dialogBox.messages = new List<DialogBox.Message>();
        }

        public void OnEventActivate(DialogueEvent dialogueEvent, OracleEventObjectJson dialogData)
        {
            if (dialogData.score != null)
            {
                this.owner.ChangePlayerScore(dialogData.score.action, dialogData.score.amount);
            }
            if (dialogData.movement != null)
            {
                IteratorKit.Logger.LogWarning($"Change movement to {dialogData.movement}");
                if (Enum.TryParse(dialogData.movement, out CMOracleMovement tmpMovement))
                {
                    this.owner.movementBehavior = tmpMovement;
                }
                else
                {
                    IteratorKit.Logger.LogError($"Invalid movement option provided: {dialogData.movement}");
                }
            }
            if (dialogData.screens.Count > 0)
            {
                this.owner.ShowScreens(dialogData.screens);
            }
            if (dialogData.gravity != -50f)
            {
                this.owner.forceGravity = true;
                this.owner.roomGravity = dialogData.gravity;
                List<AntiGravity> antiGravEffects = this.owner.oracle.room.updateList.OfType<AntiGravity>().ToList();
                foreach (AntiGravity antiGravEffect in antiGravEffects)
                {
                    antiGravEffect.active = (this.owner.roomGravity >= 1);
                }
            }
                
        }

        public class CMOracleTextEvent : TextEvent
        {
            public new CMConversation owner;
            public ChangePlayerScoreJson playerScoreData;
            public OracleEventObjectJson dialogData;
            public CMOracleTextEvent(CMConversation owner, string text, OracleEventObjectJson dialogData) : base(owner, dialogData.delay, text, dialogData.hold)
            {
                this.owner = owner;
                this.playerScoreData = dialogData.score;
                this.dialogData = dialogData;

            }

            public override void Activate()
            {
                IteratorKit.Logger.LogWarning("activate text ev");
                base.Activate();
                this.owner.dialogBox.currentColor = this.dialogData.color;
                this.owner.OnEventActivate(this, dialogData); // get owner to run addit checks
            }
        }


        public class CMOracleActionEvent : DialogueEvent
        {

            public new CMConversation owner;
            string action;
            public string actionParam;
            public ChangePlayerScoreJson playerScoreData;
            public OracleEventObjectJson dialogData;

            public CMOracleActionEvent(CMConversation owner, string action, OracleEventObjectJson dialogData) : base(owner, dialogData.delay)
            {
                IteratorKit.Logger.LogWarning("Adding custom event");
                this.owner = owner;
                this.action = action;
                this.actionParam = dialogData.actionParam;
                this.playerScoreData = dialogData.score;
                this.dialogData = dialogData;
            }

            public override void Activate()
            {
                base.Activate();
                IteratorKit.Logger.LogInfo($"Triggering action ${action}");
                this.owner.owner.NewAction(action, this.actionParam); // this passes the torch over to CMOracleBehavior to run the rest of this shite
                this.owner.OnEventActivate(this, dialogData); // get owner to run addit checks
            }


            public static void LogAllDialogEvents()
            {
                for (int i = 0; i < DataPearl.AbstractDataPearl.DataPearlType.values.Count; i++)
                {
                    IteratorKit.Logger.LogInfo($"Pearl: {DataPearl.AbstractDataPearl.DataPearlType.values.GetEntry(i)}");
                }
                for (int i = 0; i < AbstractPhysicalObject.AbstractObjectType.values.Count; i++)
                {
                    IteratorKit.Logger.LogInfo($"Item: {AbstractPhysicalObject.AbstractObjectType.values.GetEntry(i)}");
                }
            }


        }
    }
}
