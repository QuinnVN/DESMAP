# Domain docs

Engineering skills must read the relevant domain documentation before exploring or changing the codebase.

## Required reading

- Read the root `CONTEXT.md`.
- Read relevant decisions under `docs/adr/`.
- If a future `CONTEXT-MAP.md` exists, use it to locate the applicable context.

Proceed silently when an optional document or directory does not exist.

## Layout

This is a single-context repository:

```
/
├── CONTEXT.md
├── docs/
│   └── adr/
└── src/
```

## Vocabulary

Use the canonical terms defined in `CONTEXT.md`. Do not replace them with terms listed under `_Avoid_`.

If work contradicts an existing ADR, identify the conflict instead of silently overriding the decision.
