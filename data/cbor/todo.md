# Universal CBOR and CDDL library development plan

## Goal and conformance baseline

Build a dependency-free-under-`src/` .NET library that handles CBOR from raw binary tokens through high-level object and schema workflows. The normative baseline is RFC 8949 (CBOR) and RFC 8610 (CDDL), including the corrections and compatible extensions in RFC 9682. The plan also includes RFC 8742 CBOR Sequences, RFC 9165 additional CDDL controls, and RFC 9741 CDDL text controls. The downloaded copies and research snapshots are indexed in `specs/SOURCES.md`.

The library will use only built-in .NET APIs in `src/`. `Span<byte>`, `ReadOnlySpan<byte>`, `Memory<byte>`, `ReadOnlyMemory<byte>`, `IBufferWriter<byte>`, `Stream`, `BigInteger`, `Half`, and `System.Text.Json` should be used where they fit the API and the syntax policy. External packages may be used only by `tests/` for independent interoperability checks.

RFC requirements take priority over behavior seen in other libraries. Extensions not standardized by the listed RFCs are exposed through registries and interfaces rather than embedded as mandatory dialect syntax. In particular, unpublished CDDL module/CSV/freezer drafts are not part of the conformance claim.

## Constraints that shape the design

- Follow `syntax.md` even when normal modern C# would be shorter. Source will use file-scoped namespaces, non-generic classes, fields instead of properties, block-bodied methods, positional arguments, and no overloads.
- Represent polymorphic CBOR values and CDDL syntax with flat, discriminated data classes. Do not use inheritance, records, structs, tuples, custom generic types, nullable value types, or `IEquatable`.
- Keep every public type under `src/Api` and in its required category. Keep all implementation helpers internal and outside `src/Api`.
- Give each public implementation class one public constructor and only the instance methods declared by its interface. No public static methods are planned.
- Use signed `int`/`long` for counts, offsets, and limits. Use `uint`/`ulong` only for encoded bit patterns, CBOR arguments, simple values, and tag numbers.
- Use exceptions for exceptional failures. Incomplete streaming input, destination-too-small, parse diagnostics, and ordinary validation mismatches receive result/status objects because they are expected outcomes.
- Preserve arbitrary map keys, map order, duplicate keys, unknown simple values, unknown tags, float width/NaN bits, and definite/indefinite representation metadata. Automatic native-object mapping is an optional high layer and must not weaken the generic model.

## Findings from existing libraries

| Library | Features worth carrying into this plan | Limitation or lesson addressed here |
| --- | --- | --- |
| libcbor | Layered DOM and streaming APIs, all major types, indefinite items, UTF-8 checks, incremental callbacks, no runtime dependencies | Keep low-level token processing usable without constructing a DOM, while also offering safe high-level classes. |
| TinyCBOR | Direct buffer encoding, pull parsing, container traversal, validation modes, CBOR-to-text and CBOR-to-JSON conversion | Make buffer exhaustion an ordinary status and verify container counts/state so callers cannot silently emit malformed data. |
| Python cbor2 | File-like streaming, canonical encoding, semantic/default/tag hooks, string/value sharing, cyclic references, generic unknown-tag handling | Preserve sharing tags and cycles deliberately; keep hooks interface-based because delegates and source attributes are unavailable. |
| fxamacker/cbor | RFC 8949 well-formedness, preferred and deterministic modes, CBOR Sequences, extended diagnostic notation, duplicate-key policies, tag validity, security limits, reusable configurations | Provide both core and length-first deterministic profiles, exact limit controls, first-item/remaining-input results, and strict tag policies. |
| cddl-cat | CDDL AST-to-intermediate-tree compilation, representation-neutral validation, lookup contexts, CBOR and JSON validation | Cover the features its survey marks missing: rule extensions, sockets/plugs, all controls, tags/representation types, hexfloat, tagged prelude types, and float-width validation. |
| anweiss/cddl | Full grammar/update/control coverage, source spans, CBOR/JSON validation, feature selection, schema sample generation | Avoid known greedy occurrence shortcuts; use bounded exhaustive group matching with deterministic diagnostics. |

## Complete feature inventory

### 1. CBOR binary core and generic data model

- All eight major-type spaces: unsigned integers, negative integers, byte strings, text strings, arrays, maps, tags, floats/simple values.
- Full unsigned 64-bit CBOR argument range and negative range through `-1 - UInt64.MaxValue`, represented with `BigInteger` when CLR primitives are insufficient.
- Definite-length and indefinite-length byte/text strings, arrays, and maps; valid chunks; break handling; nesting; and correct map pair accounting.
- Half-, single-, and double-precision floating point, positive/negative zero, infinities, quiet/signaling NaNs, preserved float width and raw NaN payload bits.
- Boolean, null, undefined, unassigned simple values 0 through 255, and rejection of non-well-formed two-byte encodings for simple values below 32.
- A mutable generic `CborValue` tree/graph with ordered `CborMapEntry` lists so non-string keys, duplicate keys, and original order survive decoding.
- Optional representation metadata for integer/length widths, indefinite forms and chunks, float width/bits, and encoded source range. A preserve-representation encoder validates metadata rather than blindly copying stale bytes.
- Structural equality and map-key equivalence according to the selected CBOR data model, including numeric, tagged, array, and map keys; cycle-aware comparison for shared values.

### 2. Decoding, encoding, and deterministic serialization

