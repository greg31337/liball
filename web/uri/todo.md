# Universal URI library development plan

## Goal

Build a dependency-free production library for strict URI, IRI, URN, URI Template, and Web Link processing on .NET 10. The implementation will cover low-level character and percent-encoding primitives through high-level parsing, composition, validation, resolution, relativization, normalization, comparison, template expansion, URN equivalence, and Link header utilities.

Production code may use only built-in .NET APIs. `ReadOnlySpan<char>`, `ReadOnlySpan<byte>`, and other span types should be used at scanning and encoding boundaries where they avoid temporary substrings without complicating the field-based public model. External packages and downloaded corpora may be used only by tests.

## Standards baseline and interpretation

- RFC 3986 is the normative URI grammar, component model, resolution algorithm, syntax normalization, and comparison ladder.
- RFC 3987 is the normative IRI and IRI-reference grammar and the basis for reversible UTF-8 URI/IRI conversion. Unicode normalization is explicit and never silently changes an already-parsed identifier.
- RFC 6570 Level 4 is the normative URI Template grammar and expansion model; Levels 1–3 are reported as classifications of the same parsed model.
- RFC 8141 is the normative URN grammar and equivalence model, including `?+` r-components, `?=` q-components, and fragments.
- RFC 8288 is the normative Web Link model and Link header serialization. RFC 8187 supplies extended parameter decoding, RFC 5646 supplies `hreflang` validation, and current HTTP field combination rules come from RFC 9110.
- Verified RFC Editor errata stored under `specs/errata/` are part of conformance decisions. Held/reported errata must become targeted tests only after their effect is reviewed rather than being accepted automatically.
- RFC 5952 supplies the canonical IPv6 spelling used by normalization.
- RFC 9844 obsoletes RFC 6874 and restores the unmodified RFC 3986 host grammar. Strict parsing therefore rejects IPv6 zone identifiers in URI literals. A future explicit UI adapter could handle zone IDs, but the generic URI model will not.
- IDN host mapping is separate from generic IRI-to-URI mapping. Generic conversion can always UTF-8 percent-encode an `ireg-name`; DNS-oriented conversion uses the built-in .NET IDN facility behind an explicit option and validates labels before invoking throwing framework APIs. The API will not claim a platform-independent IDNA version where .NET behavior depends on its Unicode/runtime data.
- Syntax-based normalization never sorts query parameters, removes empty query/fragment delimiters, guesses a default scheme, or applies scheme semantics. Those behaviors are available only through explicit scheme profiles or query utilities.
- URN equivalence compares only the assigned-name, lowercases the scheme and NID, and uppercases percent-triplet hex digits in the NSS. It does not decode NSS triplets, and ignores r-, q-, and f-components as required by RFC 8141.
- Parsing is strict by default. A recovery mode exists only for the RFC 8288 Appendix B Link-header algorithm and returns diagnostics for discarded or repaired input.

## Research synthesis

| Library | Useful surface adopted | Boundary or behavior not copied |
| --- | --- | --- |
| uriparser | Parse/recompose, strict and compatibility resolution, inverse reference creation, normalization analysis, query form codec, component-oriented errors | C ownership APIs are irrelevant; filename conversion is scheme-specific and outside the selected RFCs |
| Python `rfc3986` | Immutable-result style, authority decomposition, configurable validation requirements, builder/copy workflows, throwing and result-based entry points | Deprecated per-component validation shortcuts are consolidated into one validation service |
| Python `uritemplate` | Reusable parsed templates, variable inspection, scalar/list/map values, partial expansion | Partial expansion is labeled as a library extension; reverse matching is ambiguous and not part of RFC 6570 |
| URI.js | Component editing, explicit component normalization, relative/absolute conversion, comparison, query utilities, common-path helpers | Query sorting/deduplication is never part of generic equivalence; ISO-8859 conversion, URI guessing in prose, fragment-query conventions, and mutable fluent state are excluded |
| Ada | Clear parse-or-error results, component and serialized forms, validation before access, explicit input limits | Ada implements the WHATWG URL state machine; its permissive/special-scheme URL rules and URLPattern are not mixed with RFC 3986/3987 parsing |

The local research snapshots are under `specs/libraries/`. They are design inputs only and will not be referenced by `src/`.

## Complete feature inventory

### 1. Common representation, diagnostics, and limits

- Field-based parsed models preserve the difference between an absent component and a present empty component, including empty authority, port, query, and fragment delimiters.
- URI reference classification covers URI, absolute-URI, relative-ref, network-path, absolute-path, relative-path, empty reference, and same-document reference.
- Authority data separates user information, host, and port while retaining the original registered-name or IP-literal spelling needed for lossless recomposition.
- Host classification distinguishes absent host, registered name, IPv4, IPv6, and IPvFuture.
- Every expected-failure operation has a result object containing success, value, diagnostic code, component, zero-based offset, length, and message. Throwing conveniences use domain-specific exceptions carrying the same diagnostic.
- Input and output limits cover total characters, component characters, path segments, query items, template expressions/variables/composite members, links, parameters, and expanded output. Limit failures are deterministic diagnostics, not partial allocations.
- Operations do not perform DNS resolution, network access, filesystem access, or locale-sensitive comparison.

### 2. URI parsing, validation, and composition — RFC 3986

- Parse and validate `URI-reference`, `URI`, `absolute-URI`, and `relative-ref` productions with whole-input consumption.
- Apply first-match-wins disambiguation between URI and relative-ref, between IPv4 and registered name, and among the five path productions.
- Parse scheme, `//` authority, userinfo, host, port, path, query, and fragment with exact delimiter-presence tracking.
- Validate scheme syntax and ASCII case-insensitive scheme handling without imposing a scheme registry.
- Parse authority at the last `@`, validate userinfo, bracket IP literals, distinguish an empty port from no port, and reject unbracketed IPv6.
- Strictly parse four-decimal-octet IPv4 addresses; reject .NET-compatible but RFC-incompatible hexadecimal, octal, shortened, signed, whitespace, and overflow forms.
- Parse every RFC 3986 IPv6 alternative, including embedded strict IPv4, and parse IPvFuture version/address syntax.
- Reject obsolete IPv6 zone identifiers in accordance with RFC 9844's restoration of the RFC 3986 grammar.
- Validate registered names, every path form, query, and fragment with component-specific allowed character sets and complete percent triplets.
- Preserve path segment boundaries, repeated slashes, trailing slash, empty segments, and encoded delimiters.
- Recompose losslessly from a parsed model and compose validated references from caller-created component data.
- Provide component-level validation and configurable requirements such as required scheme, authority, host, path, query, fragment, or absolute form.
- Report all validation diagnostics when requested, while parse entry points report the first fatal diagnostic.

