# Universal Diameter Base Protocol and Application Framework Development Plan

## Goal and scope

Build a dependency-free-under-`src/` C# library that can act as a Diameter client, server, relay, proxy, or redirect agent; implements RFC 6733 base protocol and accounting; implements the RFC 8506 Credit-Control Application (DCCA); and supplies a dictionary-driven extension framework for additional IETF, 3GPP, ETSI, and vendor applications without changing the core codec or stack.

The library must cover the complete path from binary framing and AVP values through transports, peer state, routing, sessions, application dispatch, accounting, credit control, overload handling, persistence seams, and operational inspection. Tests may use external packages and processes, but all production code under `src/` must use only built-in .NET libraries.

## Standards and research basis

The downloaded source set and provenance are recorded in `specs/SOURCES.md`. The primary standards are RFC 6733 and RFC 8506. Supporting behavior is drawn from RFC 3539, RFC 5729, RFC 6408, RFC 6737, RFC 7068, RFC 7075, RFC 7683, RFC 7944, and RFC 8581–8583, plus the current IANA AAA, S-NAPTR, port, and enterprise-number registries.

Research conclusions from existing implementations:

- freeDiameter separates a reusable protocol/dictionary layer from peer, watchdog, routing, dispatch, and extension machinery. Its dictionary models vendors, applications, types, enumerated constants, AVPs, commands, and ABNF-like occurrence rules. Its routing hooks, redirect cache, relay support, failover/failback, peer ACLs, and application/dictionary extensions are important completeness targets.
- jDiameter exposes raw messages alongside application sessions, peer/realm tables, request listeners, routing, validation, statistics, overload control, and pluggable session persistence. This supports making sessions and applications first-class public contracts rather than hiding them behind a monolithic node.
- Open5GS uses freeDiameter as a base stack and keeps Cx, Gx, Gy, Rx, S6a, S6b, and SWx dictionaries/message adapters in separate modules. This supports a neutral core plus independently registered application packages and typed adapters.
- The plan does not copy source or APIs from these projects. It uses their feature boundaries to check that the RFC-driven design has no major operational gaps.

## Architectural constraints from `syntax.md`

- Every production compilation unit uses a file-scoped namespace and only the permitted C# subset.
- Public API is organized strictly under `src/Api`: interfaces in `Contracts`, field-based data classes in `Data`, constants-only classes in `Static`, enums in `Enums`, direct `System.Exception` subclasses in `Exceptions`, and interface-implementing classes in `Classes`.
- Public data objects use public fields, not properties. Each is used by at least one public interface or another public data object.
- Every public implementation class has at most one constructor, implements one or more interfaces, and exposes no public instance member that is absent from those interfaces. Interfaces do not inherit other interfaces.
- Public operations are instance interface methods. There are no public static methods, public delegates, events, properties, method overloads, generic declarations, `ref`/`out` parameters, tuples, or optional parameters.
- Operations that would conventionally use `TryX(out value)`, tuples, delegates, or generic callbacks use named result data classes and callback interfaces instead.
- Public method names are globally distinct within every implementation's combined interface set. Names include the operation domain where needed, such as `DecodeMessage`, `RegisterApplication`, and `ReadPeerSnapshot`.
- The public API is synchronous because `await` is disallowed. Implementations use blocking .NET I/O plus internal worker threads, bounded queues, timers, and explicit lifecycle methods. Callback interfaces deliver inbound requests, answers, state changes, and failures.
- `Span<T>` and `ReadOnlySpan<T>` are used for immediate codec operations. Stored or asynchronously queued bytes use owned arrays or `ReadOnlyMemory<T>` so spans never escape their valid lifetime.
- Signed integers represent counts and sizes. Unsigned integers represent protocol fields, flags, identifiers, counters defined as unsigned on the wire, and binary masks.
- Exceptions report exceptional failures. Decode, validation, route-selection, and send outcomes that commonly fail also have exception-less result objects.
- XML documentation required by `syntax.md` is written with each public declaration as part of implementation, not treated as a separate documentation workstream.
- Production dependencies are limited to the .NET runtime libraries. JSON dictionaries use `System.Text.Json`; networking uses `System.Net`, `System.Net.Sockets`, `System.Net.Security`, and `System.Security.Cryptography.X509Certificates`.

## Support boundary

- TCP/IPv4, TCP/IPv6, and TLS-over-TCP with mutual authentication are portable built-in transports.
- SCTP is supported by a built-in socket transport when the host OS and .NET socket layer expose SCTP. Capability probing produces a normal unsupported result on other hosts. Multistream behavior, payload protocol identifier 46, ordered delivery requirements, and failover semantics are covered where available.
- .NET has no portable built-in DTLS/SCTP implementation. `IDiameterTransportFactory` and `IDiameterConnection` permit a host-supplied DTLS/SCTP transport without linking it into `src/`. An IPsec-protected connection is represented as an externally secured transport and still undergoes Diameter peer authorization.
- The core includes the base application, base accounting, DCCA, capabilities update, realm redirection, routing priority, overload, and load dictionaries/behavior. Other Diameter applications are extensions registered through the same public framework.
- Persistence contracts are public. In-memory and file-backed stores use only .NET. Database, distributed-cache, or cluster adapters live outside `src/` and can be used by consumers or tests.

## Complete feature inventory

### 1. Binary protocol and value codecs

- Diameter version 1, 20-byte header, 24-bit message length and command code, application ID, Hop-by-Hop Identifier, End-to-End Identifier, and R/P/E/T command flags.
- Strict reserved-bit checks, legal flag combinations, request/answer correlation rules, maximum message size, and exact four-byte message alignment.
- AVP header parsing and encoding with 32-bit code, V/M/P flags, 24-bit length, optional Vendor-ID, payload, and zero-filled four-byte padding.
- Ordered and repeated AVPs; preservation of unknown optional AVPs; rejection/reporting of unsupported mandatory AVPs at validation time; lossless decode/re-encode.
- Basic AVP types: OctetString, Integer32, Integer64, Unsigned32, Unsigned64, Float32, Float64, and Grouped.
- Common derived types: Address for IPv4/IPv6, Time using the NTP epoch and era-safe conversion, UTF8String, DiameterIdentity, DiameterURI, Enumerated, IPFilterRule, and QoSFilterRule.
- Diameter identity/realm normalization and comparison, URI scheme/transport/protocol parsing, UTF-8 validation, network-byte-order primitives, and precise unsigned-value handling.
- Nested Grouped AVPs with configurable depth/count/byte limits and exact inner AVP validation.
- Incremental stream framing for fragmented headers/bodies, coalesced messages, clean EOF, truncated EOF, and retained remainder bytes.
- Buffer-size calculation, caller-provided `Span<byte>` encoding, owned-array convenience results, and explicit consumed/written counts.
- Strict and permissive decode profiles, structured codec issues, stable byte offsets, and safe handling of malformed or hostile lengths without unbounded allocation.

### 2. Dictionary and validation system

- Definitions for vendors, applications, base/derived types, named enumerated values, AVPs, request/answer commands, and command/grouped-AVP occurrence rules.
- Lookup by numeric identity and by canonical name, with `(Vendor-ID, AVP-Code)` and `(Application-ID, Command-Code, request flag)` as primary keys.
- Required/prohibited V/M/P flag masks, fixed-head rules, required/optional AVPs, minimum/maximum occurrences, ordering groups, and unconstrained extension AVPs.
- Transactional dictionary composition, deterministic duplicate/conflict detection, immutable read snapshots, and independent dictionary modules.
- Programmatic definition registration plus a versioned JSON dictionary format loaded with `System.Text.Json`; no XML parser is required for the production format.
- Built-in RFC 6733, RFC 8506, RFC 6737, RFC 7075, RFC 7683, RFC 7944, and RFC 8581–8583 dictionaries.
- Message and Grouped AVP validation, request/answer flags, application ID agreement, mandatory AVP recognition, enumerated domains, fixed-length types, UTF-8/identity/URI/address validation, and occurrence rules.
- Structured validation issues that can be converted into protocol-correct answers with Result-Code, Experimental-Result, Error-Message, Error-Reporting-Host, and Failed-AVP.
- Dictionary introspection sufficient for diagnostic formatting and for application adapters to discover definitions without coupling to internal types.