- Exact-one-item decode, first-item decode with byte count/remaining data, empty/multiple-item sequence decode, and skip-without-materialization.
- Low-level incremental token reader accepting fragmented `ReadOnlySpan<byte>` input and reporting `Done`, `NeedMoreData`, or an error with absolute byte offset and path.
- Low-level stateful token writer targeting `Span<byte>`/`IBufferWriter<byte>`, with definite-container count checks and legal indefinite-container transitions.
- High-level DOM encoder/decoder and measurement API; no partial high-level value is returned after an exceptional failure.
- Synchronous `Stream` reader/writer for back-to-back items, retaining over-read bytes correctly and supporting non-seekable streams.
- RFC 8742 CBOR Sequences, including empty sequences, concatenated items, item offsets, and sequence diagnostic notation.
- Preferred serialization: shortest integer and length heads and optional shortest exactly preserving float width.
- RFC 8949 core deterministic encoding: shortest arguments, no indefinite lengths, shortest exact floats, canonical NaN, and lexicographic ordering by deterministic encoded map keys.
- RFC 8949 length-first core deterministic encoding: encoded-key length followed by lexical bytes.
- Deterministic-conformance checking that identifies the first non-minimal head, forbidden indefinite form, non-preferred float/NaN, or out-of-order/duplicate map key.
- Configurable policies for trailing bytes, duplicate keys, invalid UTF-8, indefinite items, tags, simple values, maximum nesting, item count, collection size, string bytes, total bytes, and retained source bytes.
- Complete malformed-input handling: reserved additional information 28–30, illegal indefinite major types, stray/missing break, wrong chunk type, nested indefinite string chunks, odd maps, truncation at every head/payload position, length overflow, and resource-limit exhaustion.

### 3. Tags and extended data models

- Lossless nested representation of every tag number, including unknown and future IANA tags.
- Pluggable tag registry and interface-based handlers for validation, native conversion, and encoding, with explicit unknown-tag and forbidden-tag policies.
- Built-in handling for RFC 8949 tags 0/1 (date/time), 2/3 (bignums), 4/5 (decimal fractions/bigfloats), 21/22/23 (expected base encoding), 24 (embedded CBOR), 32–36 (URI/base text/regular expression/MIME hints), and 55799 (self-described CBOR).
- Configurable validation of admissible tag content types and values; invalid known tags can be rejected or preserved as generic tags.
- IANA tag metadata snapshot exposed as static data where useful, while semantic behavior remains registry-driven so updates do not require changing the data model.
- Serialization-order-aware support for registered string-reference namespaces/references (tags 256/25) and shared values/references (tags 28/29), including forward-reference rejection, cycles, disabled/preserve/resolve policies, and deterministic restrictions.
- Recursive decoding limits for embedded CBOR and tag handlers so tags cannot bypass global resource controls.

### 4. Diagnostic notation, JSON, and native-object mapping

- RFC 8949 diagnostic formatter for every generic value, including arbitrary keys, tags, simple values, bytes, non-finite floats, and representation indicators.
- RFC 8610 Appendix G extended diagnostic parser and formatter: whitespace in byte strings, text/hex/base64 byte strings, embedded CBOR `<< >>`, embedded sequences, concatenated strings, hexadecimal/octal/binary integers, comments, and encoding indicators.
- Compact and indented formatting, deterministic map display, original-representation display, sequence input/output, escaping, and stable error locations.
- RFC 8949 CBOR↔JSON conversion with explicit policies for byte strings, non-text map keys, tags, big integers, undefined, NaN/infinities, duplicate keys, and irreversible conversions. JSON parsing uses built-in `System.Text.Json` only.
- Optional object mapping for CLR scalar types, `BigInteger`, `Half`, decimal, strings, byte memory, date/time, URI, GUID, enums, arrays, built-in collection interfaces, and field/property-based objects.
- Object mapping by map or positional array, configurable naming/key policy, null handling, required/unknown members, integer overflow, duplicate members, and shared/cyclic object handling.
- Interface-based custom type adapters; no library-defined attributes, delegates, generic methods, or generated runtime code are required.

### 5. CBOR validation and observability

- Separate well-formedness, basic validity, tag validity, deterministic-profile, and application-policy validation levels.
- Validation directly over encoded bytes through tokens when a DOM is unnecessary, and over `CborValue` for application-created values.
- Structured issues containing severity/code, byte offset, item path, and explanatory text; fail-fast and collect-many modes with an issue cap.
- Paths that distinguish array indices, map key/value positions, tags, and embedded payloads without assuming keys are strings.
- Operation objects are reusable after successful completion or reset, but become predictably faulted after unrecoverable state misuse; no shared mutable global state.

### 6. CDDL lexical analysis, parsing, and AST

- RFC 8610 grammar as corrected/replaced by RFC 9682, including empty data models and the updated text/byte-string literal grammar.
- Source locations as UTF-16 index plus line/column, complete token spans, comments/trivia retention option, Unicode identifiers, escape validation, and multiple recoverable diagnostics.
- Integer, negative integer, decimal float, hexadecimal float, text, UTF-8 byte, hexadecimal byte, and base64url byte literals.
- Type and group rules; type choices `/`; group choices `//`; type/group extensions `/=` and `//=`; root rule selection.
- Arrays, maps, groups, parenthesized types/groups, named/bareword keys, `=>`, `:`, map cut `^`, occurrences `?`, `*`, `+`, and `n*m`.
- Inclusive `..` and exclusive-upper `...` ranges, unwrapping `~`, group-to-choice enumeration `&`, tags and representation types `#major.argument`, including RFC 9682 non-literal tag/simple arguments.
- Type/group sockets `$`/`$$`, plugs, generic parameters/arguments, nested generic instantiation, and RFC operator precedence.
- Complete standard prelude, including representation-width types and tag-derived types; source rules remain distinguishable from prelude rules.
- Public flat AST plus an internal normalized intermediate schema so syntax fidelity and efficient validation do not conflict.

### 7. CDDL semantic analysis and compilation

- Rule table construction, duplicate definition checks, compatible extension merging, socket/plug resolution, undefined-name diagnostics, type-vs-group context checks, and root selection.
- Generic arity checking, capture-avoiding substitution into normalized nodes, recursive instantiation detection, and memoized instantiated rules.
- Reference graphs with legal recursive schemas, rejection/reporting of non-productive cycles, and bounded runtime recursion.
- Literal/range/control operand type checking, occurrence-bound checks, map-label checks, cut placement, unwrap target checks, and representation-type legality.
- A configurable external rule resolver for application-provided preludes/schema sets without adopting a nonstandard module syntax.
- Immutable-by-convention compiled schema objects safe for concurrent validation when callers stop mutating their public fields.

### 8. CDDL validation

