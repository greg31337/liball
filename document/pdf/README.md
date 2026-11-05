# pdf

A self-contained .NET PDF generation library with low-level canvas drawing,
automatic page layout, navigation, forms, encryption, compliance preparation,
and helpers for reports, invoices, papers, and common document templates. The
project also includes a console entry point that generates the sample report.

```bash
dotnet run --project src -- sample-report.pdf
```

The generated document includes a title, two report sections, an embedded image,
a table split across pages, headers, footers, and page numbering.

## Source policy

Production code under `src/` follows the restricted language subset documented
in `syntax.md`. Only `public`, `internal`, `static`, and `const` modifiers are
permitted. A `public` type or member is part of the supported API; all
implementation details must be explicitly `internal`.

Every public type, constructor, method, field, enum, and enum member must have
XML documentation. Documentation may use only `summary`, `remarks`, `param`,
`returns`, `exception`, `example`, `see`, `seealso`, `para`, and `c` elements.
Tests enforce these visibility and documentation requirements.

Public types are organized by role under `src/Api`: contracts, implementation
classes, data models, enums, exceptions, and static data. Public behavior is
exposed through interfaces and instance methods. For example,
`PdfPageSizeFactory` creates standard or oriented page dimensions, while
`PdfPageSize` contains only the corresponding numeric constants.

## Numeric limits

Numeric inputs are validated before size and range calculations are performed.
Raw RGB images must contain exactly three bytes per pixel, and dimensions whose
packed data cannot fit in a .NET array are rejected. Signature placeholder sizes
must be positive and cannot exceed `int.MaxValue / 2`, because every placeholder
byte is serialized as two hexadecimal characters.

## Custom layout elements

All built-in layout blocks implement `IPdfLayoutElement`. The interface exposes
measurement, fixed-area drawing, and paginated flow, so application-specific
elements and test doubles can participate in layout without inheriting from a
library class or being added to a central type switch.

Custom elements can be passed directly to `PdfLayoutDocument.AddItem`,
`PdfPageCursor.DrawItem`, and `PdfPageTemplate`. Container APIs expose
`AddElement` for stacks, rows, columns, grids, lists, and table rows. Existing
type-specific methods remain available as convenience methods.

For ordinary elements, implement `Flow` by returning
`cursor.DrawItem(this)`. Elements with special pagination behavior can instead
coordinate the cursor directly, as the built-in table, page-break, and
ensure-space elements do. Construct a `PdfLayoutItem` around any layout element
when keep-together behavior is needed.