### 3. Base commands and error processing

- Capabilities-Exchange (CER/CEA), Device-Watchdog (DWR/DWA), Disconnect-Peer (DPR/DPA), Re-Auth (RAR/RAA), Abort-Session (ASR/ASA), Session-Termination (STR/STA), and Accounting (ACR/ACA) command construction and validation.
- All RFC 6733 base AVPs, enumerated values, command occurrence tables, and Result-Code classes.
- Correct E-bit behavior, protocol-error answers, application-error answers, proxyable error propagation, Failed-AVP synthesis, and Experimental-Result handling.
- Origin-Host, Origin-Realm, Origin-State-Id, Session-Id, Destination-Host, Destination-Realm, application identifiers, Route-Record, Proxy-Info, and redirect AVP helpers.
- Unknown command, unsupported application, unsupported version, invalid header bits, invalid AVP bits/length, missing AVP, AVP-not-allowed, occurrence conflict, unable-to-deliver, loop, redirect, too-busy, and election-lost outcomes.

### 4. Transport, security, and connection lifecycle

- Active connect and passive listen for TCP over IPv4/IPv6, partial read/write handling, connection-close detection, no-delay/keepalive controls, connection timeouts, and bounded ingress/egress queues.
- TLS before CER/CEA on the secure Diameter service, TLS 1.2+ policy, mutual certificate authentication, hostname/identity checks, configurable trust anchors through built-in X.509 APIs, and certificate-to-peer authorization.
- Conditional SCTP socket support with feature detection, IPv4/IPv6, message framing across received chunks, stream selection policy, ordered delivery, and Diameter SCTP payload identifier.
- Host-supplied transport factory support for DTLS/SCTP, externally protected IPsec, test transports, and specialized environments.
- Per-connection ownership, idempotent close, graceful drain, worker shutdown, queue backpressure, receive limits, and isolation of transport faults from the node process.

### 5. Peer discovery, tables, capabilities, and state

- Static peer configuration and dynamic discovery through Diameter S-NAPTR, SRV, A, and AAAA records, including application service tags, transport tags, priority, weighted selection, TTL cache, negative cache, and fallback order.
- Peer and realm tables containing identity, realm, endpoints, configured/dynamic status, expiry, connection direction, security policy, advertised capabilities, common applications, state, timers, and operational counters.
- Peer admission policy for known and dynamic peers, identity and realm authorization, certificate checks, allowed applications, and path authorization.
- CER/CEA generation, parsing, validation, common-application calculation, no-common-application rejection, security negotiation, Origin-State-Id restart detection, and advertised Host-IP-Address/vendor/product/firmware/application data.
- Simultaneous connection election using Origin-Host identity ordering, duplicate connection elimination, inbound/outbound role tracking, and correct handoff to the surviving connection.
- Full RFC 6733 peer state machine: Closed, Wait-Conn-Ack, Wait-I-CEA, Wait-Conn-Ack/Elect, Wait-Returns, R-Open, I-Open, and Closing, with explicit events/actions and Tc/Tw timers.
- Idle watchdog generation, DWR/DWA response, pending-watchdog tracking, suspect transition, transport failure detection, failover, reconnection, and failback only after three successful watchdog exchanges.
- DPR/DPA graceful shutdown with Rebooting, Busy, and Do-Not-Want-To-Talk-To-You causes and cause-specific reconnect suppression.
- Capabilities Update Application support for the RFC 6737 permitted subset (addresses, vendor/product/firmware data, and advertised applications) while requiring connection restart for security-mechanism changes.
- Read-only peer snapshots and state-change callbacks without exposing mutable internal state.

### 6. Request tracking, reliability, and duplicate handling

- Collision-resistant Hop-by-Hop and End-to-End ID generation with restart/time components and wrap handling.
- Pending-request correlation by peer and Hop-by-Hop ID, timeout tracking, answer validation, cancellation, late-answer handling, and exact ownership cleanup.
- Failover of eligible pending requests to alternate peers, new Hop-by-Hop ID assignment, retained End-to-End ID, T-bit setting, and application/session policy checks.
- Duplicate request detection keyed by Origin-Host and End-to-End ID, bounded lifetime/cache size, cached-answer replay where safe, and application-visible duplicate metadata.
- Request rate/backpressure limits, bounded pending transactions, overload-aware admission, and explicit rejected-send results.
- Separation of transport delivery, Diameter answer receipt, application success, and protocol/application error outcomes.

### 7. Routing, agents, redirects, priority, load, and overload

- Local delivery, relay, proxy, redirect, and translation decisions, with node roles enabled independently per application/realm.
- Routing precedence for Destination-Host, local applications, realm/application routes, default routes, and peers with compatible advertised applications.
- RFC 5729 User-Name/realm routing rules, realm normalization, local-realm detection, and configurable home-realm policy.
- Route-Record insertion and predictive loop avoidance; Proxy-Info insertion/removal and answer traversal; hop-by-hop answer return along the exact request path.
- Static routes with priority/weight, peer health, sticky session routes, load-aware selection, failover candidates, and deterministic policy hooks.
- Redirect answer production and consumption; all Redirect-Host-Usage scopes; redirect cache keys, precedence, expiry, invalidation, loop protection, and secure redirect target authorization.
- RFC 7075 realm-based redirect application support and interaction with the base redirect cache.
- RFC 7944 DRMP priority propagation and priority-aware queue/routing decisions without starving lower priorities.
- RFC 7683 DOIC feature negotiation, loss abatement, overload report scopes/sequences/validity, reacting/reporting-node behavior, and report stripping/forwarding rules.
- RFC 8581 host/realm/peer overload reports for agents, RFC 8582 rate abatement, and RFC 8583 load reports used in route selection.
- Extensible route-policy and message-interceptor chains using interfaces, ordered execution, short-circuit decisions, and immutable input snapshots.

### 8. Sessions and base accounting

- Globally unique Session-Id generation using DiameterIdentity, startup time, monotonic sequence, and optional caller correlation text.
- Stateful and stateless authorization sessions for clients and servers, with RFC 6733 state machines, application-driven and server-initiated flows.
- Authorization-Lifetime, Auth-Grace-Period, Session-Timeout, Multi-Round-Time-Out, re-authentication, STR/STA termination, ASR/ASA abort, and Origin-State-Id restart inference.
- Session binding and server failover policy, route affinity, peer loss handling, session expiry, explicit release, and snapshot inspection.
- Pluggable session store with optimistic version data and in-memory implementation; sufficient serialization data for external replicated stores.
- Base accounting client/server support for EVENT, START, INTERIM, and STOP records, record numbering, session/sub-session/multi-session correlation, and Acct-Interim-Interval scheduling.
- Accounting fault resilience, local pending-record store, retransmission, failover, duplicate suppression, ordered delivery, interim overwrite rule, and Accounting-Realtime-Required behavior.
- In-memory and file-backed accounting record stores with atomic replace semantics and restart recovery using only built-in .NET APIs.

### 9. Application extension framework

- Application modules register application/vendor IDs, command definitions, AVP definitions, advertised auth/accounting capabilities, roles, request handlers, and optional session behavior.
- Dispatch key is application ID, command code, and request/answer direction, with deterministic conflict rejection and lifecycle-safe registration/removal.
- Raw-message handlers and dictionary-validated handlers; typed application adapters can be built on top without changing the stack.
- Request context exposes peer, route, session, duplicate/retransmission status, receive time, cancellation/shutdown state, and safe answer construction services.
- Outbound request API supports destination, application, timeout, failover policy, session affinity, priority, and response callbacks/results.
- Ordered inbound/outbound interceptor interfaces for policy, tracing, transformation, and translation without delegates or extension methods.
- Application exception isolation, protocol-error mapping, maximum handler concurrency, bounded queues, graceful unregister/drain, and node shutdown ordering.
- Application-specific state-store and timer seams suitable for client/server FSMs and external high-availability adapters.

