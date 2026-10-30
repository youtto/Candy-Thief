# Ninja Adventure temporary prototype art

Source: `Ninja Adventure - Asset Pack/Ninja Adventure - Asset Pack`

Included only for the Candy Thief prototype:
- `Actor/Character/{NinjaMasked,Inspector,Villager,Villager2,Villager3}/SeparateAnim/{Idle,Walk}.png`
- `Backgrounds/Tilesets/Interior/{TilesetInteriorFloor,TilesetWallSimple,TilesetInterior,Elements}.png`
- `Items/Treasure/{GoldKey,GoldCoin}.png`

License: CC0 1.0 Universal. See `LICENSE.txt`.

These files are temporary and may be replaced by final Candy Thief art. Gameplay
code must not reference these source paths directly; scene and prefab visuals own
the sprite and Animator references.

Import notes:
- Character Idle/Walk sheets are 16x16 frames. Columns are Down, Up, Left, Right.
- `TilesetInteriorFloor` and `TilesetInterior` stay 16x16 tiles.
- `TilesetWallSimple` is a decorative frame sheet, not the room wall kit.
- `Elements` is sliced as whole objects (counter, table, round table), not 16x16 tiles.
- The left 32x47 orange brick doorway in `Elements` is a wall piece, not furniture.
