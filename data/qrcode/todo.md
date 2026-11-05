# Universal QR Code library development plan

## Objective and boundaries

Build a dependency-free C# library for encoding, detecting, sampling, decoding, validating, and processing the standardized QR family. The primary conformance target is QR Code Model 2 under ISO/IEC 18004:2024. Micro QR Code is a full ISO/IEC 18004 profile; rMQR is a full ISO/IEC 23941:2022 profile; legacy Model 1 is detected and decoded for compatibility using ISO/IEC 18004 Annex N, but is not emitted by default. Proprietary QR-family formats such as iQR, SQRC, Frame QR, Secure QR, logo/art layouts, animated QR, and vendor-specific color codes are outside the standards-conformance scope.

The complete ISO publications are normative and must be obtained through a licensed source before conformance tables are frozen. Files under `specs/` are research aids; `specs/SOURCES.md` records their provenance. ISO/IEC 18004, ISO/IEC 23941, ISO/IEC 15424, ISO/IEC 15415, AIM ECI, JIS/Shift JIS, and GS1 rules take precedence over behavior observed in other libraries.

Production code under `src/` uses only the .NET base class library. Image APIs accept raw pixel buffers and matrices; PNG, JPEG, camera, UI, and video adapters belong to consumers or tests. Arrays are used in public data objects, while internal hot paths use `Span<T>` and `ReadOnlySpan<T>` where lifetimes remain local and clear. All C# must fit `syntax.md`: file-scoped namespaces, classes/interfaces/enums only, public fields instead of properties, no overloads, unique interface member names, no LINQ, no exception handling statements, and no public static methods.

## Standards and symbol coverage

| Symbol/profile | Encode | Detect | Decode | Validate/process | Notes |
|---|---:|---:|---:|---:|---|
| QR Code Model 2, versions 1-40 | Full | Full, including multiple symbols | Full | Full | Four ECC levels, eight masks, normal/mirrored and normal/reversed reflectance |
| Micro QR Code, M1-M4 | Full | Full | Full | Full | Version-specific modes/ECC, four masks, two-module quiet zone, no structured append or ECI |
| rMQR, all ISO/IEC 23941 sizes | Full | Full | Full | Full | Rectangular geometry, profile-specific finder/sub-finder/alignment/timing/format/mask rules |
| QR Code Model 1, versions 1-14 | No new-symbol generation | Compatibility | Compatibility | Inspect/validate | Decode-only legacy track; return the legacy kind explicitly |
| FNC1 first position / GS1 QR | Full | N/A | Full | Full | GS1 separator and symbology-identifier semantics |
| FNC1 second position | Full | N/A | Full | Full | Preserve and expose application indicator |
| ECI | Full protocol | N/A | Full protocol | Full | Preserve every valid assignment; transcode built-in supported character sets and retain raw bytes for unsupported assignments |
| Structured append | Full, 2-16 Model 2 symbols | Multi-symbol | Full assembly | Full | Validate sequence, position, count, parity, duplicate/missing/conflicting parts |
| Hanzi mode | Compatibility option | N/A | Compatibility option | Full | GB/T extension, never silently treated as ISO/IEC 18004 conformance |

## Feature inventory

### Data analysis and segment encoding

- Numeric mode with 3/2/1 digit packing, version-family character-count widths, length limits, and invalid-input rejection.
- Alphanumeric mode with the exact 45-character table, 2/1 character packing, case preservation, and GS1 percent escape/separator rules.
- Byte mode for arbitrary binary including zero bytes, explicit ECI assignment, standards-defined default interpretation, UTF-8 convenience input, and lossless raw-byte output.
- Kanji mode using the permitted double-byte Shift JIS ranges and 13-bit transformation, with strict rejection or byte-mode fallback for unmappable text.
- Optional Hanzi subset 1 / GB2312 compatibility with explicit opt-in and visible non-ISO metadata.
- Manual segments, automatic single-mode selection, and globally optimal mixed-mode segmentation across numeric, alphanumeric, byte encodings/ECIs, and Kanji.
- Dynamic-programming optimization that includes mode headers, character-count widths, ECI switches, FNC1 placement, version groups, symbol family constraints, and structured-append overhead.
- Terminator, bit padding, byte alignment, alternating pad codewords, exact capacity checks, and round-trip preservation of segment boundaries and control segments.

### Version, family, error-correction, and mask selection

- Forced symbol kind/version/size, allowed minimum and maximum bounds, or automatic selection of the smallest fitting standardized symbol.
- Forced ECC level or automatic/boosted ECC selection that never enlarges the selected symbol unless requested.
- Capacity calculation for every version/size, mode, ECC level, and control-header combination, including remainder bits.
- Forced valid mask or automatic exhaustive mask evaluation using each profile's mask formulas and penalty/score rules.
- Stable tie-breaking and reproducible output; expose selected kind, version/size, ECC, mask, total/data/ECC codewords, and unused capacity.

### Error correction and message construction

- GF(256) arithmetic with the QR primitive polynomial, exponent/log tables, polynomial operations, and generator polynomials.
- Reed-Solomon parity generation per block; short/long block partitioning; data and ECC interleaving; remainder bits.
- Syndrome calculation, Euclidean/Berlekamp-Massey-equivalent locator solving, Chien search, Forney magnitudes, error correction, and known-erasure assistance.
- Correctable-limit enforcement, verification after repair, corrected-error/erasure counts, and uncorrectable reporting without fabricated payloads.
- BCH generation/correction for format and version information, redundant-copy comparison, and confidence metadata.

### Matrix construction and symbol processing

