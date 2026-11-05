# Universal Text Template Library Development Plan

## Goal

Build a universal text template processing and generation library for .NET that can parse, validate, render, and compose templates for any text output format: HTML, XML, JSON, Markdown, source code, config files, email, CSV, and plain text.

The implementation must use only built-in .NET libraries. The source code must follow the simplified C# subset in `syntax.md`: simple classes, fields, constructors, block-bodied methods, explicit types, narrowly scoped interfaces, no properties, no class inheritance, no LINQ, no attributes, no records, no delegates declared by this project, and no external packages.

## Research Notes

Primary sources reviewed:

- Handlebars expressions, helpers, block helpers, partials, escaping, and whitespace control: https://handlebarsjs.com/guide/expressions.html, https://handlebarsjs.com/guide/builtin-helpers.html, https://handlebarsjs.com/guide/block-helpers.html, https://handlebarsjs.com/guide/partials.html
- Mustache variables, sections, inverted sections, comments, partials, delimiter changes, lambdas, dynamic names, blocks, and parents: https://mustache.github.io/mustache.5.html
- Jinja variables, filters, tests, comments, whitespace control, escaping, assignments, inheritance, blocks, includes, imports, and macros: https://jinja.palletsprojects.com/en/stable/templates/
- Liquid objects, tags, filters, control flow, iteration, variables, raw/comment/liquid/render/include tags, and whitespace control: https://shopify.github.io/liquid/basics/introduction/, https://shopify.github.io/liquid/tags/control-flow/, https://shopify.github.io/liquid/tags/iteration/, https://shopify.github.io/liquid/tags/template/, https://shopify.github.io/liquid/tags/variable/, https://shopify.github.io/liquid/basics/whitespace/
- Scriban blocks, comments, literals, variables, objects, arrays, functions, expressions, statements, includes, whitespace control, auto indentation, runtime API, built-ins, and Liquid compatibility: https://scriban.github.io/docs/, https://scriban.github.io/docs/language/, https://scriban.github.io/docs/runtime/, https://scriban.github.io/docs/builtins/, https://scriban.github.io/docs/liquid-support/
- Django template language variables, filters, tags, inheritance, autoescape, custom tags, and safety philosophy: https://docs.djangoproject.com/en/dev/ref/templates/language/, https://docs.djangoproject.com/en/dev/ref/templates/builtins/
- Nunjucks inheritance, blocks, macros, imports, includes, raw/verbatim, filter blocks, call blocks, async loops, and loader safety warnings: https://mozilla.github.io/nunjucks/templating.html
- Go text/template actions, pipelines, variables, functions, template invocation, if/with/range, and execution model: https://pkg.go.dev/text/template
- Razor syntax, expressions, encoding, code blocks, control structures, directives, layouts, sections, and generated-code model: https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor

## Design Principles

- Treat templates as text, not HTML-first documents.
- Keep the core deterministic, sandboxable, and dependency-free.
- Separate lexing, parsing, binding, evaluation, rendering, diagnostics, and loading.
- Support a native syntax first, then add compatibility modes for Mustache-like, Handlebars-like, Liquid-like, and Jinja-like templates as opt-in parser profiles.
- Prefer explicit runtime registration over reflection-heavy magic.
- Make escaping configurable by output format.
- Make strictness configurable for missing values, type conversions, unknown functions, unsafe members, and recursion depth.
- Keep public APIs stable and small; put advanced control in options and registries.
- Keep implementation compatible with `syntax.md`: expose only the supported API as `public`, mark implementation types and members `internal`, and use ordinary classes, fields, explicit loops, and explicit collection handling.

## Feature Inventory, Low Level To High Level

### Source And Text Handling

- Template source abstraction for strings, files, embedded resources, and named templates.
- Source identity with name, path, version, and optional content hash.
- Line and column mapping for diagnostics.
- Unicode-safe text scanning using built-in `string`, `char`, `Rune` when needed, and `StringBuilder`.
- Newline normalization options: preserve, normalize to LF, normalize to CRLF.
- Configurable maximum template length.
- Configurable encoding for file loading through built-in `System.Text.Encoding`.

### Lexing

