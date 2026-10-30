# Current task — Week 3 vertical slice
Goal: Title → Start → simple test room → WASD player movement and collisions → HUD placeholder.

## Steps
1. Inspect Unity version, scene, input setup, project assets; report only blockers.
2. Propose minimum file/scene changes (max 8 lines). Await approval if destructive or package changes needed.
3. Implement movement/collision and camera using existing project configuration.
4. Implement title/start and HUD placeholders; keep scene references valid.
5. Validate compile/Play Mode through available tools; otherwise give exact manual verification steps.

## Current baseline
- Implemented: WASD/arrow-key movement, Rigidbody2D collision, and camera follow in
  `SampleScene`.
- Not implemented: title/start flow and HUD placeholder.
- No new packages.

## Temporary art integration
- Implemented: Ninja Adventure CC0 art with corrected slicing. `Elements` is whole
  objects (counter/table/round table), interior walls use `TilesetInterior` peach
  joints, floor uses `Floor_5_1`. Player Visual/Animator remains separate from
  `PlayerController`.
- Scene contents: rectangular room, south door gap, 3 furniture prefabs, player,
  1 guard, 2 shoppers.
- Static checks passed: v4 import marker on source sprites, Interior wall tile
  assets, and expected scene object names.
- Manual verification pending: Unity Game View screenshot, Play Mode movement
  animation, collision feel, Y-sort in front/behind furniture, and camera tracking.
  Unity MCP was unavailable.
