# Universal SIP signaling library development plan

## Goal and scope

Build a transport-independent, client-and-server SIP signaling library for .NET 10. The library will implement the protocol, transaction, dialog, registration, proxy, authentication, extension, and high-level dialog-usage layers defined by RFC 3261, RFC 3263, RFC 7616, and RFC 8760, including the corrections and commonly deployed extensions listed below.

The library boundary ends at signaling. It will parse and generate SDP, MIME bodies, presence documents, and other SIP payloads and will coordinate offer/answer state, but it will not implement RTP/RTCP, codecs, mixers, capture/playback, ICE connectivity checks, TURN, or media security. Applications may pass opaque bodies and integrate their own media stack.

`src/` will use only built-in .NET APIs. In particular, networking will use `System.Net`, `System.Net.Sockets`, `System.Net.Security`, and `System.Net.WebSockets`; cryptography will use `System.Security.Cryptography`; text and binary processing will use `System.Text`, `System.Buffers`, `System.IO`, and span APIs; XML event-package bodies will use built-in XML APIs. External packages may be used only by `tests/`.

## Design constraints from `syntax.md`

- The public surface is method-and-field based: data classes expose fields, contracts expose uniquely named instance methods, and public implementation classes implement those contracts.
- Public operations are never static. Constants are fields on static data classes.
- Public APIs do not use properties, events, delegates, overloads, generic declarations, optional/named/ref/out/in/params arguments, value-type nullable forms, async/await, tuples, records, structs, LINQ, or inheritance between library classes.
- Potentially blocking network work is exposed as synchronous operations plus explicit polling, deadlines, cancellation data, and event/result queues. Internals may use dedicated threads and built-in concurrent collections without leaking forbidden syntax into `src/**/*.cs`.
- Expected parse and lookup failures have result-returning APIs; convenience operations may throw a dedicated exception. Result classes replace `TryX(..., out ...)` signatures.
- Span-based internal parsing and formatting should avoid unnecessary copies. Public data remains stable reference types and arrays/lists because custom `ref` and generic APIs are forbidden.
- Every public data class is consumed by a contract or another data class; every public enum is consumed by a contract or data class; every public implementation class has one constructor and exposes only interface-declared methods.

## Research-derived architecture

The plan deliberately combines the strongest recurring boundaries from the researched stacks:

- PJSIP separates transport, endpoint/module dispatch, transactions, dialogs, authentication, proxying, registration, invite sessions, event usages, and message-body handling. Its capability matrix also highlights rport, Path/Service-Route, Outbound, PRACK, UPDATE, REFER/Replaces, session timers, events, PUBLISH, MESSAGE, multipart, torture tests, and modern digest algorithms.
- reSIProcate separates a transport/transaction core from the Dialog Usage Manager and from the repro proxy/registrar. That separation is retained so applications can use the codec or transaction layer without adopting automatic dialogs, and can compose server roles independently.
- Sofia-SIP's conformance matrix distinguishes syntax-level support from automatic protocol behavior. This plan does the same: unknown extension syntax is always preserved, while automatic behaviors are implemented by explicit dialog-usage components.
- JAIN SIP's provider/listening-point, transaction, dialog, factory, and listener concepts motivate a transport-neutral endpoint and an explicit event pump. This plan uses result/event data instead of callbacks so it fits the restricted C# subset.
- Unlike older implementations, SHA-256 and SHA-512/256 digest negotiation, RFC 6026 INVITE transaction corrections, RFC 6665 event corrections, strict framing limits, and IPv6 torture cases are baseline behaviors rather than compatibility add-ons.

Local research snapshots are under `specs/research/`; authoritative RFC text is under `specs/rfc/`.

### Downloaded reference corpus

- Required foundation: RFC 3261 (core SIP), RFC 3263 (server location), RFC 7616 (modern HTTP Digest), and RFC 8760 (SIP digest update).
- Syntax, DNS, bodies, and SDP: RFC 2045, 2046, 2782, 3403, 3404, 3420, 3863, 3966, 5621, 5888, and 8866.
- Core corrections and conformance: RFC 4320, 4321, 4475, 5057, 5118, 6026, 6141, and 6228.
- Methods and dialog usages: RFC 2976, 3262, 3264, 3311, 3428, 3515, 3680, 3842, 3856, 3857, 3858, 3891, 3892, 3903, 3994, 4028, 4235, 4480, 4488, 4662, 4916, 5360, 5367, 6086, 6665, and 7989.
- Routing, identity, NAT, and capabilities: RFC 3323, 3325, 3326, 3327, 3581, 3608, 3840, 3841, 5626, 5627, 5630, 5768, 5806, 6442, 6809, 7044, 8224, 8225, 8599, and 8840.
- Transport and security extensions: RFC 3320, 3329, 3485, 4077, 4168, 5922, 5923, 7118, and 8898.
- Library references: the current PJSIP SIP capability matrix and developer guide; reSIProcate's stack/DUM/repro descriptions; Sofia-SIP's feature conformance matrix; and the NIST JAIN SIP reference implementation overview. These are comparative inputs, not normative specifications.

Every listed RFC is stored as `specs/rfc/rfcNNNN.txt`. The comparative snapshots retain their source-specific names under `specs/research/` so the feature analysis can be reproduced without relying on a changing web page.

## Standards and feature inventory

### Core syntax, messages, and extensibility

- RFC 3261 request/status lines, all core methods, response classes and status codes, header grammar, compact header names, whitespace folding compatibility, quoted strings, comments where legal, tokens, host/port syntax, IPv4/IPv6 references, and binary-safe message bodies.
- Incremental parsing from datagrams and streams; exact Content-Length framing; concatenated stream messages; incomplete/error/complete parse results; configurable limits for start line, header count, header line, aggregate headers, body, and whole message.
- Canonical serialization and loss-preserving forwarding: original order, duplicate headers, unknown methods, unknown status codes, unknown headers, unknown URI schemes, unknown parameters, and opaque body bytes survive parse/serialize cycles.
- Comma-combined versus non-combinable headers; long and compact names; case-insensitive names and tokens while preserving user-facing spelling and quoted content.
- Typed codecs for Via, Route, Record-Route, Contact, From, To, Call-ID, CSeq, Max-Forwards, Content-Length, Content-Type, Content-Disposition, Accept, Allow, Supported, Require, Proxy-Require, Unsupported, Date, Retry-After, Warning, Server, User-Agent, Expires, Min-Expires, and Retry-After.
- Typed extension-header codecs for Authorization challenge/credentials, Authentication-Info, Path, Service-Route, RSeq, RAck, Session-Expires, Min-SE, Reason, Event, Allow-Events, Subscription-State, SIP-ETag, SIP-If-Match, Refer-To, Referred-By, Refer-Sub, Replaces, Join, Target-Dialog, Accept-Contact, Reject-Contact, Request-Disposition, Feature-Caps, Privacy, P-Asserted-Identity, P-Preferred-Identity, History-Info, Session-ID, Geolocation, Resource-Priority, and push/outbound/GRUU parameters.
- Generic extension registry and generic typed parameter/address/header helpers so unsupported future extensions remain usable without changing the core.
- `sip:`, `sips:`, and `tel:` URI parsing/formatting, URI parameters and headers, escaping/unescaping, canonical comparison, default ports, password/user-info handling, transport/maddr/ttl/user/lr/gr/ob parameters, telephone number normalization, and RFC 3261 URI equivalence rules.
- MIME Content-Type and Content-Disposition, multipart bodies with nested parts and boundary safety, `message/sipfrag`, opaque bodies, and RFC 5621 body-disposition handling.
- RFC 8866 SDP parsing/serialization for session lines, media lines, connections, bandwidths, timings, attributes, unknown extensions, IPv4/IPv6, grouping/mid, RTCP, ICE-related attributes as data, and loss-preserving round trips.
- XML codecs for PIDF presence, rich-presence extension preservation, watcher information, dialog-info, reginfo, message-summary, and is-composing payloads; secure XML settings prohibit DTD/external-entity resolution.
- RFC 4475 and RFC 5118 torture-message compatibility, including legal edge cases, rejection of invalid framing, and deterministic error locations.

