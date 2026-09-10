# LegacySift UX principles

LegacySift is intentionally a one-purpose migration/recovery tool. Its interface must be understandable by a person with little or no technical experience.

## Core rule

If a user must first learn how LegacySift works before they can use it safely, the interface needs to be simplified.

## Direction must always be obvious

The two folders have permanent roles:

- **OLD folder**: the recovered folder that may be cleaned.
- **CURRENT folder**: the protected reference. LegacySift only reads it.

Never use generic labels such as A/B, left/right, source/destination, or reference as the main user-facing terms.

## No hidden primary action

The complete workflow must be visible in a normal application window. Users must not need to maximize the window or discover scrolling to find the cleanup step.

## Explain outcomes, not implementation

Prefer language such as:

- `NOT FOUND IN CURRENT`
- `POSSIBLE DIFFERENT VERSIONS`
- `IDENTICAL COPIES`

Technical terms such as SHA-256, hash, reparse point, and manifest belong in technical documentation or detailed warnings, not in the normal workflow.

## Cleanup meaning

Before cleanup, the UI must explicitly explain:

> LegacySift removes from the OLD folder only files for which an identical copy exists in CURRENT. Files not found in CURRENT, different versions, and files skipped for safety remain in OLD.

The recommended action is reversible: identical copies are moved to a safety folder, not permanently deleted.

## Protected side

The CURRENT folder must always be visually marked as protected/read-only. Cleanup code must never be given a path outside the OLD folder as a writable target.

## Language

All user-facing text must be localizable. English and Italian are the initial languages; new languages must not require changes to comparison or cleanup logic.
