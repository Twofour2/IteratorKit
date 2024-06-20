# Overriding Five Pebbles

To start, you need your own mod folder inside "\Rain World\RainWorld_Data\StreamingAssets\mods"

Create a new file in it and call it "oracle.json"

Paste in this starter template:

```
{
        "id": "SL",
        "for": ["Yellow"],
        "events": {
            "generic": {
                "playerEnter": [
                    {
                        "texts": ["This is custom text just for monk!"]
                    }
                ]
            },
        }
}
```

Make sure the ID is one of the following:  
SS = Pebbles (inlc. rot pebbles)  
DM = PastMoon (Alive)  

`"for"` in the example limits this override to just Monk (aka yellow). You can also specify this on the events themselves.  
The built in slug cats are: `White (Survivor), Yellow (Monk), Red (Hunter)`  
Downpour DLC: `Rivulet, Artificer, Saint, Spear, Gourmand, Slugpup, Inv`  
For custom slugcats using slugbase, this uses the `id` field

**Currently Five Pebbles (SL) and PastMoon (DM) support most (if not all) of the event features avalible to custom iterators. ****

**For building events, please see [the events document](~/events.md)**

# Overriding Looks To The Moon

Modifying LTTM works similar to how custom iterators work with couple of limitations:

- You can't modify LTTM's appearance

- You can't have LTTM in "Revived" mode

- You can't change the number of neurons LTTM starts with

The above things could likely be added with custom code.

IteratorKit will track how many neurons the player has taken and supports a few additional events on top of the usual ones:

- playerTakeNeuron

- playerReleaseNeuron (only plays if a conversation is running, this is just how RW handles it)

Sample LTTM JSON:

```json
"id": "SL",
"roomId": "SL_AI",
"pearlFallback": "moon",
"events": {
    "generic": {
        "playerEnter": [
            {
                "texts": [ "Player enter: <PLAYERNAME>!" ]
            }
        ],
        "playerConversation": [
            {
                "texts": [ "This is custom text for custom slugcats like you <PLAYERNAME>!" ]
             }
        ],

    }
}
```
