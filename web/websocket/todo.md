# Universal WebSocket framing and handshake library development plan

## Goal and scope

Build a dependency-free C# WebSocket protocol library for both client and server roles. The library will provide transport-neutral opening handshakes, incremental frame encoding and decoding, extension negotiation and transformation, message streaming, control-frame handling, and a high-level connection state machine.

The core protocol targets RFC 6455. HTTP/1.1 Upgrade follows RFC 6455 plus current HTTP semantics and messaging in RFC 9110 and RFC 9112. Extended CONNECT follows RFC 8441 for HTTP/2 and RFC 9220 for HTTP/3. `permessage-deflate` follows RFC 7692 and DEFLATE follows RFC 1951.

The boundary is deliberately Sans-I/O: callers provide HTTP metadata or byte chunks and move produced bytes to their transport. The library does not open sockets, perform DNS, TLS, proxy tunneling, HTTP/2 framing/HPACK, HTTP/3 framing/QPACK, authentication, routing, reconnection, or broadcasting. This keeps the framing and handshake implementation universal and compatible with the restricted C# subset. HTTP/2 and HTTP/3 adapters must convey SETTINGS availability, pseudo-fields, regular fields, DATA bytes, end-of-stream, and reset signals to the library.

## Implementation constraints from `syntax.md`

- All `src/` code uses only the .NET base class libraries; test-only packages and external conformance tools are permitted under `tests/`.
- No async syntax is used. Incremental input, output draining, explicit time advancement, and pull-based events provide non-blocking integration without owning a scheduler.
- No delegates or events are used. Policies and extension hooks are interfaces; connection notifications are pulled as `WebSocketEvent` data objects.
- No properties are used. Public data classes expose documented fields; operational state is queried through uniquely named interface methods.
- No `ref`, `out`, tuples, records, structs, custom generics, or nullable value types are used. Operations return result data classes, while `ReadOnlySpan<byte>` and `Span<byte>` are used for bounded zero-copy input/output where their by-value use is valid.
- Every public implementation class implements an interface, exposes one constructor, and exposes only that interface's instance methods. Interface method names remain unique across each implementation's combined interface set; methods are not overloaded.
- Public API files live only in the required `src/Api` category folders. Internal parsers and state machines live outside `src/Api` and expose no public types.
- Signed integers represent sizes and counts. `uint` is limited to masking keys and other bit-oriented values. Wire lengths up to 63 bits are validated incrementally without requiring a whole-message allocation.
- Because source code cannot use `try`/`catch`, protocol validation is performed before conversion where possible. Internal Base64, token, integer, and UTF-8 validation avoids exception-driven parsing; unavoidable BCL I/O/compression failures propagate as documented BCL exceptions or are represented by exception-less result paths before mutation.
- Required XML comments are written with each public type/member implementation, but there are no standalone documentation or release work items.

## Research corpus

The relevant references have been downloaded into `specs/` so implementation and review do not depend on mutable web pages.

### Standards and registries

| Local file | Authority and use |
| --- | --- |
| `specs/rfcs/rfc6455.txt` and `specs/rfcs/rfc6455-inline-errata.html` | RFC Editor: opening handshake, wire framing, messages, control frames, close behavior, errors, extensions, security, and verified errata. |
| `specs/rfcs/rfc8441.txt` and `specs/rfcs/rfc8441-errata.html` | RFC Editor: HTTP/2 `SETTINGS_ENABLE_CONNECT_PROTOCOL`, Extended CONNECT, WebSocket pseudo-fields, response rules, and stream closure. |
| `specs/rfcs/rfc9220.txt` | RFC Editor: application of Extended CONNECT to HTTP/3. |
| `specs/rfcs/rfc7692.txt` and `specs/rfcs/rfc7692-errata.html` | RFC Editor: extension negotiation, per-message compression, context takeover, window limits, and RSV1 rules. |
| `specs/rfcs/rfc1951.txt` | RFC Editor: raw DEFLATE blocks, Huffman coding, and sliding-window semantics needed for a built-in-only compression implementation. |
| `specs/rfcs/rfc9110.txt`, `rfc9112.txt`, `rfc9113.txt`, and `rfc9114.txt` | RFC Editor: current HTTP semantics plus HTTP/1.1, HTTP/2, and HTTP/3 field and message rules. |
| `specs/rfcs/rfc6454.txt` | RFC Editor: Origin serialization and comparison. |
| `specs/rfcs/rfc3629.txt` | RFC Editor: UTF-8 validity rules. |
| `specs/rfcs/rfc4648.txt` | RFC Editor: canonical Base64 for `Sec-WebSocket-Key` and `Sec-WebSocket-Accept`. |
| `specs/rfcs/rfc7936.txt` | RFC Editor: case-insensitive uniqueness rules for registered subprotocol names. |
| `specs/rfcs/rfc8307.txt` | RFC Editor: the WebSocket well-known URI update to RFC 6455; retained as a related protocol reference, not a core API requirement. |
| `specs/registries/iana-websocket.xml` | IANA: current extensions, subprotocols, versions, close codes, opcodes, and framing-header-bit registries. |
| `specs/whatwg-websockets.html` | WHATWG: browser-facing WebSocket behavior and modern handshake integration considerations. |

### Existing implementation survey

| Library snapshot | Useful design/features observed | Plan response |
| --- | --- | --- |
| `specs/libraries/ws-README.md`, `ws-api.md` | Client/server roles, strict payload and fragment limits, automatic pong, explicit ping/pong, fragmented sends, stream facade, buffered output visibility, close timeout, handshake hooks, subprotocols, redirects, and configurable `permessage-deflate`. | Adopt protocol limits, output/backpressure visibility, automatic control handling, chunked sends, handshake customization, and compression controls. Leave redirects, agents, and socket ownership to transport integrations. |
| `specs/libraries/websocketpp-README.md` | Shared client/server protocol core, message/event API, thread safety, and interchangeable transports including raw buffers. | Adopt one role-parameterized protocol core and transport neutrality. Make a connection instance single-consumer/single-producer with explicit serialization rules rather than embedding thread scheduling. |
| `specs/libraries/gorilla-websocket-doc.go` | One concurrent reader plus one writer, streaming message readers/writers, all control messages, default ping and close responses, origin checks, buffer bounds/pools, prepared messages, and no-context-takeover compression. | Adopt streaming chunks, safe default origin policy, independent read/write pumping, automatic ping/close responses, reusable caller buffers, and configurable compression. Prepared-message caching is outside the protocol core but encoded frame bytes may be reused by servers only when masking and negotiated extensions allow it. |
| `specs/libraries/python-websockets-features.html` | Sans-I/O layer, whole and fragmented message delivery, keepalive/heartbeat/latency, close details from both peers, custom extensions, security limits, handshake alteration, subprotocol selection, and opening/closing timeouts. | Adopt Sans-I/O architecture, both streaming and reassembled delivery, explicit local/remote close information, custom extension pipeline, security limits, deterministic time advancement, and heartbeat events. |
| `specs/libraries/boost-beast-*.html` | Role-symmetric layered streams, caller-controlled buffers/backpressure, automatic fragmentation, control callbacks, automatic ping response, handshake decorators, timeout state, and explicit thread-safety expectations. | Adopt symmetric role validation, caller-owned pumping/backpressure, automatic fragmentation, pull-based control events, handshake fields, and explicit concurrency/state rules. Do not copy transport, executor, or asynchronous abstractions. |
| `specs/conformance/autobahn-testsuite-README.md` | More than 500 conformance cases covering framing, reserved bits, opcodes, fragmentation, UTF-8, ping/pong, closing, limits, and `permessage-deflate`. | Add client and server adapters for the maintained Autobahn container and require strict protocol case groups, excluding only its performance/limit-duration groups when they are not functional conformance tests. |