- Validation of `CborValue`, encoded CBOR, CBOR Sequence payloads used by `.cborseq`, and the RFC 8610 Appendix E JSON subset.
- Exact validation of prelude types, literal values, integer/float representation widths, tagged types, major types, ranges, arrays, maps, tables, structs, and arbitrary map keys.
- Ordered PEG-style array/group matching and unordered map matching with bounded backtracking, correct occurrences, choice selection, cuts, group enumeration, and no greedy shortcut that changes language semantics.
- Type/group rule references, extensions, sockets/plugs, generic instantiations, unwrap, recursive rules, and feature selection.
- All RFC 8610 controls: `.size`, `.bits`, `.regexp`, `.cbor`, `.cborseq`, `.within`, `.and`, `.lt`, `.le`, `.gt`, `.ge`, `.eq`, `.ne`, and `.default`.
- All RFC 9165 controls: `.plus`, `.cat`, `.det`, `.abnf`, `.abnfb`, and `.feature`, including RFC 5234/RFC 7405 ABNF parsing and matching.
- All RFC 9741 controls: `.b64u`, `.b64u-sloppy`, `.b64c`, `.b64c-sloppy`, `.b32`, `.h32`, `.b45`, `.hex`, `.hexlc`, `.hexuc`, `.base10`, `.printf`, `.json`, and `.join`.
- XML Schema regular-expression semantics required by `.regexp`, rather than silently substituting incompatible .NET regex semantics.
- Custom control registry with interface-based validate/generate behavior and explicit unknown-control policy.
- Precise mismatch reports with instance path, schema source span, failing rule/control, expected-versus-actual summary, selected feature, and bounded nested-choice details.
- Validation budgets for depth, steps, alternatives, memo entries, decoded embedded bytes, regex/ABNF states, issues, and elapsed cancellation checks supplied synchronously by options/hook state.

### 9. Schema-driven generation and integration hooks

- Seeded sample generation from any selected productive root rule, covering literals, ranges, choices, occurrences, groups, maps, tags, generics, and recursive schemas within configurable depth/size limits.
- Generated values are validated against the compiled schema before being returned; inability to construct a value produces a structured generation failure rather than invalid output.
- Generation strategies for boundary/minimal/representative values and deterministic choice selection from a seed.
- `ICddlGenerationHook` can supply values for named rules, semantic tags, custom controls, or application-specific types and receives source/rule context plus remaining budgets.
- A schema traversal/emission pipeline sends document/rule/node events to an `ICddlSchemaSink`, enabling downstream code generation without a dependency on a code-model or templating library.
- Hook/sink exceptions are wrapped with rule and source context, and reentrancy/state rules are defined by operation objects rather than global state.

### 10. Security and correctness expectations

- Arithmetic is explicit and overflow-safe despite project checked arithmetic; length and offset conversions are validated before allocation or slicing.
- Default limits reject resource-exhaustion inputs early; callers can raise limits deliberately. No declared encoded length causes a proportional allocation before policy checks.
- Incremental UTF-8, base encodings, diagnostic strings, CDDL strings, ABNF, regex, and JSON are bounded and reject malformed scalar sequences.
- Duplicate-key detection uses CBOR equivalence, not CLR dictionary equality, and deterministic encoding rejects equal encoded keys.
- Deep structures use explicit stacks where recursion could exhaust the CLR stack, within the syntax policy.
- Tests cover all public API behavior, every standards example, malformed classes, fragmented-input boundaries, deterministic byte identity, limits, hooks, and error metadata.

## Proposed public API and file organization

The signatures below are directional rather than final fine detail. Every method is an instance method. Method names are unique within each interface and within every implementation's combined interface set.

### `src/Api/Data`

| Planned file/type | Public field groups and purpose |
| --- | --- |
| `CborValue.cs` | Discriminant plus integer, byte/text, array, map, tag, float, simple, sharing, and representation fields for the complete generic model. |
| `CborMapEntry.cs` | Key and value fields; lists preserve order and duplicates. |
| `CborFloatValue.cs` | Numeric value, exact width, and raw bits/NaN metadata. |
| `CborRepresentation.cs` | Original head width, indefinite/chunk representation, float representation, and source-offset metadata. |
| `CborToken.cs` | Low-level token kind, argument, payload segment, container length, and representation data. |
| `CborReadResult.cs`, `CborTokenReadResult.cs`, `CborWriteResult.cs` | Status, value/token, consumed/written/required counts, and structured failure fields. |
| `CborDecodeOptions.cs`, `CborEncodeOptions.cs` | Limits, validity policies, tag/sharing behavior, float policy, deterministic profile, and representation policy. |
| `CborDiagnosticOptions.cs`, `CborJsonOptions.cs`, `CborMappingOptions.cs` | Text layout/EDN, lossy JSON policy, and native-object mapping policy. |
| `CborValidationResult.cs`, `CborValidationIssue.cs` | Validity outcome and bounded issue collection with path/offset/code. |
| `CborTagRegistration.cs`, `CborTagResult.cs` | Tag number, handler, metadata, and handler outcome. |
| `CborTypeRegistration.cs`, `CborMappingContext.cs` | Native type adapter registration and current mapping/reference context. |
| `CddlSourceSpan.cs`, `CddlDiagnostic.cs` | Source positions and parser/compiler diagnostics. |
| `CddlNode.cs`, `CddlRule.cs`, `CddlDocument.cs` | Flat source AST for expressions/groups and ordered rules/trivia. |
| `CddlSchema.cs` | Compiled rule table, root, normalized nodes, source mapping, and registries. |
| `CddlParseOptions.cs`, `CddlParseResult.cs`, `CddlCompileOptions.cs`, `CddlCompileResult.cs` | Recovery/retention/prelude options and exception-less parse/compile results. |
| `CddlValidationOptions.cs`, `CddlValidationResult.cs`, `CddlValidationIssue.cs` | Root/features/budgets/reporting controls and schema mismatch details. |
| `CddlGenerationOptions.cs`, `CddlGenerationContext.cs`, `CddlGenerationResult.cs` | Seed/strategy/budgets/hooks and generated value or failure. |
| `CddlControlRegistration.cs`, `CddlControlResult.cs` | Custom control name, handler, and evaluation/generation result. |
| `CddlEmissionOptions.cs`, `CddlEmissionContext.cs` | Schema traversal selection, naming state, and source context. |