### Via, routing, and Contact behavior

- Client branch generation with the `z9hG4bK` magic cookie and cryptographically random collision-resistant tokens.
- Via insertion/removal, received parameter, RFC 3581 `rport`, multicast `maddr`/`ttl`, sent-by validation, response destination selection, transport aliases, and response routing over the originating reliable connection.
- Loose routing and legacy strict-routing transformations; Route consumption; Record-Route insertion; route-set construction and direction reversal; remote-target refresh from Contact only when allowed.
- Contact wildcard rules, multiple contacts, q-values, expires parameters, instance/reg-id/ob/pub-gruu/temp-gruu parameters, Contact comparison, NAT observed-address metadata, and SIPS downgrade prevention.
- Max-Forwards insertion/decrement/483 behavior, loop/spiral detection, merged-request detection, request-target rewriting, and outbound proxy precedence.

### DNS and target selection (RFC 3263)

- Numeric address, explicit port, transport parameter, `sip`/`sips`, and no-explicit-transport decision paths.
- NAPTR discovery and order/preference selection for supported SIP services, SRV lookup and RFC 2782 priority/weight ordering, A/AAAA lookup, default ports/transports, and secure-transport constraints.
- Built-in DNS wire client for NAPTR/SRV/A/AAAA because .NET does not expose all required record types uniformly; UDP with truncation retry over TCP, compression-pointer validation, transaction matching, response-code handling, and bounded parsing.
- TTL and negative caching, expiry, cache flushing, resolver timeouts/retries, multiple name servers, CNAME following with loop/depth limits, deterministic injected randomness for tests, IPv4/IPv6 candidate interleaving, failover, and transport-error feedback.
- Optional ENUM-to-SIP resolution as a high-level routing extension using NAPTR while keeping RFC 3263 target resolution independent.

### Transport integration

- Transport-neutral received/sent message envelopes containing local/remote endpoints, selected transport, connection/flow identity, security metadata, timestamp, and parse outcome.
- UDP over IPv4/IPv6, datagram size controls, one-message-per-datagram validation, source/destination metadata, and safe handling of truncation/fragmentation risk.
- TCP listeners and clients, incremental Content-Length framing, multiple messages per connection, partial reads/writes, connection pooling, idle expiry, keepalive, retry/failover rules, and the RFC 3261 oversized-UDP switch to congestion-controlled transport.
- TLS listeners and clients using `SslStream`, SIPS port/transport behavior, current TLS defaults, server-name validation, certificate chain and usage validation, mutual TLS, application certificate policy, peer identity metadata, and RFC 5922 domain certificate matching.
- RFC 5923 connection reuse and alias handling with authenticated peer/target safeguards.
- RFC 7118 SIP over WebSocket client/server integration, `sip` subprotocol negotiation, text/binary frame handling, fragmentation assembly, one SIP message per WebSocket message, and origin/size policy hooks.
- RFC 5626 Outbound flows, flow tokens, reg-id and instance-id, keepalives, Flow-Timer, flow failure, primary/backup flow selection, Path integration, and connection-oriented flow reuse.
- CRLF keepalive sender/receiver and RFC 3581 response symmetry. Pluggable custom transports allow SCTP, DTLS, QUIC, in-process, or host-specific transports without adding dependencies to `src/`.
- Explicit event-pump lifecycle: bind/connect/send/receive/poll/close, deadlines, cancellation requests, backpressure/queue limits, connection state changes, and transport failure reporting.

### Transactions (RFC 3261 plus corrections)

- INVITE client transaction states Calling/Proceeding/Completed/Terminated with Timers A, B, and D; transport-error handling; provisional/final response delivery; retransmission matching; and ACK construction for non-2xx final responses.
- INVITE server transaction states Proceeding/Completed/Confirmed/Terminated with Timers G, H, and I; provisional/final response retransmission; ACK matching; and reliable-transport behavior.
- Non-INVITE client states Trying/Proceeding/Completed/Terminated with Timers E, F, and K and RFC 4320 response/retransmission behavior.
- Non-INVITE server states Trying/Proceeding/Completed/Terminated with Timer J, retransmission response replay, and reliable-transport behavior.
- RFC 6026 accepted-state handling with Timer M on the client side and Timer L on the server side, transaction-user retransmission of INVITE 2xx responses until ACK, and distinct end-to-end ACK for 2xx responses.
- Transaction keys, magic-cookie and RFC 2543 compatibility matching, CANCEL matching, ACK special cases, branch/sent-by/method rules, merged request detection, retransmission absorption, and transport affinity.
- Stateless request and response forwarding paths alongside stateful operations.
- Configurable T1/T2/T4 and timer scheduler based on monotonic time; explicit time advancement/test clock integration; transaction limits, cleanup, snapshots, and events.
- Transport failover only where allowed: alternate targets before a response, no unsafe failover after a transaction is established, and correct Via/branch regeneration for a new attempt.

### Dialogs and dialog usages

- Dialog identification by Call-ID/local-tag/remote-tag; UAC/UAS roles; early, confirmed, and terminated states; local/remote sequence numbers; local/remote URIs; secure flag; route set; remote target; and dialog lifetime.
- Dialog creation from provisional and 2xx responses, forked early dialogs, multiple 2xx responses, independent ACK/BYE cleanup, early-dialog replacement, and RFC 5057 multiple-usage isolation.
- In-dialog request construction and validation, monotonically increasing local CSeq, remote CSeq ordering, target-refresh methods, strict/loose route application, authentication reuse, and route-set immutability.
- INVITE usage with incoming/outgoing INVITE, provisional responses, reliable provisional responses and PRACK/RSeq/RAck, CANCEL, ACK, BYE, redirect following, re-INVITE, UPDATE, hold/resume signaling, overlapping request/glare handling, and RFC 6141 target refresh rules.
- RFC 3264 offer/answer state over opaque or SDP bodies: one outstanding offer, offerless INVITE, answer placement, reliable provisional interactions, UPDATE/re-INVITE exchanges, rollback on failure, and application-controlled media decisions.
- RFC 4028 session timers: Supported/Require negotiation, refresher selection, Min-SE/422 retry, UPDATE or re-INVITE refresh, expiry events, and proxy Session-Expires behavior.
- RFC 6228 `199 Early Dialog Terminated`, RFC 4916 connected identity, RFC 7989 Session-ID, Reason propagation, and dialog event reporting.
- Back-to-back dialog composition that keeps leg identifiers, routing, authentication, CSeq, Contact, SDP, and transaction state independent; application policy decides message/body mapping.

### Digest and token authentication

- RFC 7616/RFC 8760 client support for multiple WWW-Authenticate and Proxy-Authenticate challenges, realm selection, strongest-supported algorithm selection, independent origin/proxy credential scopes, challenge caching, stale retry, nextnonce, Authentication-Info/rspauth verification, and bounded retry loops.
- MD5 and MD5-sess interoperability plus SHA-256, SHA-256-sess, SHA-512/256, and SHA-512/256-sess using built-in cryptography; MD5 is compatibility-only and never preferred over a supported stronger challenge.
- `qop=auth` and `qop=auth-int`, nonce count, cnonce, algorithm token validation, charset UTF-8, userhash, quoted escaping, request-target digest input, and body integrity.
- Server challenges for 401/407, multiple algorithms ordered strongest first, opaque values, domain parameter, stale indication, credential verification, Authentication-Info, and proxy/origin separation.
- Cryptographically random, time-bounded, tamper-evident nonces; replay detection by nonce/user/cnonce/nc; nonce expiry; clock skew; realm isolation; constant-time digest comparison; and credential/secret clearing where practical.
- RFC 8898 Bearer token parsing/generation and a policy/provider abstraction for external token validation. The library will not implement an OAuth authorization server.
- Mutual TLS identity and trusted-peer authentication usable by proxy/registrar policy.

### Registration and location service

