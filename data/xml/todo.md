# Universal XML library development plan

## Objective

Build a self-contained .NET 10 library for standards-conforming XML 1.0/1.1 parsing, namespace processing, DTD validation, mutable tree processing, push and pull streaming, serialization and canonicalization, XPath 3.1, and XSLT 3.0. Production code under src/ may use only the .NET base class libraries. Tests may use external packages and the downloaded conformance suites.

The implementation is synchronous because syntax.md forbids await. Incremental push and pull APIs provide bounded-memory integration points for applications that need nonblocking I/O orchestration outside this library. ReadOnlySpan<char>, Span<char>, ReadOnlySpan<byte>, and Span<byte> should be used inside scanners, codecs, escaping, and serialization where their lifetime rules fit; public resumable state uses arrays or ReadOnlyMemory<T>.

## Standards and conformance targets

The normative and supporting material is stored under specs/ and indexed by specs/SOURCES.md.

- XML processors: XML 1.0 Fifth Edition and XML 1.1 Second Edition, both validating and non-validating behavior, with all well-formedness constraints, validity constraints, entity handling, encodings, and standalone-document rules.
- Namespace processors: Namespaces in XML 1.0 Third Edition and 1.1 Second Edition, including namespace undeclaration differences and reserved-prefix rules.
- Tree model: XML Information Set, DOM Level 3 Core concepts, DOM Load/Save concepts, traversal, ranges, XML Base, and xml:id. The public model is idiomatic to this restricted C# API rather than a source-compatible W3C DOM binding.
- Streaming: SAX2-equivalent push events plus a cursor/pull reader and an incremental byte feeder.
- Resolution: RFC 3986/3987/8089 URI handling, OASIS XML Catalogs 1.1, XInclude 1.0 plus the XPointer framework and element() scheme.
- Serialization: XSLT and XQuery Serialization 3.1, XML escaping, and configurable XML, XHTML, HTML, text, JSON, and adaptive methods.
- Canonicalization: Canonical XML 1.1 and Exclusive XML Canonicalization 1.0. Canonical XML 2.0 is an experimental hook/profile because its local document is a W3C Note, not a Recommendation.
- Query: XPath 3.1, XDM 3.1, Functions and Operators 3.1, XML Schema 1.1 built-in datatypes, maps, arrays, and function items.
- Transformation: XSLT 3.0 using XPath 3.1. The target is a full basic XSLT 3.0 processor first, followed by optional schema-aware and streaming conformance tiers.
- Optional schema-aware tier: XML Schema 1.1 structures and datatypes sufficient for validation, type annotation, schema-element/schema-attribute tests, typed modes, validation instructions, and XSLT schema-awareness.
- Test authority: W3C XML Conformance Test Suite, W3C QT3, and W3C XSLT 3.0 suites downloaded under specs/test-suites/.

Conformance reports must distinguish supported standard versions and optional features. Recovery extensions must never silently change the behavior of strict conformance mode.

## Scope boundaries

- Included: document and external parsed entities, DTD validation, XDM built-in schema types, XML Schema 1.1 in the optional schema-aware tier, catalogs, XInclude, XPath 3.1, and XSLT 3.0.
- Included through hooks, not built-in policy: application URI schemes, network retrieval, extension XPath/XSLT functions and instructions, custom collations, result-document destinations, tracing, and canonicalization consumers such as signature code.
- Excluded: XQuery language syntax, XSL-FO rendering, XML Signature/encryption, HTML error-recovery parsing, CSS selectors, Relax NG, Schematron, EXSLT compatibility libraries, and vendor-specific Saxon/Xalan/libxml2 extensions.
- External resource access is denied by default. Callers opt in through resolvers and explicit scheme/root/size limits. DTD and entity expansion are bounded independently to prevent exponential and quadratic expansion attacks.
- No performance/benchmark, documentation-site, packaging, or release work is part of this plan.

## Complete feature inventory, low level to high level

### 1. Bytes, characters, names, and lexical processing

- Byte-order-mark detection; XML declaration/text declaration parsing; encoding sniffing; UTF-8 and UTF-16BE/LE; caller-supplied built-in .NET encodings; encoding-name matching; malformed byte detection; declaration/transport encoding conflict diagnostics.
- XML 1.0 versus 1.1 character repertoires, restricted characters, surrogate and scalar validation, legal character references, noncharacters where allowed, and XML 1.1 normalization checking hooks.
- End-of-line normalization for document entities and external entities, including XML 1.1 NEL and line-separator handling.
- Span-based scanning for markup delimiters, names, Nmtokens, whitespace, public identifiers, system literals, quoted values, character/entity references, CDATA sections, comments, processing instructions, XML declarations, and text declarations.
- Unicode-aware NameStartChar/NameChar, NCName, QName, prefix/local-name splitting, expanded-name equality, reserved names, and qualified-name construction checks.
- Source tracking by absolute character/byte offset, one-based line and column, source URI, entity/include stack, and ranges for syntax nodes.
- Exact conformance errors for malformed markup, forbidden delimiter sequences, duplicate attributes, mismatched tags, illegal declarations, premature EOF, and trailing document content.

### 2. XML document and fragment parsing

- Prolog, XML declaration, comments, processing instructions, document type declaration, exactly one document element, epilog, and document-level whitespace.
- Element start/end/empty tags; normalized attributes; character data; CDATA; comments; processing instructions; predefined and numeric references; entity boundaries when requested.
- XML 1.0 and 1.1 documents, external parsed entities, and opt-in balanced fragments with inherited namespace/base/lang/space context.
- Strict, recover-none parsing plus configurable preservation of CDATA boundaries, entity-reference nodes, ignorable whitespace, comments, processing instructions, lexical prefix choices, and original source locations.
- Inputs from string, char memory, byte memory, Stream, and TextReader represented by one XmlInput data object rather than method overloads.
- Configurable cancellation polling through a caller-owned interface, depth/node/text/attribute/name/DTD/entity/resource limits, and deterministic reset/disposal behavior using ordinary synchronous methods.

### 3. Namespaces in XML

- Namespace declaration recognition, default namespaces, prefix scopes, expanded names, in-scope namespace calculation, undeclaration rules, and namespace fixup.
- No default namespace for attributes; namespace-aware duplicate attribute detection; QName prefix binding; xml and xmlns reserved binding constraints.
- Namespace events for push/pull APIs and optional retention of declaration attributes in the tree.
- Prefix generation and conflict repair for tree edits, copying, serialization, XSLT namespace construction, and result trees.

### 4. DTDs, entities, notations, and validation

- Internal/external subsets; declarations for elements, attribute lists, general entities, parameter entities, and notations; conditional INCLUDE/IGNORE sections in external subsets.
- Parameter-entity recognition and nesting, declaration precedence, external subset inclusion, text declarations, public/system identifiers, unparsed entities, and notation association.
- Entity replacement, recursive-entity detection, standalone constraints, entity-boundary reporting, skipped/unresolved entity reporting, and base URI tracking across entity stacks.
- Attribute types CDATA, ID, IDREF(S), ENTITY(IES), NMTOKEN(S), and enumerations/NOTATION; whitespace normalization; declared/default/fixed values; required attributes.
- Element content models EMPTY, ANY, mixed content, children sequences/choices, occurrence operators, deterministic-content-model checks, and runtime validation.
- Document validity: root/DOCTYPE agreement, declared elements/attributes/entities/notations, ID uniqueness, deferred IDREF resolution, notation validity, element content, standalone validity, and validity diagnostics that can accumulate.
- Pluggable resource and catalog resolution, URI normalization, public identifier normalization, catalog exact/rewrite/delegate/nextCatalog behavior, caching, cycle detection, and security policy.

### 5. Event, pull, and tree APIs

- SAX-style push callbacks for document/entity/DTD boundaries, namespace mappings, start/end elements, attributes, text, ignorable whitespace, CDATA, comments, processing instructions, declarations, skipped entities, and diagnostics.
- Incremental byte/character feeding, pause/resume/abort, final-block detection, and event backpressure by handler response.
- Pull cursor events exposing current kind, name, value, attributes, namespaces, depth, base URI, xml:lang/xml:space, empty-element status, source location, and subtree skipping.
- Mutable information-set tree with document, document fragment, element, attribute, namespace, text, CDATA, entity reference, processing instruction, comment, and document type node kinds.
- Parent/owner-document links; ordered children and attributes; node identity/document order; clone/import/adopt; insert/append/replace/remove; normalize/coalesce; text content; namespace lookup; ID lookup.
- Tree invariants: one root/DOCTYPE ordering for documents, permitted child types, no cycles, unique attributes by expanded name, owner transfer rules, read-only/frozen snapshots, and mutation versioning.
- Traversal in all XPath axes, filtered iterators, ranges/positions, and stable behavior or explicit invalidation when the tree mutates.
- Builder from parser events, event emitter from trees, and adapters between push, pull, tree, XDM, XPath, XSLT, and serializer layers.

### 6. Related XML processing

- Effective xml:base calculation and URI resolution; inherited xml:lang and xml:space; xml:id recognition and normalization.
- XInclude XML/text inclusion, fallback, accept/accept-language metadata, encoding, parse modes, recursive includes, loop detection, fixup of xml:base/xml:lang, and XPointer element() selection.
- XML Catalog 1.1 public/system/URI resolution with groups, prefer, delegation, rewriting, suffixes where applicable, and chained catalogs.

### 7. Escaping, serialization, and canonicalization

- Context-correct escaping/unescaping for text, attributes, comments, processing instructions, CDATA splitting, public/system literals, XPath string literals, URI components, and JSON strings.
- XML 1.0/1.1 output; encoding declaration/BOM; standalone/doctype; empty-element policy; namespace fixup; illegal-character policy; indentation; newline selection; CDATA section elements; character maps; Unicode normalization; media type.
- Serialization 3.1 sequence normalization and methods: xml, xhtml, html, text, json, adaptive, and caller extension methods.
- Full serialization parameters: allow-duplicate-names, byte-order-mark, cdata-section-elements, doctype-public/system, encoding, escape-uri-attributes, html-version, include-content-type, indent, item-separator, json-node-output-method, media-type, method, normalization-form, omit-xml-declaration, standalone, suppress-indentation, undeclare-prefixes, use-character-maps, and version.
- JSON serialization of maps/arrays/atomic values/nodes, duplicate-key policy, escaping, non-finite number handling, fallback, and node output method.
- Canonical XML 1.1 inclusive canonicalization with/without comments and Exclusive XML Canonicalization 1.0 with inclusive prefix lists: document subsets, namespace/attribute ordering, inherited xml attributes, URI handling, line normalization, UTF-8 output, and no declaration/DTD.
- Streaming canonicalization sink and transform hooks so digest/signature layers can consume canonical bytes without becoming dependencies of src/.

