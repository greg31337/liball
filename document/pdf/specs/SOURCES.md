# Specification sources

Some local reference files have been replaced by links. See [Reference downloads](../REFERENCE-DOWNLOADS.md) for the current download-only files and their original paths. The inventory below also records historical acquisitions.

Retrieved on 2026-08-24. The files in this directory are local implementation
references for the features listed in `todo.md`.

| Local file | Authoritative source | Relevant area |
| --- | --- | --- |
| `Adobe-PDF-Reference-1.4.pdf` | https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/pdfreference1.4.pdf | The library's initially targeted PDF 1.4 syntax and feature set. |
| `ISO-32000-1-2008-PDF-1.7.pdf` | https://opensource.adobe.com/dc-acrobat-sdk-docs/pdfstandards/PDF32000_2008.pdf | ISO PDF 1.7 file structure, graphics, fonts, images, navigation, forms, security, signatures, metadata, and tagged PDF. |
| `ISO-32000-2-errata.html` | https://pdf-issues.pdfa.org/32000-2-2020/ | Publicly resolved PDF 2.0 corrections, many of which clarify concepts shared with PDF 1.7. |
| `RFC-1950-zlib.txt` | https://www.rfc-editor.org/rfc/rfc1950.txt | ZLIB stream format used with Flate compression. |
| `RFC-1951-deflate.txt` | https://www.rfc-editor.org/rfc/rfc1951.txt | DEFLATE compression format. |
| `ITU-T-T81-JPEG.pdf` | https://www.w3.org/Graphics/JPEG/itu-t81.pdf | JPEG coding used by PDF DCT image streams. |
| `JFIF-1.02.pdf` | https://www.w3.org/Graphics/JPEG/jfif3.pdf | JPEG File Interchange Format metadata and component conventions. |
| `W3C-PNG-Third-Edition.html` | https://www.w3.org/TR/2025/REC-png-3-20250624/ | PNG parsing, decoding, alpha, color, and metadata. |
| `opentype/*.html` | https://learn.microsoft.com/en-us/typography/opentype/spec/ | OpenType 1.9.1 container and the tables needed for loading, metrics, mapping, and TrueType subsetting. |
| `Unicode-UAX-9-Bidirectional-Algorithm.html` | https://www.unicode.org/reports/tr9/ | Bidirectional and right-to-left text ordering. |
| `Unicode-UAX-14-Line-Breaking.html` | https://www.unicode.org/reports/tr14/ | Unicode line-break opportunities for wrapping. |
| `XMP-Specification-Part-1.pdf` | https://raw.githubusercontent.com/adobe/xmp-docs/master/XMPSpecifications/XMPSpecificationPart1.pdf | XMP data model, serialization, and core properties. |
| `XMP-Specification-Part-2.pdf` | https://raw.githubusercontent.com/adobe/xmp-docs/master/XMPSpecifications/XMPSpecificationPart2.pdf | Standard XMP property schemas. |
| `XMP-Specification-Part-3.pdf` | https://raw.githubusercontent.com/adobe/xmp-docs/master/XMPSpecifications/XMPSpecificationPart3.pdf | XMP storage in files, including PDF. |
| `ETSI-EN-319-142-1-PAdES.pdf` | https://www.etsi.org/deliver/etsi_EN/319100_319199/31914201/01.01.01_60/en_31914201v010101p.pdf | PAdES signature building blocks and baseline signatures. |
| `Factur-X-1.09.2-resource.html` | https://fnfe-mpe.org/factur-x/factur-x_en/ | Current Factur-X/ZUGFeRD profiles, packaging description, and official specification access. |
| `PDF-A-in-a-Nutshell-2.0.pdf` | https://pdfa.org/wp-content/uploads/2013/05/PDFA_in_a_Nutshell_211.pdf | PDF/A requirements and implementation background. |
| `Tagged-PDF-Best-Practice-Guide.pdf` | https://pdfa.org/wp-content/uploads/2023/07/Tagged-PDF-Best-Practice-Guide.pdf | Tagged PDF and PDF/UA-1 implementation guidance. |
| `Matterhorn-Protocol-1.1.pdf` | https://pdfa.org/wp-content/uploads/2021/04/Matterhorn-Protocol-1-1.pdf | Testable PDF/UA-1 failure conditions. |
| `Well-Tagged-PDF-1.0.pdf` | https://pdfa.org/wp-content/uploads/2024/02/Well-Tagged-PDF-WTPDF-1.0.pdf | Tagged PDF requirements for accessibility and reuse in PDF 2.0. |
| `W3C-WCAG-2.2.html` | https://www.w3.org/TR/WCAG22/ | General accessible-content requirements relevant to generated documents. |
| `W3C-SVG-2.html` | https://www.w3.org/TR/SVG2/ | The planned SVG import subset. |

The current ISO 32000-2:2020 bundle and the ISO 14289 PDF/UA bundle are
available at no charge only after per-user registration. They were not copied
without that registration. Obtain them from the official access pages:

- https://pdfa.org/resource/iso-32000-2/
- https://pdfa.org/sponsored-standards/

The current Factur-X/ZUGFeRD complete package also requires an email address to
send a download link. Its official resource page was saved, but the gated
package was not requested on the user's behalf.

Several PDF Association files rejected direct retrieval. The local copies of
the tagged-PDF guide, WTPDF, and PDF/A guide were therefore retrieved through
the Internet Archive using the authoritative URLs above. The Matterhorn copy
was retrieved from a public mirror after the authoritative host rejected the
download; its SHA-256 digest is
`b8bf0b00fd789ef8d8548d2eeb7e83194db394d9f1be741f2d0ef1d62bff50c6`.
