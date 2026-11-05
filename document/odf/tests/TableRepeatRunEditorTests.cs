using System.Text;

namespace odf.Tests;

public class TableRepeatRunEditorTests
{
    [Test]
    public void IsolatesOneCellAndRowFromLargeRepeatedRuns()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Cells'>"
            + "<table:table-row table:number-rows-repeated='100'>"
            + "<table:covered-table-cell table:number-columns-repeated='10'/>"
            + "</table:table-row></table:table>");
        OdfTable table = document.Tables[0];

        OdfCell target = TableRepeatRunEditor.SplitCellForEdit(
            table,
            42,
            7);
        StyleDocumentModel.WriteAttribute(
            target.Node!,
            OdfNamespaces.Table,
            "style-name",
            OdfNamespaces.TablePrefix,
            "Changed");
        TableRepeatRunEditor.CoalesceTableRuns(table);
        OdfDocument reread = WriteAndRead(document);
        OdfTable rereadTable = reread.Tables[0];
        OdfCell rereadTarget = TableDocumentModel.GetCell(
            rereadTable,
            42,
            7)!;

        Assert.Multiple(() =>
        {
            Assert.That(table.Rows, Has.Length.EqualTo(3));
            Assert.That(table.Rows[0].RepeatCount, Is.EqualTo(42));
            Assert.That(table.Rows[1].RepeatCount, Is.EqualTo(1));
            Assert.That(table.Rows[2].RepeatCount, Is.EqualTo(57));
            Assert.That(table.Rows[1].Cells, Has.Length.EqualTo(3));
            Assert.That(table.Rows[1].Cells[0].RepeatCount, Is.EqualTo(7));
            Assert.That(table.Rows[1].Cells[1], Is.SameAs(target));
            Assert.That(table.Rows[1].Cells[2].RepeatCount, Is.EqualTo(2));
            Assert.That(target.IsCovered, Is.True);
            Assert.That(rereadTable.Rows, Has.Length.EqualTo(3));
            Assert.That(rereadTarget.IsCovered, Is.True);
            Assert.That(
                AttributeValue(rereadTarget.Node!, OdfNamespaces.Table, "style-name"),
                Is.EqualTo("Changed"));
            Assert.That(TableDocumentModel.GetCell(rereadTable, 42, 6)!.Node,
                Is.Not.SameAs(rereadTarget.Node));
        });
    }

    [Test]
    public void IsolatesOneRowAndPreservesTheSurroundingRepeatRuns()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Rows'>"
            + "<table:table-row table:number-rows-repeated='6'>"
            + "<table:table-cell/></table:table-row>"
            + "</table:table>");
        OdfTable table = document.Tables[0];

        OdfRow target = TableRepeatRunEditor.SplitRowForEdit(table, 3);
        StyleDocumentModel.WriteAttribute(
            target.Node!,
            OdfNamespaces.Table,
            "style-name",
            OdfNamespaces.TablePrefix,
            "SelectedRow");
        TableRepeatRunEditor.CoalesceTableRuns(table);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(table.Rows, Has.Length.EqualTo(3));
            Assert.That(
                table.Rows.Select(item => item.RepeatCount),
                Is.EqualTo(new long[] { 3, 1, 2 }));
            Assert.That(TableDocumentModel.GetRow(table, 3), Is.SameAs(target));
            Assert.That(reread.Tables[0].Rows, Has.Length.EqualTo(3));
            Assert.That(
                AttributeValue(
                    TableDocumentModel.GetRow(reread.Tables[0], 3)!.Node!,
                    OdfNamespaces.Table,
                    "style-name"),
                Is.EqualTo("SelectedRow"));
        });
    }

    [Test]
    public void IsolatesOneDeclaredColumnAndPreservesColumnCount()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Columns'>"
            + "<table:table-column table:number-columns-repeated='8'/>"
            + "<table:table-row><table:table-cell "
            + "table:number-columns-repeated='8'/></table:table-row>"
            + "</table:table>");
        OdfTable table = document.Tables[0];

        OdfColumn target = TableRepeatRunEditor.SplitColumnForEdit(table, 5);
        StyleDocumentModel.WriteAttribute(
            target.Node!,
            OdfNamespaces.Table,
            "style-name",
            OdfNamespaces.TablePrefix,
            "SelectedColumn");
        TableRepeatRunEditor.CoalesceTableRuns(table);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(table.ColumnCount, Is.EqualTo(8));
            Assert.That(table.Columns, Has.Length.EqualTo(3));
            Assert.That(
                table.Columns.Select(item => item.RepeatCount),
                Is.EqualTo(new long[] { 5, 1, 2 }));
            Assert.That(TableDocumentModel.GetColumn(table, 5), Is.SameAs(target));
            Assert.That(reread.Tables[0].Columns, Has.Length.EqualTo(3));
            Assert.That(
                AttributeValue(
                    TableDocumentModel.GetColumn(reread.Tables[0], 5)!.Node!,
                    OdfNamespaces.Table,
                    "style-name"),
                Is.EqualTo("SelectedColumn"));
        });
    }

    [Test]
    public void CoalescesUnchangedSplitRunsBackToTheirOriginalCompactForm()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Compact'>"
            + "<table:table-column table:number-columns-repeated='10'/>"
            + "<table:table-row table:number-rows-repeated='20'>"
            + "<table:table-cell table:number-columns-repeated='10'/>"
            + "</table:table-row></table:table>");
        OdfTable table = document.Tables[0];

        TableRepeatRunEditor.SplitCellForEdit(table, 9, 4);
        TableRepeatRunEditor.SplitColumnForEdit(table, 4);
        TableRepeatRunEditor.CoalesceTableRuns(table);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(table.Rows, Has.Length.EqualTo(1));
            Assert.That(table.Rows[0].RepeatCount, Is.EqualTo(20));
            Assert.That(table.Rows[0].Cells, Has.Length.EqualTo(1));
            Assert.That(table.Rows[0].Cells[0].RepeatCount, Is.EqualTo(10));
            Assert.That(table.Columns, Has.Length.EqualTo(1));
            Assert.That(table.Columns[0].RepeatCount, Is.EqualTo(10));
            Assert.That(reread.Tables[0].Rows, Has.Length.EqualTo(1));
            Assert.That(reread.Tables[0].Columns, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CoalescesEquivalentAdjacentRunsAcrossFormattingWhitespace()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Adjacent'>"
            + "<table:table-column table:number-columns-repeated='2'/>\n"
            + "<table:table-column table:number-columns-repeated='3'/>"
            + "<table:table-row>"
            + "<table:table-cell/>\n<table:table-cell/>"
            + "</table:table-row>\n"
            + "<table:table-row>"
            + "<table:table-cell/>\n<table:table-cell/>"
            + "</table:table-row>"
            + "</table:table>");
        OdfTable table = document.Tables[0];

        TableRepeatRunEditor.CoalesceTableRuns(table);
        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(table.Columns, Has.Length.EqualTo(1));
            Assert.That(table.Columns[0].RepeatCount, Is.EqualTo(5));
            Assert.That(table.Rows, Has.Length.EqualTo(1));
            Assert.That(table.Rows[0].RepeatCount, Is.EqualTo(2));
            Assert.That(table.Rows[0].Cells, Has.Length.EqualTo(1));
            Assert.That(table.Rows[0].Cells[0].RepeatCount, Is.EqualTo(2));
            Assert.That(reread.Tables[0].Columns, Has.Length.EqualTo(1));
            Assert.That(reread.Tables[0].Rows, Has.Length.EqualTo(1));
            Assert.That(reread.Tables[0].Rows[0].Cells, Has.Length.EqualTo(1));
        });
    }

    private static string AttributeValue(
        OdfNode node,
        string namespaceName,
        string localName)
    {
        OdfAttribute? attribute = node.Attributes.SingleOrDefault(
            item => item.Name.NamespaceName == namespaceName
                && item.Name.LocalName == localName);
        return attribute == null ? "" : attribute.Value;
    }

    private static OdfDocument ReadSpreadsheet(string tables)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:styles/>"
            + "<office:body><office:spreadsheet>"
            + tables
            + "</office:spreadsheet></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "repeat-runs.fods",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "repeat-runs.fods",
            new OdfLoadOptions());
    }
}
