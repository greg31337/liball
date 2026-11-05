# Universal Markdown Library Development Plan

## Scope

Build a dependency-free .NET Markdown library that can parse, inspect, transform, and generate Markdown-family documents. The core target is full CommonMark compatibility, with opt-in support for widely used dialect features such as GitHub Flavored Markdown (GFM), footnotes, definition lists, front matter, attributes, typographic substitutions, and safe HTML rendering modes.

The implementation must use only built-in .NET APIs and must follow the simplified C# syntax rules in `syntax.md`: classes, enums, and method-only interfaces; `public`, `internal`, `static`, and `const` modifiers only; no properties, records, structs, project-declared generics, LINQ, or source attributes; and no class inheritance. Classes may implement interfaces. Public symbols define the supported API and require XML documentation using only the elements allowed by `syntax.md`; implementation symbols must be `internal`. Use the same `namespace markdown;` declaration in every source file.

## Research Notes

- CommonMark 0.31.2 defines the baseline grammar: preprocessing, tabs, block parsing, inline parsing, block precedence, leaf blocks, container blocks, links, images, emphasis, code spans, raw HTML, line breaks, and a two-phase parsing strategy.
- GFM is a strict CommonMark superset and adds tables, task list items, strikethrough, bare URL autolinks, and disallowed raw HTML filtering.
- markdown-it emphasizes configurable syntax rules, presets, plugin-style rule replacement, safe defaults, inline rendering, token streams, linkify, typographer behavior, and syntax highlighting hooks.
- Markdig emphasizes a pipeline builder, immutable reusable pipeline, ordered block and inline parsers, extension registration, AST source locations, trivia tracking for roundtrip rendering, nesting limits, document callbacks, HTML options, and many built-in extensions.
- cmark emphasizes a small C API for parsing to an AST, manipulating the AST, and rendering to HTML, CommonMark, XML, LaTeX, and man pages, with safe defaults for raw HTML and dangerous links.
- goldmark emphasizes CommonMark compliance, standard-library-only implementation, extensibility through block parsers, inline parsers, paragraph transformers, AST transformers, renderers, source positions, fuzz testing, and built-in tables, strikethrough, task lists, definition lists, footnotes, typographer, and CJK behavior.
- pulldown-cmark emphasizes pull parsing as a low-allocation event stream, optional AST construction, source maps, event transformations, text event merging, and optional footnotes, GFM tables, task lists, strikethrough, and highlight syntax.

Sources reviewed:

- https://spec.commonmark.org/0.31.2/
- https://github.github.com/gfm/
- https://markdown-it.github.io/markdown-it/
- https://github.com/markdown-it/markdown-it
- https://xoofx.github.io/markdig/
- https://xoofx.github.io/markdig/docs/advanced/pipeline/
- https://github.com/commonmark/cmark
- https://github.com/yuin/goldmark
- https://github.com/pulldown-cmark/pulldown-cmark

## High-Level Architecture

1. Input layer: normalize source text into lines, offsets, tab-aware indentation, and replacement handling for invalid input characters.
2. Block parser: perform CommonMark phase 1 line-by-line parsing into a block tree, collecting link reference definitions and preserving source spans.
3. Inline parser: perform CommonMark phase 2 parsing inside inline-capable block nodes using delimiter stacks, bracket stacks, link reference state, and extension hooks.
4. AST model: represent document nodes, blocks, inlines, attributes, literals, spans, trivia, and diagnostics in ordinary classes and enums.
5. Event stream: expose a pull-style stream of start, end, text, code, break, and raw events for low-allocation processing.
6. Processing layer: provide visitors, tree walking, node search, transforms, normalization, slug generation, reference resolution, link rewriting, and document analysis.
7. Rendering layer: render AST or events to HTML, normalized Markdown, plain text, debug tree text, and a simple XML-like AST dump.
8. Generation layer: create Markdown from code through a writer API and optional AST builder helpers.
9. Extension layer: enable dialect features through ordered parser, transform, renderer, and visitor registries whose behavior is defined by small method-only interfaces, without project-declared generic abstractions.
10. Safety layer: default to escaped HTML, safe URI filtering, bounded nesting, bounded parser work, and deterministic behavior on malformed input.
11. Validation layer: run CommonMark and GFM example fixtures, local unit tests, roundtrip tests, fuzz-like random tests, and performance regression checks.

