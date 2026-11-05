# Universal PPTX Library Development Plan

## Goal

Build a universal `.pptx` parsing, processing, and generation library in C# that uses only built-in .NET libraries in `src`. The library should read existing PowerPoint Open XML files, preserve unknown content on round-trip, expose practical editing APIs, and generate valid new `.pptx` files.

## Research Inputs

- Microsoft Open XML SDK: low-level OPC package, relationships, typed Open XML, Flat OPC, high-performance generation, modification, search/replace, splitting/combining, and chart cache updates. Source: https://github.com/dotnet/Open-XML-SDK
- Microsoft PresentationML structure: minimum valid presentation parts include presentation, presentation properties, slide master, slide layout, theme, and optional slides; slide parts can relate to notes, comments, charts, diagrams, embedded objects, hyperlinks, images, audio, video, and more. Source: https://learn.microsoft.com/en-us/office/open-xml/presentation/structure-of-a-presentationml-document
- python-pptx: create/read/update `.pptx`, round-trip unknown Open XML content, add slides/images/textboxes/tables/autoshapes/charts, placeholders, masters/layouts, notes, and document properties. Source: https://python-pptx.readthedocs.io/en/stable/
- Apache POI XSLF: create/read presentations, slide layouts, delete/reorder slides, slide size, read shapes, add/read images, format text, hyperlinks, render slides to images, and merge presentations. Source: https://poi.apache.org/components/slideshow/xslf-cookbook.html
- PPTXjs: parse/render text, text blocks, shapes, custom shapes, images, video, audio, charts, SmartArt, tables, and themes. Source: https://pptx.js.org/pages/docs.html
- Aspose.Slides: broad feature model covering presentations, slides, shapes, tables, text, animations, audio/video, OLE, comments, notes, protection, VBA, exports, cloning, and rendering. Source: https://docs.aspose.com/slides/net/product-overview/

## Implementation Constraints From syntax.md

- Use file-scoped namespace `namespace pptx;` in every source file.
- Use classes, enums, and narrow role-based interfaces; no structs, records, delegates, or attributes.
- Use fields, constructors, and ordinary block-bodied methods; avoid C# properties in `src`.
- Use only `public`, `internal`, `static`, and `const` modifiers. Public types and members define the supported API; implementation details must be `internal`.
- Add XML documentation to every public type, constructor, method, field, enum, and enum member. Documentation may use only `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<exception>`, `<example>`, `<see>`, `<seealso>`, `<para>`, and `<c>`.
- Avoid class inheritance, generic type/method declarations, generic constraints, LINQ, `var`, named arguments, default parameter values, `params`, `ref`, `out`, `in`, `try/catch/finally`, pattern matching, `yield`, `await`, and expression-bodied members. Interfaces may contain instance methods only and may not inherit other interfaces.
- Generic types from built-in .NET libraries are allowed, such as `List<T>`, `Dictionary<TKey,TValue>`, and `IReadOnlyList<T>`.
- Source implementation must use built-in .NET APIs only, primarily `System.IO`, `System.IO.Compression`, `System.Xml`, `System.Text`, `System.Buffers`, `System.Globalization`, `System.Security.Cryptography`, and `System.Collections`.
- Because `try/catch` is disallowed in source, error handling should use explicit validation, result objects, fail-fast exceptions from controlled checks, and tests that cover bad inputs.

## Design Principles

- Preserve first, understand second: every part, relationship, content type override, unknown XML element, unsupported extension, embedded binary, and `mc:AlternateContent` block must survive load-save unless the user explicitly removes it.
- Use a layered design: ZIP/OPC package, XML parts, PresentationML model, DrawingML model, high-level authoring helpers, then automation workflows.
- Keep the mutable object model concrete and use small role-based interfaces only at replaceable workflow and infrastructure boundaries.
- Do not depend on the Open XML SDK, SkiaSharp, ImageSharp, System.Drawing.Common, or any third-party package in `src`.
- Prefer deterministic output ordering where PowerPoint compatibility allows it.
- Preserve source package compatibility even when generated XML is normalized.
- Treat external relationships, embedded files, and large ZIP entries as security-sensitive.

## Feature Inventory, Low Level To High Level

### Package And OPC

