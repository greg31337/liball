# Universal DNS Library Development Plan

## Goal and completion criteria

Build a dependency-free `net10.0` C# library for DNS wire-format processing, DNS names and resource records, zone text, query transports, stub and iterative recursive resolution, authoritative answer production, dynamic update, zone transfer, EDNS, TSIG, and DNSSEC. The mandatory protocol baseline is RFC 1034, RFC 1035, RFC 6891, and RFC 7766. Later RFCs are included where they clarify the baseline, define records in the current IANA registry, or represent functionality expected from a general-purpose DNS library.

Completion means that:

- The library can losslessly decode, inspect, modify, and encode valid DNS messages, including unknown classes, types, flags, RDATA, and EDNS options.
- Strict and exception-less codec paths reject malformed, truncated, cyclic, overlong, or internally inconsistent data without unbounded work or out-of-range access.
- Name compression is decoded safely and emitted deterministically where the protocol permits it.
- Every currently assigned RR TYPE is represented by `DnsRecordType`; standardized schemas have typed data models, while unknown, private, experimental, obsolete, or externally specified data can always round-trip as opaque RDATA.
- UDP, TCP, TLS, HTTPS, and QUIC query transports have a common synchronous API; ordinary DNS automatically retries a truncated UDP answer over TCP.
- Stub resolution supports system-style configuration, search processing, caching, aliases, retries, and server selection.
- Iterative resolution follows referrals from root hints, enforces bailiwick, resolves name-server addresses, supports negative caching and QNAME minimization, and can validate DNSSEC.
- Authoritative processing produces correct positive, referral, wildcard, CNAME/DNAME, NODATA, NXDOMAIN, delegation, glue, EDNS, and truncation behavior from an in-memory zone store.
- Zone master files, AXFR, IXFR, NOTIFY, UPDATE, TSIG, EDNS options, DNSSEC records, validation, signing helpers, and zone digest processing are supported.
- All behavior exposed through `src/Api` has direct behavioral tests, boundary tests, malformed-input tests where applicable, and independent interoperability vectors.
- `src/` references only built-in .NET assemblies. Test-only packages and external fixture generators are allowed under `tests/`.

## Scope boundaries

- This is a library, not a command-line tool, recursive daemon product, persistent database, or operating-system resolver replacement.
- The public API is synchronous because `await` is forbidden by `syntax.md`. Cancellation and time budgets are explicit request fields. Implementations may synchronously wait on built-in .NET operations when a transport exposes only an asynchronous primitive.
- UDP/TCP client and authoritative service support is core. DoT, DoH, and DoQ clients are parity features implemented only with built-in `SslStream`, `HttpClient`, and `System.Net.Quic`; availability failures are reported clearly on unsupported platforms.
- DNSSEC cryptography is limited to algorithms available from built-in .NET cryptography. All algorithms still parse and round-trip; policy and result objects distinguish secure, insecure, bogus, indeterminate, and unsupported-algorithm outcomes.
- Multicast DNS, LLMNR, DNS service discovery browsing, DNS Stateful Operations sessions, catalog zones, and provider-specific DNS management APIs are not part of the first complete release. Their numeric codes remain losslessly processable through generic wire models.

## Standards and research basis

The downloaded reference snapshot is under `specs/`:

- `specs/rfc/` contains 107 RFC text documents. The core set is RFC 1034, 1035, 6891, and 7766. The collection also covers clarifications, serial arithmetic, negative caching, unknown RRs, IPv6, wildcards, DNAME, EDNS options, DNSSEC, TSIG, update/notify/transfer, modern record types, encrypted transports, QNAME minimization, stale answers, compact denial, and current synchronization records.
- `specs/iana/dns-parameters.{txt,xml}` is the 2026-08-24 DNS Parameters registry snapshot and is the source of numeric classes, RR types, opcodes, response codes, header flags, EDNS option codes, EDE codes, and related registries.
- `specs/iana/dns-sec-alg-numbers.xml` and `specs/iana/root-anchors.xml` provide the DNSSEC algorithm registry and root trust-anchor snapshot.
- `specs/libraries/` contains feature/reference snapshots for dnspython, miekg/dns, dnsjava, ldns, and c-ares.

