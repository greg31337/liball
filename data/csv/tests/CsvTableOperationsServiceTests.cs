using System;
using NUnit.Framework;
using csv;

namespace csv.Tests;

[TestFixture]
public class CsvTableOperationsServiceTests
{
    [Test]
    public void CountRowsAndGetRowExposeBoundedRowAccess()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable table = CreateTable();

        Assert.That(operations.CountRows(table), Is.EqualTo(3));
        Assert.That(operations.GetRow(table, 1).Fields, Is.EqualTo(new string[] { "2", "Grace", "Hopper" }));
        Assert.Throws<ArgumentOutOfRangeException>(() => operations.GetRow(table, 3));
    }

    [Test]
    public void CloneCreatesIndependentHeaderSchemaRowsAndDiagnostics()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable table = CreateTable();
        table.Diagnostics.Items.Add(new CsvDiagnostic());

        CsvTable clone = operations.Clone(table);
        table.Header.Names[0] = "Changed";
        table.Schema.Columns[0].Name = "Changed";
        table.Rows[0].Fields[0] = "Changed";
        table.Diagnostics.Items[0].Code = "Changed";

        Assert.That(clone.Header.Names[0], Is.EqualTo("Id"));
        Assert.That(clone.Schema.Columns[0].Name, Is.EqualTo("Id"));
        Assert.That(clone.Rows[0].Fields[0], Is.EqualTo("1"));
        Assert.That(clone.Diagnostics.Items[0].Code, Is.EqualTo(""));
    }

    [Test]
    public void SkipLimitAndSliceReturnRequestedIndependentRows()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable table = CreateTable();

        CsvTable skipped = operations.SkipRows(table, 1);
        CsvTable limited = operations.LimitRows(table, 2);
        CsvTable sliced = operations.SliceRows(table, 1, 1);
        CsvTable unbounded = operations.SliceRows(table, -1, -1);

        Assert.That(skipped.Rows.Count, Is.EqualTo(2));
        Assert.That(skipped.Rows[0].Fields[1], Is.EqualTo("Grace"));
        Assert.That(limited.Rows.Count, Is.EqualTo(2));
        Assert.That(limited.Rows[1].Fields[1], Is.EqualTo("Grace"));
        Assert.That(sliced.Rows.Count, Is.EqualTo(1));
        Assert.That(sliced.Rows[0].Fields[1], Is.EqualTo("Grace"));
        Assert.That(unbounded.Rows.Count, Is.EqualTo(3));

        sliced.Rows[0].Fields[1] = "Changed";
        Assert.That(table.Rows[1].Fields[1], Is.EqualTo("Grace"));
    }

    [Test]
    public void ColumnLookupAndSelectionProjectByIndexAndName()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable table = CreateTable();

        CsvTable byIndex = operations.SelectColumns(table, new int[] { 2, 0 });
        CsvTable byName = operations.SelectColumnsByName(table, new string[] { "First", "Missing", "Id" });

        Assert.That(operations.GetColumnIndex(table, "First"), Is.EqualTo(1));
        Assert.That(operations.GetColumnIndex(table, "Missing"), Is.EqualTo(-1));
        Assert.That(byIndex.Header.Names, Is.EqualTo(new string[] { "Last", "Id" }));
        Assert.That(byIndex.Rows[0].Fields, Is.EqualTo(new string[] { "Lovelace", "1" }));
        Assert.That(byName.Header.Names, Is.EqualTo(new string[] { "First", "", "Id" }));
        Assert.That(byName.Rows[1].Fields, Is.EqualTo(new string[] { "Grace", "", "2" }));
        Assert.That(byName.Schema.Columns[2].Index, Is.EqualTo(2));
    }

    [Test]
    public void ReorderAndRenameReturnIndependentTables()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable table = CreateTable();

        CsvTable reordered = operations.ReorderColumns(table, new int[] { 2, 1, 0 });
        CsvTable renamedByName = operations.RenameColumn(table, "First", "Given");
        CsvTable renamedByIndex = operations.RenameColumnAt(table, 2, "Surname");

        Assert.That(reordered.Header.Names, Is.EqualTo(new string[] { "Last", "First", "Id" }));
        Assert.That(reordered.Rows[2].Fields, Is.EqualTo(new string[] { "Turing", "Alan", "3" }));
        Assert.That(renamedByName.Header.Names, Is.EqualTo(new string[] { "Id", "Given", "Last" }));
        Assert.That(renamedByName.Schema.Columns[1].Name, Is.EqualTo("Given"));
        Assert.That(renamedByIndex.Header.Names, Is.EqualTo(new string[] { "Id", "First", "Surname" }));
        Assert.That(renamedByIndex.Rows[0].Header.Names[2], Is.EqualTo("Surname"));
        Assert.That(table.Header.Names, Is.EqualTo(new string[] { "Id", "First", "Last" }));
    }

    [Test]
    public void SortByIndexAndNameSupportBothDirectionsWithoutMutatingSource()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable table = CreateTable();

        CsvTable ascending = operations.SortRowsByColumn(table, 1, true);
        CsvTable descending = operations.SortRowsByName(table, "Last", false);

        Assert.That(ascending.Rows[0].Fields[1], Is.EqualTo("Ada"));
        Assert.That(ascending.Rows[1].Fields[1], Is.EqualTo("Alan"));
        Assert.That(ascending.Rows[2].Fields[1], Is.EqualTo("Grace"));
        Assert.That(descending.Rows[0].Fields[2], Is.EqualTo("Turing"));
        Assert.That(descending.Rows[1].Fields[2], Is.EqualTo("Lovelace"));
        Assert.That(descending.Rows[2].Fields[2], Is.EqualTo("Hopper"));
        Assert.That(table.Rows[0].Fields[1], Is.EqualTo("Ada"));
        Assert.That(table.Rows[1].Fields[1], Is.EqualTo("Grace"));
    }

    [Test]
    public void GroupByIndexAndNamePreserveFirstSeenOrderAndCloneRows()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable table = CreateAggregateTable();

        CsvGroup[] byIndex = operations.GroupRowsByColumn(table, 0);
        CsvGroup[] byName = operations.GroupRowsByName(table, "Department");

        Assert.That(byIndex.Length, Is.EqualTo(2));
        Assert.That(byIndex[0].Key, Is.EqualTo("Engineering"));
        Assert.That(byIndex[0].Count, Is.EqualTo(2));
        Assert.That(byIndex[0].Rows[1].Fields[1], Is.EqualTo("Linus"));
        Assert.That(byIndex[1].Key, Is.EqualTo("Operations"));
        Assert.That(byIndex[1].Count, Is.EqualTo(1));
        Assert.That(byName[0].Key, Is.EqualTo("Engineering"));
        Assert.That(byName[1].Key, Is.EqualTo("Operations"));

        byName[0].Rows[0].Fields[1] = "Changed";
        Assert.That(table.Rows[0].Fields[1], Is.EqualTo("Ada"));
    }

    [Test]
    public void CountByIndexAndNameReturnCountsInFirstSeenOrder()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable table = CreateAggregateTable();

        CsvAggregate[] byIndex = operations.CountRowsByColumn(table, 0);
        CsvAggregate[] byName = operations.CountRowsByName(table, "Department");

        Assert.That(byIndex.Length, Is.EqualTo(2));
        Assert.That(byIndex[0].Key, Is.EqualTo("Engineering"));
        Assert.That(byIndex[0].Count, Is.EqualTo(2));
        Assert.That(byIndex[1].Key, Is.EqualTo("Operations"));
        Assert.That(byIndex[1].Count, Is.EqualTo(1));
        Assert.That(byName[0].Count, Is.EqualTo(2));
        Assert.That(byName[1].Count, Is.EqualTo(1));
    }

    [Test]
    public void SumByIndexAndNameUseDecimalConversionOptions()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable table = CreateAggregateTable();
        CsvConversionOptions options = new CsvConversionOptions();
        options.DecimalSeparator = ",";
        options.ThousandsSeparator = ".";

        CsvAggregate[] byIndex = operations.SumRowsByColumn(table, 0, 2, options);
        CsvAggregate[] byName = operations.SumRowsByName(table, "Department", "Hours", options);

        Assert.That(byIndex.Length, Is.EqualTo(2));
        Assert.That(byIndex[0].Key, Is.EqualTo("Engineering"));
        Assert.That(byIndex[0].Count, Is.EqualTo(2));
        Assert.That(byIndex[0].Sum, Is.EqualTo(13.75m));
        Assert.That(byIndex[1].Key, Is.EqualTo("Operations"));
        Assert.That(byIndex[1].Count, Is.EqualTo(1));
        Assert.That(byIndex[1].Sum, Is.EqualTo(7.5m));
        Assert.That(byName[0].Sum, Is.EqualTo(13.75m));
        Assert.That(byName[1].Sum, Is.EqualTo(7.5m));
    }

    [Test]
    public void InnerJoinByIndexAndNameReturnEveryMatchWithUniqueHeaders()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable left = CreateJoinLeftTable();
        CsvTable right = CreateJoinRightTable();

        CsvTable byIndex = operations.InnerJoin(left, right, 0, 0);
        CsvTable byName = operations.InnerJoinByName(left, right, "Id", "UserId");

        Assert.That(byIndex.Rows.Count, Is.EqualTo(3));
        Assert.That(byIndex.Header.Names, Is.EqualTo(new string[] { "Id", "Name", "Name_right", "Status", "Name_right2", "Status_right", "Score" }));
        Assert.That(byIndex.Schema.Columns[4].Name, Is.EqualTo("Name_right2"));
        Assert.That(byIndex.Schema.Columns[6].Name, Is.EqualTo("Score"));
        Assert.That(byIndex.Schema.Columns[6].TypeKind, Is.EqualTo(CsvTypeKind.Int32));
        Assert.That(byIndex.Rows[0].Fields, Is.EqualTo(new string[] { "1", "Ada", "Countess", "active", "Augusta", "verified", "10" }));
        Assert.That(byIndex.Rows[2].Fields, Is.EqualTo(new string[] { "2", "Grace", "Admiral", "active", "Amazing", "retired", "21" }));
        Assert.That(byName.Header.Names, Is.EqualTo(byIndex.Header.Names));
        Assert.That(byName.Rows[1].Fields, Is.EqualTo(new string[] { "2", "Grace", "Admiral", "active", "Rear Admiral", "verified", "20" }));

        byName.Rows[0].Fields[1] = "Changed";
        Assert.That(left.Rows[0].Fields[1], Is.EqualTo("Ada"));
        Assert.That(right.Rows[0].Fields[1], Is.EqualTo("Augusta"));
    }

    [Test]
    public void LeftJoinByIndexAndNameNullFillEveryUnmatchedRightField()
    {
        ICsvTableOperations operations = new CsvTableOperationsService();
        CsvTable left = CreateJoinLeftTable();
        CsvTable right = CreateJoinRightTable();

        CsvTable byIndex = operations.LeftJoin(left, right, 0, 0);
        CsvTable byName = operations.LeftJoinByName(left, right, "Id", "UserId");

        Assert.That(byIndex.Rows.Count, Is.EqualTo(4));
        Assert.That(byName.Rows.Count, Is.EqualTo(4));
        Assert.That(byIndex.Rows[3].Fields, Is.EqualTo(new string?[] { "3", "Linus", "Creator", "pending", null, null, null }));
        Assert.That(byName.Rows[3].Fields[4], Is.Null);
        Assert.That(byName.Rows[3].Fields[5], Is.Null);
        Assert.That(byName.Rows[3].Fields[6], Is.Null);
        Assert.That(byName.Rows[3].Header, Is.SameAs(byName.Header));
    }

    static CsvTable CreateTable()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Id", "First", "Last" };
        table.Schema.Columns.Add(CreateColumn("Id", 0));
        table.Schema.Columns.Add(CreateColumn("First", 1));
        table.Schema.Columns.Add(CreateColumn("Last", 2));
        table.Rows.Add(CreateRow(new string[] { "1", "Ada", "Lovelace" }, 1));
        table.Rows.Add(CreateRow(new string[] { "2", "Grace", "Hopper" }, 2));
        table.Rows.Add(CreateRow(new string[] { "3", "Alan", "Turing" }, 3));
        return table;
    }

    static CsvTable CreateAggregateTable()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Department", "Name", "Hours" };
        table.Schema.Columns.Add(CreateColumn("Department", 0));
        table.Schema.Columns.Add(CreateColumn("Name", 1));
        table.Schema.Columns.Add(CreateColumn("Hours", 2));
        table.Rows.Add(CreateRow(new string[] { "Engineering", "Ada", "10,25" }, 1));
        table.Rows.Add(CreateRow(new string[] { "Operations", "Grace", "7,5" }, 2));
        table.Rows.Add(CreateRow(new string[] { "Engineering", "Linus", "3,5" }, 3));
        return table;
    }

    static CsvTable CreateJoinLeftTable()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "Id", "Name", "Name_right", "Status" };
        table.Schema.Columns.Add(CreateColumn("Id", 0));
        table.Schema.Columns.Add(CreateColumn("Name", 1));
        table.Schema.Columns.Add(CreateColumn("Name_right", 2));
        table.Schema.Columns.Add(CreateColumn("Status", 3));
        table.Rows.Add(CreateRow(new string[] { "1", "Ada", "Countess", "active" }, 1));
        table.Rows.Add(CreateRow(new string[] { "2", "Grace", "Admiral", "active" }, 2));
        table.Rows.Add(CreateRow(new string[] { "3", "Linus", "Creator", "pending" }, 3));
        return table;
    }

    static CsvTable CreateJoinRightTable()
    {
        CsvTable table = new CsvTable();
        table.Header.Names = new string[] { "UserId", "Name", "Status", "Score" };
        table.Schema.Columns.Add(CreateColumn("UserId", 0));
        table.Schema.Columns.Add(CreateColumn("Name", 1));
        table.Schema.Columns.Add(CreateColumn("Status", 2));
        table.Schema.Columns.Add(CreateColumn("Score", 3));
        table.Schema.Columns[3].TypeKind = CsvTypeKind.Int32;
        table.Rows.Add(CreateRow(new string[] { "1", "Augusta", "verified", "10" }, 1));
        table.Rows.Add(CreateRow(new string[] { "2", "Rear Admiral", "verified", "20" }, 2));
        table.Rows.Add(CreateRow(new string[] { "2", "Amazing", "retired", "21" }, 3));
        table.Rows.Add(CreateRow(new string[] { "4", "Unknown", "inactive", "40" }, 4));
        return table;
    }

    static CsvColumn CreateColumn(string name, int index)
    {
        CsvColumn column = new CsvColumn();
        column.Name = name;
        column.Index = index;
        return column;
    }

    static CsvRow CreateRow(string[] fields, long rowNumber)
    {
        CsvRow row = new CsvRow();
        row.Fields = fields;
        row.RowNumber = rowNumber;
        return row;
    }
}
