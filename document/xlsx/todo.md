# Universal XLSX Library Development Plan

## Goal

Build a universal XLSX parsing, processing, and generating library in simplified C# as defined in `syntax.md`.

The implementation must use only built-in .NET libraries. Primary building blocks should be `System.IO`, `System.IO.Compression`, `System.Xml`, `System.Text`, `System.Buffers`, `System.Globalization`, `System.Security.Cryptography`, and other framework assemblies that ship with .NET. Use `Span<T>`, `ReadOnlySpan<T>`, pooled buffers, and streaming readers/writers where they reduce allocation or enable large-workbook processing.

The first-class target is XLSX. The architecture should also preserve and optionally support related OOXML spreadsheet package variants such as XLSM, XLTX, and XLTM. Legacy XLS and XLSB are out of scope unless a later phase adds a no-dependency binary parser.

## Simplified C# Constraints

- Source files use file-scoped namespaces.
- Only the deliberate API layer uses `public`; implementation types, constructors, methods, and fields use `internal`.
- Public API classes expose fields and ordinary methods, and narrowly scoped public interfaces expose instance methods only. The API does not use properties, records, structs, extension methods, attributes, or LINQ.
- Every public type, constructor, method, field, enum, and enum member has XML documentation. Documentation uses only `summary`, `remarks`, `param`, `returns`, `exception`, `example`, `see`, `seealso`, `para`, and `c` elements.
- API methods avoid `ref`, `out`, `in`, `params`, default parameters, optional arguments, generic method declarations, and nullable type syntax.
- Exception classes may derive directly from `System.Exception`.
- Implementation code avoids `try`/`catch`/`finally`; validation and parse methods return status objects where recovery is expected and throw library exceptions only at stable boundaries.
- API classes do not depend on class inheritance except for exceptions; classes may implement capability interfaces.
- Generic .NET collection types such as `List<T>` and `Dictionary<TKey, TValue>` are allowed.
- Streaming and parsing internals should prefer explicit loops, `XmlReader`, `XmlWriter`, `ZipArchive`, spans, pooled arrays, and deterministic state machines.

## Research Inputs

Existing libraries and platform references used to derive the feature inventory:

- Microsoft Open XML SpreadsheetML documentation: workbook, worksheet, chartsheet, table, pivot table, pivot cache, calculation chain, shared strings, conditional formatting, and formulas are core package parts.
  - https://learn.microsoft.com/en-us/office/open-xml/spreadsheet/structure-of-a-spreadsheetml-document
- OOXML package and SpreadsheetML references: spreadsheets are ZIP packages with content types, relationships, parts, sheets, cells, styles, shared strings, formulas, comments, hyperlinks, images, sorted tables, and filtered tables.
  - https://ooxml.info/docs/8/8.2/
  - https://ooxml.info/docs/8/8.5/
  - https://ooxml.info/docs/12/12.2/
- ClosedXML feature surface: workbook loading/saving, worksheets, ranges, rows, columns, styles, formulas, calc engine, tables, autofilters, data validation, conditional formatting, pivot tables, sparklines, hyperlinks, comments, page setup, sheet protection, worksheet copying, named ranges, rich text, and print settings.
  - https://github.com/ClosedXML/ClosedXML/wiki
- EPPlus feature surface: files and streams, templates, autofilters, data validation, conditional formatting, tables, pivot tables, slicers, formula calculation, charts, images, drawings, VBA signing, OLE objects, in-cell pictures, dynamic arrays, HTML/JSON export, and async I/O.
  - https://devtest.epplussoftware.com/en/Developers/Features
  - https://www.epplussoftware.com/
- ExcelDataReader feature surface: fast low-level reader, sheet cursor API, DataSet projection, row/column metadata, values, formulas, number formats, merged cells, hyperlinks, header/footer metadata, XLSX/XLSM support, CSV/XLS/XLSB adjacent scope.
  - https://github.com/ExcelDataReader/ExcelDataReader
