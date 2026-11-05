using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class StreamingSheetReaderTests
{
    [Test]
    public void Read_AdvancesRowCursorAndFormatsCellValues()
    {
        string xml = "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\">" +
            "<sheetData>" +
            "<row r=\"1\"><c r=\"A1\" s=\"0\"><v>1234.5</v></c><c r=\"B1\" t=\"s\"><v>0</v></c><c r=\"C1\" t=\"inlineStr\"><is><t>Inline</t></is></c><c r=\"D1\" t=\"b\"><v>1</v></c></row>" +
            "<row r=\"2\"><c r=\"A2\" s=\"1\"><v>61</v></c><c r=\"B2\" s=\"2\"><v>0.125</v></c></row>" +
            "</sheetData>" +
            "</worksheet>";
        XlsxSharedStringTablePart sharedStrings = CreateSharedStrings();
        XlsxStylesPart styles = CreateStyles();
        XlsxStreamingSheetOpenResult openResult = new XlsxStreamingSheetReader().OpenStringWithWorkbookContext(xml, sharedStrings, styles, false);
        IXlsxStreamingSheetRowCursorOperations cursorOperations = new XlsxStreamingSheetRowCursorOperations();
        IXlsxStreamingSheetRowOperations rowOperations = new XlsxStreamingSheetRowOperations();
        XlsxStreamingSheetRowCursor cursor = openResult.Cursor!;

        bool first = cursorOperations.Read(cursor);
        XlsxStreamingSheetRow? row1 = cursor.Current;
        bool second = cursorOperations.Read(cursor);
        XlsxStreamingSheetRow? row2 = cursor.Current;
        bool third = cursorOperations.Read(cursor);

        Assert.That(openResult.IsValid, Is.True);
        Assert.That(first, Is.True);
        Assert.That(row1!.RowIndex, Is.EqualTo(1));
        Assert.That(row1.Cells.Count, Is.EqualTo(4));
        Assert.That(rowOperations.GetCell(row1, "A1")!.Value.Kind, Is.EqualTo(XlsxCellValueKind.Number));
        Assert.That(row1.GetCell("A1")!.FormattedValue, Is.EqualTo("1,234.50"));
        Assert.That(row1.GetCell("B1")!.Value.Kind, Is.EqualTo(XlsxCellValueKind.Text));
        Assert.That(row1.GetCell("B1")!.FormattedValue, Is.EqualTo("Ada"));
        Assert.That(row1.GetCell("C1")!.FormattedValue, Is.EqualTo("Inline"));
        Assert.That(row1.GetCell("D1")!.FormattedValue, Is.EqualTo("TRUE"));
        Assert.That(second, Is.True);
        Assert.That(row2!.RowIndex, Is.EqualTo(2));
        Assert.That(row2.GetCell("A2")!.FormatCode, Is.EqualTo("m/d/yy"));
        Assert.That(row2.GetCell("A2")!.FormattedValue, Is.EqualTo("3/1/00"));
        Assert.That(row2.GetCell("B2")!.FormattedValue, Is.EqualTo("12.5%"));
        Assert.That(third, Is.False);
        Assert.That(cursor.RowsRead, Is.EqualTo(2));
        Assert.That(cursor.CellsRead, Is.EqualTo(6));
        Assert.That(cursor.WorksheetPart.Rows.Count, Is.EqualTo(0));
        cursorOperations.Close(cursor);
        Assert.That(cursor.EndOfRows, Is.True);
    }

    [Test]
    public void Read_DoesNotAccumulateRowsAcrossCursorReads()
    {
        XlsxStreamingSheetOpenResult openResult = new XlsxStreamingSheetReader().OpenStringWithWorkbookContext(CreateManyRowsWorksheet(75), new XlsxSharedStringTablePart(), new XlsxStylesPart(), false);
        int rows = 0;
        bool neverAccumulated = true;

        while (openResult.Cursor!.Read())
        {
            rows = rows + 1;
            if (openResult.Cursor.WorksheetPart.Rows.Count != 0)
            {
                neverAccumulated = false;
            }
        }

        Assert.That(openResult.IsValid, Is.True);
        Assert.That(rows, Is.EqualTo(75));
        Assert.That(openResult.Cursor.RowsRead, Is.EqualTo(75));
        Assert.That(neverAccumulated, Is.True);
    }

    [Test]
    public void Read_ReportsRowCellAndSharedStringDiagnostics()
    {
        string xml = "<worksheet xmlns=\"" + XlsxNamespaces.Spreadsheet + "\">" +
            "<sheetData><row r=\"bad\"><c r=\"A1\" t=\"n\"><v>bad</v></c><c r=\"B1\" t=\"s\"><v>4</v></c></row></sheetData>" +
            "</worksheet>";
        XlsxStreamingSheetOpenResult openResult = new XlsxStreamingSheetReader().OpenStringWithWorkbookContext(xml, CreateSharedStrings(), new XlsxStylesPart(), false);

        bool read = openResult.Cursor!.Read();

        Assert.That(read, Is.True);
        Assert.That(openResult.Cursor.IsValid, Is.False);
        Assert.That(HasDiagnostic(openResult.Cursor, "xml.unsignedInteger.invalid"), Is.True);
        Assert.That(HasDiagnostic(openResult.Cursor, "xml.double.invalid"), Is.True);
        Assert.That(HasDiagnostic(openResult.Cursor, "streamingSheet.sharedStringMissing"), Is.True);
        Assert.That(openResult.Cursor.Current!.GetCell("A1")!.Value.Kind, Is.EqualTo(XlsxCellValueKind.UnknownRawValue));
        Assert.That(openResult.Cursor.Current.GetCell("B1")!.FormattedValue, Is.EqualTo(""));
    }

    [Test]
    public void OpenBytes_ReportsEmptyInput()
    {
        XlsxStreamingSheetOpenResult result = new XlsxStreamingSheetReader().OpenBytes(new byte[0]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(HasDiagnostic(result, "streamingSheet.open.bytesEmpty"), Is.True);
    }

    static XlsxSharedStringTablePart CreateSharedStrings()
    {
        XlsxSharedStringTablePart part = new XlsxSharedStringTablePart();
        XlsxSharedStringItem item = new XlsxSharedStringItem();
        item.Text = "Ada";
        part.Items.Add(item);
        return part;
    }

    static XlsxStylesPart CreateStyles()
    {
        XlsxStylesPart styles = new XlsxStylesPart();
        XlsxNumberFormatDefinition percentFormat = new XlsxNumberFormatDefinition();
        percentFormat.NumberFormatId = 164;
        percentFormat.HasNumberFormatId = true;
        percentFormat.FormatCode = "0.0%";
        styles.NumberFormats.Add(percentFormat);
        styles.CellFormats.Add(CreateCellFormat(4));
        styles.CellFormats.Add(CreateCellFormat(14));
        styles.CellFormats.Add(CreateCellFormat(164));
        return styles;
    }

    static XlsxCellFormatDefinition CreateCellFormat(uint numberFormatId)
    {
        XlsxCellFormatDefinition format = new XlsxCellFormatDefinition();
        format.NumberFormatId = numberFormatId;
        format.HasNumberFormatId = true;
        format.ApplyNumberFormat = true;
        format.HasApplyNumberFormat = true;
        return format;
    }

    static string CreateManyRowsWorksheet(int rowCount)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        builder.Append("<worksheet xmlns=\"");
        builder.Append(XlsxNamespaces.Spreadsheet);
        builder.Append("\"><sheetData>");
        int row = 1;
        while (row <= rowCount)
        {
            builder.Append("<row r=\"");
            builder.Append(row.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append("\"><c r=\"A");
            builder.Append(row.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append("\"><v>");
            builder.Append(row.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append("</v></c></row>");
            row = row + 1;
        }

        builder.Append("</sheetData></worksheet>");
        return builder.ToString();
    }

    static bool HasDiagnostic(XlsxStreamingSheetOpenResult result, string code)
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

    static bool HasDiagnostic(XlsxStreamingSheetRowCursor cursor, string code)
    {
        foreach (XlsxDiagnostic diagnostic in cursor.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }
}
