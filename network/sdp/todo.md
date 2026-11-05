# Universal SDP library development plan

## Objective

Build a dependency-free .NET library that can parse, preserve, inspect, validate, edit, compare, negotiate, and generate Session Description Protocol documents. RFC 8866 is the core grammar and semantic baseline; RFC 8122 and RFC 8839 are first-class TLS/DTLS and ICE profiles. The design also covers the RTP, WebRTC, grouping, BUNDLE, header-extension, simulcast, and data-channel specifications needed for practical interoperability.

“Universal” has two parts:

- Every syntactically valid RFC 8866 field, registered token, experimental attribute, and unknown future extension can be represented and round-tripped without requiring a library update.
- Common standardized attributes have typed projections, validation, mutation, and high-level negotiation helpers. Typed support never prevents access to the original generic attribute.

The library is a representation and signaling toolkit. It will not send media, perform ICE connectivity checks, implement DTLS/SRTP/SCTP/RTP, access DNS or the network, or select codecs using platform media implementations.

## Implementation constraints

- Follow `syntax.md` exactly for all `src/**/*.cs` and `tests/**/*.cs` files. Existing C# code is not precedent for exceptions to that policy.
- Put public data classes only in `src/Api/Data`, operation interfaces only in `src/Api/Contracts`, field-only static data classes only in `src/Api/Static`, enums only in `src/Api/Enums`, custom exceptions only in `src/Api/Exceptions`, and interface implementations only in `src/Api/Classes`.
- Keep internal implementation types outside `src/Api`; no public types may exist there.
- Model public state with documented fields, not properties. Data classes must expose at least one public instance field, contain no public methods, and implement no interfaces.
- Give each public implementation class at most one constructor and expose only the instance methods declared by its interface(s).
- Avoid method overloading. Use distinct method names, and return result objects instead of `out`, `ref`, nullable value types, or tuple results.
- Use ordinary classes only: no records, structs, delegates, generics declared by this project, inheritance hierarchies, extension methods, LINQ, pattern matching, async code, or exception-catching logic.
- Use signed integers for counts and sizes. Reserve unsigned integers for SSRCs, ICE priorities, bit masks, and other explicitly binary/protocol values.
- Use only built-in .NET libraries under `src/`. External test libraries and fixture packages are permitted under `tests/`.
- Use `ReadOnlySpan<char>` and `ReadOnlySpan<byte>` in internal tokenization and numeric parsing where they reduce allocations without leaking lifetime-sensitive state into data objects.
- Offer throwing entry points for programmer/configuration errors and result-based entry points for expected malformed remote input. Diagnostics must carry stable codes, severity, scope, field/attribute name, media index, line number, column, and human-readable detail.
- Apply configurable input limits before allocation: total characters/bytes, line length, line count, media count, attributes per scope, token length, candidate count, and diagnostic count.
- Do not add external source dependencies, performance/benchmark work, release work, or end-user documentation work to this plan.

## Research basis and adopted lessons

The downloaded sources are indexed in `specs/README.md`.

- RFC 8866 supplies the ordered `v/o/s/i/u/e/p/c/b/t/r/z/k/a/m` grammar, Unicode rules, session/media scoping, connection and multicast forms, typed times, repetition schedules, and extensible attribute model.
- RFC 8122 supplies connection-oriented TLS transport signaling, `setup`, `connection`, and certificate `fingerprint` rules. DTLS role and continuity behavior is supplemented by RFC 5763 and RFC 8842.
- RFC 8839 supplies ICE credentials, candidates, default destinations, `ice-lite`, `ice-mismatch`, `ice-options`, remote candidates, end-of-candidates, ICE restarts, and offer/answer inheritance rules. RFC 8445 and RFC 8838 supply candidate and trickle-ICE semantics.
- `sdp-transform` and `libsdptransform` demonstrate demand for convenient typed RTP, FMTP, RTCP feedback, ICE, DTLS, extmap, SSRC, RID, simulcast, SCTP, clock, source-filter, image, BUNDLE, and BFCP projections, plus preservation of unknown attributes.
- PJSIP demonstrates the value of separate parse, print, semantic validation, clone, comparison, transport compatibility, media deactivation, attribute mutation, codec ordering, payload matching, and stateful offer/answer operations.
- GStreamer demonstrates complete mutable coverage of every core session/media field, indexed add/insert/replace/remove operations, generic attributes, multiple connections/bandwidth/timing entries, text conversion, and caps-like media projection.
- This design improves on closed grammars by retaining ordered raw fields and attributes, exposing strict and interoperable parsing modes, and separating syntax diagnostics from profile validation.

Comparison-derived coverage map:

| Reference | Capabilities examined | Features carried into this plan |
| --- | --- | --- |
| `sdp-transform` | String parse/write; object grammar for core fields; RTP map/FMTP/RTCP feedback; ICE/DTLS; extmap; SSRC/MSID; groups/BUNDLE; RID/simulcast/imageattr; SCTP; source filters; clocks; BFCP; unknown attributes | Convenient typed projections, canonical writer, LF interoperability, and opaque extension survival, with fuller RFC 8866 timing/connection modeling than its grammar provides |
| `libsdptransform` | C++ equivalent of the JavaScript grammar and JSON-shaped parse/write API | Cross-language fixture compatibility and the same typed-attribute breadth without taking a JSON or external-library dependency |
| PJSIP SDP | Parse/print/validate; clone/compare; generic attribute create/find/add/remove; typed rtpmap/fmtp/rtcp/ssrc; transport comparison; media deactivation; initial/subsequent offer-answer state and codec/payload policies | Separate parsing, generation, semantic validation, editing, comparison, media rejection, transport compatibility, codec preference, symmetric payload, and subsequent-negotiation operations |
| GStreamer SDP | Full session/media field model; parse/text conversion; copy; indexed get/add/insert/replace/remove for repeated fields and attributes; URI/caps-oriented helpers | Complete mutable core-field coverage, deterministic ordering, deep-copy isolation, generic queries/edits, multiple session/media entries, and effective media projections |

## Complete feature catalogue

### 1. Text, parsing, and round-trip fidelity

- Accept .NET strings, character spans, UTF-8 byte spans, CRLF input, and configurable interoperability acceptance of bare LF or CR.
- Enforce the one-character field discriminator, `=`, allowed byte/character classes, UTF-8 validity, NUL rejection, and RFC line ordering in strict mode.
- Provide strict, interoperable, and lossless modes. Strict mode rejects violations; interoperable mode accepts explicitly catalogued real-world deviations with warnings; lossless mode additionally retains original line order, spelling, whitespace, unknown fields, malformed-but-recoverable lines, line endings, and source positions.
- Parse without mutating caller buffers, share no mutable global state, and produce deterministic diagnostics with bounded recovery.
- Preserve unknown one-letter fields and every unknown or vendor `a=` attribute as opaque name/value data.
- Preserve duplicate fields/attributes where the model permits them; diagnose forbidden duplicates without silently discarding input.
- Preserve distinction between property attributes (`a=name`) and value attributes (`a=name:value`), including an explicitly empty value.
- Support canonical generation and lossless re-emission. Canonical output uses RFC field order, CRLF, deterministic attribute ordering policy, invariant numeric formatting, and no terminal partial line.
- Support configurable omission or preservation of obsolete `k=`, `a=cat`, and `a=keywds` data while diagnosing their status under RFC 8866.
- Provide safe maximum limits and terminal “too many diagnostics” behavior for adversarial input.

### 2. RFC 8866 session description