- Exact finder, separator, timing, alignment, dark-module, corner/sub-finder, format, version, and quiet-zone geometry for each supported profile.
- Zig-zag data placement and extraction, reserved-module maps, mask application/removal, format/version placement, and profile-specific orientation.
- Module provenance classification (function, data, ECC, remainder, quiet zone, unknown) for inspection and damage analysis.
- Matrix transforms for rotation, mirror, inversion, crop/add quiet zone, integer scaling, and raw 1-bit/8-bit raster rendering.
- Logical conformance validation: dimensions, quiet zone, fixed patterns, reserved cells, format/version consistency, codeword capacity, and decodability.
- Quality analysis on supplied grayscale/module samples: symbol contrast, modulation, fixed-pattern damage, axial/grid non-uniformity, print growth, unused error correction, scan grade inputs, and an explicit distinction between calculated indicators and calibrated ISO verifier certification.

### Image preprocessing, detection, geometry, and sampling

- Raw Gray8, RGB24, RGBA32, and BGRA32 input with checked width/height/stride/buffer arithmetic and deterministic luminance conversion.
- Global histogram, local/hybrid adaptive, and caller-supplied binary matrices; normal, inverted, and automatic polarity paths.
- Optional denoise, small-gap closing, contrast normalization, and bounded multi-scale search without mutating caller buffers.
- Run-ratio finder scans in rows and columns, cross-checks, connected-region/capstone checks, center clustering, module-size estimation, and false-positive scoring.
- Group compatible finder candidates into one or many Model 2/Model 1 symbols; single-finder Micro QR and rMQR-specific grouping; duplicate suppression.
- Orientation, rotation, mirrored geometry, dimension/version estimation, alignment-pattern search, timing-pattern refinement, and damaged/missing alignment fallback.
- Homography/perspective transform, boundary-safe mapping, center and multi-point module sampling, confidence/unknown modules, and iterative grid refinement against fixed patterns.
- Cropped/absent quiet-zone tolerance as a detection option while reporting conformance failure; multi-symbol results ordered deterministically.

### Decode and damaged-symbol recovery

- Direct decoding from a clean module matrix or a detected/sampled image candidate.
- Profile discrimination, orientation/mirror/inversion trials, dimension validation, format/version BCH recovery, unmasking, codeword extraction, deinterleaving, RS repair, and payload parsing.
- Strict parsing for every data/control mode, count field, terminator, padding, ECI designator width, FNC1 rules, structured-append header, and illegal/reserved mode.
- Text plus raw bytes and per-segment raw bytes/text/ECI metadata, so decoding never loses information when a character set is unavailable or malformed.
- Symbology identifiers and modifier metadata derived from ECI/FNC1 state rather than injected into payload data.
- Recovery ladder using both format/version copies, mirror/inversion, alternative threshold candidates, resampling offsets, alignment refinement, erasure-aware RS, and bounded uncertain-module flips prioritized by confidence.
- Deterministic limits for candidate count, retry count, image dimensions, allocation sizes, segments, and structured-append sets; cancellation-free synchronous APIs with no global mutable state.
- Failure taxonomy distinguishing not found, invalid geometry, unsupported profile/mode/ECI text conversion, malformed data, checksum failure, capacity overflow, incomplete structured append, and resource limit.

### High-level workflows

- Encode text, bytes, explicit segments, GS1/FNC1, and structured-append sequences.
- Detect-only, decode-only, scan one image to all results, and assemble structured-append results collected across images.
- Exception APIs for programming/configuration failures and exception-less attempt results for expected scan/decode failures.
- Thread-safe reusable stateless service instances and no external state, codecs, native code, or filesystem access in `src/`.

## Reference-library findings

| Library | Useful coverage observed | Gaps that this plan closes |
|---|---|---|
| ZXing | Model 2 encoder/decoder/detector; optimal multi-mode and multi-ECI encoding; Kanji/Hanzi paths; GS1/FNC1; ECI; mirror fallback; pure-matrix and multi-symbol scanning; structured-append decode assembly | No complete QR-family Model 2 + Micro + rMQR surface in the reviewed Java QR package; encoding/detection options are split across APIs |
| ZBar | Image scanning, finder-line/candidate geometry, Model 2 decode, numeric/alphanumeric/byte/Kanji, ECI, FNC1 first/second, structured-append parsing, orientation | Decoder-focused; no complete encoder or modern typed symbol-processing surface |
| quirc | Small dependency-free detector/decoder, multiple symbols, adaptive thresholding, region/capstone finder detection, perspective refinement, Model 2 RS repair, numeric/alphanumeric/byte/Kanji and ECI metadata | No encoder, structured-append/FNC1 completeness, Micro/rMQR, or rich lossless metadata |
| libqrencode | Model 2 and experimental Micro encoding, numeric/alphanumeric/byte/Kanji, input chunks, optimized splitting, structured append, raw module output | No detector/decoder; README states ECI/FNC1 and Model 1 are unsupported |
| Nayuki QR Code generator | Clear Model 2 generator, versions 1-40, four ECC levels, exact mask scoring, forced bounds/mask, ECC boost, manual ECI, Kanji and globally optimal segments in advanced Java path | No decoding/detection, Micro/rMQR, structured append, or FNC1 workflow |
| Segno | Model 2/Micro generation, numeric/alphanumeric/byte/Kanji, opt-in Hanzi, ECI, structured append, mask/ECC/version selection, quiet-zone-aware serialization; current source also contains rMQR tables/work | Generator-focused; no image detection or decoding, and rendering breadth is intentionally outside this core's dependency-free raw-pixel boundary |

