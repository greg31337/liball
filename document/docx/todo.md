# Universal DOCX Library Development Plan

## Goal

Build a universal DOCX parsing, processing, and generating library in simplified C# using only built-in .NET APIs. The library should read, inspect, edit, validate, create, and save WordprocessingML `.docx` files while preserving unknown package parts and markup whenever possible.

## Implementation Constraints

- Use only built-in .NET APIs, primarily `System.IO`, `System.IO.Compression`, `System.Xml`, `System.Text`, `System.Globalization`, and `System.Security.Cryptography`.
- Do not use Open XML SDK, DocumentFormat.OpenXml, SharpZipLib, Newtonsoft.Json, HtmlAgilityPack, or any other external runtime library.
- Follow `syntax.md` for all production files in `src`.
- Use file-scoped namespaces, classes, enums, fields, constructors, and ordinary block-bodied methods.
- Avoid properties, records, structs, delegates, attributes, class inheritance, extension methods, generic type declarations, generic method declarations, `params`, named arguments, `try`/`catch`, LINQ, `async`, iterators, and preprocessor directives.
- Use small method-only interfaces at stable substitution boundaries; do not mirror every concrete model class with an interface.
- Use only the `static`, `public`, `internal`, and `const` modifiers in production code. Reserve `public` for the deliberately supported API; mark parsing, serialization, validation plumbing, and other implementation helpers `internal`.
- Add XML documentation to every public type, constructor, method, field, enum, and enum member. Use only the XML elements allowed by `syntax.md`, and do not add XML documentation to internal symbols.
- Use explicit loops and helper classes instead of LINQ.
- Use public fields and simple methods only where state or behavior is part of the supported API; keep storage and helper members internal.
- Preserve unknown XML elements, attributes, relationships, and package parts by default.
- Prefer lossless parsing and round-tripping over normalization.
- Treat OPC relationships as the source of truth for locating parts; do not assume `/word/document.xml` always exists.

## Research Notes

- Microsoft Open XML documentation describes `.docx` as a ZIP-based Open XML package made of parts and relationships, with `WordprocessingDocument` representing the package and the main document part containing WordprocessingML content such as body, paragraph, run, and text elements. Source: https://learn.microsoft.com/en-us/office/open-xml/word/how-to-create-a-word-processing-document-by-providing-a-file-name
- Open XML package documentation emphasizes `[Content_Types].xml`, `.rels` relationship parts, package parts, random access through ZIP, and multiple WordprocessingML stories including main document, glossary, headers, footers, comments, text boxes, footnotes, and endnotes. Source: https://github.com/OfficeDev/open-xml-docs/blob/master/docs/about-the-open-xml-sdk.md
- OPC guidance warns not to hard-code part paths and recommends using the relationship graph as the primary structure. Source: https://wiki.openoffice.org/wiki/OOXML/Open_Packaging_Conventions_%28OPC%29
- Apache POI XWPF exposes high-level document objects for paragraphs, runs, tables, sections, headers, footers, pictures, footnotes, endnotes, hyperlinks, fields, and content controls, while still allowing lower-level XML access when high-level APIs are incomplete. Sources: https://poi.apache.org/components/document/index.html and https://poi.apache.org/components/document/quick-guide-xwpf.html
- python-docx demonstrates ergonomic high-level APIs for opening/creating documents, adding paragraphs/headings/tables/pictures/page breaks, accessing sections, styles, settings, core properties, headers, and footers. Sources: https://python-docx.readthedocs.io/en/develop/api/document.html, https://python-docx.readthedocs.io/en/stable/api/section.html, and https://python-docx.readthedocs.io/en/stable/user/styles-using.html
- docx4j positions itself as a create/load/edit/write library for Microsoft Open XML files with an in-memory object representation. Source: https://www.docx4java.org/trac/docx4j

## Complete Feature List, Low Level To High Level

### Package And OPC Layer

