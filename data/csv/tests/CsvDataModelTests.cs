using System;
using System.Reflection;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvDataModelTests
{
    [Test]
    public void CsvRow_CreateCopiesFieldsAndSupportsIndexAccess()
    {
        string[] fields = new string[] { "alpha", "beta" };
        CsvRow row = CsvRow.Create(fields, 7);
        fields[0] = "changed";

        Assert.That(row.RowNumber, Is.EqualTo(7));
        Assert.That(row.CountFields(), Is.EqualTo(2));
        Assert.That(row.HasField(0), Is.True);
        Assert.That(row.HasField(2), Is.False);
        Assert.That(row.GetField(0), Is.EqualTo("alpha"));
        Assert.That(row.GetField(1), Is.EqualTo("beta"));
        Assert.That(row.GetField(-1), Is.EqualTo(""));
        Assert.That(row.GetField(4), Is.EqualTo(""));

        row.SetField(1, "gamma");

        Assert.That(row.GetField(1), Is.EqualTo("gamma"));

        row.AddField("delta");

        Assert.That(row.CountFields(), Is.EqualTo(3));
        Assert.That(row.GetField(2), Is.EqualTo("delta"));
    }

    [Test]
    public void CsvRow_CreateAndClonePreserveNullableFields()
    {
        string?[] fields = new string?[] { "alpha", null };
        CsvRow row = CsvRow.Create(fields, 1);
        fields[1] = "changed";

        CsvRow clone = row.Clone();
        row.SetField(1, "value");
        row.AddField(null);

        Assert.That(clone.GetField(1), Is.Null);
        Assert.That(row.GetField(1), Is.EqualTo("value"));
        Assert.That(row.GetField(2), Is.Null);
    }

    [Test]
    public void CsvRow_CloneCopiesFieldsHeaderAndDiagnostics()
    {
        CsvRow row = CsvRow.Create(new string[] { "1", "2" }, 3);
        row.Line = 4;
        row.Column = 5;
        row.CharacterOffset = 6;
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        options.HasHeader = true;
        CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "A", "B" }, 1), options);
        row.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "A", "B" }, 1), options);
        row.Diagnostics.Add(CsvDiagnostic.CreateParserError("P", "m", 3, 1, 3, 4, "1,2"));

        CsvRow clone = row.Clone();
        row.SetField(0, "changed");
        row.Header.Names[0] = "Changed";
        row.Diagnostics.Get(0).Code = "Changed";

        Assert.That(clone.GetField(0), Is.EqualTo("1"));
        Assert.That(clone.Line, Is.EqualTo(4));
        Assert.That(clone.Column, Is.EqualTo(5));
        Assert.That(clone.CharacterOffset, Is.EqualTo(6));
        Assert.That(clone.Header.Names[0], Is.EqualTo("A"));
        Assert.That(clone.Diagnostics.Get(0).Code, Is.EqualTo("P"));
    }

    [Test]
    public void CsvHeader_CreateFromRowGeneratesMissingNamesAndFindsByName()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        options.NormalizeHeaderNames = true;
        options.CaseSensitive = false;
        options.GeneratedHeaderPrefix = "Field";
        CsvRow headerRow = CsvRow.Create(new string[] { " Name ", "", "AGE" }, 1);

        CsvHeader header = CsvHeader.CreateFromRow(headerRow, options);

        Assert.That(header.Names, Is.EqualTo(new string[] { "Name", "Field2", "AGE" }));
        Assert.That(header.IndexOf("name"), Is.EqualTo(0));
        Assert.That(header.IndexOf(" field2 "), Is.EqualTo(1));
        Assert.That(header.IndexOf("age"), Is.EqualTo(2));
        Assert.That(header.IndexOf("missing"), Is.EqualTo(-1));
        Assert.That(headerRow.Header, Is.SameAs(header));
    }

    [Test]
    public void CsvHeader_CreateGeneratedBuildsConfiguredNames()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        options.GeneratedHeaderPrefix = "Field";
        options.CaseSensitive = false;
        CsvHeader header = CsvHeader.CreateGenerated(3, options);

        Assert.That(header.Names, Is.EqualTo(new string[] { "Field1", "Field2", "Field3" }));
        Assert.That(header.CaseSensitive, Is.False);
        Assert.That(header.IndexOf("field2"), Is.EqualTo(1));
    }

    [Test]
    public void CsvHeader_DuplicateErrorModeRecordsDiagnostic()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvRow headerRow = CsvRow.Create(new string[] { "A", "A" }, 1);

        CsvHeader header = CsvHeader.CreateFromRow(headerRow, options);

        Assert.That(header.Diagnostics.Count(), Is.EqualTo(1));
        Assert.That(header.Diagnostics.Get(0).Code, Is.EqualTo("DuplicateHeader"));
        Assert.That(header.IndexOf("A"), Is.EqualTo(0));
    }

    [Test]
    public void CsvHeader_LastWinsModeReturnsLastDuplicateIndex()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        options.DuplicateHeaderMode = CsvDuplicateHeaderMode.LastWins;
        CsvRow headerRow = CsvRow.Create(new string[] { "A", "B", "A" }, 1);

        CsvHeader header = CsvHeader.CreateFromRow(headerRow, options);

        Assert.That(header.Diagnostics.Count(), Is.EqualTo(0));
        Assert.That(header.IndexOf("A"), Is.EqualTo(2));
    }

    [Test]
    public void CsvRow_GetFieldByNameUsesAssignedHeader()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvRow headerRow = CsvRow.Create(new string[] { "Id", "Name" }, 1);
        CsvHeader header = CsvHeader.CreateFromRow(headerRow, options);
        CsvRow row = CsvRow.Create(new string[] { "42", "Ada" }, 2);
        row.Header = header;

        Assert.That(row.HasFieldByName("Name"), Is.True);
        Assert.That(row.HasFieldByName("Missing"), Is.False);
        Assert.That(row.GetFieldByName("Name"), Is.EqualTo("Ada"));

        row.SetFieldByName("Name", "Grace");

        Assert.That(row.GetFieldByName("Name"), Is.EqualTo("Grace"));
    }

    [Test]
    public void CsvColumn_CreateAndCloneCopyColumnMetadata()
    {
        CsvColumn column = CsvColumn.Create("Amount", 2);
        column.TypeKind = CsvTypeKind.Decimal;
        column.IsNullable = false;
        column.FormatHint = "0.00";
        column.SampledCount = 10;
        column.MissingCount = 1;
        column.NullCount = 2;
        column.EmptyCount = 3;
        column.NonNullCount = 4;

        CsvColumn clone = column.Clone();
        column.Name = "Changed";

        Assert.That(clone.Name, Is.EqualTo("Amount"));
        Assert.That(clone.Index, Is.EqualTo(2));
        Assert.That(clone.TypeKind, Is.EqualTo(CsvTypeKind.Decimal));
        Assert.That(clone.IsNullable, Is.False);
        Assert.That(clone.FormatHint, Is.EqualTo("0.00"));
        Assert.That(clone.SampledCount, Is.EqualTo(10));
        Assert.That(clone.MissingCount, Is.EqualTo(1));
        Assert.That(clone.NullCount, Is.EqualTo(2));
        Assert.That(clone.EmptyCount, Is.EqualTo(3));
        Assert.That(clone.NonNullCount, Is.EqualTo(4));
    }

    [Test]
    public void CsvSchema_CreateFromHeaderBuildsColumns()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvHeader header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Id", "Name" }, 1), options);

        CsvSchema schema = CsvSchema.CreateFromHeader(header);

        Assert.That(schema.Count(), Is.EqualTo(2));
        Assert.That(schema.GetColumn(0).Name, Is.EqualTo("Id"));
        Assert.That(schema.GetColumn(1).Index, Is.EqualTo(1));
        Assert.That(schema.GetColumnIndex("Name"), Is.EqualTo(1));
        Assert.That(schema.GetColumnIndex("Missing"), Is.EqualTo(-1));
    }

    [Test]
    public void CsvSchema_InferFromSampleUsesHeaderNamesAndInfersColumnTypes()
    {
        CsvHeaderOptions headerOptions = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Id", "Active", "Amount", "Created", "Stamp", "Clock", "Key", "Blob", "Name" }, 1), headerOptions);
        table.AddRow(CsvRow.Create(new string[] { "1", "true", "10.5", "2024-02-29", "2024-02-29T01:02:03", "13:14:15", "01234567-89ab-cdef-0123-456789abcdef", "0A FF", "Ada" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "2", "false", "11.75", "2024-03-01", "2024-03-01 04:05:06", "09:00", "11111111-2222-3333-4444-555555555555", "0B-01", "Grace" }, 3));

        CsvSchema schema = CsvSchema.InferFromSample(table, CsvInferenceOptions.CreateDefault());

        Assert.That(schema.Count(), Is.EqualTo(9));
        Assert.That(schema.GetColumn(0).Name, Is.EqualTo("Id"));
        Assert.That(schema.GetColumn(0).TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(schema.GetColumn(1).TypeKind, Is.EqualTo(CsvTypeKind.Boolean));
        Assert.That(schema.GetColumn(2).TypeKind, Is.EqualTo(CsvTypeKind.Decimal));
        Assert.That(schema.GetColumn(3).TypeKind, Is.EqualTo(CsvTypeKind.DateOnly));
        Assert.That(schema.GetColumn(4).TypeKind, Is.EqualTo(CsvTypeKind.DateTime));
        Assert.That(schema.GetColumn(5).TypeKind, Is.EqualTo(CsvTypeKind.TimeOnly));
        Assert.That(schema.GetColumn(6).TypeKind, Is.EqualTo(CsvTypeKind.Guid));
        Assert.That(schema.GetColumn(7).TypeKind, Is.EqualTo(CsvTypeKind.Binary));
        Assert.That(schema.GetColumn(8).TypeKind, Is.EqualTo(CsvTypeKind.String));
        Assert.That(schema.GetColumn(8).IsNullable, Is.False);
    }

    [Test]
    public void CsvSchema_InferFromSampleTracksNullabilityAndHonorsMaxSampleRows()
    {
        CsvHeaderOptions headerOptions = CsvHeaderOptions.CreateDefault();
        CsvInferenceOptions inferenceOptions = CsvInferenceOptions.CreateDefault();
        inferenceOptions.MaxSampleRows = 2;
        inferenceOptions.TreatEmptyAsNull = true;
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Id", "Nickname", "Code" }, 1), headerOptions);
        table.AddRow(CsvRow.Create(new string[] { "1", "", "A" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "2", "Bee", "B" }, 3));
        table.AddRow(CsvRow.Create(new string[] { "", "", "" }, 4));

        CsvSchema schema = CsvSchema.InferFromSample(table, inferenceOptions);

        Assert.That(schema.GetColumn(0).TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(schema.GetColumn(0).IsNullable, Is.False);
        Assert.That(schema.GetColumn(1).TypeKind, Is.EqualTo(CsvTypeKind.String));
        Assert.That(schema.GetColumn(1).IsNullable, Is.True);
        Assert.That(schema.GetColumn(2).IsNullable, Is.False);
    }

    [Test]
    public void CsvSchema_InferFromSampleGeneratesNamesAndCountsWidestSampledRow()
    {
        CsvTable table = new CsvTable();
        table.AddRow(CsvRow.Create(new string[] { "1", "alpha" }, 1));
        table.AddRow(CsvRow.Create(new string[] { "2", "beta", "3" }, 2));

        CsvSchema schema = CsvSchema.InferFromSample(table, CsvInferenceOptions.CreateDefault());

        Assert.That(schema.Count(), Is.EqualTo(3));
        Assert.That(schema.GetColumn(0).Name, Is.EqualTo("Column1"));
        Assert.That(schema.GetColumn(1).Name, Is.EqualTo("Column2"));
        Assert.That(schema.GetColumn(2).Name, Is.EqualTo("Column3"));
        Assert.That(schema.GetColumn(2).TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(schema.GetColumn(2).IsNullable, Is.True);
    }

    [Test]
    public void CsvSchema_InferFromSampleCanDisableTypeAndNullabilityInference()
    {
        CsvInferenceOptions inferenceOptions = CsvInferenceOptions.CreateDefault();
        inferenceOptions.InferTypes = false;
        inferenceOptions.InferNullability = false;
        CsvTable table = new CsvTable();
        table.AddRow(CsvRow.Create(new string[] { "1" }, 1));

        CsvSchema schema = CsvSchema.InferFromSample(table, inferenceOptions);

        Assert.That(schema.GetColumn(0).TypeKind, Is.EqualTo(CsvTypeKind.Unknown));
        Assert.That(schema.GetColumn(0).IsNullable, Is.True);
    }

    [Test]
    public void CsvTable_AddRowWiresHeaderAndSupportsLookup()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Id", "Name" }, 1), options);
        table.Schema = CsvSchema.CreateFromHeader(table.Header);
        CsvRow row = CsvRow.Create(new string[] { "1", "Grace" }, 2);

        table.AddRow(row);

        Assert.That(table.CountRows(), Is.EqualTo(1));
        Assert.That(table.GetRow(0), Is.SameAs(row));
        Assert.That(row.Header, Is.SameAs(table.Header));
        Assert.That(table.GetColumnIndex("Name"), Is.EqualTo(1));
        Assert.That(table.GetRow(0).GetFieldByName("Name"), Is.EqualTo("Grace"));
    }

    [Test]
    public void CsvTable_CloneCopiesRowsHeaderSchemaAndDiagnostics()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Id" }, 1), options);
        table.Schema = CsvSchema.CreateFromHeader(table.Header);
        table.AddRow(CsvRow.Create(new string[] { "1" }, 2));
        table.Diagnostics.Add(CsvDiagnostic.CreateProcessingError("P", "m", 2, 0, 2, 0, "1"));

        CsvTable clone = table.Clone();
        table.Header.Names[0] = "Changed";
        table.GetRow(0).SetField(0, "changed");
        table.Diagnostics.Get(0).Code = "Changed";

        Assert.That(clone.Header.Names[0], Is.EqualTo("Id"));
        Assert.That(clone.GetRow(0).GetField(0), Is.EqualTo("1"));
        Assert.That(clone.Schema.GetColumn(0).Name, Is.EqualTo("Id"));
        Assert.That(clone.Diagnostics.Get(0).Code, Is.EqualTo("P"));
    }

    [Test]
    public void CsvTable_SelectColumnsByNameProjectsHeadersSchemaAndRows()
    {
        CsvTable table = CreatePeopleTable();
        table.Schema.GetColumn(0).TypeKind = CsvTypeKind.Int32;

        CsvTable selected = table.SelectColumnsByName(new string[] { "Last", "Id" });

        Assert.That(selected.Header.Names, Is.EqualTo(new string[] { "Last", "Id" }));
        Assert.That(selected.Schema.GetColumn(0).Name, Is.EqualTo("Last"));
        Assert.That(selected.Schema.GetColumn(0).Index, Is.EqualTo(0));
        Assert.That(selected.Schema.GetColumn(1).TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(selected.GetRow(0).Fields, Is.EqualTo(new string[] { "Lovelace", "1" }));
        Assert.That(selected.GetRow(0).GetFieldByName("Id"), Is.EqualTo("1"));
        Assert.That(table.Header.Names, Is.EqualTo(new string[] { "Id", "First", "Last" }));
    }

    [Test]
    public void CsvTable_ReorderColumnsUsesRequestedIndexOrder()
    {
        CsvTable table = CreatePeopleTable();

        CsvTable reordered = table.ReorderColumns(new int[] { 2, 1, 0 });

        Assert.That(reordered.Header.Names, Is.EqualTo(new string[] { "Last", "First", "Id" }));
        Assert.That(reordered.GetRow(1).Fields, Is.EqualTo(new string[] { "Hopper", "Grace", "2" }));
        Assert.That(reordered.GetColumnIndex("First"), Is.EqualTo(1));
    }

    [Test]
    public void CsvTable_RenameColumnUpdatesHeaderSchemaAndRowLookup()
    {
        CsvTable table = CreatePeopleTable();

        CsvTable renamed = table.RenameColumn("First", "Given");

        Assert.That(renamed.Header.Names, Is.EqualTo(new string[] { "Id", "Given", "Last" }));
        Assert.That(renamed.Schema.GetColumn(1).Name, Is.EqualTo("Given"));
        Assert.That(renamed.GetColumnIndex("Given"), Is.EqualTo(1));
        Assert.That(renamed.GetRow(0).GetFieldByName("Given"), Is.EqualTo("Ada"));
        Assert.That(table.GetColumnIndex("First"), Is.EqualTo(1));
        Assert.That(table.GetColumnIndex("Given"), Is.EqualTo(-1));
    }

    [Test]
    public void CsvTable_SkipLimitAndSliceRowsReturnIndependentTables()
    {
        CsvTable table = CreatePeopleTable();

        CsvTable skipped = table.SkipRows(1);
        CsvTable limited = table.LimitRows(1);
        CsvTable sliced = table.SliceRows(1, 1);

        Assert.That(skipped.CountRows(), Is.EqualTo(1));
        Assert.That(skipped.GetRow(0).GetFieldByName("First"), Is.EqualTo("Grace"));
        Assert.That(limited.CountRows(), Is.EqualTo(1));
        Assert.That(limited.GetRow(0).GetFieldByName("First"), Is.EqualTo("Ada"));
        Assert.That(sliced.CountRows(), Is.EqualTo(1));
        Assert.That(sliced.GetRow(0).GetFieldByName("Last"), Is.EqualTo("Hopper"));

        skipped.GetRow(0).SetFieldByName("First", "Changed");

        Assert.That(table.GetRow(1).GetFieldByName("First"), Is.EqualTo("Grace"));
    }

    [Test]
    public void CsvTable_SortRowsByNameSortsAscendingWithoutMutatingSource()
    {
        CsvTable table = CreateSortablePeopleTable();

        CsvTable sorted = table.SortRowsByName("First", true);

        Assert.That(sorted.GetRow(0).GetFieldByName("First"), Is.EqualTo("Ada"));
        Assert.That(sorted.GetRow(1).GetFieldByName("First"), Is.EqualTo("Alan"));
        Assert.That(sorted.GetRow(2).GetFieldByName("First"), Is.EqualTo("Grace"));
        Assert.That(table.GetRow(0).GetFieldByName("First"), Is.EqualTo("Grace"));
    }

    [Test]
    public void CsvTable_SortRowsByColumnSortsDescending()
    {
        CsvTable table = CreateSortablePeopleTable();

        CsvTable sorted = table.SortRowsByColumn(2, false);

        Assert.That(sorted.GetRow(0).GetFieldByName("Last"), Is.EqualTo("Turing"));
        Assert.That(sorted.GetRow(1).GetFieldByName("Last"), Is.EqualTo("Lovelace"));
        Assert.That(sorted.GetRow(2).GetFieldByName("Last"), Is.EqualTo("Hopper"));
    }

    [Test]
    public void CsvTable_GroupRowsByNameCollectsRowsByKey()
    {
        CsvTable table = CreateDepartmentTable();

        CsvGroup[] groups = table.GroupRowsByName("Dept");

        Assert.That(groups.Length, Is.EqualTo(2));
        Assert.That(groups[0].Key, Is.EqualTo("Eng"));
        Assert.That(groups[0].Count, Is.EqualTo(2));
        Assert.That(groups[0].Rows[0].GetFieldByName("Name"), Is.EqualTo("Ada"));
        Assert.That(groups[1].Key, Is.EqualTo("Ops"));
        Assert.That(groups[1].Count, Is.EqualTo(1));

        groups[0].Rows[0].SetFieldByName("Name", "Changed");

        Assert.That(table.GetRow(0).GetFieldByName("Name"), Is.EqualTo("Ada"));
    }

    [Test]
    public void CsvTable_CountAndSumRowsByNameAggregateGroups()
    {
        CsvTable table = CreateDepartmentTable();

        CsvAggregate[] counts = table.CountRowsByName("Dept");
        CsvAggregate[] totals = table.SumRowsByName("Dept", "Hours", CsvConversionOptions.CreateDefault());

        Assert.That(counts.Length, Is.EqualTo(2));
        Assert.That(counts[0].Key, Is.EqualTo("Eng"));
        Assert.That(counts[0].Count, Is.EqualTo(2));
        Assert.That(counts[1].Key, Is.EqualTo("Ops"));
        Assert.That(counts[1].Count, Is.EqualTo(1));
        Assert.That(totals[0].Key, Is.EqualTo("Eng"));
        Assert.That(totals[0].Count, Is.EqualTo(2));
        Assert.That(totals[0].Sum, Is.EqualTo(13m));
        Assert.That(totals[1].Key, Is.EqualTo("Ops"));
        Assert.That(totals[1].Sum, Is.EqualTo(7m));
    }

    [Test]
    public void CsvTable_InnerJoinByNameCombinesMatchingRows()
    {
        CsvTable users = CreateJoinUsersTable();
        CsvTable scores = CreateJoinScoresTable();
        scores.Schema.GetColumn(1).TypeKind = CsvTypeKind.Int32;

        CsvTable joined = users.InnerJoinByName(scores, "UserId", "UserId");

        Assert.That(joined.Header.Names, Is.EqualTo(new string[] { "UserId", "Name", "Score" }));
        Assert.That(joined.Schema.GetColumn(2).Name, Is.EqualTo("Score"));
        Assert.That(joined.Schema.GetColumn(2).TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(joined.CountRows(), Is.EqualTo(3));
        Assert.That(joined.GetRow(0).Fields, Is.EqualTo(new string[] { "1", "Ada", "10" }));
        Assert.That(joined.GetRow(1).Fields, Is.EqualTo(new string[] { "2", "Grace", "20" }));
        Assert.That(joined.GetRow(2).Fields, Is.EqualTo(new string[] { "2", "Grace", "21" }));
        Assert.That(joined.GetRow(1).GetFieldByName("Score"), Is.EqualTo("20"));
        Assert.That(users.CountRows(), Is.EqualTo(3));
        Assert.That(scores.CountRows(), Is.EqualTo(4));
    }

    [Test]
    public void CsvTable_LeftJoinByNameKeepsUnmatchedLeftRows()
    {
        CsvTable users = CreateJoinUsersTable();
        CsvTable scores = CreateJoinScoresTable();

        CsvTable joined = users.LeftJoinByName(scores, "UserId", "UserId");

        Assert.That(joined.Header.Names, Is.EqualTo(new string[] { "UserId", "Name", "Score" }));
        Assert.That(joined.CountRows(), Is.EqualTo(4));
        Assert.That(joined.GetRow(0).Fields, Is.EqualTo(new string[] { "1", "Ada", "10" }));
        Assert.That(joined.GetRow(1).Fields, Is.EqualTo(new string[] { "2", "Grace", "20" }));
        Assert.That(joined.GetRow(2).Fields, Is.EqualTo(new string[] { "2", "Grace", "21" }));
        Assert.That(joined.GetRow(3).Fields, Is.EqualTo(new string?[] { "3", "Linus", null }));
        Assert.That(joined.GetRow(3).GetFieldByName("Score"), Is.Null);
    }

    [Test]
    public void CsvTable_InnerJoinUsesIndexKeys()
    {
        CsvTable users = CreateJoinUsersTable();
        CsvTable scores = CreateJoinScoresTable();

        CsvTable joined = users.InnerJoin(scores, 0, 0);

        Assert.That(joined.CountRows(), Is.EqualTo(3));
        Assert.That(joined.Header.Names, Is.EqualTo(new string[] { "UserId", "Name", "Score" }));
        Assert.That(joined.GetRow(2).Fields, Is.EqualTo(new string[] { "2", "Grace", "21" }));
    }

    [TestCase(typeof(CsvRow))]
    [TestCase(typeof(CsvHeader))]
    [TestCase(typeof(CsvColumn))]
    [TestCase(typeof(CsvSchema))]
    [TestCase(typeof(CsvTable))]
    [TestCase(typeof(CsvGroup))]
    [TestCase(typeof(CsvAggregate))]
    public void DataModelTypes_ExposePublicFieldsAndNoProperties(Type modelType)
    {
        FieldInfo[] fields = modelType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        PropertyInfo[] properties = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.That(fields.Length, Is.GreaterThan(0));
        Assert.That(properties, Is.Empty);
    }

    public static CsvTable CreatePeopleTable()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Id", "First", "Last" }, 1), options);
        table.Schema = CsvSchema.CreateFromHeader(table.Header);
        table.AddRow(CsvRow.Create(new string[] { "1", "Ada", "Lovelace" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "2", "Grace", "Hopper" }, 3));
        return table;
    }

    public static CsvTable CreateSortablePeopleTable()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Id", "First", "Last" }, 1), options);
        table.Schema = CsvSchema.CreateFromHeader(table.Header);
        table.AddRow(CsvRow.Create(new string[] { "2", "Grace", "Hopper" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "1", "Ada", "Lovelace" }, 3));
        table.AddRow(CsvRow.Create(new string[] { "3", "Alan", "Turing" }, 4));
        return table;
    }

    public static CsvTable CreateDepartmentTable()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "Dept", "Name", "Hours" }, 1), options);
        table.Schema = CsvSchema.CreateFromHeader(table.Header);
        table.AddRow(CsvRow.Create(new string[] { "Eng", "Ada", "10" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "Ops", "Grace", "7" }, 3));
        table.AddRow(CsvRow.Create(new string[] { "Eng", "Linus", "3" }, 4));
        return table;
    }

    public static CsvTable CreateJoinUsersTable()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "UserId", "Name" }, 1), options);
        table.Schema = CsvSchema.CreateFromHeader(table.Header);
        table.AddRow(CsvRow.Create(new string[] { "1", "Ada" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "2", "Grace" }, 3));
        table.AddRow(CsvRow.Create(new string[] { "3", "Linus" }, 4));
        return table;
    }

    public static CsvTable CreateJoinScoresTable()
    {
        CsvHeaderOptions options = CsvHeaderOptions.CreateDefault();
        CsvTable table = new CsvTable();
        table.Header = CsvHeader.CreateFromRow(CsvRow.Create(new string[] { "UserId", "Score" }, 1), options);
        table.Schema = CsvSchema.CreateFromHeader(table.Header);
        table.AddRow(CsvRow.Create(new string[] { "1", "10" }, 2));
        table.AddRow(CsvRow.Create(new string[] { "2", "20" }, 3));
        table.AddRow(CsvRow.Create(new string[] { "2", "21" }, 4));
        table.AddRow(CsvRow.Create(new string[] { "4", "40" }, 5));
        return table;
    }
}
