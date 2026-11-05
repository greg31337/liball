# Architecture

UniversalHtml is designed as a synchronous, zero-dependency .NET 10 pipeline for processing HTML without a browser runtime. The package is currently an alpha under active construction; this document describes the intended stable component boundaries, not a claim that every component has already been implemented. The implementation order and current status are tracked in [`todo.md`](../todo.md).

## Pipeline and ownership

```text
bytes or Stream                    string, span, or TextReader
       |                                      |
       +---------- decoding and input normalization
                              |
                         tokenizer
                              |
                       tree constructor
                              |
                     document or fragment
                    /          |          \
              selectors    serializer    extraction
                    \          |          /
                     sanitizer and policy
                              |
                    HTML, text, or Markdown
```

Each layer accepts concrete inputs and returns concrete result classes or owned arrays. Caller spans are never retained. The DOM owns node, attribute, and text data; parsing a fragment or document never leaves result state backed by a caller buffer.

The dependency direction is one-way:

1. Core utilities, limits, source locations, diagnostics, and generated tables depend only on the base class library.
2. Input normalization and decoding depend on the core layer.
3. Entity handling and tokenization consume normalized input.
4. Tree construction consumes tokens and produces the DOM.
5. Traversal, selectors, URLs, and microsyntax helpers operate on the DOM or owned values.
6. Serialization, sanitization, text conversion, Markdown conversion, metadata, and readability consume those lower layers.
7. The public façade coordinates components but contains no separate parsing rules.

The tokenizer and tree constructor remain separate state machines. Malformed-input correction happens during parsing so later consumers see the same corrected tree. Sanitization is a distinct policy operation and must not be confused with parsing or serialization.

## Cross-cutting invariants

- Normal package builds never access the network and never run a generator. Standards-derived tables are committed source generated from checksum-pinned local inputs.
- Every externally reachable workflow has finite resource limits independent of cancellation. A shared budget prevents pipeline stages from resetting consumed work.
- Streaming APIs are synchronous. Buffering may be incremental, but ownership and leave-open behavior are explicit.
- ASCII rules use ordinal ASCII operations. HTML grammar decisions never depend on the current culture.
- Source spans are end-exclusive and preserve normalized character offsets separately from original UTF-16 offsets. Byte offsets exist only when decoding tracked them.
- Expected malformed input produces diagnostics and usable partial results when safe. Exceptions are reserved for invalid arguments, programmer errors, invariant violations, and unrecoverable exhaustion.
- Public reusable option or policy objects are copied, frozen, or compiled before concurrent use. A running operation is not affected by later caller mutations.
- Serialization is deterministic for a fixed tree and options. Sanitization decisions are namespace-aware and are recorded separately from parser corrections.

## Security boundaries

Parsing answers what tree the supplied bytes or characters represent. It does not make that tree safe. Sanitization applies an explicit compiled policy to a fresh tree or clone by default and returns an audit report. Modifying sanitized output afterward can invalidate the safety guarantee.

Resource limits defend availability; they are not a substitute for sanitization. Cancellation is cooperative; it is not a resource limit. URL and inline-CSS checks are separate policy layers because an allowed element or attribute name does not imply a safe value.

The library performs no I/O except reading caller-provided streams or readers and writing caller-requested results. It does not retrieve URLs, execute scripts, load subresources, or evaluate CSS.

## Non-goals

UniversalHtml intentionally does not provide:

- HTTP fetching, crawling, cookies, sessions, redirects, caching, or browser navigation;
- JavaScript execution, custom-element lifecycle, shadow DOM, mutation observers, rendering, layout, computed style, media-query evaluation, or accessibility-tree construction;
- form submission, a browsing context, or a general CSS object model;
- general XML or XHTML parsing, validation, DTD/entity expansion, XPath, XSLT, or XML round-tripping;
- Markdown-to-HTML parsing or byte-for-byte preservation of the source markup;
- a guarantee that arbitrary changes made after sanitization remain safe;
- compatibility with every API shape of jsoup, AngleSharp, Html Agility Pack, Beautiful Soup, parse5, html5lib, DOMPurify, or other reference projects;
- unbounded processing, implicit network lookups, or optional runtime parser/encoding packages.

These boundaries are product constraints. Expanding one requires an explicit plan and security review, not an incidental helper added to an otherwise related feature.

## Production syntax consequences

Production code deliberately uses a restricted C# subset. Every production type, constructor, method, and field has explicit `public` or `internal` visibility. Public classes, constructors, methods, fields, enums, interfaces, and enum members form the supported package API; internal symbols do not. Public API symbols have plain-English XML documentation using only the constructs permitted by the syntax policy, while internal symbols do not carry API documentation. Result, diagnostic, exception, inspection-only rule, and compiled-configuration classes are public only when callers inspect, catch, or pass them in a supported workflow, and library-owned construction paths remain internal. Compiled snapshots exchanged only between internal stages are internal. Caller-authored option, policy, context, and DOM values expose construction only when the workflow requires it; when named factories cover every supported construction state, their duplicate constructors remain internal. Defensive-copy helpers on publicly immutable values remain internal; public `Copy()` methods are reserved for mutable caller-facing configurations. The DOM uses tagged concrete classes rather than an inheritance hierarchy so ownership, mutation, cloning, and traversal share one invariant-preserving representation. State is exposed through methods instead of properties and indexers. Nullable reference annotations describe natural absence, while expected negative outcomes use concrete result classes rather than ambiguous null results, tuples, or `out` parameters. Collections are concrete snapshots rather than library-defined generic abstractions.

Interfaces are limited to stable behavioral seams. `IHtmlParser` and `IHtmlTokenizer` let applications depend on and substitute incremental operations without exposing parser implementation state. `IHtmlTextOptions` and `IHtmlMarkdownOptions` give mutable builders and immutable compiled snapshots one conversion boundary, so converters do not need parallel public overloads for both representations. Internally, the parser consumes an `IHtmlTokenizerControl`; an adapter keeps tree-builder controls out of the public tokenizer contract and permits focused parser orchestration tests with deterministic tokenizer stubs. DOM nodes, result models, compiled sanitizer policies, and sanitizer stages remain concrete because substituting them would weaken ownership, snapshot, or security invariants without simplifying a supported workflow.

The sanitizer's element, node, attribute, URL, CSS, nested-content, template-marker, and DOM-clobbering stages are deliberately internal. Only the coordinating sanitizer operation applies the complete policy, shared limits, cancellation, failure-closed behavior, and audit reporting required by the security contract.

[`syntax.md`](../syntax.md) is authoritative. [`CONTRIBUTING.md`](../CONTRIBUTING.md) translates every restriction into a contributor checklist and explains the resulting API patterns.
