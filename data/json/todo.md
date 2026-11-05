# Portable JSON development plan

## 1. Goal and scope

Build a dependency-free C# JSON library whose runtime implementation uses only the .NET base class libraries. It will cover the complete ECMA-404 / ISO/IEC 21778 value grammar, RFC 8259 UTF-8 interchange behavior, an exact seven-kind data model, low-level and high-level parsing, pull and push streaming, incremental input, stateful writing, deterministic output, RFC 6901 pointer navigation, and optional RFC 8785 JSON Canonicalization Scheme output.

The implementation namespace should be PortableJson. The library is a JSON syntax/data-model library, not a reflection-based object mapper. The following are deliberately outside the first complete implementation:

- POCO reflection binding, attributes, converters tied to application types, and source generation.
- Comments, trailing commas, single-quoted strings, unquoted names, NaN, infinities, hexadecimal numbers, and other non-JSON extensions.
- JSON Schema, JSONPath, JSON Patch, Merge Patch, JSON5, CBOR, BSON, and YAML.
- Network I/O and asynchronous APIs. Callers can supply built-in Stream or IBufferWriter<byte> instances.
- Performance benchmark projects, syntax-policy tests, documentation work, packaging, and release work.

## 2. Normative baseline and interpretation

The local research bundle is indexed by specs/SOURCES.md.

- ECMA-404 2nd edition and ISO/IEC 21778:2017 define the accepted syntax. A JSON text may contain any JSON value at the root, including a scalar.
- RFC 8259 supplies Internet interchange rules: UTF-8 outside closed ecosystems, exact JSON whitespace, interoperability guidance for duplicate names and numbers, and the optional ability to ignore an initial BOM. Strict mode will reject a BOM by default; a specific BOM policy can accept one only at byte offset zero.
- RFC 7493 supplies an opt-in I-JSON profile: valid Unicode scalar strings and unique object names. Exact number lexemes remain available even when they exceed native numeric ranges.
- RFC 6901 supplies JSON Pointer string and URI-fragment parsing and evaluation. No Unicode normalization or case folding is performed.
- RFC 7464 supplies an explicit JSON text sequence mode in addition to single-value and whitespace-separated-value input.
- RFC 8785 supplies the optional JCS profile. JCS is distinct from merely deterministic output: it rejects duplicates and lone surrogates, requires finite IEEE-754 binary64 values, emits ECMAScript-compatible shortest number forms, preserves string data without normalization, uses no whitespace, and recursively sorts names by unsigned UTF-16 code units.
- Unicode 17 defines valid UTF-8 and UTF-16 surrogate behavior. Invalid UTF-8 is always rejected. Escaped lone surrogates can be represented only in the syntax-compatible profile; the portable Unicode-scalar and JCS profiles reject them.

### Fixed semantic decisions

- The canonical DOM has exactly seven kinds: null, boolean, number, string, array, and object, with boolean represented as one kind and one value field.
- A number is stored first as its validated original decimal lexeme. Conversion to Int64, UInt64, Decimal, or Double is explicit, culture-independent, range-checked, and available in throwing and exception-less forms.
- Arrays retain element order. Objects retain member order and can retain duplicate names, because the base standards do not assign duplicate-name semantics.
- Duplicate handling is explicit: Preserve, Reject, KeepFirst, or KeepLast. Preserve is required for syntax-faithful processing; the portable preset uses Reject.
- Duplicate comparison is performed after escape decoding, by exact ordinal UTF-16 code-unit equality, without normalization.
- DOM source ranges are optional. Token locations always track absolute UTF-8 byte offset; line and byte/scalar columns are enabled by option. Lines and columns are one-based, offsets are zero-based, and CRLF counts as one line break.
- Resource limits are checked before allocation or state growth. All count and size values use signed integer types.
- Incremental parsing can split at every byte boundary, including inside a multibyte UTF-8 scalar, escape, surrogate pair, literal, number, CRLF, and delimiter.
- All parsers consume and validate skipped content. Skipping changes materialization, not conformance checking.
- Input buffers and streams remain caller-owned and are never closed by the library. Output is flushed only when explicitly requested or completed.
- Public mutable data classes can be constructed directly, so every serializer/canonicalizer validates the model before emitting bytes.

## 3. Existing-library feature research

| Library | Features adopted into this plan | Important boundary |
| --- | --- | --- |
| Jackson | Token parser/generator foundation, tree model layered over streaming, skip/copy-current-structure behavior, duplicate detection, configurable nesting/document/string/number limits, location-rich errors, and non-blocking input concepts. | No Java databinding or permissive read features. |
| System.Text.Json | UTF-8-first reader/writer, resumable reader state, bytes-consumed accounting, raw token access, skip/capture subtree, Stream and IBufferWriter output, mutable DOM concepts, maximum depth, and multiple top-level values. | The public API cannot use ref structs or ref parameters under syntax.md, so resumable state belongs to class instances and results. |
| Newtonsoft.Json | Pull reader/writer, ordered mutable token tree, source line information, multiple-content mode, duplicate policy, number conversion choices, and configurable formatting. | No date coercion, type metadata, or .NET object contract system. JSON strings stay strings. |
| simdjson | Strict UTF-8 validation, DOM and forward-only processing distinction, reusable parser state, subtree skipping, pointer lookup, and multi-document streams. | No validate-only-what-you-use behavior; every accepted document is completely validated. |
| RapidJSON | DOM/SAX symmetry, raw-number delivery, handler-controlled termination, stream abstractions, encoding validation, pretty writer, and DOM-to-event publication. | UTF-8 is the public wire encoding; no in-situ mutation or alternate wire encodings. |
| serde_json | Exact generic Value model, raw numeric/value concepts, stream deserialization, depth protection, explicit arbitrary-precision behavior, and preserve-versus-sort object order. | No Rust-style typed Serde binding or compile-time features. |
| Gson | Simple begin/end pull reader and writer, peek/skip, current path, strictness, nesting limit, tree model, and configurable pretty formatting. | Only standards-conforming strict JSON is accepted. |

