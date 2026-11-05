# Profile compatibility matrix

This matrix records the T116 run performed on 2026-08-20. It is evidence for
the exact application versions and corpus artifacts shown here, not a general
claim about other versions or files. An unavailable application is never
counted as passed or failed.

## Run environment and applications

The host was macOS 26.5.2 (Darwin 25.5.0, arm64) with .NET SDK 10.0.103 and
target framework `net10.0`.

| Key | Application | Exact tested version | Availability | Intended checks |
|---|---|---|---|---|
| RAW | raw | `1.0.0+0c3d919b48b83e385140cb90b5a872c84560438a` | Available | Metadata/open, decode, render, encode, structural reopen, pixel identity |
| LR | LibRaw (`raw-identify` or `dcraw_emu`) | — | Unavailable | Identification, decode, rendered output |
| DC | dcraw | — | Unavailable | Identification, decode, rendered output |
| ET | ExifTool | — | Unavailable | Metadata |
| E2 | Exiv2 | — | Unavailable | Metadata |
| AD | Adobe DNG Converter | — | Unavailable; no opt-in executable configured | Application open/convert |
| CD | Canon Digital Photo Professional | — | Unavailable; no opt-in executable configured | Canon application open/render |
| NN | Nikon NX Studio | — | Unavailable; no opt-in executable configured | Nikon application open/render |

The independent tools were absent from `PATH` and their `RAW_TEST_*`
overrides were unset. Vendor applications require an explicit override and no
override was set. Consequently, no external-tool version could be measured.

## Results by profile and application/version

`P` means all listed checks passed, `U` means the application was unavailable,
and `—` means a check does not apply. Check abbreviations are metadata/open
(`M`), decode against the recorded sample digest (`D`), canonical render
against the recorded digest (`R`), exact-profile encode (`W`), structural
reopen (`O`), and stored-pixel identity (`I`).

| Profile | Direction | RAW 1.0.0 | LR | DC | ET | E2 | AD | CD | NN |
|---|---|---|---|---|---|---|---|---|---|---|
| `canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.read` | Read | P: M/D/R | U | U | U | U | U | U | U |
| `canon.eos-6d-mark-ii.cr2.mraw-3x2.firmware-1.0.2.read` | Read | P: M/D/R | U | U | U | U | U | U | U |
| `canon.eos-6d-mark-ii.cr2.sraw-3x2.firmware-1.0.2.read` | Read | P: M/D/R | U | U | U | U | U | U | U |
| `canon.eos-20d.cr2.raw-3x2.firmware-2.0.3.read` | Read | P: M/D/R | U | U | U | U | U | U | U |
| `canon.eos-1d.tiff.raw-3x2.firmware-1.4.0.read` | Read | P: M/D/R | U | U | U | U | U | U | U |
| `nikon.z-7.nef.uncompressed-14bit.fx.read.experimental` | Read | P*: M/D/R | U | U | U | U | U | U | U |
| `nikon.z-8.nef.lossless-14bit.fx.read.experimental` | Read | P*: M/D/R | U | U | U | U | U | U | U |
| `canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.write.experimental` | Write | P: W/O/I/R | U | U | U | U | U | U | U |
| `nikon.z-7.nef.uncompressed-14bit.fx.write.experimental` | Write | P*: W/O/I/R | U | U | U | U | U | U | U |
| `nikon.z-8.nef.lossless-14bit.fx.write.experimental` | Write | P*: W/O/I/R | U | U | U | U | U | U | U |

`P*` is limited to the exact-profile generated artifact: there is no
hash-pinned Nikon camera file. The generated artifacts prove deterministic
writer structure, project reopen/decode, stored-pixel identity, and canonical
rendering, but not camera-origin or independent-application compatibility.
All three write profiles therefore remain experimental.

## Executed suites and evidence

The corpus generator deterministically regenerated all three writer outputs,
validated structural reopen and stored-pixel identity, and then opened,
decoded, and rendered all eight repository artifacts. Every decoded-sample
and canonical-render digest matched the values in `manifest.json` after this
run.

| Suite | Filter or command | Result |
|---|---|---|
| Corpus metadata/decode/render/writer artifact run | `dotnet run --project corpus/generator/raw.CorpusGenerator.csproj --no-restore` | Passed: 3 writer outputs and 8 opened/decoded/rendered artifacts |
| Metadata interoperability | `MetadataRoundTripInteroperabilityTests`, `TiffInteroperabilityTests` | 15 passed, 4 skipped, 0 failed; skips were unavailable ExifTool/Exiv2 checks |
| Decode interoperability | `Cr2DecodeCorpusTests`, `NefDecodeCorpusTests`, `Cr2LosslessJpegDecoderTests`, `NikonCompressionCodecTests` | 53 passed, 0 skipped, 0 failed |
| Render interoperability | `ColorRenderConformanceTests`, `RenderPipelineTests`, `DemosaicGoldenPropertyTests` | 113 passed, 0 skipped, 0 failed |
| Writer interoperability | `Cr2WriterCompatibilityTests`, `NefWriterCompatibilityTests`, `Cr2EncoderTests`, `NefEncoderTests` | 20 passed, 0 skipped, 0 failed |
| Full regression suite | `dotnet test tests/raw.Tests.csproj --no-restore` | 976 passed, 4 skipped, 0 failed; the same unavailable ExifTool/Exiv2 checks were skipped |

The canonical render recipe and digests are defined in `README.md` and
`manifest.json`. Remaining real-camera and independent-application gaps are
tracked in `coverage.md`.
