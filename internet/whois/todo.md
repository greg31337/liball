# Universal WHOIS protocol library development plan

## Goal and scope

Build a reusable, synchronous, dependency-free C# library for RFC 3912 WHOIS query/response exchanges, from direct TCP port 43 access through safe automatic server discovery and referral following. The library will support domain names (including IDNs), IPv4 and IPv6 addresses, IP prefixes, autonomous system numbers, reverse-DNS names, and caller-defined free-form WHOIS queries.

The protocol's response is intentionally human-readable and has no standardized character-set marker or universal record schema. Therefore, raw response bytes are the source of truth. Text decoding, generic line/field parsing, server selection, and referral traversal are separate layers and never discard the raw bytes.

The source project will use only built-in .NET APIs. No package dependency will be added under `src/`. Test-only packages are allowed under `tests/`.

## Specification and ecosystem findings

### Normative and operational findings

- RFC 3912 requires TCP, normally port 43; a request is text terminated by ASCII CRLF; a response may contain any number of lines; only server connection close marks the end of the response.
- RFC 3912 does not define authentication, confidentiality, integrity, response grammar, error grammar, maximum response size, timeout behavior, or an encoding negotiation mechanism. US-ASCII was historically predominant, but deployed servers use other encodings.
- IANA's WHOIS service accepts domains, IPv4/IPv6 addresses, address ranges and CIDR prefixes, and AS numbers. Its responses are UTF-8, use ordered `key: value` fields, use `%` comment lines, and advertise a more authoritative server with `refer:`.
- ICANN gTLD examples use repeated colon-delimited fields, blank-line record boundaries, `Registrar WHOIS Server:`, older `Whois Server:`, and non-WHOIS web referral URLs. A generic ordered multimap is portable; a single-value dictionary is not.
- A referral target is untrusted response data. Automatic traversal needs DNS/IP destination policy checks, port restrictions, a hop limit, and endpoint-plus-query loop detection. Caller-directed direct queries need a separately configurable policy so private registry infrastructure remains usable.
- `rwhois://` identifies the distinct RFC 2167 protocol, normally on port 4321. Parsing and exposing this referral is useful, but sending an RFC 3912 request to it is not protocol-correct.

### Existing-library feature survey