- Protocol version `v=` with required value zero.
- Origin `o=` username, 64-bit decimal session identifier/version represented without precision loss, network type, address type, and unicast address.
- Session name `s=`, optional session information `i=`, URI `u=`, repeated email `e=`, and repeated phone `p=` fields with international text support where permitted.
- Session-level connection `c=`, repeated bandwidth `b=`, one or more timing blocks, time-zone adjustments `z=`, legacy encryption key `k=`, generic attributes, and zero or more media descriptions.
- Correct required/optional/cardinality/order rules, including the requirement for at least one `t=` line and session- versus media-level placement.
- Deep cloning and structural/semantic comparison without relying on inheritance, records, or `IEquatable`.

### 3. Media descriptions and connection data

- `m=` media token, port, optional port count, protocol token, ordered format list, zero-port rejection/disable semantics, and arbitrary registered or extension media/protocol/format tokens.
- Media title `i=`, repeated media connection data, repeated bandwidth, legacy encryption key, and ordered generic attributes.
- `IN IP4` and `IN IP6` unicast, multicast IPv4 `address/ttl[/count]`, multicast IPv6 `address[/count]`, FQDN/external-address preservation, and extension network/address types.
- Address/TTL/count syntax and range validation while retaining raw values needed by non-IP extensions.
- Session inheritance and media override resolution for connection, direction, ICE, DTLS, bandwidth, language, and other scoped values.
- Media activation/rejection helpers, port and port-count manipulation, MID lookup, and effective transport endpoint resolution.

### 4. Timing, recurrence, and zones

- NTP start/stop values as lossless unsigned decimal text plus checked conversions to/from .NET time representations when representable.
- Permanent sessions (`t=0 0`), bounded sessions, multiple active periods, and attached `r=` recurrence blocks.
- Typed-time units `d`, `h`, `m`, `s`, unitless seconds, positive repeat interval/active duration, and zero or more offsets.
- `z=` time/offset pairs including signed offsets and multiple adjustments.
- Helpers to test permanent activity, calculate representable occurrence windows, and normalize typed times without unbounded expansion.

### 5. Generic and core attributes

- Ordered add, insert, replace, remove-one, remove-all, find-first, and find-all operations with ASCII case-insensitive attribute-name comparison and case-sensitive values.
- RFC 8866 properties and values: `recvonly`, `sendrecv`, `sendonly`, `inactive`, `orient`, `type`, `charset`, `sdplang`, `lang`, `framerate`, `quality`, `tool`, and legacy category/keyword forms.
- RTSP-oriented `control` preservation/projection and generic support for all IANA-registered attributes not otherwise typed.
- IANA registry snapshot access for registered media, protocols, bandwidth types, network/address types, encryption methods, attributes, group semantics, and RTP payload/header-extension identifiers.
- Attribute-scope and multiplexing-category metadata where registered, with validation of session-only, media-only, both-scope, NORMAL, IDENTICAL, TRANSPORT, SUM, and CAUTION behavior.

### 6. RTP formats and codecs

- Ordered `m=` payload/format tokens, numeric RTP payload types 0–127, wildcard payload selectors where allowed, and non-RTP formats.
- `rtpmap` payload, encoding name, clock rate, and optional encoding parameters/channel count.
- `fmtp` payload and ordered semicolon parameter list with whitespace normalization, name/value and flag parameters, duplicate preservation, case handling, and raw fallback.
- `ptime`, `maxptime`, `framerate`, `quality`, and codec-specific opaque parameter preservation.
- Static RTP/AVP payload mappings from RFC 3551 and dynamic payload requirements.
- Codec capability projection and matching by media kind, encoding name, clock rate, channel count, format parameters, and configurable strict/compatible policy.
- Preference ordering, intersection, payload-number remapping, RTX `apt` association, RED/ULPFEC/FlexFEC association preservation, telephone-event ranges, and wildcard RTCP feedback.
- Typed convenience validation for Opus (RFC 7587), VP8 (RFC 7741), VP9 (RFC 9628), H.264 (RFC 6184), H.265/HEVC (RFC 7798), RTX (RFC 4588), RED (RFC 2198), ULPFEC (RFC 5109), FlexFEC (RFC 8627), comfort noise (RFC 3389), and telephone-event (RFC 4733) while retaining AV1 and all other unknown codec parameters generically.
- No media encoder/decoder discovery and no claim that two codec configurations are operationally usable beyond signaling compatibility.

### 7. RTP/RTCP attributes and sources

- `rtcp` port plus optional network/address triple (RFC 3605).
- `rtcp-mux`, `rtcp-mux-only`, `rtcp-rsize`, and RTP/RTCP mux offer/answer validation.
- `rtcp-fb` payload or wildcard, feedback type, optional subtype/parameter, `trr-int`, ACK/NACK, CCM/FIR, PLI, SLI, RPSI, TMMBR/TMMBN, transport-wide congestion feedback, and unknown feedback retention.
- `rtcp-xr` generic block token/value parsing plus loss-rle, dup-rle, pkt-rcpt-times, stat-summary, voip-metrics, receiver-rtt, and unknown blocks.
- `ssrc` identifier with optional source attribute/value, repeated entries, `cname`, `msid`, `mslabel`, `label`, arbitrary source attributes, and unsigned 32-bit range handling.
- `ssrc-group` semantics and ordered SSRC lists, including FID, FEC, FEC-FR, SIM, DUP, and extension semantics.
- `msid`, `msid-semantic`, media-stream/track association, and consistency checks across media descriptions.
- `content`, `label`, and source-filter `incl`/`excl` projections.

### 8. ICE attributes and helpers

- Session- and media-level `ice-ufrag`, `ice-pwd`, `ice-options`, and `ice-lite`, with inheritance and credential length/character validation.
- `candidate` foundation, component, transport, priority, connection address, port, candidate type, related address/port, TCP type, and arbitrary extension name/value pairs in original order.
- Host, server-reflexive, peer-reflexive, and relayed candidate rules; UDP and TCP rules; component, priority, port, related endpoint, and `tcptype` constraints.
- `remote-candidates`, `end-of-candidates`, `ice-mismatch`, trickle option, and partial-description/trickle fragment parsing and generation.
- Default destination consistency checks against candidates and connection/port data.
- Add/deduplicate/remove/filter/sort candidates, mark gathering complete, resolve effective credentials, set remote-candidate pairs, and detect ICE restart from changed credentials.
- Bundle-aware ICE resolution so the tagged m-section supplies transport-category attributes after negotiation.
- Helpers manipulate signaling only; they do not gather candidates, contact STUN/TURN servers, or run ICE state machines.

### 9. TLS, DTLS, and security attributes

- `fingerprint` hash algorithm and colon-separated certificate fingerprint, accepting registered algorithms and preserving extension algorithms; validate octets and algorithm-specific digest length.
- `setup` roles `active`, `passive`, `actpass`, and `holdconn`, including valid offer/answer role selection.
- Connection-oriented `connection:new|existing`, TLS connection reuse, and transport applicability from RFC 8122/RFC 4145.
- `tls-id` parsing, uniqueness, offer/answer continuity, and association with DTLS transport.
- Session/media inheritance and BUNDLE effective fingerprint/setup/tls-id resolution.
- Multiple fingerprint handling for algorithm agility and deterministic preferred-algorithm selection without performing certificate verification.
- SDES `crypto` tag, suite, key parameters, and session parameters as an explicitly supported legacy typed attribute; sensitive values are never logged in diagnostic text.

### 10. RTP header extensions and extmaps

