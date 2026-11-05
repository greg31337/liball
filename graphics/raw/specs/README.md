# Reference material

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

This directory contains the standards and implementation references used to plan the library. They are research inputs, not runtime dependencies. In particular, the LGPL reference source files must not be copied into `src/`; any implementation must be written independently from the specifications and observed file behavior, with applicable licenses reviewed first.

Coverage audit (2026-08-25): the local collection is sufficient for the feature inventory in `todo.md`. The publisher catalogues still identify DNG 1.7.1.0, Exif 3.1/DC-010-2026, IPTC Photo Metadata 2025.1, and ICC.1:2022 as the current applicable public specifications. TIFF/BigTIFF, JPEG, XMP, IPTC IIM, zlib/Deflate, CR2/CIFF, NEF, maker-note, and independent implementation references are also already present. No redundant downloads were added; the proprietary-format limitations described below remain the only material documentation gap.

## Standards and normative references

| Local file | Source | Purpose |
|---|---|---|
| `tiff-6.0.pdf` | [Adobe TIFF 6.0](https://helpx.adobe.com/content/dam/help/en/camera-raw/digital-negative/jcr_content/root/content/flex/items/position/position-par/download_section_1690056821/download-1/TIFF6.pdf) | TIFF headers, IFDs, field types, strips, tiles, predictors, and baseline compression. |
| `tiff-photoshop-notes.pdf` | [Adobe Photoshop TIFF Technical Notes](https://helpx.adobe.com/content/dam/help/en/camera-raw/digital-negative/jcr_content/root/content/flex/items/position/position-par/download_section_1690056821/download-2/TIFFphotoshop.pdf) | TIFF extensions including Deflate-related conventions. |
| `tiff-pagemaker-notes.pdf` | [Adobe PageMaker TIFF Technical Notes](https://helpx.adobe.com/content/dam/help/en/camera-raw/digital-negative/jcr_content/root/content/flex/items/position/position-par/download_section_1690056821/download-3/TIFFPM6.pdf) | TIFF supplement and interoperability notes. |
| `tiff-technical-note-2.html` | [LibTIFF copy of TIFF Technical Note 2](https://libtiff.gitlab.io/libtiff/specification/technote2.html) | Corrected TIFF/JPEG representation and lossless JPEG constraints. |
| `bigtiff-design.html` | [LibTIFF BigTIFF design](https://libtiff.gitlab.io/libtiff/specification/bigtiff.html) | 64-bit TIFF offset and IFD layout. |
| `dng-1.7.1.0.pdf` | [Adobe DNG 1.7.1.0](https://helpx.adobe.com/content/dam/help/en/camera-raw/digital-negative/jcr_content/root/content/flex/items/position/position-par/download_section_733958301/download-1/DNG_Spec_1_7_1_0.pdf) | Public TIFF/EP-derived raw tags, raw-to-linear mapping, color matrices, profiles, and opcodes. DNG itself is a research reference, not a requested output format. |
| `exif-3.1-2026.pdf` | [CIPA DC-008-2026 Exif 3.1](https://www.cipa.jp/e/std/std-sec.html) | Current Exif and GPS IFD tag definitions. Downloaded after accepting CIPA's displayed disclaimer. |
| `exif-metadata-for-xmp-2026.pdf` | [CIPA DC-010-2026](https://www.cipa.jp/e/std/std-sec.html) | Current Exif-to-XMP mapping and reconciliation. Downloaded after accepting CIPA's displayed disclaimer. |
| `xmp-part-1.pdf` | [Adobe XMP Part 1](https://github.com/adobe/XMP-Toolkit-SDK/blob/main/docs/XMPSpecificationPart1.pdf) | XMP data model, RDF serialization, and core properties. |
| `xmp-part-2.pdf` | [Adobe XMP Part 2](https://github.com/adobe/XMP-Toolkit-SDK/blob/main/docs/XMPSpecificationPart2.pdf) | Standard XMP schemas. |
| `xmp-part-3.pdf` | [Adobe XMP Part 3](https://github.com/adobe/XMP-Toolkit-SDK/blob/main/docs/XMPSpecificationPart3.pdf) | XMP packets and storage in files. |
| `icc-1-2022.pdf` | [ICC.1:2022](https://www.color.org/specification/ICC.1-2022-05.pdf) | ICC v2/v4 profile structure and color transforms. |
| `iptc-iim-4.2.pdf` | [IPTC IIM 4.2](https://www.iptc.org/std/IIM/4.2/specification/IIMV4.2.pdf) | Legacy IPTC IIM metadata records. |
| `iptc-photo-metadata-2025.1.html` | [IPTC Photo Metadata 2025.1](https://www.iptc.org/std/photometadata/specification/IPTC-PhotoMetadata-2025.1.html) | IPTC Core and Extension semantics and mappings. |
| `iptc-photo-metadata-2025.1.json` | [IPTC machine-readable TechReference](https://iptc.org/std/photometadata/specification/iptc-pmd-techreference_2025.1.json) | Machine-readable field definitions for test/data generation. |
| `itu-t-t81-jpeg.pdf` | [ITU-T T.81 / JPEG material](https://www.w3.org/Graphics/JPEG/itu-t81.pdf) | JPEG marker and coding reference, including lossless predictive coding. |
| `rfc1950-zlib.txt` | [RFC 1950](https://www.rfc-editor.org/rfc/rfc1950.txt) | Zlib wrapper format. |
| `rfc1951-deflate.txt` | [RFC 1951](https://www.rfc-editor.org/rfc/rfc1951.txt) | Deflate bitstream format. |

ISO 12234-2:2001 (TIFF/EP) is relevant but is not freely downloadable from ISO. Its [official catalogue entry](https://www.iso.org/standard/29377.html) and the publicly available TIFF 6.0/DNG materials above identify the relationship without placing an unauthorized copy here.

## Proprietary-format descriptions and interoperability references

| Local file | Source | Purpose and status |
|---|---|---|
| `cr2-format-poster.pdf` | [libcraw2 CR2 poster](https://github.com/lclevy/libcraw2/blob/master/docs/cr2_poster.pdf) | Reverse-engineered CR2 header, IFD layout, raw slices, and metadata map. Non-normative. |
| `cr2-lossless-jpeg.pdf` | [libcraw2 lossless JPEG poster](https://github.com/lclevy/libcraw2/blob/master/docs/cr2_lossless.pdf) | Visual description of CR2 lossless JPEG decoding. Non-normative. |
| `cr2-libopenraw.html` | [libopenraw CR2 description](https://libopenraw.freedesktop.org/formats/cr2/) | CR2 TIFF organization, header, compression, slicing, and thumbnail notes. Non-normative. |
| `canon-ciff-1.0r4.pdf` | [Archived Canon CIFF 1.0 revision 4](https://web.archive.org/web/20081230095207/http://xyrion.org/ciff/CIFFspecV1R04.pdf) | Historical Canon image-data container and metadata definitions relevant to old Canon conventions. |
| `nef-libopenraw.html` | [libopenraw NEF description](https://libopenraw.freedesktop.org/formats/nef/) | NEF TIFF organization and thumbnail locations. Non-normative. |
| `nikon-nef-recording-reference.html` | [Nikon NEF recording documentation](https://onlinemanual.nikonimglib.com/z7_z6/en-nsg/09_menu_guide_03_07.html) | Manufacturer description of 12/14-bit and uncompressed/lossy/lossless options. It does not disclose the codec. |
| `nikon-sdk-information.html` | [Nikon SDK information](https://sdk.nikonimaging.com/information/en/) | Manufacturer confirmation that Nikon distributes a licensed NEF/NRW Image SDK rather than a public bitstream specification. |
| `canon-makernote-reference.pm` | [ExifTool Canon module](https://github.com/exiftool/exiftool/blob/master/lib/Image/ExifTool/Canon.pm) | Reverse-engineered Canon tag catalogue. GPL/Artistic licensed reference; do not copy into source. |
| `nikon-makernote-reference.pm` | [ExifTool Nikon module](https://github.com/exiftool/exiftool/blob/master/lib/Image/ExifTool/Nikon.pm) | Reverse-engineered Nikon tag catalogue. GPL/Artistic licensed reference; do not copy into source. |
| `cr2-decoder-reference.cpp` | [RawSpeed CR2 decoder](https://github.com/darktable-org/rawspeed/blob/develop/src/librawspeed/decoders/Cr2Decoder.cpp) | Interoperability reference for observed CR2 variants. LGPL licensed; do not copy into source. |
| `nikon-decompression-reference.cpp` | [RawSpeed Nikon decompressor](https://github.com/darktable-org/rawspeed/blob/develop/src/librawspeed/decompressors/NikonDecompressor.cpp) | Interoperability reference for observed Nikon compression. LGPL licensed; do not copy into source. |
| `rawspeed-camera-reference.xml` | [RawSpeed camera data](https://github.com/darktable-org/rawspeed/blob/develop/data/cameras.xml) | Reference catalogue for camera aliases, CFA, crop, black/white, and color data. LGPL project data; establish provenance before using any values. |

## Existing-library feature references

| Local file | Source | Purpose |
|---|---|---|
| `libraw-feature-reference.html` | [LibRaw documentation](https://www.libraw.org/docs) | Broad RAW decoder/processor API and feature comparison. |
| `rawspeed-feature-reference.rst` | [RawSpeed README](https://github.com/darktable-org/rawspeed/blob/develop/README.rst) | Focused raw decode-stage scope, normalization, CFA, crop, and camera database behavior. |

## Known documentation gap

Canon CR2 and Nikon NEF are proprietary families, not single fully published formats. No public Canon or Nikon document in this collection is sufficient to implement every camera variant or to guarantee that a newly generated CR2/NEF will be accepted by vendor software. Nikon High Efficiency and High Efficiency Star NEF compression is especially undocumented and may involve third-party intellectual property. The implementation plan therefore requires observed-profile capability reporting, corpus-based conformance, opaque-data preservation, and an explicit legal/specification gate for those variants.

The completed [Nikon HE/HE* codec implementation gate](nikon-he-legal-review.md) did not clear a decoder or encoder. HE/HE* remains recognition/metadata/preview-only until the documented specification, patent, licensing, clean-room, and conformance requirements are all satisfied.
