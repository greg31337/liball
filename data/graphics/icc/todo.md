# Universal ICC color-profile library development plan

## Objective and scope

Build a dependency-free (under `src/`) .NET library that can safely parse, preserve, inspect, validate, construct, transform, and deterministically serialize ICC profiles from the low-level binary representation through high-level colour descriptions. The compatibility target is ICC.1 v2.4, ICC.1 v4.0 through v4.4 / ISO 15076-1:2025, the approved 2025 ICC.1 amendments, and ICC.2:2023 (iccMAX/v5) plus published amendments and errata. ICC.1 is the default interoperability target; ICC.2 functionality is an advanced layer that remains backward-compatible with v2/v4 inputs.

The production assembly will use only built-in .NET APIs, including `System.Buffers.Binary`, `Span<T>`/`ReadOnlySpan<T>`, `Memory<T>`/`ReadOnlyMemory<T>`, `Stream`, `System.IO.Compression`, and `System.Security.Cryptography`. External packages and native reference engines may be used only by tests. Every implementation must fit the restricted C# subset in `syntax.md`: field-based public data classes, non-overloaded interfaces, one-constructor public implementation classes, default-int enums, static data classes containing fields only, no properties/records/structs/generic declarations/LINQ/async/try-catch in `src/`, and all public types in the mandated `src/Api` category directory.

Full measurement-device control, chart generation, regression fitting from raw characterization measurements, display calibration loading, operating-system profile installation, and complete PDF/OpenXPS/SVG/CSS document parsers are outside this library. The construction layer creates standards-compliant profiles from already supplied colourimetric definitions, curves, matrices, LUTs, named colours, and transform pipelines. The embedding layer handles directly addressable raster/container encodings and exposes raw-profile integration points for larger document parsers.

## Research basis

| Source | Capabilities that shape this plan |
| --- | --- |
| ICC.1:2001-04, ICC.1:2022, ISO 15076-1, and current ICC registries | v2/v4 headers, profile classes, tag tables, required/optional tags, tag types, PCS encodings, rendering intents, profile IDs, computational models, and registered signatures. |
| ICC.2:2023 / ISO 20677 | v5 headers, extended/spectral PCS, PCC, MCS, colour-encoding profiles, tag structures and arrays, programmable and spectral MPEs, BRDF/directional profiles, HDR, and abbreviated profiles. |
| ICC profile assessment and security guidance | Strict structure validation, bounded allocation, no gaps or hidden trailing data, registered/private-tag reporting, calculator limits, required-tag/type checks, round-trip and smoothness quality checks. |
| Little CMS | Cooked and raw tag access, v2/v4 mixing, profile creation, pipelines, named colours, multiprofile/device-link transforms, proofing, gamut checking, black-point compensation, profile ID, and robust extension handling. |
| ArgyllCMS/icclib | Strict and warning parse modes, detailed dumps, unknown/duplicate-tag robustness, forward and inverse lookup, transform tracing, device links, embedded-profile discovery, calibration/private-tag awareness, spectral colour work, and quality verification. |
| SampleICC and current iccDEV/IccProfLib | Broad ICC.1 and ICC.2 object coverage, read/write/apply/validate flows, all major iccMAX tag/array/structure/MPE forms, calculator execution, spectral and PCC workflows, and conformance test profiles. |
| Apple ColorSync | Immutable/mutable profile views, header/tag authoring, profile verification, embedding, localized descriptions, and precomputed profile-sequence transforms. |
| skcms | Small bounded parser, matrix/TRC and A2B/B2A prioritization, canonical colour spaces, approximate profile equality, safe transform execution, and rejection of malformed profiles. |

The implementation will combine the breadth of iccDEV and Little CMS with skcms-style bounds discipline, Argyll-style diagnostics, and a ColorSync-style separation between profile data and operations. It will preserve unsupported registered or private payloads byte-for-byte instead of treating support for every vendor extension as a prerequisite for safely reading a profile.

## Downloaded standards and references

All downloaded material is research input and must not become a production dependency.

| Local resource | Purpose |
| --- | --- |
| `specs/icc/ICC.1-2001-04.pdf` | Final ICC v2.4 profile specification. |
| `specs/icc/ICC.1-2022-05.pdf` | ICC v4.4 specification corresponding to ISO 15076-1:2025. |
| `specs/icc/ICC.1-Adaptive-Gain-Curve-2025.pdf` | Approved adaptive gain curve tag/type amendment. |
| `specs/icc/ICC.1-CICP-Unspecified-Primaries-Amendment.pdf` | Current CICP amendment. |
| `specs/icc/ICC.2-2023.pdf` | Current iccMAX/v5 specification. |
| `specs/icc/ICC.2-Extended-Device-Colour-Space-Amendment.pdf` | Current ICC.2 extended device colour-space amendment. |
| `specs/icc/ICC.2-2019-Cumulative-Errata.pdf` | Published cumulative ICC.2 corrections that remain relevant to the revised text. |
| `specs/icc/ISO-18619-Black-Point-Compensation.pdf` | ICC black-point compensation algorithm. |
| `specs/icc/ISO-15076-1-2025.html` and `ISO-20677-2019.html` | Official ISO publication metadata for the standards corresponding to ICC.1 and ICC.2; the freely available ICC texts above provide the implementable technical content. |
| `specs/icc/registry-*.html` | Snapshots of public/private tag, type, iccMAX, and dict metadata registries. |
| `specs/icc/profile-assessment.html` and `profile-security.html` | ICC validation, quality, and security criteria. |
| `specs/embedding/ICC-Technote-Profile-Embedding.pdf` | ICC embedding requirements and carrier-format matrix. |
| `specs/embedding/Embedding-ICC.2-in-ICC.1.pdf` | Embedded v5 profile (`ICC5`) rules. |
| `specs/embedding/PNG-Third-Edition.html` | PNG `iCCP` container rules. |
| `specs/rfcs/RFC-1321-MD5.txt` | Profile ID digest algorithm reference. |
| `specs/rfcs/RFC-1950-ZLIB.txt` and `RFC-1951-DEFLATE.txt` | PNG compressed profile representation. |
| `specs/rfcs/RFC-9649-WebP.txt` | WebP `ICCP` chunk representation. |
| `specs/color-science/CIEDE2000-Sharma-Wu-Dalal.pdf` | CIEDE2000 algorithm and reference values. |
| `specs/reference-libraries/littlecms/` | Little CMS tutorial, API reference, and public header. |
| `specs/reference-libraries/argyllcms/` | ArgyllCMS overview, dump, lookup, and device-link references. |
| `specs/reference-libraries/iccdev/` and `sampleicc/` | Current iccDEV headers/API notes and SampleICC design notes. |
| `specs/reference-libraries/colorsync/` | Apple ColorSync profile/transform reference snapshot. |
| `specs/reference-libraries/skcms/` | skcms parser, transform source, and public declarations. |

## Complete feature inventory

### Binary foundation and profile envelope

The binary layer covers checked big-endian signed/unsigned 8/16/32/64-bit integers, IEEE binary16/binary32/binary64, s15Fixed16, u16Fixed16, u1Fixed15, u8Fixed8, XYZNumber, dateTimeNumber, positionNumber, response16Number, spectral ranges, FourCC conversion, checked offset/count arithmetic, four-byte alignment, and exact span/stream reads and writes. Parsing supports memory and seekable/non-seekable streams, strict and recovery modes, caller-configurable size/count/depth/compression/calculator limits, byte-offset diagnostics, immutable input ownership, and no unbounded allocation.

