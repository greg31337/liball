# SDP requirements trace

This trace closes T167 in `todo.md`. It maps each behavioral requirement group and every public contract operation to test evidence. The audit inventory contains 170 public contract methods: 142 methods implemented by the library and 28 consumer-implemented visitor callbacks. All 142 library operations occur in behavioral tests. Visitor callbacks are listed separately because the library neither invokes nor implements them.

Evidence labels used below are:

- **P** — positive or representative behavior.
- **N** — malformed input, invalid state, expected failure, or throwing programmer-error behavior.
- **B** — protocol, numeric, allocation, collection, or configured-limit boundary.
- **R/I** — parse/write round trip, encode/decode round trip, idempotence, non-mutation, or multi-operation interaction. `N/A` means a round trip is not meaningful for that contract.

## Evidence suites

| Label | Test evidence |
|---|---|
| PARSE | [`tests/Api/SdpParserTests.cs`](tests/Api/SdpParserTests.cs), [`tests/Unit/Parsing`](tests/Unit/Parsing), [`tests/Conformance/Rfc8866`](tests/Conformance/Rfc8866) |
| MALFORMED | [`tests/Unit/Parsing/SdpMalformedInputCorpusTests.cs`](tests/Unit/Parsing/SdpMalformedInputCorpusTests.cs), [`tests/Unit/Parsing/SdpAdversarialLimitTests.cs`](tests/Unit/Parsing/SdpAdversarialLimitTests.cs) |
| WRITE | [`tests/Api/SdpWriterTests.cs`](tests/Api/SdpWriterTests.cs), [`tests/Api/SdpWriterUtf8Tests.cs`](tests/Api/SdpWriterUtf8Tests.cs), [`tests/Api/SdpWriterValidationTests.cs`](tests/Api/SdpWriterValidationTests.cs), [`tests/Unit/Writing`](tests/Unit/Writing) |
| ROUNDTRIP | [`tests/Support/SdpFixtureRoundTripAssert.cs`](tests/Support/SdpFixtureRoundTripAssert.cs), [`tests/Support/SdpFixtureLosslessAssert.cs`](tests/Support/SdpFixtureLosslessAssert.cs), all suites under [`tests/Interop`](tests/Interop) |
| ATTR | [`tests/Api/SdpAttributeCodecTests.cs`](tests/Api/SdpAttributeCodecTests.cs), all suites under [`tests/Unit/Attributes`](tests/Unit/Attributes) |
| VALIDATE | [`tests/Api/SdpValidatorTests.cs`](tests/Api/SdpValidatorTests.cs), all suites under [`tests/Unit/Validation`](tests/Unit/Validation) |
| REGISTRY | [`tests/Api/SdpRegistrySnapshotTests.cs`](tests/Api/SdpRegistrySnapshotTests.cs), [`tests/Api/SdpRegistryEnumConversionTests.cs`](tests/Api/SdpRegistryEnumConversionTests.cs), [`tests/Unit/Registry`](tests/Unit/Registry) |
| EDIT | [`tests/Api/SdpEditorTests.cs`](tests/Api/SdpEditorTests.cs), [`tests/Api/SdpPublicApiBranchCoverageTests.cs`](tests/Api/SdpPublicApiBranchCoverageTests.cs) |
| CODEC | [`tests/Unit/Operations/SdpCodecServiceTests.cs`](tests/Unit/Operations/SdpCodecServiceTests.cs), [`tests/Conformance/Rtp`](tests/Conformance/Rtp) |
| ICE | [`tests/Unit/Operations/SdpIceServiceTests.cs`](tests/Unit/Operations/SdpIceServiceTests.cs), [`tests/Unit/Operations/SdpTrickleIceFragmentTests.cs`](tests/Unit/Operations/SdpTrickleIceFragmentTests.cs), [`tests/Conformance/Rfc8839`](tests/Conformance/Rfc8839) |
| DTLS | [`tests/Unit/Operations/SdpDtlsServiceTests.cs`](tests/Unit/Operations/SdpDtlsServiceTests.cs), [`tests/Conformance/Rfc8122`](tests/Conformance/Rfc8122) |
| EXTMAP | [`tests/Unit/Operations/SdpExtMapServiceTests.cs`](tests/Unit/Operations/SdpExtMapServiceTests.cs), [`tests/Conformance/Rtp/SdpExtMapRfc8285ConformanceTests.cs`](tests/Conformance/Rtp/SdpExtMapRfc8285ConformanceTests.cs) |
| GROUP | [`tests/Unit/Operations/SdpGroupServiceTests.cs`](tests/Unit/Operations/SdpGroupServiceTests.cs), [`tests/Unit/Operations/SdpBundleServiceTests.cs`](tests/Unit/Operations/SdpBundleServiceTests.cs) |
| STREAM | [`tests/Unit/Operations/SdpStreamServiceTests.cs`](tests/Unit/Operations/SdpStreamServiceTests.cs), [`tests/Unit/Operations/SdpStreamEditingTests.cs`](tests/Unit/Operations/SdpStreamEditingTests.cs), [`tests/Conformance/Rtp/SdpStreamRfcConformanceTests.cs`](tests/Conformance/Rtp/SdpStreamRfcConformanceTests.cs) |
| DATA | [`tests/Unit/Operations/SdpDataChannelServiceTests.cs`](tests/Unit/Operations/SdpDataChannelServiceTests.cs), [`tests/Unit/Operations/SdpBfcpProjectionTests.cs`](tests/Unit/Operations/SdpBfcpProjectionTests.cs), [`tests/Conformance/DataChannel`](tests/Conformance/DataChannel) |
| QUERY | [`tests/Unit/Operations/SdpQueryServiceTests.cs`](tests/Unit/Operations/SdpQueryServiceTests.cs) |
| TIMING | [`tests/Unit/Operations/SdpTimingServiceTests.cs`](tests/Unit/Operations/SdpTimingServiceTests.cs), [`tests/Unit/Operations/SdpTimingOccurrenceTests.cs`](tests/Unit/Operations/SdpTimingOccurrenceTests.cs) |
| COMPARE | [`tests/Api/SdpComparerTests.cs`](tests/Api/SdpComparerTests.cs) |
| FACTORY | [`tests/Api/SdpSessionFactoryTests.cs`](tests/Api/SdpSessionFactoryTests.cs) |
| NEGOTIATE | [`tests/Api/SdpOfferAnswerServiceTests.cs`](tests/Api/SdpOfferAnswerServiceTests.cs), [`tests/Api/SdpSubsequentOfferTests.cs`](tests/Api/SdpSubsequentOfferTests.cs), [`tests/Conformance/Rfc3264`](tests/Conformance/Rfc3264), [`tests/Conformance/WebRtc`](tests/Conformance/WebRtc) |
| API | [`tests/Api/SdpImplementationConstructorTests.cs`](tests/Api/SdpImplementationConstructorTests.cs), [`tests/Api/SdpOperationDataTests.cs`](tests/Api/SdpOperationDataTests.cs), [`tests/Api/SdpConcurrencyIsolationTests.cs`](tests/Api/SdpConcurrencyIsolationTests.cs) |

