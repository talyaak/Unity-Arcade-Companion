# Asset ledger (Pulse Arena)

Every generated, licensed, downloaded or third-party asset gets one row here **before** it enters `Assets/`.
Rules are in `docs/ASSET_PIPELINE.md`. Owner: architect. The builder proposes rows; the architect sets `status`.

Columns:

- **id**: `A-001`, `A-002`, ... Never reused.
- **file**: path under `Assets/` once imported, or `(not imported)`.
- **tool**: the generator or source, with its version or model name.
- **prompt**: the exact prompt text, or a link to the prompt log entry in `docs/asset-prompts/<id>.md`. For a
  non-generated asset, the download URL.
- **date**: date generated or downloaded (YYYY-MM-DD).
- **licence terms**: the terms as they applied on that date, quoted or linked, with the URL. "Standard terms" is not enough.
- **commercial use**: `yes`, `no` or `unknown`. `unknown` blocks import.
- **reviewer**: who checked the row (architect), and the date.
- **status**: `proposed`, `licence-checked`, `approved`, `imported`, `rejected`, `removed`.

An asset may be imported only when `commercial use` is `yes` and `status` is `approved`.

| id | file | tool | prompt | date | licence terms | commercial use | reviewer | status |
|---|---|---|---|---|---|---|---|---|
| | | | | | | | | |

No rows yet. Nothing has been generated or imported. The seven palette colours and the four primitive shapes
(cube, sphere, cylinder, capsule) are Unity built-ins and engine-made, not ledger assets.