### `src/Api/Enums`

- `CborValueKind.cs`, `CborTokenKind.cs`, `CborFloatWidth.cs`, `CborLengthKind.cs`, `CborReadStatus.cs`, and `CborWriteStatus.cs` describe the binary/model surface.
- `CborEncodingProfile.cs`, `CborMapKeyOrder.cs`, `CborDuplicateKeyPolicy.cs`, `CborTagPolicy.cs`, `CborSharedValuePolicy.cs`, `CborValidationLevel.cs`, `CborIssueSeverity.cs`, `CborDiagnosticStyle.cs`, and `CborJsonPolicy.cs` configure behavior and appear in options/results.
- `CddlNodeKind.cs`, `CddlRuleKind.cs`, `CddlLiteralKind.cs`, `CddlOccurrenceKind.cs`, `CddlDiagnosticSeverity.cs`, `CddlValidationCode.cs`, `CddlGenerationStrategy.cs`, and `CddlControlStatus.cs` describe AST, diagnostics, validation, and generation.

### `src/Api/Contracts`

| Planned interface | Proposed operations |
| --- | --- |
| `ICborDecoder.cs` | `Decode` one item and `DecodeFirst` with consumption metadata. |
| `ICborEncoder.cs` | `Encode`, `EncodeInto`, and `Measure`. |
| `ICborTokenReader.cs` | Incremental `ReadToken`, `SkipValue`, and `ResetReader`. |
| `ICborTokenWriter.cs` | `WriteToken`, `CompleteWriter`, and `ResetWriter`. |
| `ICborStreamReader.cs` | `ReadNext` item from a constructor-supplied stream. |
| `ICborStreamWriter.cs` | `WriteNext` and `FlushOutput` to a constructor-supplied stream. |
| `ICborValidator.cs` | `ValidateEncoding` and `ValidateValue`. |
| `ICborDiagnosticConverter.cs` | `FormatDiagnostic` and `ParseDiagnostic`. |
| `ICborJsonConverter.cs` | `ConvertToJson` and `ConvertFromJson`. |
| `ICborObjectMapper.cs` | `MapToValue` and `MapFromValue` using explicit runtime types/options. |
| `ICborTypeAdapter.cs` | `SupportsType`, `EncodeObject`, and `DecodeObject`. |
| `ICborTagHandler.cs` | `ValidateTag`, `DecodeTag`, and `EncodeTag`. |
| `ICborTagRegistry.cs` | `AddTag` and `FindTag`. |
| `ICddlParser.cs` | Throwing `ParseDocument` and diagnostic `TryParseDocument`. |
| `ICddlCompiler.cs` | Throwing `CompileSchema` and diagnostic `TryCompileSchema`. |
| `ICddlValidator.cs`, `ICddlJsonValidator.cs` | `ValidateCbor` and `ValidateJson`, respectively. |
| `ICddlRuleResolver.cs` | `ResolveRule` for external schema/prelude lookup. |
| `ICddlControlHandler.cs`, `ICddlControlRegistry.cs` | `EvaluateControl`/`GenerateControl` and `AddControl`/`FindControl`. |
| `ICddlValueGenerator.cs` | `GenerateValue` for a compiled root/rule. |
| `ICddlGenerationHook.cs` | `BeginGeneration`, `GenerateNode`, and `EndGeneration`. |
| `ICddlSchemaEmitter.cs`, `ICddlSchemaSink.cs` | `EmitSchema` and begin/accept/end traversal events for downstream generators. |

### `src/Api/Static`

- `CborTagNumbers.cs` contains constants for standardized/core and serialization-order-aware tags used by built-in behavior.
- `CborSimpleValues.cs` contains boolean/null/undefined and reserved simple-value constants.
- `CborMediaTypes.cs` contains `application/cbor`, `application/cbor-seq`, and structured suffix constants.
- `CddlPrelude.cs` contains the RFC prelude text/data only; parsing remains an instance operation.
- `CddlControlNames.cs` contains registered control-operator names from RFCs 8610, 9165, and 9741.

### `src/Api/Exceptions`

Each exception derives directly from `System.Exception`: `CborDecodeException.cs`, `CborEncodeException.cs`, `CborValidationException.cs`, `CborDiagnosticException.cs`, `CborMappingException.cs`, `CddlParseException.cs`, `CddlCompileException.cs`, `CddlValidationException.cs`, and `CddlGenerationException.cs`.

### `src/Api/Classes`

- `CborDecoder.cs`, `CborEncoder.cs`, `CborTokenReader.cs`, `CborTokenWriter.cs`, `CborStreamReader.cs`, and `CborStreamWriter.cs` implement the matching binary contracts.
- `CborValidator.cs`, `CborDiagnosticConverter.cs`, `CborJsonConverter.cs`, `CborObjectMapper.cs`, and `CborTagRegistry.cs` implement high-level operations/registries.
- `CddlParser.cs`, `CddlCompiler.cs`, `CddlValidator.cs`, `CddlJsonValidator.cs`, `CddlControlRegistry.cs`, `CddlValueGenerator.cs`, and `CddlSchemaEmitter.cs` implement schema operations.

### Internal implementation files outside `src/Api`

