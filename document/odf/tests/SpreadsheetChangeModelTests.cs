using System.Text;

namespace odf.Tests;

public class SpreadsheetChangeModelTests
{
    [Test]
    public void MaterializesDetectiveMetadataAndEveryTrackedChangeKind()
    {
        OdfDocument document = ReadSpreadsheet(ChangeXml());
        OdfDetectiveRange range = document.DetectiveRanges[0];
        OdfDetectiveOperation operation = document.DetectiveOperations[0];
        OdfSpreadsheetChange cellChange = document.SpreadsheetChanges[0];
        OdfSpreadsheetChange movement = document.SpreadsheetChanges[3];

        Assert.Multiple(() =>
        {
            Assert.That(document.TrackSpreadsheetChanges, Is.True);
            Assert.That(document.DetectiveRanges, Has.Length.EqualTo(1));
            Assert.That(range.RangeAddress, Is.EqualTo("Sheet1.A2:Sheet1.B4"));
            Assert.That(range.Direction, Is.EqualTo("from-same-table"));
            Assert.That(range.ContainsError, Is.True);
            Assert.That(range.Cell, Is.Not.Null);
            Assert.That(document.DetectiveOperations, Has.Length.EqualTo(1));
            Assert.That(operation.Name, Is.EqualTo("trace-precedents"));
            Assert.That(operation.Index, Is.EqualTo(2));
            Assert.That(operation.Cell, Is.SameAs(range.Cell));
            Assert.That(document.SpreadsheetChanges, Has.Length.EqualTo(4));
            Assert.That(cellChange.Kind, Is.EqualTo("cell-content"));
            Assert.That(cellChange.Id, Is.EqualTo("c1"));
            Assert.That(cellChange.AcceptanceState, Is.EqualTo("pending"));
            Assert.That(cellChange.Creator, Is.EqualTo("Alice"));
            Assert.That(cellChange.Comment, Is.EqualTo("Changed a value"));
            Assert.That(cellChange.CellColumn, Is.EqualTo(0));
            Assert.That(cellChange.CellRow, Is.EqualTo(1));
            Assert.That(document.SpreadsheetChanges[1].Kind,
                Is.EqualTo("insertion"));
            Assert.That(document.SpreadsheetChanges[1].Count, Is.EqualTo(2));
            Assert.That(document.SpreadsheetChanges[2].Kind,
                Is.EqualTo("deletion"));
            Assert.That(movement.Kind, Is.EqualTo("movement"));
            Assert.That(movement.SourceStartRow, Is.EqualTo(0));
            Assert.That(movement.SourceEndRow, Is.EqualTo(3));
            Assert.That(movement.TargetStartColumn, Is.EqualTo(2));
            Assert.That(movement.TargetEndRow, Is.EqualTo(2));
        });
    }

    [Test]
    public void SynchronizesDetectiveAndTrackedChangeEditsAdditionsAndRemovals()
    {
        OdfDocument document = ReadSpreadsheet(ChangeXml());
        document.TrackSpreadsheetChanges = false;
        document.DetectiveRanges[0].Direction = "to-another-table";
        document.DetectiveRanges[0].ContainsError = false;
        OdfDetectiveOperation replacement = new OdfDetectiveOperation();
        replacement.Cell = document.DetectiveRanges[0].Cell;
        replacement.Name = "trace-dependents";
        replacement.Index = 5;
        document.DetectiveOperations = [replacement];
        OdfSpreadsheetChange cellChange = document.SpreadsheetChanges[0];
        cellChange.AcceptanceState = "accepted";
        cellChange.Comment = "Reviewed";
        OdfSpreadsheetChange movement = document.SpreadsheetChanges[3];
        movement.TargetEndRow = 4;
        OdfSpreadsheetChange added = new OdfSpreadsheetChange();
        added.Id = "c5";
        added.Kind = "insertion";
        added.Creator = "Bob";
        added.Date = "2026-09-02T10:30:00";
        added.Comment = "Added a column";
        added.Axis = "column";
        added.Position = 2;
        added.Count = 1;
        added.TableIndex = 0;
        document.SpreadsheetChanges = [cellChange, movement, added];

        OdfDocument reread = WriteAndRead(document);
        OdfSpreadsheetChange reviewed = reread.SpreadsheetChanges.Single(
            item => item.Id == "c1");
        OdfSpreadsheetChange inserted = reread.SpreadsheetChanges.Single(
            item => item.Id == "c5");

        Assert.Multiple(() =>
        {
            Assert.That(reread.TrackSpreadsheetChanges, Is.False);
            Assert.That(reread.DetectiveRanges[0].Direction,
                Is.EqualTo("to-another-table"));
            Assert.That(reread.DetectiveRanges[0].ContainsError, Is.False);
            Assert.That(reread.DetectiveOperations, Has.Length.EqualTo(1));
            Assert.That(reread.DetectiveOperations[0].Name,
                Is.EqualTo("trace-dependents"));
            Assert.That(reread.DetectiveOperations[0].Index, Is.EqualTo(5));
            Assert.That(reread.SpreadsheetChanges, Has.Length.EqualTo(3));
            Assert.That(reviewed.AcceptanceState, Is.EqualTo("accepted"));
            Assert.That(reviewed.Comment, Is.EqualTo("Reviewed"));
            Assert.That(reread.SpreadsheetChanges.Any(item => item.Id == "c2"),
                Is.False);
            Assert.That(reread.SpreadsheetChanges.Any(item => item.Id == "c3"),
                Is.False);
            Assert.That(inserted.Axis, Is.EqualTo("column"));
            Assert.That(inserted.Creator, Is.EqualTo("Bob"));
            Assert.That(inserted.Comment, Is.EqualTo("Added a column"));
            Assert.That(reread.SpreadsheetChanges.Single(
                item => item.Id == "c4").TargetEndRow, Is.EqualTo(4));
        });
    }