## Global requirements

| ID | Requirement | Evidence |
|---|---|---|
| G1 | Represent and round-trip every RFC 8866 construct plus unknown future fields and attributes. | P/N/B: PARSE, ATTR; R/I: ROUNDTRIP |
| G2 | Typed standardized projections retain access to their generic source attributes. | P/N/B: ATTR and the protocol operation suites; R/I: ROUNDTRIP |
| G3 | Public API organization and restricted source syntax. | Source-organization policy, not runtime behavior; no syntax-policy tests by design. |
| G4 | Public data uses fields, implementations expose their contracts, distinct method names, and result objects. | P: API and [`tests/Api/SdpPublicDataCollectionTests.cs`](tests/Api/SdpPublicDataCollectionTests.cs); syntax conformance is N/A. |
| G5 | Signed counts/sizes and unsigned protocol-binary values. | P/B: [`tests/Api/SdpTypedDataTests.cs`](tests/Api/SdpTypedDataTests.cs), MALFORMED, CODEC, ICE, STREAM |
| G6 | Only built-in runtime dependencies; spans are internal parsing aids. | Project/source constraint, not runtime behavior; parser entry-point equivalence is covered by PARSE. |
| G7 | Throw for programmer/configuration errors and return structured diagnostics for malformed remote input. | P/N/B: PARSE, WRITE, VALIDATE, MALFORMED, API |
| G8 | Bound characters, bytes, lines, media, attributes, tokens, candidates, and diagnostics before growth. | P/N/B: MALFORMED, ICE, WRITE; R/I: API concurrency/isolation |

## Feature requirements

Each row corresponds to one behavioral bullet in sections 1–16 of `todo.md`.

### 1. Text, parsing, and round-trip fidelity