## Proposed public API

All public types live under `src/Api`. Data types have public fields and no properties; operation classes expose only interface members and one constructor. Names below are the intended stable surface, without final field-level signatures.

### `src/Api/Enums`

- `QrSymbolKind`: Model2, Micro, rMQR, LegacyModel1.
- `QrEncodingMode`: Terminator, Numeric, Alphanumeric, Byte, Kanji, Hanzi, Eci, Fnc1First, Fnc1Second, StructuredAppend.
- `QrErrorCorrectionLevel`: None, Low, Medium, Quartile, High.
- `QrMaskPattern`: Automatic and profile-valid mask identifiers zero through seven.
- `QrPixelFormat`: Gray8, Rgb24, Rgba32, Bgra32.
- `QrBinarizationMode`: Automatic, GlobalHistogram, HybridLocal, SuppliedBinary.
- `QrPolarity`: Automatic, DarkOnLight, LightOnDark.
- `QrModuleValue`: Unknown, Light, Dark.
- `QrModuleRole`: Unknown, QuietZone, Finder, Separator, Timing, Alignment, Format, Version, DarkModule, Data, ErrorCorrection, Remainder.
- `QrFailureReason`: None, NotFound, InvalidInput, InvalidGeometry, InvalidFormat, InvalidVersion, InvalidMode, InvalidData, UnsupportedTextEncoding, ChecksumFailure, CapacityExceeded, IncompleteSequence, ConflictingSequence, ResourceLimit.
- `QrQualityGrade`: NotGraded and numeric grade bands required by the licensed quality standards.

### `src/Api/Data`

- `QrPoint`, `QrQuadrilateral`: image-space geometry.
- `QrImage`: raw pixels, dimensions, stride, and pixel format.
- `QrMatrix`: width, height, module values, optional role/confidence arrays, and quiet-zone extent.
- `QrSegment`: mode, character count, raw data, text, ECI assignment, and control metadata.
- `QrEncodeOptions`: family/version/size bounds, ECC/mask policy, optimization, ECI, FNC1, Hanzi, quiet-zone, and resource limits.
- `QrDecodeOptions`: strictness, allowed families, polarity/mirror/recovery settings, text-conversion policy, and resource limits.
- `QrDetectionOptions`: binarization, scale/candidate limits, quiet-zone tolerance, multi-symbol, sampling, and refinement settings.
- `QrRenderOptions`: scale, border/quiet-zone, dark/light byte values, inversion, and role rendering.
- `QrQualityOptions`: aperture/sample assumptions, grading controls, and applicable profile.
- `QrSymbol`: kind, version/size, ECC, mask, matrix, codeword/capacity counts, segments, and conformance state.
- `QrSymbolSequence`: structured-append symbols and sequence metadata.
- `QrDetection`: corners, sampled matrix, kind/version estimates, polarity/mirror state, confidence, and diagnostics.
- `QrDetectionSet`: zero or more detections plus preprocessing diagnostics.
- `QrStructuredAppendInfo`: index, total, parity, application identity, and completeness metadata.
- `QrDecodeResult`: text, raw bytes, segments, symbol metadata, ECI/FNC1/symbology identifier, correction counts, geometry, and warnings.
- `QrDecodeSet`: decoded results, unassembled results, and assembled structured-append results.
- `QrAttemptResult`: success flag, failure reason, optional decode result, and diagnostic counts.
- `QrEncodeAttemptResult`: success flag, failure reason, optional symbol/sequence, and required capacity.
- `QrCorrectionResult`: repaired codewords, success/failure, error/erasure counts, syndromes, and verification state.
- `QrCapacityRequest`, `QrCapacityResult`: low-level fit/capacity query.
- `QrValidationResult`: logical conformance failures, damaged roles/modules, and recoverability indicators.
- `QrQualityMetric`, `QrQualityReport`: named measurements, grades, overall result, and verifier limitations.
- `QrRaster`: raw Gray8 output, dimensions, and stride.

### `src/Api/Static`

- `QrLimits`: standardized version/size/mask/structured-append/ECI bounds and default safety limits as fields only.
- `QrEciAssignments`: common registered assignment numbers as fields only; the runtime registry remains internal.
- `QrSymbologyIdentifiers`: QR-family ISO/IEC 15424 identifier strings as fields only.
- `QrCharacterSets`: the QR alphanumeric repertoire and canonical encoding names as fields only.

### `src/Api/Exceptions`

- `QrInputException`, `QrCapacityException`, `QrDetectionException`, `QrDecodeException`, `QrChecksumException`, `QrStructuredAppendException`, `QrUnsupportedFeatureException`, and `QrResourceLimitException`, each deriving directly from `System.Exception` as required by `syntax.md`.

### `src/Api/Contracts`

- `IQrSegmenter`: `CreateTextSegments`, `CreateByteSegment`, `OptimizeTextSegments`, `ValidateSegments`.
- `IQrCapacityCalculator`: `CalculateCapacity`, `SelectSymbolParameters`.
- `IQrReedSolomonCodec`: `CreateErrorCorrection`, `CorrectCodewords`.
- `IQrMatrixProcessor`: `BuildMatrix`, `ExtractCodewords`, `TransformMatrix`, `ClassifyModules`, `ValidateMatrix`.
- `IQrEncoder`: `EncodeText`, `EncodeBytes`, `EncodeSegments`, `EncodeStructuredAppend`, `TryEncodeText`, `TryEncodeSegments`.
- `IQrBinarizer`: `BinarizeImage`, `CreateLuminanceImage`.
- `IQrDetector`: `DetectImage`, `DetectBinaryMatrix`.
- `IQrPerspectiveSampler`: `SampleDetection`, `ResampleDetection`.
- `IQrDecoder`: `DecodeMatrix`, `DecodeDetection`, `TryDecodeMatrix`, `TryDecodeDetection`.
- `IQrStructuredAppendAssembler`: `AssembleSequence`, `TryAssembleSequence`.
- `IQrScanner`: `ScanImage`, `ScanBinaryMatrix`.
- `IQrRenderer`: `RenderSymbol`, `RenderRoleMap`.
- `IQrQualityAnalyzer`: `AnalyzeMatrixQuality`, `AnalyzeImageQuality`.