- Open `.pptx` from file path, stream, and byte array.
- Create a new empty presentation package.
- Save to file path, stream, and byte array.
- Read and write ZIP entries with `System.IO.Compression.ZipArchive`.
- Parse and write `[Content_Types].xml`.
- Parse and write root and part `.rels` relationship files.
- Resolve internal part URIs, relative targets, fragments, and external targets.
- Preserve unknown parts and unknown relationships.
- Add, replace, rename, and delete package parts.
- Detect duplicate part names, bad content types, invalid relationship IDs, broken targets, and missing required parts.
- Maintain deterministic part ordering and relationship ID allocation.
- Support package inspection and diagnostics.
- Support package cloning and merge preparation.
- Support basic zip-bomb protections with configurable maximum entry count, inflated size, and compression ratio.
- Reject path traversal, absolute ZIP entry names, invalid URI normalization, and unsafe XML parser settings.

### XML Infrastructure

- Parse XML parts with namespace-aware `XmlReader` and `XmlDocument`.
- Write XML with stable namespaces and PowerPoint-compatible prefixes.
- Preserve unknown XML nodes in editable parts.
- Preserve markup compatibility attributes such as `mc:Ignorable`.
- Preserve extension lists such as `p:extLst` and `a:extLst`.
- Provide XML cursor/helper utilities for child lookup, insertion, removal, attributes, and text content.
- Provide QName and namespace constants for package relationships, PresentationML, DrawingML, chart, relationships, core properties, app properties, and custom properties.
- Provide typed parsing helpers for integers, doubles, booleans, percentages, ST_OnOff values, dates, colors, and relationship IDs.
- Provide XML diff-friendly serialization for tests.

### Validation And Diagnostics

- Validate package structure.
- Validate required content types.
- Validate root presentation relationship.
- Validate presentation part references.
- Validate slide, layout, master, notes, handout, theme, chart, media, and embedded object relationships.
- Validate well-formed XML for all XML parts.
- Validate part-specific minimum shape.
- Validate ID uniqueness for slides, shapes, comments, timing nodes, and relationships.
- Validate safe external hyperlink and media relationship metadata.
- Provide diagnostics with severity, code, message, part name, relationship ID, and XML path.
- Provide repair helpers for common recoverable issues.
- Provide compatibility warnings for features this library preserves but does not edit.

### Core Model

- Presentation object with slides, masters, layouts, themes, sections, custom shows, notes master, handout master, view properties, and document properties.
- Slide collection with add, insert, remove, reorder, clone, import, and find by ID/name.
- Slide layout collection and lookup by type/name.
- Slide master collection with theme binding and placeholder inheritance.
- Notes slide support with speaker notes text and notes shapes.
- Handout master support with preservation and basic metadata access.
- Section support with create, rename, remove, and slide assignment.
- Custom slideshow support with ordered slide references.
- Presentation-wide size, orientation, grid, guides, default text styles, and slide number settings.
- App properties, core properties, and custom document properties.
- Language, author, revision, company, category, keywords, created/modified dates, and thumbnail preservation.

### Units, Geometry, And Coordinates

- EMU, points, inches, centimeters, pixels-at-DPI, angles, percentages, and coordinate conversion helpers.
- Rectangles, points, margins, transforms, flips, rotation, and anchor bounds.
- Z-order management.
- Group transform calculations.
- Shape locks and non-visual properties.
- Connection points for connectors.
- Crop rectangles for pictures.

### Themes And Styles

- Read and write theme color schemes.
- Resolve theme colors with tint, shade, alpha, luminance, saturation, hue, complement, inverse, grayscale, and system color transforms where practical.
- Read and write font schemes.
- Read and write format schemes for fills, lines, effects, and backgrounds.
- Support slide, layout, master, and theme style inheritance.
- Expose effective formatting computation for text, shapes, tables, and charts.
- Preserve unsupported theme extensions.

### Slides, Masters, Layouts, And Placeholders

- Create blank slides and slides from layouts.
- Apply, replace, and inspect slide layouts.
- Resolve placeholder inheritance from slide to layout to master.
- Support title, body, centered title, subtitle, date, footer, slide number, picture, chart, table, media, object, content, and custom placeholders.
- Populate placeholder text, images, tables, and charts.
- Preserve and edit slide background fills.
- Support hidden slides.
- Support slide tags and user-defined metadata.
- Support slide transitions as parsed and preserved, with common transition editing.
- Support slide timing and advance settings.

### Shape Tree

- Enumerate all shapes on slides, masters, layouts, notes, and groups.
- Add, remove, clone, rename, reorder, and find shapes.
- Support auto shapes with preset geometry.
- Support freeform/custom geometry.
- Support lines and connectors.
- Support text boxes.
- Support pictures.
- Support graphic frames for tables, charts, diagrams, and OLE.
- Support group shapes, nested groups, and transform inheritance.
- Support background shapes.
- Support shape IDs and names.
- Support locks, visibility, rotation, flips, and alternative text.
- Support hyperlinks on shapes.
- Support shape-level custom data and extension preservation.