The profile envelope covers the complete 128-byte v2/v4 header and v5 reinterpretation: declared size, preferred CMM, packed version/sub-version, class/sub-class, data colour space, PCS, creation time, `acsp`, platform, flags, manufacturer, model, 64-bit device attributes, rendering intent, PCS illuminant, creator, v4/v5 profile ID, v5 spectral PCS/ranges, bi-spectral range, MCS, and reserved bytes. It covers tag count/table entries, unique signatures, shared payload offsets, alignment/padding, contiguous canonical layouts, unknown data, exact end-of-file, and profiles embedded in other data.

### Versions, classes, spaces, and intent semantics

Supported profile generations are v2.x (with v2 Lab/XYZ encoding and legacy text/tag rules), v4.0-v4.4 (including negative PCSXYZ clarification, CICP, metadata/dict, MPE, and 2025 amendments), and v5.0 iccMAX. Supported ICC.1 classes are Input, Display, Output, DeviceLink, ColorSpace, Abstract, and NamedColor. ICC.2 adds ColorEncodingSpace, MultiplexIdentification, MultiplexLink, and MultiplexVisualization plus subclass metadata. Data spaces include XYZ, Lab, Luv, YCbCr, Yxy, RGB, Gray, HSV, HLS, CMYK, CMY, named colour, one through fifteen colour channels, legacy MCH aliases, extended device spaces, spectral PCS, bi-spectral PCS, and multiplex spaces with authoritative channel-count resolution.

Rendering support covers perceptual, media-relative colorimetric, saturation, and ICC-absolute colorimetric intents; A2B/B2A versus D2B/B2D precedence and fallback; profile-class direction rules; v2/v4 PCS encoding differences; media-white normalization; ICC-absolute/media-relative conversion; perceptual and saturation reference-gamut signatures; HDR/SDR range flags and HdrToSdr intent tags; black-point compensation; gamut and proofing transforms; and intent-preserving profile sequences/device links.

### ICC.1 tags and tag types

All ICC v4 public tags are modeled: AToB0/1/2, BToA0/1/2, BToD0/1/2/3, DToB0/1/2/3, blue/green/red matrix columns and TRCs, grayTRC, calibrationDateTime, charTarget, chromaticAdaptation, chromaticity, CICP, colorantOrder, colorantTable, colorantTableOut, colorimetricIntentImageState, copyright, device manufacturer/model descriptions, gamut, luminance, measurement, metadata, mediaWhitePoint, namedColor2, outputResponse, perceptual/saturation rendering-intent gamut, preview0/1/2, profileDescription, profileSequenceDesc, profileSequenceIdentifier, technology, viewing-condition description, and viewingConditions. The adaptive gain curve tag/type amendment is also modeled and executable.

Legacy v2 coverage adds mediaBlackPoint, namedColor, CRD info, PostScript CRD0/1/2/3, PostScript CSA, PostScript rendering intent, screening description/data, UCR/BG, device settings, legacy profile-description/localized-description variants, textDescriptionType, crdInfoType, namedColorType, screeningType, and ucrBgType. Common registered vendor payloads such as `vcgt` and `MHC2` receive recognizable inspection records when feasible; every other registered/private tag remains available as lossless raw data.

All ICC.1 v4 tag types are covered: chromaticity, CICP, colorant order/table, curve, data, date/time, dict, LUT8, LUT16, lutAToB, lutBToA, measurement, multi-localized Unicode, multi-process elements, namedColor2, parametric curve functions 0-4 with corrected equations, profile-sequence description/identifier, response-curve-set16, signed/unsigned fixed arrays, signature, text, unsigned integer arrays, viewing conditions, and XYZ arrays. LUT processing includes input curves, 3x3 matrix and offsets, N-dimensional CLUT, output curves, correct element ordering, 8/16-bit quantization, PCS range mapping, and absent-stage identity behavior.

The v4 multiProcessElementsType supports curve-set (`cvst`), matrix (`matf`), CLUT (`clut`), begin/end ACS placeholders, arbitrary channel counts, float32 ranges, shared/nested offsets, segmented curves, formula curve segments, sampled curve segments, and safe sequential evaluation. Unknown future processing elements are preserved and cause a precise unsupported-transform result rather than corrupt parsing.

### ICC.2 / iccMAX tags, types, structures, arrays, and processing elements

ICC.2 tag coverage includes AToB0-3, AToM0, BToA0-3, BToD0-3, DToB0-3, MToA0, MToB0-3, MToS0-3; all BRDF colorimetric/spectral parameter 0-3, AToB, BToA, BToD, DToB, MToB, and MToS families; all directional AToB, BToA, BToD, and DToB intent families; calibration time, characterization target, CICP, colour-encoding parameters/name, input/output colorant order/info, image state, copyright, custom/standard PCC transforms, CxF, manufacturer/model descriptions, gamut-boundary descriptions 0-3, HdrToSdr0-3, multiplex defaults/types, measurement input/info, media white, metadata, named colours, perceptual/saturation gamut, profile sequence information, reference name, source PCC, spectral viewing conditions/white point, surface maps, and technology.

ICC.2 tag-type coverage includes CICP, colorant order, curve, data, date/time, dict, embedded height/normal images, float16/32/64 arrays, gamut-boundary description, lutAToB/lutBToA, measurement, multi-localized Unicode, multi-process elements, parametric curves 0-8, signed/unsigned fixed arrays, signature, sparse-matrix arrays, spectral viewing conditions, tag arrays, tag structures, text, unsigned integer arrays, UTF-8, UTF-16, zipped UTF-8, XYZ, and zipped XML, with lossless fallback for future types.

ICC.2 processing-element coverage includes calculator, curve set, CLUT, extended CLUT, matrix, sparse matrix, tint array, tone map, XYZ-to-Jab, Jab-to-XYZ, emission CLUT/matrix/observer, inverse emission matrix, reflectance CLUT/observer, and future elements. Calculator execution covers all ICC.2 operation signatures and operand forms (input/output, constants, stack/vector manipulation, arithmetic, powers/logarithms/trigonometry, comparisons, Boolean/conditional/select, temporary/persistent variables, vector/matrix operations, colour-space conversions, environment/PCC access, and sub-element invocation) under deterministic instruction, stack, recursion, storage, and floating-point limits.

ICC.2 hierarchical data covers generic and typed tagStruct/tagArray trees, BRDF transform, colorant info, colour-encoding parameters, measurement info, named colour with tints, PCC, profile info, tint-zero structures, named-colour arrays, profile-info arrays, and nested structure arrays. Spectral support covers custom and standard illuminants/observers, reflectance/transmittance/radiant and fluorescence data, spectral and bi-spectral ranges, late-bound spectral-to-colorimetric conversion, media-white/PCC adjustment, sparse matrices, MCS channel naming/defaults/reordering, gamut boundary meshes, surface height/normal maps, BRDF parameter/function evaluation, directional emission, and abbreviated colour-encoding profiles.

### Colour descriptions and computation

The high-level colour layer provides normalized and encoded XYZ, Lab, xyY, and LCh descriptions; device-channel vectors with colour-space metadata; D50 standard values; v2/v4 PCS encode/decode; XYZ↔Lab, XYZ↔xyY, Lab↔LCh; matrix multiplication/inversion; Bradford chromatic adaptation and explicit `chad` application/inversion; media-relative↔ICC-absolute conversion; curve evaluation/inversion/sampling; CIE76, CIE94 graphic-arts/textile, and CIEDE2000 differences; localized profile/tag descriptions; named-colour lookup by exact name, normalized name, PCS value, and nearest ΔE; colorant/channel descriptions; and human-readable summaries of profiles, tags, transforms, rendering intents, viewing conditions, and CICP metadata.

### Transform compilation and execution

