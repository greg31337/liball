# Universal RTP/RTCP library development plan

## Goal and scope

Build a transport-agnostic, payload-agnostic .NET library that covers RTP/RTCP packet processing from strict wire codecs through per-source tracking, reports, feedback, buffering, repair integration, and a high-level session state machine. The normative baseline is RFC 3550, RFC 4585, RFC 8285, and RFC 5761. Companion RFCs in `specs/rfcs/` define profile constants and interoperable optional modules.

The library will:

- Decode, validate, preserve, modify, size, and encode RTP packets, every RFC 3550 RTCP packet, compound RTCP, AVPF feedback, and unknown future RTCP/extension data.
- Track 16-bit sequence and 32-bit timestamp spaces across wraparound, restart, duplication, reordering, multiple clock rates, and SSRC changes.
- Calculate RFC-defined packet loss, fraction lost, extended highest sequence, interarrival jitter, sender counters, LSR/DLSR, and RTT.
- Manage multiple local and remote SSRCs, CSRCs, CNAME associations, collisions, loops, membership, sender state, timeouts, BYE, and RTCP scheduling.
- Support RFC 8285 one-byte and two-byte extension elements, mixed-format policy, ID-to-URI mappings, common standardized extension payloads, and lossless unknown extensions.
- Support regular, early, immediate, and reduced-size feedback; feedback suppression; Generic NACK, PLI, SLI, RPSI, application feedback, and common later standardized feedback formats.
- Provide bounded reordering/jitter buffering, packet history, loss notifications, NACK generation, RFC 4588 retransmission adaptation, and transport/media-neutral retransmission and FEC hooks.
- Accept inbound datagrams and produce data/outbound-control decisions synchronously. Networking, threading, timers, media encoders/decoders, SDP parsing, and security transports remain application concerns.

## Non-negotiable implementation constraints

- `src/` uses only built-in .NET APIs; no external package or native dependency is permitted there.
- Span-based read/write APIs are preferred for wire data. Public data objects own arrays so their lifetime is explicit; codecs must not retain caller spans.
- All public declarations live under `src/Api` and follow `syntax.md`: contracts in `Contracts`, field-based models in `Data`, constants-only classes in `Static`, enums in `Enums`, direct `System.Exception` subclasses in `Exceptions`, and interface implementations in `Classes`.
- Public APIs have no properties, overloads, generic declarations, delegates, events, public static methods, or nullable value types. Operation names are unique within each interface and across every interface implemented by one class.
- All public members receive the XML comments required by `syntax.md` as part of their implementation; this plan has no standalone documentation work.
- Expected parse/capacity failures expose both throwing and exception-less methods with distinct names. Programming/configuration errors use exceptions.
- Integers used for sizes and counts are signed. Unsigned integers are reserved for RTP/RTCP bit fields, identifiers, sequence/timestamp spaces, and NTP values.
- No task adds syntax-policy/source-checking tests, performance tests, benchmarks, packaging, release work, or user documentation.

## Standards profile and compatibility boundary

### Normative baseline

- RFC 3550: RTP fixed header, CSRC list, padding, profile extension envelope, RTP validity checks, RTCP SR/RR/SDES/BYE/APP, compound validation, source state, statistics, interval calculation, reconsideration, collision/loop handling, and translator/mixer-relevant metadata.
- RFC 4585: RTPFB and PSFB common headers; Generic NACK, PLI, SLI, RPSI, AFB; regular/early/immediate feedback rules, dithering, suppression, and AVPF compound packets.
- RFC 8285: one-byte profile `0xBEDE`, two-byte profile family `0x1000`, padding, reserved IDs, element length rules, mixed-format policy, and lossless handling of profile-specific extensions.
- RFC 5761: RTP/RTCP discrimination on one port, forbidden RTP payload-type range 64–95 when multiplexing, and conservative rejection of ambiguous/malformed datagrams.

### Companion modules included in the plan

- RFC 3551 AVP payload-type constants; RFC 5506 reduced-size RTCP; RFC 3611 RTCP XR and unknown XR blocks.
- RFC 4588 RTX adaptation; RFC 2198 and RFC 5109 metadata/hooks for redundancy and FEC without embedding a media-specific FEC algorithm.
- RFC 5104 common codec-control feedback and RFC 8888 congestion-control feedback.
- RFC 6051 NTP-64, RFC 6464/6465 audio levels, and RFC 8843/8852 MID/RID/RRID extension payload helpers.
- RFC 6222 SSRC generation, RFC 7160 clock-rate changes, RFC 7164 leap-second-safe timestamp correlation, RFC 8108 multi-stream reports, and RFC 8860 multiple media types in a session.
- RFC 4571 length framing as an optional transport adapter, while UDP and connection ownership stay outside the library.
- RFC 8083 observable circuit-breaker inputs and status are surfaced; policy for terminating an application remains with the caller.
- Verified RFC Editor errata are applied where they affect packet behavior. In particular, the RFC 5761 RSI collision is payload type 81; the RFC 4585 verified ABNF corrections are recorded but do not add SDP parsing; held RFC 3550 algorithm clarifications are resolved against its appendices and interoperability vectors.

### Explicitly outside the built-in implementation

- Payload-specific packetizers/depacketizers for codecs such as Opus, H.264, H.265, VP8, VP9, or AV1. Applications plug these in through payload interfaces.
- A concrete parity/XOR/Reed-Solomon/FlexFEC algorithm. The core transports the metadata and invokes repair hooks.
- SRTP/SRTCP, DTLS, ICE, STUN/TURN, SDP offer/answer parsing, RTSP, SIP, sockets, DNS, multicast membership, and congestion-control algorithms.
- Non-standard REMB, absolute-send-time, absolute-capture-time, playout-delay, video-layer-allocation, and draft transport-wide feedback receive lossless raw support but no first-class typed API in the baseline.

## Research findings applied to the design