### 10. RFC 8506 Credit-Control Application

- Application ID 4 advertisement; CCR/CCA command 272; complete command grammar, all RFC 8506 AVPs, all enumerated values, and all credit-control Result-Codes.
- Client and server state machines for session-based and one-time-event credit control, with Tx timer, request sequencing, persistence, and duplicate/out-of-order detection.
- INITIAL, UPDATE, TERMINATION, and EVENT requests; monotonic CC-Request-Number; CC-Correlation-Id; CC-Sub-Session-Id; and service-context/rating input hooks.
- Requested, granted, and used service units for time, money, input/output/total octets, and service-specific units, including partial grants and reporting.
- Multiple-Services-Credit-Control with Service-Identifier, Rating-Group, independent quotas, pooled units, G-S-U pool references, and Multiple-Services-Indicator.
- Tariff-Time-Change and Tariff-Change-Usage handling, quota validity timers, quota threshold/exhaustion notifications, intermediate interrogation scheduling, and final usage reporting.
- Server-initiated RAR/RAA credit re-authorization and coordination with the base authorization session.
- Final-Unit-Indication actions: terminate, redirect, and restrict access; legacy and extension redirect forms; restriction filters; QoS final-unit indication; graceful service termination.
- One-time service-price enquiry, balance check, direct debit, and refund with Requested-Action and correct stateless behavior.
- CCFH and DDFH failure policies, fail-open/fail-closed/continue behavior, server failover, CC-Session-Failover negotiation, pending transaction recovery, and failback.
- Cost information, unit value/exponent/currency handling, subscription identifiers, user-equipment information and extensions, and privacy-sensitive AVP handling/data minimization hooks.
- Application-neutral rating handler contracts so the library provides protocol/state machinery without embedding billing business rules.

### 11. Configuration, operations, and resilience

- Field-based node, peer, route, transport, security, session, accounting, DCCA, overload, limit, and timer configuration data with eager cross-field validation.
- Safe start/stop/drain lifecycle, idempotent disposal through explicit methods, bounded background workers, and deterministic cleanup without relying on finalizers.
- Runtime addition/removal of peers, routes, dictionaries, and applications where protocol-safe; immutable replacement for settings that cannot be mutated safely.
- Statistics snapshots for bytes/messages/AVPs, decode/validation errors, peers, watchdogs, requests, timeouts, retransmissions, duplicates, routes, redirects, sessions, accounting, DCCA, load, and overload.
- Structured event sink interfaces for protocol, peer, routing, session, application, security, and error events, with payload redaction controls.
- Health snapshot covering listener state, peer availability, route availability, queue saturation, timer lag, store health, overload state, and recent failures.
- Thread safety for concurrent client calls, callbacks, peer transitions, route changes, timer expiry, and shutdown, with callbacks invoked outside internal locks.

## Proposed public API

Names are provisional but their category and responsibility are fixed. Methods listed are representative operation names, not signatures.

### `src/Api/Contracts`

| Planned file | Proposed interfaces and operations |
| --- | --- |
| `IDiameterMessageCodec.cs` | `IDiameterMessageCodec`: `MeasureMessage`, `EncodeMessage`, `DecodeMessage`; `IDiameterAvpCodec`: uniquely named encode/decode operations for every basic and derived AVP type. |
| `IDiameterDictionary.cs` | `IDiameterDictionary`: register/merge/read definition snapshots and lookups; `IDiameterDictionaryLoader`: load a JSON dictionary module; `IDiameterValidator`: validate a message or grouped AVP and build an error answer. |
| `IDiameterTransport.cs` | `IDiameterTransportFactory`: create listeners/connections and probe support; `IDiameterListener`: accept/close; `IDiameterConnection`: send/receive/close/read connection metadata. |
| `IDiameterSecurity.cs` | `IPeerAdmissionPolicy`: authorize discovered/inbound peers; `ICertificatePeerPolicy`: authorize certificates against Diameter identity/realm; `IRedactionPolicy`: redact event data. |
| `IDiameterDiscovery.cs` | `IDiameterPeerDiscovery`: discover endpoints for a realm/application and invalidate cache; `IDnsResolver`: resolve NAPTR, SRV, A, and AAAA records. |
| `IDiameterPeers.cs` | `IDiameterPeerManager`: add/remove/connect/disconnect peers and read snapshots; `IPeerEventHandler`: receive state/capability/failure events. |
| `IDiameterRouting.cs` | `IDiameterRouter`: select request routes, process redirects, and read route snapshots; `IRoutePolicy`: evaluate/score candidates; `ITranslationHandler`: translate requests/answers between applications. |
| `IDiameterRequests.cs` | `IDiameterRequestClient`: send/cancel requests and read pending snapshots; `IDiameterAnswerHandler`: receive answer/timeout/failure outcomes; `IDuplicateRequestStore`: detect and cache duplicate outcomes. |
| `IDiameterSessions.cs` | `IDiameterSessionManager`: create/find/update/end sessions and read snapshots; `IDiameterSessionStore`: load/save/remove session state; `ISessionEventHandler`: receive session transitions. |
| `IDiameterAccounting.cs` | `IDiameterAccountingClient`: send event/start/interim/stop records; `IDiameterAccountingHandler`: process accounting requests; `IAccountingRecordStore`: persist pending records. |
| `IDiameterApplications.cs` | `IDiameterApplicationHost`: register/unregister modules and read registrations; `IDiameterApplication`: provide registration and handle lifecycle; `IDiameterRequestHandler`: handle inbound requests; `IMessageInterceptor`: process ordered inbound/outbound stages. |
| `IDiameterCreditControl.cs` | `IDiameterCreditControlClient`: begin/update/terminate sessions and execute events; `ICreditControlRequestHandler`: perform rating/credit decisions; `ICreditControlStore`: persist DCCA state; `ICreditControlEventHandler`: receive quota/final-unit/failure events. |
| `IDiameterOverload.cs` | `IDiameterOverloadManager`: publish/process overload and load information and make admission decisions; `IOverloadSource`: provide local overload/load state. |
| `IDiameterNode.cs` | `IDiameterNode`: configure/start/drain/stop, send requests, access subsystems, and read node health/statistics. |
| `IDiameterEvents.cs` | `IDiameterEventSink`: receive structured operational events; `IDiameterClock`: UTC/monotonic time; `IDiameterIdentifierSource`: create protocol identifiers. |

### `src/Api/Data`

