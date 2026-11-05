# Universal barcode library development plan

## 1. Objective and completion definition

Build a dependency-free-under-`src/`, managed C# library for encoding, decoding, rendering, detecting, parsing, and validating the barcode families covered by the standards baseline below. “Universal” means one consistent data model and processing pipeline for every supported linear, matrix, and stacked-linear symbology; it does not mean every barcode symbology ever published.

The first complete release is done when all of the following are true:

- Code 128, GS1-128, EAN-8, EAN-13, UPC-A, UPC-E, EAN/UPC two- and five-digit add-ons, Code 39, ITF, ITF-14, Codabar, Data Matrix ECC 200, GS1 DataMatrix, PDF417, Compact PDF417, and Macro PDF417 can be encoded, decoded from a pure module pattern, detected and decoded from an image, rendered, and validated through the public API.
- All mandatory checksums, Reed-Solomon error correction, quiet zones, symbol dimensions, start/stop/finder patterns, compaction rules, human-readable interpretation, GS1 Application Identifier rules, and symbology identifiers are implemented.
- Image processing handles multiple symbols, region-of-interest restriction, normal/reversed polarity, mirrored symbols where the symbology permits recovery, right-angle rotation, arbitrary in-plane rotation, moderate skew/perspective, blur/noise/uneven illumination, and damaged symbols within their error-correction capability.
- Every public type and every observable branch of every public API operation has positive, boundary, negative, round-trip, malformed-input, and interoperability coverage.
- `src/` references only the .NET runtime libraries. External libraries and native command-line tools may be used only by `tests/` as independent oracles and fixture generators.

The library will not claim calibrated ISO/IEC 15415 or ISO/IEC 15416 verifier certification. It will report useful image-quality and conformance diagnostics, but certified print-quality grading requires calibrated illumination, aperture, reflectance measurements, and equipment outside this software-only scope.

## 2. Normative baseline and scope

Implementation must use the current edition or amendment available when this plan was written (2026-08-27):

| Standard | Planned coverage |
| --- | --- |
| ISO/IEC 15417:2007 plus Amendment 1:2026 | Code 128 patterns, code sets A/B/C, functions, dimensions, checksum, reference decoding, identifiers |
| ISO/IEC 15420:2025 | EAN-13, EAN-8, UPC-A, UPC-E, add-ons, parity, guards, dimensions, reference decoding, identifiers |
| ISO/IEC 16388:2023 | Code 39 character set, patterns, optional modulo-43 check, dimensions, reference decoding, identifiers |
| ISO/IEC 16022:2024 | Data Matrix ECC 200 encodation, placement, sizes, Reed-Solomon correction, decoding, application parameters |
| ISO/IEC 15438:2015 | PDF417, Compact PDF417, compaction, dimensions, row indicators, Reed-Solomon correction, Macro PDF417 |
| ISO/IEC 16390:2007 | Interleaved 2 of 5 patterns, dimensions, optional check character, reference decoding, identifiers |
| ISO/IEC 15424:2025 | AIM data-carrier/symbology identifier prefixes and modifier meanings |
| AIM BC3-2000 / USS Codabar | Rationalized Codabar patterns, start/stop variants, dimensions, optional modulo-16 check |
| GS1 General Specifications 26.0 | GS1 data syntax, GS1-128, GS1 DataMatrix, EAN/UPC, ITF-14, GTIN/check digits, sizing, HRI, application profiles |
| GS1 Barcode Syntax Dictionary, 2026-08-26 snapshot | Complete assigned AI table, component formats, linters, required/excluded associations, Digital Link qualifiers |
| GS1 Digital Link URI Syntax 1.7.0 | Conversion between AI element strings and canonical uncompressed GS1 Digital Link URIs |

DMRE, MicroPDF417, GS1 DataBar, GS1 Composite, QR Code, GS1 QR Code, Aztec, MaxiCode, and postal symbologies are extension points, not first-release codecs, because their governing symbology standards are outside the requested baseline. Data Matrix rectangular sizes defined by ISO/IEC 16022 are in scope; the additional DMRE sizes governed by ISO/IEC 21471 are not.

## 3. Research findings and design consequences

| Library | Relevant strengths found | Consequence for this plan |
| --- | --- | --- |
| ZXing | Unified reader/writer abstractions, pure-symbol and image paths, format hints, result points/metadata, multi-format dispatch, 1D scanline readers, Data Matrix and PDF417 support | Use a common request/result model, format filtering, metadata, pure-module entry points, and a scanline abstraction |
| ZBar | Streaming/raw-intensity decoding, low-level width scanning, format enablement, EAN/UPC normalization, multi-symbol image reads | Keep pixel ingestion independent of image-file frameworks; support scanline accumulation and explicit format/length filters |
| Zint | Broad standards fidelity, GS1 mode, ECI, structured append, optimal encodation, exact sizing, HRT controls, raster and vector output | Treat layout and rendering as separate stages; expose ECI/structured append and physical sizing; cross-check encoder vectors against Zint |
| Dynamsoft Barcode Reader | Multiple localization strategies, ROI and expected-count controls, grayscale/binarization/deblur modes, inversion/mirroring, deformation resistance, DPM handling, confidence and multi-result ordering | Build a configurable image pipeline with fast and robust paths, candidate confidence, preprocessing fallbacks, perspective correction, and deduplication |
| Barcode4J | Generator architecture separating barcode beans/layout from canvas providers; SVG and raster-oriented output; mature 1D/PDF417/Data Matrix generation | Keep symbology layout device-independent and let renderers consume bars/modules/text primitives |
| bwip-js/BWIPP | More than 100 encoder profiles, rich text layout, guard-bar handling, GS1 options, SVG/PNG/canvas backends | Represent HRT as positioned glyph runs and render contiguous dark modules efficiently; use BWIPP fixtures as a secondary encoder oracle |
| ZXing.Net | .NET-oriented reader/writer facade, decoding options, allowed lengths/add-ons, custom renderers, platform-neutral luminance input | Keep public data classes framework-neutral, provide exception-less decode attempts, and avoid `System.Drawing` in the source API |

The comparison also exposes traps to avoid: silently accepting invalid check digits, conflating UPC-A with EAN-13 without preserving the observed format, adding quiet zones twice, stretching modules by non-integral raster scaling, losing raw bytes during text conversion, accepting a decoded payload before verifying its structure, and presenting heuristic image confidence as standards conformance.

## 4. Complete feature inventory, low level to high level

### 4.1 Binary, numeric, and geometric foundations

- Checked integer arithmetic and capacity calculations that fail with domain exceptions instead of wrapping.
- Packed and unpacked bit rows, mutable module matrices, row/column extraction, rotation, reflection, crop, border insertion, and dark-region bounds.
- Run-length conversion in both directions, normalized pattern variance, guard/finder matching, and forward/reverse traversal.
- `ReadOnlySpan<byte>`, `Span<byte>`, and `Span<int>` in internal hot paths when data need not escape a call; arrays in public data holders because spans cannot be stored there.
- GF(256) arithmetic and polynomial operations for Data Matrix Reed-Solomon coding.
- Modulo-929 arithmetic, polynomial operations, erasure handling, and Euclidean error-location/error-magnitude solving for PDF417.
- CRC-32 and Adler-32 helpers for built-in PNG writing, not barcode payload validation.
- Integer and floating-point points, rectangles, quadrilaterals, line intersections, affine transforms, perspective transforms, grid sampling, and clipping.
- Deterministic luminance conversion for Gray8, RGB24, BGR24, RGBA32, and BGRA32 buffers with explicit stride and bounds validation.

### 4.2 Shared barcode model and behavior

