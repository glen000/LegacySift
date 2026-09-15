# Changelog

## 0.2.2-alpha

Final multilingual UI and repeatable layout QA pass.

- Expanded the interface to 34 complete translation dictionaries and closed the 0.2.2-alpha language scope.
- Added Romanian, Czech, Greek, Hungarian, Swedish, Korean, Indonesian and Vietnamese.
- Added Danish, Norwegian Bokmål, Finnish, Slovak, Bulgarian, Croatian, Bengali, Thai, Malay, Filipino, Estonian, Latvian and Lithuanian, including all three Baltic languages.
- Removed Russian from the application, catalog, culture detection, saved settings, flags and compiled resources; old `ru` settings now fall back to English.
- Kept the language chooser independent of the selected language with native name, English name, visible code and lightweight runtime-drawn flags.
- Compacted the normal window and added working-area fitting so the main workflow remains discoverable on constrained Windows desktops.
- Returned unused collapsed-cleanup height to the results grid so the default constrained window shows actual result rows without requiring maximization.
- Added multi-state WinForms layout regression checks across every supported language, representative screen sizes and 100%/125%/150% scaling approximations.
- Added programmatic screenshot capture for representative Latin, Cyrillic, CJK, Devanagari and Korean interfaces.
- Arabic/RTL remains deferred until the directional OLD-to-CURRENT safety design can be implemented deliberately.
- Comparison and cleanup safety logic is unchanged.

## 0.2.1-alpha

Focused UX correction pass after the first real-world visual test.

- OLD and CURRENT folders are shown side-by-side with permanent roles.
- CURRENT is clearly marked as protected/read-only.
- The normal workflow no longer depends on vertical scrolling to discover cleanup.
- Result categories now explain what happens to each group of files.
- Cleanup explains what will remain in OLD before any modification is possible.
- Cleanup confirmation lists identical copies, files not found in CURRENT, possible different versions, and unchecked files.
- Fixed normal-window clipping that could hide the CURRENT-folder Browse button and the protected-folder reminder at some DPI/window sizes.
- Comparison and cleanup safety logic remain unchanged.

## 0.2.0-alpha

First native C#/.NET Framework prototype with exact-content comparison, reversible safety-folder cleanup, restore support, English/Italian UI, and Windows build/test automation.
