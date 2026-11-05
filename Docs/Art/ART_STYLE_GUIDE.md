# Candy Thief — Art Style Guide

- Version: 0.1
- Status: Working baseline (2026-11-05)
- Target: mobile portrait, 9:16; design reference 1080 × 1920

## 1. Art direction
Kitsch, comic, slightly pathetic 2D casual-game visuals. The thief is a broke beginner, not a successful criminal. Cute but not feminine or fairy-tale-like. Readability on small mobile screens takes priority over detail.

## 2. Master references
- Title A: daytime candy-store street chase; approved direction.
- Title B: nighttime rooftop escape; approved direction.
- Lobby A/B/C: latest revised concepts; approved *direction*, not final production assets.
- Reference images should be uploaded alongside this guide as Project files. Names: `REF_TITLE_AB.png`, `REF_LOBBY_ABC.png` (or original image files with clear labels).
- If references are unavailable in a new chat, ask for them rather than claiming pixel-exact consistency.

## 3. Characters
- SD proportions: about 2–2.5 heads tall; lobby characters smaller and simpler than in title art.
- Large round head, compact body, short limbs; small widely spaced dot eyes, minimal mouth and facial details.
- Thick, rounded dark outlines; simple flat fills, at most one soft shadow step.
- Player: red beanie, black eye mask with tiny light-colored dot eyes, striped shirt, black pants; keep silhouette recognizable.
- Lobby: player alone by default; two companion positions show empty `+` slots until explicitly assigned. Do not draw companions as permanently present in background.
- Desired idle motion: deliberately trivial/awkward sway, tiny bounce, blink, head tilt. Prepare separate parts or frames as needed; a still PNG does not itself animate.

## 4. Environment
- Lobby: shabby, sparsely furnished hideout with **large visible empty floor and wall areas**.
- Examples: peeling plaster, old floorboards, one cheap chair, exposed bulb, a few boxes. Keep props sparse and low contrast.
- No treasure piles, gold bars, luxurious furniture, or aspirational wall poster. Specifically **exclude** the “세계 최고의 사탕 도둑이 되자” poster.
- Title: colorful candy-shop village; title A daytime chase and title B nighttime rooftops are both valid.

## 5. Color and UI
- Main family: candy pink, sky blue, vanilla, muted red; neutral charcoal UI surfaces.
- Working palette **provisional, not sampled from artwork**: pink `#E98BA8`, sky `#91C8F0`, vanilla `#FFE4B0`, red `#D85B65`, outline `#342A35`, charcoal `#41404C`.
- Avoid overwhelmingly pink menus, hearts, ribbons, sparkles, glossy gradients and excessive ornament.
- Navigation: restrained charcoal/gray, simple functional icons, clear labels. Primary action: cream button with bold outline.
- Text and interactive controls should be built as separate Unity UI elements (e.g., TextMeshPro), not baked into backgrounds.

## 6. Screen specifications
### Splash/title
- Logo `CANDY THIEF`, thief escaping with candy sack, candy-shop/village backdrop.
- No START or settings buttons.
- Display for **at least 1 second**, then automatically transition when lobby loading finishes; show longer if loading takes longer.

### Lobby
- Top: profile/nickname, energy, gold, gold candy, settings.
- Center: small player character and two **unassigned** companion slots; spacious shabby hideout.
- Main CTA: `도둑질 시작!` → stage selection (stage details are **not** shown in lobby).
- Bottom: character management, gacha (companions/items), collection, shop, missions.
- Keep all navigation accessible on a phone safe area; do not assume a screenshot is a production-ready layout.

## 7. Asset production rules
1. Generate 3 exploratory concepts when requested; label A/B/C.
2. Select one and iterate with explicit change scope; preserve other elements.
3. Create production assets separately: background without UI/characters, transparent character sprites, icons, 9-slice-ready button/panel backgrounds.
4. Validate transparent backgrounds, dimensions, silhouette, readability, and animation pivot/parts in Unity.
5. Generative images are visual references until manually validated; consistent exact geometry and perfect layer extraction are not guaranteed.
6. Record accepted assets and revisions in `ART_ASSET_LIST.md`.

## 8. Standard prompt footer
> Follow `ART_STYLE_GUIDE.md` and the attached master reference images. Preserve Candy Thief's thick rounded outlines, simplified goofy SD proportions, muted candy palette, sparse shabby environments and restrained charcoal UI. Mobile portrait 9:16. Do not add companions to the lobby unless assigned. Avoid extra posters, wealth, decorative pink overload, or dense clutter. State which existing elements must remain unchanged when editing.

## 9. Change control
- Update this guide only when the user explicitly approves a new art direction.
- Track version/date and update the master reference images after a major approved change.