## Complete Feature List

### Low-Level Input Features

- Unicode text input through `string`, `TextReader`, and `ReadOnlyMemory<char>` friendly call paths where possible.
- Line ending handling for LF, CRLF, CR, and final unterminated lines.
- CommonMark tab behavior with tab stops of four columns for block indentation.
- Source position tracking with absolute offsets, line numbers, columns, and source spans.
- Replacement of null characters with U+FFFD.
- Configurable maximum nesting depth.
- Configurable maximum input size.
- Configurable maximum reference definitions.
- Configurable maximum generated output size.
- Optional trivia capture for whitespace, marker choice, delimiter text, fence text, heading closers, and original line endings.
- Diagnostic collection for limits, unsupported extension syntax, unsafe content suppression, and recovery decisions.
- Allocation-aware scanners for characters, lines, indentation, punctuation classes, whitespace classes, and delimiter runs.
- Entity and numeric character reference decoding using built-in .NET APIs and internal tables where required.
- HTML escaping helpers.
- URI classification helpers for safe, unsafe, mailto, absolute, relative, fragment, and data URI policies.

### CommonMark Block Features

- Document root.
- Blank lines.
- Paragraphs.
- Thematic breaks.
- ATX headings.
- Setext headings.
- Indented code blocks.
- Fenced code blocks with backtick fences.
- Fenced code blocks with tilde fences.
- Fenced code block info string capture.
- HTML blocks for all seven CommonMark HTML block types.
- Link reference definitions.
- Block quotes.
- Ordered list items with `.` markers.
- Ordered list items with `)` markers.
- Ordered list start numbers.
- Bullet list items with `-`, `+`, and `*` markers.
- Nested lists.
- Tight list detection.
- Loose list detection.
- Empty list items.
- Lazy continuation lines.
- Container continuation matching.
- Block precedence exactly as CommonMark specifies.
- Correct interruption rules for paragraphs, headings, thematic breaks, lists, HTML blocks, and code fences.

### CommonMark Inline Features

- Text nodes.
- Soft line breaks.
- Hard line breaks from backslash.
- Hard line breaks from two trailing spaces.
- Code spans with arbitrary backtick run lengths.
- Backslash escapes.
- Entity references.
- Numeric character references.
- Emphasis with `*`.
- Emphasis with `_`.
- Strong emphasis with `**`.
- Strong emphasis with `__`.
- Nested emphasis and strong emphasis.
- Intraword underscore rules.
- Delimiter stack algorithm for emphasis.
- Inline links.
- Full reference links.
- Collapsed reference links.
- Shortcut reference links.
- Empty link text and empty destinations.
- Link titles using double quotes.
- Link titles using single quotes.
- Link titles using parentheses.
- Link destination escaping and unescaping.
- Link reference normalization.
- Images.
- Autolinks for absolute URI syntax.
- Autolinks for email syntax.
- Raw HTML inline tags.
- Raw HTML comments.
- Raw HTML processing instructions.
- Raw HTML declarations.
- Raw HTML CDATA sections.
- Link nesting prohibition.
- Correct precedence between code spans, HTML tags, brackets, links, and emphasis.

### GFM Features

- Pipe tables.
- Table header parsing.
- Table delimiter row parsing.
- Left, center, right, and default column alignment.
- Escaped pipe handling inside table cells.
- Inline parsing inside table cells.
- Strikethrough with `~~`.
- Task list items with checked and unchecked states.
- Bare URL autolinks.
- Bare email autolinks.
- `www.` autolinks.
- GFM disallowed raw HTML filtering.
- GFM feature preset that enables tables, task lists, strikethrough, autolinks, and disallowed raw HTML filtering together.

### Widely Used Extension Features