### 3. Percent encoding and Unicode primitives

- Validate percent triplets and report the exact `%` or invalid hex digit.
- Encode Unicode scalar values as strict UTF-8 with uppercase percent triplets and selectable RFC component allowlists.
- Encode arbitrary bytes without interpreting them as text.
- Decode triplets to bytes; separately decode valid UTF-8 to Unicode so byte decoding never guesses a character encoding.
- Support strict Unicode decoding and explicit replacement mode; reject isolated UTF-16 surrogates in strict encoding.
- Preserve reserved encodings when decoding would change delimiter semantics, and decode only unreserved triplets for syntax normalization.
- Normalize hex digit case without double-encoding existing valid triplets.
- Expose component-specific sets for userinfo, reg-name, path segment, path, query, fragment, URN components, URI Template simple expansion, and reserved expansion.

### 4. URI normalization and comparison

- RFC 3986 syntax normalization: lowercase scheme and registered-name host, uppercase percent hex digits, decode percent-encoded unreserved characters, and remove dot segments.
- Dot-segment removal follows RFC 3986 section 5.2.4 for absolute and relative paths without collapsing ordinary empty segments.
- Normalize IPv4 to decimal dotted quad and IPv6 to RFC 5952 lowercase compressed form, including embedded IPv4 handling.
- Analyze which components would change before normalization.
- Scheme-aware profiles can normalize an authority with an empty path to `/`, remove an empty/default port, and apply declared default-port rules; built-in constants support HTTP, HTTPS, WS, WSS, and FTP defaults without treating the IANA registry as frozen code.
- Preserve empty query and fragment delimiters unless an explicit profile requests their removal.
- Comparison levels include character-for-character, RFC syntax-normalized, and explicit scheme-profile comparison. Results explain the first differing component.
- Never infer resource equivalence through redirects, DNS aliases, filesystem case rules, content, or network access.
- Query canonicalization, ordering, and duplicate policy are separate utilities and opt-in comparison inputs, not RFC generic normalization.

### 5. Resolution, relativization, editing, and path/query utilities

- Resolve every relative-reference form against an absolute base using RFC 3986 sections 5.2.2–5.2.4.
- Support strict resolution and the RFC 3986 compatibility behavior that ignores a reference scheme identical to the base scheme.
- Cover base query inheritance, empty references, fragment-only and query-only references, authority replacement, path merge, and abnormal examples.
- Relativize an absolute target against an absolute base, choosing among same-document, query/fragment-only, relative-path, absolute-path, network-path, and absolute URI forms according to explicit policy.
- Guarantee `Resolve(base, Relativize(base, target))` reproduces the target under the selected normalization/equality policy.
- Prevent ambiguous relative output by adding `./` when a first segment contains `:` and by guarding leading `//`.
- Build references from components, clone with an explicit change set, clear or set optional delimiters, and validate the result before returning it.
- Parse and compose ordered query pairs while preserving duplicates, missing `=`, empty names, empty values, and delimiter choice.
- Provide an explicit `application/x-www-form-urlencoded` mode for `+`/space handling; plain RFC query mode never gives `+` special meaning.
- Add, replace, remove, filter, sort, and deduplicate query pairs only when explicitly requested, with ordinal comparison defaults.
- Join paths, find a common base path, split segments, and remove dot segments without decoding reserved `/` or `?` characters.

### 6. IRI parsing, conversion, normalization, and comparison — RFC 3987

- Parse `IRI-reference`, `IRI`, `absolute-IRI`, and `irelative-ref` using `ihier-part`, `iauthority`, `ipath-*`, `iquery`, and `ifragment` productions.
- Accept `ucschar` in allowed IRI components and `iprivate` only where RFC 3987 permits it; reject controls, spaces, malformed UTF-16, and forbidden code points.
- Reuse strict URI IP-literal parsing; Unicode is accepted only in `ireg-name` and the internationalized path/query/fragment productions.
- Convert IRI to URI by UTF-8 percent-encoding non-ASCII scalars while preserving valid URI syntax and existing percent triplets.
- Convert URI to IRI only for complete legal UTF-8 triplet sequences whose decoded characters are safe and legal in that component; preserve encoded reserved characters, `%`, illegal ASCII, invalid UTF-8, and unsafe code points.
- Guarantee the RFC 3987 URI-to-IRI-to-URI round trip modulo permitted percent-triplet case and unreserved normalization.
- Offer explicit DNS-host A-label/U-label conversion through built-in .NET IDN support, with label length, separator, and round-trip validation and a clearly reported mapping mode.
- Apply NFC only when requested for producer-side IRI normalization; retain a mode that diagnoses but does not change non-normalized input.
- Implement bidi conformance diagnostics from RFC 3987 and host-label bidi validation applicable to the selected IDN mapping mode.
- Compare IRIs as exact Unicode strings, NFC-normalized strings, or mapped and syntax-normalized URIs, always under an explicit comparison level.

### 7. URI Templates — RFC 6570

- Parse the complete Level 4 grammar and classify the minimum level required by a template.
- Preserve literal and expression source spans and produce a reusable template model.
- Validate printable Unicode literals, percent triplets, balanced non-nested braces, operator position, non-empty variable lists, variable-name dot rules, and reserved future operators.
- Parse case-sensitive variable names including percent triplets without decoding them.
- Parse prefix modifiers from 1 through 9999 Unicode characters and explode modifiers; reject zero, excess digits, invalid combinations, and modifiers on incompatible values.
- Model undefined, scalar, list, and ordered associative values, preserving empty scalar values and deterministic member order.
- Expand all operators: simple, reserved `+`, fragment `#`, label `.`, path `/`, path parameter `;`, query `?`, and continuation `&`.
- Correctly expand scalar/list/map values with and without explode for every operator, including empty and undefined values.
- Apply prefix length by Unicode scalar count without splitting surrogate pairs or percent triplets.
- Apply UTF-8 percent encoding exactly as required for simple versus reserved expansion and for Unicode template literals.
- Return partial diagnostic output as described by RFC 6570 on malformed input, and provide a strict throwing entry point.
- Provide reusable template inspection for ordered unique variable names, occurrences, operators, modifiers, and required level.
- Provide a non-standard, explicitly named partial-expansion utility that leaves unresolved expressions valid for later expansion.
- Compose a parsed template model without semantic rewriting; percent-triplet case canonicalization is opt-in.
- High-level helpers expand and then parse, resolve against a base URI, or normalize the resulting URI using caller-selected URI options.
- Reverse matching/extraction is excluded because RFC 6570 expansion is not generally invertible.