### 8. XDM 3.1 and schema datatypes

- Every value is a sequence; empty/singleton/multiple sequences; item identity; stable document order; atomization; effective Boolean value; sequence-type matching and occurrence indicators.
- Node kinds and properties: document, element, attribute, namespace, processing instruction, comment, and text; node name, string value, typed value, type annotation, nilled, base/document URI, parent, children, attributes, namespace nodes, unparsed entities.
- Atomic hierarchy: untypedAtomic, string and derived string types, boolean, decimal/integer and all bounded integer derivations, float, double, duration/yearMonthDuration/dayTimeDuration, dateTime/dateTimeStamp/date/time and Gregorian partial dates, hexBinary, base64Binary, anyURI, QName, and NOTATION.
- Lexical/value-space parsing, canonical lexical representation, casts/castability, numeric promotion, subtype relationships, timezone handling, duration arithmetic, QName namespace sensitivity, NaN/INF/signed zero, rounding, and overflow/error rules.
- Function items with QName/arity/signature and invocation; immutable maps with same-key semantics; immutable arrays of sequences; lookup; arrays/maps as functions.
- Static and dynamic contexts, focus, variables, function libraries, namespaces, base URI, default element/function namespace, collations, decimal formats, implicit timezone, current date/time, documents, collections, URI collections, environment access policy, and external resource resolvers.

### 9. XPath 3.1 parsing and evaluation

- Complete lexical grammar: comments, whitespace, EQNames/QNames, literals, numeric forms, keywords, terminal delimitation, and precise static source ranges.
- Primary/postfix expressions: literals, variables, parenthesized/context item, static calls, named function references, inline functions, function coercion, dynamic calls, predicates, argument placeholders, map/array constructors, lookup, and unary lookup.
- Paths: absolute/relative paths, / and //, all forward/reverse axes, abbreviated syntax, name/kind tests, schema tests in the schema-aware tier, predicates, document ordering, duplicate elimination, and focus changes.
- Operators and control expressions: comma, simple map, arrow, unary, arithmetic, string concatenation, range, general/value/node comparisons, and/or, union/intersect/except, if, for, let, quantified some/every, instance of, treat as, castable as, and cast as.
- Static analysis: parsing, name binding, arity resolution, variable binding, type/cardinality inference, updating prohibition, backwards-compatibility mode where required by XSLT, implementation-defined feature registry, and W3C XPST/XPTY errors.
- Dynamic evaluation: focus/context, lazy-safe sequence iteration where possible, conversions/promotions, atomization, EBV, comparison semantics/collations, node identity/order, closures, tail-call-safe function invocation, deterministic error propagation, and W3C XPDY/FO errors.
- Compile once/evaluate many, expression caching isolated by static context, variable binding, trace hooks, cancellation/limits, and thread-safe immutable compiled expressions.

### 10. XPath Functions and Operators 3.1

- Context/cardinality: position, last, current date/time/timezone, default collation/language, static base URI, zero-or-one, one-or-more, exactly-one, empty, exists, head, tail, count.
- Boolean/numeric: true, false, boolean, not, number, abs, ceiling, floor, round, round-half-to-even, numeric arithmetic/comparison, sum, avg, min, max.
- Strings/codepoints: string, codepoints-to-string, string-to-codepoints, compare, codepoint-equal, concat, string-join, substring, string-length, normalize-space, normalize-unicode, upper/lower-case, translate, contains, starts/ends-with, substring-before/after, contains-token.
- Regular expressions: matches, replace, tokenize, analyze-string using XML Schema regex syntax, flags, captures, zero-length rules, replacement-string rules, and regex errors.
- URI/QName: resolve-uri, encode-for-uri, iri-to-uri, escape-html-uri, QName, resolve-QName, prefix/local-name/namespace-uri from QName, namespace-uri-for-prefix, in-scope-prefixes.
- Node functions: name, local-name, namespace-uri, node-name, nilled, string/data, base-uri, document-uri, root, path, has-children, innermost, outermost, generate-id, lang, id, element-with-id, idref.
- Sequences: index-of, distinct-values, insert-before, remove, reverse, subsequence, unordered, deep-equal, sort, for-each, filter, fold-left/right, for-each-pair.
- Date/time/duration: component extraction, timezone adjustment, constructors, comparisons, add/subtract/multiply/divide, date/time subtraction, format-date, format-time, and format-dateTime.
- Binary/math: hex/base64 comparison and conversion; math pi, exp/exp10, log/log10, pow, sqrt, sin, cos, tan, asin, acos, atan, and atan2.
- Maps/arrays: size, keys, contains, get, put, entry, remove, merge, for-each, find; array size/get/put/append/subarray/remove/insert-before/head/tail/reverse/join/flatten/for-each/filter/folds/pair/sort.
- Function items: function-lookup, function-name, function-arity, apply, closures, coercion, and higher-order callback error behavior.
- Documents/collections/text/environment: doc, doc-available, collection, uri-collection, unparsed-text, unparsed-text-lines, availability, environment-variable and available-environment-variables under explicit policy.
- Parsing/JSON/serialization/transform: parse-xml, parse-xml-fragment, parse-json, json-doc, json-to-xml, xml-to-json, serialize, transform, parse-ietf-date, random-number-generator, trace, and error. load-xquery-module is recognized and reports the specified unsupported-optional-feature error because XQuery is outside scope.
- Collations, Unicode/codepoint behavior, type promotion, operator mappings, rounding/timezone stability, deterministic current-time snapshot, and every mandated FO error code.

### 11. XSLT 3.0 stylesheet compilation

- Stylesheet/transform and package roots; simplified stylesheets; namespace/version handling; forwards/backwards compatibility; extension and excluded-result prefixes.
- Modules: include/import precedence, URI/base resolution, cycles, top-level declaration assembly, package use, accept/expose/override, component visibility, abstract/final components, symbolic component references, and package version selection.
- Static context, static variables/parameters, shadow attributes, use-when, default namespaces/collations/xpath-default-namespace, expand-text/text value templates, extension availability, and static error reporting.
- Declarations: modes, templates, functions, variables/parameters, attribute sets, keys, accumulators, decimal formats, output formats, character maps, namespace aliases, whitespace strip/preserve, global context item, import-schema, and package metadata.
- XPath compilation for select/test/match/use/group/sort/key/accumulator expressions; attribute value templates; sequence types; patterns; streamability analysis; dependency/cycle analysis; duplicate/conflict rules.
- Pattern grammar and matching: node/name/kind tests, axes allowed by patterns, predicates, id/key/rooted patterns, union/intersect/except, priority calculation, import precedence, declaration order, modes, and next-match/apply-imports candidate chains.

### 12. XSLT 3.0 transformation execution

- Initial match selection, initial named template/function/mode, global context, global variables/params, tunnel params, required/static params, lazy global evaluation with circularity detection, current item/group/merge/accumulator state.
- Built-in template rules and on-no-match behaviors: text-only-copy, shallow/deep copy, shallow/deep skip, fail; warning-on-no-match and on-multiple-match.
- Template instructions: apply-templates, call-template, next-match, apply-imports, with-param, context-item, sequence, value-of, copy, copy-of, document, element, attribute, namespace, text, comment, processing-instruction.
- Control and iteration: if, choose/when/otherwise, for-each, iterate/next-iteration/break/on-completion, try/catch with error variables and rollback option, assert, message/terminate, fallback.
- Construction helpers: variable/param, where-populated, on-empty, on-non-empty, use-attribute-sets, namespace inheritance/copy-namespaces, duplicate attributes/namespaces, and complex/simple content construction.
- Sorting: sort keys, stable ordering, data types, order, case-order, lang, collation, NaN/empty handling, multi-key sorting, perform-sort.
- Grouping: for-each-group with group-by, group-adjacent, group-starting-with, group-ending-with, composite keys, collation, current-group, and current-grouping-key.
- Numbering: xsl:number single/multiple/any levels, count/from patterns, value, format tokens, language, ordinal, letter-value, grouping separator/size, and number formatting.
- Keys and indexes: declaration merge, match/use, composite, collation, per-document lazy index, key() third argument, document identity, and concurrency-safe caches.
- Accumulators: rules, pre/post values, phases, streamable restrictions, accumulator-before/after, dependency and circularity errors.
- String/regex: analyze-string, matching-substring, non-matching-substring, regex-group, text value templates, disable-output-escaping compatibility behavior.
- XSLT function library: current, current-group, current-grouping-key, current-merge-group, current-merge-key, document, key, unparsed-entity-uri, unparsed-entity-public-id, format-number, format-integer, system-property, element-available, function-available, type-available, stream-available, accumulator-before, accumulator-after, regex-group, snapshot, and xsl:original component references.
- Maps/arrays and higher-order functions: xsl:map/map-entry, xsl:array/array-member, stylesheet functions, closures, override/finality, dynamic calls, and xsl:evaluate with controlled static/dynamic context.
- Multi-document operations: document/collection/unparsed-text functions, source-document, merge/merge-source/merge-key/merge-action, base URI and document pool consistency.
- Result production: result-document, named output formats, per-result serialization, URI resolution, collision detection, principal/secondary results, sequence destinations, and caller-controlled commit/rollback.
- Packages and dynamic transformation: fn:transform options/results, public/final/abstract components, package linking, package data/version selection, and separately compiled package reuse.
- Diagnostics and recovery: XTSE/XTTE/XTDE errors, xsl:message, xsl:assert, error object variables, source stack, template/mode/component trace, configurable recovery only where the specification permits.

### 13. Streaming and extensibility

- Streaming parser, pull reader, event-to-event transform path, serializer sink, and canonicalizer sink operate without constructing a full tree when the requested operation permits.
- XSLT guaranteed-streamable analysis: posture/sweep, consuming/motionless/free-ranging expressions, grounded/striding/crawling/roaming constructs, streamable modes/templates/functions/accumulators, xsl:fork, xsl:source-document, snapshot, and copy-of.
- Runtime streamed nodes with bounded lifetime, explicit grounding, accumulator state, streamed result delivery, and deterministic errors for illegal retained access.
- Interface-based extension hooks (no delegates): URI/resource/document/collection resolver, collation, XPath function, XSLT instruction/function, message/trace receiver, result-document resolver, cancellation, error reporter, serialization method, and canonical byte sink.
- Thread safety: immutable compiled XPath/stylesheets/packages, isolated dynamic contexts, immutable static registries after compilation, no ambient mutable global resolver state.

