# Universal Language Codes Library Development Plan

## Goal and boundaries

Build a dependency-free C# library for authoritative ISO 639 language-code data, exact and normalized lookup, conversion, normalization, validation, historical-code handling, macrolanguage and language-group relationships, and a standards-bounded BCP 47 interoperability layer. Runtime code under `src/` will use only the .NET base class libraries. Test-only dependencies remain permitted.

The implementation must obey `syntax.md`. In particular, the public API will use field-based data classes, non-overloaded instance methods on interfaces, one public implementation class with one constructor, and no public static methods. Public result objects cannot be deeply immutable because the policy permits public data fields but not instance `readonly` fields or properties. Therefore immutable means that generated internal tables are never mutated or exposed: every public array and data object is a defensive snapshot, so caller mutation cannot corrupt the catalog.

The current ISO 639:2023 model has four identifier sets. Set 1 is alpha-2, Set 2 is alpha-3 for widely known languages and groups (including bibliographic and terminology alternatives), Set 3 is alpha-3 for comprehensive individual-language coverage, and Set 5 is alpha-3 for language families and groups. There is no Set 4 or Set 6 table to implement. Former Part 4 principles were consolidated into ISO 639:2023; the proposed API uses the current “Set” terminology while retaining familiar `Iso639Set` enum names.

Full CLDR locale intelligence is outside the ISO/IANA-backed core: translated display names beyond the official source languages, likely-subtag inference, language distance, population estimates, and locale negotiation based on territory/script statistics are excluded. Full BCP 47 syntax, registry validation, canonicalization, and RFC 4647 matching are included, but CLDR-specific tag minimization/maximization is not. Probabilistic language detection from text is also out of scope.

## Downloaded standards and data snapshots

Retrieved on 2026-08-27. Dynamic registries must be pinned by checksum and upstream date in the planned data manifest before generation.

- `specs/iso-639/access-to-databases.pdf` — ISO instructions for the Set 1, 2, 3, and 5 databases.
- `specs/iso-639/change-request-access.pdf` — ISO change-request entry point.
- `specs/iso-639/maintenance-flowchart.pdf` — ISO 639 Maintenance Agency change workflow.
- `specs/iso-639/maintenance-agency-terms-of-reference.pdf` — maintenance roles and responsibilities.
- `specs/iso-639-2/description.html` and `ISO-639-2_utf-8.txt` — official LoC Set 1/2 description and English/French code table, including bibliographic/terminology alternatives.
- `specs/iso-639-3/scope.html`, `types.html`, and `relationships.html` — official Set 3 scope, language-type, and cross-set descriptions.
- `specs/iso-639-3/download-tables.html`, `iso-639-3.tab`, `iso-639-3_Name_Index.tab`, `iso-639-3-macrolanguages.tab`, and `iso-639-3_Retirements.tab` — active codes, reference/print/inverted names, macrolanguage mappings, and retired codes.
- `specs/iso-639-5/description.html`, `iso639-5.tsv`, and `code-list-with-hierarchy.html` — official Set 5 names and family/group hierarchy. The TSV does not contain hierarchy, so hierarchy must be parsed from and reconciled against the LoC table.
- `specs/ietf/rfc5646.txt` — BCP 47 language-tag syntax, registry semantics, canonical form, and extension/private-use rules.
- `specs/ietf/rfc4647.txt` — basic filtering and lookup for language ranges.
- `specs/ietf/language-subtag-registry.txt` — current IANA registry, including descriptions, deprecations, preferred values, macrolanguages, extlangs, suppress-script data, grandfathered tags, variants, and other tag subtags.

## Existing-library research and adopted lessons

