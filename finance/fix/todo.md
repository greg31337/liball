# Universal FIX library development plan

## Outcome and scope

Build a dependency-free .NET library that spans byte-level FIX TagValue processing through dictionary-driven application messages, validation, durable classic FIX/FIXT sessions, and FIX Orchestra tooling. The runtime must support FIX 4.0-4.4, FIX 5.0/SP1/SP2, FIXT 1.1, and FIX Latest without hard-coding the application model into the engine. FIX Latest EP309 is the current machine-readable baseline; later extension packs must be loadable without changing the core.

The implementation under `src/` may use only the .NET base libraries. `Span<byte>` and `ReadOnlySpan<byte>` should be used at synchronous codec and transport boundaries where they avoid copying. Tests may use external packages. There are intentionally no performance/benchmark, syntax-policy/source-checking, documentation, packaging, or release tasks in this plan.

## Research basis

Normative and machine-readable sources are downloaded under `specs/`; provenance is recorded in `specs/README.md`.

- The [FIX TagValue standard](specs/fix/FIX_TagValue_Encoding_v1.0_June_2020.pdf), [FIX Session Layer](specs/fix/FIX_Session_Layer_with_Errata_November_2020.pdf), [session test cases](specs/fix/FIX_Session_Testcases_June_2020.pdf), and [FIXT 1.1](specs/fix/FIX_Transport_1.1.pdf) define the wire and recovery behavior.
- The [FIX Latest EP309 Orchestra repository](specs/fix/FIXOrchestra_FIX.Latest_EP309/FIXOrchestra_FIX.Latest_EP309.xml) supplies the complete current application vocabulary. The EP284 PDF volumes in `specs/fix/application/` provide the narrative business specifications, supplemented by later changes present in EP309.
- [Orchestra v1.0](specs/orchestra/FIX-Orchestra-v1.0-Technical-Standard.pdf), [Orchestra v1.1 RC3](specs/orchestra/Orchestra-v1.1-RC3-Technical-Specification.pdf), and their schemas define repository, interface, workflow, conditional-presence, and multiple-encoding metadata.
- The SBE, SOFH, FIXS, and RFC material under `specs/sbe/`, `specs/sofh/`, `specs/fixs/`, and `specs/rfcs/` defines integration boundaries for binary codecs, framing, TLS, ALPN, charset naming, and requirement words.
- [QuickFIX](https://github.com/quickfix/quickfix) contributes the proven dictionary-driven initiator/acceptor, generated-message, scheduling, validation, store, and logging model.
- [QuickFIX/J](https://github.com/quickfix-j/quickfixj) adds fine-grained validation/session settings, dynamic sessions, failover, queue control, multiple threading models, FIX Latest, and Orchestra-driven generation.
- [QuickFIX/n](https://github.com/connamara/quickfixn) validates the C# ecosystem needs for generated field/message models, dictionary analysis, dynamic sessions, acceptance tests, and FIXT message cracking.
- [Artio](https://github.com/artiofix/artio) informs explicit framing, codec boundaries, replay/archive control, session ownership, backpressure, slow-consumer behavior, and binary/FIXP adapter seams.
- [OnixS](https://www.onixs.biz/net-fix-engine.html) contributes flat/structured/typed representations, independently configurable sessions, pluggable stores, scheduling, failover, throttling, TLS, custom dialects, audit trails, and SBE/FAST integration patterns.

The local copies of the cited engine materials are in `specs/research/`.

## Architectural rules forced by `syntax.md`

- Public state is represented by data-holding classes with public fields; there are no properties, records, structs, tuples, nullable value types, or generic declarations.
- Operations are instance methods on small, non-inheriting interfaces. Methods have unique names and positional parameters; results that would ordinarily use `out`, tuples, or nullable value types use result data classes.
- Every public implementation class implements one or more interfaces, has at most one public constructor, and exposes no member that is absent from its interfaces. Public static methods are never used.
- Public constants live in method-free static data classes. Every custom exception derives directly from `System.Exception`.
- Public types exist only below `src/Api/` in the required category directory. Internal helpers exist only below `src/Internal/`; generated client code is returned as data and is not silently written into `src/`.
- The public API is synchronous because `await` is unavailable. Caller-driven polling, bounded queues, and replaceable transport/store contracts preserve compatibility with external event loops.
- Span values cross method boundaries but are never stored in public data classes. Durable/raw values use `byte[]`; text conversion is explicit and culture invariant.

## Complete feature inventory

### 1. TagValue wire format and scalar values

- Byte-exact `tag=value<SOH>` reading and writing, Latin-1 by default, configurable single-byte encodings by bilateral agreement, strict digit/tag parsing, configurable message/field size limits, and raw-byte preservation for unknown or extension fields.
- Streaming framing across arbitrary chunks: incomplete prefix/body/trailer, multiple messages per buffer, junk-prefix recovery policy, maximum frame size, and precise consumed/required byte counts.
- Mandatory envelope ordering: BeginString(8), BodyLength(9), MsgType(35), StandardHeader, body, StandardTrailer, CheckSum(10). BodyLength is measured from the byte after tag 9's delimiter through the delimiter before tag 10; CheckSum is the modulo-256 sum rendered as exactly three digits.
- Decode-time and encode-time calculation/verification of BodyLength and CheckSum, with strict, permissive, and diagnostic-only modes.
- Every TagValue datatype and alias: int, SeqNum, NumInGroup, Length, TagNum, float, Qty, Price, PriceOffset, Amt, Percentage, char, Boolean, String, MultipleCharValue, MultipleStringValue, Currency, Country, Exchange, MonthYear, DayOfMonth, LocalMktDate, UTCDateOnly, UTCTimeOnly, UTCTimestamp, TZTimeOnly, TZTimestamp, data, and XMLData.
- Culture-independent lexical validation and canonical formatting, arbitrary decimal scale where .NET `decimal` suffices, timestamp precision from seconds through nanoseconds/picoseconds represented without losing source digits, leap/day/calendar checks, and preservation of a valid original lexical form when requested.
- Length/data pairing with adjacency and exact byte count, allowing embedded SOH and arbitrary octets only in data fields.
- Allocation-conscious span codecs plus convenient message-object APIs; explicit results for incomplete buffers and expected parse failures, exceptions for misuse or unrecoverable operation failure.
- SOFH big- and little-endian framing, length/encoding type validation, and codec dispatch metadata.

### 2. General message model

- Ordered header, body, and trailer field collections that retain original wire order, duplicate occurrences for diagnostics, raw bytes, and unknown/custom tags.
- Mutable flat fields and structured nested repeating groups, with count-tag synchronization, delimiter-field enforcement, strict group member order, nested groups, zero instances, and independent group-entry storage.
- Dictionary-assisted reconstruction of group/component structure from flat TagValue input; components remain logical dictionary constructs because TagValue does not encode component boundaries.
- Field lookup/set/remove/iteration, header/body/trailer routing, group add/read/remove, cloning, normalized comparison, and conversion between flat and structured forms without LINQ.
- Generic runtime messages for every dictionary message type and optional strongly typed generated models; no engine rebuild is required for venue-defined messages or tags.

### 3. Dictionaries, versions, and dialects

- Canonical definitions for datatypes, code sets and code values, fields, header/trailer, components, repeating groups, messages, categories, scenarios, presence rules, length/data associations, and transport/application version metadata.
- Indexed lookup by tag/name, MsgType/name, component/group ID or name, code value, and scenario, with duplicate/collision diagnostics.
- Built-in normalized dictionaries for FIX 4.0, 4.1, 4.2, 4.3, 4.4, FIXT 1.1, FIX 5.0, 5.0 SP1, 5.0 SP2, and FIX Latest EP309, while permitting newer repositories at runtime.
- Import/export of the common QuickFIX XML dictionary dialect, including custom fields, messages, components, groups, header/trailer, enums, required flags, and nested groups.
- Import and mapping of Orchestra repositories, plus dictionary slicing, deterministic overlay/merge, extension and replacement rules, version comparison, and immutable provider snapshots for concurrent readers.
- Separate transport and application dictionaries for FIXT. Application dictionary selection by DefaultApplVerID(1137), ApplVerID(1128), ApplExtID(1156), CstmApplVerID(1129), and configured defaults.
- Session/profile-specific dialects, user-defined tag ranges, tolerant counterparty deviations as explicit options, and custom dictionary/store/provider implementations.

### 4. Message validation

- Layered validation so callers can request framing, envelope, session-header, dictionary grammar, datatype, code-set, group, conditional-rule, and application-semantic checks independently.
- Malformed tags/fields, empty values, duplicate fields, unknown fields, illegal header/body/trailer placement, tag order options, missing required fields, unsupported message types, wrong BeginString or application version, BodyLength/CheckSum mismatch, and limits.
- Full datatype lexical/range validation, boolean/code-set membership, timestamp precision, encoded length/data consistency, and configurable user-defined field acceptance.
- Repeating-group count, delimiter, member order, allowed-member, required-member, duplicate-member, nested-group, and premature termination checks.
- Orchestra scenario and conditional-presence rules, mutually exclusive elements, discriminator-dependent code sets, rule evaluation against message fields and external state, and clear diagnostics for rules that cannot be evaluated.
- Multiple issues per pass, severity, stable error code, field tag, group path, message type, source location, FIX SessionRejectReason mapping, and BusinessMessageReject mapping.
- Validation profiles equivalent to the useful QuickFIX/J flags, while strict FIX conformance remains the default.

### 5. Classic FIX and FIXT sessions

- Acceptor and initiator roles, complete state machine, dynamic session creation/removal, multiple independently configured sessions, session identity/qualifiers, comp/sub/location IDs, and duplicate connection policy.
- Logon negotiation/authentication, EncryptMethod, HeartBtInt, ResetSeqNumFlag, NextExpectedMsgSeqNum, DefaultApplVerID, password/raw-data hooks, application-version negotiation, validation before state mutation, and clean reject/disconnect behavior.
- Logout initiation/response/simultaneous logout, logout timeout, graceful vs immediate disconnect, text diagnostics, and reconnection behavior.
- Heartbeat generation on outbound inactivity, TestRequest on inbound inactivity, matched TestReqID heartbeat, heartbeat/test-request timeout, timer reset rules, and deterministic clock injection.
- Persistent next sender/target sequence numbers beginning at 1; increment rules for sent/received/rejected/duplicate messages; too-low and too-high handling; PossDupFlag, PossResend, OrigSendingTime, and SendingTime latency/ordering checks.
- Gap detection and queuing, ResendRequest ranges including EndSeqNo=0, optional redundant/closed-interval/chunked requests, replay from store, application callback before replay, administrative-message gap fills, SequenceReset GapFill vs reset modes, NewSeqNo validation, and recovery completion events.
- Correct replay header rewriting, original SendingTime preservation, PossDupFlag, no sequence consumption for gap fills, and policies for unavailable/corrupt store ranges.
- Administrative Heartbeat, TestRequest, ResendRequest, Reject, SequenceReset, Logon, Logout, and BusinessMessageReject construction/processing for FIX4 and FIXT profiles.
- Pluggable memory/file/custom message stores with atomic sequence persistence, ordered range retrieval, reset/refresh, corruption diagnostics, and retention policy. File storage uses only base .NET APIs.
- Caller-driven session polling, bounded inbound/outbound queues, explicit backpressure/slow-consumer decisions, message throttling, send-before-logon policy, and application/admin callbacks without delegates.
- Daily/weekly time-zone-aware schedules, non-stop sessions, reset-on-logon/logout/disconnect/session-boundary options, reconnect backoff, ordered failover endpoints, and disabled intervals.
- Transport-neutral session core over an `ISessionTransport`; built-in stream, TCP initiator, TCP acceptor, and `SslStream`/FIXS transports; IPv4/IPv6, partial reads/writes, EOF, connect/logon/logout timeouts, certificate policy data, and ALPN `fix` configuration.
- Configurable event/message audit logging with redaction of credentials and secure fields, null/file/custom sinks, and snapshots suitable for operations and deterministic recovery.

### 6. FIX application layer

- Dictionary-driven support for all 181 FIX Latest EP309 messages and 6,203 fields, plus supported legacy versions. Runtime support covers custom venue messages without generated code.
- Complete business categories: AccountReporting, Allocation, Application, BusinessReject, CollateralManagement, Confirmation, CrossOrders, EventCommunication, Indication, MarginRequirementManagement, MarketData, MarketStructureReferenceData, MultilegOrders, Network, OrderMassHandling, PartiesAction, PartiesReferenceData, PayManagement, PositionMaintenance, ProgramTrading, QuotationNegotiation, RegistrationInstruction, SecuritiesReferenceData, SettlementInstruction, SettlementStatusManagement, SingleGeneralOrderHandling, Testing, TradeCapture, TradeManagement, and UserManagement.
- High-level creation, typed field access, message identification/cracking through interfaces, category lookup, normalized display strings, and validation/session send paths shared by admin and application messages.
- Generated field constants, code-set enums or constants as appropriate, data models, message builders/readers, and crackers for selected dictionary slices, versions, and scenarios.

### 7. FIX Orchestra tooling

- Secure XML parsing with namespace-aware support for Orchestra v1.0 and v1.1 RC3 repository and interfaces schemas, XInclude resolution policy, schema validation, source locations, metadata, and unknown extension preservation.
- Repository content: datatypes, code sets, fields, components, groups, messages, categories, sections, scenarios, pedigree, deprecation/replacement, semantic concepts, FIXML and encoding appinfo, annotations, and rendering hints.
- Behavior content: presence rules, Score conditional/assignment expressions, variables, actors, flows, states, transitions, responses, message references, timers, and external state.
- Interface content: services, protocols, encodings, session protocols, transports, identifiers, addresses, security keys, session configurations, and repository links/overrides.
- Orchestra 1.1 multiple-encoding framework and external encoding-schema metadata, with logical-model separation from TagValue/SBE details.
- Semantic validation beyond XSD, canonical write/round trip, deterministic merge/diff, repository slice/compress, scenario/version selection, and conversion to/from the canonical FIX dictionary and QuickFIX XML.
- Structured diagnostics rather than XML parser leakage; safe limits and disabled external entity expansion by default.

### 8. Generation and binary/SBE integration

- Deterministic C# generation that obeys `syntax.md`: file-scoped namespaces, field-only public data classes, small interfaces, implementation classes, no overloads/properties/attributes/generic declarations, XML comments on every public symbol, and collision-safe identifiers.
- Generation results returned as `GeneratedFile[]` with diagnostics and a manifest; callers choose where to write. Incremental regeneration identifies unchanged outputs without deleting unrelated files.
- Dictionary/Orchestra-to-SBE XML schema generation for supported scalar/group mappings, template/schema/version IDs, null values, constant fields, semantic types, since/deprecated version, byte order, variable data, and explicit diagnostics for unmappable constructs.
- `IBinaryMessageCodec` hooks map external SBE/FAST/proprietary codecs to the canonical message model. Encoding type, template ID, schema ID, version, and SOFH metadata are carried without taking an external runtime dependency.
- Generic encoding/framing separation permits TagValue, SBE, FIXML, JSON, protobuf, or venue-native codecs to share dictionary/application semantics and transport/session orchestration where the applicable protocol permits it. Only TagValue and SOFH framing are implemented in core; SBE schema generation and codec contracts are supplied, not a complete SBE runtime.

## Proposed public API

Names are provisional, but category and dependency boundaries are deliberate. Every data/enum type listed here must be referenced by an interface or another public data class before it is added.

| API category | Proposed types | Responsibility |
|---|---|---|
| Enums | `FixFieldType`, `FixMemberKind`, `FixPresence`, `DecodeStatus`, `EncodeStatus`, `FrameStatus`, `ValidationSeverity`, `ValidationLayer`, `SessionRole`, `SessionState`, `SessionEventKind`, `SessionResetMode`, `MessageDisposition`, `LogDirection`, `TransportState`, `OrchestraVersion`, `OrchestraRuleKind`, `GenerationTarget`, `BinaryEncodingType` | Closed status/options used by data and contracts; all members have explicit `int` values. |
| Static data | `FixTags`, `FixAdminMessageTypes`, `FixBeginStrings`, `FixApplicationVersions`, `FixEncodingTypes`, `FixDefaults` | Standard tags/string codes/default limits only; no methods. |
| Wire/message data | `FixField`, `FixGroup`, `FixGroupEntry`, `FixMessage`, `DecodeOptions`, `DecodeResult`, `EncodeOptions`, `EncodeResult`, `FrameOptions`, `FrameResult`, `FieldParseResult`, `MessageEditResult` | Raw and structured messages plus non-throwing codec/edit results. |
| Dictionary data | `FixDictionary`, `FixDictionaryIdentity`, `FixDatatypeDefinition`, `FixCodeSetDefinition`, `FixCodeDefinition`, `FixFieldDefinition`, `FixMemberDefinition`, `FixComponentDefinition`, `FixGroupDefinition`, `FixMessageDefinition`, `FixScenarioDefinition`, `FixDictionaryOptions`, `DictionaryDiagnostic`, `DictionaryLoadResult`, `DictionaryDiff`, `DictionaryChange` | Canonical version-neutral grammar and diagnostics. |
| Validation data | `ValidationOptions`, `ValidationResult`, `ValidationIssue`, `ValidationContext`, `RuleEvaluationResult` | Layer selection, external state, all issues, and FIX reject mappings. |
| Session data | `SessionId`, `SessionSettings`, `SessionEndpoint`, `SessionSchedule`, `SessionTimeWindow`, `SessionTlsSettings`, `SessionThrottleSettings`, `SessionSnapshot`, `SessionEvent`, `SessionPollResult`, `SessionSendResult`, `TransportReadResult`, `TransportWriteResult`, `StoredMessage`, `StoreRangeResult`, `AuthenticationRequest`, `AuthenticationResult`, `ApplicationMessageContext`, `ApplicationDecision`, `LogEntry` | Configuration, state, transport/store results, callbacks, and audit records. |
| Orchestra data | `OrchestraRepository`, `OrchestraMetadata`, `OrchestraAnnotation`, `OrchestraRule`, `OrchestraVariable`, `OrchestraActor`, `OrchestraFlow`, `OrchestraStateMachine`, `OrchestraState`, `OrchestraTransition`, `OrchestraResponse`, `OrchestraInterface`, `OrchestraService`, `OrchestraProtocol`, `OrchestraSession`, `OrchestraReadOptions`, `OrchestraWriteOptions`, `OrchestraDiagnostic`, `OrchestraReadResult`, `OrchestraDiff`, `OrchestraChange` | Loss-aware repository/interface/behavior representation and tool results. |
| Generation/binary data | `ModelGenerationOptions`, `GeneratedFile`, `GenerationDiagnostic`, `GenerationResult`, `SbeGenerationOptions`, `BinaryMessageContext`, `BinaryDecodeResult`, `BinaryEncodeResult` | Generated source/schema output and external codec metadata/results. |
| Contracts | `IFieldValueCodec`, `IMessageDecoder`, `IMessageEncoder`, `IMessageFramer`, `IMessageOperations`, `IDictionaryLoader`, `IDictionaryWriter`, `IDictionaryProvider`, `IDictionaryComposer`, `IDictionaryQuery`, `IMessageValidator`, `IRuleEvaluator`, `IMessageStore`, `IFixLog`, `ISessionClock`, `ISessionTransport`, `ISessionAuthenticator`, `ISessionApplication`, `IMessageThrottle`, `IFixSession`, `ISessionEngine`, `ISessionSettingsLoader`, `IOrchestraReader`, `IOrchestraWriter`, `IOrchestraValidator`, `IOrchestraMapper`, `IOrchestraDiffer`, `IOrchestraSlicer`, `IModelGenerator`, `ISbeSchemaGenerator`, `IBinaryMessageCodec` | All public operations. Each interface has unique non-overloaded instance method names and no inheritance. |
| Implementations | `FixFieldValueCodec`, `TagValueMessageDecoder`, `TagValueMessageEncoder`, `TagValueMessageFramer`, `SofhMessageFramer`, `FixMessageOperations`, `QuickFixDictionaryLoader`, `QuickFixDictionaryWriter`, `OrchestraDictionaryLoader`, `EmbeddedDictionaryProvider`, `FixDictionaryComposer`, `FixDictionaryQuery`, `FixMessageValidator`, `OrchestraRuleEvaluator`, `MemoryMessageStore`, `FileMessageStore`, `NullFixLog`, `FileFixLog`, `SystemSessionClock`, `StreamSessionTransport`, `TcpInitiatorTransport`, `TcpAcceptorTransport`, `TlsSessionTransport`, `BasicSessionAuthenticator`, `TokenBucketMessageThrottle`, `FixSession`, `FixSessionEngine`, `TextSessionSettingsLoader`, `OrchestraReader`, `OrchestraWriter`, `OrchestraValidator`, `OrchestraMapper`, `OrchestraDiffer`, `OrchestraSlicer`, `CSharpModelGenerator`, `SbeSchemaGenerator` | Base-library implementations of the contracts. |
| Exceptions | `FixEncodingException`, `FixDictionaryException`, `FixValidationException`, `FixSessionException`, `FixStoreException`, `FixTransportException`, `OrchestraException`, `ModelGenerationException` | Each derives directly from `System.Exception` and has one constructor. Expected parse/validation outcomes use result data instead. |

Representative contract shapes, without fixing fine details:

- `IMessageDecoder.DecodeMessage(ReadOnlySpan<byte>, FixDictionary, DecodeOptions) -> DecodeResult`
- `IMessageEncoder.EncodeMessage(FixMessage, FixDictionary, Span<byte>, EncodeOptions) -> EncodeResult`
- `IMessageFramer.ReadFrame(ReadOnlySpan<byte>, FrameOptions) -> FrameResult`
- `IMessageValidator.ValidateMessage(FixMessage, FixDictionary, ValidationOptions, ValidationContext) -> ValidationResult`
- `IDictionaryProvider.ResolveDictionary(FixDictionaryIdentity) -> DictionaryLoadResult`
- `IFixSession.PollSession() -> SessionPollResult`, `SendMessage(FixMessage) -> SessionSendResult`, and explicit start/logout/stop/snapshot methods
- `ISessionApplication` receives unique create/logon/logout/admin/application/replay callbacks and returns `ApplicationDecision` where a decision is required
- `IOrchestraReader.ReadRepository(Stream, OrchestraReadOptions) -> OrchestraReadResult`
- `IModelGenerator.GenerateModels(FixDictionary, ModelGenerationOptions) -> GenerationResult`
- `IBinaryMessageCodec.DecodeBinary(ReadOnlySpan<byte>, BinaryMessageContext) -> BinaryDecodeResult`

## Planned source files

One public type is planned per file. Related file names are grouped below only to keep this plan readable.

### Public API files

- `src/Api/Enums/`: `FixFieldType.cs`, `FixMemberKind.cs`, `FixPresence.cs`, `DecodeStatus.cs`, `EncodeStatus.cs`, `FrameStatus.cs`, `ValidationSeverity.cs`, `ValidationLayer.cs`, `SessionRole.cs`, `SessionState.cs`, `SessionEventKind.cs`, `SessionResetMode.cs`, `MessageDisposition.cs`, `LogDirection.cs`, `TransportState.cs`, `OrchestraVersion.cs`, `OrchestraRuleKind.cs`, `GenerationTarget.cs`, `BinaryEncodingType.cs`.
- `src/Api/Static/`: `FixTags.cs`, `FixAdminMessageTypes.cs`, `FixBeginStrings.cs`, `FixApplicationVersions.cs`, `FixEncodingTypes.cs`, `FixDefaults.cs`.
- `src/Api/Data/` wire/message: `FixField.cs`, `FixGroup.cs`, `FixGroupEntry.cs`, `FixMessage.cs`, `DecodeOptions.cs`, `DecodeResult.cs`, `EncodeOptions.cs`, `EncodeResult.cs`, `FrameOptions.cs`, `FrameResult.cs`, `FieldParseResult.cs`, `MessageEditResult.cs`.
- `src/Api/Data/` dictionary/validation: `FixDictionary.cs`, `FixDictionaryIdentity.cs`, `FixDatatypeDefinition.cs`, `FixCodeSetDefinition.cs`, `FixCodeDefinition.cs`, `FixFieldDefinition.cs`, `FixMemberDefinition.cs`, `FixComponentDefinition.cs`, `FixGroupDefinition.cs`, `FixMessageDefinition.cs`, `FixScenarioDefinition.cs`, `FixDictionaryOptions.cs`, `DictionaryDiagnostic.cs`, `DictionaryLoadResult.cs`, `DictionaryDiff.cs`, `DictionaryChange.cs`, `ValidationOptions.cs`, `ValidationResult.cs`, `ValidationIssue.cs`, `ValidationContext.cs`, `RuleEvaluationResult.cs`.
- `src/Api/Data/` session: `SessionId.cs`, `SessionSettings.cs`, `SessionEndpoint.cs`, `SessionSchedule.cs`, `SessionTimeWindow.cs`, `SessionTlsSettings.cs`, `SessionThrottleSettings.cs`, `SessionSnapshot.cs`, `SessionEvent.cs`, `SessionPollResult.cs`, `SessionSendResult.cs`, `TransportReadResult.cs`, `TransportWriteResult.cs`, `StoredMessage.cs`, `StoreRangeResult.cs`, `AuthenticationRequest.cs`, `AuthenticationResult.cs`, `ApplicationMessageContext.cs`, `ApplicationDecision.cs`, `LogEntry.cs`.
- `src/Api/Data/` Orchestra/generation: `OrchestraRepository.cs`, `OrchestraMetadata.cs`, `OrchestraAnnotation.cs`, `OrchestraRule.cs`, `OrchestraVariable.cs`, `OrchestraActor.cs`, `OrchestraFlow.cs`, `OrchestraStateMachine.cs`, `OrchestraState.cs`, `OrchestraTransition.cs`, `OrchestraResponse.cs`, `OrchestraInterface.cs`, `OrchestraService.cs`, `OrchestraProtocol.cs`, `OrchestraSession.cs`, `OrchestraReadOptions.cs`, `OrchestraWriteOptions.cs`, `OrchestraDiagnostic.cs`, `OrchestraReadResult.cs`, `OrchestraDiff.cs`, `OrchestraChange.cs`, `ModelGenerationOptions.cs`, `GeneratedFile.cs`, `GenerationDiagnostic.cs`, `GenerationResult.cs`, `SbeGenerationOptions.cs`, `BinaryMessageContext.cs`, `BinaryDecodeResult.cs`, `BinaryEncodeResult.cs`.
- `src/Api/Contracts/`: one file for every `I...` contract listed in the API table: `IFieldValueCodec.cs`, `IMessageDecoder.cs`, `IMessageEncoder.cs`, `IMessageFramer.cs`, `IMessageOperations.cs`, `IDictionaryLoader.cs`, `IDictionaryWriter.cs`, `IDictionaryProvider.cs`, `IDictionaryComposer.cs`, `IDictionaryQuery.cs`, `IMessageValidator.cs`, `IRuleEvaluator.cs`, `IMessageStore.cs`, `IFixLog.cs`, `ISessionClock.cs`, `ISessionTransport.cs`, `ISessionAuthenticator.cs`, `ISessionApplication.cs`, `IMessageThrottle.cs`, `IFixSession.cs`, `ISessionEngine.cs`, `ISessionSettingsLoader.cs`, `IOrchestraReader.cs`, `IOrchestraWriter.cs`, `IOrchestraValidator.cs`, `IOrchestraMapper.cs`, `IOrchestraDiffer.cs`, `IOrchestraSlicer.cs`, `IModelGenerator.cs`, `ISbeSchemaGenerator.cs`, `IBinaryMessageCodec.cs`.
- `src/Api/Classes/`: one file for every concrete implementation listed in the API table, with the same type/file name.
- `src/Api/Exceptions/`: `FixEncodingException.cs`, `FixDictionaryException.cs`, `FixValidationException.cs`, `FixSessionException.cs`, `FixStoreException.cs`, `FixTransportException.cs`, `OrchestraException.cs`, `ModelGenerationException.cs`.

### Internal implementation and resource files

- `src/Internal/Wire/`: `AsciiDigits.cs`, `ByteSearch.cs`, `ChecksumCalculator.cs`, `BodyLengthCalculator.cs`, `FieldScanner.cs`, `DataFieldScanner.cs`, `ScalarParser.cs`, `ScalarFormatter.cs`, `TimestampParser.cs`, `TimestampFormatter.cs`, `TagValueFrameScanner.cs`, `SofhFrameScanner.cs`, `MessageBuilder.cs`, `GroupParser.cs`.
- `src/Internal/Dictionary/`: `DictionaryIndex.cs`, `DictionaryNormalizer.cs`, `DictionaryOverlay.cs`, `DictionaryVersionSelector.cs`, `QuickFixXmlParser.cs`, `QuickFixXmlEmitter.cs`, `EmbeddedDictionaryLoader.cs`.
- `src/Internal/Validation/`: `EnvelopeValidator.cs`, `DatatypeValidator.cs`, `CodeSetValidator.cs`, `StructureValidator.cs`, `GroupValidator.cs`, `ConditionalValidator.cs`, `RejectMapper.cs`, `ScoreLexer.cs`, `ScoreParser.cs`, `ScoreEvaluator.cs`.
- `src/Internal/Session/`: `SessionStateMachine.cs`, `SessionTimers.cs`, `SequenceManager.cs`, `GapTracker.cs`, `ResendProcessor.cs`, `ReplayBuilder.cs`, `AdminMessageFactory.cs`, `SessionScheduler.cs`, `ReconnectPlanner.cs`, `SessionQueue.cs`, `FileStoreIndex.cs`, `FileStoreRecord.cs`, `SettingsParser.cs`.
- `src/Internal/Orchestra/`: `SecureXmlReader.cs`, `SchemaCatalog.cs`, `XIncludeResolver.cs`, `OrchestraV10Parser.cs`, `OrchestraV11Parser.cs`, `InterfacesParser.cs`, `OrchestraSemanticValidator.cs`, `OrchestraCanonicalizer.cs`, `OrchestraMerge.cs`, `OrchestraSlice.cs`, `SourceLocationReader.cs`.
- `src/Internal/Generation/`: `CSharpNameResolver.cs`, `CSharpEmitter.cs`, `GeneratedApiPlanner.cs`, `SbeTypeMapper.cs`, `SbeSchemaEmitter.cs`.
- `src/Resources/Dictionaries/`: normalized embedded dictionary resources for supported legacy versions, FIXT 1.1, and the selected FIX Latest baseline; generated from the checked specification inputs, not manually maintained.

### Planned test files

- `tests/Wire/`: `FieldValueCodecTests.cs`, `TagValueFramerTests.cs`, `TagValueDecoderTests.cs`, `TagValueEncoderTests.cs`, `EnvelopeCalculationTests.cs`, `DataFieldTests.cs`, `RepeatingGroupCodecTests.cs`, `SofhFramerTests.cs`, `WireBoundaryTests.cs`, `WireRoundTripTests.cs`.
- `tests/Dictionary/`: `DictionaryModelTests.cs`, `QuickFixDictionaryTests.cs`, `OrchestraDictionaryTests.cs`, `EmbeddedDictionaryTests.cs`, `DictionaryOverlayTests.cs`, `DictionarySelectionTests.cs`, `DictionaryRoundTripTests.cs`.
- `tests/Validation/`: `EnvelopeValidationTests.cs`, `DatatypeValidationTests.cs`, `StructureValidationTests.cs`, `RepeatingGroupValidationTests.cs`, `ConditionalValidationTests.cs`, `ValidationProfileTests.cs`, `RejectMappingTests.cs`.
- `tests/Session/`: `SessionStateTests.cs`, `LogonLogoutTests.cs`, `HeartbeatTests.cs`, `SequenceNumberTests.cs`, `ResendRequestTests.cs`, `GapFillTests.cs`, `ReplayTests.cs`, `MemoryStoreTests.cs`, `FileStoreTests.cs`, `ScheduleTests.cs`, `ReconnectFailoverTests.cs`, `ThrottleBackpressureTests.cs`, `TcpTransportTests.cs`, `TlsTransportTests.cs`, `MultiSessionTests.cs`, `FixSessionConformanceTests.cs`.
- `tests/Orchestra/`: `OrchestraV10ReadTests.cs`, `OrchestraV11ReadTests.cs`, `OrchestraSchemaTests.cs`, `OrchestraSemanticTests.cs`, `OrchestraWriteRoundTripTests.cs`, `OrchestraMappingTests.cs`, `OrchestraDiffTests.cs`, `OrchestraSliceTests.cs`, `ScoreLanguageTests.cs`.
- `tests/Generation/`: `CSharpModelGeneratorTests.cs`, `GeneratedSourceCompilationTests.cs`, `GeneratedModelRoundTripTests.cs`, `SbeSchemaGeneratorTests.cs`, `BinaryCodecContractTests.cs`.
- `tests/Api/`: `PublicApiConstructionTests.cs`, `PublicContractTests.cs`, `ApplicationCategoryTests.cs`, `EndToEndPeerTests.cs`, `CompleteApiCoverageTests.cs`.
- `tests/Fixtures/`: exact wire vectors, malformed corpus, session transcripts, Orchestra samples, generated-model snapshots, TLS test certificates, and dictionary/dialect fixtures.

## Delivery order and implementation tasks

Tasks are dependency ordered. A task is complete only when its production behavior and the directly named tests pass; the final coverage phase verifies every reachable member and error path of `src/Api`.

### Foundation and canonical API

- [DONE] T001: Create the required `src/Api` category directories, `src/Internal` subsystem directories, resource directory, and matching test directories without adding any source dependency outside the .NET base libraries.
- [DONE] T002: Implement the public enum files with explicit stable int32 values and use each enum from a public data class or contract.
- [DONE] T003: Implement the method-free static classes for core tags, admin MsgTypes, BeginStrings, application versions, encoding identifiers, and safe default limits.
- [DONE] T004: Implement the wire/message public data classes with public fields, ordered collections, raw byte values, structured groups, and no behavior beyond permitted internal helpers.
- [DONE] T005: Implement dictionary and validation public data classes, including identities, definition graphs, diagnostics, diffs, contexts, and result containers.
- [DONE] T006: Implement session, Orchestra, generation, and binary public data classes and ensure every data-holding type is consumed by a contract or another data class.
- [DONE] T007: Implement all public contracts as small non-inheriting interfaces with unique, non-overloaded, synchronous instance methods and positional parameters.
- [DONE] T008: Implement the eight direct-from-`System.Exception` public exception classes, each with one constructor and XML documentation.
- [DONE] T009: Add API construction and contract-shape tests covering every public data class, enum, static field, constructor, interface method signature, and exception.

### Scalar codecs, TagValue framing, and messages

- [DONE] T010: Implement internal byte search, decimal digit, limit, BodyLength, and modulo-256 CheckSum primitives over spans.
- [DONE] T011: Implement invariant parsing and formatting for integer, sequence/count/length/tag, decimal numeric aliases, char, Boolean, strings, multivalue strings, country, currency, and exchange values.
- [DONE] T012: Implement strict calendar/time parsing and formatting for all FIX date/time types and precision levels without using nullable value types.
- [DONE] T013: Implement data/XMLData handling with dictionary-declared length associations, exact byte counts, embedded SOH support, and adjacency enforcement.
- [DONE] T014: Implement `FixFieldValueCodec` and explicit non-throwing parse result behavior for expected lexical failures.
- [DONE] T015: Implement incremental TagValue frame scanning for partial, complete, concatenated, oversized, and malformed streams with precise consumed/required counts.
- [DONE] T016: Implement `TagValueMessageFramer` with strict and configurable recovery policies and no retention of caller spans.
- [DONE] T017: Implement flat header/body/trailer field decoding with raw byte and wire-order preservation, envelope extraction, duplicate retention, and source offsets.
- [DONE] T018: Implement dictionary-assisted nested repeating-group decoding, including delimiter discovery, ordered members, nested groups, and malformed-group diagnostics.
- [DONE] T019: Implement `TagValueMessageDecoder`, including BeginString, BodyLength, MsgType, CheckSum, encoded data, strict/permissive options, and incomplete input results.
- [DONE] T020: Implement canonical field and message encoding with ordered envelope emission, configurable order preservation, encoded data, and group flattening.
- [DONE] T021: Implement `TagValueMessageEncoder` with exact output sizing, caller-provided destination spans, BodyLength/CheckSum calculation, and insufficient-buffer results.
- [DONE] T022: Implement `FixMessageOperations` for field/header/trailer routing, lookup, mutation, removal, cloning, structured group manipulation, count synchronization, and flat/structured conversion.
- [DONE] T023: Implement SOFH v1.0/v1.1 big- and little-endian scanning and `SofhMessageFramer`, including encoding type and size validation.
- [DONE] T024: Add exhaustive scalar codec tests for canonical, boundary, culture, malformed, precision, and round-trip cases for every exposed datatype operation.
- [DONE] T025: Add framing/envelope tests for arbitrary chunk boundaries, multiple frames, junk, limits, exact BodyLength, and every checksum value from 000 through 255.
- [DONE] T026: Add decoder/encoder tests for unknown tags, duplicate tags, empty values, raw bytes, alternate single-byte encodings, insufficient buffers, and lossless/canonical round trips.
- [DONE] T027: Add encoded-data and nested repeating-group codec tests, including SOH in payloads, zero/incorrect counts, wrong delimiters, invalid order, and nested termination.
- [DONE] T028: Add SOFH standard-vector, endian, partial-frame, concatenated-frame, unknown-encoding, and limit tests.

### Dictionary formats, providers, and supported versions

- [DONE] T029: Implement canonical dictionary normalization and indexes for tag/name, MsgType/name, components, groups, code sets, scenarios, and length/data pairs.
- [DONE] T030: Implement `FixDictionaryQuery` with deterministic lookup results and ambiguity/missing-definition diagnostics.
- [DONE] T031: Implement `QuickFixDictionaryLoader` for all supported QuickFIX XML constructs, nested groups, components, enums, custom fields/messages, and header/trailer definitions using `System.Xml` only.
- [DONE] T032: Implement `QuickFixDictionaryWriter` with deterministic ordering and loss diagnostics for canonical concepts not representable by QuickFIX XML.
- [DONE] T033: Implement `OrchestraDictionaryLoader` as the initial structural Orchestra-to-canonical mapper for v1.0 and v1.1 namespaces, deferring behavioral rules to the Orchestra phase.
- [DONE] T034: Implement `FixDictionaryComposer` for deterministic base-plus-overlay merge, add/replace/remove, venue extension tags, collision detection, and provenance preservation.
- [DONE] T035: Implement dictionary diff data for additions, removals, definition changes, requiredness, code values, messages, groups, and scenario changes.
- [DONE] T036: Implement FIXT transport/application dictionary selection using BeginString and ApplVerID/DefaultApplVerID/ApplExtID/CstmApplVerID precedence.
- [DONE] T037: Generate normalized embedded resources for FIX 4.0-4.4, FIXT 1.1, FIX 5.0/SP1/SP2, and FIX Latest EP309 from the checked specification inputs.
- [DONE] T038: Implement `EmbeddedDictionaryProvider` with immutable cached snapshots, identity lookup, concurrent reads, and runtime provider fallback for unknown/newer versions.
- [DONE] T039: Add dictionary model/index/query tests for duplicates, aliases, recursive component references, length pairs, scenarios, and stable lookup.
- [DONE] T040: Add QuickFIX XML import/export tests against every fixture in `specs/fix/quickfix-dictionaries/` and custom dialect fixtures.
- [DONE] T041: Add embedded dictionary integrity tests proving each version loads, all references resolve, session/application layering is correct, and representative messages/groups validate.
- [DONE] T042: Add composer/diff tests for extensions, conflicts, overrides, deletions, provenance, and deterministic results.
- [DONE] T043: Add FIXT dictionary-selection tests for logon defaults, per-message overrides, custom versions, missing dictionaries, and simultaneous sessions using different application versions.

### Validation and conditional rules

- [DONE] T044: Implement the layered validation coordinator and options for envelope, session header, dictionary structure, datatypes, code sets, groups, and conditional rules.
- [DONE] T045: Implement envelope/header/trailer validation for mandatory positions, duplicates, BodyLength, CheckSum, BeginString, MsgType, version, and configured limits.
- [DONE] T046: Implement dictionary structure validation for supported message type, allowed fields, required fields, header/body/trailer placement, component expansion, field-order policy, and user-defined fields.
- [DONE] T047: Implement datatype, range, code-set, timestamp, empty-value, and length/data validation for all FIX scalar definitions.
- [DONE] T048: Implement recursive repeating-group validation for count, delimiter, order, allowed/required/duplicate members, nested groups, and group paths.
- [DONE] T049: Implement the Score lexer and parser for Orchestra conditional-presence and assignment expressions using only permitted internal C# syntax.
- [DONE] T050: Implement `OrchestraRuleEvaluator` for field/state lookup, comparisons, logical/arithmetic operators, code literals, existence, assignment, and indeterminate results.
- [DONE] T051: Implement conditional/scenario validation, mutually exclusive members, discriminator-dependent code sets, external variables, and unsupported-rule diagnostics.
- [DONE] T052: Implement stable validation issue codes and mapping to SessionRejectReason, RefTagID, RefMsgType, RefSeqNum, and BusinessRejectReason.
- [DONE] T053: Implement `FixMessageValidator` with multi-issue collection, severity filtering, deterministic issue order, and strict/default/tolerant profile behavior.
- [DONE] T054: Add envelope, structural, datatype, code-set, timestamp, and user-defined-field validation tests for every option and failure mapping.
- [DONE] T055: Add recursive group validation tests for malformed count/delimiter/order/member/nesting cases and precise diagnostic paths.
- [DONE] T056: Add Score lexer/parser/evaluator and conditional-presence tests from Orchestra examples plus malformed and indeterminate expressions.
- [DONE] T057: Add validation profile and reject-mapping tests that compare strict conformance with explicitly enabled counterparty deviations.

### Stores, transports, and classic session protocol

- [DONE] T058: Implement `SystemSessionClock` and deterministic fake-clock contract fixtures for session timer tests.
- [DONE] T059: Implement `MemoryMessageStore` with atomic sequence state, ordered message range retrieval, reset, refresh, and retention.
- [DONE] T060: Define and implement the append/index/sequence file format used by `FileMessageStore`, including partial-tail and corruption detection.
- [DONE] T061: Implement `FileMessageStore` atomic sequence persistence, append-before-send semantics, range replay, reset/refresh, locking, and restart recovery.
- [DONE] T062: Implement `NullFixLog` and redaction-aware `FileFixLog` for incoming/outgoing wire records and session events.
- [DONE] T063: Implement `StreamSessionTransport` for partial synchronous reads/writes, EOF, close, state, and caller-supplied connected streams.
- [DONE] T064: Implement `TcpInitiatorTransport` with IPv4/IPv6 endpoints, connection timeout, ordered failover endpoints, socket options, and reconnect-safe lifecycle.
- [DONE] T065: Implement `TcpAcceptorTransport` with bind/listen/accept lifecycle, endpoint capture, connection timeout, and one accepted connection per session transport instance.
- [DONE] T066: Implement `TlsSessionTransport` over `SslStream` with client/server mode, certificate settings, protocol floor, hostname validation data, and FIXS ALPN.
- [DONE] T067: Implement `BasicSessionAuthenticator` and authentication request/result flow for comp IDs, credentials, raw data, and custom application decisions.
- [DONE] T068: Implement `TokenBucketMessageThrottle` and bounded queue decisions without timers hidden from the session clock.
- [DONE] T069: Implement `TextSessionSettingsLoader` for defaults plus per-session sections, stable diagnostics, environment-independent parsing, schedules, endpoints, stores, validation flags, and FIXT dictionaries.
- [DONE] T070: Implement the internal session state machine for disconnected, connecting, awaiting logon, active, logout pending, recovery, and stopped states in both roles.
- [DONE] T071: Implement session identity and inbound header checks for comp/sub/location IDs, qualifiers, BeginString, SendingTime, OrigSendingTime, PossDupFlag, and latency.
- [DONE] T072: Implement Logon creation/processing, validation-before-mutation, HeartBtInt and application-version negotiation, ResetSeqNumFlag, NextExpectedMsgSeqNum, authentication, and rejection.
- [DONE] T073: Implement Logout creation/processing, simultaneous logout, timeout, graceful/immediate close, text reasons, and application notification ordering.
- [DONE] T074: Implement heartbeat and TestRequest timers, matched IDs, inactivity rules, timeout disconnects, and timer reset behavior.
- [DONE] T075: Implement persistent sender/target sequence consumption rules and correct handling of expected, too-low, too-high, duplicate, rejected, and queued messages.
- [DONE] T076: Implement gap tracking and ResendRequest generation for open-ended, closed, redundant, and configured chunk-size ranges.
- [DONE] T077: Implement SequenceReset reset and GapFill processing, NewSeqNo checks, stale gap fills, queued-message release, and recovery completion.
- [DONE] T078: Implement outbound replay from `IMessageStore`, application replay decisions, PossDupFlag/OrigSendingTime/SendingTime rewriting, admin gap fills, unavailable ranges, and no extra sequence consumption.
- [DONE] T079: Implement all administrative message factories and processing paths for Heartbeat, TestRequest, ResendRequest, Reject, SequenceReset, Logon, Logout, and BusinessMessageReject under FIX4/FIXT profiles.
- [DONE] T080: Implement `FixSession` caller-driven start, poll, send, logout, stop, snapshot, queues, validation, application/admin callbacks, logging, throttling, and transport/store coordination.
- [DONE] T081: Implement daily/weekly/time-zone session scheduling and configured sequence resets at session boundary, logon, logout, or disconnect.
- [DONE] T082: Implement reconnect backoff, failover rotation, disabled intervals, connect/logon/logout deadlines, and explicit terminal/retryable transport outcomes.
- [DONE] T083: Implement `FixSessionEngine` for dynamic add/remove/find, independent multi-session polling, duplicate identity detection, lifecycle aggregation, and event isolation.
- [DONE] T084: Add shared message-store contract tests, restart/corruption/partial-tail file tests, retention tests, and sequence atomicity tests.
- [DONE] T085: Add stream/TCP/TLS transport contract tests for partial I/O, EOF, timeout, IPv4/IPv6 where available, certificates, hostname failure, ALPN, reconnect, and failover.
- [DONE] T086: Add settings, schedule, time-zone, reset-policy, throttle, queue-bound, logging-redaction, and authenticator tests.
- [DONE] T087: Add deterministic initiator/acceptor logon, logout, heartbeat, TestRequest, timeout, invalid-logon, and callback-order tests.
- [DONE] T088: Add exhaustive sequence-number tests for expected/high/low/duplicate messages, PossDupFlag, OrigSendingTime, rejected messages, disconnects, and resets.
- [DONE] T089: Add resend/gap-fill/replay tests for open/closed/chunked ranges, nested gaps, stale resets, unavailable store data, admin gap fills, application replay decisions, and recovery queue release.
- [DONE] T090: Implement all mandatory and optional scenarios from `FIX_Session_Testcases_June_2020.pdf` as automated initiator/acceptor conformance tests for FIX4 and FIXT profiles.
- [DONE] T091: Add multi-session tests for dynamic lifecycle, mixed FIX/FIXT versions, independent stores/dictionaries/schedules, failure isolation, and simultaneous recovery.

### Orchestra repository, interfaces, workflows, and transforms

- [DONE] T092: Implement secure XML reader settings, safe limits, external entity prohibition, namespace dispatch, optional controlled XInclude, and source location capture.
- [DONE] T093: Implement built-in schema catalog and XSD validation for Orchestra v1.0 repository/interfaces and v1.1 RC3 repository/interfaces documents.
- [DONE] T094: Implement Orchestra metadata, annotations, categories, sections, pedigree, deprecation/replacement, semantic concepts, appinfo, and unknown-extension parsing.
- [DONE] T095: Implement v1.0 datatype, code set, field, component, group, message, member, scenario, and presence parsing into the loss-aware Orchestra model.
- [DONE] T096: Implement v1.1 RC3 logical model, external datatype definitions, multiple-encoding framework, encoding appinfo, and external schema references.
- [DONE] T097: Implement actor, flow, state machine, state, transition, response, timer, variable, conditional rule, and assignment parsing.
- [DONE] T098: Implement Orchestra Interfaces parsing for interfaces, services, protocols, encodings, session protocols, transports, identifiers, addresses, security keys, sessions, and repository links/overrides.
- [DONE] T099: Implement semantic validation for unique IDs/names, resolved references, scenario inheritance, member cardinality, group delimiters, code types, rule references, workflow transitions, protocol stacks, and repository links.
- [DONE] T100: Implement `OrchestraReader` with XSD plus semantic diagnostics and partial-result policy, preserving source locations and supported unknown extensions.
- [DONE] T101: Implement canonical `OrchestraWriter` output for repository and interfaces content with stable ordering, namespaces, metadata, rules, workflows, and extensions.
- [DONE] T102: Implement `OrchestraMapper` in both directions between Orchestra and canonical dictionaries, with explicit loss diagnostics for behavior/encoding metadata.
- [DONE] T103: Implement deterministic Orchestra diff across metadata, definitions, scenarios, encodings, rules, workflows, and interfaces.
- [DONE] T104: Implement Orchestra slicing/compression by messages, categories, scenarios, versions, and dependency closure across fields/components/groups/code sets/rules.
- [DONE] T105: Add v1.0 and v1.1 RC3 schema/read tests against downloaded schemas, official FIX Latest EP309, interfaces fixtures, XInclude, invalid XML, unsafe entities, unknown extensions, and source locations.
- [DONE] T106: Add Orchestra semantic validation tests for every reference/collision/type/cardinality/workflow/protocol-stack rule and multiple simultaneous diagnostics.
- [DONE] T107: Add canonical write/read round-trip tests preserving logical content, behavior, interfaces, metadata, extensions, and deterministic bytes.
- [DONE] T108: Add Orchestra-to-dictionary-to-QuickFIX and reverse mapping tests with expected loss diagnostics, version/scenario selection, and nested groups.
- [DONE] T109: Add diff and slice tests proving dependency closure, scenario/encoding preservation, stable ordering, and no mutation of source repositories.

### Application coverage, generated models, and SBE hooks

- [DONE] T110: Implement generic application message creation and identification for any dictionary MsgType, including selected scenario and application version metadata.
- [DONE] T111: Implement interface-based message cracking/dispatch for admin vs application messages and category/name lookup without delegates, reflection requirements, or generated-code dependency.
- [DONE] T112: Validate the EP309 repository import and enumerate all 181 messages, 6,203 fields, categories, components, groups, and code sets with no unresolved canonical references.
- [DONE] T113: Implement deterministic C# name resolution for tags, MsgTypes, code values, reserved words, invalid characters, duplicate names, scenarios, and version namespaces.
- [DONE] T114: Implement the C# source emitter for syntax-compliant field constants, code values, data models, message operations, and crackers with required XML documentation.
- [DONE] T115: Implement `CSharpModelGenerator` selection by version/category/message/scenario, dependency closure, deterministic `GeneratedFile` results, unchanged-file hashes, diagnostics, and manifest data.
- [DONE] T116: Implement Orchestra-to-SBE type mapping for scalars, enums, composites, groups, variable data, constants, null values, semantic types, byte order, IDs, and version metadata.
- [DONE] T117: Implement `SbeSchemaGenerator` with deterministic valid SBE XML and explicit errors for unsupported precision, recursion, conditional layouts, and ambiguous mappings.
- [DONE] T118: Finalize `IBinaryMessageCodec` and binary result/context contracts so external codecs can preserve schema/template/version/encoding metadata and map to canonical messages.
- [DONE] T119: Add application coverage tests that construct, encode, decode, identify, and validate at least one valid minimal instance of every dictionary message whose required fields can be synthesized, with explicit fixtures for conditionally required messages.
- [DONE] T120: Add legacy-version application tests for representative order, execution, market data, quote, security definition, allocation, trade capture, position, collateral, settlement, and user/session flows.
- [DONE] T121: Add generator snapshot and Roslyn compilation tests for all output categories, naming collisions, selected slices, nested groups, FIXT separation, and repeated deterministic runs.
- [DONE] T122: Add generated-model runtime round-trip tests through the generic message model, TagValue encoder/decoder, validator, and message cracker.
- [DONE] T123: Add SBE schema validation tests against the downloaded SBE XSD, mapping tests for all supported constructs, stable ID/version tests, and unsupported-construct diagnostics.
- [DONE] T124: Add fake external binary codec contract tests through SOFH framing and canonical-message mapping without introducing an external runtime dependency under `src/`.

### End-to-end completeness and API coverage

- [DONE] T125: Build a deterministic in-memory duplex transport fixture that can fragment, coalesce, delay, drop, duplicate, reorder where deliberately invalid, disconnect, and reconnect peer traffic.
- [DONE] T126: Add end-to-end initiator/acceptor tests from settings and dictionaries through TCP-like framing, logon, application exchange, validation reject, recovery, heartbeat, and logout.
- [DONE] T127: Add restart end-to-end tests proving file-store sequence and replay continuity across multiple physical connections in one FIX session.
- [DONE] T128: Add FIXT end-to-end tests that negotiate default application versions and route per-message ApplVerID overrides to different dictionaries on one session.
- [DONE] T129: Add custom-dialect end-to-end tests importing Orchestra and QuickFIX XML extensions, generating models, exchanging custom messages/groups, and applying custom validation rules.
- [DONE] T130: Add malformed-input corpus tests for every public decoder/framer/loader/validator entry point, including bounded random chunking and mutation, asserting deterministic results and no hangs.
- [DONE] T131: Add resource-limit tests for maximum message, field, group count/depth, XML size/depth, queue size, store record, and generated output limits.
- [DONE] T132: Add concurrency tests for immutable dictionary sharing, store/session locks, dynamic engine changes, simultaneous sessions, logging, and snapshot consistency.
- [DONE] T133: Add complete public implementation contract tests shared across framers, dictionary loaders/writers, stores, logs, transports, Orchestra tools, generators, and session roles.
- [DONE] T134: Add explicit success, expected-failure-result, misuse-exception, boundary, and state-transition tests for every public method exposed by `src/Api/Contracts`.
- [DONE] T135: Add tests that read/write every public data field and exercise every enum/static constant in a real API flow, not only via reflection.
- [DONE] T136: Add tests for every public implementation constructor with valid and invalid dependency/configuration combinations and verify failure atomicity.
- [DONE] T137: Add tests for every public exception creation/throw path and ensure expected protocol/validation failures remain structured results where specified.
- [DONE] T138: Produce and enforce an API coverage matrix mapping every `src/Api` type/member to one or more functional tests, and close every uncovered behavior or error path.
- [DONE] T139: Run the full test suite with line and branch coverage for `src/Api` and its reachable internals, add focused tests for all meaningful uncovered branches, and require complete exposed-API functionality coverage.
- [DONE] T140: Perform the final acceptance run against all downloaded FIX Session cases, all embedded dictionaries, FIX Latest EP309, Orchestra v1.0/v1.1 RC3 fixtures, generated-source compilation, binary hooks, and multi-peer end-to-end scenarios.
