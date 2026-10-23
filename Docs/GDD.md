# Candy Thief — GDD v0.1
Status: MVP working spec; unconfirmed details marked TBD. Owner approval required for changes.

## Pitch
2D top-down candy-shop stealth action. Evade guard and shoppers, collect the key candy, and escape. Optional gold candy adds risk/reward.

## Target
Unity 2D / PC WebGL / keyboard. 6-week remaining development window (study weeks 3–8).

## MVP loop
Title → Start → Shop → collect key candy → reach exit → Result → Retry/Title.
Detection/failure → Result → Retry/Title. Gold candy counts only on successful clear.

## MVP rules
- One player character; no companions in MVP.
- Player moves with WASD; collisions block movement.
- One daytime shop level, one guard; up to five shoppers as scope allows.
- Guard patrols, sees player in a directional field of view, and cannot see through solid obstacles.
- Detection causes failure; exact detection delay TBD.
- Key candy is required for clearing; reaching exit without key cannot clear.
- Gold candy is optional; collected amount is granted only on successful clear.
- Result screen shows clear/fail and gold earned. Retry resets the stage.
- UI: title/start, HUD (key acquired, gold count, detection feedback), result/retry.
- Save only best clear / accumulated reward if time allows; no account/server.

## Out of scope (MVP)
Night stages, companions/gacha, costumes, auto-skip, cooldown, progression economy, multiplayer, server, complex chase AI.

## Art direction
Cute but kitschy low-detail pixel art; candy-shop pastel palette (mint/sky-blue/pink/yellow/purple); avoid heart-heavy/feminine ornamentation. Small readable silhouettes, obvious guard/shopper roles. Art is replaceable; prototype with placeholders.

## Acceptance criteria
1. From title, enter shop and move with WASD; collision works.
2. Guard patrol and field-of-view detection work; walls block detection.
3. Key pickup unlocks exit; without key no clear.
4. Gold pickup tracked; failure awards zero.
5. Clear/fail/retry UI works without errors.
6. WebGL build launches in browser and completes one loop.

## Open decisions (ask before implementation if blocking)
- Detection timing (instant vs gauge); guard and shopper roles in detection.
- Exact input package (legacy vs new Input System) follows existing project configuration.
- Gold persistent save policy and hosting target.