- Registration client state machine: initial registration, 401/407 retry, 3xx handling policy, 423/Min-Expires retry, refresh scheduling, jitter, contact matching, multiple bindings, explicit unregister, wildcard unregister, transport/flow failure recovery, and shutdown behavior.
- Registrar validation: To/AOR rules, Contact requirements, wildcard constraints, Call-ID/CSeq ordering, per-contact or header Expires, min/max/default expiry, zero-expiry removal, atomic updates, retransmission idempotence, 200 response contact list, Date, and supported extension advertisement.
- Location service query/update/remove/expire operations with pluggable application storage, deterministic binding ordering, q-values, received/path/service-route/flow metadata, instance/reg-id uniqueness, and expiration events.
- RFC 3327 Path, RFC 3608 Service-Route, RFC 5626 Outbound, RFC 5627 GRUU, RFC 8599 push notification parameters, and RFC 3680 registration event generation.
- Registrar authentication/authorization hooks, domain ownership policy, third-party registration policy, SIPS/contact security validation, binding and request limits, and privacy-safe observable events.

### Proxy, redirect, and server roles

- Stateless proxy validation/forwarding and stateful proxy transactions for every method; Via/Record-Route/Max-Forwards processing; Route and strict-route handling; response Via removal; rport; and transport/DNS integration.
- Sequential and parallel forking, location lookup, q-value grouping, redirect target recursion policy, per-branch cancellation, late 2xx forwarding, multiple 2xx handling, best-final-response selection, 6xx behavior, provisional response forwarding and Timer C resets, branch timeout, and aggregate completion.
- CANCEL generation/forwarding and 200/487 behavior; ACK forwarding for non-2xx and stateless forwarding of ACK for 2xx; no transaction creation for ACK; branch correlation and cleanup.
- Record-routing policy, SIPS preservation, Path and Service-Route interoperability, outbound flow routing, connection reuse, topology/application header policy, and trusted-network identity/privacy policy.
- Authentication at proxy and registrar, authorization policy, request/response header sanitation, malformed-message rejection, overload response policy, loop/spiral/merged-request controls, and rate/size/concurrency limits.
- Redirect server producing policy-selected 3xx contacts with q/expires and preserving correct response headers.
- OPTIONS capability server and generic UAS dispatch for extension methods and application-generated provisional/final responses.
- B2BUA building blocks, but no implicit topology hiding, billing, media relay, or call-control policy.

### High-level user-agent operations and extensions

- Endpoint combines transports, resolver, transaction layer, dialog layer, authentication, and an event queue while preserving the ability to use each lower layer independently.
- Generic UAC request sending and UAS request handling, automatic mandatory headers, capability negotiation, extension dispatch, request cancellation, response generation, and typed state snapshots.
- INVITE session API for calls without media; OPTIONS client/server; pager-mode MESSAGE client/server; INFO package framework; REFER with implicit subscription or `norefersub`; blind and attended transfer through Replaces; Join and Target-Dialog correlation.
- RFC 6665 SUBSCRIBE/NOTIFY client and server usages: initial/in-dialog subscription, forked subscriptions, Event/id matching, expires/423, Subscription-State/reason/retry-after, refresh, termination, NOTIFY-before-200 race, version/order checks, and event package registration.
- RFC 3903 PUBLISH client/server: ETag/If-Match, initial/modify/refresh/remove, 412/423 recovery, expiration, conditional updates, and publication store abstraction.
- Built-in event packages/codecs for presence/PIDF, watcher-info, dialog-info, registration state, message summary, and resource-list aggregation while allowing custom packages and opaque bodies.
- Caller preferences/callee capabilities, Feature-Caps, Allowed/Supported/Require/Unsupported negotiation, and method/extension capability discovery.
- Privacy, asserted/preferred identity, History-Info, connected identity, Geolocation, Resource-Priority, SIP Identity/PASSporT field preservation and verification hooks; full certificate acquisition and STIR/SHAKEN policy stays application-provided.
- SIP push parameters, Outbound/GRUU helpers, ICE/trickle-ICE option tags and SDP attribute preservation, and SigComp negotiation metadata. SigComp compression is an optional pluggable transport-content coding rather than a baseline built-in compressor.
- Unknown method, header, option tag, event package, content type, and URI scheme pass-through is mandatory, ensuring extensions not explicitly modeled remain usable.

### Reliability, safety, and observability

- All parsers are bounded, non-recursive where practical, integer-overflow checked, and resistant to request smuggling, conflicting Content-Length, malicious DNS compression pointers, MIME boundary abuse, header bombs, and XML external entities.
- Cryptographic tokens use `RandomNumberGenerator`; secrets and authorization values are redacted from default diagnostic events; comparisons use fixed-time operations where applicable.
- Deterministic state snapshots and structured events expose transitions, timers, network metadata, errors, and correlation IDs without requiring callbacks or public events/delegates.
- Thread-safe endpoint, transaction, dialog, registrar, and proxy state; defined ownership/copying of mutable byte arrays and lists; no application callback while holding an internal lock.
- Configurable limits and graceful overload behavior for messages, transactions, dialogs, bindings, branches, connections, queues, DNS work, authentication attempts, and XML/MIME depth.
- Interoperability fixtures cover canonical RFC examples, malformed/adversarial traffic, packet fragmentation, connection loss, timer races, fork races, and cross-role end-to-end scenarios.

## Proposed public API

Names below define coarse responsibilities, not final method signatures. Each contract will use uniquely named methods and request/result/event data classes so no overloads, delegates, properties, or `out` parameters are needed.

### `src/Api/Contracts`

- `ISipUriCodec`: parse, validate, compare, normalize, and format SIP/SIPS/TEL URIs.
- `ISipHeaderCodec`: parse and format core and extension header values through typed result data.
- `ISipMessageCodec`: incremental/datagram parse, validate, measure, and serialize SIP messages.
- `ISipMimeCodec`: parse and format MIME entities, multipart bodies, and sipfrag.
- `ISipSdpCodec`: parse, validate, and format SDP without media transport behavior.
- `ISipXmlBodyCodec`: secure parse/format for supported SIP XML body families.
- `ISipDnsResolver`: RFC 3263 and ENUM resolution, cache control, and resolution events.
- `ISipTransport`: bind/connect/send/poll/close for one transport implementation.
- `ISipTransportManager`: register transports, select targets, manage flows/connections, send, and poll transport events.
- `ISipCredentialProvider`: retrieve origin/proxy credentials for a realm and target.
- `ISipTokenValidator`: validate external bearer tokens and return identity/scope data.
- `ISipCertificatePolicy`: approve/reject peer certificate and SIP domain identity data.
- `ISipAuthenticationClient`: select challenges, authorize requests, and verify Authentication-Info.
- `ISipAuthenticationServer`: create challenges, verify credentials, manage nonces, and emit authentication results.
- `ISipTransactionLayer`: create/send/receive/cancel/terminate transactions, advance timers, and poll events/snapshots.
- `ISipDialogLayer`: create/find/update/terminate dialogs, construct in-dialog requests, and poll dialog events.
- `ISipOfferAnswerSession`: coordinate offers, answers, rollbacks, and current negotiated body state.
- `ISipInviteSession`: originate/accept/progress/answer/cancel/ack/update/refresh/terminate INVITE usages and poll events.
- `ISipRegistrationClient`: register/refresh/unregister/recover and poll registration state.
- `ISipLocationService`: query/update/remove/expire registration and publication records.
- `ISipRegistrar`: validate and process REGISTER requests and expire bindings.
- `ISipRoutePolicy`: choose proxy, redirect, registrar, and B2BUA routing decisions.
- `ISipAuthorizationPolicy`: authorize operations after identity establishment.
- `ISipProxy`: process requests/responses, fork/cancel branches, and poll proxy events.
- `ISipRedirectServer`: process a request into a standards-compliant redirect response.
- `ISipSubscriptionClient`: create/refresh/terminate subscriptions and consume NOTIFY.
- `ISipSubscriptionServer`: accept/reject subscriptions, send NOTIFY, and expire usages.
- `ISipPublicationClient`: create/modify/refresh/remove publications.
- `ISipPublicationServer`: validate conditional publication operations and expire state.
- `ISipMessageClient`: send pager-mode MESSAGE and correlate responses.
- `ISipInfoPackage`: validate/handle one INFO package without callback delegates.
- `ISipOptionsService`: send OPTIONS and answer capability queries.
- `ISipBackToBackSession`: create/manage two independent dialog legs and expose mapping decisions.
- `ISipExtension`: advertise option tags/methods/headers and process extension requests through result data.
- `ISipEndpoint`: lifecycle, generic UAC/UAS dispatch, component access through operations, and unified event polling.

