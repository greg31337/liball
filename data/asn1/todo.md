# Universal ASN.1 schema compiler and portable runtime development plan

## Goal and conformance boundary

Build a schema compiler, code generator, reflection model, and schema-driven/runtime-codec library in C# that covers the 2021 editions of ITU-T X.680, X.681, X.682, X.683, and X.690 through X.696, including the X.690 (2021) Erratum 1. “Complete” means every normative grammar production, static-semantic rule, abstract value form, encoding procedure, canonical restriction, and applicable extension mechanism in those editions is either implemented or rejected with a precise diagnostic where the standard itself declares the input invalid. The target includes:

| Layer | Complete feature inventory |
|---|---|
| Lexical and source layer | Unicode source text, byte-order marks, whitespace, ASN.1 comments, identifiers and word forms, reserved words, number/real/binary/hex/string/time literals, XML value tokens, field references, encoding references, lexical ambiguity handling, source spans, trivia, error recovery, multiple source units, and incremental tokenization/parsing. |
| X.680 modules | Module identifiers, definitive OID/IRI identification, encoding references, `DEFINITIONS`, `BEGIN`/`END`, `EXPORTS`, `IMPORTS`, assigned module identifiers, module versions, `EXPLICIT`/`IMPLICIT`/`AUTOMATIC TAGS`, `EXTENSIBILITY IMPLIED`, all assignment categories, encoding-control sections, symbol visibility, transitive import resolution, duplicate/ambiguous/cyclic import diagnostics, and deterministic compilation ordering. |
| X.680 types | BOOLEAN, INTEGER and named numbers, ENUMERATED roots/extensions, REAL, BIT STRING and named bits, OCTET STRING, NULL, OBJECT IDENTIFIER, RELATIVE-OID, OID-IRI, RELATIVE-OID-IRI, CHOICE, SEQUENCE, SET, SEQUENCE OF, SET OF, tagged/prefixed types, selection types, `COMPONENTS OF`, INSTANCE OF, EXTERNAL, EMBEDDED PDV, CHARACTER STRING, ObjectDescriptor, restricted character strings, UTF8String, UniversalString, BMPString, useful GeneralizedTime/UTCTime types, TIME, DATE, TIME-OF-DAY, DATE-TIME, DURATION, type references, and legacy ANY/ANY DEFINED BY input compatibility isolated from strict 2021 conformance. |
| X.680 values and extensibility | Every basic and XML value notation; named values/bits; signed and arbitrarily large integers; decimal/binary/special REAL values including infinities, NaN, and minus zero; character and tuple/quadruple strings; OID/IRI arcs; structured, collection, and CHOICE values; defined values; empty values; defaults; optional components; extension markers; extension additions; versioned extension-addition groups; extension end markers; exception specifications; and normalized value equality/order. |
| Tagging | Universal/application/context/private tag classes, low/high tag numbers, explicit and implicit tags, automatic tag synthesis, default tag modes, tag inheritance through references, CHOICE/ANY/open-type rules, tag uniqueness and ambiguity analysis, canonical tag metadata, and diagnostics for illegal implicit tagging or colliding alternatives/components. |
| X.682 and subtype constraints | Single-value, contained-subtype, value-range with MIN/MAX and open/closed endpoints, permitted alphabet, size, type constraints, inner `WITH COMPONENT`/`WITH COMPONENTS` constraints with PRESENT/ABSENT/OPTIONAL, pattern, property-settings, union/intersection/EXCEPT/ALL EXCEPT, extension markers and additional elements, root/extension partitioning, nested constraints, exception specs, satisfiability/emptiness, finite range/alphabet computation, and run-time validation. |
| X.682 general constraints | User-defined `CONSTRAINED BY`, simple table constraints, component-relation constraints and at-notation paths, contents constraints using CONTAINING/ENCODED BY, constraint parameters, object-set-driven open-type resolution, relational consistency, and encoding-visible effective-constraint calculation. User-defined constraint bodies are preserved and surfaced for application validation when their prose semantics cannot be executed. |
| X.681 information objects | Object class assignments; type, fixed/variable value, fixed/variable value-set, object, and object-set fields; UNIQUE/OPTIONAL/DEFAULT; WITH SYNTAX token and optional-group grammars; default and defined object syntax; object/object-set assignments; object set algebra and extension markers; field references; information-from-objects forms; object/value/value-set/object-set extraction; TYPE-IDENTIFIER and ABSTRACT-SYNTAX useful classes; INSTANCE OF; table dispatch maps; extensible object sets; dynamic registrations; and open-type decode/encode. |
| X.683 parameterization | Formal parameters and governors for types, values, value sets, object classes, objects, and object sets; dummy references; actual parameter lists; parameterized assignments/references for every assignment category; parameterized value-set types; governor inference; substitution with lexical scope and capture avoidance; nested/recursive instantiation; memoization; cycle detection; and diagnostics for arity, kind, governor, and constraint mismatches. |
| Schema semantics and IR | Name binding, namespaces, assignment-kind disambiguation, reference graph, recursive type legality, value evaluation, constraint normalization, tag calculation, COMPONENTS OF expansion, automatic tags, parameter instantiation, object-set/table materialization, open-type dispatch, encoding instruction resolution, canonical stable IDs, immutable-by-convention lowered IR, source-to-IR traceability, deterministic serialization, and complete error/warning diagnostics. |
| Code generation | Deterministic field-based C# models compatible with `syntax.md`; safe identifier/file mapping; constants and named values; choice discriminants; optional/default presence fields; extensibility and unknown data containers; constructors and interface-backed operations where permitted; schema descriptors; reflection tables; constraint validators; codec entry points; encode-only/decode-only/runtime-table modes; namespace/output mapping; collision handling; reproducible output; and compilation of multi-module recursive schemas. |
| Reflection metadata | Modules, imports/exports, assignments, type/value/object graphs, fields, tags, constraints, defaults, extension roots/groups, parameter origins, object-class/table relationships, open-type dispatch, encoding instructions, source locations, generated names, and traversal/query without requiring generated CLR reflection attributes. |
| X.690 BER | Schema-aware and schema-less identifier/length/content processing; short/high tag forms; definite/indefinite length; EOC; primitive/constructed encodings; all universal types; constructed and segmented strings; SET/SEQUENCE/CHOICE; EXTERNAL/EMBEDDED PDV/INSTANCE OF/open types; unknown TLV retention; nested resource limits; event/tree/value decoding; streaming encode/decode; and exact malformed-input offsets. |
| X.690 DER/CER | DER minimal identifiers/lengths/integers/OIDs/REAL/time/string forms, definite lengths, default omission, BIT STRING unused-bit checks, SET component order, SET OF lexicographic order, and primitive-string rules; CER indefinite constructed forms, 1000-character/octet segmentation, canonical primitive forms, set ordering, and EOC; strict canonical validators that diagnose the first violation without re-encoding; and canonical transcode/check APIs. |
| X.691 PER family | BASIC-PER and CANONICAL-PER, aligned (APER) and unaligned (UPER), bit/octet alignment, constrained/semi-constrained/unconstrained whole numbers, normally-small values, length determinants and fragmentation, effective size/value/alphabet constraints, root/extension bits, presence maps, extension addition bitmaps/groups, open-type wrapping, CHOICE/ENUMERATED indexes, restricted/unrestricted strings, all primitive/constructed/useful types, table/open-type dispatch, unknown extension retention, trailing padding checks, streaming bit I/O, and canonical-PER restrictions. |
| X.692 ECN | Complete ECN lexical/grammar support; encoding class/object/object-set assignments; built-in class categories and groups; encoding structures; primitive encodings; concatenation, alternatives, repetition, optionality, tags, padding and alignment; size/range/alphabet/representation/byte-order/bit-order properties; conditions and determinants; encoding links, paths and mappings; parameters/governors; ordered encoding object lists; `#ENCODINGS`; built-in BER/PER encoding objects; replacement structures; transformations; validation; lowering to executable encoding plans; and symmetric streaming execution. |
| X.693 XER family | BASIC-XER, CANONICAL-XER, and EXTENDED-XER; XML 1.0 and namespace correctness; all ASN.1 types and lexical forms; namespace/name derivation; attributes/elements/text/list representations; whitespace and escaping; unknown extension XML retention; schema-aware streaming XML; canonical namespace/attribute/content handling; and all XER instructions: ANY-ATTRIBUTES, ANY-ELEMENT, ATTRIBUTE, BASE64, DECIMAL, DEFAULT-FOR-EMPTY, ELEMENT, EMBED-VALUES, GLOBAL-DEFAULTS, LIST, NAME, NAMESPACE, PI-OR-COMMENT, TEXT, UNTAGGED, USE-NIL, USE-NUMBER, USE-ORDER, USE-QNAME, USE-TYPE, USE-UNION, WHITESPACE, their negations, target lists, combinations, and final-instruction resolution. |
| X.694 XSD mapping | XML Schema 1.0 document loading and namespace/import/include/redefine resolution; built-in XSD type module; simple restriction/list/union types and facets; complex/simple content; sequence/choice/all particles and occurrence ranges; elements/attributes; groups/attribute groups; extension/restriction derivation; default/fixed/nillable/abstract/form/block/final behavior; wildcards and processContents; mixed content; substitution groups; QName/namespace mapping; annotations where semantically relevant; generated module identity/imports/types/constraints; required XER encoding instructions; deterministic name collision handling; and explicit diagnostics for constructs X.694 declares unmappable. |
| X.695 PER instructions | PER encoding-reference recognition, prefixed and targeted instructions, encoding-control assignment lists, target lists by type/built-in/context path, positive and negating instructions, precedence and multiple-assignment/final-instruction resolution, applicability checks, unknown-instruction errors for UPER, silent APER behavior, preservation through contents/type constraints, and an application-extensible registered-instruction handler/registry model. |
| X.696 OER family | BASIC-OER and CANONICAL-OER; preambles; length determinants; fixed/variable signed and unsigned integer encodings derived from constraints; enumerated values; REAL; BIT/OCTET/character strings; OID/relative OID/IRI/time types; sequence/set optional/default maps; choices and tags; collections; extensions/open types/table dispatch; unknown extension retention; canonical integer/length/preamble/default/order rules; streaming octet I/O; and strict COER checking. |
| Runtime cross-cutting | One normalized value model shared by codecs; schema-driven and generated-code paths; byte, bit, text, and Stream inputs/outputs; incremental state machines returning NeedMoreData/Done/Error; safe maximum depth, element count, length, integer size, and allocation limits; exact consumed/written counts; non-throwing result APIs for frequent incomplete-input cases plus exceptions for operation failure; unknown root/extension/open content preservation; re-encoding fidelity where permitted; canonicality reports; deterministic behavior; thread-safe immutable compiled schemas; and no external dependency under `src/`. |

