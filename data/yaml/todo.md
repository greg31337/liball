# Universal YAML Library Development Plan

## Goal

Build a universal YAML parsing, processing, and generating library in C# for .NET 10 using only built-in .NET libraries. The implementation must follow the simplified C# subset in `syntax.md`: file-scoped `namespace yaml;`, ordinary classes/enums/interfaces, only `static`, `public`, `internal`, and `const` modifiers, no properties, no class inheritance outside exceptions, no generic type or method declarations, no LINQ, no `try/catch`, and no external YAML dependencies. Public symbols define the supported API layer; implementation symbols are internal. Every public type, constructor, method, field, enum, enum member, and interface method has XML documentation using only the elements allowed by `syntax.md`.

The library should support three usage levels:

1. Low-level tokens and parsing events for tooling and diagnostics.
2. A mutable document/node model for inspection, transformation, comments, tags, anchors, and formatting choices.
3. High-level load/dump and object conversion APIs for common application use.

## Research Notes

Sources reviewed:

- YAML 1.2.2 specification: https://yaml.org/spec/1.2.2/
- YAML Test Matrix and YAML Test Suite notes: https://matrix.yaml.info/ and https://www.yaml.info/libraries/dev.html
- `yaml` JavaScript library documentation: https://eemeli.org/yaml/
- PyYAML documentation: https://pyyaml.org/wiki/PyYAMLDocumentation
- ruamel.yaml round-trip comment documentation: https://pypi.org/project/ruamel.yaml/0.10/
- js-yaml package documentation: https://www.npmjs.com/package/js-yaml
- go-yaml v3 node model source documentation: https://github.com/go-yaml/yaml/blob/v3/yaml.go
- YamlDotNet serializer/deserializer documentation: https://github-wiki-see.page/m/aaubry/YamlDotNet/wiki/Serialization.Serializer and https://github-wiki-see.page/m/aaubry/YamlDotNet/wiki/Serialization.Deserializer

Critical lessons from existing libraries:

- Mature libraries expose layered APIs. The `yaml` JavaScript package has simple parse/stringify calls, document APIs, and lexer/parser/composer layers.
- YAML 1.2 support should be the default. YAML 1.1 compatibility should be explicit because scalar resolution differs.
- The YAML Test Suite is essential for parser correctness. It currently targets YAML 1.2 and includes many edge cases across valid and invalid inputs.
- Round-trip editing matters for configuration files. ruamel.yaml and go-yaml preserve comments near nodes; the JavaScript `yaml` library preserves comments and blank lines.
- Node APIs should preserve source location, style, tag, anchor, comments, and collection order. go-yaml exposes node kind, style, tag, value, anchor, alias target, comments, line, and column.
- Schema handling is a must-have: failsafe, JSON, core, and optional YAML 1.1 behavior are common across js-yaml and the JavaScript `yaml` library.
- Anchors and aliases are core YAML graph features, including recursive structures.
- Duplicate mapping keys should be accepted at the syntax layer but controlled during composition/loading with configurable behavior.
- Object mapping should support naming conventions and custom converters, as YamlDotNet does, but this project must design those as classes and methods rather than interfaces.
- Emitter options should cover indentation, line width, scalar style, flow/block collection style, document markers, key ordering, and invalid-value handling.
- Security limits are necessary for untrusted input: maximum alias expansion, maximum depth, maximum input size, maximum scalar length, and duplicate-key policy.

## Development Principles

- Use no external runtime dependencies beyond built-in .NET libraries.
- Prefer explicit classes, fields, and ordinary methods that comply with `syntax.md`.
- Expose only supported API entry points as public; keep parsing state and implementation helpers internal.
- Document public API symbols in plain English, including behavior that is not obvious from the signature.
- Avoid implementation designs that require interfaces, inheritance, properties, records, extension methods, LINQ, or generic API declarations.
- Keep parsing separate from composition, schema resolution, loading, and emission.
- Preserve enough presentation detail for round-trip editing without requiring byte-identical formatting in the first release.
- Treat YAML documents as ordered graphs, not just dictionaries and arrays.
- Report diagnostics with line, column, index, severity, code, and message.
- Make every high-level API delegate to lower-level stages so tooling users can opt into the layer they need.

## Necessary Features From Low Level To High Level

### Character And Input Handling

- UTF-8 and UTF-16 string input through built-in .NET text APIs.
- BOM detection for byte input.
- Line break normalization for `\n`, `\r\n`, and `\r`.
- Unicode scalar validation and clear diagnostics for invalid control characters.
- Source position tracking by absolute index, line, and column.
- Configurable input size and scalar length limits.

### Scanner And Tokens

