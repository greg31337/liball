# Versioning and compatibility

This policy defines what callers may rely on across releases. It covers the
library API and observable font-processing behavior; it does not version the
OpenType, Unicode, PDF, PDF/A, or PDF/UA specifications themselves.

## Release numbers

Tagged releases use Semantic Versioning (`MAJOR.MINOR.PATCH`). The project does
not currently declare a stable package version in `src/ttf.csproj`, so an
untagged repository build is a development snapshot rather than a compatibility
anchor.

Before 1.0, a minor release may contain a documented breaking change and a patch
release remains compatible within its minor line. Starting with 1.0:

- a **major** release may remove or change supported public API and may
  intentionally replace broad behavioral contracts;
- a **minor** release adds backward-compatible API or format support and may
  contain announced data/specification upgrades whose output changes are
  described below; and
- a **patch** release fixes correctness, interoperability, or security defects
  without intentionally breaking supported public signatures.

Correctness and security take priority over accepting malformed input or
reproducing an incorrect result. A patch can therefore reject an input that was
accepted only because validation was missing, stop an unsafe compatibility
repair, add a limit check, change an erroneous shape, or regenerate incorrect
output bytes. Such a change must be called out in release notes when it affects
a known supported workflow.

Repository commits, branch names, assembly timestamps, and the default version
assigned by the .NET SDK are not substitutes for a tagged release version.

## Unicode data

Runtime Unicode property data is pinned to Unicode 17.0.0 and to the input
fingerprint embedded in `src/Unicode/UnicodeData.bin`. The authoritative input
archive, hashes, and generator procedure are recorded in
`tests/Tools/UnicodeData/inputs.json` and
`tests/Tools/UnicodeData/README.md`. Production builds never download Unicode
data.

The following promises apply:

- a release never silently substitutes host or newly downloaded data for the
  bundled property database;
- a Unicode-version upgrade is an announced minor change, or part of a major
  release, and updates the version, fingerprint, generator output, conformance
  fixtures, and release notes together;
- correcting generation or interpretation while keeping the same normative
  Unicode inputs may ship as a patch; and
- the Unicode version is expected to remain fixed throughout a release's patch
  line unless a security or severe correctness issue requires an explicitly
  documented exception.

A Unicode upgrade can change script and script-extension assignment, bidi
levels and mirroring, grapheme and line boundaries, joining, Indic categories,
emoji behavior, vertical orientation, fallback segmentation, shaping, caret
positions, and PDF Unicode attribution. Callers must not assume those results
are stable across a documented Unicode upgrade.

.NET normalization is used where the runtime supplies the required operation;
the bundled database supplies the other pinned properties. Reproducible archives
should pin both the `ttf` release and the supported .NET runtime used to produce
them.

## Shaping compatibility

The shaping contract is the output defined by this library: glyph identifiers,
advances, offsets, directions, source UTF-16 ranges, Unicode sequences, cluster
safety, and logical/visual mappings for the same face, static instance, text,
options, Unicode data, and library version.

HarfBuzz 14.3.0 is a pinned independent test oracle for the supported corpus. It
is not a runtime dependency, a public API compatibility target, or a promise to
match every past or future HarfBuzz release. Documented differences include the
library's original UTF-16 source ranges, default normalization, malformed UTF-16
policy, representation of omitted default ignorables, and configured broken
cluster handling. AAT and Graphite programs remain outside the shaping contract.

Within one exact release, repeated shaping with identical inputs and options is
deterministic and independent of current culture or process-global font state.
Across releases:

- an intentional change to default normalization, default features, script
  planning, cluster policy, or supported technology requires at least a minor
  release after 1.0 unless it corrects a defect;
- a shaping bug or security fix may change glyphs, positions, clusters, or
  diagnostics in a patch release; and
- a Unicode or supported OpenType specification upgrade may change shaping in
  the minor release that announces it.

Changed supported corpus results must be reviewed as explicit expectation
updates, not hidden by changing or removing differential tests.

## Deterministic generated artifacts

For one exact library version, identical input bytes, face index, normalized
coordinates, policy objects, requested text/glyph/feature/palette usage, and
supported runtime produce byte-for-byte identical successful output. The
contract includes:

