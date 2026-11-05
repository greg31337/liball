# PPTX implementation references

Large reference files are available through [Reference downloads](../REFERENCE-DOWNLOADS.md). The source inventory below also records historical acquisitions.

Retrieved 2026-08-25 from the authoritative publishers. The directory was
empty before this review.

## ECMA-376 / ISO/IEC 29500

- `ecma-376/part-1/`: ECMA-376 Part 1, *Fundamentals and Markup Language
  Reference*, 5th edition (2016), plus the publisher's Strict XSD, RELAX NG,
  DrawingML geometry, style, and border resources.
- `ecma-376/part-2/`: ECMA-376 Part 2, *Open Packaging Conventions*, 5th
  edition (2021), plus the publisher's OPC XSD and RELAX NG resources.
- `ecma-376/part-3/`: ECMA-376 Part 3, *Markup Compatibility and
  Extensibility*, 5th edition (2015).
- `ecma-376/part-4/`: ECMA-376 Part 4, *Transitional Migration Features*,
  5th edition (2016), plus the publisher's Transitional XSD and RELAX NG
  resources.

Source: <https://ecma-international.org/publications-and-standards/standards/ecma-376/>

## Microsoft implementation and extension specifications

- `microsoft/MS-PPTX.pdf`: PowerPoint extensions to PresentationML.
- `microsoft/MS-ODRAWXML.pdf`: Office DrawingML extensions, including chart,
  picture, diagram, table, and theme extensions.
- `microsoft/MS-OI29500.pdf`: Microsoft Office implementation notes for
  ISO/IEC 29500.
- `microsoft/MS-OEXTXML.pdf`: shared Office extension-list structures.
- `microsoft/MS-OFFMACRO.pdf`: macro-enabled OOXML package differences.
- `microsoft/MS-OVBA.pdf`: the VBA project binary format stored in macro-enabled
  presentations.
- `microsoft/MS-OFORMS.pdf`: Office Forms binary persistence used by embedded
  controls.
- `microsoft/MS-CFB.pdf`: Compound File Binary storage used by VBA, OLE, and
  some embedded objects.

Sources: Microsoft Open Specifications, using the current published PDFs at
<https://learn.microsoft.com/en-us/openspecs/office_standards/ms-offstandlp/d5784a8b-7070-466b-befa-b7bf3724c6f0>,
<https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-offfflp/6ae2fd93-51fc-4e75-a54a-1b175c627b51>,
and <https://learn.microsoft.com/en-us/openspecs/windows_protocols/ms-cfb/53989ce4-7b05-4f8d-829b-d08d6148375b>.

## Supporting standards

- `supporting/PKWARE-APPNOTE.txt`: PKWARE ZIP Application Note, relevant to
  the physical ZIP package representation used by OPC.
- `supporting/RFC3986.txt`: RFC 3986 URI generic syntax and relative-reference
  resolution, used by OPC part names and relationship targets.
- `supporting/XML-1.0.html`: W3C XML 1.0 Fifth Edition.
- `supporting/XML-Namespaces-1.0.html`: W3C Namespaces in XML 1.0 Third
  Edition.

Sources: <https://pkware.cachefly.net/webdocs/casestudies/APPNOTE.TXT>,
<https://www.rfc-editor.org/rfc/rfc3986.txt>, <https://www.w3.org/TR/xml/>,
and <https://www.w3.org/TR/xml-names/>.

`SHA256SUMS` records hashes for the primary documents. The ECMA-provided
schema/resource ZIP files are retained in their publisher-supplied form.