### 8. URNs — RFC 8141

- Detect and parse the case-insensitive `urn` scheme, 2–32 character NID, non-empty NSS, optional r-component, optional q-component, and optional f-component.
- Enforce delimiter order (`?+`, then `?=`, then `#`), reject bare `?` outside optional components, and preserve empty/present optional components where the grammar permits them.
- Validate ASCII-only wire syntax, component character rules, complete percent triplets, NID start/end alphanumeric rules, and generic versus namespace-registration restrictions as distinct validation modes.
- Expose assigned-name separately from rqf components and convert a parsed URN to the generic URI model without losing URN boundaries.
- Normalize generic URN lexical form exactly as RFC 8141 requires: lowercase scheme/NID and uppercase NSS percent hex digits without decoding them.
- Compare assigned names for RFC 8141 URN equivalence while ignoring r-, q-, and f-components.
- Offer a namespace-handler contract for optional namespace-specific NSS validation, normalization, and equivalence; generic operations never guess those rules.
- Provide helpers that transfer q-components and f-components to a resolved locator under explicit collision policy, while leaving r-component resolution semantics to a resolver supplied by the application.
- Do not claim validation of assignment, persistence, namespace registration, or resource resolution from generic syntax alone.

### 9. Web Links and Link header utilities — RFC 8288

- Model link context, target, relation types, target attributes, original parameter order, repeated parameters, and raw plus decoded extended values.
- Parse one or multiple combined Link field values without splitting commas inside angle brackets or quoted strings.
- Implement the strict RFC grammar and the RFC 8288 Appendix B recovery algorithm as separate modes.
- Parse target URI-references inside `<...>`, quoted-string escapes, tokens, optional whitespace, comma-separated link-values, and semicolon parameters with precise offsets.
- Interpret `rel` and `rev` relation lists, registered relation tokens case-insensitively, and extension relation types as absolute URIs.
- Interpret standard attributes: `anchor`, repeatable `hreflang`, `media`, `title`, RFC 8187 `title*`, and `type`, while preserving extension attributes.
- Enforce first-occurrence handling where RFC 8288 requires it, retain all occurrences for diagnostics/round-trip inspection, and keep all valid repeated `hreflang` values.
- Decode RFC 8187 extended values with charset/language metadata and UTF-8 support; validate language tags and report unsupported charsets without corrupting raw values.
- Serialize field values with correct token versus quoted-string selection, escaping, extended-value encoding, parameter ordering, and safe line-value composition without obsolete folding.
- Resolve relative targets against effective context, resolve `anchor` against the supplied representation context, and expose links whose anchor cannot be honored rather than silently changing context.
- Filter link sets by relation, target, context, media type, language, and attribute using correct comparison rules.
- Convert IRI context/target inputs to URI references for HTTP serialization and provide decoded IRI views for applications.
- Preserve ordering in parsing and serialization while treating ordering as semantically insignificant unless a caller explicitly requests it.

### 10. Robustness and security behavior

- All parsers are whole-input, deterministic, culture-invariant, and bounded by caller-visible limits.
- Diagnostics never echo more input than necessary; no operation logs, resolves, fetches, or dereferences identifiers.
- Tests cover delimiter confusion, multiple `@`, encoded delimiters, Unicode bidi controls, invalid UTF-8 triplets, null/control characters, very deep paths, oversized template expansion, malicious quoted strings, and comma/semicolon ambiguity.
- Parsed and normalized values remain data, not authorization decisions. Callers must separately enforce allowed schemes, hosts, ports, credentials, and private-network policies.

## Explicit non-goals

- WHATWG browser URL parsing, special-scheme state machines, Windows drive-letter URLs, and URLPattern.
- DNS lookup, HTTP fetching, URN resolution protocols, redirect equivalence, or link traversal.
- Heuristic URL extraction from prose, automatic scheme guessing, filesystem-path conversion, fragment-as-query conventions, or ISO-8859 URI transcoding.
- Scheme-specific parsers beyond declarative normalization profiles, and namespace-specific URN semantics beyond the extension contract.
- URI Template reverse matching.
- Performance benchmarks, source-policy tests, documentation projects, packaging, and release automation.

## Proposed public API and source files

All public types use namespace `S11.Web.Uri`. Each file contains one public type. Public data is represented by fields, not properties. Every implementation class has one constructor at most and exposes only methods declared by its interface. Method names are unique—there are no overloads. There are no public static methods.

### `src/Api/Data`

- `TextRange.cs` — offset and length.
- `UriDiagnostic.cs` — domain/code, severity, component, range, and message.
- `OperationLimits.cs` — all input, collection, nesting, and output limits.
- `UriAuthority.cs` — userinfo/host/port values and delimiter-presence fields.
- `UriReference.cs` — scheme, authority, path, query, fragment, kinds, and original text.
- `UriParseOptions.cs`, `UriValidationOptions.cs`, `UriNormalizationOptions.cs`, `UriResolutionOptions.cs`, `UriRelativizationOptions.cs`, `UriComparisonOptions.cs` — explicit operation policies.
- `UriParseResult.cs`, `UriValidationResult.cs`, `UriTextResult.cs`, `UriComparisonResult.cs` — exception-less outcomes.
- `UriSchemeProfile.cs` — declarative scheme normalization behavior.
- `UriReferenceChanges.cs` — explicit set/clear/keep component edits.
- `PercentCodecOptions.cs`, `PercentDecodeResult.cs` — encoding/decoding policy and byte/text outcomes.
- `UriQueryParameter.cs`, `UriQueryOptions.cs`, `UriQueryResult.cs` — ordered query data, codec policy, and diagnostics.
- `IriReference.cs`, `IriParseOptions.cs`, `IriConversionOptions.cs`, `IriParseResult.cs` — internationalized model and conversion policy.
- `UriTemplate.cs`, `UriTemplatePart.cs`, `UriTemplateVariable.cs`, `UriTemplateValue.cs`, `UriTemplateMapEntry.cs`, `UriTemplateBinding.cs`, `UriTemplateParseOptions.cs`, `UriTemplateExpansionOptions.cs`, `UriTemplateParseResult.cs`, `UriTemplateExpansionResult.cs` — reusable template syntax and values.
- `Urn.cs`, `UrnParseOptions.cs`, `UrnParseResult.cs`, `UrnLocatorMergeOptions.cs` — URN components and operation policy.
- `WebLink.cs`, `WebLinkParameter.cs`, `WebLinkSet.cs`, `WebLinkParseOptions.cs`, `WebLinkParseResult.cs`, `WebLinkResolutionResult.cs`, `WebLinkFilter.cs` — link model, parse policy, resolution, and filtering criteria.

