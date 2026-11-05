using System.Text;

namespace odf.Tests;

public class TableDocumentModelTests
{
    [Test]
    public void IndexesLargeRowColumnAndCellRepetitionsWithoutExpansion()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Large'>"
            + "<table:table-column table:number-columns-repeated='5'/>"
            + "<table:table-row table:number-rows-repeated='2'>"
            + "<table:table-cell table:number-columns-repeated='5'/>"
            + "</table:table-row>"
            + "<table:table-row table:number-rows-repeated='1000000'>"
            + "<table:covered-table-cell table:number-columns-repeated='4'/>"
            + "<table:table-cell/>"
            + "</table:table-row>"
            + "</table:table>");
        OdfTable table = document.Tables[0];
        OdfRow repeated = TableDocumentModel.GetRow(table, 500000)!;
        OdfCell covered = TableDocumentModel.GetCell(table, 500000, 3)!;
        OdfCell ordinary = TableDocumentModel.GetCell(table, 500000, 4)!;

        Assert.Multiple(() =>
        {
            Assert.That(table.RowCount, Is.EqualTo(1000002));
            Assert.That(table.ColumnCount, Is.EqualTo(5));
            Assert.That(table.Rows, Has.Length.EqualTo(2));
            Assert.That(table.Columns, Has.Length.EqualTo(1));
            Assert.That(repeated, Is.SameAs(table.Rows[1]));
            Assert.That(repeated.StartIndex, Is.EqualTo(2));
            Assert.That(repeated.RepeatCount, Is.EqualTo(1000000));
            Assert.That(repeated.Cells, Has.Length.EqualTo(2));
            Assert.That(covered.IsCovered, Is.True);
            Assert.That(covered.RepeatCount, Is.EqualTo(4));
            Assert.That(ordinary.IsCovered, Is.False);
            Assert.That(TableDocumentModel.GetRow(table, 1000002), Is.Null);
            Assert.That(TableDocumentModel.GetCell(table, 0, 5), Is.Null);
        });
    }

    [Test]
    public void RecordsHeaderAndNestedGroupMembershipForRowsAndColumns()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Groups'>"
            + "<table:table-header-columns>"
            + "<table:table-column table:number-columns-repeated='2'/>"
            + "</table:table-header-columns>"
            + "<table:table-column-group><table:table-column-group>"
            + "<table:table-column table:number-columns-repeated='3'/>"
            + "</table:table-column-group></table:table-column-group>"
            + "<table:table-header-rows>"
            + "<table:table-row><table:table-cell/></table:table-row>"
            + "</table:table-header-rows>"
            + "<table:table-row-group><table:table-row-group>"
            + "<table:table-row table:number-rows-repeated='4'>"
            + "<table:table-cell/></table:table-row>"
            + "</table:table-row-group></table:table-row-group>"
            + "</table:table>");
        OdfTable table = document.Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(table.Columns, Has.Length.EqualTo(2));
            Assert.That(table.Columns[0].IsHeader, Is.True);
            Assert.That(table.Columns[0].GroupDepth, Is.Zero);
            Assert.That(table.Columns[1].IsHeader, Is.False);
            Assert.That(table.Columns[1].GroupDepth, Is.EqualTo(2));
            Assert.That(table.ColumnGroups, Has.Length.EqualTo(2));
            Assert.That(table.ColumnGroups[0].StartIndex, Is.EqualTo(2));
            Assert.That(table.ColumnGroups[0].Count, Is.EqualTo(3));
            Assert.That(table.ColumnGroups[0].Depth, Is.EqualTo(1));
            Assert.That(table.ColumnGroups[1].Depth, Is.EqualTo(2));
            Assert.That(TableDocumentModel.GetColumn(table, 4), Is.SameAs(
                table.Columns[1]));
            Assert.That(table.Rows, Has.Length.EqualTo(2));
            Assert.That(table.Rows[0].IsHeader, Is.True);
            Assert.That(table.Rows[1].GroupDepth, Is.EqualTo(2));
            Assert.That(table.RowGroups, Has.Length.EqualTo(2));
            Assert.That(table.RowGroups[0].StartIndex, Is.EqualTo(1));
            Assert.That(table.RowGroups[0].Count, Is.EqualTo(4));
            Assert.That(table.RowGroups[1].Depth, Is.EqualTo(2));
            Assert.That(TableDocumentModel.GetRow(table, 4), Is.SameAs(
                table.Rows[1]));
        });
    }

    [Test]
    public void IndexesNestedTablesWithParentCellOwnershipAndDocumentOrder()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Outer'>"
            + "<table:table-row><table:table-cell>"
            + "<text:p>Before</text:p>"
            + "<table:table table:name='Inner'>"
            + "<table:table-row><table:table-cell/></table:table-row>"
            + "</table:table>"
            + "</table:table-cell></table:table-row>"
            + "</table:table>"
            + "<table:table table:name='Following'>"
            + "<table:table-row><table:table-cell/></table:table-row>"
            + "</table:table>");
        OdfTable outer = document.Tables[0];
        OdfTable inner = document.Tables[1];
        OdfCell parentCell = outer.Rows[0].Cells[0];

        Assert.Multiple(() =>
        {
            Assert.That(
                document.Tables.Select(item => item.Name),
                Is.EqualTo(new[] { "Outer", "Inner", "Following" }));
            Assert.That(parentCell.NestedTables, Is.EqualTo(new[] { inner }));
            Assert.That(inner.ParentTable, Is.SameAs(outer));
            Assert.That(inner.ParentCell, Is.SameAs(parentCell));
            Assert.That(document.Tables[2].ParentTable, Is.Null);
            Assert.That(TableDocumentModel.FindTable(document, "Inner"), Is.SameAs(
                inner));
        });
    }

    [Test]
    public void InfersWidthFromCellsAndNormalizesInvalidRepeatCounts()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Inferred'>"
            + "<table:table-row table:number-rows-repeated='0'>"
            + "<table:table-cell table:number-columns-repeated='-2'/>"
            + "<table:table-cell table:number-columns-repeated='7'/>"
            + "</table:table-row>"
            + "</table:table>");
        OdfTable table = document.Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(table.RowCount, Is.EqualTo(1));
            Assert.That(table.ColumnCount, Is.EqualTo(8));
            Assert.That(table.Rows[0].RepeatCount, Is.EqualTo(1));
            Assert.That(table.Rows[0].Cells[0].RepeatCount, Is.EqualTo(1));
            Assert.That(table.Rows[0].Cells[1].StartColumnIndex, Is.EqualTo(1));
            Assert.That(TableDocumentModel.GetColumn(table, 0), Is.Null);
            Assert.That(TableDocumentModel.GetCell(table, 0, 7), Is.SameAs(
                table.Rows[0].Cells[1]));
        });
    }

    [Test]
    public void RequiresUniqueTableNamesForLookup()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Duplicate'><table:table-row/>"
            + "</table:table>"
            + "<table:table table:name='Duplicate'><table:table-row/>"
            + "</table:table>"
            + "<table:table table:name='Unique'><table:table-row/>"
            + "</table:table>");

        Assert.Multiple(() =>
        {
            Assert.That(TableDocumentModel.FindTable(document, "Duplicate"), Is.Null);
            Assert.That(TableDocumentModel.FindTable(document, "Unique"), Is.SameAs(
                document.Tables[2]));
            Assert.That(document.Tables[2].PartPath, Is.Empty);
        });
    }

    private static OdfDocument ReadSpreadsheet(string tables)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:styles/>"
            + "<office:body><office:spreadsheet>"
            + tables
            + "</office:spreadsheet></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "sparse-table.fods",
            new OdfLoadOptions());
    }
}
