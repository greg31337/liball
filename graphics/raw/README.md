# raw

`raw` is a dependency-free .NET library for inspecting, decoding, rendering,
editing, and writing headerless sensor data, Canon CR2 files, and Nikon NEF
files. The implementation is being developed incrementally; the compatibility
levels below describe the v1 contract, not the feature set of the current
scaffold.

## Public identity

- NuGet package ID: `raw`
- Assembly name: `raw`
- Public C# namespace: `raw`

Applications should reference the package by its package ID and import the
`raw` namespace. Public APIs will remain in that namespace; format-specific
details will be represented by API types rather than additional public root
namespaces.

The completed [public API audit](public-api.md) maps the implementation to the
planned responsibilities and records the format-specific boundary.

For dependency-injected applications, `IRawCodec` exposes the canonical
bounded-memory, opened-document, processing, and generic descriptor workflow,
with `RawCodecService` as the default stateless implementation. `IRawWriter`
and `RawWriterService` provide the corresponding generic RAW, CR2, and NEF
planning and write workflow, including validated staged CR2 rewrites. Other
overload-heavy path and stream conveniences remain internal so the public API
stays within the instance-only API policy.

Writer profile selection accepts the read-only `IRawProfileCatalogue`
contract. `RawProfileCatalogue` implements that contract together with the
mutable `IRawProfileRegistry` registration contract. The read-only contract can
also be supplied on `RawOpenOptions` or `RawMetadataOptions` when reads should
use a caller-managed profile source.

```csharp
IRawCodec codec = new RawCodecService();
RawDocument document = codec.Open(bytes, descriptor, new RawOpenOptions());
RawSensorImage image = codec.Decode(document, new RawDecodeOptions());

IRawWriter writer = new RawWriterService();
byte[] encoded = writer.EncodeGenericUnsigned(descriptor, samples, 0, rowStride, new RawGenericEncodeOptions());
```

The completed [security review](security-review.md) records the arithmetic,
XML, allocation, staged-file, opaque-copy, and tolerant-recovery threat review.

## Supported .NET platforms

The library targets .NET 10 (`net10.0`). It is supported on any operating
system and architecture for which Microsoft supports a compatible .NET 10
runtime. Production code uses only .NET built-ins and does not require a
native imaging library, platform-specific graphics API, or external codec.

The v1 package does not target .NET Framework, Mono, earlier .NET target
frameworks, browser/WASM environments, Native AOT, or mobile platforms as
separately tested compatibility targets. Those environments may work when
they can consume `net10.0`, but they are not part of the v1 support promise.

## Stream ownership

Every `Stream` supplied to the library remains owned by the caller. The
library never disposes or closes a caller-owned input or output stream,
including when an operation succeeds, fails validation, is cancelled, or
throws. The caller is responsible for keeping the stream open for as long as
deferred reads or writes can use it and for disposing it afterward.

An operation may advance a stream's position. APIs that require seeking will
state that requirement and reject a non-seekable stream when a bounded
streaming path is unavailable. The library does not promise to restore the
original position. Buffers, streams, and temporary storage created internally
by the library remain library-owned.

## V1 compatibility contract

Headerless `.raw` input is supported only when the caller supplies an exact
descriptor containing the required geometry, sample representation, layout,
and byte-order information. File length or the `.raw` extension alone is
never treated as authoritative. Descriptor inference, where available,
returns explicit candidates and never silently selects an ambiguous layout.

CR2 and NEF are profile-driven format families. Support for one camera,
compression, or layout does not imply support for another. Each opened file
reports its capabilities independently using these levels:

| Level | Contract |
|---|---|
| Recognized | The container or signature and basic identity can be reported safely. |
| Inspected | The container graph, standard metadata, private block locations, previews, and raw payload descriptions can be enumerated. |
| Decoded | Raw samples can be reconstructed with a known layout, compression, channel or CFA interpretation, and level/area metadata. |
| Rendered | The decoded data can be converted into a documented displayable or linear output. |
| Preserving rewrite | Supported edits can be written while unknown/private data and undecoded payloads are preserved according to the write report. |
| Encoded | A new payload and container can be emitted for a named, validated write profile. |
| Vendor-confirmed | The output has passed this project's structural checks and the named independent or vendor tools recorded by the profile. |

A lower support level never implies a higher one. In particular, successful
decode does not imply that new-file encoding is safe. Unsupported compression
or an unknown camera profile produces an explicit capability result and
diagnostic; the library does not guess pixels or silently substitute a codec.
Unknown metadata is exposed as typed or opaque data where possible and is
preserved by default during a preserving rewrite.

Non-experimental CR2 and NEF encoding is enabled only for named write profiles
backed by corpus evidence and independent compatibility checks. Experimental
writers additionally require explicit caller opt-in. Lossless writers must
reopen their output and verify pixel identity. Any lossy write requires an
explicit opt-in and reports the irreversible operation. Nikon HE/HE* payloads
are recognized for metadata and preview access in v1, but raw decoding and
encoding remain unavailable. The completed
[implementation-gate review](specs/nikon-he-legal-review.md) found no currently
cleared specification and patent-licensing basis for an in-tree codec.

Version 1.0.0 ships descriptor-driven generic RAW support as the stable path.
Every built-in CR2 and NEF read or write profile remains experimental,
requires explicit caller opt-in, and reports `VendorConfirmed` as false. This
keeps the available corpus-backed implementations usable without presenting
missing camera-origin or independent-application evidence as a broader
compatibility claim.

Inputs are parsed within caller-configurable resource limits. Malformed,
truncated, oversized, ambiguous, and unsupported input returns deterministic
diagnostics rather than partial pixels presented as a successful decode.
Preserving rewrites retain unknown/private blocks by default and report any
data that must be relocated, re-encoded, invalidated, or dropped before a
write is committed.

## Conformance corpus

External and private conformance evidence is inventoried by the versioned
[corpus manifest](corpus/README.md). The manifest records exact hashes,
provenance, redistribution status, camera/firmware/variant labels, linked
profiles, expected capabilities, and explicit placeholders for private files.

The [T117 release-readiness record](release-readiness.md) documents the
cross-platform build/test gate, concurrency and determinism checks, package
XML documentation, and managed-only dependency inspection. The final
[v1.0.0 release gate](v1-release.md) records profile evidence, capability
semantics, writer pixel identity, package verification, and known limits.
