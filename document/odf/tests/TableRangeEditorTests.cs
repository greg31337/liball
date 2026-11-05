using System.Text;

namespace odf.Tests;

public class TableRangeEditorTests
{
    [Test]
    public void ClearsContentAndFormattingIndependently()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'><table:table-row>"
            + "<table:table-cell table:style-name='KeepStyle' table:protect='true' "
            + "office:value-type='string' office:string-value='remove' table:formula='of:=1'/>"
            + "<table:table-cell table:style-name='RemoveStyle' table:protect='true' "
            + "office:value-type='float' office:value='42' table:formula='of:=40+2'/>"
            + "<table:table-cell><table:table table:name='Nested'>"
            + "<table:table-row><table:table-cell/></table:table-row>"
            + "</table:table></table:table-cell>"
            + "</table:table-row></table:table>");

        OdfTable contentCleared = TableRangeEditor.ClearContents(
            document,
            document.Tables[0],
            0,
            0,
            1,
            1);
        OdfTable nestedCleared = TableRangeEditor.ClearContents(
            document,
            contentCleared,
            0,
            2,
            1,
            1);
        OdfTable formatCleared = TableRangeEditor.ClearFormats(
            document,
            nestedCleared,
            0,
            1,
            1,
            1);
        OdfTable reread = WriteAndRead(document).Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(TableDocumentModel.GetCell(formatCleared, 0, 0)!.Value.Kind,
                Is.EqualTo(OdfValueKind.Empty));
            Assert.That(TableDocumentModel.GetCell(formatCleared, 0, 0)!.Formula,
                Is.Empty);
            Assert.That(TableDocumentModel.GetCell(formatCleared, 0, 0)!.StyleName,
                Is.EqualTo("KeepStyle"));
            Assert.That(TableDocumentModel.GetCell(formatCleared, 0, 0)!.IsProtected,
                Is.True);
            Assert.That(TableDocumentModel.GetCell(formatCleared, 0, 1)!.Value.NumericValue,
                Is.EqualTo(42));
            Assert.That(TableDocumentModel.GetCell(formatCleared, 0, 1)!.Formula,
                Is.EqualTo("of:=40+2"));
            Assert.That(TableDocumentModel.GetCell(formatCleared, 0, 1)!.StyleName,
                Is.Empty);
            Assert.That(TableDocumentModel.GetCell(formatCleared, 0, 1)!.IsProtected,
                Is.False);
            Assert.That(document.Tables.Any(table => table.Name == "Nested"), Is.False);
            Assert.That(TableDocumentModel.GetCell(reread, 0, 1)!.Value.NumericValue,
                Is.EqualTo(42));
        });
    }

    [Test]
    public void TransposesValuesStylesAndRelativeFormulaReferences()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'>"
            + "<table:table-row>" + StringCell("a", "A")
            + StringCell("b", "B")
            + "<table:table-cell table:style-name='C' table:formula='of:=[.C1]+[.$C$1]'/>"
            + "</table:table-row>"
            + "<table:table-row>" + StringCell("d", "D")
            + StringCell("e", "E") + StringCell("f", "F")
            + "</table:table-row>"
            + "<table:table-row><table:table-cell table:number-columns-repeated='3'/></table:table-row>"
            + "</table:table>");

        OdfTable transposed = TableRangeEditor.TransposeRange(
            document,
            document.Tables[0],
            0,
            0,
            2,
            3,
            0,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(CellText(transposed, 0, 0), Is.EqualTo("a"));
            Assert.That(CellText(transposed, 0, 1), Is.EqualTo("d"));
            Assert.That(CellText(transposed, 1, 0), Is.EqualTo("b"));
            Assert.That(CellText(transposed, 1, 1), Is.EqualTo("e"));
            Assert.That(CellText(transposed, 2, 1), Is.EqualTo("f"));
            Assert.That(TableDocumentModel.GetCell(transposed, 2, 0)!.StyleName,
                Is.EqualTo("C"));
            Assert.That(TableDocumentModel.GetCell(transposed, 2, 0)!.Formula,
                Is.EqualTo("of:=[.A3]+[.$A$3]"));
            Assert.That(TableDocumentModel.GetCell(transposed, 0, 2)!.Formula,
                Is.Empty);
        });
    }

    [Test]
    public void TransposeSwapsMergedSpanAxesAndCoveredSlots()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'>"
            + "<table:table-row><table:table-cell table:number-columns-spanned='2' "
            + "table:number-rows-spanned='1'/><table:covered-table-cell/></table:table-row>"
            + "<table:table-row><table:table-cell table:number-columns-repeated='2'/></table:table-row>"
            + "</table:table>");

        OdfTable result = TableRangeEditor.TransposeRange(
            document,
            document.Tables[0],
            0,
            0,
            2,
            2,
            0,
            0);
        SemanticValidationResult validation = Validate(document);

        Assert.Multiple(() =>
        {
            Assert.That(TableDocumentModel.GetCell(result, 0, 0)!.ColumnSpan,
                Is.EqualTo(1));
            Assert.That(TableDocumentModel.GetCell(result, 0, 0)!.RowSpan,
                Is.EqualTo(2));
            Assert.That(TableDocumentModel.GetCell(result, 1, 0)!.IsCovered,
                Is.True);
            Assert.That(TableDocumentModel.GetCell(result, 0, 1)!.IsCovered,
                Is.False);
            Assert.That(validation.Diagnostics.Any(
                item => item.Code == OdfDiagnosticCodes.InvalidMerge),
                Is.False);
        });
    }

    [Test]
    public void GeneratesNumericSeriesAndRepeatsFormulaAwareSeedPatterns()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'>"
            + "<table:table-row>" + StringCell("seed", "Seed")
            + "<table:table-cell table:style-name='Formula' table:formula='of:=[.A1]'/><table:table-cell/><table:table-cell/>"
            + "</table:table-row>"
            + "<table:table-row table:number-rows-repeated='2'>"
            + "<table:table-cell table:style-name='Target' table:number-columns-repeated='4'/>"
            + "</table:table-row></table:table>");

        OdfTable copied = TableRangeEditor.CopySeries(
            document,
            document.Tables[0],
            0,
            0,
            1,
            2,
            1,
            0,
            2,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(CellText(copied, 1, 0), Is.EqualTo("seed"));
            Assert.That(CellText(copied, 2, 2), Is.EqualTo("seed"));
            Assert.That(TableDocumentModel.GetCell(copied, 1, 1)!.Formula,
                Is.EqualTo("of:=[.A2]"));
            Assert.That(TableDocumentModel.GetCell(copied, 1, 3)!.Formula,
                Is.EqualTo("of:=[.C2]"));
            Assert.That(TableDocumentModel.GetCell(copied, 2, 3)!.StyleName,
                Is.EqualTo("Formula"));
        });

        OdfTable filled = TableRangeEditor.FillSeries(
            document,
            copied,
            1,
            0,
            2,
            3,
            2,
            1.5);
        OdfTable reread = WriteAndRead(document).Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(TableDocumentModel.GetCell(filled, 1, 0)!.Value.NumericValue,
                Is.EqualTo(2));
            Assert.That(TableDocumentModel.GetCell(filled, 1, 2)!.Value.NumericValue,
                Is.EqualTo(5));
            Assert.That(TableDocumentModel.GetCell(filled, 2, 2)!.Value.NumericValue,
                Is.EqualTo(9.5));
            Assert.That(TableDocumentModel.GetCell(filled, 1, 0)!.StyleName,
                Is.EqualTo("Seed"));
            Assert.That(TableDocumentModel.GetCell(reread, 2, 2)!.Value.NumericValue,
                Is.EqualTo(9.5));
        });
    }

    [Test]
    public void SearchesAndReplacesValuesAndFormulasAcrossLogicalRanges()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'><table:table-row>"
            + StringCell("Alpha alpha", "Text")
            + "<table:table-cell office:value-type='float' office:value='12'/>"
            + "<table:table-cell table:formula='of:=CONCATENATE(&quot;Alpha&quot;;[.A1])'/>"
            + "</table:table-row></table:table>");
        OdfTable table = document.Tables[0];

        OdfSearchMatch[] found = TableRangeEditor.SearchRange(
            table,
            0,
            0,
            1,
            3,
            "alpha",
            false,
            false);
        OdfSearchMatch[] exact = TableRangeEditor.SearchRange(
            table,
            0,
            0,
            1,
            3,
            "12",
            true,
            true);
        OdfSearchMatch[] replaced = TableRangeEditor.ReplaceRange(
            document,
            table,
            0,
            0,
            1,
            3,
            "alpha",
            "Beta",
            false,
            true);
        OdfTable result = document.Tables[0];
        OdfTable reread = WriteAndRead(document).Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(found, Has.Length.EqualTo(2));
            Assert.That(found[0].RowIndex, Is.Zero);
            Assert.That(found[0].ColumnIndex, Is.Zero);
            Assert.That(found[0].IsFormula, Is.False);
            Assert.That(found[1].ColumnIndex, Is.EqualTo(2));
            Assert.That(found[1].IsFormula, Is.True);
            Assert.That(exact, Has.Length.EqualTo(1));
            Assert.That(exact[0].IsExact, Is.True);
            Assert.That(replaced, Has.Length.EqualTo(2));
            Assert.That(CellText(result, 0, 0), Is.EqualTo("Beta Beta"));
            Assert.That(TableDocumentModel.GetCell(result, 0, 2)!.Formula,
                Is.EqualTo("of:=CONCATENATE(\"Beta\";[.A1])"));
            Assert.That(CellText(reread, 0, 0), Is.EqualTo("Beta Beta"));
        });
    }

    private static string StringCell(string value, string styleName)
    {
        return "<table:table-cell table:style-name='" + styleName
            + "' office:value-type='string' office:string-value='"
            + value + "'/>";
    }

    private static string CellText(
        OdfTable table,
        long rowIndex,
        long columnIndex)
    {
        OdfCell? cell = TableDocumentModel.GetCell(
            table,
            rowIndex,
            columnIndex);
        return cell == null ? "" : cell.Value.StringValue;
    }

    private static SemanticValidationResult Validate(OdfDocument document)
    {
        OdfValidationOptions options = new OdfValidationOptions();
        options.Mode = OdfValidationMode.Complete;
        options.Version = OdfVersion.Version14;
        return new SemanticDocumentValidator(options).ValidateDocument(document);
    }

    private static OdfDocument ReadSpreadsheet(string tables)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:of='" + OdfNamespaces.OpenFormula + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:body><office:spreadsheet>"
            + tables
            + "</office:spreadsheet></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "range-editing.fods",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "range-editing.fods",
            new OdfLoadOptions());
    }
}