### `src/Api/Classes`

- `QrSegmenter`, `QrCapacityCalculator`, `QrReedSolomonCodec`, `QrMatrixProcessor`, `QrEncoder`, `QrBinarizer`, `QrDetector`, `QrPerspectiveSampler`, `QrDecoder`, `QrStructuredAppendAssembler`, `QrScanner`, `QrRenderer`, and `QrQualityAnalyzer`, each implementing its matching contract with one public constructor and no extra public members.

## Planned source files

```text
src/Api/Enums/{QrSymbolKind,QrEncodingMode,QrErrorCorrectionLevel,QrMaskPattern,QrPixelFormat,QrBinarizationMode,QrPolarity,QrModuleValue,QrModuleRole,QrFailureReason,QrQualityGrade}.cs
src/Api/Data/{QrPoint,QrQuadrilateral,QrImage,QrMatrix,QrSegment,QrEncodeOptions,QrDecodeOptions,QrDetectionOptions,QrRenderOptions,QrQualityOptions,QrSymbol,QrSymbolSequence,QrDetection,QrDetectionSet,QrStructuredAppendInfo,QrDecodeResult,QrDecodeSet,QrAttemptResult,QrEncodeAttemptResult,QrCorrectionResult,QrCapacityRequest,QrCapacityResult,QrValidationResult,QrQualityMetric,QrQualityReport,QrRaster}.cs
src/Api/Static/{QrLimits,QrEciAssignments,QrSymbologyIdentifiers,QrCharacterSets}.cs
src/Api/Exceptions/{QrInputException,QrCapacityException,QrDetectionException,QrDecodeException,QrChecksumException,QrStructuredAppendException,QrUnsupportedFeatureException,QrResourceLimitException}.cs
src/Api/Contracts/{IQrSegmenter,IQrCapacityCalculator,IQrReedSolomonCodec,IQrMatrixProcessor,IQrEncoder,IQrBinarizer,IQrDetector,IQrPerspectiveSampler,IQrDecoder,IQrStructuredAppendAssembler,IQrScanner,IQrRenderer,IQrQualityAnalyzer}.cs
src/Api/Classes/{QrSegmenter,QrCapacityCalculator,QrReedSolomonCodec,QrMatrixProcessor,QrEncoder,QrBinarizer,QrDetector,QrPerspectiveSampler,QrDecoder,QrStructuredAppendAssembler,QrScanner,QrRenderer,QrQualityAnalyzer}.cs
src/Internal/Core/{BitBuffer,BitReader,BitMatrixBuffer,CheckedDimensions,ArrayPoolLease,IntegerMath}.cs
src/Internal/Tables/{Model2VersionTable,MicroVersionTable,RectangularMicroVersionTable,LegacyModel1Table,AlignmentPositionTable,EccBlockTable,FormatCodeTable,VersionCodeTable,EciRegistry,ShiftJisTable,Gb2312Table}.cs
src/Internal/ErrorCorrection/{GaloisField256,GaloisPolynomial,ReedSolomonGenerator,ReedSolomonEncoder,ReedSolomonDecoder,BchCodec,ErrorLocationSolver}.cs
src/Internal/Encoding/{NumericEncoder,AlphanumericEncoder,ByteEncoder,KanjiEncoder,HanziEncoder,EciEncoder,Fnc1Encoder,SegmentOptimizer,SymbolSelector,CodewordFinalizer,BlockInterleaver,StructuredAppendSplitter}.cs
src/Internal/Matrix/{FunctionPatternBuilder,Model2MatrixBuilder,MicroMatrixBuilder,RectangularMicroMatrixBuilder,DataModuleWalker,MaskEvaluator,FormatInformationCodec,VersionInformationCodec,ModuleClassifier,MatrixTransformer,MatrixValidator}.cs
src/Internal/Imaging/{LuminanceConverter,GlobalHistogramBinarizer,HybridBinarizer,BinaryMorphology,ImagePyramid}.cs
src/Internal/Detection/{RunRatioScanner,FinderCandidate,FinderClusterer,FinderGrouper,MicroFinderDetector,RectangularMicroFinderDetector,AlignmentPatternDetector,DimensionEstimator,DetectionDeduplicator,DetectionCoordinator}.cs
src/Internal/Geometry/{PerspectiveTransform,GridSampler,SamplingConfidence,GridRefiner,BoundaryMapper}.cs
src/Internal/Decoding/{SymbolDiscriminator,MatrixParser,CodewordExtractor,BlockDeinterleaver,PayloadParser,NumericDecoder,AlphanumericDecoder,ByteDecoder,KanjiDecoder,HanziDecoder,EciDecoder,Fnc1Decoder,LegacyModel1Decoder,DecodeRecoveryCoordinator}.cs
src/Internal/Quality/{FixedPatternAnalyzer,ContrastAnalyzer,ModulationAnalyzer,GridUniformityAnalyzer,PrintGrowthAnalyzer,UnusedErrorCorrectionAnalyzer,QualityGrader}.cs
```

