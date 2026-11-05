# Universal OpenDocument SDK development plan

## 1. Product goal

Build a dependency-free-under-`src/` .NET SDK that can read, preserve, validate, create, generate, query, and edit OpenDocument Format documents from the ZIP/XML package layer through high-level text, spreadsheet, presentation, drawing, chart, formula, and metadata operations.

The implementation target is the union of ISO/IEC 26300 and OASIS OpenDocument:

- ISO/IEC 26300-1:2015, 26300-2:2015, and 26300-3:2015 / ODF 1.2 are the ISO interoperability baseline.
- OASIS ODF 1.3 is a required deployed compatibility baseline.
- OASIS ODF 1.4, published as an OASIS Standard on 2025-10-06, is the primary forward feature baseline.
- ODF 1.0 and 1.1 are read, preserve, validate, and migration compatibility targets.
- Strict, conforming, extended-conforming, and foreign-extension content are distinguished rather than silently normalized.
- Package documents and flat XML documents (`.fodt`, `.fods`, `.fodp`, `.fodg`) receive equal document-model support.
- All standard document kinds are covered: text, text master, spreadsheet, presentation, drawing/graphics, chart, formula, image, and their template variants.

“Universal” means complete representational coverage and safe round trips at the low level, plus progressively richer typed and high-level operations. It does not mean reproducing a desktop office suite's layout/rendering engine. Pagination, font shaping, visual rendering, printing, PDF export, and import/export of non-ODF office formats are outside this package.

## 2. Binding implementation constraints

- Follow `syntax.md` for every C# file. Existing code is not evidence that a syntax form is allowed.
- `src/` uses only the .NET base/shared framework. No NuGet or other external runtime dependencies are permitted.
- Prefer `System.IO.Compression`, `System.Xml`, `System.Text`, `System.Buffers`, `System.Security.Cryptography`, collections, and span-based parsing where appropriate.
- Public APIs contain no properties, public static methods, overloads, generic declarations, delegates, records, structs, or nullable value types.
- Every public type is in `src/Api`, is in exactly one allowed API category, and has XML documentation as part of its implementation.
- Public data classes expose fields and do not implement interfaces. Public implementation classes expose one constructor and only methods declared by their interface.
- Public factory behavior is exposed through an instance implementation of an interface, never through a public static method.
- Interfaces use unique method names. Path, stream, bytes, package, and flat-document variants therefore use distinct names such as `ReadPath`, `ReadStream`, and `ReadBytes`, not overloads.
- Public implementation classes each implement one SDK interface unless a checked method-name audit proves a multi-interface implementation has no collision.
- Static data classes contain constants or static readonly fields only and no methods.
- Custom exceptions derive directly from `System.Exception`; there is no custom exception inheritance tree.
- Internal code lives outside `src/Api`. It uses the same simplified C# syntax and remains replaceable behind public contracts.
- Input streams remain caller-owned unless an options field explicitly requests ownership transfer. Output must be deterministic when deterministic-save options are selected.
- Security limits are mandatory: maximum package entries, expanded bytes, compression ratio, XML depth, attributes, text length, formula depth, and embedded-package depth.

## 3. Research basis and downloaded source material

### Standards stored under `specs/`

- `specs/odf-1.0/`: the OASIS ODF 1.0 OS PDF and approved errata used for legacy ISO/IEC 26300:2006 compatibility; the 1.0 schema is embedded in the specification.
- `specs/odf-1.1/`: the OASIS ODF 1.1 OS PDF plus normative, strict, and manifest RELAX NG schemas.
- `specs/odf-1.2/`: the complete OASIS ODF 1.2 OS bundle, including the three ISO/IEC 26300-equivalent parts, schemas, ontologies, HTML, PDF, ODT, and original archive.
- `specs/odf-1.3/`: the complete OASIS ODF 1.3 OS bundle, including introduction, packages, schema, OpenFormula, RELAX NG schemas, ontologies, namespace description, and original archive.
- `specs/odf-1.4/`: the complete OASIS ODF 1.4 OS bundle with the same parts and normative resources.
- `specs/references/zip-appnote.txt`: PKWARE ZIP APPNOTE, the package container basis.
- `specs/references/xml-1.0.html`, `xml-namespaces-1.0.html`, `xml-id.html`, and `xml-schema-datatypes-2.html`: the XML information-set and lexical datatype foundations.
- `specs/references/relax-ng.html`: RELAX NG validation semantics.
- `specs/references/xml-signature-core-2e.html`, `xml-signature-core-1.1.html`, and `xml-encryption-core.html`: signature and encryption foundations.
- `specs/references/rfc2898-pbkdf2.txt`, `rfc3174-sha1.txt`, `rfc3986-uri.txt`, `rfc3987-iri.txt`, `rfc4880-openpgp.txt`, `rfc6838-media-types.txt`, and `rfc8018-pkcs5.txt`: normative and successor RFC material used by ODF packaging.
- `specs/references/xlink-1.0.html`, `svg-1.1-second-edition.html`, `mathml-2.0-second-edition.html`, `smil-2.0.html`, `xforms-1.0-third-edition.html`, `xsl-1.1.html`, and `css-writing-modes-4.html`: embedded vocabularies and compatible formatting/animation semantics used by the ODF schema.
- `specs/references/rdf-concepts-2004.html`, `rdf-xml-syntax-2004.html`, `rdfa-core-1.1.html`, `grddl.html`, and `dublin-core-metadata-terms.html`: semantic metadata foundations.
- `specs/references/xml-base.html`, `canonical-xml-1.0.html`, `exclusive-xml-canonicalization.html`, and `etsi-ts-101903-v1.4.1-xades.pdf`: base-IRI, canonicalization, and advanced-signature references.
- `specs/references/rfc1951-deflate.txt`, `rfc4648-base-encodings.txt`, and `rfc5646-language-tags.txt`: compression, binary text encoding, and language-tag references.