| Planned file | Proposed field-based data classes |
| --- | --- |
| `ProtocolData.cs` | `DiameterMessage`, `DiameterAvp`, `DiameterApplicationId`, `DiameterUri`, `DiameterEndpoint`, `DiameterIdentity`. |
| `CodecData.cs` | `DiameterEncodeResult`, `DiameterDecodeResult`, `DiameterAvpDecodeResult`, `DiameterFramingResult`, `DiameterCodecIssue`, `DiameterValueResult`. |
| `DictionaryData.cs` | `DiameterDictionaryModule`, `DiameterVendorDefinition`, `DiameterApplicationDefinition`, `DiameterTypeDefinition`, `DiameterEnumValueDefinition`, `DiameterAvpDefinition`, `DiameterCommandDefinition`, `DiameterAvpRuleDefinition`, `DiameterDictionarySnapshot`. |
| `ValidationData.cs` | `DiameterValidationResult`, `DiameterValidationIssue`, `DiameterErrorAnswerRequest`. |
| `ConfigurationData.cs` | `DiameterNodeConfiguration`, `DiameterLimitConfiguration`, `DiameterTimerConfiguration`, `DiameterTransportConfiguration`, `DiameterSecurityConfiguration`. |
| `PeerData.cs` | `DiameterPeerConfiguration`, `DiameterPeerCapabilities`, `DiameterPeerSnapshot`, `DiameterPeerEvent`, `DiameterDisconnectRequest`. |
| `DiscoveryData.cs` | `DiameterDiscoveryRequest`, `DiameterDiscoveryResult`, `DiameterNaptrRecord`, `DiameterSrvRecord`, `DiameterAddressRecord`. |
| `RoutingData.cs` | `DiameterRouteConfiguration`, `DiameterRouteRequest`, `DiameterRouteCandidate`, `DiameterRouteDecision`, `DiameterRouteSnapshot`, `DiameterRedirectEntry`, `DiameterTranslationResult`. |
| `RequestData.cs` | `DiameterSendRequest`, `DiameterSendResult`, `DiameterAnswerResult`, `DiameterPendingRequestSnapshot`, `DiameterDuplicateResult`. |
| `SessionData.cs` | `DiameterSessionConfiguration`, `DiameterSessionRecord`, `DiameterSessionSnapshot`, `DiameterSessionEvent`, `DiameterSessionUpdate`. |
| `AccountingData.cs` | `DiameterAccountingRequest`, `DiameterAccountingAnswer`, `DiameterAccountingRecord`, `DiameterAccountingResult`, `DiameterAccountingStoreResult`. |
| `ApplicationData.cs` | `DiameterApplicationRegistration`, `DiameterCommandRegistration`, `DiameterRequestContext`, `DiameterApplicationResult`, `DiameterInterceptorContext`. |
| `CreditControlData.cs` | `DiameterCreditControlRequest`, `DiameterCreditControlAnswer`, `DiameterCreditControlSession`, `DiameterCreditControlResult`, `DiameterServiceUnit`, `DiameterMultipleServiceCredit`, `DiameterPoolReference`, `DiameterFinalUnitInstruction`, `DiameterCostInformation`, `DiameterSubscriptionIdentifier`, `DiameterUserEquipmentInformation`. |
| `OverloadData.cs` | `DiameterOverloadConfiguration`, `DiameterOverloadReport`, `DiameterLoadReport`, `DiameterAdmissionRequest`, `DiameterAdmissionDecision`. |
| `OperationsData.cs` | `DiameterNodeStatistics`, `DiameterNodeHealth`, `DiameterSubsystemHealth`, `DiameterOperationalEvent`. |

### `src/Api/Enums`

| Planned file | Proposed enums |
| --- | --- |
| `ProtocolEnums.cs` | `DiameterMessageFlags`, `DiameterAvpFlags`, `DiameterAvpBaseType`, `DiameterAddressFamily`, `DiameterTransportKind`, `DiameterSecurityMode`, `DiameterNodeRole`. |
| `PeerEnums.cs` | `DiameterPeerState`, `DiameterPeerEventKind`, `DiameterConnectionDirection`, `DiameterDisconnectCause`, `DiameterPeerSource`, `DiameterCapabilityChange`. |
| `RoutingEnums.cs` | `DiameterRouteAction`, `DiameterAgentRole`, `DiameterRedirectHostUsage`, `DiameterRouteSource`, `DiameterRoutingPriority`, `DiameterTranslationDisposition`. |
| `SessionEnums.cs` | `DiameterSessionKind`, `DiameterSessionRole`, `DiameterSessionState`, `DiameterAuthRequestType`, `DiameterAuthSessionState`, `DiameterReAuthRequestType`, `DiameterTerminationCause`. |
| `AccountingEnums.cs` | `DiameterAccountingRecordType`, `DiameterAccountingRealtimeRequired`, `DiameterAccountingState`. |
| `CreditControlEnums.cs` | `DiameterCreditControlRequestType`, `DiameterRequestedAction`, `DiameterCreditControlState`, `DiameterCreditControlFailureHandling`, `DiameterDirectDebitFailureHandling`, `DiameterCreditControlUnitType`, `DiameterFinalUnitAction`, `DiameterTariffChangeUsage`, `DiameterMultipleServicesIndicator`, `DiameterCheckBalanceResult`, `DiameterCreditControlDisposition`, `DiameterSubscriptionIdType`, `DiameterUserEquipmentInfoType`, `DiameterRedirectAddressType`. |
| `OverloadEnums.cs` | `DiameterOverloadReportType`, `DiameterOverloadAlgorithm`, `DiameterOverloadScope`, `DiameterAdmissionAction`, `DiameterLoadSource`. |
| `ResultEnums.cs` | `DiameterResultClass`, `DiameterIssueSeverity`, `DiameterCodecStatus`, `DiameterValidationStatus`, `DiameterSendStatus`, `DiameterHealthStatus`. |

### `src/Api/Static`

| Planned file | Proposed constants-only classes |
| --- | --- |
| `DiameterProtocolConstants.cs` | Header sizes, alignment, version, ports, SCTP payload identifiers, time epoch, flag masks, and protocol limits/defaults. |
| `DiameterApplicationIds.cs` | Base/common, base accounting, credit control, capabilities update, realm redirect, and standardized extension IDs. |
| `DiameterCommandCodes.cs` | All base, accounting, DCCA, capabilities update, and extension command codes used by the core. |
| `DiameterAvpCodes.cs` | RFC 6733, RFC 8506, capabilities-update, redirect, DRMP, DOIC, and load AVP codes. |
| `DiameterResultCodes.cs` | RFC 6733 and RFC 8506 standard result codes. |
| `DiameterVendorIds.cs` | IETF and commonly required neutral vendor IDs used by built-in dictionaries. |

### `src/Api/Exceptions`

| Planned file | Proposed direct `System.Exception` subclasses |
| --- | --- |
| `DiameterCodecException.cs` | Exceptional encode/decode misuse or unrecoverable codec failure. |
| `DiameterDictionaryException.cs` | Invalid/conflicting dictionary modules. |
| `DiameterValidationException.cs` | Caller-requested throwing validation mode. |
| `DiameterTransportException.cs` | Transport creation or I/O lifecycle failure. |
| `DiameterPeerException.cs` | Peer configuration/state operation failure. |
| `DiameterRoutingException.cs` | Caller-requested throwing route failure. |
| `DiameterSessionException.cs` | Invalid session lifecycle operation. |
| `DiameterApplicationException.cs` | Registration or application lifecycle failure. |
| `DiameterCreditControlException.cs` | Invalid DCCA lifecycle operation. |

### `src/Api/Classes`

| Planned file | Public implementation classes |
| --- | --- |
| `DiameterMessageCodec.cs` | Implements message framing and wire encoding/decoding contracts. |
| `DiameterAvpCodec.cs` | Implements all base and derived AVP value codecs. |
| `DiameterDictionary.cs` | Implements composed immutable dictionary snapshots and registration. |
| `DiameterJsonDictionaryLoader.cs` | Implements versioned JSON dictionary loading. |
| `DiameterValidator.cs` | Implements command/grouped AVP validation and error-answer construction. |
| `DiameterTcpTransportFactory.cs` | Implements portable TCP listener/connection creation. |
| `DiameterTlsTransportFactory.cs` | Implements TLS-over-TCP listener/connection creation. |
| `DiameterSctpTransportFactory.cs` | Implements conditional platform SCTP and capability probing. |
| `DiameterPeerDiscovery.cs` | Implements static and DNS discovery. |
| `DiameterPeerManager.cs` | Implements peer table, capabilities exchange, state machine, watchdog, failover, and disconnect. |
| `DiameterRouter.cs` | Implements local/agent routing, redirects, priority, load, and policy chains. |
| `DiameterRequestClient.cs` | Implements pending request tracking, answer correlation, timeout, retry, and duplicate integration. |
| `DiameterSessionManager.cs` | Implements authorization session state and storage. |
| `DiameterAccountingApplication.cs` | Implements base accounting client/server state and resilience. |
| `DiameterApplicationHost.cs` | Implements registration, dispatch, interceptors, and lifecycle. |
| `DiameterCreditControlApplication.cs` | Implements RFC 8506 client/server state machines and typed DCCA operations. |
| `DiameterOverloadManager.cs` | Implements DOIC, rate control, agent reports, and load conveyance. |
| `DiameterInMemorySessionStore.cs` | Implements the default session store. |
| `DiameterInMemoryAccountingStore.cs` | Implements volatile pending accounting storage. |
| `DiameterFileAccountingStore.cs` | Implements recoverable file-backed accounting storage. |
| `DiameterInMemoryCreditControlStore.cs` | Implements the default DCCA state store. |
| `DiameterSystemClock.cs` | Implements UTC and monotonic time. |
| `DiameterIdentifierSource.cs` | Implements Hop-by-Hop, End-to-End, Session-Id, and Origin-State-Id generation. |
| `DiameterNode.cs` | Composes and implements the complete node lifecycle API. |

