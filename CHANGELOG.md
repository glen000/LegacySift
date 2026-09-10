# Changelog

## 0.2.1-alpha — 2026-09-10

- Rebuilt the main workflow so the cleanup step is always visible in a normal-size window; maximizing the app is no longer required to discover the primary action.
- Placed OLD and CURRENT folder choices side by side to make the fixed direction easier to understand at a glance.
- Renamed result groups to clearer outcome-based wording: **Not found in CURRENT / Possible different versions / Identical copies / Files not checked**.
- Added a plain-language explanation inside every result tab.
- Added an always-visible cleanup explanation that states exactly what will remain in OLD after cleanup.
- Reworded the cleanup confirmation to list what will be removed and what will stay before any change is made.
- Kept the recommended reversible Safety folder as the default cleanup method.
- Preserved the existing comparison engine, SHA-256 matching, protected CURRENT-folder invariant, pre-cleanup revalidation and restore protections.

## 0.2.0-alpha — 2026-09-10

- Renamed project from FolderSift to **LegacySift**.
- Reworked the main interface around plain-language migration steps.
- Removed A/B terminology from the primary UI: **OLD folder** and **CURRENT protected folder** are now explicit.
- Added clear protected/read-only messaging for the current folder.
- Added English and Italian localization infrastructure.
- Added Windows-language detection and a language selector.
- Renamed user-facing "quarantine" wording to **Safety folder / Cartella di sicurezza**.
- Moved less-common cleanup choices behind **Show other options**.
- Reworded results as Files to keep / Versions to check / Already present / Problems.
- Preserved SHA-256 exact-match behavior and pre-cleanup revalidation.
- Preserved restore-without-overwrite behavior.
- Kept `asInvoker` manifest: no administrator elevation requested.

## 0.1.0-alpha — 2026-09-09

- Initial C# / WinForms proof of concept.
- Exact duplicate detection across different names and paths.
- Possible-version warning for same-name, different-content files.
- Safety-folder cleanup and restore prototype.
- Initial path protections and smoke tests.