- Stream start and stream end tokens.
- Document start `---` and document end `...` tokens.
- Directive tokens for `%YAML`, `%TAG`, and reserved directives.
- Indentation tracking for block structure.
- Block sequence entry `-`.
- Block mapping key `?` and value `:`.
- Flow sequence delimiters `[`, `]`.
- Flow mapping delimiters `{`, `}`.
- Flow entry delimiter `,`.
- Comment tokens with before, inline, and following placement data.
- Whitespace and blank-line accounting for round-trip metadata.
- Tag and anchor property tokens.
- Alias tokens.
- Plain scalar tokens.
- Single-quoted scalar tokens.
- Double-quoted scalar tokens with escape validation.
- Literal block scalar `|` tokens.
- Folded block scalar `>` tokens.
- Block scalar indentation and chomping indicators.
- Error recovery tokens for editor/tooling scenarios.

### Parser And Events

- Event stream API similar to YAML processors: stream start/end, document start/end, sequence start/end, mapping start/end, scalar, alias, comment, and diagnostic events.
- Block sequence parsing.
- Block mapping parsing.
- Compact nested mapping parsing.
- Explicit mapping keys.
- Empty node parsing where supported.
- Flow sequence parsing.
- Flow mapping parsing.
- Flow scalar continuation and folding.
- Block scalar folding, literal preservation, indentation detection, and chomping.
- Directives parsing and validation.
- Tag handle expansion.
- Node properties parsing for tags and anchors.
- Clear errors for malformed indentation, malformed flow collections, invalid aliases, invalid escapes, repeated directives, and illegal reserved indicators.

### Composer And Document Model

- `YamlStream` containing ordered documents.
- `YamlDocument` containing directives, root node, diagnostics, and comments outside the root.
- Single `YamlNode` class with `Kind`, `Style`, `Tag`, `Value`, `Anchor`, `AliasName`, `AliasTarget`, `Children`, `Pairs`, source span, and comment fields.
- `YamlPair` class for ordered key/value pairs with source span and comments.
- `YamlDirective` class for YAML and TAG directives.
- `YamlDiagnostic` class for errors and warnings.
- `YamlSourceSpan` class for start/end index and line/column.
- Ordered mapping support.
- Duplicate key detection with configurable policy.
- Complex key support at the node layer.
- Anchor table per document.
- Alias resolution including recursive graph support.
- Merge key `<<` support behind schema/options.
- Unknown/local tag preservation.
- Comment and blank-line attachment to nearby nodes.
- Node cloning, traversal, lookup, add, remove, replace, and path navigation methods.

### Schemas And Scalar Resolution

- Failsafe schema: strings, sequences, mappings.
- JSON schema: JSON-compatible null, bool, int, float, string, sequence, mapping.
- YAML 1.2 core schema as default.
- Optional YAML 1.1 schema compatibility.
- Null resolution for `null`, `Null`, `NULL`, and `~` according to selected schema.
- Boolean resolution according to selected schema.
- Integer resolution including decimal, hex, octal where schema permits.
- Floating-point resolution including exponent, infinity, and NaN where schema permits.
- String fallback for unknown plain scalars.
- Timestamp support as optional YAML 1.1/schema feature using `DateTimeOffset`.
- Binary support as optional YAML 1.1/schema feature using `Convert.FromBase64String`.
- Set, ordered map, and pairs tags as node-level features first, with optional high-level conversion later.
- Custom tag resolvers registered through option objects.
- Custom scalar converters registered through option objects.

### Processing APIs

- Query node by path.
- Create scalar, sequence, mapping, alias, and pair nodes.
- Add, insert, update, and remove sequence items and mapping pairs.
- Preserve mapping order.
- Choose scalar and collection styles.
- Attach or update before, inline, and after comments.
- Normalize tags.
- Rename anchors.
- Resolve aliases into graph references or keep alias nodes.
- Convert document model to JSON-compatible values.
- Convert JSON-compatible values to document model.
- Validate document against configurable limits and schema rules.

### Emitter And Generator

- Emit YAML streams with one or more documents.
- Emit optional document start and end markers.
- Emit block style sequences and mappings.
- Emit flow style sequences and mappings.
- Emit plain, single-quoted, double-quoted, literal, and folded scalars.
- Emit comments and blank lines from node metadata.
- Emit tags, anchors, aliases, YAML directives, and TAG directives.
- Enforce configurable indentation.
- Enforce configurable line width for wrapping/folding where practical.
- Preserve requested scalar style where legal.
- Choose safe fallback quoting for ambiguous or invalid plain scalars.
- Stable output for deterministic tests.
- Optional sorted mapping output for string keys.
- Safe cycle handling through anchors and aliases.