| ID | Feature | Evidence |
|---|---|---|
| 1.1 | String, character-span, and UTF-8 input; CRLF plus configured LF/CR interoperability. | P/N/B: PARSE; R/I: ROUNDTRIP |
| 1.2 | Field discriminator, equals sign, character classes, UTF-8, NUL, and strict ordering. | P/N/B: PARSE, MALFORMED; R/I: ROUNDTRIP |
| 1.3 | Strict, interoperable, and lossless modes with warnings and source preservation. | P/N/B: PARSE, [`tests/Unit/Parsing/SdpLosslessRecoveryTests.cs`](tests/Unit/Parsing/SdpLosslessRecoveryTests.cs); R/I: ROUNDTRIP |
| 1.4 | Input immutability, no shared global state, deterministic bounded recovery. | P/N/B: MALFORMED; R/I: API concurrency/isolation |
| 1.5 | Opaque unknown one-letter fields and vendor attributes. | P/N/B: PARSE, ATTR; R/I: ROUNDTRIP and NEGOTIATE unknown-extension tests |
| 1.6 | Permitted duplicate preservation and forbidden-duplicate diagnostics. | P/N/B: PARSE, MALFORMED; R/I: ROUNDTRIP |
| 1.7 | Property, valued, and explicitly empty attributes remain distinct. | P/N/B: PARSE, ATTR; R/I: ROUNDTRIP |
| 1.8 | Canonical ordering/formatting and lossless re-emission without partial lines. | P/N/B: WRITE; R/I: ROUNDTRIP |
| 1.9 | Configurable obsolete `k=`, `cat`, and `keywds` emission and validation. | P/N/B: WRITE, VALIDATE; R/I: ROUNDTRIP |
| 1.10 | Safe limits and terminal diagnostic-capacity behavior. | P/N/B: MALFORMED, WRITE; R/I: API concurrency/isolation |

### 2. RFC 8866 session description

| ID | Feature | Evidence |
|---|---|---|
| 2.1 | Required zero protocol version. | P/N/B: PARSE, VALIDATE; R/I: ROUNDTRIP |
| 2.2 | Lossless origin identifiers/version and address tokens. | P/N/B: PARSE; R/I: ROUNDTRIP |
| 2.3 | Session name, information, URI, repeated email/phone, and international text. | P/N/B: PARSE, RFC 8866 conformance; R/I: ROUNDTRIP |
| 2.4 | Session connection, bandwidth, timing, zones, key, attributes, and media. | P/N/B: PARSE, RFC 8866 conformance; R/I: ROUNDTRIP |
| 2.5 | Required, optional, cardinality, order, timing, and scope rules. | P/N/B: PARSE, VALIDATE; R/I: ROUNDTRIP |
| 2.6 | Deep clone and structural/semantic comparison. | P/N/B: EDIT, COMPARE; R/I: clone isolation and comparison idempotence |

### 3. Media descriptions and connection data

| ID | Feature | Evidence |
|---|---|---|
| 3.1 | Media token, port/count, protocol, formats, rejection, and extension tokens. | P/N/B: PARSE, VALIDATE; R/I: ROUNDTRIP |
| 3.2 | Media title, repeated connections/bandwidth, key, and ordered attributes. | P/N/B: PARSE, WRITE; R/I: ROUNDTRIP |
| 3.3 | IPv4/IPv6 unicast/multicast, FQDN, and extension network/address types. | P/N/B: RFC 8866 conformance, browser/SIP/RTSP interop; R/I: ROUNDTRIP |
| 3.4 | Address, TTL, and count bounds with raw extension preservation. | P/N/B: [`tests/Unit/Parsing/SdpConnectionLineParserTests.cs`](tests/Unit/Parsing/SdpConnectionLineParserTests.cs), VALIDATE; R/I: ROUNDTRIP |
| 3.5 | Scoped inheritance and media override for effective values. | P/N/B: QUERY, ICE, DTLS, EXTMAP; R/I: COMPARE interaction tests |
| 3.6 | Activation/rejection, port ranges, MID lookup, and transport endpoints. | P/N/B: EDIT, QUERY, GROUP; R/I: NEGOTIATE |

### 4. Timing, recurrence, and zones

| ID | Feature | Evidence |
|---|---|---|
| 4.1 | Lossless NTP decimal text and representable .NET conversion. | P/N/B/R: TIMING |
| 4.2 | Permanent, bounded, multiple, and repeated timing periods. | P/N/B: TIMING, RFC 8866 conformance; R/I: ROUNDTRIP |
| 4.3 | Typed-time units, positivity, and offsets. | P/N/B/R: TIMING, [`tests/Unit/Text/SdpTypedTimeParserTests.cs`](tests/Unit/Text/SdpTypedTimeParserTests.cs) |
| 4.4 | Signed multi-pair time-zone adjustments. | P/N/B: PARSE, TIMING; R/I: ROUNDTRIP |
| 4.5 | Permanent/activity queries, bounded occurrences, and normalization. | P/N/B/R: TIMING |

### 5. Generic and core attributes