- Tokenizer for text, output tags, statement tags, comments, raw blocks, escaped delimiters, and EOF.
- Configurable delimiter profiles: native, Mustache/Handlebars `{{ }}`, Liquid/Jinja `{% %}` plus `{{ }}`, and custom delimiters.
- Whitespace trim markers such as `{{-`, `-}}`, `{%-`, `-%}`, and Handlebars-style `~`.
- Raw/verbatim/escape blocks that bypass nested parsing.
- Standalone-line detection for logic-only tags and comments.
- Delimiter change support for Mustache-style `{{=<% %>=}}`.
- Recovery tokens for malformed blocks so parsing can continue and report multiple errors.

### Syntax Tree

- Immutable-enough AST classes for document, text, output, comment, raw, statement, expression, block, partial, include, inheritance, macro/function, and error nodes.
- Source spans on every node.
- Parent-free AST to keep memory simple, with explicit visitors implemented as normal classes.
- Syntax tree inspection API for tooling.
- Round-trip formatting support as a later feature.

### Parsing

- Native expression parser with literals, variables, paths, indexers, function calls, filter pipes, unary operators, binary operators, conditional expressions, null coalescing, and grouping.
- Statement parser for `if`, `else`, `elseif`, `unless`, `case`, `when`, `for`, `while`, `with`, `set`, `assign`, `capture`, `include`, `render`, `partial`, `block`, `extends`, `macro`, `return`, `break`, and `continue`.
- Mustache compatibility parser for variables, unescaped variables, sections, inverted sections, comments, partials, delimiter changes, lambdas, dynamic names, blocks, and parents.
- Handlebars compatibility parser for path expressions, helpers, hash arguments, subexpressions, block helpers, partial blocks, inline partials, block parameters, and data variables.
- Liquid compatibility parser for objects, tags, filters, operators, ranges, `forloop`, `assign`, `capture`, `increment`, `decrement`, `render`, and deprecated `include` behavior.
- Jinja/Django compatibility parser for variables, filters, tests, `set`, `filter` blocks, `include`, `import`, macros, blocks, and inheritance.
- Razor-inspired mode only as a future generation target, not as arbitrary C# execution.

### Diagnostics

- Structured diagnostics with code, severity, message, source span, and optional hint.
- Error recovery for unterminated tags, unknown tags, mismatched block names, invalid expressions, unexpected EOF, and recursive includes.
- Warning diagnostics for deprecated tags, unsafe output, unused blocks, unreachable branches, and compatibility-mode ambiguity.
- Diagnostic formatting for console and IDE use.

### Binding And Name Resolution

- Template symbol table for variables, locals, loop variables, block names, macros, functions, filters, tests, partials, and includes.
- Scope stack for root model, current context, locals, loop scopes, macro scopes, and include/render scopes.
- Lookup order configurable by profile: dictionary-first, member-first, local-first, helper-first, or strict.
- Path access for dictionaries, `IDictionary<string, object>`, `IReadOnlyDictionary<string, object>`, objects, arrays, lists, enumerables, and public fields/properties.
- Optional member access and null-safe traversal.
- Case sensitivity options.
- Member naming policy options: exact, camelCase, snake_case, kebab-case, and custom map.
- Block parameter aliases and parent context access.
- Reserved name protection for runtime internals.

### Runtime Values

- Template value wrapper for null, missing, boolean, number, string, safe string, list, map, object, function, and template fragment.
- Truthiness rules configurable by syntax profile.
- Numeric conversion rules for built-in integral, floating, decimal, and nullable values.
- String conversion and culture-aware formatting.
- Safe string marker for escaping decisions.
- Missing value behavior: empty string, diagnostic, exception, or custom marker.

### Expressions

- Variables, `this`, root, parent, local variables, and loop variables.
- Dotted paths and bracket indexers.
- String, number, boolean, null, empty, array, object, and range literals.
- Unary operators: not, negative, positive.
- Binary operators: equality, inequality, comparison, logical and/or, arithmetic, contains, starts-with, ends-with, and null coalescing.
- Conditional expression.
- Function calls with positional arguments.
- Filter pipes with positional arguments.
- Test expressions such as `is defined`, `is empty`, `is odd`, and custom tests.
- Subexpressions for helper-like profiles.
- Optional strict mode that forbids assignments and complex expressions for logic-less templates.

### Statements And Control Flow