- XlsxWriter feature surface: writer-only XLSX generation, formatting, merged cells, defined names, charts, autofilters, data validation, conditional formatting, images, rich strings, comments, textboxes, sparklines, VBA macros, memory-optimized writing, formulas, dates, tables, headers/footers, and page setup.
  - https://xlsxwriter.readthedocs.io/
  - https://xlsxwriter.readthedocs.io/contents.html
- SheetJS feature surface: workbook data model, read/write APIs, formulas, array formulas, dynamic formulas, file conversions, dense/sparse sheet models, and A1-style formula storage.
  - https://docs.sheetjs.com/docs/api/
  - https://docs.sheetjs.com/docs/csf/features/formulae/
- Apache POI feature surface: cells, rows, sheets, workbooks, styles, fonts, colors, comments, hyperlinks, data formats, data validation, conditional formatting, charts, drawings, anchors, tables, pivot tables, formula evaluation, named ranges, workbook types, and VBA parts.
  - https://poi.apache.org/apidocs/dev/org/apache/poi/ss/usermodel/package-summary.html
  - https://poi.apache.org/apidocs/dev/org/apache/poi/xssf/usermodel/package-summary.html
- openpyxl feature surface: reading/writing XLSX, styles, conditional formatting, charts, images, worksheet tables, comments, validation, formulas, pivot metadata preservation, and rich worksheet operations.
  - https://openpyxl.readthedocs.io/en/stable/formatting.html

## Feature Inventory

### Package And OPC Layer

- Open ZIP-backed OOXML packages from file path, stream, byte array, and memory buffer.
- Save packages to file path, stream, and byte array.
- Read and write `[Content_Types].xml`.
- Read and write package relationships in `_rels/.rels`.
- Read and write part relationships in `.rels` sidecar files.
- Resolve relationship targets including relative paths, absolute package paths, fragments, and external targets.
- Normalize package paths and block path traversal.
- Preserve unknown content types, relationship types, parts, attributes, and extension lists.
- Detect malformed ZIP archives, missing required parts, duplicate parts, duplicate relationship IDs, invalid content types, and unsupported package variants.
- Detect ZIP bomb risks with configurable limits for entries, compressed bytes, uncompressed bytes, XML depth, XML token size, relationship count, shared string count, cell count, and worksheet dimensions.
- Support strict and transitional OOXML namespaces.
- Support markup compatibility constructs, including alternate content and ignored namespaces, by preserving unsupported nodes.
- Support package core properties, app properties, and custom properties.
- Support thumbnails and preview metadata preservation.
- Support custom XML parts and relationships.
- Support digital signature parts preservation.
- Support digital signature validation and authoring as a later advanced phase if available with built-in .NET cryptography APIs.
- Support encrypted package detection.
- Support Agile/Standard OOXML encryption as an advanced phase using built-in cryptography plus a no-dependency compound-file parser if required.

### XML Infrastructure

- Central namespace constants for SpreadsheetML, DrawingML, relationships, package metadata, extended properties, custom properties, and markup compatibility.
- Streaming XML read helpers with explicit state machines.
- Streaming XML write helpers with stable element and attribute order.
- XML value parsers for booleans, integers, unsigned integers, doubles, decimals, dates, times, colors, cell references, ranges, formulas, relationship IDs, and ST_* enum values.
- XML escaping and shared string rich-text handling using `XmlReader` and `XmlWriter`.
- Unknown XML preservation strategy for unsupported nodes, attributes, and namespaces.
- Diagnostics with part path, element name, attribute name, row, column, relationship ID, and severity.

### Workbook Model

- Workbook object with sheets, names, styles, theme, shared strings, calculation settings, external links, properties, and package metadata.
- Worksheet, chartsheet, macro sheet, and dialog sheet metadata.
- Sheet order, sheet IDs, relationship IDs, visibility, tab color, code name, state, and display name.
- Workbook views, active tab, window state, selected tabs, and workbook protection.
- Workbook defined names, local sheet names, print titles, print areas, hidden names, function names, and external names.
- Workbook calculation mode, full calculation flags, iterative calculation settings, precision-as-displayed, reference mode, and calculation chain metadata.
- Workbook file properties: title, subject, creator, company, manager, category, keywords, comments, language, created/modified timestamps, application metadata, custom properties.
- Workbook compatibility settings and extension lists.
- Template open and save flow.
- Preserve macros, VBA project parts, signature parts, and OLE relationships in macro-enabled packages.