## Complete feature inventory

### Layer 1: protocol vocabulary and safe primitives

- F001: Client and server roles with role-correct masking and close behavior.
- F002: All 16 wire opcode values represented, including reserved data/control opcodes for extension-aware validation and diagnostics.
- F003: Standard close codes, reserved pseudo-codes, private/application ranges, and current IANA values represented without preventing valid future/private integer codes.
- F004: Connection states covering connecting, open, closing, closed, and failed, with separate local and remote close facts.
- F005: Parse results distinguish completed input, incomplete input, backpressure, closed input, limit rejection, and protocol failure without using exception flow for routine partial data.
- F006: Cryptographically random 16-byte client handshake nonces and unpredictable 32-bit frame masking keys using built-in cryptography.
- F007: Canonical Base64 validation/encoding for keys and accepts, including the corrected RFC 6455 errata vector.
- F008: SHA-1 accept computation over the exact key text plus RFC GUID.
- F009: Incremental strict UTF-8 validation across arbitrary chunks and fragments, rejecting overlong encodings, surrogates, invalid continuations, and values above U+10FFFF.
- F010: Configurable signed limits for HTTP header bytes/count/line length, frame payload, message payload, fragment count, buffered input/output, queued events, close time, and heartbeat timing.

### Layer 2: frame codec and masking

- F011: Incremental frame-header parsing for 7-bit, 16-bit, and 63-bit payload lengths.
- F012: Canonical/minimal payload-length encoding validation, rejection of a set 64-bit sign bit, integer overflow protection, and configured-length enforcement before allocation.
- F013: Frame-header writing and encoded-size measurement for all legal length forms.
- F014: FIN, RSV1/RSV2/RSV3, opcode, mask flag, mask key, and payload length exposed through frame data models.
- F015: In-place XOR masking/unmasking with a byte offset so a payload split across calls continues the four-byte mask cycle correctly.
- F016: Fresh masking key for every client-originated frame, including control and continuation frames; server frames remain unmasked.
- F017: Enforcement that received client frames are masked and received server frames are unmasked for HTTP/1, HTTP/2, and HTTP/3 transports.
- F018: Partial headers, partial masks, partial payloads, multiple frames in one input chunk, and zero-length payloads supported.
- F019: Control frames require FIN, use opcodes 8-15 correctly, and never exceed 125 payload bytes or use extended lengths.
- F020: Reserved bits and reserved opcodes rejected unless exactly claimed by a negotiated extension.
- F021: Unknown/reserved control opcodes cause connection failure; extension-defined data behavior is routed through the negotiated extension pipeline.
- F022: Low-level frame read and write APIs preserve consumption/production counts and never silently discard trailing bytes.
- F023: Close payload codec handles empty payload, two-byte network-order code plus optional UTF-8 reason, and rejects a one-byte payload.
- F024: RFC sample frames, all boundary lengths (0, 1, 125, 126, 65535, 65536), every alignment of partial input, and masking offsets are deterministic test vectors.

### Layer 3: opening handshake

- F025: Incremental bounded HTTP/1.1 request/status-line and header parsing with strict CRLF, token field names, safe field values, duplicate preservation, and rejection of obs-fold, NUL, bare CR/LF, and header injection.
- F026: HTTP/1.1 client request generation using GET, origin-form resource name, Host, `Upgrade: websocket`, `Connection: Upgrade`, random `Sec-WebSocket-Key`, and version 13.
- F027: Optional Origin, ordered subprotocol offers, ordered extension offers, cookies/authentication metadata supplied by callers, user-agent, and validated extra fields.
- F028: IPv4, bracketed IPv6, explicit/default ports, path/query, `ws`/`wss`, Internationalized Domain Name handling through built-in `Uri`/IDNA facilities, and rejection of URI fragments or user-info.
- F029: HTTP token-list handling is case-insensitive where the relevant HTTP grammar requires it, preserves ordered preference where WebSocket requires it, and handles combined or repeated field lines.
- F030: Server validation of HTTP/1.1-or-newer GET, request target, Host, Upgrade token, Connection token, exactly one valid key decoding to 16 bytes, and supported WebSocket version.
- F031: Server success response generation with status 101, Upgrade/Connection fields, exact SHA-1 accept, at most one selected offered subprotocol, and negotiated extensions.
- F032: Server rejection results support 400 malformed request, 403 rejected origin/policy, 404/other application rejection, 426 unsupported version with `Sec-WebSocket-Version: 13`, and caller-provided safe fields/body metadata.
- F033: Client response validation requires status 101, Upgrade and Connection tokens, exactly matching accept, a single selected offered subprotocol, and only valid offered extension configurations.
- F034: Informational responses and non-101 final responses are surfaced without changing to WebSocket framing; redirect policy remains with the caller.
- F035: Bytes following the terminating HTTP/1 header block are retained as immediate WebSocket frame input.
- F036: Configurable origin policy with a safe default comparing normalized Origin host/scheme/port to the request authority when Origin is present, plus an interface for application policy.
- F037: Configurable subprotocol selection preserving client offer order or server preference, with validation of tokens, duplicate names, case-sensitive protocol matching, and RFC 7936 registry-name considerations.
- F038: Handshake customization allows validated additional request/response fields but prevents overriding protocol-critical fields inconsistently.
- F039: HTTP/2 Extended CONNECT generation requires peer `SETTINGS_ENABLE_CONNECT_PROTOCOL = 1`, `:method = CONNECT`, `:protocol = websocket`, and valid `:scheme`, `:path`, and `:authority`.
- F040: HTTP/2 Extended CONNECT omits Connection, Upgrade, Host, `Sec-WebSocket-Key`, and `Sec-WebSocket-Accept`, uses lowercase field names, retains Origin/version/subprotocol/extensions, and accepts a valid 2xx response rather than 101.
- F041: HTTP/2 server validation rejects use before the setting is enabled, missing/duplicate/invalid pseudo-fields, forbidden connection-specific fields, unsupported protocols, and invalid regular WebSocket fields.
- F042: HTTP/3 Extended CONNECT reuses RFC 8441 semantics with the independently advertised setting value 0x08 and maps clean FIN/reset signals to transport-end input.
- F043: HTTP/2 and HTTP/3 stream DATA contains unchanged RFC 6455 frames, including client masking, fragmentation, control frames, extensions, and close handshake.
- F044: Handshake APIs return negotiated transport, URI/authority/path, subprotocol, extensions, request/response metadata, and structured rejection/error details for both roles.

### Layer 4: extensions and `permessage-deflate`