### `src/Api/Data`

Codec/model files: `SipUri.cs`, `SipUriParameter.cs`, `SipUriHeader.cs`, `SipNameAddress.cs`, `SipHeader.cs`, `SipMessage.cs`, `SipViaValue.cs`, `SipRouteValue.cs`, `SipContactValue.cs`, `SipCSeqValue.cs`, `SipMediaType.cs`, `SipContentDisposition.cs`, `SipMimeEntity.cs`, `SipParseOptions.cs`, `SipParseResult.cs`, `SipWriteOptions.cs`, `SipWriteResult.cs`, `SipHeaderParseResult.cs`, `SipValidationIssue.cs`, `SipSessionDescription.cs`, `SipMediaDescription.cs`, `SipSdpAttribute.cs`, `SipXmlBody.cs`.

Networking files: `SipEndpointAddress.cs`, `SipDnsOptions.cs`, `SipResolutionRequest.cs`, `SipResolutionResult.cs`, `SipServerCandidate.cs`, `SipTransportOptions.cs`, `SipTlsOptions.cs`, `SipWebSocketOptions.cs`, `SipTransportMessage.cs`, `SipTransportEvent.cs`, `SipSendRequest.cs`, `SipSendResult.cs`, `SipConnectionInfo.cs`, `SipFlow.cs`.

Authentication files: `SipCredential.cs`, `SipDigestChallenge.cs`, `SipDigestAuthorization.cs`, `SipAuthenticationRequest.cs`, `SipAuthenticationResult.cs`, `SipNonceOptions.cs`, `SipBearerToken.cs`, `SipCertificateIdentity.cs`, `SipAuthorizationDecision.cs`.

State-machine files: `SipTimerOptions.cs`, `SipTransactionKey.cs`, `SipTransactionOptions.cs`, `SipTransactionSnapshot.cs`, `SipTransactionEvent.cs`, `SipDialogId.cs`, `SipDialogOptions.cs`, `SipDialogSnapshot.cs`, `SipDialogEvent.cs`, `SipOfferAnswer.cs`, `SipInviteOptions.cs`, `SipInviteRequest.cs`, `SipInviteEvent.cs`.

Service files: `SipRegistrationOptions.cs`, `SipRegistrationRequest.cs`, `SipRegistrationBinding.cs`, `SipRegistrationSnapshot.cs`, `SipRegistrationEvent.cs`, `SipLocationQuery.cs`, `SipLocationResult.cs`, `SipRegistrarOptions.cs`, `SipProxyOptions.cs`, `SipProxyRequest.cs`, `SipProxyTarget.cs`, `SipProxyResult.cs`, `SipProxyEvent.cs`, `SipRouteDecision.cs`, `SipRedirectResult.cs`.

Usage/endpoint files: `SipSubscriptionRequest.cs`, `SipSubscriptionEvent.cs`, `SipPublicationRequest.cs`, `SipPublicationEvent.cs`, `SipInstantMessageRequest.cs`, `SipInstantMessageEvent.cs`, `SipInfoRequest.cs`, `SipInfoResult.cs`, `SipOptionsResult.cs`, `SipCapabilitySet.cs`, `SipExtensionResult.cs`, `SipBackToBackEvent.cs`, `SipStackOptions.cs`, `SipStackEvent.cs`, `SipCancellation.cs`.

### `src/Api/Enums`

- Message/model: `SipMessageKind.cs`, `SipMethod.cs`, `SipStatusClass.cs`, `SipUriScheme.cs`, `SipParseStatus.cs`, `SipValidationSeverity.cs`, `SipBodyDisposition.cs`.
- Network: `SipTransportKind.cs`, `SipConnectionState.cs`, `SipDnsRecordKind.cs`, `SipResolutionStatus.cs`, `SipTransportEventKind.cs`.
- Authentication: `SipDigestAlgorithm.cs`, `SipDigestQuality.cs`, `SipAuthenticationStatus.cs`, `SipCredentialScope.cs`.
- Transactions/dialogs: `SipTransactionKind.cs`, `SipTransactionRole.cs`, `SipTransactionState.cs`, `SipTransactionEventKind.cs`, `SipDialogRole.cs`, `SipDialogState.cs`, `SipOfferAnswerState.cs`, `SipInviteSessionState.cs`.
- Services/usages: `SipRegistrationState.cs`, `SipProxyMode.cs`, `SipProxyEventKind.cs`, `SipSubscriptionState.cs`, `SipPublicationState.cs`, `SipBackToBackState.cs`, `SipStackEventKind.cs`.

### `src/Api/Static`

- `SipMethods.cs`: standard method-name constants.
- `SipHeaderNames.cs`: long and compact header-name constants.
- `SipStatusCodes.cs`: registered core and extension status-code constants.
- `SipReasonPhrases.cs`: conventional reason-phrase constants without making them normative.
- `SipOptionTags.cs`: standard option-tag constants.
- `SipMediaTypes.cs`: SIP, SDP, multipart, presence, and event-package media types.
- `SipTransportNames.cs`: transport tokens, default ports, and WebSocket subprotocol constants.
- `SipDefaults.cs`: protocol defaults and safe default limits.
- `SipTimerNames.cs`: transaction/dialog timer-name constants.

### `src/Api/Exceptions`

- `SipParseException.cs`, `SipProtocolException.cs`, `SipTransportException.cs`, `SipResolutionException.cs`, `SipTransactionException.cs`, `SipDialogException.cs`, `SipAuthenticationException.cs`, `SipRegistrationException.cs`, and `SipPolicyException.cs`; each derives directly from `System.Exception` as required by `syntax.md`.

### `src/Api/Classes`

- Codec implementations: `SipUriCodec.cs`, `SipHeaderCodec.cs`, `SipMessageCodec.cs`, `SipMimeCodec.cs`, `SipSdpCodec.cs`, `SipXmlBodyCodec.cs`.
- Network implementations: `SipDnsResolver.cs`, `UdpSipTransport.cs`, `TcpSipTransport.cs`, `TlsSipTransport.cs`, `WebSocketSipTransport.cs`, `SipTransportManager.cs`.
- Authentication implementations: `SipAuthenticationClient.cs`, `SipAuthenticationServer.cs`.
- Protocol state implementations: `SipTransactionLayer.cs`, `SipDialogLayer.cs`, `SipOfferAnswerSession.cs`, `SipInviteSession.cs`.
- Service implementations: `SipRegistrationClient.cs`, `SipLocationService.cs`, `SipRegistrar.cs`, `SipProxy.cs`, `SipRedirectServer.cs`.
- Usage implementations: `SipSubscriptionClient.cs`, `SipSubscriptionServer.cs`, `SipPublicationClient.cs`, `SipPublicationServer.cs`, `SipMessageClient.cs`, `SipOptionsService.cs`, `SipBackToBackSession.cs`, `SipEndpoint.cs`.

### Internal source layout

Non-public implementation helpers will live outside `src/Api`: `src/Internal/Codec/`, `src/Internal/Dns/`, `src/Internal/Transport/`, `src/Internal/Authentication/`, `src/Internal/Transactions/`, `src/Internal/Dialogs/`, `src/Internal/Registration/`, `src/Internal/Proxy/`, `src/Internal/Usages/`, `src/Internal/Scheduling/`, `src/Internal/Security/`, and `src/Internal/Collections/`. Types remain flat rather than nested. Internal classes isolate span parsers, state machines, timer queues, connection pools, DNS wire records, nonce/replay stores, and extension registries from the stable API.

### Planned test layout