- ZIP archive open, create, read, update, and save.
- Stream-based and file-path-based loading and saving.
- Package part discovery.
- Package part path normalization.
- Package part URI validation.
- Content type default parsing.
- Content type override parsing.
- Content type creation and update.
- Package-level relationship parsing.
- Part-level relationship parsing.
- Internal relationship resolution.
- External relationship preservation.
- Relationship ID allocation.
- Relationship target normalization.
- Relationship deletion and orphan detection.
- Unknown part preservation.
- Unknown relationship preservation.
- Binary part preservation.
- XML part preservation.
- Part ordering stability where practical.
- Compression level selection.
- Duplicate ZIP entry detection.
- Missing required package item detection.
- Package repair mode for recoverable packaging issues.
- Package diagnostics with severity, code, message, and location.

### XML And Namespace Layer

- Streaming XML read support.
- Streaming XML write support.
- Namespace prefix preservation where possible.
- Namespace URI based lookup.
- Known namespace constants.
- Unknown namespace preservation.
- XML whitespace preservation controls.
- XML attribute read/write helpers.
- XML text escaping through built-in writers.
- AlternateContent parsing and preservation.
- Markup Compatibility attribute preservation.
- Ignorable namespace handling.
- Unknown element subtree buffering.
- XML validation helpers for required attributes and child order.
- XML canonical comparison helpers for tests.

### WordprocessingML Core Model

- Document root.
- Body.
- Paragraphs.
- Runs.
- Text nodes.
- Tabs.
- Line breaks.
- Page breaks.
- Column breaks.
- Carriage returns.
- No-break hyphens.
- Soft hyphens.
- Symbols.
- Field characters.
- Simple fields.
- Hyperlink runs.
- Bookmarks.
- Proofing errors.
- Permissions ranges.
- Smart tags and custom XML markup preservation.
- Structured document tags and content controls.
- Paragraph-level unknown content preservation.
- Run-level unknown content preservation.

### Text Extraction And Plain Text Processing

- Whole document plain text extraction.
- Story-specific text extraction.
- Paragraph text extraction.
- Table text extraction.
- Header and footer text extraction.
- Footnote and endnote text extraction.
- Comment text extraction.
- Text extraction with configurable separators.
- Text extraction with field result handling.
- Text extraction with deleted/inserted revision controls.
- Text search.
- Text replacement across runs.
- Text replacement preserving run formatting where possible.
- Tokenized text traversal.
- Unicode and XML whitespace handling.
- Plain text import.

### Formatting

- Run formatting: bold, italic, underline, strike, double strike, caps, small caps, hidden, vanish, emboss, imprint, outline, shadow.
- Run font family, size, color, highlight, shading, character spacing, position, kerning, language, right-to-left, complex script flags.
- Paragraph formatting: style, alignment, indentation, spacing, tabs, borders, shading, outline level, page break before, keep lines, keep next, widow control, contextual spacing, text direction.
- Table formatting: table style, width, layout, alignment, borders, shading, cell margins, cell spacing, look flags.
- Row formatting: height, header repeat, split prevention.
- Cell formatting: width, grid span, vertical merge, vertical alignment, text direction, borders, shading, margins.
- Section formatting: page size, orientation, margins, gutters, columns, headers/footers mode, title page, page numbering.
- Document defaults.
- Theme-aware formatting.
- Direct formatting read/write.
- Style-based formatting read/write.
- Effective formatting calculation in a later phase.

### Styles

- Styles part parsing.
- Paragraph styles.
- Character styles.
- Table styles.
- Numbering styles.
- Linked styles.
- Based-on style chains.
- Next paragraph style.
- Style aliases.
- Style priority and visibility flags.
- Default styles.
- Latent styles.
- Document defaults for paragraph and run properties.
- Style creation.
- Style update.
- Style deletion with usage detection.
- Style lookup by ID.
- Style lookup by name.
- Built-in style name mapping.
- Style import and conflict resolution.

### Numbering And Lists

- Numbering part parsing.
- Abstract numbering definitions.
- Numbering instances.
- Multi-level lists.
- Bullet lists.
- Decimal lists.
- Letter lists.
- Roman numeral lists.
- Restart behavior.
- Level overrides.
- Paragraph numbering properties.
- Numbering style links.
- Picture bullets preservation.
- List creation helpers.
- List continuation helpers.
- List text label calculation.

### Tables