### Worksheet Grid

- Sparse cell storage for large sheets.
- Dense optional storage for small sheets or fast matrix access.
- Row and column indexing by 1-based Excel coordinates.
- A1 references, R1C1 references, absolute references, 3D references, sheet-qualified references, and named range resolution.
- Cell types: blank, number, shared string, inline string, boolean, error, formula, date/time logical value, rich text, and unknown raw cell.
- Cell cached values and formula values.
- Row metadata: height, custom height, hidden, outline level, collapsed, style, custom format, spans, thick top/bottom.
- Column metadata: width, custom width, hidden, best fit, outline level, collapsed, style, phonetic, min/max spans.
- Sheet dimension parsing and recalculation.
- Merged cells with validation for overlaps and bounds.
- Hyperlinks including external URLs, internal anchors, tooltips, display text, and relationship-backed links.
- Comments, notes, threaded comments, authors, comment positions, visibility, and legacy VML comments.
- Cell rich text runs, phonetic runs, text preservation, whitespace preservation, and inline formatting.
- Data validations with list, whole number, decimal, date, time, text length, custom formula, input messages, error messages, operators, blank handling, dropdown behavior, and IME mode.
- Conditional formatting with cell rules, text rules, time period rules, top/bottom rules, above/below average rules, duplicate/unique rules, formulas, color scales, data bars, icon sets, stop-if-true, priorities, differential formats, and extension rules.
- AutoFilters with range, value filters, custom filters, dynamic filters, date groups, top filters, color filters, icon filters, blank filters, and sort state.
- Sort conditions for ranges and tables.
- Sheet views: gridlines, headings, zoom, right-to-left, page break preview, page layout view, frozen panes, split panes, selections, active cell, and selected ranges.
- Sheet formatting: default row height, default column width, outline settings, zero height, base column width, and thick border defaults.
- Page setup: paper size, orientation, scale, fit-to-page, margins, page breaks, print options, print area, print titles, headers, footers, first page number, black-and-white, draft, comments print mode, errors print mode, horizontal/vertical centering.
- Sheet protection with built-in hash algorithms, locked/hidden cell styles, allowed actions, and password hash preservation.
- Sheet scenarios, custom views, ignored errors, smart tags, web publish items, and extension metadata preservation.

### Cell Values And Formatting

- Numeric values using `double` with exact text preservation where needed.
- Decimal-friendly APIs for financial input while storing SpreadsheetML-compatible numeric strings.
- Dates and times with 1900 date system, 1904 date system, serial conversion, leap-year compatibility behavior, elapsed time, durations, and ISO date cells.
- Boolean values.
- Excel error values: `#NULL!`, `#DIV/0!`, `#VALUE!`, `#REF!`, `#NAME?`, `#NUM!`, `#N/A`, `#GETTING_DATA`, and newer errors where supported.
- Shared string table de-duplication, reference counts, phonetics, rich text, and streaming load mode.
- Inline string reading/writing.
- Cell style IDs and default style resolution.
- Number format IDs, built-in number formats, custom number formats, culture-invariant format storage, and formatted text rendering.
- Fonts: name, size, bold, italic, underline, strike, color, scheme, family, charset, vert align, outline, shadow, condense, extend.
- Fills: pattern fills, foreground/background colors, gradient fills, stops, angle, left/right/top/bottom positions.
- Borders: left, right, top, bottom, diagonal, vertical, horizontal, style, color, diagonal up/down.
- Alignment: horizontal, vertical, text rotation, wrap text, shrink-to-fit, indent, relative indent, justify last line, reading order.
- Protection style: locked and hidden.
- Named styles, cell style formats, cell formats, differential formats, table styles, pivot styles, slicer styles, colors, indexed colors, theme colors, tints, and RGB colors.
- Theme parsing and writing including color schemes, font schemes, format schemes, and fallback colors.
- Style interning and de-duplication.
- Style mutation APIs that avoid accidental whole-workbook style explosion.
- Rich text formatting independent of cell style.
- Basic format evaluation to render display strings without recalculating formulas.