- `tests/Unit/Codec/`: URI, headers, messages, framing, MIME, SDP, and XML behavior.
- `tests/Unit/Dns/` and `tests/Unit/Transport/`: wire codecs, selection, sockets, TLS, WebSocket, flows, and failures.
- `tests/Unit/Authentication/`: digest vectors, challenge policy, nonces, replay protection, bearer and certificate policy.
- `tests/Unit/Transactions/` and `tests/Unit/Dialogs/`: every state/timer transition and malformed/out-of-order input.
- `tests/Unit/Registration/`, `tests/Unit/Proxy/`, and `tests/Unit/Usages/`: service and dialog-usage state machines.
- `tests/Integration/`: in-process client/server, proxy, registrar, redirect, forking, transport, and reconnection scenarios.
- `tests/Conformance/`: RFC examples, RFC 4475/RFC 5118 torture fixtures, digest RFC vectors, DNS ordering fixtures, and captured interoperability messages.
- `tests/Api/`: black-box coverage of every public constructor, interface method, data field contract, enum value, constant, result status, event kind, and exception path exposed from `src/Api`.

## Implementation tasks

### Foundation and public model

- [DONE] T001: Create the `src/Api` and `src/Internal` directory structure and establish the root file-scoped namespaces without adding source dependencies beyond the .NET runtime.
- [DONE] T002: Implement the public message/model enums and static constant classes for methods, headers, statuses, option tags, media types, transports, defaults, and timer names.
- [DONE] T003: Implement the public URI, name-address, parameter, header, message, parse/write result, and validation data classes and connect every type to a codec contract.
- [DONE] T004: Implement the public networking, authentication, transaction, dialog, registration, proxy, usage, and endpoint enums and data classes and connect every type to a contract or containing data model.
- [DONE] T005: Implement the public exception classes, each directly based on `System.Exception`, with non-overloaded construction compatible with the restricted syntax.
- [DONE] T006: Implement internal bounded byte/text readers, ASCII token utilities, case-insensitive comparison, checked numeric parsing, buffer ownership, secure random token creation, monotonic clock access, and deterministic test seams.
- [DONE] T007: Implement internal queue, correlation-ID, limit accounting, and immutable-snapshot copying helpers used consistently by all stateful layers.
- [DONE] T008: Add black-box tests for all foundation data fields, enum values, constants, copy/ownership rules, default limits, and exception payload behavior exposed through `src/Api`.

### URI, header, message, and body codecs

- [DONE] T009: Implement `SipUriCodec` for SIP, SIPS, TEL, absolute unknown URIs, escaping, IPv4/IPv6 hosts, user-info, URI parameters, URI headers, and bounded parse errors.
- [DONE] T010: Implement RFC 3261 URI comparison and normalization, including default ports, parameter significance, percent-encoding equivalence, header matching, transport, user, ttl, method, maddr, lr, and telephone behavior.
- [DONE] T011: Implement generic token, quoted-string, comment, host/port, parameter, comma-list, q-value, date, delta-seconds, and name-address header primitives.
- [DONE] T012: Implement typed Via parsing/formatting including branches, sent-by, received, rport, maddr, ttl, alias, unknown parameters, and IPv6 literals.
- [DONE] T013: Implement typed Route, Record-Route, Contact, From, To, Call-ID, CSeq, and Max-Forwards parsing/formatting with wildcard and multi-value rules.
- [DONE] T014: Implement typed content, capability, expiry, warning, retry, product, and all core RFC 3261 header codecs, including compact names and comma-combination rules.
- [DONE] T015: Implement typed authentication headers for challenge, credentials, and Authentication-Info with lossless unknown scheme/parameter preservation.
- [DONE] T016: Implement typed dialog, session timer, reliable provisional response, event, publication, transfer, caller preference, routing, identity, history, feature capability, geolocation, priority, GRUU, Outbound, and push extension headers.
- [DONE] T017: Implement `SipMessageCodec` start-line and header parsing with incremental complete/incomplete/error results, exact byte offsets, legal whitespace compatibility, duplicates, unknown fields, and configurable limits.
- [DONE] T018: Implement datagram validation and stream framing using exact Content-Length rules, concatenated messages, partial input, conflicting lengths, body limits, and trailing-byte reporting.
- [DONE] T019: Implement canonical and preservation-oriented message serialization, byte measurement, compact-name option, controlled folding policy, binary bodies, and Content-Length reconciliation.
- [DONE] T020: Implement semantic message validation for mandatory headers, request/response constraints, singleton duplication, method/CSeq consistency, URI/SIPS rules, and configurable strict versus compatibility modes.
- [DONE] T021: Implement a generic extension registry and generic header/parameter APIs so unknown methods, headers, schemes, content types, option tags, and event packages can be preserved and processed.
- [DONE] T022: Implement `SipMimeCodec` for Content-Type/Disposition, nested multipart parsing/formatting, safe boundary creation, preamble/epilogue, sipfrag, unknown transfer encodings, and RFC 5621 disposition requirements.
- [DONE] T023: Implement `SipSdpCodec` for RFC 8866 session/media structure, IPv4/IPv6, timings, bandwidth, attributes, unknown lines, grouping/mid, RTCP, ICE/trickle data, and preservation round trips.
- [DONE] T024: Implement `SipXmlBodyCodec` for PIDF, rich-presence extension nodes, watcher-info, dialog-info, reginfo, message-summary, and is-composing with DTD and external entities disabled.
- [DONE] T025: Add URI unit tests for every grammar branch, comparison rule, normalization rule, escaping case, IPv4/IPv6 form, TEL behavior, unknown scheme, malformed input, and configured limit.
- [DONE] T026: Add core-header unit tests for every typed field, compact name, list rule, quoted/comment escape, duplicate behavior, unknown parameter, canonical output, and malformed value.
- [DONE] T027: Add extension-header unit tests covering authentication, Path/Service-Route, PRACK, timers, events, publication, transfer, caller preferences, identities, history, Feature-Caps, GRUU, Outbound, and push parameters.
- [DONE] T028: Add message parser tests for fragmented input at every byte boundary, pipelined streams, datagram constraints, body bytes, missing/conflicting lengths, limit boundaries, smuggling attempts, and exact error positions.
- [DONE] T029: Add serializer and round-trip tests proving canonical output and loss preservation for duplicate/unknown headers, unknown methods/statuses, arbitrary body bytes, compact forms, and forwarded messages.
- [DONE] T030: Import the RFC 4475 and RFC 5118 torture messages as conformance fixtures and test each expected acceptance, rejection, normalization, routing, and IPv6 outcome.
- [DONE] T031: Add MIME/sipfrag tests for nested multipart, quoted boundaries, empty parts, binary content, disposition handling, malformed boundaries, depth/size limits, and round trips.
- [DONE] T032: Add SDP tests for all modeled lines and attributes, unknown extension preservation, IPv4/IPv6, malformed structure, line/size limits, canonical format, and RFC examples.
- [DONE] T033: Add secure XML body tests for all supported event documents, namespaces/extensions, malformed documents, DTD/entity attacks, depth/size limits, and round trips.

### Routing primitives, DNS, and transports