## Planned internal source files

Internal types remain outside `src/Api` and are never public.

| Area | Planned files |
| --- | --- |
| Wire | `src/Wire/NetworkByteOrder.cs`, `src/Wire/MessageEncoder.cs`, `src/Wire/MessageDecoder.cs`, `src/Wire/AvpEncoder.cs`, `src/Wire/AvpDecoder.cs`, `src/Wire/StreamFramer.cs`, `src/Wire/ValueCodecs.cs`, `src/Wire/FilterRuleCodec.cs`. |
| Dictionary | `src/Dictionary/DictionaryBuilder.cs`, `src/Dictionary/DictionaryIndexes.cs`, `src/Dictionary/RuleValidator.cs`, `src/Dictionary/BaseDictionaryFactory.cs`, `src/Dictionary/CreditControlDictionaryFactory.cs`, `src/Dictionary/ExtensionDictionaryFactory.cs`. |
| Transport | `src/Transport/TcpListenerWorker.cs`, `src/Transport/TcpConnectionWorker.cs`, `src/Transport/TlsConnectionWorker.cs`, `src/Transport/SctpConnectionWorker.cs`, `src/Transport/ConnectionQueues.cs`. |
| DNS | `src/Discovery/DnsMessageCodec.cs`, `src/Discovery/DnsClient.cs`, `src/Discovery/DiscoveryCache.cs`, `src/Discovery/SrvSelector.cs`. |
| Peer | `src/Peer/PeerStateMachine.cs`, `src/Peer/PeerConnectionElection.cs`, `src/Peer/CapabilitiesProcessor.cs`, `src/Peer/WatchdogController.cs`, `src/Peer/PeerTable.cs`, `src/Peer/PeerTimers.cs`. |
| Core | `src/Core/BaseCommandFactory.cs`, `src/Core/BaseCommandHandler.cs`, `src/Core/ErrorAnswerFactory.cs`, `src/Core/PendingRequestTable.cs`, `src/Core/DuplicateRequestCache.cs`, `src/Core/BoundedWorkQueue.cs`, `src/Core/TimerScheduler.cs`. |
| Routing | `src/Routing/RouteTable.cs`, `src/Routing/RouteSelector.cs`, `src/Routing/LoopDetector.cs`, `src/Routing/ProxyProcessor.cs`, `src/Routing/RedirectCache.cs`, `src/Routing/PriorityQueue.cs`, `src/Routing/LoadSelector.cs`. |
| Sessions | `src/Sessions/AuthorizationClientStateMachine.cs`, `src/Sessions/AuthorizationServerStateMachine.cs`, `src/Sessions/SessionTimerController.cs`, `src/Sessions/SessionTable.cs`. |
| Accounting | `src/Accounting/AccountingClientStateMachine.cs`, `src/Accounting/AccountingServerStateMachine.cs`, `src/Accounting/AccountingRecordQueue.cs`, `src/Accounting/AccountingRecovery.cs`. |
| Applications | `src/Applications/ApplicationRegistry.cs`, `src/Applications/CommandDispatcher.cs`, `src/Applications/InterceptorPipeline.cs`, `src/Applications/ApplicationWorkQueue.cs`. |
| Credit control | `src/CreditControl/CreditControlClientStateMachine.cs`, `src/CreditControl/CreditControlServerStateMachine.cs`, `src/CreditControl/CreditControlCommandFactory.cs`, `src/CreditControl/QuotaController.cs`, `src/CreditControl/MultipleServiceController.cs`, `src/CreditControl/FinalUnitController.cs`, `src/CreditControl/CreditControlRecovery.cs`. |
| Overload | `src/Overload/DoicProcessor.cs`, `src/Overload/LossAbatement.cs`, `src/Overload/RateAbatement.cs`, `src/Overload/OverloadReportCache.cs`, `src/Overload/LoadReportCache.cs`. |
| Operations | `src/Operations/StatisticsCollector.cs`, `src/Operations/HealthCollector.cs`, `src/Operations/EventPublisher.cs`, `src/Operations/ConfigurationValidator.cs`. |

## Implementation tasks

Tasks are ordered by dependency. Every implementation task includes the required public XML comments and stays within the allowed C# subset. Test tasks cover observable behavior, boundary conditions, failures, and thread-safe lifecycle behavior; they do not add syntax-policy checks or benchmarks.

### Foundation and public model

- [DONE] T001: Create the planned source/test directory layout and a single stable file-scoped namespace scheme for API and internal code.
- [DONE] T002: Implement protocol, result, peer, routing, session, accounting, credit-control, overload, and health enums with explicit int32 values.
- [DONE] T003: Implement constants-only API classes for protocol values, application IDs, command codes, AVP codes, result codes, and vendor IDs from the bundled RFC/IANA snapshots.
- [DONE] T004: Implement the core field-based protocol data classes for messages, AVPs, identities, application identifiers, URIs, and endpoints.
- [DONE] T005: Implement codec/framing result and issue data classes that replace `out` parameters and represent incomplete, invalid, and successful outcomes.
- [DONE] T006: Implement all configuration data classes and eager configuration validation rules, including cross-field transport/security/timer/limit checks.
- [DONE] T007: Implement the direct `System.Exception` public exception types, each with one policy-compliant constructor.
- [DONE] T008: Implement `IDiameterClock`, `DiameterSystemClock`, `IDiameterIdentifierSource`, and `DiameterIdentifierSource` with testable UTC and monotonic time seams.
- [DONE] T009: Add tests for every public enum value, constant, data-class default/field contract, exception constructor, clock behavior, and identifier-source boundary exposed so far.

### Wire framing and codecs

- [DONE] T010: Implement internal network-byte-order reads/writes for signed and unsigned 24-, 32-, and 64-bit wire values using spans.
- [DONE] T011: Implement Diameter header measurement and encoding, including legal R/P/E/T bits, version, 24-bit limits, and identifier fields.
- [DONE] T012: Implement Diameter header decoding with stable offsets and rejection of short, oversized, misaligned, unsupported-version, and reserved-bit inputs.
- [DONE] T013: Implement AVP measurement and encoding with optional Vendor-ID, legal V/M/P bits, exact length, and zero padding.
- [DONE] T014: Implement AVP decoding with vendor-header checks, payload slicing/copy ownership, padding checks, and ordered/repeated AVP preservation.
- [DONE] T015: Implement Integer32, Integer64, Unsigned32, Unsigned64, Float32, Float64, and OctetString value codecs.
- [DONE] T016: Implement UTF8String validation/encoding and DiameterIdentity/realm normalization and comparison rules.
- [DONE] T017: Implement IPv4/IPv6 Address AVP and DiameterURI parse/encode behavior, including scheme, FQDN, port, transport, and protocol parameters.
- [DONE] T018: Implement Diameter Time AVP conversion against the NTP epoch with era/wrap-safe behavior and deterministic boundary handling.
- [DONE] T019: Implement Enumerated, IPFilterRule, and QoSFilterRule value representation and syntax validation without LINQ or regular-expression dependencies in the hot parser.
- [DONE] T020: Implement recursive Grouped AVP encoding/decoding with configurable depth, item-count, and total-byte limits.
- [DONE] T021: Implement complete message encoding to caller spans and owned arrays, including exact written/required sizes.
- [DONE] T022: Implement complete message decoding with strict/permissive profiles and preservation of unknown AVPs.
- [DONE] T023: Implement incremental stream framing for fragmented and coalesced messages, retained remainder data, EOF distinction, and maximum-frame enforcement.
- [DONE] T024: Implement `IDiameterMessageCodec`, `IDiameterAvpCodec`, `DiameterMessageCodec`, and `DiameterAvpCodec` over the internal wire components.
- [DONE] T025: Add exhaustive tests for message header and AVP header flag/length/alignment/padding combinations and all malformed boundary cases.
- [DONE] T026: Add exhaustive round-trip and known-vector tests for every public basic and derived AVP codec, including minimum/maximum signed and unsigned values.
- [DONE] T027: Add grouped-depth/count/size-limit tests, unknown-AVP lossless round trips, invalid UTF-8/identity/URI/address/filter tests, and Time epoch/era tests.
- [DONE] T028: Add incremental framing tests for every split point, multiple frames per read, empty reads, clean/truncated EOF, oversized declarations, and recovery after invalid data.
- [DONE] T029: Add property-based and mutation/fuzz tests under `tests/` proving decoders do not crash, over-read, retain unbounded data, or accept internally inconsistent lengths.

