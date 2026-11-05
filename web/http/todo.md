# Universal HTTP/1.1 and HTTP/2 library development plan

## Goal and scope

Build a transport-independent .NET library that can parse, validate, transform, reason about, and serialize HTTP request/response messages for HTTP/1.1 and HTTP/2. The normative baseline is RFC 9110, RFC 9111, RFC 9112, RFC 9113, and RFC 7541, with narrowly required supporting specifications for URI syntax, language matching, structured fields, content codings, cache stale extensions, HTTP priorities, extended CONNECT, and the HTTP/2 ORIGIN frame.

The library is universal in endpoint role: the same data model and semantic services work for clients, origin servers, proxies, gateways, and caches. It is not a socket stack or application framework. Callers own I/O, scheduling, persistence, TLS, DNS, and application dispatch; codecs consume and produce bytes, messages, events, and protocol actions.

In scope:

- Incremental, strict-by-default parsing and serialization of requests, responses, informational responses, content, and trailers.
- Complete RFC 9110 message semantics, core fields, method properties, status handling, conditional requests, ranges, negotiation, routing metadata, intermediaries, authentication syntax, and representation metadata.
- Complete RFC 9111 cache-policy calculations and transformations, without prescribing a cache storage backend.
- HTTP/1.1 start lines, field sections, body-length selection, chunked coding, trailers, persistent connections, upgrades, tunnels, pipelining boundaries, and incomplete-message detection.
- HTTP/2 frame codecs, connection preface, settings, streams, flow control, push, errors, graceful shutdown, message mapping, HPACK, deprecated wire priority, RFC 9218 extensible priority, RFC 8441 extended CONNECT, RFC 8336 ORIGIN, and safe preservation of unknown extension frames/settings.
- Registered method, status, field-name, coding, range-unit, and HTTP/2 parameter snapshots from IANA, while continuing to accept syntactically valid extension tokens and unknown numeric values.
- Both whole-message convenience APIs and bounded incremental APIs suitable for fragmented input. Span-based byte input/output is used wherever lifetime rules allow it; retained public data owns its byte arrays.
- Strict resource limits and exception-less result objects for frequent incomplete-input, insufficient-output, selection, and policy outcomes. Exceptions report malformed input, invalid protocol state, limit violations, and unsupported content transformations.

Out of scope:

- Network sockets, connection pools, DNS, TLS implementation, certificate policy, ALPN negotiation itself, HTTP server routing, and HTTP client redirect/retry orchestration. Protocol-name constants and post-negotiation HTTP/2 initialization remain in scope.
- HTTP/3, QUIC, QPACK, WebSocket frame processing, cookie policy/storage, concrete authentication schemes, proxy configuration, cache storage/eviction, and application content/media parsing. Generic extension fields, authentication challenges/credentials, CONNECT tunnels, and WebSocket bootstrapping metadata remain representable.
- Transparent decompression of the obsolete `compress` coding. It is recognized as a registered coding; gzip, deflate/zlib, and Brotli use only built-in .NET implementations.
- Performance and benchmark work, generated documentation sites, packaging, release automation, and syntax-policy/source-checking tests.

## Specification and research baseline

The downloaded source set is indexed in [`specs/README.md`](specs/README.md). Normative RFC text and errata are under `specs/rfcs/` and `specs/errata/`; current registries are under `specs/iana/`; upstream library descriptions are under `specs/references/`; and the complete compressed HPACK interoperability corpus is under `specs/test-vectors/`.

Research-derived design choices:

| Reference | Feature coverage observed | Plan consequence |
| --- | --- | --- |
| llhttp | Incremental request/response parsing, message and chunk boundaries, pause/resume, upgrades, and separately controlled leniencies whose security risks are documented | Use a resumable pull parser, strict defaults, exact consumed-byte reporting, explicit terminal/tunnel states, and no single blanket “lenient” switch |
| nghttp2 | Reusable HTTP/2 framing, public HPACK encoder/decoder, client/server sessions, settings, flow control, push, errors, and extension support | Keep raw frames and HPACK usable independently from the higher connection state machine |
| hyper | Low-level client/server HTTP/1 and HTTP/2 building blocks and transport separation | Do not embed sockets or application routing; expose message and connection drivers that callers feed and drain |
| h2 | Stream-oriented connection driver, explicit send/receive capacity, settings, reset, ping, and trailers | Model connection-level and stream-level flow control explicitly and return protocol actions instead of hiding buffering |
| Netty HTTP codecs | Separate streamed/full messages, duplicate-preserving headers, limits, upgrades, chunk aggregation, content codings, frame listeners, connection state, and local/remote flow controllers | Preserve field lines and order, make aggregation optional, provide configurable limits, and separate low-level codecs from semantic helpers |
| h2spec and http2jp HPACK cases | Section-oriented HTTP/2 conformance and shared-context HPACK interoperability vectors | Build tests directly from RFC sections and upstream vectors, including fragmentation and malformed cases |

## Complete feature inventory

### Common message and field model

- Version-neutral request and response models with method/status control data, target or pseudo-header metadata, ordered header fields, owned content bytes, ordered trailer fields, and protocol version.
- Informational response support, HEAD response context, successful CONNECT tunnel transitions, upgrade transitions, no-content response rules, and explicit message completeness.
- Case-insensitive field-name lookup without changing field values; original field-name spelling and insertion order retained for HTTP/1 serialization and diagnostics.
- Byte-exact field-value preservation across HTTP/1 and HTTP/2, including opaque `obs-text`; names and grammar tokens remain ASCII, prohibited controls/CR/LF/NUL are rejected, and text conversion is reversible rather than culture- or Unicode-normalizing.
- Multiple field lines retained individually. Safe list combination follows field grammar; non-combinable fields are never silently joined. Empty values, optional whitespace, quoted strings, comments where allowed, parameters, token lists, and extension values are preserved.
- Field-name and field-value validation, ASCII/token utilities, whitespace normalization, obsolete folding rejection by default, trailer validation, connection-specific field detection, and configurable limits on line, section, field count, value, trailer, and content sizes.
- Generic parsers/serializers for tokens, quoted strings and escaping, comma/hash lists, semicolon parameters, quality values, HTTP dates, URI references, entity tags, byte ranges/content ranges, authentication challenge/credential syntax, Via elements, and Structured Fields items/lists/dictionaries.
- Registry-backed constants for field names/values, methods, status codes/reason phrases, content/transfer codings, range units, protocol names, HTTP/2 frame/settings/error identifiers, and HPACK static-table data. Unknown extension names and values remain valid when their base grammar allows them.

Core field coverage is explicit rather than inferred from a generic header map:

- Message framing/routing fields: Connection, Content-Length, Host, Max-Forwards, TE, Trailer, Transfer-Encoding, Upgrade, and Via.
- Representation fields: Content-Encoding, Content-Language, Content-Location, Content-Range, Content-Type, ETag, Last-Modified, and Accept-Ranges.
- Request context fields: Expect, From, Referer, Range, User-Agent, and every If-* precondition field.
- Response control fields: Age, Allow, Date, Expires, Location, Retry-After, Server, and Vary.
- Negotiation fields: Accept, Accept-Charset, Accept-Encoding, and Accept-Language.
- Authentication fields: WWW-Authenticate, Authorization, Authentication-Info, Proxy-Authenticate, Proxy-Authorization, and Proxy-Authentication-Info. Their generic scheme syntax is supported; scheme-specific cryptography and credential policy are not.
- Cache fields: Age, Cache-Control, Date, ETag, Expires, Last-Modified, and Vary, including rules for updating or excluding them during validation.
- HTTP/2 control fields: pseudo-fields, Priority, and Cookie combination/splitting rules. All other registered or extension fields remain ordered, validated, and round-trippable even when the library assigns them no special semantics.

### Methods, statuses, request targets, and routing