- Output statement with escaping mode.
- Raw output statement.
- Comments: inline, block, and standalone.
- Conditional blocks with `if`, `elseif`, `else`, and `unless`.
- Switch-like blocks with `case`, `when`, and `else`.
- Loops over enumerables, dictionaries, arrays, strings, and numeric ranges.
- Loop metadata: index, index0, first, last, length, reverse indexes, key, value, depth, parent loop.
- Loop options: limit, offset, reversed, empty/else branch.
- `break` and `continue`.
- `with` blocks that change current context.
- Assignment blocks: `set`, `assign`, and scoped locals.
- Capture blocks that render nested content into a variable.
- Increment and decrement counters for Liquid compatibility.
- Filter blocks that transform nested rendered content.
- Macro/function definitions for reusable content.
- Call blocks that pass nested content to a macro/function.

### Composition

- Named template loader abstraction.
- File system loader using built-in `System.IO`.
- In-memory loader for tests and embedded scenarios.
- Include/render statement with isolated or inherited context.
- Partial registration and partial invocation.
- Dynamic partial/include names with strict validation.
- Template inheritance with `extends`, named `block`, block override, optional `super`, and parent templates.
- Mustache parent/block extension support.
- Layout-style partial blocks.
- Import of macros/functions from named templates.
- Include recursion detection and depth limits.

### Functions, Filters, Helpers, And Tests

- Built-in string filters: upper, lower, capitalize, title, trim, ltrim, rtrim, replace, split, join, slice, truncate, truncate words, remove, append, prepend, newline to br.
- Built-in HTML filters: escape, escape once, strip HTML, safe, raw.
- Built-in URL filters: URL encode, URL decode.
- Built-in JSON filter using built-in `System.Text.Json`.
- Built-in number filters: abs, ceil, floor, round, plus, minus, times, divide, modulo, format.
- Built-in date/time filters using `DateTime`, `DateTimeOffset`, `TimeSpan`, and culture-aware formatting.
- Built-in collection filters: first, last, size, reverse, sort, sort natural, uniq, compact, map, where, sum.
- Built-in object filters: keys, values, has key, default.
- Built-in tests: defined, missing, null, empty, truthy, falsy, number, string, list, map, even, odd, divisible by, equal to, safe.
- Runtime registration for custom functions, filters, tests, helpers, and block helpers using built-in `Func<>` types or concrete handler classes.
- Safe output return support from functions and filters.
- Optional function purity metadata for caching later.

### Escaping And Output Safety

- Configurable output encoder: none, HTML, XML, JSON string, URL, JavaScript string, CSV field.
- Autoescape on/off per engine, template, block, or render call.
- Safe string support to prevent double escaping.
- Raw/unescaped output syntax.
- Escape filter and force escape filter.
- Output-format profiles.
- Security guidance diagnostics for likely unsafe raw output.

### Whitespace And Formatting

- Preserve whitespace by default.
- Left trim and right trim markers.
- Standalone tag trimming.
- Greedy and non-greedy trim modes.
- Auto indentation for multiline values.
- Keep or strip final trailing newline.
- Optional render-time indentation helper.
- Minimized output mode for generated code/config.
- Round-trip source preservation for tooling.

### Compatibility Modes

- Native mode with balanced feature set and predictable semantics.
- Mustache mode for logic-less templates.
- Handlebars mode for helpers, block helpers, partials, subexpressions, and HTML escaping defaults.
- Liquid mode for Shopify-style tags, filters, truthiness, `forloop`, `render`, and variable assignment.
- Jinja/Django mode for filters, tests, macro/import/include/block/extends concepts and autoescape profile.
- Strict compatibility flags per mode so users can choose exact or pragmatic behavior.

### Loading, Caching, And Compilation

- Parse result caching by source identity and options.
- Render plan compilation from AST to an executable node tree.
- Template cache with configurable capacity.
- Dependency tracking for includes, partials, and inherited templates.
- Cache invalidation by source version or timestamp.
- Preparse/precompile API for application startup.
- Thread-safe read-only compiled templates.

### Rendering

- Render to string.
- Render to `TextWriter`.
- Render with reusable context.
- Render with cancellation for async paths.
- Async render path for async loaders and async functions using built-in `Task`.
- Streaming-friendly output writer.
- Render limits: max output length, max include depth, max loop iterations, max render time checks, max expression depth.
- Culture-aware rendering.
- Deterministic mode for stable generated output.

### Extensibility

- Custom syntax profile registration.
- Custom delimiters.
- Custom loaders.
- Custom encoders.
- Custom value resolvers and member access policies.
- Custom filters/functions/tests/helpers/block helpers.
- Custom diagnostics sinks.
- Custom cache implementation using built-in collection interfaces if needed.

