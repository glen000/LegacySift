# LegacySift visual identity and Light UI

LegacySift 0.2.3-alpha applies one restrained Light visual layer to the existing safety workflow. The product metaphor remains:

**legacy gray → safe comparison bridge → protected current blue**

## Assets

- Full supplied artwork: `docs/assets/legacysift-logo-original.png`
- Cropped light-background logo: `docs/assets/legacysift-logo-light.png`
- Official application-icon source: `src/LegacySift/Assets/legacysift-icon-source.png`
- Documentation copy of the same source: `docs/assets/legacysift-icon-source.png`
- Multi-resolution Windows icon: `src/LegacySift/Assets/legacysift-icon.ico`
- 256 px documentation icon: `docs/assets/legacysift-icon-256.png`
- Actual-size icon QA sheet: `docs/assets/legacysift-icon-qa.png`

The supplied full wordmark remains unchanged. The separately supplied 1254×1254 application icon is the sole icon source of truth: its composition, proportions and colors are preserved, with only high-quality resizing for the 16/24/32/48/64/128/256 px PNG and ICO frames. Earlier alternate vector icon marks remain removed.

## Light-only behavior

LegacySift always starts with the approved Light palette. There is no Theme control, runtime theme dialog, Dark mode or System-following visual mode. Obsolete `theme=dark`, `theme=light`, `theme=system` and unknown theme values written by development builds are ignored; saving a language preference rewrites the settings file without the obsolete line.

The centralized Light palette remains the styling source for forms, surfaces, OLD/CURRENT panels, safety badges, buttons, links, tabs, text fields, summaries, result grids, progress, radio buttons, checkboxes and the language dialog. Dark mode is deferred and is not currently shipped.

## Native Windows caption

LegacySift retains the standard Windows non-client frame and its native drag, system menu, Snap, minimize, maximize, restore and close behavior. At window creation the application explicitly clears the immersive-Dark hint using `DWMWA_USE_IMMERSIVE_DARK_MODE` (attribute 20, with the older attribute 19 fallback).

Where the running DWM supports Windows 11 caption colors, LegacySift requests a white caption, dark navy text and `DWMWA_COLOR_NONE` for the border so an unrelated accent line is not introduced. The three explicit color calls are treated as one capability: if any is unsupported, successful partial changes are reset to the OS default. Older Windows versions therefore keep a normal native Light caption. A frame-only repaint refreshes the native caption without recreating the form handle or changing client geometry.

## Styling scope

Reliability takes priority over decoration:

- standard text boxes remain rectangular;
- text fields use a one-pixel palette border around a native borderless `TextBox`, including a blue focus cue;
- tabs retain native measurement and hit testing while the approved Light headers and underline are painted consistently;
- result grids retain restrained horizontal separators and Light borders;
- cleanup uses an integrated title/divider instead of a heavy native `GroupBox` rectangle;
- radio buttons and checkboxes retain native glyphs and focus behavior;
- scrollbars and `FolderBrowserDialog` remain owned by Windows;
- no third-party UI framework or custom font is used;
- no persistent bitmap, brush, pen or font allocation is performed during repaint;
- no changes are made to comparison or cleanup semantics.