- F045: Complete `Sec-WebSocket-Extensions` parser/formatter for comma-separated offers, semicolon parameters, tokens, quoted strings, quoted-pair escapes, optional whitespace, repeated fields, and ordered alternatives.
- F046: Duplicate or malformed parameters are preserved long enough for the responsible extension to reject them correctly; response extensions must correspond to offers.
- F047: Custom extension interface covers client offer creation, server offer acceptance, client response validation, claimed RSV bits/opcodes, per-connection state, and incremental inbound/outbound message transforms.
- F048: Negotiated extensions have deterministic order; outgoing transforms run in negotiated order, incoming transforms run in reverse, and incompatible RSV/opcode claims fail negotiation.
- F049: Extension state resets at connection end, observes message boundaries and fragmentation, and never transforms control frames unless its registered semantics explicitly permit them.
- F050: RFC 7692 `permessage-deflate` client offers and server responses support `server_no_context_takeover`, `client_no_context_takeover`, `server_max_window_bits`, and optional/valued `client_max_window_bits` with role-correct meaning.
- F051: Multiple `permessage-deflate` alternatives, ordered preference, values 8 through 15, parameter duplication, unknown parameters, valueless/value-required distinctions, and unsupported configurations are handled exactly by RFC 7692 negotiation rules.
- F052: RSV1 marks only the first frame of a compressed data message; continuation and control frames never set RSV1, and compressed/uncompressed messages may be interleaved.
- F053: Compression/decompression operates incrementally across message fragments while control frames may interleave, and UTF-8 validation applies to decompressed text.
- F054: Raw DEFLATE removes the final `00 00 ff ff` sync-flush tail on send and restores it on receive, including empty, incompressible, and block-boundary messages.
- F055: Context takeover retains direction-specific sliding-window history across compressed messages; negotiated no-context-takeover resets exactly at the message boundary and uncompressed messages do not alter compression history.
- F056: Negotiated window bits constrain both encoder and decoder sliding windows; dynamic, fixed, and stored DEFLATE blocks are decoded, while the encoder emits valid bounded streams using built-in-only implementation code.
- F057: Per-message compression can be enabled/disabled by send request, with a configurable size threshold, only when the extension was negotiated.
- F058: Decompressed-size, compression-state, and buffered-output limits prevent zip bombs or unbounded memory use and map failures to a deterministic protocol/limit error.
- F059: Extension negotiation and transformation work identically with HTTP/1, HTTP/2, and HTTP/3 opening handshakes.

### Layer 5: message streaming and connection state

- F060: A single connection state machine supports either role and consumes arbitrary input chunks while exposing bounded pending output and pull-based events.
- F061: Whole-message send for text and binary payloads, plus chunked send with explicit first/final boundaries and optional automatic frame sizing.
- F062: Receive modes provide either bounded reassembled messages or start/data/end streaming chunks without requiring whole-message allocation.
- F063: Fragmented messages begin with text/binary opcode, continue only with continuation opcodes, cannot overlap, and preserve the initial message type and compression state.
- F064: Unfragmented messages, zero-length messages, and fragmentation at every byte boundary are supported.
- F065: Control frames can interleave between fragments without disrupting message, UTF-8, compression, or mask state.
- F066: Incremental text validation spans input chunks and fragments; invalid text fails with close code 1007 before invalid application data is delivered.
- F067: Configured frame, message, decompressed-message, fragment-count, input-buffer, output-buffer, and event-queue limits apply before growth and use close code 1009 where appropriate.
- F068: Backpressure stops input consumption before an event/output limit would be exceeded and resumes without data loss when the caller drains output/events.
- F069: Send ordering is deterministic; only one fragmented outgoing data message is active, while control frames can be prioritized between its fragments.
- F070: Explicit Ping and Pong sends accept payloads up to 125 bytes; received Ping/Pong payloads are surfaced unchanged.
- F071: Automatic Pong echoes the Ping application data promptly, can be disabled for manual policy, and works during an open or closing connection while output capacity permits.
- F072: Unsolicited Pong is supported; tracked Ping payloads produce acknowledgement and round-trip/latency events without treating unmatched Pong as an error.
- F073: Optional keepalive uses caller-supplied monotonic time advancement, configurable idle/ping/pong deadlines, unpredictable ping tokens, and deterministic timeout failure without background timers.
- F074: Local close initiation validates code/reason, serializes at most 125 bytes, stops new data messages, and is idempotent.
- F075: A received Close validates code and UTF-8 reason, records remote details, automatically echoes once when necessary, stops data delivery, and completes the two-way close handshake.
- F076: Simultaneous close, close during fragmented messages, control frames during closing, peer EOF after close, premature EOF, transport reset, and close timeout have explicit state transitions.
- F077: Final connection information distinguishes clean close, protocol failure, local/remote codes and reasons, transport EOF/reset, and which side initiated closing; wire-only pseudo-codes 1005/1006/1015 are never transmitted.
- F078: Protocol failures queue the correct Close when possible (1002 protocol, 1007 data, 1009 size), avoid recursive failures, and then await transport termination according to role/transport semantics.
- F079: `EndInput` and `Abort` integrate TCP EOF, HTTP/2/3 clean end-of-stream, and reset/transport failures without pretending an incomplete close handshake was clean.
- F080: Read and write pumping may proceed independently, but each direction is serialized per connection; control sends/close are queued safely through the same output path.
- F081: Connection information exposes negotiated subprotocol/extensions, role, transport, state, pending output/event counts, message counters, and close facts without exposing mutable internal buffers.
- F082: No received payload is retained after its event is drained unless whole-message mode was explicitly selected; send inputs are copied or fully consumed according to an explicit, testable ownership rule.

### Layer 6: interoperability and security

- F083: Header, frame, message, and compression parsing are invariant under every possible transport chunk boundary.
- F084: Client-generated keys/masks use cryptographic randomness; masking keys are not reused by the connection implementation.
- F085: HTTP field names/values, subprotocol names, extension names/parameters, close reasons, and URIs are validated before serialization to prevent request/response splitting or ambiguous negotiation.
- F086: Reserved bits, opcodes, close codes, non-minimal lengths, wrong-role masking, invalid UTF-8, invalid compression, and malformed extension responses fail closed.
- F087: Configurable limits have secure finite defaults and can be tightened but not set to internally unsafe or overflowing values.
- F088: RFC examples and errata, IANA assignments, adversarial vectors, randomized chunk-boundary tests, and client/server loopback tests cover the complete public API behavior.
- F089: Autobahn client and server conformance adapters exercise framing, fragmentation, UTF-8, ping/pong, close, reserved fields, and RFC 7692 case groups.
- F090: Differential handshake/frame fixtures from the surveyed libraries are accepted when conforming and rejected when intentionally malformed; network convenience behavior is not made part of the protocol API.

## Architecture and data flow

```text
HTTP metadata / HTTP/1 bytes                         transport DATA bytes
              |                                               |
              v                                               v
 client/server handshaker -> negotiated connection options -> frame codec
              |                                               |
              v                                               v
  subprotocol + extension pipeline <-> message/fragment state machine
                                                      |
                              +-----------------------+------------------+
                              v                                          v
                    pull WebSocketEvent                         drain encoded bytes
```

1. Handshake codecs parse or build metadata without owning the HTTP connection. HTTP/1 additionally has a bounded incremental text codec.
2. Successful handshake output initializes one role-specific `WebSocketConnection` with immutable negotiated facts copied from the result.
3. The connection feeds bytes into the frame codec, validates the role and extension claims, applies reverse extension transforms, tracks message state, and queues bounded events.
4. Outgoing requests pass through message validation and extensions, fragment as configured, acquire fresh client mask keys, and queue bounded encoded bytes.
5. Callers repeatedly drain events/output and report monotonic time or transport termination. No callbacks, threads, sockets, or hidden timers are owned by `src/`.

## Proposed public API and source files

The signatures below are intentionally coarse. Exact fields and parameters are settled while implementing their task, but the folder, type category, responsibility, unique method naming, and data flow are fixed by this plan.

