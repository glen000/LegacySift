# LegacySift

LegacySift is a simple, safe tool for comparing a folder recovered from an old computer, disk, or backup with the folder you use today.

Its direction is intentionally fixed:

- **OLD folder**: the folder that may be cleaned.
- **CURRENT folder**: the protected reference, used only for comparison.

LegacySift answers one question:

> What is still in the old folder that I do not already have in the current folder?

During comparison, nothing is modified. Exact duplicates can later be moved out of the old folder into a recoverable safety folder. The current folder is never cleaned.

## Design goals

- understandable without technical knowledge;
- no left/right ambiguity;
- current folder clearly marked as protected/read-only;
- exact-content comparison;
- reversible cleanup by default;
- no administrator privileges required;
- local-only operation, with no upload, telemetry, or online account;
- multilingual user interface.

## Languages

The first development build includes English and Italian. The UI text is separated from the comparison engine so more languages can be added without changing the safety logic.

## Status

LegacySift is currently in alpha development. Do not use an alpha build as the only copy of important data.

## License

LegacySift is released under the MIT License.