- `src/Binary/`: `CborBinaryPrimitives.cs`, `CborHalfPrecision.cs`, `CborUtf8.cs`, `CborArgumentReader.cs`, `CborArgumentWriter.cs`, `CborKeyComparer.cs`, `CborValueComparer.cs`, `CborLimitState.cs`.
- `src/Encoding/`: `CborEncoderCore.cs`, `CborTokenWriterCore.cs`, `CborSizeCalculator.cs`, `CborDeterministicEncoder.cs`, `CborRepresentationEncoder.cs`, `CborSharingEncoder.cs`.
- `src/Decoding/`: `CborDecoderCore.cs`, `CborTokenizerCore.cs`, `CborDomBuilder.cs`, `CborSkipState.cs`, `CborSequenceState.cs`, `CborSharingDecoder.cs`.
- `src/Tags/`: `CborBuiltInTags.cs`, `CborDateTimeTags.cs`, `CborBigNumberTags.cs`, `CborEmbeddedTag.cs`, `CborReferenceTags.cs`, `CborTagValidator.cs`.
- `src/Diagnostic/`: `CborDiagnosticLexer.cs`, `CborDiagnosticParserCore.cs`, `CborDiagnosticFormatterCore.cs`, `CborDiagnosticEscapes.cs`.
- `src/Json/`: `CborJsonReader.cs`, `CborJsonWriter.cs`, `CborJsonPolicyEngine.cs`.
- `src/Mapping/`: `CborObjectMapperCore.cs`, `CborBuiltInTypeAdapters.cs`, `CborReflectionMap.cs`, `CborObjectReferenceState.cs`.
- `src/Cddl/Parsing/`: `CddlLexer.cs`, `CddlToken.cs`, `CddlParserCore.cs`, `CddlStringParser.cs`, `CddlNumberParser.cs`, `CddlRecovery.cs`.
- `src/Cddl/Compilation/`: `CddlPreludeLoader.cs`, `CddlRuleTable.cs`, `CddlNormalizer.cs`, `CddlExtensionMerger.cs`, `CddlGenericExpander.cs`, `CddlReferenceGraph.cs`, `CddlSemanticChecker.cs`.
- `src/Cddl/Validation/`: `CddlValidationEngine.cs`, `CddlMatchState.cs`, `CddlTypeMatcher.cs`, `CddlArrayMatcher.cs`, `CddlMapMatcher.cs`, `CddlGroupMatcher.cs`, `CddlMemoTable.cs`, `CddlEquality.cs`.
- `src/Cddl/Controls/`: `CddlCoreControls.cs`, `CddlAdditionalControls.cs`, `CddlTextControls.cs`, `CddlAbnfParser.cs`, `CddlAbnfMatcher.cs`, `CddlXsdRegexParser.cs`, `CddlXsdRegexMatcher.cs`, `CddlBaseEncoding.cs`, `CddlPrintfMatcher.cs`.
- `src/Cddl/Generation/`: `CddlValueGeneratorCore.cs`, `CddlGenerationState.cs`, `CddlControlGenerator.cs`, `CddlSchemaEmitterCore.cs`.

## Planned test files

Tests may use external packages, reference implementations, and downloaded vectors. Planned suites are `tests/BinaryPrimitiveTests.cs`, `tests/CborTokenReaderTests.cs`, `tests/CborTokenWriterTests.cs`, `tests/CborDecoderTests.cs`, `tests/CborEncoderTests.cs`, `tests/CborMalformedInputTests.cs`, `tests/CborIndefiniteLengthTests.cs`, `tests/CborFloatTests.cs`, `tests/CborDeterministicTests.cs`, `tests/CborSequenceTests.cs`, `tests/CborStreamTests.cs`, `tests/CborLimitTests.cs`, `tests/CborTagTests.cs`, `tests/CborSharingTests.cs`, `tests/CborValidationTests.cs`, `tests/CborDiagnosticTests.cs`, `tests/CborJsonTests.cs`, `tests/CborObjectMapperTests.cs`, `tests/CborInteroperabilityTests.cs`, `tests/CddlLexerTests.cs`, `tests/CddlParserTests.cs`, `tests/CddlGrammarUpdateTests.cs`, `tests/CddlCompilerTests.cs`, `tests/CddlPreludeTests.cs`, `tests/CddlValidationTests.cs`, `tests/CddlGroupMatchingTests.cs`, `tests/CddlCoreControlTests.cs`, `tests/CddlAdditionalControlTests.cs`, `tests/CddlTextControlTests.cs`, `tests/CddlJsonValidationTests.cs`, `tests/CddlGenerationTests.cs`, `tests/CddlHookTests.cs`, `tests/CddlSchemaEmitterTests.cs`, `tests/PublicApiBehaviorTests.cs`, and `tests/AdversarialInputTests.cs`.

## Implementation tasks

### Foundation and generic model

- [DONE] T001: Create the required `src/Api` category directories and internal feature directories, keeping public and internal type placement aligned with `syntax.md`.
- [DONE] T002: Implement the public CBOR enums and static constant classes for major/model kinds, statuses, encoding profiles, policies, simple values, tags, and media types.
- [DONE] T003: Implement `CborValue`, `CborMapEntry`, `CborFloatValue`, and `CborRepresentation` as the lossless flat generic data model, including sharing/cycle representation fields.
- [DONE] T004: Implement CBOR option, result, validation issue, tag registration, mapping context, and registration data classes with safe defaults and explicit limits.
- [DONE] T005: Implement the direct-to-`System.Exception` CBOR exception classes and stable error-code/message/offset/path propagation.
- [DONE] T006: Declare the CBOR public contracts and matching implementation-class shells with one constructor, unique method names, and no public members beyond their interfaces.
- [DONE] T007: Implement overflow-safe big-endian binary primitives for CBOR heads and 8/16/32/64-bit arguments using spans.
- [DONE] T008: Implement half/single/double conversion and exact raw-bit handling for zeros, subnormals, infinities, quiet/signaling NaNs, and payload preservation.
- [DONE] T009: Implement incremental UTF-8 validation/decoding across chunks, including shortest-form, surrogate, out-of-range, and incomplete-sequence rejection.
- [DONE] T010: Implement cycle-aware CBOR value equality, RFC map-key equivalence, hashing support internal to duplicate detection, and deterministic encoded-key comparison without `IEquatable`.
- [DONE] T011: Add tests for all generic model variants, representation metadata, equality/key equivalence, cycles, defaults, and public result/status behavior.
- [DONE] T012: Add exhaustive tests for binary heads, boundary integers/lengths, overflow rejection, half precision, NaN payloads, and fragmented UTF-8.

### Token decoding and high-level decoding