- `extmap` identifier, optional direction, optional encryption URI marker, extension URI, and optional extension attributes.
- Identifier range, uniqueness, direction compatibility, session/media scope, and `extmap-allow-mixed` validation per RFC 8285.
- Known URI constants for transmission offset, absolute send/capture time, audio level, video orientation, transport-wide congestion control, MID, RTP stream ID, repaired RTP stream ID, playout delay, video content type/timing, color space, and extension URIs retained from registries.
- Resolve effective mappings, find URI by ID or ID by URI, intersect offer/local capabilities, choose answer directions, allocate nonconflicting IDs, and remap IDs while updating related state.
- BUNDLE-wide extmap conflict detection and optional consistent remapping across a BUNDLE group.

### 11. Grouping and BUNDLE

- Generic `group` semantics and ordered identification tags plus media-level `mid`.
- Known LS, FID, SRF, ANAT, FEC, DDP, BUNDLE, and extension semantics without closing the token space.
- Validate unique MIDs, group references, duplicate membership, empty groups, rejected media, multiple groups, and semantics-specific constraints.
- `bundle-only`, tagged m-section selection, initial and subsequent offer/answer rules, adding/removing/disabling m-sections, zero-port behavior, and rejection behavior per RFC 9143.
- Resolve BUNDLE transport address, ICE, DTLS, extmaps, RTCP mux requirements, IDENTICAL/TRANSPORT/NORMAL/SUM/CAUTION attributes, and aggregate bandwidth.
- Create, update, dissolve, and query BUNDLE groups while maintaining `group`, `mid`, and transport-category consistency.

### 12. RID, simulcast, and image constraints

- `rid` ID, send/recv direction, ordered restriction parameters, payload-type lists, maximum width/height/fps/fs/br/pps/bpp, dependency IDs, quality, and unknown restrictions.
- `simulcast` send/recv lists, ordered alternatives, stream layers, paused (`~`) alternatives, cross-reference to RID direction, uniqueness, and offer/answer subset rules.
- RTP stream ID and repaired stream ID extmap associations required by RID/simulcast.
- `imageattr` payload selector, send/recv sets, wildcard, bracketed alternatives, x/y/sar/par/q constraints, and raw fallback.
- High-level projection from RID/simulcast attributes into send and receive layer descriptions and safe removal/renaming of referenced RID IDs.

### 13. SCTP data channels and BFCP

- `UDP/DTLS/SCTP` and `TCP/DTLS/SCTP` media, `sctp-port`, and `max-message-size`, including zero/unlimited and default-size semantics.
- Legacy `sctpmap` preservation and interoperability parsing.
- `dcmap` channel ID and ordered subprotocol/label/max-retr/max-time/ordered parameters plus `dcsa` subprotocol attributes, duplicate and stream-ID validation, and generic unknown parameter retention.
- Data-channel capability projection and offer/answer validation without implementing SCTP or DCEP.
- BFCP transports and `floorctrl`, `confid`, `userid`, `floorid`, `bfcpver`, `setup`, and `connection` attributes using current RFC 8856 rules.

### 14. Clock and multicast-related extensions

- `source-filter` filter mode, network/address type, destination, and source list validation.
- `ts-refclk` clock source and extension data plus `mediaclk` ID, clock name/value, and optional rational rate.
- Reference-clock and media-clock scope/inheritance checks without implementing clock synchronization.
- Multicast connection counts, source filtering, and session/media address consistency.

### 15. Validation profiles

- Syntax-only, RFC 8866 semantic, ICE, TLS/DTLS, RTP, WebRTC, BUNDLE, data-channel, BFCP, and combined strict profiles.
- Required field/cardinality/order checks; token, Unicode, numeric overflow, port, payload, SSRC, TTL, duration, and identifier bounds.
- Cross-field rules: session or every-media connection, media protocol/format compatibility, dynamic payload `rtpmap`, attribute scope/cardinality, duplicate payload mappings, fmtp/feedback payload references, effective direction, and rejected media.
- Cross-media rules: MID/group references, unique payload mappings within BUNDLE RTP sessions, extmap conflicts, SSRC/RID consistency, MSID consistency, ICE credential/candidate rules, DTLS roles/fingerprints, RTCP mux, and SCTP requirements.
- Severity policy lets applications promote selected warnings to errors or suppress explicitly identified interoperability warnings without suppressing fatal parser failures.
- Validation never mutates the description; normalization is an explicit separate operation.

### 16. Generation, editing, comparison, and high-level operations

- Construct valid minimal sessions and media descriptions with caller-supplied origin/version/address/timing data; do not hide security-sensitive defaults.
- Canonical writer, lossless writer, string and UTF-8 output, required-buffer-size reporting, and result-based generation failure.
- Deep clone; add, insert, reorder, remove, reject, and reactivate media; increment origin version; normalize line/attribute order; and strip selected extension attributes.
- Query effective session/media values, attributes, codecs, endpoints, ICE/DTLS state, groups, BUNDLE state, extmaps, streams, and data-channel settings.
- Structural and semantic comparison with path-addressed differences and options to ignore origin version, ordering where insignificant, raw formatting, or selected attributes.
- Generate offers from endpoint capabilities; answer offers with direction intersection, codec/extmap matching, symmetric payload mapping, media rejection, ICE/DTLS selection, RTCP mux, BUNDLE subset, RID/simulcast subset, and data-channel constraints.
- Validate an answer against its offer and build subsequent offers while preserving m-line order, rejected m-lines, payload mappings, MID identity, and origin-version rules.
- Return a negotiation result containing the generated description, effective negotiated media, warnings/errors, payload/extmap mappings, bundle groups, and rejection reasons.

## Proposed public API and files

Names describe responsibilities, not final member-level signatures. Every public member will have policy-compliant XML documentation when implemented.

### Data model — `src/Api/Data`

Core syntax and results:

- `SdpSessionDescription.cs` — complete session, ordered media, attributes, raw/lossless state.
- `SdpOrigin.cs` — origin username, lossless identifiers, network/address tokens.
- `SdpConnectionData.cs` — network/address tokens, address, multicast TTL/count.
- `SdpBandwidth.cs` — bandwidth type, value, and extension-safe raw data.
- `SdpTiming.cs`, `SdpRepeatTime.cs`, `SdpTypedTime.cs`, `SdpTimeZoneAdjustment.cs` — timing graph.
- `SdpEncryptionKey.cs` — obsolete key method/value retained for compatibility.
- `SdpMediaDescription.cs` — m-line, title, connections, bandwidths, key, attributes, raw state.
- `SdpAttribute.cs` — generic property/value attribute, source location, raw spelling.
- `SdpRawLine.cs`, `SdpSourceLocation.cs` — lossless/recovery representation.
- `SdpParseOptions.cs`, `SdpParseResult.cs`, `SdpDiagnostic.cs` — bounded result-based parsing.
- `SdpWriteOptions.cs`, `SdpWriteResult.cs` — canonical/lossless generation settings and result.
- `SdpValidationOptions.cs`, `SdpValidationResult.cs`, `SdpValidationIssue.cs` — validation profiles and issues.
- `SdpComparisonOptions.cs`, `SdpComparisonResult.cs`, `SdpDifference.cs` — structural/semantic diff.
- `SdpAttributeRegistration.cs`, `SdpRegisteredToken.cs` — registry metadata projections.

Typed attributes and transport/media projections:

- `SdpRtpMapAttribute.cs`, `SdpFormatParametersAttribute.cs`, `SdpFormatParameter.cs`, `SdpRtcpAttribute.cs`, `SdpRtcpFeedbackAttribute.cs`, `SdpRtcpExtendedReportAttribute.cs`.
- `SdpCodec.cs`, `SdpCodecCapability.cs`, `SdpMediaCapability.cs`, `SdpEndpointCapabilities.cs`, `SdpPayloadMapping.cs`.
- `SdpSourceAttribute.cs`, `SdpSourceGroupAttribute.cs`, `SdpMediaStreamId.cs`, `SdpSourceFilterAttribute.cs`.
- `SdpIceCredentials.cs`, `SdpIceCandidate.cs`, `SdpIceCandidateExtension.cs`, `SdpRemoteCandidate.cs`, `SdpIceDescription.cs`.
- `SdpFingerprintAttribute.cs`, `SdpCryptoAttribute.cs`, `SdpDtlsDescription.cs`.
- `SdpExtMapAttribute.cs`, `SdpHeaderExtension.cs`, `SdpHeaderExtensionMapping.cs`.
- `SdpGroupAttribute.cs`, `SdpBundleGroup.cs`, `SdpBundleTransport.cs`, `SdpEffectiveMedia.cs`.
- `SdpRidAttribute.cs`, `SdpRidRestriction.cs`, `SdpSimulcastAttribute.cs`, `SdpSimulcastStream.cs`, `SdpSimulcastAlternative.cs`, `SdpImageAttribute.cs`.
- `SdpClockReferenceAttribute.cs`, `SdpMediaClockAttribute.cs`.
- `SdpSctpDescription.cs`, `SdpDataChannelAttribute.cs`, `SdpDataChannelParameter.cs`, `SdpBfcpDescription.cs`.
- `SdpNegotiationOptions.cs`, `SdpNegotiationResult.cs`, `SdpMediaNegotiation.cs`, `SdpRejection.cs`.
- `SdpAttributeDecodeResult.cs`, `SdpAttributeEncodeResult.cs`, `SdpBufferWriteResult.cs` — non-throwing operation results necessitated by the syntax policy.

### Operation contracts — `src/Api/Contracts`

- `ISdpParser.cs` — text and UTF-8 parsing entry points.
- `ISdpWriter.cs` — text, UTF-8, and caller-buffer generation entry points with distinct names.
- `ISdpValidator.cs` — profile-driven validation.
- `ISdpAttributeCodec.cs` — generic-to-typed decoding and typed-to-generic encoding.
- `ISdpRegistry.cs` — query the built-in IANA snapshot and known-token metadata.
- `ISdpSessionFactory.cs` — create minimal sessions and media.
- `ISdpEditor.cs` — clone and controlled session/media/attribute mutation.
- `ISdpQueryService.cs` — effective values, scope inheritance, MID/media lookup, and endpoint projection.
- `ISdpTimingService.cs` — NTP conversion, normalization, and bounded occurrence calculation.
- `ISdpCodecService.cs` — codec projection, matching, ordering, intersection, and payload remapping.
- `ISdpIceService.cs` — effective ICE state, candidate editing, completion, remote candidates, and restart detection.
- `ISdpDtlsService.cs` — fingerprint, setup-role, TLS-ID, and effective DTLS handling.
- `ISdpExtMapService.cs` — effective extmaps, negotiation, ID allocation, conflict detection, and remapping.
- `ISdpGroupService.cs` — generic group/MID creation, update, removal, and resolution.
- `ISdpBundleService.cs` — BUNDLE construction, mutation, validation, and transport resolution.
- `ISdpStreamService.cs` — SSRC, MSID, RID, simulcast, and layer projection/editing.
- `ISdpDataChannelService.cs` — SCTP/data-channel projection and validation.
- `ISdpComparer.cs` — structural and semantic comparison.
- `ISdpOfferAnswerService.cs` — initial/subsequent offer creation, answer creation, and answer validation.

### Static data — `src/Api/Static`

- `SdpFieldNames.cs` — RFC field characters/names.
- `SdpAttributeNames.cs` — standardized typed attribute names.
- `SdpMediaTypes.cs`, `SdpTransportProtocols.cs`, `SdpBandwidthTypes.cs`, `SdpNetworkTypes.cs`, `SdpAddressTypes.cs` — common registered tokens.
- `SdpGroupSemantics.cs`, `SdpFingerprintAlgorithms.cs`, `SdpCryptoSuites.cs` — grouping/security tokens.
- `SdpCodecNames.cs`, `SdpStaticPayloadTypes.cs`, `SdpRtcpFeedbackTypes.cs` — RTP constants.
- `SdpHeaderExtensionUris.cs` — standardized/common RTP header-extension URIs.
- `SdpIceTokens.cs`, `SdpDataChannelTokens.cs`, `SdpClockTokens.cs` — protocol constants.
- `SdpLimits.cs` — safe default input/output limits.
- `SdpDiagnosticCodes.cs` — stable string codes for parse, validation, write, and negotiation diagnostics.
- `SdpRegistrySnapshot.cs` — readonly registry data generated from the downloaded IANA snapshots; data only, no methods.

### Enums — `src/Api/Enums`

- `SdpParseMode.cs`, `SdpWriteMode.cs`, `SdpLineEnding.cs`, `SdpValidationProfile.cs`, `SdpValidationSeverity.cs`.
- `SdpScope.cs`, `SdpAttributeKind.cs`, `SdpMediaKind.cs`, `SdpAddressKind.cs`, `SdpDirection.cs`.
- `SdpIceCandidateType.cs`, `SdpIceTransportProtocol.cs`, `SdpIceTcpType.cs`, `SdpDtlsSetupRole.cs`.
- `SdpMuxCategory.cs`, `SdpGroupSemantic.cs`, `SdpCodecMatchMode.cs`, `SdpNegotiationRole.cs`.
- `SdpDifferenceKind.cs`, `SdpRejectionReason.cs`, `SdpClockSourceKind.cs`.

Unknown/extension values remain in companion string fields; enum `Unknown` values never discard the original token.

### Exceptions — `src/Api/Exceptions`

- `SdpParseException.cs`, `SdpValidationException.cs`, `SdpGenerationException.cs`, `SdpNegotiationException.cs` — throwing API failures, each directly derived from `System.Exception` with one constructor.

### Public implementations — `src/Api/Classes`

- `SdpParser.cs`, `SdpWriter.cs`, `SdpValidator.cs`, `SdpAttributeCodec.cs`, `SdpRegistry.cs`.
- `SdpSessionFactory.cs`, `SdpEditor.cs`, `SdpQueryService.cs`, `SdpTimingService.cs`.
- `SdpCodecService.cs`, `SdpIceService.cs`, `SdpDtlsService.cs`, `SdpExtMapService.cs`.
- `SdpGroupService.cs`, `SdpBundleService.cs`, `SdpStreamService.cs`, `SdpDataChannelService.cs`.
- `SdpComparer.cs`, `SdpOfferAnswerService.cs`.

Each class implements its same-purpose contract, has one public constructor at most, has no public static methods, and exposes no additional public instance members.

## Planned internal source files

Internal types live outside `src/Api` and may be reorganized without changing the public API.