### `src/Api/Enums`

| File/type | Purpose |
| --- | --- |
| `WebSocketRole.cs` / `WebSocketRole` | Client or server behavior. |
| `WebSocketOpcode.cs` / `WebSocketOpcode` | All four-bit opcode values, including reserved slots. |
| `WebSocketMessageType.cs` / `WebSocketMessageType` | Text or binary application messages. |
| `WebSocketMessageDeliveryMode.cs` / `WebSocketMessageDeliveryMode` | Reassembled or streaming receive delivery. |
| `WebSocketConnectionState.cs` / `WebSocketConnectionState` | Connecting, open, closing, closed, or failed. |
| `WebSocketParseStatus.cs` / `WebSocketParseStatus` | Complete, need input, need output/event capacity, end, or rejected. |
| `WebSocketEventType.cs` / `WebSocketEventType` | Message start/data/end, ping, pong, ping acknowledgement, close, error, and transport end. |
| `WebSocketHandshakeTransport.cs` / `WebSocketHandshakeTransport` | HTTP/1 Upgrade, HTTP/2 Extended CONNECT, or HTTP/3 Extended CONNECT. |
| `WebSocketHandshakeOutcome.cs` / `WebSocketHandshakeOutcome` | Accepted, rejected HTTP response, incomplete, or failed. |
| `WebSocketExtensionStatus.cs` / `WebSocketExtensionStatus` | Offered, negotiated, declined, or invalid. |
| `WebSocketErrorKind.cs` / `WebSocketErrorKind` | Handshake, protocol, UTF-8, compression, limit, state, or transport category. |
| `WebSocketCloseCode.cs` / `WebSocketCloseCode` | Known RFC/IANA close codes, alongside raw integer support in close data. |

Every enum is used by at least one data class or interface as required by the public API policy.

### `src/Api/Static`

| File/type | Public fields only |
| --- | --- |
| `WebSocketProtocolConstants.cs` / `WebSocketProtocolConstants` | RFC GUID, version 13, setting identifier 0x08, header/pseudo-field names, opcode/length bounds, and control payload maximum. |
| `WebSocketCloseCodes.cs` / `WebSocketCloseCodes` | Integer constants for known, reserved, and pseudo close codes while APIs retain custom-code support. |
| `WebSocketDefaults.cs` / `WebSocketDefaults` | Finite default header/frame/message/fragment/buffer/queue and timing limits. |

These types contain no methods or delegate-valued fields.

### `src/Api/Data`

| File/type | Main public fields and use |
| --- | --- |
| `WebSocketHeaderField.cs` / `WebSocketHeaderField` | Name and value; lists preserve duplicate fields and order. |
| `WebSocketHttpRequest.cs` / `WebSocketHttpRequest` | Transport, method/pseudo-fields, target, and regular fields. |
| `WebSocketHttpResponse.cs` / `WebSocketHttpResponse` | Transport, status, fields, and optional rejection body bytes. |
| `WebSocketHttp1ReadResult.cs` / `WebSocketHttp1ReadResult` | Parse status, consumed count, request/response, and structured error. |
| `WebSocketClientHandshakeOptions.cs` / `WebSocketClientHandshakeOptions` | URI, transport, origin, protocols, extensions, extra fields, limits, and peer setting availability. |
| `WebSocketServerHandshakeOptions.cs` / `WebSocketServerHandshakeOptions` | transports, policies, supported protocols/extensions, rejection settings, and limits. |
| `WebSocketHandshakeResult.cs` / `WebSocketHandshakeResult` | Outcome, request/response, selected protocol, negotiated extensions, client key, and error. |
| `WebSocketExtensionParameter.cs` / `WebSocketExtensionParameter` | Parameter name, has-value flag, and decoded value without nullable value types. |
| `WebSocketExtensionOffer.cs` / `WebSocketExtensionOffer` | Extension name and ordered parameters. |
| `WebSocketExtensionNegotiation.cs` / `WebSocketExtensionNegotiation` | Status, accepted configuration, claimed RSV/opcodes, and error. |
| `WebSocketExtensionContext.cs` / `WebSocketExtensionContext` | Role, direction, message type/boundaries, fragment facts, and connection limits for custom transforms. |
| `WebSocketTransformResult.cs` / `WebSocketTransformResult` | Status, consumed/produced counts, transformed bytes, RSV changes, and error. |
| `PerMessageDeflateOptions.cs` / `PerMessageDeflateOptions` | Offer/require flags, context takeover, window bits with explicit has-value flags, threshold, and decompression limit. |
| `WebSocketFrameHeader.cs` / `WebSocketFrameHeader` | FIN/RSV/opcode/mask/key and signed payload length. |
| `WebSocketFrameReadResult.cs` / `WebSocketFrameReadResult` | Parse status, consumed bytes, header, payload chunk, end flag, and error. |
| `WebSocketFrameWriteRequest.cs` / `WebSocketFrameWriteRequest` | Header facts, payload bytes/offset/count, masking key source, and continuation offset. |
| `WebSocketCloseInfo.cs` / `WebSocketCloseInfo` | has-code flag, raw/known code, reason, initiator/sent/received/clean facts. |
| `WebSocketCloseReadResult.cs` / `WebSocketCloseReadResult` | Validity, decoded close info, and error. |
| `WebSocketError.cs` / `WebSocketError` | Error kind, close code, HTTP status, stable identifier, and safe diagnostic text. |
| `WebSocketHeartbeatOptions.cs` / `WebSocketHeartbeatOptions` | enabled flag, idle interval, pong timeout, and token size. |
| `WebSocketConnectionOptions.cs` / `WebSocketConnectionOptions` | Role, transport, delivery/fragmentation/limit settings, auto-pong/close, heartbeat, mask source, and negotiated extensions. |
| `WebSocketConnectionInfo.cs` / `WebSocketConnectionInfo` | Snapshot of state, negotiation, pending counts, message counters, timing, and close/error facts. |
| `WebSocketSendRequest.cs` / `WebSocketSendRequest` | Whole message type/payload, compression choice, and automatic fragmentation size. |
| `WebSocketSendChunk.cs` / `WebSocketSendChunk` | First/final flags, message type, payload, and compression choice for streaming send. |
| `WebSocketEvent.cs` / `WebSocketEvent` | Event type, message/control payload chunk, boundary flags, ping timing, close info, and error. |

Every data class has at least one public non-static field, contains no public methods, implements no interface, and is used in a contract or another data class.

### `src/Api/Contracts`

