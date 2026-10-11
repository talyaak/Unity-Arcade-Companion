# T10 reference notes (2026-10-11, builder)

Reference only. Nothing from these sources is in the repo: no images, no art, no names, no code. The pictures were kept
outside the repo and looked at for layout and feel. All takeaways below are in our own words.

## Sources
- Marketing cover, 16:9, "Goods Triple Match 3D": https://www.crazygames.com/fr/game/goods-triple-match-3d
- App Store listing with portrait gameplay screenshots (three of them looked at): https://apps.apple.com/app/1607122287
- Search results on triple-match and 2.5D tile games, for camera and tile notes: several app listings and two design pages
  (seeles.ai). The design pages are AI-prompt pages for Three.js builds, so their notes are suggestions, not evidence from
  shipped games.

## What the pictures showed (takeaways)
- **Background:** a dark, saturated colour behind the board (deep teal and purple in the portrait shots). The glossy items
  stand out against it.
- **Tiles and objects:** rounded, glossy, soft-shaded objects with soft contact shadows. They read as objects, not as
  primitive shapes.
- **Camera:** the board fills the width of a portrait screen. The camera is high and tilted down, so both the tops and the
  sides of the stacks read.
- **Goal:** a clear banner at the top with the target, and a small card showing it. Our equivalent is the stage and the
  cleared count.
- **Tray:** a row of recessed, rounded, dark slots along the bottom. A filled slot shows its item.
- **Buttons and bars:** rounded bars and rounded buttons. Buttons read as pressable through a darker lower edge.
- **Cover:** a chunky, outlined title badge and round icon buttons with a small count badge.

## Design notes from the search results (not verified against shipped games)
- Camera: a perspective camera about 45 to 60 degrees above the board. Some notes use 45 degrees for an isometric look.
- Depth in stacks: a directional light with soft shadows does most of the work.
- Tiles: bevelled or rounded boxes with some thickness.
- Many tiles: instanced meshes, to keep draw calls low.
- Taps: a slightly larger touch area than the visible tile, to forgive imprecise taps (not adopted yet).

## What T10 takes from this
- Tiles: a rounded slab, glossy top, one flat emblem per symbol (seven distinct shapes), no primitive spheres or capsules.
- Camera: steeper pitch (55 to 60 degrees) and a narrower field of view, to test. Dark navy background.
- HUD: a top bar with the stage and the score, a cleared-count bar, a tray meter of seven slots, and buttons with a darker
  lower edge that shift when pressed. UI Toolkit does not support box-shadow or gradients, so the 3D look is faked with
  borders.

## Left out on purpose
Timers, coins and currency, shop and cart icons, lives, levels with lock icons, and boosters. These are either money-like
or new rules, and the brief and D4 keep them out of the MVP.
