# Custom Pearls

To start, you need your own mod folder inside "<Game Install Location>\Rain World\RainWorld_Data\StreamingAssets\mods"

Create a new file in it and call it "pearls.json"

Paste in this starter template:

```
[
    {
        "pearl": "customPearl",
        "color": {"r": 1, "g": 0, "b": 0},
        "highlight": {"r": 0, "g": 1, "b": 0},
        "dialogs": {
            "moon": {
                "texts": ["This is a perfectly ripe tomato.", "It looks delicious! How did you find this <PLAYERNAME>?"]
            },
            "pebbles": {
                "texts": ["You found a tomato.", "It's probably best you keep it <PLAYERNAME>."]
            },
            "pastMoon": {
                "delay": 100,
                "texts": ["This is a perfect tomato.", "No wait, it's just a red pearl."]
            }
        }
        
    }
]
```

1. Open the dev tools with "o" and change to the objects tab 
2. Change the "OBJECTS" window to the "Consumable" page
3. Spawn a data pearl
4. Change it from "misc" to "customPearl". This may take a while of clicking as it's at the end of the list.
5. Hit "r" to reload and your pearl will now be where you placed it. This pearl is temporary.

Take this pearl up to your favorite iterator and you'll now see custom text!