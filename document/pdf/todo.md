# PDF Library Development Plan

> Historical note: this file records the original development plan. It is not
> an API manifest. The current source and its XML documentation define the
> public API, while `syntax.md` is the authoritative source-language policy.

## Goal

Build a standalone C# PDF generation library that starts with reliable PDF
writing primitives and grows into a practical document toolkit for reports,
invoices, papers, forms, certificates, and other business documents.

The implementation must follow the simplified C# syntax subset in `syntax.md`.
Production files use file-scoped namespaces, classes, enums, fields,
constructors, block-bodied methods, ordinary loops, ordinary conditionals, and
positional arguments. Only `public`, `internal`, `static`, and `const` modifiers
are permitted. Public types and members define the supported API; every other
symbol must be explicitly `internal`. Every public type, constructor, method,
field, enum, and enum member requires plain-English XML documentation using only
the elements allowed by `syntax.md`. The complete allowed and prohibited syntax
lists in `syntax.md` take precedence over this summary.

## Research Basis

Existing libraries show that successful PDF generation stacks usually have
both a low-level PDF/canvas API and higher-level document layout helpers.

- PDFsharp focuses on drawing-style PDF creation and processing, while
  MigraDoc adds an object model with paragraphs, tables, charts, sections,
  bookmarks, links, tables of contents, indexes, and automatic page breaks.
  Source: https://docs.pdfsharp.net/
- QuestPDF emphasizes a layout engine with page settings, margins, headers,
  footers, page numbers, text styles, images, tables, rows, columns, layers,
  page breaks, repeated content, barcodes, charts, and developer preview/debug
  tooling. Sources:
  https://www.questpdf.com/quick-start.html
  https://www.questpdf.com/features-overview.html
  https://www.questpdf.com/api-reference/page/settings.html
- iText Core highlights enterprise PDF needs: PDF 1.x/PDF 2.0 generation,
  low-level structure access, high-level document APIs, PDF/A, PDF/UA,
  tagging, bookmarks, watermarks, page numbers, encryption, digital signatures,
  forms, flattening, page assembly, and optimization. Sources:
  https://itextpdf.com/products/itext-core
  https://itextpdf.com/products/features
- Apache PDFBox highlights document operations beyond generation: create PDFs,
  embedded fonts and images, text extraction, split/merge, forms, PDF/A
  validation, rendering pages as images, printing, and digital signatures.
  Source: https://pdfbox.apache.org/
- ReportLab separates low-level canvas drawing from high-level Platypus
  document layout with templates, frames, flowables, paragraphs, tables, charts,
  links, outlines, forms, and encryption. Sources:
  https://docs.reportlab.com/reportlab/userguide/ch2_graphics/
  https://docs.reportlab.com/reportlab/userguide/ch5_platypus/
  https://docs.reportlab.com/reportlab/userguide/ch4_pdffeatures/
- HTML-to-PDF tools such as wkhtmltopdf show that HTML/CSS template conversion
  is a common workflow for invoices, certificates, contracts, and marketing
  documents, but this should be treated as a later adapter rather than the core
  rendering model. Source: https://wkhtmltopdf.org/index

## Critical Must-Have Features

1. Correct PDF serialization with stable object IDs, offsets, page trees,
   resource dictionaries, streams, and metadata.
2. Low-level canvas API for text, images, paths, shapes, colors, transforms,
   clipping, and graphics state.
3. Font management with built-in fonts first, then TrueType/OpenType loading,
   embedding, subsetting, Unicode text, and accurate measurement.
4. Image support for raw RGB first, then JPEG and PNG loading, sizing, scaling,
   and reuse.
5. Page settings: standard sizes, custom sizes, orientation, margins,
   backgrounds, watermarks, headers, footers, and page numbers.
6. Automatic layout: text wrapping, stacks, rows, columns, grids, tables,
   lists, frames, page breaks, repeated headers, and keep-together behavior.
7. Reusable styling: colors, text styles, paragraph styles, borders, padding,
   spacing, table styles, document themes, and units.
8. Navigation and interactivity: links, anchors, outlines/bookmarks, table of
   contents, named destinations, and annotations.
