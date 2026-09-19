# LegacySift localization

LegacySift 0.2.2-alpha ships 34 complete UI dictionaries. English is the final fallback.

| Code | Language | Native name | Windows UI mapping |
|---|---|---|---|
| en | English | English | `en-*` and unsupported cultures |
| it | Italian | Italiano | `it-*` |
| bn | Bengali | বাংলা | `bn-*` |
| bg | Bulgarian | Български | `bg-*` |
| zh-Hans | Chinese (Simplified) | 简体中文 | `zh-*` for now |
| hr | Croatian | Hrvatski | `hr-*` |
| cs | Czech | Čeština | `cs-*` |
| da | Danish | Dansk | `da-*` |
| nl | Dutch | Nederlands | `nl-*` |
| et | Estonian | Eesti | `et-*` |
| fil | Filipino | Filipino | `fil-*` |
| fi | Finnish | Suomi | `fi-*` |
| de | German | Deutsch | `de-*` |
| fr | French | Français | `fr-*` |
| el | Greek | Ελληνικά | `el-*` |
| hi | Hindi | हिन्दी | `hi-*` |
| hu | Hungarian | Magyar | `hu-*` |
| id | Indonesian | Bahasa Indonesia | `id-*`; legacy `in` setting accepted |
| ja | Japanese | 日本語 | `ja-*` |
| ko | Korean | 한국어 | `ko-*` |
| lv | Latvian | Latviešu | `lv-*` |
| lt | Lithuanian | Lietuvių | `lt-*` |
| ms | Malay | Bahasa Melayu | `ms-*` |
| nb | Norwegian Bokmål | Norsk bokmål | `nb-*`; `no-*` and `nn-*` use this single Norwegian UI |
| pl | Polish | Polski | `pl-*` |
| pt | Portuguese | Português | `pt-*` |
| ro | Romanian | Română | `ro-*` |
| sk | Slovak | Slovenčina | `sk-*` |
| es | Spanish | Español | `es-*` |
| sv | Swedish | Svenska | `sv-*` |
| th | Thai | ไทย | `th-*` |
| tr | Turkish | Türkçe | `tr-*` |
| uk | Ukrainian | Українська | `uk-*` |
| vi | Vietnamese | Tiếng Việt | `vi-*` |

The chooser always uses a bilingual English/Italian heading. Every row shows a small runtime-drawn flag, the native name, the English name and a visible code, so users can recover after accidentally selecting an unfamiliar language.

Russian is intentionally unsupported and has no shipped dictionary, catalog entry, flag or culture mapping. Windows `ru-*`, old saved `language=ru` values and unknown saved codes fall back to English. Ukrainian remains supported.

Arabic, Hebrew, Persian and Urdu are deferred. The current safety interface communicates a fixed left-to-right OLD → CURRENT relationship, so RTL support requires a deliberate layout and usability pass rather than a text-only translation.

Automated tests require every dictionary to have exactly the English key set and the same numbered placeholders. New or materially revised translations should still receive native-speaker review before a stable release.