- [DONE] T034: Implement branch, tag, Call-ID, cnonce, boundary, flow-token, and opaque-token generation with cryptographic randomness and required syntax.
- [DONE] T035: Implement Via request insertion and response removal, sent-by/branch matching, received/rport updates, response destination calculation, and reliable-connection affinity.
- [DONE] T036: Implement loose and strict Route processing, Record-Route construction, route-set reversal, outbound proxy precedence, remote-target selection, and SIPS downgrade prevention.
- [DONE] T037: Implement Contact parsing semantics, comparison, wildcard validation, q/expires ordering, target refresh, GRUU/instance/reg-id/ob parameters, and observed-address metadata.
- [DONE] T038: Implement Max-Forwards, loop/spiral fingerprints, merged-request keys, request-target rewriting, and generic routing validation helpers.
- [DONE] T039: Implement the bounded DNS wire codec for queries and NAPTR, SRV, A, AAAA, CNAME responses, including compression pointers, transaction IDs, truncation, names, TTLs, and response codes.
- [DONE] T040: Implement the DNS client over UDP with TCP retry, configured/system name servers, retry/deadline handling, CNAME limits, response matching, and malformed-response rejection.
- [DONE] T041: Implement RFC 3263 numeric, explicit-port, explicit-transport, NAPTR, SRV, A/AAAA, SIP/SIPS, default-port, and secure-target resolution paths.
- [DONE] T042: Implement RFC 2782 priority/weight ordering, deterministic random injection, IPv4/IPv6 candidate interleaving, failover feedback, TTL/negative caching, expiry, and cache flushing.
- [DONE] T043: Implement optional ENUM NAPTR resolution into ordered SIP/SIPS targets with bounded regexp processing and policy filtering.
- [DONE] T044: Implement `UdpSipTransport` for IPv4/IPv6 bind, send, receive, endpoint metadata, datagram/truncation limits, polling, cancellation, close, and transport events.
- [DONE] T045: Implement the shared reliable-stream connection/framing core with partial I/O, write queues, message queues, connection identity, pooling, deadlines, idle expiry, keepalive, and backpressure.
- [DONE] T046: Implement `TcpSipTransport` listeners and outbound connections over the reliable-stream core, including pooling, reconnect/failover signaling, IPv4/IPv6, and oversized-UDP fallback integration.
- [DONE] T047: Implement `TlsSipTransport` using `SslStream` with SIPS behavior, hostname/domain checks, chain and usage validation, mutual TLS, certificate-policy results, peer identity, and safe TLS defaults.
- [DONE] T048: Implement RFC 5922 SIP domain identity and RFC 5923 connection reuse/alias checks with authenticated peer safeguards.
- [DONE] T049: Implement `WebSocketSipTransport` for client/server integration, `sip` subprotocol, text/binary frames, fragmented frames, one-message framing, origin/size policy, keepalive, and close behavior.
- [DONE] T050: Implement CRLF keepalives and RFC 5626 flow management including instance/reg-id, Flow-Timer, primary/backup flows, tokens, failures, and Path/transport association.
- [DONE] T051: Implement `SipTransportManager` registration, target selection, connection/flow reuse, send/poll/close, queue limits, custom transport integration, and structured transport events.
- [DONE] T052: Add Via/Route/Contact routing unit tests for loose/strict routing, route-set directions, received/rport, reliable affinity, Contact wildcards/q/expires, target refresh, SIPS, loops, spirals, and merged keys.
- [DONE] T053: Add DNS wire-codec tests for every supported record, compression graph, truncation/TCP retry, malformed pointer/name/length, response matching, TTL, CNAME loop, and size/depth limit.
- [DONE] T054: Add RFC 3263 table-driven tests for every URI/port/transport/security branch, NAPTR order/preference, SRV priority/weight, A/AAAA interleaving, fallback, cache, expiry, and error result.
- [DONE] T055: Add ENUM tests for valid replacements, ordering, unsupported services, malformed expressions, loops, limits, and policy filtering.
- [DONE] T056: Add UDP loopback tests for IPv4/IPv6, metadata, one-message validation, exact size limits, cancellation, queue pressure, socket errors, and shutdown.
- [DONE] T057: Add TCP tests for byte-wise fragmentation, pipelining, partial writes, simultaneous connections, pool reuse, idle expiry, keepalives, disconnects, reconnect decisions, backpressure, and oversized-UDP switching.
- [DONE] T058: Add TLS tests with generated test roots/leaf certificates for valid/invalid names, chains, dates, usages, mutual TLS, SIPS requirements, certificate policy, identity metadata, reuse, failure, and shutdown.
- [DONE] T059: Add WebSocket tests for handshake/subprotocol, text/binary and fragmented frames, message boundaries, origin policy, ping/close, invalid payloads, limits, and cancellation.
- [DONE] T060: Add Outbound/flow tests for registration association, primary/backup choice, keepalive and Flow-Timer, flow-token integrity, failure/recovery, Path routing, and connection reuse.

### Transactions

- [DONE] T061: Implement transaction keys and RFC 3261 magic-cookie matching plus bounded RFC 2543 compatibility matching for requests, responses, ACK, and CANCEL.
- [DONE] T062: Implement the monotonic timer scheduler and configurable T1/T2/T4, explicit advancement, cancellation, stable ordering, and cleanup without invoking application-facing code under locks.
- [DONE] T063: Implement INVITE client transaction states, Timers A/B/D, provisional/final delivery, retransmissions, transport errors, and non-2xx ACK generation.
- [DONE] T064: Implement INVITE server transaction states, Timers G/H/I, response retransmissions, request retransmission absorption, ACK matching, and reliable-transport rules.
- [DONE] T065: Implement non-INVITE client transaction states, Timers E/F/K, provisional/final handling, retransmissions, transport errors, and RFC 4320 behavior.
- [DONE] T066: Implement non-INVITE server transaction states, Timer J, request retransmission absorption, response replay, and reliable-transport rules.
- [DONE] T067: Implement RFC 6026 accepted-state processing, client Timer M, server Timer L, INVITE 2xx retransmission ownership, end-to-end ACK matching, and timeout cleanup.
- [DONE] T068: Implement CANCEL creation/matching and ACK special cases, including correct branch/CSeq/Via reuse, 200-to-CANCEL, 487-to-INVITE coordination, and races.
- [DONE] T069: Implement merged-request detection, stateless send/forward APIs, malformed transaction rejection, transaction count limits, and overload results.
- [DONE] T070: Implement target failover rules and transport affinity, including new branch/Via for a safe retry and suppression after a response establishes the transaction.
- [DONE] T071: Implement `SipTransactionLayer` event/snapshot queues, transition reasons, correlation, cancellation/termination operations, lifecycle, and thread-safe ownership.
- [DONE] T072: Add exhaustive INVITE client/server tests for every state, input, retransmission, timer boundary, reliable/unreliable mode, transport failure, ACK variant, and invalid transition.
- [DONE] T073: Add exhaustive non-INVITE client/server tests for every state, input, retransmission, provisional/final response, timer boundary, reliable/unreliable mode, and RFC 4320 case.
- [DONE] T074: Add RFC 6026 tests for accepted state, repeated 2xx, repeated end-to-end ACK, multiple 2xx, Timer L and Timer M behavior, and transaction-user retransmission ownership.
- [DONE] T075: Add CANCEL/ACK race tests for cancel-before-response, cancel-after-provisional, final-before-CANCEL, duplicate messages, lost responses, late ACK, and unmatched inputs.
- [DONE] T076: Add matching tests for magic-cookie and legacy transactions, merged requests, forked branches, transport affinity, failover, malformed keys, count limits, cleanup, and snapshot/event ordering.
- [DONE] T077: Add model-based transaction state-machine tests that generate valid and invalid event/timer sequences and assert invariants, terminal cleanup, and no duplicate application delivery.

### Dialogs, offer/answer, and INVITE usages