9. Document metadata and compliance path: title, author, subject, keywords,
   creation date, PDF/A preparation, tagging preparation, and accessibility
   reading order.
10. Security and distribution: encryption, permissions, digital-signature
    preparation, attachments, deterministic output, and validation tools.
11. High-level templates for reports, invoices, academic/technical papers,
    forms, certificates, receipts, labels, and letters.
12. Developer tooling: diagnostics, debug layout overlays, golden-file tests,
    PDF structure tests, sample documents, and performance benchmarks.

## Feature Inventory From Low Level To High Level

### Level 1: PDF File Core

- PDF version selection, initially PDF 1.4.
- PDF object model for catalog, pages, page, font, image, content stream,
  metadata, outlines, annotations, and form objects.
- Cross-reference table writer.
- Trailer writer.
- Stream writer.
- ASCII and binary stream support.
- Flate compression.
- Hex encoding helper.
- Indirect object references.
- Resource dictionaries.
- Page tree construction.
- Object ID allocation.
- Deterministic object ordering.
- Save to file.
- Save to stream.
- Save to byte array.
- PDF validation smoke tests.

### Level 2: Canvas And Graphics

- Coordinate helpers using PDF points.
- Graphics state save and restore.
- Transforms: translate, scale, rotate, skew, matrix.
- Stroke color and fill color.
- RGB color.
- Gray color.
- CMYK color.
- Alpha/transparency later.
- Line width.
- Line cap.
- Line join.
- Dash pattern.
- Move, line, cubic Bezier curve, close path.
- Stroke path.
- Fill path.
- Fill and stroke path.
- Rectangle.
- Rounded rectangle approximation.
- Circle and ellipse approximation.
- Clipping path.
- Horizontal and vertical rules.
- Image draw with scale.
- Image draw with preserve-aspect-fit.
- Image draw with crop-fill.
- Reusable form XObjects for repeated graphics.
- Watermark drawing.
- Debug drawing layer.

### Level 3: Text And Fonts

- Standard PDF fonts: Helvetica, Times, Courier, Symbol, ZapfDingbats.
- Font registry.
- Font resource names.
- Text escaping.
- Text positioning.
- Text matrix operations.
- Font size.
- Bold and italic style mapping.
- Text color.
- Character spacing.
- Word spacing.
- Line height.
- Baseline helpers.
- Simple width measurement.
- Font metrics abstraction.
- TrueType/OpenType font loading.
- Font embedding.
- Font subsetting.
- Unicode text encoding.
- ToUnicode CMap generation.
- Fallback font chain.
- Paragraph text wrapping.
- Text alignment: left, center, right, justify.
- Rich text spans.
- Superscript and subscript.
- Footnote marker support.
- Hyphenation later.
- Right-to-left text later.

### Level 4: Images And Media

- Raw RGB image embedding.
- JPEG passthrough.
- PNG decoding.
- Grayscale image support.
- Image DPI metadata.
- Image cache and reuse.
- Image scaling helpers.
- Image fit modes.
- Image clipping.
- Alt text metadata preparation.
- SVG import later.
- Barcode and QR code adapter later.

### Level 5: Page And Document Settings

- Standard page sizes: Letter, Legal, A4, A3, A5, receipt widths.
- Portrait and landscape helpers.
- Custom page size.
- Margins.
- Bleed and trim boxes later.
- Page background.
- Header region.
- Footer region.
- Main content region.
- Page numbering.
- Total page count pass.
- First-page different header/footer.
- Odd/even header/footer.
- Sections with independent page settings.
- Document metadata.
- Language metadata.
- Viewer preferences later.

### Level 6: Layout Engine

- Box model with content, padding, border, and margin.
- Measurement pass.
- Draw pass.
- Available area model.
- Page cursor.
- Flow content across pages.
- Manual page break.
- Keep with next.
- Keep together.
- Ensure space.
- Columns.
- Rows.
- Grids.
- Stacks.
- Absolute positioning.
- Layers.
- Tables.
- Repeating table headers.
- Repeating table footers.
- Cell padding.
- Cell borders.
- Cell background.
- Column width strategies.
- Row height strategies.
- Cell spanning later.
- Lists with bullets and numbers.
- Captions.
- Figures.
- Sidebars.
- Callouts.
- Debug layout overlay.