- Footnote definitions.
- Footnote references.
- Footnote backreferences in HTML output.
- Definition lists.
- Heading IDs.
- Auto heading ID generation.
- Explicit heading attributes.
- Generic block attributes where practical.
- Generic inline attributes where practical.
- YAML-style front matter capture as data, not parsed YAML.
- TOML-style front matter capture as data.
- JSON-style front matter capture as data.
- Abbreviations.
- Superscript.
- Subscript.
- Inserted text.
- Marked or highlighted text using `==`.
- Math spans.
- Math blocks.
- Diagram blocks as fenced code metadata, rendered through caller hooks only.
- Emoji shortcode preservation and optional mapping table.
- Typographer replacements for quotes, apostrophes, ellipses, dashes, and copyright-style symbols.
- CJK soft-break behavior option.
- Admonition or alert blocks.
- Description/details blocks.
- Table of contents generation.
- Link reference collection and reporting.
- Link definition rewriting.
- Image reference collection and reporting.
- Include directive support as an optional preprocessor, disabled by default.
- Custom container blocks with caller-registered marker names.
- Custom fenced block handling by info string.
- Custom inline delimiters with caller-registered rule names.

### AST and Processing Features

- Complete typed node model for all core and extension nodes.
- Parent and child navigation.
- Sibling navigation helpers.
- Ordered child insertion and removal.
- Node cloning.
- Document cloning.
- Source spans on every node where available.
- Optional trivia spans for roundtrip editing.
- Node attributes for IDs, classes, key/value pairs, and renderer hints.
- Diagnostics attached to document and nodes.
- Tree walking in preorder and postorder.
- Visitor-style processing through `IMarkdownNodeVisitor`, with a callback-based class for convenience.
- Event stream generation from AST.
- AST construction from event stream.
- Markdown normalization.
- Markdown formatting with configurable line endings and wrapping.
- Plain text extraction.
- Heading outline extraction.
- Link and image inventory extraction.
- Front matter extraction.
- Word count and reading-time analysis.
- Slug generation with collision handling.
- Transform pipeline for document-level and node-level rewrites.
- Safe link rewriting hooks.
- HTML sanitization-adjacent filtering for raw HTML and unsafe links without promising full sanitization.
- Roundtrip preservation mode when trivia is enabled.
- Partial document parsing for editor scenarios.
- Incremental parsing plan for future editor integration.

### Rendering and Generating Features

- HTML renderer.
- XHTML-compatible HTML option.
- Soft break rendering options.
- Hard wrap option.
- Raw HTML passthrough option.
- Raw HTML suppression option.
- Unsafe link suppression option.
- Attribute rendering.
- Footnote rendering.
- Table rendering.
- Task list rendering.
- Heading ID rendering.
- Caller-provided code block rendering hook.
- Caller-provided link rendering hook.
- Caller-provided image rendering hook.
- Caller-provided raw HTML handling hook.
- Normalized Markdown renderer.
- CommonMark renderer.
- GFM renderer.
- Plain text renderer.
- Debug tree renderer.
- XML-like AST renderer.
- Markdown writer for programmatic generation.
- Markdown builder convenience class.
- Escaping rules for generated Markdown text.
- Stable output formatting options.
- Deterministic rendering independent of current culture.

### API and Product Features

- Static convenience methods for parse, render, normalize, and plain text conversion.
- Reusable immutable pipeline created from a mutable builder.
- CommonMark preset.
- GFM preset.
- Advanced preset.
- Zero preset for caller-built syntax.
- Feature flags for every built-in extension.
- Ordered parser rule registration.
- Ordered renderer rule registration.
- Ordered transform registration.
- Event stream API for low-allocation processing.
- AST API for rich transformations.
- Diagnostics API.
- Source map API.
- Test fixture runner for CommonMark and GFM examples.
- Benchmark command or test category.
- Fuzz-like robustness test category.
- Documentation examples for basic use, pipeline use, AST transforms, event transforms, and Markdown generation.

## Public API Completion Tasks