The resulting scope intentionally includes features that are absent or optional in individual libraries when they are needed for portability: explicit duplicate preservation, exact number lexemes, complete incremental boundary behavior, both pull and push APIs, locations on tokens and DOM nodes, deterministic ordering, and standards-defined JCS.

## 4. Complete feature inventory, low level to high level

### 4.1 Input, encoding, and framing

- Accept UTF-8 from ReadOnlySpan<byte>, byte arrays, Stream, and incremental byte chunks.
- Accept .NET string input through explicit UTF-16-to-Unicode validation and UTF-8 conversion.
- Validate UTF-8 shortest form, continuation bytes, scalar range, overlong sequences, surrogate encodings, truncation, and values above U+10FFFF.
- Configurable initial UTF-8 BOM rejection or acceptance; reject embedded/repeated BOMs.
- Single JSON text mode with leading/trailing JSON whitespace and mandatory end-of-input.
- Whitespace-separated multiple-value mode with a document boundary token.
- RFC 7464 record-separator mode, optional LF after each value, empty/incomplete record handling, and configured stop-or-resynchronize error behavior.
- Caller-visible accepted/consumed byte counts and final-input state.
- Stream buffering that copes with tokens larger than the initial buffer while respecting maximum token and buffered-byte limits.

### 4.2 Lexical grammar

- Recognize exactly six structural characters, three lowercase literal names, strings, numbers, and four JSON whitespace code points.
- Reject comments, trailing separators, missing separators/colons, extra tokens, invalid literal casing, and every non-JSON whitespace code point.
- Preserve raw UTF-8 token bytes when requested, with documented lifetime independent of caller buffers.
- Report token start/end, depth, decoded value, raw number lexeme, property/value role, and current RFC 6901 pointer.

### 4.3 Strings and Unicode escapes

- Scan quotation marks, reverse solidus escapes, and unescaped content without treating embedded NUL as termination.
- Support only quote, reverse solidus, solidus, b, f, n, r, t, and uXXXX escapes.
- Require exactly four hexadecimal digits and accept uppercase or lowercase hexadecimal digits.
- Combine escaped high/low surrogate pairs into a supplementary scalar.
- Detect high-without-low, low-without-high, reversed, repeated, or truncated surrogate sequences.
- Syntax-compatible mode retains escaped lone surrogate code units; Unicode-scalar and JCS modes reject them.
- Reject raw U+0000 through U+001F, malformed UTF-8, and direct UTF-8 encodings of surrogate code points.
- Accept valid noncharacters and unescaped U+2028/U+2029; do no normalization, case folding, or grapheme processing.
- Decode property names before duplicate comparison and pointer creation.
- Enforce independent raw-token, decoded-string UTF-8 byte, and decoded UTF-16 code-unit limits.

### 4.4 Numbers

- Enforce minus, integer, fraction, and exponent grammar exactly.
- Reject leading plus, leading zeroes, missing integer/fraction/exponent digits, decimal comma, underscores, whitespace inside a number, NaN, and infinities.
- Preserve sign, integer/fraction/exponent spelling, negative zero, trailing fractional zeroes, and exponent case/sign in the raw lexeme.
- Classify integer, fractional, and exponent-bearing lexemes without changing their value.
- Convert explicitly to Int64, UInt64, Decimal, and Double with invariant culture, full-token consumption, overflow detection, and finite-double checks.
- Offer exception-less conversion results because conversion mismatch and overflow are expected conditions.
- Allow syntactically valid arbitrary-length numbers subject to configured length; never force a lossy conversion during parsing.
- Format Int64, UInt64, Decimal, and finite Double values as valid culture-independent JSON numbers.
- Implement JCS/ECMAScript binary64 serialization separately from ordinary round-trip .NET formatting, including negative zero and exponent normalization.

### 4.5 Structural parser

- Accept every root value kind and empty/non-empty arrays and objects.
- Maintain an iterative container stack rather than relying on recursive parsing.
- Enforce state transitions for object name, colon, object value, comma/end, array value, and comma/end.
- Track depth consistently across reader, DOM, writer, and diagnostics.
- Detect duplicate decoded names under each configured duplicate policy at every nesting level.
- Validate skipped values and entire input, including trailing content.
- Reset and reuse parser instances after success or failure without leaking prior state.

### 4.6 Resource controls

