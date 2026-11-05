using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DrawingAnchorImagePlacementTests
{
    [Test]
    public void DrawingPartApis_AddTwoCellAndOneCellImageAnchorsAndRoundTrip()
    {
        XlsxDrawingPart drawing = new XlsxDrawingPart();
        XlsxDrawingImagePlacementResult twoCell = drawing.AddTwoCellImage("rIdImage1", "B2", "D6", "Logo", "Company logo");
        XlsxDrawingImagePlacementResult oneCell = drawing.AddOneCellImage("rIdImage2", "E5", "914400", "457200", "Badge", "Badge image");
        XlsxDrawingPartWriteResult write = new XlsxDrawingPartWriter().WriteString(drawing);
        XlsxDrawingPart readBack = new XlsxDrawingPartReader().ReadString(write.Xml);

        Assert.That(twoCell.IsValid, Is.True);
        Assert.That(oneCell.IsValid, Is.True);
        Assert.That(drawing.ImageAnchors.Count, Is.EqualTo(2));
        Assert.That(twoCell.Anchor!.From.Column, Is.EqualTo(1));
        Assert.That(twoCell.Anchor.From.Row, Is.EqualTo(1));
        Assert.That(twoCell.Anchor.To.Column, Is.EqualTo(3));
        Assert.That(twoCell.Anchor.To.Row, Is.EqualTo(5));
        Assert.That(oneCell.Anchor!.HasExtent, Is.True);
        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("<xdr:twoCellAnchor>"));
        Assert.That(write.Xml, Does.Contain("<xdr:oneCellAnchor>"));
        Assert.That(write.Xml, Does.Contain("name=\"Logo\""));
        Assert.That(write.Xml, Does.Contain("descr=\"Company logo\""));
        Assert.That(write.Xml, Does.Contain("r:embed=\"rIdImage1\""));
        Assert.That(write.Xml, Does.Contain("<xdr:ext cx=\"914400\" cy=\"457200\" />"));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.ImageAnchors.Count, Is.EqualTo(2));
        Assert.That(readBack.ImageAnchors[0].RelationshipId, Is.EqualTo("rIdImage1"));
        Assert.That(readBack.ImageAnchors[0].From.Column, Is.EqualTo(1));
        Assert.That(readBack.ImageAnchors[0].To.Row, Is.EqualTo(5));
        Assert.That(readBack.ImageAnchors[1].RelationshipId, Is.EqualTo("rIdImage2"));
        Assert.That(readBack.ImageAnchors[1].ExtentCxEmu, Is.EqualTo("914400"));
    }

    [Test]
    public void WorkbookAddWorksheetImage_CreatesWorksheetDrawingImageAndRelationshipParts()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxDrawingImagePlacementResult placement = workbook.AddWorksheetImage(worksheet, PngBytes(), "", "B2", "D5", "Logo");
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.That(placement.IsValid, Is.True);
        Assert.That(placement.HasImagePart, Is.True);
        Assert.That(placement.HasDrawingPart, Is.True);
        Assert.That(placement.HasWorksheetRelationships, Is.True);
        Assert.That(placement.HasDrawingRelationships, Is.True);
        Assert.That(worksheet.HasDrawing, Is.True);
        Assert.That(worksheet.Drawing.RelationshipId, Is.EqualTo("rIdDrawing1"));
        Assert.That(save.IsValid, Is.True);
        Assert.That(package.IsValid, Is.True);
        AssertEntryText(package, XlsxPackagePaths.Worksheet1, "<drawing r:id=\"rIdDrawing1\" />");
        AssertEntryText(package, "/xl/worksheets/_rels/sheet1.xml.rels", "Type=\"" + XlsxRelationshipTypes.Drawing + "\"");
        AssertEntryText(package, "/xl/worksheets/_rels/sheet1.xml.rels", "Target=\"../drawings/drawing1.xml\"");
        AssertEntryText(package, "/xl/drawings/drawing1.xml", "<xdr:twoCellAnchor>");
        AssertEntryText(package, "/xl/drawings/drawing1.xml", "r:embed=\"rIdImage1\"");
        AssertEntryText(package, "/xl/drawings/_rels/drawing1.xml.rels", "Type=\"" + XlsxRelationshipTypes.Image + "\"");
        AssertEntryText(package, "/xl/drawings/_rels/drawing1.xml.rels", "Target=\"../media/image1.png\"");
        Assert.That(package.GetEntry("/xl/media/image1.png")!.Data, Is.EqualTo(PngBytes()));
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, XlsxContentTypes.Drawing);
        AssertEntryText(package, XlsxPackagePaths.ContentTypes, "image/png");
    }

    [Test]
    public void DrawingPlacementApis_ReportInvalidStates()
    {
        XlsxDrawingPart drawing = new XlsxDrawingPart();
        XlsxDrawingImagePlacementResult invalidCell = drawing.AddTwoCellImage("rIdImage1", "XFE1", "B2", "Bad", "");
        XlsxDrawingPart missingRelationship = new XlsxDrawingPart();
        XlsxDrawingImageAnchor anchor = new XlsxDrawingImageAnchor();
        missingRelationship.ImageAnchors.Add(anchor);
        XlsxDrawingPartWriteResult missingRelationshipWrite = new XlsxDrawingPartWriter().WriteString(missingRelationship);
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        XlsxDrawingImagePlacementResult? nullWorksheet = workbook.AddWorksheetImage(null!, PngBytes(), "", "A1", "B2", "Bad");
        XlsxDrawingImagePlacementResult invalidImage = workbook.AddWorksheetImage(new XlsxWorksheetPart(), UnsupportedBytes(), "", "A1", "B2", "Bad");

        Assert.That(invalidCell.IsValid, Is.False);
        Assert.That(missingRelationshipWrite.IsValid, Is.False);
        Assert.That(nullWorksheet.IsValid, Is.False);
        Assert.That(invalidImage.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidCell, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(missingRelationshipWrite, "drawingWriter.imageRelationshipIdMissing"), Is.True);
        Assert.That(HasDiagnostic(nullWorksheet, "worksheetImage.worksheetNull"), Is.True);
        Assert.That(HasDiagnostic(invalidImage, "image.formatUnsupported"), Is.True);
    }

    static byte[] PngBytes()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
    }

    static byte[] UnsupportedBytes()
    {
        return new byte[] { 0x00, 0x01, 0x02, 0x03 };
    }

    static void AssertEntryText(XlsxZipPackageReadResult package, string path, string expected)
    {
        XlsxPackageEntry? entry = package.GetEntry(path);
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.GetText(), Does.Contain(expected));
    }

    static bool HasDiagnostic(XlsxDrawingImagePlacementResult result, string code)
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

    static bool HasDiagnostic(XlsxDrawingPartWriteResult result, string code)
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