- [DONE] T078: Implement dialog IDs, UAC/UAS creation from requests and provisional/final responses, early/confirmed/terminated state, secure flag, and snapshots.
- [DONE] T079: Implement route-set creation/direction, local/remote targets and tags, local CSeq generation, remote CSeq ordering, and in-dialog request construction/validation.
- [DONE] T080: Implement forked early dialogs, multiple 2xx dialogs, independent ACK/BYE cleanup, early-dialog replacement, and unmatched-dialog handling.
- [DONE] T081: Implement target-refresh behavior for INVITE/UPDATE and RFC 6141 re-INVITE success/failure rules without changing immutable route sets.
- [DONE] T082: Implement RFC 5057 multiple dialog-usage tracking, usage-local termination, dialog lifetime, and application-visible usage identifiers.
- [DONE] T083: Implement `SipDialogLayer` lookup, mutation commands, termination, transaction association, authentication context association, event queue, limits, and cleanup.
- [DONE] T084: Implement `SipOfferAnswerSession` for opaque/SDP offers and answers, one-offer invariant, offerless INVITE, provisional/final answers, UPDATE/re-INVITE, rollback, and application rejection.
- [DONE] T085: Implement outgoing INVITE usage through provisional, early media signaling, success, redirection policy, failure, cancellation, ACK, and BYE.
- [DONE] T086: Implement incoming INVITE usage through trying/ringing/reliable provisional/answer/reject/cancel/ACK/BYE and automatic transaction responses.
- [DONE] T087: Implement RFC 3262 reliable provisional responses with 100rel negotiation, RSeq/RAck, PRACK transactions, retransmission/timers, offer/answer interaction, and failure.
- [DONE] T088: Implement UPDATE, re-INVITE, hold/resume body signaling, overlapping request/glare handling, Retry-After, and target refresh.
- [DONE] T089: Implement RFC 4028 session timer negotiation, 422/Min-SE retry, refresher role, UPDATE/re-INVITE refresh, proxy parameters, expiry, and events.
- [DONE] T090: Implement Reason, connected identity, Session-ID, 199 early-dialog termination, and capability/identity propagation within invite-session events.
- [DONE] T091: Implement `SipInviteSession` snapshots/events, explicit deadlines/cancellation, authentication retry association, transport failure behavior, and terminal cleanup.
- [DONE] T092: Add dialog creation and routing tests for UAC/UAS, early/confirmed/terminated, route sets, strict/loose routing, CSeq, target refresh, SIPS, malformed and out-of-dialog requests.
- [DONE] T093: Add forking/multiple-usage tests for early forks, multiple 2xx, independent ACK/BYE, replaced early dialogs, usage termination, dialog retention, and cleanup.
- [DONE] T094: Add offer/answer tests for every legal placement, offerless flows, reliable provisional responses, UPDATE/re-INVITE, collisions, rollback, invalid overlap, opaque bodies, and SDP bodies.
- [DONE] T095: Add outgoing and incoming INVITE usage tests for every state, provisional/final class, redirect, cancellation race, ACK loss/retry, BYE, transport error, timeout, and invalid command.
- [DONE] T096: Add PRACK tests for negotiation, ordering, duplicate/lost PRACK and reliable provisional responses, RAck mismatch, offer/answer placement, timeout, and required-extension failure.
- [DONE] T097: Add re-INVITE/UPDATE/session-timer tests for glare, target-refresh success/failure, hold/resume bodies, 422 retry, refresher roles, refresh failure, expiry, and proxy parameters.

### Authentication and authorization

- [DONE] T098: Implement digest algorithm and hash helpers for MD5, SHA-256, SHA-512/256 and sess variants using built-in cryptography, exact hexadecimal encoding, and fixed-time comparison.
- [DONE] T099: Implement RFC 7616 digest calculation for no-qop, auth, auth-int, nonce count, cnonce, charset UTF-8, userhash, method/target/body inputs, and rspauth.
- [DONE] T100: Implement `SipAuthenticationClient` challenge parsing/selection, strongest-first algorithm policy, realm/target credential lookup, origin/proxy separation, retry bounds, stale, nextnonce, cache, and Authentication-Info verification.
- [DONE] T101: Implement nonce creation/validation with cryptographic randomness, timestamp/realm binding, tamper evidence, expiry, opaque data, key rotation inputs, and privacy-safe results.
- [DONE] T102: Implement replay tracking by realm/identity/nonce/cnonce/nc with bounded storage, expiry, monotonic nonce counts, duplicate detection, and concurrency safety.
- [DONE] T103: Implement `SipAuthenticationServer` 401/407 challenge generation, multiple algorithms in preference order, verification, stale handling, Authentication-Info, proxy/origin separation, and credential-provider policy.
- [DONE] T104: Implement RFC 8898 Bearer header handling and `ISipTokenValidator` integration with realm, scope, error, and retry results while leaving token issuance external.
- [DONE] T105: Implement mutual-TLS identity and trusted-peer authentication results for use by registrar, proxy, and authorization policy.
- [DONE] T106: Add digest primitive tests using all RFC 7616 and RFC 8760 vectors plus independent generated vectors for every algorithm, sess mode, qop, charset, userhash, body, rspauth, and malformed input.
- [DONE] T107: Add client-authentication tests for multi-challenge ordering, unsupported/downgrade cases, multiple realms, origin/proxy scopes, stale/nextnonce, cache, retry limits, missing credentials, and Authentication-Info failure.
- [DONE] T108: Add nonce/replay tests for valid, tampered, expired, future, wrong-realm, key-rotation, duplicate, non-monotonic nc, concurrent requests, eviction, and constant-time verification paths.
- [DONE] T109: Add server-authentication tests for 401/407, ordered multi-algorithm challenges, every verification status, stale behavior, Authentication-Info, auth-int, userhash, proxy/origin isolation, and policy rejection.
- [DONE] T110: Add bearer, mutual-TLS, trusted-peer, and authorization-policy tests for success, denial, malformed credentials, identity propagation, scope/realm mismatch, and secret redaction.

### Registration, location, proxy, and server roles

- [DONE] T111: Implement registration-client request creation, mandatory headers, stable Call-ID, increasing CSeq, Contact generation, outbound proxy/Service-Route use, and authentication association.
- [DONE] T112: Implement registration-client state for initial success, refresh scheduling/jitter, 401/407, 3xx policy, 423/Min-Expires, transient/permanent failure, contact matching, expiry, and shutdown.
- [DONE] T113: Implement unregister-all and per-contact unregister, transport/flow failure recovery, Outbound primary/backup flows, push parameters, GRUU capture, and registration events/snapshots.
- [DONE] T114: Implement `SipLocationService` in-memory binding query/update/remove/expire with atomic AOR updates, q ordering, Call-ID/CSeq metadata, Path/flow/received/GRUU metadata, and deterministic snapshots.
- [DONE] T115: Implement `ISipLocationService` integration seams that allow application-provided durable storage without adding a database dependency to `src/`.
- [DONE] T116: Implement registrar request validation, AOR/domain/third-party policy, wildcard and Contact constraints, Call-ID/CSeq ordering, expiry selection, retransmission idempotence, and atomic changes.
- [DONE] T117: Implement registrar responses for success, authentication, interval-too-brief, malformed/forbidden/unsupported cases, complete Contact lists, Date, capabilities, Path, Outbound, and GRUU.
- [DONE] T118: Implement binding expiry and registration event generation, including RFC 3680 state/version/reason data and flow/push status changes.
- [DONE] T119: Implement stateless proxy request/response validation and transformations for Via, Route, Record-Route, Contact, Max-Forwards, rport, SIPS, DNS, transport, and response Via removal.
- [DONE] T120: Implement stateful proxy branch transactions, Timer C start/reset/expiry behavior, provisional forwarding, final-response collection, best-response selection, 6xx handling, timeout, late responses, and cleanup.
- [DONE] T121: Implement sequential and parallel forking from route policy and location results, q grouping, branch limits, per-branch state, redirect recursion/depth policy, and target deduplication.
- [DONE] T122: Implement proxy CANCEL/ACK behavior for branch cancellation, cancel races, 200/487, non-2xx ACK transaction routing, 2xx ACK stateless forwarding, multiple 2xx, and late branches.
- [DONE] T123: Implement proxy authentication/authorization, trusted peer and identity/privacy header policy, Path/Service-Route/Outbound routing, Record-Route policy, and connection reuse.
- [DONE] T124: Implement proxy overload and safety policy for malformed input, loops/spirals/merged requests, transaction/branch/rate/queue limits, Retry-After, sanitation, and redacted events.
- [DONE] T125: Implement redirect server route-policy evaluation, 3xx Contact/q/expires construction, SIPS rules, and error responses.
- [DONE] T126: Implement OPTIONS service and generic UAS dispatch with Allow/Supported/Accept/Allow-Events capabilities, extension methods, application responses, and automatic mandatory headers.
- [DONE] T127: Add registration-client tests for every success/failure/retry/refresh/unregister state, contact matching, 3xx, 423, authentication, jitter bounds, flow failure/recovery, GRUU, push, and shutdown.
- [DONE] T128: Add location-service tests for atomic updates, multiple bindings, q ordering, expiry, Call-ID/CSeq, retransmissions, wildcard removal, Path/flow/received metadata, GRUU uniqueness, concurrency, and custom storage integration.
- [DONE] T129: Add registrar tests for every validation and response branch, expiry precedence/bounds, third-party policy, authentication, idempotence, atomicity, Date/Contact/capabilities, Path, Outbound, GRUU, and registration events.
- [DONE] T130: Add stateless proxy tests for every method and response class, Via/Route/Record-Route/rport/Max-Forwards transformations, strict routing, SIPS, DNS/transport failures, loops, and malformed input.
- [DONE] T131: Add stateful proxy tests for Timer C start/reset/expiry, provisional/final aggregation, best-response rules, 6xx, branch timeouts, retransmissions, transport failures, cleanup, and event/snapshot order.
- [DONE] T132: Add forking tests for sequential/parallel/q groups, redirect recursion, duplicate targets, branch limits, CANCEL races, non-2xx and 2xx ACK, multiple/late 2xx, location changes, and flow routing.
- [DONE] T133: Add proxy security/policy tests for authentication, authorization, trusted peers, identity/privacy, header sanitation, Record-Route, Path/Service-Route/Outbound, overload limits, Retry-After, and redaction.
- [DONE] T134: Add redirect, OPTIONS, and generic UAS tests for capability construction, extension dispatch, all policy results, mandatory headers, response routing, retransmissions, and malformed requests.