Primary online references used for the comparison are the [RFC Editor](https://www.rfc-editor.org/), [IANA DNS Parameters registry](https://www.iana.org/assignments/dns-parameters), [dnspython manual](https://dnspython.readthedocs.io/en/stable/manual.html), [miekg/dns](https://github.com/miekg/dns), [dnsjava](https://github.com/dnsjava/dnsjava), [ldns](https://nlnetlabs.nl/documentation/ldns/), and [c-ares](https://github.com/c-ares/c-ares/blob/main/FEATURES.md).

### Comparison-library findings applied to this plan

| Library | Relevant demonstrated surface | Feature decision for this library |
| --- | --- | --- |
| dnspython | Names and IDNA, typed and unknown RDATA, message construction, EDNS, UDP/TCP/TLS/HTTPS/QUIC queries, stub resolver/cache, zones and zone text, transfers, update, TSIG, DNSSEC | Provide the same layers as composable C# interfaces and data objects; include both wire and presentation forms, aliases, caching, zones, transfer, update, and security. |
| miekg/dns | Client and server construction, all RRs, name compression, zone files and directives, UDP/TCP/DoT, EDNS options, AXFR/IXFR, TSIG/SIG(0), DNSSEC | Include low-level server and client primitives, complete registry coverage, deterministic compression, master-file directives, transfers, authentication, and DNSSEC. |
| dnsjava | Almost all known plus unknown records, query/update/transfer, cache, authoritative-only server, UDP-to-TCP fallback, multi-server resolver, EDNS, TSIG, DNSSEC validation | Include explicit fallback, server health/selection, cache and authoritative engine APIs, and make unknown-data round-trip a core invariant. |
| ldns | Packet/RR/name/zone central models, wire-to-host and host-to-wire modules, string conversion, network and stub resolver modules, update, TSIG, DNSSEC signing/verification | Keep codec, text, transport, resolver, zone, and security services independently usable rather than hiding wire details behind only high-level lookup calls. |
| c-ares | Hardened exception-less parsing/building, asynchronous-style parallel stub behavior, system resolver configuration, negative caching, DNS Cookies, 0x20 case randomization, special-use handling, address sorting | Add result-object `Try` paths, strict resource limits, system/search configuration, cache semantics, query hardening, and address/service convenience processing. |

## Feature inventory

### 1. Primitive wire processing and limits

- Network-order unsigned 8-, 16-, 32-, and 48-bit reads/writes; bounded byte slices; character strings; type bitmaps; base16/base32hex/base64 helpers; IP address encoding; timestamp and duration conversion.
- `ReadOnlySpan<byte>` and `Span<byte>` on hot codec entry points, with byte-array result objects where data must escape a call.
- Exact 12-byte header handling, transaction ID, QR, opcode, AA, TC, RD, RA, Z, AD, CD, base RCODE, four section counts, and preservation/rejection policy for reserved bits.
- Central limits for label length, expanded name length, pointer offset, pointer hops, message length, section counts, record RDLENGTH, TCP frame length, alias/referral depth, retries, and work performed on attacker-controlled input.
- Strict throwing operations and frequent-failure `Try` operations returning result data rather than forbidden `out` parameters.
- No unchecked truncation when converting signed API sizes to protocol-width bit fields.

### 2. DNS names

- Root, absolute, and relative names; labels retained as wire octets; presentation parsing and escaping; case-preserving display with ASCII case-insensitive DNS comparison.
- Equality, canonical comparison, suffix/ancestor tests, label count, concatenation, relativization, parent and common-ancestor operations through an operation interface rather than methods on data classes.
- RFC 1035 length enforcement: labels at most 63 octets and expanded names at most 255 octets including length/root octets.
- Compression-pointer decoding at any legal name-bearing wire location, backward and mixed pointer/label chains, pointer loop and hop detection, bounds checks, and correct continuation offset.
- Compression emission with a per-message suffix table, longest-suffix selection, 14-bit offset limits, deterministic output, optional compression disablement, and no compression where a later RFC forbids it.
- Canonical lowercase wire form with compression disabled for DNSSEC ordering/signing.
- IDNA conversion through built-in `IdnMapping`, kept explicitly separate from raw DNS names so arbitrary legal wire labels remain lossless.
- Reverse-map name construction and parsing for IPv4 `in-addr.arpa` and nibble-form IPv6 `ip6.arpa`.

### 3. Message and RRset model

- Mutable public data holders for header, question, resource record, RRset, message, and all four sections; numeric enum members plus raw integer fields where unknown values must survive.
- Correct distinction between data TYPE/CLASS and meta QTYPE/QCLASS; ordinary QUERY, STATUS preservation, NOTIFY, UPDATE section semantics, and DSO numeric preservation.
- RRset grouping by canonical owner, class, and type; TTL normalization; set equality independent of order; canonical sorting; duplicate suppression; intersection/difference helpers.
- Query/response correlation by ID, QR, opcode, question, endpoint, and optional randomized-case echo policy.
- Message cloning, normalized comparison, size estimation, section lookup, answer-chain extraction, and RCODE composition from header plus EDNS extended RCODE.
- Unknown codes, private-use values, and unrecognized header/EDNS flags remain representable even when no enum name exists.

### 4. Resource-record data

- Typed RFC 1035 records: A, NS, MD, MF, CNAME, SOA, MB, MG, MR, NULL, WKS, PTR, HINFO, MINFO, MX, and TXT.
- Typed/shared-schema legacy records: RP, AFSDB, X25, ISDN, RT, NSAP, NSAP-PTR, SIG, KEY, PX, GPOS, LOC, NXT, SRV, NAPTR, KX, CERT, A6, DNAME, APL, SPF, NID, L32, L64, LP, EUI48, EUI64, TA, and DLV.
- Typed modern/security records: AAAA, DS, SSHFP, IPSECKEY, RRSIG, NSEC, DNSKEY, DHCID, NSEC3, NSEC3PARAM, TLSA, SMIMEA, HIP, CDS, CDNSKEY, OPENPGPKEY, CSYNC, ZONEMD, SVCB, HTTPS, DSYNC, HHIT, BRID, URI, CAA, AMTRELAY, RESINFO, and NXNAME.
- Typed meta/pseudo records where meaningful: OPT, TKEY, and TSIG. IXFR, AXFR, MAILB, MAILA, and ANY are query types and are rejected as ordinary stored RDATA.
- Generic opaque RDATA for EID, NIMLOC, ATMA, SINK, NINFO, RKEY, TALINK, UNECE, ISO, UINFO, UID, GID, UNSPEC, AVC, DOA, WALLET, CLA, IPN, unassigned types, private-use types, obsolete formats not selected for typed decoding, and future registrations.
- Every type can decode/encode as opaque on request, allowing exact preservation even when typed validation would reject the schema.
- Length-prefixed character string arrays preserve boundaries, including empty strings and maximum 255-octet strings.
- Domain-name fields follow each RR's compression permission and canonicalization rules.
- NSEC/NSEC3/CSYNC type bitmap windows are ordered, minimally encoded, and strictly validated.
- SVCB/HTTPS parameter keys are strictly increasing on wire, retain unknown keys, reject duplicates, and type known mandatory, ALPN, no-default-ALPN, port, IPv4 hint, ECH, IPv6 hint, DoH path, and OHTTP values.
- Presentation parser/formatter supports mnemonic and `TYPE####`/`CLASS####` syntax and RFC 3597 `\# <length> <hex>` unknown RDATA.

### 5. DNS message codec

- Whole-message decode/encode; header-only inspection; single-name, question, RR, RDATA, and TCP-frame operations.
- Strict section count/RDLENGTH accounting, exact input consumption option, trailing-byte policy, maximum output size, and deterministic record order policy.
- OPT recognized only in Additional, singleton/version/owner validation, extended RCODE merge/split, and TSIG placement checks.
- Encoder truncation policy that removes complete RRsets, never partial individual records, sets TC, preserves the question when possible, and respects classic 512-byte or negotiated EDNS UDP size.
- TCP framing with two-octet length prefix, incremental partial-read accumulation, multiple frames on a persistent stream, zero/oversize rejection, and no assumption that one socket read equals one message.
- Decode diagnostics carry offset, section, record index, field, and reason without retaining caller spans.

### 6. Presentation format and zones

- DNS name, class, type, TTL, RDATA, question, RR, RRset, and message text conversion.
- Master-file tokenization for whitespace, quoted strings, escapes, comments, parentheses, omitted owners, relative names, and multiline RDATA.
- `$ORIGIN`, `$TTL`, `$INCLUDE` with relative path/origin handling and cycle/depth limits, and miekg/BIND-style `$GENERATE` with bounded expansion.
- RFC 2308 SOA minimum/negative-TTL semantics and flexible TTL units.
- Zone validation: exactly one apex SOA, apex NS presence, in-zone ownership, CNAME exclusivity, DNAME constraints, delegation cuts, glue classification, RRset TTL consistency, serial arithmetic, and optional DNSSEC/ZONEMD checks.
- Canonical zone output and round-trip-preserving output options; deterministic ordering suitable for fixtures and ZONEMD.

### 7. EDNS

- OPT owner/root, requestor UDP payload size, extended RCODE, version, DO/CO/unknown flags, option list, and effective response-code handling.
- EDNS version negotiation and BADVERS; FORMERR policy for malformed/multiple OPT; no-EDNS fallback for incompatible upstream servers.
- Typed options: Update Lease, NSID, DAU, DHU, N3U, Client Subnet, EXPIRE, COOKIE, TCP Keepalive, Padding, CHAIN, Key Tag, Extended DNS Error, Report Channel, and ZONEVERSION.
- Opaque preservation for LLQ, reserved, experimental, private, draft, and future options.
- ECS family/source/scope/address-bit validation and masking; DNS Cookie client/server size and validation rules; padding block calculation; EDE code plus optional UTF-8 text; option-specific length checks.
- UDP payload clamping and path-safe defaults, request/response option policy, and amplification-aware authoritative behavior.

### 8. Query transports and client behavior

- Common transport request/result model includes endpoint/URI, elapsed time, response bytes/message, transport used, timeout/cancellation state, truncation/fallback metadata, and protocol diagnostics.
- UDP over IPv4/IPv6 with randomized query IDs, randomized source port provided by the OS, connected or endpoint-filtered receive, timeout via polling/deadline, response matching, and ignoring unrelated datagrams within the budget.
- TCP per RFC 7766: length framing, partial reads/writes, persistent connections, multiple sequential queries, idle/lifetime limits, server-initiated close handling, and connection reuse policy.
- Automatic UDP-to-TCP retry on TC, locally detected UDP truncation, or response larger than the receive policy; optional direct TCP.
- Multi-server selection, rotation/randomization, bounded retries, per-attempt and total lifetime budgets, protocol-error classification, and EDNS downgrade.
- DoT with SNI, certificate validation, configurable authentication name, port 853, and connection reuse.
- DoH POST and GET, `application/dns-message`, HTTP status/content-type/size validation, URI bootstrap configuration, and no accidental use of the DNS resolver being bootstrapped.
- DoQ stream framing and protocol errors using built-in QUIC where available.
- Test-injectable clock, entropy, and transport interfaces without public delegate declarations.

### 9. Stub resolver and high-level lookup helpers

- Explicit resolver configuration plus POSIX `resolv.conf` parsing and a portable caller-supplied system configuration model; nameservers, ports, search/domain, ndots, attempts, timeout, rotate, TCP, and trust-AD options.
- Absolute and relative query handling, search-list order, trailing-dot semantics, no duplicate candidates, and terminal/error policy per candidate.
- Positive and negative cache keyed by canonical question plus relevant DNSSEC/ECS context; TTL aging; zero TTL; SOA-derived NXDOMAIN/NODATA TTL; capacity eviction; flush and subtree invalidation; optional serve-stale window.
- CNAME and DNAME chain processing, synthesized CNAME for DNAME, canonical name reporting, loop/depth control, and caching of intermediate RRsets.
- A/AAAA address lookup, PTR reverse lookup, MX lookup with preference ordering, SRV priority/weight selection using injected entropy, HTTPS/SVCB service binding resolution, and TXT convenience results without hiding the original message.
- Address ordering using a documented RFC 6724-compatible subset and preservation of DNS TTL/security metadata.
- Special-use local policy hooks represented as data rather than hard-coded interception; `.onion` rejection and `localhost` behavior can be enabled by configuration.
- Parallel-query behavior may be achieved by caller concurrency; all public objects document thread-safety expectations through behavior and tests, not mutable global state.

### 10. Iterative recursive resolver

- Root-hint initialization and refresh input, closest-known delegation start, iterative RD=0 queries, referral parsing, glue use, and recursive address lookup for out-of-bailiwick name servers.
- Bailiwick enforcement for cached authority/glue, delegation-cut tracking, lame referral detection, cyclic dependency and depth limits, sibling/out-of-zone glue rejection, and retry across candidate authorities.
- CNAME/DNAME following, wildcard answers, NXDOMAIN and NODATA proofs, empty non-terminals, referral versus authoritative negative-answer distinction, and aggressive NSEC/NSEC3 negative caching when validated.
- QNAME minimization with fallback for broken authorities; 0x20 case randomization; unpredictable IDs/source ports; response-question/source validation; maximum work and query budgets.
- EDNS capability learning, payload reduction, no-EDNS fallback, TCP fallback, server health, lame-server suppression, and per-zone reachability state.
- Recursive cache separates authoritative, answer, glue, negative, failure, DNSKEY/DS, and validation metadata; TTL never increases and credibility rules prevent lower-quality replacement.
- Optional serve-stale behavior, prefetch threshold, failure caching bounded by policy, and EDE reporting for high-level failures.
- DNSSEC chain-of-trust validation from configured trust anchors, with CD/DO/AD behavior and secure/insecure/bogus/indeterminate results.

### 11. DNSSEC and authenticated messages

- Parse/encode and canonicalize DS, CDS, DNSKEY, CDNSKEY, RRSIG, NSEC, NSEC3, NSEC3PARAM, SIG, KEY, and related algorithm/digest fields.
- Canonical owner/RDATA ordering, wildcard label handling, original TTL handling, inception/expiration serial-time comparisons, key-tag calculation, DS digest generation, and RRset signed-data construction.
- Built-in validation/signing where .NET supports RSA/SHA-1 validation, RSA/SHA-256, RSA/SHA-512, ECDSA P-256/SHA-256, ECDSA P-384/SHA-384, Ed25519, SHA-1/SHA-256/SHA-384 DS digests, and NSEC3 SHA-1.
- Algorithm policy follows RFC 8624-style allowed/validate-only/forbidden/unsupported states and can be replaced through configuration data.
- NSEC and NSEC3 authenticated denial checks for NXDOMAIN, NODATA, wildcard, opt-out, closest encloser, next closer, iteration/salt limits, and malformed chains.
- Trust-anchor loading from DS/DNSKEY records, RFC 5011-style state explicitly left out of initial scope, and root-anchor fixture compatibility.
- Zone signing helpers build RRSIGs and NSEC/NSEC3 chains without owning private-key storage; private key bytes/objects are caller-supplied and never logged.
- ZONEMD scheme/hash calculation and verification over canonical zone data.
- TSIG request/response and multi-message transfer MAC chaining; HMAC-SHA-256/384/512, legacy SHA-1, time/fudge checks, original ID, BADKEY/BADSIG/BADTIME, canonical key/algorithm names, and mandatory last-additional placement.
- SIG(0) and TKEY receive full wire/presentation models; SIG(0) verification and automated TKEY negotiation are extension points, not required cryptographic operations in the first release.

### 12. Zone storage and authoritative answers

- In-memory zone store indexed by canonical reversed labels and RRset key, supporting exact names, closest encloser, wildcard candidate, ancestor/delegation lookup, predecessor/successor for DNSSEC, and immutable snapshot-style reads at request boundaries.
- Zone add/replace/delete and atomic transaction objects represented as data plus operation interfaces; SOA serial arithmetic and journal entries support IXFR.
- Longest-zone match across multiple classes/zones; authoritative processing only below an owned apex and not beneath delegated children.
- Exact answer, CNAME chain, DNAME plus synthesized CNAME, wildcard synthesis, referral NS, in-bailiwick A/AAAA glue, apex and delegation DS handling, additional-section processing for NS/MX/SRV/SVCB targets, and duplicate suppression.
- Correct AA, RA, RD echo, AD/CD policy, RCODEs, question echo, SOA negative authority, NXDOMAIN versus NODATA, empty non-terminals, CNAME exclusivity, QTYPE ANY minimization, and unknown QTYPE behavior.
- EDNS negotiation, COOKIE verification/issue, EDE, response-size selection, RRset-atomic truncation, amplification limits, malformed query handling, NOTIMP for unsupported opcodes, and REFUSED policy.
- Pluggable data-only policy for recursion availability, allow/deny networks, transfer/update permissions, minimal responses, additional data, ANY behavior, maximum UDP size, rate limits, and DNSSEC serving.
- UDP and TCP authoritative service loops, TCP framing/pipelining/idle limits, bounded request processing, graceful stop signal represented without public events/delegates, and query handler isolation through exception-less decode paths.

### 13. Zone maintenance protocols

- AXFR query/client/server with SOA-first/SOA-last validation, multi-message transfer, message size policy, TCP-only operation, TSIG chaining, atomic zone replacement, and out-of-zone rejection.
- IXFR query/client/server with serial arithmetic, up-to-date response, delete/add delta sequences, AXFR fallback, journal coverage checks, TSIG chaining, and atomic application.
- NOTIFY construction, primary/secondary validation, SOA serial comparison, acknowledgement, configured target retries, and transfer trigger result.
- RFC 2136 UPDATE zone/prerequisite/update/additional sections; name-in-use/name-not-in-use, RRset-exists/value-dependent/value-independent prerequisites; add/delete-RR/delete-RRset/delete-name; class/TTL/type restrictions; atomic application and update RCODE mapping.
- TSIG authentication and authorization policy for transfers, notify, and update; no persistence of secrets in public static state.

## Public API shape

The names below are proposed API units, not final signatures. Every public member will have the required XML comments as part of its implementation task; there is no separate documentation task.

### `src/Api/Data`

- Core wire/message data: `DnsName`, `DnsMessageHeader`, `DnsQuestion`, `DnsResourceRecord`, `DnsResourceRecordSet`, `DnsMessage`, `DnsDecodeOptions`, `DnsEncodeOptions`, `DnsDecodeResult`, `DnsEncodeResult`, `DnsDiagnostic`, `DnsTcpFrame`, and `DnsCanonicalOrderOptions`.
- Shared RDATA data: `DnsAddressData`, `DnsNameData`, `DnsTwoNameData`, `DnsPreferenceNameData`, `DnsCharacterStringsData`, `DnsOpaqueData`, and `DnsFixedBytesData`.
- Specific RDATA data: `DnsSoaData`, `DnsWksData`, `DnsPxData`, `DnsLocData`, `DnsSrvData`, `DnsNaptrData`, `DnsCertData`, `DnsAplData`, `DnsSignatureData`, `DnsKeyData`, `DnsDsData`, `DnsSshfpData`, `DnsIpsecKeyData`, `DnsNsecData`, `DnsNsec3Data`, `DnsNsec3ParamData`, `DnsDhcidData`, `DnsTlsaData`, `DnsHipData`, `DnsCsyncData`, `DnsZonemdData`, `DnsSvcbData`, `DnsSvcbParameter`, `DnsDsyncData`, `DnsIlNpData`, `DnsEuiData`, `DnsUriData`, `DnsCaaData`, `DnsAmtRelayData`, `DnsResolverInformationData`, `DnsTkeyData`, and `DnsTsigData`.
- EDNS data: `DnsEdnsData`, `DnsEdnsOption`, `DnsEdnsClientSubnet`, `DnsEdnsCookie`, `DnsEdnsKeepalive`, `DnsEdnsExtendedError`, `DnsEdnsZoneVersion`, and `DnsEdnsOptionResult`.
- Text/zone data: `DnsTextOptions`, `DnsTextResult`, `DnsZone`, `DnsZoneNode`, `DnsZoneLoadOptions`, `DnsZoneLoadResult`, `DnsZoneValidationResult`, `DnsZoneDiagnostic`, `DnsZoneChange`, `DnsZoneTransaction`, and `DnsZoneJournalEntry`.
- Transport/query data: `DnsEndpoint`, `DnsTransportRequest`, `DnsTransportResult`, `DnsTransportOptions`, `DnsTlsOptions`, `DnsHttpsOptions`, `DnsQuicOptions`, `DnsQueryRequest`, `DnsQueryResult`, `DnsServerState`, and `DnsClockValue`.
- Resolver/cache data: `DnsResolverOptions`, `DnsResolverConfiguration`, `DnsResolverServer`, `DnsResolveRequest`, `DnsResolveResult`, `DnsAliasStep`, `DnsCacheEntry`, `DnsCacheLookup`, `DnsRootHints`, `DnsRecursiveOptions`, `DnsDelegation`, `DnsAddressResult`, `DnsMailResult`, `DnsServiceResult`, and `DnsServiceEndpoint`.
- Security data: `DnsSecurityPolicy`, `DnsSecurityResult`, `DnsTrustAnchor`, `DnsKeyMaterial`, `DnsSignatureRequest`, `DnsTsigKey`, `DnsTsigResult`, and `DnsZoneDigestResult`.
- Authoritative/maintenance data: `DnsAuthoritativeOptions`, `DnsAuthoritativeRequest`, `DnsAuthoritativeResult`, `DnsServerOptions`, `DnsServerRunResult`, `DnsTransferRequest`, `DnsTransferResult`, `DnsNotifyRequest`, `DnsNotifyResult`, `DnsUpdateRequest`, `DnsUpdatePrerequisite`, `DnsUpdateChange`, `DnsUpdateResult`, and `DnsAuthorizationPolicy`.

All data classes contain public mutable fields and no public methods. Optional value semantics use a value field plus an explicit presence boolean; nullable reference fields are used only for reference types.

### `src/Api/Enums`

- Protocol registries: `DnsClass`, `DnsRecordType`, `DnsOpcode`, `DnsResponseCode`, `DnsMessageSection`, `DnsHeaderFlag`, `DnsEdnsFlag`, and `DnsEdnsOptionCode`.
- Record/security registries: `DnsSvcbParameterKey`, `DnsDnssecAlgorithm`, `DnsDigestAlgorithm`, `DnsNsec3HashAlgorithm`, `DnsTsigError`, `DnsExtendedErrorCode`, `DnsSecurityStatus`, and `DnsAlgorithmPolicy`.
- Operation state: `DnsDiagnosticCode`, `DnsTransportKind`, `DnsTransportStatus`, `DnsCacheEntryKind`, `DnsResolveStatus`, `DnsAuthoritativeAnswerKind`, `DnsZoneChangeKind`, `DnsTransferKind`, `DnsUpdatePrerequisiteKind`, `DnsUpdateChangeKind`, and `DnsServerStatus`.

Enums use the default signed `int` underlying type required by `syntax.md`. Raw protocol integer fields remain alongside enum convenience fields when the registry permits unknown values.

### `src/Api/Static`

- `DnsProtocolLimits`: header, label, name, classic UDP, EDNS, TCP, count, and compression constants.
- `DnsPorts`: DNS, DoT, DoH default, and DoQ port constants.
- `DnsWellKnownNames`: root, reverse-map suffix, DNSSEC algorithm-name, and TSIG algorithm-name fields. These are static readonly data only; no methods.

### `src/Api/Contracts`

- Low level: `IDnsNameService`, `IDnsMessageCodec`, `IDnsRecordDataCodec`, `IDnsRecordSetService`, and `IDnsTextCodec`.
- Zones/security: `IDnsZoneCodec`, `IDnsZoneStore`, `IDnsDnssecService`, `IDnsTsigService`, and `IDnsZoneDigestService`.
- Network/query: `IDnsTransport`, `IDnsQueryClient`, `IDnsCache`, `IDnsResolver`, and `IDnsRecursiveResolver`.
- Authoritative/maintenance: `IDnsAuthoritativeService`, `IDnsUpdateService`, `IDnsTransferClient`, `IDnsTransferService`, `IDnsNotifyService`, and `IDnsServer`.
- High level: `IDnsLookupService`.

Interfaces contain only uniquely named instance methods, no inheritance, no generic methods, and no overloads. Examples of distinct operation names are `DecodeMessage`, `TryDecodeMessage`, `EncodeMessage`, `TryEncodeMessage`, `ParseZone`, `FormatZone`, `Exchange`, `Query`, `Resolve`, `ResolveIteratively`, `AnswerAuthoritatively`, `ApplyUpdate`, `ReceiveTransfer`, and `LookupAddresses`.

### `src/Api/Exceptions`

- `DnsFormatException`, `DnsTransportException`, `DnsResolutionException`, `DnsZoneException`, and `DnsSecurityException`, each deriving directly from `System.Exception`.

### `src/Api/Classes`

- Codec/model operations: `DnsNameService`, `DnsMessageCodec`, `DnsRecordDataCodec`, `DnsRecordSetService`, and `DnsTextCodec`.
- Zone/security operations: `DnsZoneCodec`, `MemoryDnsZoneStore`, `DnsDnssecService`, `DnsTsigService`, and `DnsZoneDigestService`.
- Transports/query: `DnsUdpTransport`, `DnsTcpTransport`, `DnsTlsTransport`, `DnsHttpsTransport`, `DnsQuicTransport`, and `DnsQueryClient`.
- Resolution: `MemoryDnsCache`, `DnsStubResolver`, `DnsRecursiveResolver`, and `DnsLookupService`.
- Authoritative/maintenance: `DnsAuthoritativeService`, `DnsUpdateService`, `DnsTransferClient`, `DnsTransferService`, `DnsNotifyService`, and `DnsServer`.

Every public implementation class implements exactly its listed operation interface, has at most one public constructor, and exposes no public members beyond that constructor and its interface members.

## Planned source files

Public files may contain several closely related public data-holder or enum declarations; no non-public helper is placed under `src/Api`.

```text
src/Api/Data/CoreWireData.cs
src/Api/Data/MessageData.cs
src/Api/Data/SharedRdata.cs
src/Api/Data/ClassicRdata.cs
src/Api/Data/LegacyRdata.cs
src/Api/Data/SecurityRdata.cs
src/Api/Data/ModernRdata.cs
src/Api/Data/EdnsData.cs
src/Api/Data/TextData.cs
src/Api/Data/ZoneData.cs
src/Api/Data/TransportData.cs
src/Api/Data/ResolverData.cs
src/Api/Data/SecurityData.cs
src/Api/Data/AuthoritativeData.cs
src/Api/Data/MaintenanceData.cs
src/Api/Enums/ProtocolEnums.cs
src/Api/Enums/RecordEnums.cs
src/Api/Enums/SecurityEnums.cs
src/Api/Enums/OperationEnums.cs
src/Api/Static/DnsProtocolLimits.cs
src/Api/Static/DnsPorts.cs
src/Api/Static/DnsWellKnownNames.cs
src/Api/Contracts/IDnsNameService.cs
src/Api/Contracts/IDnsMessageCodec.cs
src/Api/Contracts/IDnsRecordDataCodec.cs
src/Api/Contracts/IDnsRecordSetService.cs
src/Api/Contracts/IDnsTextCodec.cs
src/Api/Contracts/IDnsZoneCodec.cs
src/Api/Contracts/IDnsZoneStore.cs
src/Api/Contracts/IDnsDnssecService.cs
src/Api/Contracts/IDnsTsigService.cs
src/Api/Contracts/IDnsZoneDigestService.cs
src/Api/Contracts/IDnsTransport.cs
src/Api/Contracts/IDnsQueryClient.cs
src/Api/Contracts/IDnsCache.cs
src/Api/Contracts/IDnsResolver.cs
src/Api/Contracts/IDnsRecursiveResolver.cs
src/Api/Contracts/IDnsAuthoritativeService.cs
src/Api/Contracts/IDnsUpdateService.cs
src/Api/Contracts/IDnsTransferClient.cs
src/Api/Contracts/IDnsTransferService.cs
src/Api/Contracts/IDnsNotifyService.cs
src/Api/Contracts/IDnsServer.cs
src/Api/Contracts/IDnsLookupService.cs
src/Api/Exceptions/DnsFormatException.cs
src/Api/Exceptions/DnsTransportException.cs
src/Api/Exceptions/DnsResolutionException.cs
src/Api/Exceptions/DnsZoneException.cs
src/Api/Exceptions/DnsSecurityException.cs
src/Api/Classes/DnsNameService.cs
src/Api/Classes/DnsMessageCodec.cs
src/Api/Classes/DnsRecordDataCodec.cs
src/Api/Classes/DnsRecordSetService.cs
src/Api/Classes/DnsTextCodec.cs
src/Api/Classes/DnsZoneCodec.cs
src/Api/Classes/MemoryDnsZoneStore.cs
src/Api/Classes/DnsDnssecService.cs
src/Api/Classes/DnsTsigService.cs
src/Api/Classes/DnsZoneDigestService.cs
src/Api/Classes/DnsUdpTransport.cs
src/Api/Classes/DnsTcpTransport.cs
src/Api/Classes/DnsTlsTransport.cs
src/Api/Classes/DnsHttpsTransport.cs
src/Api/Classes/DnsQuicTransport.cs
src/Api/Classes/DnsQueryClient.cs
src/Api/Classes/MemoryDnsCache.cs
src/Api/Classes/DnsStubResolver.cs
src/Api/Classes/DnsRecursiveResolver.cs
src/Api/Classes/DnsLookupService.cs
src/Api/Classes/DnsAuthoritativeService.cs
src/Api/Classes/DnsUpdateService.cs
src/Api/Classes/DnsTransferClient.cs
src/Api/Classes/DnsTransferService.cs
src/Api/Classes/DnsNotifyService.cs
src/Api/Classes/DnsServer.cs
src/Internal/Wire/WireReader.cs
src/Internal/Wire/WireWriter.cs
src/Internal/Wire/CompressionTable.cs
src/Internal/Wire/FrameReader.cs
src/Internal/Records/RecordSchemaCodec.cs
src/Internal/Records/TypeBitmapCodec.cs
src/Internal/Records/SvcbParameterCodec.cs
src/Internal/Text/ZoneTokenizer.cs
src/Internal/Text/ZoneDirectiveProcessor.cs
src/Internal/Net/SocketOperations.cs
src/Internal/Net/ConnectionPool.cs
src/Internal/Resolver/ResolutionState.cs
src/Internal/Resolver/DelegationState.cs
src/Internal/Resolver/CacheIndex.cs
src/Internal/Zone/ZoneIndex.cs
src/Internal/Zone/ZoneTransactionEngine.cs
src/Internal/Security/CanonicalDnsData.cs
src/Internal/Security/DnssecAlgorithms.cs
src/Internal/Security/NsecProofValidator.cs
src/Internal/Security/Nsec3ProofValidator.cs
src/Internal/Security/TsigMac.cs
```

## Simplified C# implementation strategy

- Public data is held in classes with fields; behavior lives behind interfaces and implementation classes. No properties, records, structs, nested types, extension methods, or user-defined generic types are planned.
- Optional protocol integers use `HasValue` booleans plus non-nullable value fields because nullable value types are forbidden. Optional object/string/array fields may use nullable reference syntax.
- `Try` operations return result data classes because `out`/`ref` parameters are forbidden. Throwing operations raise the category-specific exception; malformed network input should normally use the exception-less path.
- Methods are intentionally not overloaded. Names identify variants such as text versus wire, strict versus try, message versus frame, and single versus batch.
- No interface inherits another. A public class normally implements one interface, preventing combined-interface name collisions.
- Iteration uses ordinary loops rather than LINQ. Type dispatch uses the RR/option numeric code and explicit casts rather than pattern matching or `typeof`.
- `ReadOnlySpan<byte>` and `Span<byte>` are used only as method inputs/outputs that do not escape. Persistent bytes use `byte[]` because classes cannot contain span fields.
- Networking and cryptography are synchronous. Socket polling, explicit result codes, cancellation checks, and exception-less codec entry points reduce the need for forbidden `try`/`catch`; unrecoverable built-in failures propagate as operation exceptions at the public boundary where feasible under the syntax subset.
- No mutable global resolver/cache/entropy state is introduced. Static API classes contain constants or readonly data only and no methods.

## Implementation tasks

Tasks are ordered by dependency. A task includes the implementation and required XML comments for any public API members it introduces. Test tasks are explicitly separate and cover the entire public `src/Api` surface; there are no syntax-policy, source-scanning, performance, benchmark, documentation, packaging, or release tasks.

### Foundation and public model

- [DONE] T001: Define protocol, record, security, EDNS, transport, resolver, authoritative, transfer, update, and diagnostic enums from the checked-in IANA snapshots, retaining raw numeric fields for unknown values.
- [DONE] T002: Define `DnsProtocolLimits`, `DnsPorts`, and `DnsWellKnownNames` as method-free static data classes with conservative defaults and protocol maxima.
- [DONE] T003: Define core name, header, question, record, RRset, message, codec option, codec result, diagnostic, and TCP-frame public data classes.
- [DONE] T004: Define shared, classic, legacy, security, and modern RDATA public data classes and map every assigned RR TYPE to a typed, shared-schema, meta-query, or opaque representation.
- [DONE] T005: Define EDNS and typed EDNS-option public data classes, including generic opaque option data.
- [DONE] T006: Define text, zone, zone validation, zone change, transaction, and journal public data classes.
- [DONE] T007: Define transport, endpoint, query, timing, and server-state public data classes.
- [DONE] T008: Define stub resolver, iterative resolver, cache, delegation, alias-chain, address, mail, and service-result public data classes.
- [DONE] T009: Define DNSSEC, trust-anchor, key-material, signing, TSIG, and zone-digest public data classes.
- [DONE] T010: Define authoritative, server, authorization, update, notify, and transfer public data classes.
- [DONE] T011: Define all low-level, network, resolver, security, zone, authoritative, and maintenance operation interfaces with unique non-overloaded method names.
- [DONE] T012: Define format, transport, resolution, zone, and security exceptions, each derived directly from `System.Exception`.

### Wire primitives, names, and message structure

- [DONE] T013: Implement bounded big-endian wire reading for integers, bytes, character strings, addresses, type bitmaps, and subregions with offset-aware diagnostics.
- [DONE] T014: Implement bounded big-endian wire writing, size counting, patchable section counts/RDLENGTH, and explicit overflow failure.
- [DONE] T015: Implement raw DNS name validation, root/absolute/relative construction, label and expanded-length enforcement, and case-preserving storage.
- [DONE] T016: Implement presentation-name parsing and escaping, including decimal escapes, special characters, root, relative names, and origin joining.
- [DONE] T017: Implement ASCII case-insensitive equality, canonical comparison, suffix/ancestor, parent, concatenate, relativize, and common-ancestor name operations.
- [DONE] T018: Implement IDNA-to-DNS and DNS-to-Unicode operations using built-in `IdnMapping` without changing raw wire-name semantics.
- [DONE] T019: Implement IPv4 and IPv6 reverse-map construction and validation helpers.
- [DONE] T020: Implement compressed-name decoding with pointer bounds, continuation offset, loop/hop detection, maximum expanded length, and diagnostic location.
- [DONE] T021: Implement deterministic longest-suffix name compression with legal-location policy, 14-bit offsets, and canonical/uncompressed modes.
- [DONE] T022: Implement header and question decoding/encoding with all flags, unknown opcode/RCODE preservation, and count validation.
- [DONE] T023: Implement `DnsRecordSetService` grouping, TTL normalization, duplicate handling, set operations, canonical ordering, and answer-chain lookup.
- [DONE] T024: Implement message clone, normalized comparison, section search, answer-chain extraction, RCODE merge/split, and encoded-size estimation operations.

### Resource-record codecs

- [DONE] T025: Implement shared codecs for opaque bytes, fixed bytes, single name, two names, preference plus name, character strings, addresses, and integer/name combinations.
- [DONE] T026: Implement RFC 1035 A, NS, MD, MF, CNAME, SOA, MB, MG, MR, NULL, WKS, PTR, HINFO, MINFO, MX, and TXT wire codecs.
- [DONE] T027: Implement RP, AFSDB, X25, ISDN, RT, NSAP, NSAP-PTR, PX, GPOS, KX, and legacy shared-schema wire codecs.
- [DONE] T028: Implement AAAA, LOC, SRV, NAPTR, CERT, DNAME, APL, URI, CAA, EUI48, and EUI64 wire codecs.
- [DONE] T029: Implement SIG, KEY, DS, CDS, DNSKEY, CDNSKEY, SSHFP, IPSECKEY, RRSIG, DHCID, TLSA, SMIMEA, OPENPGPKEY, and HIP wire codecs.
- [DONE] T030: Implement NXT, NSEC, NSEC3, NSEC3PARAM, and CSYNC codecs with strict canonical type-bitmap handling.
- [DONE] T031: Implement NID, L32, L64, LP, AMTRELAY, ZONEMD, RESINFO, DSYNC, HHIT, BRID, and NXNAME wire codecs.
- [DONE] T032: Implement SVCB and HTTPS wire codecs with sorted unique parameter keys, typed known parameter values, mandatory-key checks, and unknown preservation.
- [DONE] T033: Implement TKEY and TSIG wire codecs, including 48-bit time fields and TSIG placement metadata.
- [DONE] T034: Implement opaque fallback for all untyped, experimental, obsolete, private-use, unassigned, and future RR types, with opt-in opaque decoding for otherwise typed records.
- [DONE] T035: Implement record-level schema dispatch, exact RDLENGTH consumption, compression permission per field, and typed-to-opaque error policy.

### Message codec and text processing

- [DONE] T036: Implement full message decode with section counts, record indexes, strict/trailing-data modes, resource limits, and exception-less diagnostics.
- [DONE] T037: Implement full message encode with deterministic section ordering options, count derivation, maximum-size enforcement, and exact-buffer sizing.
- [DONE] T038: Implement RRset-atomic UDP truncation that sets TC and retains the question and as much ordered content as policy permits.
- [DONE] T039: Implement TCP length-prefix encode/decode and incremental frame accumulation for partial and multiple frames.
- [DONE] T040: Implement strict throwing and result-object `Try` entry points for name, RDATA, RR, message, and TCP-frame operations.
- [DONE] T041: Implement text formatting/parsing for names, TTLs, classes, types, questions, RR fields, and RFC 3597 unknown RDATA.
- [DONE] T042: Implement presentation codecs for every typed RDATA schema, including canonical and human-readable output modes.
- [DONE] T043: Implement message text output with header flags, sections, EDNS details, and stable ordering suitable for diagnostics.
- [DONE] T044: Implement master-file tokenizer for comments, quotes, escapes, parentheses, multiline records, omitted owners, and source locations.
- [DONE] T045: Implement master-file record parser with owner/origin inheritance, default TTL/class, relative names, and flexible TTL units.
- [DONE] T046: Implement `$ORIGIN`, `$TTL`, `$INCLUDE`, and bounded `$GENERATE` processing with include path, origin, cycle, depth, and expansion controls.
- [DONE] T047: Implement canonical and round-trip-oriented zone serialization with deterministic record ordering.
- [DONE] T048: Implement zone structural validation for apex SOA/NS, ownership, RRset TTLs, CNAME/DNAME rules, delegations/glue, and serial arithmetic.

### EDNS

- [DONE] T049: Implement OPT recognition, singleton/root/additional-section validation, UDP size, extended RCODE, version, DO/CO/unknown flags, and option framing.
- [DONE] T050: Implement typed Update Lease, NSID, DAU, DHU, N3U, EXPIRE, TCP Keepalive, Padding, CHAIN, and Key Tag option codecs.
- [DONE] T051: Implement Client Subnet family/prefix/address masking and strict request/response scope validation.
- [DONE] T052: Implement DNS Cookie option construction, parsing, client/server length rules, and built-in cryptographic server-cookie generation/verification.
- [DONE] T053: Implement Extended DNS Error, Report Channel, and ZONEVERSION option codecs and validation.
- [DONE] T054: Implement opaque EDNS option preservation, private/experimental codes, duplicate/order policy, and malformed-length diagnostics.
- [DONE] T055: Implement EDNS request construction, response negotiation, BADVERS, payload clamping, no-EDNS downgrade metadata, and option response policy.

### Network transports and query client

- [DONE] T056: Implement internal monotonic deadline, cancellation, entropy, endpoint, socket-polling, and complete-send/receive helpers using built-in .NET APIs.
- [DONE] T057: Implement IPv4/IPv6 UDP exchange with source/ID/question matching, unrelated-datagram filtering, receive limits, and timeout results.
- [DONE] T058: Implement TCP exchange with framing, partial I/O, persistent connection reuse, idle/lifetime limits, and sequential-query support per RFC 7766.
- [DONE] T059: Implement UDP-to-TCP fallback for TC, local truncation, and response-size conditions while preserving total lifetime and result metadata.
- [DONE] T060: Implement multi-server selection, rotation/randomization, retry budgets, server health, protocol failure classification, and EDNS downgrade in `DnsQueryClient`.
- [DONE] T061: Implement DoT exchange using built-in `SslStream`, authentication-name/SNI handling, certificate validation, port defaults, framing, and reuse.
- [DONE] T062: Implement DoH GET/POST exchange using built-in `HttpClient`, media type, HTTP status, body limit, bootstrap endpoint, and redirect policy validation.
- [DONE] T063: Implement DoQ exchange using built-in `System.Net.Quic`, DNS message framing, stream lifecycle, protocol error mapping, and platform-availability results.
- [DONE] T064: Implement transport disposal/stop behavior, thread-safe connection state, and deterministic resource ownership across all public transports.

### Cache, stub resolver, and lookup helpers

- [DONE] T065: Implement canonical cache keys, positive/negative/failure entries, TTL aging, zero-TTL behavior, capacity eviction, flush, and subtree invalidation.
- [DONE] T066: Implement RFC 2308/RFC 9520 NXDOMAIN and NODATA cache TTL derivation, NXDOMAIN cuts, and bounded failure caching.
- [DONE] T067: Implement optional serve-stale lookup/prefetch metadata without increasing original TTL or replacing fresh data.
- [DONE] T068: Implement explicit resolver configuration validation and POSIX `resolv.conf` parsing for nameserver, search/domain, ndots, timeout, attempts, rotate, TCP, and trust-AD.
- [DONE] T069: Implement stub candidate-name generation for absolute/relative inputs, ndots, search order, duplicate suppression, and terminal status handling.
- [DONE] T070: Implement stub query execution through `DnsQueryClient`, cache lookup/store, retries, requested recursion/EDNS/DNSSEC flags, and response validation.
- [DONE] T071: Implement CNAME/DNAME chain processing, DNAME CNAME synthesis, canonical-name reporting, intermediate caching, and loop/depth limits.
- [DONE] T072: Implement A/AAAA lookup and address result aggregation with TTL/security metadata and RFC 6724-compatible ordering.
- [DONE] T073: Implement PTR reverse, MX preference, TXT, and SRV priority/weighted-selection lookup operations using injected entropy.
- [DONE] T074: Implement SVCB/HTTPS alias/service-mode processing, mandatory parameter enforcement, target resolution, and service endpoint results.
- [DONE] T075: Implement configurable `.onion`, `localhost`, and caller-supplied special-use policies without hard-coded global interception.

### Iterative recursive resolution

- [DONE] T076: Implement root-hint loading, validation, cache seeding, and closest-known-delegation selection.
- [DONE] T077: Implement RD=0 iterative query state, query/work/depth budgets, referral recognition, delegation tracking, and authority candidate selection.
- [DONE] T078: Implement glue extraction and bailiwick validation, sibling/out-of-zone rejection, credibility ranking, and safe cache insertion.
- [DONE] T079: Implement recursive address resolution for name servers, cyclic dependency detection, alternate-server progress, and lame referral suppression.
- [DONE] T080: Implement iterative positive, CNAME, DNAME, wildcard, referral, NXDOMAIN, NODATA, empty-nonterminal, and authoritative-negative processing.
- [DONE] T081: Implement QNAME minimization with per-zone fallback for incompatible authorities.
- [DONE] T082: Implement 0x20 query-case randomization, response case/source/question validation, unpredictable IDs, and retry-safe entropy usage.
- [DONE] T083: Implement per-server EDNS capability learning, payload reduction, no-EDNS fallback, TCP fallback, health, and reachability state.
- [DONE] T084: Implement recursive cache credibility/replacement rules for answer, authority, glue, negative, failure, DNSKEY/DS, and validation data.
- [DONE] T085: Implement iterative total-lifetime handling, stale-answer fallback, prefetch decisions, EDE failure mapping, and final result diagnostics.

### DNSSEC, TSIG, and zone digests

- [DONE] T086: Implement DNSSEC canonical name/RDATA/RRset ordering, wildcard owner reconstruction, original TTL handling, and RRSIG signed-data construction.
- [DONE] T087: Implement key-tag calculation, DNSKEY public-key decoding, DS/CDS digest generation, serial-time inception/expiration checks, and algorithm policy.
- [DONE] T088: Implement built-in RSA/SHA-1 validation, RSA/SHA-256, RSA/SHA-512, ECDSA P-256/SHA-256, ECDSA P-384/SHA-384, and Ed25519 signature adapters where supported by .NET.
- [DONE] T089: Implement RRset signature validation across candidate keys/signatures with secure, bogus, insecure, indeterminate, forbidden, and unsupported results.
- [DONE] T090: Implement DS-to-DNSKEY chain validation, trust-anchor matching, zone-cut transitions, CD/DO/AD behavior, and key-cache integration.
- [DONE] T091: Implement NSEC authenticated denial for NXDOMAIN, NODATA, wildcard, closest-encloser, next-closer, and bitmap semantics.
- [DONE] T092: Implement bounded NSEC3 hashing and authenticated denial including salt, iterations, opt-out, closest-encloser, next-closer, and wildcard proofs.
- [DONE] T093: Integrate DNSSEC validation and aggressive NSEC/NSEC3 negative caching into iterative resolution.
- [DONE] T094: Implement caller-key zone/RRset signing helpers and NSEC/NSEC3 chain generation without private-key persistence.
- [DONE] T095: Implement ZONEMD simple scheme calculation and verification with supported digest algorithms and canonical zone exclusions.
- [DONE] T096: Implement TSIG HMAC algorithms, canonical variables, request/response MAC, time/fudge, original ID, error mapping, and constant-time verification.
- [DONE] T097: Implement TSIG multi-message MAC chaining for AXFR/IXFR and integrate authenticated query/response handling.

### Zone storage and authoritative processing

- [DONE] T098: Implement canonical reversed-label zone index, RRset key index, exact/ancestor/closest-encloser/wildcard/predecessor/successor lookup, and longest-zone match.
- [DONE] T099: Implement atomic zone add/replace/delete transactions, invariant checks, snapshot reads, SOA serial updates, and journal entry creation.
- [DONE] T100: Implement authoritative request validation, zone selection, opcode/question policy, class/type handling, and base response/header construction.
- [DONE] T101: Implement exact positive answers, CNAME chains, DNAME plus synthesized CNAME, wildcard synthesis, and duplicate suppression.
- [DONE] T102: Implement delegation referrals, DS-at-cut behavior, in-bailiwick A/AAAA glue, and optional additional records for NS/MX/SRV/SVCB targets.
- [DONE] T103: Implement NODATA, NXDOMAIN, empty-nonterminal, SOA authority, wildcard denial, and correct AA/RCODE semantics.
- [DONE] T104: Implement authoritative DNSSEC record serving, DO/CD behavior, precomputed signatures/proofs, and AD policy.
- [DONE] T105: Implement authoritative EDNS version/option processing, COOKIE behavior, EDE, payload negotiation, and amplification-aware response limits.
- [DONE] T106: Implement minimal ANY responses, unknown QTYPE behavior, configurable minimal/additional policies, REFUSED/NOTIMP/SERVFAIL mapping, and RRset-atomic truncation.
- [DONE] T107: Implement data-driven network authorization and rate-limit decisions for query, update, notify, and transfer operations.

### Update, notify, transfers, and server loops

- [DONE] T108: Implement UPDATE message construction/parsing and all name/RRset prerequisite forms with zone/class/TTL/type validation.
- [DONE] T109: Implement atomic add, delete-RR, delete-RRset, and delete-name update application with prerequisite evaluation and RFC 2136 RCODE mapping.
- [DONE] T110: Integrate TSIG authentication, authorization policy, journal creation, and SOA serial policy into dynamic update.
- [DONE] T111: Implement NOTIFY construction/parsing, primary/secondary authorization, SOA serial comparison, acknowledgement, and transfer-trigger result.
- [DONE] T112: Implement AXFR client receive state with SOA boundaries, TCP-only enforcement, multi-message validation, TSIG chaining, and atomic zone result.
- [DONE] T113: Implement AXFR server streaming with message-size partitioning, SOA boundaries, authorization, and TSIG chaining.
- [DONE] T114: Implement IXFR client state for up-to-date, delta delete/add sequences, serial validation, AXFR fallback, TSIG, and atomic apply.
- [DONE] T115: Implement IXFR server journal selection, delta streaming, AXFR fallback when coverage is unavailable, authorization, and TSIG.
- [DONE] T116: Implement UDP authoritative service loop with bounded receive buffers, exception-less message decode, response sizing, cancellation, and stop state.
- [DONE] T117: Implement TCP authoritative service loop with accept lifecycle, framed partial I/O, pipelined sequential queries, idle/lifetime limits, and graceful stop state.
- [DONE] T118: Integrate QUERY, NOTIFY, UPDATE, AXFR, and IXFR dispatch into `DnsServer` with per-operation authorization and stable failure mapping.

### Complete public API test coverage

- [DONE] T119: Create tests for every public enum value, static protocol field, data-holder field/default, exception constructor, and interface/class construction contract exposed under `src/Api`.
- [DONE] T120: Create exhaustive wire primitive tests for every integer width, boundary, truncated input, overflow, exact consumption, and span/buffer sizing path.
- [DONE] T121: Create exhaustive DNS name tests for root/relative/absolute names, escaping, IDNA, comparison, suffix operations, reverse maps, length limits, and invalid labels.
- [DONE] T122: Create adversarial name-compression tests for all legal pointer forms plus loops, forward/out-of-range pointers, excessive hops, expanded overflow, and deterministic emission.
- [DONE] T123: Create header, question, section, unknown-code, reserved-flag, count, clone, normalized-comparison, size-estimation, and RCODE composition tests.
- [DONE] T124: Create wire and presentation round-trip tests for every typed/shared RDATA schema and every assigned `DnsRecordType` classification.
- [DONE] T125: Create opaque RDATA tests for unknown, private, experimental, obsolete, future, and forced-opaque decoding with byte-exact round trips.
- [DONE] T126: Create type-bitmap and SVCB/HTTPS tests for canonical order, duplicate/unknown parameters, mandatory keys, malformed lengths, and every known parameter schema.
- [DONE] T127: Create message codec tests for all sections, mixed compression, RDLENGTH/count failures, trailing data, limits, TSIG/OPT placement, deterministic encode, and strict versus `Try` behavior.
- [DONE] T128: Create truncation and TCP frame tests for RRset atomicity, tiny budgets, classic/EDNS sizes, partial reads, multiple frames, zero/oversize frames, and round trips.
- [DONE] T129: Create text codec tests for every name/TTL/type/class/RDATA form, unknown RFC 3597 form, canonical output, escapes, and parse-format-parse equivalence.
- [DONE] T130: Create zone parser tests for comments, quotes, parentheses, omitted owners, relative origins, every directive, include cycles/depth, generate bounds, source diagnostics, and deterministic serialization.
- [DONE] T131: Create zone validation tests for SOA/NS requirements, serial arithmetic, CNAME/DNAME exclusivity, delegation/glue, TTL consistency, out-of-zone names, DNSSEC, and ZONEMD states.
- [DONE] T132: Create EDNS tests for OPT placement/version/RCODE/flags/payload and every typed, opaque, duplicate, malformed, request, and response option path.
- [DONE] T133: Create deterministic UDP tests on IPv4/IPv6 loopback for endpoint filtering, ID/question matching, unrelated datagrams, timeout/cancellation, receive limits, and malformed responses.
- [DONE] T134: Create deterministic TCP tests for framing, partial I/O, connection reuse/close, sequential queries, idle/lifetime limits, timeout/cancellation, and server pipelining.
- [DONE] T135: Create query-client tests for UDP-to-TCP fallback, direct TCP, multi-server selection, rotation, retries, total lifetime, health, and EDNS downgrade.
- [DONE] T136: Create loopback DoT tests with test certificates for SNI/authentication, invalid certificates, framing, timeouts, and reuse.
- [DONE] T137: Create loopback DoH tests for GET/POST, media type, bootstrap, redirects, HTTP errors, body limits, malformed DNS, and cancellation.
- [DONE] T138: Create conditional loopback DoQ tests for framing, stream lifecycle, protocol errors, timeout, cancellation, and unsupported-platform results.
- [DONE] T139: Create cache tests for canonical keys, positive/negative/failure data, TTL aging, zero TTL, credibility, capacity, flush/subtree invalidation, stale, and prefetch metadata using a fake clock.
- [DONE] T140: Create resolver-configuration and search tests for valid/invalid `resolv.conf`, ndots, absolute names, candidate order, duplicates, retries, and terminal statuses.
- [DONE] T141: Create stub resolver tests for cache interaction, aliases/DNAME synthesis, loop limits, negative answers, DNSSEC flags, EDNS/TCP behavior, and all public result statuses.
- [DONE] T142: Create high-level address, reverse, MX, TXT, SRV selection, SVCB/HTTPS, special-use policy, TTL, and security-metadata tests with deterministic entropy.
- [DONE] T143: Create iterative resolver scripted-transport tests for root start, closest delegation, referrals, in/out-of-bailiwick glue, missing NS addresses, lame/cyclic authorities, retries, and budget exhaustion.
- [DONE] T144: Create iterative response tests for CNAME, DNAME, wildcard, empty nonterminal, NXDOMAIN, NODATA, delegation DS, QNAME minimization fallback, 0x20 validation, EDNS downgrade, and TCP fallback.
- [DONE] T145: Create recursive cache tests for credibility/replacement, glue isolation, delegation cuts, negative cuts, failure bounds, stale answers, prefetch, and per-server capability/health state.
- [DONE] T146: Create DNSSEC canonicalization, key-tag, DS digest, algorithm-policy, timestamp, wildcard-label, and signed-data fixture tests.
- [DONE] T147: Create DNSSEC algorithm tests using published RSA, ECDSA, Ed25519, unsupported, forbidden, bad-key, bad-signature, and expired/not-yet-valid vectors.
- [DONE] T148: Create chain-of-trust tests from root anchors through secure/insecure delegations, key rollovers, missing algorithms, bogus DS/DNSKEY/RRSIG data, CD/DO/AD behavior, and cache reuse.
- [DONE] T149: Create exhaustive NSEC/NSEC3 denial tests for NXDOMAIN, NODATA, wildcard, closest/next-closer, opt-out, bitmap, salt/iteration limits, malformed proofs, and aggressive caching.
- [DONE] T150: Create signing, NSEC/NSEC3 chain generation, and ZONEMD calculation/verification tests against independent published or generated fixtures.
- [DONE] T151: Create TSIG tests for each supported HMAC, request/response and transfer chaining, time/fudge, truncation, wrong key/MAC/ID, BADKEY/BADSIG/BADTIME, placement, and constant-time result behavior.
- [DONE] T152: Create zone-store tests for exact/ancestor/wildcard/order lookup, longest-zone match, atomic transactions, snapshots, serial updates, journals, concurrent reads, and all public store operations.
- [DONE] T153: Create authoritative positive-answer tests for exact, CNAME, DNAME, wildcard, additional synthesis, duplicate suppression, classes, unknown types, and header/flag semantics.
- [DONE] T154: Create authoritative referral/negative tests for delegation, DS at cut, glue/bailiwick, NODATA, NXDOMAIN, empty nonterminal, SOA authority, wildcard denial, DNSSEC proofs, and AA semantics.
- [DONE] T155: Create authoritative EDNS/policy tests for BADVERS, cookies, EDE, payloads, amplification limits, ANY minimization, REFUSED/NOTIMP/SERVFAIL, rate limits, and truncation.
- [DONE] T156: Create dynamic-update tests for every prerequisite and mutation form, invalid zone/class/TTL/type cases, atomic rollback behavior, serial/journal updates, RCODEs, TSIG, and authorization.
- [DONE] T157: Create NOTIFY tests for construction, acknowledgement, authorization, serial comparison, target retry, ignored notifications, and transfer-trigger results.
- [DONE] T158: Create AXFR tests for empty/minimal/large zones, SOA boundaries, message partitioning, partial/malformed transfers, authorization, TSIG chains, cancellation, and atomic replacement.
- [DONE] T159: Create IXFR tests for up-to-date, one/multiple deltas, wraparound serials, malformed sequences, missing journal fallback, authorization, TSIG, cancellation, and atomic apply.
- [DONE] T160: Create UDP/TCP authoritative server integration tests for malformed packets, all opcode dispatches, persistent/pipelined TCP, limits, cancellation, stop, and concurrent clients.
- [DONE] T161: Add byte-exact RFC and independent-library fixture tests generated with dnspython, miekg/dns, dnsjava, ldns, or c-ares for names, records, messages, EDNS, transfers, TSIG, and DNSSEC.
- [DONE] T162: Add randomized/property-based decode-encode-decode and malformed-input tests for names, every RDATA family, EDNS options, messages, zone text, and TCP frames with bounded runtime.
- [DONE] T163: Audit the public `src/Api` inventory against tests and add behavioral coverage for every public constructor, interface method, implementation branch/status, enum/code mapping, and data result shape not already exercised.
