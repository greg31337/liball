# JPEG implementation references

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

The repository had no local specification files when this set was assembled on 2026-08-25. The documents below cover the coding processes, interchange formats, color conventions, metadata formats, and compatibility behavior in the feature inventory. Normative standards take precedence over informative implementation references.

## JPEG standards

- `ITU-T-T81-1992-JPEG-1.pdf` — Core JPEG-1 coding processes and codestream syntax (ITU-T T.81 / ISO/IEC 10918-1), from the [W3C standards mirror](https://www.w3.org/Graphics/JPEG/itu-t81.pdf). The current ITU catalog copy requires paid or TIES access.
- `ITU-T-T84-1996-JPEG-extensions.pdf` — JPEG extensions and SPIFF (ITU-T T.84 / ISO/IEC 10918-3), from [ITU-T](https://www.itu.int/rec/T-REC-T.84/en).
- `ITU-T-T86-2024-APPn-markers.pdf` — Registered APPn marker identifiers and preservation rules (ITU-T T.86 / ISO/IEC 10918-4), from [ITU-T](https://www.itu.int/rec/T-REC-T.86/en).
- `ITU-T-T87-1998-JPEG-LS.pdf` — JPEG-LS coding and marker syntax (ITU-T T.87 / ISO/IEC 14495-1), from [ITU-T](https://www.itu.int/rec/T-REC-T.87/en).
- `ITU-T-T871-2011-JFIF.pdf` — JFIF and JFXX interchange rules, thumbnails, color conversion, and sample positioning (ITU-T T.871 / ISO/IEC 10918-5), from [ITU-T](https://www.itu.int/rec/T-REC-T.871/en).
- `ITU-T-T872-2012-printing.pdf` — JPEG print color encodings, APP14, and ICC use (ITU-T T.872 / ISO/IEC 10918-6), from [ITU-T](https://www.itu.int/rec/T-REC-T.872/en).
- `JPEG-Committee-JPEG-1-overview.html` and `JPEG-Committee-JPEG-1-workplan.html` — JPEG Committee descriptions and the authoritative map of the JPEG-1 standards family, from [jpeg.org](https://jpeg.org/jpeg/) and its [workplan](https://jpeg.org/jpeg/workplan.html).

ITU-T T.83 / ISO/IEC 10918-2 compliance testing is relevant but is not bundled because the official English document currently requires TIES or paid access.

## Metadata and color standards

- `CIPA-DC-008-2026-Exif-3.1.pdf` — Exif 3.1, downloaded through the [CIPA standards page](https://www.cipa.jp/e/std/std-sec.html).
- `CIPA-DC-007-2026-MPF.pdf` — Multi-Picture Format, downloaded through the [CIPA standards page](https://www.cipa.jp/e/std/std-sec.html).
- `CIPA-DC-010-2026-Exif-XMP.pdf` — Exif metadata mapping to XMP, downloaded through the [CIPA standards page](https://www.cipa.jp/e/std/std-sec.html).
- `Adobe-TIFF-6.0.pdf` — TIFF 6.0 structure used by Exif and MPF, from the [ITU mirror](https://www.itu.int/itudoc/itu-t/com16/tiff-fx/docs/tiff6.pdf).
- `Adobe-XMP-Part-3-2020.pdf` — XMP packet storage in JPEG and Extended XMP chunking, from Adobe's [XMP Toolkit repository](https://github.com/adobe/XMP-Toolkit-SDK/blob/main/docs/XMPSpecificationPart3.pdf).
- `ICC.1-2001-profile-v2.4.pdf` and `ICC.1-2022-profile-v4.4.pdf` — ICC v2 and v4 profile formats and JPEG embedding guidance, from the [International Color Consortium](https://www.color.org/specifications/).
- `IPTC-IIM-4.2.pdf` — IPTC Information Interchange Model carried in Photoshop resource blocks, from [IPTC](https://iptc.org/std/IIM/4.2/specification/IIMV4.2.pdf).
- `Adobe-Photoshop-file-formats-2019.html` — Photoshop image resource block layout used in APP13, from [Adobe](https://www.adobe.com/devnet-apps/photoshop/fileformatashtml/).
- `Adobe-Tech-Note-5116-DCT-filters.pdf` — Adobe APP14 color-transform marker and DCT interoperability behavior, preserved via the [Internet Archive](https://web.archive.org/web/20160320205929/http://partners.adobe.com/public/developer/en/ps/sdk/5116.DCT_Filter.pdf) because Adobe's original URL is no longer live.

## Informative implementation reference

- `libjpeg-turbo-libjpeg-api.txt` — The libjpeg/libjpeg-turbo behavior and advanced API reference, from the [official repository](https://github.com/libjpeg-turbo/libjpeg-turbo/blob/main/doc/libjpeg.txt). It is informative and does not override the standards above.
