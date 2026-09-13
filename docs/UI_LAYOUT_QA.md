# WinForms UI layout QA

The normal opening window is the acceptance target. Maximizing is not required to identify the language control, both folder roles and Browse buttons, the fixed comparison direction, the check action, results, cleanup method, confirmation, cleanup action and protected-CURRENT reminder.

The test executable builds real WinForms controls for all 21 languages and exercises these states:

1. initial state;
2. folders selected;
3. analysis complete with missing files, possible versions, exact duplicates and an unchecked file;
4. cleanup ready and confirmed;
5. other cleanup options expanded;
6. language dialog with all 21 entries;
7. guide/safety tab content present through its normal scrollable text view.

Programmatic layout matrix:

| Desktop reference | Client area used by harness | Scale treatment |
|---|---:|---|
| 1366×768 | 1144×673 | 100%, highest-priority constrained case |
| 1600×900 | 1144×701 | 100% |
| 1920×1080 | 1144×701 | 100% |
| 2560×1440 | 1144×701 | 100% |
| 1600×900 | 1434×825 | programmatic 125% approximation |
| 1920×1080 | 1724×1001 | programmatic 150% approximation |
| 2560×1440 | 1724×1051 | programmatic 150% approximation |

The harness checks non-zero bounds, containment of critical controls, OLD/CURRENT and Browse-button separation, critical button text measurement, language-row text fit, dictionary completeness, placeholders, safety vocabulary and script/diacritic preservation. Tolerances avoid one-pixel font-rendering failures.

The 125% and 150% cases are explicit programmatic scaling approximations. They do not claim that a physical Windows monitor was switched to those DPI settings. CI programmatically renders representative screenshots for Italian, German, Ukrainian, Simplified Chinese, Hindi and Korean at the constrained 1366×768 reference size. Final physical DPI and glyph appearance should be spot-checked on Windows before a stable release.
