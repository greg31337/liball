# Universal HTML library development plan

Status: proposed implementation plan. All implementation work is open.

## 1. Product goal

Build a zero-dependency .NET 10 library that accepts real-world HTML, constructs the same kind of corrected tree a browser would construct, exposes safe traversal/query/mutation APIs, serializes HTML deterministically, sanitizes untrusted markup with explicit policies, and converts whole documents or subtrees to useful plain text or Markdown.

“Universal” means that the library handles complete documents and context-sensitive fragments, malformed tag soup, HTML namespaces plus embedded SVG and MathML, strings and byte/stream inputs, Unicode and common web encodings, large or adversarial inputs, and both low-level and one-call workflows. It does not mean that the library is a browser engine.

The runtime and build must use only the .NET SDK and base class libraries. No parser, selector, sanitizer, Markdown, CSS, test, analyzer, source-generator, or benchmarking package may be referenced. Generated lookup data is committed as source so normal builds never use the network.

## 2. Governing constraints and design consequences

The implementation must follow [`syntax.md`](syntax.md) for every file below `src/` and `tests/`.

- Production code uses file-scoped namespaces, classes, enums, fields, constructors, and ordinary methods only.
- Every production type, constructor, method, and field uses explicit `public` or `internal` visibility. Only the intentionally supported API surface is `public`; implementation types and members are `internal`.
- Every public type, constructor, method, field, enum, and enum member has plain-English XML documentation using only the constructs allowed by `syntax.md`. Internal declarations do not carry XML documentation.
- Library-produced results, diagnostics, exceptions, inspection-only rules, and compiled configurations expose public inspection, catching, or reuse only where a caller workflow needs it; their construction paths remain `internal`.
- Named public factories are the canonical construction path when they cover every supported state; duplicate constructors remain `internal`.
- Defensive-copy helpers for publicly immutable values remain `internal`; public `Copy()` methods are limited to mutable caller-facing configurations.
- Same-value aliases used only to clarify internal call sites remain `internal`; each public member provides a distinct supported operation or observation.
- Public state is exposed through `Get...`, `Set...`, `Add...`, `Remove...`, and `Create...` methods instead of properties or indexers.
- A single tagged `HtmlNode` class represents document, fragment, doctype, element, text, and comment nodes so all tree mutation and ownership invariants use one concrete representation.
- Public collections are returned as concrete arrays or represented by concrete collection classes; the library declares no generic types or generic methods.
- Nullable reference annotations express which methods may return `null`; those methods also say so in their API documentation and have a `Try...` result-object alternative where ambiguity matters.
- Recoverable failures use result and diagnostic classes. Exceptions are reserved for programmer errors, violated tree invariants, and resource exhaustion.
- No `ref`, `out`, iterator, LINQ, pattern, property, `try/catch/finally`, or async implementation strategy may appear in production source. Internal parsers therefore return small result classes rather than `TryParse(..., out value)` tuples.
- Streaming APIs are synchronous. The library never fetches a URL and never executes script or loads a subresource.
- `ReadOnlySpan<char>`, `ReadOnlySpan<byte>`, and `Span<char>` are used on hot scanning/escaping paths. Spans are not stored in nodes or results. Start with owned growable arrays and `StringBuilder`; use `ArrayPool<T>` only where benchmarks show a gain and control flow can always return rentals without `finally`.
- All option objects have safe defaults, explicit factory presets, copy methods, and a freeze/compile step where concurrent reuse matters.
- Parsing and conversion methods accept `CancellationToken` in their full overloads and enforce configurable limits independently of cancellation.

## 3. Standards and library research

Research was performed against primary project documentation and specifications on 2026-08-09. Features are adopted for their utility, not API compatibility.

