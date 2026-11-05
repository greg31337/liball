using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetPartWriterTests
{
    [Test]
    public void WriteString_WritesParseableBasicSheetData()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.Rows.Add(CreateRow(1, "1:3", 2, 18d, false, true));
        part.Rows[0].Cells.Add(CreateCell("A1", "s", 1, "0", "", ""));
        part.Rows[0].Cells.Add(CreateCell("B1", "", 0, "5", "SUM(A1:A2)", ""));
        part.Rows[0].Cells.Add(CreateCell("C1", "", 0, "", "", "Hello"));
        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();

        XlsxWorksheetPartWriteResult result = writer.WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<worksheet"));
        Assert.That(result.Xml, Does.Contain("xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\""));
        Assert.That(result.Xml, Does.Contain("<sheetData>"));
        Assert.That(result.Xml, Does.Contain("<row r=\"1\""));
        Assert.That(result.Xml, Does.Contain("<c r=\"A1\" s=\"1\" t=\"s\"><v>0</v></c>"));
        Assert.That(result.Xml, Does.Contain("<f>SUM(A1:A2)</f><v>5</v>"));
        Assert.That(result.Xml, Does.Contain("<c r=\"C1\" t=\"inlineStr\"><is><t>Hello</t></is></c>"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Rows.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Rows[0].RowIndex, Is.EqualTo(1));
        Assert.That(roundTrip.Rows[0].Cells.Count, Is.EqualTo(3));
        Assert.That(roundTrip.GetCell("A1")!.Value, Is.EqualTo("0"));
        Assert.That(roundTrip.GetCell("B1")!.Formula, Is.EqualTo("SUM(A1:A2)"));
        Assert.That(roundTrip.GetCell("B1")!.Value, Is.EqualTo("5"));
        Assert.That(roundTrip.GetCell("C1")!.Type, Is.EqualTo("inlineStr"));
        Assert.That(roundTrip.GetCell("C1")!.InlineStringText, Is.EqualTo("Hello"));
    }

    [Test]
    public void WriteString_WritesFullWorksheetMetadataFromFields()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.HasSheetProperties = true;
        part.SheetProperties.CodeName = "SheetCode";
        part.SheetProperties.EnableFormatConditionsCalculation = true;
        part.SheetProperties.HasEnableFormatConditionsCalculation = true;
        part.HasDimension = true;
        part.Dimension.Reference = "A1:C3";
        part.SheetViews.Add(CreateSheetView());
        part.HasFormatProperties = true;
        part.FormatProperties.BaseColumnWidth = 10;
        part.FormatProperties.HasBaseColumnWidth = true;
        part.FormatProperties.DefaultColumnWidth = 12.5d;
        part.FormatProperties.HasDefaultColumnWidth = true;
        part.FormatProperties.DefaultRowHeight = 15d;
        part.FormatProperties.HasDefaultRowHeight = true;
        part.FormatProperties.CustomHeight = true;
        part.FormatProperties.HasCustomHeight = true;
        part.Columns.Add(CreateColumn());
        part.Rows.Add(CreateRow(1, "1:3", 2, 18d, false, true));
        part.Rows[0].Cells.Add(CreateCell("A1", "s", 1, "0", "", ""));
        part.MergedCells.Add(CreateMergeCell("A1:B1"));
        part.Hyperlinks.Add(CreateHyperlink());
        part.HasPrintOptions = true;
        part.PrintOptions!.HorizontalCentered = true;
        part.PrintOptions.HasHorizontalCentered = true;
        part.PrintOptions.GridLines = false;
        part.PrintOptions.HasGridLines = true;
        part.HasPageMargins = true;
        part.PageMargins!.Left = 0.7d;
        part.PageMargins.Right = 0.7d;
        part.PageMargins.Top = 0.75d;
        part.PageMargins.Bottom = 0.75d;
        part.PageMargins.Header = 0.3d;
        part.PageMargins.Footer = 0.3d;
        part.HasPageSetup = true;
        part.PageSetup!.PaperSize = 9;
        part.PageSetup.HasPaperSize = true;
        part.PageSetup.Orientation = "landscape";
        part.PageSetup.FitToWidth = 1;
        part.PageSetup.HasFitToWidth = true;
        part.PageSetup.UseFirstPageNumber = true;
        part.PageSetup.HasUseFirstPageNumber = true;
        part.HasSheetProtection = true;
        part.SheetProtection!.Sheet = true;
        part.SheetProtection.HasSheet = true;
        part.SheetProtection.Objects = false;
        part.SheetProtection.HasObjects = true;
        part.SheetProtection.Password = "ABCD";
        part.SheetProtection.AlgorithmName = "SHA-512";
        part.SheetProtection.HashValue = "hash==";
        part.SheetProtection.SaltValue = "salt==";
        part.SheetProtection.SpinCount = 100000;
        part.SheetProtection.HasSpinCount = true;
        part.HasExtensionList = true;
        part.ExtensionList.NamespaceUri = XlsxNamespaces.Spreadsheet;
        part.ExtensionList.Extensions.Add(CreateExtension());

        XlsxWorksheetPartWriteResult result = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<sheetPr codeName=\"SheetCode\" enableFormatConditionsCalculation=\"1\" />"));
        Assert.That(result.Xml, Does.Contain("<dimension ref=\"A1:C3\" />"));
        Assert.That(result.Xml, Does.Contain("<sheetViews>"));
        Assert.That(result.Xml, Does.Contain("<cols>"));
        Assert.That(result.Xml, Does.Contain("<mergeCells count=\"1\">"));
        Assert.That(result.Xml, Does.Contain("r:id=\"rId5\""));
        Assert.That(result.Xml, Does.Contain("<printOptions"));
        Assert.That(result.Xml, Does.Contain("<pageMargins"));
        Assert.That(result.Xml, Does.Contain("<pageSetup"));
        Assert.That(result.Xml, Does.Contain("<sheetProtection"));
        Assert.That(result.Xml, Does.Contain("<ext uri=\"{44444444-4444-4444-4444-444444444444}\" />"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.HasSheetProperties, Is.True);
        Assert.That(roundTrip.SheetProperties.CodeName, Is.EqualTo("SheetCode"));
        Assert.That(roundTrip.HasDimension, Is.True);
        Assert.That(roundTrip.SheetViews.Count, Is.EqualTo(1));
        Assert.That(roundTrip.SheetViews[0].ZoomScale, Is.EqualTo(125));
        Assert.That(roundTrip.HasFormatProperties, Is.True);
        Assert.That(roundTrip.Columns.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Columns[0].Width, Is.EqualTo(20d).Within(0.0000001d));
        Assert.That(roundTrip.MergedCells.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Hyperlinks.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Hyperlinks[0].RelationshipId, Is.EqualTo("rId5"));
        Assert.That(roundTrip.HasPrintOptions, Is.True);
        Assert.That(roundTrip.HasPageMargins, Is.True);
        Assert.That(roundTrip.HasPageSetup, Is.True);
        Assert.That(roundTrip.PageSetup!.Orientation, Is.EqualTo("landscape"));
        Assert.That(roundTrip.HasSheetProtection, Is.True);
        Assert.That(roundTrip.SheetProtection!.Password, Is.EqualTo("ABCD"));
        Assert.That(roundTrip.HasExtensionList, Is.True);
        Assert.That(roundTrip.ExtensionList.Extensions.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteString_RoundTripsLoadedWorksheetMetadataAndUnknownXml()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\" xmlns:x=\"urn:x\" x:root=\"r\">" +
            "<sheetPr codeName=\"Loaded\" enableFormatConditionsCalculation=\"1\" x:prop=\"p\"><tabColor rgb=\"FFFF0000\" /></sheetPr>" +
            "<dimension ref=\"A1:B2\" />" +
            "<sheetViews><sheetView workbookViewId=\"0\" showGridLines=\"0\" x:view=\"v\"><pane xSplit=\"1\" /></sheetView></sheetViews>" +
            "<sheetFormatPr defaultRowHeight=\"15\" x:format=\"f\" />" +
            "<cols><col min=\"1\" max=\"1\" width=\"20\" customWidth=\"1\" x:col=\"c\" /></cols>" +
            "<sheetData><row r=\"1\" x:row=\"r\"><c r=\"A1\" t=\"inlineStr\" x:cell=\"c\"><is><t>Hello</t></is></c></row></sheetData>" +
            "<mergeCells count=\"1\"><mergeCell ref=\"A1:B1\" /></mergeCells>" +
            "<hyperlinks><hyperlink ref=\"A1\" r:id=\"rId9\" display=\"Loaded\" x:hyperlink=\"h\" /></hyperlinks>" +
            "<printOptions horizontalCentered=\"1\" />" +
            "<pageMargins left=\"0.7\" right=\"0.7\" top=\"0.75\" bottom=\"0.75\" header=\"0.3\" footer=\"0.3\" />" +
            "<pageSetup paperSize=\"9\" orientation=\"portrait\" x:setup=\"s\" />" +
            "<sheetProtection sheet=\"1\" password=\"ABCD\" x:protection=\"p\" />" +
            "<extLst><ext uri=\"{55555555-5555-5555-5555-555555555555}\"><x:future /></ext></extLst>" +
            "<x:future />" +
            "</worksheet>";
        XlsxWorksheetPart loaded = new XlsxWorksheetPartReader().ReadString(xml);

        XlsxWorksheetPartWriteResult result = new XlsxWorksheetPartWriter().WriteString(loaded);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(result.Xml);

        Assert.That(loaded.IsValid, Is.True);
        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("tabColor"));
        Assert.That(result.Xml, Does.Contain("x:future"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.UnknownRootAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.SheetProperties.RawXml, Does.Contain("tabColor"));
        Assert.That(roundTrip.SheetViews[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.SheetViews[0].HasPane, Is.True);
        Assert.That(roundTrip.SheetViews[0].Pane.XSplit, Is.EqualTo(1d).Within(0.0000001d));
        Assert.That(roundTrip.SheetViews[0].UnknownXml.Count, Is.EqualTo(0));
        Assert.That(roundTrip.FormatProperties.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Columns[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Rows[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.GetCell("A1")!.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Hyperlinks[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.PageSetup!.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.SheetProtection!.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.HasExtensionList, Is.True);
        Assert.That(roundTrip.UnknownXml.Count, Is.EqualTo(1));
    }

    [Test]
    public void WriteBytes_ProducesIdenticalBytesForEquivalentSheets()
    {
        XlsxWorksheetPart first = new XlsxWorksheetPart();
        first.Rows.Add(CreateRow(1, "", 0, 0d, false, false));
        first.Rows[0].Cells.Add(CreateCell("A1", "n", 0, "123.5", "", ""));
        XlsxWorksheetPart second = new XlsxWorksheetPart();
        second.Rows.Add(CreateRow(1, "", 0, 0d, false, false));
        second.Rows[0].Cells.Add(CreateCell("A1", "n", 0, "123.5", "", ""));
        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();

        XlsxWorksheetPartWriteResult firstResult = writer.WriteBytes(first);
        XlsxWorksheetPartWriteResult secondResult = writer.WriteBytes(second);

        Assert.That(firstResult.IsValid, Is.True);
        Assert.That(secondResult.IsValid, Is.True);
        Assert.That(firstResult.Bytes, Is.EqualTo(secondResult.Bytes));
    }

    [Test]
    public void WriteString_WritesEmptySheetDataForEmptyWorksheet()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetPartWriteResult result = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(result.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Xml, Does.Contain("<sheetData />"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.Rows.Count, Is.EqualTo(0));
    }

    [Test]
    public void WriteStream_WritesToProvidedStreamWithoutClosingIt()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.Rows.Add(CreateRow(1, "", 0, 0d, false, false));
        part.Rows[0].Cells.Add(CreateCell("A1", "", 0, "1", "", ""));
        MemoryStream stream = new MemoryStream();

        XlsxWorksheetPartWriteResult result = new XlsxWorksheetPartWriter().WriteStream(stream, part);

        long lengthAfterWrite = stream.Length;
        stream.WriteByte(32);

        Assert.That(result.IsValid, Is.True);
        Assert.That(lengthAfterWrite, Is.GreaterThan(0));
        Assert.That(stream.Length, Is.EqualTo(lengthAfterWrite + 1));
        stream.Dispose();
    }

    [Test]
    public void WriteBytes_RejectsNullPartRowsAndCells()
    {
        XlsxWorksheetPart nullRows = new XlsxWorksheetPart();
        nullRows.Rows = null!;
        XlsxWorksheetPart nullRow = new XlsxWorksheetPart();
        nullRow.Rows.Add(null!);
        XlsxWorksheetPart nullCells = new XlsxWorksheetPart();
        nullCells.Rows.Add(new XlsxWorksheetRow());
        nullCells.Rows[0].Cells = null!;
        XlsxWorksheetPart nullCell = new XlsxWorksheetPart();
        nullCell.Rows.Add(new XlsxWorksheetRow());
        nullCell.Rows[0].Cells.Add(null!);
        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();

        XlsxWorksheetPartWriteResult? nullPartResult = writer.WriteBytes(null!);
        XlsxWorksheetPartWriteResult nullRowsResult = writer.WriteBytes(nullRows);
        XlsxWorksheetPartWriteResult nullRowResult = writer.WriteBytes(nullRow);
        XlsxWorksheetPartWriteResult nullCellsResult = writer.WriteBytes(nullCells);
        XlsxWorksheetPartWriteResult nullCellResult = writer.WriteBytes(nullCell);

        Assert.That(nullPartResult.IsValid, Is.False);
        Assert.That(nullRowsResult.IsValid, Is.False);
        Assert.That(nullRowResult.IsValid, Is.False);
        Assert.That(nullCellsResult.IsValid, Is.False);
        Assert.That(nullCellResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullPartResult, "worksheet.partNull"), Is.True);
        Assert.That(HasDiagnostic(nullRowsResult, "worksheet.rowsNull"), Is.True);
        Assert.That(HasDiagnostic(nullRowResult, "worksheet.rowNull"), Is.True);
        Assert.That(HasDiagnostic(nullCellsResult, "worksheet.cellsNull"), Is.True);
        Assert.That(HasDiagnostic(nullCellResult, "worksheet.cellNull"), Is.True);
    }

    static XlsxWorksheetRow CreateRow(uint rowIndex, string spans, uint style, double height, bool hidden, bool customHeight)
    {
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = rowIndex;
        row.Spans = spans;
        if (style > 0)
        {
            row.Style = style;
            row.HasStyle = true;
        }

        if (height > 0)
        {
            row.Height = height;
            row.HasHeight = true;
        }

        row.Hidden = hidden;
        row.HasHidden = true;
        row.CustomHeight = customHeight;
        row.HasCustomHeight = true;
        return row;
    }

    static XlsxWorksheetCell CreateCell(string reference, string type, uint style, string value, string formula, string inlineStringText)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Type = type;
        if (style > 0)
        {
            cell.Style = style;
            cell.HasStyle = true;
        }

        cell.Value = value;
        cell.Formula = formula;
        cell.InlineStringText = inlineStringText;
        return cell;
    }

    static XlsxWorksheetView CreateSheetView()
    {
        XlsxWorksheetView view = new XlsxWorksheetView();
        view.WorkbookViewId = 0;
        view.HasWorkbookViewId = true;
        view.View = "normal";
        view.ShowGridLines = false;
        view.HasShowGridLines = true;
        view.ShowRowColHeaders = true;
        view.HasShowRowColHeaders = true;
        view.RightToLeft = true;
        view.HasRightToLeft = true;
        view.TabSelected = true;
        view.HasTabSelected = true;
        view.ZoomScale = 125;
        view.HasZoomScale = true;
        return view;
    }

    static XlsxWorksheetColumn CreateColumn()
    {
        XlsxWorksheetColumn column = new XlsxWorksheetColumn();
        column.Min = 1;
        column.Max = 2;
        column.Width = 20d;
        column.HasWidth = true;
        column.Style = 3;
        column.HasStyle = true;
        column.Hidden = true;
        column.HasHidden = true;
        column.BestFit = true;
        column.HasBestFit = true;
        column.CustomWidth = true;
        column.HasCustomWidth = true;
        column.OutlineLevel = 2;
        column.HasOutlineLevel = true;
        column.Collapsed = false;
        column.HasCollapsed = true;
        return column;
    }

    static XlsxWorksheetMergeCell CreateMergeCell(string reference)
    {
        XlsxWorksheetMergeCell mergeCell = new XlsxWorksheetMergeCell();
        mergeCell.Reference = reference;
        return mergeCell;
    }

    static XlsxWorksheetHyperlink CreateHyperlink()
    {
        XlsxWorksheetHyperlink hyperlink = new XlsxWorksheetHyperlink();
        hyperlink.Reference = "A1";
        hyperlink.RelationshipId = "rId5";
        hyperlink.Location = "Sheet1!A1";
        hyperlink.Display = "Link";
        hyperlink.Tooltip = "Tip";
        return hyperlink;
    }

    static XlsxExtension CreateExtension()
    {
        XlsxExtension extension = new XlsxExtension();
        extension.Uri = "{44444444-4444-4444-4444-444444444444}";
        extension.NamespaceUri = XlsxNamespaces.Spreadsheet;
        return extension;
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
}