### Dictionary composition and validation

- [DONE] T030: Implement field-based vendor, application, type, enum-value, AVP, command, rule, module, and snapshot data classes.
- [DONE] T031: Implement dictionary indexes and canonical name/numeric key lookup for every definition kind.
- [DONE] T032: Implement transactional module registration and composition with deterministic duplicate acceptance, conflict rejection, and immutable snapshots.
- [DONE] T033: Define and implement the versioned JSON dictionary schema and loader using `System.Text.Json`, including precise source-location errors where available.
- [DONE] T034: Implement built-in RFC 6733 vendors, applications, derived types, enum values, AVPs, commands, and occurrence rules.
- [DONE] T035: Implement built-in RFC 8506 dictionary content covering every AVP, enum, CCR/CCA rule, RAR/RAA interaction, and result code.
- [DONE] T036: Implement built-in dictionary modules for RFC 6737, RFC 7075, RFC 7683, RFC 7944, and RFC 8581–8583.
- [DONE] T037: Implement command validation for header flags, application ID, fixed-head AVPs, required/optional counts, ordering, and unconstrained extension AVPs.
- [DONE] T038: Implement AVP validation for dictionary presence, V/M/P masks, vendor identity, fixed sizes, derived values, enum domains, and nested grouped rules.
- [DONE] T039: Implement structured validation issues and protocol-correct Failed-AVP payload construction for missing, malformed, unsupported, and excess AVPs.
- [DONE] T040: Implement `IDiameterDictionary`, `IDiameterDictionaryLoader`, `IDiameterValidator`, `DiameterDictionary`, `DiameterJsonDictionaryLoader`, and `DiameterValidator`.
- [DONE] T041: Add tests for every public dictionary definition/data field, lookup path, snapshot, registration, composition, conflict, and JSON loader outcome.
- [DONE] T042: Add table-driven tests for every built-in command and grouped AVP rule and every built-in enum/flag/type definition.
- [DONE] T043: Add validation tests for valid permutations and every missing/excess/misordered/unknown-mandatory/invalid-value case, including exact Result-Code and Failed-AVP generation.

### Base commands, errors, and identifiers

- [DONE] T044: Implement Hop-by-Hop and End-to-End identifier generation with startup entropy/time, wrap behavior, and collision avoidance.
- [DONE] T045: Implement Session-Id and Origin-State-Id generation with DiameterIdentity, startup time, sequence, optional correlation text, and restart semantics.
- [DONE] T046: Implement base command factories for CER/CEA, DWR/DWA, DPR/DPA, RAR/RAA, ASR/ASA, STR/STA, and ACR/ACA.
- [DONE] T047: Implement base AVP helpers for origins, destinations, applications, sessions, Route-Record, Proxy-Info, redirects, and experimental results.
- [DONE] T048: Implement Result-Code classification and error-answer construction for every RFC 6733 protocol/transient/permanent failure used by the stack.
- [DONE] T049: Implement E-bit, P-bit, request-bit, Hop-by-Hop/End-to-End copying, Error-Reporting-Host, Error-Message, Experimental-Result, and proxy-error propagation rules.
- [DONE] T050: Add tests for uniqueness/wrap/restart behavior of all public identifiers and exact Session-Id formatting/validation.
- [DONE] T051: Add byte-exact and dictionary-validation tests for every base request/answer factory and every supported error-answer path.

### Transport and security

- [DONE] T052: Implement transport/security metadata and result data plus `IDiameterTransportFactory`, `IDiameterListener`, and `IDiameterConnection` contracts.
- [DONE] T053: Implement bounded connection queues, worker lifecycle, connection ownership, and callback-free blocking send/receive primitives.
- [DONE] T054: Implement TCP active connections for IPv4/IPv6 with timeout, partial write handling, no-delay/keepalive settings, and idempotent close.
- [DONE] T055: Implement TCP listeners for IPv4/IPv6, controlled accept loops, admission handoff, shutdown, and transient accept-failure handling.
- [DONE] T056: Integrate incremental Diameter framing into connection receive workers with clean/truncated close and per-connection limits.
- [DONE] T057: Implement TLS-over-TCP before capabilities exchange with `SslStream`, TLS 1.2+ policy, client/server certificates, hostname checks, and mutual authentication.
- [DONE] T058: Implement `IPeerAdmissionPolicy` and `ICertificatePeerPolicy` hooks and enforce certificate-to-Diameter-identity/realm authorization.
- [DONE] T059: Implement conditional SCTP transport probing and sockets with required protocol identifier, ordered delivery, stream policy, IPv4/IPv6, and clear unsupported-platform outcomes.
- [DONE] T060: Implement external transport integration for host-provided DTLS/SCTP, IPsec-protected, test, or specialized connections.
- [DONE] T061: Add fake/loopback transport tests for all public contracts, partial I/O, queue saturation, simultaneous close, repeated close, timeouts, and shutdown.
- [DONE] T062: Add TCP IPv4/IPv6 integration tests for connect/listen, fragmented/coalesced messages, peer close, limits, and reconnect.
- [DONE] T063: Add TLS integration tests with generated test CAs/certificates for mutual success, hostname/identity mismatch, untrusted chain, expiry, and policy rejection.
- [DONE] T064: Add platform-conditional SCTP integration tests and deterministic unsupported-platform contract tests.

### Discovery, peer table, capabilities, and peer state machine