Transform compilation selects the standard-precedence transform for profile class, direction, intent, and requested PCS; validates channel continuity; lowers matrix/TRC, LUT8/16, lutAToB/lutBToA, v4 MPE, and v5 MPE data into an immutable execution plan; and reports unsupported elements before applying data. Execution supports a single colour and interleaved float buffers using spans, explicit source/destination channel counts and strides, domain/range handling, 1-D linear interpolation, N-D multilinear interpolation, 3-D tetrahedral interpolation, matrix offsets, optional clipping, PCS conversion, curve inversion where mathematically valid, gamut lookup/alarm results, soft proofing, black-point compensation, multiprofile chains, abstract profiles, named-colour profiles, and DeviceLink generation/application. Transform traces expose each selected tag/stage and intermediate vector for inspection and tests.

### Validation, security, inspection, comparison, and quality

Validation modes are tolerant, conforming, and security-strict. Reports contain stable rule IDs, severity, message, profile/tag/element path, byte offset/length, and remediation hint. Checks cover all header fields and version differences; registered/unknown signatures; valid dates/enums/flags/attributes; D50 and PCS rules; profile ID; tag table bounds, integer overflow, alignment, ordering, duplicate signatures, legal shared payloads, overlaps, gaps, padding, exact EOF, and trailing content; reserved zeros; tag signature/type compatibility; nested offset ownership; counts/sizes/ranges/encodings; text termination and Unicode validity; colour-space channel counts; curve/CLUT/matrix/MPE consistency; matrix invertibility; calculator safety; compression expansion limits; v5 arrays/structures/spectral/PCC/MCS/BRDF relationships; and embedded-profile restrictions.

Semantic conformance checks enforce version-specific required/optional/forbidden tags for every profile class and model, transform direction and intent relationships, media white/chromatic adaptation requirements, DeviceLink input/output spaces and sequence descriptions, NamedColor constraints, profile/subclass consistency, ICC.1/ICC.2 embedding equivalence, and ICC.2 core requirements. A declarative policy input allows additional ICC.2 Interoperability Conformance Specification subsets without hard-coding evolving external registries into the parser.

Optional quality analysis covers curve monotonicity/invertibility, curve and profile round trips, first/second round-trip ΔE statistics (mean, maximum, and 95th percentile), transform continuity/smoothness, characterization-data comparisons when supplied, approximate profile equivalence, profile-content fingerprints, gamut membership and boundary summaries, and deterministic inspection trees/text. Inspection always works on partially understood profiles and includes raw hex/ASCII signatures, decoded values, offsets/sizes, shared-data relationships, validation issues, transform precedence, and truncation controls for large arrays/CLUTs.

### Serialization, mutation, construction, and embedding

Serialization has canonical and preservation modes. Canonical output sorts tag entries by unsigned FourCC, chooses a stable payload order, optionally interns byte-identical payloads, writes zero padding and reserved fields, emits the shortest legal representation selected by options, applies explicit fixed-point/float rounding rules, recomputes offsets/size, and computes the v4/v5 MD5 Profile ID with the required fields zeroed. Preservation mode retains unknown/private bytes, original tag order, shared-payload identity, and unmodified encodings while safely rebuilding edited sections. Both modes can write to a byte array or stream and can preflight the exact output size.

Basic construction covers empty standards-valid profile skeletons; RGB matrix/TRC input/display/colour-space profiles from primaries, white point, and per-channel curves; gray TRC profiles; XYZ identity profiles; LUT/MPE abstract profiles; named-colour profiles; simple output profiles from supplied forward/backward pipelines; DeviceLink profiles from compiled profile chains; v5 colour-encoding/abbreviated profiles; localized metadata, CICP/HDR/adaptive-gain data, viewing/measurement information, and private raw tags. Builders reject incomplete definitions, synthesize required tags, adapt source white to D50, choose version-correct text/PCS encodings, validate before returning, and remain deterministic for identical inputs.

Embedding support distinguishes the profile header's embedded/use-independent flags, the ICC.1 `ICC5` embedded-v5 tag, and external media containers. Direct adapters scan/extract/replace/remove profiles in JPEG APP2 segment sequences, PNG `iCCP` chunks with bounded zlib, TIFF/BigTIFF and DNG tag 34675, WebP RIFF `ICCP`, JPEG 2000 JP2/JPX `colr` boxes, and ISO BMFF AVIF/HEIF `colr` (`prof`/`rICC`) boxes. A bounded raw scanner finds unsegmented `acsp` candidates and validates declared sizes. Writers preserve unrelated container bytes and ordering where the carrier permits it; PDF/OpenXPS/SVG/CSS/JPEG XL integration is represented by raw payload APIs and clear unsupported-carrier results rather than incomplete document rewrites.

## Proposed public API and source files

Every public data type below is a field-only data-holding class used by at least one contract or another data class. Every implementation class exposes one constructor and only members declared by its interface. Signatures are `uint` static fields rather than enums because many FourCC values exceed signed `int` range.

### Contracts (`src/Api/Contracts`)

| Planned file/type | Responsibility |
| --- | --- |
| `IIccProfileReader.cs` | Read profiles from spans/memory/streams with options and non-throwing probe/result operations. |
| `IIccProfileWriter.cs` | Preflight and write canonical or preservation-mode bytes/streams. |
| `IIccProfileValidator.cs` | Structural, semantic, security, policy, and optional quality validation. |
| `IIccProfileInspector.cs` | Summary, tree, deterministic text, raw-tag, and transform-trace inspection. |
| `IIccProfileBuilder.cs` | Create the supported basic v2/v4/v5 profile categories from definition data. |
| `IIccProfileIdCalculator.cs` | Compute, verify, and update the ICC Profile ID. |
| `IIccSignatureDescriber.cs` | Convert registered and unknown signatures to stable names/descriptions. |
| `IIccColorMath.cs` | Colour-space conversion, encoding, matrix, and ΔE operations with uniquely named methods. |
| `IIccChromaticAdapter.cs` | Build/apply/invert Bradford and explicit adaptation matrices. |
| `IIccToneCurveEvaluator.cs` | Evaluate, invert, sample, and analyze ICC curves. |
| `IIccTransformFactory.cs` | Select/compile profile, proofing, and multiprofile transform plans. |
| `IIccColorTransform.cs` | Transform one colour or a strided span buffer and produce optional traces. |
| `IIccProfileLinker.cs` | Compile a profile sequence into a DeviceLink profile. |
| `IIccGamutAnalyzer.cs` | Test membership and summarize profile/gamut-boundary data. |
| `IIccProfileComparer.cs` | Exact semantic and approximate transform comparisons/fingerprints. |
| `IIccEmbeddedProfileReader.cs` | Detect carrier, enumerate, and extract bounded embedded profiles. |
| `IIccEmbeddedProfileWriter.cs` | Insert, replace, or remove profiles in supported carriers. |

### Data (`src/Api/Data`)

