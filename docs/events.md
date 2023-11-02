# Events System
## Events System
Events fit into one of three categories:
`generic, pearls and items`

`generic` uses the event name from [this list](/docs/eventIds.md)

`pearls and items` uses the pearl/item id. This is meant just for overriding existing pearls, If you're creating custom see the [pearls doc](/docs/pearls.md).

**Building an event**  
A basic event looks like this:

```
{
    "event": "playerEnter",
    "texts": ["Hello <PlayerName>", "This is the second dialog"]
}
```
\<PlayerName\> Will be replaced with what the oracle currently refers to the player as (usually "little creature")
The dialogs will play in the order in the list. You may also set `"random": true` to pick one dialog at random.

**Event ordering**  
Events will play out in the order they are in the json file.
```
{
    "event": "playerEnter",
    "hold": 5,
    "wait": 5,
    "texts": ["Hello <PlayerName>", "This is the second dialog"]
},
{
    "event": "playerEnter",
    "texts": ["This will play after the first set of dialogs is done."]
}
```
If you want the dialog to show right away, hold and wait are not necessary. 

**Movement**  
This determines how the oracles behaves when the event is played. If you want it to play after create another event and place it below in the list.
The avalible movements [listed here](/docs/eventIds.md).

**Action/Gravity/Sounds/MoveTo**  
WIP. Currently only custom oracles support these features.

## Pearls/Items
```
{
    "item": "tomato",
    "texts": ["This is a tomato!"]
}
```