### High-Level Loading And Dumping

- Parse one YAML document into a node model.
- Parse all YAML documents from a stream.
- Load one YAML document into built-in .NET values.
- Load all documents into built-in .NET values.
- Dump built-in .NET values to YAML.
- Serialize public fields of ordinary objects through reflection.
- Deserialize into objects with public fields and parameterless constructors.
- Support dictionaries, lists, arrays, primitive numeric types, strings, bools, null values, enums, `DateTime`, `DateTimeOffset`, `Guid`, and byte arrays.
- Naming convention support for object fields: exact, camelCase, PascalCase, snake_case, kebab-case.
- Ignore/null/default handling options.
- Unknown member handling options.
- Required member validation using option-provided field name lists rather than attributes.
- Custom converter classes for specialized types.
- Type tag mapping through option objects.
- Culture-invariant formatting and parsing.

### Diagnostics, Safety, And Tooling

- Structured diagnostics instead of raw exception-only behavior.
- Exception-throwing convenience APIs for application code.
- Non-throwing `TryParse`, `TryLoad`, and `TryDump` result APIs.
- Recursion depth limits.
- Alias expansion limits.
- Duplicate key policy: error, warn, first wins, last wins, preserve all.
- Unknown tag policy: preserve, warn, error.
- Invalid scalar policy during dump: error or quote as string.
- Optional parser recovery for editors.
- Compatibility test runner for YAML Test Suite data checked into tests later.
- Golden output tests for emitter stability.
- Fuzz-style randomized tests using built-in random generation.

## Remaining Public API Tasks

- [DONE] T001: Add byte-oriented scanning, parsing, and loading entry points to the public pipeline and processor contracts, using `ReadOnlySpan<byte>` where appropriate and built-in .NET decoding to support UTF-8 and UTF-16 BOM detection, invalid-encoding diagnostics, source positions, and configured input limits.
- [DONE] T002: Carry YAML and TAG directives through distinct public parser events into `YamlDocument.Directives`, including reserved-directive warnings, repeated-directive errors, version validation, tag-handle expansion, and re-emission.
- [DONE] T003: Complete the public parser behavior for the known unsupported YAML 1.2 cases, including missing mapping keys, nested implicit complex keys, multiline flow keys without values, and global tag handles, and address every additional grammar gap found by the pinned conformance suite.
- [DONE] T004: Add public trivia metadata that distinguishes blank lines and before, inline, and following comments on events, documents, nodes, and pairs, and preserve those placements through parse, edit, clone, and emit operations.
- [DONE] T005: Expand `IYamlModel` and `YamlModel` with public traversal and mutation operations for sequence items, ordered mapping pairs, mapping values, parsed paths, styles, tags, and comments so callers do not need internal helpers to perform the planned edits.
- [DONE] T006: Expand `IYamlModel` and `YamlModel` with public anchor rename, alias rename, alias retarget, and alias-resolution operations that update document anchor tables and safely handle shared or recursive graphs.
- [DONE] T007: Add a public non-throwing validation operation for streams, documents, and node graphs that returns structured diagnostics for schema rules, duplicate keys, unknown tags, depth, aliases, scalar length, collection size, and related configured limits.
- [DONE] T008: Add an emitter invalid-value policy to the public enums and options and enforce error-versus-safe-quoting behavior while retaining deterministic output and safe cycle handling.
- [DONE] T009: Integrate `YamlScalarConverterRegistry` with schema resolution, loading, dumping, serialization, and deserialization so registered scalar converters and their preferred tags and styles affect every applicable public operation.
- [DONE] T010: Implement `YamlObjectOptions.UseTypeTags` and public CLR-type-to-YAML-tag mappings end to end for nested object serialization and deserialization, including unknown-tag policy interaction and custom type converters.
- [DONE] T011: Make the public throwing processor operations report processing and conversion failures consistently as `YamlException`, and make the public `TryParse`, `TryParseAll`, `TryLoad`, and `TryDump` paths return diagnostics for every expected failure without partially successful values being mistaken for success.
- [DONE] T012: Add direct public-contract tests for every `IYamlPipeline` operation and its concrete implementation, covering tokenization, event ordering, composition, directives, schema resolution, validation, emission, diagnostics, null inputs, malformed inputs, recovery, and byte/BOM inputs.
- [DONE] T013: Add direct public-contract tests for every `IYamlModel` operation and its concrete implementation, covering construction, traversal, all sequence and mapping edits, parsed paths, cloning, trivia, styles, tags, anchors, aliases, merge keys, JSON-compatible conversion, naming, graph cycles, and invalid requests.
- [DONE] T014: Add direct public-contract tests for `IYamlProcessorFactory`, `IYamlProcessor`, and their implementations, covering configuration isolation, every parse/load/dump/serialize/deserialize and non-throwing path, all schema and policy choices, built-in CLR values, reflection-based objects, naming rules, required and unknown members, type tags, and custom converters.
- [DONE] T015: Add public API data and option tests that cover constructor defaults, caller mutation, independent option copies, every enum value and policy branch, result and diagnostic population, source spans, tokens, events, directives, documents, nodes, pairs, registries, standard tags, and `YamlException` behavior.
- [DONE] T016: Replace the small YAML Test Suite subset with parameterized tests over every case in the pinned `data-2022-01-17` archive, checking public scanner tokens, parser events, JSON-compatible loaded values, emitter fixtures where supplied, invalid-input diagnostics, and an empty unsupported-case list.
- [DONE] T017: Add deterministic randomized tests through public APIs for generated scalar and collection graphs, parse-emit-parse equivalence, editor recovery, malformed input, aliases and cycles, and security limits, recording any minimized failure as a fixed regression case.