## Findings from existing libraries

| Library | Feature lesson applied to this plan |
| --- | --- |
| Expat | A small incremental event core, explicit handlers, pause/resume, DTD/entity callbacks, and amplification limits are the right lowest-level parser architecture; Expat does not supply the tree/query/transform layers. |
| libxml2 + libxslt | A shared tree plus SAX, push, reader, writer, XPath, catalogs, XInclude, DTD/schema validation, and C14N demonstrates the value of adapters around one core. Its XPath/XSLT level is primarily 1.0, so it is not the semantic ceiling here. |
| Apache Xerces | Separating well-formed-only, DTD-validating, and schema-validating configurations and exposing DOM/SAX/pull-style parsing, resolver, encoding, and error controls informs the conformance profiles. |
| Apache Xalan | Separate stylesheet parsing, compiled templates, transformation runtime, XPath compiler, keys, and extension modules remain useful architectural boundaries, but Xalan's released baseline is XPath/XSLT 1.0. |
| lxml | A cohesive application-facing facade over parsing, trees, XPath, XSLT, validation, XInclude, and C14N is valuable; this plan keeps that breadth while avoiding native dependencies and adding 3.1/3.0 semantics. |
| Saxon | XPath 3.1 maps/arrays/higher-order functions, XSLT 3.0 packages, accumulators, dynamic evaluation, JSON, result documents, schema-awareness, and streaming define the high-level completeness target and motivate explicit basic/schema-aware/streaming tiers. |

## Architecture and implementation rules

1. Layers flow from codecs and scanner to parser state machine, DTD/namespace processors, events, tree/XDM, XPath, XSLT, and output sinks. Higher layers may depend downward only.
2. There is one lexical/parser truth. DOM building, push callbacks, pull reads, DTD validation, XInclude, and XSLT streaming consume the same normalized events.
3. Public APIs use data-holding classes plus behavior interfaces because syntax.md forbids properties, delegates, records, structs, generic declarations, overloads, and many modern constructs.
4. All public types live under src/Api in their mandated category. No internal type is placed under src/Api, and no public type is placed elsewhere.
5. Public implementation classes have one constructor and expose only members declared by their interfaces. Public methods are instance methods; static public classes contain constants/readonly fields only.
6. Each interface method name is unique inside that interface and across every interface implemented by one class. Different input forms travel through data classes instead of overloads or default/named parameters.
7. Nullable syntax is used only for reference types. Counts and sizes are signed integers; unsigned types are limited to binary/codepoint/bit operations.
8. Source code uses only built-in .NET libraries. No LINQ is used. Loops and explicit collections replace it. Failure uses typed exceptions; frequently expected lookup/match failures also have exception-less result methods.
9. Required public XML comments use only the constructs permitted by syntax.md and are delivered with each API implementation.
10. Security limits and resolver policy are explicit data passed to instances. Defaults deny external access and bound all attacker-controlled growth.

## Proposed public API and files

This is a type/responsibility map, not a frozen list of method signatures. Before implementation, every name must be checked for the cross-interface uniqueness rule.

### src/Api/Data

- SourceLocation.cs, SourceRange.cs, XmlDiagnostic.cs — positions, ranges, standard error code/category/severity, message, related location, and entity/template stack.
- XmlName.cs, XmlNamespaceBinding.cs, XmlAttributeData.cs — QName/expanded-name and ordered namespace/attribute data used by parser, tree, XPath, and XSLT interfaces.
- XmlInput.cs, XmlOutput.cs, XmlResolvedResource.cs — discriminated field bags for string/character/byte/Stream/TextReader inputs, Stream/TextWriter/sink outputs, URI/media/encoding metadata, and ownership flags.
- XmlSecurityLimits.cs, XmlResolverPolicy.cs, XmlParserOptions.cs — limits, allowed schemes/roots, XML version/DTD/entity/namespace/validation/preservation choices.
- XmlEvent.cs, XmlEventBatch.cs, XmlReaderState.cs — push/pull event payloads and cursor state without public properties.
- XmlNode.cs, XmlDocument.cs, XmlNodeSelection.cs, XmlRange.cs — mutable tagged tree, document metadata/DTD/indexes, selections, and traversal ranges.
- XmlDocumentType.cs, XmlElementDeclaration.cs, XmlAttributeDeclaration.cs, XmlEntityDeclaration.cs, XmlNotationDeclaration.cs — retained DTD grammar and declaration data.
- XmlSerializationOptions.cs, XmlCanonicalizationOptions.cs, XmlCharacterMapEntry.cs — complete Serialization 3.1 and canonicalization controls.
- XmlCatalogEntry.cs, XmlIncludeOptions.cs — catalog records and XInclude processing policy.
- XdmAtomicValue.cs, XdmSequence.cs, XdmMapEntry.cs, XdmMapValue.cs, XdmArrayValue.cs, XdmFunctionItem.cs — public XDM values represented without user-defined generic types or inheritance.
- XPathStaticContext.cs, XPathDynamicContext.cs, XPathVariableBinding.cs, XPathFunctionBinding.cs, XPathCompiledExpression.cs, XPathEvaluationResult.cs — compile/evaluation inputs and results.
- XmlSchemaSet.cs, XmlSchemaValidationOptions.cs, XmlSchemaTypeInfo.cs, XmlSchemaValidationResult.cs — optional schema-tier compiled-set handle, policies, type annotation, and collected results.
- XsltCompileOptions.cs, XsltPackageReference.cs, XsltExecutable.cs, XsltParameter.cs, XsltTransformOptions.cs, XsltTransformResult.cs, XsltSecondaryResult.cs — stylesheet/package compilation and transformation data.
- XsltExtensionBinding.cs, XsltTraceEvent.cs, XsltMessage.cs, XsltStreamabilityReport.cs — extension registration, observability, and static streaming analysis.

Every data class above has at least one public instance field and is consumed by an interface or another data class. Data classes contain no public methods and implement no interfaces.

### src/Api/Enums

- XmlVersion.cs, XmlNodeKind.cs, XmlEventKind.cs, XmlInputKind.cs, XmlOutputKind.cs.
- XmlDiagnosticSeverity.cs, XmlErrorCategory.cs, XmlValidationMode.cs, XmlDtdProcessing.cs, XmlEntityHandling.cs, XmlNamespaceHandling.cs, XmlStandalone.cs, XmlSpaceMode.cs.
- XmlParserStatus.cs, XmlHandlerAction.cs, XmlResourceKind.cs, XmlResourceAccess.cs, XmlCatalogEntryKind.cs.
- XmlSerializationMethod.cs, XmlCanonicalizationMethod.cs, XmlCharacterEscapeContext.cs, XmlInvalidCharacterPolicy.cs.
- XdmItemKind.cs, XdmAtomicType.cs, XdmOccurrence.cs, XPathLanguageVersion.cs, XPathCompatibilityMode.cs.
- XmlSchemaValidity.cs, XmlSchemaProcessContents.cs, XmlSchemaDerivationMethod.cs.
- XsltConformanceLevel.cs, XsltComponentVisibility.cs, XsltRecoveryPolicy.cs, XsltResultDisposition.cs, XsltTraceEventKind.cs, XsltStreamability.cs.

Every enum is referenced by a data class or contract, and every member receives required XML documentation when implemented.

### src/Api/Static

- XmlNamespaces.cs — xml, xmlns, XInclude, XPointer, XML Catalog, and XML Schema namespace URI fields.
- XmlMediaTypes.cs — XML media types and common encodings.
- XmlErrorCodes.cs — stable library/XML/namespace/DTD/XInclude/catalog error code fields.
- XPathNamespaces.cs, XPathErrorCodes.cs — fn, math, map, array, xs, err namespaces and standard XPath/F&O codes.
- XsltNamespaces.cs, XsltErrorCodes.cs — XSLT namespace and XTSE/XTTE/XTDE code fields.
- XmlSerializationParameterNames.cs — Serialization 3.1 expanded-name fields.

These are public static data classes with fields only and no methods or delegates.

### src/Api/Exceptions

- XmlException.cs — direct System.Exception base for general XML failures.
- XmlWellFormednessException.cs, XmlNamespaceException.cs, XmlValidationException.cs, XmlResourceException.cs, XmlLimitException.cs — direct System.Exception subclasses carrying public diagnostic fields.
- XmlSerializationException.cs, XmlCanonicalizationException.cs, XmlSchemaException.cs — output and optional schema failures.
- XPathException.cs, XPathStaticException.cs, XPathTypeException.cs, XPathDynamicException.cs — general and phase-specific XPath failures.
- XsltException.cs, XsltStaticException.cs, XsltTypeException.cs, XsltDynamicException.cs, XsltTerminationException.cs — compile/run/termination failures.

The policy permits only simple direct System.Exception inheritance for exception types; these classes therefore do not inherit from one another.

### src/Api/Contracts and corresponding src/Api/Classes

