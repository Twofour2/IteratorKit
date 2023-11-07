# Custom Pearls

To start, you need your own mod folder inside "<Game Install Location>\Rain World\RainWorld_Data\StreamingAssets\mods"

Create a new file in it and call it "pearls.json"

Paste in this starter template:

```
[
    {
        "pearl": "customPearl",
        "color": {"r": 255, "g": 0, "b": 0},
        "highlight": {"r": 0, "g": 255, "b": 0},
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

## Custom dialog colors
For custom colors use a prefix at the start of your text.
```
    ...
    "texts" ["FP: This will appear blue"]
```
**Prefixes:**
```
SRS: RGBA(1.000, 0.750, 0.750, 1.000)
NSH: RGBA(0.750, 1.000, 0.750, 1.000)
WO: RGBA(0.750, 0.750, 1.000, 1.000)
EOC: RGBA(1.000, 1.000, 0.900, 1.000)
PI: RGBA(1.000, 0.750, 1.000, 1.000)
GS: RGBA(0.900, 0.600, 0.400, 1.000)
SI: RGBA(0.750, 0.750, 0.750, 1.000)
HR: RGBA(0.550, 0.700, 0.550, 1.000)
BSM: RGBA(0.900, 0.850, 0.600, 1.000)
CW: RGBA(0.750, 0.750, 0.900, 1.000)
Five Pebbles: RGBA(0.400, 0.850, 0.750, 1.000)
EP: RGBA(0.400, 0.850, 0.750, 1.000)
FP: RGBA(0.400, 0.850, 0.750, 1.000)
HF: RGBA(0.550, 0.550, 0.900, 1.000)
NGI: RGBA(0.700, 0.550, 0.800, 1.000)
UU: RGBA(0.550, 0.900, 0.550, 1.000)
Andrew: RGBA(1.000, 1.000, 1.000, 1.000)
Will: RGBA(1.000, 0.300, 0.350, 1.000)
Screams: RGBA(0.300, 1.000, 0.530, 1.000)
Dakras: RGBA(0.720, 0.330, 0.930, 1.000)
Cappin: RGBA(0.000, 0.380, 0.650, 1.000)
Norgad: RGBA(0.670, 0.560, 0.840, 1.000)
```