- Table parse/create/edit.
- Row parse/create/edit.
- Cell parse/create/edit.
- Nested tables.
- Mixed paragraph/table body order.
- Table grid parsing.
- Column width handling.
- Cell merge detection.
- Horizontal merge support.
- Vertical merge support.
- Header rows.
- Repeating rows.
- Table captions and descriptions.
- Table style options.
- Table insertion and deletion.
- Row insertion and deletion.
- Cell insertion and deletion where valid.
- Table text extraction.
- Table to matrix projection.

### Sections, Headers, And Footers

- Section property parsing.
- Multiple section support.
- Default/even/first page headers.
- Default/even/first page footers.
- Header/footer relationship management.
- Header/footer creation.
- Header/footer editing.
- Header/footer inheritance and link-to-previous behavior.
- Page size and orientation changes.
- Margins and gutters.
- Columns.
- Page numbering settings.
- Section breaks.

### Media, Drawings, And Embedded Objects

- Image part discovery and preservation.
- Image relationship parsing.
- Image metadata and content type detection.
- Inline drawing parsing.
- Floating drawing preservation.
- Add image from bytes.
- Add image from stream.
- Image sizing in EMUs.
- Image alt text.
- Image cropping preservation.
- Chart part preservation.
- Diagram part preservation.
- Embedded object part preservation.
- OLE object preservation.
- VML drawing preservation.

### Links, References, And Fields

- External hyperlinks.
- Internal anchors.
- Bookmark creation and lookup.
- Cross-reference field preservation.
- Page number fields.
- Date fields.
- Author/title fields.
- Table of contents field preservation.
- Field instruction parsing.
- Field result parsing.
- Field update markers.
- Simple field creation.
- Complex field preservation.

### Notes, Comments, And Review Markup

- Footnotes.
- Endnotes.
- Footnote references.
- Endnote references.
- Comments.
- Comment ranges.
- Comment references.
- Inserted revisions.
- Deleted revisions.
- Move-from and move-to revisions.
- Formatting change revisions.
- Review metadata preservation.
- Revision acceptance.
- Revision rejection.
- Revision-aware text extraction.
- Track changes setting preservation.

### Document Properties And Settings

- Core properties.
- Extended application properties.
- Custom properties.
- Document settings.
- Compatibility settings.
- View settings.
- Zoom settings.
- Hyphenation settings.
- Proofing settings.
- Default tab stop.
- Track revisions setting.
- Update fields setting.
- Protection settings metadata preservation.
- Read-only recommendation preservation.
- Language settings.
- Font table.
- Theme parts.
- Web settings.

### Content Controls, Custom XML, And Data Binding

- Structured document tag parsing.
- Plain text content controls.
- Rich text content controls.
- Repeating section controls.
- Checkbox controls.
- Combo box controls.
- Drop-down controls.
- Date picker controls.
- Picture controls.
- Content control aliases and tags.
- Data binding metadata.
- Custom XML parts.
- Custom XML relationships.
- Custom XML item properties.
- Fill content controls from key/value data.
- Extract content control values.

### Templates, Merge, And Assembly

- Placeholder search.
- Placeholder replacement.
- Repeating block expansion.
- Conditional block inclusion.
- Table row templating.
- Content control based merge.
- Bookmark based insertion.
- Document append.
- Document split.
- Section import.
- Style import.
- Numbering import.
- Media import.
- Relationship remapping.
- ID remapping for bookmarks, comments, notes, drawings, and numbering.
- Conflict detection during merge.

### Generation APIs

- Create minimal valid document.
- Add paragraph.
- Add heading.
- Add run.
- Add formatted text.
- Add table.
- Add image.
- Add hyperlink.
- Add bookmark.
- Add page break.
- Add section break.
- Add header.
- Add footer.
- Add footnote.
- Add endnote.
- Add comment.
- Add style.
- Add numbering definition.
- Save as `.docx`.
- Save as `.dotx` template if requested.
- Preserve macro parts when opening `.docm`, but do not execute or author macros.

### Validation, Repair, And Compatibility

