# Changelog

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
