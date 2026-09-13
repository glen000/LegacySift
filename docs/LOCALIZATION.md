# LegacySift localization

LegacySift 0.2.2-alpha ships 21 complete UI dictionaries. English is the final fallback.

| Code | Language | Native name | Windows UI mapping |
|---|---|---|---|
| en | English | English | `en-*` and unsupported cultures |
| it | Italian | Italiano | `it-*` |
| de | German | Deutsch | `de-*` |
| fr | French | Français | `fr-*` |
| es | Spanish | Español | `es-*` |
| pt | Portuguese | Português | `pt-*` |
| pl | Polish | Polski | `pl-*` |
| nl | Dutch | Nederlands | `nl-*` |
| tr | Turkish | Türkçe | `tr-*` |
| uk | Ukrainian | Українська | `uk-*` |
| zh-Hans | Chinese (Simplified) | 简体中文 | `zh-*` for now |
| ja | Japanese | 日本語 | `ja-*` |
| hi | Hindi | हिन्दी | `hi-*` |
| ro | Romanian | Română | `ro-*` |
| cs | Czech | Čeština | `cs-*` |
| el | Greek | Ελληνικά | `el-*` |
| hu | Hungarian | Magyar | `hu-*` |
| sv | Swedish | Svenska | `sv-*` |
| ko | Korean | 한국어 | `ko-*` |
| id | Indonesian | Bahasa Indonesia | `id-*`; legacy `in` setting accepted |
| vi | Vietnamese | Tiếng Việt | `vi-*` |

The chooser always uses a bilingual English/Italian heading. Every row shows a small runtime-drawn flag, the native name, the English name and a visible code, so users can recover after accidentally selecting an unfamiliar language.

Russian is intentionally unsupported and has no shipped dictionary, catalog entry, flag or culture mapping. Windows `ru-*`, old saved `language=ru` values and unknown saved codes fall back to English. Ukrainian remains supported.

Arabic is deferred. The current safety interface communicates a fixed left-to-right OLD → CURRENT relationship, so RTL support requires a deliberate layout and usability pass rather than a text-only translation.

Automated tests require every dictionary to have exactly the English key set and the same numbered placeholders. New or materially revised translations should still receive native-speaker review before a stable release.
