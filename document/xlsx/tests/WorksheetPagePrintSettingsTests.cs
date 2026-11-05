using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetPagePrintSettingsTests
{
    [Test]
    public void ReadString_ParsesHeadersFootersAndPageBreaks()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\">" +
            "<sheetData />" +
            "<printOptions horizontalCentered=\"1\" verticalCentered=\"0\" headings=\"1\" gridLines=\"0\" />" +
            "<pageMargins left=\"0.5\" right=\"0.6\" top=\"0.7\" bottom=\"0.8\" header=\"0.2\" footer=\"0.3\" />" +
            "<pageSetup paperSize=\"9\" orientation=\"landscape\" scale=\"95\" fitToWidth=\"1\" fitToHeight=\"2\" firstPageNumber=\"4\" useFirstPageNumber=\"1\" />" +
            "<headerFooter differentOddEven=\"1\" differentFirst=\"1\" scaleWithDoc=\"0\" alignWithMargins=\"1\" x:hf=\"h\">" +
            "<oddHeader>&amp;LLeft&amp;CCenter</oddHeader><oddFooter>&amp;RPage &amp;P</oddFooter><evenHeader>Even</evenHeader><firstFooter>First</firstFooter><x:future /></headerFooter>" +
            "<rowBreaks count=\"1\" manualBreakCount=\"1\"><brk id=\"3\" min=\"0\" max=\"10\" man=\"1\" x:brk=\"r\" /></rowBreaks>" +
            "<colBreaks count=\"1\" manualBreakCount=\"0\"><brk id=\"2\" min=\"1\" max=\"20\" man=\"0\" /></colBreaks>" +
            "</worksheet>";

        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.HasPrintOptions, Is.True);
        Assert.That(part.PrintOptions!.HorizontalCentered, Is.True);
        Assert.That(part.PrintOptions.GridLines, Is.False);
        Assert.That(part.HasPageMargins, Is.True);
        Assert.That(part.PageMargins!.Left, Is.EqualTo(0.5d).Within(0.0000001d));
        Assert.That(part.PageMargins.Footer, Is.EqualTo(0.3d).Within(0.0000001d));
        Assert.That(part.HasPageSetup, Is.True);
        Assert.That(part.PageSetup!.PaperSize, Is.EqualTo(9));
        Assert.That(part.PageSetup.Orientation, Is.EqualTo("landscape"));
        Assert.That(part.PageSetup.Scale, Is.EqualTo(95));
        Assert.That(part.HasHeaderFooter, Is.True);
        Assert.That(part.HeaderFooter!.DifferentOddEven, Is.True);
        Assert.That(part.HeaderFooter.DifferentFirst, Is.True);
        Assert.That(part.HeaderFooter.ScaleWithDoc, Is.False);
        Assert.That(part.HeaderFooter.AlignWithMargins, Is.True);
        Assert.That(part.HeaderFooter.OddHeader, Is.EqualTo("&LLeft&CCenter"));
        Assert.That(part.HeaderFooter.OddFooter, Is.EqualTo("&RPage &P"));
        Assert.That(part.HeaderFooter.EvenHeader, Is.EqualTo("Even"));
        Assert.That(part.HeaderFooter.FirstFooter, Is.EqualTo("First"));
        Assert.That(part.HeaderFooter.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.HeaderFooter.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.RowBreaks.Count, Is.EqualTo(1));
        Assert.That(part.RowBreaks[0].Id, Is.EqualTo(3));
        Assert.That(part.RowBreaks[0].Min, Is.EqualTo(0));
        Assert.That(part.RowBreaks[0].Max, Is.EqualTo(10));
        Assert.That(part.RowBreaks[0].Manual, Is.True);
        Assert.That(part.RowBreaks[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.ColumnBreaks.Count, Is.EqualTo(1));
        Assert.That(part.ColumnBreaks[0].Id, Is.EqualTo(2));
        Assert.That(part.ColumnBreaks[0].Manual, Is.False);
    }

    [Test]
    public void WriteString_WritesConstructedHeadersFootersAndBreaksAndRoundTrips()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.HasPrintOptions = true;
        part.PrintOptions!.HorizontalCentered = true;
        part.PrintOptions.HasHorizontalCentered = true;
        part.PrintOptions.Headings = true;
        part.PrintOptions.HasHeadings = true;
        part.HasPageMargins = true;
        part.PageMargins!.Left = 0.5d;
        part.PageMargins.Right = 0.6d;
        part.PageMargins.Top = 0.7d;
        part.PageMargins.Bottom = 0.8d;
        part.PageMargins.Header = 0.2d;
        part.PageMargins.Footer = 0.3d;
        part.HasPageSetup = true;
        part.PageSetup!.PaperSize = 9;
        part.PageSetup.HasPaperSize = true;
        part.PageSetup.Orientation = "portrait";
        part.PageSetup.Scale = 100;
        part.PageSetup.HasScale = true;
        part.HasHeaderFooter = true;
        part.HeaderFooter!.OddHeader = "&CReport";
        part.HeaderFooter.OddFooter = "&RPage &P";
        part.HeaderFooter.ScaleWithDoc = true;
        part.HeaderFooter.HasScaleWithDoc = true;
        part.RowBreaks.Add(CreateBreak(5, 0, 10, true));
        part.ColumnBreaks.Add(CreateBreak(2, 1, 20, false));

        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<headerFooter scaleWithDoc=\"1\">"));
        Assert.That(writeResult.Xml, Does.Contain("<oddHeader>&amp;CReport</oddHeader>"));
        Assert.That(writeResult.Xml, Does.Contain("<rowBreaks count=\"1\" manualBreakCount=\"1\">"));
        Assert.That(writeResult.Xml, Does.Contain("<colBreaks count=\"1\" manualBreakCount=\"0\">"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.HeaderFooter!.OddHeader, Is.EqualTo("&CReport"));
        Assert.That(roundTrip.RowBreaks[0].Id, Is.EqualTo(5));
        Assert.That(roundTrip.ColumnBreaks[0].Manual, Is.False);
    }

    [Test]
    public void WriteBytes_RejectsInvalidPagePrintSettings()
    {
        XlsxWorksheetPart nullPrintOptions = new XlsxWorksheetPart();
        XlsxWorksheetPart nullMargins = new XlsxWorksheetPart();
        XlsxWorksheetPart badMargins = new XlsxWorksheetPart();
        XlsxWorksheetPart nullSetup = new XlsxWorksheetPart();
        XlsxWorksheetPart badSetup = new XlsxWorksheetPart();
        XlsxWorksheetPart nullHeaderFooter = new XlsxWorksheetPart();
        XlsxWorksheetPart nullBreaks = new XlsxWorksheetPart();
        XlsxWorksheetPart badBreak = new XlsxWorksheetPart();

        nullPrintOptions.HasPrintOptions = true;
        nullPrintOptions.PrintOptions = null;
        nullMargins.HasPageMargins = true;
        nullMargins.PageMargins = null;
        badMargins.HasPageMargins = true;
        badMargins.PageMargins!.Left = -0.1d;
        nullSetup.HasPageSetup = true;
        nullSetup.PageSetup = null;
        badSetup.HasPageSetup = true;
        badSetup.PageSetup!.Scale = 5;
        badSetup.PageSetup.HasScale = true;
        badSetup.PageSetup.Orientation = "sideways";
        nullHeaderFooter.HasHeaderFooter = true;
        nullHeaderFooter.HeaderFooter = null;
        nullBreaks.RowBreaks = null!;
        badBreak.RowBreaks.Add(new XlsxWorksheetPageBreak());
        XlsxWorksheetPageBreak invalidUnknowns = CreateBreak(1, 0, 10, true);
        invalidUnknowns.UnknownAttributes = null!;
        badBreak.ColumnBreaks.Add(invalidUnknowns);

        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();
        XlsxWorksheetPartWriteResult nullPrintOptionsResult = writer.WriteBytes(nullPrintOptions);
        XlsxWorksheetPartWriteResult nullMarginsResult = writer.WriteBytes(nullMargins);
        XlsxWorksheetPartWriteResult badMarginsResult = writer.WriteBytes(badMargins);
        XlsxWorksheetPartWriteResult nullSetupResult = writer.WriteBytes(nullSetup);
        XlsxWorksheetPartWriteResult badSetupResult = writer.WriteBytes(badSetup);
        XlsxWorksheetPartWriteResult nullHeaderFooterResult = writer.WriteBytes(nullHeaderFooter);
        XlsxWorksheetPartWriteResult nullBreaksResult = writer.WriteBytes(nullBreaks);
        XlsxWorksheetPartWriteResult badBreakResult = writer.WriteBytes(badBreak);

        Assert.That(HasDiagnostic(nullPrintOptionsResult, "worksheet.printOptionsNull"), Is.True);
        Assert.That(HasDiagnostic(nullMarginsResult, "worksheet.pageMarginsNull"), Is.True);
        Assert.That(HasDiagnostic(badMarginsResult, "worksheet.pageMarginRange"), Is.True);
        Assert.That(HasDiagnostic(nullSetupResult, "worksheet.pageSetupNull"), Is.True);
        Assert.That(HasDiagnostic(badSetupResult, "worksheet.pageSetupScaleRange"), Is.True);
        Assert.That(HasDiagnostic(badSetupResult, "worksheet.pageSetupOrientationUnsupported"), Is.True);
        Assert.That(HasDiagnostic(nullHeaderFooterResult, "worksheet.headerFooterNull"), Is.True);
        Assert.That(HasDiagnostic(nullBreaksResult, "worksheet.pageBreaksNull"), Is.True);
        Assert.That(HasDiagnostic(badBreakResult, "worksheet.pageBreakIdMissing"), Is.True);
        Assert.That(HasDiagnostic(badBreakResult, "worksheet.pageBreakUnknownAttributesNull"), Is.True);
    }

    [Test]
    public void WorkbookPrintAreaAndTitlesHelpers_WriteLocalDefinedNames()
    {
        XlsxWorkbookPart workbook = new XlsxWorkbookPart();
        workbook.Sheets.Add(CreateSheet("Sheet 1", 1, "rId1"));

        XlsxWorkbookPrintDefinitionResult area = workbook.SetPrintArea(0, "Sheet 1", "$A$1:$C$5");
        XlsxWorkbookPrintDefinitionResult titles = workbook.SetPrintTitles(0, "Sheet 1", "$1:$2", "$A:$B");
        XlsxWorkbookPartWriteResult writeResult = new XlsxWorkbookPartWriter().WriteString(workbook);
        XlsxWorkbookPart roundTrip = new XlsxWorkbookPartReader().ReadString(writeResult.Xml);

        Assert.That(area.IsValid, Is.True);
        Assert.That(area.CreatedDefinedName, Is.True);
        Assert.That(area.DefinedName!.Text, Is.EqualTo("'Sheet 1'!$A$1:$C$5"));
        Assert.That(titles.IsValid, Is.True);
        Assert.That(titles.CreatedDefinedName, Is.True);
        Assert.That(titles.DefinedName!.Text, Is.EqualTo("'Sheet 1'!$1:$2,'Sheet 1'!$A:$B"));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("name=\"_xlnm.Print_Area\""));
        Assert.That(writeResult.Xml, Does.Contain("name=\"_xlnm.Print_Titles\""));
        Assert.That(roundTrip.DefinedNames.Count, Is.EqualTo(2));
        Assert.That(roundTrip.GetDefinedName("_xlnm.Print_Area")!.Text, Is.EqualTo("'Sheet 1'!$A$1:$C$5"));
    }

    [Test]
    public void WorkbookPrintAreaAndTitlesHelpers_ReportInvalidDefinitions()
    {
        XlsxWorkbookPart nullDefinedNames = new XlsxWorkbookPart();
        XlsxWorkbookPart missingSheetName = new XlsxWorkbookPart();
        XlsxWorkbookPart missingArea = new XlsxWorkbookPart();
        XlsxWorkbookPart missingTitles = new XlsxWorkbookPart();

        nullDefinedNames.DefinedNames = null!;

        XlsxWorkbookPrintDefinitionResult nullDefinedNamesResult = nullDefinedNames.SetPrintArea(0, "Sheet1", "$A$1");
        XlsxWorkbookPrintDefinitionResult missingSheetNameResult = missingSheetName.SetPrintArea(0, "", "$A$1");
        XlsxWorkbookPrintDefinitionResult missingAreaResult = missingArea.SetPrintArea(0, "Sheet1", "");
        XlsxWorkbookPrintDefinitionResult missingTitlesResult = missingTitles.SetPrintTitles(0, "Sheet1", "", "");

        Assert.That(nullDefinedNamesResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullDefinedNamesResult, "workbook.definedNamesNull"), Is.True);
        Assert.That(missingSheetNameResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingSheetNameResult, "workbook.printDefinitionSheetNameMissing"), Is.True);
        Assert.That(missingAreaResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingAreaResult, "workbook.printAreaReferenceMissing"), Is.True);
        Assert.That(missingTitlesResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(missingTitlesResult, "workbook.printTitlesReferenceMissing"), Is.True);
    }

    static XlsxWorksheetPageBreak CreateBreak(uint id, uint min, uint max, bool manual)
    {
        XlsxWorksheetPageBreak pageBreak = new XlsxWorksheetPageBreak();
        pageBreak.Id = id;
        pageBreak.HasId = true;
        pageBreak.Min = min;
        pageBreak.HasMin = true;
        pageBreak.Max = max;
        pageBreak.HasMax = true;
        pageBreak.Manual = manual;
        pageBreak.HasManual = true;
        return pageBreak;
    }

    static XlsxWorkbookSheet CreateSheet(string name, uint sheetId, string relationshipId)
    {
        XlsxWorkbookSheet sheet = new XlsxWorkbookSheet();
        sheet.Name = name;
        sheet.SheetId = sheetId;
        sheet.RelationshipId = relationshipId;
        sheet.SheetType = XlsxSheetType.Worksheet;
        sheet.ContentType = XlsxContentTypes.Worksheet;
        return sheet;
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

    static bool HasDiagnostic(XlsxWorkbookPrintDefinitionResult result, string code)
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