| ID | Feature | Evidence |
|---|---|---|
| 5.1 | Ordered attribute add/insert/replace/remove/find with specified comparison rules. | P/N/B: EDIT, QUERY; R/I: editor identity/order tests |
| 5.2 | Core direction, orientation, type, language, frame, quality, tool, and obsolete attributes. | P/N/B: ATTR, VALIDATE; R/I: ROUNDTRIP |
| 5.3 | RTSP control and generic registered/unknown attribute support. | P/N/B: ATTR, RTSP interop; R/I: ROUNDTRIP |
| 5.4 | Checked-in IANA snapshot access for all advertised registries. | P/N/B: REGISTRY; R/I: N/A (read-only lookup) |
| 5.5 | Scope and multiplexing metadata and validation. | P/N/B: REGISTRY, VALIDATE, GROUP; R/I: NEGOTIATE |

### 6. RTP formats and codecs

| ID | Feature | Evidence |
|---|---|---|
| 6.1 | Ordered payload/format tokens, RTP range, wildcard selectors, and non-RTP formats. | P/N/B: CODEC, RTP conformance; R/I: ROUNDTRIP |
| 6.2 | `rtpmap` payload, encoding, clock, and parameters/channels. | P/N/B: ATTR, CODEC; R/I: ROUNDTRIP |
| 6.3 | Ordered/raw `fmtp` parameters, flags, duplicates, case, and whitespace. | P/N/B: ATTR, CODEC; R/I: ROUNDTRIP |
| 6.4 | Packet time, frame rate, quality, and opaque codec parameter retention. | P/N/B: ATTR, CODEC; R/I: ROUNDTRIP |
| 6.5 | RFC 3551 static mappings and dynamic mapping requirements. | P/N/B: CODEC, VALIDATE; R/I: ROUNDTRIP |
| 6.6 | Codec projection and strict/compatible matching dimensions. | P/N/B: CODEC; R/I: NEGOTIATE |
| 6.7 | Preference, intersection, allocation/remap, repair associations, telephone events, and wildcard feedback. | P/N/B: CODEC; R/I: NEGOTIATE |
| 6.8 | Typed Opus, VP8/9, H.264/265, RTX, RED/FEC, comfort-noise, and telephone-event validation with generic fallback. | P/N/B: CODEC, RTP conformance; R/I: ROUNDTRIP |
| 6.9 | Signaling compatibility only; no media implementation claims. | Scope constraint; N/A runtime behavior. |

### 7. RTP/RTCP attributes and sources

| ID | Feature | Evidence |
|---|---|---|
| 7.1 | `rtcp` port and optional address triple. | P/N/B: ATTR, VALIDATE; R/I: ROUNDTRIP |
| 7.2 | RTCP mux/rsize properties and offer/answer validation. | P/N/B: ATTR, VALIDATE; R/I: NEGOTIATE |
| 7.3 | Typed and unknown `rtcp-fb` forms. | P/N/B: ATTR, CODEC; R/I: ROUNDTRIP |
| 7.4 | Typed and unknown `rtcp-xr` blocks. | P/N/B: ATTR, RTP conformance; R/I: ROUNDTRIP |
| 7.5 | Unsigned SSRC and arbitrary ordered source attributes. | P/N/B: ATTR, STREAM, VALIDATE; R/I: ROUNDTRIP |
| 7.6 | Known and extension source-group semantics with ordered SSRCs. | P/N/B: ATTR, STREAM; R/I: ROUNDTRIP |
| 7.7 | MSID projections and cross-media consistency. | P/N/B: STREAM, VALIDATE; R/I: NEGOTIATE |
| 7.8 | Content, label, and source-filter projections. | P/N/B: ATTR, STREAM, QUERY; R/I: ROUNDTRIP |

### 8. ICE attributes and helpers

| ID | Feature | Evidence |
|---|---|---|
| 8.1 | Scoped ICE credentials/options/lite with validation. | P/N/B: ICE, VALIDATE; R/I: ROUNDTRIP |
| 8.2 | Complete candidate grammar and ordered extensions. | P/N/B: ATTR, ICE; R/I: ROUNDTRIP |
| 8.3 | Candidate type, transport, endpoint, TCP, and range rules. | P/N/B: ICE, RFC 8839 conformance; R/I: ROUNDTRIP |
| 8.4 | Remote candidates, completion/mismatch, trickle option, and fragment I/O. | P/N/B/R: ICE |
| 8.5 | Default destination consistency. | P/N/B: VALIDATE, RFC 8839 conformance; R/I: NEGOTIATE |
| 8.6 | Candidate mutation/sort/completion, effective credentials, remote pairs, and restart detection. | P/N/B/R: ICE |
| 8.7 | BUNDLE-aware ICE ownership. | P/N/B: ICE, GROUP; R/I: NEGOTIATE |
| 8.8 | Signaling-only helpers with no network state machine. | Scope constraint; N/A runtime behavior. |