| Planned files/types | Data represented |
| --- | --- |
| `IccProfile.cs`, `IccProfileHeader.cs`, `IccVersion.cs`, `IccDateTime.cs`, `IccProfileId.cs` | Profile root and complete generation-aware header. |
| `IccTagTableEntry.cs`, `IccTag.cs`, `IccTagData.cs`, `IccRawData.cs` | Directory, decoded/raw payload union, offsets, padding, and shared identity. |
| `IccReadOptions.cs`, `IccWriteOptions.cs`, `IccResourceLimits.cs` | Parse/write modes, preservation choices, and hard resource ceilings. |
| `IccXyzNumber.cs`, `IccXyzColor.cs`, `IccLabColor.cs`, `IccXyYColor.cs`, `IccLchColor.cs` | Encoded and high-level colour values. |
| `IccColorVector.cs`, `IccMatrix.cs`, `IccChromaticity.cs`, `IccEncodingRange.cs` | N-channel vectors, dense matrices, chromaticities, and ranges. |
| `IccViewingConditions.cs`, `IccColorDescription.cs`, `IccColorDifference.cs` | Viewing context and human-oriented colour results. |
| `IccCurve.cs`, `IccParametricCurve.cs`, `IccCurveSegment.cs`, `IccCurveAnalysis.cs` | Identity/gamma/sampled/parametric/segmented curve data and analysis. |
| `IccClut.cs`, `IccLut.cs`, `IccMultiStageLut.cs` | LUT8/LUT16/mAB/mBA grids, tables, matrices, curves, and encoding metadata. |
| `IccMultiProcessElements.cs`, `IccProcessingElement.cs`, `IccCalculatorProgram.cs` | v4/v5 pipeline elements, unknown elements, and calculator bytecode. |
| `IccLocalizedString.cs`, `IccMetadataEntry.cs`, `IccTextData.cs` | ASCII, Unicode, localized, dictionary, UTF, compressed text/XML data. |
| `IccNamedColor.cs`, `IccNamedColorSet.cs`, `IccColorant.cs`, `IccColorantSet.cs` | Named colours/tints and input/output colorants. |
| `IccMeasurement.cs`, `IccResponseCurve.cs`, `IccProfileSequenceEntry.cs`, `IccProfileSequenceIdentifier.cs` | Measurement, response, and profile sequence records. |
| `IccCicp.cs`, `IccAdaptiveGainCurve.cs`, `IccAdaptiveGainCurveNode.cs` | Video coding metadata and executable adaptive gain curves. |
| `IccSpectralRange.cs`, `IccSpectralData.cs`, `IccSpectralViewingConditions.cs`, `IccSparseMatrix.cs` | iccMAX spectral and sparse data. |
| `IccTagArray.cs`, `IccTagStructure.cs`, `IccStructureMember.cs` | Generic lossless v5 hierarchical containers. |
| `IccColorEncodingParameters.cs`, `IccProfileConnectionConditions.cs`, `IccMultiplexChannels.cs` | iccMAX abbreviated encoding, PCC, and MCS definitions. |
| `IccGamutBoundary.cs`, `IccGamutVertex.cs`, `IccGamutTriangle.cs`, `IccGamutResult.cs` | Boundary meshes and gamut results. |
| `IccBrdf.cs`, `IccSurfaceMap.cs`, `IccEmbeddedImage.cs` | BRDF, directional, height/normal-map, and embedded-image data. |
| `IccValidationPolicy.cs`, `IccValidationIssue.cs`, `IccValidationReport.cs` | Conformance policy and stable diagnostics. |
| `IccInspectionOptions.cs`, `IccInspectionNode.cs`, `IccProfileSummary.cs` | Bounded structured/text inspection. |
| `IccRgbProfileDefinition.cs`, `IccGrayProfileDefinition.cs`, `IccProfileBuildOptions.cs` | Basic profile construction inputs. |
| `IccTransformOptions.cs`, `IccTransformPlan.cs`, `IccTransformStage.cs`, `IccTransformTrace.cs` | Transform selection, compiled plan metadata, and tracing. |
| `IccProfileLinkOptions.cs`, `IccComparisonReport.cs`, `IccQualityStatistics.cs` | Device-link, comparison, and quality-analysis results. |
| `IccEmbeddingOptions.cs`, `IccEmbeddedProfile.cs`, `IccEmbeddedProfileResult.cs` | Carrier locations, extracted data, and mutation results. |
| `IccOperationResult.cs` | Exception-less expected-failure result used by probe/validation/embedding APIs. |

### Enums (`src/Api/Enums`)

Planned files are `IccVersionKind.cs`, `IccProfileClass.cs`, `IccColorSpaceKind.cs`, `IccPcsKind.cs`, `IccRenderingIntent.cs`, `IccTagDataKind.cs`, `IccCurveKind.cs`, `IccCurveSegmentKind.cs`, `IccProcessingElementKind.cs`, `IccInterpolationKind.cs`, `IccValidationSeverity.cs`, `IccValidationMode.cs`, `IccWriteMode.cs`, `IccTransformDirection.cs`, `IccIntentFallback.cs`, `IccPcsEncodingKind.cs`, `IccColorDifferenceFormula.cs`, `IccInspectionDetail.cs`, `IccGamutStatus.cs`, `IccEmbeddedFormat.cs`, `IccEmbeddedOperation.cs`, `IccMeasurementObserver.cs`, `IccMeasurementGeometry.cs`, `IccStandardIlluminant.cs`, and `IccOperationStatus.cs`.

### Static data (`src/Api/Static`)

Planned field-only classes are `IccConstants.cs`, `IccProfileClassSignatures.cs`, `IccColorSpaceSignatures.cs`, `IccTagSignatures.cs`, `IccTypeSignatures.cs`, `IccProcessingElementSignatures.cs`, `IccArraySignatures.cs`, `IccStructureSignatures.cs`, `IccTechnologySignatures.cs`, `IccImageStateSignatures.cs`, `IccStandardColorimetry.cs`, and `IccValidationRuleIds.cs`.

### Exceptions (`src/Api/Exceptions`)

Planned direct `System.Exception` subclasses are `IccProfileFormatException.cs`, `IccProfileValidationException.cs`, `IccProfileWriteException.cs`, `IccTransformException.cs`, `IccUnsupportedFeatureException.cs`, and `IccEmbeddingException.cs`, each with one message constructor.

### Public implementations (`src/Api/Classes`)

Planned one-constructor interface implementations are `IccProfileReader.cs`, `IccProfileWriter.cs`, `IccProfileValidator.cs`, `IccProfileInspector.cs`, `IccProfileBuilder.cs`, `IccProfileIdCalculator.cs`, `IccSignatureDescriber.cs`, `IccColorMath.cs`, `IccChromaticAdapter.cs`, `IccToneCurveEvaluator.cs`, `IccTransformFactory.cs`, `IccColorTransform.cs`, `IccProfileLinker.cs`, `IccGamutAnalyzer.cs`, `IccProfileComparer.cs`, `IccEmbeddedProfileReader.cs`, and `IccEmbeddedProfileWriter.cs`.

### Internal implementation files

The planned non-public implementation is split into `src/Internal/Binary/{BigEndianReader,BigEndianWriter,IccNumberCodec,FourCcCodec,CheckedSize,SpanCursor,StreamBuffer}.cs`; `src/Internal/Profiles/{ProfileCodec,HeaderCodec,TagTableCodec,ProfileIdCore,ProfileClone}.cs`; `src/Internal/Tags/{TagCodecDispatcher,UnknownTagCodec,LegacyTagCodec,ArrayTagCodec,TextTagCodec,LocalizedUnicodeTagCodec,DictionaryTagCodec,DateTimeTagCodec,SignatureTagCodec,XyzTagCodec,MeasurementTagCodec,ViewingConditionsTagCodec,ChromaticityTagCodec,CicpTagCodec,NamedColorTagCodec,ColorantTagCodec,ResponseCurveTagCodec,ProfileSequenceTagCodec,LutTagCodec,MultiProcessElementsTagCodec,AdaptiveGainCurveTagCodec,IccMaxTagCodec,IccMaxArrayCodec,IccMaxStructureCodec,IccMaxElementCodec,CalculatorCodec,SpectralCodec,GamutBoundaryCodec,EmbeddedImageCodec}.cs`; `src/Internal/Transforms/{CurveMath,MatrixMath,ColorMathCore,ChromaticAdaptationCore,ClutInterpolator,PipelineCompiler,PipelineExecutor,IntentSelector,PcsConverter,BlackPointCompensation,GamutEvaluator,CalculatorVirtualMachine,SpectralMath,IccCamMath,BrdfEvaluator,TransformLinker}.cs`; `src/Internal/Validation/{ValidationContext,HeaderValidator,TagTableValidator,TagDataValidator,ProfileClassValidator,TransformValidator,IccMaxValidator,SecurityValidator,QualityValidator}.cs`; `src/Internal/Inspection/{InspectionBuilder,InspectionTextWriter,SignatureDatabase}.cs`; `src/Internal/Construction/{ProfileConstructionCore,MatrixProfileFactory,LutProfileFactory,NamedColorProfileFactory,DeviceLinkFactory}.cs`; and `src/Internal/Embedding/{CarrierDetector,RawProfileScanner,JpegProfileCodec,PngProfileCodec,TiffProfileCodec,WebPProfileCodec,Jpeg2000ProfileCodec,IsoBmffProfileCodec}.cs`.