### Shape Formatting

- Solid, gradient, picture, pattern, group, and no-fill support.
- Line formatting with color, width, dash, cap, compound type, join, arrows, and transparency.
- Shadow, reflection, glow, soft edges, bevel, 3D, and effect preservation with editing for common options.
- Preset geometry adjustments.
- Shape text anchoring, margins, autofit, vertical text, wrap, and orientation.

### Text

- Extract all visible text from decks, slides, notes, comments, tables, charts, and grouped shapes.
- Text frames with body properties.
- Paragraphs with alignment, indentation, bullet, numbering, spacing, tabs, and default run properties.
- Runs with text, line breaks, fields, symbols, hyperlinks, and character formatting.
- Font family, size, bold, italic, underline, strike, baseline, capitalization, kerning, spacing, language, fill color, highlight, and hyperlink support.
- Bullet types including none, character bullet, auto-numbered bullet, and picture bullet preservation.
- Search and replace with formatting preservation.
- Text normalization utilities for indexing and extraction.
- Basic text fitting and overflow inspection without rendering.

### Images And Media

- Add and extract images.
- Preserve and identify PNG, JPEG, GIF, BMP, TIFF, SVG, EMF, WMF, and unknown image content types.
- Compute image dimensions from common headers with built-in byte parsing.
- Add picture shapes with crop, stretch, and tile support.
- Replace image binary while preserving relationships and geometry.
- Deduplicate identical media by hash.
- Support embedded audio and video relationships.
- Support linked audio and video relationships.
- Preserve media poster frames.
- Expose media inventory and external link audit APIs.

### Tables

- Add, read, edit, and remove tables.
- Rows, columns, cells, grid widths, row heights, and cell coordinates.
- Cell text frames.
- Cell fill, border, margin, merge, vertical alignment, and text direction.
- Table style IDs and first-row, last-row, first-column, last-column, banded-row, and banded-column flags.
- Merge and split cells.
- Import tabular data from arrays and enumerable rows.
- Extract tables into simple row/cell data.

### Charts

- Preserve all chart parts and chart relationships.
- Add and edit common charts: column, bar, line, pie, doughnut, area, scatter, bubble, radar, stock, surface, combo, and histogram-like generated charts when represented by supported chart XML.
- Chart titles, legends, axes, gridlines, labels, markers, series, categories, values, number formats, and data labels.
- Embedded workbook preservation.
- Generate and update chart cached data.
- Optionally generate minimal embedded workbook XML using built-in ZIP/XML APIs.
- Support chart style and color style parts.
- Extract chart data into simple tables.
- Preserve unsupported chart extensions and advanced options.

### SmartArt, Diagrams, OLE, Controls, And Embedded Packages

- Preserve SmartArt diagram parts, data, layout, colors, styles, and drawing relationships.
- Expose diagram inventory and text extraction.
- Preserve embedded OLE objects and embedded packages.
- Add generic embedded package parts.
- Replace embedded package binaries.
- Preserve ActiveX controls and control properties.
- Preserve VBA project parts and expose macro presence.
- Provide security diagnostics for macros, ActiveX, external OLE links, and embedded packages.

### Comments, Notes, Tags, And Metadata

- Read and write speaker notes.
- Read, create, edit, and delete comments where comment model is supported.
- Preserve modern/threaded comments where unsupported.
- Manage comment authors.
- Read and write slide tags and user-defined tags.
- Read and write custom XML parts with relationships.
- Support document thumbnails.

### Animations, Timing, And Transitions

- Preserve animation timing trees.
- Parse common entrance, emphasis, exit, motion path, and media animations.
- Add, remove, and reorder basic effects.
- Expose animation trigger, duration, delay, repeat, and target shape APIs.
- Preserve advanced/custom effects not directly modeled.
- Parse and edit common slide transitions.
- Manage slide advance timing and loop settings.

### Hyperlinks And Actions

- Read, create, update, and remove external hyperlinks.
- Support internal slide hyperlinks.
- Support action settings for next slide, previous slide, first slide, last slide, custom show, URL, file, and macro references as preserved data.
- Provide hyperlink inventory and safety audit.

### Processing Workflows

- Extract text for indexing.
- Extract media files.
- Replace text globally or by scope.
- Replace images globally or by relationship/shape.
- Fill templates from key/value data.
- Clone slides within a deck.
- Import slides from another deck with dependencies.
- Merge presentations.
- Split presentations.
- Remove unused parts.
- Normalize or compact packages.
- Inspect compatibility and unsupported features.
- Compare package manifests and editable model summaries.
- Generate simple reports/decks from data.

### Generation Workflows