### 9. TLS, DTLS, and security attributes

| ID | Feature | Evidence |
|---|---|---|
| 9.1 | Fingerprint algorithms/octet/digest validation with extension algorithms. | P/N/B: ATTR, DTLS, RFC 8122 conformance; R/I: ROUNDTRIP |
| 9.2 | Setup roles and offer/answer selection. | P/N/B: DTLS, VALIDATE; R/I: NEGOTIATE |
| 9.3 | Connection new/existing, reuse, and transport applicability. | P/N/B: ATTR, VALIDATE, RFC 8122 conformance; R/I: NEGOTIATE |
| 9.4 | TLS-ID parsing, uniqueness, continuity, and transport association. | P/N/B: DTLS, VALIDATE; R/I: NEGOTIATE |
| 9.5 | Scoped/BUNDLE effective DTLS resolution. | P/N/B: DTLS, GROUP; R/I: NEGOTIATE |
| 9.6 | Multiple fingerprints and deterministic preference. | P/N/B: DTLS; R/I: fingerprint replacement tests |
| 9.7 | Typed SDES crypto and diagnostic redaction. | P/N/B: ATTR, MALFORMED, VALIDATE; R/I: ROUNDTRIP |

### 10. RTP header extensions and extmaps

| ID | Feature | Evidence |
|---|---|---|
| 10.1 | Complete extmap grammar including direction, encryption, URI, and extension text. | P/N/B: ATTR, EXTMAP; R/I: ROUNDTRIP |
| 10.2 | Identifier, uniqueness, direction, scope, and mixed-mode validation. | P/N/B: EXTMAP, VALIDATE, RFC 8285 conformance; R/I: NEGOTIATE |
| 10.3 | Known header-extension URI constants and registry fallback. | P/N/B: REGISTRY, [`tests/Api/SdpStaticDataTests.cs`](tests/Api/SdpStaticDataTests.cs); R/I: ROUNDTRIP |
| 10.4 | Resolve/query/intersect/direction-select/allocate/remap operations. | P/N/B/R: EXTMAP |
| 10.5 | BUNDLE conflict detection and consistent remap. | P/N/B: EXTMAP, GROUP, VALIDATE; R/I: NEGOTIATE |

### 11. Grouping and BUNDLE

| ID | Feature | Evidence |
|---|---|---|
| 11.1 | Ordered group semantics/tags and MID. | P/N/B: GROUP, ATTR; R/I: ROUNDTRIP |
| 11.2 | Known and extension group semantics. | P/N/B: GROUP, REGISTRY; R/I: ROUNDTRIP |
| 11.3 | MID/group reference, membership, empty/rejected/multiple/semantic validation. | P/N/B: GROUP, VALIDATE; R/I: NEGOTIATE |
| 11.4 | Bundle-only and initial/subsequent offer-answer behavior. | P/N/B/R: GROUP, NEGOTIATE |
| 11.5 | Effective BUNDLE transport categories, bandwidth, ICE, DTLS, extmap, and mux. | P/N/B: GROUP, QUERY; R/I: NEGOTIATE |
| 11.6 | Group/BUNDLE create, update, dissolve, and query operations. | P/N/B/R: GROUP |

### 12. RID, simulcast, and image constraints

| ID | Feature | Evidence |
|---|---|---|
| 12.1 | RID direction, restrictions, payloads, numeric bounds, dependencies, and extensions. | P/N/B: ATTR, STREAM, VALIDATE; R/I: ROUNDTRIP |
| 12.2 | Simulcast lists, alternatives, pause, references, uniqueness, and subset rules. | P/N/B: ATTR, STREAM, VALIDATE; R/I: NEGOTIATE |
| 12.3 | RID/simulcast extmap association rules. | P/N/B: VALIDATE, RTP conformance; R/I: NEGOTIATE |
| 12.4 | Imageattr selectors, sets, ranges, alternatives, and raw fallback. | P/N/B: ATTR, STREAM; R/I: ROUNDTRIP |
| 12.5 | Stream projection plus safe RID remove/rename/filter. | P/N/B/R: STREAM |

### 13. SCTP data channels and BFCP