Not in the mandatory conformance boundary: JER, GSER, ASN.X, legacy X.208 macros, protocol-specific NAS/ACN dialects, GUI/IDE features, or code-generation languages other than C#. Their useful design ideas and fixtures may inform the architecture, but adding them requires a later scope decision. Performance benchmarks, release work, end-user documentation, and syntax-policy/source-scanning tests are deliberately absent from this backlog.

## Research-derived feature baseline

| Implementation | Relevant observed coverage | Design lesson / gap this plan addresses |
|---|---|---|
| [vlm/asn1c](https://github.com/vlm/asn1c) | C/C++ generation, descriptors, constraint checks, incremental BER decode, DER encode, UPER, OER/COER-compatible path, and XER/CXER; its own interoperability table says CER encoding and APER are unsupported. | Keep portable descriptors and resumable decoding, but cover CER, APER, full canonical checks, X.681/X.683 semantics, and all X.692–X.696 recommendations. |
| [OSS Nokalva ASN.1 Tools](https://www.oss.com/asn1/products/documentation/asn1_csharp_5.5/asn1csharp-doc.html) | ASN.1:1990–2021, BER/DER, PER/UPER, OER/COER, XER/CXER/E-XER, JER, generated models/codecs, validation, printing, compiler directives, and partial/selective decode; product literature also lists CER and canonical PER variants. | Commercial-tool breadth is the practical completeness bar: validation, selective/streaming operations, encoding control, reflection-like metadata, and canonical variants must work together. |
| [Objective Systems ASN1C](https://obj-sys.com/products/asn1c/asn1c_ccpp.php) | Multi-language generation; BER/DER/CER, APER/UPER, OER/COER, XER/XML/JER; value notation, extension groups, content/table constraints, and broad type support. | Make table/content constraints, value compilation, open-type dispatch, extension groups, and all major encodings part of one IR instead of codec-specific special cases. |
| [asn1tools](https://asn1tools.readthedocs.io/en/latest/) | Python compile/encode/decode for BER, DER, PER, UPER, OER, XER, JER, and GSER, plus limited C generation; documentation explicitly excludes X.681 `CLASS`, X.683 parameterization, and EMBEDDED PDV. | Easy schema-driven APIs are valuable, but the compiler must not stop at the commonly used X.680 subset. |
| [pyasn1](https://pyasn1.readthedocs.io/en/latest/) | Dynamic ASN.1 value/schema objects, tagging and constraints, BER/CER/DER codecs, schema-less BER and streaming decode, and open-type support; no textual ASN.1 compiler or PER/OER/XER family. | Preserve a useful dynamic/reflection path independent of generated code and provide true incremental decoding. |
| [Bouncy Castle ASN.1](https://www.bouncycastle.org/documentation/documentation-c/) | Mature low-level ASN.1 object, parser, generator, and cryptographic BER/CER/DER usage, including stream-oriented forms; not a universal textual schema compiler and does not target PER/OER/XER. | Expose a low-level TLV API as well as schema-aware high-level codecs, and test heavily with PKIX/CMS material. |
| [ESA ASN1SCC](https://github.com/esa/asn1scc) | Embedded-focused C/Ada/Scala generation, UPER, custom ACN encodings, bounded/static representations, and verification-oriented output. | ECN needs a real executable encoding-plan layer; generated representations must surface bounds and avoid requiring runtime CLR reflection. |
| [commercial asn1c FAQ](https://www.asn1c.com/faq.html) | BER/CER/DER, PER, macros, information objects, parameterization, ROSE, and cross-development. | Information objects and parameterization are established compiler requirements, not optional parser syntax. |

The local copies and normative/supporting source provenance are inventoried in `specs/README.md`.

## Architecture and implementation constraints

The pipeline is `source bytes/text -> incremental lexer -> recoverable concrete syntax -> bound semantic graph -> normalized constraints/tags/objects/parameters -> encoding-neutral IR -> reflection catalog + C# generator + codec plans -> generated or dynamic runtime values`. BER, PER, OER, XER, and ECN plans share schema descriptors and value validation; canonical modes add rule-specific validation and deterministic ordering.

All `src/` code uses built-in .NET APIs only. `System.Numerics.BigInteger`, `System.IO.Stream`, `System.Text`, `System.Xml` primitives where their exception behavior is compatible, and `Span<T>`/`ReadOnlySpan<T>` are preferred where appropriate. The runtime cannot depend on Roslyn, parser generators, XML packages, or third-party ASN.1 libraries. Tests may use external packages and tools for independent interoperability oracles.

The `syntax.md` subset determines the API shape: public data is mutable field-based classes; operations are interfaces implemented by public classes; library-defined generic types/methods, properties, attributes, overloads, delegates, records/structs, value-type nullables, `ref`/`out`, iterators, async/await, LINQ, exception inheritance chains, and public static methods are not used. Distinct method names replace overloads, result classes replace `ref`/`out` and nullable value results, synchronous `Stream` plus explicit incremental state objects replace async APIs, and composition replaces class inheritance. Generated C# defaults to the same small portable subset. Expected incomplete-input states use result/status objects; invalid schema, invalid value, unsupported registered behavior, and I/O failures use direct `System.Exception` subclasses. Required XML comments using only the constructs allowed by `syntax.md` are part of implementing each public declaration rather than a separate documentation task.

## Proposed public API and files

The list is intentionally shape-level: exact fields and parameter types are fixed while implementing T001–T010. Every data class will have at least one public instance field and be consumed by an interface or another data class; every enum will be consumed by a public data/interface type; public implementation classes will have one constructor and expose only members declared by their interfaces.

### `src/Api/Contracts`

| File / interface | Operation surface |
|---|---|
| `IAsnCompiler.cs` | Compile source units and options into diagnostics, schema IR, metadata, and generated files. |
| `IAsnLexer.cs` | Tokenize a source unit, including incremental continuation and source spans. |
| `IAsnParser.cs` | Parse tokens/source into module syntax results with recovery diagnostics. |
| `ISchemaValidator.cs` | Bind, normalize, and validate one or more parsed modules. |
| `IAsnValueValidator.cs` | Validate a runtime value against a reflected type and its effective constraints. |
| `IAsnCodeGenerator.cs` | Generate deterministic C# and/or runtime descriptor artifacts from a validated schema. |
| `IAsnReflectionService.cs` | Build/query a reflection catalog and resolve qualified module/type/value names. |
| `IAsnCodec.cs` | Encode, decode, try-decode, and check canonicality for one configured encoding-rule variant. |
| `IAsnCodecProvider.cs` | Create configured schema-driven codecs without public static factories. |
| `IAsnStreamingCodec.cs` | Encode/decode values through `Stream` with limits and exact progress results. |
| `IAsnIncrementalDecoder.cs` | Consume successive byte/text chunks, report NeedMoreData/Done/Error, complete, and reset. |
| `IBerTlvReader.cs` | Read schema-less BER/CER/DER TLV events or trees. |
| `IBerTlvWriter.cs` | Write schema-less BER/CER/DER TLV events/trees. |
| `IEcnCompiler.cs` | Compile ECN sections/specifications into validated executable encoding plans. |
| `IXsdSchemaImporter.cs` | Map XML Schema 1.0 sources into validated ASN.1 modules per X.694. |
| `IPerInstructionRegistry.cs` | Register, resolve, and enumerate application-provided PER instruction handlers. |
| `IPerInstructionHandler.cs` | Validate and apply one registered PER instruction to a lowering context. |

### `src/Api/Data`

| Files | Purpose |
|---|---|
| `AsnSourceUnit.cs`, `AsnSourcePosition.cs`, `AsnSourceSpan.cs`, `AsnDiagnostic.cs`, `AsnDiagnosticRelatedLocation.cs` | Inputs and exact diagnostic locations. |
| `AsnCompilerOptions.cs`, `AsnCompilationInput.cs`, `AsnCompilationResult.cs`, `AsnParserResult.cs`, `AsnLexerResult.cs`, `AsnValidationResult.cs`, `AsnGenerationOptions.cs`, `AsnGeneratedFile.cs` | Compiler phase requests/results without overloads or out parameters. |
| `AsnToken.cs`, `AsnModule.cs`, `AsnImport.cs`, `AsnAssignment.cs`, `AsnType.cs`, `AsnComponent.cs`, `AsnValue.cs`, `AsnNamedValue.cs`, `AsnConstraint.cs`, `AsnConstraintElement.cs`, `AsnTag.cs`, `AsnParameter.cs`, `AsnActualParameter.cs` | Public syntax/semantic schema model. |
| `AsnObjectClass.cs`, `AsnObjectClassField.cs`, `AsnInformationObject.cs`, `AsnInformationObjectSet.cs`, `AsnObjectFieldSetting.cs`, `AsnOpenTypeDispatchEntry.cs` | X.681 information-object and open-type metadata. |
| `AsnEncodingInstruction.cs`, `AsnEncodingPlan.cs`, `AsnEncodingPlanStep.cs`, `AsnEcnClass.cs`, `AsnEcnObject.cs`, `AsnPerInstructionRegistration.cs` | X.692/X.693/X.695 encoding control and executable plan data. |
| `AsnReflectionCatalog.cs`, `AsnModuleDescriptor.cs`, `AsnTypeDescriptor.cs`, `AsnFieldDescriptor.cs`, `AsnValueDescriptor.cs`, `AsnConstraintDescriptor.cs`, `AsnEncodingDescriptor.cs` | Stable reflection metadata used by generated and schema-driven paths. |
| `AsnRuntimeValue.cs`, `AsnRuntimeFieldValue.cs`, `AsnBitStringValue.cs`, `AsnObjectIdentifierValue.cs`, `AsnRealValue.cs`, `AsnTimeValue.cs`, `AsnOpenTypeValue.cs`, `AsnUnknownExtension.cs`, `AsnUnknownField.cs` | Encoding-neutral values, including arbitrary integers, bit counts, special reals, open content, and preserved extensions. |
| `AsnCodecOptions.cs`, `AsnResourceLimits.cs`, `AsnEncodeRequest.cs`, `AsnEncodeResult.cs`, `AsnDecodeRequest.cs`, `AsnDecodeResult.cs`, `AsnCanonicalityResult.cs`, `AsnStreamProgress.cs`, `AsnIncrementalInput.cs`, `AsnIncrementalResult.cs` | Codec configuration/results, limits, consumed/written counts, and non-throwing incremental states. |
| `BerTlv.cs`, `BerTlvEvent.cs`, `BerReadRequest.cs`, `BerReadResult.cs`, `BerWriteRequest.cs`, `BerWriteResult.cs` | Low-level schema-less TLV API. |
| `AsnXsdInput.cs`, `AsnXsdImportOptions.cs`, `AsnXsdImportResult.cs` | X.694 input, namespace resolution, and mapping result. |

### `src/Api/Enums`

`AsnEncodingRules.cs`, `AsnDiagnosticSeverity.cs`, `AsnDiagnosticCategory.cs`, `AsnTokenKind.cs`, `AsnAssignmentKind.cs`, `AsnTypeKind.cs`, `AsnValueKind.cs`, `AsnConstraintKind.cs`, `AsnTagClass.cs`, `AsnTaggingMode.cs`, `AsnParameterKind.cs`, `AsnObjectFieldKind.cs`, `AsnInstructionKind.cs`, `AsnPlanStepKind.cs`, `AsnRuntimeValueKind.cs`, `AsnDecodeStatus.cs`, `AsnCanonicalityStatus.cs`, `AsnTlvEventKind.cs`, `AsnRealKind.cs`, `AsnTimeKind.cs`, `AsnUnknownDataPolicy.cs`, and `AsnCodeGenerationTarget.cs`.

### `src/Api/Static`

| File / class | Fields only |
|---|---|
| `AsnStandardEditions.cs` | Supported recommendation editions and errata identifiers. |
| `AsnUniversalTags.cs` | Universal tag-number constants. |
| `AsnEncodingObjectIdentifiers.cs` | Standard BER/CER/DER/PER/OER/XER transfer-syntax and encoding-rule OIDs/IRIs. |

### `src/Api/Exceptions`

Each directly derives from `System.Exception`: `AsnCompilationException.cs`, `AsnParseException.cs`, `AsnSchemaException.cs`, `AsnConstraintException.cs`, `AsnEncodingException.cs`, `AsnDecodingException.cs`, `AsnCanonicalityException.cs`, `AsnResourceLimitException.cs`, `AsnEcnException.cs`, `AsnXsdMappingException.cs`, and `AsnCodeGenerationException.cs`.

### `src/Api/Classes`

| File / implementation class | Implements |
|---|---|
| `AsnCompiler.cs` | `IAsnCompiler` |
| `AsnLexer.cs` | `IAsnLexer` |
| `AsnParser.cs` | `IAsnParser` |
| `AsnSchemaValidator.cs` | `ISchemaValidator` |
| `AsnValueValidator.cs` | `IAsnValueValidator` |
| `AsnCodeGenerator.cs` | `IAsnCodeGenerator` |
| `AsnReflectionService.cs` | `IAsnReflectionService` |
| `AsnCodec.cs` | `IAsnCodec` |
| `AsnCodecProvider.cs` | `IAsnCodecProvider` |
| `AsnStreamingCodec.cs` | `IAsnStreamingCodec` |
| `AsnIncrementalDecoder.cs` | `IAsnIncrementalDecoder` |
| `BerTlvReader.cs` | `IBerTlvReader` |
| `BerTlvWriter.cs` | `IBerTlvWriter` |
| `EcnCompiler.cs` | `IEcnCompiler` |
| `XsdSchemaImporter.cs` | `IXsdSchemaImporter` |
| `PerInstructionRegistry.cs` | `IPerInstructionRegistry` |

## Planned internal source files

All types below are non-public and therefore outside `src/Api`. File-scoped namespaces and composition are used throughout.

| Directory | Planned files |
|---|---|
| `src/Compiler/Lexing` | `SourceCursor.cs`, `LexerState.cs`, `TokenBuffer.cs`, `TriviaScanner.cs`, `LiteralScanner.cs`, `IdentifierScanner.cs`, `XmlValueScanner.cs`, `IncrementalLexSession.cs` |
| `src/Compiler/Parsing` | `TokenCursor.cs`, `ParseContext.cs`, `ModuleParser.cs`, `AssignmentParser.cs`, `TypeParser.cs`, `ValueParser.cs`, `ConstraintParser.cs`, `ObjectClassParser.cs`, `ObjectParser.cs`, `ParameterParser.cs`, `EncodingControlParser.cs`, `EcnParser.cs`, `RecoverySet.cs`, `IncrementalParseSession.cs` |
| `src/Compiler/Semantics` | `CompilationGraph.cs`, `ModuleBinder.cs`, `ImportResolver.cs`, `SymbolTable.cs`, `ReferenceBinder.cs`, `AssignmentClassifier.cs`, `TypeResolver.cs`, `ValueEvaluator.cs`, `TagResolver.cs`, `AutomaticTagger.cs`, `ComponentsOfExpander.cs`, `ConstraintNormalizer.cs`, `ConstraintSolver.cs`, `ParameterInstantiator.cs`, `ObjectClassResolver.cs`, `ObjectSetEvaluator.cs`, `OpenTypeTableBuilder.cs`, `EncodingInstructionResolver.cs`, `SchemaRuleValidator.cs`, `DiagnosticBag.cs` |
| `src/Compiler/Ir` | `IrBuilder.cs`, `IrModule.cs`, `IrAssignment.cs`, `IrType.cs`, `IrField.cs`, `IrValue.cs`, `IrConstraint.cs`, `IrObjectTable.cs`, `IrEncodingInstruction.cs`, `IrStableId.cs`, `IrSerializer.cs` |
| `src/Compiler/Generation` | `CSharpGenerator.cs`, `CSharpNameMapper.cs`, `CSharpFilePlanner.cs`, `CSharpTextWriter.cs`, `ModelEmitter.cs`, `ConstantEmitter.cs`, `ChoiceEmitter.cs`, `PresenceEmitter.cs`, `ValidatorEmitter.cs`, `DescriptorEmitter.cs`, `CodecFacadeEmitter.cs`, `GeneratedSourceMap.cs` |
| `src/Runtime/Common` | `ValueAccess.cs`, `IntegerMath.cs`, `RealMath.cs`, `TimeValueRules.cs`, `OidRules.cs`, `CharacterSetRules.cs`, `ConstraintEvaluator.cs`, `OpenTypeResolver.cs`, `UnknownDataStore.cs`, `ResourceCounter.cs`, `ByteAccumulator.cs`, `TextAccumulator.cs`, `CodecPlanExecutor.cs` |
| `src/Runtime/Ber` | `BerIdentifierReader.cs`, `BerIdentifierWriter.cs`, `BerLengthReader.cs`, `BerLengthWriter.cs`, `BerPrimitiveCodec.cs`, `BerConstructedCodec.cs`, `BerStringCodec.cs`, `BerRealCodec.cs`, `BerTimeCodec.cs`, `BerSetSorter.cs`, `BerEventReader.cs`, `BerEventWriter.cs`, `BerDecodeMachine.cs`, `BerEncodeMachine.cs`, `DerRuleChecker.cs`, `CerRuleChecker.cs` |
| `src/Runtime/Per` | `BitInput.cs`, `BitOutput.cs`, `PerConstraintInfo.cs`, `PerIntegerCodec.cs`, `PerLengthCodec.cs`, `PerStringCodec.cs`, `PerCollectionCodec.cs`, `PerExtensionCodec.cs`, `PerOpenTypeCodec.cs`, `PerDecodeMachine.cs`, `PerEncodeMachine.cs`, `PerCanonicalChecker.cs`, `PerInstructionApplier.cs` |
| `src/Runtime/Oer` | `OerInput.cs`, `OerOutput.cs`, `OerLengthCodec.cs`, `OerIntegerCodec.cs`, `OerStringCodec.cs`, `OerPreambleCodec.cs`, `OerCollectionCodec.cs`, `OerExtensionCodec.cs`, `OerOpenTypeCodec.cs`, `OerDecodeMachine.cs`, `OerEncodeMachine.cs`, `CoerRuleChecker.cs` |
| `src/Runtime/Xer` | `XmlCharRules.cs`, `XmlTokenReader.cs`, `XmlTokenWriter.cs`, `XmlNamespaceContext.cs`, `XerNameResolver.cs`, `XerPrimitiveCodec.cs`, `XerCollectionCodec.cs`, `XerInstructionApplier.cs`, `XerDecodeMachine.cs`, `XerEncodeMachine.cs`, `CxerRuleChecker.cs`, `UnknownXmlStore.cs` |
| `src/Runtime/Ecn` | `EcnSymbolTable.cs`, `EcnClassResolver.cs`, `EcnObjectEvaluator.cs`, `EcnPropertyResolver.cs`, `EcnLinkResolver.cs`, `EcnStructureLowerer.cs`, `EcnPlanValidator.cs`, `EcnPlanExecutor.cs`, `EcnPrimitiveCodecs.cs`, `EcnDecodeMachine.cs`, `EcnEncodeMachine.cs` |
| `src/Compiler/Xsd` | `XsdDocumentReader.cs`, `XsdNamespaceResolver.cs`, `XsdComponentTable.cs`, `XsdSimpleTypeMapper.cs`, `XsdComplexTypeMapper.cs`, `XsdParticleMapper.cs`, `XsdAttributeMapper.cs`, `XsdFacetMapper.cs`, `XsdWildcardMapper.cs`, `XsdSubstitutionMapper.cs`, `XsdNameMapper.cs`, `XsdModuleEmitter.cs`, `XsdXerInstructionEmitter.cs` |

Planned tests mirror these areas under `tests/Api`, `tests/Compiler`, `tests/Runtime/Ber`, `tests/Runtime/Per`, `tests/Runtime/Oer`, `tests/Runtime/Xer`, `tests/Runtime/Ecn`, `tests/Xsd`, `tests/Generation`, `tests/Interop`, and `tests/Fixtures`. Tests may reference Bouncy Castle and external fixture generators, but `src/asn1.csproj` must remain dependency-free beyond the target .NET framework.

## Dependency-ordered implementation backlog

### Foundation and public surface

- [DONE] T001: Fix the supported target framework and portability baseline, enable deterministic builds, and configure `src/asn1.csproj` with only built-in .NET references and `tests/asn1.Tests.csproj` with the selected test framework.
- [DONE] T002: Create the `src/Api` directory categories and implement the public enums, ensuring every enum has explicit int32 members and is consumed by a public data or contract type.
- [DONE] T003: Implement source-unit, position, span, diagnostic, and related-location data classes with normalized line/column/offset semantics for Unicode and multi-file compilation.
- [DONE] T004: Implement compiler, parser, lexer, validation, generation, XSD, and codec request/result data classes using fields, explicit presence flags instead of nullable value types, and distinct result objects instead of `ref`/`out`.
- [DONE] T005: Implement the public schema, information-object, encoding-plan, reflection, runtime-value, unknown-data, and TLV data classes, and define invariants for arbitrary-size integers, bit lengths, special REAL values, and time values.
- [DONE] T006: Implement all public operation interfaces with unique non-overloaded instance method names and synchronous `Stream`/span-friendly signatures.
- [DONE] T007: Implement direct `System.Exception` public exception classes with one constructor each and stable diagnostic/error payload conventions.
- [DONE] T008: Implement the public static data classes for standards editions, universal tags, and encoding OIDs/IRIs using fields only.
- [DONE] T009: Implement public class shells with exactly one constructor and only their interface-declared public instance members, wiring internal implementations through composition.
- [DONE] T010: Define default resource limits for nesting, collection elements, tag/length octets, integer bits, text characters, open-type bytes, total allocation, and diagnostics, with explicit unlimited behavior only when requested.
- [DONE] T011: Implement reusable byte/text accumulators, cursor arithmetic, stable ordering helpers, resource counters, and BigInteger conversion helpers without unsafe code or external libraries.
- [DONE] T012: Implement a shared diagnostic bag with deterministic ordering, de-duplication, related locations, phase/category codes, warning escalation, and recovery limits.

### Lexer and concrete parser: X.680–X.683 common notation

- [DONE] T013: Implement the source cursor with BOM handling, Unicode scalar validation, CR/LF normalization, exact offsets, and resumable chunk boundaries.
- [DONE] T014: Implement whitespace and ASN.1 comment scanning, including line-terminated and paired-hyphen comments, comment adjacency, retained trivia, unterminated-comment diagnostics, and incremental continuation.
- [DONE] T015: Implement reserved words, typereferences, valuereferences, modulereferences, object-class references, object/object-set references, field references, encoding references, identifiers, and lexical case/category rules.
- [DONE] T016: Implement integer, real, exponent, binary-string, hex-string, quoted character-string, tuple, quadruple, and XML value lexical forms with separator and invalid-character diagnostics.
- [DONE] T017: Implement punctuation, assignment, ellipsis, range, version-group, at-notation, encoding-prefix, and every other X.680–X.683 lexical token, resolving longest-match ambiguities deterministically.
- [DONE] T018: Implement incremental lex sessions that resume every token kind across arbitrary UTF-8/text chunk splits and return NeedMoreData without losing trivia or locations.
- [DONE] T019: Implement token cursors, bounded lookahead, speculative checkpoints without exception control flow, expected-token diagnostics, and production-specific recovery sets.
- [DONE] T020: Parse module identifiers, definitive OID/IRI values, encoding references, tag defaults, extensibility defaults, bodies, and encoding-control sections.
- [DONE] T021: Parse EXPORTS/IMPORTS clauses, symbol lists of every assignment category, assigned module identifiers, version references, multiple source modules, and empty/all export forms.
- [DONE] T022: Parse type, value, value-set, object-class, object, and object-set assignment heads while retaining ambiguous assignment nodes for semantic classification.
- [DONE] T023: Parse BOOLEAN, INTEGER/named-number, ENUMERATED/root-extension, REAL, BIT STRING/named-bit, OCTET STRING, NULL, OID/relative-OID/IRI, and all defined/reference types.
- [DONE] T024: Parse restricted/unrestricted character string, ObjectDescriptor, UTF8String, UniversalString, BMPString, GeneralizedTime, UTCTime, TIME, DATE, TIME-OF-DAY, DATE-TIME, and DURATION types.
- [DONE] T025: Parse CHOICE, SEQUENCE, SET, SEQUENCE OF, SET OF, named types, optional/default components, COMPONENTS OF, extension markers, extension additions, versioned extension groups, and extension end markers.
- [DONE] T026: Parse tagged and generally prefixed types, tag classes/numbers, explicit/implicit modes, encoding instruction prefixes, selection types, INSTANCE OF, EXTERNAL, EMBEDDED PDV, and CHARACTER STRING.
- [DONE] T027: Parse all basic value notations for scalar, string, OID/IRI, named, structured, collection, CHOICE, open, EXTERNAL, EMBEDDED PDV, INSTANCE OF, useful/time, and defined values.
- [DONE] T028: Parse XML value notation for every applicable ASN.1 type, including markup boundaries, escaping, namespaces, empty forms, and lexical recovery back into ASN.1 source.
- [DONE] T029: Parse subtype constraints, parenthesized element-set specifications, union/intersection/EXCEPT/ALL EXCEPT, extension markers/additions, nested constraints, and exception specifications.
- [DONE] T030: Parse single-value, contained-subtype, value-range with endpoint inclusion/exclusion and MIN/MAX, SIZE, FROM, PATTERN, property SETTINGS, type constraints, and WITH COMPONENT(S) forms.
- [DONE] T031: Parse X.682 user-defined, simple-table, component-relation with at-notation, and contents CONTAINING/ENCODED BY constraints.
- [DONE] T032: Parse X.681 object classes and all field-spec kinds with UNIQUE, OPTIONAL, DEFAULT, governors, and WITH SYNTAX token/optional-group templates.
- [DONE] T033: Parse X.681 default/defined object syntax, every field-setting kind, object/object-set assignments, object-set algebra/extensions, and information-from-objects forms.
- [DONE] T034: Parse X.683 formal parameter lists, governors, dummy references, actual parameter lists, parameterized assignments/references, and parameterized value-set types for every assignment kind.
- [DONE] T035: Implement parser recovery at assignment, module, component, constraint, object syntax, parameter-list, and encoding-control boundaries while preserving partial syntax trees and all actionable diagnostics.
- [DONE] T036: Implement incremental parser sessions that retain production stacks explicitly and resume at every token boundary without local functions, iterators, async, or exception-driven control flow.
- [DONE] T037: Parse legacy ANY and ANY DEFINED BY only when compatibility mode is enabled, map them to explicit open-type syntax nodes, and diagnose other retired X.208 macro syntax without corrupting following modules.

### Binding, modules, tagging, values, and schema validation

- [DONE] T038: Build per-module symbol tables with separate namespaces/categories, export visibility, duplicate detection, qualified names, stable declaration order, and source links.
- [DONE] T039: Resolve imports by module name plus assigned OID/IRI/version, enforce export rules, detect missing/ambiguous symbols, and support multiple revisions of a named module.
- [DONE] T040: Build the inter-module dependency graph, identify legal recursive type cycles, reject illegal value/object/parameter cycles, and compile strongly connected type groups deterministically.
- [DONE] T041: Classify ambiguous assignments and references by governor/context across all X.680–X.683 categories, reporting kind conflicts with related declaration locations.
- [DONE] T042: Resolve defined types and selection types, expand COMPONENTS OF with extension/version provenance, and retain alias/source identity separately from effective type identity.
- [DONE] T043: Calculate universal/default/explicit/implicit tag chains through aliases and selections, reject illegal implicit tags, and preserve tag origin metadata.
- [DONE] T044: Synthesize AUTOMATIC TAGS across root and extension components, honor manually tagged boundaries, and validate duplicate/colliding tags for SET, CHOICE, and open alternatives.
- [DONE] T045: Evaluate named numbers/bits, integer arithmetic permitted by value notation, defined values, structured/default values, OID arcs, relative OIDs, IRIs, and cross-module constants using BigInteger where needed.
- [DONE] T046: Normalize REAL values across decimal, binary tuple, special-value, zero, and minus-zero forms while retaining enough information for canonical encoding decisions.
- [DONE] T047: Validate restricted string alphabets, Unicode scalar legality, tuple/quadruple forms, bit/hex unused positions, OID arc rules, IRI lexical rules, and useful/time lexical and calendar semantics.
- [DONE] T048: Normalize SEQUENCE/SET/CHOICE/collection values, apply defaults, distinguish absent from present-default, reject duplicates/unknown root fields, and retain permitted extension values.
- [DONE] T049: Validate extension marker placement, extension-addition group versions, extension end markers, ENUMERATED numbering, component optionality/default rules, and version-bracket consistency.
- [DONE] T050: Implement schema diagnostic codes and related locations for every X.680 module/type/value/tag/extensibility static-semantic violation.

### Constraints: X.680 and X.682

- [DONE] T051: Lower constraints into a normalized set-expression graph with explicit root/extension partitions and type-domain identity.
- [DONE] T052: Normalize and combine single values, ranges, contained subtypes, unions, intersections, exclusions, ALL EXCEPT, nested constraints, and sequential constraint application.
- [DONE] T053: Implement exact arbitrary-integer and enumerated range algebra, endpoint inclusion, MIN/MAX, disjoint intervals, emptiness, membership, and finite cardinality where computable.
- [DONE] T054: Implement size-domain algebra for bit/octet/character strings and collections, reject negative/impossible sizes, and expose finite lower/upper bounds to PER/OER/code generation.
- [DONE] T055: Implement permitted-alphabet algebra over Unicode scalar ranges and named character repertoires, including alphabet indexing metadata for PER.
- [DONE] T056: Implement PATTERN parsing/validation semantics required by X.680 without an external regex dependency, including repetitions, alternatives, groups, character ranges, anchors defined by the notation, and invalid-pattern diagnostics.
- [DONE] T057: Implement property SETTINGS parsing and semantic validation for TIME and DURATION, including basic kind, date/time precision, year forms, local/UTC/difference, midnight, recurrence, intervals, and fractional precision.
- [DONE] T058: Implement WITH COMPONENT and WITH COMPONENTS normalization, PRESENT/ABSENT/OPTIONAL presence rules, nested component constraints, partial/full specifications, and recursive validation.
- [DONE] T059: Implement type constraints, contents constraints, encoding references in ENCODED BY, containing-type compatibility, and effective inner codec metadata.
- [DONE] T060: Implement simple table constraints and component-relation constraints, validate governing object sets and at-notation paths, and produce open-type/table dispatch relationships.
- [DONE] T061: Preserve user-defined CONSTRAINED BY bodies, formal parameters, exception identifiers/values, and expose an explicit application-validation requirement when no executable meaning exists.
- [DONE] T062: Detect contradictory/empty constraints, invalid constraint application by type, non-value-preserving subtype relationships, and constraint cycles with precise source diagnostics.
- [DONE] T063: Implement the shared runtime constraint evaluator for every executable normalized constraint and a failure path that reports the violated constraint and nested value location.
- [DONE] T064: Compute encoding-effective constraints separately for BER, PER, OER, XER, contents, and type constraints so codecs never infer bounds from syntactically present but encoding-irrelevant constraints.

### Information object specification: X.681 and open types

- [DONE] T065: Bind object classes and validate every field kind, governor, UNIQUE requirement, optional/default setting, field-name category, and forward reference.
- [DONE] T066: Compile WITH SYNTAX templates into deterministic token grammars, validate literal/field tokens and optional groups, and diagnose ambiguous or unreachable object syntax.
- [DONE] T067: Bind default-syntax and defined-syntax object values to field settings, apply defaults, validate required fields, and type-check value/value-set/object/object-set settings.
- [DONE] T068: Evaluate object-set literals, references, unions, intersections, exclusions, ALL EXCEPT, extension markers, and parameterized operands with stable identity and duplicate UNIQUE-key detection.
- [DONE] T069: Implement type/value/value-set/object/object-set-from-objects resolution, chained field selection, class-field types, and their governing type/object-class checks.
- [DONE] T070: Materialize TYPE-IDENTIFIER, ABSTRACT-SYNTAX, and INSTANCE OF semantics and standard useful-class definitions without hidden source dependencies.
- [DONE] T071: Build simple-table and component-relation dispatch maps keyed by normalized UNIQUE values, including multi-level at-notation lookup and ambiguity/missing-key diagnostics.
- [DONE] T072: Represent extensible object sets as static roots plus dynamic additions, and implement thread-safe per-codec/per-catalog registration without mutable global state.
- [DONE] T073: Implement open-type runtime values carrying resolved type identity, decoded value, original encoded payload, encoding-rule identity, dispatch key, and unresolved status.
- [DONE] T074: Implement open-type encoding/decoding through information-object tables for BER, PER, OER, and XER, preserving unresolved payloads when policy permits.
- [DONE] T075: Validate all X.681 rules for field references, object class compatibility, UNIQUE fields, extension behavior, defined syntax, information extraction, and table constraints.

### Parameterization: X.683

- [DONE] T076: Bind formal parameters with governor/kind inference, scoped dummy references, shadowing rules, and declaration-order-independent lookup.
- [DONE] T077: Validate actual parameter arity, category, governor, subtype/object-class compatibility, and value/object-set constraints before substitution.
- [DONE] T078: Implement capture-avoiding substitution for types, values, value sets, object classes, objects, object sets, constraints, tags, field paths, and encoding instructions.
- [DONE] T079: Instantiate every parameterized assignment/reference form, including parameterized value-set types and nested actual parameter lists.
- [DONE] T080: Memoize instantiations by normalized actual arguments and source identity while retaining per-use source traces and deterministic stable IDs.
- [DONE] T081: Support legal recursive parameterized types through deferred fixups and reject non-terminating expansion, changing-governor recursion, and illegal value/object recursion.
- [DONE] T082: Integrate parameter instantiation with imports, COMPONENTS OF, automatic tags, constraint normalization, information objects, open-type tables, and encoding-control target resolution.
- [DONE] T083: Emit complete X.683 diagnostics for invalid governors, dummy-reference categories, missing/excess actuals, parameterized-reference misuse, cycles, and post-substitution invalid schemas.

### Intermediate representation, reflection, and generation

- [DONE] T084: Define the lowered encoding-neutral IR for modules, assignments, effective types/tags, fields, values/defaults, normalized constraints, extension partitions, object tables, and instructions.
- [DONE] T085: Assign deterministic stable IDs and topological emission order independent of input file order, dictionary iteration, locale, or process architecture.
- [DONE] T086: Preserve source spans and derivation chains from IR nodes back through aliases, COMPONENTS OF, parameter instantiations, automatic tags, XSD mappings, and encoding-control assignments.
- [DONE] T087: Implement deterministic IR serialization/deserialization for compiler/runtime handoff using a versioned built-in binary format, strict bounds, and unknown-version rejection.
- [DONE] T088: Build the public reflection catalog from IR, including modules/imports/exports, assignments, fields, tags, constraints, defaults, extensions, parameters, object tables, source locations, generated names, and codec plans.
- [DONE] T089: Implement qualified module/type/value resolution and traversal through `IAsnReflectionService` without CLR attributes, runtime code generation, or public static methods.
- [DONE] T090: Implement the normalized dynamic runtime value model and schema-directed conversion rules between data fields, choice selections, collections, open types, and descriptors.
- [DONE] T091: Implement deterministic C# namespace/type/member/file name mapping, keyword escaping without verbatim identifiers if disallowed, Unicode normalization, collision suffixes, and source-name metadata.
- [DONE] T092: Generate field-based public data classes with presence flags, choice discriminant enums, default initialization, extension/unknown containers, and types compatible with the allowed language subset.
- [DONE] T093: Generate constants/named values/static data, arbitrary-integer representations, OID/bit-string initializers, and module-level value containers without public static methods or forbidden member forms.
- [DONE] T094: Generate schema descriptors/reflection tables and initialization code without attributes, nested types, user-defined generics, properties, overloads, inheritance, LINQ, or nullable value types.
- [DONE] T095: Generate constraint validators and configured codec facade classes whose public members are interface-declared and whose method names remain unique.
- [DONE] T096: Generate correct cross-module references, recursive type initialization, parameter-instantiation sharing, object-table dispatch, imports, namespace mappings, and deterministic file partitions.
- [DONE] T097: Support model-only, descriptor-only, encode-only, decode-only, and combined C# generation targets without changing wire semantics or omitting reflection dependencies needed by the selected mode.
- [DONE] T098: Return all generated outputs as `AsnGeneratedFile` values with stable relative paths, content, source mappings, and collision diagnostics; file-system writes remain the caller's responsibility.
- [DONE] T099: Implement compile orchestration from source through parsing, binding, validation, IR, reflection, and optional generation, preserving partial results and deterministic diagnostics on failure.

### BER, DER, and CER: X.690

- [DONE] T100: Implement low-level identifier parsing/writing for all tag classes, primitive/constructed bit, short/high tag forms, BigInteger-range tag rejection, minimal-form checking, truncation, and configured octet limits.
- [DONE] T101: Implement definite short/long and indefinite length parsing/writing, EOC recognition, overflow/limit checks, minimal-form checks, and nested remaining-length accounting.
- [DONE] T102: Implement schema-less TLV event/tree reading and writing, exact encoded-slice retention, iterative nesting state, sibling traversal, and safe skip/select operations.
- [DONE] T103: Implement BER BOOLEAN, INTEGER, ENUMERATED, NULL, BIT STRING, OCTET STRING, OID, relative OID, OID-IRI, relative-OID-IRI, REAL, and all restricted/unrestricted character string primitives.
- [DONE] T104: Implement BER time/useful types, ObjectDescriptor, EXTERNAL, EMBEDDED PDV, CHARACTER STRING, INSTANCE OF, and open-type content with correct universal tags and component structures.
- [DONE] T105: Implement BER SEQUENCE, SET, CHOICE, SEQUENCE OF, SET OF, explicit/implicit tag chains, OPTIONAL/DEFAULT, extensions, constructed string segmentation, and indefinite-length recursion.
- [DONE] T106: Implement schema-aware BER decoding with tag-driven CHOICE/SET dispatch, ambiguity reporting, unknown-field policy, required/default validation, open-type tables, and trailing-data accounting.
- [DONE] T107: Implement schema-aware BER encoding from dynamic/generated values with constraint validation, selected definite/indefinite lengths, optional/default policy, and sink-based output.
- [DONE] T108: Preserve unknown BER extensions and open fields as exact TLV slices plus tag/location metadata and re-emit them in BER when requested without corrupting known field ordering.
- [DONE] T109: Implement DER canonical emission for minimal identifiers/lengths/integers/enumerated/OIDs/REAL, primitive strings, zero unused bits, normalized time, omitted defaults, definite lengths, and shortest permitted representations.
- [DONE] T110: Implement DER SET component tag order and SET OF full-encoding lexicographic order using bounded temporary storage and stable error reporting.
- [DONE] T111: Implement CER canonical emission with indefinite constructed forms, required 1000-unit string segmentation by type, primitive/constructed selection, EOC, canonical scalar forms, default omission, and set ordering.
- [DONE] T112: Implement strict DER and CER validators that report the first canonicality violation and byte offset directly from input, including ordering, segmentation, length, scalar, padding, and time violations.
- [DONE] T113: Implement BER-to-DER/CER canonical transcoding through schema-aware values and schema-less TLV where semantics permit, explicitly rejecting cases that require unavailable type information.
- [DONE] T114: Implement resumable BER/CER/DER decode and streaming encode state machines across every identifier, length, scalar content, constructed child, EOC, and canonical sort boundary.

### PER, UPER, APER, and canonical PER: X.691

- [DONE] T115: Implement MSB-first bit input/output with arbitrary chunk boundaries, exact bit counts, octet alignment, padding validation, bounded buffering, and no dependence on machine endianness.
- [DONE] T116: Implement constrained, semi-constrained, and unconstrained whole-number procedures; non-negative binary integers; two's-complement integers; normally-small non-negative whole numbers; and large-range handling with BigInteger.
- [DONE] T117: Implement aligned and unaligned length determinants, normally-small lengths, fixed/constrained sizes, fragmentation at standard boundaries, and exact reassembly/resource-limit enforcement.
- [DONE] T118: Derive PER-visible value, size, permitted-alphabet, and component constraints from normalized IR, including extensible roots and cases where constraints do not affect encoding.
- [DONE] T119: Implement PER BOOLEAN, INTEGER, ENUMERATED, NULL, REAL, OID/relative OID/IRI, BIT/OCTET STRING, character strings, and useful/time types for aligned and unaligned variants.
- [DONE] T120: Implement known-multiplier and unrestricted character strings, canonical character ordering, permitted-alphabet indexes, alignment thresholds, and fragmentation.
- [DONE] T121: Implement SEQUENCE/SET root presence maps, DEFAULT comparison, extension-present bit, extension addition/group bitmaps, open-type wrappers, and normally-small extension lengths.
- [DONE] T122: Implement CHOICE/ENUMERATED root and extension indexes, extension open types, SEQUENCE OF/SET OF lengths/fragments, and ordering requirements for canonical PER.
- [DONE] T123: Implement EXTERNAL, EMBEDDED PDV, CHARACTER STRING, INSTANCE OF, contents constraints, table-constrained open types, and unresolved open payload policy.
- [DONE] T124: Implement BASIC-PER aligned (APER) encode/decode with every mandated alignment point and zero-padding validation.
- [DONE] T125: Implement BASIC-PER unaligned (UPER) encode/decode with bit-contiguous fields and X.695 instruction hooks.
- [DONE] T126: Implement CANONICAL-PER aligned and unaligned restrictions, deterministic extension/default/set handling, canonical normally-small/length forms, and strict canonical input checks.
- [DONE] T127: Preserve unknown PER extension additions/groups as index, presence, exact open-type octets, and relevant padding metadata, and re-emit them without requiring a newer schema.
- [DONE] T128: Implement incremental PER decoding across every individual bit field, alignment boundary, determinant, fragment, bitmap, open type, nested value, and end-of-input completion call.
- [DONE] T129: Implement streaming PER encoding to byte sinks while bounding only fields that normatively require prior length or canonical ordering.
- [DONE] T130: Detect non-zero padding, invalid constrained offsets, forbidden extension indexes, malformed fragment sequences, truncated open types, impossible lengths, resource-limit violations, and trailing significant bits with exact bit offsets.

### OER and COER: X.696

- [DONE] T131: Implement OER length determinants with short/long forms, minimal COER checks, overflow/resource limits, and incremental continuation.
- [DONE] T132: Derive fixed-width signed/unsigned integer forms from effective constraints and implement variable-length two's-complement/non-negative encodings for unbounded cases.
- [DONE] T133: Implement OER BOOLEAN, INTEGER, ENUMERATED, REAL, NULL, BIT/OCTET STRING, OID/relative OID/IRI, character strings, and useful/time type encodings.
- [DONE] T134: Implement OER sequence/set preambles, OPTIONAL/DEFAULT bitmaps, root components, extension-present flag, extension addition/group bitmaps, and open-type wrapping.
- [DONE] T135: Implement OER CHOICE tags/indexes, SEQUENCE OF/SET OF lengths and elements, explicit contents constraints, EXTERNAL, EMBEDDED PDV, CHARACTER STRING, and INSTANCE OF.
- [DONE] T136: Implement object-table open-type dispatch and unresolved open-value handling for OER/COER.
- [DONE] T137: Implement BASIC-OER encode/decode with schema-directed width, length, preamble, extension, and value-validation behavior.
- [DONE] T138: Implement CANONICAL-OER emission and strict COER checks for integer/length minimality, preamble/default behavior, REAL/string forms, ordering, extension contents, and unused bits.
- [DONE] T139: Preserve unknown OER extensions with their addition/group index and exact open-type contents and re-emit them under compatible schemas.
- [DONE] T140: Implement resumable OER/COER decode and streaming encode across length, preamble, fixed/variable scalar, collection, extension bitmap, and open-type boundaries.
- [DONE] T141: Report malformed tags/choices, invalid preamble padding, non-canonical forms, truncated values, impossible constrained widths, resource violations, and trailing data with exact byte offsets.

### XER, canonical XER, and extended XER: X.693

- [DONE] T142: Implement an incremental XML 1.0 token reader/writer for the XER-required subset with Unicode/name validation, entity/character references, CDATA, comments, processing instructions, namespaces, escaping, and resource limits.
- [DONE] T143: Implement namespace context, prefix resolution, QName values, reserved-prefix rules, duplicate attributes, well-formed nesting, XML declaration handling, and deterministic prefix assignment.
- [DONE] T144: Implement BASIC-XER element-name derivation, wrappers, and scalar lexical encodings for BOOLEAN, INTEGER, ENUMERATED, REAL, NULL, strings, bits/octets, OIDs/IRIs, time/useful, and special values.
- [DONE] T145: Implement BASIC-XER SEQUENCE, SET, CHOICE, collections, optional/default components, extensions, EXTERNAL, EMBEDDED PDV, CHARACTER STRING, INSTANCE OF, and open types.
- [DONE] T146: Implement canonical XER emission and strict validation for whitespace, lexical forms, namespace/prefix use, attribute ordering, empty elements, default treatment, SET/SET OF ordering, and character escaping.
- [DONE] T147: Parse XER encoding-control sections, prefixes, targeted instruction lists, target paths, GLOBAL-DEFAULTS, namespace/control-namespace declarations, and positive/negative instructions.
- [DONE] T148: Resolve instruction precedence, inheritance, negation, target expansion, final-instruction combinations, legality by target type, and modified-encodings semantics.
- [DONE] T149: Implement ANY-ATTRIBUTES and ANY-ELEMENT wildcard formats, namespace constraints, raw XML retention, and validation.
- [DONE] T150: Implement ATTRIBUTE, ELEMENT, UNTAGGED, NAME, NAMESPACE, TEXT, and LIST instructions with interactions and collision/well-formedness checks.
- [DONE] T151: Implement BASE64, DECIMAL, USE-NUMBER, USE-QNAME, USE-TYPE, USE-UNION, and WHITESPACE instructions for applicable types and both encode/decode directions.
- [DONE] T152: Implement DEFAULT-FOR-EMPTY, EMBED-VALUES, USE-NIL, USE-ORDER, PI-OR-COMMENT, and their interactions with optional/default/choice/collection content.
- [DONE] T153: Implement EXTENDED-XER encode/decode by applying the complete final instruction set while retaining a separate BASIC/CANONICAL mode.
- [DONE] T154: Preserve unknown XER extension elements/attributes as namespace-aware token fragments and re-emit them without prefix capture or semantic alteration where policy permits.
- [DONE] T155: Implement resumable XER decoding and streaming encoding across UTF-8 scalar, entity, tag, attribute, namespace, text, list, wildcard fragment, and nested-value boundaries.
- [DONE] T156: Implement XER diagnostics for malformed XML, unexpected/duplicate/misordered elements, invalid lexical forms, namespace errors, illegal instructions/combinations/targets, unresolved open types, and resource limits.

### Encoding Control Notation: X.692

- [DONE] T157: Extend lexing/parsing for the complete X.692 ECN notation, including encoding definitions, class/object/object-set assignments, parameters, properties, links, paths, mappings, conditions, and ordered encoding object lists.
- [DONE] T158: Build ECN symbol tables and governors for encoding classes, encoding objects, encoding object sets, encoding structures, parameters, and imported ASN.1 types.
- [DONE] T159: Implement built-in encoding class categories and category groups for alternatives, concatenation, repetition, optionality, tags, booleans, bit strings, character strings, integers, null, object identifiers, octet strings, open types, padding, real values, and encoding structures.
- [DONE] T160: Implement encoding class assignment, class-field definitions, encoding structure definitions, field optionality/repetition/alternatives, and category compatibility checks.
- [DONE] T161: Implement encoding object assignment and settings, encoding object sets, set algebra, ordered lists, defaults, parameterized objects/classes, and `#ENCODINGS` selection.
- [DONE] T162: Implement ECN property parsing/resolution for representation, size, range, alphabet, alignment, padding, byte order, bit order, signedness, termination, and other standard primitive properties.
- [DONE] T163: Implement standard primitive bit-field codecs and property combinations for boolean, integer, real, bit/octet/character strings, OID, null, open type, tag, and padding categories.
- [DONE] T164: Implement concatenation, alternatives, repetition, optionality, tag, determinant, and conditional encoding structures with validated control dependencies.
- [DONE] T165: Implement encoding links, relative/absolute field paths, determinant relationships, presence/choice/length/value mappings, and link-cycle/directionality validation.
- [DONE] T166: Implement replacement structures, contained-value and open-type mappings, encoding transformations, and reversible abstract-value/encoding-value conversion validation.
- [DONE] T167: Implement the X.692 predeclared encoding classes/objects/object sets, including standard BER/PER encoding objects, with stable identities rather than reparsing hidden text.
- [DONE] T168: Lower validated ECN definitions into encoding-neutral executable plan steps with explicit bit/byte dependencies, bounds, buffer requirements, source traces, and reversibility metadata.
- [DONE] T169: Validate ECN plan determinism, decodability, field coverage, legal categories/properties, satisfiable conditions, determinant availability, recursion, bounds, and ambiguous alternatives.
- [DONE] T170: Execute ECN plans symmetrically for encode/decode over streaming bit I/O, pausing on partial determinants, fields, repetitions, alternatives, replacements, and nested standard encodings.
- [DONE] T171: Integrate ECN plans with reflection, generated code, normalized constraints/values, open types, unknown-data policy, resource limits, and compiler diagnostics.
- [DONE] T172: Report every ECN syntax/static-semantic/execution error with the ECN source span, affected ASN.1 type/field path, plan step, bit offset, and related declaration locations.

### XML Schema mapping: X.694

- [DONE] T173: Implement bounded XML Schema 1.0 document loading with XML/base URI, target namespace, namespace prefixes, schema locations, and deterministic resolver inputs supplied by `AsnXsdInput` rather than ambient network access.
- [DONE] T174: Resolve XSD include, import, and redefine graphs; chameleon schemas; duplicate components; cycles; namespace mismatches; and stable document/component identities.
- [DONE] T175: Implement the X.694 XSD built-in type mapping and the XSDv2 ASN.1 support module for string, numeric, boolean, binary, QName/URI, name/token, and date/time/duration families.
- [DONE] T176: Map simple-type restrictions and length/digit/bounds/enumeration/pattern/whitespace facets into ASN.1 types, values, constraints, and XER instructions with XSD value-space fidelity.
- [DONE] T177: Map simple list and union types, member/item references, anonymous types, defaults/fixed values, and QName-valued data with required USE-UNION/LIST/USE-QNAME behavior.
- [DONE] T178: Map global/local element and attribute declarations, qualified/unqualified forms, occurrence ranges, nillable/abstract/default/fixed/block/final flags, and anonymous types.
- [DONE] T179: Map sequence, choice, and all particles; element/attribute groups; attribute groups; complex/simple content; extension/restriction derivation; and occurrence nesting into ASN.1 components/constraints.
- [DONE] T180: Map mixed content, substitution groups, `xsi:type`, `xsi:nil`, wildcards/any/anyAttribute, namespace constraints, and processContents into X.694-prescribed structures and XER instructions.
- [DONE] T181: Generate deterministic ASN.1 module names/identifiers/imports, type/value names, collision resolutions, AUTOMATIC TAGS, constraints, XSD support references, and encoding-control sections.
- [DONE] T182: Preserve source mapping from generated ASN.1/IR back to XSD documents/components and emit precise diagnostics for invalid or X.694-unmappable XSD constructs.
- [DONE] T183: Feed mapped modules through the normal binder/validator/IR/code-generator pipeline and ensure XER round trips reproduce the XSD-defined XML information within X.694 guarantees.

### PER encoding instructions: X.695

- [DONE] T184: Parse PER type-prefix instruction details and PER encoding-control assignment lists, including target lists by type reference, built-in type, component identifier/path, ALL forms, and identifiers in context.
- [DONE] T185: Implement instruction applicability, positive/negating assignment, target specificity, assignment order, multiple-assignment combination, inheritance through references, and final-instruction resolution.
- [DONE] T186: Implement X.695 restrictions: instructions affect only unaligned PER, unknown identifying keywords produce not-supported errors in UPER, APER silently ignores them, and contents/type constraints retain final instructions while other constraints discard them as specified.
- [DONE] T187: Implement the application-extensible PER instruction registry and handler contract with duplicate-key rejection, immutable snapshot use during compilation, deterministic validation, and no global mutable registry.
- [DONE] T188: Carry final PER instructions through IR/reflection/code generation into UPER lowering and report handler applicability/conflict/non-reversibility failures at compile time.

### Streaming, incremental operation, preservation, and integration

- [DONE] T189: Implement `AsnCodecProvider` selection for BER, DER, CER, APER, UPER, canonical aligned/unaligned PER, OER, COER, BASIC-XER, CANONICAL-XER, EXTENDED-XER, and ECN plans.
- [DONE] T190: Implement one-shot encode/decode APIs over byte/text buffers with exact consumed/written counts, schema/type lookup, value validation controls, unknown policy, canonical mode, and resource limits.
- [DONE] T191: Implement `Stream` encode/decode adapters that do not assume seekability, short-read behavior, known total length, or a single write acceptance, and propagate I/O exceptions consistently.
- [DONE] T192: Implement the public incremental decoder as a rule-selected explicit state machine with Consume/Complete/Reset behavior, zero-length chunks, multiple values, leftover input, terminal-state rules, and reusable resource accounting.
- [DONE] T193: Implement selective decode/skip by reflected field path for BER, PER, OER, XER, and ECN where their structure permits, preserving skipped open/extension data and validating framing/canonicality.
- [DONE] T194: Implement uniform unknown-extension/open-field preservation containers and per-rule re-emission policies, preventing cross-rule reuse of opaque payloads unless explicitly decoded and re-encoded.
- [DONE] T195: Implement canonicality checking as a first-class non-throwing report for DER, CER, canonical aligned/unaligned PER, COER, and canonical XER, with rule, offset, path, diagnostic code, and repairability status.
- [DONE] T196: Implement transcode orchestration between supported rules through normalized runtime values, retaining unknown data when representable and diagnosing lossy/unresolved transfers before output mutation.
- [DONE] T197: Make compiled schemas, reflection catalogs, codec plans, and instruction-registry snapshots safe for concurrent read/use; isolate mutable incremental/stream state per operation.
- [DONE] T198: Audit all decoders for depth/length/integer/allocation amplification, indefinite nesting, fragmented lengths, XML entities, object-table fanout, and diagnostic floods, enforcing limits before allocation or recursion.

### Complete test coverage of `src/Api` functionality

- [DONE] T199: Create a public-API coverage matrix that maps every public interface method, implementation class, data field invariant, enum value, static field, and exception failure mode to executable tests, and fail review if any API behavior is unmapped.
- [DONE] T200: Test construction and use of every public data/result/options class, including presence flags, null reference handling, invalid combinations, limit boundaries, unknown-data containers, and round-trip preservation of public fields.
- [DONE] T201: Test every public implementation through its interface rather than internal entry points, including constructor validation, method success, each documented failure category, repeated calls, and concurrent independent use.
- [DONE] T202: Add lexer conformance tests for every token/trivia/literal/reference category, Unicode and BOM cases, malformed forms, exact spans, recovery, and every possible split point in incremental tokenization.
- [DONE] T203: Add parser conformance tests for every X.680–X.683 production and alternative, ambiguous assignment resolution inputs, malformed recovery boundaries, XML value notation, legacy compatibility gates, and every incremental split point.
- [DONE] T204: Compile all normative example modules extractable from X.680–X.683 and verify expected successes/diagnostics, normalized tags, constraints, parameter instances, object sets, and values.
- [DONE] T205: Compile RFC 5911, RFC 5912, and RFC 6268 modules as multi-module corpora to cover imports, parameterization, information object classes, WITH SYNTAX, table/component-relation constraints, and open types.
- [DONE] T206: Add module/import tests for OID/IRI/version identity, exports, missing/ambiguous/cyclic imports, same-name revisions, deterministic order, and cross-module recursive types.
- [DONE] T207: Add type/value/tag/extensibility tests covering every built-in/useful type, value notation, automatic/implicit/explicit tagging case, COMPONENTS OF, selection, extension group/version, and invalid collision.
- [DONE] T208: Add constraint tests for every set operator and constraint kind, root/extension partition, range/size/alphabet boundary, empty/contradictory set, pattern/settings form, inner presence rule, exception spec, and runtime failure path.
- [DONE] T209: Add information-object/open-type tests for every field kind, default/defined syntax, optional groups, set algebra, useful class, information-from-objects form, static/dynamic table entry, dispatch ambiguity, and unresolved preservation policy.
- [DONE] T210: Add parameterization tests for every formal/actual category, governor inference, nested and recursive instantiation, memoization identity, imports, substitution capture avoidance, and every category/arity/cycle failure.
- [DONE] T211: Add IR/reflection tests proving deterministic stable IDs/serialization, complete source traces, all descriptor relationships, qualified queries, extension/object/instruction metadata, and rejection of incompatible IR versions.
- [DONE] T212: Add code-generation golden tests for all model shapes and options, then compile generated multi-module C# in test-only temporary projects and exercise generated values, validators, reflection descriptors, and codec facades end to end.
- [DONE] T213: Add generation tests for hostile identifiers, Unicode normalization, keywords, filesystem collisions, deterministic file/content ordering, recursive types, all API organization categories, and each model/descriptor/encode/decode target combination.
- [DONE] T214: Add schema-less BER TLV tests for every tag/length form, definite/indefinite nesting, EOC, constructed strings, skip/event/tree modes, exact slices, malformed/truncated inputs, limits, and all incremental byte splits.
- [DONE] T215: Add BER schema-aware vectors for every ASN.1 type, tag mode, optional/default/extension case, set/choice dispatch, open type, constructed alternative, indefinite length, stream short read/write, and unknown TLV retention/re-emission.
- [DONE] T216: Add DER tests from RFC 5280/RFC 8017 certificates and key structures plus generated edge vectors, covering every canonical scalar/length/tag/time/default/padding/order rule and first-violation offsets.
- [DONE] T217: Add CER vectors for primitive/constructed thresholds at 999/1000/1001 units, every string type, indefinite/EOC rules, set ordering, malformed segmentation, canonical checking, and round trips.
- [DONE] T218: Cross-check BER/DER behavior against test-only Bouncy Castle APIs and independently produced PKIX/CMS fixtures, including schema-less parsing and canonical DER bytes.
- [DONE] T219: Add UPER and APER bit-exact vectors for every primitive/constructed type, constrained/semi/unconstrained integer boundary, alignment threshold, length/fragment size, alphabet, presence/extension bitmap, choice/enum extension, open type, and each input bit split.
- [DONE] T220: Add canonical aligned/unaligned PER tests for deterministic output, canonical restrictions, non-zero padding, malformed fragmentation, unknown extensions, object-table dispatch, and strict canonical reports.
- [DONE] T221: Cross-check PER vectors against at least two independent test-only implementations or published standards vectors, recording the schema, abstract value, exact bit length, aligned bytes, and unaligned bytes for reproducibility.
- [DONE] T222: Add OER/COER vectors for every integer width/boundary, length form, primitive/constructed type, preamble/default/extension map, choice tag, open type, unknown extension, canonical restriction, stream behavior, and byte split.
- [DONE] T223: Cross-check OER/COER fixtures against an independent test-only implementation and verify canonical/non-canonical paired inputs produce equal abstract values but distinct canonicality results.
- [DONE] T224: Add BASIC-XER/CANONICAL-XER tests for every ASN.1 type and lexical form, namespace and escaping case, structures/collections/extensions/open types, default/empty handling, ordering, malformed XML, and every UTF-8/token split.
- [DONE] T225: Add EXTENDED-XER tests for every positive and negated encoding instruction, every valid/invalid instruction combination, target precedence, GLOBAL-DEFAULTS, namespace/QName, wildcard, mixed/text/list/union/nil/type behavior, and unknown XML retention.
- [DONE] T226: Validate XER output with built-in XML readers and independent XML fixtures while ensuring decoder limits reject entity, depth, text, attribute, and namespace amplification cases.
- [DONE] T227: Add ECN parser/semantic tests for every X.692 production, class category, object/object-set/parameter form, property, encoding structure, link/path/mapping, condition, replacement, transformation, and prescribed invalid case.
- [DONE] T228: Execute all usable X.692 normative examples and test each lowered plan in both directions, at every bit/chunk split, with determinant/link cycles, ambiguous alternatives, non-reversible mappings, padding/alignment, and resource failures.
- [DONE] T229: Add XSD mapping tests for every X.694 mapping clause using XSD documents that cover built-ins, facets, lists/unions, particles, derivation, declarations/groups, wildcards, substitution, nil/type, mixed content, namespaces, import/include/redefine, and unmappable diagnostics.
- [DONE] T230: Map and compile the downloaded XSDv2 module/reference examples, then round-trip representative XML instances through XER and compare XML information and schema validity according to X.694 guarantees.
- [DONE] T231: Add X.695 tests for prefixed/targeted instructions, every target-list form, positive/negated precedence, multiple assignments, contents/type constraint retention, other-constraint discard, UPER handler application, unknown-key errors, and APER silent ignore.
- [DONE] T232: Add streaming tests using non-seekable short-read/short-write streams, zero-byte reads, one-byte chunks, randomized deterministic chunking, truncation at every boundary, multiple concatenated values, completion/reset, and injected I/O failures for every rule family.
- [DONE] T233: Add unknown-extension preservation tests that decode with newer schemas and older schemas in both directions for BER, PER, OER, and XER, verifying exact same-rule re-emission and explicit rejection of unsafe cross-rule opaque transfer.
- [DONE] T234: Add canonicality API tests for every canonical family, checking valid, repairable invalid, and semantically invalid statuses; exact offsets/paths; first-error stability; and agreement with canonical encoder output.
- [DONE] T235: Add resource-limit and adversarial tests for high tags, huge/indefinite lengths, deeply nested structures, giant integers/OIDs, fragmented PER, extension bitmaps, XML entities/namespaces/text, ECN repetitions, diagnostic floods, and allocation-before-limit regressions.
- [DONE] T236: Add randomized grammar-derived schema/value tests with fixed seeds for parse-print/reparse equivalence, encode/decode equality across all applicable rules, canonical idempotence, and malformed-input mutation.
- [DONE] T237: Add an end-to-end test that imports a multi-module schema, validates and reflects it, generates and compiles C#, encodes with generated code, incrementally decodes with the dynamic runtime under every compatible rule, validates constraints, and preserves unknown extensions.
- [DONE] T238: Require complete line and branch coverage for functionality reachable through `src/Api`, review all exclusions as unreachable generated/compiler plumbing, and close every remaining public-API coverage-matrix gap with focused behavior tests.