### `src/Api/Enums`

- `DiagnosticDomain.cs`, `DiagnosticSeverity.cs`, `UriDiagnosticCode.cs`, `UriComponent.cs`.
- `UriReferenceKind.cs`, `UriHostKind.cs`, `UriComparisonLevel.cs`, `UriNormalizationChange.cs`, `UriResolutionMode.cs`, `UriRelativizationMode.cs`.
- `PercentEncodeSet.cs`, `PercentDecodeMode.cs`, `UnicodeNormalizationMode.cs`, `IriIdnMode.cs`.
- `UriTemplatePartKind.cs`, `UriTemplateOperator.cs`, `UriTemplateValueKind.cs`, `UriTemplateLevel.cs`.
- `UrnValidationMode.cs`, `UrnEquivalenceLevel.cs`, `UrnLocatorCollisionMode.cs`.
- `WebLinkParseMode.cs`, `WebLinkParameterKind.cs`, `WebLinkRelationKind.cs`.

### `src/Api/Static`

- `UriCharacterSets.cs` — public constant strings for RFC reserved, general delimiters, sub-delimiters, and unreserved characters.
- `UriSchemeDefaults.cs` — public scheme-name and default-port constants for HTTP, HTTPS, WS, WSS, and FTP.
- `UriTemplateOperatorData.cs` — public operator-character constants and the reserved-future operator set.
- `WebLinkAttributeNames.cs` — public standard target-attribute name constants.

These classes contain constants only. Mutable IANA registries are not compiled into static API data.

### `src/Api/Contracts`

- `IUriParser.cs` — `ParseReference`, `TryParseReference`, `TryParseReferenceSpan`, `ParseAbsoluteUri`, `TryParseAbsoluteUri`, and `TryParseRelativeReference`.
- `IUriValidator.cs` — `ValidateReference`, `ValidateComponents`, and `ValidateAllDiagnostics`.
- `IUriComposer.cs` — `ComposeReference`, `TryComposeReference`, and `MeasureReference`.
- `IPercentCodec.cs` — `ValidatePercentEncoding`, `EncodeText`, `EncodeBytes`, `EncodeSpan`, `DecodeBytes`, `DecodeText`, and `NormalizePercentEncoding`.
- `IUriNormalizer.cs` — `AnalyzeNormalization`, `NormalizeSyntax`, and `NormalizeWithProfile`.
- `IUriComparer.cs` — `CompareExact`, `CompareSyntax`, and `CompareWithProfile`.
- `IUriResolver.cs` — `ResolveReference` and `TryResolveReference`.
- `IUriRelativizer.cs` — `RelativizeReference` and `TryRelativizeReference`.
- `IUriEditor.cs` — `BuildFromComponents`, `TryBuildFromComponents`, and `CopyWithChanges`.
- `IUriQueryCodec.cs` — `ParseQuery`, `ComposeQuery`, `AddQueryParameter`, `ReplaceQueryParameters`, `RemoveQueryParameters`, `SortQueryParameters`, and `DeduplicateQueryParameters`.
- `IUriPathUtility.cs` — `RemoveDotSegments`, `JoinPaths`, `SplitPathSegments`, and `FindCommonBasePath`.
- `IIriParser.cs` — `ParseIriReference`, `TryParseIriReference`, `TryParseIriReferenceSpan`, and `TryParseAbsoluteIri`.
- `IIriConverter.cs` — `ConvertIriToUri`, `TryConvertIriToUri`, `ConvertUriToIri`, `ConvertHostToAscii`, and `ConvertHostToUnicode`.
- `IIriNormalizer.cs` — `NormalizeIri` and `DiagnoseIriNormalization`.
- `IIriComparer.cs` — `CompareIriExact`, `CompareIriUnicode`, and `CompareIriAsUri`.
- `IUriTemplateParser.cs` — `ParseTemplate`, `TryParseTemplate`, and `TryParseTemplateSpan`.
- `IUriTemplateExpander.cs` — `ExpandTemplate`, `TryExpandTemplate`, and `ExpandTemplatePartially`.
- `IUriTemplateComposer.cs` — `ComposeTemplate` and `CanonicalizeTemplatePercentCase`.
- `IUriTemplateInspector.cs` — `ListTemplateVariables`, `ListTemplateOccurrences`, and `DetermineTemplateLevel`.
- `IUriTemplateProcessor.cs` — `ExpandToUri`, `ExpandAndResolve`, and `ExpandAndNormalize`.
- `IUrnParser.cs` — `ParseUrn`, `TryParseUrn`, and `TryParseUrnSpan`.
- `IUrnNormalizer.cs` — `NormalizeUrn` and `NormalizeAssignedName`.
- `IUrnComparer.cs` — `CompareUrnExact`, `CompareUrnAssignedName`, and `CompareUrnNamespaceSpecific`.
- `IUrnNamespaceHandler.cs` — `ValidateNamespaceSpecificString`, `NormalizeNamespaceSpecificString`, and `CompareNamespaceSpecificStrings` for application-supplied namespace rules.
- `IUrnLocatorUtility.cs` — `ApplyUrnComponentsToLocator` and `SeparateAssignedName`.
- `IWebLinkParser.cs` — `ParseLinkFieldValues`, `TryParseLinkFieldValues`, and `TryParseLinkFieldValueSpan`.
- `IWebLinkComposer.cs` — `ComposeLinkFieldValue`, `TryComposeLinkFieldValue`, and `MeasureLinkFieldValue`.
- `IWebLinkResolver.cs` — `ResolveWebLinks` and `ResolveWebLink`.
- `IWebLinkQuery.cs` — `FilterWebLinks`, `FindLinksByRelation`, `FindLinksByTarget`, and `FindLinksByContext`.

### `src/Api/Exceptions`

- `UriSyntaxException.cs`, `UriValidationException.cs`, `PercentEncodingException.cs`, `UriNormalizationException.cs`, `UriResolutionException.cs`.
- `IriSyntaxException.cs`, `IriConversionException.cs`.
- `UriTemplateSyntaxException.cs`, `UriTemplateExpansionException.cs`.
- `UrnSyntaxException.cs`, `UrnEquivalenceException.cs`.
- `WebLinkSyntaxException.cs`, `WebLinkResolutionException.cs`.