- [DONE] T065: Implement peer/discovery record, configuration, capability, snapshot, event, and disconnect data classes.
- [DONE] T066: Implement the DNS wire codec and resolver for NAPTR, SRV, A, and AAAA records with UDP truncation fallback, response correlation, name compression, and defensive limits.
- [DONE] T067: Implement RFC 6408 S-NAPTR application/transport matching, SRV fallback, priority/weight choice, A/AAAA expansion, TTL/negative caching, and cache invalidation.
- [DONE] T068: Implement static-first discovery and dynamic peer expiry/admission through `IDiameterPeerDiscovery` and `DiameterPeerDiscovery`.
- [DONE] T069: Implement the peer table for configured/dynamic peers, endpoints, expiry, connection direction, capabilities, state, timers, and counters.
- [DONE] T070: Implement CER creation and inbound CER parsing/validation for identity, realm, addresses, vendor/product/firmware, applications, vendors, and security IDs.
- [DONE] T071: Implement CEA processing, common auth/accounting/vendor-specific application calculation, no-common-application failure, and peer capability snapshots.
- [DONE] T072: Implement simultaneous inbound/outbound connection election and duplicate-connection disposal using RFC 6733 Origin-Host comparison.
- [DONE] T073: Implement the complete RFC 6733 peer state/event/action table for Closed, Wait-Conn-Ack, Wait-I-CEA, Wait-Conn-Ack/Elect, Wait-Returns, R-Open, I-Open, and Closing.
- [DONE] T074: Implement Tc connection retry timers with jitter policy, cause-specific reconnect suppression, dynamic-peer expiry, and safe manual connect/disconnect operations.
- [DONE] T075: Implement Tw idle watchdog scheduling, DWR/DWA handling, pending watchdog detection, suspect behavior, failover, and three-exchange failback stabilization.
- [DONE] T076: Implement DPR/DPA graceful drain and Rebooting, Busy, and Do-Not-Want-To-Talk-To-You behavior.
- [DONE] T077: Implement Origin-State-Id peer restart detection and notification to request/session/application subsystems.
- [DONE] T078: Implement RFC 6737 capabilities update request/answer handling, atomic update of the permitted capability subset, and rejection of in-place security-mechanism changes.
- [DONE] T079: Implement `IDiameterPeerManager`, `IPeerEventHandler`, and `DiameterPeerManager` with snapshots and callbacks outside internal locks.
- [DONE] T080: Add deterministic DNS codec/resolver/cache/priority/weight tests and scripted discovery fallback/expiry tests for every public discovery outcome.
- [DONE] T081: Add CER/CEA byte/validation tests for all capability forms, security negotiation, application intersection, rejection, and capability update.
- [DONE] T082: Add table-driven peer FSM tests for every RFC state/event/action cell, illegal event, timer race, connection election, shutdown, and callback behavior.
- [DONE] T083: Add watchdog/failure tests for idle traffic suppression, missed answers, suspect transition, failover, reconnect, three-success failback, and DPR reconnect rules.

### Request tracking, reliability, routing, and agents

- [DONE] T084: Implement send/answer/pending/duplicate data classes and public request/answer/duplicate-store contracts.
- [DONE] T085: Implement the bounded pending-request table keyed by peer and Hop-by-Hop ID with timeout, cancellation, late answer, and exact cleanup.
- [DONE] T086: Implement inbound answer correlation and validation of request bit, command code, application ID, identifiers, peer/path, and error semantics.
- [DONE] T087: Implement failover eligibility and retransmission with alternate route, new Hop-by-Hop ID, retained End-to-End ID, T bit, and application/session policy.
- [DONE] T088: Implement duplicate-request detection keyed by Origin-Host/End-to-End ID with bounded expiry and cached-answer replay policy.
- [DONE] T089: Implement routing/redirect/translation data classes and `IDiameterRouter`, `IRoutePolicy`, and `ITranslationHandler` contracts.
- [DONE] T090: Implement static route tables by realm/application/host with priorities, weights, defaults, peer capability filtering, health, and snapshots.
- [DONE] T091: Implement local delivery and Destination-Host/Destination-Realm/application routing precedence.
- [DONE] T092: Implement RFC 5729 User-Name realm routing and configurable local/home realm policy.
- [DONE] T093: Implement Route-Record insertion/checking, predictive loop avoidance, path authorization, and loop error answers.
- [DONE] T094: Implement relay request/answer handling with exact Hop-by-Hop transaction/path state and transparent End-to-End preservation.
- [DONE] T095: Implement proxy request/answer handling with Proxy-Info/Proxy-State insertion, local state protection, ordered removal, and error traversal.
- [DONE] T096: Implement redirect answer generation, all Redirect-Host-Usage scopes, Redirect-Max-Cache-Time, cache precedence/expiry, security validation, and loop prevention.
- [DONE] T097: Implement RFC 7075 realm-based redirect application and its cache/routing interaction.
- [DONE] T098: Implement translation-agent dispatch and explicit translated/untranslated/error outcomes without hiding end-to-end application semantics.
- [DONE] T099: Implement RFC 7944 DRMP handling and stable priority queues with starvation prevention.
- [DONE] T100: Implement route policy chains, candidate scoring, deterministic tie breaking, session affinity, and safe short-circuit decisions.
- [DONE] T101: Implement `IDiameterRequestClient`, `IDiameterAnswerHandler`, `DiameterRequestClient`, and integration with peer/routing/application subsystems.
- [DONE] T102: Add exhaustive pending-request and answer-correlation tests for success, errors, mismatches, timeout, cancellation, late answers, peer loss, retransmission, and cleanup.
- [DONE] T103: Add duplicate-cache tests for collision keys, expiry, capacity, replay safety, retransmission indication, and application-visible metadata.
- [DONE] T104: Add table-driven routing tests for every local/relay/proxy/redirect/translation branch, route precedence, peer capability, failover, loop, and unable-to-deliver outcome.
- [DONE] T105: Add redirect scope/cache/security/expiry tests, RFC 7075 tests, RFC 5729 tests, Proxy-Info/Route-Record path tests, and DRMP queue tests.

### Authorization sessions and accounting

- [DONE] T106: Implement session configuration, record, update, snapshot, and event data plus session manager/store/event contracts.
- [DONE] T107: Implement in-memory optimistic-version session storage and atomic session-table operations.
- [DONE] T108: Implement stateful authorization client state machine, including initial authorization, multi-round exchange, lifetime/grace/session timers, re-auth, termination, abort, and failure.
- [DONE] T109: Implement stateful authorization server state machine, including server-initiated re-auth/abort, timer enforcement, and cleanup.
- [DONE] T110: Implement stateless authorization operation and ensure no residual session state after completion.
- [DONE] T111: Implement Session-Binding, Session-Server-Failover, route affinity, peer loss/restart behavior, Origin-State-Id inference, and explicit session release.
- [DONE] T112: Implement `IDiameterSessionManager`, `IDiameterSessionStore`, `ISessionEventHandler`, `DiameterSessionManager`, and `DiameterInMemorySessionStore`.
- [DONE] T113: Implement accounting request/answer/record/result data and accounting client/handler/store contracts.
- [DONE] T114: Implement accounting client state for EVENT and START/INTERIM/STOP sequences, record numbering, intervals, sub-sessions, multi-sessions, and correlation.
- [DONE] T115: Implement accounting server validation, duplicate detection, ordered processing, answer generation, and application handoff.
- [DONE] T116: Implement Accounting-Realtime-Required policies, failover/retransmission, pending record retention, interim overwrite, STOP durability, and recovery.
- [DONE] T117: Implement in-memory and atomic file-backed accounting stores with corruption detection and restart replay.
- [DONE] T118: Implement `DiameterAccountingApplication` over the base application framework and advertise base accounting capability.
- [DONE] T119: Add client/server authorization session tests for every RFC transition, timer, stateful/stateless mode, re-auth, abort, termination, peer restart/loss, failover, and store conflict.
- [DONE] T120: Add accounting tests for every record sequence, interval, correlation identifier, duplicate, retransmission, failure policy, interim overwrite, file recovery, and public store outcome.

### Application extension framework

- [DONE] T121: Implement application registration, command registration, request context, result, and interceptor context data classes.
- [DONE] T122: Implement the application/handler/interceptor contracts with unique method names and no delegate or generic API requirements.
- [DONE] T123: Implement atomic application registration/removal keyed by application/command/direction with deterministic conflict checks and capability updates.
- [DONE] T124: Implement raw and validated inbound dispatch, request context construction, local answer validation, and protocol-error fallback.
- [DONE] T125: Implement ordered inbound/outbound interceptor pipelines with continue, replace, answer, drop, redirect, and fail outcomes.
- [DONE] T126: Implement bounded per-application work queues, configured concurrency, exception isolation, overload response, and callback execution outside core locks.
- [DONE] T127: Implement lifecycle start, graceful unregister/drain, forced stop, and node shutdown ordering for application modules.
- [DONE] T128: Implement typed-adapter support through dictionary lookup/value codecs while preserving raw unknown AVPs for future/vendor applications.
- [DONE] T129: Implement `IDiameterApplicationHost`, `IDiameterApplication`, `IDiameterRequestHandler`, `IMessageInterceptor`, and `DiameterApplicationHost`.
- [DONE] T130: Add tests for every public registration/dispatch/interceptor/lifecycle outcome, conflict, capability update, handler exception, queue saturation, unregister race, and raw/validated adapter path.
- [DONE] T131: Add a test-only sample vendor application and dictionary proving that new AVPs, grouped rules, commands, client/server handling, sessions, and capability advertisement require no core changes.