- Maximum document bytes, input buffer bytes, token bytes, decoded string bytes/code units, number characters, nesting depth, array items, object members, total DOM nodes, and output bytes.
- A value of zero means use the documented safe default; negative values are rejected as invalid options.
- Limits apply consistently to one-shot, pull, push, stream, DOM, raw-value, pointer traversal, writer, and canonicalizer paths.
- Fail before integer overflow, allocation beyond limit, or counter wraparound.
- Distinguish syntax, Unicode, numeric-conversion, state, I/O, and limit diagnostics.

### 4.7 Diagnostics and locations

- Stable diagnostic-code enum with specific codes for every lexical, structural, UTF-8, escape, surrogate, duplicate, framing, limit, pointer, DOM invariant, writer-state, and I/O failure family.
- Human-readable message, source range, byte offset, line/columns when enabled, depth, current pointer, actual token, expected token set summary, and bounded source excerpt.
- Correct locations across incremental chunks, CR, LF, CRLF, multibyte scalars, escaped text, and stream buffer refills.
- Throwing APIs use dedicated exceptions; exception-less APIs return the same JsonDiagnostic data.
- Push handlers can pause or stop without an error, and errors are distinguishable from caller-requested termination.

### 4.8 Pull streaming API

- Peek/read one token at a time in depth-first order.
- Feed incremental UTF-8 and signal final input independently.
- Return TokenAvailable, NeedMoreData, DocumentComplete, StreamComplete, Paused, Stopped, or Error status without ref/out parameters.
- Expose decoded strings/property names, exact number lexemes, booleans, null, structural tokens, depth, range, and pointer.
- Skip the current scalar or complete subtree while still validating it.
- Capture the current scalar/subtree into a JsonValue without constructing the rest of the document.
- Support single, whitespace-separated, and RFC 7464 sequence modes.
- Reset for reuse and make behavior after completion/error explicit.

### 4.9 Push streaming API

- Publish the same token and boundary model to IJsonTokenHandler.
- Feed whole or partial buffers and complete input independently.
- Handler actions Continue, Pause, and Stop; resume after Pause without replaying delivered events.
- Propagate precise handler position and parser diagnostic on failure.
- Permit composition with DOM builder, validator, filter, transformer, and writer without third-party dependencies.

### 4.10 DOM and canonical value model

- JsonDocument owns one root JsonValue.
- JsonValue is a tagged data class with fields for null, boolean, exact number, string, array list, or ordered object-member list.
- JsonMember retains decoded name, value, name range, member range, and ordinal position.
- Optional ranges on all values; no parent pointer is stored, avoiding cycles.
- Preserve duplicate members and insertion/source order when requested.
- Factory methods for every kind, deep clone, and verified creation from a number lexeme.
- Model validator catches mismatched kind/payload, null required fields, invalid number lexemes, invalid UTF-16 strings, cycles, excessive depth/nodes, and invalid members.
- Array index and object member lookup, all-duplicate lookup, append/insert/replace/remove/clear operations, and deterministic enumeration through built-in lists.
- Deep equality modes for exact model comparison and JSON semantic comparison, with an explicit duplicate/object-order rule.
- DOM-to-token traversal so writers and push consumers share behavior.

### 4.11 JSON Pointer and traversal hooks

- Parse and format RFC 6901 JSON Pointer strings, including empty pointer, empty tokens, ~0, and ~1.
- Parse and format URI fragment representation with correct UTF-8 percent encoding/decoding.
- Reject bad tilde escapes, bad percent escapes, invalid UTF-8 fragments, leading-zero array indexes, signs, non-digits, and index overflow.
- Evaluate from root across objects and arrays; member names are exact and unnormalized.
- Define duplicate-name resolution explicitly and allow resolving all matching duplicate members.
- Return throwing and exception-less results with the failed segment and current location.
- Depth-first visitor exposes value, parent context, member name/array index, pointer, depth, and source range.
- Visitor can Continue, SkipChildren, or Stop.
- Pull/push tokens carry a pointer so callers can implement pointer-selective processing without building the full DOM.

### 4.12 Serializer and stateful writer

- Serialize a DOM to UTF-8 byte array, UTF-16 string, Stream, or IBufferWriter<byte>.
- Stateful writer methods for object/array begin/end, property names, null, boolean, string, exact number lexeme, Int64, UInt64, Decimal, Double, DOM value, and validated raw JSON value.
- Enforce exactly one root in single-value mode and correct name/value/container state.
- Reject non-finite floating point values, invalid raw number/value text, invalid UTF-16 under selected Unicode policy, invalid DOM state, and depth/output limits.
- Compact and pretty modes with configurable ASCII whitespace indentation, LF or CRLF newline, and space-after-colon behavior.
- String escaping modes: minimal JSON, ASCII-only, and HTML-safe; always escape controls, quote, and reverse solidus.
- Optional solidus escaping and lowercase/uppercase hexadecimal escape digits outside JCS.
- Property ordering: preserve, ordinal UTF-16, ordinal Unicode scalar, or JCS.
- Deterministic modes recursively order object members while never reordering arrays.
- Duplicate handling during serialization; ordering modes use a stable tie rule, while JCS rejects duplicates.
- Flush, complete, reset/reuse, bytes-pending/committed reporting, and no automatic closure of caller output.
- Raw-value writing validates exactly one complete JSON value before copying it.

### 4.13 Canonicalization and deterministic output