The ISO catalogue descriptions for [part 1](https://www.iso.org/standard/66363.html), [part 2](https://www.iso.org/standard/66375.html), and [part 3](https://www.iso.org/standard/66376.html) establish that ISO/IEC 26300:2015 corresponds to ODF 1.2. The freely available OASIS OS texts under `specs/odf-1.2/` provide the implementable full text. ISO is preparing ODF 1.3 replacements; those are compatibility tracking inputs, not a reason to omit OASIS 1.4.

### Existing-library findings stored under `specs/research/`

- ODFDOM/ODF Toolkit uses package, typed XML DOM, document, and high-level feature layers. It covers ZIP resources, manifest, schema-derived element/attribute types, foreign-node preservation, document classes, table/cell/range operations, presentation slides, RDFa, validation, search, encryption/signature structures, and initial collaboration operations. Its schema-generated breadth is the model for low-level completeness, while this SDK uses a qualified-name tree plus rule tables to fit `syntax.md`.
- Apache ODF Toolkit's former Simple API adds document, feature, and business layers: text documents, paragraphs, sections, lists, headers/footers, text boxes, images, spans, page breaks, contents indexes, tables/cells/ranges, metadata, presentation slides/notes, navigation, template filling, merging, and extraction. These scenarios define the convenience layer, but the deprecated API itself is not copied.
- odfpy demonstrates a compact generic element API backed by ODF grammar tables. It handles namespace-aware load/save, package parts, manifest, metadata, styles, automatic styles, master styles, settings, embedded objects, images, thumbnails, all major ODF namespace modules, and utilities such as linting, metadata changes, text extraction, and XHTML conversion. Its generic-node approach supports this plan's preservation model.
- LibreOffice's `xmloff` is a SAX/context-stack ODF importer/exporter shared across applications, with application-specific filters and explicit standard-version/extended modes. This supports separate streaming and materialized pipelines, contextual validation, broad feature preservation, and version-aware export.
- The retained local descriptions are `odfdom-overview.html`, `odf-toolkit-home.html`, `odf-toolkit-simple-api.html`, `odfpy-readme.md`, and `libreoffice-xmloff.html`.

### Design conclusions from comparison

| Concern | ODFDOM / Toolkit | odfpy | LibreOffice | Planned SDK |
|---|---|---|---|---|
| Package access | Full package API | ZIP load/save | Full filters/storage | Editable `OdfPackage` plus streaming package reader/writer |
| XML access | Generated typed DOM | Generic grammar-checked nodes | SAX context stack | Lossless generic tree, schema rule tables, typed feature views |
| High-level access | Document and deprecated Simple API | Mostly element-centric | Full application models | Focused editors for all major document families |
| Unknown content | Generally preserved | Generic nodes preserve it | Version/extension aware | Preserve by default with namespace and ordering fidelity |
| Validation | Separate validator | Grammar checks/lint | Import/export diagnostics | Structural, package, lexical, semantic, and profile validation |
| Streaming | DOM-centered | SAX load | SAX-centered | Both event streaming and materialized models |
| Collaboration | Initial operation model | Raw tracked-change nodes | Mature tracked changes | Standards model plus stable change-tracking foundations |

## 4. Architecture and complete feature catalog

### Layer 0 — primitives, names, versions, and values

- ODF version parsing and comparison for 1.0, 1.1, 1.2, 1.3, 1.4, unknown future versions, strict/extended profiles, and producer-specific extension modes.
- Complete namespace registry for `office`, `style`, `text`, `table`, `draw`, `presentation`, `chart`, `form`, `script`, `config`, `number`, `anim`, `smil`, `svg`, `dr3d`, `math`, `fo`, `xlink`, `xml`, `dc`, `meta`, `manifest`, `dsig`, `ds`, `xforms`, `xhtml`, `grddl`, RDF/RDFa, and foreign namespaces.
- Qualified names, namespace scopes, prefixes, NCName/XML ID handling, IRI and package-relative IRI resolution, CURIE/safe-CURIE values, language/country/script tags, and MIME media types.
- Lexical parsing/formatting for boolean, signed integer, decimal, float including INF/-INF/NaN where allowed, percentage, color, length, positive/non-negative length, pixel length, angle, coordinate, point/vector/view box, date, time, date-time, duration, base64, style names, cell addresses, cell ranges, and range lists.
- Unit-preserving values for millimetres, centimetres, inches, points, picas, pixels, em/ex, and percentages; conversion is explicit and does not discard the original lexical form.
- Value provenance: parsed, computed, defaulted, inherited, foreign, invalid-but-preserved, or synthesized.

### Layer 1 — ZIP packages and manifests

- Read/write `.odt`, `.ods`, `.odp`, `.odg`, `.odc`, `.odf`, `.odi`, `.odm`, all template MIME variants, and arbitrary conforming/extended packages.
- ZIP stored/deflated entries, ZIP64 consumption and production where .NET supports it, CRC checking, duplicate-name detection, UTF-8 names, directory entries, timestamps, deterministic ordering/timestamps, and non-seekable stream handling.
- ODF `mimetype` entry rules: first local entry, uncompressed, no extra field, exact media type, root-manifest consistency, inference and diagnostic recovery.
- `META-INF/manifest.xml`: root and file entries, media types, versions, sizes, preferred view modes, directory entries, embedded subdocuments, encryption data, algorithm, start-key generation, key derivation, encrypted keys, PGP data, cipher data, and foreign attributes/elements.
- Standard parts: `content.xml`, `styles.xml`, `meta.xml`, `settings.xml`, `META-INF/manifest.xml`, signatures, RDF manifests, thumbnails, images, scripts, configurations, embedded objects, subpackages, and arbitrary resources.
- Add, replace, rename, copy, move, extract, enumerate, open, and remove entries while keeping manifest references and internal IRIs consistent.
- Package path canonicalization and traversal prevention; collision handling for normalized names; bounded expansion against ZIP bombs and recursive embedded packages.
- Flat XML detection and mapping to/from the same `OdfDocument` model.

### Layer 2 — package security and integrity

- ODF password encryption metadata, checksums, start-key generation, PBKDF2 parameters, salts, initialization vectors, Blowfish CFB compatibility, AES CBC variants, and version-aware algorithm URIs.
- Public-key/OpenPGP encrypted-key structures are parsed, preserved, validated, and exposed even where a caller supplies its own decrypted key material.
- Decrypt/encrypt individual package entries and whole-document save flows; never log or retain passwords longer than required.
- XML Signature 1.0/1.1 structures, same-package reference resolution, canonicalization, digest verification, signature verification, certificate data, signature status, document/macro signature files, and signatures-versus-encryption ordering.
- Signature creation using supported .NET cryptographic algorithms and caller-supplied keys/certificates; unsupported algorithms remain preservable and produce explicit diagnostics.
- Manifest CRC/digest/checksum consistency and tamper diagnostics.

### Layer 3 — universal XML/document model

- Namespace-aware element, attribute, text, CDATA-equivalent text, comment, and processing-instruction representation with parent/child order.
- Preserve unknown ODF-version content, foreign elements/attributes, prefixes when requested, significant whitespace, mixed content, XML IDs, and lexical values.
- Safe XML reader settings: no external entity resolution, bounded depth/size, controlled DTD behavior, and clear malformed-XML diagnostics.
- Load/save the four canonical XML roots (`office:document`, `office:document-content`, `office:document-styles`, `office:document-meta`, `office:document-settings`) and flat documents.
- Root sections: scripts, font-face declarations, styles, automatic styles, master styles, metadata, settings, and body.
- Node creation, insertion, replacement, removal, reparenting, clone, traversal, qualified-name search, attribute access, text-content access, ID index, style-name index, and reference index.
- Round-trip modes: preserve, canonical deterministic, compact, and readable indentation.
- Cross-part ownership and reference integrity when nodes/styles/resources move.

### Layer 4 — styles, formatting, and layout descriptions

- Common, automatic, default, and named styles; style families; parent/next/list/master-page/data-style relationships; inheritance and cascade resolution; unused-style detection.
- Font-face declarations, font family/generic family/pitch/charset, embedded font-face URIs and binary data.
- Paragraph, text, section, table, table-column, table-row, table-cell, graphic, chart, drawing-page, presentation, Ruby, and list style families.
- Page layouts, page masters/master pages, headers, footers, header-left/footer-left, first-page variants, background, columns, footnote separators, page numbering, print orientation, margins, borders, padding, shadows, and writing modes.
- Text formatting: fonts, size, weight, style, variant, color, background, underline/overline/strike, outline, shadow, transform, position, kerning, spacing, language, hyphenation, emphasis, rotation, scale, and combine.
- Paragraph formatting: alignment, indents, margins, line height/spacing, tabs/leaders, borders, padding, background, keep/widow/orphan rules, breaks, drop caps, numbering, register-true, and writing mode.
- Number/data styles: number, scientific, fraction, percentage, currency, date, time, boolean, and text styles; embedded text, conditions, maps, locale, transliteration, calendar, decimal places, grouping, and display factors.
- Gradient, hatch, fill image, opacity, stroke dash, marker, background image, sound, and presentation page-layout resources.
- Style editing, copying across documents, dependency closure, rename with reference updates, deduplication, effective-property lookup, and automatic-style synthesis.

### Layer 5 — text documents and shared text content

- Paragraphs, headings/outline levels, spans, hyperlinks, bookmarks/ranges, reference marks/ranges, alphabetic index marks, table-of-content marks, user index marks, bibliography marks, Ruby, tabs, spaces, soft page breaks, line breaks, and text extraction.
- Sections, linked sections, columns, protected sections, text boxes, frames, chains, page/column breaks, and conditional text.
- Ordered, unordered, image, and outline lists; nested lists, continuation, start values, display levels, label alignment, bullets, number formats, and list headers.
- Fields: page/page-count, date/time, author/sender, filename/template/chapter, statistics, variables, user fields, sequences, expressions, placeholders, input fields, conditional/hidden text and paragraphs, database/mail-merge fields, references, sheet/cell fields, and metadata fields.
- Notes/footnotes/endnotes, citations, note bodies, note configuration, continuation notices, and numbering.
- Tables in text flow, captions, table headers, repeated headers, covered/merged cells, nested tables, cell text, and formulas.
- Indexes: table of contents, alphabetical, bibliography, user-defined, table, illustration, and object indexes; sources, templates, entry components, separators, hyperlinks, and generated bodies.
- Bibliography configuration and entries.
- Headers, footers, master-page content, page sequences, title pages, and master documents with linked subdocuments/indexes.
- Search by literal/regular expression/style/element, text ranges and cursors, replace, insert/delete text, split/merge paragraphs, copy/move blocks, template placeholders, and content controls where represented by the target version.

### Layer 6 — tables and spreadsheets

- Tables, named tables, table sources/scenarios, row/column groups, headers, repeated rows/columns, visibility, protection, print ranges/titles, and table templates.
- Sparse storage without forced expansion of repeated rows/columns/cells; random access resolves repeats and edits split repeated runs minimally.
- Cell types: empty, string, float/decimal, percentage, currency, date, time/duration, boolean, error, and formula with cached result; preserve exact lexical values and displayed text.
- Rows, columns, cells, covered cells, spans/merges, matrix formulas/ranges, array formulas, cell ranges, and range addresses including external/document references.
- Insert/delete/move/copy rows, columns, cells, and ranges with reference adjustment; merge/unmerge; clear contents versus formatting; transpose; fill series; find/replace.
- Named expressions/ranges, label ranges, database ranges, filters, sort keys, subtotals, data pilot/pivot tables, consolidation, scenarios, detective precedents/dependents, and tracked changes.
- Cell content validation: condition/formula, allow-empty, display-list, help and error messages, stop/warning/information behavior.
- Conditional formatting and conditional styles, sparkline and newer-version constructs when present, with foreign/future preservation.
- Spreadsheet settings: iteration, null date, precision-as-shown, case sensitivity, regular expressions/wildcards, automatic recalculation, and active/selected ranges.
- Sheet protection keys and algorithm metadata are preserved and validated; protection is not represented as cryptographic security.

### Layer 7 — OpenFormula parsing and calculation

- Namespace prefixes (`of`, legacy `oooc`/vendor formulas), leading `=`, whitespace, separators, operators, precedence, parentheses, scalar literals, string escaping, numbers, percentages, errors, identifiers, function calls, arrays, named expressions, cell/range references, intersections, reference concatenation, and external references.
- Abstract syntax tree represented without public inheritance: node-kind enum plus data fields and child lists.
- Parse and format formulas with source spans, preservation mode, canonical mode, locale-independent syntax, and structured diagnostics.
- Reference rebasing for insert/delete/move/copy, absolute/relative rows/columns, sheet names, 3D ranges, and named expressions.
- Evaluation value system: empty, number, string, boolean, date/date-time, time/duration, complex, reference, array, and every OpenFormula error.
- Scalar/array broadcasting, implicit intersection rules appropriate to the selected version/profile, type conversion, comparison, rounding, date serial/null-date handling, and deterministic recalculation.
- Dependency graph, dirty propagation, topological evaluation, circular reference detection, iterative calculation settings, volatile functions, and cached-result updates.
- Complete OpenFormula function groups and conformance levels: Small, Medium, and Large evaluators; matrix; bit operation; byte-position text; complex number; database; date/time; external access; financial; information; lookup/reference; logical; mathematical; rounding; statistical/distribution; number-representation conversion; string/text; compatibility/legacy; and version-added functions.
- External-access functions are parsed and evaluated only from resources/resolvers explicitly supplied as operation data; formula evaluation performs no implicit network or process access.
- Function metadata records argument counts/types, optional arguments without C# default parameters, return behavior, volatility, array behavior, and specification/version availability.
- Unknown/vendor functions remain round-trippable and evaluate to an explicit unsupported-function result unless a future built-in handler exists.

### Layer 8 — drawings, images, media, and embedded objects

- Drawing pages and layers; frames and anchors; z-order; groups; transforms; view boxes; clipping; glue points; events; accessibility title/description.
- Shapes: rectangle, ellipse/circle, line, polyline, polygon, regular polygon, path, connector, measure, caption, control, text box, custom shape/enhanced geometry, equation handles, and extrusion/3D scene/cube/sphere/rotate/extrude constructs.
- Geometry parsing/formatting for SVG paths, points, transforms, enhanced paths, modifiers, equations, handles, and connector routing metadata.
- Graphic properties: fill/stroke, opacity, gradients, hatches, bitmap fills, markers, shadows, wrapping, contour, run-through, positioning, alignment, crop/clip, luminance/contrast/gamma, color modes, and filters.
- Images from package entries, external IRIs, and `office:binary-data`; media type detection by declared data and safe signature sniffing; add/replace/extract/deduplicate; SVG and raster resources preserved without decoding requirement.
- Audio/video plugins, applets, floating frames, OLE/embedded objects, embedded ODF subdocuments, MathML formulas, charts, and thumbnails.
- Resource relationship updates and orphan-resource detection/removal.

### Layer 9 — presentations, animations, and charts

- Presentation pages/slides, master pages, layouts/placeholders, notes, handouts, headers/footers, page visibility, names/IDs, and presentation settings/shows.
- Create, insert, delete, reorder, duplicate, copy between documents, and merge slides with closure over styles, masters, layouts, images, media, charts, notes, and IDs.
- Transitions and SMIL/animation timing trees: sequence, parallel, iterate, animate, animate color/motion/transform, set, command, audio, timing, begin/end, duration, repeat, fill/restart, acceleration/deceleration, and target references.
- Presentation event listeners, actions, sound, show/hide shape/text, navigation orders, custom shows, and rehearsal timing metadata.
- Charts: title/subtitle/footer/legend, plot area, axes, grids, wall/floor, data tables, categories, domains, series, data points, error indicators/bars, regression curves/equations, stock/gantt/bubble/scatter/radar/surface and standard chart classes.
- Chart styles, labels, markers, interpolation, stacked/percentage/3D variants, source ranges, embedded local tables, and refresh/reference consistency.

### Layer 10 — metadata, annotations, tracking, forms, scripts, and settings

- Dublin Core and ODF metadata: title, description, subject, creator, initial creator, creation/modification/print dates, printed-by, keywords, language, editing cycles/duration, generator, template, auto-reload, hyperlink behavior, document statistics, and typed user-defined fields.
- RDFa attributes in content, RDF/XML metadata files, package metadata manifest ontology, triples, prefixes/CURIEs, subject resolution, and metadata graph preservation/query/update.
- Annotations/comments: author/creator, dates, initials, resolved state where supported, annotation ranges, replies/threads where represented, text content, positioning, and deletion.
- Tracked-change foundations: changed regions, insertion, deletion, format change, unknown change, change info, change starts/ends, change IDs, acceptance/rejection, visibility, authorship/timestamps/comments, and reference-safe edits.
- Operation log foundation for future collaboration: stable node IDs, ordered edit records, preconditions, author/session metadata, replay, inversion where defined, and serialization as extension data without claiming ODF standard conformance.
- Configuration/settings trees and typed config items/sets/maps; view settings and application settings preserved separately from portable document semantics.
- Forms and controls: forms, properties, buttons, text/textarea, checkbox/radio, list/combo, value range, image, file, date/time, frame, hidden controls, labels, control references, events, validation, binding, and XForms models/instances/binds/submissions.
- Database front-end document structures: data sources, connections, queries, tables, forms, reports, filter/order statements, login and driver settings, without implementing a database engine.
- Scripts and event listeners are represented, preserved, inspected, added/removed, and security-classified; scripts are never executed by the SDK.

### Layer 11 — validation, compatibility, and diagnostics

- Package conformance: ZIP structure, `mimetype`, manifest completeness/consistency, media types, signatures/encryption structures, entry paths, required parts, and embedded packages.
- XML well-formedness, namespace correctness, IDs/references, root/part selection, element/attribute/content constraints derived from each bundled RELAX NG schema, and datatypes.
- ODF semantic rules not expressible in RELAX NG: style references, cross-part references, ranges, merges, formula syntax, value/value-type agreement, master/layout references, resource existence, chart sources, change ranges, and version gates.
- Validation modes: strict fail-fast, complete diagnostics, compatibility/recovery, package-only, XML-only, semantic-only, and profile-specific.
- Diagnostic severity, stable code, message, part path, line/column when available, node path/ID, related location, ODF version, and specification section.
- Migration analysis and controlled conversion among ODF 1.0–1.4: additions, removals, renamed/deprecated constructs, downgrade loss diagnostics, and extension fallback.
- Producer compatibility profiles for strict OASIS output, broadly interoperable LibreOffice/Apache/OpenOffice output, preservation of Microsoft/LibreOffice extensions, and future-version passthrough.
- Optional repair operations are explicit and report every mutation; validation itself never silently edits.

### Layer 12 — streaming, high-level creation, and editing

- Pull-style streaming events for package entries and XML nodes, subtree skipping, selected-part reading, selected-element materialization, and text/table-row projection.
- Forward-only writers for package entries, XML parts, large text bodies, and large sparse spreadsheets without building a complete tree.
- Streaming copy-through: transform selected parts/elements while copying untouched package entries byte-for-byte where possible.
- Backpressure is naturally synchronous because `await` is unavailable under `syntax.md`; cancellation uses explicit state checked between units of work.
- Create each document kind/template from a minimal conforming skeleton; clone from templates; assemble parts; set metadata/styles; add resources; save package or flat XML.
- Transaction-like edit sessions stage changes in memory, validate before commit, return a mutation report, and can discard staged changes without relying on exceptions for routine conflicts.
- High-level editors for text, tables, spreadsheets, slides, drawings, styles, metadata, annotations, and tracked changes use typed data while retaining access to underlying qualified-name nodes.
- Query API spans parts, nodes, text, styles, IDs, ranges, formulas, resources, annotations, and changes without LINQ in `src/`.

## 5. Proposed public API and source files

All entries below are one public type per file. Method signatures are deliberately deferred; names indicate responsibility, and every interface will use unique non-overloaded method names.

### `src/Api/Data` — field-based data model

| Planned file | Public data class and role |
|---|---|
| `OdfQualifiedName.cs`, `OdfNamespaceDeclaration.cs`, `OdfAttribute.cs`, `OdfNode.cs` | Universal namespace-aware XML tree and lexical preservation |
| `OdfPackage.cs`, `OdfPackageEntry.cs`, `OdfManifest.cs`, `OdfManifestEntry.cs`, `OdfPart.cs` | Package/manifest/part models |
| `OdfEncryptionInfo.cs`, `OdfEncryptedKey.cs`, `OdfSignature.cs`, `OdfSignatureReference.cs` | Security metadata and verification results |
| `OdfDocument.cs`, `OdfVersionInfo.cs`, `OdfLoadOptions.cs`, `OdfSaveOptions.cs`, `OdfValidationOptions.cs`, `OdfSecurityLimits.cs` | Document aggregate and operation options |
| `OdfDiagnostic.cs`, `OdfLocation.cs`, `OdfEditResult.cs`, `OdfSearchMatch.cs` | Diagnostics, locations, edit reports, and query results |
| `OdfStreamEvent.cs`, `OdfStreamSelection.cs`, `OdfCancellationState.cs` | Synchronous streaming protocol data |
| `OdfStyle.cs`, `OdfStyleProperty.cs`, `OdfFontFace.cs`, `OdfPageLayout.cs`, `OdfMasterPage.cs`, `OdfNumberStyle.cs` | Styles and layout descriptions |
| `OdfTextRange.cs`, `OdfTextBlock.cs`, `OdfTextRun.cs`, `OdfSection.cs`, `OdfList.cs`, `OdfListItem.cs`, `OdfTextField.cs`, `OdfNote.cs`, `OdfIndex.cs`, `OdfBibliographyEntry.cs` | Typed text concepts |
| `OdfTable.cs`, `OdfRow.cs`, `OdfColumn.cs`, `OdfCell.cs`, `OdfCellRange.cs`, `OdfCellValue.cs` | Shared table and cell concepts |
| `OdfNamedExpression.cs`, `OdfDatabaseRange.cs`, `OdfFilterRule.cs`, `OdfSortRule.cs`, `OdfValidationRule.cs`, `OdfPivotTable.cs` | Spreadsheet data features |
| `OdfFormula.cs`, `OdfFormulaToken.cs`, `OdfFormulaNode.cs`, `OdfFormulaResult.cs`, `OdfFunctionInfo.cs`, `OdfDependency.cs` | OpenFormula syntax/evaluation data |
| `OdfDrawingObject.cs`, `OdfGeometry.cs`, `OdfTransform.cs`, `OdfImage.cs`, `OdfEmbeddedObject.cs`, `OdfMedia.cs` | Drawing and resource concepts |
| `OdfSlide.cs`, `OdfSlideTransition.cs`, `OdfAnimation.cs`, `OdfPresentationShow.cs` | Presentation and animation concepts |
| `OdfChart.cs`, `OdfChartAxis.cs`, `OdfChartSeries.cs`, `OdfChartDataPoint.cs` | Chart concepts |
| `OdfMetadata.cs`, `OdfUserMetadata.cs`, `OdfDocumentStatistics.cs`, `OdfRdfTriple.cs` | Metadata and RDF concepts |
| `OdfAnnotation.cs`, `OdfChange.cs`, `OdfOperation.cs` | Comments, standard tracked changes, and collaboration foundation |
| `OdfFormControl.cs`, `OdfFormProperty.cs`, `OdfDatabaseObject.cs`, `OdfSetting.cs`, `OdfScript.cs` | Forms, database, settings, and inert script descriptions |

Every data class above is consumed by at least one contract or another data class. Data classes use public fields, may use a single public constructor, and may contain only internal helper methods.

### `src/Api/Contracts` — operations

| Planned file | Interface responsibility |
|---|---|
| `IOdfPackageReader.cs`, `IOdfPackageWriter.cs`, `IOdfManifestService.cs` | Read/write/edit package and manifest data |
| `IOdfXmlReader.cs`, `IOdfXmlWriter.cs` | Read/write universal XML trees and standard parts |
| `IOdfDocumentReader.cs`, `IOdfDocumentWriter.cs`, `IOdfDocumentFactory.cs` | Load/save/create package and flat documents |
| `IOdfStreamingReader.cs`, `IOdfStreamingWriter.cs` | Forward-only event and projection pipelines |
| `IOdfValidator.cs`, `IOdfRepairService.cs`, `IOdfVersionService.cs` | Validation, explicit repair, migration, and compatibility analysis |
| `IOdfSecurityService.cs` | Entry encryption/decryption and signature verification/creation |
| `IOdfFormulaParser.cs`, `IOdfFormulaEvaluator.cs` | OpenFormula parse/format/rebase and calculation |
| `IOdfDocumentEditor.cs`, `IOdfTextEditor.cs`, `IOdfTableEditor.cs`, `IOdfSpreadsheetEditor.cs` | General, text, table, and spreadsheet edits |
| `IOdfPresentationEditor.cs`, `IOdfDrawingEditor.cs`, `IOdfChartEditor.cs` | Slides, drawings/resources, and charts |
| `IOdfStyleEditor.cs`, `IOdfMetadataEditor.cs`, `IOdfAnnotationEditor.cs`, `IOdfChangeTracker.cs` | Styles, metadata/RDF, comments, and changes |
| `IOdfFormEditor.cs`, `IOdfSettingsEditor.cs` | Forms and settings/configuration |
| `IOdfQueryService.cs` | Cross-layer query/search/navigation |

### `src/Api/Classes` — public implementations

Each class implements the same-stem interface and has one constructor: `OdfPackageReader.cs`, `OdfPackageWriter.cs`, `OdfManifestService.cs`, `OdfXmlReader.cs`, `OdfXmlWriter.cs`, `OdfDocumentReader.cs`, `OdfDocumentWriter.cs`, `OdfDocumentFactory.cs`, `OdfStreamingReader.cs`, `OdfStreamingWriter.cs`, `OdfValidator.cs`, `OdfRepairService.cs`, `OdfVersionService.cs`, `OdfSecurityService.cs`, `OdfFormulaParser.cs`, `OdfFormulaEvaluator.cs`, `OdfDocumentEditor.cs`, `OdfTextEditor.cs`, `OdfTableEditor.cs`, `OdfSpreadsheetEditor.cs`, `OdfPresentationEditor.cs`, `OdfDrawingEditor.cs`, `OdfChartEditor.cs`, `OdfStyleEditor.cs`, `OdfMetadataEditor.cs`, `OdfAnnotationEditor.cs`, `OdfChangeTracker.cs`, `OdfFormEditor.cs`, `OdfSettingsEditor.cs`, and `OdfQueryService.cs`.

### `src/Api/Static` — fields only

| Planned file | Constants/static readonly data |
|---|---|
| `OdfNamespaces.cs` | Standard namespace IRIs and preferred prefixes |
| `OdfMediaTypes.cs` | Document/template/part media types |
| `OdfPartNames.cs` | Standard package paths and root part names |
| `OdfAlgorithmIdentifiers.cs` | Encryption, digest, derivation, canonicalization, and signature identifiers |
| `OdfFormulaNames.cs` | Formula namespace names, errors, and standardized function names |
| `OdfDiagnosticCodes.cs` | Stable public diagnostic-code strings |

### `src/Api/Enums`

`OdfVersion.cs`, `OdfConformanceClass.cs`, `OdfDocumentKind.cs`, `OdfPackageKind.cs`, `OdfCompressionMethod.cs`, `OdfNodeKind.cs`, `OdfStreamEventKind.cs`, `OdfValueKind.cs`, `OdfValueProvenance.cs`, `OdfUnitKind.cs`, `OdfStyleFamily.cs`, `OdfFormulaConformance.cs`, `OdfFormulaTokenKind.cs`, `OdfFormulaNodeKind.cs`, `OdfFormulaError.cs`, `OdfDrawingKind.cs`, `OdfChartKind.cs`, `OdfAnimationKind.cs`, `OdfTransitionKind.cs`, `OdfValidationSeverity.cs`, `OdfValidationMode.cs`, `OdfEncryptionMethod.cs`, `OdfSignatureStatus.cs`, `OdfChangeKind.cs`, `OdfAnnotationState.cs`, `OdfTextFieldKind.cs`, `OdfFormControlKind.cs`, and `OdfEditStatus.cs`.

Every enum is consumed by a public data field or contract method. Members use explicit or default Int32 constant values and receive XML documentation when implemented.

### `src/Api/Exceptions`

`OdfPackageException.cs`, `OdfXmlException.cs`, `OdfDocumentException.cs`, `OdfValidationException.cs`, `OdfFormulaException.cs`, `OdfEditException.cs`, and `OdfSecurityException.cs`. Each derives directly from `System.Exception` and has one constructor.

### Planned internal source files outside `src/Api`

| Directory | Planned files |
|---|---|
| `src/Core/Primitives` | `OdfLexicalParser.cs`, `OdfLexicalFormatter.cs`, `OdfNameRules.cs`, `OdfIriResolver.cs`, `OdfCellAddressParser.cs`, `OdfUnitConverter.cs`, `OdfVersionRules.cs` |
| `src/Core/Package` | `ZipPackageReader.cs`, `ZipPackageWriter.cs`, `ZipEntryIndex.cs`, `PackagePathRules.cs`, `MimetypeRules.cs`, `ManifestParser.cs`, `ManifestWriter.cs`, `ManifestSynchronizer.cs`, `EmbeddedPackageWalker.cs` |
| `src/Core/Security` | `PackageEncryption.cs`, `BlowfishCipher.cs`, `Pbkdf2Derivation.cs`, `PackageChecksum.cs`, `XmlCanonicalizer.cs`, `XmlSignatureReader.cs`, `XmlSignatureVerifier.cs`, `XmlSignatureWriter.cs`, `SecurityBuffer.cs` |
| `src/Core/Xml` | `SecureXmlSettings.cs`, `OdfTreeBuilder.cs`, `OdfTreeWriter.cs`, `NamespaceScope.cs`, `NodeIndex.cs`, `ReferenceIndex.cs`, `PartRootRules.cs`, `ForeignContentPreserver.cs`, `XmlEventReader.cs`, `XmlEventWriter.cs` |
| `src/Core/Schema` | `SchemaRule.cs`, `SchemaPattern.cs`, `SchemaDatatype.cs`, `SchemaRuleIndex.cs`, `Odf12Rules.cs`, `Odf13Rules.cs`, `Odf14Rules.cs`; rule tables are derived during implementation and checked in as ordinary simplified C# |
| `src/Core/Styles` | `StyleIndex.cs`, `StyleResolver.cs`, `StyleCascade.cs`, `StyleDependencyGraph.cs`, `StyleCopier.cs`, `AutomaticStyleBuilder.cs`, `NumberStyleFormatter.cs` |
| `src/Core/Text` | `TextNavigator.cs`, `TextExtractor.cs`, `TextMutator.cs`, `ListModel.cs`, `FieldModel.cs`, `IndexModel.cs`, `NoteModel.cs`, `TextReferenceUpdater.cs` |
| `src/Core/Table` | `SparseTableIndex.cs`, `RepeatedRunEditor.cs`, `CellValueCodec.cs`, `RangeMutator.cs`, `MergeMap.cs`, `TableReferenceUpdater.cs`, `SpreadsheetFeatureModel.cs` |
| `src/Core/Formula` | `FormulaLexer.cs`, `FormulaGrammarParser.cs`, `FormulaFormatter.cs`, `FormulaReferenceRebaser.cs`, `FormulaValueConverter.cs`, `FormulaDependencyGraph.cs`, `FormulaCalculationEngine.cs`, `FormulaFunctionRegistry.cs`, plus `FunctionsMatrix.cs`, `FunctionsBit.cs`, `FunctionsByteText.cs`, `FunctionsComplex.cs`, `FunctionsDatabase.cs`, `FunctionsDateTime.cs`, `FunctionsExternal.cs`, `FunctionsFinancial.cs`, `FunctionsInformation.cs`, `FunctionsLookup.cs`, `FunctionsLogical.cs`, `FunctionsMath.cs`, `FunctionsRounding.cs`, `FunctionsStatistical.cs`, `FunctionsNumberRepresentation.cs`, `FunctionsText.cs`, and `FunctionsCompatibility.cs` |
| `src/Core/Drawing` | `DrawingModel.cs`, `SvgPathParser.cs`, `TransformParser.cs`, `EnhancedGeometryParser.cs`, `DrawingReferenceUpdater.cs`, `ImageResourceManager.cs`, `EmbeddedObjectManager.cs` |
| `src/Core/Presentation` | `SlideModel.cs`, `SlideDependencyClosure.cs`, `SlideCopier.cs`, `AnimationModel.cs`, `PresentationShowModel.cs` |
| `src/Core/Chart` | `ChartModel.cs`, `ChartRangeResolver.cs`, `ChartReferenceUpdater.cs` |
| `src/Core/Metadata` | `MetadataModel.cs`, `RdfaReader.cs`, `RdfXmlReader.cs`, `RdfXmlWriter.cs`, `AnnotationModel.cs`, `TrackedChangeModel.cs`, `OperationLog.cs` |
| `src/Core/Forms` | `FormModel.cs`, `XFormsModel.cs`, `DatabaseDocumentModel.cs`, `SettingsModel.cs`, `ScriptModel.cs` |
| `src/Core/Validation` | `ValidationContext.cs`, `PackageValidator.cs`, `XmlSchemaValidator.cs`, `SemanticValidator.cs`, `FormulaValidator.cs`, `VersionValidator.cs`, `RepairEngine.cs`, `DiagnosticCollector.cs` |
| `src/Core/Streaming` | `PackageEventPipeline.cs`, `XmlEventPipeline.cs`, `TextProjection.cs`, `TableRowProjection.cs`, `StreamingCopyTransformer.cs` |
| `src/Core/Editing` | `DocumentMutation.cs`, `EditSession.cs`, `EditPrecondition.cs`, `ReferenceUpdater.cs`, `ResourceGarbageCollector.cs`, `DocumentQueryEngine.cs` |

## 6. Implementation tasks

Tasks are ordered by dependency. A task includes its production tests when those tests are tightly local; dedicated public-API coverage and interoperability tasks are listed in the final phases.

### Phase A — public foundation and primitive values

- [DONE] T001: Establish the internal layer directories and namespaces, preserving the required `src/Api` category boundaries and using only built-in .NET references.
- [DONE] T002: Implement all planned public enums with stable Int32 values and consume each enum from a public data class or contract.
- [DONE] T003: Implement `OdfNamespaces`, `OdfMediaTypes`, `OdfPartNames`, `OdfAlgorithmIdentifiers`, `OdfFormulaNames`, and `OdfDiagnosticCodes` as fields-only static data classes.
- [DONE] T004: Implement `OdfQualifiedName`, `OdfNamespaceDeclaration`, `OdfAttribute`, and XML name/NCName/XML-ID validation.
- [DONE] T005: Implement version data and parsing for ODF 1.0–1.4, unknown future versions, conformance classes, and extended producer profiles.
- [DONE] T006: Implement IRI/URI, package-relative reference, CURIE/safe-CURIE, language, country, script, and media-type lexical handling.
- [DONE] T007: Implement invariant boolean, integer, decimal, float, percentage, base64, and color lexical parsing/formatting with invalid-but-preserved results.
- [DONE] T008: Implement length, pixel length, angle, coordinate, point, vector, view-box, unit preservation, and explicit unit conversion.
- [DONE] T009: Implement XML Schema date, time, date-time, and duration lexical handling without nullable value-type APIs.
- [DONE] T010: Implement style-name, cell-address, cell-range, range-list, quoted-sheet, absolute/relative, external-reference parsing and formatting.
- [DONE] T011: Implement all public options, limits, location, diagnostic, edit-result, search-match, stream-selection, and cancellation-state data classes with safe defaults.
- [DONE] T012: Implement each direct `System.Exception` public exception with a single policy-compliant constructor.

### Phase B — package and manifest layer

- [DONE] T013: Implement ZIP package inspection over path, stream, and byte inputs with duplicate-entry, UTF-8-name, CRC, ZIP64, and unsupported-compression diagnostics.
- [DONE] T014: Enforce package security limits for entry count, total expanded size, single-entry size, compression ratio, path length, and nested package depth.
- [DONE] T015: Implement canonical package path handling, relative IRI resolution, normalization-collision detection, and traversal rejection.
- [DONE] T016: Implement `mimetype` detection and validation, including first-entry/stored/no-extra-field rules, MIME inference, and root-manifest consistency.
- [DONE] T017: Implement manifest XML parsing for every ODF 1.2–1.4 manifest element/attribute and lossless foreign content.
- [DONE] T018: Implement manifest writing with deterministic namespace declarations, ordering options, and exact lexical-value preservation mode.
- [DONE] T019: Implement package entry enumeration/open/extract and editable add/replace/rename/move/copy/remove operations.
- [DONE] T020: Implement manifest synchronization for all package mutations, directories, resources, standard parts, and embedded subdocuments.
- [DONE] T021: Implement standard-part discovery for content, styles, metadata, settings, signatures, RDF, thumbnails, images, scripts, configurations, and arbitrary resources.
- [DONE] T022: Implement embedded ODF package/subdocument discovery and bounded recursive traversal.
- [DONE] T023: Implement ZIP writing with stored/deflated selection, required mimetype placement, deterministic mode, CRC generation, and non-seekable output handling supported by .NET.
- [DONE] T024: Implement copy-through saving of untouched entries and explicit ownership rules for all opened streams.
- [DONE] T025: Implement `IOdfPackageReader`, `IOdfPackageWriter`, and `IOdfManifestService` plus their public implementation classes.

### Phase C — package encryption, signatures, and integrity

- [DONE] T026: Implement manifest encryption-data, algorithm, start-key-generation, key-derivation, encrypted-key, PGP-data, cipher-data, salt, IV, checksum, and size models.
- [DONE] T027: Implement secure temporary key/password byte handling, clearing, and operation-scoped secret lifetime using built-in .NET facilities.
- [DONE] T028: Implement PBKDF2/HMAC start-key and per-entry key derivation for ODF version-specific parameters with RFC test vectors.
- [DONE] T029: Implement ODF Blowfish CFB compatibility locally and verify it against independent published vectors because .NET has no built-in Blowfish primitive.
- [DONE] T030: Implement supported AES-CBC entry encryption/decryption, padding, IV, digest/checksum, and compressed-before-encrypted ordering.
- [DONE] T031: Parse and preserve OpenPGP encrypted-key metadata and expose caller-supplied key-material hooks without executing an external OpenPGP implementation.
- [DONE] T032: Implement XML canonicalization modes required by ODF signature verification using simplified syntax and bounded input.
- [DONE] T033: Parse XML Signature 1.0/1.1, XAdES signed/unsigned properties, X509 data, references, transforms, digest/signature methods, and ODF signature container files.
- [DONE] T034: Verify same-package reference digests, signatures, certificate material, and encryption/signature ordering with explicit unsupported-algorithm statuses.
- [DONE] T035: Create ODF signature files with supported built-in .NET key algorithms and caller-provided key/certificate material.
- [DONE] T036: Implement `IOdfSecurityService` and `OdfSecurityService` for entry and document encryption/decryption, verification, signing, and integrity diagnostics.

### Phase D — universal XML and document parts

- [DONE] T037: Implement the `OdfNode` universal tree for elements, attributes, text, comments, and processing instructions with ordered mixed content.
- [DONE] T038: Implement secure XML reader configuration with external resources disabled and limits for characters, depth, attributes, and entity behavior.
- [DONE] T039: Implement namespace-scope tracking, preferred-prefix selection, prefix-preserving mode, reserved-prefix rules, and qualified-name lookup.
- [DONE] T040: Implement materialized XML tree loading with line/column locations, lexical values, significant whitespace, XML IDs, and foreign content.
- [DONE] T041: Implement XML writing in preserve, canonical deterministic, compact, and readable modes without losing unknown content.
- [DONE] T042: Implement ID, qualified-name, style-name, and cross-reference indexes with invalidation after mutations.
- [DONE] T043: Implement node create/insert/replace/remove/reparent/clone/traverse/search and text-content operations.
- [DONE] T044: Implement standard root recognition and section mapping for document, content, styles, metadata, settings, and flat XML roots.
- [DONE] T045: Implement separate-part and flat-document assembly/disassembly while retaining the same `OdfDocument` aggregate.
- [DONE] T046: Implement `OdfPart`, `OdfDocument`, load/save option handling, standard body-kind detection, and future-version passthrough.
- [DONE] T047: Implement `IOdfXmlReader`, `IOdfXmlWriter`, `IOdfDocumentReader`, and `IOdfDocumentWriter` plus their implementation classes.
- [DONE] T048: Implement `IOdfDocumentFactory` and `OdfDocumentFactory` to create minimal conforming package and flat skeletons for every document/template kind.

### Phase E — schema rules, version handling, and validation core

- [DONE] T049: Translate the bundled ODF 1.0 embedded grammar and ODF 1.1/1.2 RELAX NG grammars into checked-in internal element, attribute, child-content, occurrence, and datatype rule tables.
- [DONE] T050: Translate the bundled ODF 1.3 RELAX NG, manifest, and signature grammars and record deltas from 1.2.
- [DONE] T051: Translate the bundled ODF 1.4 RELAX NG, manifest, and signature grammars and record deltas from 1.3.
- [DONE] T052: Implement a bounded schema-rule validator covering choices, groups, interleaves, repetitions, mixed content, foreign-content allowances, and datatype checks.
- [DONE] T053: Implement package validation for ZIP/mimetype/manifest/required parts/media types/entry paths/subpackages/signature and encryption structures.
- [DONE] T054: Implement XML root/part/namespace/ID/schema validation with complete diagnostic collection and fail-fast mode.
- [DONE] T055: Implement semantic reference validation for styles, masters, resources, ranges, merges, values, formulas, charts, changes, and embedded objects.
- [DONE] T056: Implement stable diagnostics with severity, code, part, line/column, node location, related location, version, and specification section.
- [DONE] T057: Implement strict, complete, recovery, package-only, XML-only, semantic-only, and profile validation modes.
- [DONE] T058: Implement version-gate checks, future-version preservation, ODF 1.0–1.4 migration analysis, and downgrade loss diagnostics.
- [DONE] T059: Implement compatibility profiles for strict OASIS, interoperable ODF, LibreOffice/OpenOffice extended, Microsoft extensions, and preserve-existing output.
- [DONE] T060: Implement explicit repair proposals and application for safe package, manifest, namespace, ID, and broken-reference repairs with mutation reports.
- [DONE] T061: Implement `IOdfValidator`, `IOdfRepairService`, and `IOdfVersionService` plus their public implementation classes.

### Phase F — styles and layout descriptions

- [DONE] T062: Implement parsing/modeling/writing for named, default, common, and automatic styles across every ODF style family.
- [DONE] T063: Implement style parent/next/list/master/data-style relationships, cycle detection, inheritance, defaults, and effective-property resolution.
- [DONE] T064: Implement font-face declarations and embedded/external font-face resource references.
- [DONE] T065: Implement text and paragraph property access for the complete ODF property sets and lexical value preservation.
- [DONE] T066: Implement section, table, column, row, cell, graphic, chart, drawing-page, presentation, Ruby, and list property access.
- [DONE] T067: Implement page-layout and master-page access, headers/footers, variants, columns, footnote separators, page numbering, and page properties.
- [DONE] T068: Implement number, scientific, fraction, percentage, currency, date, time, boolean, and text data-style models and format interpretation.
- [DONE] T069: Implement list/outline styles, bullets, numbering, images, level rules, label alignment, and continuation.
- [DONE] T070: Implement gradient, hatch, fill-image, opacity, stroke-dash, marker, background-image, sound, and presentation-layout resources.
- [DONE] T071: Implement automatic-style synthesis, style rename/reference update, dependency closure, cross-document copy, deduplication, and unused-style detection.
- [DONE] T072: Implement `IOdfStyleEditor` and `OdfStyleEditor` for style lookup, effective properties, creation, modification, copy, rename, and cleanup.

### Phase G — text and master documents

- [DONE] T073: Implement paragraph, heading, span, hyperlink, whitespace control, soft-break, and plain/structured text extraction models.
- [DONE] T074: Implement bookmarks, reference marks, index marks, ranges, Ruby, XML IDs, and reference-safe navigation.
- [DONE] T075: Implement sections, linked/protected sections, columns, frames, text boxes, frame chains, anchors, and breaks.
- [DONE] T076: Implement ordered/unordered/image/outline lists, nested lists, continuation, start values, headers, and label configuration.
- [DONE] T077: Implement all standard text field families, variable/user/sequence declarations, conditional/hidden content, placeholders, and database fields.
- [DONE] T078: Implement footnotes/endnotes, citations, bodies, configuration, continuation notices, and numbering.
- [DONE] T079: Implement all index families, index sources, entry templates/components, generated bodies, and bibliography configuration/entries.
- [DONE] T080: Implement header/footer/master-page text flow, page sequences, title-page patterns, and master-document linked subdocuments.
- [DONE] T081: Implement text cursors/ranges, insert/delete, split/merge paragraphs, copy/move blocks, and style application with tracked reference updates.
- [DONE] T082: Implement literal/regular-expression/style/element text search and replacement without LINQ in production code.
- [DONE] T083: Implement template placeholder/user-field filling while preserving formatting and optionally retaining field structures.
- [DONE] T084: Implement `IOdfTextEditor` and `OdfTextEditor` for all typed text creation, access, and modification scenarios.

### Phase H — shared tables and spreadsheets

- [DONE] T085: Implement sparse table indexing for repeated rows, columns, cells, covered cells, headers, groups, and random access without eager expansion.
- [DONE] T086: Implement repeat-run splitting/coalescing so single-cell/row/column edits preserve compact ODF representation.
- [DONE] T087: Implement typed cell values and cached formula results for empty, string, numeric, percentage, currency, date, duration, boolean, and error values.
- [DONE] T088: Implement table, row, column, cell, range, style/default-style, visibility, protection, print range/title, and template data views.
- [DONE] T089: Implement insert/delete/move/copy for cells, rows, columns, and ranges with absolute/relative reference updates.
- [DONE] T090: Implement covered cells, merge/unmerge, matrix spans, matrix/array formulas, nested tables, and structural validation.
- [DONE] T091: Implement clear-content versus clear-format, transpose, fill/copy series, and range search/replace operations.
- [DONE] T092: Implement named ranges/expressions, label ranges, database ranges, sort, filters, subtotals, consolidation, and scenarios.
- [DONE] T093: Implement content validation rules, help/error messages, display-list behavior, and conditional styles/formatting.
- [DONE] T094: Implement data pilot/pivot tables, source ranges, fields, levels, members, subtotals, filters, and output ranges.
- [DONE] T095: Implement detective precedents/dependents, operation cells, tracked spreadsheet changes, and range-reference integrity.
- [DONE] T096: Implement spreadsheet calculation settings, null date, iteration, precision, matching behavior, selected ranges, and active table.
- [DONE] T097: Implement sheet/table source, scenario, protection-key metadata, and print/display settings with version awareness.
- [DONE] T098: Implement `IOdfTableEditor` and `OdfTableEditor` for tables shared by text, spreadsheet, presentation, and chart content.
- [DONE] T099: Implement `IOdfSpreadsheetEditor` and `OdfSpreadsheetEditor` for sheet and spreadsheet-specific creation/modification.

### Phase I — OpenFormula

- [DONE] T100: Implement the span-based formula lexer for all ODF 1.2–1.4 tokens, literals, names, separators, operators, references, errors, and array syntax.
- [DONE] T101: Implement formula parsing with precedence, source locations, structured diagnostics, namespace prefixes, vendor functions, and bounded nesting.
- [DONE] T102: Implement formula AST data, preservation formatting, canonical formatting, string escaping, and round-trip stability.
- [DONE] T103: Implement absolute/relative cell, range, sheet, 3D, external, named, intersection, and reference-concatenation semantics.
- [DONE] T104: Implement formula reference rebasing for copy/move and row/column/sheet insertion, deletion, rename, and reorder.
- [DONE] T105: Implement the full formula value/error system, scalar/array/reference behavior, conversions, comparisons, and rounding.
- [DONE] T106: Implement date/time/duration serial behavior, null-date settings, leap-year rules, time zones where specified, and locale-independent calculations.
- [DONE] T107: Implement dependency extraction, dependency graph, dirty propagation, topological recalculation, circular references, and iterative settings.
- [DONE] T108: Implement the complete database, information, and external-access function groups with ODF conformance/version metadata and explicit caller-supplied resource resolution.
- [DONE] T109: Implement the complete date/time and logical function groups with ODF conformance/version metadata.
- [DONE] T110: Implement the complete financial function group and required day-count/basis behaviors.
- [DONE] T111: Implement the complete lookup/reference, matrix, and array function groups.
- [DONE] T112: Implement the complete mathematical, trigonometric, bit-operation, conversion, and complex-number function groups.
- [DONE] T113: Implement the complete statistical, probability-distribution, regression, and ranking function groups.
- [DONE] T114: Implement the complete string/text function group with Unicode-aware behavior required by OpenFormula.
- [DONE] T115: Implement compatibility, legacy, volatile, and version-added function groups and explicit unknown-function behavior.
- [DONE] T116: Implement calculation scheduling, cached-value updates, deterministic volatile inputs supplied through data fields, and error propagation.
- [DONE] T117: Implement `IOdfFormulaParser`, `OdfFormulaParser`, `IOdfFormulaEvaluator`, and `OdfFormulaEvaluator` for parse/format/rebase/evaluate/recalculate operations.

### Phase J — drawings, resources, and embedded content

- [DONE] T118: Implement drawing page, layer, frame, anchor, z-order, group, event, transform, accessibility, and reference models.
- [DONE] T119: Implement rectangle, ellipse, line, polyline, polygon, path, connector, measure, caption, control, and text-box shapes.
- [DONE] T120: Implement custom/enhanced geometry, modifiers, equations, handles, glue points, connector routing, and shape presets.
- [DONE] T121: Implement SVG path, points, transforms, view boxes, clipping/contours, and preserve-mode lexical formatting.
- [DONE] T122: Implement 3D scenes, lights, cube, sphere, rotate, extrude, projection, camera, material, and backface metadata.
- [DONE] T123: Implement complete graphic property access for fill/stroke/markers/shadow/wrap/position/crop/color adjustment/filter behavior.
- [DONE] T124: Implement image load/add/replace/extract/deduplicate for package, external, and base64 sources with safe media sniffing.
- [DONE] T125: Implement audio/video/plugin/applet/floating-frame resource descriptions without executing active content.
- [DONE] T126: Implement embedded ODF subdocuments, OLE objects, MathML parts, charts, object replacements, and thumbnail handling.
- [DONE] T127: Implement drawing/resource reference updates, cross-document dependency copy, orphan detection, and explicit garbage collection.
- [DONE] T128: Implement `IOdfDrawingEditor` and `OdfDrawingEditor` for drawings, images, media, geometry, and embedded objects.

### Phase K — presentations, animations, and charts

- [DONE] T129: Implement slides/pages, master/layout/placeholder assignment, notes, handouts, headers/footers, visibility, names, IDs, and navigation order.
- [DONE] T130: Implement slide create/insert/delete/reorder/duplicate and same-document copy with reference integrity.
- [DONE] T131: Implement cross-document slide copy and presentation merge with transitive style/master/layout/resource/chart/note dependency closure.
- [DONE] T132: Implement transitions, presentation events/actions/sounds, custom shows, show settings, and rehearsal timing.
- [DONE] T133: Implement SMIL sequence/parallel/iterate timing trees and all ODF animate/set/command/audio/motion/transform constructs.
- [DONE] T134: Implement animation target/reference resolution, timing parsing, repeat/fill/restart, acceleration/deceleration, and version validation.
- [DONE] T135: Implement `IOdfPresentationEditor` and `OdfPresentationEditor` for slide, notes, layout, show, transition, and animation operations.
- [DONE] T136: Implement chart title/subtitle/footer/legend, plot area, axes, grids, wall/floor, data tables, and categories.
- [DONE] T137: Implement all standard chart classes and variants, series, domains, data points, labels, markers, interpolation, stacking, percentage, and 3D metadata.
- [DONE] T138: Implement error indicators/bars, regression curves/equations, stock components, source ranges, and embedded chart data tables.
- [DONE] T139: Implement chart-style, source-range, and formula-reference updates after spreadsheet and cross-document edits.
- [DONE] T140: Implement `IOdfChartEditor` and `OdfChartEditor` for chart creation, data binding, modification, and validation.

### Phase L — metadata, RDF, annotations, changes, forms, and settings

- [DONE] T141: Implement all standard Dublin Core/ODF metadata fields, typed user-defined metadata, generator/template/auto-reload/hyperlink behavior, and statistics.
- [DONE] T142: Implement RDFa extraction/update for content nodes, CURIE/prefix handling, subjects, properties, datatypes, and resources.
- [DONE] T143: Implement RDF/XML metadata-part and package-metadata-manifest reading/writing with triple preservation and query.
- [DONE] T144: Implement `IOdfMetadataEditor` and `OdfMetadataEditor` for simple metadata, user metadata, statistics, RDF triples, and package metadata.
- [DONE] T145: Implement annotation point/range content, creator/date/initials/resolution, thread/reply extensions, positioning, and reference-safe deletion.
- [DONE] T146: Implement `IOdfAnnotationEditor` and `OdfAnnotationEditor` for annotation create/find/update/resolve/remove operations.
- [DONE] T147: Implement standard tracked changed regions, insertion, deletion, format change, change info, unknown changes, and boundary references.
- [DONE] T148: Implement accept/reject one/all/selected changes, visible-text projection, author/date/comment filtering, and spreadsheet change structures.
- [DONE] T149: Implement stable-node-ID operation records, edit preconditions, ordered replay, supported inversion, and extension serialization foundations.
- [DONE] T150: Implement `IOdfChangeTracker` and `OdfChangeTracker` for recording, enumerating, accepting, rejecting, replaying, and exporting changes.
- [DONE] T151: Implement forms, every standard control kind, properties, labels, bindings, event descriptions, validation, and control references.
- [DONE] T152: Implement XForms models, instances, binds, submissions, schema references, and preservation without network submission or script execution.
- [DONE] T153: Implement database front-end document source/connection/query/table/form/report and driver/login setting models without database execution.
- [DONE] T154: Implement settings/config item, set, map, and map-entry trees with typed lexical values and portable-versus-view separation.
- [DONE] T155: Implement script and event-listener inspection/add/remove plus active-content security classification; never execute scripts.
- [DONE] T156: Implement `IOdfFormEditor`, `OdfFormEditor`, `IOdfSettingsEditor`, and `OdfSettingsEditor`.

### Phase M — streaming, querying, and edit sessions

- [DONE] T157: Implement forward-only package-entry events with selection, entry skipping, size limits, and caller-owned stream semantics.
- [DONE] T158: Implement forward-only XML events with namespace scopes, attributes, text, locations, subtree skip, and cancellation checks.
- [DONE] T159: Implement selected-subtree materialization and selected-part loading into `OdfNode` without loading unrelated package content.
- [DONE] T160: Implement streaming plain-text projection for text documents, headers/footers, notes, comments, and selected text containers.
- [DONE] T161: Implement streaming sparse-spreadsheet row/cell projection with repeat counts and typed values.
- [DONE] T162: Implement forward-only XML/package writers for large text and spreadsheet generation with manifest finalization.
- [DONE] T163: Implement streaming copy-transform for selected XML nodes/parts while preserving untouched entries and foreign content.
- [DONE] T164: Implement `IOdfStreamingReader`, `OdfStreamingReader`, `IOdfStreamingWriter`, and `OdfStreamingWriter`.
- [DONE] T165: Implement cross-part query indexes for names, IDs, qualified names, text, styles, ranges, formulas, resources, annotations, and changes.
- [DONE] T166: Implement `IOdfQueryService` and `OdfQueryService` with literal, regular-expression, node, field, and reference queries.
- [DONE] T167: Implement edit sessions with staging, preconditions, validation-before-commit, discard, mutation reports, and frequent-conflict status returns.
- [DONE] T168: Implement document-level part/resource add/remove/copy, body-kind checks, flat/package conversion, and explicit repair integration.
- [DONE] T169: Implement resource garbage collection, reference updater orchestration, ID collision handling, and deterministic name generation.
- [DONE] T170: Implement `IOdfDocumentEditor` and `OdfDocumentEditor` for document-wide edit-session and resource operations.

### Phase N — complete public API coverage and conformance tests

- [DONE] T171: Create public API construction/default tests covering every data class field, option default, enum member, static data field, and direct exception type.
- [DONE] T172: Create lexical round-trip and invalid-input tests for every primitive exposed through `OdfQualifiedName`, values, versions, units, dates, durations, IRIs, and cell/range addresses.
- [DONE] T173: Create `IOdfPackageReader`, `IOdfPackageWriter`, and `IOdfManifestService` contract tests covering every public method on paths, streams, bytes, malformed packages, mutation, and deterministic save.
- [DONE] T174: Create package security-limit tests for ZIP bombs, entry/path collisions, traversal, duplicates, CRC failures, deep packages, malformed manifests, and ownership behavior.
- [DONE] T175: Create encryption/decryption tests from published PBKDF2, Blowfish, AES, and ODF fixture vectors and cover every `IOdfSecurityService` method and status.
- [DONE] T176: Create XML signature parse/verify/create tests with valid, altered, unsupported, encrypted, multi-reference, certificate, XAdES, and canonicalization fixtures.
- [DONE] T177: Create `IOdfXmlReader` and `IOdfXmlWriter` contract tests for every public method, node kind, namespace case, mixed content, whitespace, foreign content, and output mode.
- [DONE] T178: Create `IOdfDocumentReader`, `IOdfDocumentWriter`, and `IOdfDocumentFactory` contract tests for every document/template kind in package and flat form.
- [DONE] T179: Create round-trip preservation tests for unknown namespaces, future ODF versions, vendor extensions, arbitrary entries, lexical values, IDs, and prefix-preserve mode.
- [DONE] T180: Create schema-rule tests generated as test data from every element, attribute, datatype, and occurrence rule in the bundled 1.0, 1.1, 1.2, 1.3, and 1.4 schemas.
- [DONE] T181: Create `IOdfValidator`, `IOdfRepairService`, and `IOdfVersionService` contract tests covering every mode, severity, diagnostic field, repair, migration, downgrade loss, and compatibility profile.
- [DONE] T182: Cross-check validation results against the ODF Toolkit Validator on shared valid/invalid fixture corpora, with external tools confined to tests.
- [DONE] T183: Create `IOdfStyleEditor` contract tests covering every style family, inheritance/default/cycle case, page/master layout, data style, automatic style, copy, rename, and cleanup method.
- [DONE] T184: Create `IOdfTextEditor` contract tests covering every public method and all paragraphs, headings, spans, links, lists, sections, fields, notes, indexes, bibliography, header/footer, master-document, cursor, search, and template cases.
- [DONE] T185: Create `IOdfTableEditor` contract tests covering every public method, sparse repeats, values, nested tables, merges, matrix spans, row/column/cell/range edits, formats, and references.
- [DONE] T186: Create `IOdfSpreadsheetEditor` contract tests covering every public method, named/database ranges, validation, conditional formats, pivot tables, filters, sorts, subtotals, scenarios, detective data, and settings.
- [DONE] T187: Create formula lexer/parser/formatter/rebase tests covering every token, grammar production, precedence, reference form, error, array, namespace, vendor, source location, and public parser method.
- [DONE] T188: Create OpenFormula evaluator tests for every standardized function and conformance level using specification examples and independent expected-value datasets allowed under `tests/`.
- [DONE] T189: Create formula dependency/recalculation tests for every public evaluator method, type conversion, array behavior, error propagation, cycles, iteration, volatility, and cached result.
- [DONE] T190: Create `IOdfDrawingEditor` contract tests covering every public method, shape, geometry/path/transform, layer/frame/anchor, 3D description, graphic property, image/media, and embedded object case.
- [DONE] T191: Create `IOdfPresentationEditor` contract tests covering every public method, slide/master/layout/note/handout/show operation, dependency-copy case, transition, event, and animation timing construct.
- [DONE] T192: Create `IOdfChartEditor` contract tests covering every public method and chart class, axis, series, category, domain, data point, label, regression, error bar, source range, and embedded table case.
- [DONE] T193: Create `IOdfMetadataEditor` contract tests covering every metadata field/type, statistics, RDFa form, RDF/XML graph, CURIE, package ontology, preservation, and query method.
- [DONE] T194: Create `IOdfAnnotationEditor` and `IOdfChangeTracker` contract tests covering every public method, point/range/thread comment, all change kinds, accept/reject, projection, replay, inversion, and broken-boundary diagnostic.
- [DONE] T195: Create `IOdfFormEditor` and `IOdfSettingsEditor` contract tests covering every control, property, binding, XForms construct, database structure, config type, setting, script description, and non-execution guarantee.
- [DONE] T196: Create `IOdfStreamingReader` and `IOdfStreamingWriter` contract tests covering every public method, event, selection, skip, cancellation, projection, non-seekable stream, partial materialization, and forward-only write path.
- [DONE] T197: Create `IOdfQueryService` and `IOdfDocumentEditor` contract tests covering every query, staged edit, precondition, commit/discard, resource/reference update, garbage collection, and flat/package conversion method.
- [DONE] T198: Create end-to-end creation/modification/round-trip tests for text, master text, spreadsheet, presentation, drawing, chart, formula, image, and every template MIME type.
- [DONE] T199: Create interoperability fixture tests that open and resave representative ODF 1.0–1.4 documents from LibreOffice, Apache ODF Toolkit/ODFDOM, odfpy, and Microsoft Office without losing supported or foreign content.
- [DONE] T200: Create a functional public-surface coverage matrix test that fails when any `src/Api` public constructor, method, field, enum member, static field, or documented exception behavior lacks a behavioral test.
- [DONE] T201: Create malformed/fuzz-corpus regression tests for package, XML, manifest, formula, range, geometry, RDF, and change inputs using fixed checked-in cases and bounded execution.
- [DONE] T202: Create deterministic-output golden tests for package ordering, timestamps, namespaces, XML serialization, automatic names, formulas, styles, and flat/package conversions.

## 7. Completion criteria

- Every planned public file exists in the required `src/Api` category and satisfies the simplified C# policy.
- `src/` has no external package/library dependency; test-only dependencies and independent validators remain under `tests/` tooling.
- Every feature in sections 4 and 6 is implemented or is explicitly represented/preserved with a diagnostic for unsupported execution semantics.
- ODF 1.2, 1.3, and 1.4 package/schema/formula conformance suites pass at their selected profiles; ODF 1.0/1.1 fixtures read, preserve, validate, and migrate as designed.
- Unknown namespaces, vendor extensions, unsupported cryptographic metadata, active content, and future-version constructs survive preservation round trips.
- The complete `src/Api` functional surface is covered by tests, including failures, diagnostics, stream ownership, limits, and all document kinds.
- No completion criterion depends on performance benchmarks, syntax-policy tests, documentation tasks, release work, rendering, or non-ODF format conversion.