- `src/Text/SdpLineReader.cs`, `SdpTokenReader.cs`, `SdpUtf8Decoder.cs`, `SdpNumberParser.cs`, `SdpTypedTimeParser.cs`, `SdpValueWriter.cs`, `SdpUtf8Writer.cs`.
- `src/Parsing/SdpParserCore.cs`, `SdpSessionLineParser.cs`, `SdpMediaLineParser.cs`, `SdpAttributeLineParser.cs`, `SdpRecovery.cs`, `SdpParseContext.cs`.
- `src/Attributes/SdpAttributeDecoderCore.cs`, `SdpAttributeEncoderCore.cs`, `RtpAttributeCodec.cs`, `IceAttributeCodec.cs`, `DtlsAttributeCodec.cs`, `ExtMapAttributeCodec.cs`, `GroupAttributeCodec.cs`, `StreamAttributeCodec.cs`, `DataChannelAttributeCodec.cs`, `ClockAttributeCodec.cs`, `BfcpAttributeCodec.cs`.
- `src/Writing/SdpWriterCore.cs`, `SdpCanonicalOrder.cs`, `SdpLosslessWriter.cs`.
- `src/Validation/SdpValidationContext.cs`, `SdpCoreRules.cs`, `SdpAttributeRules.cs`, `SdpRtpRules.cs`, `SdpIceRules.cs`, `SdpDtlsRules.cs`, `SdpExtMapRules.cs`, `SdpGroupRules.cs`, `SdpBundleRules.cs`, `SdpStreamRules.cs`, `SdpDataChannelRules.cs`, `SdpBfcpRules.cs`.
- `src/Operations/SdpCloneCore.cs`, `SdpMutationCore.cs`, `SdpEffectiveValueResolver.cs`, `SdpCodecMatcher.cs`, `SdpPayloadAllocator.cs`, `SdpExtMapAllocator.cs`, `SdpBundleResolver.cs`, `SdpOfferAnswerCore.cs`, `SdpComparisonCore.cs`, `SdpTimingCalculator.cs`.
- `src/Registry/SdpRegistryData.cs`, `SdpRegistryLookup.cs` — generated/internal lookup over the checked-in IANA snapshot data.
- `src/Diagnostics/SdpDiagnosticBuilder.cs`, `SdpDiagnosticLimiter.cs`.

## Test layout and coverage strategy

- `tests/Unit/Text`, `Parsing`, `Attributes`, `Writing`, `Validation`, `Operations`, and `Registry` mirror internal behavior through public APIs.
- `tests/Conformance/Rfc8866`, `Rfc8122`, `Rfc8839`, `Rtp`, `WebRtc`, `Bundle`, and `DataChannel` use examples and boundary cases derived from the checked-in specifications.
- `tests/Interop/SdpTransform`, `LibSdpTransform`, `Pjsip`, `GStreamer`, `Browsers`, `Sip`, and `Rtsp` contain independently expressed fixtures covering accepted field/attribute shapes and known deviations.
- `tests/RoundTrip` verifies canonical stability, lossless fidelity, unknown-extension survival, text/UTF-8 equivalence, and parse-write-parse semantic equivalence.
- `tests/Api` covers every public constructor/default, every contract method through its public implementation, every exception path, result-based failure path, static token set, and enum-backed unknown-token behavior.
- Tests may use NUnit and external test-only libraries. They must not introduce nullable value types. No syntax-policy/source-scanning tests and no benchmarks are planned.

## Implementation tasks

### Foundation and reference data

- [DONE] T001: Record the target namespace and assembly naming conventions in the initial source skeleton without changing the public folder policy.
- [DONE] T002: Create the `src/Api/Data`, `Contracts`, `Static`, `Enums`, `Exceptions`, and `Classes` folders and the internal source/test folder layout listed above.
- [DONE] T003: Add the public parse, write, validation, scope, direction, ICE, DTLS, grouping, codec, comparison, rejection, and clock enums with documented members and extension-safe `Unknown` members.
- [DONE] T004: Add field, attribute, media, protocol, network, address, bandwidth, group, security, ICE, data-channel, clock, codec, RTCP feedback, and header-extension static data classes.
- [DONE] T005: Define safe parser/writer limit constants and stable diagnostic code constants as documented static fields.
- [DONE] T006: Transform the checked-in IANA SDP and RTP XML snapshots into policy-compliant readonly source data without adding a runtime XML or external dependency.
- [DONE] T007: Implement internal registry lookup tables with correct token case rules and expose them through `ISdpRegistry` and `SdpRegistry`.
- [DONE] T008: Add public registry projection data classes for registered tokens, attribute scopes, mux categories, references, and status.
- [DONE] T009: Test every public static token group, registry query path, case rule, unknown lookup, duplicate registry alias, and snapshot record projection.

### Core data model and diagnostics

- [DONE] T010: Implement source-location, raw-line, generic attribute, origin, connection, bandwidth, encryption-key, timing, recurrence, zone, media, and session data classes with initialized mutable collections.
- [DONE] T011: Implement parse/write/validation option and result data classes with safe defaults, explicit success fields, bounded diagnostics, and no nullable value types.
- [DONE] T012: Implement diagnostic, validation issue, source path, comparison difference, and operation buffer-result data classes.
- [DONE] T013: Implement the typed RTP, codec, source, ICE, DTLS, extmap, grouping, RID/simulcast, clock, SCTP/data-channel, BFCP, and negotiation data classes listed in the API plan.
- [DONE] T014: Add one documented constructor to each public data class only where construction invariants or collection initialization require it; otherwise use field initializers.
- [DONE] T015: Implement diagnostic collection and limit handling so one terminal diagnostic is emitted when configured capacity is exhausted.
- [DONE] T016: Implement the four public exception classes with one constructor each and sensitive-value-safe messages.
- [DONE] T017: Test all public data defaults, collection independence, constructor invariants, result success/failure representation, source paths, diagnostic limiting, and exception messages/inner exception behavior.

### Low-level text parsing

- [DONE] T018: Implement allocation-conscious character and UTF-8 line readers with CRLF, accepted alternate endings, exact offsets, line/column tracking, line limits, and terminal-line handling.
- [DONE] T019: Implement span-based token readers for spaces, non-whitespace tokens, attribute names, decimal numbers, signed values, and remainder capture.
- [DONE] T020: Implement checked parsing for signed/unsigned decimal values, ports, payload types, SSRCs, priorities, NTP values, and overflow diagnostics.
- [DONE] T021: Implement UTF-8 validation, BOM policy, NUL/control rejection, allowed RFC character classes, and international text decoding.
- [DONE] T022: Implement typed-time parsing and canonical formatting for unitless seconds and `d/h/m/s` units.
- [DONE] T023: Implement bounded recovery primitives that can retain malformed lines without losing the following media/session boundary.
- [DONE] T024: Test every line-ending form, empty/terminal line, BOM, valid/invalid UTF-8 sequence, Unicode text field, NUL/control, length boundary, numeric boundary/overflow, typed-time unit, and recovery boundary.

### RFC 8866 parser

- [DONE] T025: Define `ISdpParser` with distinctly named text and UTF-8 result-based entry points and implement the public `SdpParser` shell.
- [DONE] T026: Implement the parser context, mode/limit enforcement, diagnostic paths, and transition from session scope to media scope.
- [DONE] T027: Parse and validate the `v=` field discriminator, cardinality, position, and required version zero.
- [DONE] T028: Parse `o=` with lossless 64-bit decimal identifiers, token/address preservation, and strict six-token cardinality.
- [DONE] T029: Parse `s=`, session/media `i=`, `u=`, repeated `e=`, and repeated `p=` with their text and placement rules.
- [DONE] T030: Parse session/media `c=` including extension types, IPv4/IPv6 unicast, multicast TTL, address count, and raw fallback.
- [DONE] T031: Parse repeated session/media `b=` lines with registered and extension bandwidth types and checked values.
- [DONE] T032: Parse multiple `t=` blocks, attached `r=` repetitions, and `z=` adjustment pairs with correct ordering.
- [DONE] T033: Parse and preserve obsolete session/media `k=` values with strict-profile warnings.
- [DONE] T034: Parse generic property/value `a=` lines while preserving name spelling, empty values, order, duplicates, raw line, and source location.
- [DONE] T035: Parse `m=` media, port/count, protocol, ordered formats, following media fields, and zero-port state.
- [DONE] T036: Enforce RFC field order, required fields, cardinality, field scope, legal line prefixes, and session-before-media structure according to parse mode.
- [DONE] T037: Preserve unknown field types and malformed-but-recoverable core lines in lossless mode with explicit diagnostics.
- [DONE] T038: Test all RFC 8866 ABNF productions, field orders, cardinalities, scopes, minimal/full examples, permanent/multiple/repeating sessions, multicast forms, empty values, unknown fields, and malformed recovery.
- [DONE] T039: Test strict versus interoperable versus lossless behavior against SIP, RTSP, browser, GStreamer, PJSIP, sdp-transform, and libsdptransform fixture families.
- [DONE] T040: Test adversarial line/field/media/attribute/token/diagnostic limits and verify partial results never omit a reported fatal condition.