- [`richardpenman/whois`](https://github.com/richardpenman/whois) provides direct TCP queries, IANA/TLD discovery, IDNA conversion, IPv4/IPv6 connection preference, timeouts, recursive registrar referrals, SOCKS injection, referral SSRF filtering, UTF-8 replacement decoding, raw text, not-found detection, and extensive TLD-specific structured parsers.
- [`likexian/whois`](https://github.com/likexian/whois) supports domains, IPv4/IPv6, CIDRs, and ASNs; IANA discovery; direct server/port overrides; a pluggable dialer; one timeout budget across connect/write/read; ARIN-specific query prefixes; multiple referral labels and URI schemes; optional referral following; and either a full response chain or only the final response.
- [`domainr/whois`](https://github.com/domainr/whois) separates prepared request, transport response, adapter, and parsed record; supports cancellation/deadlines, custom dialing, response byte limits, raw bodies, charset detection/conversion, checksums/MIME persistence, and server-specific query adapters. Its universal structured parser remains deliberately incomplete.
- [`magiclen/whois-rust`](https://github.com/magiclen/whois-rust) and its maintained forks use a configurable TLD/server map, per-server query templates, IDN/punycode flags, direct host mode, caller timeouts, and synchronous/async lookup variants.
- [`whoizz`](https://docs.rs/whoizz/latest/whoizz/) demonstrates a small Rust design using IANA referrals for both domains and IP resources, caller timeouts, raw text, and best-effort common-field extraction.
- [`foundry.fsky.io/fsky/whodis/whois`](https://pkg.go.dev/foundry.fsky.io/fsky/whodis/whois) separates exact one-server `Query` from automatic `Lookup`; keeps opaque bytes and every referral hop; returns partial responses with later-hop errors; bounds query bytes, response bytes, time, and hops; injects resolver/dialer/endpoint policies; prevents DNS-rebinding by dialing a previously approved IP literal; and uses different endpoint policies for direct and automatic operations.

### Conclusions applied to this design

- Include exact one-server query and automatic multi-hop lookup as distinct public operations.
- Keep transport, decoding, generic parsing, selection, formatting, and referral policy replaceable through interfaces rather than delegates, because delegates are forbidden by `syntax.md`.
- Return complete raw exchange metadata at every layer. Decoded text and parsed fields are derived views.
- Provide both exception-throwing operations and distinctly named exception-less operations because network failures are common and `ref`/`out`-based `Try` APIs are forbidden.
- Do not embed a large, stale TLD server table. The built-in selector starts classifiable lookups at `whois.iana.org`; callers can inject a fixed, cached, enterprise, or test selector.
- Do not include hundreds of brittle TLD-specific normalized record parsers in the RFC 3912 core. Generic ordered fields, records, comments, raw lines, referrals, and raw bytes are the stable portable layer.

## C# implementation constraints

- Follow `syntax.md` for every file under `src/` and `tests/`; existing code does not relax it.
- Use one file-scoped namespace per C# file, ordinary classes and interfaces, public fields instead of properties, block-bodied methods, positional arguments, and no method overloads.
- Keep all public types under `src/Api` and in the required category directory. Every public implementation class implements at least one public interface and exposes only one constructor plus interface-declared instance methods.
- No public static methods are proposed. Defaults and well-known labels are public static/const fields only.
- All public API declarations receive the required XML comments while they are implemented; this is part of each implementation task, not a separate documentation work item.
- The API is synchronous because `await` is forbidden. Cancellation is still supported with `System.Threading.CancellationToken`, and connect operations may synchronously wait on built-in cancellable socket APIs.
- Because `try`/`catch` is forbidden, built-in DNS/socket/stream helpers start cancellable .NET task-based operations, wait synchronously through non-throwing completion observation, inspect task status/exception fields, and return status-bearing result data. Expected network failures do not require exception handling syntax; an unexpected exception thrown directly by a caller-supplied implementation is outside that implementation's contract and may propagate.
- Public data uses arrays and `List<T>` where ownership must be unambiguous. Internal byte/character scanning should use `ReadOnlySpan<byte>` and `ReadOnlySpan<char>` where it avoids allocation, but spans are never stored in fields.
- Internal state is copied from mutable option objects during construction so a configured client can be used concurrently without per-call shared mutation.

## Complete feature inventory

### 1. Wire framing and direct TCP exchange

- Validate a direct endpoint host, numeric port range, non-empty query, maximum encoded query size, and absence of CR, LF, NUL, and other prohibited control characters.
- Encode only the caller's query text, append exactly ASCII `\r\n`, send the entire payload even when a socket accepts a partial write, and never append a second terminator supplied by a malicious/mistaken query.
- Resolve DNS through an injectable resolver, preserve all returned IPv4/IPv6 candidates and resolution failure metadata in `WhoisResolution`, apply configurable address-family preference, and try eligible addresses deterministically.
- Apply endpoint policy to each resolved address before connection. Pass the approved IP literal to the connection factory so automatic operations cannot be redirected by a second DNS lookup.
- Connect through an injectable stream-producing connection factory returning a status-bearing `WhoisConnection`; the built-in implementation uses TCP and supports IPv4 and IPv6. This hook permits test streams, custom proxies, and enterprise transports without adding source dependencies.
- Read until remote EOF as required by RFC 3912. CR/LF inside the body never ends the exchange.
- Retain bytes received before timeout, cancellation, reset, or other read failure and mark the exchange status accordingly.
- Enforce an in-memory response byte limit while reading. Retain the configured prefix, mark it truncated, and report a typed limit failure.
- Record selected endpoint, approved address, exact query text, sent payload encoding, start time, elapsed duration, byte count, completion status, failed operation stage, and underlying exception when present.
- Dispose connection and stream resources deterministically on every normal path supported by the restricted syntax.

### 2. Timeout and cancellation helpers

- Separate DNS resolution, connect, write, read-idle, and whole-operation timeout settings.
- Compute a monotonic overall deadline once and clamp every later stage to the smaller of its stage timeout and remaining overall budget.
- Reset only the read-idle deadline when bytes are received; never extend the overall deadline.
- Safely convert `TimeSpan` values to the bounded integer millisecond values expected by socket APIs, including sub-millisecond positive values and `Timeout.Infinite` where explicitly allowed.
- Observe caller cancellation before DNS, between address attempts, before/after writes, and during reads.
- Classify cancellation caused by the caller separately from a configured timeout.
- Ensure zero, negative, infinite, overflow-sized, and internally inconsistent timeout values are either deliberately supported or rejected before network activity.

### 3. Query classification and normalization

- Accept an explicit query-kind hint or auto-detect domain, IPv4 address, IPv6 address, IPv4 prefix, IPv6 prefix, AS number, reverse-DNS name, and free-form query.
- Trim only safe surrounding whitespace and a domain's optional final root dot; do not alter free-form direct queries.
- Convert Unicode domain labels to IDNA ASCII form with built-in `IdnMapping`, lowercase invariantly for routing, and validate label/domain lengths and empty labels.
- Canonicalize IP literals with `IPAddress`, including IPv4-mapped IPv6 behavior defined by tests.
- Parse CIDR prefixes without tuple, pattern, range, unsafe, or nullable-value syntax; validate prefix width and mask host bits for stable loop keys.
- Normalize ASNs with or without an `AS` prefix and validate the unsigned 32-bit ASN range while storing counts/sizes as signed integers.
- Recognize `.in-addr.arpa` and `.ip6.arpa` as reverse-domain queries.
- Preserve both original caller text and normalized text for diagnostics and raw direct-query fidelity.

### 4. Query formatting and encoding

- A replaceable formatter maps a normalized query plus selected endpoint to server-specific command text.
- Default formatting sends normalized text unchanged to IANA and ordinary servers.
- Built-in compatibility rules cover ARIN network (`n +`) and ASN (`a +`) commands and Verisign exact domain (`domain`) queries, with deterministic endpoint matching.
- Explicit direct query bypasses automatic classification/formatting so callers can use arbitrary server-specific search syntax.
- A replaceable codec strictly encodes the formatted request with the configured built-in .NET `Encoding`, validates the encoded byte limit, and adds CRLF after encoding.
- Default automatic domain queries are ASCII after IDNA conversion. Free-form direct queries default to strict ASCII but can opt into another caller-provided built-in encoding.

### 5. Lossless raw response and encoding helpers

- Raw bytes are always returned, including empty bodies and partial/truncated bodies.
- Caller-supplied response encoding takes precedence; otherwise optionally detect UTF BOMs, try strict UTF-8, and then apply the configured failure mode.
- Decoding modes include strict failure, UTF-8 replacement, and ISO-8859-1 lossless byte-to-character fallback. The selected encoding name and whether fallback/replacement occurred are exposed.
- Never infer an encoding from response text labels that RFC 3912 does not standardize.
- Preserve original newline sequences in raw bytes and decoded text. Normalized line views are derived without mutating text.
- Make embedded NUL and other unusual characters visible to callers rather than silently dropping them.

### 6. Generic response processing

- Split CRLF, bare LF, and bare CR correctly, including empty responses, leading/trailing blank lines, and a final line without a newline.
- Preserve line number, raw line text, normalized content, and line kind: blank, comment, field, continuation, or unstructured text.
- Recognize `%` and `#` comments conservatively while keeping their complete text.
- Parse a field only at the first colon, trim separator whitespace without trimming meaningful internal/trailing value content, preserve original key casing, and allow empty values.
- Preserve field order and duplicate keys; provide case-insensitive field lookup returning every match.
- Associate leading-whitespace continuation lines with the preceding field in the derived value while also retaining the continuation line.
- Group field-bearing sections separated by blank lines into ordered generic records without treating disclaimers or prose as registration facts.
- Enforce parser limits derived from the already bounded response and avoid regular expressions/LINQ. Use span-based scanners for line and delimiter discovery where appropriate.
- Do not classify availability, rate limits, registrant contacts, dates, or domain status as authoritative universal facts; callers can build registry-specific parsers on the ordered generic model.

### 7. Server selection hooks

- `IWhoisServerSelector` chooses an initial endpoint for an already classified query. It can implement static maps, enterprise registries, offline behavior, or test routing.
- The built-in selector routes supported automatic queries to `whois.iana.org:43`, relying on IANA's authoritative referral for domains, IP resources, prefixes, and ASNs.
- Caller-specified endpoint selection bypasses the automatic selector and is clearly marked as caller-directed for endpoint-policy purposes.
- Unknown/free-form automatic queries fail with a typed no-server result rather than guessing; they remain usable through direct query or a custom selector.
- Selection results can supply an endpoint and optional query text override, allowing Rust-style per-server templates without a JSON dependency or embedded database.
- Server hostnames, IDN hostnames, bracketed IP literals, IPv4 literals, IPv6 literals, and non-default ports are normalized consistently.

### 8. Referral parsing and policy

- Scan ordered fields and raw directive lines case-insensitively for `refer`, `whois`, `Whois Server`, `Registrar WHOIS Server`, `ReferralServer`, `Referral Server`, and `%referral` forms found across IANA, ICANN, RIR, Python, and Go implementations.
- Parse plain hosts, `host:port`, bracketed IPv6 with port, `whois://`, `rwhois://`, `http://`, and `https://` values without treating a web URL as a TCP WHOIS endpoint.
- Preserve source field, raw value, source line, parsed scheme, host, port, path/authority-area suffix, referral kind, validity, and rejection reason.
- Normalize WHOIS endpoints for equality while retaining the advertised form.
- Return all parsed referrals in response order. Select the next automatic WHOIS hop with explicit priority (`refer`/IANA, registrar WHOIS, generic WHOIS) and stable first-match behavior.
- Expose RWhois and web referrals but do not follow them as RFC 3912 exchanges by default. A custom selection/traversal implementation can opt into another protocol library.
- Reject malformed hosts, user-info, missing hosts, invalid ports, control characters, ambiguous unbracketed IPv6-with-port values, and unsupported URI components.

### 9. Safe referral traversal

- Keep every completed discovery/registry/registrar response as a separate ordered lookup hop; never concatenate bodies and lose provenance.
- Re-run formatting for the referred endpoint so server-specific query syntax is applied at each hop.
- Resolve and policy-check every automatic referral. The safe built-in policy permits only public unicast addresses and ports 43/4321, rejecting loopback, private, link-local, multicast, unspecified, documentation, benchmarking, and other special-use destinations.
- Provide a built-in allow-all policy for explicit trusted/private use, but keep direct and automatic policies independently configurable.
- Detect self-referrals and loops by normalized scheme/host/port plus normalized query.
- Enforce maximum referral hops and distinguish loop, hop-limit, unsupported-protocol, no-server, disallowed-endpoint, and transport failures.
- Preserve partial lookup results in exception-less return values and in typed exceptions from throwing APIs when discovery succeeded but a later hop failed.
- Permit disabling referral following while still parsing and exposing referrals.
- Permit choosing whether IANA discovery responses remain in the final hop list; default to retaining them for auditability.

### 10. High-level client behavior

- `Query` performs exactly one caller-directed exchange and throws for operational failure; `TryQuery` returns the raw exchange status and error without throwing for expected network failure.
- `Lookup` classifies, normalizes, selects, formats, queries, decodes, parses, and safely follows referrals; it throws typed contextual failures containing partial results.
- `TryLookup` performs the same work but returns a result with completion/failure metadata for expected selection, policy, network, limit, decoding, or referral failures.
- Per-call options control kind hint, endpoint override, encodings, timeouts, limits, address preference, referral following, discovery retention, and cancellation.
- Client dependencies are constructor-injected interfaces and copied configuration. Built-in implementations have no global mutable state and are reusable across concurrent calls.
- No automatic retry is performed because rate-limit banners and non-idempotent server policy are unstandardized; callers can retry explicitly based on typed failure metadata.

### 11. Error model

- Direct validation errors identify the invalid query/endpoint/option before network activity.
- No-server errors include original and normalized query and query kind.
- Transport errors include operation stage, endpoint, approved address when known, underlying exception, and partial raw response.
- Response errors include raw response, selected encoding, and decoding/parsing context.
- Referral errors include rejected referral or traversal reason and the partial lookup result.
- All custom exceptions derive directly from `System.Exception` as required by `syntax.md`; no custom exception inheritance tree is used.
- Exception-less results retain an `Exception?` field plus status enum so failure details are not reduced to strings.

## Explicit non-goals

- RDAP, HTTP-based WHOIS pages, web scraping, TLS-wrapped WHOIS, server implementation, bulk WHOIS, and registry account/authentication workflows.
- Full RFC 2167 RWhois sessions. `rwhois://` referrals are parsed and exposed, not silently treated as RFC 3912.
- A bundled, updateable global TLD/RIR server database, public-suffix database, DNS resolver implementation beyond built-in .NET DNS, or on-disk/network cache.
- Universal normalized registrar/contact/date/status/availability objects. Those require registry-specific policy and can be layered over generic fields and raw bytes.
- SOCKS/HTTP proxy protocol implementations. `IWhoisConnectionFactory` is the extension point for a caller-supplied proxy stream.
- Logging, metrics, CLI/UI, dependency injection framework integration, serialization formats, MIME persistence, checksums, retries, batching, and rate limiting.
- Performance/benchmark work, release work, and documentation-site work.

## Proposed public API

Names below are architectural, not fine-grained signatures. All data classes expose documented public fields and no public methods; all operations are instance methods on interfaces and their implementation classes.

### `src/Api/Enums`

- `WhoisQueryKind` — `Automatic`, `Domain`, `IPv4Address`, `IPv6Address`, `IPv4Prefix`, `IPv6Prefix`, `AutonomousSystem`, `ReverseDomain`, and `FreeForm`.
- `WhoisReferralScheme` — `Unspecified`, `Whois`, `RWhois`, `Http`, `Https`, and `Unsupported`.
- `WhoisReferralKind` — `Iana`, `Registry`, `Registrar`, `Generic`, `RWhois`, `Web`, and `Unknown`.
- `WhoisLineKind` — `Blank`, `Comment`, `Field`, `Continuation`, and `Text`.
- `WhoisExchangeStatus` — `Complete`, `Cancelled`, `TimedOut`, `ResponseLimitReached`, `ResolveFailed`, `ConnectFailed`, `WriteFailed`, and `ReadFailed`.
- `WhoisOperationStage` — `None`, `Select`, `Resolve`, `Connect`, `Write`, `Read`, `Decode`, `Parse`, and `Refer`.
- `WhoisAddressPreference` — `System`, `IPv4First`, and `IPv6First`.
- `WhoisDecodingMode` — `Strict`, `ReplaceInvalidUtf8`, and `Latin1Fallback`.
- `WhoisEndpointSource` — `Direct`, `InitialSelection`, and `Referral`.

### `src/Api/Static`

- `WhoisDefaults` — TCP/RWhois ports, IANA host, default timeout values, query/response limits, and referral-hop limit as const/static readonly fields.
- `WhoisFieldNames` — well-known referral field/directive labels as string constants only.

### `src/Api/Data`

- `WhoisEndpoint` — scheme, host, port, and advertised value.
- `WhoisTimeoutOptions` — resolve, connect, write, read-idle, and overall `TimeSpan` values.
- `WhoisLimits` — maximum encoded query bytes, response bytes, referral hops, lines, and fields.
- `WhoisEncodingOptions` — request encoding, optional response encoding, BOM detection, and decoding mode.
- `WhoisClientOptions` — timeout, limit, encoding, address preference, referral, discovery-retention, and endpoint-policy behavior settings.
- `WhoisLookupRequest` — caller text, kind hint, optional endpoint override, per-call options, and cancellation token.
- `WhoisQuery` — original, normalized, routing key, query kind, and validity context.
- `WhoisServerSelection` — selected endpoint, endpoint source, optional formatted-query override, and selection reason.
- `WhoisRequest` — endpoint, source, exact query before CRLF, encoding/time/limit settings, and cancellation token.
- `WhoisResolution` — ordered resolved addresses, completion status, duration, and error from the resolver boundary.
- `WhoisConnection` — approved address, owned connected stream, completion status, duration, and error from the connection boundary.
- `WhoisRawResponse` — request metadata, approved address, raw body, timestamps/duration, status, stage, truncation flag, and error.
- `WhoisTextResponse` — raw response, decoded text, selected encoding name, BOM/fallback/replacement flags, decoding status, and error.
- `WhoisLine` — line number, raw content, normalized content, kind, and linked field index.
- `WhoisField` — ordered index, record index, source line, original/canonical name, raw value, and derived continuation-aware value.
- `WhoisRecord` — record index plus ordered fields and lines belonging to the generic record.
- `WhoisParsedResponse` — text response plus ordered lines, fields, records, parse status, and error.
- `WhoisReferral` — source metadata, advertised value, scheme/kind, parsed endpoint, path suffix, validity, and rejection reason.
- `WhoisLookupHop` — selection, raw/text/parsed response, all referrals, and the selected next referral.
- `WhoisLookupResult` — classified query, ordered hops, completion flag, failed stage, and error; used both as a normal result and typed-exception partial result.

Every data type above is consumed by at least one contract or nested in another data type, satisfying the public data usage rule.

### `src/Api/Contracts`

- `IWhoisQueryClassifier` — `Classify(WhoisLookupRequest)` returns a normalized `WhoisQuery`.
- `IWhoisQueryFormatter` — `FormatQuery(WhoisQuery, WhoisServerSelection)` returns server-specific text before CRLF.
- `IWhoisQueryCodec` — `EncodeQuery(WhoisRequest)` validates/encodes the query and returns the framed byte array.
- `IWhoisHostResolver` — `ResolveHost(WhoisEndpoint, WhoisAddressPreference, CancellationToken)` returns a status-bearing `WhoisResolution` with ordered `IPAddress[]` candidates.
- `IWhoisEndpointPolicy` — `AllowEndpoint(WhoisEndpoint, IPAddress, WhoisEndpointSource)` decides whether an already resolved destination is eligible.
- `IWhoisConnectionFactory` — `OpenConnection(WhoisEndpoint, IPAddress, WhoisTimeoutOptions, CancellationToken)` returns a status-bearing `WhoisConnection` owning the connected `Stream` for the approved address.
- `IWhoisTransport` — `Exchange(WhoisRequest)` performs one exception-less, status-bearing RFC 3912 exchange.
- `IWhoisResponseDecoder` — `DecodeResponse(WhoisRawResponse, WhoisEncodingOptions)` returns a lossless derived text view or a typed failure.
- `IWhoisResponseParser` — `ParseResponse(WhoisTextResponse, WhoisLimits)` and `FindFields(WhoisParsedResponse, string)` build/query the generic ordered model; the names are distinct rather than overloaded.
- `IWhoisReferralParser` — `ParseReferrals(WhoisParsedResponse)` returns every recognized/refused referral without network activity.
- `IWhoisServerSelector` — `SelectServer(WhoisQuery, WhoisLookupRequest)` returns the initial endpoint or a typed no-server failure.
- `IWhoisClient` — `Query`, `TryQuery`, `Lookup`, and `TryLookup` provide throwing and exception-less direct/high-level operations with unique method names.

### `src/Api/Classes`

- `WhoisQueryClassifier : IWhoisQueryClassifier` — built-in target validation, IDNA conversion, classification, and canonicalization.
- `WhoisQueryFormatter : IWhoisQueryFormatter` — ordinary, IANA, ARIN, and Verisign formatting rules.
- `WhoisQueryCodec : IWhoisQueryCodec` — strict encoding, byte-limit validation, and CRLF framing.
- `WhoisDnsResolver : IWhoisHostResolver` — built-in DNS/IP-literal resolution and address ordering.
- `WhoisAllowAllEndpointPolicy : IWhoisEndpointPolicy` — explicit trusted/direct endpoint policy.
- `WhoisPublicEndpointPolicy : IWhoisEndpointPolicy` — automatic-referral public-address and WHOIS-port policy.
- `WhoisTcpConnectionFactory : IWhoisConnectionFactory` — cancellable built-in TCP connection establishment to an approved literal address.
- `WhoisTcpTransport : IWhoisTransport` — bounded write/read loop, deadlines, EOF completion, partial bytes, and exchange status.
- `WhoisResponseDecoder : IWhoisResponseDecoder` — BOM/configured/UTF-8/fallback decoding.
- `WhoisResponseParser : IWhoisResponseParser` — span-based universal line, field, continuation, record, and lookup processing.
- `WhoisReferralParser : IWhoisReferralParser` — referral label recognition, URI/host parsing, normalization, and priority metadata.
- `IanaWhoisServerSelector : IWhoisServerSelector` — default IANA starting route and explicit no-server behavior for free-form input.
- `WhoisClient : IWhoisClient` — direct exchange and end-to-end automatic lookup orchestration.

Each implementation class will have one dependency/configuration constructor only. Convenience is provided by initialized option fields and `WhoisDefaults`, not by public static factories or constructor overloads.

### `src/Api/Exceptions`

- `WhoisQueryException` — invalid query, kind hint, endpoint, encoding, limit, or timeout input.
- `WhoisServerNotFoundException` — automatic selection cannot route the normalized resource.
- `WhoisTransportException` — failed operation stage with endpoint/address and partial `WhoisRawResponse`.
- `WhoisResponseException` — decoding or generic parsing failure with the raw/text response context.
- `WhoisReferralException` — invalid, disallowed, looping, unsupported, or excessive referral with partial `WhoisLookupResult`.

Each exception derives directly from `System.Exception` and has at most one public constructor.

## Planned source files

```text
src/
  Api/
    Enums/
      WhoisAddressPreference.cs
      WhoisDecodingMode.cs
      WhoisEndpointSource.cs
      WhoisExchangeStatus.cs
      WhoisLineKind.cs
      WhoisOperationStage.cs
      WhoisQueryKind.cs
      WhoisReferralKind.cs
      WhoisReferralScheme.cs
    Static/
      WhoisDefaults.cs
      WhoisFieldNames.cs
    Data/
      WhoisClientOptions.cs
      WhoisConnection.cs
      WhoisEncodingOptions.cs
      WhoisEndpoint.cs
      WhoisField.cs
      WhoisLimits.cs
      WhoisLine.cs
      WhoisLookupHop.cs
      WhoisLookupRequest.cs
      WhoisLookupResult.cs
      WhoisParsedResponse.cs
      WhoisQuery.cs
      WhoisRawResponse.cs
      WhoisRecord.cs
      WhoisReferral.cs
      WhoisRequest.cs
      WhoisResolution.cs
      WhoisServerSelection.cs
      WhoisTextResponse.cs
      WhoisTimeoutOptions.cs
    Contracts/
      IWhoisClient.cs
      IWhoisConnectionFactory.cs
      IWhoisEndpointPolicy.cs
      IWhoisHostResolver.cs
      IWhoisQueryClassifier.cs
      IWhoisQueryCodec.cs
      IWhoisQueryFormatter.cs
      IWhoisReferralParser.cs
      IWhoisResponseDecoder.cs
      IWhoisResponseParser.cs
      IWhoisServerSelector.cs
      IWhoisTransport.cs
    Classes/
      IanaWhoisServerSelector.cs
      WhoisAllowAllEndpointPolicy.cs
      WhoisClient.cs
      WhoisDnsResolver.cs
      WhoisPublicEndpointPolicy.cs
      WhoisQueryClassifier.cs
      WhoisQueryCodec.cs
      WhoisQueryFormatter.cs
      WhoisReferralParser.cs
      WhoisResponseDecoder.cs
      WhoisResponseParser.cs
      WhoisTcpConnectionFactory.cs
      WhoisTcpTransport.cs
    Exceptions/
      WhoisQueryException.cs
      WhoisReferralException.cs
      WhoisResponseException.cs
      WhoisServerNotFoundException.cs
      WhoisTransportException.cs
  Internal/
    AddressClassifier.cs
    ByteAccumulator.cs
    CidrParser.cs
    EndpointParser.cs
    IdnNormalizer.cs
    LineScanner.cs
    ReferralValueParser.cs
    TimeoutBudget.cs
```

Internal helpers remain non-public and outside `src/Api`. They use classes only and comply with the same restricted source syntax.

## Planned test files and coverage

```text
tests/
  ApiDefaultsTests.cs
  ApiDataTests.cs
  EndpointParserTests.cs
  QueryClassifierTests.cs
  QueryFormatterTests.cs
  QueryCodecTests.cs
  TimeoutBudgetTests.cs
  DnsResolverTests.cs
  EndpointPolicyTests.cs
  TcpConnectionFactoryTests.cs
  TcpTransportTests.cs
  ResponseDecoderTests.cs
  ResponseParserTests.cs
  ReferralParserTests.cs
  ServerSelectorTests.cs
  WhoisClientQueryTests.cs
  WhoisClientLookupTests.cs
  WhoisClientFailureTests.cs
  WhoisClientConcurrencyTests.cs
  PublicApiBehaviorTests.cs
  Fixtures/
    FakeConnectionFactory.cs
    FakeEndpointPolicy.cs
    FakeHostResolver.cs
    FakeServerSelector.cs
    FakeTransport.cs
    LoopbackWhoisServer.cs
    WhoisResponseFixtures.cs
```

Coverage is behavioral, deterministic, and network-independent except for loopback TCP. It covers every public constructor, public field default, interface method, implementation method, enum value, static field, success path, validation branch, timeout/cancellation stage, response limit, encoding mode, parser line kind, referral syntax, endpoint-policy decision, traversal termination, throwing API, and exception-less API. Captured IANA, ICANN/gTLD, ARIN, RIPE/APNIC-style, malformed, non-UTF-8, and adversarial referral bodies are fixtures. No test depends on the live Internet.

## Implementation tasks

### Foundation and public model

- [DONE] T001: Add the nine public enums under `src/Api/Enums` with stable explicit integer values used by the request, response, parsing, and referral models.
- [DONE] T002: Add `WhoisDefaults` and `WhoisFieldNames` under `src/Api/Static` with fields only and conservative RFC/IANA defaults.
- [DONE] T003: Implement `WhoisEndpoint`, `WhoisTimeoutOptions`, `WhoisLimits`, and `WhoisEncodingOptions` data classes with safe initialized fields.
- [DONE] T004: Implement `WhoisClientOptions`, `WhoisLookupRequest`, `WhoisQuery`, `WhoisServerSelection`, and `WhoisRequest` data classes and ensure every enum/data type is consumed by an interface or another data class.
- [DONE] T005: Implement `WhoisResolution`, `WhoisConnection`, `WhoisRawResponse`, `WhoisTextResponse`, `WhoisLine`, `WhoisField`, `WhoisRecord`, and `WhoisParsedResponse` data classes for status-bearing network boundaries and lossless ordered response processing.
- [DONE] T006: Implement `WhoisReferral`, `WhoisLookupHop`, and `WhoisLookupResult` data classes for complete multi-hop provenance and partial failure results.
- [DONE] T007: Add all twelve operation contracts under `src/Api/Contracts` with unique non-overloaded instance method names and no interface inheritance.
- [DONE] T008: Implement the five direct-`System.Exception` public exception types with their relevant context and partial-result fields.
- [DONE] T009: Add tests for all public enum values, static defaults, data field initialization, and exception context construction.

### Validation, classification, and formatting

- [DONE] T010: Implement shared guard logic for null references, empty/control-character queries, host/port validation, positive limits, and consistent timeout relationships.
- [DONE] T011: Implement `IdnNormalizer` with built-in `IdnMapping`, root-dot handling, label/domain length checks, invariant routing form, and original-text preservation.
- [DONE] T012: Implement `CidrParser` for IPv4/IPv6 prefix validation, canonical network masking, and stable text without tuples, patterns, LINQ, or custom structs.
- [DONE] T013: Implement `WhoisQueryClassifier` for explicit hints and automatic domain, IP, prefix, ASN, reverse-domain, and free-form classification.
- [DONE] T014: Test query validation against empty, whitespace, multiline, NUL/control, oversized, malformed IDN, malformed IP/prefix, out-of-range ASN, and conflicting kind-hint inputs.
- [DONE] T015: Test normalization of ASCII/Unicode/root-dotted domains, IPv4/IPv6 variants, host-bit CIDRs, AS/decimal forms, reverse zones, and direct free-form text.
- [DONE] T016: Implement `WhoisQueryFormatter` with pass-through IANA/ordinary behavior and deterministic ARIN network/ASN and Verisign exact-domain rules.
- [DONE] T017: Test every formatter rule, endpoint hostname casing/trailing-dot normalization, selection query override, and unknown-server pass-through behavior.
- [DONE] T018: Implement `WhoisQueryCodec` with strict caller encoding, encoded-byte limits, exact one-time CRLF framing, and rejection of injected terminators.
- [DONE] T019: Test ASCII, UTF-8, alternate built-in encoding, non-encodable text, byte-boundary, partial-character, and CRLF injection codec cases.

### Timeout, resolution, endpoint policy, and connections

- [DONE] T020: Implement `TimeoutBudget` using monotonic elapsed time for stage/overall deadline intersection, idle-read reset, cancellation classification, and safe millisecond conversion.
- [DONE] T021: Test finite/infinite/zero/negative/sub-millisecond/overflow timeout validation, overall budget exhaustion, read-idle reset, and caller cancellation precedence.
- [DONE] T022: Implement `AddressClassifier` for IPv4/IPv6 public-unicast versus private, loopback, link-local, multicast, unspecified, documentation, benchmarking, and other special-use ranges.
- [DONE] T023: Implement `WhoisAllowAllEndpointPolicy` and `WhoisPublicEndpointPolicy`, including separate direct/automatic behavior and port 43/4321 rules.
- [DONE] T024: Test endpoint policies over representative IPv4/IPv6 special-use boundaries, public addresses, nonstandard ports, direct endpoints, and automatic referrals.
- [DONE] T025: Implement `WhoisDnsResolver` for IP literals, built-in DNS, deterministic address-family ordering, duplicate removal, timeout, and cancellation, returning `WhoisResolution` by inspecting task completion without `try`/`catch`.
- [DONE] T026: Test resolver literal bypass, IPv4/IPv6 ordering, duplicate results, DNS failure status, timeout, cancellation, and empty resolution through deterministic fixtures where necessary.
- [DONE] T027: Implement `WhoisTcpConnectionFactory` so it connects only to the resolver-approved literal IP, applies the remaining connect budget without re-resolving the advertised host, and returns `WhoisConnection` by inspecting task completion without `try`/`catch`.
- [DONE] T028: Test loopback IPv4 and available IPv6 connections, refused endpoints, connect cancellation/timeout, literal-address dialing, and stream disposal.

### TCP transport

- [DONE] T029: Implement `ByteAccumulator` with a fixed maximum, retained prefix on overflow, and span-based append operations.
- [DONE] T030: Implement `WhoisTcpTransport` address-attempt orchestration, policy evaluation, cancellable task-based connect/write/read completion observation, complete partial-write handling, EOF-delimited reads, idle/overall timeouts, and deterministic disposal without `try`/`catch`.
- [DONE] T031: Ensure `WhoisTcpTransport` returns status-bearing partial raw responses for expected resolution/connect/write/read/cancel/timeout/limit failures while reserving exceptions for invalid/invariant inputs.
- [DONE] T032: Build the reusable loopback WHOIS server and fake resolver, connection, policy, selector, and transport fixtures under `tests/Fixtures`.
- [DONE] T033: Test that transport sends the exact encoded query plus one CRLF and handles deliberately fragmented/partial writes.
- [DONE] T034: Test multiline, binary, empty, fragmented, delayed, large, reset, and final-line-without-newline responses, proving only EOF completes a response.
- [DONE] T035: Test resolve/connect/write/read-idle/overall timeout and caller-cancellation status, failed-stage metadata, retained partial bytes, and resource disposal.
- [DONE] T036: Test response-limit boundaries, truncation metadata, address fallback ordering, all-address failure aggregation, and approved-address recording.

### Decoding and generic response processing

- [DONE] T037: Implement `WhoisResponseDecoder` with explicit encoding precedence, BOM detection, strict UTF-8, replacement mode, Latin-1 fallback, and complete decoding metadata.
- [DONE] T038: Test UTF-8/UTF-16 BOMs, explicit built-in encodings, ASCII, malformed UTF-8, replacement, Latin-1 byte preservation, empty bodies, embedded NUL, and strict failures with raw context.
- [DONE] T039: Implement span-based `LineScanner` for CRLF, LF, CR, mixed endings, empty lines, and unterminated final lines without changing the decoded text.
- [DONE] T040: Implement `WhoisResponseParser` line classification, first-colon field parsing, key canonicalization, duplicate preservation, continuation association, record grouping, and parser limits.
- [DONE] T041: Implement case-insensitive `FindFields` returning every ordered match without LINQ and without collapsing repeated values.
- [DONE] T042: Add captured IANA, ICANN/gTLD, ARIN, RIPE/APNIC-style, prose/disclaimer, repeated-field, continuation, and malformed response fixtures.
- [DONE] T043: Test every newline and line-kind case, comment prefix, empty value, colon-in-value, key casing, whitespace rule, continuation, blank record boundary, unstructured text, and final line.
- [DONE] T044: Test ordered duplicate lookup, record membership, parser count limits, empty response behavior, and preservation links back to text/raw bytes.

### Referral parsing and initial selection

- [DONE] T045: Implement `EndpointParser` for DNS/IDN hosts, IPv4, bracketed IPv6, optional ports, WHOIS/RWhois/web URIs, default ports, and normalized equality keys.
- [DONE] T046: Test valid endpoint forms and reject user-info, controls, invalid/missing ports, missing hosts, URI fragments/queries where forbidden, ambiguous IPv6, and unsupported schemes.
- [DONE] T047: Implement `ReferralValueParser` and `WhoisReferralParser` for all recognized labels/directives, case/spacing variants, raw/source metadata, stable order, kind inference, and rejection reasons.
- [DONE] T048: Implement deterministic next-hop priority metadata while retaining all WHOIS, RWhois, web, duplicate, malformed, and self referrals.
- [DONE] T049: Test IANA `refer:`, ICANN registrar fields, legacy `Whois Server`, ARIN `ReferralServer`, `%referral`, plain/nondefault port, IPv6, RWhois authority path, web URL, duplicates, malformed values, and false positives in prose.
- [DONE] T050: Implement `IanaWhoisServerSelector` for all classifiable automatic resource kinds, caller endpoint override handling, and typed free-form no-server behavior.
- [DONE] T051: Test IANA selection for domains, IDNs, addresses, prefixes, ASNs, reverse zones, endpoint overrides, query overrides, and free-form failure.

### High-level orchestration and failures

- [DONE] T052: Implement `WhoisClient.TryQuery` as one caller-directed, non-referring exchange using copied per-call configuration and the direct endpoint policy.
- [DONE] T053: Implement `WhoisClient.Query` as the throwing direct wrapper that converts failed raw status into `WhoisTransportException` with partial response context.
- [DONE] T054: Test direct query exact-text fidelity, selector/formatter/referral bypass, custom endpoint/port/encoding/timeouts, success, and each throwing versus exception-less failure mapping.
- [DONE] T055: Implement `WhoisClient.TryLookup` classification, initial selection, per-hop formatting, exchange, decode, parse, referral extraction, and ordered hop assembly.
- [DONE] T056: Add safe traversal controls for referral disablement, automatic endpoint policy, discovery retention, WHOIS-only default following, maximum hops, stable next-hop selection, and endpoint-plus-query loop detection.
- [DONE] T057: Implement `WhoisClient.Lookup` as the throwing wrapper that maps selection, transport, response, and traversal failure to typed exceptions carrying the partial lookup result.
- [DONE] T058: Test an IANA-to-registry-to-registrar loopback chain with exact ordered raw/text/parsed/referral provenance and server-specific query reformatting at each hop.
- [DONE] T059: Test referral-disabled and discovery-hidden modes, multiple referral priority, self-referral, two-server loop, maximum hops, unsupported RWhois, web-only referral, no referral, and malformed referral outcomes.
- [DONE] T060: Test public-policy refusal of private/special referrals, allow-all/custom-policy opt-in, nonstandard direct ports, DNS rebinding prevention, mixed allowed/disallowed addresses, and custom resolver/connection hooks.
- [DONE] T061: Test initial no-server, first-hop failure, partial later-hop failure, response limit, decode failure, parser failure, cancellation, and timeout across both `Lookup` and `TryLookup`.
- [DONE] T062: Test custom classifier, formatter, selector, resolver, endpoint policy, connection factory, transport, decoder, parser, and referral parser implementations independently to prove every public hook is honored.

### Complete API-layer verification

- [DONE] T063: Test every public implementation constructor for valid dependency injection and every null/invalid dependency or option rejection path.
- [DONE] T064: Test that configured clients do not retain per-call response/traversal state and produce isolated deterministic results under concurrent functional use.
- [DONE] T065: Add API behavior tests that instantiate every public data/exception/implementation type, exercise every interface method, read every static field, and cover every enum member in a meaningful public scenario.
- [DONE] T066: Run the complete test suite with coverage collection, inspect uncovered branches in `src/Api`, and add focused behavioral cases until all reachable functionality exposed by the `src/Api` layer is covered.
- [DONE] T067: Run clean `dotnet build` and `dotnet test` from the repository root and resolve all compiler warnings, test failures, nondeterminism, and platform-specific loopback assumptions.