Each exception has one constructor accepting the corresponding diagnostic/result context and derives directly from `System.Exception`.

### `src/Api/Classes`

- `UriParser.cs`, `UriValidator.cs`, `UriComposer.cs`, `PercentCodec.cs`, `UriNormalizer.cs`, `UriComparer.cs`, `UriResolver.cs`, `UriRelativizer.cs`, `UriEditor.cs`, `UriQueryCodec.cs`, `UriPathUtility.cs`.
- `IriParser.cs`, `IriConverter.cs`, `IriNormalizer.cs`, `IriComparer.cs`.
- `UriTemplateParser.cs`, `UriTemplateExpander.cs`, `UriTemplateComposer.cs`, `UriTemplateInspector.cs`, `UriTemplateProcessor.cs`.
- `UrnParser.cs`, `UrnNormalizer.cs`, `UrnComparer.cs`, `UrnLocatorUtility.cs`.
- `WebLinkParser.cs`, `WebLinkComposer.cs`, `WebLinkResolver.cs`, `WebLinkQuery.cs`.

Each class implements the like-named contract and delegates shared behavior to non-public core classes.

## Planned internal source files

Non-public implementation types live outside `src/Api` and remain classes with fields/methods compatible with `syntax.md`.

- `src/Internal/Text/SpanCursor.cs`, `TextOutput.cs`, `UnicodeScalar.cs`, `Utf8CodecCore.cs`, `Ascii.cs`, `AsciiCase.cs`.
- `src/Internal/Diagnostics/DiagnosticFactory.cs`, `LimitGuard.cs`.
- `src/Internal/Percent/PercentScanner.cs`, `PercentEncoderCore.cs`, `PercentDecoderCore.cs`, `PercentNormalizerCore.cs`.
- `src/Internal/Uri/UriGrammar.cs`, `UriParserCore.cs`, `AuthorityParser.cs`, `IpV4Parser.cs`, `IpV6Parser.cs`, `IpFutureParser.cs`, `UriComposerCore.cs`, `UriValidatorCore.cs`, `PathAlgorithm.cs`, `UriNormalizerCore.cs`, `UriComparerCore.cs`, `UriResolverCore.cs`, `UriRelativizerCore.cs`, `UriEditorCore.cs`, `QueryCodecCore.cs`.
- `src/Internal/Iri/IriGrammar.cs`, `IriParserCore.cs`, `IriConverterCore.cs`, `IdnHostCore.cs`, `IriNormalizerCore.cs`, `IriComparerCore.cs`, `BidiValidator.cs`.
- `src/Internal/Template/TemplateGrammar.cs`, `TemplateParserCore.cs`, `TemplateOperatorInfo.cs`, `TemplateEncoder.cs`, `TemplateExpanderCore.cs`, `TemplateComposerCore.cs`, `TemplateInspectorCore.cs`.
- `src/Internal/Urn/UrnGrammar.cs`, `UrnParserCore.cs`, `UrnNormalizerCore.cs`, `UrnComparerCore.cs`, `UrnLocatorCore.cs`.
- `src/Internal/WebLink/WebLinkGrammar.cs`, `HttpQuotedString.cs`, `ExtendedParameterCodec.cs`, `LanguageTagValidator.cs`, `WebLinkParserCore.cs`, `WebLinkComposerCore.cs`, `WebLinkResolverCore.cs`, `WebLinkQueryCore.cs`.

## Test layout and coverage policy

- `tests/Unit/` mirrors every public contract and tests each public method's success, expected-failure result, throwing failure, option modes, null/empty boundaries, and result-field semantics.
- `tests/Conformance/Rfc3986/`, `Rfc3987/`, `Rfc6570/`, `Rfc8141/`, and `Rfc8288/` contain section-labeled examples, ABNF boundaries, errata cases, and normative algorithm vectors.
- `tests/Conformance/Unicode/` consumes the downloaded normalization and IDNA vectors only where they match the explicitly selected .NET mapping mode.
- `tests/Conformance/UriTemplate/` consumes every downloaded RFC, extended, and negative JSON vector.
- `tests/Generated/` produces bounded valid and one-mutation-invalid component/reference cases, resolution/relativization round trips, compose/parse round trips, URI/IRI conversion round trips, and parse/compose Link round trips. These are correctness tests, not benchmarks.
- `tests/Integration/` tests cross-layer workflows such as template expansion to normalized URI, URN-to-URI modeling, and Link IRI conversion/resolution.
- Coverage is complete only when every public `src/Api` method, every enum-controlled behavior, every public result/exception path, and every documented absent-versus-empty field state has a test. Coverage collection is used to find gaps; no syntax-policy or source-layout checker tests are added.

## Implementation tasks

### Foundation

- [DONE] T001: Create the planned `src/Api` and `src/Internal` directory/namespace layout and the matching behavior-focused test directories.
- [DONE] T002: Implement common diagnostic enums, `TextRange`, `UriDiagnostic`, and diagnostic factories with stable offsets, lengths, components, severities, and messages.
- [DONE] T003: Implement `OperationLimits`, per-operation option data, and internal limit guards for input, collection, nesting, and output limits.
- [DONE] T004: Implement the constant-only static API classes for URI characters, scheme/default-port values, URI Template operators, and Web Link attribute names.
- [DONE] T005: Implement culture-invariant ASCII classification, hex conversion, and ordinal ASCII case folding used by all grammars.
- [DONE] T006: Implement UTF-16 scalar scanning and strict UTF-8 encode/decode cores, including non-BMP characters, malformed surrogates, overlong encodings, and invalid scalar rejection.
- [DONE] T007: Implement span cursor and bounded text-output helpers that report offsets without `ref`, `out`, unsafe code, properties, or exception handling.
- [DONE] T008: Add unit tests for diagnostics, limits, ASCII rules, Unicode scalar handling, UTF-8 validity, and bounded output behavior.

### Percent encoding

