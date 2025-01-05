# Iterator Kit

[Iterator kit](https://github.com/Twofour2/IteratorKit) is a mod for rainworld that allows for easier building of new iterators and custom iterator dialogs.

**Features**

- Customizable colors, gowns and sigils
- Events system for running actions and dialogs
- Item/Pearl dialogs
- Override dialogs/events for specific slugcats. I.e. custom slugs
- Create custom pearls with unique dialogs for each iterator

**Docs**  
[Creating your own iterator](~/iterators.md) 
[Overriding existing iterators](~/overrideOracles.md)   
[Creating custom pearls](~/pearls.md)  
[Creating Events](~/events.md)  
[List of Event Ids](~/eventIds.md)  
[Writing custom code](~/customCode.md)

**Debug Tools**
I highly reccomend enabling the Unity console at `Rain World\BepInEx\config\BeInEx.cfg` go to `[Logging.Console]` and set `Enabled` to `true`.

Enable the on screen debugging text by placing a file in the root of your mod called `enabledebug` (no file extension). This file is provided in the sample mod.

The following keys are also availible when the dev tools active (`o` key):  

- `Shift + 0` force save location to current room, use with reload for quick testing
- `Shift + 9` toggle on screen oracle debug text
- `Shift + 6` remove the `HasHadMainPlayerConversation` save file flag. This allows the conversations to run again
- `Shift + -` Prints all asset and shader names to the console


**Verify**  
Before filing an issue, please verify you have the mod correctly installed.

1. Open the dev tools with "o" and change to the objects tab  
2. Change the "OBJECTS" window to the "Consumable" page  
3. Spawn a data pearl  
4. Change it from "misc" to "tomato". This may take a while of clicking as it's at the end of the list.  
5. If you see the pearl the mod it correctly installed and loaded.  

**Contact**   
Feel free to get in touch with me via the rainworld discord (same username as github).  
