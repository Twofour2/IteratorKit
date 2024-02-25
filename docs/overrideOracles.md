# Overriding existing iterators

To start, you need your own mod folder inside "\Rain World\RainWorld_Data\StreamingAssets\mods"

Create a new file in it and call it "oracle.json"

Paste in this starter template:

```
{
        "id": "SL",
        "for": ["Yellow"],
        "events": {
            "generic": [
                {
                    "event": "playerEnter",
                    "texts": ["This is custom text just for monk!"]
                }
            ],
        }
}
```
Make sure the ID is one of the following:  
SS = Pebbles (inlc. rot pebbles)  
SL = Moon   
DM = PastMoon (Alive)  

`"for"` in the example limits this override to just Monk (aka yellow). You can also specify this on the events themselves.  
The built in slug cats are: `White (Survivor), Yellow (Monk), Red (Hunter)`  
Downpour DLC: `Rivulet, Artificer, Saint, Spear, Gourmand, Slugpup, Inv`  
For custom slugcats using slugbase, this uses the `id` field

**Currently Five Pebbles (SL) and PastMoon (DM) support most (if not all) of the event features avalible to custom iterators. LTTM (SS) only supports very basic dialogs.**

**For building events, please see [the events document](~/events.md)**