### Formulas And Calculation

- Preserve formula text exactly when possible.
- Read and write normal formulas.
- Read and write shared formulas.
- Read and write array formulas.
- Read and write dynamic array formulas and spill ranges.
- Read and write data table formulas.
- Read and write formula cached results.
- Read and write formula metadata, calc chain entries, and calculation properties.
- Parse A1 references and ranges.
- Parse sheet-qualified and workbook-qualified references.
- Parse structured references to tables.
- Parse named references.
- Tokenize formulas into a simple expression model for dependency analysis and reference updates.
- Render formulas from the expression model.
- Adjust formulas during row/column insert/delete/move/copy operations.
- Detect volatile functions and external references.
- Preserve unknown and future formula syntax.
- Calculation engine phase 1: dependency graph, arithmetic, comparisons, text concatenation, references, ranges, errors, coercion, and aggregate functions.
- Calculation engine phase 2: common functions such as SUM, AVERAGE, MIN, MAX, COUNT, COUNTA, IF, IFS, SWITCH, AND, OR, NOT, IFERROR, VLOOKUP, HLOOKUP, XLOOKUP, INDEX, MATCH, XMATCH, FILTER, SORT, UNIQUE, TEXT, VALUE, DATE, TIME, YEAR, MONTH, DAY, TODAY, NOW, ROUND, FLOOR, CEILING, ABS, POWER.
- Calculation engine phase 3: dynamic arrays, spilled arrays, implicit intersection, LET, LAMBDA metadata preservation, and advanced statistical/financial functions.
- Calculation options to recalculate all supported formulas, recalculate changed formulas, preserve cached values, or mark workbook for Excel recalculation.

### Ranges And Editing

- Range object with start/end row and column fields.
- Address parsing and formatting.
- Cell enumeration without LINQ.
- Get/set/clear cell values.
- Get/set/clear styles.
- Merge and unmerge ranges.
- Copy ranges within a sheet and across sheets.
- Move ranges.
- Insert/delete rows and columns.
- Shift cells up/down/left/right.
- Fill values, formulas, and series.
- Find used range, data range, first/last used row/column.
- Sort ranges.
- Apply filters.
- Resize tables and named ranges.
- Transpose ranges.
- Validate overlapping merges, tables, validations, filters, and pivots.
- Update affected formulas, hyperlinks, validations, conditional formatting, charts, tables, pivot sources, drawings, comments, page breaks, print areas, and defined names after structural edits.

### Tables

- Read and write table parts.
- Table names, display names, ranges, headers, totals rows, insert rows, published flags, comments, and connection IDs.
- Table columns with names, IDs, totals functions, totals labels, formulas, calculated column formulas, XML mappings, and styles.
- Table styles, built-in style names, custom style preservation, banded rows/columns, first/last column highlighting.
- Table autofilters and sort state.
- Table resize operations.
- Convert range to table.
- Export table rows to row cursor or `System.Data.DataTable`.
- Import `System.Data.DataTable` into a table.

### Pivot Tables, Slicers, And Timelines

- Preserve pivot table parts and pivot cache parts during load/save.
- Read pivot table definitions, fields, items, row fields, column fields, page fields, data fields, formats, filters, sort settings, and style info.
- Read pivot cache definitions, cache fields, shared items, records, source ranges, source tables, and refresh flags.
- Write basic pivot tables from table or range sources.
- Add row, column, page, and data fields.
- Support summary functions: sum, count, average, max, min, product, count numbers, standard deviation, variance.
- Support numeric grouping and date grouping.
- Support calculated fields and calculated items.
- Support pivot filters and label/value filters.
- Support pivot styles and pivot areas.
- Support slicer cache, slicers, table slicers, pivot slicers, and slicer styles.
- Support timelines as an advanced feature.
- Preserve unsupported pivot extensions.

### Charts, Drawings, And Media