- [DONE] T013: Implement tokenization of every definite major type and simple/float form with exact source consumption and representation metadata.
- [DONE] T014: Implement token container-stack state for definite arrays/maps/tags, enforcing item/pair counts and reporting completion tokens consistently.
- [DONE] T015: Implement indefinite arrays/maps and byte/text strings, legal chunk rules, break processing, and nested-container interactions.
- [DONE] T016: Implement incremental head/payload buffering so every byte boundary can return `NeedMoreData` and resume without re-reading or losing data.
- [DONE] T017: Implement token-level skip of one complete value, including nested/indefinite items, tag chains, limits, and consumed-byte reporting.
- [DONE] T018: Implement all well-formedness failures from RFC 8949 Appendix F plus reserved additional information, odd maps, illegal chunks, and truncation errors.
- [DONE] T019: Implement centralized decode limits for bytes, nesting, items, strings, arrays, maps, retained representation, and embedded recursive decoding before allocation.
- [DONE] T020: Implement DOM construction from the token stream while retaining ordered duplicate map entries, tag nesting, representation details, and configurable source offsets.
- [DONE] T021: Implement exact-one and first-item high-level decode operations, trailing-data policy, atomic failure behavior, and reset/reuse state.
- [DONE] T022: Implement tag 25/256 string references and tag 28/29 shared values/references with disabled, preserve, and resolve policies, including cycles and bad-reference rejection.
- [DONE] T023: Add token-reader tests covering every type, container transition, illegal state, malformed vector, skip path, limit, and fragmentation boundary.
- [DONE] T024: Add decoder/DOM tests covering all RFC 8949 Appendix A vectors, representation retention, duplicate maps, unknown values/tags, exact-one/first semantics, and failure offsets/paths.
- [DONE] T025: Add adversarial decode tests for declared-length bombs, nesting, item floods, UTF-8 fragmentation, cyclic sharing, invalid references, and embedded-data limit propagation.

### Token encoding, DOM encoding, and deterministic modes

- [DONE] T026: Implement span/`IBufferWriter<byte>` token output for all scalar heads and payloads, with destination-too-small reporting that never corrupts writer state.
- [DONE] T027: Implement definite container writer state and exact item/pair count verification, tag-content count enforcement, completion, reset, and misuse faulting.
- [DONE] T028: Implement indefinite container/string token output with legal chunk/break transitions and rejection of malformed token sequences.
- [DONE] T029: Implement high-level size measurement and basic DOM encoding for every value, arbitrary map keys, nested tags, and unknown simple values.
- [DONE] T030: Implement preferred integer/length serialization and configurable shortest-lossless float reduction while preserving negative zero and non-finite semantics.
- [DONE] T031: Implement core deterministic encoding, including shortest heads/floats, canonical NaN, prohibition of indefinite items, and lexical deterministic-key sorting.
- [DONE] T032: Implement length-first core deterministic map-key sorting and reject deterministic duplicate/equivalent encoded keys.
- [DONE] T033: Implement preserve-representation encoding with consistency checks for requested head widths, chunks, indefinite forms, float bits, and mutated values.
- [DONE] T034: Implement sharing/reference encoding with encounter-order tables, cycle handling, configured thresholds/policies, and compatibility checks for deterministic profiles.
- [DONE] T035: Add token-writer tests for every legal transition, buffer boundary, count mismatch, incomplete output, reset, and illegal transition.
- [DONE] T036: Add encoder tests for every generic value, integer boundary, float width, tag, indefinite form, representation-preserving round trip, measurement, and atomic destination behavior.
- [DONE] T037: Add deterministic tests using RFC examples and independently computed key orders for core and length-first profiles, including NaNs, negative zero, bignums, and duplicate keys.

### Streams, sequences, validation, and tags

- [DONE] T038: Implement buffered non-seekable `Stream` reading of one item at a time without losing over-read bytes and with clean end-of-stream versus truncation status.
- [DONE] T039: Implement `Stream` writing of successive values/tokens, explicit flushing, ownership/leave-open behavior, and state validation after I/O failures.
- [DONE] T040: Implement RFC 8742 empty and multi-item sequence processing with per-item offsets, concatenation behavior, and sequence limits.
- [DONE] T041: Implement encoded-byte validation levels for well-formedness, UTF-8/duplicate basic validity, known-tag validity, configured policies, and issue collection/fail-fast modes.
- [DONE] T042: Implement deterministic-profile validation without re-encoding the entire input, including minimal heads, float/NaN form, indefinite prohibition, and map order.
- [DONE] T043: Implement value-model validation for application-created graphs, invalid discriminant/field combinations, cycles, tags, map duplicates, limits, and requested deterministic encodability.
- [DONE] T044: Implement the tag registry, immutable-snapshot lookup during an operation, duplicate registration policy, and generic unknown-tag behavior.
- [DONE] T045: Implement RFC 8949 date/time, epoch, bignum, decimal-fraction, bigfloat, encoded-CBOR, encoding-hint, URI/text-hint, MIME, and self-described tag handlers.
- [DONE] T046: Validate known tag content type/value semantics, date/time syntax, bignum preferred form, decimal/bigfloat shape, embedded item count, and recursive limits.
- [DONE] T047: Add stream and sequence tests for empty input, concatenated values, fragmented reads, non-seekable/short reads, over-read retention, truncation, write failures, and leave-open behavior.
- [DONE] T048: Add complete validator tests for each well-formedness/basic/tag/deterministic issue code, issue caps, paths, offsets, and fail-fast/collect modes.
- [DONE] T049: Add tag tests for built-in typed conversions, unknown/nested tags, invalid content, registry customization, serialization-order tags, embedded recursion, and all policies.

### Diagnostic notation, JSON, and object mapping