### Typed attribute codec

- [DONE] T041: Define `ISdpAttributeCodec`, public decode/encode result objects, and the `SdpAttributeCodec` implementation with one non-overloaded operation per direction.
- [DONE] T042: Implement common direction, language, charset, type, orient, tool, quality, framerate, ptime, maxptime, content, label, control, and obsolete attribute projections.
- [DONE] T043: Implement `rtpmap` decoding/encoding with payload, codec, rate, and optional encoding parameter validation.
- [DONE] T044: Implement `fmtp` decoding/encoding with ordered parameters, flags, duplicates, quoted/opaque values, whitespace normalization, and raw fallback.
- [DONE] T045: Implement `rtcp`, `rtcp-mux`, `rtcp-mux-only`, `rtcp-rsize`, `rtcp-fb`, and `rtcp-xr` projections.
- [DONE] T046: Implement `ssrc`, `ssrc-group`, `msid`, `msid-semantic`, and source attribute projections with unsigned identifier handling.
- [DONE] T047: Implement `source-filter`, `ts-refclk`, `mediaclk`, and `imageattr` projections.
- [DONE] T048: Implement generic `group`/`mid`, BUNDLE markers, and registered grouping-semantics projections.
- [DONE] T049: Ensure every typed encoder can produce a generic attribute, every decoder retains its originating generic attribute, and failed decoding never destroys opaque data.
- [DONE] T050: Test all common and RTP/source/group/clock/image typed attributes with normative examples, all optional forms, unknown tokens, duplicates, invalid bounds, whitespace variants, encode/decode symmetry, and failure preservation.

### ICE SDP

- [DONE] T051: Implement `ice-ufrag`, `ice-pwd`, `ice-options`, `ice-lite`, `ice-mismatch`, and `end-of-candidates` typed attribute codecs.
- [DONE] T052: Implement candidate decoding/encoding for required fields, all standard candidate/transport/TCP types, related address/port, and ordered unknown extension pairs.
- [DONE] T053: Implement `remote-candidates` tuple-list decoding/encoding and component uniqueness checks.
- [DONE] T054: Add ICE validation rules for scope, inheritance, credential syntax/length, candidate numeric/address/type constraints, TCP requirements, related endpoint rules, and marker cardinality.
- [DONE] T055: Add default-destination/candidate consistency, component coverage, RTCP-mux interaction, ICE-lite role, trickle option, and partial gathering validation.
- [DONE] T056: Define `ISdpIceService` and implement effective credential/candidate resolution across session, media, and BUNDLE scopes.
- [DONE] T057: Implement candidate add, deduplication, removal/filtering, deterministic priority ordering, completion marker, and remote-candidate update operations.
- [DONE] T058: Implement ICE restart detection and restart editing that replaces credentials, clears stale candidates/remote candidates/end markers, and respects BUNDLE transport ownership.
- [DONE] T059: Implement standalone trickle ICE SDP fragment parsing/generation with MID/m-line association and end-of-candidates handling.
- [DONE] T060: Test every RFC 8839 candidate and credential ABNF boundary, inheritance combination, ICE-lite/full role, default destination, mismatch, trickle/fragment, candidate extension, TCP case, restart, BUNDLE, and malformed-input result path.

### TLS, DTLS, and SDES security

- [DONE] T061: Implement `fingerprint` decoding/encoding for algorithm tokens and colon-separated octets without including fingerprint contents in failure messages.
- [DONE] T062: Implement `setup`, `connection`, and `tls-id` typed attributes with extension-safe token retention.
- [DONE] T063: Implement legacy `crypto` decoding/encoding for tag, suite, key parameters, session parameters, and opaque unknown forms without logging key material.
- [DONE] T064: Add RFC 8122 validation for applicable transports, setup/connection scope and cardinality, fingerprint syntax/algorithm length, and TLS connection reuse.
- [DONE] T065: Add DTLS-SRTP validation for offer/answer setup roles, session/media inheritance, multiple fingerprints, tls-id uniqueness/continuity, and secure transport profiles.
- [DONE] T066: Define `ISdpDtlsService` and implement effective DTLS resolution, preferred fingerprint selection, role selection, and fingerprint/TLS-ID replacement.
- [DONE] T067: Make DTLS resolution BUNDLE-aware and report inconsistent transport-category attributes across grouped media.
- [DONE] T068: Test all RFC 8122, RFC 5763, and RFC 8842 examples/boundaries, algorithms, malformed octets, role pairs, connection reuse, inheritance, multiple fingerprints, BUNDLE behavior, and secret-redaction paths.

### Codecs, RTP, and RTCP

- [DONE] T069: Define `ISdpCodecService` and implement codec projection by joining m-line formats with `rtpmap`, `fmtp`, feedback, ptime, and static RTP/AVP mappings.
- [DONE] T070: Implement validation for payload range/uniqueness, dynamic `rtpmap` requirements, duplicate/inconsistent maps, dangling fmtp/feedback, wildcard feedback, clock rate, channel count, and protocol applicability.
- [DONE] T071: Implement codec matching modes using media kind, name, rate, channels, and compatible/required format parameters without LINQ or project-declared generics.
- [DONE] T072: Implement stable codec preference ordering and local/remote intersection while preserving unknown codecs and format order.
- [DONE] T073: Implement payload allocation/remapping with symmetric-answer mapping, collision detection, associated RTX `apt`, RED/FEC lists, feedback, RID `pt`, and simulcast references.
- [DONE] T074: Implement typed Opus parameter validation and compatible matching including stereo, FEC, DTX, bitrate, playback rate, and packetization timing.
- [DONE] T075: Implement typed H.264 parameter validation and compatible matching including packetization mode, profile-level-id, level asymmetry, and profile/level compatibility.
- [DONE] T076: Implement VP8, VP9, H.265/HEVC, RTX, RED, ULPFEC/FlexFEC, comfort-noise, and telephone-event parameter/range validation and association checks.
- [DONE] T077: Implement RTCP port/mux/mux-only/reduced-size and feedback capability intersection rules.
- [DONE] T078: Test codec projection for static/dynamic/non-RTP formats, parameter parser edge cases, every matching mode, stable ordering, allocation exhaustion, remapping graph updates, and dangling references.
- [DONE] T079: Test Opus, H.264, H.265/HEVC, VP8, VP9, RTX, RED, ULPFEC/FlexFEC, comfort-noise, telephone-event, RTCP feedback/XR, ptime/maxptime, and wildcard behavior with compatible and incompatible pairs.

### Extmaps