## Implementation tasks

### Foundation and public model

- [DONE] T001: Create the mandated `src/Api` category directories and internal binary/profile/tag/transform/validation/inspection/construction/embedding directories without introducing any public type outside `src/Api`.
- [DONE] T002: Implement field-only API option/result primitives for resource limits, read/write modes, operation status, byte locations, and exception-less probes, with conservative defaults suitable for untrusted profiles.
- [DONE] T003: Implement the public version, date/time, profile ID, header, tag-table entry, raw-data, tag-data union, tag, and profile data classes and connect every data class to a public contract or containing data class.
- [DONE] T004: Implement default-int public enums for versions, classes, colour/PCS kinds, rendering intents, curve/element kinds, validation, transforms, inspection, gamut, measurements, and embedding.
- [DONE] T005: Implement field-only static signature/constants classes for ICC.1, ICC.2, current amendments, standard colorimetry, and stable validation rule IDs.
- [DONE] T006: Implement the six one-constructor public exception classes and establish which throwing API operations use each exception versus returning `IccOperationResult`.
- [DONE] T007: Define all public contracts with unique non-overloaded method names, positional parameters, span/memory/stream forms where appropriate, and no interface inheritance or generic methods.
- [DONE] T008: Implement checked FourCC ASCII/hex conversion and stable signature description lookup with registered ICC.1/ICC.2 names plus an unknown/private representation.
- [DONE] T009: Implement checked size/count/offset arithmetic that rejects signed overflow, unsigned wrap, impossible array sizes, and resource-limit violations before allocation.
- [DONE] T010: Implement big-endian span cursors and stream buffering for exact reads, writes, skips, slices, alignment, and absolute byte-offset tracking on seekable and non-seekable inputs.
- [DONE] T011: Implement all ICC.1 base number codecs, including fixed-point rounding/saturation policy, XYZNumber, dateTimeNumber, positionNumber, response16Number, and rejection/reporting of non-finite values where forbidden.
- [DONE] T012: Implement ICC.2 extended number codecs for binary16/binary64, angles, horizontal/vertical values, spectral ranges, tint arrays, value encodings, and sparse-matrix encodings.
- [DONE] T013: Implement deep profile/tag-data cloning without inheritance, reflection-dependent serialization, or aliasing mutable caller buffers.
- [DONE] T014: Implement public colour, matrix, curve, LUT, MPE, text, named-colour, measurement, sequence, CICP, adaptive-gain, spectral, hierarchy, gamut, BRDF, validation, inspection, construction, transform, comparison, and embedding data classes listed in the API plan.
- [DONE] T015: Implement all public one-constructor service classes as thin interface implementations backed by internal components, exposing no public members beyond their implemented contracts.

### Header, tag table, and profile parsing

- [DONE] T016: Implement generation-aware parsing of every byte in ICC v2 and v4 headers, retaining raw values alongside decoded class/space/intent data and recording reserved-field anomalies.
- [DONE] T017: Implement ICC.2 v5 header parsing, including profile/device subclass versioning, spectral PCS and ranges, bi-spectral range, MCS, HDR flags, extended device spaces, and contextual bytes 100-127.
- [DONE] T018: Implement strict version recognition for v2.x, v4.0-v4.4, and v5.0 with tolerant preservation of future minor versions and precise rejection of unsupported major versions when requested.
- [DONE] T019: Implement profile-size, `acsp`, minimum-size, truncation, trailing-data, embedded-offset, and exact-end checks before tag decoding.
- [DONE] T020: Implement tag-table parsing with checked count math, bounds/alignment checks, duplicate-signature detection, legal shared-payload recognition, overlap/gap detection, padding capture, and original-order preservation.
- [DONE] T021: Implement lazy bounded tag decoding so header/table inspection does not allocate all payloads and repeated access returns a stable decoded value or stable failure.
- [DONE] T022: Implement tag codec dispatch by payload type signature and profile generation, retaining original payload bytes for preservation mode and falling back losslessly for unknown or unsupported types.
- [DONE] T023: Implement strict, conforming, and recovery parser behavior, including accumulation of non-fatal diagnostics without reading past a malformed tag boundary.
- [DONE] T024: Implement profile reads from `ReadOnlySpan<byte>`, `ReadOnlyMemory<byte>`, seekable streams, and non-seekable streams, plus a cheap header/profile probe that does not throw for expected invalid input.
- [DONE] T025: Implement bounded raw `acsp` scanning that validates candidate declared sizes and tag tables and reports every valid unsegmented profile location without false reads past the carrier.

### ICC.1 tag decoding and encoding

- [DONE] T026: Implement integer/fixed/XYZ array, signature, date/time, data, and version-specific ASCII text tag codecs with reserved-byte, count, terminator, and alignment handling.
- [DONE] T027: Implement v2 textDescriptionType with ASCII, Unicode, ScriptCode fields, inconsistent legacy lengths, tolerant decoding, and byte-preserving reserialization.
- [DONE] T028: Implement multiLocalizedUnicodeType and localized lookup/fallback, UTF-16BE validation, duplicate locale reporting, stable locale ordering, and shared-string payload handling.
- [DONE] T029: Implement dictType with name/value and localized display-name/value positions, legal sharing, metadata registry keys, unknown keys, and deterministic entry ordering.
- [DONE] T030: Implement curveType identity/gamma/sampled forms and parametricCurveType functions 0-4 using the corrected ICC.1:2022 equations and exact parameter counts.
- [DONE] T031: Implement chromaticityType, measurementType, viewingConditionsType, and CICP type including the unspecified-primaries amendment and class/space restrictions.
- [DONE] T032: Implement colorantOrderType and colorantTableType with 1-15 channel checks, fixed 32-byte names, v2/v4 PCS decoding, device-link output semantics, and duplicate colourant diagnostics.
- [DONE] T033: Implement namedColorType and namedColor2Type with prefix/suffix, exact names, PCS and optional device coordinates, tint semantics where applicable, channel consistency, and nearest-colour data preparation.
- [DONE] T034: Implement profileSequenceDescType and profileSequenceIdentifierType with nested version-specific description types, profile IDs, legal offset sharing, and complete sequence records.
- [DONE] T035: Implement responseCurveSet16Type with measurement-unit signatures, per-channel response arrays, XYZ values, response16Numbers, offsets, and channel/count validation.
- [DONE] T036: Implement lut8Type and lut16Type parsing/writing with input/output table counts, 3x3 matrix, N-dimensional CLUT sizing, integer PCS encodings, and overflow-safe table allocation.
- [DONE] T037: Implement lutAToBType and lutBToAType parsing/writing for every legal stage combination, curve/matrix/CLUT offsets, identity omissions, channel transitions, and v2/v4 encoding differences.
- [DONE] T038: Implement v4 multiProcessElementsType container parsing/writing with float32 channel ranges, position tables, shared elements, nested bounds, and future-element preservation.
- [DONE] T039: Implement v4 MPE curve-set, segmented formula/sampled curves, arbitrary matrix with constants, and N-dimensional float CLUT element codecs.
- [DONE] T040: Implement begin/end ACS element recognition and pass-through semantics while retaining unsupported vendor ACS payloads and identifying non-executable pipelines.
- [DONE] T041: Implement the adaptiveGainCurveType header, shared RGB node tables, GUID/image-specific rules, cubic Hermite evaluation data, target-headroom metadata, and current validation constraints.
- [DONE] T042: Implement legacy crdInfoType, screeningType, ucrBgType, namedColorType, deviceSettingsType raw-known representation, and all v2 PostScript/screening/UCR tag mappings.
- [DONE] T043: Implement recognized inspection and lossless round-trip support for common registered `vcgt`, `MHC2`, and other vendor payloads without treating them as ICC core conformance types.
- [DONE] T044: Implement the complete version-specific mapping from every ICC.1 public/legacy tag signature to permitted tag types, semantic role, profile classes, and channel expectations.