- Preserve drawing parts, chart parts, image parts, VML drawings, diagram parts, and media relationships.
- Read and write worksheet drawing anchors: one-cell, two-cell, and absolute anchors.
- Read and write images with raw bytes and content type preservation.
- Add PNG, JPEG, GIF, BMP, SVG, WebP, and EMF/WMF images when content type is supported by Excel; do not require image decoding unless built-in APIs are available.
- Read and write hyperlinks on drawings.
- Read and write text boxes, shapes, connectors, and basic shape text.
- Read and write chart sheets.
- Read and write embedded charts.
- Chart types: column, bar, line, area, pie, doughnut, scatter, bubble, radar, stock, surface, combo, and waterfall/treemap/sunburst/histogram preservation.
- Chart series, categories, values, titles, legends, axes, gridlines, labels, trendlines, error bars, data tables, layouts, styles, colors, and theme integration.
- Sparklines and sparkline groups.
- In-cell pictures and IMAGE-function metadata preservation.
- OLE objects, embedded packages, controls, and form controls preservation.
- Authoring OLE objects as an advanced feature.

### Workbook Processing And Data APIs

- Streaming workbook reader for large files.
- Streaming worksheet row reader with low allocation.
- DOM-like workbook model for editing.
- Hybrid load modes: metadata only, values only, full fidelity, formulas only, styles only, selected sheets, selected ranges.
- Workbook writer that can stream worksheets while preserving package parts.
- Memory-optimized writer for large exports.
- Template loading with data replacement.
- Value projection to row arrays.
- Header row detection helpers.
- Column name to index and index to name helpers.
- `System.Data.DataTable` import/export.
- CSV import/export as a convenience layer using built-in .NET APIs.
- JSON-like object model export is out of scope unless implemented with built-in `System.Text.Json` and non-generic API wrappers.
- Workbook comparison and diff diagnostics.
- Workbook validation against structural rules.
- Repair mode for common malformed workbook issues.
- Formula dependency graph and impacted-cell analysis.
- External link listing and removal.
- Workbook sanitization: remove macros, external links, comments, hidden sheets, custom XML, personal metadata, or unknown parts.
- Workbook merging and sheet copying between workbooks.

### Performance And Reliability

- Allocation-aware parsing using spans and pooled buffers where practical.
- Avoid loading large XML text into memory unless preserving unknown nodes requires it.
- Shared string strategies: eager table, lazy table, streaming table, and inline fallback.
- Worksheet strategies: sparse dictionary, ordered row store, streaming cursor.
- Style interning to avoid duplicate style records.
- Write deterministic XML for stable tests.
- Configurable limits for cells, rows, columns, shared strings, XML depth, relationships, parts, images, comments, formulas, and package bytes.
- Cancellation is out of scope unless supported without `async`/`await` and without changing simplified syntax.
- Thread safety for independent workbook instances.
- No global mutable state except constant tables initialized safely.
- Detailed diagnostics without throwing for recoverable issues.
- Compatibility test corpus using generated fixtures and real-world fixtures that are safe to store.

### Security

- Block ZIP path traversal and invalid package names.
- Block external entity resolution in XML.
- Limit XML depth, attributes, text node length, and total text bytes.
- Limit decompressed bytes and compression ratio.
- Detect external links, external images, external OLE, external data connections, and query tables.
- Provide APIs to strip active content and external relationships.
- Preserve, remove, or reject macros according to explicit options.
- Preserve, remove, or reject embedded OLE objects according to explicit options.
- Validate workbook and sheet protection hashes.
- Support encrypted XLSX detection with clear error if decryption is not enabled.
- Avoid evaluating formulas that can trigger external or unsafe behavior.

### Compatibility Targets

- Excel for Microsoft 365.
- Excel 2007 and newer XLSX.
- LibreOffice Calc.
- Google Sheets import/export compatibility where practical.
- Apple Numbers import/export compatibility where practical.
- Strict OOXML and Transitional OOXML.
- Windows, macOS, and Linux without requiring Excel or Office.

## Public API Completion Tasks

