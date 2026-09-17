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

The Dark palette uses a restrained blue-gray hierarchy (`#0F1720`, `#151F2B`, `#1A2735`, `#213142`) with `#304154` borders, `#E7EDF4` primary text and `#2589F5` primary actions. OLD is now a nearly neutral, slightly warm charcoal surface with amber confined to its semantic badge/accent. CURRENT is a calm cool-neutral surface with its green protected badge; neither side is rendered as a large saturated color block.

## Native Windows caption

LegacySift retains the standard Windows non-client frame and its native drag, system menu, Snap, minimize, maximize, restore and close behavior. Theme changes first request `DWMWA_USE_IMMERSIVE_DARK_MODE` (attribute 20, with the older attribute 19 fallback). Where the running DWM supports the complete set, LegacySift also sets `DWMWA_BORDER_COLOR`, `DWMWA_CAPTION_COLOR` and `DWMWA_TEXT_COLOR` so Windows accent-color settings do not introduce an unrelated colored edge.

The three explicit color calls are treated as one capability: if any is unsupported, any successful partial change is reset to the OS default. Older Windows versions therefore retain their normal native caption while still receiving the supported immersive light/dark hint. A frame-only refresh updates the caption during immediate theme switching without recreating the form handle or changing the client layout.

## Styling scope

The centralized palette styles forms, surfaces, OLD/CURRENT panels, safety badges, buttons, links, tabs, text fields, summaries, result grids, progress, radio buttons, checkboxes and the language/theme dialogs.

Reliability takes priority over decoration:

- standard text boxes remain rectangular;
- text fields use a one-pixel palette border around a native borderless `TextBox`, including a blue focus cue;
- radio buttons and checkboxes retain native glyphs and focus behavior;
- scrollbars and `FolderBrowserDialog` remain owned by Windows;
- no third-party UI framework or custom font is used;
- no persistent bitmap, brush, pen or font allocation is performed during repaint;
- no changes are made to comparison or cleanup semantics.

In Dark mode, tab headers use an integrated active surface and a three-pixel blue underline instead of a box around every tab. Result grids use subtle horizontal separators with no outer frame, and the cleanup section uses a title/divider treatment rather than the native high-contrast `GroupBox` rectangle. Light mode retains its approved semantic surfaces and layout.
