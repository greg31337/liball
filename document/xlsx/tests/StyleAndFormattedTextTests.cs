using System;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StyleAndFormattedTextTests
{
    [Test]
    public void WorkbookSaveOpen_RoundTripsStyledCellsAndFormattedText()
    {
        XlsxWorkbook workbook = XlsxWorkbook.Create();
        workbook.RenameSheet("Sheet1", "Styled");
        int moneyStyleId = CreateMoneyStyle().AddToWorkbook(workbook);
        int dateStyleId = CreateDateStyle().AddToWorkbook(workbook);
        XlsxWorksheetPart worksheet = CreateStyledWorksheet((uint)moneyStyleId, (uint)dateStyleId);
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteBytes(worksheet);
        workbook.UnknownParts.AddPart(XlsxPackagePaths.Worksheet1, XlsxContentTypes.Worksheet, worksheetWrite.Bytes);
        XlsxReaderOptions readerOptions = new XlsxReaderOptions();
        readerOptions.SelectedSheets.Add("Styled");

        XlsxWorkbookSaveResult saveResult = workbook.SaveBytes();
        XlsxWorkbookOpenResult openResult = XlsxWorkbook.OpenBytes(saveResult.Bytes, readerOptions);
        XlsxZipPackageReadResult package = new XlsxZipPackageReader().ReadBytes(saveResult.Bytes, new XlsxReaderOptions());

        Assert.That(moneyStyleId, Is.EqualTo(0));
        Assert.That(dateStyleId, Is.EqualTo(1));
        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(saveResult.IsValid, Is.True);
        Assert.That(package.GetEntry(XlsxPackagePaths.Styles), Is.Not.Null);
        Assert.That(openResult.IsValid, Is.True);
        Assert.That(openResult.Workbook!.HasStyles, Is.True);
        Assert.That(openResult.Workbook.Styles.NumberFormats.Count, Is.EqualTo(2));
        Assert.That(openResult.Workbook.Styles.Fonts.Count, Is.EqualTo(2));
        Assert.That(openResult.Workbook.Styles.Fills.Count, Is.EqualTo(1));
        Assert.That(openResult.Workbook.Styles.Borders.Count, Is.EqualTo(1));
        Assert.That(openResult.Workbook.Styles.CellFormats.Count, Is.EqualTo(2));
        Assert.That(openResult.Workbook.Worksheets.Count, Is.EqualTo(1));
        AssertStyledWorksheet(openResult.Workbook.Worksheets[0], openResult.Workbook.Styles, (uint)moneyStyleId, (uint)dateStyleId);
    }

    [Test]
    public void StylesPartWriteRead_PreservesConstructedFormatsAndRichInlineText()
    {
        XlsxStylesPart styles = new XlsxStylesPart();
        int styleId = CreateMoneyStyle().AddToStylesPart(styles);
        XlsxStylesPartWriteResult stylesWrite = new XlsxStylesPartWriter().WriteString(styles);
        XlsxStylesPart roundTripStyles = new XlsxStylesPartReader().ReadString(stylesWrite.Xml);
        XlsxWorksheetPart worksheet = CreateStyledWorksheet((uint)styleId, (uint)styleId);
        XlsxWorksheetPartWriteResult worksheetWrite = new XlsxWorksheetPartWriter().WriteString(worksheet);
        XlsxWorksheetPart roundTripWorksheet = new XlsxWorksheetPartReader().ReadString(worksheetWrite.Xml);

        Assert.That(styleId, Is.EqualTo(0));
        Assert.That(stylesWrite.IsValid, Is.True);
        Assert.That(roundTripStyles.IsValid, Is.True);
        Assert.That(roundTripStyles.NumberFormats[0].FormatCode, Is.EqualTo("#,##0.00"));
        Assert.That(roundTripStyles.Fonts[0].RawXml, Does.Contain("<b"));
        Assert.That(roundTripStyles.Fills[0].RawXml, Does.Contain("FFCCEEFF"));
        Assert.That(roundTripStyles.Borders[0].RawXml, Does.Contain("<bottom style=\"thin\">"));
        Assert.That(roundTripStyles.CellFormats[0].RawXml, Does.Contain("horizontal=\"right\""));
        Assert.That(worksheetWrite.IsValid, Is.True);
        Assert.That(roundTripWorksheet.IsValid, Is.True);
        Assert.That(roundTripWorksheet.GetCell("C1")!.HasInlineString, Is.True);
        Assert.That(roundTripWorksheet.GetCell("C1")!.InlineString.IsRichText, Is.True);
        Assert.That(roundTripWorksheet.GetCell("C1")!.InlineString.RichTextRuns.Count, Is.EqualTo(2));
        Assert.That(roundTripWorksheet.GetCell("C1")!.InlineString.RichTextRuns[0].PropertiesRawXml, Does.Contain("<b"));
    }

    static XlsxStyleBuilder CreateMoneyStyle()
    {
        XlsxStyleBuilder builder = new XlsxStyleBuilder();
        builder.SetNumberFormat("#,##0.00");
        builder.SetFontName("Calibri");
        builder.SetFontSize(11);
        builder.SetBold(true);
        builder.SetFontColor("003366");
        builder.SetFillColor("CCEEFF");
        builder.SetBorder("bottom", "thin", "336699");
        builder.SetHorizontalAlignment("right");
        builder.SetVerticalAlignment("center");
        return builder;
    }

    static XlsxStyleBuilder CreateDateStyle()
    {
        XlsxStyleBuilder builder = new XlsxStyleBuilder();
        builder.SetNumberFormat("m/d/yy");
        builder.SetFontName("Aptos");
        builder.SetItalic(true);
        builder.SetFontColor("006600");
        return builder;
    }

    static XlsxWorksheetPart CreateStyledWorksheet(uint moneyStyleId, uint dateStyleId)
    {
        XlsxWorksheetPart worksheet = new XlsxWorksheetPart();
        worksheet.SetNumberValue("A1", 1234.5d);
        worksheet.GetCell("A1")!.Style = moneyStyleId;
        worksheet.GetCell("A1")!.HasStyle = true;
        worksheet.SetDateTimeValue("B1", new DateTime(2026, 8, 9, 0, 0, 0, DateTimeKind.Utc));
        worksheet.GetCell("B1")!.Style = dateStyleId;
        worksheet.GetCell("B1")!.HasStyle = true;
        worksheet.SetInlineStringValue("C1", "BoldTail");
        worksheet.GetCell("C1")!.InlineString = CreateRichInlineString();
        worksheet.GetCell("C1")!.InlineStringText = "BoldTail";
        worksheet.GetCell("C1")!.HasInlineString = true;
        worksheet.ApplyCalculatedDimension();
        return worksheet;
    }

    static XlsxInlineString CreateRichInlineString()
    {
        XlsxInlineString value = new XlsxInlineString();
        value.IsRichText = true;
        value.Text = "BoldTail";
        value.RichTextRuns.Add(CreateRun("Bold", "<rPr><b /></rPr>"));
        value.RichTextRuns.Add(CreateRun("Tail", "<rPr><i /></rPr>"));
        return value;
    }

    static XlsxSharedStringRichTextRun CreateRun(string text, string propertiesRawXml)
    {
        XlsxSharedStringRichTextRun run = new XlsxSharedStringRichTextRun();
        run.Text = text;
        run.PropertiesRawXml = propertiesRawXml;
        return run;
    }

    static void AssertStyledWorksheet(XlsxWorksheetPart worksheet, XlsxStylesPart styles, uint moneyStyleId, uint dateStyleId)
    {
        XlsxWorksheetCell? moneyCell = worksheet.GetCell("A1");
        XlsxWorksheetCell? dateCell = worksheet.GetCell("B1");
        XlsxWorksheetCell? richTextCell = worksheet.GetCell("C1");
        XlsxCellValueResult moneyValue = XlsxCellValueConverter.FromWorksheetCell(moneyCell);
        XlsxCellValueResult dateValue = XlsxCellValueConverter.FromWorksheetCell(dateCell);
        XlsxCellValueResult richTextValue = XlsxCellValueConverter.FromWorksheetCell(richTextCell);
        XlsxFormattedTextRenderResult renderedMoney = XlsxFormattedTextRenderer.RenderCellValue(moneyValue.Value, GetFormatCode(styles, moneyCell!.Style)!, false);
        XlsxFormattedTextRenderResult renderedDate = XlsxFormattedTextRenderer.RenderCellValue(dateValue.Value, GetFormatCode(styles, dateCell!.Style)!, false);
        XlsxFormattedTextRenderResult renderedRichText = XlsxFormattedTextRenderer.RenderCellValue(richTextValue.Value, "0;0;0;\"Label: \"@", false);

        Assert.That(moneyCell.HasStyle, Is.True);
        Assert.That(moneyCell.Style, Is.EqualTo(moneyStyleId));
        Assert.That(dateCell.HasStyle, Is.True);
        Assert.That(dateCell.Style, Is.EqualTo(dateStyleId));
        Assert.That(moneyValue.IsValid, Is.True);
        Assert.That(dateValue.IsValid, Is.True);
        Assert.That(richTextValue.IsValid, Is.True);
        Assert.That(renderedMoney.IsValid, Is.True);
        Assert.That(renderedMoney.Text, Is.EqualTo("1,234.50"));
        Assert.That(renderedDate.IsValid, Is.True);
        Assert.That(renderedDate.Text, Is.EqualTo("8/9/26"));
        Assert.That(richTextCell!.InlineString.IsRichText, Is.True);
        Assert.That(richTextCell.InlineString.RichTextRuns[0].PropertiesRawXml, Does.Contain("<b"));
        Assert.That(renderedRichText.IsValid, Is.True);
        Assert.That(renderedRichText.Text, Is.EqualTo("Label: BoldTail"));
    }

    static string? GetFormatCode(XlsxStylesPart styles, uint styleId)
    {
        if (styles == null || styles.CellFormats == null || styleId >= styles.CellFormats.Count)
        {
            return "General";
        }

        XlsxCellFormatDefinition cellFormat = styles.CellFormats[(int)styleId];
        if (!cellFormat.HasNumberFormatId)
        {
            return "General";
        }

        foreach (XlsxNumberFormatDefinition numberFormat in styles.NumberFormats)
        {
            if (numberFormat.HasNumberFormatId && numberFormat.NumberFormatId == cellFormat.NumberFormatId)
            {
                return numberFormat.FormatCode;
            }
        }

        XlsxNumberFormatParseResult builtIn = XlsxNumberFormatParser.ParseBuiltIn(cellFormat.NumberFormatId);
        if (builtIn.IsValid && builtIn.FormatCode.Length > 0)
        {
            return builtIn.FormatCode;
        }

        return "General";
    }
}
