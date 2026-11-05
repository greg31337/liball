using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetMergedCellsTests
{
    [Test]
    public void ReadString_ParsesMergeRangesAndReportsInvalidMergeDefinitions()
    {
        string validXml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<sheetData />" +
            "<mergeCells count=\"2\"><mergeCell ref=\"A1:B2\" /><mergeCell ref=\"D4:E4\" /></mergeCells>" +
            "</worksheet>";
        string invalidXml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<sheetData />" +
            "<mergeCells count=\"3\"><mergeCell ref=\"A1\" /><mergeCell ref=\"B2:C3\" /><mergeCell ref=\"C3:D4\" /></mergeCells>" +
            "</worksheet>";

        XlsxWorksheetPart valid = new XlsxWorksheetPartReader().ReadString(validXml);
        XlsxWorksheetPart invalid = new XlsxWorksheetPartReader().ReadString(invalidXml);

        Assert.That(valid.IsValid, Is.True);
        Assert.That(valid.MergedCells.Count, Is.EqualTo(2));
        Assert.That(valid.MergedCells[0].Reference, Is.EqualTo("A1:B2"));
        Assert.That(valid.MergedCells[0].HasRange, Is.True);
        Assert.That(valid.MergedCells[0].FirstRow, Is.EqualTo(1));
        Assert.That(valid.MergedCells[0].FirstColumn, Is.EqualTo(1));
        Assert.That(valid.MergedCells[0].LastRow, Is.EqualTo(2));
        Assert.That(valid.MergedCells[0].LastColumn, Is.EqualTo(2));
        Assert.That(invalid.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalid, "worksheet.mergeCellSingleCell"), Is.True);
        Assert.That(HasDiagnostic(invalid, "worksheet.mergeCellOverlap"), Is.True);
    }

    [Test]
    public void MergeCells_AddsNormalizedRangesAndRejectsOverlaps()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetMergeCellsResult first = part.MergeCells("C3:B2");
        XlsxWorksheetMergeCellsResult second = part.MergeCellsByCoordinates(5, 1, 5, 2);
        XlsxWorksheetMergeCellsResult overlapping = part.MergeCells("C3:D4");

        Assert.That(first.IsValid, Is.True);
        Assert.That(first.Reference, Is.EqualTo("B2:C3"));
        Assert.That(first.HasMergedCell, Is.True);
        Assert.That(part.MergedCells.Count, Is.EqualTo(2));
        Assert.That(part.MergedCells[0].Reference, Is.EqualTo("B2:C3"));
        Assert.That(part.MergedCells[1].Reference, Is.EqualTo("A5:B5"));
        Assert.That(second.IsValid, Is.True);
        Assert.That(overlapping.IsValid, Is.False);
        Assert.That(HasDiagnostic(overlapping, "worksheet.mergeCellOverlap"), Is.True);
        Assert.That(part.IsCellMerged("C3"), Is.True);
        Assert.That(part.GetMergedCell("B2")!.Reference, Is.EqualTo("B2:C3"));
        Assert.That(part.IsCellMerged("D4"), Is.False);
    }

    [Test]
    public void UnmergeCells_RemovesIntersectingMergeRanges()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.MergeCells("A1:B2");
        part.MergeCells("D4:E5");

        XlsxWorksheetMergeCellsResult singleCellUnmerge = part.UnmergeCells("B2");
        XlsxWorksheetMergeCellsResult rangeUnmerge = part.UnmergeCells("C4:D4");

        Assert.That(singleCellUnmerge.IsValid, Is.True);
        Assert.That(singleCellUnmerge.RemovedCount, Is.EqualTo(1));
        Assert.That(rangeUnmerge.IsValid, Is.True);
        Assert.That(rangeUnmerge.RemovedCount, Is.EqualTo(1));
        Assert.That(part.MergedCells.Count, Is.EqualTo(0));
    }

    [Test]
    public void WriteString_RoundTripsMergedCellsCreatedByOperations()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.MergeCells("A1:B1");
        part.MergeCells("C3:D4");

        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<mergeCells count=\"2\">"));
        Assert.That(writeResult.Xml, Does.Contain("<mergeCell ref=\"A1:B1\" />"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.MergedCells.Count, Is.EqualTo(2));
        Assert.That(roundTrip.MergedCells[1].Reference, Is.EqualTo("C3:D4"));
    }

    [Test]
    public void WriteBytes_RejectsInvalidMergedCellCollections()
    {
        XlsxWorksheetPart nullMergedCells = new XlsxWorksheetPart();
        XlsxWorksheetPart nullMergeCell = new XlsxWorksheetPart();
        XlsxWorksheetPart singleCellMerge = new XlsxWorksheetPart();
        XlsxWorksheetPart overlappingMerges = new XlsxWorksheetPart();

        nullMergedCells.MergedCells = null!;
        nullMergeCell.MergedCells.Add(null!);
        singleCellMerge.MergedCells.Add(CreateMergeCell("A1"));
        overlappingMerges.MergedCells.Add(CreateMergeCell("A1:B2"));
        overlappingMerges.MergedCells.Add(CreateMergeCell("B2:C3"));

        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();
        XlsxWorksheetPartWriteResult nullMergedCellsResult = writer.WriteBytes(nullMergedCells);
        XlsxWorksheetPartWriteResult nullMergeCellResult = writer.WriteBytes(nullMergeCell);
        XlsxWorksheetPartWriteResult singleCellMergeResult = writer.WriteBytes(singleCellMerge);
        XlsxWorksheetPartWriteResult overlappingMergesResult = writer.WriteBytes(overlappingMerges);

        Assert.That(nullMergedCellsResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullMergedCellsResult, "worksheet.mergedCellsNull"), Is.True);
        Assert.That(nullMergeCellResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullMergeCellResult, "worksheet.mergeCellNull"), Is.True);
        Assert.That(singleCellMergeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(singleCellMergeResult, "worksheet.mergeCellSingleCell"), Is.True);
        Assert.That(overlappingMergesResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(overlappingMergesResult, "worksheet.mergeCellOverlap"), Is.True);
    }

    static XlsxWorksheetMergeCell CreateMergeCell(string reference)
    {
        XlsxWorksheetMergeCell mergeCell = new XlsxWorksheetMergeCell();
        mergeCell.Reference = reference;
        return mergeCell;
    }

    static bool HasDiagnostic(XlsxWorksheetPart part, string code)
    {
        foreach (XlsxDiagnostic diagnostic in part.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxWorksheetPartWriteResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static bool HasDiagnostic(XlsxWorksheetMergeCellsResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
