# Asset pipeline (Pulse Arena)

Status: **approved, later stage** (user, 2026-10-11). This is a plan only. No generator work, no generated assets and no\nledger rows until the user starts the asset stage. Nothing is imported. Primitives are the art (`GAME_DESIGN.md`, Visual standard).

Scope: 3D tile models and their textures, and later audio. Original work only. No third-party brands, characters,
logos or lookalikes. Names stay original too.

## 1. Prompt log

For each generated asset, keep `docs/asset-prompts/<id>.md` with:

- the exact prompt, the negative prompt if any, the tool and its version or model name;
- the seed and settings if the tool exposes them;
- every attempt, including rejected ones, with one line saying why;
- the date, and who ran it.

Prompts describe a **generic form** ("a glossy faceted gem tile, flat top, seven-sided silhouette"). They never name
a brand, a game, a franchise, a living artist or a studio style.

## 2. Licence check (before any use)

1. Open the tool's terms as of the generation date. Copy the relevant clause and its URL into the ledger row.
2. Record **commercial use** as `yes`, `no` or `unknown`. `unknown` means stop.
3. Check for terms that matter even when commercial use is `yes`: attribution required, output ownership, training or
   publicity rights the tool keeps, plan-tier limits (free tiers often exclude commercial use), and any
   restriction on shipping inside an app.
4. Check the output itself for third-party marks: logos, text, recognisable characters or products. Reject on any hit.
5. Set the row to `licence-checked`. Only the architect can set `approved`.

An asset whose terms later change keeps the terms in force on its date. Re-check before a store release.

## 3. Import rules (the builder enforces; the review gate checks)

| Item | Rule |
|---|---|
| Unit scale | 1 Unity unit = 1 m. A tile is 0.9 x 0.9 units on the board grid, height up to 0.4. Import with scale factor 1 and no rescale hacks. |
| Pivot | Centre of the tile footprint, at the tile's base plane. Forward is +Z, up is +Y. No rotated parents. |
| Triangles per tile | At most 500 (budget). A 6x6 stage has about 54 tiles, so 27,000 triangles, plus the tray. Over budget means decimate before import, not after. |
| Textures | At most 512 x 512 per tile atlas, power of two, sRGB for colour. No unused channels. Mipmaps on. Compress for the target (ASTC on Android). |
| Materials | Shared, per D3: one per palette colour and dimmed state. Models import with no embedded materials (Material Creation Mode: None) and use the shared ones. A symbol is told apart by **shape** as well as colour. |
| Colliders | None on the model. Tapping is decided by Core and the controller. |
| Read/Write | Off. Import cameras and lights off. Normals: import. Tangents: off unless a shader needs them. |
| Names | `Tile_<Symbol>` in PascalCase, original names only. No generator output filename survives. |
| Shadows | Cast on, receive on. One directional light (D5). |

The seven symbols need a distinct silhouette each. A model that only differs by colour fails the comfort rule.

## 4. Review gate

A generated asset moves through: `proposed` -> `licence-checked` -> `approved` -> `imported`.

Before `approved`, the architect checks that the ledger row is complete, the prompt log exists, the licence clause
is quoted, commercial use is `yes`, and the output has no third-party marks. Before `imported`, the builder shows:

- the import settings (a read of the import settings, or a screenshot);
- the triangle count and texture size against section 3;
- a screenshot of the tile on the board at 16:9 and 9:16, in the shared material, silhouette readable at phone size;
- the Profiler reading with the full board (draw calls, GC Alloc still 0 B in Playing).

A reject names the failed line and the smallest next step. A removed asset gets `removed` in the ledger; its row stays.

## 5. Generator integration

None until this spec is approved by the user. Then one generator at a time, one asset first, through this gate. Any
generator whose terms exclude commercial use, or that cannot be recorded in the ledger, is not used. Money-related
questions about a generator (paid plans, credits) go to the user.

## 6. Audio

The same ledger and gate apply. Until audio is chosen and logged, only silent or generated placeholder clips exist
(`GAME_DESIGN.md`, Audio).

## Known gaps for the art stage

- **Silhouette gap:** the primitives give four shapes (cube, sphere, cylinder, capsule) for seven symbols, so symbols
  5 to 7 repeat a shape and differ by colour only. This misses the comfort rule that each symbol has its own shape.
  Accepted for the primitive stage. The art stage must give each of the seven symbols a distinct silhouette.
- **Tile scale:** the 0.9-unit tile footprint in section 3 is a target for the art stage. Setting the primitive tiles to
  0.9 units is a T2 task for the builder.