- Core method semantics for GET, HEAD, POST, PUT, DELETE, CONNECT, OPTIONS, and TRACE: safe, idempotent, cacheable, request-content expectations, response-content rules, and target-form constraints.
- Metadata for every method in the downloaded IANA registry, including registered WebDAV and extension methods, while accepting arbitrary valid method tokens with conservative default semantics.
- Status classification for informational, successful, redirection, client-error, and server-error responses; payload/content rules; cache defaults; redirect method rewriting guidance; retry implications; and semantics for every RFC 9110 status.
- Constants and metadata for every status in the downloaded IANA registry, while preserving unknown three-digit extension codes and reason phrases.
- Explicit RFC 9110 status behavior covers 100/101; 200–206; 300–305, unused 306, 307, and 308; 400–418, 421, 422, and 426; and 500–505. Registry-only codes still receive classification and metadata without inventing semantics beyond their registered specification.
- Origin-form, absolute-form, authority-form, and asterisk-form request targets; scheme/authority/path/query decomposition; URI normalization that does not change semantics; Host and `:authority` reconciliation; effective request URI construction; userinfo rejection; and malformed authority/port handling.
- Origin, authority, connection, proxy, gateway, tunnel, upgrade, and Via semantics; hop-by-hop field removal using both standard names and Connection options; Max-Forwards processing; and forwarding-loop metadata without implementing proxy routing.

### Representations, metadata, content codings, and negotiation

- Representation metadata for Content-Type, Content-Encoding, Content-Language, Content-Length, Content-Location, validators, and range support, keeping representation data distinct from message content and transfer coding.
- Media type/range parsing with type, subtype, wildcards, case rules, quoted parameters, parameter precedence, and quality factors.
- Content coding parsing and registry recognition; gzip, deflate/zlib, and Brotli encode/decode services backed only by `System.IO.Compression`; stacked coding application/removal in the correct order; identity handling; unknown-coding results; decompressed-size limits; and coding-error exceptions.
- Proactive content negotiation across Accept, Accept-Charset, Accept-Encoding, and Accept-Language with absence/empty-value semantics, wildcard matching, exclusions at `q=0`, specificity, media parameters, deterministic tie breaking, server preference, and identity coding rules.
- RFC 4647 basic and extended language filtering plus lookup; language-tag comparison rules without attempting a language-tag registry implementation.
- Vary parsing, wildcard behavior, normalized selection keys, and comparison of selecting request fields. Reactive negotiation metadata is preserved, but no non-standard variant protocol is invented.
- RFC 9218 Priority structured dictionary, urgency, incremental flag, unknown priority parameters, client/server signal merge rules, and priority update metadata; no mandatory scheduling algorithm.

### Conditional requests and ranges

- Strong and weak entity-tag comparison, wildcard tag lists, Last-Modified/Date precision rules, and validator generation inputs without generating application-specific validators.
- If-Match, If-Unmodified-Since, If-None-Match, If-Modified-Since, and If-Range parsing/evaluation with RFC 9110 precedence, recipient role, current representation existence, method, and safe-method distinctions.
- Explicit outcomes for proceed, not modified, precondition failed, ignore Range, and malformed/ignored preconditions.
- Bytes range parsing for closed, open-ended, and suffix ranges; unknown range-unit preservation; satisfiability against selected representation length; overlap/adjacency coalescing policy; ordering; abuse limits; and 206/416 Content-Range construction.
- Single-range and `multipart/byteranges` assembly/parsing, safe boundary generation/validation, per-part metadata, exact encoded-length calculation, and HEAD behavior.

### Caching semantics

- Cache-Control request and response directive parsing with case-insensitive names, quoted/numeric values, duplicate conflict handling, standard directives, extension preservation, field-name lists for `no-cache`/`private`, and overflow-safe delta-seconds.
- Shared/private cache distinction, authorization rules, method/status cacheability, explicit freshness, heuristic cacheability, `must-understand`, `no-store`, `private`, incomplete responses, partial responses, trailers, and unknown-status/method handling.
- Primary cache keys from method and target URI plus Vary-based secondary keys; `Vary: *`; field normalization; multiple stored variants; and invalidation targets for unsafe successful requests, Location, and Content-Location with same-origin checks.
- Current-age calculation using request/response times, Date, Age, apparent age, corrected initial age, response delay, resident time, clock skew protection, and saturating arithmetic.
- Freshness lifetime from s-maxage, max-age, Expires/Date, and bounded configurable heuristic Last-Modified calculations; request max-age/min-fresh/max-stale constraints.
- Stored-response selection; freshness/staleness decisions; no-cache validation; must-revalidate/proxy-revalidate; RFC 5861 stale-if-error and stale-while-revalidate extensions referenced by RFC 9111; disconnected operation; only-if-cached; and warning/diagnostic outcomes without generating obsolete Warning fields.
- Conditional revalidation request construction; 304 metadata merge; prohibited hop-by-hop field removal; retained content/metadata rules; multiple matching stored responses; HEAD metadata refresh; and validation failure handling.
- Cached response construction with updated Age, request-method compatibility, Range from stored complete content, response collapse metadata, and invalidation decisions. Storage I/O, eviction, and concurrency are caller responsibilities.

### HTTP/1.1 syntax, framing, and connection behavior

- Incremental request/status line parsing across arbitrary byte splits, strict CRLF, method/version/status validation, reason-phrase preservation, all four target forms, leading-empty-line handling where RFC 9112 permits it, and HTTP/1.0 compatibility where unambiguous.
- Incremental field-section parsing with configurable limits, exact whitespace rules, rejection of whitespace before colon, safe handling policy for obsolete folding, invalid-name/value rejection, duplicate preservation, and parser events for message head, content chunks, trailers, completion, protocol switch, and tunnel data.
- RFC 9112 message-body length algorithm in exact precedence order, using request method and response context: HEAD, 1xx/204/304, successful CONNECT, Transfer-Encoding versus Content-Length, final chunked coding, valid identical duplicate Content-Length, requests without framing, and close-delimited responses.
- Request-smuggling defenses: reject ambiguous TE/CL combinations, divergent or malformed duplicate Content-Length, chunked not-final, invalid transfer-coding lists, bare LF, invalid control characters, and post-close pipelining by default. Any compatibility policy is individual, explicit, and never changes the strict default.
- Fixed-length content, chunked coding, hexadecimal sizes, chunk extensions, overflow detection, terminal chunk, declared/undeclared trailers, forbidden trailer fields, close-delimited bodies, truncated message detection, and surplus-byte reporting for pipelined messages.
- Transfer coding grammar and ordering; chunked encoder/decoder; registered but unsupported coding recognition; no silent decoding of non-chunked transfer codings.
- Persistence and close decisions for HTTP/1.0 and HTTP/1.1, Connection options, proxy rules, half-close/incomplete input, pipeline message boundaries, 100-continue metadata, Upgrade/101 handoff, and CONNECT tunnel handoff.
- Canonical request/response serialization with injection-safe start lines and fields, caller-selected field-name casing policy, validated framing, automatic or verified Content-Length/chunked choice, chunk extensions, trailers, no-body rules, and bounded Span writes plus allocating convenience results.
- `message/http` single-message and `application/http` pipeline payloads are supported by the same parser/serializer boundaries; MIME packaging beyond those registered wire forms remains a caller concern.

### HPACK

- Prefix integer encode/decode for all prefix widths, overflow/truncation checks, and canonical output.
- Raw and Huffman string literals, the complete RFC 7541 code table, EOS rejection, padding validation, invalid-code detection, and adaptive encoder choice.
- Exact static table indices, connection-scoped dynamic table insertion/eviction, RFC entry-size accounting, zero-size behavior, duplicate entries, lookup by name/value, and independent encoder/decoder state.
- Indexed fields; literal fields with incremental indexing, without indexing, and never indexed; indexed or literal names; ordered duplicates; empty names/values validation at the HTTP/2 mapping layer; and propagation of never-index/sensitive metadata.
- Dynamic table size updates only at legal header-block positions, minimum-first update rule when size changes more than once, SETTINGS limit enforcement, header-list size accounting, compressed-block size limits, integer/string limits, and compression errors.
- Encoder strategy controls for indexing and Huffman use without changing semantics, deterministic output for tests, state reset only on a new connection, and multiple header blocks sharing context.

### HTTP/2 framing, streams, connections, and message mapping