### ICC.2 tag decoding and encoding

- [DONE] T045: Implement ICC.2 UTF-8, UTF-16, zipped UTF-8, zipped XML, float16/32/64 array, and sparseMatrixArray tag types with bounded decompression and encoding validation.
- [DONE] T046: Implement generic tagStructType and tagArrayType codecs with nested type dispatch, offsets, identifiers, shared members, recursion limits, unknown members, and deterministic canonical layout.
- [DONE] T047: Implement typed BRDF transform, colorant info, colour-encoding parameters, measurement info, named-colour/tint, PCC, profile-info, and tint-zero structure projections over generic structures.
- [DONE] T048: Implement namedColorArray, profileInfoArray, generic structure arrays, multiplex type/default records, and colour-encoding abbreviated-profile data.
- [DONE] T049: Implement spectralViewingConditionsType for standard/custom illuminants and observers, spectral ranges, colorimetry, and late-binding inputs.
- [DONE] T050: Implement gamutBoundaryDescriptionType mesh vertices/faces, PCS and optional device coordinates, topology/index checks, and intent-specific GBD tags.
- [DONE] T051: Implement embeddedHeightImageType and embeddedNormalImageType metadata/payload codecs plus surfaceMap structures under strict dimension/size/compression limits.
- [DONE] T052: Implement extended ICC.2 multiProcessElementsType parsing/writing, including element positions, channel continuity, interpolation/value encodings, future elements, and profile/PCC context.
- [DONE] T053: Implement ICC.2 single/segmented curve-set, matrix, CLUT, extended CLUT, sparse matrix, tint array, and tone-map processing-element codecs.
- [DONE] T054: Implement XYZToJab and JabToXYZ processing-element codecs and their complete ICC CAM viewing-condition data.
- [DONE] T055: Implement emission CLUT, emission matrix, inverse emission matrix, emission observer, reflectance CLUT, and reflectance observer processing-element codecs.
- [DONE] T056: Implement calculatorElement bytecode parsing/writing for every operation signature, operands, sub-elements, environment references, variables, and forward-compatible unknown-operation preservation.
- [DONE] T057: Implement all ICC.2 A/B/D/M/S, BRDF, directional, HDR-to-SDR, PCC, spectral, MCS, surface, sequence, reference, colour-encoding, measurement, and named-colour tag mappings listed in the feature inventory.
- [DONE] T058: Implement the extended device colour-space amendment and preserve compatibility with the original ICC.2:2023 channel/signature rules.
- [DONE] T059: Implement ICC.1 embeddedProfileTag (`ICC5`) decoding/writing with nested resource limits, same-class/device-space checks, embedded/use-independent flags, and fallback-to-v4 semantics.

### Deterministic writing and profile identity

- [DONE] T060: Implement a two-pass profile writer that pre-encodes payloads, checks the final 32-bit profile size, chooses stable offsets, and writes exact bytes to memory or streams.
- [DONE] T061: Implement canonical ordering by unsigned tag signature, stable payload ordering, optional byte-identical payload interning, four-byte zero padding, contiguous layout, and exact EOF.
- [DONE] T062: Implement preservation-mode writing that retains unknown/private payloads, original tag ordering, shared payload identity, legal original encodings, and untouched bytes while rebuilding edited offsets safely.
- [DONE] T063: Implement deterministic numeric/text serialization rules for fixed-point rounding, float bit preservation/canonicalization options, Unicode normalization policy, terminators, locale ordering, and dictionary/structure ordering.
- [DONE] T064: Implement v2/v4/v5 header serialization with version-appropriate reserved/context fields, embedded flags, attributes, D50/spectral illuminants, and caller-selected creation-time preservation.
- [DONE] T065: Implement RFC 1321/ICC Profile ID computation using built-in .NET cryptography and the required zeroing of flags, rendering intent, and ID fields without mutating the source profile.
- [DONE] T066: Implement Profile ID verify/update operations and writer integration so canonical output is reproducible and preservation output changes the ID only under explicit policy.
- [DONE] T067: Implement byte-size preflight and write-failure reporting that identifies the exact tag/element exceeding limits or lacking a writable codec.

### Colour science, curves, and descriptions

- [DONE] T068: Implement XYZ, Lab, xyY, and LCh data models and conversions using double precision, ICC D50 constants, explicit white points, stable achromatic-hue behavior, and ICC piecewise constants.
- [DONE] T069: Implement v2 and v4 PCS Lab/XYZ integer encode/decode, float PCS range handling, negative PCSXYZ support, normalization, quantization, and round-trip guarantees within encoded precision.
- [DONE] T070: Implement dense matrix multiplication, composition, determinant, inversion, vector application, singularity checks, and 3x3 conveniences without public operator overloads.
- [DONE] T071: Implement Bradford chromatic-adaptation matrix construction, `chad` application/inversion, D50 adaptation, and media-white/PCS adopted-white conversions.
- [DONE] T072: Implement media-relative to ICC-absolute and ICC-absolute to media-relative PCS conversion for reflective, transmissive, emissive, scene, and extended-range cases defined by profile metadata.
- [DONE] T073: Implement identity, gamma, sampled, parametric, segmented formula, segmented sampled, and adaptive-gain curve evaluation with documented endpoint and extrapolation behavior.
- [DONE] T074: Implement curve inversion for monotonic curves, analytical parametric cases, numerical sampled/segmented cases, duplicate plateaus, and explicit non-invertible results.
- [DONE] T075: Implement curve sampling/resampling, monotonicity, continuity, derivative/slope, domain/range, and round-trip analysis returned through `IccCurveAnalysis`.
- [DONE] T076: Implement CIE76, both CIE94 application parameter sets, and CIEDE2000 colour differences against published reference vectors.
- [DONE] T077: Implement localized profile/tag descriptions, CICP descriptions, colour-space/channel names, intent/class summaries, FourCC hex/ASCII formatting, and stable human-readable `IccColorDescription` results.
- [DONE] T078: Implement named-colour exact/normalized lookup, prefix/suffix handling, tint interpolation, PCS/device coordinate selection, and nearest CIEDE2000 matching.

### Transform selection, execution, linking, and gamut