| ID | Feature | Evidence |
|---|---|---|
| 13.1 | Current SCTP transports, port, and message-size defaults/unlimited values. | P/N/B: DATA, VALIDATE; R/I: ROUNDTRIP |
| 13.2 | Legacy SCTP map preservation/interoperability. | P/N/B: DATA, interop; R/I: ROUNDTRIP |
| 13.3 | DCMAP/DCSA parameters, duplicates, IDs, and unknown retention. | P/N/B: ATTR, DATA, VALIDATE; R/I: ROUNDTRIP |
| 13.4 | Data-channel projection/capability intersection and offer-answer validation. | P/N/B: DATA; R/I: NEGOTIATE |
| 13.5 | Current BFCP transports and attributes per RFC 8856. | P/N/B: DATA, BFCP conformance; R/I: ROUNDTRIP |

### 14. Clock and multicast extensions

| ID | Feature | Evidence |
|---|---|---|
| 14.1 | Source-filter modes, addresses, sources, and validation. | P/N/B: ATTR, QUERY, VALIDATE; R/I: ROUNDTRIP |
| 14.2 | Timestamp reference and media clock forms/rates. | P/N/B: ATTR, QUERY; R/I: ROUNDTRIP |
| 14.3 | Clock scope/inheritance checks. | P/N/B: QUERY, VALIDATE; R/I: ROUNDTRIP |
| 14.4 | Multicast counts, filters, and address consistency. | P/N/B: PARSE, QUERY, VALIDATE; R/I: ROUNDTRIP |

### 15. Validation profiles

| ID | Feature | Evidence |
|---|---|---|
| 15.1 | Syntax, RFC 8866, ICE, TLS/DTLS, RTP, WebRTC, BUNDLE, data-channel, BFCP, and combined profiles. | P/N/B: VALIDATE and all protocol conformance suites; R/I: validator non-mutation tests |
| 15.2 | Required/cardinality/order, token/Unicode/numeric/port/payload/SSRC/TTL/duration/ID bounds. | P/N/B: PARSE, VALIDATE, MALFORMED; R/I: ROUNDTRIP for accepted boundaries |
| 15.3 | Cross-field connection, transport/format, mapping/reference, direction, and rejection rules. | P/N/B: VALIDATE; R/I: NEGOTIATE |
| 15.4 | Cross-media group/payload/extmap/source/MSID/ICE/DTLS/mux/SCTP rules. | P/N/B: VALIDATE; R/I: NEGOTIATE |
| 15.5 | Severity promotion/suppression without hiding fatal parse failures. | P/N/B: [`tests/Unit/Validation/SdpValidationSeverityPolicyTests.cs`](tests/Unit/Validation/SdpValidationSeverityPolicyTests.cs), MALFORMED; R/I: N/A |
| 15.6 | Validation non-mutation and explicit normalization. | P/N/B: VALIDATE, EDIT; R/I: API concurrency/isolation |

### 16. Generation, editing, comparison, and negotiation

| ID | Feature | Evidence |
|---|---|---|
| 16.1 | Explicit minimal session/media construction. | P/N/B: FACTORY; R/I: WRITE and ROUNDTRIP |
| 16.2 | Canonical/lossless text and UTF-8 writers, buffers, limits, and result failures. | P/N/B: WRITE; R/I: ROUNDTRIP |
| 16.3 | Clone and complete ordered media/attribute mutation/normalization operations. | P/N/B/R: EDIT |
| 16.4 | Effective-value queries across all advertised protocol projections. | P/N/B: QUERY and protocol operation suites; R/I: COMPARE/NEGOTIATE interactions |
| 16.5 | Structural/semantic path-addressed comparison and ignore options. | P/N/B/R: COMPARE |
| 16.6 | Initial offer/answer construction and all advertised intersections/selections. | P/N/B/R: NEGOTIATE |
| 16.7 | Answer validation and subsequent offers preserving stable signaling identity. | P/N/B/R: NEGOTIATE |
| 16.8 | Negotiation results, effective media, mappings, bundles, diagnostics, and rejection reasons. | P/N/B: [`tests/Api/SdpNegotiationResultConformanceTests.cs`](tests/Api/SdpNegotiationResultConformanceTests.cs), NEGOTIATE; R/I: input immutability tests |

## Public API operation trace

All public implementation constructors are covered positively by API constructor tests. Constructors have no inputs, failure branches, boundaries, or round-trip semantics.

