using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetMetadataTests
{
    [Test]
    public void Reader_LoadsTypedWorksheetMetadataAndPreservesUnsupportedChildren()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPartReader().ReadString(MetadataXml());

        Assert.That(worksheet.IsValid, Is.True, Diagnostics(worksheet.Validation));
        Assert.That(worksheet.SheetProperties.HasTabColor, Is.True);
        Assert.That(worksheet.SheetProperties.TabColor.Rgb, Is.EqualTo("FFFF0000"));
        Assert.That(worksheet.SheetProperties.OutlineProperties.SummaryBelow, Is.False);
        Assert.That(worksheet.SheetProperties.OutlineProperties.ShowOutlineSymbols, Is.True);
        Assert.That(worksheet.SheetProperties.PageSetupProperties.FitToPage, Is.True);
        Assert.That(worksheet.SheetProperties.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(worksheet.HasScenarios, Is.True);
        Assert.That(worksheet.Scenarios.Current, Is.EqualTo(1));
        Assert.That(worksheet.Scenarios.Items[0].Name, Is.EqualTo("Forecast"));
        Assert.That(worksheet.Scenarios.Items[0].InputCells[0].Reference, Is.EqualTo("A1"));
        Assert.That(worksheet.Scenarios.Items[0].InputCells[0].Value, Is.EqualTo("42"));
        Assert.That(worksheet.CustomSheetViews[0].Guid, Is.EqualTo("{11111111-1111-1111-1111-111111111111}"));
        Assert.That(worksheet.CustomSheetViews[0].ShowGridLines, Is.False);
        Assert.That(worksheet.IgnoredErrors[0].NumberStoredAsText, Is.True);
        Assert.That(worksheet.SmartTags[0].Reference, Is.EqualTo("C3"));
        Assert.That(worksheet.SmartTags[0].Properties[0].Key, Is.EqualTo("urn:key"));
        Assert.That(worksheet.WebPublishItems[0].DestinationFile, Is.EqualTo("report.html"));
    }

    [Test]
    public void Operations_AuthorTypedMetadataThatRoundTripsThroughWorksheetXml()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();

        XlsxWorksheetMetadataResult color = operations.SetTabColorRgb(worksheet, "FF336699");
        XlsxWorksheetMetadataResult outline = operations.SetOutlineProperties(worksheet, false, true, true, true);
        XlsxWorksheetMetadataResult scenario = operations.AddScenario(worksheet, "Plan A");
        XlsxWorksheetScenarioInputCell input = new XlsxWorksheetScenarioInputCell();
        input.Reference = "B2";
        input.Value = "100";
        scenario.Scenario!.InputCells.Add(input);
        XlsxWorksheetMetadataResult view = operations.AddCustomSheetView(worksheet, "{22222222-2222-2222-2222-222222222222}");
        view.CustomView!.Scale = 85;
        view.CustomView.HasScale = true;
        XlsxWorksheetMetadataResult ignored = operations.AddIgnoredError(worksheet, "A1 B2:C3");
        ignored.IgnoredError!.Formula = true;
        ignored.IgnoredError.HasFormula = true;
        XlsxWorksheetMetadataResult smartTag = operations.AddSmartTag(worksheet, "C4", 7);
        XlsxWorksheetSmartTagProperty property = new XlsxWorksheetSmartTagProperty();
        property.Key = "kind";
        property.Value = "customer";
        smartTag.SmartTag!.Properties.Add(property);
        XlsxWorksheetMetadataResult publish = operations.AddWebPublishItem(worksheet, 3, "https://example.test/report.html");
        publish.WebPublishItem!.SourceType = "range";
        publish.WebPublishItem.SourceReference = "A1:C10";

        XlsxWorksheetPartWriteResult write = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(write.Xml);

        Assert.That(color.IsValid, Is.True, Diagnostics(color.Validation));
        Assert.That(outline.IsValid, Is.True, Diagnostics(outline.Validation));
        Assert.That(scenario.IsValid, Is.True, Diagnostics(scenario.Validation));
        Assert.That(view.IsValid, Is.True, Diagnostics(view.Validation));
        Assert.That(ignored.IsValid, Is.True, Diagnostics(ignored.Validation));
        Assert.That(smartTag.IsValid, Is.True, Diagnostics(smartTag.Validation));
        Assert.That(publish.IsValid, Is.True, Diagnostics(publish.Validation));
        Assert.That(write.IsValid, Is.True, Diagnostics(write.Validation));
        Assert.That(write.Xml, Does.Contain("tabColor"));
        Assert.That(write.Xml, Does.Contain("outlinePr"));
        Assert.That(write.Xml, Does.Contain("scenarios"));
        Assert.That(write.Xml, Does.Contain("customSheetViews"));
        Assert.That(write.Xml, Does.Contain("ignoredErrors"));
        Assert.That(write.Xml, Does.Contain("cellSmartTags"));
        Assert.That(write.Xml, Does.Contain("webPublishItems"));
        Assert.That(roundTrip.IsValid, Is.True, Diagnostics(roundTrip.Validation));
        Assert.That(roundTrip.SheetProperties.TabColor.Rgb, Is.EqualTo("FF336699"));
        Assert.That(roundTrip.Scenarios.Items[0].InputCells[0].Value, Is.EqualTo("100"));
        Assert.That(roundTrip.CustomSheetViews[0].Scale, Is.EqualTo(85));
        Assert.That(roundTrip.IgnoredErrors[0].Formula, Is.True);
        Assert.That(roundTrip.SmartTags[0].Properties[0].Value, Is.EqualTo("customer"));
        Assert.That(roundTrip.WebPublishItems[0].SourceReference, Is.EqualTo("A1:C10"));
    }

    [Test]
    public void MetadataOperations_ReportInvalidAndDuplicateInputs()
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        IXlsxWorksheetPartOperations operations = new XlsxWorksheetPartOperations();

        XlsxWorksheetMetadataResult badColor = operations.SetTabColorRgb(worksheet, "red");
        XlsxWorksheetMetadataResult scenario = operations.AddScenario(worksheet, "Plan");
        XlsxWorksheetMetadataResult duplicateScenario = operations.AddScenario(worksheet, "plan");
        XlsxWorksheetMetadataResult badIgnored = operations.AddIgnoredError(worksheet, "not-a-range");
        XlsxWorksheetMetadataResult badSmartTag = operations.AddSmartTag(worksheet, "A1", 0);
        XlsxWorksheetMetadataResult badPublish = operations.AddWebPublishItem(worksheet, 0, "");

        Assert.That(badColor.IsValid, Is.False);
        Assert.That(scenario.IsValid, Is.True);
        Assert.That(duplicateScenario.IsValid, Is.False);
        Assert.That(HasDiagnostic(duplicateScenario.Validation, "worksheet.scenarioNameDuplicate"), Is.True);
        Assert.That(badIgnored.IsValid, Is.False);
        Assert.That(badSmartTag.IsValid, Is.False);
        Assert.That(HasDiagnostic(badSmartTag.Validation, "worksheet.smartTagTypeRange"), Is.True);
        Assert.That(badPublish.IsValid, Is.False);
        Assert.That(HasDiagnostic(badPublish.Validation, "worksheet.webPublishIdRange"), Is.True);
    }

    [Test]
    public void DrawingHyperlink_ReadsEditsWritesAndClearsTypedImageHyperlink()
    {
        XlsxDrawingPart drawing = new XlsxDrawingPartReader().ReadBytes(System.Text.Encoding.UTF8.GetBytes(DrawingXml()));
        IXlsxDrawingPartOperations operations = new XlsxDrawingPartOperations();

        Assert.That(drawing.IsValid, Is.True, Diagnostics(drawing.Validation));
        Assert.That(drawing.ImageAnchors[0].HasHyperlink, Is.True);
        Assert.That(drawing.ImageAnchors[0].Hyperlink.RelationshipId, Is.EqualTo("rIdOldLink"));
        Assert.That(drawing.ImageAnchors[0].Hyperlink.Tooltip, Is.EqualTo("Old tip"));
        Assert.That(drawing.ShapeAnchors[0].HasHyperlink, Is.True);
        Assert.That(drawing.ShapeAnchors[0].Hyperlink.RelationshipId, Is.EqualTo("rIdShapeLink"));

        XlsxValidationResult set = operations.SetImageHyperlink(drawing, 0, "rIdNewLink", "New tip");
        XlsxDrawingPartWriteResult write = new XlsxDrawingPartWriter().WriteString(drawing);
        XlsxDrawingPart roundTrip = new XlsxDrawingPartReader().ReadBytes(write.Bytes);

        Assert.That(set.IsValid, Is.True, Diagnostics(set));
        Assert.That(write.IsValid, Is.True, Diagnostics(write.Validation));
        Assert.That(write.Xml, Does.Contain("rIdNewLink"));
        Assert.That(write.Xml, Does.Contain("New tip"));
        Assert.That(roundTrip.ImageAnchors[0].HasHyperlink, Is.True);
        Assert.That(roundTrip.ImageAnchors[0].Hyperlink.RelationshipId, Is.EqualTo("rIdNewLink"));

        XlsxValidationResult clear = operations.ClearImageHyperlink(roundTrip, 0);
        XlsxDrawingPartWriteResult clearedWrite = new XlsxDrawingPartWriter().WriteString(roundTrip);
        XlsxDrawingPart clearedRoundTrip = new XlsxDrawingPartReader().ReadBytes(clearedWrite.Bytes);

        Assert.That(clear.IsValid, Is.True, Diagnostics(clear));
        Assert.That(clearedWrite.Xml, Does.Not.Contain("rIdNewLink"));
        Assert.That(clearedRoundTrip.ImageAnchors[0].HasHyperlink, Is.False);
        Assert.That(clearedRoundTrip.ShapeAnchors[0].HasHyperlink, Is.True);
        Assert.That(operations.SetImageHyperlink(roundTrip, 4, "rIdLink", "").IsValid, Is.False);
        Assert.That(operations.SetImageHyperlink(roundTrip, 0, "bad id", "").IsValid, Is.False);
    }

    static string MetadataXml()
    {
        return "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\" xmlns:x=\"urn:future\">" +
            "<sheetPr codeName=\"Metadata\"><tabColor rgb=\"FFFF0000\" /><outlinePr summaryBelow=\"0\" summaryRight=\"1\" showOutlineSymbols=\"1\" applyStyles=\"1\" x:mode=\"future\" /><pageSetUpPr autoPageBreaks=\"1\" fitToPage=\"1\" /><x:future /></sheetPr>" +
            "<sheetData />" +
            "<scenarios current=\"1\" show=\"0\" sqref=\"A1\"><scenario name=\"Forecast\" user=\"Ada\" locked=\"1\" count=\"1\"><inputCells r=\"A1\" val=\"42\" numFmtId=\"4\" /></scenario><x:futureScenario /></scenarios>" +
            "<customSheetViews><customSheetView guid=\"{11111111-1111-1111-1111-111111111111}\" scale=\"90\" showGridLines=\"0\"><selection activeCell=\"A1\" /></customSheetView></customSheetViews>" +
            "<ignoredErrors><ignoredError sqref=\"A1 B2:C3\" numberStoredAsText=\"1\" formula=\"0\" /></ignoredErrors>" +
            "<cellSmartTags><cellSmartTag r=\"C3\" type=\"2\" xmlBased=\"1\"><cellSmartTagPr key=\"urn:key\" val=\"value\" /><x:futureTag /></cellSmartTag></cellSmartTags>" +
            "<webPublishItems count=\"1\"><webPublishItem id=\"1\" sourceType=\"range\" sourceRef=\"A1:B2\" destinationFile=\"report.html\" title=\"Report\" autoRepublish=\"1\" /></webPublishItems>" +
            "</worksheet>";
    }

    static string DrawingXml()
    {
        return "<xdr:wsDr xmlns:xdr=\"" + XlsxNamespaces.SpreadsheetDrawing + "\" xmlns:a=\"" + XlsxNamespaces.Drawing + "\" xmlns:r=\"" + XlsxNamespaces.OfficeDocumentRelationships + "\">" +
            "<xdr:twoCellAnchor><xdr:from><xdr:col>0</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>0</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:from><xdr:to><xdr:col>1</xdr:col><xdr:colOff>0</xdr:colOff><xdr:row>1</xdr:row><xdr:rowOff>0</xdr:rowOff></xdr:to><xdr:pic><xdr:nvPicPr><xdr:cNvPr id=\"1\" name=\"Picture 1\"><a:hlinkClick r:id=\"rIdOldLink\" tooltip=\"Old tip\" history=\"1\" /></xdr:cNvPr><xdr:cNvPicPr /></xdr:nvPicPr><xdr:blipFill><a:blip r:embed=\"rIdImage\" /></xdr:blipFill><xdr:spPr /></xdr:pic><xdr:clientData /></xdr:twoCellAnchor>" +
            "<xdr:twoCellAnchor><xdr:from><xdr:col>2</xdr:col><xdr:row>2</xdr:row></xdr:from><xdr:to><xdr:col>3</xdr:col><xdr:row>3</xdr:row></xdr:to><xdr:sp><xdr:nvSpPr><xdr:cNvPr id=\"2\" name=\"Shape 1\"><a:hlinkClick r:id=\"rIdShapeLink\" /></xdr:cNvPr><xdr:cNvSpPr /></xdr:nvSpPr><xdr:spPr /></xdr:sp><xdr:clientData /></xdr:twoCellAnchor>" +
            "</xdr:wsDr>";
    }

    static bool HasDiagnostic(XlsxValidationResult validation, string code)
    {
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }
        return false;
    }

    static string Diagnostics(XlsxValidationResult validation)
    {
        string text = "";
        foreach (XlsxDiagnostic diagnostic in validation.Diagnostics)
        {
            text = text + diagnostic.Code + ": " + diagnostic.Message + "\n";
        }
        return text;
    }
}
