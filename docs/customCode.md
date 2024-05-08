# Custom Code Actions

For this it is assumed that you already know how to write a code mod for rainworld, I wont go over this here. See the modding wiki for more details https://rainworldmodding.miraheze.org/wiki/Category:Code_Mods

To do this you need to instantiate an oracle ID in your mod, then check that this matches the ID provided in your oracle.json file.

Example:  

```
public static readonly Oracle.OracleID SRS = new Oracle.OracleID("SRS", register: true);

private void OnEnable()
{
    On.OracleBehavior.Update += OracleBehavior_Update;
}

private void OracleBehavior_Update(On.OracleBehavior.orig_Update orig, OracleBehavior self, bool eu)
{
    orig(self, eu);
    if (self.oracle.ID == SRS)
    {
        // your custom code here
    }
}
```

# Writing custom event code

If you wish to do more advanced actions such as triggering custom dialogs you will need to add IteratorKit as an assembly reference to your mod
[Download the latest dll](/bin/Debug/net481/IteratorKit.dll) and add it [the same way you added the Unity/RW/BepInEx dll files](https://rainworldmodding.miraheze.org/wiki/BepInPlugins#Step_2.1_-_Setting_up_the_Mod_Main_class)

Once you've done that you should be able to reference the IteratorKit classes.   
The below example will trigger a dialog with the event name "customCodeEvent" under the generic category   

```
using IteratorKit.CMOracle;

...

private void OracleBehavior_Update(On.OracleBehavior.orig_Update orig, OracleBehavior self, bool eu)
{
    orig(self, eu);
    if (self.oracle.ID == SRS)
    {
        CMOracleBehavior cmBehavior = self as CMOracleBehavior;
        cmBehavior.cmConversation = new CMConversation(cmBehavior, CMConversation.CMDialogType.Generic, "customCodeEvent");

    }
}
```

and in your oracle.json file:

```
...
{
    "event": "customCodeEvent",
    "texts": ["This event can be triggered by custom code"]
}
```

# Listening for events

IteratorKit provides `OnEventStart` and `OnEventEnd`.  
Add the following lines to your on enable:

```
private void OnEnable()
{
    CMOracleBehavior.OnEventStart += OnEventStart;
    CMOracleBehavior.OnEventEnd += OnEventEnd;
}
```

and the following methods:

```
public void OnEventStart(CMOracleBehavior cmBehavior, string eventName, OracleEventObjectJson eventData)
{
    if (cmBehavior.oracle.ID == SRS)
    {
        Logger.LogInfo("event triggered " + eventName);
        if (eventData.forSlugcats.Contains(SlugcatStats.Name.Yellow))
        {
            Logger.LogInfo("This runs for Monk only");
            cmBehavior.action = CMOracleBehavior.CMOracleAction.killPlayer;
        }
        if (eventName == "myCustomEvent")
        {
            // run code your own event code
        }
    }

}

public void OnEventEnd(CMOracleBehavior cMOracleBehavior, string eventName)
{
    if (cMOracleBehavior.oracle.ID == SRS)
    {
        Logger.LogInfo("event ended " + eventName);
    }
}
```