# Overriding Looks To The Moon

Modifying LTTM works similar to how custom iterators work with couple of limitations:

- You can't modify LTTM's appearance

- You can't have LTTM in "Revived" mode

- You can't change the number of neurons

The above things could likely be added with custom code.

IteratorKit will track how many neurons the player has taken and supports a few additional events on top of the usual ones:

- playerTakeNeuron

- playerReleaseNeuron (only plays if a conversation is running, this is just how RW handles it)

Sample LTTM:

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