- OPC validation.
- WordprocessingML required part validation.
- Relationship validation.
- Content type validation.
- XML well-formedness validation.
- Required element validation.
- Required attribute validation.
- ID uniqueness validation.
- Relationship target existence validation.
- Style reference validation.
- Numbering reference validation.
- Header/footer reference validation.
- Media reference validation.
- Bookmark range validation.
- Comment range validation.
- Repair missing content types.
- Repair missing relationship IDs.
- Repair orphaned relationships.
- Repair empty body.
- Compatibility mode detection.
- Strict vs Transitional namespace detection.
- Macro-enabled package detection.

### Performance And Robustness

- Streaming package reads.
- Lazy part parsing.
- Lazy media loading.
- Large document text extraction.
- Bounded memory subtree buffering.
- Configurable preservation mode.
- Configurable validation mode.
- Deterministic saving for tests.
- Large image pass-through.
- Corrupt package diagnostics.
- Thread-safe static helpers where practical.
- No shared mutable global state except constants.

### Testing And Tooling

- Unit tests for OPC parsing and writing.
- Unit tests for relationship resolution.
- Unit tests for content types.
- Unit tests for minimal document creation.
- Unit tests for paragraphs and runs.
- Unit tests for tables.
- Unit tests for styles.
- Unit tests for numbering.
- Unit tests for headers and footers.
- Unit tests for media relationships.
- Unit tests for comments, footnotes, and endnotes.
- Unit tests for text extraction and replacement.
- Unit tests for templating and merge.
- Round-trip fixture tests.
- Golden XML tests.
- Corrupt input tests.
- Syntax conformance tests for simplified C#.
- Cross-compatibility smoke tests with documents produced by Microsoft Word, LibreOffice, Google Docs export, Pages export, and common server-side generators.

## Implementation Tasks

