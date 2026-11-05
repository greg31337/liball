# Standards and conformance

This document fixes the interpretation of the project scope. It is a target for completed work, not
a claim that the current code implements every item below. The authoritative current implementation
claim, including the public codec interoperability limitation, is
[Release status and policy](release-status.md). A completed task in [`todo.md`](../todo.md) records
work performed but does not by itself establish end-to-end feature support.

## Frozen standards baseline

The baseline was frozen on 2026-08-17. An undated reference to one of these documents means the
specific edition listed here, not a later web revision:

| Area | Baseline |
| --- | --- |
| AVIF | Alliance for Open Media, *AV1 Image File Format (AVIF)* v1.2.0 |
| AV1 in ISOBMFF | Alliance for Open Media, *AV1 Codec ISO Media File Format Binding* v1.3.0 |
| AV1 bitstream | Alliance for Open Media, *AV1 Bitstream & Decoding Process Specification* v1.0.0 with Errata 1 |
| Base media format | ISO/IEC 14496-12:2026 |
| HEIF | ISO/IEC 23008-12:2025, including Amd 1:2025 for tone-map derivation and Amd 2:2026 for the low-overhead image format |
| MIAF | ISO/IEC 23000-22:2025 |
| Gain maps | ISO 21496-1:2025 |
| Color signaling | ITU-T H.273 (07/2024) |
| ICC | ICC.1:2022 (profile version 4.4), while accepting the explicitly supported ICC v2 forms |
| Exif | CIPA DC-008-Translation-2026, Exif 3.1, plus the 2026 Exif metadata for XMP mapping |
| XMP | Adobe XMP Specifications Parts 1 (2012), 2 (2022), and 3 (2020) |
| Media type and codecs parameter | The `image/avif` IANA registration and RFC 6381 |
| HDR10+ in AV1 | Alliance for Open Media, *Carriage of HDR10+ Metadata in AV1* snapshot retrieved 2026-08-17 |

[`specs/README.md`](../specs/README.md) records the exact local snapshots, official locations, and
which ISO editions require licensed text. The official edition named above governs when a public
historical draft or amendment sample differs from it. Published errata that are explicitly part of
the named edition apply; a later edition, amendment, draft, or living-page change requires an
intentional baseline update here before it changes required behavior.

[`conformance-matrix.md`](conformance-matrix.md) maps every explicit AVIF 1.2 assertion and each AV1
decoder requirement family in this baseline to behavioral tests.

The precedence for overlapping requirements is: the AVIF specification for an AVIF profile or file,
the AV1-in-ISOBMFF binding for AV1 carriage, AV1 for bitstream decoding, and the referenced ISO base
media/HEIF/MIAF standards for container behavior. A more specific requirement overrides a general
one. Contradictions that cannot be resolved by that rule must produce a documented diagnostic and a
new baseline decision; they must not be silently guessed in code.

## Project vocabulary

The following terms have fixed meanings in documentation, diagnostics, and public APIs:

- **Parse** reads bounded syntax into a model. It does not imply semantic validity or pixel decode.
- **Inspect** parses and exposes the available container, item, track, property, metadata, and OBU
  model without requiring pixels to be decoded.
- **Validate** evaluates the selected conformance policy and returns deterministic findings. A
  successful parse alone is not a successful validation.
- **Decode** runs the normative AV1 decoding process and produces planar samples. **Render** applies
  derived-image operations, color conversion, transforms, gain maps, and output packing to decoded
  samples.
- **Encode** creates a conforming AV1 bitstream from input samples. **Author** packages encoded data
  and metadata as a new AVIF file. **Rewrite** changes an existing container without generation-loss
  AV1 re-encoding unless the operation explicitly requests encoding.
- **Recognized** means syntax can be identified and bounded. **Supported** means the requested
  operation is implemented and participates in validation. **Preserved** means original bytes and
  relationships can be emitted unchanged when their ranges and dependencies remain valid. These
  terms are independent: recognized or preserved data is not necessarily decodable or authorable.
- **Conforming input** satisfies every applicable requirement of the frozen baseline for its
  declared brands, profiles, essential properties, and requested operation. **Conforming output**
  satisfies those requirements without relying on permissive-reader behavior.
