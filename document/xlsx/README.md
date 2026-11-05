# xlsx

`xlsx` is a small C# library for reading, writing, editing, streaming, validating, repairing, comparing, and sanitizing XLSX packages.

The public API is result-oriented: most operations return an object with `IsValid`, `Validation.Diagnostics`, and operation-specific data such as bytes, XML, parsed parts, or counters.

Only symbols declared `public` are supported API. Public types live under `src/Api` and are separated into contracts, data, static constants, enums, exceptions, and interface-backed implementation classes. Field-bearing models expose behavior through companion `*Operations` implementations; package composition helpers, XML traversal routines, formula evaluation state, and similar mechanics remain `internal`. Public symbols include XML documentation in the generated assembly documentation file.

## Create

Create a minimal workbook package and write it to bytes:

```csharp
using xlsx;

XlsxWorkbookOperations workbookOperations = new XlsxWorkbookOperations();
XlsxWorkbook workbook = workbookOperations.Create();
workbook.WorkbookPart.Sheets[0].Name = "Sales";

XlsxWorkbookSaveResult save = workbookOperations.SaveBytes(workbook);
if (!save.IsValid)
{
    // Inspect save.Validation.Diagnostics.
    return;
}

byte[] bytes = save.Bytes;
```

Use `SaveFile(path)` when you want the library to write directly to disk.

## Read

Open a workbook from bytes and inspect workbook metadata:

```csharp
using xlsx;

XlsxWorkbookOpenResult open = new XlsxWorkbookOperations().OpenBytes(bytes);
if (!open.IsValid)
{
    // Inspect open.Validation.Diagnostics.
    return;
}

XlsxWorkbook workbook = open.Workbook;
string firstSheetName = workbook.WorkbookPart.Sheets[0].Name;
```

Use `XlsxReaderOptions` to select sheets, preserve unknown package parts, collect statistics, or change safety limits:

```csharp
XlsxReaderOptions options = new XlsxReaderOptions();
options.SelectedSheets.Add("Sales");
options.CollectStatistics = true;

XlsxWorkbookOpenResult open = new XlsxWorkbookOperations().OpenBytesWithOptions(bytes, options);
```

## Edit

Edit worksheet cells through `XlsxWorksheetPart`, then write the worksheet part into the workbook package path you want to replace:

```csharp
using xlsx;

XlsxWorkbookOperations workbookOperations = new XlsxWorkbookOperations();
XlsxWorksheetPartOperations worksheetOperations = new XlsxWorksheetPartOperations();
XlsxWorkbook workbook = workbookOperations.Create();
workbook.WorkbookPart.Sheets[0].Name = "Sales";

XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
worksheet.PartPath = XlsxPackagePaths.Worksheet1;
worksheetOperations.SetTextValue(worksheet, "A1", "Region");
worksheetOperations.SetTextValue(worksheet, "B1", "Amount");
worksheetOperations.SetTextValue(worksheet, "A2", "North");
worksheetOperations.SetNumberValue(worksheet, "B2", 1250);

XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
if (!worksheetWrite.IsValid)
{
    return;
}

new XlsxUnknownPartStoreOperations().AddPart(workbook.UnknownParts, XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet, worksheetWrite.Bytes);

XlsxWorkbookSaveResult save = workbookOperations.SaveBytes(workbook);
```

When opening an existing workbook, use `XlsxReaderOptions.SelectedSheets` to load only the sheets you intend to edit.

## Stream

Write large sheets row by row without building a full worksheet object:

```csharp
using System.Collections.Generic;
using System.IO;
using xlsx;

MemoryStream sheetStream = new MemoryStream();
XlsxStreamingSheetWriteResult begin = new XlsxStreamingSheetWriter().BeginStream(sheetStream);
XlsxStreamingSheetWriteSession session = begin.Session;
XlsxCellValueOperations cellValues = new XlsxCellValueOperations();

session.WriteRow(new List<XlsxCellValue>
{
    cellValues.CreateText("Region"),
    cellValues.CreateText("Amount")
});
session.WriteRow(new List<XlsxCellValue>
{
    cellValues.CreateText("North"),
    cellValues.CreateNumber(1250)
});

XlsxStreamingSheetWriteResult close = session.Close();
byte[] worksheetXml = sheetStream.ToArray();
```

Read package metadata without loading large sheet entries:

```csharp
XlsxStreamingWorkbookReadResult streamRead = new XlsxStreamingWorkbookReader().ReadBytes(bytes);
int packageEntryCount = streamRead.WorkbookInfo.PackageEntryCount;
```

## Style

Build a style once, add it to a workbook, and assign its style id to cells:

```csharp
using xlsx;

XlsxWorkbook workbook = new XlsxWorkbookOperations().Create();
XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
XlsxWorksheetPartOperations worksheetOperations = new XlsxWorksheetPartOperations();
worksheet.PartPath = XlsxPackagePaths.Worksheet1;

XlsxStyleBuilder headerStyle = new XlsxStyleBuilder();
XlsxStyleBuilderOperations styleOperations = new XlsxStyleBuilderOperations();
styleOperations.SetBold(headerStyle, true);
styleOperations.SetFillColor(headerStyle, "FFCC00");
styleOperations.SetHorizontalAlignment(headerStyle, "center");

int styleId = styleOperations.AddToWorkbook(headerStyle, workbook);

worksheetOperations.SetTextValue(worksheet, "A1", "Region");
worksheetOperations.SetTextValue(worksheet, "B1", "Amount");
worksheetOperations.GetCell(worksheet, "A1").Style = (uint)styleId;
worksheetOperations.GetCell(worksheet, "A1").HasStyle = true;
worksheetOperations.GetCell(worksheet, "B1").Style = (uint)styleId;
worksheetOperations.GetCell(worksheet, "B1").HasStyle = true;
```

## Table

Create and manage table metadata on a worksheet:

```csharp
using xlsx;

XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
XlsxWorksheetPartOperations worksheetOperations = new XlsxWorksheetPartOperations();
worksheetOperations.SetTextValue(worksheet, "A1", "Region");
worksheetOperations.SetTextValue(worksheet, "B1", "Amount");
worksheetOperations.SetTextValue(worksheet, "A2", "North");
worksheetOperations.SetNumberValue(worksheet, "B2", 1250);

XlsxWorksheetTableResult table = worksheetOperations.AddTable(worksheet, "A1:B2", "SalesTable");
worksheetOperations.SetTableStyleWithStripeOptions(worksheet, "SalesTable", "TableStyleMedium9", false, true);
worksheetOperations.SetTableTotalsRow(worksheet, "SalesTable", true);
worksheetOperations.SetTableColumnTotalsRowFunction(worksheet, "SalesTable", "Amount", "sum");
```

Use `XlsxTablePartWriter` when you need the standalone table XML part.

## Formula

Set a formula with a cached value, calculate it, and save the updated cache:

```csharp
using xlsx;

XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
XlsxWorksheetPartOperations worksheetOperations = new XlsxWorksheetPartOperations();
worksheetOperations.SetNumberValue(worksheet, "A1", 10);
worksheetOperations.SetNumberValue(worksheet, "A2", 15);
worksheetOperations.SetFormulaCachedValue(worksheet, "A3", "SUM(A1:A2)", "", "");

XlsxFormulaCalculationResult calculation = worksheetOperations.CalculateCellFormula(worksheet, "A3");
string cachedValue = worksheetOperations.GetCell(worksheet, "A3").Value; // "25"
```

The formula parser and writer can be used directly:

```csharp
XlsxFormulaParseResult parse = new XlsxFormulaParser().Parse("SUM(A1:A2)");
XlsxFormulaWriteResult write = new XlsxFormulaWriter().WriteString(parse);
```

## Sanitize

Remove active content, external links, comments, hidden sheets, personal metadata, custom XML, OLE objects, and unknown package parts before saving:

```csharp
using xlsx;

XlsxReaderOptions options = new XlsxReaderOptions();
options.AllowMacroEnabledPackages = true;

XlsxWorkbookOperations workbookOperations = new XlsxWorkbookOperations();
XlsxWorkbookOpenResult open = workbookOperations.OpenBytesWithOptions(bytes, options);
XlsxWorkbookSanitizeResult sanitize = workbookOperations.Sanitize(open.Workbook);
XlsxWorkbookSaveResult cleanSave = workbookOperations.SaveBytes(open.Workbook);
```

Disable individual sanitizer options when you need to retain a category:

```csharp
XlsxWorkbookSanitizeOptions sanitizeOptions = new XlsxWorkbookSanitizeOptions();
sanitizeOptions.RemoveUnknownParts = false;

XlsxWorkbookSanitizeResult sanitize = new XlsxWorkbookOperations().SanitizeWithOptions(workbook, sanitizeOptions);
```

## Validate And Repair

Validate package structure and workbook model limits:

```csharp
XlsxValidationResult validation = new XlsxWorkbookValidator().ValidateBytes(bytes);
```

Repair common workbook and worksheet issues:

```csharp
XlsxWorkbookRepairResult repair = new XlsxWorkbookRepairer().RepairBytes(bytes);
```

Validation and repair also have small interface contracts for dependency injection and test doubles. Existing convenience methods still use the built-in implementations:

```csharp
IXlsxWorkbookValidator validator = new XlsxWorkbookValidator();
XlsxValidationResult validation = new XlsxWorkbookOperations().ValidateUsing(workbook, validator);

IXlsxWorkbookRepairer repairer = new XlsxWorkbookRepairer();
XlsxWorkbookRepairResult repair = new XlsxWorkbookOperations().RepairUsing(workbook, repairer);
```