All production implementations below must use only built-in .NET APIs; span and memory types should be used where they make scanning or streaming more efficient without making the public contract unsafe.

- [DONE] T001: Add public typed AST data contracts for every `MarkdownNodeKind`, including the feature-specific metadata needed for headings, lists, code fences, links and images, tables, task items, footnotes, front matter, math, diagrams, admonitions, containers, and custom nodes.
- [DONE] T002: Expose invariant-preserving AST operations through public interfaces and implementation classes for child append, insert, replace, and removal; parent and sibling navigation; attribute access; and node-local diagnostics.
- [DONE] T003: Expose public node and document cloning, kind-based search, preorder and postorder walking, and a callback visitor adapter that drives `IMarkdownNodeVisitor` and honors every `MarkdownTreeWalkResult` value.
- [DONE] T004: Add public parsing entry points for `TextReader` and `ReadOnlyMemory<char>` inputs, with span-based scanning where appropriate, while preserving the same normalization, source positions, diagnostics, limits, pipelines, and transforms as string parsing.
- [DONE] T005: Add public partial-document and incremental-reparse operations for editor scenarios, including changed-range inputs, stable unaffected nodes where practical, corrected source maps, and deterministic fallback to full parsing.
- [DONE] T006: Complete `IMarkdownPipelineBuilder` so callers can configure every feature flag, safety and formatting option, resource limit, soft-break mode, trivia mode, emoji mapping, custom container, custom fenced block, and custom inline delimiter exposed by the planned presets.
- [DONE] T007: Add public ordered registry operations for block rules, inline rules, transforms, and renderer rules, including insert-before, insert-after, replace, remove, clear, and detached registration inspection without exposing internal collection types.
- [DONE] T008: Add a disabled-by-default include preprocessor contract and caller-supplied include resolver, with relative-name context, cycle and depth handling, source attribution, diagnostics, and no production dependency beyond built-in .NET APIs.
- [DONE] T009: Add the planned description/details block feature and generic block and inline attribute attachment, including feature flags, node kinds and typed data, parsing, preset registration, and rendering behavior.
- [DONE] T010: Complete the public HTML rendering layer with document and event streaming to `TextWriter`, pipeline-aware source and document methods, XHTML and break modes, attributes, all extension nodes, raw HTML and unsafe URI policies, and caller rules for code, links, images, raw HTML, and arbitrary nodes.
- [DONE] T011: Complete the public Markdown rendering layer with pipeline-aware source and document methods, `TextWriter` streaming, CommonMark and GFM output modes, normalized and trivia-preserving modes, configurable line endings and wrapping, stable escaping, and deterministic culture-independent output.
- [DONE] T012: Expose pipeline-aware public plain-text, debug-tree, and XML-like AST rendering for documents and streams, with output-size enforcement and consistent source-span and attribute representation.
- [DONE] T013: Expose programmatic Markdown generation through public writer and builder interfaces for text, headings, paragraphs, lists, quotes, code, links, images, tables, extension constructs, raw Markdown, escaping, and stable formatting, plus conversion of generated events or nodes into a document.
- [DONE] T014: Add low-allocation public pull event readers over source and AST plus an event writer that validates event nesting and builds ASTs or feeds renderers, without requiring callers to materialize a `MarkdownEvent[]`.
- [DONE] T015: Complete public document analysis with heading outlines, collision-safe slugs, table-of-contents generation, link and image inventories, link-reference definitions, front matter, plain text, word and character counts, and configurable reading-time results.
- [DONE] T016: Expose public processing operations for normalization, node and document transforms, reference resolution, link-definition and destination rewriting, image-reference rewriting, and transform ordering while retaining diagnostics and source maps.
- [DONE] T017: Complete the public diagnostics, source-map, and trivia contracts with safe add and lookup operations, per-node diagnostic attachment, precise span queries, original line-ending and delimiter trivia, and source-preserving round trips after supported edits.
- [DONE] T018: Add public URI policy and rewrite hooks for links and images, explicit raw-HTML filtering decisions, and exception-less classification helpers for expected unsafe-content outcomes while retaining bounded deterministic rendering.
- [DONE] T019: Complete the instance-based `IMarkdown` and `IMarkdownDataServices` façade so every public parsing, processing, event, rendering, generation, analysis, cloning, traversal, and source-map capability is reachable through interfaces without exposing internal implementation types.
- [DONE] T020: Add consumer-facing contract tests that instantiate and exercise every public type, constructor, field, enum value, interface method, and façade operation from a separate test assembly, including null, invalid-state, and detached-copy behavior.
- [DONE] T021: Replace the CommonMark subset fixture coverage with a runner over every example in `specs/core/commonmark-0.31.2-examples.json`, asserting public parse and HTML results and reporting example and section identifiers on failure.
- [DONE] T022: Add a fixture extractor and conformance runner for every example in `specs/core/gfm-0.29-spec.html`, covering the five GFM extensions through the public GFM pipeline while retaining CommonMark 0.31.2 baseline behavior.
- [DONE] T023: Add public-API input tests for strings, readers, memory, all line endings, tabs, null replacement, Unicode punctuation and line breaking, entities, source positions, final unterminated lines, maximum sizes, and reader failures.
- [DONE] T024: Add a public CommonMark feature matrix that verifies the typed AST, diagnostics, source maps, HTML, normalized Markdown, plain text, debug tree, XML-like output, and round trips for every planned core block and inline construct and their precedence interactions.
- [DONE] T025: Add a public extension feature matrix covering every GFM and advanced feature flag alone and in representative combinations, including parse trees, attributes, diagnostics, HTML, Markdown, plain text, custom mappings, and disabled-feature fallback text.
- [DONE] T026: Add exhaustive public AST tests for typed metadata, navigation, insertion, replacement, removal, attributes, node diagnostics, cloning, searching, visitor traversal, malformed caller-built trees, and source/trivia preservation.
- [DONE] T027: Add public pipeline and extensibility tests for all presets, individual options and limits, immutable pipeline copies, ordered add/insert/replace/remove/clear behavior, custom parser triggers, transforms, renderer rules, and concurrent pipeline reuse.
- [DONE] T028: Add public renderer tests across every node kind and output mode, including streaming and string equivalence, escaping, XHTML, soft and hard breaks, wrapping, line endings, deterministic cultures, output limits, custom rendering rules, unsafe URI handling, and raw HTML filtering.
- [DONE] T029: Add public generation tests for every writer and builder operation, Markdown escaping at ambiguous boundaries, nested constructs, extension constructs, stable formatting, generated-document equivalence, and parse-generate-parse round trips.
- [DONE] T030: Add public event tests for source and AST pull readers, depth and child indexes, attributes and trivia, malformed event rejection, event-to-AST reconstruction, event-to-renderer output, event transforms, early termination, and large streamed inputs.
- [DONE] T031: Add public processing and analysis tests for transforms, normalization, outlines, collision-safe slugs, table of contents, references, front matter, link and image rewrites, plain-text extraction, counts, reading time, and preservation of diagnostics and source maps.
- [DONE] T032: Add public include, partial-parse, and incremental-reparse tests for nested includes, cycles, missing content, source attribution, changed ranges, boundary-sensitive Markdown constructs, unaffected content, and full-parse equivalence.
- [DONE] T033: Add public safety and robustness tests for unsafe HTML, GFM disallowed tags, dangerous and data URI variants, malformed Unicode and Markdown, nesting and reference limits, pathological delimiter and bracket runs, deterministic recovery, and fuzz-like generated inputs.
- [DONE] T034: Add boundary and failure tests for every public operation, including empty inputs and documents, extreme valid values, invalid enums and options, null references, disposed readers and writers, throwing caller rules and resolvers, output-limit failures, and repeat use after failure.
- [DONE] T035: Add a public-contract coverage matrix that maps every exposed member, node kind, feature flag, preset, render mode, event kind, diagnostic severity, URI kind, and tree-walk result to behavioral tests, then fill every uncovered contract entry with boundary and interaction coverage.
