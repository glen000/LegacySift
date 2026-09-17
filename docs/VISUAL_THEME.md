# LegacySift visual identity and themes

LegacySift 0.2.3-alpha applies a restrained visual layer to the existing safety workflow. The product metaphor remains:

**legacy gray → safe comparison bridge → protected current blue**

## Assets

- Full supplied artwork: `docs/assets/legacysift-logo-original.png`
- Cropped light-background logo: `docs/assets/legacysift-logo-light.png`
- Official application-icon source: `src/LegacySift/Assets/legacysift-icon-source.png`
- Documentation copy of the same source: `docs/assets/legacysift-icon-source.png`
- Multi-resolution Windows icon: `src/LegacySift/Assets/legacysift-icon.ico`
- 256 px documentation icon: `docs/assets/legacysift-icon-256.png`
- Actual-size icon QA sheet: `docs/assets/legacysift-icon-qa.png`

The supplied full wordmark remains unchanged. The separately supplied 1254×1254 application icon is the sole icon source of truth: its composition, proportions and colors are preserved, with only high-quality resizing for the 16/24/32/48/64/128/256 px PNG and ICO frames. Earlier alternate vector icon marks have been removed.

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