- [DONE] T009: Implement component-specific percent-encoding allowlists for all URI, IRI, URN, Template, and Link use sites.
- [DONE] T010: Implement exact percent-triplet scanning and validation with first-error and all-error diagnostic modes.
- [DONE] T011: Implement uppercase percent encoding of arbitrary bytes and strict UTF-8 encoding of Unicode text and spans.
- [DONE] T012: Implement percent decoding to bytes without character-set assumptions.
- [DONE] T013: Implement strict and replacement-mode percent decoding from legal UTF-8 octet sequences to Unicode text.
- [DONE] T014: Implement percent-triplet hex-case normalization and unreserved-only decoding while preserving reserved delimiter semantics.
- [DONE] T015: Implement `IPercentCodec`, `PercentCodec`, option/result data, and `PercentEncodingException` with distinct non-overloaded entry points.
- [DONE] T016: Add exhaustive unit and generated tests for every encode set, byte value, hex case, malformed triplet position, UTF-8 class, reserved character, double-encoding boundary, and public percent API path.

### RFC 3986 parsing, validation, and composition

- [DONE] T017: Implement `UriAuthority`, `UriReference`, URI parse/validation option data, result data, reference/host/component enums, and syntax exceptions.
- [DONE] T018: Implement scheme scanning and first-match URI-versus-relative-reference disambiguation.
- [DONE] T019: Implement hierarchical-part and relative-part splitting with exact authority, query, and fragment delimiter-presence tracking.
- [DONE] T020: Implement authority boundary parsing using the final `@`, bracket rules, empty-host/empty-port preservation, and component-relative diagnostic offsets.
- [DONE] T021: Implement strict RFC 3986 userinfo and registered-name validation, including percent triplets and host case semantics.
- [DONE] T022: Implement strict four-octet decimal IPv4 parsing and reject abbreviated, hexadecimal, octal, signed, whitespace, and overflow forms accepted by broader platform parsers.
- [DONE] T023: Implement all RFC 3986 IPv6 grammar alternatives, zero compression, embedded strict IPv4, 128-bit storage, and detailed invalid-group diagnostics.
- [DONE] T024: Implement IPvFuture parsing and reject IPv6 zone identifiers under the current RFC 3986/RFC 9844 standards baseline.
- [DONE] T025: Implement `path-abempty`, `path-absolute`, `path-noscheme`, `path-rootless`, and `path-empty` parsing with first-segment colon and leading-slash constraints.
- [DONE] T026: Implement query and fragment scanning with encoded delimiter handling and exact absent-versus-empty states.
- [DONE] T027: Assemble whole-input parsers for URI-reference, URI, absolute-URI, and relative-ref, including span entry points and reference classification.
- [DONE] T028: Implement configurable whole-reference and component validation, requirement policies, first diagnostic, and all-diagnostics collection.
- [DONE] T029: Implement lossless URI recomposition from parsed data, validated composition from caller-created data, and safe output-length measurement.
- [DONE] T030: Implement `IUriParser`/`UriParser`, `IUriValidator`/`UriValidator`, and `IUriComposer`/`UriComposer` with throwing and exception-less paths.
- [DONE] T031: Add RFC 3986 Appendix A grammar and section example conformance tests for all URI forms and component boundaries.
- [DONE] T032: Add authority tests covering multiple `@`, encoded delimiters, empty subcomponents, every host kind, ambiguous IPv4/reg-name inputs, and invalid bracket/port combinations.
- [DONE] T033: Add complete IPv4, IPv6, embedded-IPv4, IPvFuture, and zone-identifier positive/negative tests, including RFC 5952 inputs without yet asserting normalization.
- [DONE] T034: Add public parser, validator, and composer API tests for every method, option, exception/result path, diagnostic field, lossless round trip, and caller-created invalid model.

### Normalization and comparison

- [DONE] T035: Implement RFC 3986 dot-segment removal exactly as section 5.2.4 while preserving non-dot empty segments and relative-path semantics.
- [DONE] T036: Implement generic case normalization for scheme and registered-name host without changing case-sensitive components.
- [DONE] T037: Integrate percent hex normalization and unreserved decoding in the RFC-prescribed component order.
- [DONE] T038: Implement canonical strict IPv4 serialization and RFC 5952 IPv6 serialization, including longest/first zero-run selection and embedded IPv4 cases.
- [DONE] T039: Implement syntax normalization analysis that reports every component-level change before applying it.
- [DONE] T040: Implement full syntax-based normalization while preserving absent/empty query, fragment, authority, and port distinctions not declared equivalent.
- [DONE] T041: Implement declarative `UriSchemeProfile` normalization for empty paths, empty/default ports, and profile-selected delimiter cleanup.
- [DONE] T042: Implement built-in HTTP, HTTPS, WS, WSS, and FTP profile construction from constant static data without a mutable global registry.
- [DONE] T043: Implement exact, syntax-normalized, and scheme-profile URI comparison with first-difference component reporting.
- [DONE] T044: Implement `IUriNormalizer`/`UriNormalizer`, `IUriComparer`/`UriComparer`, their option/result data, and normalization exceptions.
- [DONE] T045: Add RFC 3986 section 6 normalization and comparison vectors, including false-negative-safe behavior and percent-encoded reserved distinctions.
- [DONE] T046: Add dot-segment tests for RFC normal/abnormal examples, repeated slashes, encoded dots, root boundaries, and relative paths.
- [DONE] T047: Add RFC 5952 canonical IPv6 tests for case, leading zeros, zero-run ties, single zero fields, IPv4 embedding, and idempotence.
- [DONE] T048: Add scheme-profile tests for default/empty ports and empty authority paths, proving that generic normalization does not apply profile semantics.
- [DONE] T049: Add normalization idempotence and compose/parse/normalize generated tests across all component forms.
- [DONE] T050: Add complete public normalizer/comparer API tests for every comparison level, option, change enum, diagnostic, result, and exception path.

### Resolution, relativization, editing, paths, and queries

