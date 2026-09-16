# LegacySift visual identity and themes

LegacySift 0.2.3-alpha applies a restrained visual layer to the existing safety workflow. The product metaphor remains:

**legacy gray → safe comparison bridge → protected current blue**

## Assets

- Full supplied artwork: `docs/assets/legacysift-logo-original.png`
- Cropped light-background logo: `docs/assets/legacysift-logo-light.png`
- Simplified vector mark: `docs/assets/legacysift-mark.svg`
- Icon master: `src/LegacySift/Assets/legacysift-icon.svg`
- Multi-resolution Windows icon: `src/LegacySift/Assets/legacysift-icon.ico`
- 256 px documentation icon: `docs/assets/legacysift-icon-256.png`
- Actual-size icon QA sheet: `docs/assets/legacysift-icon-qa.png`

The supplied full logo contains a baked white background and soft edge treatment. It is preserved rather than subjected to an unreliable automatic transparency conversion. The application icon is a separate clean vector simplification with no text or small decorative rays.

## Theme behavior

The compact **Theme / Tema…** control offers:

- **System** — reads Windows `AppsUseLightTheme` when LegacySift starts or when System is selected;
- **Light** — forces the LegacySift light palette;
- **Dark** — forces the LegacySift dark palette.

Light and Dark changes apply immediately. The selection is saved in `%LocalAppData%\LegacySift\settings.ini` next to the language preference. This pass intentionally does not keep a registry watcher alive; changing the Windows theme while LegacySift is already open is picked up the next time the application starts or System is selected again.

## Styling scope

The centralized palette styles forms, surfaces, OLD/CURRENT panels, safety badges, buttons, links, tabs, text fields, summaries, result grids, progress, radio buttons, checkboxes and the language/theme dialogs.

Reliability takes priority over decoration:

- standard text boxes remain rectangular;
- radio buttons and checkboxes retain native glyphs and focus behavior;
- scrollbars and `FolderBrowserDialog` remain owned by Windows;
- no third-party UI framework or custom font is used;
- no persistent bitmap, brush, pen or font allocation is performed during repaint;
- no changes are made to comparison or cleanup semantics.
