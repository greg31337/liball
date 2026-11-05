# Universal CSV Library Development Plan

## Goal

Build a universal CSV parsing, processing, and generation library for .NET using only built-in .NET APIs. The implementation must follow the simplified C# subset in `syntax.md`: file-scoped namespaces, classes/enums and method-only interfaces, at most one constructor per class, fields instead of properties, ordinary block-bodied methods, no attributes, no class inheritance, no generic declarations, no LINQ, no `try`/`catch`, no `async`/`await`, and no expression-bodied members. Classes may implement interfaces, but interfaces do not inherit or contain fields, default implementations, static abstract members, or generic methods. Only supported API types and members are public; implementation symbols are internal. Every public API symbol has XML documentation using only the elements permitted by `syntax.md`.

## Research Notes

Existing mature CSV tools converge on the following must-have capabilities:

- RFC 4180 support: comma delimiters, optional quoted fields, doubled quotes inside quoted fields, CRLF records, embedded delimiters, and embedded newlines.
- Liberal reading with conservative writing: CsvHelper explicitly emphasizes RFC 4180 compliance, Excel fallback behavior, low-memory forward-only reading, configurable mapping, conversion, validation, and CSV injection protections. Source: https://joshclose.github.io/CsvHelper/
- Dialect objects: Python `csv` and Apache Commons CSV both expose delimiter, quote, escape, line terminator, quoting mode, whitespace, strictness, header, null, and comment settings. Sources: https://docs.python.org/3/library/csv.html and https://commons.apache.org/proper/commons-csv/apidocs/org/apache/commons/csv/CSVFormat.Builder.html
- Sniffing and inference: Python `csv.Sniffer`, Sylvan.Data.Csv, and DuckDB detect dialects and headers from samples. DuckDB also infers column types and lets user settings override detection. Sources: https://docs.python.org/3/library/csv.html, https://www.nuget.org/packages/Sylvan.Data.Csv, and https://duckdb.org/docs/current/data/csv/auto_detection
- Streaming and large-file operation: CsvHelper yields one record at a time; Papa Parse uses callbacks/chunks; Sylvan exposes forward-only data-reader style access. Sources: https://joshclose.github.io/CsvHelper/getting-started/, https://www.papaparse.com/docs, and https://www.nuget.org/packages/Sylvan.Data.Csv
- Error handling modes: FileHelpers and DuckDB support throwing, ignoring, or storing rejected rows; TextFieldParser reports malformed line information. Sources: https://www.filehelpers.net/ and https://duckdb.org/docs/current/data/csv/overview
- Writer controls: quote minimal/all/none/non-null, custom line endings, custom null strings, escaping style, formula injection handling, append/flush behavior, and header/comment writing.

## High-Level Plan

1. Build a small, strict core that can parse RFC 4180 CSV from `TextReader` and write RFC 4180 CSV to `TextWriter`.
2. Add dialect configuration for real-world variants such as TSV, semicolon CSV, pipe-delimited files, alternate quote/escape behavior, comments, blank lines, trimming, null strings, and line endings.
3. Add row, header, schema, and diagnostics objects so callers can inspect data without reflection, attributes, or generic APIs.
4. Add narrow processing interfaces for row sources, destinations, and pipeline stages, with callback holder classes as convenient adapters.
5. Add generation helpers for writing rows, dictionaries, tables, transformed streams, inferred schemas, and safe spreadsheet-compatible output.
6. Add sniffer utilities for dialect, header, and simple type inference from a bounded text sample.
7. Add conformance, regression, fuzz-style, and large-file tests using only built-in runtime functionality plus the existing test framework.

## Feature List From Low Level To High Level

