# v1.0.0 release gate

Version 1.0.0 is approved for packaging with descriptor-driven generic RAW as
the stable path. All built-in camera read and write profiles remain
experimental, disabled unless the caller explicitly opts in, and not
vendor-confirmed. This boundary is part of the release decision: the package
does not promote generated-output or unavailable-tool results into camera or
application compatibility claims.

## Profile evidence

Every built-in profile is linked to an exact entry in
`corpus/manifest.json`. Repository artifacts have fixed byte lengths and
SHA-256 hashes; private gaps are represented explicitly rather than silently
omitted.

| Built-in profile | Release evidence | Status |
|---|---|---|
| `canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.read` | Hash-pinned CC0 camera file; metadata, decode, and render digests | Experimental |
| `canon.eos-6d-mark-ii.cr2.mraw-3x2.firmware-1.0.2.read` | Hash-pinned CC0 camera file; metadata, decode, and render digests | Experimental |
| `canon.eos-6d-mark-ii.cr2.sraw-3x2.firmware-1.0.2.read` | Hash-pinned CC0 camera file; metadata, decode, and render digests | Experimental |
| `canon.eos-20d.cr2.raw-3x2.firmware-2.0.3.read` | Hash-pinned CC0 camera file; metadata, decode, and render digests | Experimental |
| `canon.eos-1d.tiff.raw-3x2.firmware-1.4.0.read` | Hash-pinned CC0 camera file; metadata, decode, and render digests | Experimental |
| `nikon.z-7.nef.uncompressed-14bit.fx.read.experimental` | Exact-profile generated artifact plus an explicit private camera-file placeholder | Experimental; camera sample missing |
| `nikon.z-8.nef.lossless-14bit.fx.read.experimental` | Exact-profile generated artifact plus an explicit private camera-file placeholder | Experimental; camera sample missing |
| `canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.write.experimental` | Deterministic generated CR2, structural reopen, render digest, and stored-pixel identity | Experimental; external applications unavailable |
| `nikon.z-7.nef.uncompressed-14bit.fx.write.experimental` | Deterministic generated NEF, structural reopen, render digest, and stored-pixel identity | Experimental; camera sample and external applications unavailable |
| `nikon.z-8.nef.lossless-14bit.fx.write.experimental` | Deterministic generated NEF, structural reopen, render digest, and stored-pixel identity | Experimental; camera sample and external applications unavailable |

The compatibility run could not find LibRaw/dcraw, ExifTool, Exiv2, Adobe DNG
Converter, Canon Digital Photo Professional, or Nikon NX Studio. Unavailable
applications count as neither pass nor failure. No built-in profile reports
`VendorConfirmed`.

## Capability contract verified for v1

Per-document capability flags now describe operations available from that
specific open result:

- metadata-only and preview-only documents do not advertise raw decode or
  render;
- a profiled CR2/NEF opened with raw-payload access advertises decode and the
  baseline render path only when its decoder and primary raw channel layout
  are usable;
- CR2/NEF preserving rewrite is advertised only when metadata and previews
  required by the preserving rewriters were loaded;
- descriptor-driven generic RAW advertises symmetric encode, but not a
  preserving-rewrite operation that has no public implementation; and
- encoding never implies vendor confirmation.

Nikon HE and HE Star are recognition, metadata, preview, and extraction cases
only. Raw decode, render, preserving rewrite, and new-file encoding are
unavailable and return the stable unsupported-codec diagnostic. No in-tree
codec will be added without a cleared specification and licensing basis under
`specs/nikon-he-legal-review.md`.

## Lossless writer and package verification

The built-in evidence for all three lossless write profiles records both a
passed parse-write-parse structural check and a passed
`decode-compare-stored-pixel-identity` check. The standalone corpus generator
regenerated the three outputs and then reopened, decoded, and rendered all
eight repository artifacts without changing any recorded hash.

The local macOS arm64 release verification on 2026-08-20 used .NET SDK
10.0.103:

| Gate | Result |
|---|---|
| Focused capability/profile tests | 31 passed, 0 failed |
| Full Release regression suite | 982 passed, 4 unavailable-tool skips, 0 failed |
| Warning-strict Release build | 0 warnings, 0 errors |
| Corpus generator | 3 writer outputs and 8 artifact decode/render checks passed |
| NuGet package | `raw.1.0.0.nupkg` contains the managed DLL, XML documentation, and README |
| Runtime dependencies | No direct or transitive NuGet packages; no native assets |

The four skipped tests are the opt-in ExifTool/Exiv2 checks described in
`corpus/compatibility.md`; they do not weaken any vendor-confirmed claim
because no such claim is made.