### Security And Sandboxing

- No arbitrary code execution.
- Denylist and allowlist for members.
- Disable reflection by default except controlled public member access.
- Block access to dangerous members such as type metadata, process, environment, file system, network, and service providers.
- Runtime quotas for depth, loops, output size, includes, recursion, and template count.
- Strict mode for untrusted templates.
- Safe loader roots for file templates.
- Path normalization and traversal prevention.
- Optional audit diagnostics for unsafe configuration.

### Tooling

- Syntax tree dump.
- Formatter and normalizer.
- Linter rules.
- Compatibility analyzer for migrating between profiles.
- Template dependency graph.
- Source map from output chunks to input spans.
- Benchmark harness using built-in .NET diagnostics.
- Golden-file test helpers.

### Packaging And Documentation

- Generate XML documentation for every public type, constructor, method, field, enum, and enum member. Keep internal implementation symbols undocumented.
- Use only the XML elements allowed by `syntax.md`; keep broader conceptual and usage guidance in Markdown.
- README quick start.
- Syntax reference.
- Compatibility matrix.
- Security model documentation.
- API overview.
- Examples for HTML, Markdown, JSON, source generation, email, and configuration files.

## High Level Phases

1. Foundation: source model, diagnostics, tokens, spans, options, and tests.
2. Native MVP: lexer, parser, AST, expression evaluator, output rendering, variables, filters, `if`, `for`, comments, raw, and whitespace trim.
3. Runtime Model: context stack, value conversion, member access, escaping, culture, render limits, and strict mode.
4. Composition: loaders, includes, partials, template cache, dependency tracking, and recursion protection.
5. Reuse Features: blocks, inheritance, macros/functions, filter blocks, capture, and imports.
6. Compatibility Profiles: Mustache, Handlebars, Liquid, and Jinja/Django behavior gates.
7. Production Hardening: async rendering, streaming, diagnostics polish, security audits, tooling, docs, benchmarks, and package readiness.

## Public API Completion Tasks