### Level 7: Navigation, Forms, And Document Features

- Internal links.
- External links.
- Named anchors.
- Outlines/bookmarks.
- Table of contents generation.
- Index marker preparation.
- File attachments later.
- Basic annotations.
- AcroForm field creation later.
- Text fields later.
- Checkboxes later.
- Radio buttons later.
- Form flattening later.
- Page import and stamping later.
- Split and merge later.
- PDF/A validation preparation later.
- PDF/UA tagging preparation later.
- Encryption later.
- Digital signature placeholder later.

### Level 8: Report Helpers

- Report document template.
- Cover page.
- Section heading.
- Subsection heading.
- Paragraph.
- KPI card.
- Summary table.
- Detail table.
- Grouped table.
- Totals row.
- Chart placeholder.
- Figure with caption.
- Header/footer with report name and page number.
- Executive summary.
- Appendix.
- Data-source adapters later.

### Level 9: Invoice Helpers

- Invoice document template.
- Seller block.
- Buyer block.
- Shipping block.
- Invoice number/date/due date.
- Line item table.
- Quantity, unit price, discount, tax, and total columns.
- Subtotal, tax total, discount total, grand total.
- Payment terms.
- Remittance instructions.
- Notes.
- Logo.
- Tax identifier fields.
- Currency formatting.
- Locale formatting.
- Credit note variant.
- Receipt variant.
- Factur-X/ZUGFeRD metadata later.

### Level 10: Paper And Publishing Helpers

- Paper document template.
- Title page.
- Authors and affiliations.
- Abstract.
- Keywords.
- Headings.
- Figure.
- Table.
- Caption.
- Cross-reference markers.
- Footnotes.
- Endnotes.
- Bibliography entries.
- Citations.
- Table of contents.
- List of figures.
- List of tables.
- Appendices.
- Two-column layout later.
- Running heads.
- Page labels.

### Level 11: Other Templates

- Certificate template.
- Letter template.
- Contract template.
- Receipt template.
- Label sheet template.
- Packing slip template.
- Statement template.
- Worksheet template.
- Form template.
- Badge template.

### Level 12: Tooling, Testing, And Quality

- Unit tests for primitive PDF syntax.
- Unit tests for text escaping and measurement.
- Unit tests for image validation.
- Golden PDF byte tests where deterministic.
- PDF structure inspection tests.
- Rendered-page snapshot tests later.
- Sample gallery.
- Performance benchmarks.
- Memory benchmarks.
- Compatibility checks with common PDF viewers.
- Public API examples.
- Debug layout output.
- Error messages for layout overflow.
- Documentation generated from samples.

## Public API Completion Tasks

All production implementations below must use only built-in .NET APIs; test-only external libraries and tools are permitted.