| File/type | Proposed operations (no overloads) |
| --- | --- |
| `IWebSocketMaskKeySource.cs` / `IWebSocketMaskKeySource` | `CreateMaskingKey`. |
| `IWebSocketMasker.cs` / `IWebSocketMasker` | `ApplyMask` over a span, key, and stream offset. |
| `IWebSocketUtf8Validator.cs` / `IWebSocketUtf8Validator` | `ResetValidation`, `ValidateChunk`, and `CompleteValidation`. |
| `IWebSocketCloseCodec.cs` / `IWebSocketCloseCodec` | `ReadClosePayload`, `MeasureClosePayload`, and `WriteClosePayload`. |
| `IWebSocketFrameCodec.cs` / `IWebSocketFrameCodec` | `ReadFrame`, `MeasureFrame`, `WriteFrame`, and `ResetFrameRead`. |
| `IWebSocketHttp1Codec.cs` / `IWebSocketHttp1Codec` | `ReadClientRequest`, `ReadServerResponse`, `MeasureClientRequest`, `MeasureServerResponse`, `WriteClientRequest`, `WriteServerResponse`, and `ResetHttpRead`. |
| `IWebSocketOriginPolicy.cs` / `IWebSocketOriginPolicy` | `IsOriginAllowed`. |
| `IWebSocketSubprotocolSelector.cs` / `IWebSocketSubprotocolSelector` | `SelectSubprotocol`. |
| `IWebSocketExtensionHeaderCodec.cs` / `IWebSocketExtensionHeaderCodec` | `ParseExtensionFields`, `MeasureExtensionFields`, and `WriteExtensionFields`. |
| `IWebSocketExtension.cs` / `IWebSocketExtension` | Uniquely named offer, acceptance, response-validation, claim, outbound-transform, inbound-transform, message-completion, and reset operations. |
| `IWebSocketClientHandshake.cs` / `IWebSocketClientHandshake` | `CreateHttp1Request`, `ValidateHttp1Response`, `CreateExtendedConnectRequest`, and `ValidateExtendedConnectResponse`. |
| `IWebSocketServerHandshake.cs` / `IWebSocketServerHandshake` | `EvaluateHttp1Request` and `EvaluateExtendedConnectRequest`. |
| `IWebSocketConnection.cs` / `IWebSocketConnection` | `ReceiveBytes`, `EndInput`, `WritePendingBytes`, `GetPendingWriteCount`, `ReadNextEvent`, `GetPendingEventCount`, `SendWholeMessage`, `SendMessageChunk`, `SendPing`, `SendPong`, `StartClose`, `AdvanceTime`, `AbortConnection`, and `GetConnectionInfo`. |

### `src/Api/Exceptions`

| File/type | Use |
| --- | --- |
| `WebSocketProtocolException.cs` / `WebSocketProtocolException` | Invalid wire protocol where an exception-based API boundary is selected. |
| `WebSocketHandshakeException.cs` / `WebSocketHandshakeException` | Invalid handshake construction or terminal validation failure. |
| `WebSocketExtensionException.cs` / `WebSocketExtensionException` | Invalid extension configuration or transform failure. |
| `WebSocketLimitException.cs` / `WebSocketLimitException` | Caller request exceeds a configured local limit before it is queued. |
| `WebSocketStateException.cs` / `WebSocketStateException` | Operation is invalid for the current connection/streaming state. |

Each exception derives directly from `System.Exception` and has one constructor compatible with the syntax policy.

### `src/Api/Classes`

| File/type | Implements |
| --- | --- |
| `CryptographicWebSocketMaskKeySource.cs` / `CryptographicWebSocketMaskKeySource` | `IWebSocketMaskKeySource` using `RandomNumberGenerator`. |
| `WebSocketMasker.cs` / `WebSocketMasker` | `IWebSocketMasker`. |
| `WebSocketUtf8Validator.cs` / `WebSocketUtf8Validator` | `IWebSocketUtf8Validator`. |
| `WebSocketCloseCodec.cs` / `WebSocketCloseCodec` | `IWebSocketCloseCodec`. |
| `WebSocketFrameCodec.cs` / `WebSocketFrameCodec` | `IWebSocketFrameCodec`. |
| `WebSocketHttp1Codec.cs` / `WebSocketHttp1Codec` | `IWebSocketHttp1Codec`. |
| `DefaultWebSocketOriginPolicy.cs` / `DefaultWebSocketOriginPolicy` | `IWebSocketOriginPolicy`. |
| `DefaultWebSocketSubprotocolSelector.cs` / `DefaultWebSocketSubprotocolSelector` | `IWebSocketSubprotocolSelector`. |
| `WebSocketExtensionHeaderCodec.cs` / `WebSocketExtensionHeaderCodec` | `IWebSocketExtensionHeaderCodec`. |
| `PerMessageDeflateExtension.cs` / `PerMessageDeflateExtension` | `IWebSocketExtension`. |
| `WebSocketClientHandshake.cs` / `WebSocketClientHandshake` | `IWebSocketClientHandshake`. |
| `WebSocketServerHandshake.cs` / `WebSocketServerHandshake` | `IWebSocketServerHandshake`. |
| `WebSocketConnection.cs` / `WebSocketConnection` | `IWebSocketConnection`. |

Each class has exactly one public constructor. Optional collaborators and defaults are supplied through the relevant options data class so constructor overloads and public factories are unnecessary.

## Planned internal source files

These files live outside `src/Api`, declare only internal classes, and may be consolidated when a class would otherwise be trivial.

| Area | Planned files |
| --- | --- |
| Primitives | `src/Internal/Binary/NetworkIntegerCodec.cs`, `Base64Codec.cs`, `ByteQueue.cs`, `BoundedByteQueue.cs`, `SpanCopy.cs` |
| Text and HTTP | `src/Internal/Text/Utf8State.cs`, `AsciiToken.cs`, `QuotedStringCodec.cs`, `HeaderListParser.cs`, `Http1Parser.cs`, `Http1Writer.cs`, `WebSocketUriValidator.cs`, `OriginComparer.cs` |
| Handshake | `src/Internal/Handshake/WebSocketAccept.cs`, `HandshakeFieldValidator.cs`, `ExtensionNegotiator.cs`, `SubprotocolNegotiator.cs`, `ExtendedConnectValidator.cs` |
| Framing | `src/Internal/Framing/FrameReadState.cs`, `FrameWriteState.cs`, `FrameValidator.cs`, `MaskingState.cs`, `ClosePayloadValidator.cs` |
| Compression | `src/Internal/Compression/DeflateBitReader.cs`, `DeflateBitWriter.cs`, `DeflateHuffmanTable.cs`, `DeflateSlidingWindow.cs`, `DeflateDecoder.cs`, `DeflateEncoder.cs`, `PerMessageDeflateNegotiator.cs`, `PerMessageDeflateTransform.cs` |
| Connection | `src/Internal/Connection/FragmentationState.cs`, `MessageReceiveState.cs`, `MessageSendState.cs`, `ExtensionPipeline.cs`, `ControlFrameQueue.cs`, `WebSocketEventQueue.cs`, `CloseHandshakeState.cs`, `HeartbeatState.cs`, `ConnectionFailure.cs` |

## Planned test files and coverage model

Tests use NUnit plus any test-only generators/adapters needed. Functional coverage is organized around public API behavior, not source-syntax policy and not performance.