    [Test]
    public void StructuralEditsUpdateDetectiveAndTrackedNumericRanges()
    {
        OdfDocument document = ReadSpreadsheet(ChangeXml());

        TableStructureEditor.InsertRows(
            document,
            document.Tables[0],
            1,
            2);
        OdfDocument reread = WriteAndRead(document);
        OdfSpreadsheetChange cellChange = document.SpreadsheetChanges.Single(
            item => item.Id == "c1");
        OdfSpreadsheetChange movement = document.SpreadsheetChanges.Single(
            item => item.Id == "c4");

        Assert.Multiple(() =>
        {
            Assert.That(document.DetectiveRanges[0].RangeAddress,
                Is.EqualTo("Sheet1.A4:Sheet1.B6"));
            Assert.That(cellChange.CellRow, Is.EqualTo(3));
            Assert.That(movement.SourceStartRow, Is.EqualTo(0));
            Assert.That(movement.SourceEndRow, Is.EqualTo(5));
            Assert.That(movement.TargetStartRow, Is.EqualTo(3));
            Assert.That(movement.TargetEndRow, Is.EqualTo(4));
            Assert.That(reread.DetectiveRanges[0].RangeAddress,
                Is.EqualTo("Sheet1.A4:Sheet1.B6"));
            Assert.That(reread.SpreadsheetChanges.Single(
                item => item.Id == "c1").CellRow, Is.EqualTo(3));
        });
    }

    [Test]
    public void RejectsNegativeDetectiveOperationIndex()
    {
        OdfDocument document = ReadSpreadsheet(ChangeXml());
        document.DetectiveOperations[0].Index = -1;

        Assert.That(
            () => new OdfDocumentWriter().WriteDocumentBytes(
                document,
                new OdfSaveOptions()),
            Throws.TypeOf<OdfEditException>());
    }

    private static string ChangeXml()
    {
        string info = "<office:change-info><dc:creator>Alice</dc:creator>"
            + "<dc:date>2026-09-01T12:00:00</dc:date></office:change-info>";
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:dc='" + OdfNamespaces.DublinCore + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:body><office:spreadsheet>"
            + "<table:tracked-changes table:track-changes='true'>"
            + "<table:cell-content-change table:id='c1'>"
            + "<table:cell-address table:column='0' table:row='1' table:table='0'/>"
            + "<office:change-info><dc:creator>Alice</dc:creator>"
            + "<dc:date>2026-09-01T12:00:00</dc:date>"
            + "<text:p>Changed a value</text:p></office:change-info>"
            + "<table:previous><table:table-cell/></table:previous>"
            + "</table:cell-content-change>"
            + "<table:insertion table:id='c2' table:type='row' table:position='1' "
            + "table:count='2' table:table='0'>" + info + "</table:insertion>"
            + "<table:deletion table:id='c3' table:type='column' table:position='2' "
            + "table:table='0' table:multi-deletion-spanned='1'>"
            + info + "</table:deletion>"
            + "<table:movement table:id='c4'>"
            + "<table:source-range-address table:start-column='0' table:start-row='0' "
            + "table:start-table='0' table:end-column='1' table:end-row='3' "
            + "table:end-table='0'/><table:target-range-address "
            + "table:start-column='2' table:start-row='1' table:start-table='0' "
            + "table:end-column='3' table:end-row='2' table:end-table='0'/>"
            + info + "</table:movement></table:tracked-changes>"
            + "<table:table table:name='Sheet1'><table:table-row "
            + "table:number-rows-repeated='5'><table:table-cell "
            + "table:number-columns-repeated='5'><table:detective>"
            + "<table:highlighted-range table:cell-range-address='Sheet1.A2:Sheet1.B4' "
            + "table:direction='from-same-table' table:contains-error='true'/>"
            + "<table:operation table:name='trace-precedents' table:index='2'/>"
            + "</table:detective></table:table-cell></table:table-row></table:table>"
            + "</office:spreadsheet></office:body></office:document>";
    }

    private static OdfDocument ReadSpreadsheet(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "changes.fods",
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
        return ReadSpreadsheet(Encoding.UTF8.GetString(bytes));
    }
}