- Incremental nine-byte frame-header parsing and serialization; 24-bit lengths; reserved stream bit handling; configured and peer maximum frame sizes; complete payload availability; and exact surplus-byte reporting.
- DATA, HEADERS, deprecated PRIORITY, RST_STREAM, SETTINGS, PUSH_PROMISE, PING, GOAWAY, WINDOW_UPDATE, and CONTINUATION payloads with every defined flag, padding rules, stream-id constraints, frame-size constraints, and reserved/unknown flag preservation where required.
- Unknown frame types safely ignored at the connection layer and preserved by the raw codec; unknown settings retained and ignored semantically; extension points do not desynchronize frame parsing.
- Client/server connection preface handling, initial SETTINGS requirements, ACK sequencing, setting value validation and application, max frame size, header-table size, header-list advisory limits, initial-window deltas, maximum concurrent streams, enable-push rules, and timeout actions represented without owning timers.
- Stream identifier parity and monotonicity; idle/open/half-closed/reserved/closed states; locally and remotely initiated streams; stream concurrency accounting; closed-stream retention policy; frame legality by state; idle-stream error distinctions; and stream versus connection errors.
- Header-block continuation contiguity across HEADERS/PUSH_PROMISE/CONTINUATION, single connection HPACK context per direction, decoded-list limits, compression error scope, and denial-of-service limits before aggregation.
- Connection- and stream-level flow-control windows; DATA plus padding accounting; WINDOW_UPDATE validation; 31-bit overflow checks; SETTINGS initial-window changes including negative stream windows; explicit receive-capacity release; explicit send-capacity reservation; and actionable blocked/unblocked results without hidden unbounded buffering.
- RST_STREAM cancellation, PING/ACK correlation metadata, GOAWAY last-stream/error/debug data, graceful shutdown, retry eligibility for unprocessed streams, connection termination, and protocol action generation.
- Server push promised-stream validation, associated stream rules, request pseudo-header validation, reserved-state transitions, `SETTINGS_ENABLE_PUSH`, cancellation, and cacheable pushed-response metadata.
- Deprecated RFC 7540 PRIORITY information parsed and serialized for wire compatibility; self-dependency and size validation; optional ignore behavior negotiated by `SETTINGS_NO_RFC7540_PRIORITIES`; RFC 9218 Priority and PRIORITY_UPDATE support without implementing a scheduler.
- RFC 8336 ORIGIN frame parsing/serialization, origin syntax validation, connection origin-set updates, and authority/coalescing advisory results without making certificate or DNS decisions.
- HTTP/2 request pseudo-fields `:method`, `:scheme`, `:authority`, `:path`, and RFC 8441 `:protocol`; response `:status`; pseudo-field ordering/uniqueness; CONNECT and extended CONNECT combinations; informational/final/trailing header sections; and message completeness.
- Lowercase field-name enforcement; rejection of connection-specific fields; TE restricted to `trailers`; Host/authority handling; Cookie field concatenation when mapping to the common model and optional splitting when emitting HTTP/2; Content-Length consistency with DATA; prohibited fields and malformed-message handling distinct from connection errors.
- Bidirectional HTTP/1.1-to-HTTP/2 message conversion: control-data mapping, hop-by-hop removal, Connection-nominated field removal, Host/authority reconciliation, Transfer-Encoding removal, TE trailers preservation, trailer mapping, content-length validation, and reason-phrase loss acknowledged in result metadata.
- A transport-independent connection façade that consumes bytes or decoded frames and yields ordered events/actions: send frame bytes, deliver request/response head, deliver content, deliver trailers, stream complete/reset, release capacity, acknowledge settings/ping, initiate push, send GOAWAY, and report connection/stream errors.

## C# design constraints applied

- All `src/**/*.cs` designs use only the constructs allowed by `syntax.md`: file-scoped namespaces, classes, non-generic interfaces, fields, one constructor at most, block-bodied ordinary methods, positional arguments, allowed statements/expressions, .NET generic reference types, arrays, and Span types.
- No source type is a struct, record, delegate, nested type, generic declaration, property, event, indexer, operator, iterator, async API, nullable value type, `ref`/`out` API, pattern, LINQ implementation, or inheritance hierarchy. No public static method is proposed.
- Public data classes are mutable field bags with at least one public instance field and no public methods. Optional value-type data uses an explicit `HasX` boolean plus a non-nullable value field; optional reference data uses nullable reference fields.
- Operations live in non-inheriting `I...` interfaces under `src/Api/Contracts`; each public implementation class under `src/Api/Classes` implements one interface, has at most one constructor, and exposes exactly the interface methods. Method names are unique—there are no overloads.
- Public static data classes contain fields only. Public enums use default Int32 storage and constant values. Every exception derives directly from `System.Exception` and has only one constructor.
- Non-public helpers live outside `src/Api`. No external package is referenced by `src/http.csproj`; implementation uses the BCL only. Public declarations receive the XML comments required by `syntax.md` as part of their implementation task, not as a separate documentation task.

## Proposed public APIs and planned source files

Method names below indicate the shape of each API, not final signatures. All parsing/writing methods return data result objects so they do not require forbidden `out`/`ref` parameters. Names are intentionally not overloaded.

### `src/Api/Data`

| Planned file | Public data class and role |
| --- | --- |
| `HttpHeaderField.cs` | `HttpHeaderField`: one ordered name/value field line plus sensitive/never-index metadata |
| `HttpHeaders.cs` | `HttpHeaders`: ordered `List<HttpHeaderField>` container used by all messages and field services |
| `HttpRequest.cs` | `HttpRequest`: version, method, target/control metadata, headers, content, trailers, and completeness |
| `HttpResponse.cs` | `HttpResponse`: version, status, reason phrase, headers, content, trailers, and completeness |
| `HttpRepresentation.cs` | `HttpRepresentation`: selectable representation metadata and content used by negotiation/range services |
| `HttpMethodMetadata.cs` | `HttpMethodMetadata`: normalized method token plus safe/idempotent/cacheable/core flags |
| `HttpStatusMetadata.cs` | `HttpStatusMetadata`: code, status class, known reason, content/cache/redirect characteristics |
| `HttpParameter.cs` | `HttpParameter`: name/value/quoted representation for field parameters |
| `HttpMediaType.cs` | `HttpMediaType`: type, subtype, wildcard flags, and parameters |
| `HttpWeightedValue.cs` | `HttpWeightedValue`: token/range, parameters, quality, source order, and specificity |
| `HttpLanguageRange.cs` | `HttpLanguageRange`: language range, quality, order, and wildcard metadata |
| `HttpEntityTag.cs` | `HttpEntityTag`: opaque tag and weak flag |
| `HttpByteRange.cs` | `HttpByteRange`: closed/open/suffix range input or normalized offsets |
| `HttpContentRange.cs` | `HttpContentRange`: range unit, bounds, complete length, and unsatisfied form |
| `HttpAuthenticationValue.cs` | `HttpAuthenticationValue`: scheme, token68, and ordered auth parameters |
| `HttpViaEntry.cs` | `HttpViaEntry`: received protocol, pseudonym/host, port, and comment metadata |
| `HttpPriority.cs` | `HttpPriority`: urgency, incremental flag, presence flags, and unknown structured parameters |
| `StructuredFieldParameter.cs` | `StructuredFieldParameter`: parameter name and bare item |
| `StructuredFieldMember.cs` | `StructuredFieldMember`: item/inner-list member, parameters, and dictionary key/value state |
| `StructuredFieldValue.cs` | `StructuredFieldValue`: item/list/dictionary representation with ordered members |
| `HttpNegotiationCandidate.cs` | `HttpNegotiationCandidate`: representation plus server preference and source order |
| `HttpNegotiationResult.cs` | `HttpNegotiationResult`: selected candidate, match dimensions, quality/specificity, and failure reason |
| `HttpPreconditionContext.cs` | `HttpPreconditionContext`: method, recipient role, current validators, dates, and representation existence |
| `HttpPreconditionDecision.cs` | `HttpPreconditionDecision`: proceed/304/412/range outcome and deciding condition |
| `HttpRangeRequest.cs` | `HttpRangeRequest`: unit, requested ranges, limits, and coalescing policy |
| `HttpRangeDecision.cs` | `HttpRangeDecision`: ignore/satisfiable/unsatisfiable outcome and normalized ranges/content ranges |
| `HttpMultipartRange.cs` | `HttpMultipartRange`: boundary, parts, encoded body, and exact content type/length |
| `HttpCacheControl.cs` | `HttpCacheControl`: all standard request/response directives, presence flags, field lists, and extensions |
| `HttpCacheRequestContext.cs` | `HttpCacheRequestContext`: request, request time, shared/private mode, connectivity, and policy knobs |
| `HttpCacheResponseContext.cs` | `HttpCacheResponseContext`: stored response metadata and request/response/storage/current times |
| `HttpCacheDecision.cs` | `HttpCacheDecision`: storage/use/validate/stale decision with freshness, age, key, and reason data |
| `HttpCacheValidationResult.cs` | `HttpCacheValidationResult`: merged response and affected stored-response metadata |
| `HttpContentCodingResult.cs` | `HttpContentCodingResult`: transformed bytes, applied/remaining coding names, and result status |
| `HttpLimits.cs` | `HttpLimits`: shared signed size/count limits for codecs and semantic parsers |
| `Http1ParserOptions.cs` | `Http1ParserOptions`: endpoint/message mode, response request context, strict compatibility switches, and limits |
| `Http1ParseEvent.cs` | `Http1ParseEvent`: head/content/trailer/completion/switch/tunnel event payload |
| `Http1ParseResult.cs` | `Http1ParseResult`: status, consumed count, emitted events, next-input state, and error metadata |
| `Http1SerializationOptions.cs` | `Http1SerializationOptions`: framing, casing, chunk sizing/extensions, trailer, and target options |
| `Http1SerializationResult.cs` | `Http1SerializationResult`: status, bytes/bytes-written, selected framing, and connection decision |
| `Http1FramingDecision.cs` | `Http1FramingDecision`: body framing kind, declared length, tunnel/no-body state, and validation result |
| `Http1ConnectionDecision.cs` | `Http1ConnectionDecision`: persistence, close, upgrade, tunnel, and reusable-connection outcome |
| `HpackOptions.cs` | `HpackOptions`: allowed table size, header-list/block/string limits, and encoder policies |
| `HpackTableEntry.cs` | `HpackTableEntry`: name/value and RFC size metadata |
| `HpackResult.cs` | `HpackResult`: status, consumed/written counts, headers/bytes, table state summary, and error metadata |
| `Http2Setting.cs` | `Http2Setting`: unsigned identifier/value pair with known-setting metadata |
| `Http2Settings.cs` | `Http2Settings`: ordered settings and effective known values |
| `Http2Frame.cs` | `Http2Frame`: union-style raw/decoded frame data supporting all known and unknown types without inheritance |
| `Http2FrameResult.cs` | `Http2FrameResult`: parse/write status, consumed/written counts, frame/bytes, and error metadata |
| `Http2ConnectionOptions.cs` | `Http2ConnectionOptions`: role, local settings, limits, push/extension policy, and retained closed-stream bounds |
| `Http2ConnectionInput.cs` | `Http2ConnectionInput`: inbound bytes/frame or outbound operation request represented without delegates |
| `Http2Event.cs` | `Http2Event`: delivered frame/message/content/trailer/stream/settings/ping/error event |
| `Http2Action.cs` | `Http2Action`: frame/message/capacity/push/reset/goaway operation for the caller to perform |
| `Http2ConnectionResult.cs` | `Http2ConnectionResult`: consumed count, ordered events/actions, connection status, and error scope |
| `Http2StreamSnapshot.cs` | `Http2StreamSnapshot`: stream id/state/windows/message phase/reset/push state |
| `Http2ConnectionSnapshot.cs` | `Http2ConnectionSnapshot`: local/remote settings, windows, stream counters, preface, and shutdown state |
| `Http2MessageResult.cs` | `Http2MessageResult`: request/response mapping result, header block, stream/end state, and malformed-message metadata |