| Area | Planned files |
| --- | --- |
| Public vocabulary | `tests/Api/ProtocolConstantsTests.cs`, `CloseCodesTests.cs`, `DataDefaultsTests.cs`, `OptionsValidationTests.cs`, `ExceptionBoundaryTests.cs` |
| Primitives/codecs | `tests/Api/MaskerTests.cs`, `MaskKeySourceTests.cs`, `Utf8ValidatorTests.cs`, `CloseCodecTests.cs`, `FrameCodecReadTests.cs`, `FrameCodecWriteTests.cs`, `FrameCodecChunkBoundaryTests.cs` |
| HTTP/handshake | `tests/Api/Http1CodecTests.cs`, `ClientHandshakeHttp1Tests.cs`, `ServerHandshakeHttp1Tests.cs`, `ExtendedConnectHttp2Tests.cs`, `ExtendedConnectHttp3Tests.cs`, `OriginPolicyTests.cs`, `SubprotocolSelectorTests.cs`, `HandshakeSecurityTests.cs` |
| Extensions | `tests/Api/ExtensionHeaderCodecTests.cs`, `ExtensionNegotiationTests.cs`, `PerMessageDeflateNegotiationTests.cs`, `PerMessageDeflateTransformTests.cs`, `DeflateVectorTests.cs`, `CustomExtensionIntegrationTests.cs` |
| Connection | `tests/Api/ConnectionSendTests.cs`, `ConnectionReceiveTests.cs`, `ConnectionFragmentationTests.cs`, `ConnectionStreamingTests.cs`, `ConnectionControlTests.cs`, `ConnectionHeartbeatTests.cs`, `ConnectionCloseTests.cs`, `ConnectionFailureTests.cs`, `ConnectionBackpressureTests.cs`, `ConnectionLimitTests.cs`, `ConnectionTransportEndTests.cs`, `ConnectionExtensionTests.cs`, `ConnectionLoopbackTests.cs` |
| Conformance | `tests/Conformance/RfcExampleTests.cs`, `RfcErrataTests.cs`, `AdversarialProtocolTests.cs`, `RandomChunkBoundaryTests.cs`, `ReferenceFixtureTests.cs`, `AutobahnClientAdapter.cs`, `AutobahnServerAdapter.cs`, with configs under `tests/Conformance/Autobahn/` |

Complete public API coverage means every public constructor and method has successful, boundary, partial-input/output, invalid-input, limit, and invalid-state cases where applicable; every public data field, enum value, and static constant is exercised through behavior; both roles and all three handshake transports are covered; every exception-less rejection path and documented thrown exception is asserted; and branch/line reports contain no unexplained uncovered `src/Api` behavior.

## Implementation tasks

### Foundation and public vocabulary

- [DONE] T001: Create the `src/Api` category directories and internal module directories shown in this plan, using a single file-scoped `LibS11.Web.WebSocket` namespace family and no external `src/` dependencies.
- [DONE] T002: Implement all public enums under `src/Api/Enums`, including every wire opcode and known close code, and wire each enum into at least one public data or contract type.
- [DONE] T003: Implement `WebSocketProtocolConstants`, `WebSocketCloseCodes`, and `WebSocketDefaults` as public field-only static data classes with current RFC/IANA values and finite safe defaults.
- [DONE] T004: Implement header, HTTP request/response, parse result, error, and handshake option/result data classes under `src/Api/Data` with explicit flags instead of nullable value types.
- [DONE] T005: Implement extension, frame, close, send, event, heartbeat, connection option, and connection information data classes under `src/Api/Data` with documented ownership and sentinel rules.
- [DONE] T006: Implement the five public exception classes as direct `System.Exception` subclasses with one constructor each and stable diagnostic messages.
- [DONE] T007: Implement internal centralized validation for limits and option combinations, including overflow-safe relationships between frame, message, decompression, buffer, queue, fragment, and timing limits.
- [DONE] T008: Implement bounded byte/event queue primitives that report capacity pressure without dropping bytes or events and do not allocate from untrusted declared lengths.

### Primitive, UTF-8, masking, close, and frame codecs

- [DONE] T009: Implement internal network-order 16-bit and 63-bit integer read/write helpers with sign-bit, overflow, minimal-encoding, and partial-input handling.
- [DONE] T010: Implement an internal canonical Base64 codec/validator sufficient for 16-byte WebSocket nonces and 20-byte SHA-1 accepts without exception-driven malformed-input parsing.
- [DONE] T011: Implement `CryptographicWebSocketMaskKeySource` and verify each call obtains unpredictable bytes from the built-in cryptographic random generator.
- [DONE] T012: Implement `WebSocketMasker.ApplyMask` for in-place spans with arbitrary offsets, zero lengths, all alignments, and identical mask/unmask behavior.
- [DONE] T013: Implement strict incremental UTF-8 state tracking and `WebSocketUtf8Validator` across arbitrary chunks, including final incomplete-sequence detection.
- [DONE] T014: Implement close-code classification for valid wire codes, forbidden pseudo-codes, reserved ranges, registered values, and private/application values.
- [DONE] T015: Implement `WebSocketCloseCodec` measurement, serialization, and parsing with network byte order, UTF-8 reason validation, the 125-byte control limit, empty payload, and one-byte rejection.
- [DONE] T016: Implement internal frame-header read state for every legal header length, partial masks, extended lengths, and preservation of unconsumed trailing bytes.
- [DONE] T017: Implement internal frame validation for FIN/RSV/opcode/control/length rules, role-specific masking, and negotiated extension claims.
- [DONE] T018: Implement incremental frame payload reading and masking across arbitrary input boundaries with accurate consumed counts and configured limit checks before allocation.
- [DONE] T019: Implement frame measurement and incremental writing with canonical lengths, fresh client masking keys, payload chunk offsets, bounded destination spans, and accurate produced counts.
- [DONE] T020: Implement `WebSocketFrameCodec` by composing the frame reader/writer states and reset behavior without leaking mutable internal buffers.

### HTTP/1 and opening handshakes

- [DONE] T021: Implement ASCII token, comma-list, quoted-string, quoted-pair, optional-whitespace, and safe HTTP field-value primitives shared by handshake and extension parsing.
- [DONE] T022: Implement duplicate-preserving header lookup/combination rules with the correct case sensitivity for HTTP field names/tokens and WebSocket subprotocol values.
- [DONE] T023: Implement a bounded incremental HTTP/1 request parser with strict CRLF, request-line validation, origin-form target handling, header byte/count/line limits, and obs-fold/control-character rejection.
- [DONE] T024: Implement a bounded incremental HTTP/1 response parser supporting informational responses followed by one final response, strict status-line/header validation, and precise trailing-byte consumption.
- [DONE] T025: Implement canonical HTTP/1 request/response measurement and writing, rejecting unsafe names/values and preventing contradictory protocol-critical fields.
- [DONE] T026: Implement `WebSocketHttp1Codec` with independent request/response read entry points, write entry points, partial destination support, and reusable reset state.
- [DONE] T027: Implement WebSocket URI validation and normalization for `ws`/`wss`, default/explicit ports, IPv4, bracketed IPv6, IDNA host names, path/query, and forbidden fragments/user-info.
- [DONE] T028: Implement RFC 6455 client nonce generation and SHA-1 accept computation using built-in cryptography plus the internal canonical Base64 codec.
- [DONE] T029: Implement subprotocol offer parsing/validation and `DefaultWebSocketSubprotocolSelector` with configurable client-order/server-order policy and no unoffered selections.
- [DONE] T030: Implement RFC 6454 origin parsing/comparison and `DefaultWebSocketOriginPolicy` with a safe same-origin default plus exact allow-list configuration.
- [DONE] T031: Implement HTTP/1 client handshake request construction with required fields, optional origin/protocols/extensions, validated extra fields, and retained key state for response validation.
- [DONE] T032: Implement HTTP/1 client response validation for status 101, Upgrade/Connection token lists, exact accept, selected subprotocol, negotiated extensions, and non-101 response reporting.
- [DONE] T033: Implement HTTP/1 server request validation for method/version/target/Host/Upgrade/Connection/key/version, origin policy, subprotocol selection, extensions, and application rejection metadata.
- [DONE] T034: Implement HTTP/1 server 101 acceptance and safe 400/403/404-or-caller-status/426 rejection response construction, including version advertisement and no inconsistent critical fields.
- [DONE] T035: Implement `WebSocketClientHandshake` HTTP/1 operations and return complete negotiation metadata suitable for initializing a connection.
- [DONE] T036: Implement `WebSocketServerHandshake` HTTP/1 evaluation and deterministic rejection/error identifiers without changing framing state before acceptance.
- [DONE] T037: Implement common Extended CONNECT pseudo-field and regular-field validation shared by HTTP/2 and HTTP/3 while prohibiting HTTP/1 connection-specific fields and key/accept fields.
- [DONE] T038: Implement HTTP/2 client Extended CONNECT construction/response validation gated by peer setting 0x08, with CONNECT/websocket pseudo-fields, lowercase regular fields, version/origin/protocol/extensions, and 2xx success.
- [DONE] T039: Implement HTTP/2 server Extended CONNECT evaluation, setting/protocol/pseudo-field validation, 2xx acceptance, and structured 4xx/501 rejection.
- [DONE] T040: Implement HTTP/3 Extended CONNECT client/server behavior using RFC 9220's independently advertised 0x08 setting and the shared negotiation rules.
- [DONE] T041: Add handshake-to-connection option transfer that freezes role, transport, selected subprotocol, extension order/configuration, and security limits without sharing mutable handshake collections.

