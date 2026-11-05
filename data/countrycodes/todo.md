# Universal Country Codes development plan

## Outcome and scope

Build a self-contained .NET library for ISO 3166 country and subdivision identifiers, data, lookup, normalization, search, and validation. The compiled library must work without network access, external services, ambient locale state, or packages outside the .NET runtime. Its generated dataset must be deterministic, versioned, immutable by encapsulation, and traceable to pinned sources.

The core scope is ISO 3166-1 (current countries and areas), ISO 3166-2 (current principal subdivisions), and ISO 3166-3 (formerly used country names/codes), supplemented only where it directly improves ISO 3166 use:

- UN M49 numeric identity and geographic region hierarchy.
- ISO reserved, formerly assigned, user-assigned, and unassigned code classification.
- CLDR/IANA locale identifiers, display names, aliases, and deprecated identifier mappings where their licenses and data coverage permit.
- Unicode normalization needed for stable international name matching.
- Provenance and version metadata for every generated snapshot.

Out of scope are currencies, calling codes, capitals, time zones, borders, coordinates, flags/images, population, and political recognition claims. Comparable libraries expose some of these, but they are not ISO 3166 code data. Runtime addition/removal of countries is also out of scope because it would conflict with the immutable, reproducible catalog; application-specific values should use ISO user-assigned ranges in the application layer.

## Research findings and feature baseline

The plan is informed by the following libraries and primary references. The comparison is about useful behavior, not API compatibility.

