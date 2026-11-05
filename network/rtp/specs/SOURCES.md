# Specification and implementation research sources

Retrieved on 2026-08-27. RFC text files are authoritative copies from the RFC Editor. The library-reference snapshots are research inputs only; they are not source dependencies and no code is copied from them.

## Primary scope

- `rfcs/rfc3550.txt` — RTP and RTCP packet formats, sequence validation, reception statistics, reports, source state, collision handling, and RTCP scheduling: <https://www.rfc-editor.org/rfc/rfc3550.txt>
- `rfcs/rfc4585.txt` — AVPF early feedback timing and RTPFB/PSFB packet formats: <https://www.rfc-editor.org/rfc/rfc4585.txt>
- `rfcs/rfc5761.txt` — RTP/RTCP single-port multiplexing and packet discrimination: <https://www.rfc-editor.org/rfc/rfc5761.txt>
- `rfcs/rfc8285.txt` — one-byte, two-byte, and mixed RTP header extensions: <https://www.rfc-editor.org/rfc/rfc8285.txt>
- `errata/rfc3550-errata.html`, `errata/rfc4585-errata.html`, `errata/rfc5761-errata.html`, and `errata/rfc8285-errata.html` — current RFC Editor errata pages.

## Companion standards

- `rfcs/rfc2198.txt` — redundant RTP audio data, used to define a generic redundancy/FEC integration boundary.
- `rfcs/rfc3551.txt` — RTP/AVP payload-type and profile constants.
- `rfcs/rfc3611.txt` — RTCP Extended Reports and unknown-block extensibility.
- `rfcs/rfc4571.txt` — length framing for RTP/RTCP over connection-oriented transports.
- `rfcs/rfc4588.txt` — RTP retransmission payload format and original-sequence-number recovery.
- `rfcs/rfc5104.txt` — codec-control feedback including FIR, TMMBR, TMMBN, and temporal-spatial trade-off messages.
- `rfcs/rfc5109.txt` — generic RTP payload FEC, used to define FEC hooks and metadata.
- `rfcs/rfc5506.txt` — reduced-size RTCP packets.
- `rfcs/rfc6051.txt` — rapid RTP synchronization and NTP-64 header extension.
- `rfcs/rfc6222.txt` — recommended SSRC identifier generation.
- `rfcs/rfc6464.txt` and `rfcs/rfc6465.txt` — client-to-mixer and mixer-to-client audio-level header extensions.
- `rfcs/rfc7160.txt` and `rfcs/rfc7164.txt` — multiple clock rates and leap-second behavior.
- `rfcs/rfc8083.txt` — RTP circuit-breaker guidance and observable session conditions.
- `rfcs/rfc8108.txt` — reporting on multiple RTP streams in one RTCP compound packet.
- `rfcs/rfc8843.txt` and `rfcs/rfc8852.txt` — MID, RID, and repaired-RID header extensions.
- `rfcs/rfc8860.txt` — multiple media types in one RTP session.
- `rfcs/rfc8888.txt` — standardized congestion-control feedback format.

All companion RFCs were retrieved from `https://www.rfc-editor.org/rfc/rfcNNNN.txt` using the number in the filename.

## Live protocol registries

- `iana/rtp-parameters.txt` and `iana/rtp-parameters.xml` — IANA RTP payload, RTCP packet, SDES, RTPFB/PSFB FMT, and compact-header-extension registries: <https://www.iana.org/assignments/rtp-parameters/>
- `iana/rtcp-xr-block-types.txt` and `iana/rtcp-xr-block-types.xml` — IANA RTCP XR block-type registry: <https://www.iana.org/assignments/rtcp-xr-block-types/>

## Existing-library research

- `library-references/pion-rtp-readme.md` and `pion-rtp-files.json` — Pion RTP wire codec, sequencer, packetizer, payload codec, and header-extension inventory: <https://github.com/pion/rtp>
- `library-references/pion-rtcp-readme.md` and `pion-rtcp-files.json` — Pion RTCP packet codec inventory: <https://github.com/pion/rtcp>
- `library-references/pion-interceptor-readme.md` and `pion-interceptor-packages.json` — Pion report, NACK, jitter-buffer, statistics, congestion-feedback, and FEC processing inventory: <https://github.com/pion/interceptor>
- `library-references/jrtplib-overview.html` — JRTPLIB session, source table, collision handling, RTCP scheduling, and transport abstraction overview: <https://jrtplib.readthedocs.io/en/v3.11.0/>
- `library-references/pjmedia-rtp.html` and `pjmedia-rtcp.html` — pjmedia RTP/RTCP codecs, sequence state, and statistics APIs: <https://docs.pjsip.org/en/latest/api/generated/pjmedia/group/group__PJMED__RTP.html> and <https://docs.pjsip.org/en/latest/api/generated/pjmedia/group/group__PJMED__RTCP.html>
- `library-references/gstreamer-rtpmanager.html`, `gstreamer-rtpsession.html`, `gstreamer-rtpbin.html`, and `gstreamer-rtpjitterbuffer.html` — GStreamer session, SSRC/payload demultiplexing, synchronization, jitter buffering, retransmission, and FEC integration: <https://gstreamer.freedesktop.org/documentation/rtpmanager/>