| Contract file | Public implementation file | Responsibility |
| --- | --- | --- |
| IXmlParser.cs | XmlParser.cs | Parse one XmlInput into XmlDocument or a balanced fragment using explicit options and collaborators. |
| IXmlPushParser.cs | XmlPushParser.cs | Feed incremental blocks, finish, pause/resume, abort, and report parser status. |
| IXmlEventHandler.cs | XmlEventCollector.cs | Receive SAX-style XmlEvent values with handler backpressure; collector is a basic reusable implementation. |
| IXmlPullReader.cs | XmlPullReader.cs | Advance a cursor, inspect XmlReaderState, skip subtrees, and close input. |
| IXmlTreeNavigator.cs | XmlTreeNavigator.cs | Read/traverse/search nodes, namespace scopes, IDs, axes, and ranges. |
| IXmlTreeEditor.cs | XmlTreeEditor.cs | Create/import/adopt/clone/insert/replace/remove/normalize/freeze tree data with invariant checks. |
| IXmlEventEmitter.cs | XmlEventEmitter.cs | Emit a tree or subtree as normalized event batches. |
| IXmlResourceResolver.cs | XmlDefaultResolver.cs | Resolve an explicitly permitted URI/resource into XmlResolvedResource. |
| IXmlCatalogResolver.cs | XmlCatalogResolver.cs | Load catalogs and resolve public/system/URI identifiers. |
| IXmlErrorReporter.cs | XmlDiagnosticCollector.cs | Receive diagnostics; collector provides an accumulating implementation. |
| IXmlCancellation.cs | XmlCancellationFlag.cs | Poll caller-controlled cancellation without async or delegate syntax. |
| IDtdValidator.cs | DtdValidator.cs | Validate a parsed document/events against retained DTD declarations. |
| IXmlIncludeProcessor.cs | XmlIncludeProcessor.cs | Apply XInclude/XPointer processing to a tree using a resolver. |
| IXmlEscaper.cs | XmlEscaper.cs | Escape/unescape one context with explicit version/encoding policy. |
| IXmlSerializer.cs | XmlSerializer.cs | Serialize XmlDocument, XmlNode, or XdmSequence through XmlOutput and options. |
| IXmlCanonicalizer.cs | XmlCanonicalizer.cs | Canonicalize full trees or node selections to an output/sink. |
| IXmlByteSink.cs | XmlByteCollector.cs | Receive canonical/serialized byte blocks; collector is the baseline implementation. |
| IXmlSchemaCompiler.cs | XmlSchemaCompiler.cs | Optional tier: compile XML Schema 1.1 documents into XmlSchemaSet. |
| IXmlSchemaValidator.cs | XmlSchemaValidator.cs | Optional tier: validate/annotate trees or events and return validation results. |
| IXPathCompiler.cs | XPathCompiler.cs | Compile XPath 3.1 text against XPathStaticContext. |
| IXPathEvaluator.cs | XPathEvaluator.cs | Evaluate XPathCompiledExpression against XPathDynamicContext. |
| IXPathFunction.cs | XPathFunctionAdapter.cs | Invoke one extension function via XdmSequence arguments/result. |
| IXPathVariableResolver.cs | XPathVariableResolver.cs | Resolve external variables without delegates. |
| IXPathFunctionResolver.cs | XPathFunctionResolver.cs | Resolve extension functions by expanded QName and arity. |
| IXPathCollectionResolver.cs | XPathCollectionResolver.cs | Resolve doc/collection/uri-collection resources under policy. |
| IXPathCollation.cs | XPathCodepointCollation.cs | Compare strings and generate collation keys; codepoint collation is built in. |
| IXPathTraceReceiver.cs | XPathTraceCollector.cs | Receive trace events without affecting evaluation semantics. |
| IXsltCompiler.cs | XsltCompiler.cs | Compile a stylesheet/package into immutable XsltExecutable. |
| IXsltTransformer.cs | XsltTransformer.cs | Run a compiled executable with explicit source, parameters, context, and destinations. |
| IXsltPackageResolver.cs | XsltPackageResolver.cs | Resolve packages by name/version and module resources by URI. |
| IXsltExtensionInstruction.cs | XsltExtensionInstructionAdapter.cs | Execute an extension instruction through data-only request/result objects. |
| IXsltMessageReceiver.cs | XsltMessageCollector.cs | Receive xsl:message and assertion events. |
| IXsltResultDocumentResolver.cs | XsltResultDocumentCollector.cs | Allocate, commit, reject, or roll back secondary result destinations. |
| IXsltTraceReceiver.cs | XsltTraceCollector.cs | Receive template/function/instruction/component trace events. |
| IXsltStreamAnalyzer.cs | XsltStreamAnalyzer.cs | Report guaranteed streamability and reasons before execution. |

The final signatures may split a responsibility if required to retain unique method names, but may not merge unrelated responsibilities merely for convenience.

## Planned internal source files

Internal types are non-public and live outside src/Api.

### Core, text, and diagnostics

- src/Core/Guard.cs, Limits.cs, ObjectPool.cs, StringTable.cs, NameTable.cs, StableSort.cs, UriTools.cs.
- src/Text/ByteDecoder.cs, EncodingDetector.cs, Utf8Codec.cs, Utf16Codec.cs, ScalarReader.cs, LineNormalizer.cs, XmlCharClasses.cs, XmlNameScanner.cs, ValueBuffer.cs.
- src/Diagnostics/DiagnosticFactory.cs, ErrorCodeMap.cs, SourceTracker.cs, SourceStack.cs.

### XML scanner, parser, namespaces, DTD, and resolution

- src/Parsing/XmlScanner.cs, XmlToken.cs, XmlTokenKind.cs, XmlDeclarationParser.cs, XmlParserEngine.cs, XmlParserState.cs, XmlFragmentContext.cs, AttributeNormalizer.cs, EntityExpander.cs, ParserEventQueue.cs.
- src/Namespaces/NamespaceContext.cs, NamespaceFrame.cs, NamespaceFixup.cs, QualifiedNameParser.cs.
- src/Dtd/DtdScanner.cs, DtdParser.cs, DtdGrammar.cs, DtdContentModelParser.cs, DtdAutomaton.cs, DtdAttributeProcessor.cs, DtdValidationEngine.cs, IdRegistry.cs, EntityManager.cs.
- src/Resolution/ResourceManager.cs, ResolverGuard.cs, CatalogParser.cs, CatalogIndex.cs, CatalogMatcher.cs, ResourceCache.cs.

### Events, tree, related XML, and output

- src/Events/PushDriver.cs, PullDriver.cs, EventBuffer.cs, EventPipeline.cs.
- src/Tree/TreeBuilder.cs, TreeInvariants.cs, TreeMutation.cs, TreeTraversal.cs, AxisCursor.cs, NodeOrder.cs, NamespaceNodeView.cs, TextCoalescer.cs, IdIndex.cs.
- src/Include/XIncludeEngine.cs, XPointerParser.cs, XPointerEvaluator.cs, XmlContextFixup.cs.
- src/Serialization/SerializationEngine.cs, SequenceNormalizer.cs, NamespaceWriter.cs, XmlOutputWriter.cs, HtmlOutputWriter.cs, TextOutputWriter.cs, JsonOutputWriter.cs, AdaptiveOutputWriter.cs, Indenter.cs, CharacterMap.cs.
- src/Canonicalization/CanonicalizerEngine.cs, InclusiveCanonicalizer.cs, ExclusiveCanonicalizer.cs, CanonicalAttributeSorter.cs.
- src/Escaping/EscapeEngine.cs, JsonEscapeEngine.cs, UriEscapeEngine.cs.

### XDM, schema, XPath, and functions

- src/Xdm/XdmFactory.cs, XdmSequenceBuilder.cs, XdmTypeSystem.cs, AtomicCaster.cs, AtomicComparer.cs, EffectiveBooleanValue.cs, NodeAtomizer.cs, MapStore.cs, ArrayStore.cs, FunctionRegistry.cs.
- src/Xdm/DecimalValue.cs, IntegerValue.cs, FloatingValue.cs, DurationValue.cs, DateTimeValue.cs, BinaryValue.cs, QNameValue.cs.
- src/Schema/SchemaScanner.cs, SchemaParser.cs, SchemaComponents.cs, SchemaResolver.cs, SchemaTypeDerivation.cs, SchemaContentModel.cs, SchemaFacetValidator.cs, IdentityConstraintEngine.cs, SchemaValidationEngine.cs, TypeAnnotator.cs.
- src/XPath/XPathLexer.cs, XPathToken.cs, XPathParser.cs, XPathAst.cs, XPathBinder.cs, XPathTypeAnalyzer.cs, XPathOptimizer.cs, XPathPlan.cs, XPathRuntime.cs, FocusStack.cs, VariableFrame.cs, FunctionCallFrame.cs, PathEvaluator.cs, PredicateEvaluator.cs, PatternParser.cs, PatternMatcher.cs.
- src/XPath/Functions/ContextFunctions.cs, BooleanFunctions.cs, NumericFunctions.cs, StringFunctions.cs, RegexFunctions.cs, UriFunctions.cs, QNameFunctions.cs, NodeFunctions.cs, SequenceFunctions.cs, DateTimeFunctions.cs, DurationFunctions.cs, BinaryFunctions.cs, MathFunctions.cs, MapFunctions.cs, ArrayFunctions.cs, HigherOrderFunctions.cs, ResourceFunctions.cs, JsonFunctions.cs, SerializationFunctions.cs, TransformFunctions.cs, EnvironmentFunctions.cs, ErrorFunctions.cs.
- src/XPath/Regex/RegexParser.cs, RegexAst.cs, RegexCompiler.cs, RegexMatcher.cs, UnicodeCategoryTable.cs.
- src/XPath/Json/JsonParser.cs, JsonXmlConverter.cs.

### XSLT compiler and runtime

- src/Xslt/XsltModuleLoader.cs, StylesheetParser.cs, PackageLinker.cs, ComponentRegistry.cs, ImportPrecedence.cs, StaticEvaluator.cs, UseWhenEvaluator.cs, AttributeValueTemplateParser.cs, TextValueTemplateParser.cs.
- src/Xslt/Declarations/ModeCompiler.cs, TemplateCompiler.cs, FunctionCompiler.cs, GlobalVariableCompiler.cs, AttributeSetCompiler.cs, KeyCompiler.cs, AccumulatorCompiler.cs, DecimalFormatCompiler.cs, OutputCompiler.cs, CharacterMapCompiler.cs, WhitespaceRuleCompiler.cs, NamespaceAliasCompiler.cs.
- src/Xslt/Patterns/XsltPatternParser.cs, PatternPriority.cs, TemplateRuleIndex.cs, TemplateSelector.cs.
- src/Xslt/Runtime/TransformationEngine.cs, ExecutionFrame.cs, GlobalVariableStore.cs, ParameterBinder.cs, ModeRuntime.cs, TemplateRuntime.cs, InstructionRuntime.cs, SequenceConstructor.cs, ResultTreeBuilder.cs, ResultDocumentManager.cs, DocumentPool.cs, ErrorObject.cs.
- src/Xslt/Runtime/ApplyTemplatesInstruction.cs, CallTemplateInstruction.cs, ControlInstructions.cs, ConstructionInstructions.cs, CopyInstructions.cs, IterationInstructions.cs, SortInstruction.cs, GroupInstruction.cs, NumberInstruction.cs, KeyRuntime.cs, AccumulatorRuntime.cs, AnalyzeStringInstruction.cs, MapArrayInstructions.cs, MergeInstruction.cs, EvaluateInstruction.cs, TryCatchInstruction.cs, MessageInstruction.cs.
- src/Xslt/Streaming/Posture.cs, Sweep.cs, StreamabilityAnalyzer.cs, StreamedNode.cs, StreamFocus.cs, StreamingTemplateRuntime.cs, StreamingAccumulator.cs, ForkRuntime.cs, GroundingBuffer.cs.

## Test source organization