| Library | Relevant feature set found | Design consequence |
| --- | --- | --- |
| [Pion RTP](https://github.com/pion/rtp), [Pion RTCP](https://github.com/pion/rtcp), and [Pion Interceptor](https://github.com/pion/interceptor) | Separate RTP/RTCP codecs; packetizer/sequencer; typed header extensions; SR/RR, NACK, PLI/SLI/RPSI/FIR, XR, congestion feedback; pluggable reports, jitter buffer, NACK, stats, and FEC processors. | Keep wire objects independent from stateful processing, preserve unknown formats, and expose small hook interfaces around a useful built-in pipeline. |
| [JRTPLIB](https://jrtplib.readthedocs.io/en/v3.11.0/) | High-level session, transport abstraction, participant/SSRC table, incoming queues, automatic RTCP scheduling, collision management, BYE, and lower-level reusable components for mixers/translators. | Make the high-level session transport-neutral and synchronous, but keep codec, scheduler, source, and statistics components usable independently. |
| [pjmedia RTP](https://docs.pjsip.org/en/latest/api/generated/pjmedia/group/group__PJMED__RTP.html) and [RTCP](https://docs.pjsip.org/en/latest/api/generated/pjmedia/group/group__PJMED__RTCP.html) | Compact RTP encode/decode, sequence probation/dropout/misorder state, SR/RR models, loss/jitter/RTT statistics, feedback construction, and update functions driven by caller timestamps. | Offer deterministic state-machine APIs that receive explicit timing metadata and return snapshots/decisions without owning threads. |
| [GStreamer RTP manager](https://gstreamer.freedesktop.org/documentation/rtpmanager/) | Multi-SSRC sessions, SR/RR scheduling, RTP/RTCP demux, SSRC/PT demux, RTCP-based inter-stream synchronization, reorder/jitter buffer, late/lost notifications, RTX queues, NACK retry policy, and auxiliary retransmission/FEC elements. | Separate session, demux, jitter-buffer, history, and repair responsibilities; support multiple streams and return explicit loss/repair/session events. |

Downloaded research inputs and exact URLs are inventoried in `specs/SOURCES.md`.

## Complete feature inventory

### 1. Binary and error-handling foundation

- Network-byte-order reads/writes for 8/16/24/32/64-bit fields, signed 24-bit cumulative loss, four-byte alignment, bounded slicing, and overflow-safe length arithmetic.
- Exact encoded-length calculation before writes; no partial output on failed `Try...` operations.
- Throwing and exception-less encode/decode paths with consumed/written byte counts stored in result data rather than `out` parameters.
- Strict and tolerant decoding modes: strict rejects standard violations; tolerant preserves unknown packet types, feedback FMTs, XR blocks, and extension profiles without accepting unsafe lengths.
- Configurable packet, compound-packet, extension, source-count, queue, and history limits to bound memory use on hostile input.

### 2. RTP packet codec

- Version, padding, extension, marker, payload type, sequence number, timestamp, SSRC, 0–15 CSRC identifiers, optional extension envelope, payload, and padding.
- Validation of version 2, CSRC count, extension word length, packet minimum/maximum length, nonzero valid padding count, and payload boundaries.
- Lossless round trip of unknown extension profiles and payload bytes; optional preservation or normalization of padding bytes.
- Span encode/decode, encoded-size query, array convenience results, and safe repeated reuse with no retained caller buffer.
- Packet construction with cryptographically generated initial sequence/timestamp/SSRC values and monotonic sequence advancement.

### 3. RTP header extensions

- RFC 3550 raw extension envelope for any 16-bit profile.
- RFC 8285 one-byte elements: IDs 1–14, data lengths 1–16, zero padding, reserved ID 15 termination, and malformed-tail rejection.
- RFC 8285 two-byte elements: IDs 1–255, lengths 0–255, appbits/profile preservation, padding, and malformed-tail rejection.
- Automatic smallest-valid-format selection, explicit format selection, mixed one/two-byte policy, deterministic ordering, duplicate-ID policy, and four-byte padding.
- ID/URI mapping registry that is supplied by signaling or application code; no built-in SDP parser.
- Typed helpers for NTP-64, client and mixer audio levels, MID, RID, and repaired RID, with raw fallback for every other URI.
- Safe add, replace, remove, find, and enumerate operations represented without public indexers, properties, or method overloads.

### 4. RTCP packet and compound codecs

- Common header validation: version, padding, count/FMT, packet type, length-in-words, packet boundaries, and padding only where legal.
- Sender Report with NTP timestamp, RTP timestamp, sender packet/octet counters, and 0–31 reception report blocks.
- Receiver Report with 0–31 reception report blocks.
- Reception report signed 24-bit cumulative loss, fraction lost, extended highest sequence, jitter, LSR, and DLSR.
- SDES with multiple chunks; CNAME, NAME, EMAIL, PHONE, LOC, TOOL, NOTE, PRIV, unknown item preservation, END marker, and alignment.
- BYE with multiple sources, optional reason, and alignment; APP with subtype, SSRC, four-character name, and opaque application data.
- Compound packet iteration, size calculation, encoding, and RFC 3550 validation: SR/RR first, required CNAME, legal ordering, only final-packet padding, and aggregate limits.
- RFC 5506 reduced-size mode and RFC 4585 minimal feedback compounds as explicit validation/build modes rather than silently weakening compound rules.
- Unknown RTCP packet preservation and round trip so new packet types do not require a core release.
- Optional RFC 4571 two-byte frame length codec for incremental connection-oriented input and output.

### 5. RTCP feedback codecs

- Common RTPFB/PSFB sender SSRC, media-source SSRC, FMT, and FCI handling with unknown FMT preservation.
- RFC 4585 Generic NACK PID/BLP packing and unpacking, canonical grouping, duplicate elimination, and 16-bit wrap-aware missing-sequence expansion.
- PLI empty-FCI validation; SLI first/macroblock-count/picture-ID entries; RPSI padding-bit count, payload type, and native bit string; opaque AFB.
- RFC 5104 FIR sequence entries; TMMBR/TMMBN exponent, mantissa, overhead entries; temporal-spatial trade-off request/notification; rapid resynchronization request.
- RFC 8888 congestion-control report timestamps, begin sequence, report blocks, ECN/loss/arrival-time symbols, and padding.
- Lossless raw support for non-standard or future feedback formats such as REMB and transport-wide CC.

### 6. RTCP Extended Reports

- RFC 3611 XR packet and block framing, block-specific length validation, and unknown-block preservation.
- Loss and duplicate RLE, packet receipt times, receiver reference time, DLRR sub-blocks, statistics summary, and VoIP metrics.
- Hooks for later registered XR blocks without changing the compound codec.

### 7. Sequence, timestamp, and clock tracking

- RFC 3550 Appendix A.1 probation, maximum dropout, maximum misorder, bad-sequence restart confirmation, duplicate/reorder detection, wrap cycles, base sequence, and extended sequence number.
- Configurable thresholds with RFC defaults and an explicit status result for probation, accepted, duplicate, reordered, large jump, restart, and invalid input.
- Sender sequencer with secure randomized start, wrap-safe advancement, and explicit restart/reset behavior.
- 32-bit RTP timestamp unwrapping, forward/backward delta classification, discontinuity/reset handling, and 64-bit extended timeline.
- Payload-type clock-rate registry and RFC 7160 clock-rate change handling without comparing incomparable timestamp units.
- NTP 64-bit and compact-NTP conversions, system-clock injection, monotonic elapsed-time use for intervals, wall-clock correlation for SR, and leap-second-safe behavior.
- SR-derived RTP/NTP correlation and conversion of RTP timestamps to a common presentation timeline for inter-stream synchronization.

### 8. Reception and transmission statistics

- Per-SSRC received-valid, duplicate, reordered, discarded, late, repaired, and recovered packet/octet counters.
- Expected packet count from extended sequence range; cumulative loss with signed 24-bit report clamping; interval expected/received deltas and fraction lost.
- RFC 3550 interarrival jitter estimator `J = J + (|D| - J) / 16` in RTP timestamp units, using caller-supplied monotonic arrival time and the active clock rate.
- Last SR compact timestamp, SR arrival time, DLSR generation, RR/SR arrival tracking, and RTT calculation with invalid/negative sample rejection.
- Sender packet/octet counts, last RTP timestamp, last send time, and SR eligibility; octet count excludes RTP headers and padding.
- Immutable-by-convention snapshot data returned as independent copies so caller mutation cannot corrupt tracker/session state.
- Circuit-breaker observables for sustained timeout/loss/RTT conditions without imposing application shutdown policy.

### 9. RTCP report construction and scheduling

- SR versus RR selection based on sender state, sender info generation, reception-block selection, and splitting more than 31 report blocks across packets/compounds.
- CNAME inclusion and rotating optional SDES items within the SDES bandwidth share.
- RFC 3550 member/sender bandwidth split, initial interval, minimum interval, randomized interval, compensation factor, average compound-size EWMA, reconsideration, reverse reconsideration, sender timeout, member timeout, and BYE backoff.
- Injectable clock/random sources and explicit `Poll`-style decisions for deterministic applications and tests; no internal timer or background thread.
- AVPF regular/early/immediate feedback modes, `trr-int`, early-feedback allowance, dithering, suppression on equivalent received feedback, piggybacking, and feedback bandwidth accounting.
- Full compound, minimal feedback compound, and negotiated reduced-size packet construction.

### 10. Jitter buffer, loss detection, and repair integration

- Per-SSRC bounded reorder buffer keyed by extended sequence number; duplicate removal and wrap-safe ordering.
- Configurable maximum latency, capacity, dropout/misorder window, late policy, and discontinuity reset.
- Release of contiguous packets; expiry of gaps as loss; late/recovered distinction; flush and drain operations; occupancy/statistics snapshots.
- Missing-sequence tracking with NACK delay, retry interval, retry count/period, cancellation after arrival, and canonical Generic NACK generation.
- Bounded sent-packet history by age/count/bytes for retransmission lookup, with exact SSRC/sequence matching across wrap.
- RFC 4588 wrap/unwrap adapter for OSN, RTX SSRC, RTX payload type, restored original SSRC/payload type, and separate RTX sequence tracking.
- Repair request/result data and `IRtpRepairEncoder`/`IRtpRepairDecoder` hooks for application retransmission, RFC 2198 redundancy, RFC 5109 FEC, or other schemes.
- Repaired packets re-enter normal validation/statistics once, while repaired/duplicate/late counters remain distinguishable.

### 11. RTP/RTCP demultiplexing and session management

- RFC 5761 single-port classifier using version and the marker/PT octet against registered RTCP packet types, with payload types 64–95 forbidden in mux mode.
- Separate-port mode, mux mode, and auto-classification for diagnostics; ambiguous/truncated/invalid data returns unknown rather than guessing.
- Multiple local sending SSRCs and remote SSRCs; sources keyed independently of payload type while supporting multiple media types and clock rates.
- Source probation/activation, sender/member status, CSRC discovery, CNAME association, SDES updates, last activity, BYE state, inactivity expiry, and source snapshots.
- SSRC collision and loop detection using caller-supplied transport identity tokens, local SSRC reselection, sequence/timestamp reset, BYE generation for the old SSRC, and collision events.
- Synchronous ingress for RTP, RTCP, and muxed datagrams; outbound RTP preparation; RTCP/feedback polling; BYE request; reset; and snapshot operations.
- Session updates carry accepted/released RTP packets, outbound RTCP compounds, feedback/repair requests, source changes, collision/timeout/loss events, and recoverable input errors.
- Payload-type registry supplies clock rate/media identity/RTX association and enforces mux-safe assignments; signaling updates can be applied between packets.
- Packetizer/depacketizer contracts allow media modules to split/assemble payloads while the session owns RTP headers, sequence numbers, timestamps, marker decisions, and statistics.
- Thread-safe state transitions using explicit `lock` blocks; callbacks and application code are never invoked while a session lock is held.

### 12. Robustness and interoperability

- Every decoder is safe against truncation, oversized advertised lengths, count/length mismatch, arithmetic overflow, invalid padding, unknown types, and hostile nesting/counts.
- Exact boundary behavior at sequence/timestamp wrap, 24-bit loss limits, report count 31, CSRC count 15, extension element maximums, RTCP length maximum, and RFC 5761 payload boundaries.
- Deterministic byte-for-byte golden vectors from RFC examples and cross-implementation fixtures from Pion where formats overlap.
- Round-trip invariants for all typed and unknown packets/extensions, compound packets, randomized valid models, and normalized encodings.
- All stateful algorithms are testable with fake clock/random implementations and reproducible simulated packet traces.

## Layering and dependency direction

| Layer | Responsibility | May depend on |
| --- | --- | --- |
| 1. Wire | Binary primitives, RTP/RTCP/header-extension/frame codecs | Built-in .NET only |
| 2. Stream | Sequence/timestamp tracking, stats, jitter buffer, history | Wire and public data/contracts |
| 3. Control | Report builder, RTCP scheduler, feedback scheduler | Wire and stream |
| 4. Session | Source database, collision handling, mux ingress, outbound decisions | Wire, stream, and control |
| 5. Integration | Payload and repair hooks implemented by applications | Public contracts/data only |

Dependencies point downward. The wire layer has no session assumptions; the session does not own sockets, threads, signaling, codecs, or cryptographic contexts.

## Proposed public API and source files

Names below are provisional but their locations and roles are part of the plan. Each file contains one public type. Data classes expose fields only and may contain internal helper methods; implementation classes expose only one constructor and methods declared by their interface.

### `src/Api/Enums`

- `RtpPacketKind.cs` — unknown, RTP, or RTCP classifier result.
- `RtpSequenceStatus.cs` — probation, accepted, duplicate, reordered, large jump, restarted, or invalid.
- `RtpSourceStatus.cs` — probationary, active member, active sender, inactive, BYE, or collision state.
- `RtpHeaderExtensionFormat.cs` — raw RFC 3550, RFC 8285 one-byte, or RFC 8285 two-byte.
- `RtpSessionTransportMode.cs` — separate RTP/RTCP or RFC 5761 multiplexed operation.
- `RtpSessionEventType.cs` — source, loss, repair, collision, timeout, BYE, report, and recoverable-error event kinds.
- `RtpJitterBufferStatus.cs` — buffered, released, duplicate, late, lost, overflow, or reset.
- `RtpRepairKind.cs` — retransmission, redundancy, FEC, or application-defined repair.
- `RtcpPacketType.cs` — registered packet types used by the codecs and demultiplexer.
- `RtcpSdesItemType.cs` — END, CNAME, NAME, EMAIL, PHONE, LOC, TOOL, NOTE, PRIV, and unknown.
- `RtcpFeedbackPacketType.cs` — transport-layer or payload-specific feedback.
- `RtcpTransportFeedbackFormat.cs` — Generic NACK and standardized transport feedback formats.
- `RtcpPayloadFeedbackFormat.cs` — PLI, SLI, RPSI, FIR, AFB, and standardized payload feedback formats.
- `RtcpFeedbackTimingMode.cs` — regular, early, immediate, or reduced-size emission mode.
- `RtcpExtendedReportBlockType.cs` — registered RFC 3611 block types plus unknown.

### `src/Api/Static`

- `RtpProtocolConstants.cs` — version, header sizes, field limits, and RFC sequence defaults.
- `RtpAvpPayloadTypes.cs` — RFC 3551 static payload-type and clock-rate constants.
- `RtpHeaderExtensionProfiles.cs` — RFC 8285 profile identifiers/masks.
- `RtpHeaderExtensionUris.cs` — standardized extension URI strings included in this plan.
- `RtcpProtocolConstants.cs` — packet types, header/report sizes, interval defaults, and count limits.
- `RtcpFeedbackFormats.cs` — RTPFB/PSFB FMT constants.
- `RtcpExtendedReportBlockTypes.cs` — XR block numeric constants.

### `src/Api/Exceptions`

- `RtpPacketFormatException.cs` and `RtcpPacketFormatException.cs` — malformed wire data; each derives directly from `System.Exception`.
- `RtpConfigurationException.cs` — invalid payload, extension, clock, scheduler, or mux configuration.
- `RtpSessionException.cs` — invalid session state transition or unrecoverable session operation.
- `RtpBufferCapacityException.cs` — throwing form of bounded output/history/jitter-buffer capacity failure.

### `src/Api/Data`: RTP wire and extension models

- `RtpPacket.cs`, `RtpHeaderExtension.cs`, and `RtpHeaderExtensionElement.cs` — complete RTP wire model and extension elements.
- `RtpCodecResult.cs` and `RtpDatagramClassification.cs` — exception-less codec/demux result metadata.
- `RtpHeaderExtensionMapping.cs` — negotiated ID, URI, format capability, and attributes.
- `RtpAudioLevelExtension.cs`, `RtpMixerAudioLevelExtension.cs`, `RtpNtp64Extension.cs`, and `RtpStreamIdentifierExtension.cs` — typed standardized extension payloads.
- `RtpPayloadTypeDefinition.cs` — payload type, clock rate, media identity, channels, and RTX association supplied by signaling.

### `src/Api/Data`: RTCP wire models

- `RtcpHeader.cs`, `RtcpPacket.cs`, `RtcpCompoundPacket.cs`, and `RtcpCodecResult.cs` — common header, discriminated packet container, compound container, and exception-less result.
- `RtcpSenderInformation.cs`, `RtcpSenderReport.cs`, `RtcpReceiverReport.cs`, and `RtcpReceptionReport.cs` — SR/RR fields and report blocks.
- `RtcpSourceDescription.cs`, `RtcpSourceDescriptionChunk.cs`, and `RtcpSourceDescriptionItem.cs` — complete SDES structure.
- `RtcpGoodbye.cs`, `RtcpApplicationDefined.cs`, and `RtcpUnknownPacket.cs` — BYE, APP, and future packet preservation.
- `RtcpFeedbackPacket.cs`, `RtcpGenericNack.cs`, `RtcpGenericNackEntry.cs`, `RtcpPictureLossIndication.cs`, `RtcpSliceLossIndication.cs`, `RtcpSliceLossEntry.cs`, `RtcpReferencePictureSelectionIndication.cs`, and `RtcpApplicationFeedback.cs` — RFC 4585 feedback.
- `RtcpFullIntraRequest.cs`, `RtcpFullIntraRequestEntry.cs`, `RtcpBitrateControl.cs`, `RtcpBitrateControlEntry.cs`, `RtcpTemporalSpatialTradeoff.cs`, and `RtcpRapidResynchronizationRequest.cs` — RFC 5104 feedback.
- `RtcpCongestionControlFeedback.cs` and `RtcpCongestionControlReport.cs` — RFC 8888 feedback.
- `RtcpExtendedReport.cs`, `RtcpExtendedReportBlock.cs`, `RtcpRunLengthChunk.cs`, and `RtcpDelaySinceLastReceiverReport.cs` — RFC 3611 typed/unknown XR model.

### `src/Api/Data`: state, statistics, scheduling, and integration models

- `RtpPacketContext.cs` — arrival time, transport identity, repaired flag, and media-clock input accompanying a packet.
- `RtpSequenceUpdate.cs`, `RtpTimestampUpdate.cs`, and `RtpClockCorrelation.cs` — tracking results and RTP/NTP correlation.
- `RtpReceptionStatisticsSnapshot.cs`, `RtpTransmissionStatisticsSnapshot.cs`, and `RtpRemoteReportSnapshot.cs` — independent statistics snapshots.
- `RtpSourceSnapshot.cs` and `RtpSessionSnapshot.cs` — externally visible source/session state.
- `RtpSessionConfiguration.cs`, `RtcpSchedulerConfiguration.cs`, and `RtpJitterBufferConfiguration.cs` — field-based validated configuration.
- `RtcpScheduleDecision.cs`, `RtpFeedbackRequest.cs`, and `RtpFeedbackDecision.cs` — regular/feedback scheduling inputs and outputs.
- `RtpJitterBufferUpdate.cs`, `RtpRepairRequest.cs`, and `RtpRepairResult.cs` — buffer releases/losses and repair integration.
- `RtpSessionEvent.cs` and `RtpSessionUpdate.cs` — high-level ingress/poll results.
- `RtpPacketizationResult.cs` and `RtpDepacketizationResult.cs` — payload-hook results without delegates or generic types.

### `src/Api/Contracts`

- `IRtpPacketCodec.cs`, `IRtcpPacketCodec.cs`, `IRtpHeaderExtensionCodec.cs`, and `IRtpKnownHeaderExtensionCodec.cs` — wire sizing, throwing encode/decode, and exception-less encode/decode operations with unique method names.
- `IRtpRtcpDemultiplexer.cs` and `IRtpStreamFramingCodec.cs` — RFC 5761 datagram classification and RFC 4571 framing.
- `IRtpPayloadTypeRegistry.cs` and `IRtpHeaderExtensionRegistry.cs` — signaling-supplied mapping operations.
- `IRtpSequenceTracker.cs`, `IRtpTimestampTracker.cs`, `IRtpReceptionStatistics.cs`, and `IRtpTransmissionStatistics.cs` — independently reusable stream state.
- `IRtcpReportBuilder.cs`, `IRtcpScheduler.cs`, and `IRtcpFeedbackScheduler.cs` — report and timing decisions.
- `IRtpJitterBuffer.cs` and `IRtpPacketHistory.cs` — bounded receive/send buffering.
- `IRtpClock.cs` and `IRtpRandomSource.cs` — injectable time and randomness.
- `IRtpPayloadPacketizer.cs` and `IRtpPayloadDepacketizer.cs` — application media-payload hooks.
- `IRtpRetransmissionAdapter.cs`, `IRtpRepairEncoder.cs`, and `IRtpRepairDecoder.cs` — RFC 4588 adaptation and retransmission/redundancy/FEC hooks.
- `IRtpSession.cs` — synchronous high-level RTP, RTCP, mux, polling, BYE, reset, and snapshot operations.

### `src/Api/Classes`

- `RtpPacketCodec.cs`, `RtcpPacketCodec.cs`, `RtpHeaderExtensionCodec.cs`, and `RtpKnownHeaderExtensionCodec.cs` — built-in codecs.
- `RtpRtcpDemultiplexer.cs` and `RtpStreamFramingCodec.cs` — built-in transport-neutral classifiers/framing.
- `RtpPayloadTypeRegistry.cs` and `RtpHeaderExtensionRegistry.cs` — validated registries.
- `RtpSequenceTracker.cs`, `RtpTimestampTracker.cs`, `RtpReceptionStatistics.cs`, and `RtpTransmissionStatistics.cs` — built-in tracking/statistics.
- `RtcpReportBuilder.cs`, `RtcpScheduler.cs`, and `RtcpFeedbackScheduler.cs` — built-in control-plane logic.
- `RtpJitterBuffer.cs`, `RtpPacketHistory.cs`, and `RtpRetransmissionAdapter.cs` — built-in buffering/history/RTX support; the adapter receives its own dedicated contract if needed to keep exposed methods valid.
- `SystemRtpClock.cs` and `SecureRtpRandomSource.cs` — built-in clock and RFC 6222-compatible random source.
- `RtpSession.cs` — high-level implementation of `IRtpSession` only.

### Internal implementation files outside `src/Api`

- `src/Internal/Binary/RtpBinaryReader.cs`, `RtpBinaryWriter.cs`, `RtpLength.cs`, and `RtpSigned24.cs` — checked wire primitives.
- `src/Internal/Validation/RtpPacketValidator.cs`, `RtcpPacketValidator.cs`, `RtcpCompoundValidator.cs`, `RtpConfigurationValidator.cs`, and `RtpApiCopy.cs` — strict validation and defensive copying.
- `src/Internal/Rtcp/RtcpPacketDecoder.cs`, `RtcpPacketEncoder.cs`, `RtcpFeedbackCodec.cs`, `RtcpExtendedReportCodec.cs`, and `RtcpCompoundBuilder.cs` — codec dispatch and construction internals.
- `src/Internal/State/RtpSourceState.cs`, `RtpSourceTable.cs`, `RtpLocalSourceState.cs`, `RtpCollisionTracker.cs`, `RtpMissingPacketState.cs`, and `RtpSessionState.cs` — non-public mutable state.
- `src/Internal/Timing/RtpNtpTime.cs`, `RtpClockMath.cs`, `RtcpIntervalCalculator.cs`, and `RtcpFeedbackIntervalCalculator.cs` — wrap-safe time arithmetic and scheduling.
- `src/Internal/Buffering/RtpPacketHeap.cs`, `RtpPacketHistoryEntry.cs`, and `RtpRepairCoordinator.cs` — bounded ordering/history/repair internals.

## Planned test files

- `tests/Wire/` — binary primitives; RTP; RFC 8285 extensions; known extension payloads; all RTCP packet/feedback/XR types; compound packets; RFC 4571; RFC 5761.
- `tests/Tracking/` — sequence, timestamp, NTP correlation, receive/transmit statistics, report blocks, and RTT.
- `tests/Control/` — report builder, baseline scheduler, reconsideration/BYE, AVPF feedback scheduling/suppression, and reduced-size behavior.
- `tests/Buffering/` — jitter buffer, missing packets, NACK retries, packet history, RTX, and repair hooks.
- `tests/Session/` — source lifecycle, multiple streams/media, collisions/loops, ingress/egress, polling, mux/separate modes, BYE, concurrency, and snapshots.
- `tests/Fixtures/` — RFC-derived wire vectors, malformed corpora, cross-implementation Pion vectors, deterministic traces, fake clock, fake random source, payload hooks, and repair hooks.
- `tests/Api/` — behavioral coverage of every public constructor/method, data-copy isolation, failure contracts, and end-to-end public-layer scenarios.

## Implementation tasks

### Foundation and API model

- [DONE] T001: Create the planned `src/Api` and internal directory structure and establish one library namespace without changing project dependencies.
- [DONE] T002: Implement checked network-order binary reader/writer, signed 24-bit conversion, four-byte alignment, and overflow-safe size arithmetic internals.
- [DONE] T003: Implement all protocol, AVP payload, extension, feedback, and XR constants as fields-only public static data classes.
- [DONE] T004: Implement the planned public enums with explicit `int32` values and ensure each enum is consumed by an interface or data model.
- [DONE] T005: Implement the five planned direct `System.Exception` subclasses with one constructor each and consistent error messages.
- [DONE] T006: Implement RTP packet, extension, codec-result, classification, payload-definition, and known-extension data classes with defensive-copy helpers.
- [DONE] T007: Implement RTCP common, SR/RR, SDES, BYE, APP, unknown, and compound data classes with defensive-copy helpers.
- [DONE] T008: Implement RFC 4585, RFC 5104, RFC 8888, and RFC 3611 feedback/XR data classes without public inheritance or discriminated-type patterns.
- [DONE] T009: Implement configuration, tracking-result, statistics-snapshot, scheduling, buffering, repair, packetization, session-event, and session-update data classes.
- [DONE] T010: Define the planned public contracts with unique non-overloaded instance method names and Span-based codec operations.
- [DONE] T011: Implement reusable configuration validation and copy-isolation internals so invalid public field combinations fail before state mutation.
- [DONE] T012: Implement `SystemRtpClock` and `SecureRtpRandomSource`, including monotonic elapsed time and RFC 6222-quality initial identifier generation.

### RTP wire codec and extension processing

- [DONE] T013: Implement strict RTP fixed-header decoding, including version, marker/PT, sequence, timestamp, SSRC, and 0–15 CSRC values.
- [DONE] T014: Implement RTP extension-envelope, payload, and padding decoding with exact boundary validation and unknown-profile preservation.
- [DONE] T015: Implement exact RTP encoded-size calculation and throwing Span encoding without retaining caller memory.
- [DONE] T016: Implement exception-less RTP encode/decode paths that report status and consumed/written counts without partial writes.
- [DONE] T017: Implement strict/tolerant RTP validation modes, configurable size limits, and normalized-versus-preserved padding behavior.
- [DONE] T018: Implement RFC 8285 one-byte extension parsing and encoding, including padding, reserved ID 15, element limits, and alignment.
- [DONE] T019: Implement RFC 8285 two-byte extension parsing and encoding, including appbits, zero-length elements, padding, limits, and alignment.
- [DONE] T020: Implement automatic/explicit extension format selection, mixed-format policy, ordering, duplicate handling, and add/replace/remove/find operations.
- [DONE] T021: Implement the header-extension mapping registry with validated IDs, URIs, negotiated capabilities, and immutable snapshot copies.
- [DONE] T022: Implement typed NTP-64, client/mixer audio-level, MID, RID, and repaired-RID extension payload codecs with raw fallback.
- [DONE] T023: Implement the payload-type registry with clock rates, media identities, RTX associations, signaling updates, and RFC 5761 mux-safe validation.
- [DONE] T024: Implement an RTP outbound packet builder inside the session path that applies source state, secure initial values, marker choice, extension mapping, and sequence/timestamp advancement.

### RTCP wire codecs

- [DONE] T025: Implement RTCP common-header decode/encode, packet sizing, version/count/type/word-length checks, legal padding, and unknown-packet preservation.
- [DONE] T026: Implement SR sender information and reception-report block codecs, including signed 24-bit cumulative loss and 31-block limits.
- [DONE] T027: Implement full Sender Report and Receiver Report codecs with exact length/count validation.
- [DONE] T028: Implement SDES item/chunk/packet codecs for all RFC 3550 item types, PRIV prefixes, unknown items, END markers, and alignment.
- [DONE] T029: Implement BYE codec for multiple SSRCs, optional UTF-8 reason, legal length, and padding.
- [DONE] T030: Implement APP codec for subtype, SSRC, four-byte name, opaque data, and alignment.
- [DONE] T031: Implement compound RTCP parsing/iteration with aggregate packet/byte limits and lossless typed/unknown packet dispatch.
- [DONE] T032: Implement RFC 3550 compound validation for first packet, CNAME, ordering, final-only padding, and BYE placement.
- [DONE] T033: Implement full, minimal AVPF, and RFC 5506 reduced-size compound construction/validation as explicit modes.
- [DONE] T034: Implement exact compound sizing plus throwing and exception-less Span encoding with atomic capacity failure.
- [DONE] T035: Implement RFC 4571 frame sizing, incremental decode, multiple-frame consumption, zero/oversize rejection, and encode operations.

### Feedback and extended reports

- [DONE] T036: Implement RTPFB/PSFB common feedback framing and preserve unknown FMT/FCI bytes.
- [DONE] T037: Implement Generic NACK PID/BLP decode, encode, canonical wrap-aware packing, expansion, sorting, and duplicate elimination.
- [DONE] T038: Implement PLI validation and SLI entry packing/unpacking with all field-width constraints.
- [DONE] T039: Implement RPSI padding/payload-type/native-bit-string processing and opaque AFB processing.
- [DONE] T040: Implement RFC 5104 FIR and rapid-resynchronization-request feedback codecs and sequence semantics.
- [DONE] T041: Implement RFC 5104 TMMBR/TMMBN exponent/mantissa/overhead and temporal-spatial trade-off feedback codecs.
- [DONE] T042: Implement RFC 8888 congestion-control feedback symbols, timestamps, sequence ranges, report blocks, and padding.
- [DONE] T043: Implement RFC 3611 XR packet/block framing and lossless unknown-block round trips.
- [DONE] T044: Implement XR loss/duplicate RLE and packet-receipt-time blocks, including run-length and bit-vector chunks.
- [DONE] T045: Implement XR receiver-reference-time, DLRR, statistics-summary, and VoIP-metrics blocks with exact length validation.

### Sequence, timestamp, clock, and statistics engines

- [DONE] T046: Implement RFC 3550 receive-sequence probation with configurable minimum sequential packets.
- [DONE] T047: Implement sequence wrap cycles, extended sequence numbers, dropout/misorder thresholds, duplicate/reorder classification, and bad-sequence restart confirmation.
- [DONE] T048: Implement sender sequence generation with secure initialization, wrap behavior, explicit restart, and independent state per local SSRC.
- [DONE] T049: Implement RTP timestamp unwrapping, forward/backward delta classification, discontinuity reset, and extended timestamp results.
- [DONE] T050: Implement payload clock-rate changes per RFC 7160 and prevent jitter/correlation arithmetic across incompatible clock domains.
- [DONE] T051: Implement full/compact NTP conversion, fixed-point DLSR conversion, wrap-safe differences, and leap-second-safe clock correlation.
- [DONE] T052: Implement SR-derived RTP/NTP clock correlation and common presentation-time conversion for synchronizing multiple streams.
- [DONE] T053: Implement reception counters and RFC expected/received/cumulative/interval loss calculations with signed 24-bit clamping.
- [DONE] T054: Implement RFC interarrival jitter calculation using explicit monotonic arrival time and payload clock rate.
- [DONE] T055: Implement LSR/DLSR tracking, remote SR/RR ingestion, RTT samples, and rejection of invalid or negative RTT observations.
- [DONE] T056: Implement sender packet/octet counters, timestamp/send-time tracking, SR eligibility, and immutable-by-convention snapshots.
- [DONE] T057: Implement late, duplicate, reordered, discarded, repaired, recovered, timeout, and circuit-breaker observable counters.

### Reports and RTCP scheduling

- [DONE] T058: Implement report-block generation from sequence/statistics state, including fraction-lost interval rollover and LSR/DLSR fields.
- [DONE] T059: Implement SR/RR selection and sender-information construction for each local SSRC.
- [DONE] T060: Implement report-block rotation/splitting when more than 31 remote sources must be reported and support RFC 8108 multi-stream compounds.
- [DONE] T061: Implement mandatory CNAME and bandwidth-aware rotation of optional SDES items.
- [DONE] T062: Implement baseline RFC 3550 RTCP interval calculation with sender/member bandwidth split, initial interval, minimum, randomization, and compensation.
- [DONE] T063: Implement average RTCP-size EWMA, membership/sender updates, reconsideration, and reverse reconsideration.
- [DONE] T064: Implement sender/member inactivity timeouts, BYE scheduling, reconsideration/backoff, and immediate small-session BYE behavior.
- [DONE] T065: Implement poll-driven regular compound report decisions using injected clock/random sources and no internal timer/thread.
- [DONE] T066: Implement AVPF regular/early/immediate timing, `trr-int`, feedback allowance, dithering, and bandwidth accounting.
- [DONE] T067: Implement equivalent-feedback detection and suppression, piggybacking, duplicate coalescing, and pending-feedback expiry.
- [DONE] T068: Implement construction of full, minimal, and negotiated reduced-size feedback compounds from scheduler decisions.

### Jitter buffering, retransmission, and FEC hooks

- [DONE] T069: Implement a bounded per-SSRC extended-sequence reorder store with duplicate removal and deterministic capacity eviction.
- [DONE] T070: Implement contiguous release, configurable latency/gap expiry, late/lost classification, discontinuity reset, flush, and drain.
- [DONE] T071: Implement jitter-buffer occupancy, release, duplicate, late, loss, overflow, and recovery snapshots/events.
- [DONE] T072: Implement missing-packet tracking with NACK delay, reorder grace, retry timeout/count/period, and cancellation after arrival.
- [DONE] T073: Connect missing-packet state to canonical Generic NACK feedback requests and AVPF scheduler coalescing/suppression.
- [DONE] T074: Implement bounded sent-packet history by packet count, byte count, and age, with SSRC/sequence lookup across wrap.
- [DONE] T075: Define and implement `IRtpRetransmissionAdapter` as the public contract for RFC 4588 OSN wrap/unwrap without violating implementation-class API rules.
- [DONE] T076: Implement RFC 4588 RTX packet creation/restoration with RTX SSRC/PT/sequence state and original packet identity.
- [DONE] T077: Integrate application `IRtpRepairEncoder` and `IRtpRepairDecoder` hooks for retransmission, RFC 2198 redundancy, RFC 5109 FEC, and application-defined repair.
- [DONE] T078: Route repaired packets through validation/statistics exactly once and retain distinct repaired, recovered, duplicate, and late outcomes.

### Demultiplexing, source database, and high-level session

- [DONE] T079: Implement the RFC 5761 RTP/RTCP classifier with version/type checks, mux payload restrictions, complete RTCP type awareness, and unknown results for ambiguity.
- [DONE] T080: Implement remote source probation, activation, sender/member changes, CSRC discovery, activity timestamps, and immutable source snapshots.
- [DONE] T081: Implement CNAME-to-SSRC association, SDES updates, BYE marking/removal, sender timeout, member timeout, and stale-state cleanup.
- [DONE] T082: Implement multiple independent local SSRC states and multiple remote streams/media types with clock-rate and payload mapping changes.
- [DONE] T083: Implement SSRC collision and loop detection from caller transport identity, old-SSRC BYE creation, secure reselection, and source-state reset.
- [DONE] T084: Implement separate-port RTP and RTCP ingress paths with decode, validation, source/statistics updates, jitter-buffer release, and session events.
- [DONE] T085: Implement muxed-datagram ingress that classifies once and routes through the same RTP/RTCP paths without duplicated state logic.
- [DONE] T086: Implement outbound RTP preparation and payload-packetizer integration with marker/timestamp/extension decisions and transmission statistics/history.
- [DONE] T087: Implement payload-depacketizer integration over released packets, discontinuity/loss indications, and independent media-hook results.
- [DONE] T088: Implement inbound RTCP handling for reports, SDES, feedback, XR, BYE, scheduler suppression, RTT, repair, and source state.
- [DONE] T089: Implement session polling for regular reports, early feedback, retransmission/FEC work, source expiry, and queued outbound compounds/events.
- [DONE] T090: Implement session BYE, flush, reset, configuration-update, and snapshot operations with atomic state transitions.
- [DONE] T091: Make every public state component and the high-level session safe for concurrent callers using explicit locks and no invocation of application hooks while locked.
- [DONE] T092: Enforce all configured packet/source/buffer/history/feedback limits across high-level ingress and return recoverable failure events without corrupting state.

### Wire and model test coverage

- [DONE] T093: Add exhaustive binary-primitive tests for endian operations, signed 24-bit limits, alignment, truncation, capacity, and overflow boundaries.
- [DONE] T094: Add RTP golden-vector tests covering every fixed-header bit, CSRC counts 0/15, payloads, padding, extension envelope, exact sizing, and round trips.
- [DONE] T095: Add malformed RTP tests for every truncation point, bad version, inconsistent counts/lengths, invalid padding, oversized input, and strict/tolerant differences.
- [DONE] T096: Add RFC 8285 one-byte tests for IDs/lengths at every boundary, padding placement, ID 15 termination, alignment, malformed tails, and round trips.
- [DONE] T097: Add RFC 8285 two-byte/mixed-policy tests for IDs/lengths at every boundary, appbits, zero-length elements, padding, selection, duplicates, and malformed tails.
- [DONE] T098: Add known-extension payload tests for NTP-64, audio levels, MID/RID/RRID, invalid values, registry mappings, and unknown URI/profile preservation.
- [DONE] T099: Add SR/RR/report-block golden and malformed tests for counts 0/31, signed loss extrema, lengths, padding, and all numeric fields.
- [DONE] T100: Add SDES tests for every item, multiple chunks, PRIV, UTF-8 limits, END/alignment, missing CNAME, unknown items, and malformed lengths.
- [DONE] T101: Add BYE, APP, and unknown RTCP tests for source counts, optional reason, names/subtypes, opaque data, padding, normalization, and lossless round trips.
- [DONE] T102: Add compound RTCP tests for legal full/minimal/reduced forms, ordering, CNAME, BYE, padding, aggregate limits, unknown packets, iteration, and every truncation boundary.
- [DONE] T103: Add RFC 4571 tests for partial frames, concatenated frames, zero/maximum/oversized lengths, incremental consumption, capacity failure, and RTP/RTCP contents.
- [DONE] T104: Add Generic NACK tests for PID/BLP packing, wraparound, sparse/dense loss sets, duplicates, canonical order, malformed FCI, and Pion-compatible vectors.
- [DONE] T105: Add PLI, SLI, RPSI, and AFB golden/malformed tests covering all field limits, padding bits, empty FCI, and unknown FMT preservation.
- [DONE] T106: Add RFC 5104 and RFC 8888 feedback tests for every message, exponent/mantissa limit, sequence/timestamp wrap, report symbol, padding, and malformed FCI.
- [DONE] T107: Add RFC 3611 XR tests for every supported block/chunk, unknown blocks, all legal edge lengths, alignment, and nested truncation/mismatch failures.
- [DONE] T108: Add randomized valid-model round-trip and single-byte truncation tests for RTP, extensions, each RTCP type, feedback, XR, and compound packets.

### Tracking, control, buffering, and session test coverage

- [DONE] T109: Add RFC Appendix A.1 sequence-trace tests for probation, duplicates, reorder, dropout, restart confirmation, wrap cycles, reset, and configurable thresholds.
- [DONE] T110: Add timestamp tests for wrap, backward reorder, discontinuity, multiple clock rates, rate changes, SR correlation, presentation conversion, and leap-second-safe behavior.
- [DONE] T111: Add loss/fraction/jitter tests using RFC calculations and deterministic traces, including no packets, burst loss, duplicates, reorder, wrap, signed 24-bit clamps, and changing intervals.
- [DONE] T112: Add sender-counter, LSR/DLSR, RTT, remote-report, timeout, repaired/recovered, and circuit-breaker observable tests with fake clocks.
- [DONE] T113: Add report-builder tests for SR/RR choice, sender information, CNAME, optional SDES rotation, report interval rollover, more than 31 sources, and multiple local streams.
- [DONE] T114: Add deterministic RFC 3550 scheduler tests for initial/regular intervals, bandwidth split, average size, randomization bounds, compensation, reconsideration, reverse reconsideration, timeouts, and BYE backoff.
- [DONE] T115: Add deterministic AVPF tests for regular/early/immediate modes, `trr-int`, dithering, bandwidth allowance, piggybacking, equivalent-feedback suppression, and reduced-size negotiation.
- [DONE] T116: Add jitter-buffer tests for reorder, duplicate, sequence wrap, gaps, latency expiry, late recovery, overflow eviction, flush/drain, discontinuity, and independent SSRCs.
- [DONE] T117: Add NACK retry tests for delay, reorder grace, retry cadence/count/period, cancellation, coalescing, feedback suppression, and loss finalization.
- [DONE] T118: Add packet-history and RTX tests for bounds/expiry, lookup across wrap, OSN restoration, RTX SSRC/PT/sequence mapping, duplicates, and missing originals.
- [DONE] T119: Add fake retransmission, redundancy, and FEC hook tests proving repair requests/results, re-entry, success/failure isolation, and exactly-once statistics.
- [DONE] T120: Add RFC 5761 classifier tests for every RTP marker/PT combination, RTCP types, payload boundaries 63/64/95/96, truncation, ambiguity, and separate/mux configuration.
- [DONE] T121: Add source-database tests for probation, RTP/RTCP/CSRC discovery, CNAME changes, sender/member state, BYE, inactivity, multiple media, and snapshot isolation.
- [DONE] T122: Add collision/loop tests for local/remote SSRC conflicts, transport identity changes, old-SSRC BYE, secure reselection, simultaneous sources, and state reset.
- [DONE] T123: Add end-to-end public-session tests for send-only, receive-only, bidirectional, separate-port, muxed, multiple-SSRC, multiple-clock-rate, feedback, repair, reports, BYE, and reset workflows.
- [DONE] T124: Add payload packetizer/depacketizer integration tests for marker/timestamp behavior, media loss/discontinuity input, hook failure isolation, and outbound/inbound statistics.
- [DONE] T125: Add concurrency stress tests for codecs, registries, trackers, scheduler, buffer/history, and session state, asserting no deadlock, torn snapshot, duplicate release, or hook invocation under lock.
- [DONE] T126: Add hostile-input corpus tests covering length bombs, count bombs, invalid nesting, malformed UTF-8 policy, random bytes, repeated unknown types, and configured resource limits.
- [DONE] T127: Generate and commit cross-implementation wire fixtures with Pion RTP/RTCP for every overlapping typed packet and assert byte-level decode/encode interoperability.
- [DONE] T128: Audit every public constructor and interface method into behavioral tests covering success, expected failure, capacity failure, reset/reuse, and caller-mutation isolation.
- [DONE] T129: Run the complete test suite with line and branch coverage collection and close every uncovered reachable branch in the `src/Api` behavior and its supporting protocol logic.
- [DONE] T130: Run final end-to-end simulated sessions with deterministic loss, jitter, reorder, wrap, feedback suppression, retransmission recovery, FEC-hook recovery, collision, timeout, and BYE traces and resolve all failures.

## Completion criteria

- Every feature in the inventory is implemented by a task above or explicitly identified as a hook/raw-preservation boundary.
- All public API types are in the required directory category, every implementation method comes from its interface, every data model is used by an interface or another data model, and every enum is used by an interface or data model.
- `src/` references only the .NET framework; any fixture generator or test helper dependency remains under `tests/`.
- Throwing and exception-less wire APIs agree, all configured resource limits are enforced, and unknown extensions/RTCP/feedback/XR data round-trip losslessly.
- The complete test suite passes and covers every reachable behavior exposed by `src/Api`, including malformed inputs, protocol boundaries, deterministic state traces, cross-implementation fixtures, and end-to-end session workflows.