- [DONE] T001: Complete source and loading support with configurable built-in .NET encodings, embedded-resource loading, stable version and content-hash metadata, consistent synchronous and asynchronous behavior, maximum-template-length enforcement, and behavioral tests for every public loader operation.
- [DONE] T002: Add an end-to-end public syntax-profile registry so custom `TemplateProfile` instances and delimiters can be selected by render, parse, cache, analysis, and composition APIs, with strict-versus-pragmatic compatibility flags and tests for registered and unknown profiles.
- [DONE] T003: Complete lexer behavior for escaped delimiters, raw and verbatim blocks, greedy and non-greedy trim markers, standalone tags, Mustache delimiter changes, Unicode scalar boundaries, and multi-error recovery, using `ReadOnlySpan<char>` where appropriate and testing every public token kind and lexer result.
- [DONE] T004: Complete the public syntax model and parser for named and hash arguments, block parameters, branch chains, dynamic names, recovery nodes, and lossless source spans so every planned native and compatibility construct can be represented and parsed with precise diagnostics.
- [DONE] T005: Complete expression evaluation for contains, starts-with, ends-with, profile-correct precedence, null-safe access, conditional and null-coalescing expressions, ranges, collection literals, subexpressions, filters, and tests, with boundary and invalid-operation coverage through public parse and render APIs.
- [DONE] T006: Complete runtime value conversion and name resolution for built-in numeric, date/time, dictionary, read-only dictionary, list, array, string, and enumerable types; add public custom value-conversion and member-name mapping policies; and test lookup order, culture, case, parent/root access, missing values, and denied members.
- [DONE] T007: Implement the planned `while` and value-returning `return` semantics and finish native control flow for conditionals, cases, loops, loop metadata, limits, offsets, reversal, empty branches, `break`, `continue`, `with`, assignment, capture, and counters, with tests for nesting and flow propagation.
- [DONE] T008: Complete composition semantics for inherited and isolated includes, renders, partials, dynamic names, parameters, context aliases, recursion detection, dependency recording, inheritance chains, block overrides, `super`, Mustache parents, and partial blocks, with public-API integration tests for success and failure paths.
- [DONE] T009: Complete reusable-content support for macros, functions, imports, filter blocks, call blocks, helpers, block helpers, safe results, argument validation, scope isolation, and return propagation, and cover every registry callable path through rendering tests.
- [DONE] T010: Complete the planned built-in string, HTML, URL, JSON, number, date/time, collection, object, and predicate functions with profile-correct names and edge-case semantics, using only built-in .NET facilities under `src/`, and add data-driven tests for every built-in and alias.
- [DONE] T011: Add a public custom output-encoder interface and make autoescape, raw output, force escape, safe strings, output-format profiles, and render-, template-, and block-level overrides consistent across string and writer rendering, with conformance tests against the local HTML, XML, JSON, URI, ECMAScript, and CSV specifications.
- [DONE] T012: Complete whitespace and formatting behavior for preservation, both trim modes, standalone lines, newline normalization, multiline auto-indentation, final newlines, indentation helpers, minimized output, and round-trip formatting, with equivalent buffered and streaming test matrices.
- [DONE] T013: Replace thread-pool wrappers with a genuinely asynchronous render pipeline that awaits loaders and public asynchronous functions, filters, tests, and helpers; propagates cancellation during evaluation, loops, composition, and writer output; and is covered by asynchronous-only test doubles.
- [DONE] T014: Make compiled templates safely reusable and make caching thread-safe, capacity-bounded, invalidatable by source identity, version, hash, and timestamp, and injectable through `ITemplateCompilationCache`; expose preparse and precompile operations and test eviction, invalidation, concurrency, and all cache counters.
- [DONE] T015: Add public diagnostic-sink and linter interfaces and complete lexer, parser, evaluator, composition, security, deprecation, unreachable-branch, unused-block, unsafe-output, and compatibility-ambiguity diagnostics with stable codes, spans, hints, recovery behavior, and formatter tests.
- [DONE] T016: Add public render source-map data and collection APIs that map every emitted output chunk to its originating template and source span across expressions, includes, partials, macros, and inheritance, with exact buffered and streaming mapping tests.
- [DONE] T017: Enforce all public security settings end to end, including template length, output length, expression and recursion depth, include depth, loop iterations, template count, elapsed time, loader roots, symlink escape prevention, reflection defaults, member allowlists and denylists, and strict untrusted-template mode, with adversarial tests through public entry points.
- [DONE] T018: Make Mustache mode conform to the downloaded official core and optional-module suites for interpolation, sections, inverted sections, comments, partials, standalone handling, delimiter changes, lambdas, dynamic names, inheritance, and error cases, importing those fixtures into public render tests without external runtime dependencies.
- [DONE] T019: Make Handlebars mode conform to the downloaded lexical, syntactic, and scope specifications for paths, escaping, helpers, hash arguments, subexpressions, block helpers, inverse chains, data variables, block parameters, partials, partial blocks, inline partials, dynamic partials, whitespace control, and malformed input, with public render tests.
- [DONE] T020: Make Liquid mode conform to the downloaded core, parser-error, partial, truthiness, scope, filter, control-flow, iteration, variable, raw, comment, liquid, render, and include specifications, with fixture-driven public parse and render tests for strict and lax behavior.
- [DONE] T021: Complete distinct Jinja and Django modes for profile-correct filters, tests, assignments, filter and autoescape blocks, includes, imports, macros, call blocks, inheritance, scoping, undefined values, whitespace, and safety semantics, with fixture-driven tests derived from the downloaded official references.
- [DONE] T022: Expand documented-example coverage for Scriban, Nunjucks, Go `text/template`, and Razor-inspired text generation so the native feature set and compatibility analyzer report supported translations, semantic differences, and unsupported constructs through public APIs.
- [DONE] T023: Complete compatibility analysis, dependency graph, syntax dump, diagnostic formatting, formatting, whitespace normalization, source-line mapping, value creation, and writer behavior, and add direct public-interface tests for every method on these tooling services.
- [DONE] T024: Add a public API contract suite that instantiates every public class, exercises every public constructor and interface method through its declared interface, validates every enum value and data field in a functional scenario, and proves that the package is usable without `InternalsVisibleTo` access.
- [DONE] T025: Add exhaustive public-layer behavioral coverage for null and invalid inputs, empty and maximum-sized values, Unicode and newline variants, strict-mode combinations, all syntax profiles, all escaping modes, buffered, streaming, and asynchronous rendering, composition, cancellation, reentrancy, and concurrent use, closing every uncovered functional branch without syntax-policy or performance tests.