- **Unknown** means the baseline provides no implemented semantics for the construct. An unknown
  essential construct prevents the dependent operation; an unknown non-essential construct may be
  exposed and preserved.
- **Unsupported** is a known feature outside an implemented capability or the boundary declared in
  this document. It is distinct from **invalid** (contradicts the applicable syntax or semantics),
  **truncated** (required bytes are absent), and **limit exceeded** (validity was not established
  because configured resource bounds stopped processing).
- **Strict**, **compatible**, and **permissive** are validation policies. Strict enforces every
  applicable baseline requirement. Compatible permits only documented, interoperable deviations
  and reports each one. Permissive continues past recoverable nonconformance where bounds and
  dependency semantics remain unambiguous, but still reports it. No policy may bypass memory,
  arithmetic, recursion, range, essential-feature, or protection checks.

## Conformance targets

At project completion, the decoder target is every normative AVIF 1.2 still-image and timed-image
form carried by the frozen container baseline, and every legal AV1 Main, High, and Professional
profile combination at 8, 10, and 12 bits, including monochrome, 4:2:0, 4:2:2, and 4:4:4. This
includes image items and collections, grids and derived images, auxiliary alpha/depth images,
progressive and layered images, gain maps and tone-map derivations, sample transforms, and AV1 image
sequences. Unknown non-essential data remains inspectable and is preserved by rewrite when safe.

The encoder/authoring target covers the same advertised sample formats and AVIF forms, with
conforming lossless and lossy still images and conforming timed sequences. The encoder may implement
a bounded set of search strategies; encoder conformance concerns the emitted bitstream and the
documented effect of every advertised setting, not exhaustive use of the AV1 design space.

Conformance is established at four separate layers:

1. Bounded syntax: sizes, offsets, counts, arithmetic, allocations, and byte/bit reads remain within
   the supplied input and configured resource limits.
2. Structural semantics: boxes, items, properties, references, tracks, samples, OBUs, dependency
   graphs, brands, and profiles obey the frozen container and AV1 carriage rules.
3. Decode/render correctness: decoded planes match the normative AV1 process and rendered pixels
   match the selected color, metadata, derivation, transform, and packing rules.
4. Authored interoperability: generated files validate against this model and decode in independent
   current implementations used by the conformance tests.

Passing one layer does not imply passing a later layer. Tests under `tests/` use boundary and
malformed vectors, licensed or redistributable conformance corpora, independent codec comparisons,
and writer round trips. External codecs are test tools only and are never production dependencies.
Unsupported features, malformed data, ambiguous dependencies, cycles, truncation, and exhausted
resource limits must fail gracefully with stable result codes and contextual diagnostics; they must
not be treated as partial success.

Cross-platform conformance applies to every .NET 10 runtime architecture supported by the library.
Optimized or intrinsic paths must retain a managed scalar fallback with identical externally visible
results wherever the operation is specified as exact or deterministic.

## Protected and encrypted media boundary

Decryption, encryption, key discovery, license acquisition, digital-rights-management integration,
and authoring of protected media are outside the AVIF rendering scope of this project. This boundary
includes ISO Common Encryption and any other protection scheme that prevents access to the clear AV1
sample, image item, derivation dependency, or metadata required by the requested operation.

Protected structures may be parsed, identified, inspected, and preserved as opaque data when all of
their byte ranges and relationships can be handled without interpreting ciphertext. Encountering
protection is not by itself a malformed-file result. An operation that needs protected cleartext,
including decode, pixel validation, metadata interpretation, dependency reconstruction, or a rewrite
that would require regenerating protection metadata, returns the explicit unsupported-protection
result with a diagnostic identifying the affected item, track, sample, or range.

The library never accepts caller keys and never invokes a platform or external decryptor. It also
does not claim the encrypted payload is AV1-conforming, because that cannot be established from
ciphertext. Opaque preservation is permitted only when the rewrite can keep the protected bytes and
all protection-relevant addressing and associations valid; otherwise the rewrite is rejected rather
than emitting a plausibly corrupt or unintentionally unprotected file.