- static and subset SFNT programs, table order, padding, checksums, and names;
- old/new GID and CID mappings, horizontal and vertical widths, and encoding
  metadata;
- ToUnicode CMaps and `ActualText` recommendations;
- deterministic subset prefixes, resource deduplication keys, and diagnostics
  order; and
- caller-visible arrays and reports derived from the generated artifact.

Determinism is not a cross-version byte-compatibility promise. Patch fixes,
Unicode/specification upgrades, newly supported semantic writers, corrected
rounding, safer table retention, and diagnostic changes can alter output while
remaining API-compatible. A resource deduplication key is opaque and versioned;
do not use it as a permanent content-addressing scheme across library releases.

For long-lived reproduction, record at least the exact `ttf` release, supported
.NET runtime, source-font hash and face index, Unicode-data version/fingerprint,
all coordinates and options, and the ordered semantic usage supplied to each
builder. Store an output hash when the exact historical bytes matter. Pinning
only a font filename, family name, or major library version is insufficient.

## Diagnostics

`FontDiagnosticCode` is the machine-readable compatibility surface. Existing
numeric values are never reused for another condition. The reserved ranges
remain:

| Range | Category |
| --- | --- |
| 1000-1999 | Input regions, arithmetic, and resource limits |
| 2000-2999 | Containers and SFNT table directories |
| 3000-3999 | Semantic tables, glyphs, metrics, and shaping |
| 4000-4999 | Repairs, preservation, and dropped data |
| 5000-5999 | Embedding and output decisions |

Callers must tolerate unknown future codes. A compatible release may add a code
or cause an existing input to produce an additional diagnostic when validation
or feature support improves. For the same exact release, input, and options,
diagnostic ordering, face/table/offset context, and logical parser paths are
deterministic.

Messages are human-readable, non-localized English descriptions. Their wording
is not stable and must never be parsed or used as an identifier. Severity
describes the current operation's effect; a correction may change severity or
replace a generic code with a more specific new code. Persist numeric codes and
structured context rather than message text, and include the library version
when storing diagnostic records.

Exception type and documented failure category are part of public method
behavior. Exact exception messages and stack traces are not stable. New safety
validation may cause an operation to fail earlier while retaining the same
documented exception category.

## Public API behavior

The supported public surface consists of public types and members in the
`ttf` namespace together with their XML documentation. Internal types and
members, test-only friend access, reflection-visible implementation details,
private resource names, and binary layouts not explicitly documented as output
formats have no compatibility promise.

After 1.0, a major version is required to intentionally:

- remove or rename a supported public type, constructor, method, field, enum, or
  enum member;
- change a public signature or numeric value of an existing public enum member;
- change ownership so an API that returned a defensive copy instead exposes
  mutable internal storage;
- remove a documented supported format or operation without a safety reason; or
- broadly replace documented default-policy meaning rather than correct its
  implementation.

A minor release may add public types, overloads, methods, enum members, option
values, supported formats, and diagnostics. Callers should include a fallback
branch for enum values and diagnostics they do not recognize. Additive API does
not imply that older consumers can request the new behavior.

A patch release preserves public signatures but may correct validation,
rounding, bounds, mappings, output bytes, exceptions within a documented
category, and results that contradicted specifications or XML documentation.
Security fixes may narrow accepted input at any release level. Deprecated API
is normally retained for the rest of its major line when that can be done
safely; removal is documented in the next major release.

Thread-safety, input ownership, builder finalization, deterministic ordering,
and strict/compatible/inspect-only meanings are behavioral API contracts where
they are documented. The exact cache strategy, allocation pattern, diagnostic
message wording, and performance are not compatibility contracts.

## Release documentation checklist

Every tagged release should identify:

- its SemVer number and supported target runtime;
- the bundled Unicode version and whether its fingerprint changed;
- shaping/default behavior changes and updated oracle expectations;
- fixes or feature additions that can change deterministic output bytes;
- added diagnostic codes or material severity/category changes;
- public API additions, deprecations, and breaks; and
- changes to the [support matrix](support-matrix.md).

Consumers that require stronger archival stability should qualify a specific
release and retain the reproduction inputs described above rather than relying
on a floating package range.