- [DONE] T080: Implement `extmap` and `extmap-allow-mixed` typed codecs including direction, encryption marker URI, extension URI, and opaque attributes.
- [DONE] T081: Add RFC 8285 validation for identifiers, scope, cardinality, direction, encryption form, mixed one-/two-byte use, and duplicate ID/URI mappings.
- [DONE] T082: Define `ISdpExtMapService` and implement effective session/media mapping resolution plus ID/URI queries.
- [DONE] T083: Implement offer/local extmap intersection, answer direction selection, deterministic free-ID allocation, and full mapping remap.
- [DONE] T084: Implement BUNDLE-wide URI/ID compatibility validation and consistent remapping across every member media section.
- [DONE] T085: Test all extmap forms and boundaries, encrypted mappings, direction matrices, allow-mixed, session inheritance, conflicts, allocation, remapping, BUNDLE, and known/unknown URIs.

### Groups and BUNDLE

- [DONE] T086: Define `ISdpGroupService` and implement group resolution, MID lookup, group creation/update/removal, and extension semantics preservation.
- [DONE] T087: Add generic group validation for MID uniqueness, referenced/existing media, duplicate tags/membership, empty lists, rejected media, and semantics metadata.
- [DONE] T088: Define `ISdpBundleService` and implement BUNDLE group projection, tagged m-section selection, and effective transport resolution.
- [DONE] T089: Implement initial BUNDLE offer construction with MID allocation, unique fallback transports, optional bundle-only media, tagged-section selection, and group ordering.
- [DONE] T090: Implement initial BUNDLE answer handling for accepted subset, tagged-section selection, bundle-only acceptance/rejection, and transport-category placement.
- [DONE] T091: Implement subsequent BUNDLE offer/answer edits for adding, removing, moving, disabling, rejecting, and reactivating m-sections while retaining m-line/MID identity.
- [DONE] T092: Implement BUNDLE validation for address/protocol consistency, port rules, RTCP mux/mux-only, ICE/DTLS/extmap requirements, mux categories, and attribute placement.
- [DONE] T093: Implement effective IDENTICAL/TRANSPORT attributes, per-media NORMAL attributes, SUM bandwidth aggregation, and CAUTION diagnostics using registry metadata.
- [DONE] T094: Test generic group semantics and every RFC 9143 initial/subsequent offer/answer branch, multiple groups, tagged media rejection, bundle-only, zero port, transport resolution, mux categories, and malformed references.

### Streams, RID, simulcast, and MSID

- [DONE] T095: Implement `rid` typed decoding/encoding with ordered known/unknown restrictions and payload lists.
- [DONE] T096: Implement `simulcast` typed decoding/encoding with send/recv ordering, layers, alternatives, paused alternatives, and raw fallback for legacy draft syntax.
- [DONE] T097: Define `ISdpStreamService` and implement SSRC group, MSID, RID, simulcast, and image-constraint projections.
- [DONE] T098: Add SSRC/source-group validation for ranges, duplicate/conflicting attributes, semantics cardinality, references, and CNAME/MSID consistency.
- [DONE] T099: Add RFC 8830 MSID validation for identifier lengths, repeated association rules, appdata consistency, and session semantic declarations.
- [DONE] T100: Add RID validation for ID/direction/restriction syntax, payload references, dependency cycles/missing IDs, and numeric constraint bounds.
- [DONE] T101: Add simulcast validation for one attribute per media, RID existence/direction, unique alternatives, paused capability, offer/answer subset/order, and stream-ID extmap requirements.
- [DONE] T102: Implement safe RID rename/removal and layer filtering that updates dependency, simulcast, and related mappings atomically in the returned result.
- [DONE] T103: Test SSRC/source groups, MSID, RID restrictions, dependency graphs, simulcast alternatives/pause/order, legacy syntax fallback, imageattr, layer projection, and edit reference integrity.

### SCTP data channels and BFCP

- [DONE] T104: Implement `sctp-port`, `max-message-size`, and legacy `sctpmap` typed codecs and media projection.
- [DONE] T105: Implement `dcmap` and `dcsa` typed codecs with ordered channel parameters, embedded subprotocol attribute preservation, and unknown parameter support.
- [DONE] T106: Define `ISdpDataChannelService` and implement effective SCTP/data-channel projection and signaling capability intersection.
- [DONE] T107: Add RFC 8841/8864 validation for transport/protocol, required SCTP port, size semantics/default, channel ID/parameter constraints, duplicates, dcsa references, and offer/answer direction.
- [DONE] T108: Implement BFCP typed projection for current transports, floor control, conference/user/floor IDs, version, setup, and connection reuse.
- [DONE] T109: Add BFCP validation for attribute presence/cardinality, transports, floor-to-m-stream references, and offer/answer role combinations.
- [DONE] T110: Test SCTP modern/legacy forms, unlimited/default sizes, data-channel parameters, dcsa nesting/references, capability intersection, BFCP transports/roles/IDs, and invalid combinations.

### Timing and effective-value queries

- [DONE] T111: Define `ISdpTimingService` and implement checked NTP/.NET conversions that report unrepresentable values without nullable value types.
- [DONE] T112: Implement typed-time normalization and bounded recurrence occurrence calculation for a caller-provided window and result limit.
- [DONE] T113: Define `ISdpQueryService` and implement session/media lookup by index/MID plus effective connection, bandwidth, direction, language, ICE, DTLS, extmap, and endpoint resolution.
- [DONE] T114: Implement multicast/source-filter and reference/media clock effective-value validation and projection.
- [DONE] T115: Test permanent/bounded/multiple timings, NTP epoch/bounds, negative zone offsets, recurrence windows/limits, media inheritance/override combinations, multicast, clocks, and query not-found results.

### Validation engine

- [DONE] T116: Define `ISdpValidator` and implement a non-mutating rule runner with syntax-only, core, ICE, TLS/DTLS, RTP, WebRTC, BUNDLE, data-channel, BFCP, and combined profiles.
- [DONE] T117: Implement severity promotion/suppression by stable diagnostic code while keeping fatal structural errors unsuppressible.
- [DONE] T118: Implement core semantic rules for required fields, version, identifiers, addresses, connection coverage, timing, ports, formats, attribute scope/cardinality, and rejected media.
- [DONE] T119: Integrate RTP, ICE, DTLS, extmap, grouping, BUNDLE, stream, data-channel, BFCP, multicast, and clock rule sets without duplicate issues for one root cause.
- [DONE] T120: Add WebRTC profile rules for secure RTP/SCTP transports, ICE, DTLS fingerprints/setup, RTCP mux, MID/BUNDLE, extmap, MSID, and data-channel requirements.
- [DONE] T121: Ensure every issue contains stable code, severity, path, scope, media index, field/attribute, line/column when sourced from parsing, and actionable detail without secrets.
- [DONE] T122: Test every validation profile independently and combined, severity overrides, suppression, diagnostic limits, no-mutation guarantee, issue locations/paths, root-cause deduplication, and sensitive-data redaction.

### Generation and editing

- [DONE] T123: Define `ISdpSessionFactory` and implement minimal session/media creation that requires caller choices for origin, address, timing, transport, and formats where unsafe defaults would result.
- [DONE] T124: Define `ISdpWriter` and implement canonical string generation in RFC field order using CRLF and invariant formatting.
- [DONE] T125: Implement canonical generation for every core field and every typed attribute encoder while retaining generic attributes and extension tokens.
- [DONE] T126: Implement lossless generation using preserved raw lines when unchanged and localized canonical replacement when edited.
- [DONE] T127: Implement UTF-8 generation and caller-buffer writing with exact required-size/result reporting and no partial-success ambiguity.
- [DONE] T128: Validate generation inputs, reject unrepresentable or structurally invalid state according to options, and expose both result-based and documented throwing failure paths.
- [DONE] T129: Define `ISdpEditor` and implement deep clone, attribute add/insert/replace/remove, media add/insert/reorder/remove, reject/reactivate, and origin-version increment operations.
- [DONE] T130: Implement explicit normalization for field order, line endings, token casing where defined, whitespace, typed times, attributes, formats, and duplicates without running it implicitly during parse.
- [DONE] T131: Test minimal/full canonical output, exact CRLF/UTF-8 bytes, every core field/typed attribute, unknown/obsolete data, lossless unchanged/edited cases, buffer sizing, invalid model failures, and deterministic repeated writes.
- [DONE] T132: Test every editor operation for order, reference integrity, deep-copy isolation, rejected/reactivated state, origin version overflow/failure, and normalization idempotence.

