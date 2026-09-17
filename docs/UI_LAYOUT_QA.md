# WinForms UI layout QA

The normal opening window is the acceptance target. Maximizing is not required to identify the language control, both folder roles and Browse buttons, the fixed comparison direction, the check action, results, cleanup method, confirmation, cleanup action and protected-CURRENT reminder.

The test executable builds real WinForms controls for all 34 languages and exercises these states:

1. initial state;
2. folders selected;
3. analysis in progress;
4. analysis complete with missing files, possible versions, exact duplicates and an unchecked file;
5. cleanup ready;
6. cleanup confirmed;
7. other cleanup options expanded;
8. language dialog with all 34 entries in a constrained scrollable list;
9. localized System/Light/Dark theme dialog;
10. guide/safety tab content present through its normal scrollable text view.

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

Every configuration and operational state is exercised in both explicit Light and Dark modes. The harness checks non-zero bounds, containment of critical controls, OLD/CURRENT and Browse-button separation, critical button and tab text measurement, keyboard focusability, theme surfaces, icon assignment, language/theme choice text fit, dictionary completeness, placeholders, safety vocabulary and script/diacritic preservation. In the constrained analysis-complete state it also requires the active result grid to display its header plus at least four real data rows while every cleanup control remains visible. Tolerances avoid one-pixel font-rendering failures.

The 125% and 150% cases are explicit programmatic scaling/font-pressure approximations. They do not claim that a physical Windows monitor was switched to those DPI settings. CI uses real WinForms rendering at the constrained 1366×768 reference size and captures:

- Light populated: Italian, German, Ukrainian, Simplified Chinese, Hindi, Korean, Bengali, Thai and Lithuanian;
- Dark populated: Italian, German, Simplified Chinese, Hindi and Korean;
- Light and Dark initial-state Italian views;
- Light and Dark Italian analysis-in-progress views, including the active progress bar and disabled actions.

Final physical DPI, native title-bar behavior and native-speaker glyph/wording appearance should be spot-checked on Windows before a stable release.
