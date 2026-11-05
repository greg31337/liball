# AVIF development references

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

Retrieved on 2026-08-17 and augmented on 2026-08-24. These files are implementation references, not
project dependencies. The library under `src/` must not copy code from them or link to them;
applicable licenses and standards terms remain with their publishers.

The edition choices and precedence rules that define project conformance are frozen in
[`docs/conformance.md`](../docs/conformance.md). This inventory records provenance and does not float
the project baseline when an upstream URL changes.

## Normative and closely related specifications

| Local file | Source | Purpose |
| --- | --- | --- |
| `avif-1.2.0.html` | https://aomediacodec.github.io/av1-avif/v1.2.0.html | Current approved AVIF specification. |
| `av1-isobmff-1.3.0.html` | https://aomediacodec.github.io/av1-isobmff/v1.3.0.html | AV1 sample entries, `av1C`, samples, tracks, codec strings, and sample groups. |
| `av1-bitstream-spec.pdf` | https://aomediacodec.github.io/av1-spec/av1-spec.pdf | Normative AV1 bitstream syntax and decoding process, including errata. |
| `av1-hdr10plus.html` | https://aomediacodec.github.io/av1-hdr10plus/ | HDR10+ metadata carriage in AV1. |
| `itu-h273-2024.pdf` | https://www.itu.int/rec/T-REC-H.273-202407-I | CICP primaries, transfer functions, matrix coefficients, range, and chroma locations. |
| `icc-1-2022.pdf` | https://www.color.org/specification/ICC.1-2022-05.pdf | ICC v4.4 profile format and color transforms. |
| `exif-3.1-2026.pdf` | https://www.cipa.jp/e/std/std-sec.html | Current Exif 3.1 format and tag definitions. |
| `exif-metadata-for-xmp-2026.pdf` | https://www.cipa.jp/e/std/std-sec.html | Current Exif-to-XMP mapping and reconciliation rules. |
| `xmp-part1-2012.pdf` | https://github.com/adobe/XMP-Toolkit-SDK/blob/main/docs/XMPSpecificationPart1.pdf | XMP data model, serialization, and core properties. |
| `xmp-part2-2022.pdf` | https://github.com/adobe/XMP-Toolkit-SDK/blob/main/docs/XMPSpecificationPart2.pdf | Standard XMP property schemas. |
| `xmp-part3-2020.pdf` | https://github.com/adobe/XMP-Toolkit-SDK/blob/main/docs/XMPSpecificationPart3.pdf | XMP packets and embedding in files. |
| `rfc6381-codecs-parameter.txt` | https://www.rfc-editor.org/rfc/rfc6381.txt | `codecs` media-type parameter rules. |
| `iana-image-avif.txt` | https://www.iana.org/assignments/media-types/image/avif | Registered `image/avif` media type. |

## HEIF and low-overhead material

| Local file | Source | Purpose |
| --- | --- | --- |
| `heif-committee-draft.html` | https://mpeg.chiariglione.org/standards/mpeg-h/image-file-format/text-isoiec-cd-23008-12-image-file-format.html | Public historical HEIF draft useful for box syntax; current ISO text governs where it differs. |
| `heif-low-overhead-amd2-2026-sample.pdf` | https://cdn.standards.iteh.ai/samples/iso/iso-iec-23008-12-2025-amd-2-2026/a5f69d86d9bb49c394d1046235c4de0f/iso-iec-23008-12-2025-amd-2-2026.pdf | Public sample of the standardized `mif3`/`mini` low-overhead amendment. |

The following current ISO standards are required for a definitive implementation but are not freely
downloadable from ISO. Their official records are listed here so implementers can use licensed copies:

- ISO/IEC 14496-12:2026, ISO base media file format: https://www.iso.org/standard/85596.html
- ISO/IEC 23008-12:2025, Image File Format (HEIF): https://www.iso.org/standard/89035.html
- ISO/IEC 23008-12:2025/Amd 1:2025, tone map derivation: https://www.iso.org/standard/89044.html
- ISO/IEC 23008-12:2025/Amd 2:2026, low-overhead image file format: https://www.iso.org/standard/90273.html
- ISO/IEC 23000-22:2025, MIAF: https://www.iso.org/standard/87576.html
- ISO 21496-1:2025, gain map metadata: https://www.iso.org/standard/86775.html

## Existing-library feature references

| Local file | Source | Research use |
| --- | --- | --- |
| `libavif-api-reference.h` | https://github.com/AOMediaCodec/libavif/blob/main/include/avif/avif.h | Public API and feature inventory for the focused AVIF reference library. |
| `libavif-readme.md` | https://github.com/AOMediaCodec/libavif/blob/main/README.md | Codec backends, tools, and supported formats. |
| `libheif-api-reference.h` | https://github.com/strukturag/libheif/blob/master/libheif/api/libheif/heif.h | Umbrella header for the modular libheif public API. |
| `libheif-*-api-reference.h` | https://github.com/strukturag/libheif/tree/097e29b2343ece41f4ee5d3898e801997ce9e98b/libheif/api/libheif | Modular public headers for brands, images, color, metadata, auxiliary images, entity groups, security, encoding, decoding, contexts, image handles, tiles, components, errors, library lifecycle, versioning, exports, and OMAF. |
| `libheif-sequences-api-reference.h` | https://github.com/strukturag/libheif/blob/master/libheif/api/libheif/heif_sequences.h | Sequence tracks, timing, metadata tracks, and sample auxiliary information. |
| `libheif-readme.md` | https://github.com/strukturag/libheif/blob/master/README.md | Cross-check for collections, tiles, transforms, streaming, metadata, and security limits. |

The snapshots are for behavioral comparison and feature discovery only. Tests may invoke separately
installed external implementations, but production code under `src/` must use only built-in .NET APIs.