### High-level usages and extensions

- [DONE] T135: Implement pager-mode MESSAGE client/server behavior for body/content validation, authentication, redirects, size policy, response correlation, delivery events, and in-dialog/out-of-dialog use.
- [DONE] T136: Implement the RFC 6086 INFO package framework, package registration, Recv-Info/Info-Package negotiation, legacy INFO policy, body validation, responses, and dialog association.
- [DONE] T137: Implement REFER client/server behavior, refer event subscription, message/sipfrag notifications, progress/final state, `Refer-Sub: false`, authentication, and dialog association.
- [DONE] T138: Implement Replaces, Referred-By, Join, and Target-Dialog validation/correlation helpers for blind/attended transfer, call pickup, and application authorization.
- [DONE] T139: Implement `SipSubscriptionClient` for initial/forked/in-dialog subscriptions, NOTIFY-before-200, refresh, 423, Event/id matching, ordering/version, termination, retry, and events.
- [DONE] T140: Implement `SipSubscriptionServer` for authorization, initial NOTIFY, state/version management, refresh/unsubscribe, expiration, retry-after/reason, reliable delivery, and forkable event policy.
- [DONE] T141: Implement event-package registration plus presence/PIDF, watcher-info, dialog-info, registration, message-summary, and resource-list package semantics over the body codecs.
- [DONE] T142: Implement `SipPublicationClient` initial/modify/refresh/remove operations, ETag/If-Match state, 412/423 recovery, authentication, expiration, and events.
- [DONE] T143: Implement `SipPublicationServer` conditional ETag updates, publication store integration, authorization, expiry, aggregation notification triggers, and error responses.
- [DONE] T144: Implement caller preferences/callee capabilities, Feature-Caps, Supported/Require/Unsupported, Allow, Accept, and Allow-Events negotiation helpers.
- [DONE] T145: Implement privacy, asserted/preferred/connected identity, History-Info, Session-ID, Geolocation, Resource-Priority, and SIP Identity/PASSporT preservation plus application verification-policy hooks.
- [DONE] T146: Implement RFC 5626/5627/8599 high-level Outbound, GRUU, and push helpers across registration, routing, dialog targets, and contact generation.
- [DONE] T147: Implement optional ICE/trickle-ICE signaling helpers and option-tag handling over SDP attributes without implementing connectivity checks or media transport.
- [DONE] T148: Implement SigComp capability/compartment metadata and a custom transport-content-coding integration point while keeping compression itself outside the baseline implementation.
- [DONE] T149: Implement `SipBackToBackSession` with independent legs, dialog/transaction/CSeq/Contact/authentication/route state, application-controlled request/response/body mapping, cancellation, and teardown.
- [DONE] T150: Implement `SipEndpoint` composition and lifecycle, generic UAC/UAS operations, component coordination, extension registration, unified event polling, cancellation, limits, and orderly shutdown.
- [DONE] T151: Add MESSAGE and INFO tests for in/out-of-dialog use, body/content rules, authentication, redirects, correlation, package negotiation, legacy policy, invalid packages, sizes, failures, and events.
- [DONE] T152: Add REFER/transfer tests for implicit subscription, norefersub, sipfrag progress, Replaces/Referred-By, Join/Target-Dialog, blind/attended flows, authorization, races, failure, and termination.
- [DONE] T153: Add subscription-client/server tests for initial/forked/in-dialog flows, NOTIFY-before-200, refresh, unsubscribe, expiry, 423, version/order, Event/id mismatch, failures, and every termination reason.
- [DONE] T154: Add event-package tests for presence, watcher-info, dialog-info, registration, message-summary, resource-list aggregation, extension preservation, authorization, versioning, and malformed bodies.
- [DONE] T155: Add publication-client/server tests for create/modify/refresh/remove, ETag/If-Match, 412/423 recovery, concurrent conditional updates, authentication, expiry, aggregation triggers, and errors.
- [DONE] T156: Add capability and identity-extension tests for caller preferences, Feature-Caps, option negotiation, privacy, asserted/connected identity, History-Info, Session-ID, Geolocation, Resource-Priority, Identity/PASSporT hooks, and unknown extension pass-through.
- [DONE] T157: Add Outbound/GRUU/push and ICE-signaling tests across registration, route selection, target refresh, flow failure, contact generation, option tags, SDP preservation, and unsupported-policy paths.
- [DONE] T158: Add B2BUA tests proving leg isolation, mapping commands, independent authentication/routing/CSeq/Contact/SDP state, provisional/final/cancel/BYE races, failures, and cleanup.
- [DONE] T159: Add endpoint tests for every lifecycle state, generic request/response operation, component access path, extension registration, event correlation/order, cancellation/deadline, queue/limit behavior, concurrency, and shutdown.

### End-to-end conformance and complete API coverage

- [DONE] T160: Build an in-process UAC/UAS test harness with deterministic clock, controllable packet loss/duplication/reordering/fragmentation, scripted DNS, certificate fixtures, and all supported transports.
- [DONE] T161: Add end-to-end UDP and TCP call scenarios covering DNS, INVITE, provisional/final responses, ACK, re-INVITE/UPDATE, session refresh, BYE, retransmission, loss, and connection failure.
- [DONE] T162: Add end-to-end TLS/SIPS and WebSocket call scenarios covering certificate identities, mutual TLS, connection reuse, secure routing, framing, reconnect policy, and downgrade rejection.
- [DONE] T163: Add end-to-end registrar/client scenarios covering digest algorithms, refresh/unregister, Path/Service-Route, Outbound flows, GRUU, push parameters, expiry, failover, and registration events.
- [DONE] T164: Add end-to-end proxy scenarios covering stateless/stateful forwarding, loose/strict routes, forking, redirect recursion, CANCEL/ACK races, multiple 2xx, authentication, SIPS, and transport failover.
- [DONE] T165: Add end-to-end dialog-usage scenarios for PRACK, offer/answer, session timers, MESSAGE, INFO, REFER/Replaces, SUBSCRIBE/NOTIFY, PUBLISH, presence, and B2BUA leg isolation.
- [DONE] T166: Add hostile-input integration tests for message smuggling, oversized fields/bodies, parser ambiguity, DNS pointer attacks, MIME/XML depth attacks, nonce replay/tampering, queue exhaustion, and overload responses.
- [DONE] T167: Add multithreaded integration tests for concurrent sends, receives, timer expiry, registration updates, proxy forks, dialog termination, shutdown, and the invariant that application-facing work is not invoked under internal locks.
- [DONE] T168: Add a public API traceability suite that invokes every `src/Api/Contracts` method in success, expected-failure, cancellation, timeout, invalid-state, and limit cases and observes every declared result/event status.
- [DONE] T169: Add a public data-model coverage suite that reads/writes every public field through supported operations, exercises every enum member and static constant in its protocol context, and verifies copy/ownership/null handling.
- [DONE] T170: Add a public implementation coverage suite that constructs every `src/Api/Classes` type, exercises every interface member it exposes, and verifies every documented exception class can arise only from its intended failure category.
- [DONE] T171: Add conformance traceability tests mapping every normative behavior claimed in this plan to at least one focused test and at least one integration test where cross-layer behavior is involved.
- [DONE] T172: Run the complete test suite with line and branch coverage collection and close every uncovered reachable path in functionality exposed by `src/Api`, excluding only runtime-impossible defensive guards with an explicit test rationale.