## Implementation tasks

### Standards, constants, and fixtures

- [DONE] T001: Obtain publicly available equivalents of ISO/IEC 18004:2024, ISO/IEC 23941:2022, ISO/IEC 15424:2025, ISO/IEC 15415:2024, and current AIM ECI texts, then resolve every normative requirement and corrigendum used by implementation constants.
- [DONE] T002: Extract and independently double-check Model 2 versions 1-40 dimensions, capacities, remainder bits, alignment positions, ECC block groups, character-count widths, format/version BCH values, mask formulas, and placement rules.
- [DONE] T003: Extract and independently double-check Micro QR M1-M4 mode, dimension, capacity, ECC, mask, format, placement, and quiet-zone tables.
- [DONE] T004: Extract and independently double-check every ISO/IEC 23941 rMQR size, capacity, ECC, mode, mask, finder/sub-finder, alignment, timing, format, placement, and quiet-zone table.
- [DONE] T005: Extract legacy Model 1 versions 1-14 recognition, placement, format, and decode differences needed for compatibility.
- [DONE] T006: Build the registered ECI assignment registry with canonical aliases, supported .NET conversion paths, protocol-only entries, and exact variable-width assignment limits.
- [DONE] T007: Derive auditable Shift JIS and optional GB2312 mapping tables from licensed/authoritative mappings, separating standard mappings from vendor extensions.
- [DONE] T008: Create checked-in golden bitstreams, codewords, matrices, damaged matrices, structured-append sets, and raw image fixtures covering all standardized profiles and boundary versions.

### Public data model and shared core

- [DONE] T009: Add all proposed API enums with explicit values, including failure, module-role, polarity, pixel-format, and quality-grade classifications.
- [DONE] T010: Add geometry, raw image, matrix, raster, segment, structured-append, capacity, correction, and attempt-result API data classes with validation-friendly public fields.
- [DONE] T011: Add encode, decode, detection, rendering, and quality option data classes with safe defaults and explicit standardized-family controls.
- [DONE] T012: Add symbol, sequence, detection, decode, validation, and quality output data classes that retain raw information and diagnostic metadata.
- [DONE] T013: Add static API field containers for limits, common ECI assignments, symbology identifiers, and the alphanumeric repertoire.
- [DONE] T014: Add the exception set, using only exception inheritance forms permitted by `syntax.md` and retaining machine-readable failure context in fields.
- [DONE] T015: Implement checked dimension, stride, index, multiplication, allocation, and capacity arithmetic that rejects overflow before buffer access.
- [DONE] T016: Implement internal bit buffer/reader and packed matrix primitives with MSB-first operations, bounds checks, and local `Span<T>` processing.
- [DONE] T017: Implement internal pooling/temporary-buffer ownership without leaking pooled data or exposing pooled arrays through public results.
- [DONE] T018: Implement immutable-at-runtime lookup initialization and verify that public service instances have no shared mutable state.

### Data modes, ECI, FNC1, and optimization

- [DONE] T019: Implement numeric segment validation, bit-length calculation, encoding, and decoding for all terminal group sizes.
- [DONE] T020: Implement the exact alphanumeric repertoire, value mapping, bit-length calculation, encoding, and decoding.
- [DONE] T021: Implement arbitrary byte segments, zero-byte preservation, exact length accounting, and lossless decoding.
- [DONE] T022: Implement strict Kanji recognition, Shift JIS transformation/inverse transformation, text mapping, and byte fallback policy.
- [DONE] T023: Implement opt-in Hanzi subset 1 recognition, GB2312 transformation/inverse transformation, and compatibility metadata.
- [DONE] T024: Implement ECI assignment encoding/decoding for one-, two-, and three-byte designators and preserve unknown registered/unregistered values without corrupting raw payloads.
- [DONE] T025: Implement built-in text transcoding for UTF-8, ISO-8859-1, UTF-16 variants, ASCII, and all additional mappings committed under T006/T007 without external packages.
- [DONE] T026: Implement byte-encoding inference only when enabled, with deterministic fallback and an option requiring explicit ECI for non-default encodings.
- [DONE] T027: Implement FNC1 first-position encoding/decoding, GS1 percent escaping, group-separator transmission, and GS1 metadata.
- [DONE] T028: Implement FNC1 second-position encoding/decoding and application-indicator preservation.
- [DONE] T029: Implement manual segment validation, legal control-segment order, per-family mode restrictions, count limits, and empty-input behavior.
- [DONE] T030: Implement automatic single-mode selection for text and binary inputs.
- [DONE] T031: Implement globally optimal mixed numeric/alphanumeric/byte/Kanji segmentation for each character-count version group.
- [DONE] T032: Extend the optimizer across available ECI byte encodings, charging exact ECI switch/header costs and respecting caller priorities.
- [DONE] T033: Include FNC1 and structured-append header costs in optimization and prohibit illegal profile/control combinations.
- [DONE] T034: Add `IQrSegmenter` and `QrSegmenter` with the proposed uniquely named creation, optimization, and validation methods.

### Capacity, selection, and Reed-Solomon

