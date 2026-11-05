# Universal ISO 4217 Currency Codes Library — Development Plan

## Goal and scope

Build a zero-runtime-dependency C# library that provides a complete, deterministic snapshot of ISO 4217 currency and funds data, historical designations where the maintenance data permits, ISO 3166-compatible territory mappings, code normalization and validation, date-aware lookup, and metadata/rules for representing and formatting monetary amounts.

The library is a reference-data and amount-rules library. It will not own exchange rates, conversions between currencies, monetary arithmetic, allocation, accounting, persistence adapters, cryptocurrency registries, or a full locale engine. Those are separate concerns in Moneta, py-moneyed, RubyMoney, and similar packages. Locale-specific currency names, symbols, plural forms, decimal/group separators, and standard/accounting patterns are intentionally not embedded: they vary by locale, are CLDR presentation data rather than ISO 4217 identity data, and are already supplied by .NET globalization. The high-level formatter planned here is an unambiguous invariant interchange formatter using the alphabetic code.

“Complete” means complete for every record present in the pinned authoritative inputs, including currencies, funds, precious-metal and special-purpose codes, territories with multiple currencies, territories with no universal currency, and historical rows with incomplete metadata. Missing facts will remain explicitly unknown; they will not be guessed from a reused code or from a present-day currency.

## Governing implementation constraints

- `syntax.md` and `AGENTS.md` are controlling. Existing code is not evidence that a construct is permitted.
- Production code under `src/` uses only built-in .NET APIs. Appropriate examples include `System`, `System.Collections.Generic`, `System.Collections.ObjectModel`, `System.Globalization`, and span-based text inspection. No NuGet dependency is allowed under `src/`.
- All public types live under `src/Api` in the required category directory. Public data is field-based because properties are forbidden.
- Public operations are instance methods declared by interfaces. No public static method will be introduced.
- Method names are globally unique across the interfaces implemented by the public implementation class; there is no method overloading.
- Nullable value types are forbidden. Optional numeric/date facts use an explicit `Has...` boolean or `PartialDate` with a precision/known flag. Nullable reference fields are permitted where useful.
- The generated runtime tables are internal, created once, never mutated after initialization, and never exposed. Because public data classes necessarily contain mutable public fields under the syntax policy, every API result is a detached deep snapshot; callers cannot mutate the catalog by changing a returned object or array.
- Generated tables use canonical ordinal ordering and simple indexed/binary-search or dictionary-free lookup code as appropriate. No LINQ is used in `src/`.
- Span-based ASCII checks are preferred for hot text validation and normalization paths when they keep the implementation simpler; public span overloads are not planned because overloading is forbidden and string-based APIs are the portable baseline.
- Tests may use external packages, but no syntax-policy/source-checking tests and no benchmark/performance tests will be added.

## Research findings and adopted lessons

The following projects were reviewed for feature discovery. Their full money arithmetic or extension systems are not copied; only features relevant to this library’s stated boundary are adopted.