### Extension framework and `permessage-deflate`

- [DONE] T042: Implement complete incremental `Sec-WebSocket-Extensions` grammar parsing for repeated fields, alternatives, parameters, quoted strings/escapes, and malformed input with bounded counts and lengths.
- [DONE] T043: Implement deterministic extension header measurement/serialization and `WebSocketExtensionHeaderCodec`, preserving offer and parameter ordering with canonical safe output.
- [DONE] T044: Implement generic client offer creation, server selection, client response validation, duplicate handling, and rejection of unoffered or incompatible extension configurations.
- [DONE] T045: Implement negotiated RSV/opcode ownership conflict detection and an extension pipeline applying outgoing transforms in order and incoming transforms in reverse.
- [DONE] T046: Implement extension lifecycle state for message start/chunks/completion/reset, fragmentation, control-frame exclusion, connection failure, and bounded transform output.
- [DONE] T047: Implement RFC 7692 offer parsing and client/server negotiation for all four parameters, multiple alternatives, optional `client_max_window_bits`, values 8-15, directionality, and context takeover.
- [DONE] T048: Implement incremental raw DEFLATE bit input/output with LSB-first coding, stored blocks, fixed Huffman blocks, dynamic Huffman tables, end-of-block handling, and malformed-tree rejection.
- [DONE] T049: Implement bounded DEFLATE sliding-window copy/history logic for negotiated window sizes 8-15, overlap copies, distance/length validation, incremental output, and optional history retention.
- [DONE] T050: Implement the raw DEFLATE decoder for stored/fixed/dynamic blocks, arbitrary chunk boundaries, sync-flush tails, configured output limits, and deterministic invalid-stream errors.
- [DONE] T051: Implement a valid incremental raw DEFLATE encoder with bounded LZ77 matching, stored/fixed/dynamic block selection, byte-boundary sync flush, and negotiated window constraints using no external compression library.
- [DONE] T052: Implement RFC 7692 tail removal/restoration, empty-message handling, direction-specific context takeover/reset, and preservation of history across intervening uncompressed messages.
- [DONE] T053: Implement per-message RSV1 rules across fragmented messages, control interleaving, per-send compression opt-out/threshold, and decompressed text validation ordering.
- [DONE] T054: Implement `PerMessageDeflateExtension` negotiation and transform operations over the internal codec with decompression and state-memory limits.
- [DONE] T055: Implement custom extension failure mapping, reset/disposal semantics achievable without destructors, and isolation so one extension cannot silently consume another extension's claimed bits/opcodes.

### High-level connection, messages, controls, and closing

- [DONE] T056: Implement the role-parameterized connection state machine and legal transitions for open, local close, remote close, simultaneous close, protocol failure, clean end, reset, timeout, and abort.
- [DONE] T057: Implement inbound frame pumping for zero/one/many partial frames per call with exact consumption, output/event backpressure, and no loss of trailing bytes.
- [DONE] T058: Implement inbound unfragmented and fragmented message tracking, overlap/continuation validation, message type retention, fragment limits, and control-frame interleaving.
- [DONE] T059: Implement streaming receive events for message start/data/end with bounded chunk ownership and no delivery of invalid UTF-8 or invalid decompressed content.
- [DONE] T060: Implement bounded whole-message reassembly mode with identical validation semantics and message-size enforcement before buffer growth.
- [DONE] T061: Implement whole-message sends for text/binary data with validation, optional negotiated compression, configurable automatic frame size, and role-correct masking.
- [DONE] T062: Implement streaming message sends with first/final boundaries, one active fragmented send, text validation, extension state, and deterministic invalid-state rejection.
- [DONE] T063: Implement a bounded send scheduler that preserves data order, inserts queued control frames between fragments, exposes pending byte counts, and drains through caller-provided spans.
- [DONE] T064: Implement explicit Ping/Pong sends, received Ping/Pong events, payload limits, unsolicited Pong, and configurable automatic Pong echo.
- [DONE] T065: Implement Ping token tracking, acknowledgement/latency events, duplicate outstanding-token handling, and cleanup on close/failure.
- [DONE] T066: Implement caller-driven heartbeat timing for idle Ping, Pong deadline, and timeout failure using monotonic elapsed milliseconds without background timers.
- [DONE] T067: Implement local close validation/serialization, idempotence, data-send shutdown, close-priority output, and recording of local close facts.
- [DONE] T068: Implement received Close validation, remote detail recording, automatic one-time echo, fragmented-message cancellation, and suppression of later application data.
- [DONE] T069: Implement protocol/data/size failure handling that selects close 1002/1007/1009 when output remains possible, emits one error event, prevents recursive close errors, and enters failed/closing state correctly.
- [DONE] T070: Implement close timeout, peer EOF after close, premature EOF as 1006 locally, HTTP/2/3 end-of-stream mapping, reset mapping, and role-correct transport-close guidance in connection information.
- [DONE] T071: Implement connection input/output/event capacity accounting and resume semantics for every limit, including extension expansion and automatic control responses.
- [DONE] T072: Implement connection snapshots with immutable copies of negotiation, pending counts, message/frame counters, heartbeat facts, local/remote close, clean-close flag, and terminal error.
- [DONE] T073: Implement `WebSocketConnection` as the sole public high-level stateful class, composing codecs/extensions from `WebSocketConnectionOptions` and exposing only `IWebSocketConnection` operations.
- [DONE] T074: Implement an internal client/server in-memory loopback transport harness that pumps bounded chunks, time, EOF, reset, and backpressure for functional integration tests.

### Complete public API and protocol tests

