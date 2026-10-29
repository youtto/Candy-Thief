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
- Implemented: selected Ninja Adventure CC0 art, player Visual/Animator separation,
  guard/shopper/item prefabs, and a four-layer prototype shop Tilemap.
- Static checks passed: scripts compile without reported errors, required prefab/animation
  assets exist, and the generated scene contains the expected visual and collision layers.
- Manual verification pending: movement animation, collision feel, camera tracking, and
  final tile alignment in Play Mode. Unity MCP was unavailable during this task.
