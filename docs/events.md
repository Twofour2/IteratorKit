# Events System
Events fit into one of three categories:
`generic, pearls and items`

`generic` uses the event name from [this list](/eventIds.md)

`pearls and items` uses the pearl/item id. This is meant just for overriding existing pearls, If you're creating custom see the [pearls doc](/pearls.md).

## Building an event   
A basic event looks like this:

```
{
    "event": "playerEnter",
    "texts": ["Hello <PlayerName>", "This is the second dialog"]
}
```
\<PlayerName\> Will be replaced with what the oracle currently refers to the player as (usually "little creature")
The dialogs will play in the order in the list. You may also set `"random": true` to pick one dialog at random.

**Warning:** No text will play until the player has the mark! make sure your first event has `"action": "giveMark"`.

## Event ordering  
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

## PlayerConversation  
Player conversation triggers under one of two conditions to allow for it to run for both players with and without the mark:   
1. Player enters **without mark** -> Gets mark (giveMark) -> start player conversation  
OR
2. Player enters **with mark** -> start player conversation  


`playerConversation` will only trigger once per save file, once it's the player has saved it wont trigger again. This event is where you should place the main dialogs for any story purposes.  

To test the `playerConversation` event this mod provides a debug key `6` which allows playerConversation to trigger again.

## "For" and "Creatures"  
Used if you wish to restrict and event to a specific slugcat or require that a creature is present. 
```
{
    "event": "playerConversation",
    "for": ["Yellow", "Spear"]
    "creatures": ["lizards", "Yeek"]
    ...
```
This example will only run if the player is Monk or SpearMaster AND a lizard or Yeek is present in the room.
[List of creature names](/eventIds.md) (Dialog Creatures)

## Movement  
This determines how the oracles behaves when the event is played. If you want it to play after create another event and place it below in the list.
The avalible movements [listed here](/eventIds.md).

## Action/Gravity/Sounds/MoveTo 
WIP. Currently only custom oracles support these features.
```
    "event": "playerConversation",
    "gravity": 0
```

## Custom Oracle Actions  
```
generalIdle
giveMark
giveKarma
giveMaxKarma
giveFood
startPlayerConversation
kickPlayerOut
killPlayer
```

## Action Param  
Use `kickPlayerOut` to tell the code which exit to push the player towards.
```
    ...
    "action": "kickPlayerOut",
    "actionParam": "SU_test"
    ...
```  
Use `giveFood` with a number to fill the players food pips.  
Use `giveKarma` with a number to change the current karma level as well as increasing the max karma.

## Score  
This effects how "angry" the oracle is with the player. If the player is too annoying the oracle will kick the player out. `action` can be `set` `add` or `subtract`
```
...
"score": {
    "action": "subtract",
    "amount": 10
}
...
```
## Sounds/Move To    
Move to does what it says, provide it with an x and y.  
Sound accepts a sound ID (ex. `SS_AI_Exit_Work_Mode`)

## Screens    
`image` uses a file name of any image placed in the "illustrations" this includes the images in the MoreSlugcats Mod folder or any images placed in your own mod folder.  
Dont specify an image if you just wish to move it around. Set move speed to zero to instantly move the image.
```
"event": "playerEnter",
"screens": [
    {
        "image": "aiimg1_dm",
        "hold": 80,
        "alpha": 200,
        "pos": {"x": 370, "y": 300},
        "moveSpeed": 0
    },
    {
        "hold": 50,
        "alpha": 200,
        "pos": {"x": 370, "y": 200},
        "moveSpeed": 50
    },
    {
        "image": "AIimg5b",
        "hold": 80,
        "alpha": 200,
        "pos": {"x": 370, "y": 200},
        "moveSpeed": 0
    }
]
```

## Colors
Changes the dialog box color. The text prefixes described in [the pearl docs](/pearls.md) are also supported.
```
"color": {"r": 0.75, "g": 0, "b": 0.75, "a": 1}
```
You can also set this globally on your iterator
```
"id": "YourOracleId"
"dialogColor": {"r": 0, "g": 150, "b": 255, "a": 255}
...
```

## Pearls/Items
```
{
    "item": "tomato",
    "texts": ["This is a tomato!"]
}
```
## Pearl Fallback   
By default iterators will produce no dialog unless a pearl is specified. Use `pearlFallback` to use one of the existing iterators set of dialogs instead. Possible values are `pebbles`, `moon`, `pastMoon` and `futureMoon`
```
{
    "id": "CustomIterator",
    "roomId": "example",
    "pearlFallback": "pebbles"
```