- Create a minimum valid presentation from scratch.
- Create common built-in layouts.
- Create theme and default master/layout set.
- Add slides from predefined layouts.
- Add title/content/bullet/image/table/chart slides.
- Generate deterministic IDs, part names, relationship IDs, and content types.
- Save generated deck accepted by PowerPoint, Keynote, LibreOffice, and web viewers where possible.

### Rendering And Export Scope

- Full high-fidelity raster/PDF rendering is not a primary target because only built-in .NET libraries are allowed.
- Provide structural SVG export for common slides as an optional later feature.
- Provide HTML/text/JSON inspection exports for automation.
- Preserve rendering-only features even when the library cannot render them.

### Compatibility, Performance, And Testing

- Cross-platform .NET implementation.
- Stream-aware loading for package entries where practical.
- Memory limits for large decks.
- Deterministic output tests.
- Golden PPTX fixtures.
- Round-trip tests that compare package manifests and preserved XML.
- Compatibility smoke tests for PowerPoint-generated, LibreOffice-generated, Google Slides-exported, Keynote-exported, and library-generated `.pptx`.
- Fuzz-style malformed package tests.
- Syntax conformance tests for every source file.

## Tasks

- [DONE] T001: Complete safe document and package opening APIs for paths, streams, byte arrays, configurable ZIP limits, and appropriate `ReadOnlySpan<byte>` inputs, with explicit stream-ownership behavior and built-in .NET implementation only.
- [DONE] T002: Complete package validation diagnostics for required OPC and PresentationML parts, content types, relationships, well-formed XML, minimum part structures, identifier uniqueness, external targets, and all planned master, layout, notes, handout, theme, chart, media, and embedded-object links.
- [DONE] T003: Complete repair and cleanup operations for every planned recoverable package defect, including missing overrides, broken or orphaned relationships, unused parts, duplicate media, and deterministic relationship and part allocation.
- [DONE] T004: Add public package cloning, manifest inspection, manifest comparison, and editable-model summary comparison result types and operations without exposing internal implementation types.
- [DONE] T005: Add public presentation settings models and operations for view properties, grid spacing, guides, default text styles, slide-number settings, slide timing, orientation, and loop behavior.
- [DONE] T006: Complete core, application, custom, and presentation property operations, including all planned metadata fields and thumbnail read, replace, add, and remove support.
- [DONE] T007: Complete public master, layout, and theme collection operations, including lookup by type and name, layout application and replacement, theme binding, placeholder inheritance inspection, and slide background inheritance.
- [DONE] T008: Complete section and custom-show operations with lookup, rename, ordered slide assignment, removal, and validation of referenced slide identifiers.
- [DONE] T009: Complete notes-master, notes-slide, and handout-master APIs with shape-tree access, metadata access, creation, removal, and speaker-note editing while preserving unsupported content.
- [DONE] T010: Replace text-only placeholder population gaps with typed operations that populate title/body text, images, tables, charts, media, objects, and generic content placeholders while preserving layout and master inheritance.
- [DONE] T011: Complete geometry APIs for margins, rotation and flips, anchor bounds, group coordinate mapping, connector connection points, preset adjustment values, freeform paths, and picture crop rectangles.
- [DONE] T012: Complete shape-tree operations across slides, masters, layouts, notes, and nested groups, including group creation, graphic-frame creation, background shapes, z-order, cloning, shape-level custom data, and extension preservation.
- [DONE] T013: Expand the color model and resolver to read, write, and combine RGB, scheme, preset, system, and HSL colors with alpha, tint, shade, luminance, saturation, hue, complement, inverse, and grayscale transforms.
- [DONE] T014: Complete fill authoring for multi-stop gradients, picture fills, pattern fills, group fills, transparency, rotation, crop, stretch, and tile settings.
- [DONE] T015: Complete line and effect authoring for dash, cap, compound, join, arrows, transparency, shadow, reflection, glow, soft edges, bevel, and 3D settings while preserving unsupported effects.
- [DONE] T016: Complete effective-style resolution across theme, master, layout, slide, shape, table, chart, and text inheritance for colors, fonts, fills, lines, effects, and backgrounds.
- [DONE] T017: Add typed text-frame body operations for margins, anchoring, autofit, wrapping, overflow inspection, vertical text, and orientation instead of requiring callers to manipulate raw body-property XML.
- [DONE] T018: Complete paragraph operations for alignment, indentation, margins, spacing, tabs, default run properties, character bullets, automatic numbering, and preserved picture bullets.
- [DONE] T019: Complete run and font operations for family, size, bold, italic, underline, strike, baseline, capitalization, kerning, spacing, language, fill, highlight, symbols, fields, line breaks, and run-level hyperlinks, plus text normalization and basic fitting inspection.
- [DONE] T020: Complete image APIs with caller-supplied content types, `ReadOnlySpan<byte>` detection where appropriate, all planned format headers, linked and embedded picture relationships, replacement by shape or relationship, deduplication, and crop/stretch/tile control.
- [DONE] T021: Add embedded and linked audio/video creation, replacement, removal, poster-frame management, media inventory, and external-media audit operations.
- [DONE] T022: Complete table editing with row and column insertion/removal, cell coordinates, sizing, formatting, merge/split validation, style flags, and tabular import/export based on built-in collection interfaces.
- [DONE] T023: Expand chart data and series models to represent numeric and text categories, values, labels, formulas, number formats, scatter and bubble coordinates, stock data, and independently editable combo-chart series.
- [DONE] T024: Complete chart authoring for all planned chart types, titles, legends, axes, scales, gridlines, markers, labels, series formatting, style/color parts, cache updates, and minimal embedded workbooks using only built-in ZIP and XML APIs.
- [DONE] T025: Add generic embedded-package creation and removal plus ActiveX control and control-property inventory, preservation, replacement, and security diagnostics.
- [DONE] T026: Complete comment-author lifecycle operations and custom XML and tag mutation operations, including byte replacement, relationship inspection and editing, and part or tag removal.
- [DONE] T027: Complete animation and transition APIs for common entrance, emphasis, exit, motion-path, and media effects; click and sequence triggers; ordering; timing; repetition; slide advance; and presentation loop settings.
- [DONE] T028: Complete shape- and run-level hyperlink inventories and action editing for URLs, files, internal slides, navigation actions, custom shows, and preserved macro actions, with safety-audit results.
- [DONE] T029: Complete workflow APIs for scoped text replacement, global or relationship-based image replacement, asset extraction, template filling, slide dependency cloning/import, merging, splitting, and deterministic package normalization.
- [DONE] T030: Add package-manifest and editable-model comparison workflows and simple data-driven report/deck generation operations using only built-in .NET libraries.
- [DONE] T031: Add generation operations for a default theme/master/layout set, common built-in layouts, and title, content, bullet, image, table, and chart slides with deterministic identifiers and relationships.
- [DONE] T032: Add public compatibility inspection results that distinguish editable, readable, preserved-only, unsupported, and unsafe features without losing unknown XML or binary parts during round-trip.
- [DONE] T033: Add exhaustive tests for public document and package opening, saving, cloning, validation, repair, security scanning, cleanup, inspection, comparison, malformed inputs, ZIP limits, and unknown-content round-trips.
- [DONE] T034: Add exhaustive tests for presentation settings, properties, thumbnails, masters, layouts, themes, placeholders, sections, custom shows, notes, handouts, slide creation, slide ordering, cloning, importing, merging, and splitting.
- [DONE] T035: Add exhaustive tests for public unit, bounds, geometry, transform, shape-tree, group, connector, freeform, picture, fill, line, effect, color, theme, and effective-style operations, including invalid inputs and XML round-trips.
- [DONE] T036: Add exhaustive tests for public text-frame, paragraph, run, font, bullet, field, hyperlink, extraction, normalization, replacement, fitting, and outline operations across slides, notes, comments, tables, charts, and groups.
- [DONE] T037: Add exhaustive tests for public image and media detection, creation, linking, extraction, replacement, deduplication, crop/stretch/tile, poster-frame, external-link, and unsupported-format preservation operations.
- [DONE] T038: Add exhaustive tests for public table and chart creation, editing, formatting, data import/export, cache and workbook generation, style parts, all supported chart families, and unsupported chart preservation.
- [DONE] T039: Add exhaustive tests for public diagram, embedded package, OLE, ActiveX, VBA, comment, modern-comment, custom XML, tag, thumbnail, and security-diagnostic operations.
- [DONE] T040: Add exhaustive tests for public animation, transition, timing, hyperlink, action, and custom-show behavior, including preservation of advanced unmodeled XML.
- [DONE] T041: Add end-to-end public API tests for generation, extraction, replacement, template filling, cleanup, comparison, merge/split, JSON, text, outline, HTML, and SVG workflows followed by save, reopen, validate, and content verification.
- [DONE] T042: Add a public-consumer test assembly that constructs every public data type and operation class and exercises every public interface, enum, and static constant through supported workflows.
- [DONE] T043: Measure line and branch coverage for executable code under `src/Api` and add focused positive, boundary, failure, and round-trip tests until every reachable public-layer path is covered; external test-only libraries are allowed.