- Text and arbitrary-byte payloads, preserved raw bytes, selected character encoding/ECI, symbology, GS1 state, structured-append metadata, checksum metadata, and symbology identifier.
- Device-independent layouts containing logical modules, bar rectangles, finder/guard annotations, quiet-zone requirements, bearer bars, and positioned HRT glyph runs.
- Automatic format selection only when explicitly requested; deterministic tie-breaking and preservation of the exact detected format.
- Encoding options for forced or automatic size, target aspect ratio, module/X dimension, bar height, quiet-zone policy, checksum policy, ECI, structured append, HRT, and per-symbology settings.
- Decoding options for allowed formats and lengths, ROI, maximum results, expected result count, pure-symbol mode, checksum strictness, GS1 parsing, character encoding, inversion, mirroring, rotation, and effort level.
- Exception-based encode/render/configuration failures plus exception-less validation and decode-attempt results for frequent expected failures.
- Multiple-symbol results, stable reading order, duplicate suppression, confidence, corrected-error count, result points, bounding quadrilateral, orientation, skew, polarity, and diagnostic messages.

### 4.3 Checksums and identifiers

- GS1 modulo-10 check-digit calculation and verification for GTIN-8/12/13/14, SSCC, GLN, GRAI, GIAI components, and every AI component marked `csum` by the syntax dictionary.
- EAN/UPC modulo-10 validation, with optional calculation when the caller supplies one fewer digit and strict rejection of a wrong supplied digit.
- Code 128 weighted modulo-103 symbol checksum.
- Code 39 optional modulo-43 checksum, with configurable transmit/strip behavior.
- ITF optional GS1 modulo-10 check, required for ITF-14.
- Codabar optional modulo-16 checksum, with configurable transmit/strip behavior.
- Data Matrix ECC 200 Reed-Solomon generation/correction and PDF417 modulo-929 Reed-Solomon generation/correction.
- ISO/IEC 15424 symbology identifier generation and parsing, including modifiers such as `]C0`/`]C1`, `]E0`, `]I0`/`]I1`, `]d1`/`]d2`, and PDF417 identifiers as applicable.

### 4.4 Code 128 and GS1-128

- Complete 107-symbol pattern table, start A/B/C, stop and termination bar, Code A/B/C switches, SHIFT, FNC1, FNC2, FNC3, and FNC4.
- Code sets A and B for the complete ISO/IEC 646 repertoire; Code C numeric-pair compaction; upper-shift/single-shift FNC4 behavior for ISO-8859-1 bytes.
- Optimal automatic code-set selection using dynamic programming, including numeric runs, control characters, function characters, and deterministic tie-breaking; forced-set validation for A, B, or C.
- Modulo-103 checksum, 11-module symbols, 13-module stop, minimum quiet zones, X-dimension/bar-height constraints, maximum physical/symbol lengths, and HRT.
- Scanline decoding in both directions with start detection, six-element pattern variance, stop validation, checksum verification, quiet-zone validation, and raw codeword metadata.
- GS1-128 leading FNC1, AI concatenation, separator FNC1 insertion after non-terminal variable-length values, GS1-recommended element ordering, symbol capacity checks, `]C1` output, and parenthesized HRI that never encodes the parentheses.

### 4.5 EAN/UPC family

- EAN-13, EAN-8, UPC-A, UPC-E number systems 0 and 1, UPC-E expansion/compression, and lossless UPC-A/EAN-13 normalization metadata.
- L/G/R digit patterns, leading-digit parity, UPC-E number-system/check-digit parity, left/centre/right guards, add-on guards and separators, and guard-bar descent.
- Two-digit add-on parity from value modulo 4 and five-digit add-on checksum/parity; configurable required/optional allowed add-on lengths.
- Exact digit counts, automatic check-digit completion, strict check validation, number-system restrictions, and rejection of impossible UPC-E compression.
- Standard module counts, asymmetric quiet zones/quiet-zone indicators, nominal X dimensions, magnification/sizing, bar heights, add-on alignment/separation, HRT digit grouping, and check-digit placement.
- Bidirectional scanline decoding, guard and digit variance, parity reconstruction, quiet-zone enforcement, add-on association, and exact observed-format reporting.

### 4.6 Code 39

- Standard 43-character alphabet plus start/stop, nine-element 3-wide-of-9 patterns, configurable 2.0:1 through 3.0:1 wide:narrow ratio, intercharacter gap, quiet zones, and HRT.
- Optional modulo-43 check generation/validation and transmit/strip modes.
- Full ASCII encoding and decoding through shift-character pairs, including control characters, with strict rejection of malformed pairs.
- Optional concatenation/append metadata without silently joining unrelated symbols.
- Bidirectional scanline decoding with adaptive narrow/wide classification, asterisk validation, intercharacter-gap checks, quiet-zone checks, and ISO/IEC 15424 modifiers.

### 4.7 ITF and ITF-14

- Interleaved digit-pair patterns, start/stop patterns, even-length requirement, configurable narrow:wide ratio, quiet zones, bar-height rules, and optional check digit.
- ITF-14 fixed 14-digit GTIN content, mandatory GS1 check digit, `]I1` identifier, minimum size profile, rectangular or top/bottom bearer bars, bearer-bar thickness, HRT grouping, and corrugated-printing profile.
- Partial-read defenses through exact allowed length and quiet-zone enforcement.
- Bidirectional scanline decoding using ten-run digit pairs, adaptive wide/narrow thresholds, start/stop validation, checksum/length policies, and bearer-bar-tolerant localization.

### 4.8 Codabar

