# Bundled data source inventory

This document is the admission register for every dataset planned under
`src/Data`. No runtime data file may be added unless its row below has an exact
upstream version or retrieval date, a redistributable license, a documented
transformation, and a completed risk review in `manifest.json`.

The repository snapshots under `specs/` are design and conformance references.
They are not automatically approved as runtime inputs and must never be embedded
or copied wholesale into the product assembly.

Acquisition, offline transformation, review, manifest update, verification, and
commit steps are defined in `REFRESH.md`. Automated transformations live in
`tools/data/refresh.py`; builds and runtime code never download registry data.

## Admission states

- **Approved**: the stated source and transformation may be used after pinning a
  version, retaining the required notices, and recording a hash.
- **Authored**: values must be original project contributions with recorded
  provenance; they must not be scraped or copied from a reference library.
- **Hold**: do not create or ship the dataset until the named licensing or source
  issue is resolved and this register is updated.

## Standards datasets

| Planned path | Source and license | Refresh cadence | Permitted transformation | State and prohibited-use risk |
| --- | --- | --- | --- | --- |
| `manifest.json` | Project-authored metadata; distributed under the project's eventual data license. | Every data change. | Canonicalize paths, versions, retrieval dates, licenses, hashes, transforms, and review status. | **Authored.** Must not omit or overstate provenance, native locale coverage, or review status. |
| `standards/language-tags.json` | [IANA Language Subtag Registry](https://www.iana.org/assignments/language-subtag-registry/), protocol-registry data dedicated under CC0-1.0. Local snapshot: `specs/iana-language-subtag-registry.txt`, file date 2026-08-08. | On IANA file-date change; check monthly and before release. | Parse records; retain subtags, preferred values, prefixes, suppress-script, macrolanguage, scope, and deprecation dates; normalize keys without altering registered spelling. | **Approved.** Do not infer ethnicity, nationality, or an individual's language from a tag. |
| `standards/time-zones.json` | IANA Time Zone Database `zone1970.tab`; its header declares the file public domain. Local snapshot: `specs/iana-time-zones-zone1970.tab`, upstream annotation dated 2025-05-15. | Each tzdb release; check at least quarterly and before release. | Parse country codes, coordinates, zone IDs, and comments; do not copy narrative documentation. | **Approved.** A zone is not proof of a person's location; historical and future rules can change. |
| `standards/tlds.json` | [IANA Root Zone TLD list](https://data.iana.org/TLD/tlds-alpha-by-domain.txt). Local snapshot: `specs/iana-tlds-alpha-by-domain.txt`, serial 2026081400. The extract is admitted as reviewed factual registry data; IANA's protocol-registry CC0 dedication is not claimed for this separate root-zone list. | On upstream serial change; check before each release. | Retain only the serial and bare ASCII labels; lowercase, validate, deduplicate, and sort ordinally; omit comments and all delegation, contact, registrant, trademark, WHOIS, and descriptive data. | **Approved.** Presence is not proof that a generated domain is registrable, unowned, reachable, trustworthy, or safe; default generation must use reserved example domains. |
| `standards/media-types.json` | [IANA Media Types registry](https://www.iana.org/assignments/media-types/), CC0-1.0. Local snapshot: `specs/iana-media-types.xml`, updated 2026-08-13. | On registry update; check quarterly and before release. | Retain registered type/subtype names and normative references; omit contacts, templates, prose, and obsolete duplicates unless needed by an explicit API. | **Approved.** A media type does not establish that content is safe or correctly encoded. |
| `standards/ports.json` | [IANA Service Name and Transport Protocol Port Number Registry](https://www.iana.org/assignments/service-names-port-numbers/), CC0-1.0. Local snapshot: `specs/iana-service-names-port-numbers.csv`, retrieved 2026-08-14. | On registry update; check quarterly and before release. | Retain service name, port/range, transport, assignment state, and modification date; omit assignee/contact fields. | **Approved.** Never treat a registration as evidence that a service is running, safe, or reachable. Do not generate network targets from the registry. |
| `standards/countries.json` | Unicode CLDR territory data, Unicode-3.0; ISO-style codes are used only as factual identifiers. No ISO publication may be copied. | Each pinned stable CLDR release; check twice yearly. | Select alpha-2 identifiers and localized territory names needed by rollout locales; normalize and sort; record CLDR version. | **Approved** only from a pinned Unicode data file. Do not imply sovereignty, resolve territorial disputes, or infer nationality. |
| `standards/currencies.json` | Reviewed factual-field extract from SIX, the ISO 4217 Maintenance Agency, `List One` XML. Local snapshot: `specs/iso-4217-current-currencies.xml`, published 2026-01-01. No license to ISO standard text or explanatory SIX material is claimed. | Each SIX amendment; check monthly and before release. | Retain entity, currency name, alphabetic/numeric code, and minor units; deduplicate currency records while preserving entity mappings; omit standard prose and entries without a currency code. | **Approved for the reviewed factual extract.** Codes are not exchange rates, legal-tender advice, or evidence a currency is usable in a transaction. |
| `standards/iban-formats.json` | Pinned [schwifty](https://github.com/mdomke/schwifty/tree/2026.7.3/schwifty/iban_registry) 2026.7.3 `generated.json` transcription under MIT, with the exact license retained in `specs/`; the transcription identifies the SWIFT ISO 13616 IBAN Registry as upstream. | Each pinned schwifty release after checking its upstream registry refresh; check monthly and before release. | Retain country/territory code, actual IBAN prefix, IBAN/BBAN lengths, compact BBAN character pattern, SEPA flag, and bank/branch/account ranges; omit names, examples, prose, national checksum algorithms, contacts, and schwifty overwrite additions. | **Approved for the pinned MIT transcription and reviewed factual fields.** Never include bank/customer account lists or imply a generated or checksum-valid IBAN belongs to an account, is active, or can receive funds. |
| `standards/calling-codes.json` | Candidate authority is ITU-T E.164; local reference: `specs/itu-e164-country-codes-2016.pdf`. ITU claims copyright and ordinarily requires permission to reproduce its material. A separately licensed machine-readable source has not been selected. | On official amendment and at least annually. | Once a source is approved, retain only calling code, territory/service label, and source effective date. | **Approved.** Calling codes do not uniquely identify location, carrier, or person. |
| `standards/safe-ranges.json` | Reserved constants derived from IETF RFCs (including RFC 2606, RFC 3849, and RFC 5737) under the IETF Trust Legal Provisions, plus the NANPA 555-0100–0199 reservation stated by [NANPA](https://www.nanpa.com/numbering/555-line-numbers). Local references are under `specs/`. | On RFC errata/supersession or numbering-authority change; check annually and before release. | Encode only constants, prefixes, ranges, and citations; do not copy explanatory prose. | **Approved** for the cited factual constants after legal notices are retained. These are fixture/documentation ranges only, never proof of universal non-routing or permission for real-world use. |
| `standards/gs1.json` | Candidate source is GS1 General Specifications 26.0, local reference `specs/gs1-general-specifications-26.0.pdf`. The publication is copyrighted and no permission to redistribute its tables has been established. | Each GS1 General Specifications release. | Only independently implement mathematical check-digit rules unless permission is obtained; do not copy company prefixes, allocation tables, or prose. | **Approved.** Generated identifiers must never be described as allocated, licensed, or usable in commerce. |
| `standards/vehicle.json` | VIN syntax/check-digit facts derived from United States [49 CFR Part 565](https://www.ecfr.gov/current/title-49/subtitle-B/chapter-V/part-565), a U.S. government work; local reference `specs/ecfr-49-cfr-part-565-vin.html`. Fictional makes/models and non-U.S. rules require separate authored or licensed sources. | Check eCFR amendments quarterly and before release. | Encode character exclusions, positions, transliteration weights, modulus, and model-year rules; do not copy explanatory text or a live WMI/VIN registry. | **Approved** for eCFR rules; **Authored** for fictional vocabulary. Never claim manufacture, registration, ownership, or recall status. |
| `standards/device.json` | IMEI structural rules are design references from GSMA TS.06 v28.1 (`specs/gsma-ts06-imei-v28.1.pdf`). GSMA restricts distribution and licenses TAC data separately. Device vocabulary must be project-authored and fictional. | Review structural rules on each TS.06 release; authored terms annually. | Independently encode public length/check-digit behavior; use an explicitly synthetic non-allocated marker only after it is authoritatively documented. Never import TAC allocation data. | **Approved** for GSMA-derived tables; **Authored** for fictional device terms. Never claim a generated IMEI/TAC is allocated or suitable for network use. |

## Invariant datasets

| Planned path | Source and license | Refresh cadence | Permitted transformation | State and prohibited-use risk |
| --- | --- | --- | --- | --- |
| `invariant/words.json` | Original project contributions only; no dictionary, corpus, Faker dataset, or search-result scraping. Contributor must record authorship and review. | Manual review annually and whenever vocabulary changes. | Normalize Unicode, tag part of speech, remove duplicates, and attach project-authored weights. | **Authored.** Exclude personal data, slurs, defamatory pairings, secrets, distinctive quotations, and copyrighted passages. |
| `invariant/formats.json` | Project-authored masks derived from public standards already admitted above; each entry cites its controlling source. | With the controlling standard. | Convert rules into bounded masks and constraints; retain source/version per format. | **Authored.** Shapes must not be represented as assignments, credentials, accounts, or proof of validity. |
| `invariant/colors.json` | [W3C CSS Color Module Level 4, Candidate Recommendation Draft 6 August 2026](https://www.w3.org/TR/2026/CRD-css-color-4-20260806/), under the W3C Software and Document License 2023; additional display names and palettes must be project-authored. | On relevant W3C publication change; authored review annually. | Retain only standardized keyword names and numeric sRGB values; preserve the required W3C notice; construct original generic palettes. | **Approved** for the reviewed factual subset. Avoid offensive legacy names, brand/trademark palettes, accessibility claims, and culturally universal claims about color meaning. |
| `invariant/units.json` | [BIPM SI Brochure, ninth edition (2019), updated in 2026](https://www.bipm.org/en/publications/si-brochure/), CC-BY-4.0. | On SI Brochure edition/update. | Retain only factual quantity, unit, symbol, prefix, and exponent fields; credit BIPM; copy no explanatory prose. | **Approved** for the reviewed factual subset. Values are fixture data, not calibrated measurements, metrology certification, or safety advice. |
| `invariant/themes/*.json` | Original, generic project contributions for airline, animal, book/media, commerce, company, culture, education, food, health, science, software, transport, and other thematic vocabularies. Reference libraries in `specs/` are feature inventories only. | Manual review annually and on every contribution. | Normalize, categorize, deduplicate, and combine short original tokens; keep reviewer and provenance metadata. | **Authored.** No live people, brands, secrets, medical advice, copied plots/lyrics/prose, stereotypes, defamatory combinations, military operational data, or claims of completeness/authority. |

## Thematic source-code data

The compact thematic tables currently live in source-code data classes rather
than JSON resources. Their per-table provenance, authorship, license boundary,
and risk decisions were reviewed on 2026-08-15 and are recorded in
`THEMATIC-REVIEW.md`. That review applies the same admission criteria as the
manifest-backed resources but does not waive the requirement to add manifest
records if the tables are later moved into bundled resource files.

## Locale datasets

The following path families apply separately to every planned locale tag:

`en-US`, `en-GB`, `en-CA`, `en-AU`, `de-DE`, `de-AT`, `de-CH`,
`fr-FR`, `fr-CA`, `es-ES`, `es-MX`, `pt-PT`, `pt-BR`, `it-IT`,
`nl-NL`, `pl-PL`, `cs-CZ`, `sv-SE`, `nb-NO`, `da-DK`, `fi-FI`,
`ar-EG`, `ar-SA`, `fa-IR`, `he-IL`, `hi-IN`, `th-TH`, `tr-TR`,
`ru-RU`, `uk-UA`, `el-GR`, `ja-JP`, `ko-KR`, `zh-CN`, `zh-TW`,
`zh-HK`, `id-ID`, and `vi-VN`.

Every locale file is a distinct dataset. Its manifest entry must name the locale,
field-level sources, native-review status, and fallback declarations; a family row
below does not permit one locale's provenance to be silently reused for another.

| Planned path family | Source and license | Refresh cadence | Permitted transformation | State and prohibited-use risk |
| --- | --- | --- | --- | --- |
| `locales/<tag>/metadata.json` | Pinned Unicode CLDR release data under Unicode-3.0, plus the approved IANA language-tag registry. Contributor-reviewed corrections require separate provenance. | Each stable CLDR release; check twice yearly. | Select names, scripts, direction, date order, separators, territories, currencies, and fallback declarations; preserve aliases and source versions. | **Approved** from pinned files. Metadata is a default, not a claim about every speaker, resident, or political viewpoint. |
| `locales/<tag>/person.json` | Original contributions with locale-specific authorship/reviewer records. No voter rolls, phone books, customer data, social networks, baby-name sites, or copied Faker-family datasets. | Manual review annually and with each contribution. | Store short name components, weights, categories, and templates; remove real full-name pairings and rare identifying combinations. | **Authored.** Elevated privacy, bias, stereotyping, gender, religion, and live-person risks; no inference about protected traits. |
| `locales/<tag>/address.json` | Pinned [libaddressinput](https://github.com/google/libaddressinput) data (CC-BY-4.0) for field/format metadata, CLDR for localized names, and original fictional components. | Each pinned upstream release; check quarterly. | Attribute and reduce address metadata to needed fields/templates; generate original street/locality tokens; never import address examples as deliverable records. | **Approved** for pinned attributed metadata; **Authored** for values. No real household/business addresses, geocoding claims, or proof of deliverability. |
| `locales/<tag>/phone.json` | Candidate: pinned Google libphonenumber metadata under Apache-2.0, plus separately admitted national-authority safe ranges. ITU/GSMA publications alone are not approved copying sources. | Each pinned libphonenumber release; safe-range authority changes before release. | Retain structural masks, lengths, types, and formatting rules with NOTICE attribution; strip live example numbers unless independently reserved. | **Approved.** Do not infer subscriber, carrier, location, reachability, or ownership. |
| `locales/<tag>/company.json` | Original fictional vocabulary and locale-reviewed legal-form tokens from an explicitly licensed or public official source named per locale. No corporate registries or Faker datasets. | Annual manual review and on legal-form source change. | Compose generic tokens; segregate official legal-form abbreviations from fictional name fragments. | **Authored.** Avoid existing-company collisions, trademarks, tax IDs, and defamatory descriptions. |
| `locales/<tag>/commerce.json` | Original generic product/material/category vocabulary; currency metadata only from an admitted source. No retailer catalogs, product feeds, reviews, or price scraping. | Annual manual review. | Normalize categories and compose original descriptions; prices and ratings are generated, never sourced observations. | **Authored.** Avoid brands, regulated-product claims, copied reviews, misleading prices, and unsafe product instructions. |
| `locales/<tag>/text.json` | Original contributor-written short tokens and filler text with locale reviewer sign-off. CLDR may supply only licensed locale display data. No books, news, lyrics, websites, or model-generated imitation of living authors. | Annual manual review and every contribution. | Normalize, tokenize, tag part of speech, deduplicate, and assign authored weights. | **Authored.** Elevated copyright, profanity, dialect, political, and cultural-bias risk; do not present filler as translation. |
| `locales/<tag>/<optional-domain>.json` | Original or specifically licensed data named in that file's manifest entry. The general approval of a locale does not approve an optional domain. | Defined per source; at least annual review. | Only the transformation documented in the individual manifest record. | **Approved.** Apply the thematic risks above plus locale-specific privacy, cultural, regulatory, medical, and trademark review. |

## Sources that are reference-only

The following material may guide API coverage and tests but must not supply
runtime values: Faker.js, Python Faker, Bogus, Datafaker, Mimesis, MockNeat,
libphonenumber falsehoods, and the prose or examples of standards documents.
Their presence under `specs/` is not a data license. In particular, no source
may be transitively copied merely because a comparison project is open source.

## Required admission record

Before a JSON resource is committed, its `manifest.json` record must contain:

1. Resource path, schema version, data version, and SHA-256 hash.
2. Exact upstream URL, upstream release/file date, retrieval date, and local
   reference path when one exists.
3. SPDX license identifier or the exact non-SPDX permission, required notice,
   attribution, and reviewer who confirmed redistribution rights.
4. A reproducible transformation description (and script path when automated),
   including every field selected, removed, normalized, synthesized, or merged.
5. Native/fallback coverage and human-review status for locale files.
6. A risk decision covering privacy, contactability, allocation/credential
   claims, trademarks, copyright, cultural bias, regulated advice, and unsafe
   operational use.

Missing information leaves the resource on **Hold**. Generated artifacts are
reviewed and committed; runtime code never downloads or refreshes data.