Every data class above is consumed or returned by at least one contract, satisfying the public-data usage rule.

### `src/Api/Enums`

| Planned file | Public enum purpose |
| --- | --- |
| `HttpProtocolVersion.cs` | HTTP/1.0, HTTP/1.1, and HTTP/2 |
| `HttpMessageKind.cs` | Request, response, informational response, and trailers context |
| `HttpOperationStatus.cs` | Done, need input, need output, rejected, unsupported, and terminal outcomes shared by result objects |
| `HttpRequestTargetForm.cs` | Origin, absolute, authority, and asterisk forms |
| `HttpStatusClass.cs` | Informational through server error plus invalid/extension classification |
| `HttpFieldValueKind.cs` | Token, string, byte sequence, integer, decimal, boolean, date, and display-string variants needed by structured/legacy fields |
| `HttpNegotiationOutcome.cs` | Selected, no acceptable representation, malformed preference, and absent dimension |
| `HttpPreconditionOutcome.cs` | Proceed, not modified, precondition failed, ignore range, and rejected |
| `HttpRangeOutcome.cs` | Ignored, satisfiable, unsatisfiable, malformed, and unsupported unit |
| `HttpCacheOutcome.cs` | Store, do not store, serve fresh, serve stale, validate, only-if-cached miss, invalidate, and unusable |
| `HttpContentCodingOutcome.cs` | Encoded, decoded, identity, unknown, malformed, and limit exceeded |
| `Http1BodyFraming.cs` | None, content length, chunked, close delimited, tunnel, and invalid |
| `Http1ParseEventKind.cs` | Message head, content, trailers, complete, protocol switch, and tunnel bytes |
| `Http1ConnectionMode.cs` | Persistent, close, upgraded, tunneled, and unusable |
| `HpackIndexingMode.cs` | Incremental, without indexing, never indexed, and automatic |
| `Http2EndpointRole.cs` | Client and server |
| `Http2FrameType.cs` | All RFC 9113 frames plus ORIGIN, PRIORITY_UPDATE, and unknown |
| `Http2ErrorCode.cs` | Registered RFC 9113 errors plus unknown |
| `Http2StreamState.cs` | Idle, reserved local/remote, open, half-closed local/remote, and closed |
| `Http2EventKind.cs` | Connection/frame/message/stream/settings/ping/push/error event kinds |
| `Http2ActionKind.cs` | Send/ack/reset/goaway/capacity/push/message action kinds |
| `Http2ErrorScope.cs` | None, stream, and connection |

Every enum above appears in an interface signature or public data class.

### `src/Api/Static`

| Planned file | Public field-only static data class |
| --- | --- |
| `HttpMethods.cs` | String constants for every downloaded IANA method |
| `HttpStatusCodes.cs` | Integer constants for every downloaded IANA status code |
| `HttpReasonPhrases.cs` | Conventional reason strings keyed by named fields, never used as normative semantics |
| `HttpHeaderNames.cs` | Core and registered field-name constants from the IANA snapshot |
| `HttpHeaderValues.cs` | Common standard tokens such as close, keep-alive, trailers, bytes, and directive names |
| `HttpContentCodings.cs` | Registered content-coding name constants |
| `HttpTransferCodings.cs` | Registered transfer-coding name constants |
| `HttpRangeUnits.cs` | Registered range-unit constants |
| `HttpProtocolNames.cs` | HTTP/1.1, h2, and related protocol/upgrade string constants |
| `Http2Constants.cs` | Preface bytes, default/max sizes and windows, masks, flags, and numeric protocol bounds |
| `Http2SettingIdentifiers.cs` | Registered HTTP/2 settings identifiers |
| `HpackStaticTable.cs` | Readonly RFC 7541 static-table entries and Huffman code/length arrays as fields only |

### `src/Api/Contracts` and matching `src/Api/Classes`

