using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DrawingAndMediaCompletionTests
{
    [Test]
    public void DrawingOperations_AuthorTypedAnchorKindsObjectsTextAndHyperlinks()
    {
        XlsxDrawingPart drawing = new XlsxDrawingPart();
        IXlsxDrawingPartOperations operations = new XlsxDrawingPartOperations();

        XlsxDrawingObjectOperationResult textBox = operations.AddTwoCellDrawingObject(drawing, XlsxDrawingObjectKind.TextBox, "B2", "D5", "Notes", "Initial", "");
        XlsxDrawingObjectOperationResult shape = operations.AddOneCellDrawingObject(drawing, XlsxDrawingObjectKind.Shape, "E3", "914400", "457200", "Status", "Ready", "");
        XlsxDrawingObjectOperationResult connector = operations.AddAbsoluteDrawingObject(drawing, XlsxDrawingObjectKind.Connector, "100", "200", "300", "400", "Link", "", "");
        XlsxDrawingObjectOperationResult control = operations.AddOneCellDrawingObject(drawing, XlsxDrawingObjectKind.FormControl, "A8", "200000", "100000", "Check Box", "", "rIdControl1");
        XlsxValidationResult textChange = operations.SetDrawingObjectText(drawing, 0, "Updated notes");
        XlsxValidationResult hyperlink = operations.SetDrawingObjectHyperlink(drawing, 1, "rIdLink1", "Open details");
        XlsxDrawingPartWriteResult write = new XlsxDrawingPartWriter().WriteString(drawing);
        XlsxDrawingPart readBack = new XlsxDrawingPartReader().ReadString(write.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(textBox.IsValid, Is.True);
            Assert.That(shape.IsValid, Is.True);
            Assert.That(connector.IsValid, Is.True);
            Assert.That(control.IsValid, Is.True);
            Assert.That(textChange.IsValid, Is.True);
            Assert.That(hyperlink.IsValid, Is.True);
            Assert.That(write.IsValid, Is.True);
            Assert.That(write.Xml, Does.Contain("<xdr:twoCellAnchor>"));
            Assert.That(write.Xml, Does.Contain("<xdr:oneCellAnchor>"));
            Assert.That(write.Xml, Does.Contain("<xdr:absoluteAnchor>"));
            Assert.That(write.Xml, Does.Contain("<xdr:pos x=\"100\" y=\"200\" />"));
            Assert.That(write.Xml, Does.Contain("Updated notes"));
            Assert.That(write.Xml, Does.Contain("r:id=\"rIdLink1\""));
            Assert.That(write.Xml, Does.Contain("tooltip=\"Open details\""));
            Assert.That(write.Xml, Does.Contain("<xdr:cxnSp>"));
            Assert.That(write.Xml, Does.Contain("<xdr:control r:id=\"rIdControl1\""));
            Assert.That(readBack.RawAnchors.Count, Is.EqualTo(4));
            Assert.That(readBack.TextBoxAnchors[0].AnchorKind, Is.EqualTo(XlsxDrawingAnchorKind.TwoCell));
            Assert.That(readBack.TextBoxAnchors[0].Text, Is.EqualTo("Updated notes"));
            Assert.That(readBack.ShapeAnchors[0].AnchorKind, Is.EqualTo(XlsxDrawingAnchorKind.OneCell));
            Assert.That(readBack.ShapeAnchors[0].Text, Is.EqualTo("Ready"));
            Assert.That(readBack.ShapeAnchors[0].HasHyperlink, Is.True);
            Assert.That(readBack.ConnectorAnchors[0].AnchorKind, Is.EqualTo(XlsxDrawingAnchorKind.Absolute));
            Assert.That(readBack.ConnectorAnchors[0].PositionXEmu, Is.EqualTo("100"));
            Assert.That(readBack.FormControlAnchors[0].RelationshipId, Is.EqualTo("rIdControl1"));
        });
    }

    [Test]
    public void DrawingReaderWriter_PreservesDiagramAndSupportsAbsoluteImages()
    {
        string diagramXml = "<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><xdr:absoluteAnchor><xdr:pos x=\"10\" y=\"20\"/><xdr:ext cx=\"30\" cy=\"40\"/><xdr:graphicFrame><xdr:nvGraphicFramePr><xdr:cNvPr id=\"7\" name=\"SmartArt\"/><xdr:cNvGraphicFramePr/></xdr:nvGraphicFramePr><a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\"><dgm:relIds r:dm=\"rIdData\" r:lo=\"rIdLayout\"/></a:graphicData></a:graphic></xdr:graphicFrame><xdr:clientData/></xdr:absoluteAnchor></xdr:wsDr>";
        XlsxDrawingPart diagram = new XlsxDrawingPartReader().ReadString(diagramXml);
        diagram.RawXml = "";
        XlsxDrawingPartWriteResult diagramWrite = new XlsxDrawingPartWriter().WriteString(diagram);

        XlsxDrawingPart images = new XlsxDrawingPart();
        IXlsxDrawingPartOperations operations = new XlsxDrawingPartOperations();
        XlsxDrawingImagePlacementResult placement = operations.AddAbsoluteImage(images, "rIdImage1", "1000", "2000", "3000", "4000", "Logo", "Absolute logo");
        XlsxDrawingPartWriteResult imageWrite = new XlsxDrawingPartWriter().WriteString(images);
        XlsxDrawingPart imageReadBack = new XlsxDrawingPartReader().ReadString(imageWrite.Xml);

        Assert.Multiple(() =>
        {
            Assert.That(diagram.IsValid, Is.True);
            Assert.That(diagram.DiagramAnchors.Count, Is.EqualTo(1));
            Assert.That(diagram.DiagramAnchors[0].ObjectKind, Is.EqualTo(XlsxDrawingObjectKind.Diagram));
            Assert.That(diagram.DiagramAnchors[0].AnchorKind, Is.EqualTo(XlsxDrawingAnchorKind.Absolute));
            Assert.That(diagramWrite.IsValid, Is.True);
            Assert.That(diagramWrite.Xml, Does.Contain("dgm:relIds"));
            Assert.That(diagramWrite.Xml, Does.Contain("r:dm=\"rIdData\""));
            Assert.That(placement.IsValid, Is.True);
            Assert.That(placement.Anchor!.AnchorKind, Is.EqualTo(XlsxDrawingAnchorKind.Absolute));
            Assert.That(imageWrite.IsValid, Is.True);
            Assert.That(imageWrite.Xml, Does.Contain("<xdr:absoluteAnchor>"));
            Assert.That(imageWrite.Xml, Does.Contain("<xdr:pos x=\"1000\" y=\"2000\" />"));
            Assert.That(imageReadBack.ImageAnchors[0].AnchorKind, Is.EqualTo(XlsxDrawingAnchorKind.Absolute));
            Assert.That(imageReadBack.ImageAnchors[0].ExtentCyEmu, Is.EqualTo("4000"));
        });
    }

    [Test]
    public void RawImageAuthoring_SupportsEveryPlannedFormatWithoutDecoding()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();
        XlsxRawImageInsertResult png = operations.AddRawImage(workbook, PngBytes(), "image/png");
        XlsxRawImageInsertResult jpeg = operations.AddRawImage(workbook, JpegBytes(), "image/jpeg");
        XlsxRawImageInsertResult gif = operations.AddRawImage(workbook, GifBytes(), "image/gif");
        XlsxRawImageInsertResult bmp = operations.AddRawImage(workbook, BmpBytes(), "image/bmp");
        XlsxRawImageInsertResult svg = operations.AddRawImage(workbook, SvgBytes("red"), "image/svg+xml");
        XlsxRawImageInsertResult webp = operations.AddRawImage(workbook, WebpBytes(), "image/webp");
        XlsxRawImageInsertResult emf = operations.AddRawImage(workbook, EmfBytes(), "image/x-emf");
        XlsxRawImageInsertResult wmf = operations.AddRawImage(workbook, WmfBytes(), "image/x-wmf");
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(png.IsValid, Is.True);
            Assert.That(jpeg.IsValid, Is.True);
            Assert.That(gif.IsValid, Is.True);
            Assert.That(bmp.IsValid, Is.True);
            Assert.That(svg.IsValid, Is.True);
            Assert.That(webp.IsValid, Is.True);
            Assert.That(emf.IsValid, Is.True);
            Assert.That(wmf.IsValid, Is.True);
            Assert.That(svg.ImagePart!.Format, Is.EqualTo("svg"));
            Assert.That(emf.ImagePart!.Format, Is.EqualTo("emf"));
            Assert.That(wmf.ImagePart!.Format, Is.EqualTo("wmf"));
            Assert.That(svg.PartPath, Does.EndWith(".svg"));
            Assert.That(emf.PartPath, Does.EndWith(".emf"));
            Assert.That(wmf.PartPath, Does.EndWith(".wmf"));
            Assert.That(save.IsValid, Is.True);
            Assert.That(package.GetEntry(svg.PartPath)!.Data, Is.EqualTo(SvgBytes("red")));
            Assert.That(package.GetEntry(emf.PartPath)!.Data, Is.EqualTo(EmfBytes()));
            Assert.That(package.GetEntry(wmf.PartPath)!.Data, Is.EqualTo(WmfBytes()));
        });
    }

    [Test]
    public void ReplaceRawImagePart_UpdatesExistingBytesAndTypedAggregate()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        IXlsxWorkbookOperations operations = new XlsxWorkbookOperations();
        XlsxRawImageInsertResult added = operations.AddRawImage(workbook, SvgBytes("red"), "image/svg+xml");
        XlsxRawImageInsertResult replaced = operations.ReplaceRawImagePart(workbook, added.PartPath, SvgBytes("blue"), "image/svg+xml");
        XlsxRawImageInsertResult missing = operations.ReplaceRawImagePart(workbook, "/xl/media/missing.svg", SvgBytes("blue"), "image/svg+xml");
        XlsxWorkbookSaveResult save = workbook.SaveBytes();
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(save.Bytes, new XlsxReaderOptions());

        Assert.Multiple(() =>
        {
            Assert.That(replaced.IsValid, Is.True);
            Assert.That(replaced.ImagePart!.Data, Is.EqualTo(SvgBytes("blue")));
            Assert.That(workbook.Images.Count, Is.EqualTo(1));
            Assert.That(workbook.Images[0].Data, Is.EqualTo(SvgBytes("blue")));
            Assert.That(missing.IsValid, Is.False);
            Assert.That(HasDiagnostic(missing, "image.partNotFound"), Is.True);
            Assert.That(save.IsValid, Is.True);
            Assert.That(package.GetEntry(added.PartPath)!.Data, Is.EqualTo(SvgBytes("blue")));
        });
    }

    static byte[] PngBytes() => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
    static byte[] JpegBytes() => new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
    static byte[] GifBytes() => new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };
    static byte[] BmpBytes() => new byte[] { 0x42, 0x4D, 0x00, 0x00 };
    static byte[] WebpBytes() => new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 };
    static byte[] SvgBytes(string color) => Encoding.UTF8.GetBytes("<svg xmlns=\"http://www.w3.org/2000/svg\"><rect fill=\"" + color + "\"/></svg>");

    static byte[] EmfBytes()
    {
        byte[] bytes = new byte[44];
        bytes[0] = 0x01;
        bytes[40] = 0x20;
        bytes[41] = 0x45;
        bytes[42] = 0x4D;
        bytes[43] = 0x46;
        return bytes;
    }

    static byte[] WmfBytes() => new byte[] { 0xD7, 0xCD, 0xC6, 0x9A, 0, 0 };

    static bool HasDiagnostic(XlsxRawImageInsertResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code) { return true; }
        }
        return false;
    }
}
