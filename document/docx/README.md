# docx

A small .NET library for reading, editing, validating, repairing, and writing DOCX packages.

The public API is organized around `DocxPackage` for OPC package access and `DocxDocument` for WordprocessingML document operations.

Focused interfaces are available where callers commonly need substitution:

- `IDocxTextContent` provides one text-extraction contract across documents, bodies, paragraphs, tables, headers, footers, notes, comments, and other text-bearing elements.
- `IDocxPackageInspector` is the read-only package view used by validation and diagnostic code.
- `IDocxValidator` allows applications to inject or replace the validation service in their own tests.

The concrete classes remain the construction and editing API, so existing code can continue to use `DocxPackage`, `DocxDocument`, and `DocxValidator` directly.

## Install

Reference the library project from your application:

```xml
<ProjectReference Include="path/to/src/docx.csproj" />
```

Use the `docx` namespace:

```csharp
using docx;
```

## Open A Document

Open a DOCX package from a file and create a document model:

```csharp
DocxPackage package = DocxPackage.OpenFile("input.docx");
DocxDocument document = DocxDocument.OpenPackage(package);

Console.WriteLine(document.Body.BlockCount());
```

For streams, use `DocxPackage.Open(stream)`:

```csharp
using FileStream stream = File.OpenRead("input.docx");

DocxPackage package = DocxPackage.Open(stream);
DocxDocument document = DocxDocument.OpenPackage(package);
```

## Extract Text

Extract the main document body as plain text:

```csharp
DocxDocument document = DocxDocument.OpenPackage(DocxPackage.OpenFile("input.docx"));

string text = document.GetText();
Console.WriteLine(text);
```

Customize separators for tables and paragraphs:

```csharp
DocxTextOptions options = new DocxTextOptions();
options.ParagraphSeparator = "\n\n";
options.TableCellSeparator = " | ";
options.TableRowSeparator = "\n";

string text = document.GetTextWithOptions(options);
```

## Create A Document

Create a minimal DOCX, add a paragraph, update the package part, and save:

```csharp
DocxDocument document = DocxDocument.Create();
DocxParagraph paragraph = new DocxParagraph("");
DocxRun run = new DocxRun("");

run.AddText(DocxText.Create("Hello from docx"));
paragraph.AddRun(run);
document.Body.AddBlock(DocxBlock.FromParagraph(paragraph));

DocxTemplate.UpdateMainDocumentBody(document);
document.Package.SaveFile("created.docx");
```

## Add A Table

Build a table in the document body:

```csharp
DocxDocument document = DocxDocument.Create();
DocxTable table = document.Body.AddTable(2, 2);

table.GetRow(0).GetCell(0).AddParagraph(CreateParagraph("Name"));
table.GetRow(0).GetCell(1).AddParagraph(CreateParagraph("Role"));
table.GetRow(1).GetCell(0).AddParagraph(CreateParagraph("Ada"));
table.GetRow(1).GetCell(1).AddParagraph(CreateParagraph("Engineer"));

DocxTemplate.UpdateMainDocumentBody(document);
document.Package.SaveFile("table.docx");
```

One helper for creating text paragraphs:

```csharp
static DocxParagraph CreateParagraph(string text)
{
    DocxRun run = new DocxRun("");
    run.AddText(DocxText.Create(text));

    DocxParagraph paragraph = new DocxParagraph("");
    paragraph.AddRun(run);
    return paragraph;
}
```

## Add An Image

Add image bytes to the package, create a relationship from the main document, then place an inline drawing in a paragraph:

```csharp
DocxDocument document = DocxDocument.Create();
byte[] imageBytes = File.ReadAllBytes("logo.png");

DocxImage image = document.AddImageToMainDocument(imageBytes, DocxConstants.ContentTypePng);

DocxParagraph paragraph = new DocxParagraph("");
paragraph.AddInlineImage(image, 914400, 914400);
document.Body.AddBlock(DocxBlock.FromParagraph(paragraph));

DocxTemplate.UpdateMainDocumentBody(document);
document.Package.SaveFile("with-image.docx");
```

The width and height values are EMUs. `914400` EMUs is one inch.

## Fill A Template

Replace placeholders in paragraphs, tables, and supported content controls:

```csharp
DocxDocument document = DocxDocument.OpenPackage(DocxPackage.OpenFile("template.docx"));
Dictionary<string, string> values = new Dictionary<string, string>();

values.Add("Name", "Ada Lovelace");
values.Add("City", "London");

int replacementCount = document.ReplacePlaceholders(values);

document.Package.SaveFile("filled.docx");
Console.WriteLine(replacementCount);
```

Use custom delimiters when the template does not use `{{Name}}` syntax:

```csharp
document.ReplacePlaceholdersWithDelimiters(values, "[[", "]]");
```

## Validate A Package

Validate an unopened package stream for OPC-level issues:

```csharp
DocxValidator validator = new DocxValidator();

using FileStream stream = File.OpenRead("input.docx");
DocxValidationReport report = validator.Validate(stream);

if (report.HasErrors())
{
    Console.WriteLine(report.Count());
}
```

Validate a loaded package for content types, relationships, XML well-formedness, references, and required WordprocessingML parts:

```csharp
DocxPackage package = DocxPackage.OpenFile("input.docx");
DocxValidationReport report = new DocxValidator().ValidatePackage(package);
```

## Repair A Package

Apply safe repairs for common recoverable package issues:

```csharp
DocxPackage package = DocxPackage.OpenFile("input.docx");
DocxValidator validator = new DocxValidator();

validator.RepairMissingContentTypes(package);
validator.RepairMissingRelationships(package);
validator.RepairOrphanedRelationships(package);
validator.RepairEmptyBody(package);

package.SaveFile("repaired.docx");
```

Repair methods return `DocxValidationReport` instances describing what changed:

```csharp
DocxValidationReport report = validator.RepairMissingContentTypes(package);
Console.WriteLine(report.Count());
```

## Merge Documents

Append one document to another:

```csharp
DocxDocument target = DocxDocument.OpenPackage(DocxPackage.OpenFile("target.docx"));
DocxDocument source = DocxDocument.OpenPackage(DocxPackage.OpenFile("source.docx"));

int appendedBlocks = target.Append(source);

target.Package.SaveFile("merged.docx");
Console.WriteLine(appendedBlocks);
```

Insert source blocks after a bookmark in the target document:

```csharp
DocxDocument target = DocxDocument.OpenPackage(DocxPackage.OpenFile("target.docx"));
DocxDocument source = DocxDocument.OpenPackage(DocxPackage.OpenFile("insert.docx"));

int insertedBlocks = target.InsertAtBookmark("InsertHere", source);

target.Package.SaveFile("merged-at-bookmark.docx");
Console.WriteLine(insertedBlocks);
```