- [DONE] T001: Make low-level document generation usable through the public API by exposing each page's canvas and dimensions plus document page count and indexed page access through interfaces and implementation classes.
- [DONE] T002: Expose public construction and configuration paths for fixed-size blocks, stack and row spacing, column widths, grid column counts and gaps, common insets, colors, text styles, paragraph styles, and page-layer templates that are currently available only through internal helpers.
- [DONE] T003: Complete the metadata API with setters for title, author, subject, keywords, creator, producer, creation date, modification date, and XMP metadata, and serialize those values consistently in the Info dictionary and metadata stream.
- [DONE] T004: Add public PDF version selection for the planned PDF 1.x and PDF 2.0 targets, emit the selected header and catalog version correctly, and reject features that the selected version cannot represent.
- [DONE] T005: Complete the canvas API with horizontal and vertical rules, fill and stroke opacity, transparency graphics states, blend modes, text character spacing, word spacing, horizontal scaling, leading, rise, rendering mode, and explicit text-matrix operations.
- [DONE] T006: Add a built-in TrueType/OpenType loader and public font registration API using span-based binary parsing where appropriate, including validated table directories, names, Unicode cmap selection, metrics, embedding permissions, and deterministic resource identity.
- [DONE] T007: Add built-in TrueType font embedding and glyph subsetting with composite-glyph closure, width arrays, font descriptors, CID fonts, Unicode encoding, and correct ToUnicode CMaps for all emitted glyphs.
- [DONE] T008: Add public fallback-font chains and complete Unicode text layout with grapheme-safe measurement, UAX #14 line breaking, bidirectional and right-to-left ordering, superscript, subscript, footnote markers, soft-hyphen handling, and optional caller-supplied hyphenation points.
- [DONE] T009: Add built-in JPEG loading from byte arrays, spans, streams, and files with marker validation, dimensions, component/color-space handling, DPI metadata, and unchanged DCT passthrough.
- [DONE] T010: Add built-in PNG loading from byte arrays, spans, streams, and files with chunk and CRC validation, all non-animated baseline color types and bit depths, filtering, palette and transparency handling, alpha soft masks, DPI metadata, and deterministic Flate output.
- [DONE] T011: Generalize the image API for grayscale, RGB, RGBA, and encoded sources; expose pixel dimensions and DPI; preserve image caching; and apply clipping and alternate/decorative metadata consistently in canvas and layout rendering.
- [DONE] T012: Add a native SVG import subset implemented with built-in XML APIs for paths, shapes, transforms, fills, strokes, clipping, text fallback, intrinsic sizing, and explicit rejection of unsupported or unsafe content.
- [DONE] T013: Add dependency-free QR Code and common business-barcode generation APIs that produce reusable vector or monochrome image resources with input validation and human-readable text options.
- [DONE] T014: Add page bleed, trim, crop, art, and media box configuration plus public viewer preferences, initial page mode, initial layout, and display-title controls.
- [DONE] T015: Add document sections with independent page size, orientation, margins, numbering restart, headers, footers, first/odd/even variants, running heads, and single- or multi-column content flow.
- [DONE] T016: Complete reusable styling with border color and dash settings, background and opacity, padding, margin, inter-item spacing, table and paragraph styles, document themes, and explicit point, inch, millimetre, and centimetre conversion APIs.
- [DONE] T017: Add absolute-positioned layout elements and reusable ordered content layers that participate correctly in measurement, clipping, page backgrounds, watermarks, foreground overlays, and debug rendering.
- [DONE] T018: Add reusable frame, caption, figure, sidebar, callout, and chart-placeholder layout elements with pagination, styling, accessibility roles, and keep-together behavior.
- [DONE] T019: Complete table layout with fixed, proportional, auto, minimum, and maximum column strategies; explicit and content-driven row heights; row and column spans; split-row policy; and repeated headers and footers across all span combinations.
- [DONE] T020: Add keep-with-next groups and dedicated layout-overflow exceptions with enough public context to identify the element and available area when content cannot be split or placed.
- [DONE] T021: Complete navigation and publishing structures with index markers and generated indexes, public cross-reference markers, generated lists of figures and tables, configurable page labels, and link targets tied to tagged structure elements.
- [DONE] T022: Add AcroForm radio-button groups and complete field appearance generation, validation, naming, checked-state semantics, flattening, and accessibility metadata for text fields, checkboxes, radio buttons, and signature fields.
- [DONE] T023: Add built-in page import, stamping, document split, and document merge APIs that preserve or reconcile resources, boxes, annotations, forms, outlines, destinations, attachments, metadata, and deterministic object ordering without external PDF libraries.
- [DONE] T024: Add PDF/A preparation options that emit required XMP identification, output intents, embedded fonts, color information, associated-file metadata, and conformance-compatible catalog entries while preventing encryption and other prohibited features.
- [DONE] T025: Replace accessibility preparation-only records with complete tagged-PDF output APIs for structure trees, marked-content identifiers, parent trees, reading order, artifacts, language, alternate and actual text, tables, lists, headings, links, annotations, and forms.
- [DONE] T026: Add public structure and conformance validation results that check emitted PDF syntax, cross references, resource reachability, page trees, forms, signatures, PDF/A preparation, and PDF/UA preparation without requiring a third-party production dependency.
- [DONE] T027: Complete report helpers with explicit cover pages, executive summaries, subsection headings, grouped and totals tables, KPI layouts, chart placeholders, figures with captions, appendices, and report-aware headers and footers.
- [DONE] T028: Complete invoice helpers with explicit seller, buyer, and shipping layouts; discounts and tax breakdowns; totals; payment and remittance sections; logo placement; tax identifiers; and deterministic locale-aware currency, number, and date formatting using built-in globalization APIs.
- [DONE] T029: Add credit-note and Factur-X/ZUGFeRD invoice variants with validated profile metadata, embedded EN 16931 XML supplied by the caller, correct associated-file relationships, and PDF/A-compatible output.
- [DONE] T030: Complete paper and publishing helpers with affiliations, heading levels, cross-references, footnotes, endnotes, bibliography and citation linking, list of figures, list of tables, appendices, two-column sections, running heads, and page labels.
- [DONE] T031: Add the remaining planned high-level contract, packing-slip, statement, worksheet, form, and badge templates with data models and build APIs consistent with the existing certificate, letter, receipt, and label templates.
- [DONE] T032: Add complete tests for the public PDF core and writer behavior, including every constructor and method, version selection, object allocation and ordering, streams, filters, xref and trailer correctness, metadata, file and stream saving, deterministic output, invalid inputs, and independent structural parsing.
- [DONE] T033: Add complete tests for public page and canvas behavior, including page access, every graphics and text-state operator, paths, shapes, transformations, clipping, transparency, form XObjects, graphics-state balance, numeric formatting, and invalid state transitions.
- [DONE] T034: Add complete tests for public font and text behavior, including every standard font, OpenType parsing, malformed tables, embedding permissions, subsetting closure, Unicode maps, measurement, fallback, rich text, wrapping, alignment, justification, bidirectional text, hyphenation, superscript, subscript, and footnotes.
- [DONE] T035: Add complete tests for public image and media behavior, including raw formats, JPEG, PNG, malformed and truncated inputs, alpha, grayscale, DPI, caching, fit and crop modes, clipping, alternate text, SVG import, QR codes, and barcodes.
- [DONE] T036: Add complete tests for public page-setting, styling, and layout behavior, including every size and unit, orientation, box, section, margin, theme, block, container, grid, list, table strategy and span, layer, template variant, page break, keep rule, absolute placement, pagination boundary, and overflow failure.
- [DONE] T037: Add complete tests for public navigation, annotation, form, and page-operation behavior, including destinations, links, outlines, tables of contents, indexes, cross-references, page labels, every field type and appearance, flattening, import, stamping, splitting, merging, and resource reconciliation.
- [DONE] T038: Add complete tests for public security and distribution behavior, including every permission combination, password validation, encryption dictionaries and encrypted payloads, deterministic-mode interactions, attachments and MIME metadata, signature placeholders, byte ranges, and PAdES-ready signature metadata.
- [DONE] T039: Add complete tests for public validation, PDF/A, and accessibility behavior, including every issue result method and severity, all preparation rules, positive and negative conformance fixtures, tagged structure and reading order, artifacts, alternate text, forms, links, and checks with test-only independent PDF/A and PDF/UA validators.
- [DONE] T040: Add complete tests for every high-level report, invoice, credit-note, Factur-X/ZUGFeRD, paper, certificate, letter, receipt, label, contract, packing-slip, statement, worksheet, form, and badge API, including calculations, localization, missing optional data, multipage output, navigation, accessibility, and deterministic rendered snapshots.
- [DONE] T041: Add a public-API coverage test inventory that exercises every public type, constructor, field, enum value, and interface method, fills the currently untested members and error branches, and fails when a newly exposed member lacks a behavioral test.
- [DONE] T042: Add test-only interoperability checks that open representative outputs with independent parsers, renderers, font inspectors, image decoders, and common PDF conformance tools and compare their observed page, text, image, form, metadata, security, and tagging structures with expected results.