- [DONE] T051: Implement RFC 3986 path merge and reference-resolution transformations for strict mode.
- [DONE] T052: Implement identical-scheme compatibility resolution as an explicit mode separate from strict processing.
- [DONE] T053: Implement resolver validation for absolute bases, component inheritance, authority replacement, base-query inheritance, and fragment handling.
- [DONE] T054: Implement inverse reference creation across same-document, query/fragment-only, relative-path, absolute-path, network-path, and absolute fallbacks.
- [DONE] T055: Implement ambiguity guards for colon-bearing first segments and leading double slashes in generated relative references.
- [DONE] T056: Implement relativization selection modes and round-trip verification against the chosen URI comparison policy.
- [DONE] T057: Implement `IUriResolver`/`UriResolver` and `IUriRelativizer`/`UriRelativizer` with result and throwing failure paths.
- [DONE] T058: Add every RFC 3986 section 5.4 normal and abnormal resolution vector in strict and compatible modes.
- [DONE] T059: Add generated resolution/relativization round-trip tests for directory/file bases, queries, fragments, path ambiguity, authorities, and schemes.
- [DONE] T060: Add complete public resolver/relativizer API tests for invalid bases, limits, options, result diagnostics, and exceptions.
- [DONE] T061: Implement component construction and explicit keep/set/clear editing with post-edit grammar validation.
- [DONE] T062: Implement `IUriEditor`/`UriEditor` and test every component edit, delimiter state, host-kind transition, invalid combination, result, and exception path.
- [DONE] T063: Implement ordered plain-RFC query parsing that preserves duplicates, missing equals signs, empty names/values, and original order.
- [DONE] T064: Implement query composition with configurable pair delimiters and component-correct percent encoding.
- [DONE] T065: Implement explicit form-query mode with `+`/space conversion and strict percent/UTF-8 behavior.
- [DONE] T066: Implement opt-in add, replace, remove, ordinal sort, and deduplicate query operations without changing generic URI normalization.
- [DONE] T067: Implement `IUriQueryCodec`/`UriQueryCodec` with query option/result data and bounded item/output handling.
- [DONE] T068: Add query tests for duplicate stability, separators, empty states, `+`, encoded delimiters, Unicode, ordering, sorting, deduplication, malformed input, and every public method.
- [DONE] T069: Implement path split, join, common-base, and standalone dot-segment utilities without decoding reserved separators.
- [DONE] T070: Implement `IUriPathUtility`/`UriPathUtility` and test roots, trailing/repeated separators, encoded slashes, empty paths, relative paths, and every public method.

### RFC 3987 IRIs and IDN hosts

- [DONE] T071: Implement IRI data/options/results and IRI-specific grammar tables for `ucschar`, `iprivate`, and internationalized components.
- [DONE] T072: Implement IRI authority, registered-name, path, query, and fragment scanners by extending only the RFC 3987 productions over the strict URI core.
- [DONE] T073: Implement whole-input IRI-reference, IRI, absolute-IRI, and relative-IRI parsing with span, result, and throwing entry points.
- [DONE] T074: Implement IRI-to-URI conversion using strict UTF-8 percent encoding, existing-triplet preservation, and component-aware allowed sets.
- [DONE] T075: Implement URI-to-IRI conversion that decodes only complete legal UTF-8 sequences safe in the destination component and preserves reserved/illegal/ambiguous encodings.
- [DONE] T076: Implement prevalidated built-in .NET IDN host conversion, A-label/U-label round-trip checks, label/domain length checks, and explicit mapping-mode diagnostics.
- [DONE] T077: Implement requested NFC normalization and diagnose-only handling using built-in .NET normalization without silently normalizing parse results.
- [DONE] T078: Implement RFC 3987 component bidi diagnostics and applicable IDN label bidi checks.
- [DONE] T079: Implement exact Unicode, NFC, and mapped-URI IRI comparison modes.
- [DONE] T080: Implement `IIriParser`/`IriParser`, `IIriConverter`/`IriConverter`, `IIriNormalizer`/`IriNormalizer`, and `IIriComparer`/`IriComparer`.
- [DONE] T081: Add RFC 3987 syntax examples and ABNF-boundary tests for every allowed/excluded Unicode range and `iprivate` placement.
- [DONE] T082: Add RFC 3987 IRI-to-URI and URI-to-IRI step/example tests, including invalid UTF-8, encoded reserved characters, non-BMP scalars, and round-trip guarantees.
- [DONE] T083: Add Unicode normalization conformance tests using the downloaded UAX 15 corpus for the modes exposed by the public API.
- [DONE] T084: Add IDN conversion tests using applicable downloaded UTS 46/IDNA vectors, platform-mode expectations, invalid labels, separator mappings, bidi cases, and round trips.
- [DONE] T085: Add generated URI/IRI conversion and comparison idempotence tests across components and Unicode scalar ranges.
- [DONE] T086: Add complete public IRI API tests for each form, mapping mode, normalization mode, comparison level, result diagnostic, limit, and exception path.

### RFC 6570 URI Templates

- [DONE] T087: Implement template model, part/variable/value/binding data, options/results, levels, operators, and template exceptions.
- [DONE] T088: Implement template literal scanning for legal printable Unicode, valid percent triplets, brace balance, and source ranges.
- [DONE] T089: Implement expression/operator/variable-list parsing, case-sensitive variable names, percent-encoded name preservation, and reserved-future operator errors.
- [DONE] T090: Implement prefix and explode modifier parsing, Level 1–4 classification, incompatibility validation, and exact diagnostic locations.
- [DONE] T091: Implement operator metadata for prefix, separator, named expansion, empty-value behavior, and reserved-character allowance.
- [DONE] T092: Implement literal and scalar expansion with strict UTF-8 encoding, reserved expansion behavior, undefined/empty distinctions, and Unicode-scalar prefix lengths.
- [DONE] T093: Implement list expansion with and without explode for all eight defined operators.
- [DONE] T094: Implement ordered associative expansion with and without explode for all eight defined operators, including undefined members and encoded names/values.
- [DONE] T095: Implement strict, result-based, and RFC diagnostic-output expansion paths with output/member limits.
- [DONE] T096: Implement explicitly non-standard partial expansion that preserves unresolved expressions as valid templates.
- [DONE] T097: Implement template model composition, opt-in percent-triplet case canonicalization, ordered variable inspection, occurrence inspection, and level determination.
- [DONE] T098: Implement high-level expand-to-URI, expand-and-resolve, and expand-and-normalize workflows by composing existing services.
- [DONE] T099: Implement all five URI Template contracts and implementation classes with distinct non-overloaded methods.
- [DONE] T100: Import the downloaded URI Template JSON corpora as test data and execute every RFC overview, section, extended, and negative vector.
- [DONE] T101: Add focused parser tests for Unicode literals, pct-encoded variable names, dots, commas, empty lists, braces, future operators, prefixes 1/9999, and invalid modifiers.
- [DONE] T102: Add a scalar/list/map-by-operator test matrix covering explode, empty, undefined, reserved characters, Unicode, member order, and prefix behavior.
- [DONE] T103: Add tests for diagnostic partial output, strict exceptions, limits, partial expansion, recomposition, canonicalization, inspection, and all high-level workflows.
- [DONE] T104: Add complete public URI Template API tests for every method, enum value, model/result field state, option, diagnostic, and exception path.

### RFC 8141 URNs