- [DONE] T001: Add public stream and file-path entry points for creating, opening, and saving documents and packages with caller-supplied load and save options.
- [DONE] T002: Complete the public package-part API for normalized lookup, enumeration, XML and binary content access, add, replace, remove, and orphan inspection operations.
- [DONE] T003: Complete the public content-type API for default and override lookup, creation, update, removal, and effective content-type resolution.
- [DONE] T004: Complete the public relationship API for package and part relationship enumeration, lookup, internal target resolution, external target preservation, ID allocation, creation, update, removal, and orphan detection.
- [DONE] T005: Expand public load and save options to control XML whitespace, unknown markup and part preservation, validation and repair behavior, lazy XML and binary loading, deterministic ordering, compression, and document-versus-template output.
- [DONE] T006: Expose package diagnostics for duplicate ZIP entries, missing required items, corrupt input, macro-enabled content, namespace conformance, and recoverable packaging defects without losing unknown data.
- [DONE] T007: Complete public document navigation for the body, core and custom properties, headers, footers, footnotes, endnotes, comments, content controls, custom XML, font table, theme, web settings, and all relationship-discovered stories.
- [DONE] T008: Complete the public body API for ordered block and section enumeration, paragraph, table, and content-control lookup, insertion, replacement, movement, and deletion, including final section properties.
- [DONE] T009: Complete the public paragraph API for ordered child traversal and editing of runs, hyperlinks, bookmarks, fields, content controls, proofing markers, permission ranges, smart tags, custom XML markup, and preserved unknown children.
- [DONE] T010: Complete the public run API for ordered text, tab, carriage return, break, hyphen, symbol, field-character, drawing, and unknown-child traversal and mutation.
- [DONE] T011: Add public construction operations for text nodes, runs, paragraphs, body blocks, hyperlinks, bookmarks, simple fields, content controls, and every supported break or symbol kind without requiring callers to provide raw XML.
- [DONE] T012: Complete public text processing with story-selective extraction, configurable separators and revision or field-result handling, token traversal, Unicode and XML-space handling, cross-run search and replacement, formatting-preserving replacement, and plain-text import.
- [DONE] T013: Complete the public run-format model and mutation path for all planned toggle properties, fonts, size, color, highlight, shading, spacing, position, kerning, language, bidirectional, and complex-script settings.
- [DONE] T014: Complete the public paragraph-format model and mutation path for styles, alignment, indentation, spacing, tab stops, borders, shading, outline level, pagination controls, contextual spacing, and text direction.
- [DONE] T015: Complete the public table, row, and cell format models and mutation paths for styles, dimensions, layout, alignment, borders, shading, margins, spacing, look flags, captions, descriptions, row repetition and splitting, spans, merges, vertical alignment, and text direction.
- [DONE] T016: Complete the public section-format and document-default mutation paths for page size, orientation, margins, gutters, columns, header and footer modes, title page, page numbering, paragraph defaults, and run defaults.
- [DONE] T017: Add public theme-aware and style-aware effective formatting calculation for runs, paragraphs, tables, rows, and cells while retaining direct and inherited formatting separately.
- [DONE] T018: Complete the public styles API for enumeration, typed lookup by ID and name, aliases, priority and visibility flags, default and latent styles, based-on and linked chains, next styles, built-in name mapping, create, update, delete-with-usage-diagnostics, and conflict-aware import.
- [DONE] T019: Complete the public numbering API for abstract definitions, instances, levels, overrides, style links, picture-bullet preservation, multi-level bullet, decimal, letter, and Roman list creation, continuation, restart, paragraph application, and label calculation.
- [DONE] T020: Complete the public table API for table, row, cell, grid, and nested-table creation and editing; mixed body ordering; column widths; horizontal and vertical merges; header rows; insertion and deletion; text extraction; and matrix projection.
- [DONE] T021: Complete the public section, header, and footer API for multiple sections, section breaks, default, even, and first-page stories, relationship management, creation, editing, effective inheritance, and link-to-previous behavior.
- [DONE] T022: Complete the public media API for image discovery, metadata and content-type detection, byte and stream insertion or replacement, source-part relationships, inline drawing creation, EMU sizing, alt text, and preservation of cropping, floating drawings, charts, diagrams, VML, OLE, and embedded objects.
- [DONE] T023: Complete the public links, bookmarks, and fields API for external hyperlinks, internal anchors, bookmark lookup and insertion, field instructions and results, update markers, common field creation, and preservation of cross-reference, page, date, author, title, table-of-contents, and complex fields.
- [DONE] T024: Complete the public footnote and endnote API for story enumeration, lookup, creation, editing, deletion, and insertion or removal of note references.
- [DONE] T025: Complete the public comments API for enumeration, lookup, creation, editing, deletion, comment ranges, and comment references with review metadata.
- [DONE] T026: Complete the public revisions API for inserted, deleted, moved, and formatting-change traversal, review metadata, revision-aware extraction, document-wide and selected-range acceptance or rejection, and track-changes preservation.
- [DONE] T027: Complete public read and mutation paths for core, extended, and custom properties and for view, zoom, hyphenation, proofing, default-tab, revision, field-update, protection, read-only, language, compatibility, font-table, theme, and web settings.
- [DONE] T028: Complete the public content-control API for nested traversal, aliases, tags, data binding, value extraction and filling, and plain-text, rich-text, repeating-section, checkbox, combo-box, drop-down, date-picker, and picture controls.
- [DONE] T029: Complete the public custom XML API for part content, relationships, item properties, data-binding lookup, insertion, replacement, and removal while preserving unsupported XML.
- [DONE] T030: Add public template operations for placeholder discovery and replacement, repeating block expansion, conditional inclusion, table-row templates, content-control merge, and bookmark-based insertion.
- [DONE] T031: Add public assembly operations for append, split, section import, style, numbering, and media import, relationship remapping, all required ID remapping, and conflict reporting.
- [DONE] T032: Add cohesive public generation operations for a minimal document, headings, formatted text, paragraphs, runs, tables, images, hyperlinks, bookmarks, breaks, sections, headers, footers, notes, comments, styles, and numbering definitions.
- [DONE] T033: Complete public validation for OPC rules, required WordprocessingML parts, XML structure and attributes, unique IDs, relationship targets, styles, numbering, headers, footers, media, notes, bookmark ranges, comment ranges, compatibility mode, namespace conformance, and macro-enabled packages.
- [DONE] T034: Complete public safe-repair operations for missing content types, missing relationship IDs, orphaned relationships, empty bodies, and other explicitly recoverable packaging defects, returning precise severity, code, message, and location diagnostics.
- [DONE] T035: Ensure every public edit and save path uses only built-in .NET APIs and preserves unknown parts, relationships, namespaces, prefixes where possible, Markup Compatibility markup, AlternateContent, ignorable namespaces, unknown subtrees, binary payloads, and macro parts by default.
- [DONE] T036: Add public-API tests for document and package creation, stream and file-path loading and saving, load and save options, `.docx` and `.dotx` output, and macro-part pass-through.
- [DONE] T037: Add public-API tests for part names, part content access and mutation, content types, relationship resolution and mutation, orphan handling, and package-level diagnostics.
- [DONE] T038: Add public-API round-trip tests for unknown XML, attributes, namespaces, prefixes, whitespace, Markup Compatibility, AlternateContent, unknown parts and relationships, binary parts, deterministic ordering, and compression choices.
- [DONE] T039: Add public-API tests for document and story navigation, ordered body blocks, paragraphs, runs, text nodes, breaks, symbols, hyperlinks, bookmarks, fields, proofing and permission markers, smart tags, content controls, and unknown children.
- [DONE] T040: Add public-API tests for whole-document and story-specific text extraction, separators, field and revision options, token traversal, Unicode and XML-space behavior, plain-text import, search, and formatting-preserving replacement across runs.
- [DONE] T041: Add public-API tests for complete run, paragraph, table, row, cell, section, direct, default, style-inherited, theme-aware, and effective formatting read and write behavior.
- [DONE] T042: Add public-API tests for every style category, lookup mode, inheritance link, metadata flag, default and latent style, create, update, delete usage diagnostic, built-in mapping, import, and conflict result.
- [DONE] T043: Add public-API tests for numbering definitions, instances, levels, overrides, picture-bullet preservation, all planned label formats, list creation, continuation, restart, style links, and paragraph application.
- [DONE] T044: Add public-API tests for table, row, cell, grid, and nested-table creation and editing, mixed block order, widths, merges, header and repeating rows, captions, descriptions, insertion and deletion, text extraction, and matrix projection.
- [DONE] T045: Add public-API tests for multiple sections, section breaks, page setup, page numbering, default, even, and first-page headers and footers, relationship management, editing, inheritance, and link-to-previous behavior.
- [DONE] T046: Add public-API tests for media discovery, image type detection, byte and stream insertion or replacement, relationships, inline drawings, sizes, alt text, and lossless preservation of cropping, floating drawings, charts, diagrams, VML, OLE, and embedded objects.
- [DONE] T047: Add public-API tests for external and internal links, bookmark operations, simple and complex fields, field metadata, footnotes, endnotes, comments, ranges, references, revisions, review metadata, and selective or document-wide acceptance and rejection.
- [DONE] T048: Add public-API tests for core, extended, and custom properties; all planned document settings; compatibility settings; font, theme, and web parts; custom XML; data bindings; and every planned content-control type.
- [DONE] T049: Add public-API tests for placeholder, repeating-block, conditional, table-row, content-control, and bookmark templating and for append, split, import, relationship remapping, ID remapping, and merge conflict handling.
- [DONE] T050: Add public-API validation and repair tests covering every diagnostic and repair category, Strict and Transitional packages, macro-enabled packages, malformed XML, dangling references, duplicate IDs, corrupt archives, and unrecoverable-input exception behavior.
- [DONE] T051: Add golden-package and round-trip tests that generate every supported high-level construct and reopen the result through the public API to verify semantic equivalence and preservation guarantees.
- [DONE] T052: Add public-API interoperability tests using the existing Microsoft Word, LibreOffice, Google Docs, Pages, and server-generator fixtures across navigation, extraction, editing, validation, and round-trip save paths.
- [DONE] T053: Add boundary, null, invalid-index, invalid-enum, malformed-value, missing-part, non-seekable-stream, read-only-stream, and disposed-stream tests for every public constructor and method, verifying stable exception and diagnostic contracts.
- [DONE] T054: Add an inventory-driven coverage test suite that exercises every public `src/Api` constructor, interface method, implementation method, data field behavior, enum value, success path, and documented failure path through consumer-visible APIs.