| Contract | Operations | P | N/B | R/I |
|---|---|---|---|---|
| `ISdpParser` | `ParseText`, `ParseCharacters`, `ParseUtf8Bytes` | PARSE | PARSE, MALFORMED | ROUNDTRIP; text/UTF-8 equivalence |
| `ISdpWriter` | `WriteText`, `WriteTextOrThrow`, `WriteUtf8Bytes`, `WriteUtf8BytesOrThrow`, `WriteUtf8Buffer` | WRITE | WRITE limits, invalid models, invalid arguments | ROUNDTRIP; buffer atomicity |
| `ISdpValidator` | `ValidateDescription` | VALIDATE | Every profile and diagnostic limit in VALIDATE | Validation non-mutation; N/A wire round trip |
| `ISdpAttributeCodec` | `DecodeAttribute`, `EncodeAttribute` | ATTR | Unknown/malformed/unsupported/null cases in ATTR | Encode/decode and generic-source integrity |
| `ISdpRegistry` | `IsRegisteredMediaType`, `IsRegisteredTransportProtocol`, `IsRegisteredBandwidthType`, `IsRegisteredNetworkType`, `IsRegisteredAddressType`, `IsRegisteredEncryptionMethod`, `IsRegisteredAttributeName`, `IsRegisteredGroupSemantic`, `IsAssignedStaticPayloadType`, `IsRegisteredRtpEncoding`, `IsRegisteredHeaderExtensionUri`, `GetMediaTypeRegistration`, `GetTransportProtocolRegistration`, `GetBandwidthTypeRegistration`, `GetNetworkTypeRegistration`, `GetAddressTypeRegistration`, `GetEncryptionMethodRegistration`, `GetAttributeRegistration`, `GetGroupSemanticRegistration`, `GetRtpPayloadTypeRegistration`, `GetRtpEncodingRegistration`, `GetHeaderExtensionRegistration` | REGISTRY | Unknown, case, range, and null cases in REGISTRY | N/A; immutable snapshot lookup |
| `ISdpSessionFactory` | `CreateSession`, `CreateMedia` | FACTORY | Missing values, token syntax, port/count, empty formats | Generated model write/parse interaction |
| `ISdpEditor` | `CloneDescription`, `AddSessionAttribute`, `AddMediaAttribute`, `InsertSessionAttribute`, `InsertMediaAttribute`, `ReplaceSessionAttribute`, `ReplaceMediaAttribute`, `RemoveSessionAttributeAt`, `RemoveMediaAttributeAt`, `RemoveSessionAttributesByName`, `RemoveMediaAttributesByName`, `AddMedia`, `InsertMedia`, `MoveMedia`, `RemoveMediaAt`, `RejectMedia`, `ReactivateMedia`, `IncrementOriginVersion`, `NormalizeDescription` | EDIT | Nulls, both index bounds, invalid ports/counts/tokens/state, overflow | Clone isolation; mutation identity/order; normalization idempotence/atomicity |
| `ISdpQueryService` | `FindMediaByIndex`, `FindMediaByMid`, `FindMediaIndexByMid`, `ResolveEffectiveConnection`, `ResolveEffectiveBandwidths`, `ResolveEffectiveDirection`, `ResolveEffectiveLanguages`, `ResolveEffectiveDescriptionLanguages`, `ResolveEffectiveIce`, `ResolveEffectiveDtls`, `ResolveEffectiveExtMaps`, `ResolveEffectiveSourceFilters`, `ResolveEffectiveClockReferences`, `ResolveEffectiveMediaClocks`, `ResolveEffectiveMedia`, `ResolveEffectiveMediaByMid` | QUERY | Missing values, invalid selection, rejected/absent/malformed state | Independent projections; BUNDLE/inheritance interactions |
| `ISdpTimingService` | `ConvertNtpToDateTimeOffset`, `ConvertDateTimeOffsetToNtp`, `NormalizeTypedTime`, `CalculateOccurrences` | TIMING | Invalid/overflow/unrepresentable values, windows, repeats, result limits | NTP conversion round trip; chronological/idempotent normalization |
| `ISdpCodecService` | `ResolveMediaCodecs`, `IsCodecMatch`, `OrderCodecsByPreference`, `IntersectCodecs`, `AllocatePayloadTypes`, `RemapPayloadTypes` | CODEC | Null/invalid modes and graphs, payload bounds/collision/exhaustion | Stable order, negotiation, complete graph remap/atomicity |
| `ISdpIceService` | `ResolveMediaIce`, `ResolveEffectiveIce`, `ResolveIceTransportMediaIndex`, `AddCandidate`, `RemoveCandidate`, `RemoveCandidatesByFilter`, `SortCandidatesByPriority`, `MarkGatheringComplete`, `SetRemoteCandidates`, `IsIceRestart`, `RestartIce`, `ParseTrickleIceFragment`, `WriteTrickleIceFragment` | ICE | Null/index/filter/credential/fragment/model/limit cases | Fragment round trip; idempotence; BUNDLE ownership; restart atomicity |
| `ISdpDtlsService` | `ResolveMediaDtls`, `ResolveEffectiveDtls`, `ResolveDtlsTransportMediaIndex`, `SelectPreferredFingerprint`, `SelectAnswerSetupRole`, `ReplaceFingerprints`, `ReplaceTlsId` | DTLS | Null/index/role/fingerprint/TLS-ID invalid cases | Independent resolution; BUNDLE ownership; replacement atomicity |
| `ISdpExtMapService` | `ResolveMediaExtMaps`, `ResolveMediaExtMapAllowMixed`, `FindUriById`, `FindIdByUri`, `IntersectExtMaps`, `SelectAnswerDirection`, `AllocateExtMapIds`, `RemapBundleExtMapIds`, `RemapExtMapIds` | EXTMAP | Null/index/direction/ID/collision/exhaustion/incomplete graph cases | ID/URI inverse queries; graph and BUNDLE remap atomicity |
| `ISdpGroupService` | `ResolveGroups`, `ResolveMediaId`, `FindMediaIndex`, `AddGroup`, `UpdateGroup`, `RemoveGroup` | GROUP | Null/index/token/tag/semantic/source cases | Resolve/edit/remove interaction; extension preservation |
| `ISdpBundleService` | `ResolveBundleGroups`, `FindBundleGroupForMedia`, `ResolveBundleTaggedMediaIndex`, `ResolveBundleTransport`, `CreateInitialBundleOffer`, `CreateInitialBundleAnswer`, `UpdateSubsequentBundleOffer`, `UpdateSubsequentBundleAnswer` | GROUP | Invalid selection, tag, membership, overlap, disabled/rejected and transport cases | Initial/subsequent offer-answer interactions; atomic mutations |
| `ISdpStreamService` | `ResolveSourceAttributes`, `ResolveSourceGroups`, `ResolveMediaStreamIds`, `ResolveMediaStreamSemantics`, `ResolveRids`, `ResolveSimulcast`, `RenameRid`, `RemoveRid`, `FilterSimulcastLayers`, `ResolveImageAttributes` | STREAM | Null/index/ID/collision/reference/cycle/filter cases | Deep independent projections; reference-preserving atomic edits |
| `ISdpDataChannelService` | `ResolveMediaSctp`, `ResolveDataChannels`, `ResolveMediaBfcp`, `IntersectSctpCapabilities`, `IntersectDataChannelCapabilities` | DATA | Null/index/negative size/different formats/incomplete nested collections | Independent projections; capability intersection; NEGOTIATE |
| `ISdpComparer` | `CompareDescriptions` | COMPARE | Null/invalid options and incomplete/one-sided graphs | Clone equality, symmetry-relevant cases, semantic normalization |
| `ISdpOfferAnswerService` | `CreateInitialOffer`, `CreateSubsequentOffer`, `CreateAnswer` | NEGOTIATE | Null/role/template/offer/capability/range/incompatibility and bounded-diagnostic cases | Determinism, initial-answer/subsequent interaction, input immutability |