| Contract file and proposed methods | Implementation file | Responsibility |
| --- | --- | --- |
| `IHttpHeaderService.cs`: `ValidateFieldName`, `ValidateFieldValue`, `AddField`, `SetField`, `RemoveFields`, `FindFields`, `CombineFieldValues`, `ValidateTrailers` | `HttpHeaderService.cs` | Ordered header manipulation, lookup, validation, combination, and trailer rules |
| `IHttpFieldValueCodec.cs`: distinct parse/serialize methods for dates, media types, weighted lists, entity tags, ranges, auth values, Via, Cache-Control, and Priority | `HttpFieldValueCodec.cs` | Legacy HTTP field-value grammar and canonical serialization |
| `IStructuredFieldCodec.cs`: `ParseStructuredItem`, `ParseStructuredList`, `ParseStructuredDictionary`, `SerializeStructuredValue` | `StructuredFieldCodec.cs` | RFC 9651 item/list/dictionary codec used by priorities and extensions |
| `IHttpSemantics.cs`: `DescribeMethod`, `DescribeStatus`, `ClassifyStatus`, `DetermineRequestTarget`, `BuildEffectiveRequestUri`, `DetermineResponseContent`, `DetermineHopFields`, `NormalizeForwardedMessage` | `HttpSemantics.cs` | Method/status/message/routing/intermediary rules |
| `IHttpContentNegotiator.cs`: `SelectMediaType`, `SelectCharset`, `SelectContentCoding`, `SelectLanguage`, `SelectRepresentation`, `BuildVaryKey`, `CompareVaryKey` | `HttpContentNegotiator.cs` | Proactive negotiation and Vary keys |
| `IHttpPreconditionEvaluator.cs`: `CompareStrongTag`, `CompareWeakTag`, `EvaluatePreconditions`, `EvaluateIfRange` | `HttpPreconditionEvaluator.cs` | Validators and RFC precondition precedence |
| `IHttpRangeService.cs`: `ParseRangeRequest`, `NormalizeByteRanges`, `BuildContentRange`, `BuildMultipartRanges`, `ParseMultipartRanges` | `HttpRangeService.cs` | Range selection and multipart byte ranges |
| `IHttpCachePolicy.cs`: `ParseCacheControl`, `EvaluateStorage`, `BuildCacheKey`, `CalculateCurrentAge`, `CalculateFreshnessLifetime`, `EvaluateReuse`, `BuildRevalidationRequest`, `MergeNotModifiedResponse`, `EvaluateInvalidation`, `BuildCachedResponse` | `HttpCachePolicy.cs` | RFC 9111 policy engine without storage |
| `IHttpContentCoding.cs`: `EncodeContent`, `DecodeContent`, `EncodeCodingStack`, `DecodeCodingStack` | `HttpContentCoding.cs` | BCL-only gzip, deflate/zlib, Brotli, and identity transformations |
| `IHttp1Parser.cs`: `ParseHttp1Bytes`, `CompleteHttp1Input`, `ResetHttp1Parser` | `Http1Parser.cs` | Stateful incremental HTTP/1 request/response parser |
| `IHttp1Serializer.cs`: `AnalyzeHttp1Framing`, `AnalyzeHttp1Connection`, `SerializeHttp1Request`, `SerializeHttp1Response`, `WriteHttp1Request`, `WriteHttp1Response` | `Http1Serializer.cs` | Validated framing and allocating/Span serialization |
| `IHpackDecoder.cs`: `DecodeHeaderBlock`, `UpdateDecoderTableLimit`, `InspectDecoderTable` | `HpackDecoder.cs` | Stateful HPACK decoding and limit enforcement |
| `IHpackEncoder.cs`: `EncodeHeaderBlock`, `UpdateEncoderTableLimit`, `InspectEncoderTable` | `HpackEncoder.cs` | Stateful HPACK encoding and sensitive/index policy |
| `IHttp2FrameCodec.cs`: `ParseHttp2Frame`, `SerializeHttp2Frame`, `WriteHttp2Frame` | `Http2FrameCodec.cs` | Stateless incremental frame parsing and serialization |
| `IHttp2MessageCodec.cs`: `DecodeHttp2RequestHeaders`, `DecodeHttp2ResponseHeaders`, `DecodeHttp2Trailers`, `EncodeHttp2RequestHeaders`, `EncodeHttp2ResponseHeaders`, `EncodeHttp2Trailers`, `ConvertHttp1ToHttp2`, `ConvertHttp2ToHttp1` | `Http2MessageCodec.cs` | HTTP/2 pseudo-fields, semantic validation, and cross-version mapping |
| `IHttp2Connection.cs`: `ProcessHttp2Input`, `QueueHttp2Action`, `ReleaseHttp2Capacity`, `InspectHttp2Connection`, `ResetHttp2Connection` | `Http2Connection.cs` | Client/server connection, stream, settings, flow-control, push, and error state machine |

No implementation class needs to implement multiple interfaces, eliminating combined-interface name collisions.

### `src/Api/Exceptions`

Each planned exception directly derives from `System.Exception`, has one constructor, and carries public diagnostic fields where useful: `HttpProtocolException.cs`, `HttpParseException.cs`, `HttpLimitExceededException.cs`, `HttpFieldException.cs`, `HttpContentCodingException.cs`, `Http1FramingException.cs`, `HpackException.cs`, `Http2ConnectionException.cs`, and `Http2StreamException.cs`.

### Internal source files outside `src/Api`

These are internal classes only: `src/Internal/Ascii.cs`, `OctetReader.cs`, `OctetWriter.cs`, `BoundedByteBuffer.cs`, `IntegerParser.cs`, `HttpGrammar.cs`, `HeaderStore.cs`, `FieldValueParser.cs`, `FieldValueWriter.cs`, `HttpDateCodec.cs`, `UriCodec.cs`, `NegotiationMatcher.cs`, `CacheCalculator.cs`, `CompressionCodec.cs`, `Http1ParserState.cs`, `Http1LineParser.cs`, `Http1ChunkCodec.cs`, `Http1Framing.cs`, `HpackIntegerCodec.cs`, `HpackHuffmanCodec.cs`, `HpackDynamicTable.cs`, `HpackHeaderBlockCodec.cs`, `Http2FramePayloadCodec.cs`, `Http2StreamStateMachine.cs`, `Http2FlowControl.cs`, `Http2SettingsState.cs`, `Http2HeaderSequence.cs`, and `Http2ConnectionState.cs`.

## Implementation tasks

Tasks are ordered by dependency. A task is complete only when its public source declarations include required XML comments, use no external source dependency, preserve all earlier behavior, and add or update focused tests unless a later test task explicitly owns the exhaustive matrix.

### Foundation and common API

- [DONE] T001: Create the planned `src/Api` and `src/Internal` directory structure and establish the file-scoped namespace scheme without adding any package reference to `src/http.csproj`.
- [DONE] T002: Implement the common protocol, message, operation-status, target-form, status-class, negotiation, precondition, range, cache, and content-coding enums and wire each enum into a data or contract type.
- [DONE] T003: Implement the HTTP/1, HPACK, and HTTP/2 enums with exact RFC numeric values, including unknown-value representation paths that do not require nullable value types.
- [DONE] T004: Implement field-only static classes for IANA method, status, field-name, content-coding, transfer-coding, range-unit, HTTP/2 setting, and protocol constants from the downloaded snapshots.
- [DONE] T005: Implement `Http2Constants`, conventional reason phrases, and immutable HPACK static/Huffman arrays as public field-only static data.
- [DONE] T006: Implement the common header, request, response, representation, method metadata, status metadata, parameter, media, weighted value, language, entity-tag, range, authentication, Via, and priority data classes.
- [DONE] T007: Implement the structured-field, negotiation, precondition, range-decision, multipart-range, cache, content-coding, and shared-limit data classes with explicit presence flags for optional value types.
- [DONE] T008: Implement all HTTP/1 parser, event, framing, connection, serialization option, and result data classes.
- [DONE] T009: Implement all HPACK option, table-entry, and result data classes.
- [DONE] T010: Implement all HTTP/2 setting, frame, connection input/options/result, event/action, stream/connection snapshot, and message result data classes.
- [DONE] T011: Implement every planned exception as a direct `System.Exception` subclass with one constructor and stable diagnostic fields.
- [DONE] T012: Declare all planned operation interfaces with unique, non-overloaded method names and result-object return patterns compatible with the simplified syntax.
- [DONE] T013: Implement internal ASCII case folding/comparison, token/quoted character classification, decimal/hexadecimal parsing, saturating signed arithmetic, and protocol-safe byte writing using Span where appropriate.
- [DONE] T014: Implement bounded byte accumulation and octet reader/writer helpers that distinguish incomplete input, insufficient output, malformed input, and configured limit exhaustion.
- [DONE] T015: Implement the ordered header store and `HttpHeaderService`, including byte-exact value/obs-text preservation, duplicate retention, case-insensitive ASCII lookup, add/set/remove, safe field combination, original spelling, prohibited-control rejection, and trailer rules.

### Field syntax and common semantics