- [DONE] T075: Test every public enum value and static field against RFC/IANA numeric/text values by exercising the associated public codec or state behavior.
- [DONE] T076: Test every public data class default, explicit field configuration, collection ownership/copy rule, sentinel/has-value rule, and participation in at least one public contract operation.
- [DONE] T077: Test all option validation boundaries and relationships, including zero/negative/overflowing values, unsafe combinations, tightening limits, and each documented default.
- [DONE] T078: Test every public exception boundary and every exception-less routine rejection result, including stable error kind/identifier/close-or-HTTP status mappings.
- [DONE] T079: Test cryptographic key-source shape and repeated-call non-reuse without asserting deterministic random values; inject deterministic test key sources for all wire-vector tests.
- [DONE] T080: Test masking for RFC vectors, all key alignments, arbitrary positive stream offsets, empty/in-place spans, split payloads, double-application reversal, and client/server integration.
- [DONE] T081: Test UTF-8 validation for all scalar boundaries, overlong encodings, surrogates, maximum code point, invalid starts/continuations, truncation, and every split position.
- [DONE] T082: Test close codec for no code, every registered/reserved/private range, prohibited pseudo-codes, one-byte payload, UTF-8 reasons, and exact 123-byte reason/125-byte payload boundaries.
- [DONE] T083: Test frame reads for every opcode/flag combination, all payload-length boundaries, non-minimal lengths, sign bit, wrong-role masks, reserved claims, controls, partial headers/masks/payloads, multiple frames, and trailing data.
- [DONE] T084: Test frame measurement/writes for every header form, partial destinations, zero payloads, deterministic masking, fresh per-frame keys, continuation mask offsets, and read-after-write round trips.
- [DONE] T085: Generate exhaustive frame codec transport-boundary tests that split each representative encoded frame at every byte boundary and compare results with unsplit decoding.
- [DONE] T086: Test HTTP token/list/quoted-string and HTTP/1 codecs for valid repeated fields, informational responses, exact consumption, partial input/output, line/header limits, bare newline, obs-fold, NUL/control bytes, and injection attempts.
- [DONE] T087: Test RFC 6455 HTTP/1 client handshake request and server response examples, corrected errata vectors, URI/Host variants, required/optional fields, key/accept computation, and trailing first-frame bytes.
- [DONE] T088: Test every HTTP/1 client response rejection condition, including non-101, missing/wrong tokens, missing/duplicate/wrong accept, unsolicited/multiple subprotocols, and invalid/unoffered extensions.
- [DONE] T089: Test every HTTP/1 server request acceptance/rejection condition, including method/version/target/Host/key/version, combined/repeated tokens, origin policy, protocols, extensions, and safe 400/403/426 construction.
- [DONE] T090: Test default and custom origin policies for absent/null origins, normalized scheme/host/port, IPv4/IPv6/IDNA, default ports, opaque origins, and cross-origin rejection.
- [DONE] T091: Test default and custom subprotocol selection for client/server preference, case sensitivity, tokens, duplicates, no overlap, required selection, and prevention of unoffered results.
- [DONE] T092: Test HTTP/2 Extended CONNECT client/server acceptance and every failure for setting state, pseudo-fields, lowercase names, forbidden HTTP/1 fields, key/accept omission, version/origin/protocol/extensions, 2xx status, END_STREAM, and reset.
- [DONE] T093: Test HTTP/3 Extended CONNECT independently advertised setting behavior, RFC 9220 status handling, clean FIN/reset mapping, and equivalence of negotiated WebSocket framing semantics.
- [DONE] T094: Test extension header parsing/writing for repeated fields, multiple alternatives, whitespace, quoted/escaped values, malformed tokens/quotes, duplicates, limits, canonical output, and split input where supported.
- [DONE] T095: Test generic extension negotiation for ordering, decline, unoffered responses, invalid response parameters, multiple same-name offers, incompatible RSV/opcode claims, and outbound/inbound transform order.
- [DONE] T096: Test every RFC 7692 client/server parameter negotiation combination, values 8-15, absent versus valueless `client_max_window_bits`, duplicates, unknowns, multiple offers, and both context-takeover directions.
- [DONE] T097: Test raw DEFLATE decoding against RFC 1951 stored/fixed/dynamic vectors, malformed blocks/trees/distances, every input/output split, all window sizes, and configured expansion limits.
- [DONE] T098: Test raw DEFLATE encoding and round trips for empty, repetitive, incompressible, large-window, block-boundary, sync-flush, and incremental inputs at every negotiated window size.
- [DONE] T099: Test `permessage-deflate` known RFC vectors, tail handling, empty messages, fragmentation/control interleaving, RSV1 rules, compression threshold/opt-out, context retention/reset, uncompressed interleaving, UTF-8 after inflate, and zip-bomb limits.
- [DONE] T100: Test a synthetic custom extension through its complete public lifecycle, negotiation on all transports, incremental transforms, claimed bits/opcodes, ordering with another extension, reset, and failure isolation.
- [DONE] T101: Test connection receives for unfragmented/fragmented text and binary messages, zero-length cases, all chunk boundaries, streaming and reassembled modes, multiple frames per call, and precise input consumption.
- [DONE] T102: Test connection sends for whole/chunked messages, automatic/manual fragmentation, text validation, compression selection, role masks, bounded output drains, and read-back equivalence.
- [DONE] T103: Test connection protocol failures for overlap, orphan continuation, invalid UTF-8, reserved bits/opcodes, wrong masks, non-minimal lengths, invalid compression, and exact 1002/1007/1009 behavior.
- [DONE] T104: Test Ping/Pong handling for explicit sends, auto-pong enabled/disabled, echoed payloads, interleaving, unsolicited/matched/mismatched Pong, duplicate pending tokens, latency, payload limits, and backpressure.
- [DONE] T105: Test deterministic heartbeat time advancement for idle scheduling, partial elapsed steps, matching Pong, missed deadline, disabled timing, closing state, and cleanup without wall-clock sleeps.
- [DONE] T106: Test local, remote, and simultaneous close handshakes for empty/valid/invalid payloads, echo-once, idempotence, blocked output, close during fragmentation, later data suppression, and both roles.
- [DONE] T107: Test clean EOF, premature EOF, abort, HTTP/2/3 FIN, stream reset, close timeout, terminal snapshots, local/remote close details, pseudo-code non-transmission, and permanent post-failure behavior.
- [DONE] T108: Test every configured connection limit and backpressure path at below/equal/above boundaries, drain-and-resume behavior, queued automatic controls, and absence of data/event loss.
- [DONE] T109: Run randomized deterministic client/server loopback sequences over varied chunk/output/event capacities and assert message equivalence, state convergence, bounded storage, mask rules, and clean closing.
- [DONE] T110: Add RFC example, verified errata, IANA assignment, adversarial malformed-input, and surveyed-library interoperability fixtures with provenance pointers into `specs/`.
- [DONE] T111: Add Autobahn fuzzing-client and fuzzing-server adapters/configuration for both library roles and require strict passes for functional RFC 6455 and RFC 7692 groups while excluding performance-only groups.
- [DONE] T112: Run the complete test suite with coverage collection, add missing behavioral cases until every `src/Api` constructor/method/field path and all reachable protocol rejection branches are covered, and record any unreachable generated-code exclusions in test configuration.
- [DONE] T113: Perform a final built-in-dependency audit of `src/websocket.csproj` and compiled `src/` references, then run clean restore/build/test and both Autobahn roles from a fresh test environment.

## Completion criteria

- The public API layout exactly follows `syntax.md`; no public type is misplaced and no public implementation exposes operations outside its interface.
- HTTP/1 Upgrade, HTTP/2 Extended CONNECT, and HTTP/3 Extended CONNECT succeed and fail according to their specifications without requiring an owned network stack.
- Frame/mask/fragment/message/control/close behavior is invariant under arbitrary input and output chunking for both roles.
- Generic extension negotiation is usable by third parties, and built-in `permessage-deflate` covers the complete RFC 7692 parameter and streaming-transform behavior without an external `src/` dependency.
- All public API behavior has functional coverage, representative adversarial coverage, client/server loopback coverage, and applicable Autobahn strict conformance coverage.
- No performance/benchmark, syntax-policy/source-checking, documentation, packaging, or release task is part of this implementation plan.