## Remove YamlDotNet Dependency

- [DONE] T018: Inventory the YamlDotNet-backed behavior in `YamlSourceParser` and its callers, including event translation, source normalization, special-case grammar repairs, scalar corrections, and failure handling. Turn each required behavior into a regression case using the checked-in YAML specification and pinned test-suite fixtures as the authority; preserve the existing public API and follow `AGENTS.md` for all new implementation code.
- [DONE] T019: Complete the built-in `YamlScanner` tokenization needed by the replacement parser, including indentation and flow context, simple-key detection, indicator separation, document boundaries, Unicode validation, and accurate source spans. Preserve comments and blank-line trivia without treating scalar content as structure.
- [DONE] T020: Complete built-in scalar parsing for plain, single-quoted, double-quoted, literal, and folded styles, including multiline folding, escape validation, explicit and inferred block indentation, chomping, trailing blank lines, and empty scalars. Replace the source parser's text rewrites and scalar-value corrections with general grammar rules and focused regressions.
- [DONE] T021: Complete the built-in block collection parser for nested and compact sequences and mappings, indentless sequences, explicit and implicit complex keys, missing keys and values, and empty nodes. Validate indentation and simple-key restrictions while producing correctly ordered events with collection styles and source spans.
- [DONE] T022: Complete the built-in flow collection parser for nested sequences and mappings, implicit mappings inside sequences, explicit and complex keys, empty keys and values, multiline content, and trailing separators. Reject malformed delimiters, separators, and implicit keys according to the YAML grammar, covering the cases currently handled by `ParseCompactEmptyMapping` and other special cases.
- [DONE] T023: Integrate built-in stream and document parsing with `YamlDirectiveParser`, preserving explicit and implicit document markers, YAML version handling, TAG handle expansion, local and verbatim tags, anchors, aliases, and per-document state resets. Preserve tag presentation and trivia for composition and re-emission, and diagnose repeated directives, invalid properties, and undeclared tag handles.
- [DONE] T024: Implement synchronous structured parser diagnostics and bounded recovery for expected malformed input without YamlDotNet exceptions or the `YamlSourceParser` task-based exception bridge. Preserve throwing and non-throwing public behavior, meaningful diagnostic codes and source locations, configured recovery and diagnostic limits, and input, scalar, nesting, collection, and alias security limits at the applicable processing stages.
- [DONE] T025: Route `ParseScannerResult`, token parsing, byte parsing, and all processor parse/load/deserialize operations through the completed built-in scanner and parser. Verify equivalent events for equivalent source and token inputs, preserve scanner diagnostics and metadata, and remove the YamlDotNet event adapters and obsolete fallback, normalization, and special-case paths from `YamlSourceParser`.
- [DONE] T026: Validate the replacement through the full pinned `data-2022-01-17` YAML Test Suite with no unsupported-case exclusions, checking scanner behavior, parser event fixtures, invalid-input diagnostics, loaded values, and emitter round trips. Exercise both source-backed scanner results and standalone token parsing so conformance cannot be satisfied by bypassing the built-in token parser; run the existing public-contract, byte-input, trivia, schema, converter, graph, security, and deterministic randomized regressions.
- [DONE] T027: Remove the YamlDotNet `PackageReference` from `src/yaml.csproj` and any remaining code or build references, using only built-in .NET libraries for the replacement. Verify a clean restore, build, full test run, and applicable C# syntax/API policy checks; inspect resolved dependencies and build/package output to confirm YamlDotNet is absent. Update runtime-dependency documentation while retaining historical comparison material in `specs/`.
