using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DrawingObjectPreservationTests
{
    [Test]
    public void DrawingPartReaderWriter_PreservesTextBoxesShapesConnectorsAndFormControls()
    {
        string xml = DrawingXml();
        XlsxDrawingPart part = new XlsxDrawingPartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.ImageAnchors.Count, Is.EqualTo(1));
        Assert.That(part.TextBoxAnchors.Count, Is.EqualTo(1));
        Assert.That(part.ShapeAnchors.Count, Is.EqualTo(1));
        Assert.That(part.ConnectorAnchors.Count, Is.EqualTo(1));
        Assert.That(part.FormControlAnchors.Count, Is.EqualTo(1));
        Assert.That(part.RawAnchors.Count, Is.EqualTo(4));
        Assert.That(part.TextBoxAnchors[0].Name, Is.EqualTo("TextBox 1"));
        Assert.That(part.TextBoxAnchors[0].RawXml, Does.Contain("Hello text box"));
        Assert.That(part.ShapeAnchors[0].Name, Is.EqualTo("Shape 1"));
        Assert.That(part.ConnectorAnchors[0].Kind, Is.EqualTo("connector"));
        Assert.That(part.FormControlAnchors[0].Name, Is.EqualTo("Check Box 1"));
        Assert.That(part.FormControlAnchors[0].RelationshipId, Is.EqualTo("rIdControl1"));

        part.RawXml = "";
        XlsxDrawingPartWriteResult write = new XlsxDrawingPartWriter().WriteString(part);
        XlsxDrawingPart readBack = new XlsxDrawingPartReader().ReadString(write.Xml);

        Assert.That(write.IsValid, Is.True);
        Assert.That(write.Xml, Does.Contain("TextBox 1"));
        Assert.That(write.Xml, Does.Contain("Hello text box"));
        Assert.That(write.Xml, Does.Contain("Shape 1"));
        Assert.That(write.Xml, Does.Contain("<xdr:cxnSp>"));
        Assert.That(write.Xml, Does.Contain("<xdr:control"));
        Assert.That(write.Xml, Does.Contain("r:id=\"rIdControl1\""));
        Assert.That(write.Xml, Does.Contain("r:embed=\"rIdImage1\""));
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.ImageAnchors.Count, Is.EqualTo(1));
        Assert.That(readBack.TextBoxAnchors.Count, Is.EqualTo(1));
        Assert.That(readBack.ShapeAnchors.Count, Is.EqualTo(1));
        Assert.That(readBack.ConnectorAnchors.Count, Is.EqualTo(1));
        Assert.That(readBack.FormControlAnchors.Count, Is.EqualTo(1));
    }

    [Test]
    public void DrawingPartWriter_ReportsInvalidRawAnchorListsAndEntries()
    {
        XlsxDrawingPart nullTextBoxes = new XlsxDrawingPart();
        nullTextBoxes.TextBoxAnchors = null!;
        XlsxDrawingPartWriteResult nullTextBoxesWrite = new XlsxDrawingPartWriter().WriteString(nullTextBoxes);

        XlsxDrawingPart invalidAnchors = new XlsxDrawingPart();
        invalidAnchors.ShapeAnchors.Add(new XlsxDrawingRawAnchor());
        invalidAnchors.ConnectorAnchors.Add(null!);
        XlsxDrawingPartWriteResult invalidAnchorsWrite = new XlsxDrawingPartWriter().WriteString(invalidAnchors);

        Assert.That(nullTextBoxesWrite.IsValid, Is.False);
        Assert.That(invalidAnchorsWrite.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullTextBoxesWrite, "drawingWriter.textBoxAnchorsNull"), Is.True);
        Assert.That(HasDiagnostic(invalidAnchorsWrite, "drawingWriter.rawAnchorXmlMissing"), Is.True);
        Assert.That(HasDiagnostic(invalidAnchorsWrite, "drawingWriter.rawAnchorNull"), Is.True);
    }

    static string DrawingXml()
    {
        return "<xdr:wsDr xmlns:xdr=\"http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">" +
            "<xdr:twoCellAnchor editAs=\"oneCell\">" +
            Marker("from", "0", "0") +
            Marker("to", "1", "1") +
            "<xdr:sp><xdr:nvSpPr><xdr:cNvPr id=\"2\" name=\"TextBox 1\" /><xdr:cNvSpPr /></xdr:nvSpPr><xdr:spPr /><xdr:txBody><a:bodyPr /><a:p><a:r><a:t>Hello text box</a:t></a:r></a:p></xdr:txBody></xdr:sp><xdr:clientData />" +
            "</xdr:twoCellAnchor>" +
            "<xdr:twoCellAnchor>" +
            Marker("from", "2", "0") +
            Marker("to", "3", "1") +
            "<xdr:sp><xdr:nvSpPr><xdr:cNvPr id=\"3\" name=\"Shape 1\" /><xdr:cNvSpPr /></xdr:nvSpPr><xdr:spPr><a:prstGeom prst=\"rect\"><a:avLst /></a:prstGeom></xdr:spPr></xdr:sp><xdr:clientData />" +
            "</xdr:twoCellAnchor>" +
            "<xdr:twoCellAnchor>" +
            Marker("from", "4", "0") +
            Marker("to", "5", "1") +
            "<xdr:cxnSp><xdr:nvCxnSpPr><xdr:cNvPr id=\"4\" name=\"Connector 1\" /><xdr:cNvCxnSpPr /></xdr:nvCxnSpPr><xdr:spPr /></xdr:cxnSp><xdr:clientData />" +
            "</xdr:twoCellAnchor>" +
            "<xdr:oneCellAnchor>" +
            Marker("from", "6", "0") +
            "<xdr:ext cx=\"200000\" cy=\"200000\" /><xdr:control r:id=\"rIdControl1\" name=\"Check Box 1\" shapeId=\"5\" /><xdr:clientData />" +
            "</xdr:oneCellAnchor>" +
            "<xdr:twoCellAnchor>" +
            Marker("from", "8", "0") +
            Marker("to", "9", "1") +
            "<xdr:pic><xdr:nvPicPr><xdr:cNvPr id=\"6\" name=\"Picture 1\" /><xdr:cNvPicPr /></xdr:nvPicPr><xdr:blipFill><a:blip r:embed=\"rIdImage1\" /></xdr:blipFill><xdr:spPr /></xdr:pic><xdr:clientData />" +
            "</xdr:twoCellAnchor>" +
            "</xdr:wsDr>";
    }

    static string Marker(string elementName, string row, string column)
    {
        return "<xdr:" + elementName + "><xdr:col>" + column + "</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>" + row + "</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:" + elementName + ">";
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