- tests/Api/ — one fixture family per public contract/class/data/enum/static/exception category.
- tests/Parsing/, tests/Namespaces/, tests/Dtd/, tests/Resolution/, tests/Events/, tests/Tree/, tests/Include/.
- tests/Serialization/, tests/Canonicalization/, tests/Escaping/.
- tests/Xdm/, tests/Schema/, tests/XPath/, tests/XPath/Functions/, tests/XPath/Regex/, tests/XPath/Json/.
- tests/Xslt/Compilation/, tests/Xslt/Runtime/, tests/Xslt/Packages/, tests/Xslt/Streaming/.
- tests/Conformance/Xml/, tests/Conformance/Qt3/, tests/Conformance/Xslt30/ — adapters, allowlists only for explicitly unsupported optional features, and reproducible result summaries.
- tests/Support/ — input/output/resolver/handler fakes, deterministic clock/environment, tree and XDM assertions, diagnostic assertions, and conformance-suite readers.

## Implementation tasks

Tasks are ordered by dependency. A task is complete only when its production behavior, public XML comments on touched API, and focused tests satisfy the stated standard clauses; test-specific tasks add broader matrices and conformance evidence.

### Foundation and public surface

- [DONE] T001: Establish internal namespaces/directories and dependency boundaries for Core, Text, Diagnostics, Parsing, Namespaces, Dtd, Resolution, Events, Tree, Include, Serialization, Canonicalization, Escaping, Xdm, Schema, XPath, and Xslt without adding external src/ dependencies.
- [DONE] T002: Implement internal guard, limit accounting, string/name interning, explicit stable sorting, URI helpers, reusable buffers, and object pools using the smallest syntax.md-compatible language subset.
- [DONE] T003: Add all planned src/Api/Enums types and members, using int32 constants and referencing every enum from at least one public data or contract type.
- [DONE] T004: Add the planned src/Api/Static constants-only classes for standard namespace URIs, media types, serialization parameter names, and XML/XPath/XSLT error codes.
- [DONE] T005: Add source, name, input/output, resource, diagnostics, security, parser-event, and reader-state public data classes under src/Api/Data with field-only representations.
- [DONE] T006: Add tree, DTD, catalog, include, serialization, and canonicalization public data classes under src/Api/Data and ensure each is consumed by a contract or other data class.
- [DONE] T007: Add XDM, XPath, schema, and XSLT public data classes under src/Api/Data with field-only representations and no interface implementation.
- [DONE] T008: Add all planned direct-System.Exception public exception classes with diagnostic/location fields and the one permitted constructor per type.
- [DONE] T009: Add the low-level XML parser, streaming, tree, resolution, diagnostic, cancellation, validation, include, escaping, serialization, canonicalization, and byte-sink interfaces under src/Api/Contracts with globally unique member names per implementation.
- [DONE] T010: Add the schema, XPath, and XSLT interfaces under src/Api/Contracts, including resolver, extension, collation, trace, message, result-document, and streaming-analysis hooks without delegates or user-defined generics.
- [DONE] T011: Add skeletal public implementation classes under src/Api/Classes, each with exactly one constructor and only the public instance members required by its implemented interfaces.
- [DONE] T012: Create API construction and field-roundtrip tests for every public data class, every enum value, every static field, every exception constructor/diagnostic field, and every implementation constructor.

### Character decoding, lexical rules, and XML declarations

- [DONE] T013: Implement byte-source abstraction, buffered Stream/byte-memory reads, BOM detection, and safe ownership/close behavior for XmlInput.
- [DONE] T014: Implement strict UTF-8 decoding per RFC 3629, including overlong sequences, surrogate encodings, invalid continuations, truncation, and scalar limits.
- [DONE] T015: Implement strict UTF-16BE/LE decoding per RFC 2781, BOM handling, surrogate pairing, odd-byte/truncation errors, and byte offsets.
- [DONE] T016: Integrate caller-selected built-in .NET encodings, case-insensitive IANA/XML encoding names, decoder failures, and policy rejection without any third-party codecs.
- [DONE] T017: Implement XML encoding sniffing and XML/text declaration reconciliation across BOM, byte pattern, declared encoding, TextReader input, and external-entity rules.
- [DONE] T018: Implement XML 1.0 Fifth Edition character legality, character references, name character tables, and scalar-safe scanning.
- [DONE] T019: Implement XML 1.1 Second Edition character legality, restricted-character reference rules, name character tables, and normalization-checking diagnostics.
- [DONE] T020: Implement XML 1.0 and 1.1 end-of-line normalization for document/external entities while retaining accurate raw byte and normalized character locations.
- [DONE] T021: Implement SourceTracker and SourceStack for URI/entity/include, offsets, one-based lines/columns, ranges, and nested diagnostic traces.
- [DONE] T022: Implement span-based token buffers and scanners for S, Name, Nmtoken, NCName, QName, PubidChar, quoted literals, decimal/hex references, and delimiters.
- [DONE] T023: Implement XML declaration and text declaration parsers with pseudo-attribute order, quoting, version, encoding, standalone, whitespace, and placement constraints.
- [DONE] T024: Add decoding/declaration tests covering every supported input kind, chunk boundary, BOM/encoding combination, malformed sequence, XML version, EOL form, and source-location calculation.
- [DONE] T025: Add exhaustive XML 1.0/1.1 character, Name/NCName/QName, reference, restricted-character, noncharacter, surrogate, and boundary tests derived from the local Recommendations.

### Core scanner and parser

- [DONE] T026: Implement XmlToken/XmlTokenKind and the resumable scanner state machine for text, markup, references, comments, CDATA, processing instructions, start/end tags, declarations, and EOF.
- [DONE] T027: Implement comment, processing-instruction, CDATA, and character-data well-formedness checks including forbidden sequences and legal targets.
- [DONE] T028: Implement start, end, and empty element parsing with ordered raw attributes, quote preservation option, tag matching, duplicate detection hooks, and depth tracking.
- [DONE] T029: Implement document prolog/epilog state, exactly-one-root enforcement, misc content, DOCTYPE placement, trailing-content rejection, and standalone metadata.
- [DONE] T030: Implement balanced fragment parsing with explicit inherited namespace, base URI, language, and space context while keeping document parsing strict.
- [DONE] T031: Implement predefined/numeric/general entity reference dispatch, entity boundary events, unresolved/skipped behavior, and character replacement validation.
- [DONE] T032: Implement ParserEventQueue and event payload creation with preservation switches for CDATA, entity references, comments, processing instructions, prefixes, and ignorable whitespace.
- [DONE] T033: Implement depth, node, attribute, name, text, token, and total-input limit enforcement plus cancellation polling at bounded work intervals.
- [DONE] T034: Implement fatal parse shutdown, deterministic reset/close, no-recovery strict mode, partial-diagnostic retention, and the XmlWellFormednessException mapping.
- [DONE] T035: Complete XmlParser public implementation for document and fragment results across all XmlInput variants and configured collaborators.
- [DONE] T036: Add parser unit tests for every production and well-formedness constraint in document, element, attribute, reference, CDATA, comment, PI, prolog, and epilog grammar.
- [DONE] T037: Add parser chunk-splitting tests at every byte/character boundary for declarations, delimiters, Unicode sequences, references, and end tags.
- [DONE] T038: Add parser security tests for every configured limit, cancellation point, deep nesting, giant tokens, malformed EOF, and resource cleanup path.

### Namespace processing

- [DONE] T039: Implement immutable-stack NamespaceContext frames, default/prefixed bindings, in-scope enumeration, and prefix lookup.
- [DONE] T040: Implement namespace declaration processing and reserved xml/xmlns prefix/URI constraints for Namespaces 1.0 and 1.1.
- [DONE] T041: Resolve element/attribute QNames to expanded names, enforce unbound-prefix errors, no default namespace on attributes, and expanded-name duplicate attributes.
- [DONE] T042: Implement Namespace 1.0 versus 1.1 prefix undeclaration behavior and version-correct namespace event ordering.
- [DONE] T043: Implement namespace fixup and deterministic prefix generation for edited/copied/constructed trees and serialization, preserving caller prefixes when legal.
- [DONE] T044: Integrate namespace processing into scanner events, parser outputs, fragment context, and retained declaration-attribute options.
- [DONE] T045: Add namespace tests for every namespace constraint, scope transition, undeclaration/version difference, reserved binding, duplicate expanded name, fixup conflict, and event sequence.

### DTD scanning, entities, and validation

- [DONE] T046: Implement DOCTYPE name, external identifier, internal subset, and closing-delimiter parsing with root-name retention.
- [DONE] T047: Implement DTD tokenization for declarations, parameter entity references, declaration separators, public/system literals, comments, PIs, and conditional-section delimiters.
- [DONE] T048: Implement internal/external subset loading, text declarations, parameter entity expansion, proper declaration/PE nesting, and internal-subset restrictions.
- [DONE] T049: Implement INCLUDE/IGNORE conditional sections with nesting in external subsets and rejection in forbidden contexts.
- [DONE] T050: Parse and retain general/parameter parsed entities, external/unparsed entities, notations, first-declaration precedence, and entity base URIs.
- [DONE] T051: Implement recursive-entity detection, expansion character accounting, maximum amplification/activation thresholds, nesting/count/size limits, and secure unresolved-external behavior.
- [DONE] T052: Parse element declarations for EMPTY, ANY, mixed content, sequences, choices, and occurrence operators with syntax and determinism constraints.
- [DONE] T053: Compile children content models into internal deterministic validators and implement incremental element-content validation.
- [DONE] T054: Parse attribute-list declarations for every XML attribute type, enumerated/NOTATION tokens, #REQUIRED/#IMPLIED/#FIXED/default values, and declaration consistency constraints.
- [DONE] T055: Implement DTD-aware attribute whitespace normalization, default insertion, fixed-value checking, token validation, and standalone constraints.
- [DONE] T056: Implement ID uniqueness, ID default restrictions, deferred IDREF/IDREFS resolution, ENTITY/ENTITIES unparsed-entity checks, and NMTOKEN(S) validation.
- [DONE] T057: Implement notation declaration/use checks, unparsed entity constraints, declared-element/attribute checks, root/DOCTYPE agreement, and standalone validity.
- [DONE] T058: Implement non-validating versus validating behavior, ignorable-whitespace classification, accumulated validity diagnostics, and fail-fast policy.
- [DONE] T059: Complete DtdValidator public implementation for retained trees and compatible normalized event streams.
- [DONE] T060: Add DTD grammar tests for all declaration forms, parameter entities, conditional sections, precedence, external subsets, text declarations, and malformed declarations.
- [DONE] T061: Add entity tests for internal/external/parameter/unparsed entities, boundary reporting, bases/encodings, recursion, standalone behavior, and all expansion security limits.
- [DONE] T062: Add DTD validation tests for every attribute type/default mode, content model/occurrence, ID/IDREF, ENTITY/NOTATION, root, whitespace, and validity constraint.

