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

# Custom Oracle Classes

IteratorKit allows for mods to replace most of the built in classes with custom equivalents. You can override either `CMOracleBehavior` or `CMOracleSitBehavior`, or any other class used by iteratorkit except CMOracleGraphics. The custom graphics class is initialised seperately in the `OnOracleSetupGraphicsModule`,  otherwise it works the same.

```csharp
public class MyOracleBehavior : CMOracleBehavior {
    public MyOracleBehavior(Oracle oracle) : base(oracle){
        // your own code for setup
    }

    public override void Update(bool eu){
        base.Update(eu);
        // your own code to run once per frame
    }
}
```

Read the code for the class you are trying to overwrite to get an idea for how it works.

Then to setup the code when the oracle is initialised, use the `OnOracleSetupModules` hook.

```csharp
public static readonly Oracle.OracleID SRS = new Oracle.OracleID("SRS", register: true);


public void OnEnable(){
    CMOracle.CMOracle.OnOracleSetupModules += OnOracleSetupModules;
}

public void OnOracleSetupModules(CMOracle oracle){
    if (oracle.ID == SRS){
        oracle.oracleBehavior = new MyOracleBehavior(this);
        // or oracle.arm, oracle.myScreen, etc...
    }
}
```