# Universal Unicode Library Development Plan

## Goal

Build a pure C# Unicode library for normalization, segmentation, case mapping, identifier safety, and PDF-generation text support. Runtime code must use only built-in .NET APIs and project-owned generated Unicode tables. Source under `src/` must follow the simplified C# subset in `syntax.md`, including method-only interfaces for behavioral and input-source boundaries. Public symbols define the supported API and require XML documentation; implementation symbols use `internal` and do not carry XML documentation.

Unicode 17.0.0 is the current target baseline as of 2026-08-03.
Release overview: https://www.unicode.org/versions/Unicode17.0.0/

## Complete Feature List, Low Level To High Level

- Unicode version metadata and data provenance.
- Unicode scalar validation for UTF-16 strings, surrogate pairs, unpaired surrogate detection, replacement policy, and round-trip checks.
- Scalar iteration with code point, UTF-16 index, UTF-16 length, and replacement status.
- UTF-8, UTF-16LE, UTF-16BE, and UTF-32 scalar encode/decode helpers using built-in .NET primitives where appropriate.
- Code point range tables with binary search, compact stage tables, trie-like lookup, and generated lookup classes.
- Core UCD properties: general category, combining class, bidi class, decomposition mapping, decomposition type, numeric type/value, mirrored, East Asian width, joining type/group, line break, grapheme break, word break, sentence break, Indic conjunct break, Hangul syllable type, script, script extensions, block, age, emoji properties, default ignorable, variation selector, noncharacter, private use, control, whitespace, pattern whitespace, and pattern syntax.
- Character names, name aliases, named sequences, and name lookup.
- Property and property-value alias lookup for diagnostics and optional tools.
- Unicode set/range operations for internal data generation and public inspection.
- Hangul algorithmic decomposition and composition.
- Canonical decomposition and compatibility decomposition.
- Canonical ordering by combining class.
- Canonical composition with composition exclusions.
- NFC, NFD, NFKC, and NFKD normalization.
- Normalization quick check with yes/no/maybe results.
- Normalization span helpers for already-normalized prefixes.
- Normalized append and normalized concat for boundary-only repair.
- Stream-safe text format insertion and validation.
- FCD and FCC support for processing pipelines that need canonical-order guarantees.
- NFKC case fold and compare-key generation.
- Normalization conformance against `NormalizationTest.txt`.
- Grapheme cluster segmentation, including extended grapheme clusters, emoji modifier sequences, emoji ZWJ sequences, regional indicators, variation selectors, prepend/extend/spacing marks, Hangul syllable sequences, and Indic conjunct behavior.
- Word boundary segmentation for words, numbers, kana, Hebrew letters, mid-letter punctuation, and emoji.
- Sentence boundary segmentation for terminators, abbreviating punctuation behavior, close punctuation, numeric separators, and separator classes.
- Line break classification and default line break opportunities.
- Boundary iterators returning UTF-16 offsets and cluster/slice records.
- Segment-safe truncation, prefix/suffix extraction, and width-limited cutting for UI and PDF layout.
- Simple uppercase, lowercase, and titlecase mappings.
- Full uppercase, lowercase, titlecase, and case folding mappings.
- Context-sensitive casing rules such as final sigma and after-soft-dotted handling.
- Locale-sensitive casing for Turkish, Azeri, Lithuanian, and other Unicode special casing records.
- Titlecase by word or grapheme boundaries.
- Case-insensitive compare keys using canonical and compatibility options.
- Identifier character checks for `ID_Start`, `ID_Continue`, `XID_Start`, and `XID_Continue`.
- UAX #31 default identifiers.
- Immutable identifiers and normalized identifiers.
- Case-insensitive identifiers based on stable normalization and folding.
- Pattern syntax and pattern whitespace classification.
- Identifier profile builder based on allow ranges, disallow ranges, normalization form, case policy, and script policy.
- Identifier diagnostics with error location, code point, category, rule, and severity.
- UTS #39 General Security Profile.
- Confusable skeleton generation from `confusables.txt`.
- Single-script, highly restrictive, moderately restrictive, minimally restrictive, and unrestricted identifier restriction levels.
- Mixed-script detection using Script and ScriptExtensions.
- Whole-script and mixed-script confusable checks where data allows.
- Invisible, default-ignorable, join-control, bidi-control, and variation-selector warnings.
- Bidi safety checks for identifiers and labels.
- Spoof-check result object with flags and diagnostics.
- PRECIS-inspired username, nickname, and opaque-string preparation profiles without implementing external network protocols.
- IDNA-adjacent label preparation helpers limited to Unicode safety primitives, not full DNS protocol handling.
- PDFDocEncoding byte-to-Unicode and Unicode-to-byte mapping.
- PDF text string encoding selection: PDFDocEncoding when exact, UTF-16BE with BOM when needed.
- PDF text string decoding for metadata, outlines, annotations, and form text.
- UTF-16BE byte writer and reader with surrogate handling for supplementary characters.
- PDF literal-string and hex-string escaping helpers.
- ToUnicode CMap source mapping model: character code to Unicode scalar sequence.
- ToUnicode CMap range coalescing and validation helpers.
- Glyph-to-Unicode map records for font subset builders.
- Character collection and subset planning by scalar, script, and font coverage callback.
- Grapheme cluster boundaries for PDF text layout and safe text extraction mapping.
- Word and line break opportunities for PDF paragraph layout.
- Bidi class and paired bracket/mirroring data for higher-level PDF bidi layout.
- Script run detection for font fallback and shaping integration points.
- Text width classification helpers: East Asian width, emoji presentation, combining marks, zero-width controls, and format controls.
- PDF accessibility helpers: normalize text for ActualText, Alt text, bookmarks, outlines, structure elements, and metadata.
- PDF/A and PDF/UA support helpers: Unicode mapping required checks, missing ToUnicode diagnostics, and text-string encoding diagnostics.
- Data generator for Unicode files using only built-in .NET APIs.
- Generated source verification with checksums and Unicode version pinning.
- XML documentation on every public runtime type, constructor, method, field, enum, and enum member, using only the elements allowed by `syntax.md`.
- Test fixtures for official Unicode conformance files and focused PDF encoding cases.