| Library | Relevant capabilities | Gaps or design lessons for this library |
| --- | --- | --- |
| [pycountry](https://github.com/pycountry/pycountry) | ISO 3166-1 alpha-2/alpha-3/numeric/name data, ISO 3166-2 subdivisions with type and parent hierarchy, ISO 3166-3 historic countries, iteration, indexed lookup, case-insensitive cross-field lookup, Unicode/accent-aware fuzzy search, translations | Its first-match lookup can hide ambiguity and its runtime mutation is incompatible with immutable generated tables; this library will return explicit status/matches and remain immutable. |
| [i18n-iso-countries](https://github.com/michaelwittig/node-i18n-iso-countries) | Alpha-2/alpha-3/numeric conversion, validation, localized official names and aliases, name-to-code lookup, locale registration, complete code maps | It focuses on current countries and omits subdivisions/history; this library will preserve its useful conversion/localization behavior while covering all three ISO 3166 parts. |
| [python-iso3166](https://github.com/deactivated/python-iso3166) | Small standalone current-country table, integer and string numeric lookup, iteration, and indexes by name/alpha-2/alpha-3/numeric | Its intentionally narrow current-country scope is a good model for low-level predictable indexes, but it lacks aliases, localization, subdivisions, historical data, and nuanced validation. |
| [countries-list](https://github.com/annexare/Countries) | Alpha code maps, English/native names, lookup by English or native name, typed generated data, compact/minimal maps | Its capital/currency/language/phone/continent extras are excluded unless the same concept comes authoritatively from UN M49; generated minimal maps and native-name lookup remain useful design ideas. |
| [Nager.Country](https://github.com/nager/Nager.Country) | .NET provider pattern, official names, all three current country code forms, English/translated-name lookup, supported locales, region/subregion classification | It does not provide the required subdivision and ISO 3166-3 depth; the provider pattern fits the no-public-static-method policy and UN M49 supplies authoritative region relationships. |
| [wooorm/iso-3166](https://github.com/wooorm/iso-3166) | Assigned and reserved ISO 3166-1 records, conversion indexes, ISO 3166-2 parents, ISO 3166-3 change/merge/split successor relationships, explicit user-assigned ranges | It demonstrates that code assignment state and one-to-many historical transitions must be modeled explicitly rather than normalized to a guessed current country. |

Primary-source conclusions:

- ISO 3166-1 defines two-letter, three-letter, and zero-padded three-digit code elements; alpha-2 is the general-purpose identifier and numeric-3 is script-independent.
- ISO 3166-2 identifiers are the country alpha-2 prefix, a hyphen, and up to three alphanumeric characters. The suffix is not globally unique. Subdivisions can have subdivision parents.
- ISO 3166-3 alpha-4 entries represent names removed since 1974 and can describe renames, mergers, or splits. Successors can be zero, one, or many current/historical records.
- Reserved states are semantically distinct from assigned countries. User-assigned alpha-2, alpha-3, and numeric ranges are valid private syntax but are not universal identifiers.
- UN M49 numeric values and hierarchy records are related to, but not identical with, ISO assignment semantics; leading zeroes must never be discarded in canonical text.
- Names are display data and can vary by language and source. Stable joins and equality must use canonical code strings, never display names.
- BCP 47 locale tags and Unicode normalization must be processed with ordinal/invariant rules; `CurrentCulture` must never affect identifiers, sorting, lookup, validation, or generated output.

The downloaded research and source snapshots are inventoried in [specs/SOURCES.md](specs/SOURCES.md). The normative ISO publications are not vendored because ISO restricts copying/downloading; a licensed copy can be used during review without becoming a repository artifact.

## Complete feature inventory, low level to high level

### 1. Source acquisition and generated data

- Pinned inputs with source URL, upstream version/revision, retrieval date, license, and SHA-256.
- Deterministic offline generation: normal builds compile checked-in `.g.cs` files and never fetch data.
- Explicit source precedence and field-level provenance when ISO-derived Debian data, UN M49, and CLDR overlap.
- Strict reconciliation failures for duplicate canonical identifiers, conflicting code triples, unknown parents, cycles, invalid dates, malformed locale tags, or unexplained source deletions.
- Stable ordinal ordering by canonical alpha-2, full subdivision code, alpha-4, locale, and normalized alias key.
- Dataset schema version, semantic data version, effective date, generator version, source revisions, and aggregate content checksum.
- Generated internal rows and indexes that are never exposed; detached public data objects and arrays protect the canonical tables from caller mutation.
- No external library dependency in `src/`; use only built-in .NET APIs such as `System`, collections, globalization, text, and `ReadOnlySpan<char>` where useful.

### 2. ISO 3166-1 country records and code classification

- Complete current assigned records with canonical alpha-2, alpha-3, zero-padded numeric-3 text, numeric integer convenience value, short name, optional official name, and available aliases.
- A single locale-independent primary identity: canonical upper-case alpha-2 for current countries; all joins use canonical codes.
- Code-kind recognition for alpha-2, alpha-3, numeric-3, and alpha-4 without silently treating malformed text as unknown data.
- Assigned, exceptionally reserved, transitionally reserved, indeterminately reserved, formerly assigned, user-assigned, and unassigned classifications where sources permit.
- Complete algorithmic user-assigned range classification: `AA`, `QM`–`QZ`, `XA`–`XZ`, `ZZ`; `AAA`–`AAZ`, `QMA`–`QZZ`, `XAA`–`XZZ`, `ZZA`–`ZZZ`; numeric `900`–`999`.
- Leading-zero-preserving numeric conversion in both directions; integer input is accepted only through an explicitly named operation and canonicalized to exactly three digits.
- Enumeration and filtering by assignment state with stable ordering.

### 3. Names, aliases, and locales

- Distinguish ISO short, ISO official, common, native/localized, alternate, deprecated, and historical names.
- Preserve exact display spelling in NFC while generating separate matching keys; matching never rewrites display data.
- Exact name lookup against canonical English names and curated aliases.
- Localized country and subdivision display names from pinned CLDR/UN data where present, with explicit locale and source metadata.
- Canonical BCP 47 locale identifiers, case normalization by subtag type, deprecated-subtag replacement, and deterministic parent fallback (for example, `pt-BR` to `pt` only when configured).
- Supported-locale enumeration and exact locale availability checks.
- Explicit ambiguity when one name/alias identifies multiple countries or subdivisions; no first-match behavior.
- No claim of translation completeness: absence is represented as missing data and does not fall back unless the caller selects a fallback policy.

### 4. ISO 3166-2 subdivisions

- Complete available current subdivision records with full canonical code, country alpha-2, local suffix, canonical name, optional localized names/aliases, subdivision type, optional parent code, and status.
- Exact full-code lookup and lookup by country plus local suffix; bare suffix lookup is never globally assumed unique.
- Enumeration of all subdivisions or those belonging to a country, with stable code ordering.
- Exact and normalized name lookup globally or constrained to a country.
- Parent, direct-child, ancestor, and descendant traversal with cycle protection and deterministic order.
- Country resolution from the full subdivision prefix.
- Withdrawn/deprecated subdivision aliases and replacement mappings where CLDR or retained update history provides them.
- Clear distinction between an invalid subdivision shape, an unknown country prefix, an unknown well-formed code, and a withdrawn/deprecated code.

### 5. ISO 3166-3 and historical transitions

- Former country records with available former alpha-2, alpha-3, numeric-3, alpha-4, name, withdrawal date, and assignment state.
- Lookup by alpha-4 and any available former identifier or name.
- Transition type classification for significant name/code change, merger, split, and other/unknown changes.
- Explicit predecessor and successor links, including one-to-many and multi-step transitions.
- Successor resolution that returns all candidates and never guesses across a split.
- Historical code/name normalization that can report current replacement, multiple replacements, no replacement, or an unchanged/current identifier.
- Chronological and code-ordered historical enumeration where reliable dates are present.

### 6. UN M49 geographic hierarchy

- Script-independent M49 country/area code identity cross-checked against ISO numeric-3.
- World, region, subregion, and intermediate-region code/name records with parent relationships.
- Country-to-region ancestry and region-to-country enumeration.
- Separation of geographic grouping from ISO assignment status so aggregates are never mistaken for ISO countries.
- Multilingual UN names only where the pinned input provides them, retaining their locale and provenance.

### 7. Normalization

- Country alpha normalization: optional outer-whitespace removal, invariant ASCII upper-casing, exact length/character checks, and canonical zero padding for explicitly numeric inputs.
- Subdivision normalization: invariant country prefix, canonical ASCII hyphen, invariant suffix upper-casing, and controlled acceptance of common separator variants only in permissive mode.
- Locale normalization according to BCP 47/IANA casing and preferred values.
- Name matching keys based on Unicode normalization, invariant case folding available in .NET, whitespace collapse, and controlled punctuation/diacritic folding.
- Separate conservative and permissive modes; conservative mode never strips meaningful identifier characters.
- Idempotence: normalizing canonical input returns the same canonical text.
- Structured normalization results carrying status, detected kind/state, canonical value if any, and ambiguity candidates.
- String and `ReadOnlySpan<char>` paths for short code operations, with identical behavior and separately named methods to comply with the no-overloading rule.

### 8. Validation

- Separate syntactic validation from catalog membership and policy validation.
- Validation policies for syntax-only, currently assigned only, any known ISO state, historical allowed, and user-assigned allowed.
- Country alpha-2, alpha-3, numeric-3, alpha-4, any-country-code, subdivision-code, locale-tag, and name-input validation.
- Structured failure kinds for null/empty, length, character set, shape, unknown, reserved, formerly assigned, user-assigned disallowed, unknown country prefix, deprecated, ambiguous, and unsupported locale.
- Exception-less validation results for frequent expected failures plus required lookup methods that throw focused public exceptions.
- Validation messages are stable programmatic data, not localized control flow.

### 9. Exact lookup, conversion, and resolution

- Exact current-country lookup by each code form, integer numeric value, canonical/official name, alias, or auto-detected identifier.
- Explicit code conversion through a resolved country rather than independent maps that can disagree.
- Result objects with `Found`, `NotFound`, `Ambiguous`, or `InvalidInput`, optional entity, and complete deterministic candidate list.
- Required variants for callers who prefer exceptions on invalid/not-found/ambiguous results.
- Lookup filters controlling whether reserved and historical records participate.
- Locale-aware display-name retrieval without locale-dependent identifier comparison.
- Stable full-catalog snapshots and filtered lists.

### 10. Search and high-level conveniences

- Deterministic country and subdivision search across codes, canonical names, localized names, aliases, and historical names under explicit options.
- Ranked match classes: exact code, exact canonical name, exact alias, normalized exact, prefix, token, substring, and bounded fuzzy match.
- Unicode/diacritic-insensitive discovery while retaining exact spelling in results.
- Country-aware subdivision ranking and the ability to resolve a country through a matching subdivision (for example, England to the United Kingdom) without hiding why it matched.
- Stable integer scores, tie breakers, maximum-result limits using signed counts, and complete ambiguity reporting.
- Derived regional-indicator flag emoji from an assigned alpha-2 code as an explicitly non-ISO convenience; no flag images or geopolitical claim data.
- Thread-safe concurrent reads with no mutable global state.

## Architecture within the simplified C# subset

- Every `src/**/*.cs` file will use one file-scoped namespace, classes/enums/interfaces only, ordinary block-bodied methods, positional arguments, and the smallest allowed language subset.
- The design uses public fields rather than properties, no records/structs/tuples/patterns/LINQ/`try`/overloads/`out`/default parameters/value-type nullables, and no public static methods.
- Missing value-type data uses an explicit `Has...` boolean plus a non-nullable value; missing reference data uses nullable reference fields.
- Every public member receives the XML documentation required by `syntax.md` as part of its implementation, not as a separate documentation workstream.
- Each public operation is declared on an interface and implemented as an instance method on the single public `CountryCodeService` class. Method names remain unique across the combined interface set.
- Public result/data instances are detached copies. Generated internal row classes and arrays are never returned, so callers can mutate their copies without changing future results.
- Code parsing and normalization use `ReadOnlySpan<char>` internally and through distinctly named span methods where it avoids transient strings; public collection results use arrays for portability.
- `src/` has no package dependency. Generator tooling may parse the pinned files with built-in .NET XML/JSON/text APIs and must not become a runtime dependency.

## Proposed public API and files

These are type and operation names, not final signatures. Field details may be refined without changing the responsibility of each type.

### Data models — `src/Api/Data`

- `Country.cs` — detached country snapshot: alpha-2, alpha-3, numeric text/value, canonical identity, assignment status, canonical/official names, aliases, localized names, M49 region links, and derived flag emoji.
- `LocalizedName.cs` — locale tag, value, name kind, and provenance used by country/subdivision/region models.
- `Subdivision.cs` — full code, country code, suffix, name/type, optional parent, status, aliases, and localized names.
- `HistoricalCountry.cs` — former codes/name, alpha-4, optional withdrawal date represented without value-type nullability, and transition links.
- `CountryChange.cs` — alpha-4 change identity, change kind, predecessor identifiers, successor identifiers, and available effective date.
- `GeographicRegion.cs` — M49 code, canonical/localized names, level, optional parent, and child/country identifiers.
- `CountryLookupResult.cs` — lookup status, optional country, and candidate array.
- `SubdivisionLookupResult.cs` — lookup status, optional subdivision, and candidate array.
- `HistoricalLookupResult.cs` — lookup status, optional historical record/change, and candidate arrays.
- `CodeNormalizationResult.cs` — normalization status, detected code kind/state, canonical value, and replacement candidates.
- `ValidationResult.cs` — validity boolean, normalized input when available, and issue array.
- `ValidationIssue.cs` — stable failure kind, input position/length using signed integers, and message.
- `CountrySearchMatch.cs` — country, match kind/source text, normalized key, and integer score.
- `SubdivisionSearchMatch.cs` — subdivision, owning country, match kind/source text, and integer score.
- `SearchOptions.cs` — search mode, locale/fallback, inclusion policies, diacritic/punctuation behavior, and signed result limit.
- `DataVersion.cs` — schema/data versions, effective/generated dates as canonical text, pinned source versions, and aggregate checksum.

### Contracts — `src/Api/Contracts`

- `ICountryCatalog.cs` — `GetCountries`, assignment-state filtering, `FindCountryByAlpha2`, `FindCountryByAlpha2Span`, `FindCountryByAlpha3`, `FindCountryByAlpha3Span`, `FindCountryByNumericCode`, `FindCountryByNumericValue`, `FindCountryByName`, `FindCountryByAnyIdentifier`, and distinct required lookup methods.
- `ISubdivisionCatalog.cs` — all/by-country enumeration, code/string/span lookup, country-plus-suffix lookup, name lookup, parent/children/ancestors/descendants, country resolution, and required lookup.
- `IHistoricalCountryCatalog.cs` — enumeration, former-code/name lookup, alpha-4 change lookup, country change history, and successor resolution.
- `ICountryCodeNormalizer.cs` — distinct alpha-2, alpha-3, numeric, alpha-4, any-country, subdivision, locale, and name-key normalization methods plus span variants for short codes.
- `ICountryCodeValidator.cs` — distinct validation methods for every supported code kind, subdivision, locale, and name using an explicit validation policy.
- `ILocalizedNameCatalog.cs` — supported locale enumeration, locale availability, localized country/subdivision/region name lookup, and locale fallback resolution.
- `ICountrySearch.cs` — deterministic country and subdivision search plus country discovery through subdivision matches.
- `IGeographicRegionCatalog.cs` — region enumeration/lookup, region ancestry, country regions, and countries for a region.
- `IDataVersionProvider.cs` — current immutable data-version snapshot retrieval.

### Static data — `src/Api/Static`

- `CountryCodeConstants.cs` — public `const` fields only for code lengths, subdivision separator, numeric bounds, and schema constants.
- `CountryDataMetadata.cs` — public `const` or `static readonly` immutable scalar/string fields only for the compiled data version, effective date, source revisions, and content checksum; no tables or methods.

### Enums — `src/Api/Enums`

- `CountryCodeKind.cs` — unknown, alpha-2, alpha-3, numeric-3, alpha-4, and subdivision.
- `CodeAssignmentStatus.cs` — assigned, exceptionally reserved, transitionally reserved, indeterminately reserved, formerly assigned, user-assigned, and unassigned.
- `CountryNameKind.cs` — ISO short, ISO official, common, native/localized, alias, deprecated, and historical.
- `SubdivisionStatus.cs` — active, deprecated, withdrawn, and unknown.
- `CountryChangeKind.cs` — significant change, merge, split, and other/unknown.
- `GeographicRegionLevel.cs` — world, region, subregion, intermediate region, and country/area.
- `LookupStatus.cs` — found, not found, ambiguous, and invalid input.
- `NormalizationStatus.cs` — already canonical, normalized, replaced, ambiguous, and invalid.
- `ValidationPolicy.cs` — syntax only, assigned only, any known ISO, allow historical, and allow user-assigned.
- `ValidationFailureKind.cs` — stable validation failure categories described above.
- `SearchMode.cs` — exact normalized, prefix, token, substring, and fuzzy.
- `SearchMatchKind.cs` — exact code/name/alias, normalized, prefix/token/substring/fuzzy, localized, historical, and subdivision-derived.

### Exceptions — `src/Api/Exceptions`

Each exception derives directly from `System.Exception` and has at most one public constructor.

- `InvalidCountryCodeException.cs`
- `CountryNotFoundException.cs`
- `AmbiguousCountryException.cs`
- `InvalidSubdivisionCodeException.cs`
- `SubdivisionNotFoundException.cs`
- `UnsupportedLocaleException.cs`

### Public implementation — `src/Api/Classes`

- `CountryCodeService.cs` — one public constructor; implements all operation interfaces; exposes only interface-declared instance methods and no public fields or static methods.

### Internal runtime source — `src/Internal`

- `InternalCountryRow.cs`, `InternalSubdivisionRow.cs`, `InternalHistoricalCountryRow.cs`, `InternalCountryChangeRow.cs`, `InternalRegionRow.cs`, and `InternalNameRow.cs` — compact non-public generated-row representations.
- `AsciiCodeUtility.cs` — ordinal ASCII classification, case normalization, zero padding, separators, and span parsing.
- `UnicodeNameUtility.cs` — NFC display preservation and invariant searchable-key construction without LINQ.
- `LocaleTagUtility.cs` — BCP 47 parsing/canonicalization, preferred values, and fallback chains.
- `CountryIndex.cs`, `SubdivisionIndex.cs`, `HistoricalIndex.cs`, `NameIndex.cs`, and `RegionIndex.cs` — immutable-by-encapsulation sorted indexes and binary search.
- `CountryMapper.cs`, `SubdivisionMapper.cs`, `HistoricalMapper.cs`, and `RegionMapper.cs` — detached public snapshot creation.
- `ValidationEngine.cs`, `NormalizationEngine.cs`, and `SearchEngine.cs` — shared internal behavior behind the public contracts.
- `SearchScorer.cs` — deterministic bounded scoring and ordinal tie breaks.

### Checked-in generated source — `src/Generated`

- `CountryTable.g.cs` — current assigned internal country rows.
- `ReservedCodeTable.g.cs` — known reserved/former records plus generated user-assigned range metadata.
- `CountryNameTable.g.cs` — canonical, alternate, localized, and deprecated country names.
- `SubdivisionTable.g.cs` and `SubdivisionNameTable.g.cs` — current subdivisions, parent links, aliases, and localized names.
- `HistoricalCountryTable.g.cs` and `CountryChangeTable.g.cs` — ISO 3166-3 records and predecessor/successor links.
- `RegionTable.g.cs` — UN M49 hierarchy and ISO country mappings.
- `LookupIndex.g.cs` and `SearchIndex.g.cs` — pre-sorted exact and normalized keys.
- `GeneratedDataMetadata.g.cs` — internal source manifest and snapshot checksums used to populate the public metadata.

### Generator and source manifest — outside `src/`

- `tools/CountryCodes.DataGenerator/CountryCodes.DataGenerator.csproj` and ordinary class files for acquisition verification, ISO JSON, UN HTML/table, CLDR XML, IANA registry parsing, reconciliation, invariant validation, stable emission, and checksum calculation.
- `data/source-manifest.json` — pinned URLs/revisions/checksums/license identifiers and declared source precedence.
- `data/overrides.json` — reviewed, minimal aliases or corrections with reason, provenance, effective dates, and expected upstream-removal condition; never an untraceable hand-edited country table.

### Planned test source — `tests`

- Focused fixtures for every parser and malformed input under `tests/Fixtures`.
- `GeneratedDataTests.cs`, `CountryCatalogTests.cs`, `SubdivisionCatalogTests.cs`, `HistoricalCountryCatalogTests.cs`, `NormalizerTests.cs`, `ValidatorTests.cs`, `LocalizedNameCatalogTests.cs`, `CountrySearchTests.cs`, `GeographicRegionCatalogTests.cs`, `DataVersionTests.cs`, `PublicDataModelTests.cs`, `StaticDataTests.cs`, `CultureIndependenceTests.cs`, `UnicodeEdgeCaseTests.cs`, `ImmutabilityTests.cs`, `ConcurrencyTests.cs`, `SpanParityTests.cs`, and `EndToEndTests.cs`.

## Ordered implementation tasks

### Phase 1 — source policy and deterministic generation

- [DONE] T001: Freeze the first dataset scope and source precedence for ISO-derived Debian data, UN M49, CLDR, IANA, and reviewed overrides; record license compatibility and the rule that normative licensed ISO files are review inputs but are never copied into the repository or package.
- [DONE] T002: Create `data/source-manifest.json` with pinned revisions, retrieval dates, SHA-256 values, license identifiers, schema/data version rules, and fail-closed handling for unavailable or changed inputs.
- [DONE] T003: Create the built-in-.NET-only data-generator project and an acquisition-verification command that validates pinned local inputs before parsing and never participates in normal `src/` builds.
- [DONE] T004: Implement fixture-backed parsers for Debian ISO 3166-1/2/3 JSON, UN M49 country/history/hierarchy tables, CLDR names/aliases/subdivision containment XML, and the IANA language-subtag registry.
- [DONE] T005: Implement normalized generator staging classes that retain source, upstream key, locale, effective date, and field-level provenance while preserving canonical display spelling and numeric leading zeroes.
- [DONE] T006: Implement source reconciliation and strict invariants for unique code triples, legal code shapes/ranges, M49 agreement, known country prefixes, valid parents, acyclic subdivision/region hierarchies, valid transitions, valid BCP 47 tags, and explained source deltas.
- [DONE] T007: Implement reviewed `data/overrides.json` processing with a required reason/provenance/expiry condition, conflict detection, and deterministic precedence below authoritative upstream identifiers.
- [DONE] T008: Emit deterministic current-country and reserved-code generated rows, including assignment-state metadata and algorithmic user-assigned classification inputs, in stable ordinal order.
- [DONE] T009: Emit deterministic canonical, official, common, native/localized, alias, deprecated, and historical name rows with NFC display values and precomputed invariant search keys.
- [DONE] T010: Emit deterministic subdivision rows, localized/alias name rows, country links, types, parents, and available deprecated/replacement mappings with hierarchy validation.
- [DONE] T011: Emit deterministic historical-country and country-change rows with alpha-4 identities, available withdrawal dates, transition kinds, and all predecessor/successor links.
- [DONE] T012: Emit deterministic UN M49 hierarchy rows and ISO country mappings while keeping aggregate regions distinct from countries/areas.
- [DONE] T013: Emit sorted exact/search indexes and generated metadata containing schema version, data version, effective date, generator version, source revisions, and aggregate content checksum.
- [DONE] T014: Make regeneration byte-for-byte reproducible across supported cultures and repeated runs, and fail when checked-in generated output differs from a verified regeneration.

### Phase 2 — public API foundation

- [DONE] T015: Implement all planned public enums under `src/Api/Enums`, with explicit int32 values and use by at least one public data model or interface.
- [DONE] T016: Implement `Country`, `LocalizedName`, `Subdivision`, `HistoricalCountry`, `CountryChange`, and `GeographicRegion` as detached public data-holding classes under `src/Api/Data` using fields and at most one constructor each.
- [DONE] T017: Implement all lookup, normalization, validation, search, options, issue, match, and data-version result classes under `src/Api/Data`, including explicit `Has...` fields wherever a value-type value can be absent.
- [DONE] T018: Implement the planned public exceptions under `src/Api/Exceptions`, each deriving directly from `System.Exception` with one constructor and stable failure context.
- [DONE] T019: Implement `CountryCodeConstants` and `CountryDataMetadata` under `src/Api/Static` using only permitted const/static-readonly scalar or string fields and no methods or exposed mutable tables.
- [DONE] T020: Implement all planned operation interfaces under `src/Api/Contracts`, ensuring instance-only methods, unique method names across the combined interface set, no overloads/defaults/`out` parameters, and distinctly named span variants.
- [DONE] T021: Implement the one-constructor `CountryCodeService` under `src/Api/Classes`, make it implement every operation interface, and expose no public member that is not declared by those interfaces.

### Phase 3 — low-level parsing, indexes, and isolation

- [DONE] T022: Implement `AsciiCodeUtility` with allocation-free span parsing for ASCII letters/digits, invariant upper-casing, exact length checks, three-digit formatting, subdivision separator handling, and ordinal comparisons.
- [DONE] T023: Implement `LocaleTagUtility` for RFC 5646/IANA structural validation, canonical casing, preferred/deprecated subtag replacement, and deterministic fallback chains without ambient culture behavior.
- [DONE] T024: Implement `UnicodeNameUtility` for NFC display normalization and invariant match keys with configurable whitespace, punctuation, compatibility, and diacritic folding.
- [DONE] T025: Implement immutable-by-encapsulation country, subdivision, historical, name, and region indexes over sorted generated rows using explicit loops/binary search and no LINQ.
- [DONE] T026: Implement detached mappers that clone every public entity, nested name array, alias array, and candidate array so no public mutation can alter generated rows, indexes, or later results.
- [DONE] T027: Implement lazy or eager initialization with safe locking/read-only publication so all operations remain deterministic and thread-safe under concurrent first use and repeated reads.

### Phase 4 — exact country and subdivision operations

- [DONE] T028: Implement current-country enumeration and assignment-state filtering with stable alpha-2 ordering and complete detached snapshots.
- [DONE] T029: Implement exact country lookup by alpha-2, alpha-3, numeric text, numeric integer, canonical/official name, alias, and auto-detected identifier, including string/span parity and explicit invalid/not-found/ambiguous results.
- [DONE] T030: Implement required country lookup variants that translate structured failure states into `InvalidCountryCodeException`, `CountryNotFoundException`, or `AmbiguousCountryException` without changing successful semantics.
- [DONE] T031: Implement code conversion through resolved country records so every alpha-2/alpha-3/numeric conversion is consistent and always preserves the canonical zero-padded numeric string.
- [DONE] T032: Implement all/by-country subdivision enumeration, exact full-code string/span lookup, country-plus-suffix lookup, country resolution, and required lookup exceptions.
- [DONE] T033: Implement subdivision exact/normalized name lookup with optional country constraint and complete ambiguity candidates.
- [DONE] T034: Implement subdivision parent, direct-child, ancestor, and descendant traversal with cycle guards, missing-parent reporting, and stable full-code ordering.
- [DONE] T035: Implement deprecated/withdrawn subdivision recognition and replacement results without silently turning a one-to-many replacement into one guessed code.

### Phase 5 — historical, region, locale, normalization, and validation operations

- [DONE] T036: Implement historical-country enumeration and lookup by every available former code/name plus ISO 3166-3 alpha-4 change lookup.
- [DONE] T037: Implement predecessor/successor and multi-step transition traversal for rename, merger, split, and unknown changes, returning all deterministic candidates and guarding cycles.
- [DONE] T038: Implement UN M49 region enumeration/lookup, region ancestry, country-to-region traversal, and region-to-country enumeration without treating aggregate region codes as ISO countries.
- [DONE] T039: Implement supported-locale enumeration, exact locale availability, localized country/subdivision/region name lookup, and explicit configured locale fallback.
- [DONE] T040: Implement conservative and permissive normalization for alpha-2, alpha-3, numeric-3, alpha-4, any-country identifier, subdivision code, locale tag, and name key with structured idempotent results.
- [DONE] T041: Implement historical/deprecated normalization outcomes for unchanged current, one replacement, multiple replacements, no replacement, reserved, and user-assigned identifiers.
- [DONE] T042: Implement syntax-only and policy-aware validation for every country code kind, subdivision, locale, and name with stable structured issues and no exception for expected invalid input.
- [DONE] T043: Implement exact assignment classification for every syntactically possible alpha-2 code, known alpha-3/numeric/alpha-4 values, all specified user-assigned ranges, and otherwise unassigned values.

### Phase 6 — deterministic discovery conveniences

- [DONE] T044: Implement deterministic country search across codes, canonical/official/localized names, aliases, and historical names using the declared exact/normalized/prefix/token/substring/fuzzy ranking tiers.
- [DONE] T045: Implement deterministic subdivision search globally and within a country, including parent/country context and stable ranking/tie breaking.
- [DONE] T046: Implement country discovery through subdivision matches while marking the match as subdivision-derived and preserving all ambiguity rather than silently returning the first country.
- [DONE] T047: Implement signed maximum-result enforcement, bounded fuzzy distance, stable integer scores, option validation, and culture-independent result ordering.
- [DONE] T048: Implement derived regional-indicator flag emoji for currently assigned alpha-2 records, returning an explicit unavailable result for non-country/reserved/historical/user-assigned inputs.

### Phase 7 — generator and dataset verification tests

- [OPEN] T049: Add parser tests for valid, minimal, malformed, duplicate, conflicting, missing-field, invalid-Unicode, invalid-locale, and upstream-schema-change fixtures for every generator input format.
- [OPEN] T050: Add reconciliation tests covering precedence, provenance retention, reviewed override rules, unexplained deletion failures, conflicting code triples, M49 mismatch, bad dates, unknown parents, and hierarchy/transition cycles.
- [OPEN] T051: Add deterministic-emission tests that regenerate twice under multiple cultures, compare byte-for-byte output and checksums, verify stable ordering/escaping, and confirm no network access is needed for generation from pinned inputs.
- [OPEN] T052: Add full generated-country tests for snapshot counts and every row's alpha-2/alpha-3/numeric uniqueness, shape, zero padding, canonical identity, required names, assignment status, and source cross-check.
- [OPEN] T053: Add exhaustive assignment-classification tests across all 676 alpha-2 combinations, boundary samples for every alpha-3 user-assigned range, all numeric values `000`–`999`, and all known reserved/former records.
- [OPEN] T054: Add full subdivision dataset tests for unique full codes, legal prefix/suffix shape, known countries, complete types/names, valid parent links, acyclic hierarchies, deterministic ordering, and valid deprecated replacements.
- [OPEN] T055: Add full historical dataset tests for unique alpha-4 records, valid available former identifiers/dates, known transition types, referential predecessor/successor links, one-to-many preservation, and acyclic traversal behavior.
- [OPEN] T056: Add full localized-name/alias tests for NFC storage, valid canonical locales, duplicate-key ambiguity, source provenance, CLDR deprecated mappings, fallback coverage, and deterministic normalized keys.
- [OPEN] T057: Add full UN M49 tests for three-digit shapes, ISO numeric agreement, unique hierarchy nodes, valid parent levels, acyclic ancestry, and deterministic country membership.

### Phase 8 — complete public API behavior tests

- [OPEN] T058: Add tests for every public enum value, every public data class constructor/field/absence convention, detached nested arrays, and every public static constant/metadata field.
- [OPEN] T059: Add tests for every `ICountryCatalog` operation covering success, case normalization, whitespace modes, every code form, integer numeric padding, alias/name ambiguity, invalid input, not found, filtering, stable enumeration, span parity, and each required exception.
- [OPEN] T060: Add tests for every `ISubdivisionCatalog` operation covering full-code/country-suffix/name lookup, global suffix ambiguity, invalid prefix, unknown code, current/deprecated status, hierarchy traversal, stable ordering, span parity, and required exceptions.
- [OPEN] T061: Add tests for every `IHistoricalCountryCatalog` operation covering former code/name/alpha-4 lookup, rename/merge/split/unknown transitions, zero/one/many successors, multi-hop traversal, ambiguity, invalid input, and cycle guards.
- [OPEN] T062: Add tests for every `ICountryCodeNormalizer` operation and status using canonical, mixed-case, whitespace, leading-zero, separator-variant, reserved, former, user-assigned, unassigned, invalid, deprecated, one-replacement, and multi-replacement inputs.
- [OPEN] T063: Add tests for every `ICountryCodeValidator` operation and validation policy, asserting all failure kinds, issue positions/lengths, normalized values, multiple simultaneous issues where supported, and exception-less behavior.
- [OPEN] T064: Add tests for every `ILocalizedNameCatalog` operation covering supported/unsupported/canonicalized/deprecated locales, exact display retrieval, missing translations, configured fallback, no-fallback behavior, native names, and ambiguity.
- [OPEN] T065: Add tests for every `ICountrySearch` operation and ranking tier, including accent/composed/decomposed forms, punctuation/spacing variants, aliases, localized/historical names, subdivision-derived country matches, tie breakers, limits, and invalid options.
- [OPEN] T066: Add tests for every `IGeographicRegionCatalog` and `IDataVersionProvider` operation, including all hierarchy levels, missing nodes, country ancestry, membership, detached results, version fields, source revisions, and checksum agreement with generated metadata.
- [OPEN] T067: Add culture-independence tests under invariant, Turkish, Arabic, and at least one non-Latin culture to prove identical codes, normalization, validation, sorting, lookup, fallback, search scores, and generated snapshots.
- [OPEN] T068: Add Unicode edge-case tests for NFC/NFD equivalence, compatibility forms under each mode, combining marks, apostrophe/hyphen variants, non-breaking/repeated whitespace, non-ASCII case behavior, malformed surrogate input, and display-value preservation.
- [OPEN] T069: Add boundary and adversarial input tests for null/empty/very long strings, all code lengths around boundaries, non-ASCII lookalikes, control characters, malformed subdivision separators, invalid locale tags, numeric overflow text, and negative/over-999 numeric values.
- [OPEN] T070: Add immutability and concurrency tests that mutate every returned public object/array, verify future results and static metadata remain unchanged, and exercise simultaneous first-use/read operations without races or state leakage.
- [OPEN] T071: Add end-to-end tests for representative workflows: normalize then validate then resolve; convert among all country codes; localized name to country; country to subdivisions and hierarchy; former code to all successors; subdivision to country; and country to M49 ancestry.
- [OPEN] T072: Configure the existing coverage tooling to require complete line and branch coverage of every behavior exposed through `src/Api`, including all successful and failure paths in its runtime implementation, with explicitly reviewed exclusions limited to generated constant data that has exhaustive integrity tests.

## Completion criteria

- Every feature above is implemented by a checked task or is explicitly re-scoped before implementation begins.
- `src/` references only built-in .NET assemblies and complies with the repository's restricted C# syntax and API placement by construction.
- All current, subdivision, historical, reserved, alias/localized, and M49 records in the pinned supported inputs are represented or rejected with an explicit generator diagnostic.
- Generation is deterministic and offline after acquisition; ordinary build/test runs never fetch data.
- All public methods have successful, invalid, not-found, ambiguous, reserved/historical/user-assigned, culture, Unicode, mutation-isolation, and concurrency behavior covered where applicable.
- Canonical identifier behavior is locale-independent, numeric text preserves leading zeros, historical splits never guess, bare subdivision suffixes never imply global uniqueness, and callers cannot mutate canonical tables.