- Rationalized Codabar data alphabet `0-9-$:/.+`, start/stop A/B/C/D (including common T/N/*/E aliases at input), seven-element patterns, intercharacter gaps, wide:narrow ratio, quiet zones, and HRT.
- Caller-selected or preserved start/stop characters, optional modulo-16 checksum, and configurable start/stop/check transmission.
- Bidirectional scanline decoding with adaptive width classification, valid start/stop pairing, minimum-length and quiet-zone checks, and `]F` symbology identifier handling.

### 4.9 Data Matrix ECC 200 and GS1 DataMatrix

- ASCII, C40, Text, ANSI X12, EDIFACT, and Base 256 encodation/decodation, digit-pair compaction, upper shift, latch/unlatch, padding, and 253-state/255-state randomization.
- Encoder optimization across modes, deterministic tie-breaking, explicit forced mode, raw bytes, ISO-8859-1 default interpretation, ECI designators, Macro 05/06, FNC1, reader programming, and structured append of up to 16 symbols.
- All ISO/IEC 16022 ECC 200 square sizes 10x10 through 144x144 and standard rectangular sizes 8x18 through 16x48, automatic/forced size, capacity selection, data regions, finder/timing borders, and one-module quiet zone.
- Utah/corner module placement and extraction, fixed modules, block interleaving, GF(256) Reed-Solomon generation, syndrome detection, erasure-assisted correction, corrected-error counts, and uncorrectable reporting.
- Pure-module decode under 0/90/180/270 rotation and reflection, border validation, dimension inference, and de-randomization.
- Image localization by connected components and L-shaped solid-border/alternating-border evidence, candidate quadrilateral refinement, projective grid sampling, module-centre voting, polarity inversion, moderate deformation recovery, and multiple-symbol support.
- DPM-oriented low-contrast, dot-peen, separated-module, glare, and textured-background preprocessing modes using only managed image operations.
- GS1 DataMatrix first-position FNC1, `]d2` identifier, AI element strings, separators, HRI, and GS1 sizing/application constraints.

### 4.10 PDF417

- Text compaction with Alpha/Lower/Mixed/Punctuation submodes, byte compaction modes 901/924 and byte shift 913, numeric compaction with base-900/base-10 conversion, automatic optimal compaction, and explicit compaction selection.
- ECI charset/general-purpose/user-defined designators with preserved raw bytes and decoded segments.
- 1-30 data columns, 3-90 rows, 17 modules per codeword, three row clusters, start/stop patterns, left/right row indicators, length descriptor, padding, automatic dimensions/aspect ratio, and Compact PDF417 stop layout.
- Error-correction levels 0-8, recommended automatic level, modulo-929 Reed-Solomon generation, erasure-assisted correction, ambiguity resolution, corrected-error/erasure metadata, and uncorrectable reporting.
- Macro PDF417 segment index, file ID, last-segment flag, optional file name, segment count, timestamp, sender, addressee, file size, and checksum; out-of-order multi-symbol assembly and conflict/duplicate detection.
- Pure-module and scanline row decode, start/stop localization, codeword bucket/cluster validation, row-number/column-count/error-level metadata recovery, skewed-row reconciliation, missing-row tolerance, and checksum/error correction.
- Image localization through start/stop line evidence, arbitrary rotation/skew estimation, quadrilateral refinement, perspective row sampling, inverted/mirrored attempts, multiple symbols, and truncated-right-side recovery for Compact PDF417.

### 4.11 GS1 semantic processing

- Machine-generated static AI data from the checked-in GS1 Syntax Dictionary snapshot; no runtime network dependency.
- Complete two-, three-, and four-digit AI recognition, wildcard/decimal-indicator AIs, fixed/variable component lengths, character sets 39/64/82/numeric, FNC1/GS separators, and maximum-length enforcement.
- Component linters represented by the GS1 reference test corpus: check sums, dates/times, country and currency codes, coordinates, IBAN, media types, package types, piece-of-total, non-zero/zero rules, and specialized coupon fields.
- Required-AI, excluded-AI, repeated-AI, primary-key, key-qualifier, and Digital Link path/data-attribute association rules.
- Parse and build bracketed AI syntax, unbracketed element strings, barcode messages, scanner data with symbology identifiers, HRI text, and canonical uncompressed GS1 Digital Link URIs.
- Correct distinction among FNC1 symbol character, ASCII GS separator, caret placeholder used by tooling, and literal user data.
- GS1 key check digits, HRI titles/parentheses, data ordering, unknown/future AI policy, rich issue locations, and round-trip preservation.

### 4.12 Device-independent layout and rendering

- A layout stage that adds required quiet zones exactly once, applies physical sizing and integral pixel rounding, extends guard bars, adds ITF bearer bars, reserves HRT, and reports actual output size.
- Raw 1-bit and Gray8 raster output plus standards-compliant PNG and BMP byte streams using only `System.IO`/`System.IO.Compression`; configurable foreground/background RGBA colors and transparent background where the format permits.
- SVG output with integer view box, `shape-rendering="crispEdges"`, merged runs/rectangles, escaped metadata/text, optional background, configurable colors, and no scripting/external references.
- Rotation by 0/90/180/270 degrees without interpolation, nearest-neighbour integral scaling, explicit DPI and millimetre conversion, minimum-module protection, and refusal of distortion that changes module ratios.
- Built-in deterministic glyph outlines/bitmaps for raster HRT so results do not depend on installed fonts; SVG can emit either those paths or escaped text according to options.
- Symbology-specific HRT: EAN/UPC outside digits and grouping, add-ons, Code 128/39/ITF/Codabar text, GS1 parenthesized AIs, optional check/start/stop display, alignment, gap, and suppression.

### 4.13 Image ingestion, preprocessing, localization, and decoding orchestration

- Caller-owned pixel buffers with width, height, stride, format, offset, optional DPI, and ROI validation; no dependency on `Bitmap`, UI frameworks, cameras, or file codecs.
- Grayscale conversion, histogram/contrast analysis, inversion detection, global/Otsu thresholding, row thresholding, adaptive tile/hybrid thresholding, illumination normalization, box smoothing, unsharp enhancement, and small-kernel morphology.
- Fast direct scan followed by configurable connected-block, line-statistics, and finder-pattern localization fallbacks; scale pyramid and ROI mapping back to source coordinates.
- 1D candidate localization from coherent parallel bar transitions, orientation histograms, scanline fan-out, multi-row voting, and partial-candidate rejection.
- Data Matrix and PDF417 specialized localizers, arbitrary in-plane rotation, affine/projective deskew, perspective sampling, moderate local grid refinement, inversion/mirror attempts, and result coordinate restoration.
- Candidate prioritization by format evidence, quiet-zone evidence, contrast, geometric consistency, checksum/ECC status, and decode agreement; maximum-result and expected-count stopping controls.
- Duplicate suppression using payload, format, and geometric overlap; stable top-to-bottom/left-to-right result ordering.
- Diagnostics for preprocessing path, threshold, candidate rejection, checksum/ECC, quiet zones, geometry, and confidence without leaking mutable internal buffers.

### 4.14 Validation and quality diagnostics

- Pre-encode data validation separated from encoding; all issues returned together when practical, with severity, machine-readable code, field/AI, offset, expected constraint, and observed value.
- Encoded-symbol validation for dimensions, capacities, patterns, codewords, checksum/ECC, finder/guard/row indicators, quiet zones, bearer bars, and GS1 application profile.
- Render-plan validation for X dimension, magnification, bar/module height, aspect ratio, quiet zones, HRT clearance, output pixel rounding, color contrast, and target DPI.
- Image validation for detected bounds, quiet-zone infringement, module-size consistency, skew, perspective, contrast, modulation proxy, fixed-pattern damage, axial/grid nonuniformity proxy, unused ECC, decode confidence, and truncation.
- Strict conformance mode and tolerant recovery mode must be distinct; tolerant decoding never turns an invalid symbol into a conformant validation result.
- Validation reports identify the governing standard/profile and explicitly label heuristic image metrics as diagnostics rather than certified grades.

## 5. Public API proposal and planned public files

The API deliberately uses public fields rather than properties, has no overloads, uses one constructor at most per class, and gives every interface member a unique name. Every public implementation member is declared by an implemented interface. Public data classes contain fields only (plus possible internal helpers), and every public data class/enum is consumed by an interface or another public data class.

### 5.1 Enums under `src/Api/Enums`

| Planned file/type | Purpose |
| --- | --- |
| `BarcodeFormat.cs` / `BarcodeFormat` | Exact supported symbology/profile, including GS1 variants and add-ons |
| `BarcodeDimension.cs` / `BarcodeDimension` | Linear, matrix, or stacked-linear classification |
| `BarcodePixelFormat.cs` / `BarcodePixelFormat` | Gray8/RGB/BGR/RGBA/BGRA input and raster formats |
| `BarcodeRotation.cs` / `BarcodeRotation` | Right-angle render/decode rotation |
| `BarcodeMirrorMode.cs` / `BarcodeMirrorMode` | Normal, mirrored, or both decode attempts |
| `BarcodePolarity.cs` / `BarcodePolarity` | Dark-on-light, light-on-dark, or automatic |
| `BarcodeEffort.cs` / `BarcodeEffort` | Fast, balanced, thorough image pipeline |
| `BarcodeBinarizationMode.cs` / `BarcodeBinarizationMode` | Global, Otsu, row, adaptive, hybrid, automatic |
| `BarcodeLocalizationMode.cs` / `BarcodeLocalizationMode` | Direct, connected blocks, lines, finder patterns, automatic |
| `BarcodeChecksumMode.cs` / `BarcodeChecksumMode` | None, calculate, require, verify/keep, verify/strip |
| `BarcodeQuietZonePolicy.cs` / `BarcodeQuietZonePolicy` | Standard, custom-at-least-standard, exact custom for diagnostics |
| `BarcodeTextMode.cs` / `BarcodeTextMode` | Hidden, standard HRT, raw payload, custom text |
| `BarcodeRenderFormat.cs` / `BarcodeRenderFormat` | Raw1, Gray8, PNG, BMP, SVG |
| `BarcodeValidationSeverity.cs` / `BarcodeValidationSeverity` | Information, warning, error |
| `BarcodeValidationMode.cs` / `BarcodeValidationMode` | Strict conformance or tolerant diagnostic analysis |
| `Code128CodeSet.cs` / `Code128CodeSet` | Automatic, A, B, C |
| `Code39CharacterMode.cs` / `Code39CharacterMode` | Standard or Full ASCII |
| `DataMatrixShape.cs` / `DataMatrixShape` | Automatic, square, standard rectangular |
| `DataMatrixEncodingMode.cs` / `DataMatrixEncodingMode` | Automatic, ASCII, C40, Text, X12, EDIFACT, Base256 |
| `Pdf417CompactionMode.cs` / `Pdf417CompactionMode` | Automatic, text, byte, numeric |
| `Pdf417Variant.cs` / `Pdf417Variant` | Standard or Compact |
| `Gs1Syntax.cs` / `Gs1Syntax` | Bracketed, element string, barcode message, scanner data, HRI, Digital Link URI |

### 5.2 Data holders under `src/Api/Data`

| Planned file/type | Purpose |
| --- | --- |
| `BarcodePayload.cs` / `BarcodePayload` | Text/raw bytes, encoding name, ECI segments, GS1 flag |
| `EciSegment.cs` / `EciSegment` | ECI assignment and byte range |
| `StructuredAppendInfo.cs` / `StructuredAppendInfo` | Shared sequence/file metadata |
| `BarcodeEncodeRequest.cs` / `BarcodeEncodeRequest` | Format, payload, general and symbology options |
| `BarcodeEncodeOptions.cs` / `BarcodeEncodeOptions` | Sizing, quiet zone, checksum, HRT, ECI, structured append |
| `Code128Options.cs` / `Code128Options` | Code-set and function-character behavior |
| `EanUpcOptions.cs` / `EanUpcOptions` | Check-digit, add-on, UPC-E and guard behavior |
| `Code39Options.cs` / `Code39Options` | character mode, ratio, checksum, append |
| `ItfOptions.cs` / `ItfOptions` | ratio, length/check policy, ITF-14 bearer bars |
| `CodabarOptions.cs` / `CodabarOptions` | start/stop, aliases, ratio, modulo-16 behavior |
| `DataMatrixOptions.cs` / `DataMatrixOptions` | shape/size, encodation, ECI, Macro, structured append |
| `Pdf417Options.cs` / `Pdf417Options` | dimensions, variant, compaction, ECC, Macro fields |
| `BarcodeSizing.cs` / `BarcodeSizing` | requested modules/pixels/mm/DPI, height and aspect controls |
| `BarcodeQuietZone.cs` / `BarcodeQuietZone` | four-sided module/pixel quiet-zone values |
| `BarcodeSymbol.cs` / `BarcodeSymbol` | logical matrix/layout, payload and encoder metadata |
| `BarcodeSize.cs` / `BarcodeSize` | logical and physical dimensions actually selected |
| `BarcodeTextLayout.cs` / `BarcodeTextLayout` | positioned HRT runs and reserved area |
| `BarcodeTextRun.cs` / `BarcodeTextRun` | one HRT string and placement |
| `BarcodeImage.cs` / `BarcodeImage` | raw caller-provided pixels, dimensions, stride, format, DPI |
| `BarcodePoint.cs` / `BarcodePoint` | image-space coordinate |
| `BarcodeRectangle.cs` / `BarcodeRectangle` | ROI and bounds |
| `BarcodeQuadrilateral.cs` / `BarcodeQuadrilateral` | four result/candidate corners |
| `BarcodeDecodeRequest.cs` / `BarcodeDecodeRequest` | image or pure symbol plus decoding options |
| `BarcodeDecodeOptions.cs` / `BarcodeDecodeOptions` | formats, ROI, effort, transforms, checks, result limits |
| `BarcodeDecodeAttempt.cs` / `BarcodeDecodeAttempt` | exception-less success flag, results, diagnostics |
| `BarcodeDecodeResult.cs` / `BarcodeDecodeResult` | payload, exact format, location, orientation and metadata |
| `BarcodeDecodeMetadata.cs` / `BarcodeDecodeMetadata` | checksum/ECC, identifier, codewords, confidence, GS1 result |
| `BarcodeDetectionRequest.cs` / `BarcodeDetectionRequest` | image and localization-only options |
| `BarcodeDetectionOptions.cs` / `BarcodeDetectionOptions` | modes, ROI, formats, count, effort |
| `BarcodeDetectionResult.cs` / `BarcodeDetectionResult` | candidate list and diagnostics |
| `BarcodeCandidate.cs` / `BarcodeCandidate` | proposed format, quadrilateral, angle, confidence, polarity |
| `BarcodeDiagnostic.cs` / `BarcodeDiagnostic` | stage/code/message/numeric evidence |
| `BarcodeRenderRequest.cs` / `BarcodeRenderRequest` | symbol and renderer options |
| `BarcodeRenderOptions.cs` / `BarcodeRenderOptions` | output format, scaling, rotation, colors, HRT behavior |
| `BarcodeRenderedImage.cs` / `BarcodeRenderedImage` | bytes/text, MIME type, pixel/physical dimensions |
| `BarcodeValidationRequest.cs` / `BarcodeValidationRequest` | data, symbol, render plan, or image validation target |
| `BarcodeValidationReport.cs` / `BarcodeValidationReport` | conformance status, standard/profile, issues and metrics |
| `BarcodeValidationIssue.cs` / `BarcodeValidationIssue` | severity, code, location, expected/observed values |
| `BarcodeQualityMetric.cs` / `BarcodeQualityMetric` | named diagnostic value, threshold and interpretation |
| `BarcodeChecksumRequest.cs` / `BarcodeChecksumRequest` | algorithm/format and source data |
| `BarcodeChecksumResult.cs` / `BarcodeChecksumResult` | calculated/observed value and validity |
| `Gs1Element.cs` / `Gs1Element` | AI, value, components, definition and source offsets |
| `Gs1Message.cs` / `Gs1Message` | ordered elements, detected syntax, identifier and issues |
| `Gs1ProcessRequest.cs` / `Gs1ProcessRequest` | source text/bytes, source and target syntax, policy |
| `Gs1ProcessResult.cs` / `Gs1ProcessResult` | parsed message and requested serialization |
| `Gs1ApplicationIdentifierInfo.cs` / `Gs1ApplicationIdentifierInfo` | public AI definition lookup result |
| `StructuredAppendRequest.cs` / `StructuredAppendRequest` | decoded segments to assemble |
| `StructuredAppendResult.cs` / `StructuredAppendResult` | complete/incomplete assembly, payload, conflicts/missing indices |

### 5.3 Operation contracts under `src/Api/Contracts`

| Planned file/interface | Operations, without signature detail |
| --- | --- |
| `IBarcodeEncoder.cs` / `IBarcodeEncoder` | `EncodeBarcode`, `TryEncodeBarcode` |
| `IBarcodeDecoder.cs` / `IBarcodeDecoder` | `DecodeBarcode`, `TryDecodeBarcode` |
| `IBarcodeDetector.cs` / `IBarcodeDetector` | `DetectBarcodes` |
| `IBarcodeRenderer.cs` / `IBarcodeRenderer` | `RenderBarcode` |
| `IBarcodeValidator.cs` / `IBarcodeValidator` | `ValidateBarcodeData`, `ValidateBarcodeSymbol`, `ValidateBarcodeRender`, `ValidateBarcodeImage` |
| `IBarcodeChecksumCalculator.cs` / `IBarcodeChecksumCalculator` | `CalculateBarcodeChecksum`, `VerifyBarcodeChecksum` |
| `IGs1DataProcessor.cs` / `IGs1DataProcessor` | `ProcessGs1Data`, `LookupGs1ApplicationIdentifier` |
| `IStructuredAppendAssembler.cs` / `IStructuredAppendAssembler` | `AssembleStructuredAppend` |

### 5.4 Static data under `src/Api/Static`

| Planned file/type | Fields only |
| --- | --- |
| `BarcodeStandards.cs` / `BarcodeStandards` | Standard names, editions, amendment and GS1 snapshot version |
| `BarcodeSymbologyIdentifiers.cs` / `BarcodeSymbologyIdentifiers` | Public identifier prefix constants |
| `BarcodeMediaTypes.cs` / `BarcodeMediaTypes` | PNG, BMP, SVG, raw raster media-type constants |

### 5.5 Exceptions under `src/Api/Exceptions`

Each exception derives directly from `System.Exception` and has at most one constructor: `BarcodeEncodingException.cs`, `BarcodeDecodingException.cs`, `BarcodeDetectionException.cs`, `BarcodeRenderingException.cs`, `BarcodeValidationException.cs`, `Gs1SyntaxException.cs`, and `UnsupportedBarcodeFormatException.cs`.

### 5.6 Public implementations under `src/Api/Classes`

| Planned file/class | Implements |
| --- | --- |
| `BarcodeEncoder.cs` / `BarcodeEncoder` | `IBarcodeEncoder` |
| `BarcodeDecoder.cs` / `BarcodeDecoder` | `IBarcodeDecoder` |
| `BarcodeDetector.cs` / `BarcodeDetector` | `IBarcodeDetector` |
| `BarcodeRenderer.cs` / `BarcodeRenderer` | `IBarcodeRenderer` |
| `BarcodeValidator.cs` / `BarcodeValidator` | `IBarcodeValidator` |
| `BarcodeChecksumCalculator.cs` / `BarcodeChecksumCalculator` | `IBarcodeChecksumCalculator` |
| `Gs1DataProcessor.cs` / `Gs1DataProcessor` | `IGs1DataProcessor` |
| `StructuredAppendAssembler.cs` / `StructuredAppendAssembler` | `IStructuredAppendAssembler` |

## 6. Planned internal source files

All types outside `src/Api` are non-public. Files are grouped by responsibility so codec logic, image heuristics, and output formats remain independently testable.

```text
src/Internal/Core/
  BitRow.cs ModuleMatrix.cs RunLength.cs PatternMatcher.cs CheckedMath.cs
  BarcodeDispatcher.cs EncodingContext.cs DecodingContext.cs LayoutBuilder.cs
src/Internal/Geometry/
  GeometryMath.cs LineSegment.cs AffineTransform.cs PerspectiveTransform.cs GridSampler.cs
src/Internal/ErrorCorrection/
  GaloisField256.cs GaloisPolynomial256.cs DataMatrixReedSolomon.cs
  ModulusField929.cs ModulusPolynomial929.cs Pdf417ReedSolomon.cs
src/Internal/Checksums/
  Gs1Modulo10.cs Code128Modulo103.cs Code39Modulo43.cs CodabarModulo16.cs
src/Internal/Code128/
  Code128Patterns.cs Code128Planner.cs Code128Encoder.cs Code128Decoder.cs Code128Layout.cs
src/Internal/EanUpc/
  EanUpcPatterns.cs EanUpcParity.cs EanUpcEncoder.cs EanUpcDecoder.cs
  UpcECompressor.cs EanUpcAddon.cs EanUpcLayout.cs
src/Internal/Code39/
  Code39Patterns.cs Code39FullAscii.cs Code39Encoder.cs Code39Decoder.cs Code39Layout.cs
src/Internal/Itf/
  ItfPatterns.cs ItfEncoder.cs ItfDecoder.cs Itf14Profile.cs ItfLayout.cs
src/Internal/Codabar/
  CodabarPatterns.cs CodabarEncoder.cs CodabarDecoder.cs CodabarLayout.cs
src/Internal/DataMatrix/
  DataMatrixSymbolInfo.cs DataMatrixHighLevelEncoder.cs DataMatrixMinimalEncoder.cs
  DataMatrixHighLevelDecoder.cs DataMatrixPlacement.cs DataMatrixBlockInterleaver.cs
  DataMatrixEncoder.cs DataMatrixDecoder.cs DataMatrixDetector.cs DataMatrixLayout.cs
src/Internal/Pdf417/
  Pdf417CodewordTable.cs Pdf417TextCompaction.cs Pdf417ByteCompaction.cs
  Pdf417NumericCompaction.cs Pdf417CompactionPlanner.cs Pdf417Dimensions.cs
  Pdf417RowIndicators.cs Pdf417MacroCodec.cs Pdf417Encoder.cs Pdf417ScanningDecoder.cs
  Pdf417Detector.cs Pdf417AmbiguityResolver.cs Pdf417Layout.cs
src/Internal/Gs1/
  Gs1AiTable.cs Gs1AiTrie.cs Gs1DictionaryGeneratorInput.cs Gs1ElementParser.cs
  Gs1ElementBuilder.cs Gs1ScannerData.cs Gs1DigitalLink.cs Gs1Hri.cs
  Gs1AssociationValidator.cs Gs1ComponentLinters.cs Gs1ProfileValidator.cs
src/Internal/Image/
  LuminanceSource.cs HistogramAnalyzer.cs GlobalBinarizer.cs OtsuBinarizer.cs
  RowBinarizer.cs AdaptiveBinarizer.cs ImageNormalizer.cs ImageFilter.cs Morphology.cs
  ImagePyramid.cs ScanlineSampler.cs LinearLocalizer.cs ConnectedBlockLocalizer.cs
  CandidateMerger.cs CandidateScorer.cs ResultDeduplicator.cs ImageDecodePipeline.cs
src/Internal/Rendering/
  RenderPlanner.cs RasterCanvas.cs PngWriter.cs BmpWriter.cs SvgWriter.cs
  RectangleCoalescer.cs BuiltInGlyphs.cs HumanReadableTextRenderer.cs
src/Internal/Validation/
  DataValidator.cs SymbolValidator.cs RenderValidator.cs ImageQualityAnalyzer.cs
  ValidationIssueBuilder.cs
```

The GS1 AI table may be mechanically regenerated outside `src/`, but the generated `Gs1AiTable.cs` checked into `src/Internal/Gs1` is ordinary policy-compliant C# with no runtime generator or external dependency.

## 7. Test architecture and coverage definition

- NUnit is the primary test runner. Tests are divided into `tests/Unit`, `tests/Conformance`, `tests/Images`, `tests/Interoperability`, and `tests/Api`.
- Every public operation gets a contract test matrix for null/empty/default data, every enum value, minimum and maximum sizes, invalid combinations, success results, exception results, and exception-less attempt results.
- Public data-holder tests verify defaults, caller mutation boundaries, coordinate conventions, raw-byte preservation, and round trips through every consuming interface.
- Unit tests directly cover internals through the existing friend assembly, including every table, checksum, finite-field operation, compaction transition, placement corner case, parser branch, transform, and renderer chunk.
- Conformance vectors come from the standards samples, GS1 Syntax Dictionary linter corpus, GS1 examples, independently generated Zint/BWIPP/ZXing fixtures, and hand-calculated boundary vectors. Fixtures record their oracle/version and are checked in so the test suite does not need network access.
- Encoder interoperability is checked by decoding outputs with at least two independent readers where those readers support the format. Decoder interoperability is checked against symbols produced by at least two independent encoders. Disagreements are resolved against the governing standard, never by majority vote.
- Image fixtures cover clean synthetic symbols and controlled transformations: scale, crop, quiet-zone infringement, rotation, skew, perspective, blur, noise, gradient illumination, inversion, reflection, low contrast, module damage, occlusion, multiple symbols, and distracting text/lines.
- Error-correction tests inject every correctable error/erasure boundary and at least one over-capacity case for every Data Matrix block layout and PDF417 ECC level.
- Rendering tests parse produced PNG/BMP/SVG independently, compare logical dark regions rather than unstable compressed bytes, and round-trip rendered output through the library decoder.
- Complete API-layer coverage means every public class, constructor, field-driven option, interface method, return type, exception type, enum member, static field, and documented outcome is executed. Branch and mutation coverage are used to find missing assertions, but no performance benchmarks or syntax-policy/source-checking tests are added.

## 8. Implementation backlog

### Foundation and API skeleton

- [DONE] T001: Create the `src/Api` and `src/Internal` directory structure and namespaces described in this plan.
- [DONE] T002: Implement all public enums under `src/Api/Enums` with stable explicit integer values and required API comments.
- [DONE] T003: Implement shared public geometry, payload, sizing, quiet-zone, text-layout, ECI, and structured-append data classes.
- [DONE] T004: Implement public encode option/request and symbol/result data classes, including all per-symbology option holders.
- [DONE] T005: Implement public image, decode, detection, candidate, metadata, attempt, and diagnostic data classes.
- [DONE] T006: Implement public render request/options/result data classes.
- [DONE] T007: Implement public checksum, validation, quality-metric, GS1, and structured-append request/result data classes.
- [DONE] T008: Implement all operation interfaces with unique, non-overloaded instance method names.
- [DONE] T009: Implement public static data classes for standards versions, symbology identifiers, and media types.
- [DONE] T010: Implement the seven direct-`System.Exception` public exception types.
- [DONE] T011: Implement public operation classes as interface-conforming shells with one constructor each and internal dispatcher dependencies.
- [DONE] T012: Implement checked dimension, capacity, index, and allocation arithmetic shared by all codecs.
- [DONE] T013: Implement packed bit rows and mutable module matrices with crop, rotate, reflect, border, and row/column operations.
- [DONE] T014: Implement run-length extraction, normalized pattern variance, forward/reverse iterators, and pattern matching.
- [DONE] T015: Implement shared encoding/decoding contexts, immutable result copying, cancellation-free resource limits, and format dispatch.
- [DONE] T016: Implement point, rectangle, quadrilateral, line, affine, projective, clipping, and grid-sampling primitives.

### Checksums and error correction

- [DONE] T017: Implement GS1/EAN/UPC modulo-10 calculation and verification for arbitrary permitted key lengths.
- [DONE] T018: Implement Code 128 modulo-103, Code 39 modulo-43, and Codabar modulo-16 calculation and verification.
- [DONE] T019: Implement GF(256) log/antilog tables and polynomial arithmetic for the Data Matrix field.
- [DONE] T020: Implement Data Matrix Reed-Solomon parity generation, syndrome calculation, erasure-assisted correction, and failure reporting.
- [DONE] T021: Implement modulo-929 field and polynomial arithmetic for PDF417.
- [DONE] T022: Implement PDF417 Reed-Solomon parity generation, syndrome solving, erasures, error locations/magnitudes, and failure reporting.
- [DONE] T023: Wire all checksum algorithms into `BarcodeChecksumCalculator`, including format-specific input validation and exception-less verification results.

### Code 128 and GS1-128

- [DONE] T024: Add and independently verify the complete Code 128 codeword/pattern/function table.
- [DONE] T025: Implement Code 128 code-set A/B/C byte mapping, SHIFT, switches, FNC1-FNC4, and upper-shift semantics.
- [DONE] T026: Implement optimal deterministic Code 128 code-set planning and forced-code-set validation.
- [DONE] T027: Implement Code 128 codeword assembly, modulo-103 checksum, stop/termination pattern, and capacity checks.
- [DONE] T028: Implement Code 128 logical layout, standard quiet zones, sizing/bar-height validation, and HRT primitives.
- [DONE] T029: Implement forward and reverse Code 128 scanline decoding with start/stop, variance, quiet-zone, and checksum validation.
- [DONE] T030: Implement GS1-128 leading FNC1, AI separator insertion, symbol limits, identifier metadata, and GS1 HRT integration.

### EAN/UPC

- [DONE] T031: Add and independently verify EAN/UPC L/G/R digit, parity, guard, and add-on pattern tables.
- [DONE] T032: Implement EAN-13 and EAN-8 validation, check-digit completion, parity selection, and module encoding.
- [DONE] T033: Implement UPC-A encoding while preserving UPC-A identity separately from its EAN-13-compatible pattern.
- [DONE] T034: Implement UPC-E number-system 0/1 expansion, compression, parity, validation, and module encoding.
- [DONE] T035: Implement EAN/UPC two-digit and five-digit add-on validation, checksums, parity, and layout association.
- [DONE] T036: Implement EAN/UPC quiet zones, module counts, guards, guard descent, physical sizing, add-on placement, and HRT layout.
- [DONE] T037: Implement bidirectional EAN-13/EAN-8/UPC-A/UPC-E scanline decoding with parity and exact-format metadata.
- [DONE] T038: Implement add-on scanline decoding, allowed/required-length policy, and association with the main symbol.

### Code 39, ITF, and Codabar

- [DONE] T039: Add and independently verify Code 39 pattern/value tables and standard alphabet validation.
- [DONE] T040: Implement Code 39 standard and Full ASCII encoding/decoding, including malformed-shift rejection.
- [DONE] T041: Implement Code 39 modulo-43 modes, append metadata, ratio/gap/quiet-zone layout, and HRT.
- [DONE] T042: Implement bidirectional Code 39 scanline decoding with adaptive width classification and identifier modifiers.
- [DONE] T043: Add and independently verify Interleaved 2 of 5 digit/start/stop pattern tables.
- [DONE] T044: Implement ITF digit-pair encoding, even-length/check policies, ratio, quiet zones, layout, and HRT.
- [DONE] T045: Implement the ITF-14 GS1 profile with exact length/check digit, bearer bars, sizing, identifier, and partial-read defenses.
- [DONE] T046: Implement bidirectional ITF/ITF-14 scanline decoding with adaptive widths, allowed lengths, checks, and bearer-bar tolerance.
- [DONE] T047: Add and independently verify Rationalized Codabar data and start/stop pattern tables and aliases.
- [DONE] T048: Implement Codabar encoding, start/stop preservation, modulo-16 modes, ratio/gap/quiet-zone layout, and HRT.
- [DONE] T049: Implement bidirectional Codabar scanline decoding with valid terminal pairs, minimum length, check, and identifier metadata.

### Data Matrix ECC 200

- [DONE] T050: Implement the complete ISO/IEC 16022 square and standard-rectangular symbol-info/capacity/block table.
- [DONE] T051: Implement Data Matrix ASCII encodation, digit pairs, upper shift, latches, padding, and randomization.
- [DONE] T052: Implement C40, Text, ANSI X12, EDIFACT, and Base 256 encodation and de-encodation.
- [DONE] T053: Implement deterministic minimal Data Matrix mode planning plus forced-mode validation.
- [DONE] T054: Implement Data Matrix ECI, FNC1, Macro 05/06, reader programming, and structured-append control codewords.
- [DONE] T055: Implement Utah/corner data placement and extraction with fixed-module validation for every supported size.
- [DONE] T056: Implement Data Matrix data/ECC block splitting and interleaving for every symbol size.
- [DONE] T057: Implement Data Matrix high-level decode, unlatching, de-randomization, ECI byte-to-text interpretation, and raw-byte preservation.
- [DONE] T058: Implement Data Matrix encoder size/shape selection, finder/timing regions, quiet zone, layout, and metadata.
- [DONE] T059: Implement pure-module Data Matrix decoding across right-angle rotations, reflections, polarity, dimension inference, and ECC correction.
- [DONE] T060: Implement Data Matrix image finder evidence, L-border/clock-track localization, and candidate quadrilateral refinement.
- [DONE] T061: Implement Data Matrix projective grid sampling, module-centre voting, local grid refinement, and inverted/mirrored attempts.
- [DONE] T062: Implement managed DPM-oriented preprocessing for low contrast, dot-peen/separated modules, glare gradients, and textured backgrounds.
- [DONE] T063: Implement GS1 DataMatrix first-position FNC1, identifier, AI payload, HRI, sizing, and application-profile integration.

### PDF417

- [DONE] T064: Add and independently verify the complete PDF417 929-codeword patterns for all three clusters and start/stop patterns.
- [DONE] T065: Implement PDF417 text compaction submodes and exact inverse decoding.
- [DONE] T066: Implement PDF417 byte compaction 901/924, 913 shift, remainder handling, and exact inverse decoding.
- [DONE] T067: Implement PDF417 numeric base-10/base-900 compaction with large inputs and exact inverse decoding.
- [DONE] T068: Implement deterministic optimal compaction planning, forced compaction, and ECI designators.
- [DONE] T069: Implement PDF417 dimension/aspect selection, length descriptor, padding, ECC-level selection, and capacity checks.
- [DONE] T070: Implement row clusters, row indicators, standard/Compact row layout, quiet zones, and HRT metadata.
- [DONE] T071: Implement Macro PDF417 control-block encoding/decoding and every defined optional field.
- [DONE] T072: Implement PDF417 encoder assembly and modulo-929 ECC generation for levels 0 through 8.
- [DONE] T073: Implement scanline start/stop detection, 17-module codeword sampling, bucket/cluster validation, and codeword value recovery.
- [DONE] T074: Implement row-indicator metadata reconciliation for row number/count, column count, and ECC level under skew/missing rows.
- [DONE] T075: Implement PDF417 ambiguity resolution, erasure collection, ECC correction, compaction decoding, and metadata reporting.
- [DONE] T076: Implement PDF417 image localization using start/stop line evidence, arbitrary angle estimation, and quadrilateral refinement.
- [DONE] T077: Implement perspective row sampling, inverted/mirrored attempts, multiple-symbol support, and Compact PDF417 truncated-side recovery.
- [DONE] T078: Implement out-of-order Macro PDF417 assembly with missing, duplicate, conflict, checksum, and completion reporting.

### GS1 syntax and semantics

- [DONE] T079: Define the reproducible transformation from the checked-in GS1 Syntax Dictionary snapshot to policy-compatible static C# AI data.
- [DONE] T080: Generate and verify the complete AI prefix trie/table, wildcard indicators, component definitions, flags, titles, and associations.
- [DONE] T081: Implement fixed/variable AI recognition, component slicing, character-set checks, FNC1/GS termination, and source offsets.
- [DONE] T082: Port every GS1 Syntax Dictionary component linter required by the current AI table to managed C#.
- [DONE] T083: Implement GS1 check-digit, date/time, country/currency, coordinate, IBAN, media/package, coupon, and specialized component validation.
- [DONE] T084: Implement required, excluded, repeated, primary-key, qualifier, and Digital Link association validation across a message.
- [DONE] T085: Implement bracketed AI syntax parsing/building with literal-parenthesis and malformed-AI diagnostics.
- [DONE] T086: Implement unbracketed element-string and barcode-message parsing/building with exact FNC1/GS semantics.
- [DONE] T087: Implement scanner-data parsing/building and ISO/IEC 15424 identifier/modifier interpretation for every supported GS1 carrier.
- [DONE] T088: Implement GS1 HRI generation, titles, element ordering, and parenthesis/display rules.
- [DONE] T089: Implement canonical uncompressed GS1 Digital Link URI parsing/building, percent encoding, path qualifiers, query attributes, and round trips.
- [DONE] T090: Implement AI lookup and the complete `Gs1DataProcessor` conversion/validation surface with unknown-AI policy.

### Rendering

- [DONE] T091: Implement device-independent render planning that applies quiet zones once and resolves modules, pixels, millimetres, DPI, rotation, and HRT space.
- [DONE] T092: Implement rectangle/run coalescing for linear and matrix symbols without altering logical module edges.
- [DONE] T093: Implement Raw1 and Gray8 raster canvases, foreground/background/alpha handling, and nearest-neighbour integral scaling.
- [DONE] T094: Implement a dependency-free PNG writer with valid chunks, filtering, compression, CRCs, color types, and deterministic decoding tests.
- [DONE] T095: Implement a dependency-free BMP writer with correct row order, padding, headers, palette/color, and alpha behavior.
- [DONE] T096: Implement a safe SVG writer with integer view box, crisp edges, coalesced paths, escaping, colors, rotation, and no external resources.
- [DONE] T097: Implement deterministic built-in glyph data and raster/vector HRT drawing independent of installed fonts.
- [DONE] T098: Implement symbology-specific HRT placement for EAN/UPC/add-ons, GS1 AIs, guard descent, checks, and start/stop display.
- [DONE] T099: Complete `BarcodeRenderer` format dispatch, actual-size metadata, invalid-scale refusal, and render-plan diagnostics.

### Image processing, localization, and orchestration

- [DONE] T100: Implement validated raw image ingestion and deterministic Gray8 conversion for every public pixel format and stride orientation.
- [DONE] T101: Implement histograms, contrast/polarity analysis, global thresholding, Otsu, per-row, adaptive tile, and hybrid binarization.
- [DONE] T102: Implement illumination normalization, box smoothing, unsharp enhancement, and bounded small-kernel morphology.
- [DONE] T103: Implement image pyramids, ROI cropping, coordinate transforms, scale selection, and source-coordinate restoration.
- [DONE] T104: Implement direct horizontal/vertical scan, scanline fan-out, and multi-row voting for clean linear symbols.
- [DONE] T105: Implement linear-bar transition maps, orientation histograms, connected blocks, line grouping, and candidate boxes.
- [DONE] T106: Implement arbitrary-angle linear scanline sampling, deskew, reversed traversal, and cross-line result reconciliation.
- [DONE] T107: Integrate specialized Data Matrix and PDF417 localizers into the common candidate pipeline.
- [DONE] T108: Implement candidate scoring from format evidence, geometry, quiet zones, contrast, checksum/ECC, and repeated decode agreement.
- [DONE] T109: Implement effort levels and ordered preprocessing/localization fallbacks with expected-count and maximum-result stopping.
- [DONE] T110: Implement duplicate suppression, stable reading order, payload/geometry conflict handling, and multi-symbol result aggregation.
- [DONE] T111: Implement bounded diagnostic collection for thresholds, transforms, candidate rejection, correction, confidence, and coordinates.
- [DONE] T112: Complete `BarcodeDetector` localization-only behavior and `BarcodeDecoder` pure-symbol/image behavior for all formats.

### Validation and public integration

- [DONE] T113: Implement shared issue construction with stable codes, severity, standard/profile, field/AI, source offset, and expected/observed data.
- [DONE] T114: Implement pre-encode validation for every format, option combination, payload character set, capacity, checksum, and structured-append field.
- [DONE] T115: Implement logical symbol validation for all patterns, dimensions, checks, ECC, quiet zones, finders/guards/indicators, and bearer bars.
- [DONE] T116: Implement render-plan validation for physical size, module rounding, ratio, height, HRT clearance, contrast, colors, and output constraints.
- [DONE] T117: Implement image diagnostics for bounds, quiet zones, module consistency, skew/perspective, contrast/modulation proxy, fixed-pattern damage, and ECC margin.
- [DONE] T118: Implement strict versus tolerant validation behavior and prevent recovery heuristics from producing a conformant status.
- [DONE] T119: Wire all public implementation classes to internal services, enforce defensive result copies, and normalize domain exceptions.
- [DONE] T120: Implement structured-append assembly dispatch for Data Matrix and Macro PDF417 with format-specific consistency rules.

### Complete source/API test coverage

- [DONE] T121: Create shared test builders, deterministic random/error injectors, module/image transforms, and fixture provenance metadata.
- [DONE] T122: Add exhaustive tests for checked arithmetic, bit rows/matrices, runs, pattern variance, geometry, transforms, and grid sampling.
- [DONE] T123: Add exhaustive tests for all checksum calculators, finite fields, polynomial operations, and public checksum API outcomes.
- [DONE] T124: Add Code 128 table, planner, function, Latin-1, checksum, layout, pure-decode, malformed, and public API tests.
- [DONE] T125: Add GS1-128 FNC1/separator, AI ordering, capacity, identifier, HRT, round-trip, malformed, and public API tests.
- [DONE] T126: Add EAN-8/EAN-13/UPC-A/UPC-E/add-on parity, compression, check, layout, pure-decode, malformed, and API tests.
- [DONE] T127: Add Code 39 standard/Full ASCII/check/append/ratio, pure-decode, malformed, and public API tests.
- [DONE] T128: Add ITF/ITF-14 pair/check/length/bearer/quiet-zone, partial-read, pure-decode, malformed, and API tests.
- [DONE] T129: Add Codabar alphabet/aliases/start-stop/check/ratio, pure-decode, malformed, and public API tests.
- [DONE] T130: Add Data Matrix tests for every encodation mode, control feature, padding/randomization case, and mode-transition boundary.
- [DONE] T131: Add Data Matrix placement/interleaving/ECC tests for every square and standard-rectangular size and all correction boundaries.
- [DONE] T132: Add Data Matrix pure/image decode tests for rotations, reflection, inversion, perspective, DPM conditions, damage, multiple symbols, and API outcomes.
- [DONE] T133: Add GS1 DataMatrix FNC1/AI/HRI/identifier/profile, round-trip, malformed, and public API tests.
- [DONE] T134: Add PDF417 text/byte/numeric/ECI compaction tests for all submode transitions, remainder cases, and capacity boundaries.
- [DONE] T135: Add PDF417 dimension/row-indicator/cluster/Compact/ECC tests for all levels and correction/erasure boundaries.
- [DONE] T136: Add PDF417 pure/image decode tests for skew, perspective, missing rows, inversion, reflection, ambiguity, damage, multiple symbols, and API outcomes.
- [DONE] T137: Add Macro PDF417 optional-field, out-of-order assembly, missing/duplicate/conflict/checksum, and public assembler API tests.
- [DONE] T138: Run the full GS1 Syntax Dictionary linter corpus against the managed implementation and add AI-prefix/association boundary tests.
- [DONE] T139: Add GS1 bracketed/element/barcode/scanner/HRI/Digital-Link conversion round trips, invalid syntax, unknown-AI policy, and API tests.
- [DONE] T140: Add raster/SVG layout tests for every format, sizing mode, quiet-zone policy, rotation, color/alpha mode, and HRT option.
- [DONE] T141: Add independent PNG/BMP/SVG parser checks and render-to-decode round trips for every supported symbology.
- [DONE] T142: Add image-ingestion and preprocessing tests for every pixel format, stride, ROI, threshold mode, filter, polarity, and invalid buffer.
- [DONE] T143: Add localization tests for format filters, rotations, arbitrary angles, perspective, multiple/overlapping symbols, ROI, deduplication, ordering, and diagnostics.
- [DONE] T144: Add validation tests for every issue code, severity, strict/tolerant distinction, data/symbol/render/image target, and quality metric.
- [DONE] T145: Add reflection-based API inventory tests that execute every public constructor, field-driven enum option, interface method, static field, exception, and result type.
- [DONE] T146: Add cross-encoder fixtures from Zint and BWIPP and prove this decoder accepts every in-scope valid variant and rejects deliberately invalid variants.
- [DONE] T147: Add encoder interoperability tests using ZXing/ZXing.Net, ZBar where supported, Zint, and BWIPP as independent readers/oracles.
- [DONE] T148: Add decoder interoperability corpora generated by at least two independent encoders per supported format, preserving raw bytes and exact observed format.
- [DONE] T149: Add end-to-end mixed-page tests containing all supported linear/matrix/stacked formats under controlled image degradation.
- [DONE] T150: Enforce complete public API line/branch coverage and review surviving mutation-test cases for missing behavioral assertions.

## 9. Downloaded implementation references in `specs/`

The paid ISO standards were not bypassed. `specs/` contains publicly distributed samples/previews and the complete freely published GS1 materials. A licensed copy of each paid normative text is still required before claiming formal conformance.

| Local resource | Source/use |
| --- | --- |
| `ISO-IEC-15417-2007-Code-128-Sample.pdf` | Public Code 128 sample; current standard also has Amendment 1:2026 |
| `ISO-IEC-15420-2025-EAN-UPC-Sample.pdf` | Public current EAN/UPC sample |
| `ISO-IEC-16388-2023-Code-39-Sample.pdf` | Public current Code 39 sample |
| `ISO-IEC-16022-2024-Data-Matrix-Sample.pdf` | Public current Data Matrix sample |
| `ISO-IEC-15438-2015-PDF417-Sample.pdf` | Public current PDF417 sample |
| `ISO-IEC-16390-2007-ITF-Sample.pdf` | Public current ITF sample |
| `ISO-IEC-15424-2025-Symbology-Identifiers-Preview.pdf` | Public current symbology-identifier preview |
| `Codabar-AIM-BC3-Description.html` | Public Rationalized Codabar structure/check/quiet-zone description; a licensed AIM BC3 text is still required for a conformance claim |
| `GS1-General-Specifications-26.0.0.pdf` | Complete current GS1 General Specifications |
| `GS1-DataMatrix-Guideline.pdf` | Complete GS1 DataMatrix implementation guidance |
| `GS1-HRI-Guideline-1.3.0.pdf` | Current GS1 HRI guidance |
| `GS1-2D-Barcode-Colour-and-Quality-Guide.pdf` | GS1 image/color/quality guidance |
| `GS1-Barcode-Syntax-Resource-User-Guide.pdf` | GS1 AI syntax processing guidance |
| `GS1-Syntax-Dictionary.txt` | Current machine-readable AI definitions |
| `GS1-Syntax-Dictionary-and-Tests.zip` | Current AI dictionary, reference linters, test corpus, and license |
| `GS1-Digital-Link-URI-Syntax-1.7.0.pdf` | Current canonical URI rules |
| `RFC-3986-URI-Syntax.txt`, `RFC-3987-IRI.txt`, `RFC-5234-ABNF.txt` | URI/IRI and grammar references used by GS1 Digital Link |
| `ZXing-README.md`, `ZBar-README.md`, `Zint-Manual.txt`, `ZXing.Net-README.md`, `bwip-js-README.md` | Comparator feature and behavior references |
| `Barcode4J-Overview.pdf`, `Dynamsoft-Barcode-Reader-Datasheet.pdf` | Comparator generator/reader capability references |

Authoritative online landing pages used to establish current versions include [ISO/IEC 15417](https://www.iso.org/standard/43896.html), [ISO/IEC 15420](https://www.iso.org/standard/84892.html), [ISO/IEC 16388](https://www.iso.org/standard/77799.html), [ISO/IEC 16022](https://www.iso.org/standard/80926.html), [ISO/IEC 15438](https://www.iso.org/standard/65502.html), [ISO/IEC 16390](https://www.iso.org/standard/43898.html), [ISO/IEC 15424](https://www.iso.org/standard/82433.html), [GS1 General Specifications](https://ref.gs1.org/standards/genspecs/), and the [GS1 Barcode Syntax Resource](https://ref.gs1.org/tools/gs1-barcode-syntax-resource/).