- Deterministic compact output with selected ordering and stable escaping/number policies.
- Full RFC 8785 JCS mode with recursive unsigned UTF-16 name ordering, no whitespace, required escapes, lowercase hexadecimal, no normalization, duplicate rejection, Unicode-scalar enforcement, and binary64 restriction.
- JCS number test vectors from RFC 8785 Appendix B plus boundary, negative-zero, exponent, rounding, and subnormal cases.
- Canonicalization from DOM and directly from input; direct input may build/buffer individual objects because ordering is not possible with a purely forward writer.
- Verify whether input bytes already equal their canonical form.
- Ensure idempotence: canonicalizing canonical output produces identical bytes.

## 5. Proposed public API and source files

This is an API shape, not final signatures. Every public member will have XML documentation while being implemented, but there is no separate documentation task.

The syntax.md policy drives these choices:

- Public state uses fields, not properties.
- No project-defined generic types or generic methods.
- Incremental state is held by class instances, avoiding ref/out and ref-struct APIs.
- Expected failures use concrete result data classes rather than nullable value types.
- Callback behavior uses interfaces rather than delegates.
- Every method name is unique within an interface and within every implementation class; no overloads are planned.
- Each public implementation class has one constructor and exposes only members declared by its implemented interface.
- Every exception derives directly from System.Exception.
- Span and ReadOnlySpan are used on synchronous buffer-taking methods where lifetime is not retained.

### 5.1 Data classes under src/Api/Data

| Planned file | Purpose |
| --- | --- |
| JsonLimits.cs | Signed depth, document, token, string, number, collection, node, buffer, and output limits. |
| JsonReaderOptions.cs | Conformance, framing, duplicate, BOM, Unicode, location, raw-token, sequence-recovery, and limits settings. |
| JsonWriterOptions.cs | Formatting, escaping, ordering, duplicate, Unicode, validation, framing, and limits settings. |
| JsonWriterTarget.cs | Exactly one caller-owned Stream or IBufferWriter<byte> destination. |
| JsonSourcePosition.cs | Byte offset, line, byte column, and Unicode-scalar column. |
| JsonSourceRange.cs | Inclusive start and exclusive end positions. |
| JsonDiagnostic.cs | Stable code, message, range, pointer, depth, expected/actual context, and bounded excerpt. |
| JsonNumber.cs | Validated original lexeme and number classification flags. |
| JsonMember.cs | Decoded object name, value, ranges, and source ordinal. |
| JsonValue.cs | Tagged seven-kind DOM payload and optional source range. |
| JsonDocument.cs | Owned root and parse-profile metadata. |
| JsonToken.cs | Pull/push token data, decoded/raw payload, depth, pointer, and range. |
| JsonFeedResult.cs | Bytes consumed and incremental read/write status plus diagnostic. |
| JsonTokenResult.cs | Token/status/diagnostic result for pull reading. |
| JsonValueResult.cs | Value/status/diagnostic result for subtree capture and factory validation. |
| JsonDocumentResult.cs | Document success/status/diagnostic result for one-shot parsing. |
| JsonOperationResult.cs | General exception-less success/status/diagnostic and byte counts. |
| JsonInt64Result.cs | Exception-less Int64 conversion. |
| JsonUInt64Result.cs | Exception-less UInt64 conversion. |
| JsonDecimalResult.cs | Exception-less Decimal conversion. |
| JsonDoubleResult.cs | Exception-less finite Double conversion. |
| JsonPointer.cs | Original representation and decoded reference-token list. |
| JsonPointerResult.cs | Pointer evaluation result, failed segment, and diagnostic. |
| JsonVisit.cs | Current value, parent context, name/index, pointer, depth, and range. |

### 5.2 Operation contracts under src/Api/Contracts

| Planned file | Proposed operation surface |
| --- | --- |
| IJsonUtf8Validator.cs | Validate one UTF-8 JSON input without DOM construction. |
| IJsonPullReader.cs | Feed/complete input, peek/read token, skip/capture value, query counters, and reset. |
| IJsonTokenHandler.cs | Receive one JsonToken and return a handler action. |
| IJsonPushParser.cs | Feed/complete tokens into a handler, query counters, and reset. |
| IJsonDocumentParser.cs | Parse and try-parse UTF-8, string, and Stream inputs into JsonDocument. |
| IJsonValueFactory.cs | Create each value kind, validate number text, and deep-clone values. |
| IJsonValueValidator.cs | Validate a publicly mutable JsonValue/JsonDocument graph and limits. |
| IJsonDomNavigator.cs | Array/object lookup and mutation with explicit duplicate policy. |
| IJsonNumberConverter.cs | Throwing and exception-less conversions for the four supported .NET numeric forms. |
| IJsonPointerCodec.cs | Parse/format string and URI-fragment pointer forms. |
| IJsonPointerNavigator.cs | Resolve one/all pointer matches in throwing and exception-less forms. |
| IJsonValueVisitor.cs | Receive one JsonVisit and choose continue, skip children, or stop. |
| IJsonTraversal.cs | Iterative depth-first traversal and DOM-to-token publication. |
| IJsonValueComparer.cs | Exact-model and defined JSON-semantic deep comparison. |
| IJsonDocumentSerializer.cs | Serialize DOM to UTF-8, string, Stream, or IBufferWriter<byte>. |
| IJsonTokenWriter.cs | Stateful token/value/raw writing, flush, complete, counters, and reset. |
| IJsonCanonicalizer.cs | JCS canonicalize input/DOM, write canonical output, and verify canonical bytes. |