- [DONE] T079: Implement 1-D linear interpolation with exact endpoints and safe extrapolation/clipping policies for sampled curves and LUT shapers.
- [DONE] T080: Implement checked N-dimensional multilinear CLUT interpolation for 1-15 inputs and arbitrary outputs with reusable scratch spans and no per-sample unbounded allocation.
- [DONE] T081: Implement 3-D tetrahedral CLUT interpolation and expose explicit interpolation selection while retaining multilinear behavior for non-3-D tables.
- [DONE] T082: Implement execution stages for curves, matrices/offsets, CLUTs, PCS encoding conversions, channel reorder/defaults, clipping, and identity/unsupported elements.
- [DONE] T083: Implement ICC.1 transform-tag precedence for class, direction, requested intent, D2B/B2D versus A2B/B2A, intent fallback, matrix/TRC fallback, gamut tags, and absolute-intent synthesis.
- [DONE] T084: Compile gray and RGB matrix/TRC profiles into device↔PCS plans with correct curve order, matrix columns, inversion, PCSXYZ restriction, and luminance metadata.
- [DONE] T085: Compile LUT8/LUT16 and lutAToB/lutBToA tags into normalized immutable plans with correct stage ordering, channel domains, PCS ranges, and optional stages.
- [DONE] T086: Compile and execute v4 MPE curve-set/matrix/CLUT pipelines, rejecting unknown executable elements before the first sample is transformed.
- [DONE] T087: Implement the calculator virtual machine with all ICC.2 operations, deterministic IEEE behavior, verified stack effects, bounded instructions/recursion/storage, and no host-code execution.
- [DONE] T088: Implement ICC CAM XYZ↔Jab evaluation used by v5 appearance elements, including viewing-condition validation and stable inverse behavior.
- [DONE] T089: Implement spectral sampling/interpolation/integration, illuminant/observer late binding, reflectance/emission conversions, fluorescent and bi-spectral handling, and PCC colourimetric conversion.
- [DONE] T090: Implement ICC.2 spectral observer, matrix, CLUT, sparse-matrix, tint-array, tone-map, HDR-to-SDR, and extended-CLUT execution stages.
- [DONE] T091: Implement MCS channel-name matching, stable reordering, required/subset checks, missing-channel defaults, and MultiplexIdentification/Link/Visualization connections.
- [DONE] T092: Implement BRDF structure/function and directional-emission evaluation sufficient to execute the standardized parameter and function tag workflows, with explicit unsupported model results.
- [DONE] T093: Implement single-colour transform execution with input/output channel validation, optional clipping, gamut status, and trace capture for every intermediate stage.
- [DONE] T094: Implement strided interleaved float-span buffer transformation, overlap-safe behavior, exact sample-count validation, and reuse of caller/internal bounded scratch storage.
- [DONE] T095: Implement standard source/destination profile chaining through PCS, required Lab↔XYZ and v2↔v4 bridges, intent selection per link, and immutable multiprofile plans.
- [DONE] T096: Implement ISO 18619 black-point detection/compensation and integrate it only at eligible media-relative profile links under explicit transform options.
- [DONE] T097: Implement proofing chains, proofing intent, gamut-check tag execution, configurable alarm vectors, and clear separation of source, proof, and destination stages.
- [DONE] T098: Implement DeviceLink profile application semantics, input/output colour-space verification, sequence metadata, and direct A2B0/D2B0 execution.
- [DONE] T099: Implement profile linking that bakes a compatible profile sequence into a deterministic LUT/MPE DeviceLink with caller-selected grid/encoding and preserves intent/sequence information.
- [DONE] T100: Implement gamut membership through gamut tags and GBD meshes, point/triangle checks, boundary summaries, and explicit Unknown status when no valid gamut model exists.
- [DONE] T101: Implement exact semantic fingerprints and approximate profile comparison based on headers, decoded transforms, sampled curves/matrices/LUT outputs, tolerances, and substitutability reporting.

### Validation, security, quality, and inspection

- [DONE] T102: Implement stable validation reporting with severity, rule ID, path, offset/length, remediation, deterministic ordering, issue aggregation, and maximum-issue limits.
- [DONE] T103: Implement complete v2/v4/v5 header validation for size, version, class/subclass, spaces/channels, dates, `acsp`, platform, flags, manufacturer/model/creator/CMM, attributes, intent, illuminant, ID, spectral/MCS fields, and reserved bytes.
- [DONE] T104: Implement tag-table validation for checked lengths, alignment, bounds, unique signatures, valid sharing, no partial overlaps, canonical gaps/padding, exact profile end, and hidden trailing data.
- [DONE] T105: Implement generic tag-payload validation for type headers, reserved zeros, legal lengths/counts/offsets/alignment, string encodings, decompression limits, unknown/private registration status, and raw payload safety.
- [DONE] T106: Implement exhaustive tag-specific validation for every ICC.1 type, including numeric ranges, required terminators, channel counts, curve functions, matrix invertibility, CLUT sizes, nested offsets, PCS encodings, and adaptive-gain constraints.
- [DONE] T107: Implement version-specific tag/type compatibility and required/optional/forbidden tag matrices for every ICC.1 class and matrix/TRC, monochrome, LUT, MPE, named-colour, abstract, colour-space, and DeviceLink model.
- [DONE] T108: Implement cross-tag semantic validation for header/tag colour spaces, channel counts, media white and `chad`, transform pairs/intents, colorants, profile sequences, image state/viewing conditions, CICP, metadata, and profile ID.
- [DONE] T109: Implement ICC.2 type, array, structure, MPE, spectral/PCC, MCS, HDR, GBD, BRDF, directional, surface-map, subclass, abbreviated-profile, and extended-device-space validation.
- [DONE] T110: Implement declarative `IccValidationPolicy` evaluation for additional ICC.2 ICS required/optional/forbidden tags, supported elements, channel rules, and profile subclass constraints.
- [DONE] T111: Implement security-strict validation for allocation bombs, count multiplication, overlapping aliases, decompression bombs, deep nesting, calculator cost/stack/recursion, malformed images, non-finite values, executable private elements, and data hidden in gaps/trailers.
- [DONE] T112: Implement embedded-profile validation for profile flags, use-independent restrictions, permitted profile classes, container size/version rules, and ICC.1/embedded ICC.2 class/device-space equivalence.
- [DONE] T113: Implement curve/profile round-trip, transform continuity/smoothness, and first/second round-trip quality sampling with mean/maximum/95th-percentile CIEDE2000 statistics.
- [DONE] T114: Implement optional comparison with supplied characterization colour pairs, media-relative normalization, per-sample differences, aggregate quality results, and explicit absence/not-applicable status.
- [DONE] T115: Implement bounded inspection trees for headers, tag tables, decoded and raw tags, LUTs/curves/MPEs, ICC.2 hierarchies, validation issues, shared payloads, transform precedence, and large-value truncation.
- [DONE] T116: Implement deterministic plain-text inspection and profile summaries comparable in coverage to `iccdump`, including verbosity/detail options, selected tags, profile ID status, private/unknown labels, and transform traces.

### Basic profile construction and mutation

- [DONE] T117: Implement construction of generation-correct profile skeletons with required header defaults, profile description, copyright, media white, optional `chad`, creation time policy, and pre-write validation.
- [DONE] T118: Implement RGB matrix/TRC input, display, and colour-space profile construction from xy/xyY primaries, source white, per-channel curves, luminance, CICP, and localized metadata.
- [DONE] T119: Implement gray TRC and XYZ identity profile construction with correct class-specific required tags, white adaptation, PCS choice, and v2/v4 text encodings.
- [DONE] T120: Implement basic LUT/MPE abstract profile construction from supplied curves, matrices, CLUTs, adaptive-gain data, and intent metadata.
- [DONE] T121: Implement named-colour profile construction from names, prefix/suffix, PCS values, optional device coordinates/tints, colorants, and localized descriptions.
- [DONE] T122: Implement simple output-profile construction from caller-supplied validated forward/backward intent pipelines, gamut data, colorant tables, measurement/viewing data, and xCLR spaces.
- [DONE] T123: Implement v5 colour-encoding/abbreviated profile construction from registered/reference names, colour-encoding parameters, PCC/spectral data, and optional fallback transforms.
- [DONE] T124: Implement safe tag add/replace/remove and header mutation through builder inputs while preserving unknown data and refusing changes that leave an invalid profile.
- [DONE] T125: Integrate deterministic serialization, Profile ID generation, and full conformance validation into every builder result so identical definitions produce identical profile bytes under fixed creation-time options.

### Embedded profiles

