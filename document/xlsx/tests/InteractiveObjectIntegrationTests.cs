using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class InteractiveObjectIntegrationTests
{
    [Test]
    public void WorksheetInteractiveObjects_WriteReadCommentsHyperlinksSparklinesAndDrawingReferenceTogether()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        XlsxWorksheetCommentResult comment = worksheet.AddCommentWithVisibility("C3", "Ada", "Review total", true);
        XlsxWorksheetHyperlinkResult external = worksheet.AddExternalHyperlink("A1", "https://example.com/report", "Report", "Open report");
        XlsxWorksheetHyperlinkResult internalLink = worksheet.AddInternalHyperlink("B2", "'Summary'!A1", "Summary", "Jump");
        XlsxSparklineOperationResult sparkline = worksheet.AddSparkline("E2", "Sheet1!B2:D2", "line");
        worksheet.HasDrawing = true;
        worksheet.Drawing.RelationshipId = "rIdDrawing1";

        XlsxWorksheetHyperlinkResult relationshipResult = worksheet.CreateHyperlinkRelationshipsForPartPath(XlsxPackagePaths.Worksheet1);
        relationshipResult.Relationships.Relationships.Add(CreateRelationship("rIdDrawing1", XlsxRelationshipTypes.Drawing, "../drawings/drawing1.xml", XlsxPackagePaths.Worksheet1));
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxCommentsPartWriteResult commentsWrite = new XlsxCommentsPartWriter().WriteString(worksheet.CommentsPart);
        XlsxLegacyCommentDrawingWriteResult commentDrawingWrite = new XlsxLegacyCommentDrawingWriter().WriteString(worksheet.LegacyCommentDrawingPart);
        XlsxWorksheetPart readBack = new XlsxWorksheetPartReader().ReadStringWithRelationships(worksheetWrite.Xml, relationshipResult.Relationships);
        XlsxSheetDrawingRelationshipResult drawingResolution = readBack.ResolveDrawingRelationship(relationshipResult.Relationships);
        XlsxCommentsPart commentsReadBack = new XlsxCommentsPartReader().ReadString(commentsWrite.Xml);
        XlsxLegacyCommentDrawingPart commentDrawingReadBack = new XlsxLegacyCommentDrawingReader().ReadString(commentDrawingWrite.Xml);
        commentsReadBack.ApplyLegacyDrawing(commentDrawingReadBack);

        Assert.That(comment.IsValid, Is.True);
        Assert.That(external.IsValid, Is.True);
        Assert.That(internalLink.IsValid, Is.True);
        Assert.That(sparkline.IsValid, Is.True);
        Assert.That(relationshipResult.IsValid, Is.True);
        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(worksheetWrite.Xml, Does.Contain("<hyperlinks>"));
        Assert.That(worksheetWrite.Xml, Does.Contain("ref=\"A1\" r:id=\"rId1\""));
        Assert.That(worksheetWrite.Xml, Does.Contain("location=\"'Summary'!A1\""));
        Assert.That(worksheetWrite.Xml, Does.Contain("<drawing r:id=\"rIdDrawing1\" />"));
        Assert.That(worksheetWrite.Xml, Does.Contain("sparklineGroups"));
        Assert.That(commentsWrite.IsValid, Is.True);
        Assert.That(commentDrawingWrite.IsValid, Is.True);
        Assert.That(readBack.IsValid, Is.True);
        Assert.That(readBack.Hyperlinks.Count, Is.EqualTo(2));
        Assert.That(readBack.Hyperlinks[0].ExternalTarget, Is.EqualTo("https://example.com/report"));
        Assert.That(readBack.Hyperlinks[1].Location, Is.EqualTo("'Summary'!A1"));
        Assert.That(readBack.HasSparklineGroups, Is.True);
        Assert.That(readBack.SparklineGroups[0].Sparklines[0].CellReference, Is.EqualTo("E2"));
        Assert.That(drawingResolution.IsValid, Is.True);
        Assert.That(readBack.Drawing.TargetPath, Is.EqualTo("/xl/drawings/drawing1.xml"));
        Assert.That(commentsReadBack.GetComment("C3")!.Text, Is.EqualTo("Review total"));
        Assert.That(commentsReadBack.GetComment("C3")!.Visible, Is.True);
    }

    [Test]
    public void DrawingImageChartAndMediaParts_RoundTripAnchorsRelationshipsAndPayloadsTogether()
    {
        XlsxDrawingPart drawing = new XlsxDrawingPart();
        XlsxDrawingImagePlacementResult image = drawing.AddTwoCellImage("rIdImage1", "B2", "D6", "Logo", "Company logo");
        XlsxDrawingChartAnchor chartAnchor = new XlsxDrawingChartAnchor();
        chartAnchor.Name = "Chart 1";
        chartAnchor.RelationshipId = "rIdChart1";
        chartAnchor.RawXml = ChartAnchorXml();
        drawing.ChartAnchors.Add(chartAnchor);
        XlsxRelationshipsPart relationships = CreateDrawingRelationships();
        XlsxDrawingChartRelationshipResult chartResolution = drawing.ResolveChartRelationships(relationships);
        XlsxDrawingPartWriteResult drawingWrite = new XlsxDrawingPartWriter().WriteString(drawing);
        XlsxDrawingPart drawingReadBack = new XlsxDrawingPartReader().ReadString(drawingWrite.Xml);
        XlsxChartPartWriteResult chartWrite = new XlsxChartPartWriter().WriteString(CreateChart());
        XlsxChartPart chartReadBack = new XlsxChartPartReader().ReadString(chartWrite.Xml);
        XlsxImagePart imagePart = new XlsxImagePartReader().ReadBytes("/xl/media/image1.png", PngBytes());
        XlsxImagePartWriteResult imageWrite = new XlsxImagePartWriter().WriteBytes(imagePart);

        Assert.That(image.IsValid, Is.True);
        Assert.That(chartResolution.IsValid, Is.True);
        Assert.That(chartAnchor.HasRelationship, Is.True);
        Assert.That(chartAnchor.TargetPath, Is.EqualTo("/xl/charts/chart1.xml"));
        Assert.That(drawingWrite.IsValid, Is.True);
        Assert.That(drawingWrite.Xml, Does.Contain("r:embed=\"rIdImage1\""));
        Assert.That(drawingWrite.Xml, Does.Contain("r:id=\"rIdChart1\""));
        Assert.That(drawingReadBack.IsValid, Is.True);
        Assert.That(drawingReadBack.ImageAnchors.Count, Is.EqualTo(1));
        Assert.That(drawingReadBack.ChartAnchors.Count, Is.EqualTo(1));
        Assert.That(drawingReadBack.ImageAnchors[0].Name, Is.EqualTo("Logo"));
        Assert.That(drawingReadBack.ChartAnchors[0].Name, Is.EqualTo("Chart 1"));
        Assert.That(chartWrite.IsValid, Is.True);
        Assert.That(chartReadBack.IsValid, Is.True);
        Assert.That(chartReadBack.ChartTypes[0], Is.EqualTo("barChart"));
        Assert.That(chartReadBack.Series[0].ValuesFormula, Is.EqualTo("Sheet1!$B$2:$B$4"));
        Assert.That(imagePart.IsValid, Is.True);
        Assert.That(imageWrite.IsValid, Is.True);
        Assert.That(imageWrite.Bytes, Is.EqualTo(PngBytes()));
    }

    static XlsxRelationship CreateRelationship(string id, string type, string target, string sourcePartPath)
    {
        XlsxRelationship relationship = new XlsxRelationship();
        relationship.Id = id;
        relationship.Type = type;
        relationship.Target = target;
        XlsxPackagePathResult targetPath = XlsxPackagePath.ResolveRelationshipTarget(sourcePartPath, target, false);
        if (targetPath.Validation.IsValid)
        {
            relationship.TargetPath = targetPath.Path;
        }

        return relationship;
    }

    static XlsxRelationshipsPart CreateDrawingRelationships()
    {
        XlsxRelationshipsPart relationships = new XlsxRelationshipsPart();
        relationships.SourcePartPath = "/xl/drawings/drawing1.xml";
        relationships.PartPath = "/xl/drawings/_rels/drawing1.xml.rels";
        relationships.Relationships.Add(CreateRelationship("rIdImage1", XlsxRelationshipTypes.Image, "../media/image1.png", relationships.SourcePartPath));
        relationships.Relationships.Add(CreateRelationship("rIdChart1", XlsxRelationshipTypes.Chart, "../charts/chart1.xml", relationships.SourcePartPath));
        return relationships;
    }

    static XlsxChartPart CreateChart()
    {
        XlsxChartPart chart = new XlsxChartPart();
        chart.RawXml = "";
        chart.HasTitle = true;
        chart.TitleText = "Sales";
        chart.ChartTypes.Add("column");
        chart.AddSeries("Sheet1!$B$1", "Sheet1!$A$2:$A$4", "Sheet1!$B$2:$B$4");
        return chart;
    }

    static string ChartAnchorXml()
    {
        return "<xdr:twoCellAnchor xmlns:xdr=\"" + XlsxNamespaces.SpreadsheetDrawing + "\" xmlns:a=\"" + XlsxNamespaces.Drawing + "\" xmlns:c=\"" + XlsxNamespaces.Chart + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\">" +
            "<xdr:from><xdr:col>4</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>1</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from>" +
            "<xdr:to><xdr:col>8</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>15</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:to>" +
            "<xdr:graphicFrame><xdr:nvGraphicFramePr><xdr:cNvPr id=\"2\" name=\"Chart 1\" /><xdr:cNvGraphicFramePr /></xdr:nvGraphicFramePr><xdr:xfrm />" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/chart\"><c:chart r:id=\"rIdChart1\" /></a:graphicData></a:graphic></xdr:graphicFrame>" +
            "<xdr:clientData /></xdr:twoCellAnchor>";
    }

    static byte[] PngBytes()
    {
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };
    }
}