- [DONE] T016: Implement generic token, quoted-string, escape, optional-whitespace, comma-list, hash-list, comment, and semicolon-parameter parsing with complete-consumption and limit checks.
- [DONE] T017: Implement canonical field-value serialization that prevents CR/LF injection and preserves extension tokens and quoted values without lossy normalization.
- [DONE] T018: Implement all three accepted HTTP-date input forms, preferred IMF-fixdate output, invalid calendar rejection, UTC normalization, and second-precision comparison support.
- [DONE] T019: Implement URI-reference, scheme, authority, host, port, path, query, and percent-encoding validation needed for effective request URIs and HTTP/2 pseudo-fields, without taking a dependency outside .NET.
- [DONE] T020: Implement media type/range and quality-value parsing/serialization, including wildcards, quoted parameters, duplicate/conflicting `q`, precision bounds, and deterministic specificity metadata.
- [DONE] T021: Implement entity-tag lists and strong/weak comparison primitives, including wildcard, opaque-tag grammar, and malformed-list handling.
- [DONE] T022: Implement Range, Content-Range, Accept-Ranges, and range-unit parsing/serialization with overflow-safe signed lengths and extension-unit preservation.
- [DONE] T023: Implement generic authentication challenge/credentials parsing and serialization for schemes, token68, quoted auth parameters, multiple challenges, and proxy variants without implementing an authentication algorithm.
- [DONE] T024: Implement Via, Allow, Connection, Upgrade, TE, Trailer, Expect, Max-Forwards, Location, Content-Location, Retry-After, and Vary grammar utilities required by semantic services.
- [DONE] T025: Implement RFC 9651 Structured Fields bare items, parameters, inner lists, lists, and dictionaries, including canonical serialization, byte sequences, dates, display strings, numeric bounds, duplicate policy, and full malformed-input rejection.
- [DONE] T026: Implement Cache-Control parsing/serialization for all RFC 9111 directives, quoted field-name lists, delta-seconds, duplicate conflicts, standard extension directives, and lossless unknown extensions.
- [DONE] T027: Implement RFC 9218 Priority parsing/serialization on top of Structured Fields, including default urgency/incremental semantics, bounds, unknown parameters, and client/server merge rules.
- [DONE] T028: Implement registry-backed method lookup for core and all downloaded IANA methods, conservative metadata for unknown valid tokens, and RFC 9110 safe/idempotent/cacheable semantics.
- [DONE] T029: Implement status validation, classification, metadata lookup for all downloaded IANA codes, unknown three-digit code support, content-allowed rules, cache defaults, redirect categories, and reason-phrase handling.
- [DONE] T030: Implement request-target form classification, method/form compatibility, Host requirements, authority comparison, effective request URI construction, and userinfo/malformed authority rejection.
- [DONE] T031: Implement response-content and message-completeness semantics for HEAD, CONNECT, 1xx, 204, 205, 304, partial responses, informational responses, and trailers.
- [DONE] T032: Implement intermediary semantics for hop-by-hop fields, Connection-nominated fields, TE trailers, Via metadata, Max-Forwards changes, forwarding normalization, and safe same-origin comparison.
- [DONE] T033: Implement representation metadata extraction and application for Content-Type, Content-Encoding, Content-Language, Content-Length, Content-Location, ETag, Last-Modified, and Accept-Ranges.
- [DONE] T034: Implement BCL-only identity, gzip, deflate/zlib, and Brotli content encode/decode operations, ordered coding stacks, unknown coding results, malformed streams, and decompressed-output limits.

### Negotiation, conditions, ranges, and caching

- [DONE] T035: Implement Accept media-range matching with wildcards, media parameters, quality, specificity, client order, server preference, exclusions, absent/empty fields, and deterministic selection.
- [DONE] T036: Implement Accept-Charset and Accept-Encoding selection with wildcard, identity, `q=0`, absent/empty field, stacked available coding, and deterministic preference rules.
- [DONE] T037: Implement Accept-Language parsing and RFC 4647 basic filtering, extended filtering, and lookup with wildcard, quality, specificity, truncation, and deterministic selection.
- [DONE] T038: Implement combined representation selection across media type, charset, content coding, and language with a transparent score explanation and explicit no-acceptable-representation outcome.
- [DONE] T039: Implement Vary parsing, wildcard behavior, normalized selecting-field keys, absent-versus-empty distinctions, duplicate field handling, and stored/request key comparison.
- [DONE] T040: Implement If-Match, If-Unmodified-Since, If-None-Match, If-Modified-Since, and If-Range parsing plus individual condition evaluators with weak/strong and date rules.
- [DONE] T041: Implement the complete RFC 9110 precondition evaluation order for origin servers and caches, including method distinctions and proceed, 304, 412, and ignore-Range results.
- [DONE] T042: Implement byte-range normalization against representation length, suffix/open ranges, satisfiability, stable ordering, configurable overlap/adjacency coalescing, and anti-abuse count limits.
- [DONE] T043: Implement single-range 206 and unsatisfied 416 metadata construction, Content-Range serialization, and HEAD behavior.
- [DONE] T044: Implement multipart/byteranges boundary generation/validation, exact length calculation, assembly, and parsing with per-part Content-Type and Content-Range metadata.
- [DONE] T045: Implement cache storage eligibility for private/shared caches, methods, status codes, authorization, explicit/heuristic cacheability, incomplete/partial responses, no-store/private/must-understand, and extension directives.
- [DONE] T046: Implement primary cache-key construction, normalized target URI, method compatibility, Vary secondary keys, variant matching, and `Vary: *` refusal.
- [DONE] T047: Implement RFC 9111 current-age calculation from Date, Age, request/response timestamps, apparent age, corrected age, response delay, resident time, clock skew, and saturation.
- [DONE] T048: Implement freshness lifetime calculation using s-maxage, max-age, Expires, Date, and configurable bounded Last-Modified heuristics, including explicit versus heuristic provenance.
- [DONE] T049: Implement fresh stored-response reuse decisions with request max-age/min-fresh, response no-cache, request no-cache, validation requirements, method/status compatibility, and only-if-cached misses.
- [DONE] T050: Implement stale response decisions for max-stale, must-revalidate, proxy-revalidate, stale-while-revalidate, stale-if-error, disconnected operation, and shared/private distinctions.
- [DONE] T051: Implement conditional revalidation request construction that selects validators, preserves request semantics, and does not forward inappropriate stored fields.
- [DONE] T052: Implement 304 response merge across one or multiple matching stored responses, excluding hop-by-hop fields, retaining stored content, refreshing metadata/timestamps, and handling conflicting validators safely.
- [DONE] T053: Implement cached-response construction with updated Age, HEAD conversion, conditional outcomes, complete stored-content ranges, and caller-visible collapse/revalidation metadata.
- [DONE] T054: Implement invalidation decisions for successful unsafe methods and same-origin Location/Content-Location targets, leaving storage mutation to the caller.

### HTTP/1.1 codec and framing

- [DONE] T055: Implement the resumable HTTP/1 parser state engine with exact consumed-byte accounting, ordered pull events, configurable request/response mode, reset, terminal states, and arbitrary input fragmentation.
- [DONE] T056: Implement strict request-line parsing for methods, all request-target forms, HTTP version, permitted whitespace, line limits, leading empty-line policy, and injection rejection.
- [DONE] T057: Implement strict status-line parsing for HTTP version, exactly three-digit status, optional reason phrase preservation, field-independent limits, and invalid control-character rejection.
- [DONE] T058: Implement incremental HTTP/1 field-section parsing with CRLF enforcement, whitespace-before-colon rejection, duplicate preservation, optional explicit obsolete-fold normalization mode, and head events.
- [DONE] T059: Enforce initial-line, individual field, total field section, field count, content, chunk, trailer, and buffered-event limits consistently in HTTP/1 parsing.
- [DONE] T060: Implement the RFC 9112 message-body-length precedence algorithm with request method and response context for HEAD, 1xx/204/304, CONNECT, Transfer-Encoding, Content-Length, unframed requests, and close-delimited responses.
- [DONE] T061: Implement strict Transfer-Encoding and Content-Length normalization and ambiguity rejection, including TE/CL conflict, divergent duplicates, comma-list duplicates, overflow, signs, junk, chunked ordering, and unsupported request transfer codings.
- [DONE] T062: Implement incremental fixed-length content delivery with content events, exact completion, zero length, surplus pipeline bytes, and premature EOF detection.
- [DONE] T063: Implement chunked decoding with fragmented sizes/data/CRLF, extensions, unsigned hexadecimal overflow checks, configured size limits, zero chunk, and exact terminal boundaries.
- [DONE] T064: Implement chunk-trailer parsing with declaration checks, forbidden-field rejection, limits, ordered trailer events, and completion events.
- [DONE] T065: Implement close-delimited response content, explicit end-of-input completion, incomplete fixed/chunked detection, and unusable-connection results after truncation.
- [DONE] T066: Implement persistent-connection decisions for HTTP/1.0 and HTTP/1.1, Connection options, proxy recipient behavior, self-delimited messages, and close-after-message semantics.
- [DONE] T067: Implement pipeline boundary handling so one feed can complete one message and report all unconsumed bytes without consuming a later request/response or post-close data.
- [DONE] T068: Implement 100-continue metadata, Upgrade/101 recognition, caller handoff at the exact protocol-switch byte, successful CONNECT tunnel handoff, and terminal opaque tunnel-byte events.
- [DONE] T069: Implement request serialization with validated method/target/version/Host, canonical CRLF, safe fields, selected/verified Content-Length or chunked framing, no implicit unsupported transfer coding, and allocating output.
- [DONE] T070: Implement response serialization with status/reason validation, request-method context, informational/no-content/HEAD/CONNECT rules, correct framing, persistence fields, and allocating output.
- [DONE] T071: Implement bounded Span request/response writers that return insufficient-output status and exact required/written counts without partial semantic corruption.
- [DONE] T072: Implement chunked serialization with configurable chunk partitioning, validated chunk extensions, terminal chunk, declared/valid trailers, and exact encoded-size analysis.
- [DONE] T073: Implement HTTP/1 normalization helpers that remove or replace ambiguous framing fields only when explicitly serializing a validated message and never silently “repair” hostile parsed input.

