# Public API audit

This record documents the public `raw` API audit against the
responsibility-based shape in `todo.md`, including the interface-boundary
review completed on 2026-08-22 and the category-based source organization
adopted on 2026-08-24.

## Surface mapping

- `IRawCodec` and its default `RawCodecService` implementation provide the
  canonical byte-array, span, bounded-stream, path, and document workflows,
  selected raw processing stages, generic descriptor presets and geometry,
  CFA utilities, exact rational operations, and region-based generic RAW decode
  into caller-owned stored or normalized buffers as an instance dependency.
  `IRawWriter` and `RawWriterService` provide canonical generic RAW dry runs,
  in-memory encoding, caller-owned stream encoding, and validated staged path
  output for every stored and normalized input form, alongside CR2 and NEF
  planning, encoding, and preserving rewrites. `IRawCodec` also exposes
  cancellable bounded-memory and exact caller-owned-stream sidecar reads plus
  deterministic byte-array and stream writes with schema and resource-limit
  enforcement. Opened-document thumbnail selection and bounded payload
  extraction, baseline-JPEG preview decode, and deterministic eight-bit sRGB
  gray/RGB preview encoding are available through the same instance contract.
  The overload-heavy
  static codec, writer, renderer, schema, and sidecar helpers are internal
  implementation conveniences rather than public API types.
- Descriptor and image responsibilities are implemented by `RawDescriptor`,
  `RawImageInfo`, `RawStoredImage`, `RawSensorImage`, `RawRgbImage`, and their
  supporting `Raw*` model types.
- TIFF and metadata responsibilities are exposed through `RawIfd`, `RawTag`,
  `RawTagValue`, `RawRational`, `RawMetadata`, and the packet, maker-note, and
  edit models. Parser, layout, TIFF, JPEG, and codec implementation types remain
  internal.
- The planned profile-database responsibility is named `RawProfileCatalogue`.
  It implements the read-only `IRawProfileCatalogue` consumer contract and the
  mutable `IRawProfileRegistry` registration contract. CR2 and NEF profile selection,
  evidence qualification, and write-plan completion depend on that narrow
  contract rather than catalogue storage and JSON registration details.
  `RawOpenOptions` and `RawMetadataOptions` accept an optional catalogue for
  container profile matching and otherwise retain the built-in-profile default.
  The planned camera-profile responsibility is split into `RawReadProfile` and
  `RawWriteProfile`, and the planned match responsibility is represented by
  `RawProfileMatchInput`, `RawProfileMatchCandidate`, and
  `RawProfileMatchResult`. These names preserve the planned responsibilities
  while making read/write direction and result roles explicit.
- Options, limits, reports, diagnostics, capabilities, and direct exception
  classes are present in the single public `raw` namespace. Public type source
  is organized under `src/Api` by contract, data, enum, exception, and
  interface-implementation category; implementation helpers remain outside
  that tree.

## Interface boundary review

Interfaces are used only at boundaries where implementations are expected to
vary:

- `IRawCodec` is a coarse-grained application seam. Transport-suffixed methods
  expose byte-array, transient-span, bounded caller-owned-stream, and path
  inputs without interface overloads, while opened documents remain the stable
  decode, render, and validation core.
- `IRawWriter` is the corresponding write seam. Its uniquely named methods
  avoid overload ambiguity while retaining generic RAW memory, stream, path,
  and dry-run operations and canonical CR2/NEF planning, encoding, and
  rewriting.
- `IRawProfileCatalogue` is a read-only dependency of writer selection and
  planning. Registration is separated into `IRawProfileRegistry`, keeping
  mutation out of read-only writer dependencies while allowing the concrete
  catalogue to support controlled loading.
- TIFF/JPEG codecs, format decoders, and rendering stages remain concrete and
  use explicit enum/format dispatch. They are fixed in-process algorithms, and
  adding one interface per stage would enlarge the architecture without a
  current substitution requirement.

The instance contracts are the supported public operation surface. Former
public static entry points remain available only inside the assembly and to the
friend test assembly; this is an intentional source break required by the
instance-only public API policy.

## Testing impact

The instance codec and writer contracts let application-level tests replace
complete read and write workflows without file-system fixtures or static
interception. The profile contract also lets writer planning use a small
in-memory fake that returns one read/write profile and records lookup calls,
avoiding schema serialization, registration precedence, and shared catalogue
setup when those behaviors are not under test. Separate catalogue tests still
cover the concrete registration and concurrency implementation.

## Format boundaries

`RawDocument` exposes format-neutral identity, images, thumbnails, metadata,
payloads, capabilities, and diagnostics. Optional CR2 header and Canon/Nikon
profile inspection results are grouped under `RawFormatDetails`; they are no
longer separate vendor fields on the generic document model. Explicit CR2/NEF
write inputs and maker-note models remain format-specific by design.

## XML documentation

Every public type, enum member, field, constructor, and method is included in
the generated package XML documentation. The documentation build treats
`CS1591` as an error, and the generated XML uses only the documentation
constructs allowed by `syntax.md`.
