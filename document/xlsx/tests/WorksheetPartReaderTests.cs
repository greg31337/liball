using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetPartReaderTests
{
    [Test]
    public void ReadString_ParsesWorksheetMetadataAndCollections()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<sheetPr codeName=\"SheetCode\" enableFormatConditionsCalculation=\"1\" x:prop=\"p\"><tabColor rgb=\"FFFF0000\" /></sheetPr>" +
            "<dimension ref=\"A1:C3\" />" +
            "<sheetViews><sheetView workbookViewId=\"0\" view=\"normal\" showGridLines=\"0\" showRowColHeaders=\"1\" rightToLeft=\"1\" tabSelected=\"1\" zoomScale=\"125\" x:view=\"v\"><pane xSplit=\"1\" /></sheetView></sheetViews>" +
            "<sheetFormatPr baseColWidth=\"10\" defaultColWidth=\"12.5\" defaultRowHeight=\"15\" customHeight=\"1\" zeroHeight=\"0\" x:format=\"f\" />" +
            "<cols><col min=\"1\" max=\"2\" width=\"20\" style=\"3\" hidden=\"1\" bestFit=\"1\" customWidth=\"1\" outlineLevel=\"2\" collapsed=\"0\" x:col=\"c\" /></cols>" +
            "<sheetData><row r=\"1\" spans=\"1:3\" ht=\"18\" hidden=\"0\" customHeight=\"1\" s=\"2\" x:row=\"r\">" +
            "<c r=\"A1\" t=\"s\" s=\"1\" x:cell=\"c\"><v>0</v></c>" +
            "<c r=\"B1\"><f>SUM(A1:A2)</f><v>5</v></c>" +
            "<c r=\"C1\" t=\"inlineStr\"><is><t>Hello</t></is></c>" +
            "</row></sheetData>" +
            "<mergeCells count=\"1\"><mergeCell ref=\"A1:B1\" /></mergeCells>" +
            "<hyperlinks><hyperlink ref=\"C1\" r:id=\"rId5\" display=\"Link\" tooltip=\"Tip\" x:hyperlink=\"h\" /></hyperlinks>" +
            "<printOptions horizontalCentered=\"1\" verticalCentered=\"0\" headings=\"1\" gridLines=\"0\" />" +
            "<pageMargins left=\"0.7\" right=\"0.7\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\" />" +
            "<pageSetup paperSize=\"9\" orientation=\"landscape\" scale=\"90\" fitToWidth=\"1\" fitToHeight=\"2\" firstPageNumber=\"3\" useFirstPageNumber=\"1\" x:setup=\"s\" />" +
            "<sheetProtection sheet=\"1\" objects=\"0\" scenarios=\"1\" password=\"ABCD\" algorithmName=\"SHA-512\" hashValue=\"h\" saltValue=\"s\" spinCount=\"100000\" x:protection=\"p\" />" +
            "<extLst><ext uri=\"{33333333-3333-3333-3333-333333333333}\"><x:future /></ext></extLst>" +
            "<x:future />" +
            "</worksheet>";
        XlsxWorksheetPartReader reader = new XlsxWorksheetPartReader();

        XlsxWorksheetPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HasSheetProperties, Is.True);
        Assert.That(part.SheetProperties.CodeName, Is.EqualTo("SheetCode"));
        Assert.That(part.SheetProperties.EnableFormatConditionsCalculation, Is.True);
        Assert.That(part.SheetProperties.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.HasDimension, Is.True);
        Assert.That(part.Dimension.Reference, Is.EqualTo("A1:C3"));
        Assert.That(part.SheetViews.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].WorkbookViewId, Is.EqualTo(0));
        Assert.That(part.SheetViews[0].ShowGridLines, Is.False);
        Assert.That(part.SheetViews[0].RightToLeft, Is.True);
        Assert.That(part.SheetViews[0].ZoomScale, Is.EqualTo(125));
        Assert.That(part.SheetViews[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].HasPane, Is.True);
        Assert.That(part.SheetViews[0].Pane.XSplit, Is.EqualTo(1d).Within(0.0000001d));
        Assert.That(part.SheetViews[0].UnknownXml.Count, Is.EqualTo(0));
        Assert.That(part.HasFormatProperties, Is.True);
        Assert.That(part.FormatProperties.BaseColumnWidth, Is.EqualTo(10));
        Assert.That(part.FormatProperties.DefaultColumnWidth, Is.EqualTo(12.5d).Within(0.0000001d));
        Assert.That(part.FormatProperties.DefaultRowHeight, Is.EqualTo(15d).Within(0.0000001d));
        Assert.That(part.Columns.Count, Is.EqualTo(1));
        Assert.That(part.Columns[0].Min, Is.EqualTo(1));
        Assert.That(part.Columns[0].Max, Is.EqualTo(2));
        Assert.That(part.Columns[0].Width, Is.EqualTo(20d).Within(0.0000001d));
        Assert.That(part.Columns[0].Hidden, Is.True);
        Assert.That(part.Columns[0].Collapsed, Is.False);
        Assert.That(part.Rows.Count, Is.EqualTo(1));
        Assert.That(part.Rows[0].RowIndex, Is.EqualTo(1));
        Assert.That(part.Rows[0].Spans, Is.EqualTo("1:3"));
        Assert.That(part.Rows[0].Height, Is.EqualTo(18d).Within(0.0000001d));
        Assert.That(part.Rows[0].Cells.Count, Is.EqualTo(3));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("0"));
        Assert.That(part.GetCell("B1")!.Formula, Is.EqualTo("SUM(A1:A2)"));
        Assert.That(part.GetCell("B1")!.Value, Is.EqualTo("5"));
        Assert.That(part.GetCell("C1")!.InlineStringText, Is.EqualTo("Hello"));
        Assert.That(part.MergedCells.Count, Is.EqualTo(1));
        Assert.That(part.MergedCells[0].Reference, Is.EqualTo("A1:B1"));
        Assert.That(part.Hyperlinks.Count, Is.EqualTo(1));
        Assert.That(part.Hyperlinks[0].Reference, Is.EqualTo("C1"));
        Assert.That(part.Hyperlinks[0].RelationshipId, Is.EqualTo("rId5"));
        Assert.That(part.Hyperlinks[0].Display, Is.EqualTo("Link"));
        Assert.That(part.HasPrintOptions, Is.True);
        Assert.That(part.PrintOptions!.HorizontalCentered, Is.True);
        Assert.That(part.PrintOptions.GridLines, Is.False);
        Assert.That(part.HasPageMargins, Is.True);
        Assert.That(part.PageMargins!.Left, Is.EqualTo(0.7d).Within(0.0000001d));
        Assert.That(part.PageMargins.Top, Is.EqualTo(0.75d).Within(0.0000001d));
        Assert.That(part.HasPageSetup, Is.True);
        Assert.That(part.PageSetup!.PaperSize, Is.EqualTo(9));
        Assert.That(part.PageSetup.Orientation, Is.EqualTo("landscape"));
        Assert.That(part.PageSetup.FirstPageNumber, Is.EqualTo(3));
        Assert.That(part.PageSetup.UseFirstPageNumber, Is.True);
        Assert.That(part.HasSheetProtection, Is.True);
        Assert.That(part.SheetProtection!.Sheet, Is.True);
        Assert.That(part.SheetProtection.Objects, Is.False);
        Assert.That(part.SheetProtection.Scenarios, Is.True);
        Assert.That(part.SheetProtection.Password, Is.EqualTo("ABCD"));
        Assert.That(part.SheetProtection.SpinCount, Is.EqualTo(100000));
        Assert.That(part.HasExtensionList, Is.True);
        Assert.That(part.ExtensionList.Extensions.Count, Is.EqualTo(1));
        Assert.That(part.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(part.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void ReadString_AcceptsStrictSpreadsheetNamespace()
    {
        string xml = "<worksheet xmlns=\"http://purl.oclc.org/ooxml/spreadsheetml/main\"><sheetData><row r=\"1\"><c r=\"A1\"><v>42</v></c></row></sheetData></worksheet>";
        XlsxWorksheetPartReader reader = new XlsxWorksheetPartReader();

        XlsxWorksheetPart part = reader.ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.NamespaceUri, Is.EqualTo(XlsxNamespaces.StrictSpreadsheet));
        Assert.That(part.Rows.Count, Is.EqualTo(1));
        Assert.That(part.GetCell("A1")!.Value, Is.EqualTo("42"));
    }

    [Test]
    public void ReadString_ReportsMissingRootInvalidRootAndInvalidValues()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
            "<sheetPr enableFormatConditionsCalculation=\"maybe\" />" +
            "<sheetFormatPr defaultRowHeight=\"bad\" />" +
            "<sheetData><row r=\"bad\"><c r=\"A1\" s=\"bad\"><v>1</v></c></row></sheetData>" +
            "<pageSetup fitToWidth=\"bad\" useFirstPageNumber=\"maybe\" />" +
            "<sheetProtection sheet=\"maybe\" spinCount=\"bad\" />" +
            "</worksheet>";
        XlsxWorksheetPartReader reader = new XlsxWorksheetPartReader();

        XlsxWorksheetPart missingRoot = reader.ReadString("");
        XlsxWorksheetPart invalidRoot = reader.ReadString("<bad xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" />");
        XlsxWorksheetPart invalidValues = reader.ReadString(xml);

        Assert.That(missingRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingRoot, "worksheet.missingRoot"), Is.True);
        Assert.That(invalidRoot.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidRoot, "worksheet.invalidRoot"), Is.True);
        Assert.That(invalidValues.IsValid, Is.False);
        Assert.That(HasDiagnostic(invalidValues, "xml.boolean.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.double.invalid"), Is.True);
        Assert.That(HasDiagnostic(invalidValues, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(invalidValues.Rows.Count, Is.EqualTo(1));
        Assert.That(invalidValues.HasSheetProtection, Is.True);
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
}
