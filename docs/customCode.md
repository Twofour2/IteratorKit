# Custom Code Actions

For this it is assumed that you know how to write a code mod for rainworld, I wont go over this here. See the modding wiki for more details https://rainworldmodding.miraheze.org/wiki/Category:Code_Mods

Create an event with your own action name:
```
	{
		"event": "playerEnter",
		"action": "myCustomAction"
	}
```

In your mod override the OracleBehavior.SpecialEvent method and check for your event.

```
private void OnEnable()
{
	On.OracleBehavior.SpecialEvent += OracleBehavior_SpecialEvent;
}

private void OracleBehavior_SpecialEvent(On.OracleBehavior.orig_SpecialEvent orig, OracleBehavior self, string eventName)
{
    if (eventName == "myCustomEvent")
    {
        IteratorKitTest.Logger.LogWarning("This is a custom action!");  
        // add whatever code you wish here!
        self.inActionCounter = -1; // set once you're done
    }
    orig(self, eventName);
}

```

This special event action will be called every frame, use `self.inActionCounter` to keep track of how long your event has been running.  
Your event will block any new events from triggering until you set `self.inActionCounter = -1`. The mod will reset the oracle back to idle.