### Comparison

- [DONE] T133: Define `ISdpComparer` and implement path-based structural comparison for every public model field and ordered collection.
- [DONE] T134: Implement semantic comparison using effective inherited values, case rules, insignificant formatting, configurable ordering, ignored origin version, and selected ignored attributes.
- [DONE] T135: Produce added/removed/changed/moved difference objects with media/MID/attribute/payload-aware paths and both compared values where safe.
- [DONE] T136: Test equality and each difference kind across all core/typed models, options, unknown data, reordering, inheritance equivalence, BUNDLE effective values, and sensitive-value redaction.

### Offer/answer and high-level negotiation

- [DONE] T137: Define endpoint/media/codec/header-extension capabilities and negotiation option/result models with explicit policy choices and rejection reasons.
- [DONE] T138: Define `ISdpOfferAnswerService` and implement initial offer creation with stable media order, origin version, directions, codecs, ICE/DTLS, RTCP, extmaps, MIDs, BUNDLE, RID/simulcast, and SCTP capabilities.
- [DONE] T139: Implement answer m-line matching by position, protocol compatibility, zero-port rejection, direction intersection, and connection inheritance per RFC 3264.
- [DONE] T140: Implement answer codec intersection, preference policy, fmtp compatibility, symmetric payload selection, associated payload graph, RTCP feedback, ptime, and per-media rejection.
- [DONE] T141: Implement answer ICE and DTLS role/credential/fingerprint selection, RTCP mux policy, extmap intersection/allocation, and secure transport rejection.
- [DONE] T142: Implement answer generic group/BUNDLE subset, tagged media, transport attributes, MID preservation, bundle-only behavior, and multiple group handling.
- [DONE] T143: Implement answer MSID, SSRC, RID/simulcast subset, header-extension prerequisites, image constraints, and data-channel/SCTP negotiation.
- [DONE] T144: Implement answer validation against the exact offer: m-line count/order, payloads, directions, transports, addresses, ICE/DTLS, extmaps, groups/BUNDLE, streams, and SCTP.
- [DONE] T145: Implement subsequent offer creation preserving m-line/MID identity, rejected slots, payload/extmap mappings, prior BUNDLE state, ICE restart intent, DTLS continuity, and origin-version rules.
- [DONE] T146: Populate effective media negotiation results, payload/extmap maps, BUNDLE transports, warnings, errors, and explicit rejection reasons without mutating inputs.
- [DONE] T147: Test RFC 3264 initial offer/answer direction matrix, codec preference modes, symmetric/asymmetric payloads, rejection, hold, connection changes, and illegal answers.
- [DONE] T148: Test WebRTC offer/answer combinations for ICE, ICE restart/trickle, DTLS roles/fingerprints/TLS-ID, RTCP mux, extmaps, MID/MSID, BUNDLE subsets, RID/simulcast, and data channels.
- [DONE] T149: Test subsequent offers for add/remove/reject/reactivate media, version changes, codec/payload stability, BUNDLE modifications, ICE restart, DTLS continuity, and rollback-safe non-mutation on failure.
- [DONE] T150: Test negotiation results and all rejection reason values, warning/error propagation, unknown extension survival, deterministic choices, and input/capability immutability.

### Full conformance, interoperability, and API coverage

- [DONE] T151: Convert every usable RFC 8866 example plus ABNF boundary into parsing, validation, and canonical generation conformance fixtures.
- [DONE] T152: Convert RFC 8122 TLS examples and invalid role/fingerprint/connection cases into round-trip and semantic conformance fixtures.
- [DONE] T153: Convert RFC 8839 ICE offer/answer, candidate, mismatch, restart, lite, and default-destination cases into conformance fixtures.
- [DONE] T154: Add conformance fixtures from the downloaded RTP/RTCP, grouping, extmap, MSID, RID/simulcast, BUNDLE, SCTP/data-channel, source, clock, image, codec, and BFCP RFCs.
- [DONE] T155: Add independent interoperability fixtures representing every SDP field/attribute shape handled by sdp-transform and libsdptransform, including their unknown-attribute and LF behavior.
- [DONE] T156: Add independent interoperability fixtures for PJSIP parse/print/validate, attribute search/mutation, transport comparison, media deactivation, static/dynamic payload, and offer/answer cases.
- [DONE] T157: Add independent interoperability fixtures for GStreamer core message/media fields, multiple connections/bandwidth/times/zones, generic attributes, mutation order, and text conversion.
- [DONE] T158: Add realistic browser WebRTC, SIP/RTP, RTSP/control, IPv4/IPv6, unicast/multicast, audio/video/text/application, and vendor-extension fixture suites.
- [DONE] T159: Test parse-write-parse semantic equality and canonical idempotence for every conformance and interoperability fixture.
- [DONE] T160: Test byte-for-byte lossless re-emission for accepted unchanged inputs and preservation of unknown/malformed recoverable lines after localized edits.
- [DONE] T161: Add public API tests invoking every `ISdpParser`, `ISdpWriter`, `ISdpValidator`, `ISdpAttributeCodec`, `ISdpRegistry`, factory, editor, query, timing, codec, ICE, DTLS, extmap, group, BUNDLE, stream, data-channel, comparer, and offer/answer operation in success and failure modes.
- [DONE] T162: Add public API tests covering every implementation constructor, option default, result object, diagnostic/issue/difference payload, throwing exception path, and result-based expected-failure path.
- [DONE] T163: Add public API tests covering all public static registry/token fields and every enum-backed known/unknown token conversion behavior through functional operations.
- [DONE] T164: Add mutation-isolation and thread-concurrent read/parse/write/validate tests proving no shared mutable global state and no cross-operation data leakage.
- [DONE] T165: Add malformed-input corpus tests for truncation, invalid UTF-8, control characters, extreme decimal text, duplicate storms, deep attribute lists, reference cycles, limit exhaustion, and diagnostic redaction.
- [DONE] T166: Run the complete test suite with coverage collection and fill every uncovered branch in public API behavior, including error, limit, unknown-extension, and profile-specific branches.
- [DONE] T167: Perform a final requirements trace from every feature in this file and every public API operation to at least one positive, negative, boundary, and round-trip test where applicable.

## Completion criteria

- All T001–T167 tasks are closed.
- The public API follows the `syntax.md` organization and syntax constraints and uses no external runtime libraries.
- Every RFC 8866 construct and arbitrary extension data parses into a non-lossy representation.
- RFC 8122 and RFC 8839 typed behavior, validation, generation, and high-level helpers are complete.
- The advertised RTP/WebRTC/ICE/DTLS/extmap/group/BUNDLE/RID/simulcast/SCTP/BFCP feature set is covered by conformance and interoperability tests.
- Canonical output is deterministic; lossless output preserves accepted unchanged input; expected bad remote input is reportable without exceptions.
- Every operation exposed through `src/Api` has complete success, failure, boundary, and interaction coverage appropriate to its contract.