- [DONE] T035: Implement exact usable-bit and codeword capacity calculations for Model 2 across every version/ECC pair.
- [DONE] T036: Implement exact usable-bit and codeword capacity calculations for Micro QR across every version/ECC/mode combination.
- [DONE] T037: Implement exact usable-bit and codeword capacity calculations for every rMQR size/ECC/mode combination.
- [DONE] T038: Implement family/version/size selection honoring forced values, allowed ranges, smallest-fit policy, and deterministic ties.
- [DONE] T039: Implement fixed ECC, automatic ECC, and ECC boosting without unintended symbol enlargement.
- [DONE] T040: Add `IQrCapacityCalculator` and `QrCapacityCalculator` for fit queries and complete parameter selection.
- [DONE] T041: Implement GF(256) add, multiply, divide, inverse, exponent/log, and polynomial evaluation against the QR primitive polynomial.
- [DONE] T042: Implement generator-polynomial construction/caching and parity generation for all standardized ECC lengths.
- [DONE] T043: Implement syndrome calculation, locator/evaluator solving, Chien search, Forney magnitude calculation, and corrected-codeword verification.
- [DONE] T044: Add known-erasure inputs and enforce `2 * errors + erasures <= parity` correction bounds.
- [DONE] T045: Implement short/long data-block splitting and data/ECC interleaving for each profile.
- [DONE] T046: Implement inverse deinterleaving with per-block error/erasure diagnostics and aggregate correction counts.
- [DONE] T047: Add `IQrReedSolomonCodec` and `QrReedSolomonCodec` for low-level parity generation and correction.
- [DONE] T048: Implement BCH creation and nearest-valid correction for format and version information, including redundant-copy arbitration.

### Bitstream finalization and matrix generation

- [DONE] T049: Implement mode headers, character counts, payload concatenation, terminator truncation, zero padding, byte alignment, and alternating pad codewords per profile.
- [DONE] T050: Implement Model 2 reserved maps and finder, separator, timing, alignment, dark-module, format, and version patterns.
- [DONE] T051: Implement Model 2 zig-zag data/remainder placement and inverse traversal for every version.
- [DONE] T052: Implement all eight Model 2 data masks and exact four-rule penalty scoring, including finder-like run boundary handling.
- [DONE] T053: Implement forced/automatic Model 2 mask selection with stable lowest-mask tie breaking.
- [DONE] T054: Implement Micro QR function patterns, reserved map, data placement/extraction, four masks, scoring, and format placement.
- [DONE] T055: Implement rMQR function patterns, reserved map, data placement/extraction, permitted masks/scoring, and dual format information.
- [DONE] T056: Implement profile-aware quiet-zone addition/removal and retain the encoding-region versus rendered-symbol dimensions.
- [DONE] T057: Implement module-role classification for every function/data/ECC/remainder/quiet-zone cell.
- [DONE] T058: Implement rotation, mirror, polarity inversion, crop, quiet-zone, and integer-scale matrix transforms.
- [DONE] T059: Implement logical matrix validation for dimensions, fixed patterns, reserved modules, metadata, placement, padding, and decode consistency.
- [DONE] T060: Add `IQrMatrixProcessor` and `QrMatrixProcessor` for build, extract, transform, classify, and validate workflows.
- [DONE] T061: Implement `QrRenderer` raw Gray8 rendering for symbols, quiet zones, inversion, scale, and configurable light/dark values.
- [DONE] T062: Implement role-map rendering that can distinguish function, payload, ECC, remainder, damage, and unknown cells.
- [DONE] T063: Add `IQrRenderer` with symbol and role-map rendering methods.

### Encoding and structured append

- [DONE] T064: Implement the complete Model 2 encode pipeline from validated segments through selected parameters, ECC, interleaving, matrix placement, masking, and metadata.
- [DONE] T065: Implement the complete Micro QR encode pipeline with version-specific restrictions and M1 no-ECC behavior.
- [DONE] T066: Implement the complete rMQR encode pipeline for every standardized size and error-correction option.
- [DONE] T067: Implement parity calculation and legal splitting of a message into 2-16 structured-append Model 2 symbols.
- [DONE] T068: Optimize structured-append partition boundaries and symbol parameters while preserving segment/ECI/FNC1 semantics in every part.
- [DONE] T069: Implement manual structured-append headers with validation of position, total count, parity, and common-message identity.
- [DONE] T070: Add `IQrEncoder` and `QrEncoder` exception-based text, bytes, segment, and structured-append operations.
- [DONE] T071: Implement exception-less encode attempts for frequent capacity/representability failures without hiding invalid programmer configuration.

### Imaging and binarization

- [DONE] T072: Implement validated luminance conversion for Gray8, RGB24, RGBA32, and BGRA32 with correct stride and alpha treatment.
- [DONE] T073: Implement global histogram thresholding with low-contrast detection and both polarity outputs.
- [DONE] T074: Implement hybrid/local adaptive thresholding for gradients, shadows, and uneven illumination.
- [DONE] T075: Implement bounded binary cleanup options for isolated noise and small finder/timing gaps.
- [DONE] T076: Implement a bounded image pyramid/multi-scale candidate source for very small and very large modules.
- [DONE] T077: Add `IQrBinarizer` and `QrBinarizer` for luminance and binary-matrix production without changing caller memory.

### Detection, perspective correction, and sampling