- [pycountry](https://github.com/pycountry/pycountry) demonstrates packaged immutable-ish ISO snapshots, iteration, field-specific retrieval, case-insensitive general lookup, localized source data, and explicit upstream-data versioning. Adopt the catalog and generic lookup ideas, but not mutable runtime registration.
- [python-iso639](https://github.com/jacksonllee/iso639) covers Set 1/2B/2T/3 mapping, deterministic “unknown value” matching, reference/print/inverted names, macrolanguage membership, retirement reason/remedy/date, and a data-version constant. Adopt all of those data dimensions and make ambiguity explicit instead of silently taking the first name match.
- [langcodes](https://pypi.org/project/langcodes/) contributes shortest-code canonicalization, bibliographic conversion, deprecated-tag replacement, macrolanguage-aware normalization, tag validity, display-name lookup, and clear handling of ambiguous names. Adopt standards-derived canonicalization and ambiguity results; omit CLDR likely subtags, demographic data, language-distance scoring, and translated display names until separately sourced.
- [i18n-iso-languages](https://github.com/michaelwittig/node-i18n-iso-languages) contributes Set 1/2B/2T conversion, validation, name-to-code lookup, and localized display-name catalogs. The core will expose every English/French name actually supplied by ISO sources and keep the model ready for additional name languages, but will not bundle community translations as authoritative ISO data.
- [language-tags](https://www.npmjs.com/package/language-tags) contributes case-insensitive IANA subtag/tag lookup, well-formedness and validity distinctions, descriptions, deprecation/preferred values, suppress-script metadata, search/filter operations, and registry-backed BCP 47 behavior. Adopt these at the high-level tag layer while keeping the ISO catalog usable independently.

## Complete feature inventory, low level to high level

### 1. Authoritative data model and coverage

- Represent all assigned identifiers in ISO 639 Sets 1, 2, 3, and 5 without dropping groups, macrolanguages, special identifiers, ancient/historical/extinct/constructed languages, or records lacking alpha-2 codes.
- Preserve Set 2 terminology and bibliographic alpha-3 codes as typed aliases of the same entity where applicable; do not present them as distinct languages.
- Preserve Set 3 scope (`individual`, `macrolanguage`, `special`) and language type (`living`, `extinct`, `ancient`, `historical`, `constructed`, `special`).
- Preserve Set membership independently because alpha-3 spelling alone does not say whether a code belongs to Set 2, Set 3, Set 5, or several sets.
- Preserve official reference names, Set 3 print names and inverted names, Set 2 English/French names, Set 5 English/French names, comments, and the source/kind of each name.
- Preserve code aliases separately from name aliases: alpha-2, Set 2 terminology, Set 2 bibliographic, active Set 3, retired Set 3, and IANA deprecated/preferred language subtags.
- Preserve every Set 3 retirement with reason (`change`, `duplicate`, `nonexistent`, `split`, `merge`), replacement when singular, free-text remedy, and effective date. Never invent one replacement for a split.
- Preserve current and historical macrolanguage-to-member relationships and their upstream statuses.
- Preserve Set 5 parent/child hierarchy and distinguish family, language group, and remainder group notes where the source provides them.
- Represent ISO special identifiers such as `mis`, `mul`, `und`, and `zxx`, and the `qaa`–`qtz` local/private-use range, without conflating “well-formed,” “reserved,” “assigned,” and “suitable for the requested use.”
- Preserve IANA language/extlang metadata needed to bridge ISO codes to BCP 47, including deprecation, preferred value, prefix, macrolanguage, suppress-script, scope, descriptions, and added/deprecated dates.

### 2. Generated immutable runtime tables

- Generate C# tables deterministically from pinned files under `specs/`; do not parse files or access the network at runtime.
- Store compact internal rows and sorted indexes in `static readonly` arrays/dictionaries that are initialized once and never exposed.
- Use ordinal ASCII comparison for codes and explicit Unicode normalization plus ordinal comparison for names; never depend on current culture.
- Use `ReadOnlySpan<char>` internally for code shape checks, ASCII case folding, and delimiter scanning where it avoids allocations; retain string-based primary public methods for broad usability and add distinctly named span validation methods where useful.
- Build exact indexes for every active/historical code form, normalized-name indexes that retain all collisions, relationship indexes in both directions, and tag/subtag indexes.
- Return defensive copies of entries, nested arrays, names, aliases, relationships, issues, and candidates so public mutable fields cannot mutate generated state.
- Publish data edition, source dates, retrieval date, generator schema version, and record counts as constants/readonly scalar fields only; expose no public raw table.

### 3. ISO catalog, lookup, and search

- Enumerate all entities and filter deterministic snapshots by Set, scope, type, and active/retired status.
- Look up case-insensitively by explicit code form or by any recognized code form, with optional trimming controlled by options rather than hidden behavior.
- Resolve alpha-2, terminology alpha-3, bibliographic alpha-3, active Set 3, Set 5, reserved local-use, and retired codes with the match kind reported.
- Look up exact names, normalized names, and aliases across source/name languages; normalize Unicode normalization form, case, whitespace, punctuation separators, and inverted-name ordering only according to documented deterministic rules.
- Return all candidates for ambiguous names, with match source and match quality, rather than silently selecting a language.
- Provide deterministic name search (exact, prefix, then contains) with stable ordering and caller-specified result limits; do not add opaque edit-distance guessing.
- Provide a generic code-or-name resolver with documented precedence: active exact code, code alias, retired code, exact name, normalized name. Report ambiguity and the matched field.
- Provide throwing “require” operations for callers that prefer exceptions and result-returning operations for frequent not-found/ambiguous outcomes.

### 4. Relationship and history queries

- Get the macrolanguage for a member and all members for a macrolanguage; do not automatically equate a macrolanguage with each member.
- Get direct and transitive Set 5 parents/children with cycle-safe traversal and deterministic ordering.
- Query retirement metadata directly and follow one-to-one replacement chains with loop detection.
- Return split replacement candidates parsed only when the authoritative data supplies machine-resolvable codes, while retaining the original remedy text.
- Allow callers to include or exclude retired identifiers and retired macrolanguage mappings explicitly.

### 5. ISO code normalization and conversion

- Normalize surrounding whitespace only when requested, validate ASCII letters and length, and canonicalize code casing to lowercase.
- Canonicalize terminology/bibliographic equivalents to a requested code policy: Set 1 shortest form, Set 3/terminology alpha-3, Set 2 bibliographic, or preserve recognized form.
- Convert between alpha-2, terminology alpha-3, bibliographic alpha-3, and Set 3 where a mapping exists; return an explicit unavailable-target result otherwise.
- Replace deprecated/retired codes only when an authoritative unambiguous preferred value exists; report split, no replacement, invalid replacement chain, and already-canonical outcomes distinctly.
- Make macrolanguage folding an explicit opt-in policy and return the relationship used; default normalization preserves the individual language.
- Make normalization idempotent and return original input, normalized output, source form, target form, applied steps, warnings, and resolved entry.
- Never silently treat an arbitrary language name as a code during normalization; generic resolution and code normalization remain separate operations.

### 6. ISO validation

- Distinguish null/empty input, bad length, non-ASCII characters, invalid characters, unknown/unassigned code, reserved local-use code, retired code, wrong Set, wrong entity scope/type, and valid active code.
- Validate against an explicit target Set/code form and optional allowed scopes/types, not merely “two or three letters.”
- Offer both detailed diagnostics and fast boolean checks, including a span-based lexical/assignment check with a distinct method name.
- Allow policy choices for accepting retired codes, special codes, language collections/families, macrolanguages, and private-use codes.
- Report canonical alternatives for valid aliases and one-to-one retired codes without making a noncanonical form invalid unless strict-canonical mode is requested.
- Ensure validation and normalization agree: any successful normalization output must pass the corresponding validation policy.

### 7. BCP 47 and IANA interoperability

- Parse the complete RFC 5646 grammar: language, extlang, script, region, variants, extensions, private use, grandfathered tags, and private-use-only tags.
- Distinguish syntactic well-formedness from registry validity, deprecation, canonicality, and private use.
- Validate subtag order, lengths, ASCII repertoire, duplicate variants/singletons, extlang prefixes, variant prefixes, and registry assignment where RFC 5646 requires it.
- Normalize separators and recommended casing; replace grandfathered tags and deprecated subtags with IANA preferred values; shorten overlong ISO language codes; apply extlang prefix/canonical rules; remove suppress-script only under an explicit canonical policy; preserve extensions and private-use payloads.
- Resolve a tag’s primary/extlang language to the ISO catalog and return a clear unresolved/private-use outcome.
- Expose IANA descriptions and metadata for language-related subtags without presenting region/script identifiers as ISO 639 entries.
- Implement RFC 4647 basic filtering and lookup over caller-provided tag collections, preserving caller order for equal matches and supporting the wildcard rules defined by the RFC.
- Do not implement CLDR likely-subtag expansion, locale-distance “best fit,” or translated composite display names in this phase.

### 8. Versioned data updates and integrity

- Maintain a machine-readable manifest with source URL, local path, retrieval timestamp, upstream version/date, SHA-256, parser schema version, encoding, and expected header.
- Fetch updates into a temporary staging area, verify HTTP success and file shape, then require an explicit acceptance step before replacing pinned inputs.
- Produce a semantic diff of added, changed, and retired codes; name changes; Set membership changes; alias changes; relationship changes; and IANA preferred-value changes.
- Reject duplicate primary identifiers, conflicting cross-set mappings, invalid enum markers, dangling replacement/macro/group references, group cycles, malformed dates, unexpected headers, and silent record-count collapses.
- Emit byte-for-byte deterministic generated C# and a scalar data-version record; a clean regeneration must not change tracked output.
- Retain old identifiers through retirement data instead of deleting history during an update.
- Keep fetching/generation tooling outside `src/`; generated runtime code must have no package dependency beyond .NET.

## Proposed public API surface

Names below define responsibilities rather than signatures in fine detail. Every public type receives the XML documentation required by `syntax.md`; all operations are instance methods and all method names are unique across interfaces implemented by `LanguageCodeService`.

### `src/Api/Data`

- `LanguageCodeEntry` — one reconciled ISO entity with code forms, Set flags, scope/type/status, reference name, names, aliases, and source metadata.
- `LanguageName` — text, language of the name, name kind, source Set, and reference-name flag.
- `LanguageAlias` — alias text/code, alias kind, preferred code, deprecation state, and source.
- `LanguageRelationship` — typed macrolanguage-member or Set 5 parent-child edge with source/status.
- `LanguageRetirement` — retired code, former name, reason, replacement candidates, remedy, and effective date.
- `LanguageMatch` — candidate entry, matched value/field, and deterministic match rank.
- `LanguageLookupOptions` — trimming, case/name normalization, retired-code inclusion, name-language filter, search mode, and result limit.
- `LanguageLookupResult` — lookup status, selected entry when unique, candidates, and normalized query.
- `LanguageNormalizationOptions` — target form, retirement behavior, macrolanguage policy, whitespace/casing policy, and tag canonicalization switches.
- `LanguageNormalizationResult` — status, output, resolved entry, applied changes, warnings, and replacement candidates.
- `LanguageValidationOptions` — required Set/form and acceptance policy for status, scope, type, special, group, and private-use values.
- `LanguageValidationIssue` — typed validation error/warning plus offending component and message.
- `LanguageValidationResult` — well-formed/assigned/valid/canonical flags, resolved entry, canonical value, and issues.
- `LanguageTag` — parsed BCP 47 fields for language, extlangs, script, region, variants, extensions, and private use.
- `LanguageSubtagMetadata` — IANA type/value, descriptions, prefixes, preferred value, macrolanguage, suppress-script, scope, and lifecycle dates for a language-related subtag.
- `LanguageTagMatchResult` — RFC 4647 outcome, matched tag/index, and normalized range.

### `src/Api/Enums`

- `Iso639Set`, `LanguageCodeForm`, `LanguageScope`, `LanguageType`, and `LanguageCodeStatus`.
- `LanguageNameKind`, `LanguageAliasKind`, `LanguageRelationshipKind`, and `LanguageRetirementReason`.
- `LanguageLookupStatus`, `LanguageMatchKind`, and `LanguageSearchMode`.
- `LanguageNormalizationStatus`, `LanguageNormalizationStep`, and `MacrolanguageNormalizationPolicy`.
- `LanguageValidationSeverity`, `LanguageValidationError`, `LanguageSubtagType`, and `LanguageTagMatchStatus`.

### `src/Api/Contracts`

- `ILanguageCatalog` — `GetAllEntries`, `GetEntriesBySet`, `GetEntriesByScope`, `GetEntriesByType`, `LookupCode`, `RequireCode`, `LookupName`, `SearchNames`, `ResolveValue`, `RequireValue`, `GetMacrolanguageForMember`, `GetMacrolanguageMembers`, `GetGroupParents`, `GetGroupChildren`, and `GetRetirement`.
- `ILanguageNormalizer` — `NormalizeCode`, `ConvertCode`, and `NormalizeTag`.
- `ILanguageValidator` — `ValidateCode`, `ValidateCodeSpan`, `IsAssignedCode`, and `ValidateTag`.
- `ILanguageTagService` — `ParseTag`, `ResolveTagLanguage`, `GetLanguageSubtagMetadata`, `FilterTags`, and `LookupBestTag`.

### `src/Api/Static`

- `LanguageDataInfo` — scalar fields for ISO edition, data version, individual upstream dates, retrieval date, generator schema version, and record counts. It has no methods or publicly exposed collections.

### `src/Api/Exceptions`

- `LanguageCodeNotFoundException`, `AmbiguousLanguageNameException`, `InvalidLanguageCodeException`, and `InvalidLanguageTagException`, each deriving directly from `System.Exception` and having at most one constructor.

### `src/Api/Classes`

- `LanguageCodeService` — the single public implementation of `ILanguageCatalog`, `ILanguageNormalizer`, `ILanguageValidator`, and `ILanguageTagService`, with one parameterless constructor and only interface-declared public instance methods.

## Planned non-public and generated source files

- `src/Internal/Data/GeneratedLanguageEntries.cs`, `GeneratedLanguageNames.cs`, `GeneratedAliases.cs`, `GeneratedRetirements.cs`, `GeneratedRelationships.cs`, `GeneratedIanaRegistry.cs`, and `GeneratedDataMetadata.cs` — deterministic generated tables.
- `src/Internal/Data/LanguageRow.cs`, `NameRow.cs`, `AliasRow.cs`, `RetirementRow.cs`, `RelationshipRow.cs`, and `IanaSubtagRow.cs` — compact internal row classes.
- `src/Internal/Index/CodeIndex.cs`, `NameIndex.cs`, `RelationshipIndex.cs`, and `SubtagIndex.cs` — immutable lookup indexes.
- `src/Internal/Text/AsciiCode.cs`, `UnicodeName.cs`, and `LanguageTagParser.cs` — span-oriented lexical and normalization helpers.
- `src/Internal/Lookup/LanguageLookupEngine.cs`, `src/Internal/Normalization/LanguageNormalizationEngine.cs`, `src/Internal/Validation/LanguageValidationEngine.cs`, and `src/Internal/Tags/LanguageTagEngine.cs` — operation engines kept behind the public service.
- `src/Internal/Results/PublicResultFactory.cs` — deep defensive-copy boundary for every public result.
- `tools/LanguageCodes.DataGenerator/LanguageCodes.DataGenerator.csproj`, `Program.cs`, `DataManifest.cs`, `SourceReaders/*.cs`, `Model/*.cs`, `Validation/*.cs`, `Diff/*.cs`, and `Emission/*.cs` — built-in-.NET-only fetch, parse, reconcile, validate, diff, and code-generation tooling outside runtime `src/`.
- `specs/data-manifest.json` — pinned source provenance, dates, hashes, headers, encodings, and generator schema version.

## Implementation tasks

### Foundation and data generation

- [DONE] T001: Define the reconciled internal entity schema and encode invariants for Set membership, code equivalence, names, aliases, status, special/reserved entries, retirements, macrolanguages, Set 5 hierarchy, and IANA links.
- [DONE] T002: Add `specs/data-manifest.json` with every downloaded source URL/path, retrieval and upstream dates, SHA-256, encoding, expected header/media type, and generator schema version.
- [DONE] T003: Create the built-in-.NET-only `tools/LanguageCodes.DataGenerator` console project and wire it into the solution without adding a runtime project reference from `src/`.
- [DONE] T004: Implement strict Set 1/2 pipe-delimited ingestion, including the UTF-8 BOM, English/French semicolon-separated names, alpha-2 mappings, and distinct terminology/bibliographic fields.
- [DONE] T005: Implement strict Set 3 core-table ingestion for identifiers, cross-set codes, scope, language type, reference name, and comments.
- [DONE] T006: Implement Set 3 name-index ingestion that preserves every print/inverted name pair, de-duplicates exact source duplicates, and retains collisions across languages.
- [DONE] T007: Implement Set 3 macrolanguage ingestion with mapping status and referential validation for both macro and member identifiers.
- [DONE] T008: Implement Set 3 retirement ingestion with typed reasons, singular replacements, remedy text, replacement-code extraction for splits, and ISO date validation.
- [DONE] T009: Implement Set 5 TSV ingestion for identifiers and official English/French names.
- [DONE] T010: Implement Set 5 LoC hierarchy-table ingestion, reconcile every hierarchy code against the TSV, classify group/remainder notes where present, and reject hierarchy cycles.
- [DONE] T011: Implement streaming IANA Language Subtag Registry ingestion for folded fields, repeated descriptions/prefixes, language/extlang metadata, grandfathered/redundant tags, and all subtag types needed for complete tag validation.
- [DONE] T012: Reconcile Set 1/2/3/5 rows into one conceptual-entity model without merging same-spelled identifiers whose upstream semantics conflict.
- [DONE] T013: Derive typed code aliases for alpha-2, terminology, bibliographic, overlong, deprecated, retired, grandfathered, and IANA preferred-value forms while preserving provenance.
- [DONE] T014: Add generator integrity validation for headers, enum markers, code shapes, duplicate keys, cross-set equivalence, dangling edges/replacements, date formats, private-use ranges, count floors, and conflicting canonical targets.
- [DONE] T015: Implement deterministic emission with ordinal sorting, stable escaping, fixed line endings, no timestamps inside generated table bodies, and byte-identical repeated output.
- [DONE] T016: Generate the initial internal row tables and sorted lookup seeds from the pinned snapshots under `specs/`.
- [DONE] T017: Generate scalar metadata for ISO edition, each upstream date, retrieval date, schema/data version, and active/retired/name/relationship/subtag counts.

### Public model and API skeleton

- [DONE] T018: Add all planned public enums under `src/Api/Enums`, using explicit documented int32 member values and ensuring every enum is consumed by an interface or data class.
- [DONE] T019: Add `LanguageName`, `LanguageAlias`, `LanguageRelationship`, and `LanguageRetirement` data classes under `src/Api/Data` with public fields and no public methods.
- [DONE] T020: Add the reconciled `LanguageCodeEntry` data class with every code form, Set membership flags, scope/type/status, names, aliases, and relationships.
- [DONE] T021: Add lookup option/result/candidate data classes that represent unique, ambiguous, not-found, reserved, and retired outcomes without `out` parameters or nullable value types.
- [DONE] T022: Add normalization option/result data classes that report target form, status, applied steps, warnings, candidates, and resolved entry.
- [DONE] T023: Add validation option/issue/result data classes that distinguish well-formedness, assignment, acceptance policy, canonicality, errors, and warnings.
- [DONE] T024: Add `LanguageTag`, `LanguageSubtagMetadata`, and `LanguageTagMatchResult` field-based data classes capable of representing every RFC 5646 component and language-related registry record without tuples, records, properties, or generic declarations.
- [DONE] T025: Add all four planned exception classes under `src/Api/Exceptions`, each deriving directly from `System.Exception` with one documented constructor and no exception inheritance chain.
- [DONE] T026: Add the `LanguageDataInfo` static data class with generated scalar const/readonly fields only and no methods, delegates, arrays, or mutable collections.
- [DONE] T027: Add `ILanguageCatalog`, `ILanguageNormalizer`, `ILanguageValidator`, and `ILanguageTagService` with unique non-generic method names, plain parameters, and fully documented results/exceptions.

### Low-level runtime infrastructure

- [DONE] T028: Implement `AsciiCode` span helpers for ASCII-letter checks, lowercasing, length classification, separator scanning, and allocation-free lexical validation.
- [DONE] T029: Implement `UnicodeName` normalization with a fixed Unicode normalization form, invariant case handling, deterministic whitespace/punctuation folding, and explicit empty-input behavior.
- [DONE] T030: Materialize generated rows into internal static readonly tables without returning or retaining caller-owned mutable arrays.
- [DONE] T031: Build an ordinal code index covering active codes, all code forms, retired codes, reserved local-use ranges, and IANA preferred aliases, with collision checks during initialization.
- [DONE] T032: Build a normalized-name multimap that retains all source/name kinds and language collisions and supports stable exact, prefix, and contains traversal without LINQ.
- [DONE] T033: Build forward/reverse macrolanguage and Set 5 hierarchy indexes plus retirement replacement-chain indexes with cycle guards.
- [DONE] T034: Build the IANA subtag/tag indexes needed for registry assignment, prefixes, preferred values, suppress-script, grandfathered tags, and RFC 4647 matching.
- [DONE] T035: Implement `PublicResultFactory` to deep-copy every nested public result and prove internal tables never contain references returned to callers.

### Catalog and relationship operations

- [DONE] T036: Implement catalog enumeration and Set/scope/type/status filtering with stable code ordering and defensive snapshots.
- [DONE] T037: Implement explicit and generic code lookup across Set 1, Set 2 terminology/bibliographic, Set 3, Set 5, IANA aliases, local-use ranges, and retirements, reporting the matched form.
- [DONE] T038: Implement exact and normalized name lookup across reference, print, inverted, English, and French names with name-language/source filtering and explicit ambiguity.
- [DONE] T039: Implement deterministic exact/prefix/contains name search with de-duplicated candidates, match ranking, stable tie-breaking, and enforced result limits.
- [DONE] T040: Implement the generic value resolver with documented code-first precedence and throwing `RequireCode`/`RequireValue` variants for not-found and ambiguous outcomes.
- [DONE] T041: Implement direct macrolanguage parent/member queries with active/retired relationship policy and no implicit semantic substitution.
- [DONE] T042: Implement direct and transitive Set 5 parent/child queries with cycle-safe traversal, de-duplication, and deterministic order.
- [DONE] T043: Implement retirement lookup and one-to-one replacement-chain resolution while preserving split candidates and authoritative remedy text.

### Normalization and conversion

- [DONE] T044: Implement ISO code lexical normalization with explicit trimming policy, lowercase ASCII canonical casing, recognized-form classification, and idempotent results.
- [DONE] T045: Implement active code canonicalization policies for preserve-form, shortest Set 1 form, Set 3/terminology alpha-3, and Set 2 bibliographic form.
- [DONE] T046: Implement conversion among alpha-2, Set 2 terminology, Set 2 bibliographic, and Set 3 forms, returning unavailable-target and incompatible-entity statuses instead of fabricated codes.
- [DONE] T047: Implement retired/deprecated code normalization using only unambiguous authoritative replacements, with chain-loop, split, deleted-without-replacement, and already-canonical outcomes.
- [DONE] T048: Implement explicit macrolanguage normalization policies that preserve individuals by default and report every applied macro/member relationship.
- [DONE] T049: Compose all code-normalization stages so results include ordered applied steps, warnings, candidates, resolved entry, and an output that is stable on a second normalization pass.

### Validation

- [DONE] T050: Implement detailed lexical validation for null/empty, length, ASCII repertoire, separators, invalid characters, code form, and normalized casing.
- [DONE] T051: Implement registry validation for assigned, unassigned, reserved local-use, active, retired, alias, special, collection/family, macrolanguage, and individual codes.
- [DONE] T052: Implement validation policies for required Set/form, accepted statuses, scopes, types, groups, special identifiers, and private-use identifiers.
- [DONE] T053: Implement canonicality diagnostics that recommend equivalent terminology/shortest/preferred codes without rejecting noncanonical valid aliases unless strict mode is enabled.
- [DONE] T054: Implement the fast boolean assignment check and distinctly named `ReadOnlySpan<char>` validation path using the same underlying rules as detailed string validation.
- [DONE] T055: Add cross-operation invariants so successful conversions/normalizations validate under their target policy and failed validations never yield a misleading resolved entry.

### BCP 47 interoperability

- [DONE] T056: Implement a non-recursive RFC 5646 parser for langtags, grandfathered tags, and private-use-only tags, preserving all components needed to reproduce normalized output.
- [DONE] T057: Implement BCP 47 well-formedness checks for order, lengths, ASCII characters, duplicate variants, duplicate extension singletons, extension payloads, and private-use payloads.
- [DONE] T058: Implement IANA registry validity checks for language, extlang, script, region, and variant subtags, including prefix constraints and clear private-use handling.
- [DONE] T059: Implement tag canonicalization for separators/casing, preferred values, grandfathered tags, overlong ISO codes, extlang rules, and opt-in suppress-script removal while preserving extensions/private use.
- [DONE] T060: Implement resolution from a parsed tag’s effective language subtag to an ISO entry, including extlang, deprecated, reserved, and private-use outcomes.
- [DONE] T061: Implement language-related IANA subtag metadata lookup without exposing scripts, regions, or variants as ISO 639 entities.
- [DONE] T062: Implement RFC 4647 basic filtering over caller-provided tag arrays, including wildcard behavior, normalized comparison, invalid-range diagnostics, and stable input ordering.
- [DONE] T063: Implement RFC 4647 lookup over caller-provided tag arrays, including progressive truncation and extension-singleton removal rules.

### Public service integration

- [DONE] T064: Implement the parameterless `LanguageCodeService` under `src/Api/Classes` as the sole public implementation of all four contracts, exposing only the interface-declared instance methods.
- [DONE] T065: Connect public methods to the internal engines with positional arguments only, no overloading, no LINQ, no forbidden syntax, and no external runtime dependencies.
- [DONE] T066: Audit all API exits for defensive copying, deterministic ordering, null handling, documented exceptions, and isolation from mutable caller inputs.

### Versioned update workflow

- [DONE] T067: Implement a staging fetch command that downloads manifest sources to a validated temporary directory, records response metadata, and leaves pinned files untouched on any failure.
- [DONE] T068: Implement a semantic data-diff command covering code additions/changes/retirements, names, mappings, Set membership, hierarchy, IANA aliases, and preferred values.
- [DONE] T069: Implement an explicit accept-update command that verifies checksums/headers/invariants, replaces only listed spec inputs, updates manifest dates/hashes, and regenerates tables atomically.
- [DONE] T070: Implement a verify-generation command that fails when pinned inputs do not reproduce the checked-in generated tables and metadata byte for byte.
- [DONE] T071: Exercise the update workflow against an unchanged snapshot and a controlled changed fixture to confirm no-op stability, semantic diff accuracy, rollback-on-error, and retention of historical codes.

### Complete public-API test coverage

- [DONE] T072: Add generator parser tests with minimal and edge-case fixtures for every Set 2/3/5 and IANA input field, delimiter/encoding case, repeated field, malformed row, and unknown enum marker.
- [DONE] T073: Add generated-data integrity tests for expected snapshot counts, unique keys, complete cross-set mappings, all 639-2 B/T pairs, name preservation, referential integrity, acyclic Set 5 hierarchy, and manifest/hash agreement.
- [DONE] T074: Add data-model and enum tests covering every public field, enum member, null/reference default, array snapshot, and constructor behavior exposed by `src/Api`.
- [DONE] T075: Add catalog enumeration/filter tests covering every public catalog method, each Set/scope/type/status combination, deterministic ordering, empty results, and mutation isolation.
- [DONE] T076: Add code-lookup tests for every supported form, mixed casing, optional whitespace, code-form collisions, special identifiers, `qaa`–`qtz`, unknown values, retired values, and throwing require variants.
- [DONE] T077: Add name-lookup/search tests for reference/print/inverted/English/French names, Unicode normalization, case/space/punctuation handling, filters, ambiguity, stable ranking, limits, and code-versus-name precedence.
- [DONE] T078: Add relationship tests for each macrolanguage status, direct/reverse queries, languages without macros, Set 5 roots/leaves/multiple levels, transitive traversal, and fixture-injected cycles.
- [DONE] T079: Add retirement tests for every reason, one-to-one chains, split candidates, no replacement, remedies, effective dates, retired mappings, and replacement-loop fixtures.
- [DONE] T080: Add code-normalization/conversion tests for every source/target form, B/T pairs, casing/trimming policies, aliases, unavailable mappings, retirement policies, macrolanguage policies, applied-step order, and idempotence.
- [DONE] T081: Add code-validation tests for every lexical diagnostic, Set/form constraint, active/retired/alias/special/group/macro/individual policy, private-use boundary, canonical warning, and boolean/span parity.
- [DONE] T082: Add BCP 47 parser tests from RFC 5646 examples plus boundary cases for every component, grandfathered/private-use-only forms, extension sequences, duplicates, malformed order, and non-ASCII input.
- [DONE] T083: Add IANA tag validation/canonicalization tests for assigned/unassigned/deprecated subtags, preferred values, extlang/variant prefixes, overlong ISO codes, suppress-script policy, casing, extensions, private use, and normalization idempotence.
- [DONE] T084: Add RFC 4647 filtering/lookup tests from the RFC examples plus wildcard, truncation, singleton, no-match, invalid range, ordering, and duplicate-input cases.
- [DONE] T085: Add exception tests for all public throwing paths, exact exception type, input/context fields, and messages while ensuring result-returning alternatives do not throw for expected lookup/validation failures.
- [DONE] T086: Add `LanguageDataInfo` tests for edition/version/source dates/counts and consistency with generated tables without testing the syntax policy itself.
- [DONE] T087: Add service integration tests that invoke every method declared by all four public interfaces through both interface references and `LanguageCodeService`, including null, empty, minimum/maximum length, and large-result cases.
- [DONE] T088: Add mutation-isolation and concurrency tests proving returned entries/nested arrays/options cannot mutate shared data and parallel read-only calls remain deterministic and exception-free.
- [DONE] T089: Run line and branch coverage for all functionality reachable through `src/Api`, add missing behavioral cases until every public method/outcome/exception branch is exercised, and record any unreachable generated-data guards as explicit test fixtures rather than exclusions.
- [DONE] T090: Run the complete test suite and deterministic generation/update verification from a clean checkout, resolving all warnings and functional failures.