## Open Tasks

All runtime implementation in these tasks must depend only on built-in .NET APIs and project-owned generated data. Span-based APIs should be used for buffer-oriented operations where they provide a natural allocation-conscious contract.

- [DONE] T001: Download and checksum the complete pinned Unicode 17.0 UCD, emoji, and security inputs, then replace the representative generated tables with production-complete tables consumed by every public lookup and algorithm.
- [DONE] T002: Complete scalar iteration, malformed UTF-16 policy, and UTF-8/UTF-16LE/UTF-16BE/UTF-32 encoding contracts with explicit strict and replacement behavior plus Span and ReadOnlySpan buffer entry points where appropriate.
- [DONE] T003: Add exhaustive scalar and byte-encoding tests covering every surrogate shape, boundary scalar, malformed byte sequence, byte order, BOM case, destination-size failure, and round trip.
- [DONE] T004: Expand the public UCD property model and lookup contract to cover decomposition type and mapping, numeric type and value, mirroring and paired brackets, joining type and group, age, Indic conjunct break, Hangul syllable type, all planned break classes, and the complete planned binary and emoji property set.
- [DONE] T005: Add Unicode 17 data-driven property tests over full ranges and transition boundaries, including default values, unassigned and non-scalar inputs, Script_Extensions, mirrored pairs, joining data, numeric rationals, age, and every public property enum value.
- [DONE] T006: Add public character-name alias and named-sequence inspection, loose property and property-value alias resolution, and immutable range-set operations for containment, normalization, union, intersection, difference, and complement.
- [DONE] T007: Add complete behavioral tests for name and alias matching, algorithmic names, named sequences, property aliases, normalized and overlapping range inputs, all range-set operations, defensive copies, and scalar-range boundaries.
- [DONE] T008: Complete every public normalization operation against the full Unicode 17 decomposition, composition, exclusion, and quick-check data, including Hangul, normalized append and concat, stream-safe text, FCD, FCC, and stable NFKC case folding.
- [DONE] T009: Replace sampled normalization fixtures with the complete official Unicode 17 `NormalizationTest.txt` suite and add edge tests for quick-check spans, boundary repair, long non-starter runs, invalid UTF-16, idempotence, and canonical equivalence.
- [DONE] T010: Complete the public casing contract for simple and full mappings, context and locale rules, folding and compare keys, and explicit word-boundary and grapheme-boundary titlecasing without method overloading.
- [DONE] T011: Replace sampled casing fixtures with full Unicode 17 `SpecialCasing.txt` and `CaseFolding.txt` coverage and add tests for every conditional rule, supported locale tag form, expansion mapping, supplementary scalar, titlecasing mode, and caseless invariant.
- [DONE] T012: Complete grapheme, word, sentence, and UAX #14 line segmentation and add public mandatory-versus-optional line-break results plus segment-safe prefix, suffix, slicing, and display-width-limited truncation operations.
- [DONE] T013: Replace sampled boundary fixtures with the complete Unicode 17 grapheme, word, sentence, emoji, and line-break conformance suites and test empty text, malformed UTF-16, UTF-16 offset integrity, cluster-safe cuts, line-break status, and width-limited cuts.
- [DONE] T014: Add a public identifier-profile construction API for allow and deny ranges, normalization and case policies, unassigned handling, and permitted script sets while retaining the behavioral profile interface for caller-defined policies.
- [DONE] T015: Complete UAX #31 default, immutable, normalized, and case-insensitive identifier behavior and enrich diagnostics with general category and severity, then test every profile rule against the complete Unicode 17 identifier and pattern-property data.
- [DONE] T016: Expose UTS #39 Identifier_Status and Identifier_Type classification and complete the General Security Profile, Script_Extensions-aware script resolution, and every restriction-level calculation.
- [DONE] T017: Add public whole-script and mixed-script confusable checks, make confusable result flags reflect actual checks, and implement identifier and label bidi-safety diagnostics in addition to bidi-control warnings.
- [DONE] T018: Add full UTS #39 data-driven and adversarial tests for skeletons, allowed and restricted characters, script resolution, restriction levels, whole-script and mixed-script confusables, repeated-mark invisibility, controls, joiners, variation selectors, and bidi safety.
- [DONE] T019: Complete the PRECIS-inspired username, nickname, and opaque-string profiles and add an IDNA-adjacent label preparation and validation API limited to Unicode mapping, normalization, contextual, bidi, and security primitives rather than DNS processing.
- [DONE] T020: Add RFC 8264/8265/8266 and UTS #46 example-based tests plus edge coverage for profile mapping order, spaces, controls, empty values, compare keys, contextual joiners, label bidi rules, deviations, invalid scalars, and idempotence.
- [DONE] T021: Complete PDFDocEncoding, PDF text-string selection and decoding, UTF-16BE with BOM, and literal and hexadecimal string serialization, adding Span-based byte destinations for the encoding and escaping paths where appropriate.
- [DONE] T022: Add table-complete PDFDocEncoding tests and ISO 32000 cases for exact fallback selection, undefined bytes, BOM handling, supplementary scalars, malformed UTF-16 and byte input, literal escapes, hexadecimal output, and round trips.
- [DONE] T023: Complete the public glyph-to-Unicode mapping model, ToUnicode CMap validation, codespace selection, deterministic range coalescing, section splitting, scalar-sequence encoding, and actionable mapping diagnostics.
- [DONE] T024: Add focused and integration tests for ToUnicode CMaps covering one- through four-byte character codes, bfchar and bfrange choices, multi-scalar targets, supplementary scalars, section limits, duplicate and invalid mappings, deterministic output, and validation diagnostics.
- [DONE] T025: Add a public font-coverage callback contract and subset-plan results, then complete cluster-safe scalar collection, per-script planning, grapheme/word/line layout metadata, script runs, bidi bracket and mirror hints, and text-width classification.
- [DONE] T026: Add complete font-subset and PDF-layout tests for coverage gaps, fallback scripts, Common and Inherited resolution, emoji and combining clusters, word and line opportunities, East Asian widths, zero-width controls, paired brackets, mirroring, and mixed-direction runs.
- [DONE] T027: Expand PDF accessibility preparation to ActualText, Alt text, bookmarks, outlines, structure elements, and metadata, and complete PDF/A and PDF/UA diagnostics for required Unicode maps, invalid maps, missing ToUnicode data, and text-string encodings.
- [DONE] T028: Add ISO 32000 and accessibility-focused tests for every PDF text context and diagnostic rule, including normalization, prohibited or replacement scalars, extraction mappings, absent fonts, empty maps, severity aggregation, and valid PDFDocEncoding and UTF-16BE outcomes.
- [DONE] T029: Add coverage-guided behavioral tests for every public interface method, implementation constructor, data-class constructor and field, static field, enum value, success path, failure path, null argument, range error, and returned-array ownership contract in `src/Api`.
- [DONE] T030: Add cross-feature randomized and regression tests for normalization and casing idempotence, segmentation monotonicity, identifier preparation, security skeleton stability, encoding round trips, and PDF extraction mapping invariants.
