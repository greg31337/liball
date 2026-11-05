# Release status and policy

This document is the release-facing capability statement for the current source tree. It separates
implemented public behavior from the broader target in [Standards and conformance](conformance.md).
The [conformance test matrix](conformance-matrix.md) records test traceability; neither a completed
backlog item nor a passing test by itself establishes support for an end-to-end public operation.

Status terms used below are:

- **Supported**: the public operation is wired end to end and covered by behavioral tests.
- **Component**: parsing, validation, modeling, rendering, or authoring building blocks exist, but
  the generic high-level operation does not necessarily compose all of them.
- **Inspection only**: syntax can be bounded and exposed, but cannot be decoded, rendered, or
  regenerated.
- **Not supported**: the operation is outside the current implementation or project boundary.

## Standards conformance statement

The library's container syntax, structural validation, metadata handling, and AV1 syntax components
are designed against the frozen standards editions in [conformance.md](conformance.md). The 75
explicit AVIF 1.2 assertions and the AV1 decoder requirement families are mapped to focused tests in
[conformance-matrix.md](conformance-matrix.md). Strict validation is intended to enforce the
applicable container and carriage requirements, while compatible and permissive modes only relax
documented recoverable nonconformance; resource, arithmetic, essential-feature, and protection
checks remain mandatory.

The project does **not** currently make a blanket claim of AVIF or AV1 decoder/encoder conformance.
In particular, the public `AvifCodec.Decode`, `AvifCodec.Encode`, `Av1Decoder`, and `Av1Encoder` paths do not
decode or encode normative AV1 frame and tile data. They exchange a library-private planar payload
identified by `AVIFRAW1` inside an AV1 padding OBU. This is useful for deterministic managed
round trips and exercising the surrounding container and rendering APIs, but it is not an
interoperable AV1 coded frame. Files produced by the current encoder must not be represented or
distributed as standards-conforming, interoperable AVIF files.

The repository contains tested AV1 parsing, semantic validation, prediction, reconstruction,
filtering, film-grain, and encoder-decision components. Those component tests are evidence for the
specific component named by each test, not evidence that the public codec path can decode or encode
an arbitrary conforming AV1 bitstream. Likewise, local success does not constitute certification by
the Alliance for Open Media, ISO, or any other standards body. Differential tests that require
external tools are skipped when those tools are unavailable; consult the test result's skipped-test
count before treating a run as interoperability evidence.

## Supported-feature matrix

| Area | Status | Current scope |
| --- | --- | --- |
| Runtime and dependencies | Supported | Managed .NET 10 library with no native or third-party production dependency; CI builds and tests Windows, Linux, and macOS on x64 and Arm64. |
| Probe and bounded box inspection | Supported | AVIF brand probing, ISO box trees, deterministic dumps, bounded reads, diagnostics, and exposure of unknown boxes. |
| HEIF item container | Supported | `meta`, item information and locations, properties, references, entity groups, primary-item selection, and minimized-image expansion are parsed, modeled, and structurally validated. |
| Unfragmented image sequences | Supported | Movie, track, sample-table, timing, grouping, dependency, alternate, and auxiliary relationships are parsed and validated. Payload decode is subject to the private-codec limitation above. |
| Fragmented media | Inspection only | Generic `moof` and `traf` box nesting can be inspected, but fragment sample resolution, fragmented decode, and fragmented authoring are not implemented. |
| Validation policies | Supported | Strict, compatible, and permissive structural validation, stable result codes, contextual diagnostics, encoded-payload syntax checks, and configurable resource limits. Pixel validation is subject to codec support. |
| AV1 OBU and header handling | Supported | Low-overhead and length-delimited OBU parsing, sequence/frame/tile syntax, operating-point and layer selection, metadata OBUs, and semantic validation. |
| Normative AV1 frame decode | Not supported | Public decode recognizes only the private `AVIFRAW1` padding-OBU payload; ordinary coded frame/tile payloads return an unsupported or decode failure result. |
| Normative AV1 frame encode | Not supported | Public encode emits the private `AVIFRAW1` padding-OBU payload, not a standards-conforming coded AV1 frame. Quality, rate-control, and preset settings therefore do not provide interoperable AV1 compression. |
| Planar image model | Supported | Caller-owned 8-, 10-, 12-, and 16-bit monochrome, 4:2:0, 4:2:2, and 4:4:4 planes; alpha, copying, views, bounds, and explicit cleanup. AV1 encoding from 16-bit planar storage is not supported. |
| Color and packed pixels | Supported | NCLX color resolution, full/limited range conversion, chroma resampling, alpha handling, and the declared 8-bit packed gray/RGB/BGR/RGBA/BGRA/ARGB/ABGR/RGB565 formats. |
| ICC, HDR, and gain maps | Component | Bounded ICC parsing and selected transforms, HDR signaling and metadata reconciliation, tone-map metadata, gain-map generation/application, and rendering components. Unsupported ICC forms remain diagnosable or preservable rather than silently interpreted. |
| Exif, XMP, and opaque metadata | Supported | Bounded parse, attachment, extraction, preservation, and authoring, including unknown metadata where safe. |
| Grids, layers, auxiliary images, tone maps, and sample transforms | Component | Container modeling, validation, component rendering, and authoring APIs exist. The generic `DecodeItem` path does not automatically render every derived, transform, or auxiliary relationship. |
| Still and collection authoring | Component | Canonical item-container construction, metadata, grids, layers, derived items, and validation are implemented around the private encoded-payload format. Output is not interoperable until normative AV1 encode is wired in. |
| Timed authoring | Component | Track/sample tables, timing, repetition, alternatives, and auxiliary tracks are authored around private managed sample payloads; output is not an interoperable AV1 image sequence. |
| Rewrite and remux | Supported | Bounded item-container rewriting, canonicalization, and safe preservation of selected unknown, metadata, and protected structures without generation-loss re-encoding. Fragment regeneration is not supported. |
| Incremental input | Supported | A contiguous source can be appended in chunks with `NeedMoreData`, missing-range, completion, and limit reporting. Bytes are buffered in memory; this is not random-access or asynchronous I/O. |
| Protected or encrypted media | Inspection only | Protected structures may be identified and safely preserved opaquely. Decryption, encryption, keys, licenses, clear-payload validation, and protection regeneration are not supported. |

