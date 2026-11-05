using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvProcessingTablePublicTests
{
    [Test]
    public void Processor_PublicInterfacesApplyEveryPipelineStageAndBoundOutput()
    {
        CsvHeader header = CreateHeader(new string[] { "Kind", "Value" });
        List<CsvRow> sourceRows = new List<CsvRow>();
        sourceRows.Add(CreateRow(new string?[] { "position", "0" }, 1));
        sourceRows.Add(CreateRow(new string?[] { "filter", "1" }, 2));
        sourceRows.Add(CreateRow(new string?[] { "rowbad", "2" }, 3));
        sourceRows.Add(CreateRow(new string?[] { "ok", "-1" }, 4));
        sourceRows.Add(CreateRow(new string?[] { "ok", "3" }, 5));
        sourceRows.Add(CreateRow(new string?[] { "unread", "4" }, 6));
        PublicRowReader reader = new PublicRowReader(header, sourceRows);
        PublicRowWriter writer = new PublicRowWriter(true);
        List<string> rejectCodes = new List<string>();
        int progressCount = 0;
        long lastProgressWritten = -1;
        CsvPipeline pipeline = new CsvPipeline();
        pipeline.SkipRows = 1;
        pipeline.MaxRows = 1;
        pipeline.Filter = new CsvRowFilter(row => row.Fields[0] != "filter");
        pipeline.HasFilter = true;
        pipeline.Transformer = new CsvRowTransformer(EnrichRow);
        pipeline.HasTransformer = true;
        pipeline.Validator = new CsvRowValidator("RowRejected", "The row was rejected.", IsAcceptedRow, CreateRowDiagnostic);
        pipeline.HasValidator = true;
        pipeline.FieldValidator = new CsvFieldValidator("FieldRejected", "The field was rejected.", IsAcceptedField, CreateFieldDiagnostic);
        pipeline.HasFieldValidator = true;
        pipeline.RejectHandler = new CsvRejectHandler((row, diagnostic) => rejectCodes.Add(diagnostic.Code));
        pipeline.HasRejectHandler = true;
        pipeline.ProgressHandler = new CsvProgressHandler((state, row) =>
        {
            progressCount = progressCount + 1;
            lastProgressWritten = state.RowsWritten;
        });
        pipeline.HasProgressHandler = true;
        ICsvProcessor processor = new CsvProcessorService();

        long written = processor.Process(reader, writer, pipeline);

        Assert.That(written, Is.EqualTo(1));
        Assert.That(writer.Headers, Has.Count.EqualTo(1));
        Assert.That(writer.Headers[0].Names, Is.EqualTo(new string[] { "Kind", "Value" }));
        Assert.That(writer.Rows, Has.Count.EqualTo(1));
        Assert.That(writer.Rows[0].Fields, Is.EqualTo(new string[] { "OK", "3", "OK:3" }));
        Assert.That(pipeline.RowsRead, Is.EqualTo(5));
        Assert.That(pipeline.RowsSkipped, Is.EqualTo(1));
        Assert.That(pipeline.RowsFiltered, Is.EqualTo(1));
        Assert.That(pipeline.RowsTransformed, Is.EqualTo(3));
        Assert.That(pipeline.RowsRejected, Is.EqualTo(2));
        Assert.That(pipeline.RowsWritten, Is.EqualTo(1));
        Assert.That(pipeline.FieldsValidated, Is.EqualTo(6));
        Assert.That(pipeline.FieldsRejected, Is.EqualTo(1));
        Assert.That(pipeline.Rejects, Has.Count.EqualTo(2));
        Assert.That(pipeline.RejectDiagnostics.Items, Has.Count.EqualTo(2));
        Assert.That(rejectCodes, Is.EqualTo(new string[] { "RowRejected", "FieldRejected" }));
        Assert.That(progressCount, Is.EqualTo(5));
        Assert.That(lastProgressWritten, Is.EqualTo(1));
        Assert.That(reader.Index, Is.EqualTo(4));
        Assert.That(sourceRows[2].Fields, Is.EqualTo(new string[] { "rowbad", "2" }));

        sourceRows[2].Fields[0] = "changed";
        Assert.That(pipeline.Rejects[0].Fields[0], Is.EqualTo("ROWBAD"));
    }

    [Test]
    public void Processor_LoadTableUsesPublicReaderBoundsAndClonesRows()
    {
        CsvHeader header = CreateHeader(new string[] { "Id", "Name" });
        List<CsvRow> sourceRows = new List<CsvRow>();
        sourceRows.Add(CreateRow(new string?[] { "1", "Ada" }, 1));
        sourceRows.Add(CreateRow(new string?[] { "2", "Grace" }, 2));
        sourceRows.Add(CreateRow(new string?[] { "3", "Linus" }, 3));
        sourceRows[0].Diagnostics.Items.Add(CreateDiagnostic("Source", sourceRows[0], 1));
        PublicRowReader reader = new PublicRowReader(header, sourceRows);
        CsvTableOptions options = new CsvTableOptions();
        options.MaxRows = 2;
        options.IncludeHeader = true;
        options.CloneRows = true;
        options.PreserveDiagnostics = false;
        ICsvProcessor processor = new CsvProcessorService();

        CsvTable table = processor.LoadTable(reader, options);

        Assert.That(table.Header.Names, Is.EqualTo(new string[] { "Id", "Name" }));
        Assert.That(table.Schema.Columns, Has.Count.EqualTo(2));
        Assert.That(table.Rows, Has.Count.EqualTo(2));
        Assert.That(table.Rows[0].Diagnostics.Items, Is.Empty);
        Assert.That(table.Diagnostics.Items, Is.Empty);
        Assert.That(reader.Index, Is.EqualTo(1));

        sourceRows[0].Fields[1] = "changed";
        header.Names[1] = "Changed";
        Assert.That(table.Rows[0].Fields[1], Is.EqualTo("Ada"));
        Assert.That(table.Header.Names[1], Is.EqualTo("Name"));
        Assert.That(reader.Read(), Is.True);
        Assert.That(reader.GetCurrentRow().Fields[0], Is.EqualTo("3"));
    }

    [Test]
    public void TableOperations_PublicInterfaceSupportsBoundsProjectionRenameAndSortWithoutMutation()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable source = CreatePeopleTable();

        CsvTable skipped = operations.SkipRows(source, 1);
        CsvTable limited = operations.LimitRows(source, 2);
        CsvTable empty = operations.SliceRows(source, 20, 2);
        CsvTable projected = operations.SelectColumnsByName(source, new string[] { "Name", "Department" });
        CsvTable renamed = operations.RenameColumn(projected, "Department", "Team");
        CsvTable sorted = operations.SortRowsByName(renamed, "Name", false);

        Assert.That(operations.CountRows(source), Is.EqualTo(3));
        Assert.That(skipped.Rows, Has.Count.EqualTo(2));
        Assert.That(limited.Rows, Has.Count.EqualTo(2));
        Assert.That(empty.Rows, Is.Empty);
        Assert.That(operations.GetRow(source, 0).Fields[1], Is.EqualTo("Ada"));
        Assert.Throws<ArgumentOutOfRangeException>(() => operations.GetRow(source, 3));
        Assert.That(operations.GetColumnIndex(source, "Missing"), Is.EqualTo(-1));
        Assert.That(projected.Header.Names, Is.EqualTo(new string[] { "Name", "Department" }));
        Assert.That(renamed.Header.Names, Is.EqualTo(new string[] { "Name", "Team" }));
        Assert.That(sorted.Rows[0].Fields[0], Is.EqualTo("Linus"));
        Assert.That(sorted.Rows[2].Fields[0], Is.EqualTo("Ada"));
        Assert.That(source.Header.Names, Is.EqualTo(new string[] { "Department", "Name", "Hours" }));
        Assert.That(source.Rows[0].Fields, Is.EqualTo(new string[] { "Engineering", "Ada", "10.25" }));

        sorted.Rows[0].Fields[0] = "changed";
        sorted.Header.Names[0] = "Changed";
        Assert.That(source.Rows[2].Fields[1], Is.EqualTo("Linus"));
        Assert.That(source.Header.Names[1], Is.EqualTo("Name"));
    }

    [Test]
    public void TableOperations_PublicInterfaceGroupsAndAggregatesByIndexAndName()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable source = CreatePeopleTable();

        CsvGroup[] groups = operations.GroupRowsByName(source, "Department");
        CsvAggregate[] counts = operations.CountRowsByColumn(source, 0);
        CsvAggregate[] sums = operations.SumRowsByName(source, "Department", "Hours", new CsvConversionOptions());

        Assert.That(groups, Has.Length.EqualTo(2));
        Assert.That(groups[0].Key, Is.EqualTo("Engineering"));
        Assert.That(groups[0].Rows, Has.Count.EqualTo(2));
        Assert.That(groups[1].Key, Is.EqualTo("Operations"));
        Assert.That(counts[0].Count, Is.EqualTo(2));
        Assert.That(counts[1].Count, Is.EqualTo(1));
        Assert.That(sums[0].Count, Is.EqualTo(2));
        Assert.That(sums[0].Sum, Is.EqualTo(13.75m));
        Assert.That(sums[1].Sum, Is.EqualTo(7.5m));

        groups[0].Rows[0].Fields[1] = "changed";
        Assert.That(source.Rows[0].Fields[1], Is.EqualTo("Ada"));
    }

    [Test]
    public void TableOperations_PublicInterfaceJoinsByIndexAndNameWithoutMutatingInputs()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable left = CreateJoinLeftTable();
        CsvTable right = CreateJoinRightTable();

        CsvTable inner = operations.InnerJoin(left, right, 0, 0);
        CsvTable outer = operations.LeftJoinByName(left, right, "Id", "UserId");

        Assert.That(inner.Rows, Has.Count.EqualTo(3));
        Assert.That(inner.Header.Names, Is.EqualTo(new string[] { "Id", "Name", "Name_right", "Role" }));
        Assert.That(inner.Rows[2].Fields, Is.EqualTo(new string[] { "2", "Grace", "Amazing Grace", "Pioneer" }));
        Assert.That(outer.Rows, Has.Count.EqualTo(4));
        Assert.That(outer.Rows[3].Fields, Is.EqualTo(new string?[] { "3", "Linus", null, null }));

        inner.Rows[0].Fields[1] = "changed";
        outer.Header.Names[0] = "Changed";
        Assert.That(left.Rows[0].Fields[1], Is.EqualTo("Ada"));
        Assert.That(left.Header.Names[0], Is.EqualTo("Id"));
        Assert.That(right.Rows[0].Fields[1], Is.EqualTo("Augusta"));
    }

    [Test]
    public void FileService_PublicInterfaceConcatenatesAndUnionsIntoInterfaceSinks()
    {
        string firstPath = CreateTemporaryPath();
        string secondPath = CreateTemporaryPath();
        try
        {
            File.WriteAllText(firstPath, "Id,Name\n1,Ada\n", new UTF8Encoding(false));
            File.WriteAllText(secondPath, "Name,Id\nGrace,2\n", new UTF8Encoding(false));
            CsvReaderOptions options = new CsvReaderOptions();
            options.HeaderOptions.HasHeader = true;
            PublicRowWriter concatenated = new PublicRowWriter(true);
            PublicRowWriter unioned = new PublicRowWriter(true);
            ICsvFileService files = new CsvFileService();

            long concatenatedCount = files.ConcatenateToWriterByPosition(new string[] { firstPath, secondPath }, concatenated, options);
            long unionedCount = files.UnionToWriterByName(new string[] { firstPath, secondPath }, unioned, options);

            Assert.That(concatenatedCount, Is.EqualTo(2));
            Assert.That(concatenated.Headers, Has.Count.EqualTo(1));
            Assert.That(concatenated.Headers[0].Names, Is.EqualTo(new string[] { "Id", "Name" }));
            Assert.That(concatenated.Rows[0].Fields, Is.EqualTo(new string[] { "1", "Ada" }));
            Assert.That(concatenated.Rows[1].Fields, Is.EqualTo(new string[] { "Grace", "2" }));
            Assert.That(unionedCount, Is.EqualTo(2));
            Assert.That(unioned.Headers, Has.Count.EqualTo(1));
            Assert.That(unioned.Headers[0].Names, Is.EqualTo(new string[] { "Id", "Name" }));
            Assert.That(unioned.Rows[0].Fields, Is.EqualTo(new string[] { "1", "Ada" }));
            Assert.That(unioned.Rows[1].Fields, Is.EqualTo(new string[] { "2", "Grace" }));
        }
        finally
        {
            DeleteTemporaryPath(firstPath);
            DeleteTemporaryPath(secondPath);
        }
    }

    static CsvRow? EnrichRow(CsvRow source)
    {
        CsvRow row = CopyRow(source);
        string kind = row.Fields[0] == null ? "" : row.Fields[0] ?? "";
        string value = row.Fields[1] == null ? "" : row.Fields[1] ?? "";
        row.Fields = new string?[] { kind.ToUpperInvariant(), value, kind.ToUpperInvariant() + ":" + value };
        return row;
    }

    static bool IsAcceptedRow(CsvRow row)
    {
        return row.Fields[0] != "ROWBAD";
    }

    static bool IsAcceptedField(CsvRow row, int fieldIndex, string? value)
    {
        if (value != null && value.StartsWith("-", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    static CsvDiagnostic CreateRowDiagnostic(CsvRow row)
    {
        return CreateDiagnostic("RowRejected", row, 0);
    }

    static CsvDiagnostic CreateFieldDiagnostic(CsvRow row, int fieldIndex, string? value)
    {
        return CreateDiagnostic("FieldRejected", row, fieldIndex + 1);
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

    static CsvTable CreatePeopleTable()
    {
        CsvTable table = new CsvTable();
        table.Header = CreateHeader(new string[] { "Department", "Name", "Hours" });
        table.Schema.Columns.Add(CreateColumn("Department", 0));
        table.Schema.Columns.Add(CreateColumn("Name", 1));
        table.Schema.Columns.Add(CreateColumn("Hours", 2));
        table.Rows.Add(CreateRow(new string?[] { "Engineering", "Ada", "10.25" }, 1));
        table.Rows.Add(CreateRow(new string?[] { "Operations", "Grace", "7.5" }, 2));
        table.Rows.Add(CreateRow(new string?[] { "Engineering", "Linus", "3.5" }, 3));
        return table;
    }

    static CsvTable CreateJoinLeftTable()
    {
        CsvTable table = new CsvTable();
        table.Header = CreateHeader(new string[] { "Id", "Name" });
        table.Schema.Columns.Add(CreateColumn("Id", 0));
        table.Schema.Columns.Add(CreateColumn("Name", 1));
        table.Rows.Add(CreateRow(new string?[] { "1", "Ada" }, 1));
        table.Rows.Add(CreateRow(new string?[] { "2", "Grace" }, 2));
        table.Rows.Add(CreateRow(new string?[] { "3", "Linus" }, 3));
        return table;
    }

    static CsvTable CreateJoinRightTable()
    {
        CsvTable table = new CsvTable();
        table.Header = CreateHeader(new string[] { "UserId", "Name", "Role" });
        table.Schema.Columns.Add(CreateColumn("UserId", 0));
        table.Schema.Columns.Add(CreateColumn("Name", 1));
        table.Schema.Columns.Add(CreateColumn("Role", 2));
        table.Rows.Add(CreateRow(new string?[] { "1", "Augusta", "Mathematician" }, 1));
        table.Rows.Add(CreateRow(new string?[] { "2", "Rear Admiral", "Programmer" }, 2));
        table.Rows.Add(CreateRow(new string?[] { "2", "Amazing Grace", "Pioneer" }, 3));
        return table;
    }

    static CsvHeader CreateHeader(string[] names)
    {
        CsvHeader header = new CsvHeader();
        header.Names = (string[])names.Clone();
        return header;
    }

    static CsvColumn CreateColumn(string name, int index)
    {
        CsvColumn column = new CsvColumn();
        column.Name = name;
        column.Index = index;
        return column;
    }

    static CsvRow CreateRow(string?[] fields, long rowNumber)
    {
        CsvRow row = new CsvRow();
        row.Fields = (string?[])fields.Clone();
        row.RowNumber = rowNumber;
        row.Line = rowNumber;
        row.RawText = string.Join(",", fields);
        return row;
    }

    static CsvRow CopyRow(CsvRow source)
    {
        CsvRow row = new CsvRow();
        row.RowNumber = source.RowNumber;
        row.Line = source.Line;
        row.Column = source.Column;
        row.CharacterOffset = source.CharacterOffset;
        row.ByteOffset = source.ByteOffset;
        row.Fields = (string?[])source.Fields.Clone();
        row.RawText = source.RawText;
        row.Header = CreateHeader(source.Header.Names);
        return row;
    }

    static string CreateTemporaryPath()
    {
        return Path.Combine(Path.GetTempPath(), "csv-public-processing-" + Guid.NewGuid().ToString("N") + ".csv");
    }

    static void DeleteTemporaryPath(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

public class PublicRowReader : ICsvRowReader
{
    public CsvHeader Header;
    public List<CsvRow> Rows;
    public int Index;
    public CsvDiagnosticList Diagnostics;

    public PublicRowReader(CsvHeader header, List<CsvRow> rows)
    {
        Header = header;
        Rows = rows;
        Index = -1;
        Diagnostics = new CsvDiagnosticList();
    }

    public bool Read()
    {
        Index = Index + 1;
        if (Index >= Rows.Count)
        {
            return false;
        }

        Rows[Index].Header = Header;
        return true;
    }

    public bool ReadHeader()
    {
        return Header.Names.Length > 0;
    }

    public CsvRow GetCurrentRow()
    {
        return Rows[Index];
    }

    public CsvHeader GetHeaders()
    {
        return Header;
    }

    public CsvDiagnosticList GetDiagnostics()
    {
        return Diagnostics;
    }

    public bool ShouldReadHeader()
    {
        return false;
    }
}

public class PublicRowWriter : ICsvRowWriter
{
    public List<CsvHeader> Headers;
    public List<CsvRow> Rows;
    public bool WriteHeaders;

    public PublicRowWriter(bool writeHeaders)
    {
        Headers = new List<CsvHeader>();
        Rows = new List<CsvRow>();
        WriteHeaders = writeHeaders;
    }

    public void WriteHeader(CsvHeader header)
    {
        CsvHeader copy = new CsvHeader();
        copy.Names = (string[])header.Names.Clone();
        Headers.Add(copy);
    }

    public void WriteRow(CsvRow source)
    {
        CsvRow row = new CsvRow();
        row.RowNumber = source.RowNumber;
        row.Line = source.Line;
        row.Column = source.Column;
        row.CharacterOffset = source.CharacterOffset;
        row.ByteOffset = source.ByteOffset;
        row.Fields = (string?[])source.Fields.Clone();
        row.RawText = source.RawText;
        Rows.Add(row);
    }

    public bool ShouldWriteHeader()
    {
        return WriteHeaders;
    }
}