- [DONE] T050: Implement RFC 8949 diagnostic formatting for all values with correct escaping, arbitrary map keys, simple values, tags, float special values, and compact/indented layouts.
- [DONE] T051: Implement diagnostic lexing/parsing for base notation plus RFC 8610 EDN numeric bases, comments, concatenated strings, byte-string encodings/whitespace, and encoding indicators.
- [DONE] T052: Implement EDN embedded CBOR and CBOR Sequence byte strings with recursive limits and exact source diagnostics.
- [DONE] T053: Implement representation-aware and deterministic diagnostic formatting plus sequence formatting/parsing with stable round trips where notation carries the distinction.
- [DONE] T054: Implement CBOR-to-JSON conversion policies for bytes, big integers, tags, maps, duplicate/non-text keys, undefined, and non-finite floats using `System.Text.Json`.
- [DONE] T055: Implement JSON-to-CBOR conversion with integer-versus-float preservation where available, duplicate-property policy, configurable byte/tag conventions, and depth/size limits.
- [DONE] T056: Implement built-in native-object adapters for scalar numerics, `BigInteger`, `Half`, decimal, string/bytes, date/time, URI, GUID, enums, and nullable runtime values without nullable value-type syntax in source.
- [DONE] T057: Implement array/list/dictionary and reflected field/property object mapping with map/array layouts, naming/key policies, required/unknown member handling, overflow checks, and cached reflection metadata.
- [DONE] T058: Implement custom type-adapter registration and shared/cyclic native-object mapping without delegates or library-defined attributes.
- [DONE] T059: Add diagnostic tests for every RFC 8949 and RFC 8610 notation feature, invalid lexemes, escaping, indicators, embedded data, sequence forms, limits, and parse/format round trips.
- [DONE] T060: Add JSON conversion tests for every loss policy and representability edge, including duplicate/non-text keys, big integers, bytes, tags, undefined, non-finite values, and deep input.
- [DONE] T061: Add object-mapping tests for every built-in type, collections, fields/properties, layouts, adapters, shared/cyclic graphs, missing/unknown members, and conversion failures.

### CDDL public model, lexer, and parser

- [DONE] T062: Implement public CDDL enums, AST/source/diagnostic/options/result data classes, static prelude/control-name data, and direct exception classes under their required API folders.
- [DONE] T063: Declare CDDL parser/compiler/validation/control/generation/emission contracts and matching implementation-class shells with policy-compliant members.
- [DONE] T064: Implement the RFC 9682-updated lexer for whitespace, comments, identifiers, punctuation/operators, and exact source spans with optional trivia retention.
- [DONE] T065: Implement CDDL text and byte-string literals, escapes, Unicode scalar validation, UTF-8 byte literals, hex bytes, and base64url bytes per the corrected grammar.
- [DONE] T066: Implement signed/unsigned integer, decimal float, and hexadecimal float lexing/conversion with exact overflow and malformed-literal diagnostics.
- [DONE] T067: Implement parsing of type/group rules, type choices, group choices, `/=`/`//=`, empty documents, and root-rule ordering.
- [DONE] T068: Implement parsing of arrays, maps, parenthesized groups/types, entries, bareword/explicit keys, `=>`, `:`, cuts, and occurrences.
- [DONE] T069: Implement ranges, unwrapping, group enumeration, controls, tags, representation types, and RFC 9682 non-literal tag/simple arguments with correct precedence.
- [DONE] T070: Implement sockets/plugs and generic parameter/argument syntax, including nested generic use and contextual type-versus-group parsing.
- [DONE] T071: Implement bounded parser recovery that can return multiple non-overlapping diagnostics while preserving a usable partial AST in `TryParseDocument`.
- [DONE] T072: Add lexer/literal tests for every RFC grammar production, Unicode/escape update, numeric format, comment/trivia option, malformed scalar, and source span.
- [DONE] T073: Add parser tests for every construct and precedence level, all RFC 8610 examples, RFC 9682 changes, ambiguous type/group contexts, recovery, and empty documents.

### CDDL compilation and semantic analysis

- [DONE] T074: Parse/load the complete standard prelude and merge it as distinguishable built-in rules without overriding source definitions improperly.
- [DONE] T075: Build ordered rule tables and validate duplicate base definitions, rule kinds, compatible `/=`/`//=` extension merging, and extension ordering.
- [DONE] T076: Resolve named references, sockets/plugs, external resolver results, and root selection with source-aware undefined/ambiguous diagnostics.
- [DONE] T077: Validate generic arity and implement bounded, capture-safe generic substitution with memoized instantiations and nested-generic support.
- [DONE] T078: Normalize source AST into validation nodes while preserving source mapping, cuts, choices, occurrences, labels, and representation constraints.
- [DONE] T079: Build the reference graph, distinguish productive legal recursion from non-productive cycles, and calculate conservative generation/validation properties.
- [DONE] T080: Implement semantic checks for range endpoints, control operands, occurrences, map labels/cuts, unwrap/group-enum targets, tags/representation arguments, and type/group context.
- [DONE] T081: Implement throwing and exception-less compile operations with stable diagnostic ordering and an immutable-by-convention reusable `CddlSchema` result.
- [DONE] T082: Add prelude/compiler tests for every prelude type, rule extension, socket/plug, generic instantiation, recursion category, external resolution, semantic error, and source mapping.

### CDDL validation engine and RFC 8610 controls

- [DONE] T083: Implement the common CDDL validation state, rule stack, instance/schema paths, budgets, feature set, memoization, and bounded diagnostic tree.
- [DONE] T084: Implement literal, prelude scalar, integer/float width, simple, tag, major-type, range, choice, reference, generic, unwrap, and group-enumeration matching.
- [DONE] T085: Implement ordered array/group matching with complete bounded alternatives and occurrences, including nullable/repeated groups without infinite loops.
- [DONE] T086: Implement unordered map matching for structs/tables/arbitrary keys, duplicates, occurrences, group choices, cuts, and deterministic reproducible branch selection.
- [DONE] T087: Implement recursive-rule validation with memoized instance/rule states, cycle protection, productive recursion, and configured step/depth limits.
- [DONE] T088: Implement `.size` for strings/integers, `.bits`, and standards-compatible XML Schema `.regexp` parsing/matching without external source dependencies.
- [DONE] T089: Implement `.cbor` and `.cborseq` recursive binary decode/validation with shared budgets, full-consumption checks, and nested mismatch paths.
- [DONE] T090: Implement `.within`, `.and`, relational/equality controls, and `.default` with exact CBOR value/type equality and optional-entry semantics.
- [DONE] T091: Implement CBOR validation entry points from an existing value and encoded bytes, preserving byte offsets when validating decoded input.
- [DONE] T092: Add core validation tests for every AST/prelude construct, representation width, ordered/unordered group case, occurrence ambiguity, cut, recursion, generic, tag, and path.
- [DONE] T093: Add RFC 8610 control tests for valid/invalid operands and values, embedded CBOR/sequences, XSD regex differences from .NET regex, defaults, limits, and diagnostics.

### RFC 9165 and RFC 9741 controls, JSON validation, and extensions