| Source | Relevant feature surface | Decision for this library |
|---|---|---|
| [WHATWG HTML parsing](https://html.spec.whatwg.org/multipage/parsing.html) and [named references](https://html.spec.whatwg.org/multipage/named-characters.html) | Preprocessing, tokenization states, parse errors, tree-construction insertion modes, fragment parsing, foster parenting, adoption agency, quirks mode, SVG/MathML integration | Normative parsing behavior and conformance target |
| [WHATWG Encoding](https://encoding.spec.whatwg.org/) | Exhaustive web encoding labels, indexes, single/multibyte/stateful decoders, replacement behavior, and security considerations | Implement every input decoder and label required for browser-compatible HTML without relying on optional .NET code-page packages |
| [WHATWG DOM](https://dom.spec.whatwg.org/) | Node kinds, tree order, traversal, text content, mutation validity, clone, normalize | Adopt the useful server-side tree semantics without events, live browser objects, shadow roots, or custom-element reactions |
| [Selectors Level 4](https://www.w3.org/TR/selectors-4/) | Selector lists, combinators, type/id/class/attribute selectors, structural and logical pseudo-classes, `:scope`, `:is`, `:where`, `:not`, `:has`, `An+B` | Implement the deterministic, tree-observable subset; explicitly reject pseudo-elements and browser-state/layout-dependent pseudo-classes |
| [WHATWG URL](https://url.spec.whatwg.org/) and [CSS Syntax Level 3](https://www.w3.org/TR/css-syntax-3/) | Browser URL parsing/resolution and CSS tokenization/error recovery | Use as the behavior baseline for URL policy checks and the inline-CSS sanitizer, constrained to the subset the library actually exposes |
| [jsoup](https://jsoup.org/apidocs/) and its [cookbook](https://jsoup.org/cookbook/) | Document/fragment parsing, malformed-input recovery, DOM navigation and mutation, CSS/XPath queries, relative URL handling, safelist cleaning, tidy output, stream parsing | Adopt the cohesive façade, CSS queries, safelist concepts, URL resolution, mutation, serializer settings, and streaming tokenizer; omit HTTP sessions and XPath |
| [AngleSharp](https://github.com/AngleSharp/AngleSharp) | Standards-based HTML/SVG/MathML DOM, CSS selectors and CSS parsing, fragments, browsing context, navigation and forms | Adopt standards fidelity, namespaces, selectors, and CSS tokenization needed by sanitization; omit browser navigation, scripting, form submission, layout, and general CSSOM |
| [Html Agility Pack](https://html-agility-pack.net/) | Forgiving C# DOM, load/save, text extraction, XPath/XSLT | Adopt forgiving load/save and simple discovery helpers; omit XPath/XSLT because CSS selectors cover HTML queries without an XML compatibility layer |
| [Beautiful Soup](https://www.crummy.com/software/BeautifulSoup/bs4/doc/) | Friendly tree navigation/search, descendants/siblings/parents, mutation, text joining/stripping, formatter choices, encoding metadata | Adopt ergonomic navigation, search, mutation, text joining, and source-encoding metadata; do not support interchangeable external parser backends |
| [parse5](https://parse5.js.org/) and [html5lib](https://html5lib.readthedocs.io/en/stable/html5lib.html) | WHATWG parsing, document/fragments, parse diagnostics, source locations, tokenizer/tree-builder separation, walkers, serializer filters | Adopt component separation, incremental tokenization, locations, diagnostics, tree walking, and deterministic serialization |
| [DOMPurify](https://github.com/cure53/DOMPurify) | Secure defaults, HTML/SVG/MathML profiles, allow/block lists, custom elements, URI checks, template recursion, DOM-clobbering defenses, removal reports | Adopt profiles, namespace-aware policies, recursive template handling, URL and clobbering defenses, and reports; server-side output is a normal string/DOM, so Trusted Types is not applicable |
| [OWASP Java HTML Sanitizer](https://github.com/owasp/java-html-sanitizer) | Composable policies, element/attribute policies, preprocessors, postprocessors, telemetry, adversarial review | Adopt composable compiled policies, forced attributes, transforms, and audit telemetry |
| [Ganss.HtmlSanitizer](https://github.com/mganss/HtmlSanitizer) | Allowed tags/attributes/CSS properties/at-rules/schemes, URI attributes, base URI resolution, removal notifications | Adopt the separated HTML/CSS/URL allowlists and base URI support; use a returned report rather than events, which the syntax policy forbids |
| [Turndown](https://github.com/mixmark-io/turndown) | CommonMark-oriented rules, keep/remove/fallback behavior, escaping, formatting choices, plugins including GFM | Adopt rule ordering and unknown-element policies through snapshot-safe concrete rule objects rather than runtime plugins |
| [CommonMark 0.31.2](https://spec.commonmark.org/0.31.2/) and [GFM](https://github.github.com/gfm/) | Precise Markdown escaping and block/inline constructs; GFM tables, task items, strikethrough, autolinks, tag filtering | Define the converter’s output contracts and conformance fixtures |
| [html-to-text](https://www.npmjs.com/package/html-to-text) | Whitespace normalization, selector-driven formatting, word wrapping, links/images, tables, batch-compiled options | Adopt semantic formatters, wrapping, limits, and compiled reusable options |
| [Mozilla Readability](https://github.com/mozilla/readability) | Main-content scoring, readerability preflight, metadata, HTML and text results, size limits | Add an optional high-level readable-content extractor; keep it separate from sanitization |

## 4. Scope boundaries

### In scope

- WHATWG-style HTML parsing for documents and context-sensitive fragments, including browser error recovery.
- HTML, SVG, and MathML namespaces as they occur inside HTML.
- Mutable server-side DOM, source locations, diagnostics, traversal, search, CSS selectors, and URL discovery/resolution.
- Canonical, pretty, compact, and configurable HTML serialization.
- Secure-by-default, policy-driven HTML/CSS/URL sanitization with an audit report.
- Semantic plain-text conversion, Markdown conversion, metadata extraction, readable-main-content extraction, and an end-to-end processing pipeline.
- Sync string, span, byte, `Stream`, and `TextReader` entry points; incremental tokenization/parsing for bounded-memory callers.
- Deterministic behavior across cultures and operating systems, explicit resource limits, cancellation, and thread-safe reuse of compiled configuration.

### Explicit non-goals

- HTTP fetching, cookies, sessions, redirects, caching, crawling, or browser navigation.
- JavaScript execution, custom-element lifecycle, shadow DOM, mutation observers, rendering, layout, computed styles, media queries, or accessibility-tree computation.
- General XML/XHTML parsing, validation, DTD/entity expansion, XPath, XSLT, or CSS cascade/style-sheet application.
- Markdown-to-HTML parsing or round-trip preservation of the original byte-for-byte markup.
- Proving that arbitrary post-sanitization modifications remain safe. Security documentation must state that changing a sanitized tree/string can invalidate the guarantee.

## 5. Architecture and data flow

```text
bytes/Stream + encoding hint       string/span/TextReader
              |                            |
              +-- decoder/sniffer ----------+
                               |
                    input preprocessing
                               |
                   incremental tokenizer -----> token consumer API
                               |
                     HTML tree constructor
                               |
                    HtmlDocument / HtmlNode
                      /       |        \
             selectors   serializer   metadata/readability
                 |            |               |
                 +---- sanitizer ----+---------+
                                     |
                         HTML / text / Markdown
```

The tokenizer and tree constructor are separate state machines. The tree owns strings and nodes; no result references caller spans. Sanitization operates on a fresh parse result or a deep clone by default, and never mutates a caller-owned tree unless an explicitly named in-place method is used. Text and Markdown converters consume a tree so malformed-input correction, entity decoding, namespaces, and ignored raw-text content are consistent across all workflows.

## 6. Complete feature catalog

### 6.1 Input, decoding, and preprocessing

- String and `ReadOnlySpan<char>` input without copying when the parse completes within the call.
- Synchronous `TextReader` and `Stream` input with bounded buffers, caller-supplied `Encoding`, and leave-open control.
- Byte and `ReadOnlySpan<byte>` input with explicit encoding hints, BOM detection, transport-label hints, prescan of `<meta charset>`/`http-equiv`, confidence/source reporting, and encoding restart before committed tree output.
- Complete WHATWG Encoding Standard label normalization and input decoders: UTF-8, UTF-16LE/BE, every legacy single-byte encoding, Big5, EUC-JP, ISO-2022-JP, Shift_JIS, EUC-KR, gb18030/GBK, replacement, and x-user-defined. Required indexes are committed as generated lookup tables so no code-page package or operating-system registry is needed.
- Replacement of malformed byte sequences, U+0000 handling, CR/CRLF normalization, preservation of character offsets, line/column tracking, and optional original byte-offset tracking.
- Base URI, response URL, content language, encoding metadata, scripting-enabled flag, maximum-input limits, and cancellation.

### 6.2 Character references and lexical utilities

- Complete WHATWG named character reference table, including two-code-point values and legacy names without semicolons.
- Decimal and hexadecimal numeric references, invalid scalar replacement, Windows-1252 control substitutions, ambiguous ampersands, and attribute-context rules.
- Public entity decode/encode helpers with text/attribute contexts and minimal/named/numeric output modes.
- ASCII whitespace, alpha, digit, hexadecimal, case-folding, tag/attribute-name, namespace, and Unicode scalar helpers implemented with spans and culture-invariant rules.

### 6.3 HTML tokenizer

- All WHATWG tokenizer state families: data, RCDATA, RAWTEXT, script data and escaped/double-escaped variants, plaintext, tag open/name, attribute name/value variants, character references, comments/bogus comments, markup declarations, doctypes, CDATA in foreign content, and EOF handling.
- Start-tag, end-tag, character, comment, doctype, and EOF token kinds with ordered attributes, self-closing acknowledgement, source spans, and parse diagnostics.
- Correct handling of duplicate attributes, nulls, unexpected characters, incorrectly closed comments, malformed doctypes, and chunk boundaries inside any token or entity.
- Incremental `Feed`/`Read` API, pull tokenizer over `TextReader`, cancellation, token/input limits, and optional token-only operation that does not build a DOM.

### 6.4 HTML tree construction

- Every standard insertion mode: initial; before html/head; in head/head-noscript; after head; in body/text/table/table-text/caption/column-group/table-body/row/cell/select/select-in-table/template; after body/frameset; after-after body/frameset.
- Stack of open elements, scope variants, implied end tags, active formatting elements and Noah’s Ark rule, adoption-agency algorithm, reconstruction of formatting, foster parenting, pending table characters, frameset flag, form pointer, head pointer, and template insertion-mode stack.
- Document mode selection (no-quirks, limited-quirks, quirks) from doctype public/system identifiers.
- Implicit `html`, `head`, `body`, `tbody`, and other corrected elements, with source locations that distinguish explicit and generated nodes.
- Context-sensitive fragment parsing for HTML, table, select, template, RCDATA, RAWTEXT, script, plaintext, SVG, and MathML contexts.
- HTML/SVG/MathML namespace switching, integration points, SVG tag/attribute case adjustments, MathML adjustments, foreign attributes (`xlink`, `xml`, `xmlns`), and CDATA handling.
- Script elements remain inert data. The scripting flag only controls parsing branches such as `noscript`; no code executes.
- Parser diagnostics with stable codes, severity, source span, bounded collection, and continuation after normal HTML parse errors.

### 6.5 DOM model and mutation

- Node kinds: document, document fragment, document type, element, text, and comment; attributes are ordered records rather than child nodes.
- Document URL/base URL, detected encoding, quirks mode, language/direction metadata, diagnostics, document element, head, body, title, and template-content fragment.
- Namespace URI, prefix, local/qualified name, ordered attributes with namespace/prefix/local name/value/source span, and ASCII-insensitive HTML lookup rules.
- Parent, first/last child, previous/next sibling, element-only sibling/children, ancestors, descendants, subtree order, root, owner document, depth, and containment.
- `TextContent`, own text, normalized visible-ish text, inner HTML, outer HTML, class-token, dataset, and absolute-URL conveniences.
- Create, append, prepend, insert-before/after, replace, detach/remove, unwrap, wrap, empty, rename, set text/HTML, attribute/class mutation, deep/shallow clone, import, and normalize adjacent text.
- Mutation preflight that prevents cycles and invalid document children and correctly reparents nodes; mutation versioning invalidates selector/search indexes.
- Snapshot arrays rather than surprising live collections; read operations are safe concurrently after a tree is frozen, while mutation is explicitly not thread-safe.

### 6.6 Traversal, search, and CSS selectors

- Fast helpers for first/all by id, tag, class, attribute, name, and predicate; ancestor/descendant/sibling traversal and `Closest`.
- Depth-first enter/exit walking, breadth-first walking, early stop, node filtering, and configurable maximum results/visits.
- CSS escape and token parsing with precise diagnostics and selector source positions.
- Selector lists and forgiving selector lists; universal and namespace-qualified type selectors; id and class selectors.
- Attribute presence and `=`, `~=`, `|=`, `^=`, `$=`, `*=` operators with `i`/`s` modifiers and HTML-specific value case rules.
- Descendant, child, next-sibling, subsequent-sibling, and column combinator parsing; column matching may initially report unsupported because layout/table-column association is not a pure DOM relation.
- `:root`, `:scope`, `:empty`, `:first/last/only-child`, `:first/last/only-of-type`, and all `:nth-*` forms including `odd`, `even`, signed `An+B`, and `of S`.
- Logical `:is`, `:where`, `:not`, and relative-selector `:has`; language/direction `:lang` and `:dir`.
- Static document-state pseudo-classes whose answer is tree-observable: `:any-link`, `:link`, `:enabled`, `:disabled`, `:checked`, `:indeterminate`, `:required`, `:optional`, `:read-only`, `:read-write`, `:placeholder-shown`, and `:default` where determinable from markup.
- Pseudo-elements and dynamic/browser-context pseudo-classes (`:hover`, `:active`, `:focus*`, `:visited`, `:target*`, media/layout state, user-validity) fail compilation with a stable unsupported-feature diagnostic.
- Compile-once `HtmlSelector` objects, right-to-left matching, candidate seeding from lazy id/class/tag indexes, document-order unique results, `Matches`, `Closest`, and subtree scoping.

### 6.7 URL and attribute microsyntax support

- Safe lexical scheme detection that strips/recognizes encoded and control-character obfuscation before policy decisions.
- Relative reference resolution against explicit/document/base-element URI, fragment and query handling, protocol-relative URLs, and normalization through base-class-library URI primitives only after validation.
- HTML space-separated and comma-separated token lists; `class`, `rel`, `sandbox`, `headers`, `accept`, and similar token helpers.
- Robust `srcset` candidate parsing/serialization, including width/pixel-density descriptors and malformed-candidate rejection.
- Refresh-content URL extraction, language tags, integer dimensions, ordered-list numbering (`start`, `value`, `reversed`, `type`), and table span/alignment microsyntax used by higher layers.

### 6.8 HTML serialization

- Whole document, fragment, inner HTML, outer HTML, and subtree serialization to string or `TextWriter`.
- Correct namespace/prefix and foreign-content output, doctype, comments, raw-text/escapable-raw-text handling, void elements, template contents, boolean attributes, and entity escaping.
- Canonical mode with normalized lowercase HTML names and double quotes; preserve-case mode where source data permits; ASCII-only/entity mode.
- Pretty mode with indentation/newline controls that never inserts whitespace into preformatted/raw-text/phrasing contexts; compact mode and optional safe omission of standard optional tags.
- Attribute order preservation or deterministic sorting, configurable quote choice, self-closing foreign elements, output character limit, cancellation, and round-trip parse/serialize invariants.

### 6.9 Sanitization

- Immutable compiled policies and built-in presets: `TextOnly`, `BasicFormatting`, `RichText`, and conservative `HtmlOnly`; SVG/MathML are opt-in profiles.
- Namespace-aware allowlists and blocklists for elements and attributes, global and element-specific attributes, `data-*`/`aria-*`, custom elements, customized built-ins, comments, doctypes, template content, and custom namespaces.
- Disallowed-element actions: drop subtree, unwrap while keeping children, replace with text, or escape as text. Dangerous raw-text/container elements default to dropping their subtrees.
- Attribute actions: keep, drop, rename, validate, normalize, prefix, force a value, or add required values such as `rel="noopener noreferrer"` on `_blank` links.
- URL-attribute classification for `href`, `src`, `srcset`, `action`, `formaction`, `poster`, `cite`, `background`, `ping`, `xlink:href`, and configurable additions.
- Scheme/host/port policy, relative and protocol-relative controls, base resolution, fragment-only controls, credentials rejection, control/whitespace normalization, safe `data:` media-type/size checks, and per-element URL rules.
- DOM-clobbering defense for dangerous `id`/`name` values, configurable prefixing, duplicate identifier reporting, and form/name collision handling.
- Template-syntax neutralization for `{{...}}`, `${...}`, and `<%...%>` as an explicit opt-in profile, plus recursive sanitization of template fragments and any explicitly allowed nested-markup attribute such as `srcdoc`.
- Inline CSS parser/sanitizer with comments/escapes/strings/functions/numbers/dimensions, declaration recovery, property/at-rule/function allowlists, `url()` reuse of URL policy, custom-property policy, and rejection of legacy execution/binding constructs. Style attributes are disabled by default; `<style>` remains out of scope and is dropped.
- Removal/rewriting report containing stable reason codes, source spans, original names, safe redacted values, counts, truncation state, and limit/cancellation status. Reports are informational and never a security oracle.
- Parse-then-sanitize ordering to resist tag poisoning, mutation XSS, namespace confusion, raw-text breakouts, encoded-protocol tricks, malformed `srcset`, and foreign-content attacks.
- Return sanitized DOM, fragment, or canonical HTML; default operations clone input, while clearly named in-place operations are available for controlled pipelines.

### 6.10 Plain-text conversion

- Descendant text decoding with HTML whitespace collapsing, non-breaking-space policy, Unicode preservation, configurable line endings, blank-line compaction, trimming, and column wrapping.
- Semantic block boundaries for sections, paragraphs, headings, address, blockquotes, figures, details, definition lists, and generic block containers.
- `br`, thematic-break, preformatted text, tabs, code, keyboard/sample output, sub/sup, ruby annotation, bidirectional text, and hidden/inert content handling.
- Ordered/unordered/task lists with nesting, correct numbering/start/value/reversed/type, bullet styles, hanging indentation, and configurable depth.
- Links as text only, text plus URL, footnote-style references, or URL only; relative URL resolution, duplicate-reference coalescing, and unsafe-link omission.
- Images/media as alt text, title, source URL, placeholder, or omission; figure captions and optional form-control values/labels.
- Tables in compact reading order, TSV-like grid, aligned grid, or omission, with caption/header handling and bounded rowspan/colspan expansion.
- Selector-based include/exclude/replace rules, ignored script/style/template/head content by default, maximum output/visits/depth, truncation marker, cancellation, and conversion diagnostics.
- Compile-once converter settings for batch use and conversion from HTML string, document, fragment, or node.

### 6.11 Markdown conversion

- CommonMark-oriented output with configurable ATX/setext headings, emphasis/strong delimiters, semantic strikethrough, paragraphs, thematic breaks, hard/soft breaks, and context-aware escaping.
- Nested ordered/unordered/task lists with stable indentation, numbering options, tight/loose separation, and continuation blocks.
- Nested blockquotes; inline code delimiters chosen around content; fenced code blocks with collision-free fence length, info string from language/class, and preformatted whitespace preservation.
- Inline/reference/collapsed links, autolinks when safe, title escaping, base-URL resolution, duplicate reference coalescing, images, and unsafe-link fallback.
- GFM tables with header inference, alignment, cell pipe/newline escaping, colspan/rowspan fallback, plus GFM strikethrough, task items, and autolinks.
- Optional definition lists, footnotes, superscript/subscript, and raw-HTML fallback as explicit non-CommonMark extensions.
- Unknown/disallowed element policies: unwrap, drop subtree, preserve serialized HTML, escape serialized HTML, replace with text, or emit a diagnostic.
- Ordered concrete conversion rules with match selector/tag and built-in action enum; caller rules can precede or follow defaults while remaining safe to compile into immutable snapshots.
- Blank-node, keep, remove, standard, custom, and default rule precedence inspired by Turndown; separate trusted raw-HTML setting.
- Whitespace cleanup that does not damage code, tables, nested containers, or intentional hard breaks; maximum output/visits/depth, cancellation, and diagnostics.
- Presets for `CommonMark`, `GitHubFlavored`, and `ReadableMarkdown`, with conversion from HTML string, document, fragment, or node.

### 6.12 Metadata and readable-content extraction

- Metadata extraction from title, base, canonical/alternate links, meta name/http-equiv/property, Open Graph, Twitter cards, Dublin Core, author/byline hints, language/direction, favicon, description, dates, and representative image.
- Safe JSON-LD metadata extraction using `System.Text.Json` with input/depth/property limits and no remote context loading; Schema.org headline/author/date/image/publisher fields only.
- Readerability preflight and main-content scoring based on semantic elements, text length, punctuation, link density, class/id hints, sibling aggregation, and unlikely-candidate removal.
- Cleanup of navigation, ads, forms, sidebars, duplicated headings, empty nodes, and excessive links while preserving figures, tables, code, math, captions, and media placeholders.
- `HtmlArticle` result containing success, title, byline, excerpt, site name, language, direction, published time text, canonical URL, representative image, content node, sanitized HTML, plain text, Markdown, length, and diagnostics.
- Extraction never claims sanitization; the high-level convenience pipeline sanitizes the extracted subtree before returning renderable HTML.

### 6.13 High-level workflows

- One-call `Parse`, `ParseFragment`, `Sanitize`, `ToText`, `ToMarkdown`, `ExtractMetadata`, `ExtractArticle`, and combined `Process` façade methods.
- Combined pipeline options for decoding, parsing, extraction, sanitization, HTML serialization, text, and Markdown with a single shared limit budget and cancellation token.
- Result object can return corrected document, sanitized tree/HTML, text, Markdown, metadata/article, parse diagnostics, sanitizer report, conversion diagnostics, detected encoding, truncation, and elapsed stage timings.
- Presets for untrusted rich text, email-like HTML, scraped article, search indexing, and LLM ingestion. Presets are documented starting points, not opaque magic.

### 6.14 Reliability, security, performance, and maintenance

- Stable diagnostic/error codes and deterministic output independent of current culture, local time zone, dictionary iteration order, and platform newline.
- Configurable limits for bytes/chars, tokens, attributes per tag, token length, nodes, depth, selector complexity, `:has` work, CSS tokens/declarations, URL/data-URI size, table expansion, JSON depth, visits, diagnostics, and output length.
- Graceful bounded termination with partial-result flags where safe; sanitizer fails closed if parsing or a security-relevant limit is exceeded.
- No catastrophic regular expressions. Core tokenization, selectors, CSS, URL screening, and Markdown escaping use explicit linear scanners.
- Thread-safe static tables and immutable compiled policies/options/selectors; mutable documents are single-writer objects.
- Source locations can be disabled for lower memory use. Lazy indexes, cached text, and cached selector results carry mutation-version stamps.
- Conformance suites from html5lib/WHATWG named entities/WPT selector samples/CommonMark/GFM plus adversarial sanitizer corpora are vendored as attributed data, not package dependencies.
- Differential tests may be run by maintainers against browsers or established libraries outside the normal build, but product code and CI correctness do not depend on them.
- Fuzz/property testing uses an in-repository deterministic generator and asserts no hangs, bounded allocations, tree invariants, idempotent sanitization, and stable reparse/serialize behavior.
- Performance gates cover tiny fragments, malformed pages, large documents, deeply nested content, selector-heavy queries, sanitizer attacks, text, Markdown, and streaming. Allocation goals are set only after baselines are measured.
- Security policy/versioning, disclosure process, threat model, safe-use guide, changelog, and sanitizer regression corpus are release requirements.

## 7. Public API completion tasks

- [DONE] T001: Complete document and fragment input contracts for string, `ReadOnlySpan<char>`, byte array, `ReadOnlySpan<byte>`, `Stream`, and `TextReader` sources, including explicit/transport encoding hints, response and base URLs, content language, leave-open behavior, byte locations, limits, and cancellation.
- [DONE] T002: Complete the low-level tokenizer and incremental parser contracts with pull-based `TextReader` operation, context-sensitive fragment initialization, cancellation, reset/reuse, and observable need-input, completion, limit, cancellation, and failure states across arbitrary chunk boundaries.
- [DONE] T003: Expand public diagnostics and bounded-result contracts so every specified decoding, tokenizer, tree-builder, selector, URL, serialization, sanitization, conversion, extraction, and pipeline failure has a stable code plus complete retained/dropped counts, truncation, cancellation, and partial-result state.
- [DONE] T004: Complete document and node data access for detected encoding, response/base URL, language, direction, mutation version, generated-versus-source-backed locations, and doctype name/public identifier/system identifier/force-quirks data, with factories for every supported node kind.
- [DONE] T005: Complete DOM navigation and mutation with first/last element access, namespaced attribute creation/update/removal, class and dataset token operations, rename, wrap, detach, child replacement, inner/outer HTML setters, import/clone variants, and invariant-preserving frozen-tree behavior.
- [DONE] T006: Complete selector compilation and querying with strict and forgiving selector-list modes, namespace-prefix bindings, specificity inspection, compiled-selector `Matches`/`Closest`/first/all operations, scoped result and work limits, cancellation, document-order deduplication, and precise unsupported-feature diagnostics.
- [DONE] T007: Complete the public URL and HTML-microsyntax layer with bounded serialization as well as parsing for token lists and `srcset`, safe base-aware URL and scheme inspection, refresh content, language tags, dimensions and table spans/alignment, ordered-list numbering, URL-bearing attribute classification, and detailed malformed-input results.
- [DONE] T008: Complete serialization contracts for documents, fragments, subtrees, inner HTML, and outer HTML to strings and `TextWriter`, exposing canonical/preserve/ASCII-only name and entity modes, quote and attribute ordering, compact/pretty/optional-tag behavior, foreign-element syntax, limits, cancellation, and explicit completion status.
- [DONE] T009: Complete sanitizer policy configuration for namespace-aware element and attribute actions, built-in validation and prefixing, custom and customized built-in elements, nested-markup attributes, template-marker handling, URL/data-URL rules, DOM-clobbering behavior, and inline-CSS property/function/keyword/unit/custom-property controls.
- [DONE] T010: Complete sanitizer workflows for HTML strings, documents, fragments, and subtrees with clone-by-default and clearly named in-place operations, compiled-policy reuse, canonical HTML output, fail-closed parsing/security-limit behavior, cancellation, and complete redacted change-report counts and reasons.
- [DONE] T011: Complete plain-text option and compiled-option contracts for non-breaking-space and whitespace policy, line and block boundaries, wrapping, nested ordered/unordered/task lists, links and safe base resolution, images/media, form controls, tables, selector rules, hidden/inert content, truncation, limits, and diagnostics.
- [DONE] T012: Complete Markdown option and compiled-option contracts for heading/emphasis/strong/break/fence/list choices, CommonMark and GFM links/tables/task items/strikethrough/autolinks, base URL and unsafe-link fallback, extension toggles, raw-HTML trust, unknown-element behavior, ordered selector rules, truncation, limits, and diagnostics.
- [DONE] T013: Add bounded, cancellable metadata-extraction overloads and options covering title/base/canonical/alternate links, standard meta and HTTP-equivalent fields, Open Graph, Twitter, Dublin Core, selected Schema.org JSON-LD fields, language/direction, favicon, authorship, dates, publisher/site, and representative images with candidate provenance.
- [DONE] T014: Complete readable-content APIs with cancellable readerability preflight, configurable scoring and cleanup controls, safe URL handling, optional sanitized HTML/text/Markdown production, preservation of meaningful figures/tables/code/math/media, and fully observable unsuccessful, partial, truncated, and diagnostic results.
- [DONE] T015: Complete the one-call façade and combined pipeline with `ToText` and `ExtractMetadata`, document/fragment/node workflows where applicable, every supported character/byte/reader/stream input, serializer configuration, immutable stage snapshots, shared limits and cancellation, and consistent per-stage results and timings.
- [DONE] T016: Add immutable compiled forms for reusable parse, serializer, article, and processing configurations, with validated defensive snapshots and thread-safe concurrent reuse while keeping mutable documents explicitly single-writer.
- [DONE] T017: Add complete public input, decoding, entity, tokenizer, parser, diagnostics, source-location, and limit API tests covering every constructor, preset, method, input form, encoding, chunk boundary, invalid argument, malformed input, cancellation point, and result state.
- [DONE] T018: Add complete public document, node, traversal, selector, URL/microsyntax, and serializer API tests covering every accessor and mutation, namespace and fragment case, freeze/invariant failure, selector mode, URL form, serialization option, limit, cancellation, and string/writer parity.
- [DONE] T019: Add complete public sanitizer API tests covering every preset, mutable and compiled policy operation, element/attribute/CSS/URL/nested-content/clobbering decision, clone and in-place workflow, report reason and redaction state, adversarial reparse, idempotence, limit, and cancellation outcome.
- [DONE] T020: Add complete public text and Markdown API tests covering every preset, mutable and compiled option, rule and output mode, document/fragment/node/string source, malformed-tree correction, Unicode and whitespace case, safe and unsafe URL, table/list/code nesting, diagnostics, limits, truncation, and cancellation.
- [DONE] T021: Add complete public metadata, article, façade, and combined-pipeline API tests covering every extraction source and precedence rule, option and preset, supported input form, stage combination and skip/failure path, sanitization ordering, shared budget, diagnostics, timings, truncation, and cancellation.
- [DONE] T022: Establish a public-API coverage inventory and close all remaining statement and branch gaps under `src/Api`, including factories, value/result objects, enum values, defensive copies, null-return contracts, invalid arguments, and exception paths, with a coverage gate scoped to the public layer.