### Resource resolution and catalogs

- [DONE] T063: Implement URI/IRI parsing, RFC 3986 relative resolution, percent handling, file URI normalization, base stack behavior, and fragment policy using built-in .NET URI facilities plus conformance corrections.
- [DONE] T064: Implement ResolverGuard with deny-by-default external access, allowed schemes/roots, redirect/depth/byte/time-equivalent work limits, media/encoding metadata, and cycle detection.
- [DONE] T065: Complete XmlDefaultResolver for explicitly allowed file and HTTP(S) resources using built-in .NET APIs and deterministic error-to-diagnostic mapping.
- [DONE] T066: Parse XML Catalog 1.1 catalog/group entries, xml:base, prefer, exact public/system/URI mappings, rewrite/delegate, suffix entries, and nextCatalog.
- [DONE] T067: Implement normalized public identifiers, catalog matching precedence, longest-match rules, delegation/chaining, caching, duplicate handling, and catalog-cycle detection.
- [DONE] T068: Complete XmlCatalogResolver and integrate catalog-first then caller-resolver behavior for DTD entities, XInclude, schemas, XPath documents, and XSLT modules/packages.
- [DONE] T069: Add URI/resolver tests for relative/base/file/IRI cases, denied schemes/roots, redirects/cycles, encoding/media metadata, limits, disposal, and diagnostic locations.
- [DONE] T070: Add XML Catalog 1.1 tests for every entry kind, prefer scope, exact/rewrite/suffix/delegate precedence, nested groups, chains, bases, misses, malformed catalogs, and cycles.

### Push, pull, event, and tree layers

- [DONE] T071: Implement PushDriver and XmlPushParser incremental byte/character feeding, finalization, pause/resume/abort, status transitions, and handler backpressure.
- [DONE] T072: Implement SAX-equivalent normalized events for document/entity/DTD/namespace/element/text/whitespace/CDATA/comment/PI/declaration/skipped-entity/diagnostic boundaries.
- [DONE] T073: Implement XmlEventCollector and XmlDiagnosticCollector public helper classes with bounded accumulation, retrieval, and clear/reset behavior declared by their contracts.
- [DONE] T074: Implement PullDriver and XmlPullReader cursor advancement, current state, attribute/namespace access through data fields, subtree skip, EOF/error states, and close behavior.
- [DONE] T075: Add push/pull parity tests proving the same input and options produce equivalent normalized event streams across whole and arbitrarily chunked input.
- [DONE] T076: Add push/pull API tests for every event kind/state field, handler action, pause/resume/abort transition, skip operation, failure, location, limit, and resource-lifetime path.
- [DONE] T077: Implement TreeBuilder from normalized events with all planned XmlNode kinds, document metadata, ordered children/attributes/namespaces, source locations, and owner/parent links.
- [DONE] T078: Implement TreeInvariants for legal child kinds/order, single document element/DOCTYPE, no cycles, unique expanded attributes, owner-document rules, and frozen nodes.
- [DONE] T079: Implement XmlTreeEditor create/import/adopt/clone/append/insert/replace/remove operations with atomic validation before mutation.
- [DONE] T080: Implement text-content replacement, normalize/coalesce, CDATA/entity preservation choices, attribute mutation, namespace repair, and mutation versioning.
- [DONE] T081: Implement XmlTreeNavigator parent/child/sibling/attribute/namespace traversal, all XPath axes, document order, node identity, text content, base/lang/space, and ID lookup.
- [DONE] T082: Implement XmlRange and selection operations, filtered traversal, mutation invalidation rules, and frozen snapshot selection.
- [DONE] T083: Implement XmlEventEmitter so any document/subtree/selection can re-enter push, serializer, canonicalizer, schema, XPath, or XSLT pipelines.
- [DONE] T084: Add tree construction tests for all node kinds, parser preservation modes, DTD metadata, source locations, namespaces, entity boundaries, and fragment documents.
- [DONE] T085: Add tree mutation/invariant tests for every public editor/navigator operation, legal/illegal placement, ownership, cycles, attributes, namespace fixup, ranges, freezing, and mutation invalidation.

### XML Base, xml:id, XInclude, and XPointer

- [DONE] T086: Implement effective xml:base, xml:lang, and xml:space inheritance and mutation-sensitive caching.
- [DONE] T087: Implement xml:id recognition, whitespace normalization, NCName validity, uniqueness indexing, and conflict behavior with DTD/schema IDs.
- [DONE] T088: Implement XPointer framework parsing and xmlns()/element() scheme evaluation with shorthand pointers and correct fallback across scheme parts.
- [DONE] T089: Implement XInclude XML parse mode, include selection, recursive processing, loop detection, fallback, resource errors, and document-property propagation.
- [DONE] T090: Implement XInclude text parse mode with encoding selection, legal-character checks, line normalization, accept metadata, and insertion rules.
- [DONE] T091: Implement XInclude xml:base/xml:lang fixup, namespace preservation, excluded nodes, include history, and configurable depth/resource limits.
- [DONE] T092: Complete XmlIncludeProcessor and add tests for XML/text inclusion, XPointer element selection, fallback/error matrix, recursion, bases/languages, encodings, and resolver security.

### Escaping, serialization, and canonicalization

- [DONE] T093: Implement XmlEscaper text and attribute escaping/unescaping for XML 1.0/1.1, quote choice, numeric references, invalid-character policies, and chunk boundaries.
- [DONE] T094: Implement escaping/validation helpers for comments, processing instructions, CDATA splitting, public/system literals, XPath literals, URI components, and JSON strings.
- [DONE] T095: Implement XmlOutput abstraction, byte/text sinks, encoding writer, BOM/declaration coordination, short writes, flushing, ownership, and failure cleanup.
- [DONE] T096: Implement Serialization 3.1 sequence normalization from XdmSequence and tree/node inputs, including item separators, adjacent atomic spacing, node copying, and invalid sequence errors.
- [DONE] T097: Implement XML 1.0/1.1 serialization with namespace fixup, version/encoding/standalone/doctype parameters, empty elements, CDATA elements, character maps, indentation, normalization, and undeclare-prefixes.
- [DONE] T098: Implement XHTML serialization rules including namespace recognition, empty elements, script/style escaping, URI attributes, meta content-type handling, and version controls.
- [DONE] T099: Implement HTML serialization rules for versions 4/5, void/raw-text elements, boolean attributes, URI escaping, doctype/meta insertion, case behavior, and foreign XML content.
- [DONE] T100: Implement text serialization with encoding/normalization/item separators and text-value extraction.
- [DONE] T101: Implement JSON serialization for maps/arrays/atomic values/nodes with duplicate-name, escaping, fallback, non-finite numeric, indentation, and json-node-output-method rules.
- [DONE] T102: Implement adaptive serialization for every XDM item kind, nested maps/arrays/functions/nodes, and caller-supplied extension serialization method resolution.
- [DONE] T103: Implement output character maps, expansion order, illegal replacement detection, named map merging, and interaction with escaping/CDATA.
- [DONE] T104: Complete XmlSerializer public implementation and validate all Serialization 3.1 parameter combinations and standard serialization error codes.
- [DONE] T105: Implement Canonical XML 1.1 node-set processing, document subset rules, UTF-8 output, namespace/attribute ordering, escaping, inherited xml attributes, and comment switch.
- [DONE] T106: Implement Exclusive XML Canonicalization 1.0 visibly utilized namespaces, inclusive prefix lists, orphan subset handling, ordering, and comment switch.
- [DONE] T107: Implement streaming canonical byte delivery and experimental Canonical XML 2.0 profile hook without claiming Recommendation conformance.
- [DONE] T108: Complete XmlCanonicalizer and XmlByteCollector public implementations and isolate canonicalization from digest/signature policy.
- [DONE] T109: Add escaping tests for every public escape context, XML version, quote, reference, invalid scalar, delimiter hazard, URI/JSON case, and arbitrary chunk split.
- [DONE] T110: Add serialization tests for every method, every public parameter, sequence-normalization case, encoding, namespace repair, error code, output kind, resolver extension, and destination failure.
- [DONE] T111: Add canonicalization tests for inclusive/exclusive methods, with/without comments, full/subset documents, namespace inheritance, xml attributes, ordering, encodings, streaming sinks, and published examples.

### XDM values and XML Schema built-in datatypes

- [DONE] T112: Implement XdmSequence storage/building for empty/singleton/multiple values, ordered iteration without yield, concatenation, cardinality checks, and immutability.
- [DONE] T113: Implement XDM node wrappers/accessors over XmlNode for all seven XDM node kinds, identity/document order, string/typed values, names, namespaces, URIs, nilled, and annotations.
- [DONE] T114: Implement XdmAtomicValue representation and XdmTypeSystem hierarchy for every built-in XML Schema 1.1 atomic type and XPath untyped types.
- [DONE] T115: Implement string-derived datatype whitespace facets and lexical/value/canonical forms for string, normalizedString, token, language, Name, NCName, NMTOKEN, ID, IDREF, and ENTITY families.
- [DONE] T116: Implement boolean, decimal, integer and bounded integer datatype parsing, canonicalization, comparison, arithmetic, promotion, rounding, division/modulo, and overflow errors.
- [DONE] T117: Implement float/double lexical forms, IEEE comparison, NaN/INF/signed-zero behavior, promotion, arithmetic, canonical output, and numeric functions.
- [DONE] T118: Implement duration, yearMonthDuration, and dayTimeDuration parsing/canonicalization, comparison, component extraction, arithmetic, rounding, and overflow rules.
- [DONE] T119: Implement dateTime/dateTimeStamp/date/time/gYearMonth/gYear/gMonthDay/gMonth/gDay parsing, timezone presence, implicit timezone adjustment, comparison, components, arithmetic, and canonical forms.
- [DONE] T120: Implement hexBinary/base64Binary parse/canonicalize/compare and anyURI whitespace/escape semantics.
- [DONE] T121: Implement QName/NOTATION namespace-sensitive values, expanded equality, prefix retention, static/dynamic namespace resolution, and cast restrictions.
- [DONE] T122: Implement atomization, typed value derivation, untyped conversion, effective Boolean value, sequence-type matching, occurrence indicators, function conversion rules, promotion, cast, castable, treat, and instance-of.
- [DONE] T123: Implement immutable XdmMapValue with same-key semantics and immutable XdmArrayValue whose members are sequences, including structural update operations.
- [DONE] T124: Implement XdmFunctionItem metadata/invocation bridge, maps/arrays as functions, arity checking, closure environment, and interface-based extension calls.
- [DONE] T125: Add XDM node/sequence tests for every kind/property/accessor, identity/order, atomization, EBV/cardinality, annotations, maps, arrays, functions, and immutability.
- [DONE] T126: Add atomic datatype tests for every built-in type's valid/invalid lexical forms, canonical forms, casts, boundaries, equality/order, timezone, NaN/signed zero, arithmetic, promotion, and required errors.