- [DONE] T094: Implement RFC 9165 `.plus`, `.cat`, and `.det` computed-literal semantics, numeric conversion/floor behavior, byte/text compatibility, and dedenting.
- [DONE] T095: Implement RFC 5234/RFC 7405 ABNF lexer/parser and bounded matcher needed by `.abnf` and `.abnfb`, with incremental alternatives, repetition, prose rejection policy, and core-rule handling.
- [DONE] T096: Implement `.feature` selection/detail reporting so disabled alternatives fail cleanly and enabled features appear in validation results.
- [DONE] T097: Implement RFC 9741 strict/sloppy base64url/base64, base32/base32hex, Base45, case-sensitive/insensitive hex, and base10 control semantics.
- [DONE] T098: Implement RFC 9741 `.printf` format subset and typed argument matching exactly as specified, with bounded parsing and unsupported-directive diagnostics.
- [DONE] T099: Implement RFC 9741 `.json` by parsing one JSON value and validating it against the controller under Appendix E mapping rules and shared limits.
- [DONE] T100: Implement RFC 9741 `.join` validation/generation rules for text/byte element concatenation, including ambiguous decompositions within validation budgets.
- [DONE] T101: Implement the custom control registry, operation-time lookup snapshots, unknown-control policies, and handler result/source-context wrapping.
- [DONE] T102: Implement direct JSON-document CDDL validation for the RFC 8610 Appendix E subset, including JSON object-key restrictions and numeric representability.
- [DONE] T103: Add RFC 9165 tests from the specification plus edge cases for computed literals, dedenting, ABNF bytes/text/case, features, malformed grammars, and budgets.
- [DONE] T104: Add RFC 9741 tests for every strict/sloppy encoding, case rule, base10/printf/JSON/join construct, invalid trailing bits/padding, ambiguity, and resource limit.
- [DONE] T105: Add JSON CDDL validation and custom-control tests for every supported mapping, feature combination, handler outcome, registry mutation boundary, and diagnostic path/span.

### Schema-driven generation and emission hooks

- [DONE] T106: Implement seeded minimal/boundary/representative scalar generation for literals, preludes, numeric ranges, float widths, tags, and choices.
- [DONE] T107: Implement bounded array/map/group generation with occurrences, cuts, arbitrary keys, extensions, generics, and deterministic seed-based branch selection.
- [DONE] T108: Implement recursive-schema generation using productivity analysis, depth/size budgets, base-case selection, and clear failure when no finite value can be constructed.
- [DONE] T109: Implement built-in generation behavior for standard controls where constructive semantics are defined, and invoke hooks for custom/nonconstructive constraints.
- [DONE] T110: Implement `ICddlGenerationHook` lifecycle/context, supplied-value validation, rule/tag/control interception, reentrancy protection, and contextual exception wrapping.
- [DONE] T111: Validate every generated value against its selected schema/rule before success and return structured generation failure details when candidates cannot satisfy constraints.
- [DONE] T112: Implement schema traversal/emission in stable source/rule/node order with filtering/naming context and `ICddlSchemaSink` begin/accept/end lifecycle.
- [DONE] T113: Add generation tests for every construct/strategy, seed reproducibility, recursion, budgets, hook substitution/failure, control generation, and mandatory post-validation.
- [DONE] T114: Add schema-emitter tests for complete traversal, source mapping, stable ordering, filtering, sink failures, reentrancy, and all public lifecycle behavior.

### Interoperability and complete public-API coverage

- [DONE] T115: Run every downloaded RFC 8949 Appendix A vector through decode, encode, diagnostic, deterministic, and sequence-appropriate assertions, including decode-only non-preferred forms.
- [DONE] T116: Add generated malformed vectors covering every RFC 8949 Appendix F category and every truncation point, and assert exact public error status/code/offset/path.
- [DONE] T117: Add differential CBOR tests against permitted test-only reference libraries for all common data-model values, deterministic encodings, tags, and sequences, encoding intentional policy differences directly in assertions.
- [DONE] T118: Add CDDL conformance fixtures from RFCs 8610, 9165, 9682, and 9741, validating all positive examples and targeted mutations that must fail.
- [DONE] T119: Add round-trip/property tests for arbitrary bounded CBOR values, token fragmentation, diagnostic notation, JSON-representable subsets, and schema-generated values using test-only generators if useful.
- [DONE] T120: Add concurrency tests showing compiled schemas, frozen operation options/registries, encoders/decoders as documented, and independent operation instances have no shared mutable global state.
- [DONE] T121: Add public API behavior tests that instantiate and exercise every public class, method, option, enum path, result/status, exception type, registry/hook, and static constant through at least one success and each defined failure category.
- [DONE] T122: Add cancellation/budget/adversarial tests for all byte, nesting, collection, item, backtracking, memo, issue, embedded-data, ABNF, regex, JSON, generation, and sharing limits.
- [DONE] T123: Collect test coverage for `src/Api`-exposed behavior and close every uncovered reachable branch with functional tests, excluding impossible runtime/platform failure branches only with an explicit test rationale.
  - Coverage exclusion rationale: the remaining uncovered `src/Api` branches are defensive fallbacks for states that public operations cannot produce: failed limit/state helpers always supply an error code and message; tokenizers cannot complete without a token; completed DOM builders cannot lack a value; active stream reads cannot lack their tokenizer or tag snapshot; measured encoding plans cannot fail when written to their exactly sized buffer; parser, compiler, and generation failures always retain a diagnostic or error code; stream buffer count/offset violations require a `Stream` implementation to violate the `Read` contract; and sequence/array arithmetic overflows require allocations beyond supported .NET runtime limits.
- [DONE] T124: Run the full solution test suite from a clean build and resolve all compilation warnings, test failures, nondeterministic results, and interoperability mismatches before considering the feature set complete.

## Completion criteria

All tasks are complete only when the public APIs compile under `syntax.md`, `src/` has no non-.NET dependency, all listed RFC behavior is implemented, all downloaded conformance material passes, every exposed behavior has tests, and limits/errors remain observable through the planned result and exception surfaces. No benchmark, release, documentation, or syntax-policy/source-checking work is included in this plan.