- [SIX ISO 4217 Maintenance Agency](https://www.six-group.com/en/products-services/financial-information/market-reference-data/data-standards.html): authoritative current List One, current funds List Two, historical List Three, publication dates, minor units, entities, withdrawal dates, and amendments. This is the primary source for ISO identity data.
- [ISO 4217 overview](https://www.iso.org/iso-4217-currency-codes.html): establishes three-letter and three-digit representations, the relationship to ISO 3166, minor units, and current/fund/historical lists.
- [ISO 3166 overview](https://www.iso.org/iso-3166-country-codes.html): establishes alpha-2, alpha-3, and numeric-3 country identifiers and the distinction among current, formerly used, reserved, and user-assigned elements.
- [currency-codes](https://www.npmjs.com/package/currency-codes): motivates lookup by alphabetic code, numeric code, and country; enumeration of codes/numbers/countries; country arrays; and an exposed data publication date.
- [iso4217](https://pypi.org/project/iso4217/): motivates strongly canonical code objects, code/name/exponent access, case normalization, and versions tied to source publication dates.
- [iso-4217](https://pypi.org/project/iso-4217/): motivates retaining active and historical designations together, entity sets, withdrawal history, and safe major/minor-unit relationships.
- [py-moneyed](https://py-moneyed.readthedocs.io/en/latest/): motivates complete enumeration, ISO 3166 alpha-2 country lookup, multiple currencies per country, exact decimal use, subunit conversion, and explicit separation of locale formatting through CLDR/Babel.
- [JavaMoney/Moneta](https://github.com/JavaMoney/jsr354-ri): motivates interface-first access, lookup by code and territory/locale, catalog enumeration, explicit currency-aware rounding, and keeping providers/amount arithmetic separate from core currency identity.
- [RubyMoney/money ISO data](https://github.com/RubyMoney/money): motivates numeric codes as three-character text, subunit factor, smallest cash denomination, symbol ambiguity awareness, and cash rounding. This plan uses authoritative ISO/CLDR facts rather than RubyMoney’s locale-neutral symbol and separator defaults.
- [Unicode UTS #35 Part 3: Numbers](https://unicode.org/reports/tr35/tr35-numbers.html): defines standard fraction digits/rounding, cash fraction digits/rounding, territory usage intervals, and tender flags. This is the primary source for non-ISO amount-format and cash metadata.

Cross-library conclusions:

- Numeric codes must be preserved as canonical three-digit strings (`008`, not only integer `8`). An integer view may also be exposed when present.
- Alphabetic and numeric code lookup cannot both assume global uniqueness across all history. Historical numeric codes are reused, so numeric and name queries return zero-to-many results.
- Country mapping is many-to-many and time-dependent. “The currency of a country” must not be a scalar assumption.
- Official ISO minor units and CLDR standard/cash formatting rules are distinct. Both are exposed with their provenance; neither silently overwrites the other.
- Currency symbols are not universal identifiers. Ambiguous symbols such as `$`, `£`, `¥`, and `kr` are not accepted as currency-code input.
- Validation needs both strict membership checks and exception-less result APIs because invalid user input is frequent and expected.
- A data snapshot needs observable source versions and deterministic regeneration, not a package version masquerading as a data version.

## Data sources, precedence, and local specification snapshot

### Source precedence

1. SIX List One is authoritative for currently assigned alphabetic/numeric codes, official English names, entities, and ISO minor-unit digits.
2. SIX List Two identifies registered funds and supports kind classification; it does not replace List One identity fields.
3. SIX List Three is authoritative for historical entity/designation rows and withdrawal dates. A missing historical minor unit remains unknown.
4. SIX amendments explain changes after/between snapshots and are update-review inputs. The generated snapshot is never patched from prose without a corresponding effective source state.
5. CLDR 48.2 supplemental currency data supplies standard formatting digits/increments, cash digits/increments, territory currency intervals, and tender flags.
6. CLDR territory code mappings supply alpha-2, alpha-3, and numeric territory identifiers used to reconcile SIX entity names. Explicit reviewed overrides cover supranational bodies, special entities, renamed territories, and unavoidable source-name mismatches.
7. The UN M49 overview is an independent descriptive cross-check for country/area names and numeric identifiers; it does not silently override the pinned CLDR mapping.
8. On conflict, both source-qualified facts are retained where they describe different concepts. A true identity conflict fails generation and requires a reviewed override.

### Date semantics

- Full dates use proleptic Gregorian `DateOnly` values at query boundaries.
- Source dates with only a year or year-month are stored as `PartialDate` with `Year`, `Month`, `Day`, and `PartialDatePrecision`; absent components are zero.
- Usage interval starts are inclusive. Ends follow the source semantics and are normalized by the generator to an explicitly documented inclusive last date in generated rows.
- If only a withdrawal month is known, date-aware queries treat the designation as valid through the last day of that month. If only a year is known, they treat it as valid through the last day of that year. The original precision remains exposed.
- Unknown introduction/withdrawal dates remain unknown and do not become arbitrary minimum/maximum dates.

### Downloaded resources under `specs/`

- `specs/iso-4217-list-one.xml` — SIX current currencies and funds, published 2026-01-01.
- `specs/iso-4217-list-two.doc` — SIX registered fund codes, last saved 2025-05-08.
- `specs/iso-4217-list-three.xml` — SIX historical currencies and funds, published 2026-01-01.
- `specs/iso-4217-amendment-180.pdf` — amendment effective 2026-01-01 (Bulgaria EUR/BGN transition).
- `specs/iso-4217-amendments-overview.xlsx` — SIX past-amendments index.
- `specs/six-iso-4217-maintenance.html` — maintenance-agency description and download index captured 2026-08-27.
- `specs/cldr-48.2-supplementalData.xml` — pinned CLDR currency fractions, region histories, and territory code mappings.
- `specs/cldr-48.2-supplementalMetadata.xml` — pinned aliases and replacement metadata used during territory reconciliation.
- `specs/cldr-48.2-numberingSystems.xml` — pinned number-system definitions relevant to interpreting UTS #35 data.
- `specs/cldr-48.2-license.txt` — Unicode data license.
- `specs/unicode-tr35.html` and `specs/unicode-tr35-numbers.html` — LDML and number/currency metadata specification descriptions captured 2026-08-27.
- `specs/rfc5646.txt` — BCP 47 language-tag specification, retained to define the boundary between invariant library behavior and locale-specific display behavior.
- `specs/un-m49-overview.html` — UN country/area and numeric-region-code description captured 2026-08-27 for territory reconciliation review.

The complete copyrighted ISO 4217 standard is not redistributed. ISO permits use of the currency codes, and SIX publishes the recognized code lists free of charge; those maintenance lists are the generator inputs.

## Complete feature set

### 1. Currency identity and classification

- Every assigned current alphabetic code and canonical three-digit numeric code from List One.
- Official English currency/fund designation name exactly as published, plus a normalized name key used only for search.
- ISO minor-unit digit count with an explicit presence flag; `N.A.` and absent values remain unknown rather than becoming zero.
- Active and historical status, with queries scoped to active, historical, or all records.
- Historical withdrawal date and source precision where supplied; introduction dates where CLDR supplies them.
- Currency kind: ordinary monetary currency, fund/accounting unit, precious metal, supranational/special unit, testing code, no-currency code, or unknown.
- Separate source/provenance flags for ISO identity, ISO historic status, ISO fund classification, and CLDR enrichment.
- Correct handling of codes without numeric assignments and entities explicitly having no universal currency.
- Stable canonical ordering by alphabetic code, then status/date/name for deterministic enumeration.

### 2. Territory and currency-usage data

- Territory/entity records with canonical English name, alpha-2, alpha-3, and three-digit numeric ISO 3166-compatible codes where available.
- Territory classification for country, dependent territory, supranational organization, monetary institution, and other special entity.
- Current territory-to-currency mappings, including zero, one, or multiple currencies.
- Reverse currency-to-territory mappings.
- Historical usage intervals with start/end dates and original date precision where data permits.
- Legal-tender versus non-tender/fund usage from CLDR.
- Date-aware “currencies used on date” queries and complete usage-history queries.
- Explicit representation of territories with no universal currency rather than manufacturing a currency assignment.
- Reviewed reconciliation of renamed territories and SIX entities that have no ISO 3166 alpha-2 code.

### 3. Enumeration and lookup

- Enumerate all currencies by active/historical/all scope.
- Enumerate all territories/entities and all usage rows as detached snapshots.
- Exact lookup by normalized alphabetic code.
- Zero-to-many lookup by canonical numeric code, preserving leading zeroes and historical reuse.
- Exact or ordinal-ignore-case lookup by official name; ambiguous names return every match.
- Exact lookup of territory by alpha-2, alpha-3, or numeric code.
- Territory name search with explicit exact/ordinal-ignore-case mode.
- Get current currencies for a territory, currencies for a territory on a date, and complete territory usage history.
- Get current/historical territories for a currency and complete reverse usage history.
- Required alphabetic-code lookup that throws a dedicated exception, alongside exception-less lookup results.
- Lookup results distinguish malformed input, well-formed but unassigned input, not-active-in-requested-scope, and ambiguous input.

### 4. Normalization

- Alphabetic currency normalization: trim permitted surrounding Unicode whitespace, require exactly three ASCII letters after trimming, and uppercase invariantly.
- Numeric currency normalization: trim, require one to three ASCII decimal digits for lenient normalization, reject signs/separators/non-ASCII digits, and left-pad to exactly three digits.
- Strict numeric validation separately requires exactly three digits; normalization never implies assignment.
- Territory alpha-code normalization: trim ASCII letters and uppercase invariantly with alpha-2/alpha-3 length validation.
- Official-name search normalization limited to trimming and explicit ordinal case mode; no accent stripping, punctuation folding, transliteration, or culture-sensitive comparison.
- Normalization returns structured failure details without throwing for routine bad input.
- Symbols, localized names, and arbitrary aliases are never normalized into ISO codes.

### 5. Validation

- Syntax-only validation for alphabetic and numeric currency representations.
- Assignment validation against active, historical, or all scopes.
- Strict distinction among current, withdrawn, reserved/special, testing, and no-currency codes.
- Date-aware code validity using known usage/withdrawal intervals, with an explicit indeterminate outcome when the sources lack enough dates.
- Territory-code membership validation and territory/currency association validation.
- Amount-scale validation against ISO minor-unit digits.
- Standard-format amount validation against CLDR digits and rounding increment.
- Cash amount validation against CLDR cash digits and cash rounding increment.
- Explicit unknown-metadata result instead of assuming two digits.
- Structured validation results containing error category, normalized input, matched currency snapshot when available, and human-readable invariant reason.
- Dedicated exception types for required lookup, invalid currency input, and invalid amount/rule operations.

### 6. Amount-format and representation metadata

- ISO minor-unit digits and exact power-of-ten minor-unit factor when known.
- CLDR standard fraction digits and integer rounding increment at that scale.
- CLDR cash fraction digits and integer cash rounding increment, including inherited CLDR defaults.
- Explicit standard-versus-cash usage selection.
- Exact check whether a `decimal` amount is representable at the selected scale/increment.
- Explicit rounding using a caller-selected built-in `MidpointRounding`; no silent rounding during validation or conversion.
- Major-to-minor conversion with scale/increment validation and overflow reporting.
- Minor-to-major conversion with exact decimal construction.
- Invariant formatting as `CCC 1234.50`, using ASCII ISO code, ASCII digits, `-`, and `.`, no grouping, and the selected metadata scale.
- Invariant parsing of that same unambiguous format with assignment, scale, increment, and decimal-overflow validation.
- Negative and zero amount handling; preservation of the selected required scale during text formatting.
- No floating-point amount API. `decimal` and signed `long` minor-unit values are the supported public representations.

### 7. Generated, isolated, and versioned data

- Deterministic generator output from pinned inputs and small reviewed reconciliation overlays.
- Internal generated row tables for currencies, territories, usages, format rules, and lookup indexes.
- No XML, DOC, XLSX, JSON, network, reflection, or external package use at runtime.
- Runtime catalog initialization is read-only and independent of current culture/time zone.
- No generated internal array/list/table reference crosses the API boundary; returned arrays and nested data objects are deep copies.
- Data-version API and scalar static fields expose ISO publication date, CLDR version, generator schema version, and source checksums.
- Update workflow downloads into a temporary location, verifies type/version/checksum, validates invariants, produces a semantic diff, and only then replaces pinned inputs/generated output.
- Generation fails on duplicate active alphabetic codes with conflicting identity, invalid code syntax, invalid minor-unit values, impossible intervals, unmapped required territories, inconsistent reverse mappings, or unreviewed cross-source conflicts.
- Semantic update diff reports added/removed/changed codes, numeric-code changes, name/minor-unit changes, status transitions, mapping/interval changes, and formatting/cash-rule changes.

### 8. High-level usability and failure behavior

- One parameterless `CurrencyCodeService` implementation supplies catalog, normalization, validation, and amount-rule interfaces.
- Exception-less APIs are the default for user input; required/conversion APIs use dedicated exceptions when failure is exceptional.
- Every result has invariant behavior regardless of `CurrentCulture`, Turkish casing, native digit settings, or local calendar.
- Public arrays/results can be retained and modified by callers without affecting later calls.
- Empty arrays, not `null`, represent successful zero-match collection queries.
- Null reference input is handled as structured invalid input on exception-less APIs and as the documented dedicated exception on required APIs.

## Proposed public API (shape only)

The names below define responsibilities and avoid overloaded instance member names. Exact field ordering and individual XML comments are implementation details.

### `src/Api/Enums`

- `CurrencyStatus` — `Active`, `Historical`.
- `CurrencyKind` — `MonetaryCurrency`, `Fund`, `PreciousMetal`, `SpecialUnit`, `Testing`, `NoCurrency`, `Unknown`.
- `TerritoryKind` — `Country`, `DependentTerritory`, `Supranational`, `Institution`, `Other`.
- `CurrencySet` — `ActiveOnly`, `HistoricalOnly`, `All`.
- `PartialDatePrecision` — `Unknown`, `Year`, `Month`, `Day`.
- `NameMatchMode` — `ExactOrdinal`, `ExactOrdinalIgnoreCase`.
- `AmountUsage` — `IsoMinorUnit`, `StandardFormat`, `Cash`.
- `LookupError` — success/malformed/unassigned/out-of-scope/ambiguous categories.
- `NormalizationError` — success/null/empty/length/character/range categories.
- `CurrencyValidationError` — success/malformed/unassigned/status/date/territory/indeterminate categories.
- `AmountValidationError` — success/currency/metadata/scale/increment/range/format categories.

### `src/Api/Data`

- `PartialDate` — known flag, year/month/day, and precision.
- `CurrencyInfo` — alphabetic code, numeric text/integer presence and value, official name, ISO minor-unit presence/value, status, kind, source flags, introduction/withdrawal dates.
- `TerritoryInfo` — canonical name, alpha-2/alpha-3/numeric presence and values, and territory kind.
- `CurrencyUsage` — territory key, currency code, inclusive interval, tender flag, current flag, and source flags.
- `CurrencyFormatMetadata` — ISO, standard, and cash presence/digits/increments/factors plus source version.
- `CurrencyLookupResult` — found flag, error, normalized query, primary match, and all matches.
- `TerritoryLookupResult` — found flag, error, normalized query, primary match, and all matches.
- `NormalizationResult` — success flag, error, original text, and normalized text.
- `CurrencyValidationResult` — validity/indeterminate flags, error, normalized text, reason, and matched currency.
- `AmountValidationResult` — validity flag, error, reason, currency, amount, required digits/increment, and rounded candidate.
- `MinorUnitConversionResult` — success flag, error, reason, currency, major amount, and signed minor-unit value.
- `AmountTextResult` — success flag, error, reason, currency, parsed/formatted amount, and invariant text.
- `DataVersionInfo` — ISO publication date, CLDR version, generator schema version, generated snapshot identifier, and checksums.

Every public data class has at least one public non-static field and is used by an interface. It has no properties, does not implement an interface, and contains no public method.

### `src/Api/Contracts`

- `ICurrencyCatalog`
  - `GetCurrencyDataVersion()`
  - `GetCurrencyCatalog(CurrencySet)`
  - `GetTerritoryCatalog()`
  - `GetCurrencyUsageCatalog()`
  - `FindCurrencyByAlphabeticCode(string, CurrencySet)`
  - `FindCurrenciesByNumericCode(string, CurrencySet)`
  - `FindCurrenciesByOfficialName(string, NameMatchMode, CurrencySet)`
  - `GetRequiredCurrencyByAlphabeticCode(string, CurrencySet)`
  - `FindTerritoryByAlpha2Code(string)`
  - `FindTerritoryByAlpha3Code(string)`
  - `FindTerritoriesByNumericCode(string)`
  - `FindTerritoriesByOfficialName(string, NameMatchMode)`
  - `GetCurrentCurrenciesForTerritory(string, bool)`
  - `GetCurrenciesForTerritoryOnDate(string, DateOnly, bool)`
  - `GetCurrencyUsageHistoryForTerritory(string)`
  - `GetCurrentTerritoriesForCurrency(string, bool)`
  - `GetCurrencyUsageHistoryForCurrency(string)`
  - `GetCurrencyFormatMetadata(string)`
- `ICurrencyNormalizer`
  - `NormalizeCurrencyAlphabeticCode(string?)`
  - `NormalizeCurrencyNumericCode(string?)`
  - `NormalizeTerritoryAlpha2Code(string?)`
  - `NormalizeTerritoryAlpha3Code(string?)`
- `ICurrencyValidator`
  - `ValidateCurrencyAlphabeticSyntax(string?)`
  - `ValidateCurrencyNumericSyntax(string?)`
  - `ValidateCurrencyAssignment(string?, CurrencySet)`
  - `ValidateCurrencyOnDate(string?, DateOnly)`
  - `ValidateTerritoryAssignment(string?)`
  - `ValidateTerritoryCurrencyAssociation(string?, string?, DateOnly, bool)`
  - `ValidateCurrencyAmount(decimal, string?, AmountUsage)`
- `ICurrencyAmountRules`
  - `RoundCurrencyAmount(decimal, string, AmountUsage, MidpointRounding)`
  - `ConvertCurrencyMajorToMinor(decimal, string, AmountUsage)`
  - `ConvertCurrencyMinorToMajor(long, string, AmountUsage)`
  - `FormatInvariantCurrencyAmount(decimal, string, AmountUsage, MidpointRounding)`
  - `ParseInvariantCurrencyAmount(string?, AmountUsage)`

The boolean on territory/current reverse queries means “legal tender only”; it is explicit because default parameter values and named arguments are forbidden.

### `src/Api/Static`

- `CurrencyDataVersions` — public constant/static-readonly scalar fields for the pinned ISO date, CLDR version, generator schema version, and snapshot identifier. It contains no methods and exposes no array or collection.
- `CurrencyCodeConstants` — public scalar constants for alphabetic/numeric/territory code lengths and the maximum supported fraction digits. It contains no methods.

### `src/Api/Exceptions`

- `CurrencyNotFoundException` — required code/territory lookup failed.
- `InvalidCurrencyCodeException` — a required currency code is malformed, unassigned, or outside the requested scope.
- `InvalidCurrencyAmountException` — amount metadata is unavailable, scale/increment is invalid, invariant text is invalid, or conversion overflows.

Each exception derives directly from `System.Exception`, has at most one constructor, and uses only the exception base-constructor form permitted by the syntax policy.

### `src/Api/Classes`

- `CurrencyCodeService` — one public parameterless constructor; implements `ICurrencyCatalog`, `ICurrencyNormalizer`, `ICurrencyValidator`, and `ICurrencyAmountRules`; exposes only the methods declared by those interfaces.

## Planned source and support files

### Public production files

```text
src/Api/Enums/CurrencyStatus.cs
src/Api/Enums/CurrencyKind.cs
src/Api/Enums/TerritoryKind.cs
src/Api/Enums/CurrencySet.cs
src/Api/Enums/PartialDatePrecision.cs
src/Api/Enums/NameMatchMode.cs
src/Api/Enums/AmountUsage.cs
src/Api/Enums/LookupError.cs
src/Api/Enums/NormalizationError.cs
src/Api/Enums/CurrencyValidationError.cs
src/Api/Enums/AmountValidationError.cs
src/Api/Data/PartialDate.cs
src/Api/Data/CurrencyInfo.cs
src/Api/Data/TerritoryInfo.cs
src/Api/Data/CurrencyUsage.cs
src/Api/Data/CurrencyFormatMetadata.cs
src/Api/Data/CurrencyLookupResult.cs
src/Api/Data/TerritoryLookupResult.cs
src/Api/Data/NormalizationResult.cs
src/Api/Data/CurrencyValidationResult.cs
src/Api/Data/AmountValidationResult.cs
src/Api/Data/MinorUnitConversionResult.cs
src/Api/Data/AmountTextResult.cs
src/Api/Data/DataVersionInfo.cs
src/Api/Contracts/ICurrencyCatalog.cs
src/Api/Contracts/ICurrencyNormalizer.cs
src/Api/Contracts/ICurrencyValidator.cs
src/Api/Contracts/ICurrencyAmountRules.cs
src/Api/Static/CurrencyDataVersions.cs
src/Api/Static/CurrencyCodeConstants.cs
src/Api/Exceptions/CurrencyNotFoundException.cs
src/Api/Exceptions/InvalidCurrencyCodeException.cs
src/Api/Exceptions/InvalidCurrencyAmountException.cs
src/Api/Classes/CurrencyCodeService.cs
```

### Internal production files

```text
src/Internal/Data/CurrencyRow.cs
src/Internal/Data/TerritoryRow.cs
src/Internal/Data/CurrencyUsageRow.cs
src/Internal/Data/CurrencyFormatRow.cs
src/Internal/Data/GeneratedCurrencyRows.g.cs
src/Internal/Data/GeneratedTerritoryRows.g.cs
src/Internal/Data/GeneratedCurrencyUsageRows.g.cs
src/Internal/Data/GeneratedCurrencyFormatRows.g.cs
src/Internal/Data/GeneratedLookupIndexes.g.cs
src/Internal/Data/GeneratedDataVersion.g.cs
src/Internal/CurrencyDataStore.cs
src/Internal/CurrencySnapshotFactory.cs
src/Internal/AsciiCodeText.cs
src/Internal/PartialDateRules.cs
src/Internal/CurrencyAmountRules.cs
src/Internal/InvariantCurrencyAmountText.cs
```

Internal types remain outside `src/Api`, are non-public, and use only the restricted source syntax.

### Generator and reviewed input files

```text
tools/CurrencyCodes.DataGenerator/CurrencyCodes.DataGenerator.csproj
tools/CurrencyCodes.DataGenerator/Program.cs
tools/CurrencyCodes.DataGenerator/IsoListOneReader.cs
tools/CurrencyCodes.DataGenerator/IsoListThreeReader.cs
tools/CurrencyCodes.DataGenerator/CldrSupplementalReader.cs
tools/CurrencyCodes.DataGenerator/TerritoryReconciler.cs
tools/CurrencyCodes.DataGenerator/DataInvariantValidator.cs
tools/CurrencyCodes.DataGenerator/SemanticDiffWriter.cs
tools/CurrencyCodes.DataGenerator/CSharpTableWriter.cs
tools/update-currency-data.sh
data/overrides/fund-codes.csv
data/overrides/currency-kinds.csv
data/overrides/territory-name-map.csv
data/overrides/source-conflicts.csv
data/source-checksums.txt
```

The generator/tooling is not referenced by `src/currencycodes.csproj`. It uses built-in .NET XML, text, cryptography, and file APIs and writes canonical generated C#.

### Planned test files

```text
tests/DataVersionTests.cs
tests/CurrencyCatalogEnumerationTests.cs
tests/CurrencyLookupTests.cs
tests/TerritoryLookupTests.cs
tests/CurrencyUsageTests.cs
tests/CurrencyNormalizationTests.cs
tests/CurrencyValidationTests.cs
tests/CurrencyAmountValidationTests.cs
tests/CurrencyAmountRoundingTests.cs
tests/MinorUnitConversionTests.cs
tests/InvariantCurrencyAmountTextTests.cs
tests/DefensiveSnapshotTests.cs
tests/ExceptionBehaviorTests.cs
tests/GeneratedDataCompletenessTests.cs
tests/GeneratedDataInvariantTests.cs
tests/CurrentCultureIndependenceTests.cs
tests/Fixtures/ExpectedIso4217Snapshot.cs
tests/Fixtures/ExpectedCldrCurrencyRules.cs
tests/Fixtures/InvalidInputCases.cs
```

## Implementation tasks (low level to high level)

- [DONE] T001: Add the pinned source-checksum manifest for every generator input under `specs/` and record the ISO publication date, CLDR release, retrieval date, byte length, and SHA-256 digest as machine-readable data.
- [DONE] T002: Create the standalone data-generator project using only built-in .NET APIs and ensure it is not referenced by the runtime library project.
- [DONE] T003: Implement strict List One XML ingestion that preserves publication date, entity row, alphabetic code, three-digit numeric text, official name, and minor-unit presence/value without applying defaults.
- [DONE] T004: Implement strict List Three XML ingestion that preserves every historical entity/designation row, numeric text where present, withdrawal text, and year/month/day precision without inferring absent minor units.
- [DONE] T005: Transcribe and independently review List Two’s registered fund classifications into `data/overrides/fund-codes.csv`, keyed by canonical alphabetic code and source revision.
- [DONE] T006: Create the reviewed currency-kind overlay for funds, precious metals, special units, testing, no-currency, and uncertain codes without deriving kind solely from the first character.
- [DONE] T007: Implement CLDR fraction-rule ingestion, including `DEFAULT` inheritance, standard digits/rounding, cash digits/rounding, and explicit-versus-inherited provenance.
- [DONE] T008: Implement CLDR territory currency-history ingestion with inclusive normalized intervals, tender flags, and preserved source date precision.
- [DONE] T009: Implement CLDR territory-code ingestion for alpha-2, alpha-3, numeric code text/value, aliases, and replacement metadata.
- [DONE] T010: Create the reviewed SIX-entity-to-territory reconciliation overlay for spelling differences, renamed territories, organizations, supranational entities, and entries with no ISO 3166 code, using UN M49 as an independent numeric/name cross-check.
- [DONE] T011: Implement territory reconciliation and make generation fail for every unmapped required SIX country/territory entity or stale unused override.
- [DONE] T012: Implement cross-source reconciliation that keeps ISO minor-unit digits separate from CLDR standard/cash digits and requires an explicit reviewed entry for true identity conflicts.
- [DONE] T013: Implement generator invariants for code grammar, canonical numeric width, duplicate identity conflicts, range bounds, date ordering, status consistency, source completeness, and forward/reverse mapping consistency.
- [DONE] T014: Implement deterministic aggregation of repeated List One entity rows into one active `CurrencyInfo` identity plus separate currency-usage rows.
- [DONE] T015: Implement deterministic aggregation of List Three rows without collapsing distinct historical designations or numeric-code reuse.
- [DONE] T016: Implement the semantic data diff that reports code additions/removals, status transitions, numeric/name/minor-unit changes, territory mapping changes, date changes, tender changes, and standard/cash-rule changes.
- [DONE] T017: Implement canonical C# table emission with ordinal sorting, stable escaping, fixed line endings, no timestamps in generated content, and byte-identical output for identical inputs.
- [DONE] T018: Implement generated source-checksum/version emission for ISO publication date, CLDR version, generator schema version, and a content-derived snapshot identifier.
- [DONE] T019: Implement the temporary-download data-update script with HTTPS failure handling, content-type/magic checks, source-version checks, checksum calculation, generator validation, semantic diff output, and atomic replacement only after success.
- [DONE] T020: Add a generator repeatability test that runs generation twice from the pinned snapshot and byte-compares every generated file.
- [DONE] T021: Add generator fixture tests for malformed XML, duplicate rows, absent optional facts, partial dates, CLDR default inheritance, stale overrides, and unreviewed conflicts.
- [DONE] T022: Implement all public enums under `src/Api/Enums` with stable explicit integer values and ensure each enum is consumed by a public data class or interface.
- [DONE] T023: Implement `PartialDate` with explicit known state and precision so no nullable value type or invented date sentinel is exposed.
- [DONE] T024: Implement `CurrencyInfo` with separate presence/value fields for numeric code and ISO minor units, status/kind/source fields, and partial introduction/withdrawal dates.
- [DONE] T025: Implement `TerritoryInfo` with presence/value fields for alpha-2, alpha-3, numeric code, canonical name, and territory kind.
- [DONE] T026: Implement `CurrencyUsage` with stable territory/currency keys, inclusive interval metadata, tender/current state, and source flags.
- [DONE] T027: Implement `CurrencyFormatMetadata` with separate ISO, standard-format, and cash digits/increments/factors and source version.
- [DONE] T028: Implement lookup, normalization, currency-validation, amount-validation, conversion, text, and data-version result classes as detached field-based data holders.
- [DONE] T029: Implement the two public scalar-only static data classes for code constants and generated data versions without exposing tables, arrays, collections, delegates, or methods.
- [DONE] T030: Implement the three direct `System.Exception` subclasses with one constructor each and invariant failure messages supplied by callers.
- [DONE] T031: Implement `ICurrencyCatalog` with the proposed uniquely named catalog, lookup, territory mapping, reverse mapping, and metadata operations.
- [DONE] T032: Implement `ICurrencyNormalizer` with uniquely named alphabetic, numeric, alpha-2 territory, and alpha-3 territory normalization operations.
- [DONE] T033: Implement `ICurrencyValidator` with syntax, assignment, date, territory association, and amount validation operations.
- [DONE] T034: Implement `ICurrencyAmountRules` with rounding, major/minor conversion, invariant formatting, and invariant parsing operations.
- [DONE] T035: Implement internal currency, territory, usage, and format row classes that contain only the fields needed by generated tables and runtime indexes.
- [DONE] T036: Generate the complete currency, territory, usage, format, lookup-index, and data-version source files from the pinned snapshot.
- [DONE] T037: Implement `AsciiCodeText` using simple ordinal/ASCII logic and spans where appropriate for whitespace trimming, letter/digit checks, uppercasing, and numeric left-padding without culture-sensitive behavior.
- [DONE] T038: Implement partial-date comparison and end-of-month/end-of-year normalization rules for date-aware lookup while preserving original source precision in snapshots.
- [DONE] T039: Implement `CurrencySnapshotFactory` to deep-copy internal rows, partial dates, nested matches, and arrays into public data holders.
- [DONE] T040: Implement `CurrencyDataStore` initialization over generated rows with deterministic indexes for alphabetic code, numeric code, official name, territory codes/names, and both directions of usage lookup.
- [DONE] T041: Ensure the internal store never returns a generated/internal reference and that all zero-result collection queries return new empty arrays rather than `null`.
- [DONE] T042: Implement active/historical/all catalog enumeration in canonical order.
- [DONE] T043: Implement exact alphabetic-code lookup with strict normalization, requested-scope filtering, structured errors, and required-lookup exception behavior.
- [DONE] T044: Implement zero-to-many numeric-code lookup with strict canonical validation, leading-zero preservation, scope filtering, and historical reuse handling.
- [DONE] T045: Implement exact and ordinal-ignore-case official-name lookup with deterministic ambiguity ordering and no culture-sensitive/fuzzy transformations.
- [DONE] T046: Implement territory lookup by alpha-2, alpha-3, numeric code, and official name with structured malformed/unassigned/ambiguous outcomes.
- [DONE] T047: Implement complete territory and currency-usage enumeration as detached canonical snapshots.
- [DONE] T048: Implement current territory-to-currency and reverse currency-to-territory lookup with explicit tender-only filtering and correct zero/multiple result handling.
- [DONE] T049: Implement date-aware territory-to-currency lookup and full usage-history lookup in both directions, including partial/unknown source date behavior.
- [DONE] T050: Implement currency-format metadata lookup with CLDR default inheritance already resolved in generated rows and explicit unknown flags for unavailable ISO facts.
- [DONE] T051: Implement exception-less alphabetic currency, numeric currency, alpha-2 territory, and alpha-3 territory normalization, including null/empty/length/character error distinctions.
- [DONE] T052: Implement currency alphabetic/numeric syntax validation independently of catalog assignment.
- [DONE] T053: Implement active/historical/all assignment validation with normalized input, matched detached snapshot, and distinct malformed/unassigned/out-of-scope outcomes.
- [DONE] T054: Implement date-aware currency validation with valid, invalid, and indeterminate results based only on known source intervals.
- [DONE] T055: Implement territory membership and territory/currency/date association validation with explicit tender-only selection.
- [DONE] T056: Implement decimal scale and increment inspection without string/culture dependence and without silently rounding the input.
- [DONE] T057: Implement ISO-minor-unit, CLDR-standard, and CLDR-cash amount validation, including unavailable metadata, excessive scale, invalid increment, negative values, zero, and decimal boundary cases.
- [DONE] T058: Implement explicit currency-aware rounding for all supported `MidpointRounding` values using selected digits/increment and checked decimal arithmetic.
- [DONE] T059: Implement exact major-to-minor conversion to signed `long`, reporting scale/increment violations and overflow without floating-point intermediates.
- [DONE] T060: Implement exact signed-minor-to-major decimal conversion using the selected scale and rejecting metadata configurations that cannot be represented.
- [DONE] T061: Implement invariant amount formatting with canonical alphabetic code, ASCII sign/digits/decimal point, no grouping, selected fixed scale, and explicit caller-selected rounding.
- [DONE] T062: Implement invariant amount parsing for the exact `CCC amount` grammar with ASCII-only tokens, decimal overflow checks, currency assignment checks, and selected scale/increment validation.
- [DONE] T063: Implement the public parameterless `CurrencyCodeService` and wire every interface operation to the internal store/rules without adding any public member not declared on its interfaces.
- [DONE] T064: Add data-version tests covering every public static version field, `DataVersionInfo` field, source checksum, ISO date, CLDR version, generator schema version, and snapshot identifier.
- [DONE] T065: Add complete catalog-enumeration tests for active/historical/all counts, canonical order, uniqueness rules, special/fund/metal/testing/no-currency kinds, and every field populated from the pinned source.
- [DONE] T066: Add complete alphabetic, numeric, and official-name lookup tests covering every current and historical row, leading-zero numeric codes, numeric reuse, duplicate names, all scopes, null/bad input, and required-lookup exceptions.
- [DONE] T067: Add complete territory lookup tests for every known alpha-2/alpha-3/numeric mapping, named special entity, alias reconciliation result, malformed code, unassigned code, and ambiguous name.
- [DONE] T068: Add complete territory-usage tests by comparing every generated forward and reverse mapping to pinned fixtures, including multi-currency territories, no-universal-currency territories, supranational codes, tender filtering, transitions, and interval boundaries.
- [DONE] T069: Add normalization test matrices for null, empty, whitespace, every ASCII letter case, one-to-three numeric padding, leading zeroes, Unicode digits, symbols, punctuation, embedded whitespace, overlength input, and Turkish-current-culture behavior.
- [DONE] T070: Add syntax and assignment validation tests for every error/result category, active/historical/all scopes, special codes, testing/no-currency codes, and detached matched results.
- [DONE] T071: Add date-validity tests for full, month, year, and unknown precision; inclusive starts/ends; leap-year month ends; status transitions; and indeterminate outcomes.
- [DONE] T072: Add amount-metadata tests that compare every currency’s ISO facts and all explicit/inherited CLDR standard/cash rules to independently built expected fixtures, including known ISO-versus-CLDR differences.
- [DONE] T073: Add amount-validation tests covering every supported digit/increment combination, ISO/standard/cash usage, exact and inexact positive/negative amounts, zero, trailing decimal scale, unavailable metadata, unknown currency, and decimal extremes.
- [DONE] T074: Add rounding tests for every supported `MidpointRounding` value, positive/negative midpoint and non-midpoint cases, zero, carry across the integer boundary, cash increments, three/four-digit currencies, and overflow behavior.
- [DONE] T075: Add major/minor conversion tests for every scale, signed boundaries, exact round trips, scale/increment rejection, unavailable metadata, unknown currency, and `long`/`decimal` overflow paths.
- [DONE] T076: Add invariant formatter/parser tests for every currency scale, positive/negative/zero values, fixed trailing zeroes, ASCII grammar rejection, whitespace rules, round trips, explicit rounding, malformed/unknown/historical codes, and overflow.
- [DONE] T077: Add defensive-snapshot tests that mutate every public field, nested `PartialDate`, match array, and returned catalog/usage array, then prove later calls and other service instances retain the generated data unchanged.
- [DONE] T078: Add exception-behavior tests for all three public exception types and every throwing API path, while proving exception-less APIs return their documented structured errors for the same routine failures.
- [DONE] T079: Add current-culture independence tests across representative Latin, Turkish, Arabic/native-digit, comma-decimal, and non-Gregorian cultures for every normalization, lookup, validation, conversion, formatting, and parsing operation.
- [DONE] T080: Configure line and branch coverage collection for `src/Api` behavior and close every uncovered public-method branch with focused functional tests, excluding generated field-only data and compiler-generated code from misleading coverage accounting.
- [DONE] T081: Run the full generator and test suite from a clean checkout, verify byte-stable generated sources, verify no runtime project dependency beyond the target .NET framework, and resolve every data invariant or functional coverage failure.