### Consumer visitor contracts

The following methods are callbacks implemented by consumers. There is no library method that calls them and therefore no library-owned positive, failure, boundary, or round-trip behavior to test. Their enum/data payloads are covered functionally by the API data, enum conversion, diagnostic/result, and negotiation result suites. Reflection or source-shape tests are intentionally excluded.

| Contract | Consumer-implemented methods | Runtime categories |
|---|---|---|
| `ISdpEnumValueVisitor` | `VisitAddressKind`, `VisitAttributeKind`, `VisitClockSourceKind`, `VisitCodecMatchMode`, `VisitDifferenceKind`, `VisitDirection`, `VisitDtlsSetupRole`, `VisitGroupSemantic`, `VisitIceCandidateType`, `VisitIceTcpType`, `VisitIceTransportProtocol`, `VisitLineEnding`, `VisitMediaKind`, `VisitMuxCategory`, `VisitNegotiationRole`, `VisitParseMode`, `VisitRejectionReason`, `VisitScope`, `VisitValidationProfile`, `VisitValidationSeverity`, `VisitWriteMode` | N/A — callback declarations only |
| `ISdpResultVisitor` | `VisitParseResult`, `VisitWriteResult`, `VisitValidationResult`, `VisitComparisonResult`, `VisitBufferWriteResult`, `VisitNegotiationResult` | N/A — callback declarations only |
| `ISdpSessionDescriptionVisitor` | `VisitSessionDescription` | N/A — callback declaration only |

## Audit conclusion

- Every behavioral bullet in sections 1–16 has positive and negative/boundary evidence and round-trip or interaction evidence where the concept supports it.
- Every one of the 142 library-implemented public contract methods is exercised by the listed behavioral suites.
- The complete coverage run for T166 reports zero uncovered branches in `src/Api/Classes`.
- The 28 visitor methods are consumer implementation points and are explicitly classified as runtime-test N/A.
- Tests for source syntax, repository files, project files, and this trace document are intentionally absent.