- [DONE] T078: Implement row/column 1:1:3:1:1 run scanning, diagonal/cross checks, center estimates, and module-size confidence.
- [DONE] T079: Implement connected-region/capstone validation to reject line-run false positives and recover distorted finder corners.
- [DONE] T080: Implement candidate clustering/merging across scan lines, scales, and polarity passes.
- [DONE] T081: Implement Model 2/Model 1 three-finder grouping, orientation, right-angle/module-size checks, and version/dimension estimation.
- [DONE] T082: Implement Micro QR single-finder detection using timing/format/extent evidence and profile-specific orientation.
- [DONE] T083: Implement rMQR finder/sub-finder grouping, rectangular size estimation, and orientation discrimination.
- [DONE] T084: Implement alignment-pattern search at predicted locations with expanding windows and graceful damaged-pattern fallback.
- [DONE] T085: Implement timing-pattern phase checks and dimension refinement before sampling.
- [DONE] T086: Implement quadrilateral homography creation/inversion with degenerate-geometry and non-finite-coordinate rejection.
- [DONE] T087: Implement boundary-safe module-center sampling and optional multi-point voting with confidence/unknown values.
- [DONE] T088: Implement iterative perspective/grid refinement scored against finder, timing, alignment, and format evidence.
- [DONE] T089: Implement mirror and reversed-reflectance detection metadata without prematurely rewriting payload orientation.
- [DONE] T090: Implement candidate de-duplication and deterministic ordering for multiple overlapping symbols.
- [DONE] T091: Implement quiet-zone conformance measurement separately from tolerant detection of cropped symbols.
- [DONE] T092: Add `IQrDetector` and `QrDetector` for raw-image and supplied-binary detection.
- [DONE] T093: Add `IQrPerspectiveSampler` and `QrPerspectiveSampler` for initial and refined sampling of caller-provided detections.

### Matrix decoding and recovery

- [DONE] T094: Implement symbol-family discrimination from dimensions and fixed-pattern evidence, including explicit ambiguity results.
- [DONE] T095: Implement Model 2 format/version reading from both copies, BCH correction, mask/ECC extraction, and version-dimension cross-checks.
- [DONE] T096: Implement Micro QR and rMQR format/size reading and consistency checks.
- [DONE] T097: Implement Model 2/Micro/rMQR unmasking, codeword extraction, remainder validation, deinterleaving, and RS repair.
- [DONE] T098: Implement strict payload parsing for terminator, numeric, alphanumeric, byte, Kanji, Hanzi, ECI, FNC1, and structured-append modes.
- [DONE] T099: Implement lossless segment/result creation with raw bytes, decoded text when available, active ECI, and warnings for unsupported or malformed character mappings.
- [DONE] T100: Implement legacy Model 1 matrix parsing, error correction, payload decoding, and explicit legacy metadata.
- [DONE] T101: Implement symbology-identifier/modifier selection from profile, ECI, and FNC1 state according to ISO/IEC 15424.
- [DONE] T102: Implement decode retries over rotations, mirror, polarity reversal, redundant format/version choices, and strict/lenient padding policy.
- [DONE] T103: Implement image recovery retries over threshold candidates, sampling offsets, alignment alternatives, and refined homographies.
- [DONE] T104: Convert low-confidence/unknown sampled modules into codeword erasures and feed them to block-level RS recovery.
- [DONE] T105: Implement bounded confidence-prioritized uncertain-module flips only when BCH/RS verification can prove the repaired result.
- [DONE] T106: Enforce recovery budgets and deterministic failure selection so malformed or adversarial inputs cannot trigger unbounded retries.
- [DONE] T107: Add `IQrDecoder` and `QrDecoder` for exception-based and exception-less matrix/detection decoding.

### Structured-append assembly and high-level scanning

- [DONE] T108: Implement structured-append grouping by parity/count/application metadata and reject incompatible mixes.
- [DONE] T109: Implement ordering, duplicate equivalence/conflict handling, missing-part reporting, byte concatenation, text/ECI boundary handling, and aggregate diagnostics.
- [DONE] T110: Add `IQrStructuredAppendAssembler` and `QrStructuredAppendAssembler` with exception and attempt-result paths.
- [DONE] T111: Implement the high-level raw-image pipeline across luminance, polarity, thresholds, detection, sampling, recovery decode, deduplication, and structured-append assembly.
- [DONE] T112: Implement the high-level supplied-binary pipeline with the same result ordering and diagnostics.
- [DONE] T113: Add `IQrScanner` and `QrScanner`, returning empty successful sets for ordinary no-symbol images and per-candidate failures when requested.

### Validation and quality analysis

- [DONE] T114: Implement fixed-pattern damage measurement by module role and distinguish critical metadata damage from payload/ECC damage.
- [DONE] T115: Implement symbol contrast and modulation measurements from caller-supplied grayscale samples.
- [DONE] T116: Implement axial/grid non-uniformity and geometric distortion measurements from detected module centers.
- [DONE] T117: Implement print-growth and quiet-zone measurements with profile-specific nominal geometry.
- [DONE] T118: Implement unused-error-correction calculation from actual correction usage and block capacities.
- [DONE] T119: Implement profile-aware metric grading and overall-grade aggregation, explicitly returning NotGraded when calibrated inputs required by ISO/IEC 15415 are absent.
- [DONE] T120: Add `IQrQualityAnalyzer` and `QrQualityAnalyzer` for matrix and image-derived quality reports.

### Complete API-layer test coverage