- [DONE] T001: Complete the package API with `ReadOnlyMemory<byte>` and span-oriented open/read entry points, part and relationship enumeration, thumbnail and preview metadata access, and explicit Strict versus Transitional conformance reporting while using only built-in .NET APIs.
- [DONE] T002: Add public package-signature models and operations for enumerating signed parts, validating references and certificates, reporting trust separately from cryptographic validity, removing signatures, and authoring supported OPC signatures with built-in .NET cryptography.
- [DONE] T003: Add public encrypted-package operations for password-based Standard and Agile OOXML detection, decryption, and encryption over CFB containers, with explicit unsupported-algorithm results and secure password handling through built-in .NET cryptography.
- [DONE] T004: Complete the aggregate workbook model so callers can access and replace shared strings, calculation chains, chartsheets, macro/dialog sheet metadata, drawings, images, comments, threaded comments, connections, package properties, and preserved package parts through the workbook rather than only isolated part readers.
- [DONE] T005: Add public workbook and worksheet reference services for quoted and sheet-qualified A1 references, R1C1 references, 3D references, workbook-qualified references, defined-name resolution, print names, table structured references, and external-name detection.
- [DONE] T006: Add typed public worksheet models and operations for tab colors, outline properties, scenarios, custom sheet views, ignored errors, smart tags, web-publish items, drawing hyperlinks, and other planned worksheet metadata that is currently available only as raw XML.
- [DONE] T007: Complete cell-value APIs with decimal input and invariant exact-text preservation, ISO date cells, durations and elapsed-time values, all planned Excel errors, typed formula cached values, and typed rich-text and phonetic content.
- [DONE] T008: Replace raw-only style exposure with typed public font, pattern and gradient fill, border, alignment, protection, named-style, differential-style, table-style, pivot-style, slicer-style, indexed-color, and theme-color models, and complete style-building, interning, mutation, and resolution operations.
- [DONE] T009: Complete public formula services with volatile-function and unsafe/external-reference detection, workbook-wide dependency graphs, impacted-cell analysis, cross-sheet calculation context, calculation options, and explicit reporting for unsupported functions and future formula syntax.
- [DONE] T010: Complete range APIs with cell and row enumeration, get/set/clear value operations, merge and unmerge, cross-sheet copy and move, directional cell shifts, used/data/first/last range queries, filter application, named-range resize, and overlap validation for merges, tables, validations, filters, and pivots.
- [DONE] T011: Complete structural-edit APIs so row, column, cell, range, and sheet edits expose deterministic update results for formulas, names, tables, pivots, validations, conditional formatting, hyperlinks, comments, drawings, charts, page breaks, print definitions, and relationships.
- [DONE] T012: Complete table APIs with typed XML mapping and style fields, conversion from ranges, row-cursor and row-array projection, typed total functions, and table import/export operations that do not require libraries beyond built-in `System.Data`.
- [DONE] T013: Replace raw-only pivot-cache exposure with typed cache source, field, shared-item, record, grouping, refresh, and relationship models, and complete public pivot operations for numeric/date grouping, calculated items, label/value filters, summary functions, formats, areas, and cache refresh settings.
- [DONE] T014: Add complete slicer and timeline writer and authoring operations, including cache creation, table and pivot bindings, selections, timeline state, styles, relationship management, and unsupported-extension preservation.
- [DONE] T015: Complete drawing and media APIs with typed one-cell, two-cell, and absolute anchors; drawing hyperlinks; text boxes; shapes; connectors; controls; basic shape text; diagram preservation; image replacement; and raw-image authoring for every planned Excel-supported content type without image-decoding dependencies.
- [DONE] T016: Complete chart APIs with typed creation and editing for column, bar, line, area, pie, doughnut, scatter, bubble, radar, stock, surface, and combo charts, plus titles, series, axes, gridlines, labels, trendlines, error bars, data tables, layouts, styles, colors, and preservation models for newer chart types.
- [DONE] T017: Add public authoring and removal operations for embedded packages, OLE objects, form controls, VBA relationships, and in-cell picture metadata, while retaining a raw-byte path that requires no external decoder or Office installation.
- [DONE] T018: Complete workbook data-processing APIs with value projection to row arrays, header-row detection, selected-range materialization, template replacement helpers, external-content inventory, and explicit formula-dependency and impacted-cell queries.
- [DONE] T019: Add workbook merge and cross-workbook sheet-copy operations with conflict policies and result mappings for sheet names, defined names, styles, shared strings, relationships, tables, pivots, drawings, charts, comments, and formulas.
- [DONE] T020: Complete public security options with limits for parts, compression ratio, XML attributes and total text, images, comments, formulas, and external relationships, plus explicit preserve/remove/reject policies for macros, OLE, external images, external links, connections, query tables, custom XML, and unknown active content.
- [DONE] T021: Add package and XML functional tests covering every public open/save path, content type and relationship operation, URI and path edge case, Strict and Transitional namespace path, markup-compatibility behavior, unknown-part round trip, thumbnail handling, and configured resource limit.
- [DONE] T022: Add signature and encryption functional tests covering valid, invalid, tampered, unsupported, wrong-password, Standard, Agile, CFB, certificate, removal, and save/reopen scenarios using generated or redistributable fixtures.
- [DONE] T023: Add workbook functional tests covering every public workbook operation, all sheet kinds and states, properties, names, views, protection, calculation settings, package variants, aggregate part access, merge, sheet copy, template, comparison, validation, repair, and sanitization behavior.
- [DONE] T024: Add worksheet and cell functional tests covering every public grid operation and typed worksheet feature, including rows, columns, values, references, dimensions, views, panes, print settings, protection, merges, hyperlinks, comments, scenarios, custom views, ignored errors, and extension preservation.
- [DONE] T025: Add value, shared-string, rich-text, date, duration, error, number-format, color, theme, and typed-style functional tests covering successful conversions, boundary values, invalid inputs, style resolution, mutation isolation, deduplication, and read/write round trips.
- [DONE] T026: Add formula functional tests covering every public tokenizer, parser, writer, reference-update, dependency, impacted-cell, calculation, recalculation, shared/array/dynamic/data-table formula, cross-sheet, structured-reference, named-reference, external-reference, volatile, error, and unsupported-function path.
- [DONE] T027: Add range and structural-edit functional tests covering enumeration, value and style mutation, fill, sort, transpose, merge, shift, same-sheet and cross-sheet copy/move, insert/delete, overlap validation, and updates to every dependent feature exposed by the public API.
- [DONE] T028: Add table, AutoFilter, data-validation, and conditional-formatting functional tests covering every public operation, rule and filter kind, table conversion and resize, cursor and `DataTable` transfer, structured formulas, style settings, relationship updates, invalid inputs, and read/write round trips.
- [DONE] T029: Add pivot, pivot-cache, slicer, timeline, connection, query-table, and external-link functional tests covering every typed public field and operation, relationship resolution, grouping, calculations, filters, refresh state, preservation, removal, invalid inputs, and save/reopen behavior.
- [DONE] T030: Add drawing, image, chart, chartsheet, sparkline, comment, threaded-comment, embedded-object, form-control, VBA, and in-cell-picture functional tests covering every public reader, writer, authoring, relationship, preservation, replacement, and removal path without requiring Office or external source libraries.
- [DONE] T031: Add streaming and data-processing functional tests covering every public workbook and sheet reader/writer session, row cursor, selected load mode, row-array projection, header detection, CSV and `DataTable` import/export, cancellation-free failure cleanup, and interoperability between streaming and editable models.
- [DONE] T032: Add security-policy functional tests covering traversal, duplicate parts and relationships, malformed ZIP/XML, external entities, all configurable limits, active and external content detection, preserve/remove/reject policies, unsafe formula handling, protection-hash validation, and sanitizer results.
- [DONE] T033: Add compatibility round-trip tests with representative Excel 2007+, Microsoft 365, Strict OOXML, Transitional OOXML, LibreOffice, Google Sheets, and Apple Numbers fixtures, asserting preservation or explicit diagnostics for every planned package variant and extension family.
- [DONE] T034: Complete a public-behavior coverage audit and add missing success, boundary, null, malformed-input, diagnostic, and round-trip cases until every method on every `src/Api/Contracts` interface and every state-bearing public API model is exercised by feature-level tests.