- Character source buffering with configurable buffer size.
- Unicode BOM handling for UTF-8 and UTF-16 when reading from streams.
- `TextReader` and `TextWriter` based core APIs.
- Optional file path helpers using built-in `File`, `StreamReader`, and `StreamWriter`.
- RFC 4180 parser state machine.
- CRLF, LF, and CR record separator recognition.
- Quoted fields with embedded delimiters, record separators, and doubled quote escaping.
- Unquoted fields with strict rejection of invalid quote placement when strict mode is enabled.
- Empty file handling.
- Empty row handling.
- Empty field handling.
- Final row without trailing newline.
- Row and field position tracking.
- Byte/character offset tracking where available.
- Record number, physical line number, and column number diagnostics.
- Configurable delimiter.
- Configurable quote character.
- Configurable escape character.
- Configurable doubled-quote behavior.
- Configurable line terminator for writing.
- Configurable read newline handling.
- Configurable quote mode for writing.
- Configurable trim mode.
- Configurable skip-initial-space behavior.
- Configurable blank-line behavior.
- Configurable comment marker.
- Configurable null strings.
- Configurable maximum field length.
- Configurable maximum row length.
- Configurable maximum column count.
- Strict, relaxed, skip-bad-row, and collect-rejects error modes.
- Error objects with code, message, row, column, line, and raw context.
- Header row reading.
- Header normalization.
- Case-sensitive or case-insensitive header lookup.
- Duplicate header policy.
- Missing header policy.
- Missing field policy.
- Extra field policy.
- Name-based field access.
- Index-based field access.
- Optional generated column names when no header exists.
- Schema model with column names, indexes, inferred types, nullability, and sample stats.
- Type conversion for string, bool, integer, long, decimal, double, DateTime, DateOnly, TimeOnly, Guid, and byte arrays where the target runtime supports them.
- Culture-aware numeric and date conversion using built-in `CultureInfo`.
- Configurable date/time formats.
- Configurable decimal and thousands separators.
- Conversion error reporting.
- Default values for missing, empty, or invalid fields.
- Row validation callbacks.
- Field validation callbacks.
- Processing pipeline for streaming transforms.
- Column selection by index or name.
- Column reorder and rename.
- Row filtering.
- Row projection.
- Row enrichment with computed fields.
- Row limit and skip.
- Multi-file concatenation.
- Union-by-name for compatible files.
- Simple in-memory table for small datasets.
- In-memory sort for bounded datasets.
- Grouping and aggregate helpers for bounded datasets.
- Join helpers for bounded datasets.
- CSV-to-CSV transform command surface.
- Dialect sniffer for delimiter, quote, escape, newline, comments, and header.
- Type sniffer for primitive column types.
- Sampling controls for sniffing.
- Writer row API.
- Writer field API.
- Writer table API.
- Header writing.
- Comment writing.
- Null writing.
- Conservative quoting.
- Quote all fields mode.
- Quote no fields mode with escaping.
- Quote non-null fields mode.
- Spreadsheet formula injection detection.
- Spreadsheet formula injection strip, escape, or error policy.
- Append mode helpers.
- Flush behavior controls.
- Raw row capture for diagnostics and rejects.
- Reject writer for malformed rows.
- Test corpus for RFC 4180 and common dialects.
- Benchmark-friendly large-file test fixtures.
- Documentation examples.

## Remaining Public API Tasks

- [DONE] T001: Add a public table-operation contract and implementation exposing bounded row access, cloning, slicing, column lookup, selection by index or name, reordering, projection, and renaming while keeping the data-holder methods internal.
- [DONE] T002: Extend the public table-operation contract with bounded ascending and descending sorting, grouping, row counts, and decimal sums by column index or name.
- [DONE] T003: Extend the public table-operation contract with bounded inner and left joins by column index or name, including deterministic header collision handling and null-filled unmatched fields.
- [DONE] T004: Add a public table-writer contract implemented by `CsvWriter` so callers can write a `CsvTable`, its optional header, and all rows through the configured dialect, null, quoting, newline, injection, and flush policies.
- [DONE] T005: Add a public field-validation contract and callback adapter, pipeline configuration fields, per-field validation processing, diagnostics, reject handling, and counters alongside the existing row validator.
- [DONE] T006: Add a configurable public reject sink that can receive malformed parser rows and validation-rejected rows with raw input and diagnostics and can emit a CSV reject stream without external source dependencies.
- [DONE] T007: Complete schema inference metadata by exposing sampled, missing, null, empty, and non-null counts and inferred format hints for every `CsvColumn`.
- [DONE] T008: Complete value-to-text conversion for every `CsvTypeKind` using `CsvColumn.FormatHint`, configured culture, numeric separators, date/time formats, null/default policies, and a deterministic hexadecimal representation for byte arrays.
- [DONE] T009: Add exception-less conversion-result APIs for every supported target type that distinguish success, null, missing, empty, defaulted, invalid, and out-of-range inputs and return converter diagnostics with row and field context.
- [DONE] T010: Track and expose byte offsets on rows and diagnostics for stream-backed readers when the encoding permits exact accounting, while retaining character offsets and an explicit unavailable value for arbitrary `TextReader` sources.
- [DONE] T011: Bring formula-injection detection and all writer policies in line with the downloaded OWASP guidance, including carriage return, line feed, and full-width formula initiators in addition to the existing ASCII initiators.
- [DONE] T012: Add public-surface parser tests covering all RFC 4180 rules, supported newline and dialect variants, headers and field policies, null and whitespace behavior, limits, diagnostics and positions, every error mode, raw rejects, and empty or truncated inputs.
- [DONE] T013: Add public-surface converter, schema, and sniffer tests covering every supported type, culture and format option, defaults and failures, sample statistics, nullability, ambiguous samples, bounded sampling, and dialect overrides.
- [DONE] T014: Add public-surface writer, file, and safety tests covering field and table output, every quote and injection mode, escaping, nulls, comments, newline normalization, flushing, append and leave-open behavior, UTF-8 and UTF-16 BOM detection, explicit encodings, and reject output.
- [DONE] T015: Add public-surface processing and table tests covering interface-based sources and sinks, skip and limit behavior, filtering, transformation, enrichment, row and field validation, progress and reject callbacks, projection, rename, sorting, grouping, aggregates, joins, concatenation, union-by-name, bounds, and mutation isolation.
- [DONE] T016: Close remaining functional coverage gaps until every public constructor, interface method, implementation method, data and option field, enum value, success path, failure path, and observable policy branch under `src/Api` is exercised through the public API.