### 5.3 Enums under src/Api/Enums

JsonValueKind.cs, JsonTokenKind.cs, JsonReadStatus.cs, JsonWriteStatus.cs, JsonDiagnosticCode.cs, JsonConformanceProfile.cs, JsonDuplicateKeyPolicy.cs, JsonInputMode.cs, JsonSequenceErrorPolicy.cs, JsonUnicodePolicy.cs, JsonBomPolicy.cs, JsonNumberKind.cs, JsonFormatting.cs, JsonPropertyOrder.cs, JsonStringEscapeMode.cs, JsonComparisonMode.cs, JsonHandlerAction.cs, JsonVisitAction.cs, and JsonPointerEncoding.cs.

Each enum has explicit Int32 member values. Every enum is used by an interface or data class.

### 5.4 Static data under src/Api/Static

| Planned file | Fields only |
| --- | --- |
| JsonDefaults.cs | Safe default limits and formatting constants. |
| JsonMediaTypes.cs | application/json and application/json-seq strings. |
| JsonSyntaxBytes.cs | Public byte constants for JSON structural and framing characters. |

These classes contain no methods or delegates.

### 5.5 Exceptions under src/Api/Exceptions

JsonParseException.cs, JsonWriteException.cs, JsonPointerException.cs, and JsonLimitException.cs. Each has one constructor, carries a JsonDiagnostic field, and derives directly from System.Exception.

### 5.6 Public implementations under src/Api/Classes

JsonUtf8Validator.cs, JsonPullReader.cs, JsonPushParser.cs, JsonDocumentParser.cs, JsonValueFactory.cs, JsonValueValidator.cs, JsonDomNavigator.cs, JsonNumberConverter.cs, JsonPointerCodec.cs, JsonPointerNavigator.cs, JsonDepthFirstTraversal.cs, JsonValueComparer.cs, JsonDocumentSerializer.cs, JsonUtf8Writer.cs, and JsonCanonicalizer.cs.

Each class implements its matching public interface, has at most one constructor, and exposes no extra public instance members or public static methods.

### 5.7 Internal implementation files

| Area | Planned files |
| --- | --- |
| Buffers | src/Internal/Buffers/JsonByteQueue.cs, JsonByteSink.cs, JsonTokenBuffer.cs |
| UTF-8 and lexing | src/Internal/Lexing/Utf8Cursor.cs, Utf8Decoder.cs, JsonLexer.cs, JsonStringScanner.cs, JsonEscapeDecoder.cs, JsonNumberScanner.cs, JsonLiteralScanner.cs |
| Parsing | src/Internal/Parsing/JsonParserCore.cs, JsonContainerFrame.cs, JsonContainerStack.cs, JsonDuplicateTracker.cs, JsonSequenceFramer.cs, JsonEventDispatcher.cs, JsonDomBuilder.cs |
| Diagnostics | src/Internal/Diagnostics/JsonLocationTracker.cs, JsonPathTracker.cs, JsonDiagnosticFactory.cs |
| DOM | src/Internal/Dom/JsonModelValidator.cs, JsonDomOperations.cs, JsonGraphWalker.cs, JsonDeepComparer.cs |
| Pointer | src/Internal/Pointer/JsonPointerCodecCore.cs, JsonPointerEvaluator.cs |
| Writing | src/Internal/Writing/JsonWriterCore.cs, JsonWriteFrame.cs, JsonStringEncoder.cs, JsonNumberFormatter.cs, JsonPrettyFormatter.cs, JsonPropertySorter.cs, JsonCanonicalNumberFormatter.cs |

All types outside src/Api are non-public.

## 6. Test organization and coverage standard

Tests can use external packages, but src/ cannot. Planned test files:

- tests/Conformance/JsonGrammarTests.cs, JsonStringTests.cs, JsonNumberTests.cs, JsonUtf8Tests.cs, JsonSequenceTests.cs, JsonTestSuiteTests.cs.
- tests/Streaming/JsonIncrementalBoundaryTests.cs, JsonPullReaderTests.cs, JsonPushParserTests.cs, JsonSkipCaptureTests.cs, JsonStreamInputTests.cs.
- tests/Dom/JsonValueTests.cs, JsonDomMutationTests.cs, JsonModelValidationTests.cs, JsonComparisonTests.cs.
- tests/Pointer/JsonPointerCodecTests.cs, JsonPointerEvaluationTests.cs, JsonTraversalTests.cs.
- tests/Writing/JsonUtf8WriterTests.cs, JsonFormattingTests.cs, JsonEscapingTests.cs, JsonDeterministicOrderingTests.cs, JsonCanonicalizationTests.cs.
- tests/Diagnostics/JsonLocationTests.cs, JsonDiagnosticTests.cs, JsonLimitTests.cs, JsonExceptionTests.cs.
- tests/Api/JsonPublicContractBehaviorTests.cs, JsonDefaultDataTests.cs, JsonReuseAndOwnershipTests.cs, JsonRoundTripTests.cs.

Coverage means every public constructor, field-influenced behavior, enum value, and interface method implemented in src/Api has positive, boundary, invalid-state, and failure-path tests where applicable. It also means every diagnostic code is provoked by at least one test, every option is shown to change behavior, every implementation is tested through its interface, and every result/exception representation is asserted. This is behavioral API coverage, not a syntax-policy/source-layout test.