## Known limitations

- The private `AVIFRAW1` payload is the principal release limitation. It prevents interoperable
  public AV1/AVIF decode and encode even though container and AV1 algorithm components are present.
- The one-call decode path selects a single primary result. Stateful APIs expose more of the
  container model, but callers must explicitly compose some collection, progressive, derived,
  transform, gain-map, and auxiliary workflows.
- Fragmented ISO BMFF media is not resolved into samples and cannot be decoded or authored.
- Protected content cannot be decrypted or validated below its opaque encrypted boundary. A rewrite
  is rejected when protection-related addressing or associations cannot be preserved safely.
- Unknown essential boxes or properties, unsupported profiles, ambiguous dependencies, and graph
  cycles fail the dependent operation. Unknown non-essential data can be exposed and may be
  preserved when byte ranges and relationships remain valid.
- Incremental input is contiguous and buffered. Cancellation is cooperative at bounded checkpoints,
  not a hard deadline or preemptive interruption.
- The production target is .NET 10. Older target frameworks and environments outside the tested
  Windows, Linux, and macOS x64/Arm64 matrix are not supported.
- Deterministic mode promises repeatability only for the same library version, input, options, and
  relevant runtime environment. Exact encoded or rewritten bytes are not a cross-version wire
  compatibility guarantee unless a specific API says otherwise.
- External-codec and external-container differential tests require separately installed tools and
  report as skipped when those tools are absent.

## Security guidance

Treat every media file and metadata packet as untrusted input:

- Keep `AvifSecurityLimits` enabled and reduce its defaults to the application's needs. Important
  bounds include source bytes; box depth/count; item, property, reference, track, sample, frame, and
  tile counts; dimensions and pixels; metadata, ICC, Exif, and XMP bytes; dependency and recursion
  depth; decoded and temporary bytes; and worker count. A zero or negative limit is invalid, not
  unlimited.
- Prefer strict validation and leave encoded-payload validation enabled before decode, render,
  rewrite, or publication. Check the returned `AvifResultCode` and diagnostics; parse success alone
  is not semantic or pixel conformance.
- Treat `LimitExceeded` as a policy decision, not a signal to retry automatically with unbounded
  values. Applications needing firm wall-clock or process-memory isolation should also use an
  operating-system process boundary because cancellation is cooperative.
- Use separate stateful readers, writers, encoders, decoders, and rewriters for concurrent work.
  Do not concurrently mutate a single instance. Call `Close()` on owned library objects; closing a
  library object does not close a caller-owned stream.
- Result byte arrays are independent copies, while image views may share plane storage with their
  owner. Do not use a shared view after closing or replacing the owning image.
- Preservation is not sanitization. Unknown boxes, properties, metadata, and opaque protected bytes
  can carry sensitive or hostile content. Disable the corresponding `AvifRewriteOptions.Preserve*`
  settings or remove data explicitly when building a sanitizer.
- Do not interpret an accepted unknown non-essential structure as trusted. Never accept an unknown
  essential structure for an operation that depends on its semantics.
- Keep the .NET runtime and this library updated. For a suspected vulnerability, report it privately
  to the maintainers with the library revision, platform, active limits, result code, diagnostics,
  and the smallest redistributable reproducer. Avoid publishing a weaponized file before a fix is
  available.

## Versioning and compatibility policy

Published packages will use [Semantic Versioning 2.0](https://semver.org/). Until a `1.0.0` release,
the API is pre-stable: a minor release may contain breaking changes, which must be called out in its
release notes. Patch releases are intended for compatible bug, documentation, and security fixes,
but may tighten validation or reject malformed, ambiguous, or unsafe input that an earlier version
accepted.

After `1.0.0`:

- A **patch** release preserves documented public API and behavior while fixing defects. Security or
  standards-correctness fixes may make invalid input fail more strictly.
- A **minor** release adds compatible APIs, formats, or standards coverage. Deprecation should
  precede removal by at least one minor release when practical.
- A **major** release may change public signatures, ownership or lifecycle rules, default semantics,
  the minimum target framework, or another documented compatibility contract.

Published numeric result-code and enum values are never reused for a different meaning or
renumbered within a major version. New values may be appended, so callers must tolerate values they
do not yet recognize. Unknown non-essential file structures remain the forward-compatibility path;
unknown essential structures continue to fail operations that require them.

The exact output byte sequence is not a stable cross-version format unless an API explicitly makes
that promise. Semantically equivalent canonicalization, metadata ordering, encoder decisions, or
diagnostics may change in a compatible release. Any change to the frozen standards editions must be
recorded in [conformance.md](conformance.md), accompanied by a review of
[conformance-matrix.md](conformance-matrix.md), and identified in release notes.