### HPACK

- [DONE] T074: Implement HPACK prefix integer decode/encode for every prefix width with truncation, continuation, overflow, non-canonical input acceptance where RFC-compliant, and canonical output.
- [DONE] T075: Implement the full HPACK Huffman decoder table/state with EOS-symbol rejection, invalid transition detection, and terminal padding validation.
- [DONE] T076: Implement HPACK Huffman encoding and raw-versus-Huffman string selection with exact length calculation and deterministic policy control.
- [DONE] T077: Implement RFC 7541 static-table exact/index/name lookup backed by the public readonly table data.
- [DONE] T078: Implement independent encoder and decoder dynamic tables with RFC entry sizing, insertion, eviction, duplicate entries, zero limit, resize, indexed lookup, and bounded inspection snapshots.
- [DONE] T079: Implement HPACK decoding for indexed fields and all literal representations, indexed/literal names, empty values, ordered duplicates, and never-index propagation.
- [DONE] T080: Implement legal dynamic table size update placement, multiple-update minimum-first rule, SETTINGS maximum enforcement, and connection-scoped decoder state across header blocks.
- [DONE] T081: Enforce compressed block, decoded header count/list size, string size, integer length, and dynamic table bounds with HPACK versus HTTP/2 error metadata kept distinct.
- [DONE] T082: Implement HPACK encoding strategies for exact indexed match, indexed-name literals, incremental indexing, without indexing, never indexed, sensitive fields, and configurable Huffman usage.
- [DONE] T083: Implement encoder table-limit changes and required emitted size updates while preserving synchronized context across consecutive header blocks and never resetting state per stream.

### HTTP/2 frame codec and connection state

- [DONE] T084: Implement incremental HTTP/2 nine-byte frame-header parsing with 24-bit length, type/flags, reserved-bit handling, stream id, peer/configured frame limits, incomplete input, and exact consumption.
- [DONE] T085: Implement DATA payload parsing/serialization with END_STREAM, PADDED, pad length, application data extraction, padding flow-control accounting, and stream-zero rejection.
- [DONE] T086: Implement HEADERS and CONTINUATION payload parsing/serialization with END_STREAM, END_HEADERS, PADDED, deprecated priority fields, fragment extraction, padding, and stream constraints.
- [DONE] T087: Implement deprecated PRIORITY payload parsing/serialization with exclusive dependency, weight mapping, self-dependency rejection, exact size, and negotiated ignore metadata.
- [DONE] T088: Implement RST_STREAM payload parsing/serialization with exact size, nonzero stream, registered/unknown error code preservation, and stream-error metadata.
- [DONE] T089: Implement SETTINGS payload parsing/serialization with ACK rules, six-byte entries, stream-zero requirement, duplicate/unknown setting preservation, and per-setting value validation.
- [DONE] T090: Implement PUSH_PROMISE payload parsing/serialization with promised id, reserved bit, padding, header fragment, stream constraints, and END_HEADERS.
- [DONE] T091: Implement PING payload parsing/serialization with exact opaque eight bytes, ACK, stream-zero requirement, and caller correlation metadata.
- [DONE] T092: Implement GOAWAY payload parsing/serialization with last stream id, error code, bounded opaque debug data, stream-zero requirement, and reserved-bit handling.
- [DONE] T093: Implement WINDOW_UPDATE payload parsing/serialization with exact size, 31-bit nonzero increment, connection/stream target, overflow metadata, and reserved-bit handling.
- [DONE] T094: Implement generic unknown-frame preservation/serialization and safe ignore semantics, retaining type byte, flags, stream id, and bounded opaque payload.
- [DONE] T095: Implement RFC 9218 PRIORITY_UPDATE frame payload parsing/serialization, request/push element id rules, ASCII Structured Field value validation, and frame/connection error distinctions.
- [DONE] T096: Implement RFC 8336 ORIGIN frame payload parsing/serialization with repeated length-prefixed ASCII origins, scheme/host/port validation, stream-zero/flag rules, and malformed-length handling.
- [DONE] T097: Complete `Http2FrameCodec` dispatch and bounded Span writing for every known/extension/unknown frame with exact size calculation and round-trip preservation.
- [DONE] T098: Implement client/server connection preface state, initial SETTINGS exchange requirements, unexpected HTTP/1 detection metadata, and connection initialization actions.
- [DONE] T099: Implement local/remote stream id allocation and validation, parity, monotonic peer ids, idle/open/reserved/half-closed/closed transitions, and bounded closed-stream retention.
- [DONE] T100: Implement frame legality by stream state and endpoint role, including idle versus closed distinctions, frame-specific state transitions, and correct stream/connection error scope.
- [DONE] T101: Implement contiguous header-block sequencing across HEADERS/PUSH_PROMISE/CONTINUATION, interleaving rejection, per-direction HPACK use, END_HEADERS completion, and aggregate limits.
- [DONE] T102: Implement SETTINGS state application and ACK sequencing, including header-table changes, initial-window deltas, max frame size, max concurrent streams, header-list advisory limits, enable push, extended CONNECT, and no-RFC7540-priorities.
- [DONE] T103: Implement inbound connection/stream flow control with DATA padding accounting, window decrement, explicit application capacity release, batched WINDOW_UPDATE actions, overflow checks, and closed-stream behavior.
- [DONE] T104: Implement outbound connection/stream flow control with explicit capacity reservation, remote WINDOW_UPDATE, negative windows after initial-window reduction, blocked/unblocked action results, and no unbounded hidden buffering.
- [DONE] T105: Implement maximum concurrent stream enforcement, refused-stream behavior, locally and remotely initiated counts, reserved push streams, and stream cleanup.
- [DONE] T106: Implement RST_STREAM cancellation, late-frame handling, PING ACK validation, settings/ping timeout action representation, and precise protocol error codes.
- [DONE] T107: Implement GOAWAY receive/send state, monotonically decreasing last-stream rules, graceful two-stage shutdown support, unprocessed-stream retry eligibility, debug data, and terminal connection actions.
- [DONE] T108: Implement server push validation, PUSH_PROMISE request checks, associated/promised stream transitions, enable-push rules, cancellation, response completion, and pushed-response cache metadata.
- [DONE] T109: Implement deprecated priority acceptance/ignore behavior plus `SETTINGS_NO_RFC7540_PRIORITIES` consistency and RFC 9218 priority merge/update state without a scheduling algorithm.
- [DONE] T110: Implement ORIGIN connection origin-set updates and advisory authority/coalescing outcomes without making DNS, certificate, TLS, or connection-pool decisions.
- [DONE] T111: Implement connection error and stream error action generation, GOAWAY/RST_STREAM selection, error-code propagation, last processed stream tracking, and no further frame generation after terminal failure except required shutdown output.

### HTTP/2 message mapping and façade