- [DONE] T126: Implement carrier detection and bounded embedded-profile enumeration with a consistent location/result model and raw-scanner fallback.
- [DONE] T127: Implement JPEG APP2 `ICC_PROFILE` segmented profile extraction, ordering/count validation, duplicate/missing segment detection, replacement, insertion, and removal while preserving unrelated markers and scan data.
- [DONE] T128: Implement PNG `iCCP` extraction and mutation with profile-name validation, bounded built-in zlib compression/decompression, chunk CRC/order checks, and colour-chunk conflict reporting.
- [DONE] T129: Implement classic TIFF, BigTIFF, and DNG tag 34675 extraction/mutation for both byte orders, inline/offset values, IFD chains, checked offsets, and preservation of unrelated entries/data.
- [DONE] T130: Implement WebP RIFF `ICCP` chunk extraction/mutation, VP8X feature-bit updates, chunk padding, RIFF size checks, and preservation of unknown chunks.
- [DONE] T131: Implement JPEG 2000 JP2/JPX `colr` method 2/3 ICC payload enumeration and replacement using checked nested box parsing without decoding codestream image data.
- [DONE] T132: Implement ISO BMFF AVIF/HEIF `colr` `prof`/`rICC` payload enumeration and replacement with checked 32/64-bit box sizes and preservation of unrelated boxes.
- [DONE] T133: Implement clear unsupported-carrier/integration results for PDF, OpenXPS, SVG, CSS, EPS, and JPEG XL while allowing callers to pass extracted raw profile bytes through the core API.

### Complete public-API test coverage

- [DONE] T134: Build a licensed test corpus of valid and malformed v2, v4.0-v4.4, amended v4, and v5 profiles from ICC/iccDEV plus minimal hand-built fixtures for every profile class, space, intent, tag type, and processing element.
- [DONE] T135: Add deterministic binary-fixture helpers and independent big-endian encoders under `tests/` so parser expectations are not generated solely by production codecs.
- [DONE] T136: Test every public enum value, static signature/constant field, data-class field/default, options default, and signature-description result used by the `src/Api` layer.
- [DONE] T137: Test every `IIccProfileReader` operation across span, memory, seekable stream, non-seekable stream, probe, strict/recovery modes, future minor versions, partial input, and all resource limits.
- [DONE] T138: Test header and tag-table parsing for every version field, class/space/intent combination, shared tag data, ordering, alignment, padding, gaps, overlaps, duplicates, truncation, overflow, and trailing bytes.
- [DONE] T139: Test every ICC.1 base number and tag codec with minimum/maximum/boundary values, malformed lengths/counts/offsets, reserved bytes, version-specific encodings, and encode/decode golden bytes.
- [DONE] T140: Test every legacy v2 tag/type, v2 Lab/XYZ/text behavior, required-tag matrix, tolerant legacy quirk, and byte-preserving unknown/private round trip.
- [DONE] T141: Test LUT8, LUT16, lutAToB, and lutBToA parsing/writing/execution for every legal stage combination, 1-15 channels, PCS mappings, interpolation boundaries, and overflow rejection.
- [DONE] T142: Test v4 MPE containers and curve-set/matrix/CLUT/ACS elements, shared offsets, segmented curves, future elements, channel mismatch, compilation, execution, and deterministic writeback.
- [DONE] T143: Test CICP plus its amendment and adaptive gain curve parsing, validation, cubic evaluation, shared curves, headroom cases, image-specific flags, approximation metadata, and deterministic serialization.
- [DONE] T144: Test every ICC.2 extended base/tag type, compressed text/XML bound, generic/typed array and structure, unknown member, nested sharing, recursion limit, and canonical/preservation round trip.
- [DONE] T145: Test every ICC.2 processing element and transform-tag family, including sparse/tint/tone/CAM, spectral emission/reflectance, MCS, PCC, GBD, BRDF/directional, surface images, HDR, and extended device spaces.
- [DONE] T146: Test every calculator operation and operand form against independent expected stacks/results, malformed programs, non-finite behavior, variables/environment/sub-elements, and each instruction/stack/storage/recursion safety limit.
- [DONE] T147: Test `IIccProfileWriter` memory/stream/preflight operations, canonical ordering/interning/padding, preservation behavior, exact sizes, stable failures, repeated byte identity, and parse-write-parse semantic identity.
- [DONE] T148: Test `IIccProfileIdCalculator` compute/verify/update operations against known ICC IDs and independent RFC 1321 digest preparation, including fields that must be zeroed and non-mutating behavior.
- [DONE] T149: Test every `IIccColorMath` conversion, PCS encoding, matrix operation, adaptation, absolute/relative conversion, and CIE76/CIE94/CIEDE2000 result against published or independently generated vectors.
- [DONE] T150: Test every `IIccToneCurveEvaluator` operation for all curve kinds, endpoints, extrapolation, inversion, plateaus/non-monotonic failure, sampling, derivative/continuity analysis, and round trips.
- [DONE] T151: Test `IIccTransformFactory` precedence and fallback for every class/direction/intent/version and test `IIccColorTransform` single/buffer/stride/overlap/trace operations for gray, matrix/TRC, LUT, v4 MPE, and v5 MPE plans.
- [DONE] T152: Test multiprofile, proofing, gamut alarm, absolute intent, black-point compensation, named-colour, abstract, DeviceLink, HDR/SDR, v2↔v4, and ICC.1↔ICC.2 connection behavior with independent golden outputs.
- [DONE] T153: Test `IIccProfileLinker` for compatible and incompatible chains, intent metadata, colour-space continuity, selected grid/encoding, sequence tags, deterministic DeviceLink bytes, and application equivalence.
- [DONE] T154: Test `IIccProfileValidator` in all modes for every stable rule ID, severity, path/offset, required/optional/forbidden matrix, semantic relation, security ceiling, ICS policy, issue limit, and deterministic ordering.
- [DONE] T155: Test quality validation for curve/profile round trips, smoothness, characterization comparisons, mean/max/95th-percentile values, exact sampling determinism, and not-applicable cases.
- [DONE] T156: Test every `IIccProfileInspector` summary/tree/text/raw/selected-tag/trace operation at all detail levels, truncation limits, partial profiles, unknown values, shared payloads, and deterministic output.
- [DONE] T157: Test every `IIccProfileBuilder` construction category, required defaults, adaptation, localized metadata, invalid/incomplete inputs, version-specific representations, validation integration, and deterministic bytes.
- [DONE] T158: Test safe mutation for every editable header/tag operation, unknown/private preservation, shared-payload changes, invalid-result rejection, Profile ID policy, and canonical versus preservation results.
- [DONE] T159: Test every `IIccEmbeddedProfileReader` operation and malformed case for raw, JPEG, PNG, TIFF/BigTIFF/DNG, WebP, JP2/JPX, and AVIF/HEIF carriers, including multiple profiles and bounded decompression/scanning.
- [DONE] T160: Test every `IIccEmbeddedProfileWriter` insert/replace/remove operation with byte-level carrier goldens, unrelated-data preservation, size/CRC/feature updates, unsupported carriers, and re-extraction equivalence.
- [DONE] T161: Test every `IIccGamutAnalyzer` and `IIccProfileComparer` operation for exact, approximate, equivalent/substitutable, different, missing-transform, mesh boundary, and unknown-gamut cases.
- [DONE] T162: Test every public throwing operation and each public exception type/message path, and every expected-failure non-throwing operation/status, without relying on exception handling inside production source.
- [DONE] T163: Add differential tests under `tests/` against Little CMS/skcms/iccDEV or precomputed outputs for shared v2/v4 parsing, transform, ID, serialization, and validation cases, with explicit assertions for intentional standard-driven differences.
- [DONE] T164: Add randomized/property tests under `tests/` for numeric codecs, offsets/counts, profile parse-write-parse invariants, curve/LUT interpolation, matrix inversion, tag-order permutations, unknown payloads, and mutation sequences.
- [DONE] T165: Configure test coverage reporting so every public contract method, public implementation method, public exception path, and meaningful branch reachable through `src/Api` has a test, with a 100% public-API coverage gate.