- [SKIP] T121: Test every public enum value and every static data field against the licensed normative tables and valid profile combinations.
- [SKIP] T122: Test all public data models through real operations, including defaults, populated optional fields, array ownership, null handling, and diagnostic preservation.
- [DONE] T123: Test every exception-based API member for successful behavior and its specified invalid-input, unsupported-feature, capacity, checksum, sequence, and resource-limit failures.
- [DONE] T124: Test every exception-less API member for success/failure reason parity with its exception-based counterpart and absence of partial fabricated results.
- [DONE] T125: Test `IQrSegmenter`/`QrSegmenter` for every mode, boundary length, version-group count width, empty input, invalid character, manual control ordering, and optimizer tie.
- [DONE] T126: Exhaustively compare optimizer output with a brute-force segment enumerator for bounded mixed numeric/alphanumeric/byte/Kanji/ECI strings.
- [DONE] T127: Test every ECI designator width, common character mapping, multiple ECI switches, unsupported assignment preservation, malformed text, UTF-8 edge case, Shift JIS edge, and FNC1 interaction.
- [DONE] T128: Test GS1/FNC1 first and second position, percent escaping, group separators, application indicators, and symbology identifiers with GS1 vectors.
- [DONE] T129: Test `IQrCapacityCalculator`/`QrCapacityCalculator` at every exact-fit/one-bit-over boundary for every Model 2 version/ECC, Micro combination, and rMQR size/ECC.
- [DONE] T130: Test family/version/ECC boosting and forced-mask selection, including invalid combinations and stable tie behavior.
- [DONE] T131: Test `IQrReedSolomonCodec`/`QrReedSolomonCodec` against independent vectors for every ECC length, all-error, all-erasure, mixed-boundary, over-limit, and post-repair-verification cases.
- [DONE] T132: Test BCH format/version generation and recovery for all valid values, every correctable bit-error count, redundant-copy disagreement, and uncorrectable inputs.
- [DONE] T133: Test `IQrMatrixProcessor`/`QrMatrixProcessor` matrix generation and extraction for every Model 2 version/ECC/mask and all Micro/rMQR combinations using independent golden hashes.
- [DONE] T134: Test finder, separator, timing, alignment, dark-module, format/version, quiet-zone, reserved-cell, remainder, and module-role placement at all structural boundary versions/sizes.
- [DONE] T135: Test all matrix transforms and verify transform/inverse round trips, role/confidence transformation, non-square rMQR orientation, and invalid crop/scale rejection.
- [DONE] T136: Test `IQrEncoder`/`QrEncoder` round trips for text, arbitrary bytes, all modes, mixed modes, every family, forced parameters, maximum capacities, and deterministic repeatability.
- [DONE] T137: Cross-check generated Model 2 symbols with ZXing, Nayuki, libqrencode, and Segno test-side oracles and generated Micro/rMQR symbols with every applicable independent oracle.
- [DONE] T138: Test structured-append encoding for 2-16 parts, parity, optimizer partitioning, per-part ECI/FNC1, maximum payload, and illegal Micro/rMQR requests.
- [DONE] T139: Test `IQrRenderer`/`QrRenderer` exact raster dimensions, quiet zone, scale, polarity, pixel values, role maps, and caller option validation.
- [DONE] T140: Test `IQrBinarizer`/`QrBinarizer` for every pixel format/stride, alpha case, global/local thresholding, inversion, low contrast, uneven lighting, buffer bounds, and input immutability.
- [DONE] T141: Test `IQrDetector`/`QrDetector` and `IQrPerspectiveSampler`/`QrPerspectiveSampler` on clean, rotated, skewed, mirrored, inverted, cropped, blurred, noisy, low-resolution, high-resolution, and unevenly lit images.
- [DONE] T142: Test single-finder Micro QR, rectangular rMQR, legacy Model 1, missing/damaged alignment patterns, false finder patterns, perspective extremes, and quiet-zone tolerance/conformance reporting.
- [DONE] T143: Test multiple-symbol images with mixed profiles, sizes, orientations, polarities, overlaps, duplicate candidates, deterministic order, and configurable candidate limits.
- [DONE] T144: Test `IQrDecoder`/`QrDecoder` with all modes/control modes, every profile and boundary version, raw/text segment preservation, padding strictness, mirror/inversion, and direct-matrix/detection equivalence.
- [DONE] T145: Cross-check decoder results against ZXing, ZBar, and quirc fixture corpora where profiles overlap, while retaining cases those libraries do not support as native conformance vectors.
- [DONE] T146: Test damaged-symbol recovery by injecting errors and erasures into format, version, function, data, and ECC modules up to and beyond each provable correction boundary.
- [DONE] T147: Test recovery retry ordering and budgets across competing thresholds, sampling shifts, homographies, polarity/mirror choices, and adversarial uncorrectable matrices.
- [DONE] T148: Test `IQrStructuredAppendAssembler`/`QrStructuredAppendAssembler` for unordered complete sets, missing parts, exact duplicates, conflicts, mixed messages, ECI boundaries, GS1 data, and aggregate diagnostics.
- [DONE] T149: Test `IQrScanner`/`QrScanner` end to end for no symbol, one symbol, many symbols, mixed successes/failures, structured append across one image, and raw-image versus binary-matrix parity.
- [DONE] T150: Test `IQrQualityAnalyzer`/`QrQualityAnalyzer` for every metric/grade boundary, absent calibration, fixed-pattern damage, modulation, non-uniformity, print growth, quiet zone, and unused ECC.
- [DONE] T151: Add deterministic malformed-input tests for truncated bitstreams, reserved modes, impossible counts, invalid ECI, inconsistent dimensions/versions, corrupt block layouts, invalid strides, and oversized allocation requests across all public entry points.
- [DONE] T152: Add deterministic randomized encode/decode/property tests over modes, options, transforms, noise, damage, and all symbol families with captured seeds for failures.
- [DONE] T153: Configure line/branch coverage collection and require complete functional coverage of every method exposed by `src/Api`, with explicit behavioral cases for all reachable results and failures.
- [DONE] T154: Run the full test suite with checked arithmetic enabled and eliminate all compiler warnings, test failures, nondeterministic fixtures, and uncovered public API behaviors.