- [DONE] T112: Implement HTTP/2 request pseudo-header decoding/validation for normal requests, CONNECT, and OPTIONS asterisk form, including ordering, uniqueness, required/forbidden combinations, and URI/authority rules.
- [DONE] T113: Implement HTTP/2 response `:status` decoding/validation, informational and final response sequencing, no reason phrase, content rules, and duplicate/final-response rejection.
- [DONE] T114: Implement HTTP/2 regular header validation with lowercase names, pseudo-before-regular ordering, connection-specific field rejection, TE restricted to trailers, Host/authority reconciliation, and header-list limits.
- [DONE] T115: Implement Cookie field combination when decoding to the common model and optional semicolon-boundary splitting when encoding HTTP/2, without implementing cookie semantics or storage.
- [DONE] T116: Implement HTTP/2 message content and trailing HEADERS sequencing, END_STREAM handling, Content-Length versus total DATA validation, HEAD/no-content rules, malformed-message reporting, and stream completion.
- [DONE] T117: Implement RFC 8441 extended CONNECT negotiation and `:protocol` validation gated by `SETTINGS_ENABLE_CONNECT_PROTOCOL`, producing tunnel-stream metadata without WebSocket framing.
- [DONE] T118: Implement common request/response/trailer encoding into legal HTTP/2 header lists, pseudo-field order, lowercase names, sensitive flags, Content-Length rules, and HPACK-ready output.
- [DONE] T119: Implement HTTP/1-to-HTTP/2 conversion with hop-by-hop and Connection-nominated field removal, Host/authority mapping, Transfer-Encoding removal, TE trailers preservation, target decomposition, trailers, and framing validation.
- [DONE] T120: Implement HTTP/2-to-HTTP/1 conversion with request-target selection, Host construction, reason-phrase policy, Content-Length or chunked framing choice, trailer declarations, connection-field policy, and lossy-conversion metadata.
- [DONE] T121: Implement the `Http2Connection` façade that accepts fragmented bytes or decoded frames, runs frame/message/HPACK/state logic, and returns ordered delivery events and protocol actions without performing I/O.
- [DONE] T122: Implement outbound connection actions for new request/response streams, informational heads, content, trailers, reset, push, ping, settings, priority update, ORIGIN, capacity release, and graceful shutdown with role/state validation.
- [DONE] T123: Implement immutable-by-copy connection and stream snapshots sufficient for diagnostics, caller scheduling, retry decisions, flow-control inspection, and deterministic state-machine tests.

### Complete public API test coverage

- [DONE] T124: Create registry/static-data tests covering every public constant field against the downloaded IANA CSV snapshots and every HPACK static/Huffman entry against RFC 7541.
- [DONE] T125: Create direct tests for every public enum value, data-class field role, result presence convention, exception constructor/diagnostic field, and shared limit default used by an API operation.
- [DONE] T126: Create exhaustive `IHttpHeaderService` tests for casing, order, duplicates, combination, mutation, validation, injection rejection, and trailer restrictions.
- [DONE] T127: Create exhaustive `IHttpFieldValueCodec` tests from RFC examples and boundary/malformed cases for every exposed parse and serialize method.
- [DONE] T128: Create exhaustive `IStructuredFieldCodec` tests for every bare type, parameters, inner lists, lists, dictionaries, canonical output, numeric/string limits, and RFC 9651 malformed examples.
- [DONE] T129: Create exhaustive `IHttpSemantics` tests for every core and registered method/status, unknown extensions, target forms, effective URIs, response-content rules, and intermediary normalization.
- [DONE] T130: Create exhaustive `IHttpContentNegotiator` tests for each negotiation dimension, combined selection, all wildcard/absence/empty/quality/specificity ties, RFC 4647 modes, Priority merge, and Vary keys.
- [DONE] T131: Create exhaustive `IHttpPreconditionEvaluator` tests for strong/weak tags, dates, wildcard/existence cases, every RFC precedence branch, safe/unsafe methods, 304/412 outcomes, and If-Range.
- [DONE] T132: Create exhaustive `IHttpRangeService` tests for all range forms, normalization/coalescing, overflow, abuse limits, 206/416, multipart assembly/parsing, exact length, and malformed boundaries.
- [DONE] T133: Create exhaustive `IHttpCachePolicy` tests for every RFC 9111 directive and decision branch: storage, keys/Vary, age, explicit/heuristic freshness, request constraints, stale rules, disconnected use, revalidation, 304 merge, cached ranges, and invalidation.
- [DONE] T134: Create exhaustive `IHttpContentCoding` tests for identity, gzip, zlib/deflate, Brotli, coding stacks/order, empty and large bounded content, corrupt streams, unknown codings, and expansion limits using only test-side dependencies if helpful.
- [DONE] T135: Create HTTP/1 request and response parser conformance tests for every start-line, field, body-length, body, trailer, persistence, informational, upgrade, tunnel, and incomplete-message rule exposed by `IHttp1Parser`.
- [DONE] T136: Create HTTP/1 fragmentation and pipelining tests that split every representative wire message at every byte boundary, feed multiple messages in one buffer, verify consumed counts/events, and exercise reset/end-input behavior.
- [DONE] T137: Create HTTP/1 adversarial tests for request smuggling and response splitting, including TE/CL conflicts, duplicate Content-Length variants, malformed transfer-coding order, bare LF, obsolete fold, invalid controls, overflow, post-close data, and every configured limit.
- [DONE] T138: Create exhaustive `IHttp1Serializer` allocating and Span tests for requests/responses, every framing choice, no-body cases, chunks/extensions/trailers, output shortage, connection decisions, exact bytes, and parser round trips.
- [DONE] T139: Create HPACK integer, raw string, Huffman, static table, dynamic table, resize, indexed/literal mode, sensitive, and shared-context tests from every RFC 7541 appendix example.
- [DONE] T140: Extract and consume the archived http2jp corpus during tests to verify `IHpackDecoder` against every compatible wire story and verify `IHpackEncoder` output by decoding it across shared contexts.
- [DONE] T141: Create exhaustive malformed HPACK tests for truncation at every byte, integer overflow, invalid indices, illegal/oversized table updates, invalid Huffman EOS/padding/codes, size limits, and encoder/decoder state after rejection.
- [DONE] T142: Create exhaustive `IHttp2FrameCodec` tests for every frame type, every valid flag combination, padding boundary, min/max length, reserved bit, unknown type/flags, malformed size/stream id, partial input, Span shortage, and byte-exact round trip.
- [DONE] T143: Create HTTP/2 preface, settings, stream-id, stream-state, header-continuation, concurrency, push, reset, ping, GOAWAY, and error-scope tests mapped section-by-section to RFC 9113 and the h2spec coverage list.
- [DONE] T144: Create exhaustive HTTP/2 inbound/outbound flow-control tests for connection and stream windows, padding, capacity release/reservation, WINDOW_UPDATE, overflow, zero increment, settings deltas, negative stream windows, blocking, and cleanup.
- [DONE] T145: Create exhaustive `IHttp2MessageCodec` tests for request/response pseudo-fields, regular field restrictions, Cookie handling, informational/final/trailer sequences, Content-Length, CONNECT/extended CONNECT, and malformed-message versus protocol-error distinctions.
- [DONE] T146: Create HTTP/2 extension tests for deprecated priority, no-RFC7540-priorities, Priority/Priority Update, ORIGIN, extended CONNECT settings, unknown frames, unknown settings, and extension preservation/ignore behavior.
- [DONE] T147: Create end-to-end connection façade tests for client/server byte exchange covering concurrent requests, informational/final responses, streaming content/trailers, flow-control blocking, reset, push, graceful shutdown, retry eligibility, and exact event/action order.
- [DONE] T148: Create bidirectional HTTP/1.1↔HTTP/2 conversion and semantic-equivalence tests for normal, chunked/trailered, HEAD, CONNECT, informational, no-content, authority, hop-by-hop, and malformed inputs.
- [DONE] T149: Create deterministic randomized and mutation tests for field grammars, HTTP/1 chunk boundaries, HPACK header blocks, HTTP/2 frames, and connection action sequences, asserting bounded failure, no hangs, no state desynchronization, and successful canonical round trips for valid generated values.
- [DONE] T150: Add a public-API coverage matrix in the test suite that maps every `src/Api` type and every contract method to direct behavior tests, enforce complete line and branch coverage for public implementations, and close every uncovered functional branch without adding syntax-policy or source-shape tests.