### Credit-Control Application

- [DONE] T132: Implement all RFC 8506 field-based DCCA request, answer, session, result, unit, multi-service, pool, final-unit, cost, subscription, and equipment data classes.
- [DONE] T133: Implement `IDiameterCreditControlClient`, `ICreditControlRequestHandler`, `ICreditControlStore`, and `ICreditControlEventHandler` contracts.
- [DONE] T134: Implement CCR/CCA factories/parsers for INITIAL, UPDATE, TERMINATION, and EVENT with Application-ID 4, Session-Id, CC-Request-Type, and CC-Request-Number rules.
- [DONE] T135: Implement DCCA client session state, Tx timer, monotonic request numbers, pending interrogation state, persistence, and terminal cleanup.
- [DONE] T136: Implement DCCA server session state, handler/rating decisions, duplicate and out-of-order detection, idempotent answer replay, and persistence.
- [DONE] T137: Implement requested/granted/used service units for time, money, octets, and service-specific units with partial grants and exact unsigned arithmetic checks.
- [DONE] T138: Implement Multiple-Services-Credit-Control, Service-Identifier, Rating-Group, pooled units, G-S-U references, independent quota state, and Multiple-Services-Indicator.
- [DONE] T139: Implement Validity-Time, quota thresholds/exhaustion, intermediate interrogation scheduling, Tariff-Time-Change, Tariff-Change-Usage, and final usage reports.
- [DONE] T140: Implement server-initiated credit re-authorization over RAR/RAA and coordinate it with base authorization session state.
- [DONE] T141: Implement Final-Unit-Indication terminate, redirect, and restrict-access actions, including filters, legacy/extension redirect forms, QoS indication, and graceful service termination.
- [DONE] T142: Implement one-time price enquiry, balance check, direct debit, and refund operations with Requested-Action and no retained server session state.
- [DONE] T143: Implement Credit-Control-Failure-Handling and Direct-Debiting-Failure-Handling behavior for terminate/continue/retry-and-terminate policies.
- [DONE] T144: Implement CC-Session-Failover negotiation, alternate server selection, pending transaction recovery, duplicate protection, route affinity update, and failback.
- [DONE] T145: Implement cost/unit-value/exponent/currency processing with overflow-safe representation and no floating-point use for monetary decisions.
- [DONE] T146: Implement subscription identifiers, user-equipment information and extensions, CC-Correlation-Id/sub-session, service parameters, and service-specific rating input pass-through.
- [DONE] T147: Implement privacy-sensitive AVP classification, configurable minimization/redaction hooks, and agent handling that preserves required routing semantics.
- [DONE] T148: Implement `DiameterInMemoryCreditControlStore` and `DiameterCreditControlApplication` and advertise/update DCCA capability.
- [DONE] T149: Add table-driven dictionary/codec/validation tests for every RFC 8506 AVP, enum value, result code, grouped rule, and CCR/CCA occurrence rule.
- [DONE] T150: Add client/server DCCA state-machine tests for every RFC 8506 transition, timer, request type, duplicate, out-of-order request, handler result, and cleanup path.
- [DONE] T151: Add multi-service, pooled-unit, tariff-change, validity, quota exhaustion, re-authorization, and all final-unit-action tests.
- [DONE] T152: Add one-time event tests for price, balance, debit, refund, unsupported action, failure handling, duplicate protection, and stateless cleanup.
- [DONE] T153: Add CCFH/DDFH, peer loss, failover/failback, persisted recovery, route change, and privacy/redaction tests for all public DCCA outcomes.

### Overload and load extensions

- [DONE] T154: Implement overload/load configuration, report, admission request/decision data and overload manager/source contracts.
- [DONE] T155: Implement RFC 7683 OC-Supported-Features negotiation and correct insertion/removal/forwarding rules for overload AVPs.
- [DONE] T156: Implement host/realm/application overload report cache, sequence comparison, validity expiry, report scope, and loss-abatement admission decisions.
- [DONE] T157: Implement RFC 8581 peer overload reports and Diameter agent aggregation/forwarding behavior.
- [DONE] T158: Implement RFC 8582 rate-abatement token scheduling with monotonic time and explicit excess-request decisions.
- [DONE] T159: Implement RFC 8583 load report creation/cache/expiry and load-aware route candidate scoring independent of overload.
- [DONE] T160: Integrate overload admission with application queues, request origination, routing/failover, DIAMETER_TOO_BUSY, and watchdog isolation.
- [DONE] T161: Implement `IDiameterOverloadManager`, `IOverloadSource`, and `DiameterOverloadManager`.
- [DONE] T162: Add RFC example and boundary tests for feature negotiation, report scopes/sequences/expiry, loss/rate abatement, peer reports, load routing, agent handling, and all public admission decisions.

### Node composition, operations, and resilience

- [DONE] T163: Implement statistics, health, subsystem health, and operational event data classes plus event/redaction contracts.
- [DONE] T164: Implement thread-safe counters and immutable snapshots for all protocol, peer, request, route, session, accounting, DCCA, overload, load, queue, and store metrics.
- [DONE] T165: Implement structured event publication with redaction, sink-failure isolation, ordering guarantees per source, and no callbacks under internal locks.
- [DONE] T166: Implement health aggregation for listeners, peers, routes, queues, timers, stores, overload state, and recent failures.
- [DONE] T167: Implement node composition/configuration validation and dependency wiring for built-in or caller-supplied transports, policies, stores, handlers, clock, identifiers, and event sink.
- [DONE] T168: Implement idempotent configure/start/drain/stop lifecycle, runtime-safe peer/route/dictionary/application changes, and deterministic worker/store/connection shutdown.
- [DONE] T169: Implement `IDiameterNode` and `DiameterNode` as the single high-level facade while retaining direct subsystem interfaces for advanced hosts.
- [DONE] T170: Add node lifecycle tests for partial startup failure, repeated calls, drain with in-flight work, forced stop, callback reentrancy, concurrent configuration changes, and resource cleanup.
- [DONE] T171: Add statistics/event/redaction/health tests covering every public field and operation, sink failure, snapshot consistency, and subsystem degradation/recovery.

### End-to-end and public API coverage

- [DONE] T172: Build an in-process two-node test harness with controllable transport fragmentation, loss, delay, duplicate delivery, reconnect, time, DNS, and stores.
- [DONE] T173: Add end-to-end TCP and TLS tests for capabilities exchange, watchdogs, request/answer, disconnect, reconnect, failover/failback, and capability update.
- [DONE] T174: Add end-to-end client/server tests for stateful/stateless authorization, server re-auth, abort, termination, accounting, and peer restart inference.
- [DONE] T175: Add end-to-end relay, proxy, redirect, realm-redirect, translation, routing priority, loop, overload, and load-aware routing topologies.
- [DONE] T176: Add end-to-end DCCA client/server tests for complete session, multi-service, event, final-unit, failure, failover, duplicate, and recovery sequences from RFC 8506 Appendix A.
- [DONE] T177: Add interoperability tests under `tests/` against a pinned freeDiameter container/process for CER/CEA, DWR/DWA, DPR/DPA, relay routing, base accounting, raw vendor AVPs, and DCCA CCR/CCA.
- [DONE] T178: Add parser interoperability corpus tests using captured/generated valid messages from freeDiameter, jDiameter-compatible dictionaries, and Open5GS application-style vendor/grouped AVPs without vendoring their production code.
- [DONE] T179: Audit every public interface method, implementation class, data field, enum member, constant, exception, success result, and failure result in `src/Api`, and add any missing behavioral test so the complete exposed API is covered.
- [DONE] T180: Run the full test suite with coverage collection, close every uncovered reachable branch in `src/Api`, and verify that only deliberate runtime/platform guards remain unreachable on the current host.
