# Candy Thief — Art Asset List

- Version: 0.1 (2026-11-05)
- Status legend: `Reference approved` = direction approved, **not** game-ready; `Planned` = not produced; `In review`; `Final`; `Integrated`.
- Suggested ID format: `ART-<AREA>-###`.

| ID | Area | Asset | Status | Suggested output | Notes |
|---|---|---|---|---|---|
| ART-REF-001 | Reference | Title A/B master image | Reference approved | PNG | Day chase + night rooftop; store in Project files |
| ART-REF-002 | Reference | Revised lobby A/B/C master image | Reference approved | PNG | Spacious shabby room, small player, empty ally slots |
| ART-TTL-001 | Title | Candy Thief logo | Planned | Transparent PNG | Consistent wordmark |
| ART-TTL-002 | Title | Daytime title background | Planned | PNG | No UI/text/character baked in |
| ART-TTL-003 | Title | Nighttime title background | Planned | PNG | Alternative to daytime |
| ART-TTL-004 | Title | Running thief + candy sack illustration | Planned | Transparent PNG | May need day/night variants |
| ART-LOB-001 | Lobby | Shabby hideout background | Planned | PNG | Large empty wall/floor; no poster/treasure |
| ART-LOB-002 | Lobby | Main thief idle sprite/parts | Planned | Transparent PNGs | Tiny goofy movement; animation not included in static PNG |
| ART-LOB-003 | Lobby | Companion slot empty state | Planned | Transparent PNG | Two `+` slots |
| ART-LOB-004 | Lobby | Companion assigned state | Planned | UI frame/PNG | Only display assigned companion |
| ART-LOB-005 | Lobby | `도둑질 시작!` button background | Planned | Transparent PNG / 9-slice | Text in Unity |
| ART-UI-001 | Common UI | Profile frame | Planned | Transparent PNG / 9-slice | Nickname/avatar |
| ART-UI-002 | Common UI | Energy icon and bar | Planned | Transparent PNG | Value in Unity |
| ART-UI-003 | Common UI | Gold icon | Planned | Transparent PNG | Value in Unity |
| ART-UI-004 | Common UI | Gold candy icon | Planned | Transparent PNG | Value in Unity |
| ART-UI-005 | Common UI | Settings icon | Planned | Transparent PNG | Restrained neutral palette |
| ART-UI-006 | Common UI | Bottom navigation bar | Planned | PNG / 9-slice | Charcoal/gray |
| ART-UI-007 | Common UI | Character menu icon | Planned | Transparent PNG | Functional, consistent |
| ART-UI-008 | Common UI | Gacha menu icon | Planned | Transparent PNG | Companions + items |
| ART-UI-009 | Common UI | Collection menu icon | Planned | Transparent PNG | Functional, consistent |
| ART-UI-010 | Common UI | Shop menu icon | Planned | Transparent PNG | Functional, consistent |
| ART-UI-011 | Common UI | Mission menu icon | Planned | Transparent PNG | Functional, consistent |
| ART-CHR-001 | Character | Player turnaround sheet | Planned | PNG + individual transparent PNGs | Front/side/back, proportions fixed |
| ART-CHR-002 | Character | Player expression sheet | Planned | PNG | Simple expressions |
| ART-CHR-003 | Character | Player idle animation parts/frames | Planned | Transparent PNGs | Pivot and frame consistency check |
| ART-CMP-001 | Companion | Companion A design | Planned | PNG | Design TBD |
| ART-CMP-002 | Companion | Companion B design | Planned | PNG | Design TBD |
| ART-SCR-001 | Screen | Stage selection concept | Planned | 9:16 PNG | Entered from lobby CTA |
| ART-SCR-002 | Screen | Character management concept | Planned | 9:16 PNG | UI flow TBD |
| ART-SCR-003 | Screen | Gacha concept | Planned | 9:16 PNG | UI flow TBD |
| ART-SCR-004 | Screen | Collection concept | Planned | 9:16 PNG | UI flow TBD |
| ART-SCR-005 | Screen | Shop concept | Planned | 9:16 PNG | UI flow TBD |
| ART-SCR-006 | Screen | Missions concept | Planned | 9:16 PNG | UI flow TBD |

## Asset acceptance checklist
- [ ] Matches master references and style guide
- [ ] Correct portrait size / transparent background as required
- [ ] No unwanted baked-in text or UI
- [ ] Player silhouette and eye/mask/beanie consistent
- [ ] Lobby retains open space and shows no unassigned companions
- [ ] No wealth props or motivational poster
- [ ] Readable at actual mobile size
- [ ] Imported/tested in Unity, including pivot/9-slice if relevant

## Workflow / version notes
| Date | Item | Decision |
|---|---|---|
| 2026-11-05 | Title | A and B art directions liked; splash auto-transitions after minimum 1 s |
| 2026-11-05 | Lobby | Revised A/B/C direction liked; larger empty room, smaller player, neutral icons, companion slots empty by default |

## Suggested file names
`CT_<Area>_<Asset>_<Variant>_v###.png`, e.g. `CT_Lobby_Background_A_v001.png`.

## Unity project placement
- `Docs/Art/ART_STYLE_GUIDE.md`
- `Docs/Art/ART_ASSET_LIST.md`
- `Docs/Art/References/` (approved concepts)
- `Assets/Art/` (validated game-ready assets only)
