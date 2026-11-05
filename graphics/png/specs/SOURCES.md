# Specification sources

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

The files in this directory are pinned, local references for implementing and testing the library. PNG Third Edition is the normative source for both static PNG and APNG, including PNG-3 ancillary chunks; the older standalone APNG draft is therefore not duplicated here.

| Local file | Source | Purpose |
| --- | --- | --- |
| `png-3-20250624.html` | https://www.w3.org/TR/2025/REC-png-3-20250624/ | Immutable W3C PNG Third Edition Recommendation. |
| `png-3-errata.html` | https://www.w3.org/2025/06/REC-PNG-20250624-errata | Published errata for PNG Third Edition. |
| `png-3-implementation-report.html` | https://w3c.github.io/png/Implementation_Report_3e/ | W3C implementation and interoperability evidence. |
| `rfc1950-zlib.txt` | https://www.rfc-editor.org/rfc/rfc1950.txt | Normative zlib wrapper format used by PNG image data and compressed metadata. |
| `rfc1951-deflate.txt` | https://www.rfc-editor.org/rfc/rfc1951.txt | Normative DEFLATE compressed-data format. |
| `icc-1-2022-05.pdf` | https://archive.color.org/specification/ICC.1-2022-05.pdf | ICC profile format referenced by the PNG `iCCP` chunk. |
| `itu-t-h273-v4-202407.pdf` | https://www.itu.int/rec/T-REC-H.273-202407-I/en | Coding-independent code points referenced by the PNG `cICP` chunk. |
| `libpng-1.6.58-manual.txt` | https://github.com/pnggroup/libpng/blob/v1.6.58/libpng-manual.txt | Pinned stable libpng behavior and workflow reference. |
| `pngsuite/` | https://github.com/pnggroup/libpng/tree/v1.6.58/contrib/pngsuite | Pinned interoperability images and their upstream descriptions. |

The full Exif payload format is not mirrored because this library's planned `eXIf` support treats the payload as validated opaque data; the PNG-specific framing and decoder/encoder requirements are normative in PNG Third Edition.