### Optional XML Schema 1.1 schema-aware tier

- [DONE] T127: Implement schema document loading, include/import/redefine/override resolution, target namespaces, chameleon includes, cycles, and compiled-set assembly.
- [DONE] T128: Parse and compile simple types, restriction/list/union, built-in and user facets, assertions, fundamental facets, derivation, and QName resolution.
- [DONE] T129: Parse and compile complex types, simple/complex content, sequence/choice/all particles, groups, attributes/groups, wildcards, mixed content, open content, and derivation.
- [DONE] T130: Compile global/local element and attribute declarations, substitution groups, type alternatives, identity constraints, notations, defaults/fixed values, nillability, and form rules.
- [DONE] T131: Implement schema content model and wildcard validation, processContents, xsi:type, xsi:nil, schema location hints under policy, and element/attribute assessment.
- [DONE] T132: Implement all XML Schema facets including length, bounds, digits, whitespace, enumeration, pattern, explicitTimezone, assertion evaluation, and canonical comparison.
- [DONE] T133: Implement key/unique/keyref XPath subset and scoped identity-constraint tables with complete deferred reference checking.
- [DONE] T134: Implement PSVI-to-XDM annotation fields, validity outcomes, defaulted values, normalized/actual values, member types, type definitions, nilled state, and ID contribution.
- [DONE] T135: Complete XmlSchemaCompiler and XmlSchemaValidator public implementations for tree and streaming validation with collected/fail-fast diagnostics.
- [DONE] T136: Integrate schema annotations and declarations into XPath schema-element/schema-attribute tests and XSLT import-schema, validation, typed modes, and type-aware patterns.
- [DONE] T137: Add schema API/unit tests for every schema component family, derivation/facet, include/import case, wildcard, identity constraint, xsi feature, default/fixed value, PSVI field, and error outcome.

### XPath lexer, parser, compiler, and runtime

- [DONE] T138: Implement XPathLexer for complete 3.1 terminal grammar, nested comments, whitespace, keywords, EQNames/QNames, string/numeric literals, operators, and source ranges.
- [DONE] T139: Implement XPathParser precedence and AST nodes for comma, for/let/quantified/if, logical, comparison, concatenation, range, arithmetic, union/intersect/except, instance/treat/cast/castable, arrow, unary, simple-map, and paths.
- [DONE] T140: Implement primary/postfix AST parsing for variables, context item, parentheses, static/named/inline functions, partial application, dynamic calls, predicates, map/array constructors, and lookup.
- [DONE] T141: Implement full path grammar, root/descendant abbreviations, forward/reverse axes, kind/name/schema tests, predicates, and XPath 3.1 node-test syntax.
- [DONE] T142: Implement static context construction, namespace/default namespace resolution, variable/function binding, arity resolution, base URI, collations, decimal formats, type names, and feature flags.
- [DONE] T143: Implement static error checking, sequence-type/cardinality inference, function coercion checks, forbidden updating syntax, implementation-defined declarations, and XPST/XPTY code/location mapping.
- [DONE] T144: Compile AST into immutable XPathPlan instructions with constant folding and safe simplifications that preserve error, focus, ordering, and laziness semantics.
- [DONE] T145: Implement XPathRuntime expression dispatch, stack/focus/variable frames, context snapshot, resource registry, current-time snapshot, implicit timezone, cancellation, and evaluation limits.
- [DONE] T146: Implement path/axis evaluation with predicate focus, reverse-axis positions, document ordering, duplicate elimination, node identity comparisons, and streamed-node restrictions.
- [DONE] T147: Implement general/value/node comparisons, atomization, untyped conversion, numeric/type promotion, collations, existential pair semantics, NaN, and cardinality errors.
- [DONE] T148: Implement arithmetic, range, string concatenation, union/intersect/except, logical short-circuit where permitted, EBV, and exact dynamic error behavior.
- [DONE] T149: Implement for/let/quantified/if/simple-map/arrow evaluation with correct focus, variable scopes, tuple ordering, short-circuit permissions, and errors.
- [DONE] T150: Implement inline/named/static/dynamic function evaluation, closures, partial application, coercion, maps/arrays as functions, recursion, and tail-call-safe execution.
- [DONE] T151: Implement instance/treat/cast/castable and constructor functions using the XDM type system, including namespace-sensitive and schema-aware cases.
- [DONE] T152: Complete XPathCompiler, XPathEvaluator, variable/function/collection resolver, collation, and trace public implementations with compile-once/evaluate-many and thread-safe plans.
- [DONE] T153: Add XPath lexer/parser tests for every grammar production, precedence/associativity edge, comment/token boundary, EQName, path/axis/test, constructor, lookup, function, and precise static error range.
- [DONE] T154: Add XPath evaluator tests for every expression family, focus transition, ordering/duplicates, conversion/promotion, closure, context component, resolver, cancellation/limit, and XPST/XPTY/XPDY outcome.

### XPath Functions and Operators 3.1

- [DONE] T155: Implement context, position/last, current date/time/timezone, default context metadata, cardinality, empty/exists, head/tail, and count functions.
- [DONE] T156: Implement boolean and numeric constructors/functions/operators including aggregate semantics, rounding modes, untyped conversion, NaN, infinities, signed zero, and error codes.
- [DONE] T157: Implement codepoint and string constructors/functions including comparison/collation, concatenation/join, substring rounding, normalization, case mapping, translate, token, and containment functions.
- [DONE] T158: Implement the XML Schema regular-expression parser/compiler/matcher with character classes/subtraction, Unicode categories/blocks, quantifiers, captures, anchors-as-specified behavior, and flags.
- [DONE] T159: Implement matches, replace, tokenize, and analyze-string including zero-length constraints, capture numbering, replacement validation, result XML shape, and FORX errors.
- [DONE] T160: Implement URI/IRI and QName functions including resolve-uri, URI encoders, QName construction/decomposition/resolution, in-scope prefixes, and namespace errors.
- [DONE] T161: Implement node accessor/navigation functions including names, data/string, base/document URI, root/path, children, innermost/outermost, generate-id, lang, id/element-with-id/idref, and nilled.
- [DONE] T162: Implement sequence functions index-of, distinct-values, insert/remove/reverse/subsequence/unordered/deep-equal/sort and collation/same-key/stability semantics.
- [DONE] T163: Implement higher-order sequence functions for-each, filter, fold-left/right, for-each-pair and function coercion/error propagation.
- [DONE] T164: Implement date/time/duration extraction, timezone adjustment, comparisons, constructors, arithmetic, and format-date/time/dateTime picture/language/calendar/place behavior.
- [DONE] T165: Implement format-integer and format-number picture parsing, decimal formats, digit families, grouping, percent/per-mille, exponent, special values, and FODF errors.
- [DONE] T166: Implement binary conversions/comparisons and the complete math namespace function set with prescribed edge-case behavior.
- [DONE] T167: Implement every map namespace function with immutable updates, same-key, duplicate/combine policies, callbacks, find recursion, and specified errors.
- [DONE] T168: Implement every array namespace function with one-based indexing, immutable updates, subsequences, joins/flattening, callbacks, folds, paired iteration, sort, and specified errors.
- [DONE] T169: Implement function-lookup/name/arity/apply and registry resolution across built-ins, constructors, stylesheet functions, maps/arrays, and extensions.
- [DONE] T170: Implement doc/doc-available, collection/uri-collection, unparsed-text/lines/available with stable document pools, encodings, fragments, resolver policy, and FODC/FOUT errors.
- [DONE] T171: Implement environment-variable functions behind explicit allowlist policy and deterministic injected environment support for tests.
- [DONE] T172: Implement a strict RFC 8259 parser plus parse-json/json-doc options, duplicate handling, number/fallback/liberal options, escape handling, and FOJS errors.
- [DONE] T173: Implement json-to-xml and xml-to-json conversion vocabulary, validation, escaping, type mapping, duplicate keys, fallback, and roundtrip rules.
- [DONE] T174: Implement parse-xml, parse-xml-fragment, serialize, transform, parse-ietf-date, random-number-generator, trace, and error; recognize load-xquery-module and return its standard unsupported-feature error.
- [DONE] T175: Add table-driven tests covering every standard function signature/arity, optional argument form without C# overloads, return type/cardinality, context dependency, boundary value, collation/timezone, and mandated error.
- [DONE] T176: Add regex tests from XML Schema/XPath examples for syntax, Unicode classes, subtraction, flags, captures, replacement, zero-length behavior, analyze-string trees, and all FORX errors.
- [DONE] T177: Add JSON, URI, QName, date/time/number formatting, resource, environment-policy, random generator reproducibility, serialize, transform, trace, and error function integration tests.

### XSLT stylesheet and package compilation