Required fixture families:

- Every ECMA-404 grammar production and RFC 8259 example.
- All JSONTestSuite y_ fixtures accepted, all n_ fixtures rejected, and every i_ fixture assigned an explicit expected result for each conformance profile.
- RFC 6901 examples and URI-fragment cases.
- RFC 7464 valid, incomplete, malformed, and recovery sequences.
- RFC 7493 duplicate, surrogate, encoding, and number guidance cases.
- RFC 8785 sorting and number vectors.
- Generated chunk splits at every byte boundary for representative documents.
- Round trips across every value kind, duplicate policy, ordering mode, escape mode, formatting mode, and input mode.
- Invalid public DOM graphs, cycles, null payloads, mismatched tags, and post-construction mutations.
- Limit minus one, exact limit, and limit plus one for every resource counter.

## 7. Implementation tasks

Tasks are ordered by dependency. Test work is intentionally interleaved with implementation.

- [DONE] T001: Create the planned src/Api and src/Internal directory structure and establish the PortableJson file-scoped namespace without adding dependencies.
- [DONE] T002: Implement all public enums with explicit Int32 values and use each from its planned data or contract surface.
- [DONE] T003: Implement JsonDefaults, JsonMediaTypes, and JsonSyntaxBytes as field-only public static data classes.
- [DONE] T004: Implement JsonLimits, JsonReaderOptions, JsonWriterOptions, and JsonWriterTarget with one-constructor-or-field-only shapes compatible with syntax.md.
- [DONE] T005: Implement source position, source range, diagnostic, feed, token, document, value, operation, pointer, visit, and numeric conversion result data classes.
- [DONE] T006: Implement JsonNumber, JsonMember, JsonValue, and JsonDocument as the seven-kind ordered canonical data model.
- [DONE] T007: Implement the four direct-System.Exception public exception classes and preserve JsonDiagnostic data through throwing paths.
- [DONE] T008: Declare all operation interfaces with unique method names, no overloads, no ref/out parameters, and Span-based input only where the callee cannot retain it.
- [DONE] T009: Implement option normalization and validation, including zero-as-default, rejection of negative limits, target exclusivity, and compatible profile/policy combinations.
- [DONE] T010: Add tests for every defaults field, option field, enum value, result data shape, exception payload, and invalid option combination exposed by src/Api.
- [DONE] T011: Implement JsonByteQueue for incremental ownership, compaction, token growth, byte accounting, and configured buffered-byte limits.
- [DONE] T012: Implement Utf8Cursor and Utf8Decoder with strict scalar validation across contiguous and split input.
- [DONE] T013: Implement JsonLocationTracker with byte offset, one-based line, byte column, scalar column, and CR/LF/CRLF handling across chunks.
- [DONE] T014: Implement JsonLiteralScanner for exact lowercase true, false, and null recognition with resumable partial-token state.
- [DONE] T015: Implement JsonStringScanner for quotes, control rejection, raw UTF-8 segments, escapes, and resumable partial strings.
- [DONE] T016: Implement JsonEscapeDecoder for all legal escapes, four-digit hexadecimal units, surrogate pairing, and configurable lone-surrogate behavior.
- [DONE] T017: Implement JsonNumberScanner for the complete decimal grammar, exact lexeme retention, classification, and split-token state.
- [DONE] T018: Implement JsonLexer for whitespace, structural tokens, literals, strings, numbers, raw-token capture, and token-length limits.
- [DONE] T019: Add exhaustive UTF-8 tests for valid boundary scalars, malformed leaders/continuations, overlong forms, surrogate encodings, truncation, noncharacters, BOM policies, and every split point.
- [DONE] T020: Add exhaustive string tests for every escape, hexadecimal case, supplementary pairs, lone-surrogate policies, embedded NUL, raw controls, U+2028/U+2029, normalization preservation, and size limits.
- [DONE] T021: Add exhaustive number grammar tests for valid spellings and every invalid sign, zero, fraction, exponent, delimiter, and truncation form.
- [DONE] T022: Add lexer tests for exact whitespace/literal casing, raw token capture, depth-independent locations, chunk splits, and token limits.
- [DONE] T023: Implement JsonContainerFrame and JsonContainerStack for iterative root, array, and object grammar states.
- [DONE] T024: Implement JsonPathTracker so decoded names and array indexes produce escaped RFC 6901 pointers at every token.
- [DONE] T025: Implement JsonDuplicateTracker using exact decoded ordinal names and all four duplicate policies.
- [DONE] T026: Implement JsonSequenceFramer for single values, whitespace-separated values, and RFC 7464 record separators and recovery.
- [DONE] T027: Implement JsonDiagnosticFactory with stable codes, bounded excerpts, expected/actual context, ranges, depth, and pointer.
- [DONE] T028: Implement JsonParserCore to join lexer, structural stack, duplicates, framing, limits, final-input validation, and reusable state.
- [DONE] T029: Add structural grammar tests for every root kind, empty/nested containers, separator/colon failures, trailing input, premature EOF, and maximum depth.
- [DONE] T030: Add duplicate-policy tests using escaped-equivalent names, empty names, non-ASCII names, nested scopes, and Preserve/Reject/KeepFirst/KeepLast results.
- [DONE] T031: Add single/multiple/RFC 7464 framing tests for boundaries, optional LF, incomplete records, malformed records, stop/recovery, and pointer/location reset.
- [DONE] T032: Add diagnostic tests that provoke and assert every lexical, Unicode, structural, duplicate, framing, state, and input-limit diagnostic code.
- [DONE] T033: Implement JsonUtf8Validator and its IJsonUtf8Validator surface on JsonParserCore without allocating a DOM.
- [DONE] T034: Test JsonUtf8Validator through its interface for valid, invalid, limited, one-shot, and each framing/profile combination.
- [DONE] T035: Implement JsonPullReader feed, complete, peek, read, counters, status transitions, and reset behavior.
- [DONE] T036: Implement pull-reader scalar/subtree skipping with full validation and correct next-token positioning.
- [DONE] T037: Implement pull-reader scalar/subtree capture into JsonValue while leaving later input streaming.
- [DONE] T038: Add pull-reader tests for all tokens/payloads, status transitions, counters, final blocks, zero-length feeds, input ownership, and reset after success/failure.
- [DONE] T039: Add incremental pull tests that split representative and generated documents at every byte boundary and across every UTF-8, escape, number, literal, CRLF, and delimiter state.
- [DONE] T040: Add skip/capture tests for scalars, empty/deep containers, malformed skipped content, duplicates, limits, and correct reader continuation.
- [DONE] T041: Implement JsonEventDispatcher and JsonPushParser with Continue, Pause, Stop, resume, completion, counters, and reset.
- [DONE] T042: Add push-parser tests for event equivalence with pull tokens, pause/resume without replay, stop semantics, handler failure boundaries, incremental feeds, and reuse.
- [DONE] T043: Implement buffered Stream ingestion for the one-shot parser and streaming facades without closing caller streams.
- [DONE] T044: Add Stream tests for short reads, zero-length intermediate reads where applicable, oversized tokens, refill boundaries, I/O failures, and ownership.
- [DONE] T045: Implement JsonDomBuilder over parser events with ordered members, exact numbers, selected duplicate policy, optional locations, and all DOM limits.
- [DONE] T046: Implement JsonDocumentParser parse and try-parse methods for UTF-8, string, and Stream sources.
- [DONE] T047: Add document-parser tests for every input form, every root kind/profile/policy, source metadata, throwing/result parity, and input ownership.
- [DONE] T048: Integrate all JSONTestSuite parsing fixtures and record explicit expectations for implementation-defined i_ cases by profile.
- [DONE] T049: Add direct ECMA-404, RFC 8259, and RFC 7493 conformance fixtures that cover requirements not uniquely represented in JSONTestSuite.
- [DONE] T050: Implement JsonValueFactory creation for all seven kinds, validated number creation, ordered object creation, and iterative deep clone.
- [DONE] T051: Implement JsonModelValidator and JsonValueValidator for tag/payload invariants, valid numbers/UTF-16, null references, cycles, limits, and member integrity.
- [DONE] T052: Implement JsonDomOperations and JsonDomNavigator for array/object reads and mutations with bounds and duplicate semantics.
- [DONE] T053: Add canonical-model tests for every kind, exact numeric spelling, member order, duplicate retention, locations, factory creation, and deep clone independence.
- [DONE] T054: Add model-validator tests for every invalid public mutation, mismatched payload, bad number, lone surrogate, null child/name, cycle, depth, count, and node limit.
- [DONE] T055: Add DOM navigator tests for array indexes, missing members, all duplicate policies, insert/replace/remove/clear operations, and failure results.
- [DONE] T056: Implement invariant-culture throwing conversions from JsonNumber to Int64, UInt64, Decimal, and finite Double.
- [DONE] T057: Implement exception-less numeric conversions with exact success/value/diagnostic parity with throwing conversions.
- [DONE] T058: Add numeric conversion tests for minima/maxima, one-step overflow, negative unsigned input, decimal precision/scale, double overflow/underflow, negative zero, and full lexeme consumption.
- [DONE] T059: Implement JsonPointerCodecCore and JsonPointerCodec for string and URI-fragment parse/format with tilde and percent encoding.
- [DONE] T060: Implement JsonPointerEvaluator and JsonPointerNavigator for object/array traversal, duplicate selection, all-match resolution, and precise failed-segment diagnostics.
- [DONE] T061: Add RFC 6901 codec tests including empty pointer/token, ~0/~1 ordering, Unicode, URI fragments, bad tilde/percent escapes, and invalid fragment UTF-8.
- [DONE] T062: Add pointer evaluation tests for RFC examples, arrays, leading zero/sign/overflow rejection, missing/type mismatch, duplicates, normalization distinctions, and throwing/result parity.
- [DONE] T063: Implement JsonGraphWalker and JsonDepthFirstTraversal iteratively with Continue, SkipChildren, Stop, context, pointer, and DOM-to-token publication.
- [DONE] T064: Add traversal tests for visit order, context, ranges, duplicate names, pointer escaping, skip/stop, deep inputs, and token equivalence.
- [DONE] T065: Implement JsonDeepComparer and JsonValueComparer for exact-model and documented JSON-semantic modes without IEquatable.
- [DONE] T066: Add comparer tests for number spellings, negative zero, string code units, array order, object order, duplicates, and invalid models.
- [DONE] T067: Implement JsonByteSink for Stream and IBufferWriter<byte>, pending/committed counts, output limits, flush, and caller ownership.
- [DONE] T068: Implement JsonWriteFrame and JsonWriterCore structural state for one root, object names/values, arrays, depth, completion, and reset.
- [DONE] T069: Implement JsonStringEncoder for minimal, ASCII-only, HTML-safe, solidus, hexadecimal-case, invalid UTF-16, and JCS escaping rules.
- [DONE] T070: Implement ordinary JsonNumberFormatter for Int64, UInt64, Decimal, finite Double, and validated exact lexeme output using invariant culture.
- [DONE] T071: Implement JsonPrettyFormatter for compact/pretty modes, validated indentation, LF/CRLF, colon spacing, and nested empty/non-empty containers.
- [DONE] T072: Implement JsonPropertySorter for preserve, ordinal UTF-16, ordinal scalar, and stable duplicate tie ordering.
- [DONE] T073: Implement JsonUtf8Writer and IJsonTokenWriter methods for structural tokens, all scalar forms, DOM values, validated raw values, flush, complete, counters, and reset.
- [DONE] T074: Add writer-state tests for every legal transition and every invalid name/value/end/root/completion/reset transition.
- [DONE] T075: Add string-writer tests for all escape modes, control characters, ASCII boundaries, supplementary scalars, lone surrogates, hexadecimal case, and solidus behavior.
- [DONE] T076: Add number-writer tests for all numeric entry points, culture independence, negative zero, finite enforcement, invalid lexemes, and numeric boundaries.
- [DONE] T077: Add pretty-format tests for indentation/newline/colon options, empty and nested containers, and validation of non-JSON formatting characters.
- [DONE] T078: Add property-order tests for UTF-16 versus scalar ordering, prefix names, supplementary characters, combining sequences, duplicates, recursion, and unchanged arrays.
- [DONE] T079: Add raw-value tests that require exactly one fully valid value, preserve valid bytes, reject trailing/partial data, apply limits, and resume writer state correctly.
- [DONE] T080: Implement JsonDocumentSerializer for UTF-8 array, string, Stream, and IBufferWriter<byte> targets through the same writer core.
- [DONE] T081: Add serializer tests for every destination, every DOM kind, invalid mutable models, options, duplicate policies, output limits, flushing, and output ownership.
- [DONE] T082: Implement JsonCanonicalNumberFormatter with ECMAScript-compatible binary64 shortest formatting required by RFC 8785.
- [DONE] T083: Implement JsonCanonicalizer for DOM and input, recursive JCS sorting, strict string/duplicate/number validation, streaming output, and canonical-byte verification.
- [DONE] T084: Add all RFC 8785 Appendix B number vectors and property-order examples, plus subnormal, exponent-threshold, rounding, negative-zero, and invalid-number cases.
- [DONE] T085: Add JCS tests for escaping, no whitespace, no normalization, UTF-16 name sorting, recursive objects in arrays, duplicate/lone-surrogate rejection, and finite-double enforcement.
- [DONE] T086: Add canonicalization idempotence and canonical-byte verification tests across generated valid JCS model trees.
- [DONE] T087: Add end-to-end round-trip tests across parser, DOM, traversal, serializer, pull-to-writer, push-to-writer, and reparse paths for every value and option family.
- [DONE] T088: Add deterministic-output tests that repeat serialization after different construction and parsing paths and require byte-identical results under deterministic modes.
- [DONE] T089: Add all resource-limit boundary tests at limit-minus-one, exact-limit, and limit-plus-one for each one-shot, pull, push, DOM, pointer, writer, and canonicalizer path.
- [DONE] T090: Add location tests across all newline forms, multibyte UTF-8, escaped strings, chunk/stream boundaries, nested pointers, duplicate errors, and end-of-input errors.
- [DONE] T091: Add parser/writer reuse and ownership tests proving reset isolation, no stale buffers/paths/diagnostics, and no closure of caller streams.
- [DONE] T092: Add exception-versus-result parity tests for parsing, limits, numbers, pointers, DOM validation, writer state, canonicalization, and I/O failures.
- [DONE] T093: Add behavioral contract tests that invoke every public implementation through its interface and exercise every public method at least once successfully and once on each applicable failure class.
- [DONE] T094: Add tests proving every public option field and every nontrivial enum member changes or selects the documented runtime behavior.
- [DONE] T095: Add tests that provoke every JsonDiagnosticCode and assert stable code, range, pointer, depth, excerpt bounds, and expected/actual context.
- [DONE] T096: Add randomized grammar-generated valid JSON round trips with bounded depth/size and randomized invalid single-fault mutations, without timing assertions.
- [DONE] T097: Add randomized incremental chunk partition tests and compare one-shot, pull, push, Stream, and DOM acceptance plus diagnostics.
- [DONE] T098: Run the complete test suite under invariant and several non-invariant current cultures to detect accidental culture-sensitive number or ordering behavior.
- [DONE] T099: Review the final src dependency graph and remove any non-base-library runtime reference introduced during implementation.
- [DONE] T100: Run the complete API behavior and standards conformance suite and resolve every uncovered public behavior, fixture mismatch, or inconsistent diagnostic before declaring the implementation complete.
