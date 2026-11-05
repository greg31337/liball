# Universal ISO 20022 library development plan

## Goal and completion definition

Build a dependency-free-under-`src/` .NET 10 library that can ingest the official ISO 20022 repository artifacts, represent every repository concept without limiting the implementation to payments, identify and process every published message namespace and version, generate strongly typed C# models and schemas, parse and write XML losslessly where required, validate syntax and semantics, compare releases, and assist migrations. “Universal” means all business areas, message definitions, current and archived versions, registered variants, `head`, `nvlp`, `supl`, and system messages present in imported artifacts; it does not mean that the library itself transports messages over a network or runs a payment-processing workflow.

Completion requires:

- Deterministic support for the official repository snapshot selected by the caller, rather than a hard-coded annual release.
- Generic processing for unknown/future message definitions plus generated strongly typed processing for imported definitions.
- Secure XML defaults, bounded resource use, offline schema resolution, and no implicit network access during parsing or validation.
- Exact preservation of supported wildcard and supplementary XML content, namespace identity, decimal/date-time lexical meaning, element order, and message version.
- XSD, repository-constraint, data-type-facet, internal/external-code-set, profile, and caller-supplied semantic validation.
- Deterministic repository/XSD/JSON Schema/C# generation and reproducible comparison/migration output.
- Complete tests for every behavior exposed through `src/Api`, including success, expected-failure, boundary, malformed-input, unknown-content, and round-trip cases.
- No external package reference in `src/iso20022.csproj`; only the .NET base/class libraries may be used by production code. Test-only packages remain permitted.

## Standards baseline and downloaded inputs

The implementation must record the exact edition/artifact date it imported. The current planning baseline is 27 August 2026:

| Source | Current baseline | Relevance |
| --- | --- | --- |
| [ISO 20022-1](https://www.iso.org/standard/20022-1) | 2026, metamodel | Repository concepts, identity, registration, business/message model |
| [ISO 20022-2](https://www.iso.org/standard/55006.html) | 2013, UML profile | Concrete UML representation; import/export metadata only |
| [ISO 20022-3](https://www.iso.org/standard/20022-3) | 2026, modelling | Modelling workflow and consistency requirements |
| [ISO 20022-4](https://www.iso.org/standard/20022-4) | 2026, XML Schema generation | Deterministic logical-model-to-XSD rules |
| [ISO 20022-5](https://www.iso.org/standard/20022-5) | 2026, conceptual interoperability/reverse engineering | Comparison, convergence, mapping, and migration |
| [ISO 20022-6](https://www.iso.org/standard/57341.html) | 2013, transport characteristics | Transport requirement metadata, not wire transport implementation |
| [ISO 20022-7](https://www.iso.org/standard/20022-7) | 2026, registration | Registration status/provenance and repository lifecycle |
| [ISO 20022-8](https://www.iso.org/standard/20022-8) | 2026, ASN.1 generation | Preserve syntax-neutral model and ASN.1 scheme metadata; ASN.1 encoding is a later optional module, not part of the XML deliverable |
| [ISO 20022-9](https://www.iso.org/standard/20022-9) | 2026, syntax-generation requirements | Common generation abstraction and conformance |
| ISO 20022 e-Repository | 26 June 2026 snapshot | Full current/previous repository in a 2013-Ecore XMI document |
| External code sets | May 2026 v1 | Independently versioned quarterly code values in JSON/XSD/XLSX |
| W3C XML stack | XML 1.0, Namespaces 1.0, XSD 1.0, XPath 2.0, C14N 1.1, XML Signature 1.1 | XML processing, validation, supplementary locations, canonical hashes, signatures |
| IETF RFCs | RFC 3339, 3986, 4648, 8141, 8259 | Time, URI/URN, base64, and JSON lexical behavior |

The public artifacts and comparison material are stored under `specs/`; `specs/SOURCES.md` records provenance. The paid ISO standards themselves are not copied. A notable compatibility fact is that the June 2026 e-Repository is still serialized against the public ISO 20022:2013 Ecore metamodel. Importers therefore need explicit metamodel-version adapters and preservation of unrecognized content rather than assuming the publication year and serialization metamodel year match.

## Existing-library research and resulting requirements

| Product | Observed strengths | Observed boundary/gap | Requirement adopted here |
| --- | --- | --- | --- |
| [Prowide ISO 20022](https://github.com/prowide/prowide-iso20022) | Broad generated Java model across categories/versions; known and unknown message APIs; AppHdr/envelope handling; XML/JSON conversion; message metadata; namespace normalization; wildcard preservation; configurable writing; checksums | Full XSD, market-practice, cross-element, directory, and custom validation is primarily in commercial Integrator | Match broad/versioned models and generic processing; include XSD, external-code, profile-overlay, cross-element, and custom semantic validation in the core design |
| [iso20022.js](https://github.com/svapnil/iso20022.js) | Type-safe, task-oriented APIs for creating ACH/SEPA/SWIFT/RTP payment messages and ingesting CAMT; XML/JSON conversion; simple business validation | Focused subset of messages/rails rather than the repository; abstractions sometimes hide exact version/schema detail | Add optional high-level builders/projectors without weakening exact identifier, namespace, raw-model, or all-domain support |
| [moov-io/iso20022](https://github.com/moov-io/iso20022) | Generated Go models, XML/JSON conversion, validation/CLI/service patterns | Archived in 2023; coverage and generator maintenance are limited | Make generation reproducible from pinned official artifacts and keep CLI/service concerns outside the core API |
| [IBM Financial Transaction Manager](https://www.ibm.com/products/financial-transaction-manager) | Canonical ISO-based internal model, external-to-canonical mappings, validation, orchestration, lifecycle/correlation, operational metadata | Enterprise product scope includes persistence, monitoring, workflow, network, and deployment concerns | Include canonical mapping, correlation metadata, diagnostics, and migration plans; exclude persistence engine, UI, transport adapters, workflow runtime, and network connectivity |

Additional behavior taken from current Prowide change history because it represents real interoperability failures: preserve `xsd:any`; accept configured non-standard wrappers without silently changing content; keep date-time fractional precision and offset semantics; distinguish AppHdr/Document order and sibling-file layouts; extract stable business metadata such as UETR; support namespace-prefix control; and canonicalize before duplicate hashes.

## Architecture and language constraints

- All production C# must obey `syntax.md`. Existing code is not precedent.
- Public data is mutable field-based classes under `src/Api/Data`; it contains no public methods and implements no interfaces.
- Public operations are non-inheriting interfaces under `src/Api/Contracts`. Method names are unique within each interface and across all interfaces implemented by one class; distinct input forms use distinct names rather than overloads.
- Every public implementation is under `src/Api/Classes`, implements at least one interface, has at most one constructor, and exposes only its interface methods.
- Public static types under `src/Api/Static` contain fields only. Closed enumerations are under `src/Api/Enums` with explicit `int` values.
- Every exception directly derives from `System.Exception`; exception categories are not arranged in a class hierarchy.
- Public value types that can be absent use reference wrapper data classes (`IsoDateValue?`, `IsoDecimalValue?`, and similar), because nullable value-type syntax is prohibited. `decimal` is used for ISO decimal/amount semantics; signed integers are used for counts and sizes.
- Closed repository code sets may generate enums. External code sets remain string-valued catalog data because their values change independently of message versions.
- Generated C# uses file-scoped namespaces, fields, a maximum of one constructor, ordinary block methods only where a public implementation is required, and no attributes/properties/records/structs/generic declarations/LINQ/ref/out/async/overloads or other prohibited constructs.
- Because attributes are unavailable, generated XML binding uses generated internal metadata tables and explicit codecs, not `XmlSerializer` annotations or reflection-heavy convention binding.
- Span and memory types are used internally for identifier, UTF-8, numeric, base64, and lexical parsing where they simplify bounded zero-copy processing. Public stream APIs remain usable without caller-owned buffer lifetime hazards.
- Production dependencies are limited to built-in namespaces such as `System.Xml`, `System.Xml.Schema`, `System.Text.Json`, `System.IO.Compression`, `System.Security.Cryptography`, and `System.Net.Http`.

## Complete feature catalogue, low level to high level

### 1. Primitive and lexical layer

- ISO primitive wrappers for text, identifier, decimal, amount/currency, quantity, rate, boolean, binary, date, time, date-time, year, month, day, year-month, month-day, duration, URI/URN, and code values.
- Presence separate from value for optional value-type concepts; no sentinel dates, amounts, or booleans.
- Culture-invariant parsing/formatting with exact decimal scale, signed-zero policy, fractional-second precision, timezone-present/absent distinction, UTC `Z` preservation policy, and Gregorian partial-date support.
- XSD facets: length/min/max length, pattern, enumeration, whitespace, inclusive/exclusive bounds, total/fraction digits, and binary length.
- Base64 and hexadecimal binary lexical support with bounded decoding.
- Stable qualified names, namespace declarations, XML paths, source locations, and diagnostic paths.
- Configurable limits for depth, attributes, text length, element count, collection count, total bytes, schema count, and diagnostic count.

### 2. Identity, namespace, and version layer

- Strict and exception-less parsing of message identifiers such as `pacs.008.001.10`, including four-part validation and leading-zero preservation.
- Namespace URN parsing/building for message, header, envelope, supplementary, and repository namespaces.
- Prefix-independent identity, configurable prefix emission, default-namespace handling, and preservation of unknown declarations.
- Message identification from namespace, `Document`/`AppHdr`, BME/BFE wrappers, configured custom wrappers, and supplied business-service/profile context.
- Detection of conflicting namespace/body/header identifiers and deterministic diagnostic reporting.
- Exact/current/latest-compatible/version-range selectors without silently upgrading a parsed message.
- Catalogue indexes by identifier, namespace, business area, functionality, variant, version, name, message set, registration status, and artifact edition.
- Namespace alias/legacy normalization is opt-in and records every repair.

### 3. Generic XML and message-document layer

- Secure `XmlReaderSettings`: DTD prohibited, external resolution disabled, comments/processing-instruction policy explicit, and caller limits enforced.
- Generic ordered XML node tree containing qualified name, attributes, namespace declarations, text, child order, nil state, and source location.
- Raw-fragment capture for `xsd:any`, signatures, supplementary envelopes, and unknown future content with namespace closure sufficient for round-trip output.
- Known typed body plus generic fallback in one `MessageDocument`; no data loss when generated support is unavailable.
- Standalone `Document`, standalone `AppHdr`, AppHdr+Document sibling layouts, standard BME, BFE multi-message files, and configurable external wrappers.
- UTF-8 bytes, `Stream`, and `TextReader` inputs; `Stream`, `TextWriter`, buffer-writer, and string outputs with separately named methods.
- Strict and lenient modes, where lenient repairs are enumerated and never applied silently.
- XML declaration, BOM, comments, whitespace, namespace prefixes, empty-element style, and lexical-value preservation options, with canonical output as a separate mode.
- Partial path extraction and metadata-only scanning without materializing the body.

### 4. ISO 20022 repository model

- Repository snapshot, Data Dictionary, and Business Process Catalogue identity/provenance.
- Registration metadata, examples, doclets, constraints, semantic markup, synonyms, removal/deprecation/replacement information, and unknown extension data.
- Business areas, processes, roles, participants, transactions, conversations, choreography, transmissions, endpoints, broadcast lists, transport systems, transport modes, and transport characteristics.
- Business components, attributes, association ends, elements, super/subtype references, derivation links, and semantic domains represented without C# class inheritance.
- Message sets, message definitions, identifiers, building blocks, message components, attributes, association ends, choices/XOR, multiplicity, derivations, and root element/namespace data.
- Data types for text/string, identifier, boolean/indicator, decimal, amount, quantity, rate, binary, date/time family, duration, code sets/codes, user-defined/logical/schema types, syntax, encoding, external schema, and syntax message scheme.
- Convergence documentation and reverse-engineering links for concept/message mapping.
- Stable XMI ID graph with forward references, unresolved-reference diagnostics, duplicate-ID detection, and forward-compatible unknown Ecore types/attributes/elements.
- Immutable-by-convention snapshot publication after import, deterministic ordering, indexes, and structural integrity validation.

### 5. Repository and artifact import

- Streaming two-pass import of the official 100+ MB XMI/e-Repository: first pass records identities/lightweight offsets, second resolves objects and IDREF lists without loading an XML DOM.
- Ecore metamodel discovery and adapter selection, initially ISO 20022:2013 Ecore with explicit failure or preservation for newer metamodel constructs.
- Import from ZIP or raw `.iso20022`; ZIP-slip protection, entry-count/size limits, and exact-entry selection.
- XSD import sufficient to create message/data-type metadata when a full repository is unavailable.
- External code-set import from official JSON, XSD, and XLSX (ZIP/XML) forms, with equivalence checking between formats.
- BAH, BME, BFE, supplementary-extension, registered-variant, MDR metadata, and example artifact indexing.
- Artifact manifest containing origin URI, retrieval/publication time, hash, media type, edition, license notice, and approval/draft status.
- Optional catalogue download using `HttpClient`, conditional requests, caller-selected local cache, checksums, atomic replacement, cancellation, and no implicit download from parsing/validation.
- Draft versus approved/current/archive separation and reproducible pinning.

### 6. Schema and code-set services

- Local schema registry keyed by namespace/message identifier/version/profile, with collision detection and explicit precedence.
- Secure XSD compilation using local-only resolution; include/import closure, schema diagnostics, and compiled-set caching.
- Validation against exact official message XSD, BAH/BME/BFE schemas, and supplementary extension schemas.
- Internal code-set lookup from repository definitions and external code-set lookup with active/inactive status, validity date, definition, origin, and quarterly edition.
- “Latest external codes” and “pinned external codes” modes; validation reports which edition was used.
- ISO currency/country/BIC/IBAN/reference-directory hooks as caller-provided code sets or semantic rules rather than embedded stale proprietary data.

### 7. Strongly typed generated models

- Generated public field-only data classes for every imported message definition and reusable message component.
- Generated public enums for closed internal code sets with stable explicit integer assignments and XML-code lookup tables; invalid enum values never serialize silently.
- External codes represented as strings/value objects validated through the selected external catalogue.
- Generated choice discriminators, occurrence collections, required/optional wrappers, wildcard containers, and supplementary-data fields.
- Generated internal readers/writers/metadata tables for each model without C# serialization attributes, reflection discovery, or public static methods.
- Generated message registry maps exact identifiers and namespaces to model factories/codecs; generic fallback remains available.
- Domain/version partitioning avoids one monolithic source file while retaining shared component identity where safe.
- Deterministic naming/escaping for C# keywords, collisions, Unicode names, long names, and same-name/different-shape components.
- Generation manifest maps every repository XMI ID and XML name to its C# symbol and output hash.

### 8. XML parsing and writing

- Exact-version strongly typed parse, generic parse, auto-detect parse, header-only parse, metadata-only parse, and exception-less result APIs.
- Schema-valid order and multiplicity enforcement during typed reading, with configurable continue-after-error behavior.
- Typed and generic writing, stable namespace assignment, element ordering, choice enforcement, nil/absent distinction, escaping, and UTF-8 output.
- Writer modes for compact, indented, lexical-preserving, and canonicalized output.
- Round-trip preservation for wildcard elements, attributes, prefixes, comments (when enabled), and unsupported extensions.
- Standard construction of standalone messages, AppHdr+Document business messages, BME, and BFE batches.
- Writer refuses version/model mismatch, invalid choices, duplicate singleton elements, unsupported enum values, or missing required fields unless explicitly writing diagnostic/partial output.
- XML-to-JSON and JSON-to-XML according to the ISO 20022 2025 JSON Schema transformation whitepaper, with versioned JSON namespace/schema identity and loss diagnostics.

### 9. Validation pipeline

- Staged validation: secure/well-formed XML, envelope/header coherence, exact XSD, generated structural model, primitive facets, internal codes, external codes, repository constraints, profile overlay, supplementary schemas, signature, and semantic rules.
- Validation usable on raw XML, generic documents, and strongly typed models, producing one stable report shape.
- Diagnostics contain severity, stage, stable rule ID, message, qualified path, line/column when known, offending value (redaction-aware), expected constraint, repository/profile provenance, and repair hint.
- Fail-fast and collect modes; bounded diagnostic count; exception-less expected-invalid path plus exception convenience API.
- Semantic rule interface, rule-set composition, applicability by identifier/version/profile/path, deterministic ordering, and caller state through a data context rather than delegates.
- Built-in generic cross-field rules for control sums/counts, currency/amount consistency, date ordering, unique references, header/body definition match, duplicate business message IDs, and supplementary locations.
- Profile/market-practice overlays add/remove/strengthen cardinalities, values, paths, and semantic rules without mutating the base ISO definition; overlay conflicts are diagnosed.
- Validation policy records approved/draft allowance, external code edition, unknown-content policy, timezone policy, and signature policy.

### 10. Supplementary data, signatures, and integrity

- `SupplementaryData` location and envelope preservation for global and local extensions, including multiple/repeating targets.
- XPath 2.0 location syntax validation and a documented supported evaluation subset implemented without external packages; unsupported expressions are reported distinctly.
- Namespace-qualified extension payloads, registered extension lookup, can-ignore/must-understand policy supplied by profile, and optional schema validation when the extension schema is available.
- XML Signature discovery, reference/digest/signature verification, certificate result reporting, and caller-supplied trust policy using built-in cryptography/XML support.
- Signing with caller-provided key/certificate and explicit canonicalization/digest/signature algorithms; reject unsafe/deprecated algorithms by default.
- Canonical whole-message/body hashes, duplicate keys, and equality modes (byte, canonical XML, semantic model).
- Redaction-aware diagnostic and metadata extraction so account, party, remittance, and signature material is not exposed unintentionally.

### 11. Comparison, mapping, and migration

- Repository snapshot diff by stable XMI identity plus semantic fallback matching when IDs change.
- Message-version diff for added/removed/renamed/moved/type-changed/cardinality-changed/facet-changed/code-changed/constraint-changed content.
- Compatibility classification for reader, writer, schema, data-loss, and semantic impact; confidence and rationale included.
- Generated migration plan with copy, rename, move, convert, default, split, merge, code-map, drop, and custom-rule steps.
- Explicit, inspectable mapping definitions between message versions, ISO messages, and caller canonical models; no opaque reflection mapping.
- Migration execution retains unmapped data in a report, supports supplementary-data fallback only when authorized, and validates the target.
- Three-way comparison for local profile versus two ISO releases and overlay rebase conflict reporting.
- Convergence tables and repository derivation links usable for conceptual/business-component mapping.

### 12. Streaming, batch, and high-level workflows

- Pull-based streaming reader returning one BFE/business message at a time; skipped/invalid-item policy and per-item diagnostics.
- Streaming writer for BFE and caller-defined wrappers with header/count/control-sum finalization strategy that does not require holding every message model.
- Large repeating-block reader exposing selected transaction/entry nodes one at a time while retaining message context.
- Backpressure is naturally caller-controlled through synchronous pull APIs; cancellation is accepted by long-running repository/network operations where permitted by syntax.
- Metadata extraction for identifier, namespace, sender, receiver, business message ID, business service/profile, creation time, possible duplicate, priority, UETR, primary amount/currency, value date, and references.
- Task-oriented builders/projectors for common payment initiation/status, interbank transfer, cash-management statement/notification, securities, cards, FX, and trade messages are generated from semantic templates, not hand-coded to a single version.
- High-level processing service composes detection, parsing, validation, metadata, optional migration/mapping, and writing while retaining all stage reports.
- No built-in network transport, database, sanctions directory, workflow engine, rail submission, monitoring UI, or proprietary market-practice content.

## Proposed public API and planned source files

Names are provisional but file placement and category rules are mandatory. Each listed data class is consumed by at least one contract or another data class; each enum is consumed by a data class or contract.

### `src/Api/Data`

Primitive and identity files:

- `IsoTextValue.cs`, `IsoBooleanValue.cs`, `IsoDecimalValue.cs`, `IsoBinaryValue.cs`, `IsoDateValue.cs`, `IsoTimeValue.cs`, `IsoDateTimeValue.cs`, `IsoDurationValue.cs`, `IsoPartialDateValue.cs`, `IsoCodeValue.cs`, `IsoAmountValue.cs`.
- `MessageIdentifier.cs`, `NamespaceDescriptor.cs`, `QualifiedNameData.cs`, `NamespaceDeclarationData.cs`, `XmlPathData.cs`, `SourceLocation.cs`, `ResourceLimits.cs`, `ArtifactCoordinate.cs`, `ArtifactManifest.cs`, `VersionSelector.cs`.

Generic message/XML files:

- `XmlAttributeData.cs`, `DynamicXmlNode.cs`, `RawXmlFragment.cs`, `MessagePayload.cs`, `MessageHeader.cs`, `MessageEnvelope.cs`, `MessageDocument.cs`, `MessageMetadata.cs`, `MessageReadOptions.cs`, `MessageWriteOptions.cs`, `MessageReadResult.cs`, `MessageWriteResult.cs`, `MessageRepair.cs`, `MessageStreamItem.cs`, `StreamReadOptions.cs`, `StreamWriteOptions.cs`, `MetadataExtractionOptions.cs`.

Repository files:

- `RepositorySnapshot.cs`, `RepositoryProvenance.cs`, `RegistrationMetadata.cs`, `SemanticMarkupData.cs`, `ConstraintDefinition.cs`, `RepositoryReference.cs`, `RepositoryUnknownData.cs`.
- `BusinessAreaDefinition.cs`, `BusinessProcessDefinition.cs`, `BusinessRoleDefinition.cs`, `ParticipantDefinition.cs`, `BusinessTransactionDefinition.cs`, `ConversationDefinition.cs`, `MessageChoreographyDefinition.cs`, `MessageTransmissionDefinition.cs`, `MessagingEndpointDefinition.cs`, `TransportDefinition.cs`.
- `BusinessComponentDefinition.cs`, `BusinessElementDefinition.cs`, `MessageSetDefinition.cs`, `MessageDefinitionMetadata.cs`, `MessageBuildingBlockDefinition.cs`, `MessageComponentDefinition.cs`, `MessageElementDefinition.cs`, `ChoiceDefinition.cs`, `MultiplicityDefinition.cs`.
- `DataTypeDefinition.cs`, `DataTypeFacet.cs`, `CodeSetDefinition.cs`, `CodeValueDefinition.cs`, `SyntaxDefinition.cs`, `EncodingDefinition.cs`, `ExternalSchemaDefinition.cs`, `SyntaxMessageSchemeDefinition.cs`, `ConvergenceDefinition.cs`, `RepositoryImportOptions.cs`, `RepositoryImportResult.cs`, `RepositoryQueryRequest.cs`, `RepositoryQueryResult.cs`.

Schema, validation, generation, comparison, and mapping files:

- `SchemaArtifact.cs`, `SchemaRegistryOptions.cs`, `SchemaCompilationResult.cs`, `ExternalCodeCatalog.cs`, `ExternalCodeSet.cs`, `ExternalCodeValue.cs`, `CodeLookupRequest.cs`, `CodeLookupResult.cs`, `CatalogueDownloadRequest.cs`, `CatalogueDownloadResult.cs`.
- `ValidationPolicy.cs`, `ValidationRequest.cs`, `ValidationContext.cs`, `ValidationDiagnostic.cs`, `ValidationReport.cs`, `SemanticRuleDescriptor.cs`, `ProfileDefinition.cs`, `ProfileConstraint.cs`, `ProfileRuleReference.cs`, `SignatureRequest.cs`, `SignatureResult.cs`, `ChecksumRequest.cs`, `ChecksumResult.cs`.
- `GenerationOptions.cs`, `GenerationPlan.cs`, `GeneratedFile.cs`, `GenerationResult.cs`, `GenerationManifest.cs`, `GenerationSymbol.cs`.
- `RepositoryComparisonRequest.cs`, `RepositoryDiff.cs`, `DifferenceItem.cs`, `CompatibilityAssessment.cs`, `MigrationRequest.cs`, `MigrationPlan.cs`, `MigrationStep.cs`, `MigrationResult.cs`, `MappingDefinition.cs`, `MappingRuleDefinition.cs`, `MappingRequest.cs`, `MappingResult.cs`.
- `ProcessingRequest.cs`, `ProcessingResult.cs`, `BuilderRequest.cs`, `BuilderResult.cs`, `ProjectionRequest.cs`, `ProjectionResult.cs`.

Generated public data follows these patterns:

- `src/Api/Data/Generated/<ArtifactEdition>/<BusinessArea>/<MessageIdentifier>/<MessageName>.cs` for root models.
- `src/Api/Data/Generated/<ArtifactEdition>/<BusinessArea>/Components/<ComponentName>_<ShapeId>.cs` for component/choice/value wrappers.
- Generated public enum files go under `src/Api/Enums/Generated/<ArtifactEdition>/<BusinessArea>/`.
- Generated models contain fields only; all generated codec/registry logic is internal.

### `src/Api/Contracts`

- Identity/XML: `IMessageIdentifierParser.cs`, `INamespaceService.cs`, `IMessageDetector.cs`, `IXmlMessageReader.cs`, `IXmlMessageWriter.cs`, `IMessageStreamReader.cs`, `IMessageStreamWriter.cs`, `IMetadataExtractor.cs`.
- Repository/artifacts: `IRepositoryImporter.cs`, `IRepositoryQuery.cs`, `ISchemaImporter.cs`, `IExternalCodeSetImporter.cs`, `ICatalogueClient.cs`, `IArtifactStore.cs`.
- Schema/validation: `ISchemaRegistry.cs`, `IXsdValidator.cs`, `IMessageValidator.cs`, `ISemanticRule.cs`, `IProfileProvider.cs`, `ICodeSetProvider.cs`, `ISupplementaryDataValidator.cs`, `ISignatureService.cs`, `IMessageChecksum.cs`.
- Generation: `IXsdGenerator.cs`, `IJsonSchemaGenerator.cs`, `ICSharpModelGenerator.cs`.
- Comparison/migration/mapping: `IRepositoryComparer.cs`, `IMessageVersionComparer.cs`, `IMigrationPlanner.cs`, `IMessageMigrator.cs`, `IMessageMapper.cs`.
- High-level: `IMessageBuilder.cs`, `IMessageProjector.cs`, `IMessageProcessor.cs`.

The contracts expose coarse operations with uniquely named methods for each input/output form; they do not use generic methods, delegates, `ref`/`out`, default arguments, or overloads. Expected-invalid operations return result data; exceptional I/O/configuration failures use exceptions.

### `src/Api/Classes`

- `MessageIdentifierParser.cs`, `NamespaceService.cs`, `MessageDetector.cs`, `XmlMessageReader.cs`, `XmlMessageWriter.cs`, `MessageStreamReader.cs`, `MessageStreamWriter.cs`, `MetadataExtractor.cs`.
- `RepositoryImporter.cs`, `RepositoryQuery.cs`, `SchemaImporter.cs`, `ExternalCodeSetImporter.cs`, `CatalogueClient.cs`, `FileArtifactStore.cs`.
- `SchemaRegistry.cs`, `XsdValidator.cs`, `MessageValidator.cs`, `ProfileProvider.cs`, `CodeSetProvider.cs`, `SupplementaryDataValidator.cs`, `SignatureService.cs`, `MessageChecksum.cs`.
- `XsdGenerator.cs`, `JsonSchemaGenerator.cs`, `CSharpModelGenerator.cs`.
- `RepositoryComparer.cs`, `MessageVersionComparer.cs`, `MigrationPlanner.cs`, `MessageMigrator.cs`, `MessageMapper.cs`.
- `MessageBuilder.cs`, `MessageProjector.cs`, `MessageProcessor.cs`.

Each class implements its same-purpose interface(s), has one dependency-injecting constructor at most, and exposes no additional public member.

### `src/Api/Enums`

- `ArtifactKind.cs`, `RegistrationStatus.cs`, `RepositoryEntityKind.cs`, `DataTypeKind.cs`, `MessageElementKind.cs`, `EnvelopeKind.cs`, `UnknownContentMode.cs`, `NamespaceMode.cs`, `XmlFormattingMode.cs`, `DateTimeLexicalMode.cs`, `ValidationSeverity.cs`, `ValidationStage.cs`, `DiagnosticKind.cs`, `CodeSetKind.cs`, `SignatureStatus.cs`, `DifferenceKind.cs`, `CompatibilityKind.cs`, `MigrationActionKind.cs`, `MappingActionKind.cs`, `ProcessingStage.cs`, plus generated closed-code enums.

### `src/Api/Static`

- `Iso20022Namespaces.cs`: public constant/readonly namespace and URN-prefix fields only.
- `Iso20022XmlNames.cs`: standard root/header/envelope XML-name fields only.
- `Iso20022MediaTypes.cs`: artifact/media-type fields only.
- `Iso20022Limits.cs`: conservative default-limit fields only.
- `Iso20022RuleIds.cs`: stable built-in validation-rule identifier fields only.

### `src/Api/Exceptions`

- `MessageParseException.cs`, `MessageWriteException.cs`, `MessageValidationException.cs`, `RepositoryImportException.cs`, `SchemaProcessingException.cs`, `ArtifactException.cs`, `GenerationException.cs`, `MigrationException.cs`, `MappingException.cs`, `SignatureException.cs`; each directly derives from `System.Exception` and has one public constructor.

### Internal production files

- `src/Internal/Lexical/`: span-based identifier, number, binary, date/time, duration, URI, and XML-name parsers/formatters.
- `src/Internal/Xml/`: secure settings, bounded reader, namespace scope, raw-fragment capture, generic tree codec, canonicalizer, envelope codecs, and path tracker.
- `src/Internal/Repository/`: ZIP/XMI/Ecore readers, pass-one ID index, pass-two resolver, adapters, builders, indexes, integrity checks, and unknown-content store.
- `src/Internal/Schema/`: XSD loader/resolver/compiler, facet engine, code catalogs, JSON/XSD/XLSX external-code readers, and schema cache.
- `src/Internal/Validation/`: pipeline, structural/facet/code/header/profile/supplementary/signature rules, rule dispatch, and diagnostic builder.
- `src/Internal/Generation/`: normalized intermediate model, symbol table, naming, C# emitter, XSD emitter, JSON Schema emitter, deterministic file planner, and manifest writer.
- `src/Internal/Generated/<ArtifactEdition>/`: generated codecs, model metadata, internal code lookup tables, factories, and exact identifier registry.
- `src/Internal/Comparison/`: matching, graph diff, compatibility classifier, migration planner/executor, and mapping engine.
- `src/Internal/Streaming/`: BFE/batch cursor, repeating-block cursor, control totals, and bounded buffers.
- `src/Internal/HighLevel/`: semantic templates, metadata strategies, builder/projector dispatch, and composed processing pipeline.
- `src/Resources/`: embedded pinned schemas/catalogues only when explicitly selected for the build; generated resource manifest records their source hashes.

## Implementation task ledger

### Foundation and primitives

- [DONE] T001: Create the planned `src/Api` category directories and internal subsystem directories, retaining zero external production dependencies.
- [DONE] T002: Define the primitive, identity, source-location, qualified-name, artifact, selector, and resource-limit public data classes and their required enums/static fields.
- [DONE] T003: Implement span-based invariant parsing and formatting for message identifiers, integers, decimals, booleans, base64, and hexadecimal binary.
- [DONE] T004: Implement ISO/XSD date, time, date-time, duration, and partial-date lexical engines with explicit timezone and fractional-precision state.
- [DONE] T005: Implement XML name, namespace URI, URI/URN, and qualified-name lexical validation.
- [DONE] T006: Implement `IMessageIdentifierParser` and `INamespaceService`, including exception-less results and strict/lenient namespace normalization records.
- [DONE] T007: Add exhaustive primitive/lexical API tests for every legal form, boundary, absence state, culture, round trip, and malformed form.
- [DONE] T008: Add identity/namespace API tests for every message/header/envelope/supplementary namespace form, conflicts, aliases, and leading-zero versions.

### Generic XML and message containers

- [DONE] T009: Define generic XML/message data, read/write/stream options and results, repair records, envelope enums, and metadata data classes.
- [DONE] T010: Implement secure XML settings and bounded reader infrastructure with DTD/external-resource prohibition and all configured limits.
- [DONE] T011: Implement namespace-scope tracking, qualified path/line location tracking, and raw-fragment namespace closure.
- [DONE] T012: Implement the ordered generic XML node reader/writer with attributes, namespaces, mixed text, nil, comments policy, and unknown content.
- [DONE] T013: Implement `IMessageDetector` for standalone, sibling AppHdr/Document, BME, BFE, and configured wrappers without consuming seekless streams irreversibly.
- [DONE] T014: Implement `IXmlMessageReader` generic, auto-detect, exact-version, header-only, metadata-only, byte, stream, and text entry points with unique method names.
- [DONE] T015: Implement `IXmlMessageWriter` generic stream/text/buffer/string entry points and compact, indented, lexical-preserving, and canonical modes.
- [DONE] T016: Implement strict-versus-lenient repair policy and complete repair diagnostics for malformed declarations, sibling roots, prefixes, and wrappers.
- [DONE] T017: Add secure XML API tests for XXE, DTD, entity expansion, oversized constructs, deep trees, malformed encodings, and limit diagnostics.
- [DONE] T018: Add generic XML API tests for namespace/prefix/order/attribute/text/nil/comment/raw-fragment round trips and canonical stability.
- [DONE] T019: Add message detector/reader/writer API tests across every supported envelope layout, unknown message, conflicting identifiers, and non-seekable streams.

### Repository model and import

- [DONE] T020: Define all repository/provenance/registration/reference/unknown-content public data classes and repository enums.
- [DONE] T021: Define typed public data for business-process catalogue concepts, transactions, choreography, participants, endpoints, and transport metadata.
- [DONE] T022: Define typed public data for dictionary business components/elements, message sets/definitions/components/elements/choices, multiplicity, and derivations.
- [DONE] T023: Define typed public data for every data-type family, facets, code sets, syntax/encoding/schema schemes, convergence, and semantic markup.
- [DONE] T024: Implement protected ZIP/raw artifact opening with media detection, ZIP-slip defense, entry/ratio/count/size limits, and hash/provenance capture.
- [DONE] T025: Implement Ecore namespace/metamodel discovery and the ISO 20022:2013 Ecore adapter used by the June 2026 e-Repository.
- [DONE] T026: Implement streaming XMI pass one for IDs, kinds, lightweight locations, duplicates, and top-level partitions.
- [DONE] T027: Implement streaming XMI pass two for attributes, containment, space-delimited IDREFs, forward/back references, and deterministic graph resolution.
- [DONE] T028: Import Data Dictionary business concepts, elements, types, code sets/codes, constraints, semantics, and derivation graphs.
- [DONE] T029: Import Business Process Catalogue areas, processes, roles, transactions, choreography, transmissions, endpoints, and transport characteristics.
- [DONE] T030: Import message sets, definitions, identifiers, components, elements, choices, multiplicities, syntax schemes, examples/doclets, and registration lifecycle.
- [DONE] T031: Preserve unknown Ecore/XMI types, attributes, elements, and unresolved references with diagnostics sufficient for a later adapter.
- [DONE] T032: Build deterministic in-memory indexes and integrity checks without exposing mutable internal dictionaries as public API fields.
- [DONE] T033: Implement `IRepositoryImporter` for raw/ZIP stream/file forms and `IRepositoryQuery` for all proposed keys and selectors.
- [DONE] T034: Validate the bundled June 2026 repository import end to end against expected counts, representative concepts from every business domain, and zero unresolved approved references except documented artifact defects.
- [DONE] T035: Add repository importer API tests for minimal fixtures covering every Ecore class/property/reference shape, forward references, duplicates, unknowns, corrupt ZIPs, limits, and cancellation.
- [DONE] T036: Add repository query API tests for identity, name, kind, status, area, set, message identifier, namespace, version selection, and deterministic ordering.

### Artifact acquisition and external code sets

- [DONE] T037: Define artifact-store/catalogue download and external-code public request/result/catalog data classes and contracts.
- [DONE] T038: Implement `IArtifactStore` with caller-selected roots, atomic writes, collision-safe coordinates, hash verification, and no ambient home-directory assumptions.
- [DONE] T039: Implement explicit `ICatalogueClient` downloads with `HttpClient`, redirects, conditional requests, size limits, cancellation, media validation, and provenance.
- [DONE] T040: Implement official external-code JSON importer including schema metadata, code definitions, activation/deactivation, and edition data.
- [DONE] T041: Implement official external-code XSD importer and normalize its output to the same catalog model.
- [DONE] T042: Implement official external-code XLSX importer using ZIP/XML base libraries and normalize shared strings, worksheets, dates, and blank cells.
- [DONE] T043: Cross-check JSON/XSD/XLSX external-code editions and report format disagreements without choosing silently.
- [DONE] T044: Implement `ICodeSetProvider` for pinned/latest policies, internal/external separation, active-date lookup, and provenance.
- [DONE] T045: Add artifact store/catalogue client API tests with local HTTP fixtures for cache validation, redirects, truncation, bad media, hash mismatch, cancellation, and atomic failure.
- [DONE] T046: Add code importer/provider API tests for all formats, edition equivalence, active/inactive codes, dates, duplicates, malformed inputs, and lookup policy.

### XSD/schema services

- [DONE] T047: Define schema artifact/registry/compilation public data and schema contracts.
- [DONE] T048: Implement `ISchemaImporter` for XSD namespace/types/elements/facets and repository fallback metadata.
- [DONE] T049: Implement local-only include/import resolution with URI normalization, traversal protection, cycle detection, explicit precedence, and collision diagnostics.
- [DONE] T050: Implement XSD 1.0 compilation/caching with exact namespace/version/profile keys and bounded schema sets.
- [DONE] T051: Implement `ISchemaRegistry` registration, lookup, replacement policy, compiled-set invalidation, and artifact provenance.
- [DONE] T052: Implement `IXsdValidator` for raw XML and message documents with stable line/path/schema diagnostics.
- [DONE] T053: Register/import BAH V01–V04, current BME V02, BFE V02, supplementary schemas, variants, and ordinary message schemas through the same registry path.
- [DONE] T054: Add schema importer/registry API tests for include/import closure, cycles, namespace collisions, exact selection, cache invalidation, and offline resolution.
- [DONE] T055: Add XSD validator API tests against official representative valid/invalid messages from every business domain and each envelope/header family.

### Normalized generation model and generators

- [DONE] T056: Implement the internal normalized generation model that losslessly maps repository/XSD concepts without relying on C# inheritance.
- [DONE] T057: Implement deterministic symbol naming, keyword escaping, collision/shape IDs, namespace partitioning, and stable ordering.
- [DONE] T058: Define generation plan/result/file/manifest/symbol public data and generator contracts.
- [DONE] T059: Implement ISO 20022 logical-model-to-XSD generation for elements, types, choices, sequences, multiplicities, facets, codes, wildcards, and namespace rules.
- [DONE] T060: Implement JSON Schema Draft 2020-12 generation following the downloaded ISO 20022 2025 transformation recommendation.
- [DONE] T061: Implement restricted-syntax C# data emission for value wrappers, components, choices, root models, and closed-code enums.
- [DONE] T062: Implement internal metadata/codec/factory/registry emission without attributes, reflection discovery, overloads, or public helper methods.
- [DONE] T063: Emit exact repository-XMI/XML/C# symbol mappings, source hashes, generation settings, and output hashes in a deterministic manifest.
- [DONE] T064: Implement incremental generation that removes/replaces only files owned by the previous manifest and never unrelated user files.
- [DONE] T065: Add XSD generator API tests comparing normalized schemas/validation behavior with official representative XSDs across every construct.
- [DONE] T066: Add JSON Schema generator API tests for ISO mapping rules, required/optional/repeating/choice/wildcard/code/date/binary content, and deterministic output.
- [DONE] T067: Add C# generator API tests that compile generated all-domain models, instantiate every public generated type, and verify manifest-to-symbol completeness.
- [DONE] T068: Generate and check the pinned baseline model/codecs/registries for every approved/current and archived message in the selected repository snapshot.

### Typed XML codecs and JSON conversion

- [DONE] T069: Implement the generated-codec runtime for required/optional/repeating elements, attributes, choices, nil, simple content, and exact-order reading.
- [DONE] T070: Implement generated typed writing with version/model/choice/multiplicity/required-field guards and stable prefix assignment.
- [DONE] T071: Implement generated wildcard and supplementary container codecs with raw namespace-complete preservation.
- [DONE] T072: Connect exact identifier/namespace dispatch to typed models while retaining generic fallback and explicit cast/type diagnostics.
- [DONE] T073: Implement ISO 20022 JSON conversion for typed and generic models, including `$schema`/namespace identity, date/time/binary forms, choices, and wildcard loss diagnostics.
- [DONE] T074: Add typed parse/write API tests generated from every schema construct and representative messages from every business area/version family.
- [DONE] T075: Add all-generated-message smoke tests that create minimally valid instances where possible, write, XSD-validate, read, and compare semantic models.
- [DONE] T076: Add typed/generic/wildcard XML and XML/JSON/XML round-trip tests with namespace, precision, unknown-content, and exact-version assertions.

### Validation pipeline, profiles, and semantic hooks

- [DONE] T077: Define validation policy/request/context/diagnostic/report, semantic descriptor, profile, and constraint public data and enums.
- [DONE] T078: Implement the staged `IMessageValidator` pipeline with deterministic order, fail-fast/collect modes, stage selection, limits, and exception-less invalid results.
- [DONE] T079: Implement generated structural validation independent of XML parsing for required fields, multiplicity, order metadata, choices, and model/version coherence.
- [DONE] T080: Implement primitive facet and closed internal-code validation with repository provenance.
- [DONE] T081: Implement external-code validation with pinned/latest edition policy, activity dates, and edition reporting.
- [DONE] T082: Implement repository constraint dispatch and constraint applicability diagnostics for constraints not executable automatically.
- [DONE] T083: Implement `ISemanticRule` invocation, rule-set composition through data lists, applicability filters, deterministic rule IDs, and caller context.
- [DONE] T084: Implement built-in generic cross-field rules for counts/control sums, amount/currency, chronological ordering, reference uniqueness, header/body match, and duplicate business IDs.
- [DONE] T085: Implement `IProfileProvider` and overlay application for cardinality/value/path/rule restrictions with base-version compatibility and conflict detection.
- [DONE] T086: Integrate XSD, model, facet, code, repository, profile, supplementary, signature, and semantic reports into one stable diagnostic model.
- [DONE] T087: Add validation API tests for every stage, severity, fail/collect mode, limit, provenance/path/line field, redaction mode, and exception behavior.
- [DONE] T088: Add semantic hook/profile API tests for applicability, ordering, caller state, overlay strengthening, conflicts, unsupported constraints, and target-version mismatch.
- [DONE] T089: Add mutation-based invalid-message cases for every generated field constraint so each public validation path is observed to reject its intended defect.

### Supplementary data, signature, and integrity

- [DONE] T090: Define supplementary validation, signature, checksum request/result public data, status enums, and contracts.
- [DONE] T091: Implement supplementary-data discovery and preservation for global/local/repeating extensions and legacy Extension1/Extension2 forms.
- [DONE] T092: Implement XPath 2.0 `PlaceAndName` syntax checks and the safe supported evaluation subset, with distinct unsupported-expression diagnostics.
- [DONE] T093: Implement registered extension/schema lookup, location resolution, profile-supplied can-ignore/must-understand policy, and optional extension XSD validation.
- [DONE] T094: Implement XML Signature discovery and verification with caller trust policy, reference containment checks, canonicalization, and safe algorithm defaults.
- [DONE] T095: Implement XML signing with explicit algorithms/key material and deterministic insertion into supported header/envelope locations.
- [DONE] T096: Implement canonical whole-message/body hashes, semantic hashes, duplicate keys, and byte/canonical/semantic comparison modes.
- [DONE] T097: Add supplementary-data API tests from the official FAQ examples for namespaces, locations, repetitions, unknown schemas, ignore/understand, preservation, and invalid XPath.
- [DONE] T098: Add signature/checksum API tests for valid/tampered/missing-reference/unsafe-algorithm/untrusted-certificate cases and prefix-insensitive canonical equality.

### Comparison, versioning, mapping, and migration

- [DONE] T099: Define comparison/difference/compatibility/migration/mapping public data, enums, and contracts.
- [DONE] T100: Implement repository matching by XMI identity with qualified semantic fallback, ambiguity confidence, and caller override mappings.
- [DONE] T101: Implement repository graph diff for registration, business model, message model, data type, code, constraint, syntax, and transport changes.
- [DONE] T102: Implement exact message-version diff for add/remove/rename/move/type/cardinality/facet/code/constraint changes.
- [DONE] T103: Implement reader/writer/schema/data-loss/semantic compatibility classification with rationale and affected paths.
- [DONE] T104: Implement deterministic migration planning for copy/rename/move/convert/default/split/merge/code-map/drop/custom steps.
- [DONE] T105: Implement migration execution with unmapped-data report, explicit authorized supplementary fallback, and target validation.
- [DONE] T106: Implement inspectable message/canonical-model mapping rules and execution without reflection-based implicit conversion.
- [DONE] T107: Implement three-way profile rebase comparison and overlay conflict output.
- [DONE] T108: Add repository/message comparison API tests for every change/compatibility kind, identity fallback, ambiguity, and deterministic output.
- [DONE] T109: Add migration/mapping API tests for every action, conversion failure, data loss, supplementary fallback, custom rule, and target validation result.
- [DONE] T110: Verify migrations on consecutive real repository versions for representative messages in payments, securities, cards, FX, and trade domains.

### Streaming and batch processing

- [DONE] T111: Implement pull-based `IMessageStreamReader` for BFE, business-message sequences, and caller-defined wrappers with per-item diagnostics.
- [DONE] T112: Implement `IMessageStreamWriter` for BFE/custom wrappers with header data and caller-selected precomputed or deferred count/control-total strategy.
- [DONE] T113: Implement selected repeating-block streaming with parent message context, path filters, and bounded subtree materialization.
- [DONE] T114: Ensure seekable and non-seekable inputs, partial reads, cancellation, skipped invalid items, and continue/stop policies behave deterministically.
- [DONE] T115: Add stream reader/writer API tests for empty/single/mixed-version/mixed-namespace/invalid/truncated batches, limits, finalization, and non-seekable streams.
- [DONE] T116: Add repeating-block API tests proving every item is returned once, context is stable, early disposal is safe, and errors are localized.

### High-level metadata, builders, projectors, and processing

- [DONE] T117: Implement `IMetadataExtractor` strategies for identifier/namespace/header parties/message ID/service/time/duplicate/priority/UETR/amount/currency/value date/references.
- [DONE] T118: Define builder/projector/processing public request/result data and contracts without hiding exact target identifiers or validation reports.
- [DONE] T119: Generate semantic templates and `IMessageBuilder` support for payment initiation/status, interbank transfer, cash management, securities, cards, FX, and trade families.
- [DONE] T120: Implement `IMessageProjector` to stable task-oriented views while preserving source paths, identifiers, precision, and unmapped data.
- [DONE] T121: Implement `IMessageProcessor` composition of detect/read/validate/metadata/migrate/map/write with complete stage reports and caller-selected stages.
- [DONE] T122: Add metadata API tests for every supported field, absence/ambiguity, version differences, redaction, unknown models, and header/body conflicts.
- [DONE] T123: Add builder/projector API tests across every generated semantic template, supported version, required business rule, round trip, and validation outcome.
- [DONE] T124: Add processor API tests for all stage combinations, early failures, partial results, migration/mapping paths, and exact diagnostic propagation.

### Completeness and repository integration

- [DONE] T125: Add one public-API contract test inventory that maps every public interface method and implementation class to behavioral test cases without testing the syntax policy itself.
- [DONE] T126: Add reflection-based test-only API reachability tests that instantiate every public data class/enum/static field/exception and exercise every public implementation through its interface.
- [DONE] T127: Add coverage collection and require complete line and branch coverage for reachable behavior exposed by `src/Api`, with explicit reviewed exclusions only for unreachable runtime/platform failure branches.
- [DONE] T128: Add deterministic clean-worktree regeneration tests comparing generated files/manifests/hashes across repeated runs and shuffled repository input order.
- [DONE] T129: Add full-corpus tests that identify every imported message namespace, resolve its schema/model/codec/version metadata, and account for every repository message definition.
- [DONE] T130: Add full-corpus official example tests where examples exist, requiring parse, exact identification, validation expectation, write/read semantic equivalence, and wildcard preservation.
- [DONE] T131: Add forward-compatibility fixtures for unknown repository fields, future namespaces, future message versions, unknown external codes, and unavailable generated models.
- [DONE] T132: Configure the solution test workflow to build production with no external package references, run all API/corpus/generation tests, and publish coverage results without adding syntax-policy/source-checking or benchmark suites.

## Dependency order and milestones

1. T001–T019 establish safe lexical/XML foundations and generic message processing.
2. T020–T055 establish repository, artifact, code-set, and XSD truth sources.
3. T056–T076 generate and execute all-domain typed models and codecs.
4. T077–T098 complete validation, extension, signature, and integrity behavior.
5. T099–T116 add release comparison, migration, mapping, and streaming.
6. T117–T124 add task-oriented APIs without weakening the lower layers.
7. T125–T132 close public-API and full-repository completeness.

No documentation, packaging/release, syntax-policy/source-checking, or performance/benchmark tasks are included.
