using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvPublicCoverageCompletionTests
{
    [Test]
    public void FileService_RemainingPublicWriterAndCombinationMethodsProduceExpectedFiles()
    {
        string asciiPath = CreateTemporaryPath();
        string appendPath = CreateTemporaryPath();
        string firstPath = CreateTemporaryPath();
        string secondPath = CreateTemporaryPath();
        string concatenatedPath = CreateTemporaryPath();
        string unionPath = CreateTemporaryPath();
        try
        {
            ICsvFileService files = new CsvFileService();
            CsvWriter asciiWriter = files.OpenWriterAscii(asciiPath, null);
            asciiWriter.WriteField("plain");
            asciiWriter.NextRecord();
            asciiWriter.Close();

            File.WriteAllText(appendPath, "one\r\n", new UTF8Encoding(false));
            CsvWriter appendWriter = files.OpenAppendWriter(appendPath, null);
            appendWriter.WriteField("two");
            appendWriter.NextRecord();
            appendWriter.Close();
            CsvWriter appendAsciiWriter = files.OpenAppendWriterAscii(appendPath, null);
            appendAsciiWriter.WriteField("three");
            appendAsciiWriter.NextRecord();
            appendAsciiWriter.Close();

            File.WriteAllText(firstPath, "Id,Name\n1,Ada\n", new UTF8Encoding(false));
            File.WriteAllText(secondPath, "Name,Id\nGrace,2\n", new UTF8Encoding(false));
            CsvReaderOptions readerOptions = new CsvReaderOptions();
            readerOptions.HeaderOptions.HasHeader = true;
            CsvWriterOptions writerOptions = new CsvWriterOptions();
            writerOptions.WriteHeader = true;
            writerOptions.LineTerminator = "\n";

            long concatenated = files.ConcatenateFilesByPosition(
                new string[] { firstPath, secondPath }, concatenatedPath, readerOptions, writerOptions);
            long unioned = files.UnionFilesByName(
                new string[] { firstPath, secondPath }, unionPath, readerOptions, writerOptions);

            Assert.That(File.ReadAllText(asciiPath, Encoding.ASCII), Is.EqualTo("plain\r\n"));
            Assert.That(File.ReadAllText(appendPath, new UTF8Encoding(false)), Is.EqualTo("one\r\ntwo\r\nthree\r\n"));
            Assert.That(concatenated, Is.EqualTo(2));
            Assert.That(File.ReadAllText(concatenatedPath, new UTF8Encoding(false)), Is.EqualTo("Id,Name\n1,Ada\nGrace,2\n"));
            Assert.That(unioned, Is.EqualTo(2));
            Assert.That(File.ReadAllText(unionPath, new UTF8Encoding(false)), Is.EqualTo("Id,Name\n1,Ada\n2,Grace\n"));
        }
        finally
        {
            DeleteTemporaryPath(asciiPath);
            DeleteTemporaryPath(appendPath);
            DeleteTemporaryPath(firstPath);
            DeleteTemporaryPath(secondPath);
            DeleteTemporaryPath(concatenatedPath);
            DeleteTemporaryPath(unionPath);
        }
    }

    [Test]
    public void FileService_PublicReadMethodsPropagateMissingFileFailures()
    {
        string missingPath = CreateTemporaryPath();
        ICsvFileService files = new CsvFileService();

        FileNotFoundException exception = Assert.Throws<FileNotFoundException>(() => files.OpenReader(missingPath, null));

        Assert.That(exception.FileName, Is.EqualTo(missingPath));
    }

    [Test]
    public void ProcessorAndCallbackAdapters_ExecuteEveryPublicImplementationMethod()
    {
        CsvReader reader = new CsvReader(new StringReader("a,1\nb,2\n"), new CsvReaderOptions());
        StringWriter output = new StringWriter();
        CsvWriter writer = new CsvWriter(output, new CsvWriterOptions());
        ICsvProcessor processor = new CsvProcessorService();

        long count = processor.ProcessDefault(reader, writer);

        CsvRow row = new CsvRow();
        row.Fields = new string?[] { "bad", "-1" };
        ICsvRowFilter filter = new CsvRowFilter(candidate => candidate.Fields[0] != "bad");
        ICsvRowTransformer transformer = new CsvRowTransformer(candidate => null);
        ICsvRowValidator validator = new CsvRowValidator(
            "BadRow",
            "Bad row.",
            candidate => candidate.Fields[0] != "bad",
            candidate => CreateDiagnostic("BadRow", candidate, 0));
        ICsvFieldValidator fieldValidator = new CsvFieldValidator(
            "BadField",
            "Bad field.",
            (candidate, index, value) => value != "-1",
            (candidate, index, value) => CreateDiagnostic("BadField", candidate, index + 1));
        int progressCount = 0;
        int rejectCount = 0;
        ICsvProgressHandler progress = new CsvProgressHandler((pipeline, candidate) => progressCount = progressCount + 1);
        ICsvRejectHandler reject = new CsvRejectHandler((candidate, diagnostic) => rejectCount = rejectCount + 1);

        Assert.That(count, Is.EqualTo(2));
        Assert.That(output.ToString(), Is.EqualTo("a,1\r\nb,2\r\n"));
        Assert.That(filter.Include(row), Is.False);
        Assert.That(transformer.Transform(row), Is.Null);
        Assert.That(validator.IsValid(row), Is.False);
        Assert.That(validator.CreateDiagnostic(row).Code, Is.EqualTo("BadRow"));
        Assert.That(fieldValidator.IsValid(row, 1, "-1"), Is.False);
        Assert.That(fieldValidator.CreateDiagnostic(row, 1, "-1").Column, Is.EqualTo(2));

        progress.Report(new CsvPipeline(), row);
        reject.Handle(row, CreateDiagnostic("Rejected", row, 0));
        Assert.That(progressCount, Is.EqualTo(1));
        Assert.That(rejectCount, Is.EqualTo(1));
    }

    [Test]
    public void Reader_PublicDefaultsBoundsAndLifecycleBranchesRemainSafe()
    {
        CsvReader nullOptionsReader = new CsvReader(new StringReader("a,b"), null);
        Assert.That(nullOptionsReader.Read(), Is.True);
        Assert.That(nullOptionsReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b" }));

        CsvReaderOptions nullDialectOptions = new CsvReaderOptions();
        nullDialectOptions.Dialect = null!;
        CsvReader nullDialectReader = new CsvReader(new StringReader("c,d"), nullDialectOptions);
        Assert.That(nullDialectReader.Read(), Is.True);
        Assert.That(nullDialectReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "c", "d" }));

        nullDialectReader.SetField(-1, "ignored");
        nullDialectReader.SetField(10, "ignored");
        nullDialectReader.SetFieldByName("Missing", "ignored");
        Assert.That(nullDialectReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "c", "d" }));
        Assert.That(nullDialectReader.GetField(-1), Is.EqualTo(""));
        Assert.That(nullDialectReader.GetField(10), Is.EqualTo(""));

        CsvReader emptyHeaderReader = new CsvReader(new StringReader(""), new CsvReaderOptions());
        Assert.That(emptyHeaderReader.ReadHeader(), Is.False);
        Assert.DoesNotThrow(() =>
        {
            emptyHeaderReader.Close();
            emptyHeaderReader.Close();
        });

        CsvReaderOptions commentOptions = new CsvReaderOptions();
        commentOptions.AllowComments = true;
        commentOptions.Dialect.HasComment = true;
        commentOptions.Dialect.Comment = '#';
        CsvReader commentOnlyReader = new CsvReader(new StringReader("# comment without newline"), commentOptions);
        Assert.That(commentOnlyReader.Read(), Is.False);

        CsvReaderOptions escapeOptions = new CsvReaderOptions();
        escapeOptions.Dialect.UseDoubleQuoteEscaping = false;
        escapeOptions.Dialect.Escape = '\\';
        CsvReader escapeReader = new CsvReader(new StringReader("\"a\\xb\""), escapeOptions);
        Assert.That(escapeReader.Read(), Is.True);
        Assert.That(escapeReader.GetField(0), Is.EqualTo("a\\xb"));

        CsvReaderOptions rejectOptions = new CsvReaderOptions();
        rejectOptions.ErrorMode = CsvErrorMode.SkipBadRow;
        rejectOptions.HasRejectHandler = true;
        rejectOptions.RejectHandler = null;
        CsvReader rejectReader = new CsvReader(new StringReader("\"bad\"x\nok"), rejectOptions);
        Assert.That(rejectReader.Read(), Is.True);
        Assert.That(rejectReader.GetField(0), Is.EqualTo("ok"));
        Assert.That(rejectReader.GetRejects(), Is.Empty);
    }

    [Test]
    public void Writer_PublicDefaultDialectCommentsAndLifecycleBranchesProduceStableOutput()
    {
        StringWriter nullOptionsOutput = new StringWriter();
        CsvWriter nullOptionsWriter = new CsvWriter(nullOptionsOutput, null);
        nullOptionsWriter.WriteField("default");
        nullOptionsWriter.NextRecord();

        CsvWriterOptions nullDialectOptions = new CsvWriterOptions();
        nullDialectOptions.Dialect = null!;
        StringWriter nullDialectOutput = new StringWriter();
        CsvWriter nullDialectWriter = new CsvWriter(nullDialectOutput, nullDialectOptions);
        nullDialectWriter.WriteField("a,b");
        nullDialectWriter.NextRecord();

        CsvWriterOptions fallbackOptions = new CsvWriterOptions();
        fallbackOptions.LineTerminator = "";
        fallbackOptions.Dialect.LineTerminator = "";
        StringWriter fallbackOutput = new StringWriter();
        CsvWriter fallbackWriter = new CsvWriter(fallbackOutput, fallbackOptions);
        fallbackWriter.WriteComment(" leading");
        fallbackWriter.WriteComment("");
        fallbackOptions.Dialect.HasComment = true;
        fallbackWriter.WriteField("");
        fallbackWriter.NextRecord();
        Assert.DoesNotThrow(() =>
        {
            fallbackWriter.Close();
            fallbackWriter.Close();
        });

        Assert.That(nullOptionsOutput.ToString(), Is.EqualTo("default\r\n"));
        Assert.That(nullDialectOutput.ToString(), Is.EqualTo("\"a,b\"\r\n"));
        Assert.That(fallbackOutput.ToString(), Is.EqualTo("# leading\r\n#\r\n\r\n"));
    }

    [Test]
    public void Reader_PublicFileSurfaceDetectsEverySupportedUnicodePreamble()
    {
        string utf16BigEndianPath = CreateTemporaryPath();
        string utf32LittleEndianPath = CreateTemporaryPath();
        string utf32BigEndianPath = CreateTemporaryPath();
        try
        {
            ICsvFileService files = new CsvFileService();
            AssertPreambleRoundTrip(files, utf16BigEndianPath, new UnicodeEncoding(true, true), 2, "東京");
            AssertPreambleRoundTrip(files, utf32LittleEndianPath, new UTF32Encoding(false, true), 4, "alpha");
            AssertPreambleRoundTrip(files, utf32BigEndianPath, new UTF32Encoding(true, true), 4, "omega");

            byte[] bytes = new UTF8Encoding(false).GetBytes("xa,b");
            MemoryStream stream = new MemoryStream(bytes);
            stream.Position = 1;
            StreamReader textReader = new StreamReader(stream, new UTF8Encoding(false), true);
            CsvReader offsetReader = new CsvReader(textReader, new CsvReaderOptions());
            Assert.That(offsetReader.Read(), Is.True);
            Assert.That(offsetReader.GetCurrentRow().Fields, Is.EqualTo(new string[] { "a", "b" }));
            Assert.That(offsetReader.GetCurrentRow().ByteOffset, Is.EqualTo(-1));
        }
        finally
        {
            DeleteTemporaryPath(utf16BigEndianPath);
            DeleteTemporaryPath(utf32LittleEndianPath);
            DeleteTemporaryPath(utf32BigEndianPath);
        }
    }

    [Test]
    public void ConverterAndSniffer_PublicOptionsCoverNullAndSchemaMetadataFallbacks()
    {
        ICsvConverter converter = new CsvConverterService();
        CsvConversionOptions conversionOptions = new CsvConversionOptions();
        conversionOptions.UseDefaultForNull = true;
        conversionOptions.DefaultValue = "9";
        Assert.That(converter.ToInt32(null, conversionOptions), Is.EqualTo(9));

        CsvTable table = new CsvTable();
        table.Schema.Columns.Add(CreateColumn("SchemaId", 0));
        table.Schema.Columns.Add(CreateColumn("SchemaValue", 1));
        table.Rows.Add(CreateRow(new string?[] { "42", "12.5" }));
        ICsvSniffer sniffer = new CsvSnifferService();

        CsvSchema schema = sniffer.DetectTypes(table, new CsvInferenceOptions());

        Assert.That(schema.Columns, Has.Count.EqualTo(2));
        Assert.That(schema.Columns[0].Name, Is.EqualTo("SchemaId"));
        Assert.That(schema.Columns[0].TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(schema.Columns[1].Name, Is.EqualTo("SchemaValue"));
        Assert.That(schema.Columns[1].TypeKind, Is.EqualTo(CsvTypeKind.Decimal));
    }

    [Test]
    public void TableOperations_PublicMetadataFallbacksSupportSchemaOnlyAndRowOnlyTables()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable schemaOnly = new CsvTable();
        schemaOnly.Schema.Columns.Add(CreateColumn("Group", 0));
        schemaOnly.Schema.Columns.Add(CreateColumn("Amount", 1));
        schemaOnly.Rows.Add(CreateRow(new string?[] { "A", "1.5" }));
        schemaOnly.Rows.Add(CreateRow(new string?[] { "A", "2.5" }));

        Assert.That(operations.GetColumnIndex(schemaOnly, "Amount"), Is.EqualTo(1));
        CsvAggregate[] sums = operations.SumRowsByColumn(schemaOnly, 0, 1, null);
        Assert.That(sums, Has.Length.EqualTo(1));
        Assert.That(sums[0].Sum, Is.EqualTo(4m));

        CsvTable schemaRight = new CsvTable();
        schemaRight.Schema.Columns.Add(CreateColumn("Group", 0));
        schemaRight.Schema.Columns.Add(CreateColumn("Description", 1));
        schemaRight.Rows.Add(CreateRow(new string?[] { "A", "Alpha" }));
        CsvTable schemaJoin = operations.InnerJoin(schemaOnly, schemaRight, 0, 0);
        Assert.That(schemaJoin.Header.Names, Is.EqualTo(new string[] { "Group", "Amount", "Description" }));
        Assert.That(schemaJoin.Rows, Has.Count.EqualTo(2));

        CsvTable rowOnlyLeft = new CsvTable();
        rowOnlyLeft.Rows.Add(CreateRow(new string?[] { "1", "left" }));
        CsvTable rowOnlyRight = new CsvTable();
        rowOnlyRight.Rows.Add(CreateRow(new string?[] { "1", "right" }));
        CsvTable rowJoin = operations.InnerJoin(rowOnlyLeft, rowOnlyRight, 0, 0);
        CsvTable invalidRename = operations.RenameColumnAt(rowOnlyLeft, -1, "Ignored");

        Assert.That(rowJoin.Rows, Has.Count.EqualTo(1));
        Assert.That(rowJoin.Rows[0].Fields, Is.EqualTo(new string[] { "1", "left", "right" }));
        Assert.That(rowJoin.Header.Names, Is.EqualTo(new string[] { "", "", "_right" }));
        Assert.That(invalidRename.Rows[0].Fields, Is.EqualTo(new string[] { "1", "left" }));

        rowJoin.Rows[0].Fields[1] = "changed";
        Assert.That(rowOnlyLeft.Rows[0].Fields[1], Is.EqualTo("left"));
        Assert.That(rowOnlyRight.Rows[0].Fields[1], Is.EqualTo("right"));
    }

    static void AssertPreambleRoundTrip(ICsvFileService files, string path, Encoding encoding, long expectedOffset, string value)
    {
        CsvWriter writer = files.OpenWriterWithEncoding(path, null, encoding, false);
        writer.WriteField(value);
        writer.NextRecord();
        writer.Close();

        CsvReader reader = files.OpenReaderUtf8(path, null);
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetField(0), Is.EqualTo(value));
        Assert.That(reader.GetCurrentRow().ByteOffset, Is.EqualTo(expectedOffset));
        reader.Close();
    }

    static CsvDiagnostic CreateDiagnostic(string code, CsvRow row, int column)
    {
        CsvDiagnostic diagnostic = new CsvDiagnostic();
        diagnostic.Category = CsvDiagnostic.ProcessingCategory;
        diagnostic.Code = code;
        diagnostic.Message = code;
        diagnostic.Row = row.RowNumber;
        diagnostic.Column = column;
        diagnostic.Line = row.Line;
        diagnostic.CharacterOffset = row.CharacterOffset;
        diagnostic.ByteOffset = row.ByteOffset;
        diagnostic.RawContext = row.RawText;
        return diagnostic;
    }

    static CsvColumn CreateColumn(string name, int index)
    {
        CsvColumn column = new CsvColumn();
        column.Name = name;
        column.Index = index;
        return column;
    }

    static CsvRow CreateRow(string?[] fields)
    {
        CsvRow row = new CsvRow();
        row.Fields = fields;
        return row;
    }

    static string CreateTemporaryPath()
    {
        return Path.Combine(Path.GetTempPath(), "csv-public-completion-" + Guid.NewGuid().ToString("N") + ".csv");
    }

    static void DeleteTemporaryPath(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
