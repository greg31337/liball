# csv

A small CSV library for .NET focused on forward-only parsing, configurable dialects, conservative writing, schema helpers, and streaming row processing.

## Public API

The supported API is the set of `public` interfaces, classes, enums, constructors, methods, fields, and enum members under `src/`. Public symbols have XML documentation that describes their behavior and important defaults. Parser state, character buffering, inference state, mutable reader/writer machinery, and algorithmic helpers are `internal` implementation details and may change without compatibility guarantees.

The API uses public fields for configuration and data because the source syntax policy does not permit properties. Each source class declares at most one constructor; named `CreateDefault` and `Create` factories provide alternative initialization paths. Create configured objects with a factory when one is available, then change the documented fields as needed.

## Scope

- Parse RFC 4180 CSV from `TextReader`, including quoted fields, doubled quotes, embedded delimiters, embedded newlines, empty fields, and final records without trailing newlines.
- Read common dialect variants with configurable delimiter, quote, escape, newline handling, blank-line handling, comment marker, trimming, null strings, and strict or relaxed error modes.
- Track row, field, line, column, character offset, raw row text, and diagnostics for malformed data and rejected rows.
- Read headers, generate column names, normalize names, and enforce duplicate, missing, and extra field policies.
- Convert fields to common built-in types and infer simple schemas from bounded samples.
- Write CSV with minimal, all, none-with-escape, and non-null quote modes, custom line terminators, null output, comments, headers, append mode, flush controls, and spreadsheet formula injection handling.
- Process streams through filters, transforms, validators, reject handlers, and progress callbacks without loading the whole input.
- Load bounded in-memory tables for sorting, grouping, aggregates, joins, concatenation, and union-by-name helpers.

## Interface Boundaries

`CsvProcessor` consumes the narrow `ICsvRowReader` and `ICsvRowWriter` contracts. `CsvReader` and `CsvWriter` implement those contracts, while tests and applications can supply in-memory, generated, decorated, or otherwise non-text row sources and destinations without reproducing parser or writer machinery.

Pipeline stages use `ICsvRowFilter`, `ICsvRowTransformer`, `ICsvRowValidator`, `ICsvRejectHandler`, and `ICsvProgressHandler`. Implement an interface directly for a reusable stage, or use the existing `CsvRowFilter`, `CsvRowTransformer`, `CsvRowValidator`, `CsvRejectHandler`, and `CsvProgressHandler` callback adapters for concise function-based configuration. The `CsvPipeline.UseFilter`, `UseTransformer`, `UseValidator`, `UseRejectHandler`, and `UseProgressHandler` methods both assign and activate a stage; passing null disables it.

## No External Library Policy

The implementation uses only built-in .NET runtime APIs. The library source does not depend on third-party CSV, parsing, conversion, or collection libraries.

The test project uses NUnit and the .NET test SDK so the repository can verify behavior with `dotnet test`; those packages are test-only dependencies.

## Examples

### Parse rows

```csharp
using System.IO;
using csv;

CsvReaderOptions options = CsvReaderOptions.CreateDefault();
options.HeaderOptions.HasHeader = true;

CsvReader reader = CsvReader.Create(new StringReader("Id,Name\n1,Ada\n2,Grace\n"), options);
reader.ReadHeader();

while (reader.Read())
{
    string id = reader.GetFieldByName("Id");
    string name = reader.GetFieldByName("Name");
}
```

### Write rows

```csharp
using System.IO;
using csv;

StringWriter output = new StringWriter();
CsvWriterOptions options = CsvWriterOptions.CreateDefault();
options.QuoteMode = CsvQuoteMode.Minimal;

CsvWriter writer = CsvWriter.Create(output, options);
writer.WriteRow(CsvRow.Create(new string[] { "Id", "Name" }, 1));
writer.WriteRow(CsvRow.Create(new string[] { "1", "Ada, Lovelace" }, 2));
writer.Close();

string csvText = output.ToString();
```

### Sniff dialect and header

```csharp
using csv;

string sample = "Name;Age\nAda;42\nGrace;43\n";
CsvSnifferOptions options = CsvSnifferOptions.CreateDefault();

CsvDialect dialect = CsvSniffer.Sniff(sample, options);
bool hasHeader = CsvSniffer.DetectHeader(sample, dialect, options);
```

### Transform rows

```csharp
using System.IO;
using csv;

CsvReader reader = CsvReader.Create(new StringReader("ada\n"), CsvReaderOptions.CreateDefault());
StringWriter output = new StringWriter();
CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());

CsvRowTransformer transformer = CsvRowTransformer.CreateDefault();
transformer.HasTransformCallback = true;
transformer.TransformCallback = UppercaseFirstField;

CsvPipeline pipeline = CsvPipeline.CreateDefault();
pipeline.UseTransformer(transformer);

CsvProcessor.Process(reader, writer, pipeline);

static CsvRow UppercaseFirstField(CsvRow row)
{
    CsvRow transformed = row.Clone();
    transformed.SetField(0, transformed.GetField(0).ToUpperInvariant());
    return transformed;
}
```

### Reject invalid rows

```csharp
using System.IO;
using csv;

CsvReader reader = CsvReader.Create(new StringReader("ok\nbad\n"), CsvReaderOptions.CreateDefault());
StringWriter output = new StringWriter();
CsvWriter writer = CsvWriter.Create(output, CsvWriterOptions.CreateDefault());

CsvRowValidator validator = CsvRowValidator.CreateDefault();
validator.Code = "BadRow";
validator.Message = "The row is not allowed.";
validator.HasIsValidCallback = true;
validator.IsValidCallback = IsAllowedRow;

CsvRejectHandler rejectHandler = CsvRejectHandler.CreateDefault();
rejectHandler.HasHandleCallback = true;
rejectHandler.HandleCallback = CountReject;

CsvPipeline pipeline = CsvPipeline.CreateDefault();
pipeline.UseValidator(validator);
pipeline.UseRejectHandler(rejectHandler);

CsvProcessor.Process(reader, writer, pipeline);

static bool IsAllowedRow(CsvRow row)
{
    if (row.GetField(0) == "bad")
    {
        return false;
    }

    return true;
}

static void CountReject(CsvRow row, CsvDiagnostic diagnostic)
{
    string code = diagnostic.Code;
}
```

### Open files

```csharp
using csv;

CsvWriter writer = CsvFile.OpenWriter("people.csv", CsvWriterOptions.CreateDefault());
writer.WriteRow(CsvRow.Create(new string[] { "Id", "Name" }, 1));
writer.WriteRow(CsvRow.Create(new string[] { "1", "Ada" }, 2));
writer.Close();

CsvReader reader = CsvFile.OpenReader("people.csv", CsvReaderOptions.CreateDefault());
while (reader.Read())
{
    string firstField = reader.GetField(0);
}
reader.Close();
```

## Limitations

- The reader is forward-only. Use `CsvProcessor.LoadTable` only for bounded data that is small enough to hold in memory.
- Sniffing and type inference are sample-based heuristics. Supply explicit options when the input format is known.
- Converters target common built-in scalar types and simple byte arrays; object mapping and reflection-based model binding are out of scope.
- Interface extension points are intentionally limited to synchronous, instance-method-only row processing contracts. Callback adapters remain available, while default interface methods, interface inheritance, generic interface methods, async APIs, attributes, LINQ, and generic API declarations remain out of scope.
- Malformed-input recovery is deterministic but conservative. Strict mode should be used when data quality failures must stop processing immediately.
