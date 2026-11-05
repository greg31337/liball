# T119 security review

This record covers the T119 review completed on 2026-08-20. The review treats
all raw containers, metadata, profile documents, output paths, and public model
mutations as untrusted input.

## Size and offset arithmetic

Parser and writer byte ranges use `CheckedMath` before conversion, allocation,
slicing, or output planning. TIFF and BigTIFF offsets are converted to signed
positions before adding their container base, and bounded ranges use
`length <= boundary - offset` after nonnegative offset checks. TIFF validation,
payload-map validation, layout planning, codec sizes, and writer change ranges
reject overflow rather than wrapping.

The review also replaced quadratic payload-overlap and opaque-range ordering
with ordered range sweeps/sorts. This keeps validation and preservation work
bounded for high, caller-permitted tag and block counts. Payload paths must now
be nonempty and unique within each semantic kind so byte-exact verification
cannot resolve an ambiguous opaque identity.

## XML and structured metadata

XMP uses a document-conformance `XmlReader` with DTD processing prohibited, a
null resolver, character checking, and configured document/depth/field limits.
Encoded input, its worst-case decoded UTF-16 string, and the depth-tracking
arrays are charged against `MaxAllocationBytes`. XMP declarations and external
entities remain prohibited. Profile and sidecar JSON use bounded buffers,
serializer depth limits, duplicate-name rejection, and post-read model
validation.

## Allocation and decompression limits

File buffering, tag values, previews, decoded samples, codec expansion,
histograms, render buffers, and writer output pass through configured file,
metadata, decoded-byte, pixel, and single-allocation limits before allocation.
Compression codecs require an expected decoded size or a bounded maximum and
reject trailing, truncated, or expanding output that violates that contract.
The XMP accounting gap found during this review was corrected and covered by a
regression test.

## Destination and temporary-file semantics

Path writers normalize a destination in an existing directory, create a
random sibling with `CreateNew`, deny sharing while writing, flush content to
stable storage, validate both exact bytes and the reopened raw container, and
only then replace the destination. On Unix, staged files are explicitly
created with owner read/write permission only. A validation failure leaves the
owner-only sibling path in `RawPathWriteResult` for deliberate recovery; a
successful commit leaves no sibling file.

The destination directory is a caller-selected trust boundary. A principal
that can rename arbitrary entries in that directory can interfere with any
path-based commit protocol and must not be treated as untrusted co-tenancy.

## Opaque data and authenticity

Preserving rewrites require `RawUnknownDataPolicy.Preserve`, a valid bounded
payload map, retained source bytes, and fully loaded metadata/previews. Unknown
islands, raw blocks, previews, maker notes, authenticity blocks, and supported
private values are range-checked before copying. Reopened output is compared
against every byte-exact mapped payload, and raw/pixel identity checks remain
independently configurable. Authenticity material is never fabricated; edits
that invalidate it produce an explicit diagnostic.

## Tolerant recovery

Only named recovery diagnostics may downgrade a failure. The sole current
recovery omits a malformed optional CR2 IFD1 JPEG thumbnail. Structural
suppression is now limited to the two recovered interchange tags rather than
the entire IFD, so unrelated corruption in IFD1 remains an error. Strict mode
or validation without explicit recovery consent promotes recovered conditions
to errors.

## Verification

The regression suite covers hostile offsets, overflow, cycles, overlaps,
decompression/allocation limits, external entities, malformed XML, metadata
limits, truncation, cancellation, ambiguous opaque paths, staged-file
permissions, and recovery scoping. No source-syntax, project-file, or
documentation conformance tests were added for T119.
