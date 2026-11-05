# Specification and comparison-library reference set

This directory contains the primary inputs used to plan the library. The files are local snapshots for implementation and test-case research; normative behavior should be checked against the RFC status and errata pages when implementation begins.

## Core specifications

- `rfcs/rfc8866.txt` — current SDP base specification.
- `rfcs/rfc8122.txt` — connection-oriented TLS, `setup`, `connection`, and certificate fingerprint signaling.
- `rfcs/rfc8839.txt` — ICE SDP offer/answer procedures and attributes.

## Related SDP and real-time-media specifications

- Core negotiation and RTP: RFC 2198, 3264, 3389, 3551, 3605, 3611, 3890, 4145, 4585, 4588, 4733, 5109, 6184, 7004, 7587, 7741, 7798, 8035, 8627, and 9628.
- Generic and source-specific SDP extensions: RFC 4570, 4574, 4796, 5576, 5888, 6236, and 7273.
- Security: RFC 4568, 5763, 8122, and 8842.
- WebRTC/ICE/media extensions: RFC 7587, 7741, 7941, 8285, 8445, 8830, 8838, 8839, 8841, 8851, 8852, 8853, 8858, 8859, 8864, and 9143.
- BFCP: RFC 8856.
- `rfcs/rfc4566.txt` is retained as the historical predecessor to RFC 8866 because several comparison libraries and interoperability fixtures still cite it.

## Registries

- `registries/sdp-parameters.xml` — IANA SDP Parameters registry snapshot.
- `registries/rtp-parameters.xml` — IANA RTP Parameters registry snapshot.

## Existing-library references

- `library-references/sdp-transform/` — JavaScript parser/writer README, grammar, parser, writer, and license.
- `library-references/libsdptransform/` — C++ port README, grammar, and license.
- `library-references/pjsip/` — PJSIP SDP data/parsing and offer/answer negotiation headers plus license.
- `library-references/gstreamer/` — GStreamer SDP public header plus license.

The comparison sources are research references, not dependencies, and no code from them is planned for inclusion under `src/`.