- [DONE] T105: Implement URN model/options/results, validation/equivalence/collision enums, namespace-handler contract, and URN exceptions.
- [DONE] T106: Implement case-insensitive `urn` scheme and 2–32 character NID parsing with generic syntax and optional registration-rule validation modes.
- [DONE] T107: Implement non-empty NSS parsing with pchar/slash rules, strict ASCII wire format, and exact percent diagnostics.
- [DONE] T108: Implement ordered r-, q-, and f-component delimiter parsing, bare-question-mark rejection, and component-presence preservation.
- [DONE] T109: Implement URN conversion to/from the generic URI model without losing assigned-name or rqf boundaries.
- [DONE] T110: Implement generic URN lexical normalization without percent-decoding the NSS.
- [DONE] T111: Implement exact, assigned-name, and optional namespace-handler equivalence modes while excluding rqf components from assigned-name equality.
- [DONE] T112: Implement locator utilities for separating assigned names and applying q/f components under explicit collision policies.
- [DONE] T113: Implement `IUrnParser`/`UrnParser`, `IUrnNormalizer`/`UrnNormalizer`, `IUrnComparer`/`UrnComparer`, and `IUrnLocatorUtility`/`UrnLocatorUtility`.
- [DONE] T114: Add RFC 8141 syntax, optional-component, percent-encoding, NID, and invalid delimiter-order conformance vectors.
- [DONE] T115: Add RFC 8141 equivalence tests proving NID/scheme case folding, NSS percent-case behavior, no percent decoding, rqf exclusion, and namespace-handler extension behavior.
- [DONE] T116: Add locator q/f transfer tests for every collision policy and proof that r-component semantics are not invented by the generic utility.
- [DONE] T117: Add complete public URN API tests for every method, validation/equivalence mode, model/result field, diagnostic, limit, handler failure, and exception path.

### RFC 8288 Web Links

- [DONE] T118: Implement Web Link model/options/results/filter data, parameter/relation/parse enums, and Link exceptions.
- [DONE] T119: Implement HTTP optional-whitespace, token, quoted-string, quoted-pair, angle-target, semicolon, and comma scanners with absolute input offsets.
- [DONE] T120: Implement strict parsing of individual link-values and multiple combined field values without splitting quoted or bracketed commas.
- [DONE] T121: Implement the RFC 8288 Appendix B recovery parser as an explicit mode that retains diagnostics for skipped or repaired input.
- [DONE] T122: Implement relation-list parsing, registered-token case handling, extension relation absolute-URI validation, and `rel`/`rev` interpretation.
- [DONE] T123: Implement standard attribute handling for `anchor`, repeated `hreflang`, `media`, `title`, `title*`, and `type`, while preserving all extension/raw parameters in order.
- [DONE] T124: Implement RFC first-occurrence behavior, repeatable-attribute behavior, and diagnostics for ignored duplicate singleton parameters.
- [DONE] T125: Implement RFC 8187 extended-value decoding/encoding with charset, language, raw-value preservation, UTF-8 support, and unsupported/malformed diagnostics.
- [DONE] T126: Implement RFC 5646 language-tag validation for decoded extended values and `hreflang` without changing raw parameter text.
- [DONE] T127: Implement deterministic Link field serialization with token/quoted selection, escapes, angle targets, parameter ordering, extended values, and no obsolete line folding.
- [DONE] T128: Implement target resolution against effective context and anchor resolution against representation context, retaining unhonored-anchor outcomes.
- [DONE] T129: Implement IRI-to-URI conversion for HTTP Link serialization and URI-to-IRI application views through the existing converter.
- [DONE] T130: Implement link filtering by relation, target, context, media type, language, and extension attribute with field-appropriate comparison.
- [DONE] T131: Implement all four Web Link contracts and implementation classes with bounded link/parameter/output processing.
- [DONE] T132: Add RFC 8288 section examples and strict grammar tests for combined fields, commas, whitespace, quoted strings, angle targets, and all standard parameters.
- [DONE] T133: Add Appendix B recovery tests for truncated targets, malformed parameters, bad quotes, stray separators, multiple fields, and every recovery diagnostic.
- [DONE] T134: Add relation tests for multiple values, case, extensions, invalid relative extension relations, `rev`, duplicates, and filtering.
- [DONE] T135: Add RFC 8187 and language-tag tests for valid UTF-8, language metadata, percent errors, unsupported charsets, fallback title behavior, and round-trip serialization.
- [DONE] T136: Add resolution tests for relative targets, relative/absolute anchors, missing representation context, unhonored anchors, fragments, IRIs, and q/f preservation.
- [DONE] T137: Add generated parse/serialize/parse Web Link round-trip tests preserving semantic link data and explicit ordering guarantees.
- [DONE] T138: Add complete public Web Link API tests for every method, strict/recovery mode, parameter kind, model/result field, filter, limit, diagnostic, and exception path.

### Cross-layer integration and completion

- [DONE] T139: Add integration tests that parse, normalize, compare, resolve, relativize, edit, and recompose the same URI references without losing absent/empty component states.
- [DONE] T140: Add integration tests for IRI conversion followed by URI normalization/resolution and for reversible application display conversion.
- [DONE] T141: Add integration tests for URI Template expansion into URI/IRI parsing, base resolution, normalization, and failure propagation.
- [DONE] T142: Add integration tests for URNs as generic URIs, assigned-name equivalence, locator component transfer, and Web Link targets/contexts containing URNs.
- [DONE] T143: Add integration tests for Link parsing, IRI conversion, target/anchor resolution, filtering, and serialization as one workflow.
- [DONE] T144: Add generated one-mutation-invalid grammar tests for every URI/IRI/URN component and verify stable first-error offsets.
- [DONE] T145: Add adversarial correctness tests for encoded delimiter confusion, userinfo spoofing boundaries, bidi controls, malformed Unicode, expansion amplification limits, and malicious Link quoting.
- [DONE] T146: Exercise every public enum member and option combination that changes behavior, including incompatible-option diagnostics.
- [DONE] T147: Exercise every public data field's meaningful absent, empty, populated, and invalid-caller-created states through the contracts that consume it.
- [DONE] T148: Exercise every public exception type and verify its diagnostic context matches the corresponding exception-less result.
- [DONE] T149: Run coverage collection, map every public `src/Api` method and behavior branch to a named test, and close all uncovered API-behavior gaps.
- [DONE] T150: Run the full test suite on clean builds with no production dependency beyond the built-in .NET framework, and resolve all correctness, determinism, and warning failures.
