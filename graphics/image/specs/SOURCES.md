# Specification Sources

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

Downloaded on 2026-08-24. The library covers formats whose standards range from freely published specifications to proprietary or paywalled standards. Primary standards are preferred below; implementation references are included where no complete public specification is available.

## Metadata standards

- `exif-3.1-cipa-dc-008-2026.pdf` — CIPA DC-008-Translation-2026, Exif 3.1: https://www.cipa.jp/e/std/std-sec.html
- `exif-metadata-for-xmp-cipa-dc-010-2026.pdf` — CIPA DC-010-2026, Exif metadata for XMP: https://www.cipa.jp/e/std/std-sec.html
- `multi-picture-format-cipa-dc-007-2026.pdf` — CIPA DC-007-Translation-2026, Multi-Picture Format: https://www.cipa.jp/e/std/std-sec.html
- `tiff-6.0.pdf` — Adobe TIFF Revision 6.0: https://helpx.adobe.com/camera-raw/digital-negative.html
- `bigtiff-design.html` — LibTIFF BigTIFF design: https://libtiff.gitlab.io/libtiff/specification/bigtiff.html
- `geotiff-1.1.html` — OGC GeoTIFF 1.1 standard: https://docs.ogc.org/is/19-008r4/19-008r4.html
- `dng-1.7.1.0.pdf` — Adobe Digital Negative 1.7.1.0: https://helpx.adobe.com/camera-raw/digital-negative.html
- `xmp-part-1-data-model-serialization-core-properties.pdf`, `xmp-part-2-additional-properties.pdf`, and `xmp-part-3-storage-in-files.pdf` — Adobe XMP specification: https://github.com/adobe/XMP-Toolkit-SDK/tree/main/docs
- `iptc-iim-4.2.pdf` — IPTC Information Interchange Model 4.2: https://iptc.org/standards/iim/
- `iptc-photo-metadata-2025.1.html` and `iptc-photo-metadata-techreference-2025.1.json` — IPTC Photo Metadata 2025.1 and its machine-readable reference: https://iptc.org/standards/photo-metadata/iptc-standard/
- `icc.1-2022-profile-format.pdf` — ICC.1:2022 profile format: https://www.color.org/icc-1_specification/
- `c2pa-2.2.html` — C2PA Technical Specification 2.2, including JUMBF use and image embedding: https://spec.c2pa.org/specifications/specifications/2.2/specs/C2PA_Specification.html

## Image and container formats

- `jpeg-itu-t81.pdf` — ITU-T T.81, JPEG-1 coding and interchange format: https://www.w3.org/Graphics/JPEG/itu-t81.pdf
- `jpeg-jfif-1.02.pdf` — JPEG File Interchange Format 1.02: https://www.w3.org/Graphics/JPEG/jfif3.pdf
- `png-third-edition.html` — W3C PNG Third Edition, including APNG: https://www.w3.org/TR/png-3/
- `gif89a.txt` — GIF89a specification: https://www.w3.org/Graphics/GIF/spec-gif89a.txt
- `webp-rfc-9649.pdf` and `webp-container.html` — RFC 9649 and Google's WebP RIFF container specification: https://www.rfc-editor.org/rfc/rfc9649 and https://developers.google.com/speed/webp/docs/riff_container
- `bmp-bitmapfileheader.html`, `bmp-bitmapinfoheader.html`, and `bmp-bitmapv5header.html` — Microsoft bitmap header definitions: https://learn.microsoft.com/en-us/windows/win32/gdi/bitmap-structures
- `ico-cur-resource-format.html` — Microsoft icon and cursor resource format description: https://learn.microsoft.com/en-us/windows/win32/menurc/about-icons
- `svg2-single-page.html` — W3C SVG 2 draft, single-page form: https://svgwg.org/svg2-draft/single-page.html
- `avif-1.2.0.pdf` — Alliance for Open Media AV1 Image File Format 1.2.0: https://aomedia.org/specifications/avif/
- `iso-bmff-box-registry.html` — MP4 Registration Authority ISO BMFF box registry: https://mp4ra.org/registered-types/boxes
- `jpeg-2000-itu-t800-2002.pdf` — freely published 2002 edition of ITU-T T.800; the current edition is paywalled: https://www.itu.int/rec/T-REC-T.800/
- `jpeg-xl-format-overview.md` and `jpeg-xl-whitepaper.pdf` — reference-implementation format overview and JPEG Committee white paper; ISO/IEC 18181 is paywalled: https://jpeg.org/jpegxl/documentation.html
- `photoshop-file-formats.html` — Adobe Photoshop PSD/PSB and image-resource format specification: https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/
- `dds-programming-guide.html` — Microsoft DirectDraw Surface programming guide: https://learn.microsoft.com/en-us/windows/win32/direct3ddds/dx-graphics-dds-pguide
- `qoi-1.0.pdf` — Quite OK Image Format 1.0: https://qoiformat.org/qoi-specification.pdf
- `netpbm-format-index.html`, `netpbm-pam.html`, and `netpbm-pfm.html` — Netpbm format references: https://netpbm.sourceforge.net/doc/
- `pcx-format-description.html` and `tga-format-description.html` — legacy format descriptions preserved by FileFormat.Info because the original vendors no longer maintain public specifications: https://www.fileformat.info/format/pcx/ and https://www.fileformat.info/format/tga/
- `radiance-file-formats.pdf` and `radiance-picture-format.html` — Radiance file and picture format references: https://radsite.lbl.gov/radiance/refer/
- `epsf-3.0.pdf` — Adobe Encapsulated PostScript File Format 3.0 mirror: https://printtechnologies.org/standards/files/epsf_spec_v3_0.pdf
- `canon-crw-format-description.html` — ExifTool's Canon CRW/CIFF format description: https://github.com/exiftool/exiftool/blob/master/html/canon_raw.html

## Supporting formats and practical tag references

- `zlib-rfc-1950.txt` and `deflate-rfc-1951.txt` — zlib and DEFLATE formats used by compressed PNG metadata: https://www.rfc-editor.org/rfc/rfc1950 and https://www.rfc-editor.org/rfc/rfc1951
- `maker-notes/*.html` — current ExifTool practical tag references for the planned Apple, Canon, DJI, FLIR, Fujifilm, GoPro, Hasselblad, Kodak, Minolta, Nikon, Olympus, Panasonic/Leica, Pentax, Ricoh, Samsung, Sigma/Foveon, and Sony maker notes, plus a maker-note type index: https://github.com/exiftool/exiftool/tree/master/html/TagNames

HEIF/HEIC, ISO BMFF, JPEG 2000, JPEG XL, JUMBF, and many camera-vendor RAW formats are wholly or partly defined by paywalled or unpublished specifications. The AVIF, MP4RA, C2PA, older T.800, JPEG Committee, LibTIFF, CIPA, Adobe, and ExifTool resources above provide the public structural and metadata information available for this implementation.
