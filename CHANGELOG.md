# Changelog

## 1.0.1

- Item locate is now the single `locate` command (`locate Resin`, `locate` for the held item, `locate clear`). `find` / `finditem` are removed so they do not shadow vanilla `find`.

## 1.0.0

- Filter the open chest with a live search box. Occupied slots that do not match are hidden; empty slots stay usable. Matching supports `*` wildcards and substrings on localized names, tokens, and prefabs. Filter is visual-only and clears when the chest closes.
- Clear Filter restores the full chest view.
- Sort merges compatible stacks, then orders by type, name, and quality. Click again to reverse. Requires chest ownership; skipped while dragging an item.
- `find` / `finditem` glow nearby eligible chests that contain an item (`find Resin`, `find` for the held item, `find clear`). Not registered when Nearby Crafting Forked is loaded; use its `nearby` / `locate` instead.