- [DONE] T178: Implement stylesheet module loading through the guarded resolver, XML parsing, root/version recognition, simplified stylesheets, base URIs, and module-cycle detection.
- [DONE] T179: Implement include/import assembly, import precedence, declaration order, top-level constraints, forwards/backwards-compatible behavior, fallback, and static error locations.
- [DONE] T180: Implement package manifests, use-package version matching, accept/expose/override, visibility, abstract/final components, component binding, symbolic references, and separate package reuse.
- [DONE] T181: Implement stylesheet static context, namespace aliases, excluded/extension prefixes, xpath-default-namespace, default collation/mode/validation, expand-text, and version behavior.
- [DONE] T182: Implement static params/variables, shadow attributes, use-when, static XPath evaluation, visibility across modules/packages, and circular/static dependency errors.
- [DONE] T183: Implement attribute value template and text value template parsers with brace escaping, nested XPath, namespace/static context, source ranges, and errors.
- [DONE] T184: Parse/compile global/local variables and params, required/tunnel/static flags, sequence types, select/content exclusivity, and dependency graph.
- [DONE] T185: Parse/compile named and match templates, modes, priorities, visibility, context-item declarations, return types, and sequence constructors.
- [DONE] T186: Parse/compile stylesheet functions, parameters, as/cache/visibility/override semantics, recursion, and component references.
- [DONE] T187: Parse/compile modes and all built-in on-no-match behaviors, streamable/typed flags, warnings, accumulators, and mode component merging.
- [DONE] T188: Implement XSLT pattern parser/matcher plan for allowed axes, node/name/kind/schema tests, predicates, id/key, rooted paths, union/intersect/except, and motionless constraints.
- [DONE] T189: Implement default priority, import precedence, explicit priority, declaration order, ambiguity warnings/errors, mode filtering, and indexed template candidate selection.
- [DONE] T190: Parse/compile attribute sets with precedence/merge/use chains and circularity detection.
- [DONE] T191: Parse/compile key declarations with match/use/composite/collation, merged same-name declarations, types, and static constraints.
- [DONE] T192: Parse/compile accumulator declarations/rules with pre/post phases, initial value, streamable analysis hooks, dependencies, and circularity checks.
- [DONE] T193: Parse/compile decimal formats, output formats, character maps, whitespace strip/preserve rules, namespace aliases, and conflict/precedence rules.
- [DONE] T194: Parse global-context-item, import-schema, package metadata, and schema-aware declarations with conformance-tier gating and standard errors.
- [DONE] T195: Compile every XSLT 3.0 instruction and helper element into internal instruction plans, validating attributes, child structure, sequence types, contexts, and static constraints.
- [DONE] T196: Build immutable XsltExecutable with linked components, XPath plans, pattern indexes, globals, outputs, keys, accumulators, packages, conformance flags, and source maps.
- [DONE] T197: Complete XsltCompiler and XsltPackageResolver public implementations and ensure reusable compiled executables are thread-safe.
- [DONE] T198: Add XSLT compilation tests for stylesheet forms, versions, modules, packages, precedence, static evaluation, AVTs/TVTs, every declaration/instruction, patterns, conflicts, and XTSE/XTTE errors.

### XSLT transformation runtime

- [DONE] T199: Implement TransformationEngine initialization for initial source/match/template/function/mode, global context item, parameters, current-time snapshot, document pool, and principal destination.
- [DONE] T200: Implement lazy global variable/parameter evaluation, tunnel/local parameter binding, type conversion, required params, dependency cycles, and failure memoization.
- [DONE] T201: Implement template selection and invocation, built-in template rules, apply-templates, call-template, next-match, apply-imports, context-item, with-param, and mode state.
- [DONE] T202: Implement sequence/value construction and literal result elements plus xsl:document/element/attribute/namespace/text/comment/processing-instruction with namespace/content constraints.
- [DONE] T203: Implement copy and copy-of with copy-namespaces, validation/type preservation, base URI, accumulator behavior, and duplicate namespace/attribute rules.
- [DONE] T204: Implement if/choose, for-each, iterate/next-iteration/break/on-completion, variables, sequence, where-populated, on-empty, and on-non-empty.
- [DONE] T205: Implement try/catch error matching and variables, rollback-output semantics, assert, message/terminate, fallback, and error stack/source propagation.
- [DONE] T206: Implement xsl:sort and perform-sort with multi-key stable ordering, atomization, data type, order, case order, lang, collation, empty/NaN, and errors.
- [DONE] T207: Implement for-each-group for group-by, group-adjacent, group-starting-with, group-ending-with, composite/collated keys, current-group, and current-grouping-key.
- [DONE] T208: Implement xsl:number single/multiple/any selection and complete formatting tokens, alphabetic/ordinal/language/letter-value/grouping behavior.
- [DONE] T209: Implement lazy per-document key indexes, merged declarations, composite/collated keys, key() lookup/context, document identity, and safe shared caches.
- [DONE] T210: Implement accumulators, rule matching, pre/post values, applicability, before/after functions, dependency checks, and grounded runtime behavior.
- [DONE] T211: Implement analyze-string/matching-substring/non-matching-substring/regex-group and integrate the XPath regex engine and dynamic context.
- [DONE] T212: Implement xsl:map/map-entry and xsl:array/array-member plus stylesheet functions, higher-order values, closures, component override, and finality.
- [DONE] T213: Implement xsl:evaluate parsing/compilation, supplied namespace/static context, variables, context item, schema/option controls, cache isolation, and security limits.
- [DONE] T214: Implement source-document and merge/merge-source/merge-key/merge-action, input sorting, current-merge-group/key, URI/document pool behavior, and errors.
- [DONE] T215: Implement result-document, named output format selection, destination URI resolution, duplicate/collision detection, principal/secondary result bookkeeping, and atomic commit/rollback through the resolver.
- [DONE] T216: Implement fn:transform option validation and result maps for stylesheet location/node/text/executable, source variants, initial component, params, delivery format, secondary results, and errors.
- [DONE] T217: Implement the complete XSLT-specific function library, xsl:original component references, XsltTransformer, message/result-document/trace public implementations, and runtime diagnostics with template/function/component stacks.
- [DONE] T218: Add XSLT runtime tests for initialization, globals/params, every instruction family, template/mode selection, construction/copy, control flow, errors/messages, and context functions.
- [DONE] T219: Add focused XSLT tests for sorting, grouping, numbering, keys, accumulators, regex, maps/arrays/HOFs, evaluate, merge, result documents, packages, fn:transform, and all related error codes.

### Streaming, extensions, safety, and concurrency

- [DONE] T220: Implement event-to-event transformation pipeline and serializer/canonicalizer sinks that preserve bounded memory for streamable operations.
- [DONE] T221: Implement XSLT posture/sweep classification and guaranteed-streamability rules for expressions, patterns, sequence constructors, templates, functions, modes, accumulators, and packages.
- [DONE] T222: Implement streamed node/focus lifetime rules, consuming navigation, grounding buffers, snapshot/copy-of, and deterministic illegal-access errors.
- [DONE] T223: Implement streaming template execution, streamable built-in rules, xsl:source-document, xsl:fork, streaming accumulators, and streamed result delivery.
- [DONE] T224: Complete XsltStreamAnalyzer public implementation with per-construct classification, source ranges, reasons, conformance level, and fallback requirements.
- [DONE] T225: Implement all interface-based extension registries for resolvers, collations, XPath functions, XSLT functions/instructions, serialization methods, sinks, traces, messages, cancellation, and results with immutable post-compile binding.
- [DONE] T226: Enforce extension trust policy, QName/arity/signature checks, value conversion, exception wrapping, reentrancy rules, resolver limits, and deterministic fallback/availability behavior.
- [DONE] T227: Audit shared state and make compiled XPath expressions, schemas, stylesheets, packages, character maps, pattern indexes, and key definitions immutable while keeping each run context isolated.
- [DONE] T228: Add streaming tests proving output equivalence with grounded execution, bounded-lifetime enforcement, posture/sweep reports, fork/accumulator behavior, non-streamable rejection/fallback, and chunk invariance.
- [DONE] T229: Add extension/security tests for registration and lookup, collisions, type/arity errors, thrown exceptions, denied resources, reentrancy, cancellation, limits, result rollback, and no ambient global state.
- [DONE] T230: Add concurrency correctness tests that reuse compiled parser configuration, XPath plans, schema sets, and XSLT executables across parallel independent operations and verify deterministic isolation.

### Complete public API and standards conformance testing

- [DONE] T231: Build a W3C XML Conformance Test Suite adapter for specs/test-suites/xmlts-20130923.zip covering XML 1.0 editions, XML 1.1, Namespaces 1.0/1.1, valid/invalid/not-well-formed classifications, and expected diagnostics.
- [DONE] T232: Run and classify the complete W3C XML suite for every applicable parser profile; resolve every failure or record only specification-approved non-applicable metadata, with no generic skip bucket.
- [DONE] T233: Build a QT3 adapter for specs/test-suites/qt3tests-master.zip that loads catalogs/environments/dependencies, evaluates XPath 3.1 tests, supports all assertion forms, and filters XQuery-only cases explicitly.
- [DONE] T234: Run and classify all XPath 3.1/XDM/F&O/Serialization-applicable QT3 cases, including optional schema-aware cases when that tier is enabled; fix failures and retain exact unsupported optional-feature classifications.
- [DONE] T235: Build an XSLT 3.0 suite adapter for specs/test-suites/xslt30-test-master.zip covering environments, packages, schemas, sources, collections, initial modes/templates/functions, params, messages, secondary results, serialization, dependencies, and assertion forms.
- [DONE] T236: Run and classify all basic XSLT 3.0 suite cases, then schema-aware and streaming subsets; fix failures and allow exclusions only for features explicitly outside this plan such as XQuery modules.
- [DONE] T237: Add cross-layer roundtrip tests for parse-event-tree-XDM-serialize-reparse, preservation modes, namespace fixup, XML versions/encodings, DTD defaults, XInclude, canonical bytes, and source locations.
- [DONE] T238: Add XPath-versus-XSLT integration tests proving identical XPath semantics, static/dynamic contexts, functions, collations, document pools, schema annotations, errors, and resource policies inside and outside stylesheets.
- [DONE] T239: Add deterministic failure-injection tests for every public operation at input reads, decoding, resolution, handlers, extensions, tree mutation, output writes/flushes, result commits, and cancellation, verifying cleanup and typed diagnostics.
- [DONE] T240: Create and satisfy a public API coverage matrix mapping every src/Api type, constructor, interface method, public field, enum member, static field, option branch, resolver/extension callback, successful result, and documented exception to at least one executable test.
- [DONE] T241: Add compatibility tests for strict basic, validating DTD, schema-aware, and streaming profiles so enabling one optional tier cannot silently alter unrelated XML/XPath/XSLT semantics.
- [DONE] T242: Perform the final dependency and artifact audit: src/ references only built-in .NET assemblies, all downloaded suites remain reproducibly consumable, and the full functional/conformance test suite passes.

## Completion criteria

- All T001–T242 tasks are complete and no planned public operation remains a stub.
- Strict XML 1.0/1.1 and Namespaces profiles pass every applicable W3C XML test.
- XPath 3.1, XDM 3.1, Functions and Operators 3.1, and Serialization 3.1 pass every applicable QT3 test except explicitly classified XQuery-only or implementation-defined cases.
- Basic XSLT 3.0, then schema-aware and streaming tiers, pass every applicable official XSLT 3.0 test for the claimed conformance level.
- Every src/Api behavior and public data/configuration path has executable coverage, including failures, source locations, limits, resolvers, extension hooks, and output rollback.
- No external library is referenced by src/; external test dependencies remain confined to tests/.
