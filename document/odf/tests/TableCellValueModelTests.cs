using System.Text;

namespace odf.Tests;

public class TableCellValueModelTests
{
    [Test]
    public void MaterializesEverySupportedCellValueAndCachedFormulaResult()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table-row>"
            + "<table:table-cell/>"
            + "<table:table-cell office:value-type='string'>"
            + "<text:p>Hello<text:s text:c='2'/>world</text:p>"
            + "<text:p>again</text:p></table:table-cell>"
            + "<table:table-cell office:value-type='float' office:value='12.5'/>"
            + "<table:table-cell office:value-type='percentage' office:value='0.25'/>"
            + "<table:table-cell office:value-type='currency' office:value='9.75' "
            + "office:currency='USD'/>"
            + "<table:table-cell office:value-type='date' office:date-value='2026-09-02'/>"
            + "<table:table-cell office:value-type='date' "
            + "office:date-value='2026-09-02T13:14:15Z'/>"
            + "<table:table-cell office:value-type='time' office:time-value='PT2H3M4.5S'/>"
            + "<table:table-cell office:value-type='boolean' office:boolean-value='1'/>"
            + "<table:table-cell table:formula='of:=1+1' "
            + "office:value-type='float' office:value='2'/>"
            + "<table:table-cell table:formula='of:=1/0' office:value-type='string' "
            + "office:string-value='#DIV/0!'><text:p>#DIV/0!</text:p></table:table-cell>"
            + "</table:table-row>");
        OdfCell[] cells = document.Tables[0].Rows[0].Cells;

        Assert.Multiple(() =>
        {
            Assert.That(document.CellValues, Has.Length.EqualTo(11));
            Assert.That(cells[0].Value.Kind, Is.EqualTo(OdfValueKind.Empty));
            Assert.That(cells[0].Value.IsValid, Is.True);
            Assert.That(cells[1].Value.Kind, Is.EqualTo(OdfValueKind.String));
            Assert.That(cells[1].Value.StringValue, Is.EqualTo("Hello  world\nagain"));
            Assert.That(cells[2].Value.Kind, Is.EqualTo(OdfValueKind.Float));
            Assert.That(cells[2].Value.NumericValue, Is.EqualTo(12.5));
            Assert.That(cells[3].Value.Kind, Is.EqualTo(OdfValueKind.Percentage));
            Assert.That(cells[3].Value.UnitKind, Is.EqualTo(OdfUnitKind.Percentage));
            Assert.That(cells[4].Value.Kind, Is.EqualTo(OdfValueKind.Currency));
            Assert.That(cells[4].Value.Currency, Is.EqualTo("USD"));
            Assert.That(cells[5].Value.Kind, Is.EqualTo(OdfValueKind.Date));
            Assert.That(cells[6].Value.Kind, Is.EqualTo(OdfValueKind.DateTime));
            Assert.That(cells[7].Value.Kind, Is.EqualTo(OdfValueKind.Duration));
            Assert.That(cells[7].Value.DurationValue, Is.EqualTo("PT2H3M4.5S"));
            Assert.That(cells[8].Value.BooleanValue, Is.True);
            Assert.That(cells[9].Formula, Is.EqualTo("of:=1+1"));
            Assert.That(cells[9].Value.IsCachedFormulaResult, Is.True);
            Assert.That(cells[10].Value.Kind, Is.EqualTo(OdfValueKind.Error));
            Assert.That(cells[10].Value.Error, Is.EqualTo(OdfFormulaError.DivisionByZero));
            Assert.That(cells[10].Value.Cell, Is.SameAs(cells[10]));
            Assert.That(document.CellValues[10], Is.SameAs(cells[10].Value));
        });
    }

    [Test]
    public void WritesEditedTypedValuesAndFormulaCachesThroughRoundTrip()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table-row>"
            + "<table:table-cell/><table:table-cell/><table:table-cell/>"
            + "<table:table-cell/><table:table-cell/><table:table-cell/>"
            + "<table:table-cell/><table:table-cell/><table:table-cell/>"
            + "<table:table-cell office:value-type='string' office:string-value='clear'/>"
            + "</table:table-row>");
        OdfCell[] cells = document.Tables[0].Rows[0].Cells;
        cells[0].Value.Kind = OdfValueKind.String;
        cells[0].Value.StringValue = "written";
        cells[1].Value.Kind = OdfValueKind.Float;
        cells[1].Value.NumericValue = 42.25;
        cells[2].Value.Kind = OdfValueKind.Percentage;
        cells[2].Value.NumericValue = 0.375;
        cells[3].Value.Kind = OdfValueKind.Currency;
        cells[3].Value.NumericValue = 19.99;
        cells[3].Value.Currency = "EUR";
        cells[4].Value.Kind = OdfValueKind.Date;
        cells[4].Value.DateValue = "2027-01-03";
        cells[5].Value.Kind = OdfValueKind.DateTime;
        cells[5].Value.DateValue = "2027-01-03T04:05:06Z";
        cells[6].Value.Kind = OdfValueKind.Duration;
        cells[6].Value.DurationValue = "P2DT3H";
        cells[7].Value.Kind = OdfValueKind.Boolean;
        cells[7].Value.BooleanValue = true;
        cells[8].Formula = "of:=1/0";
        cells[8].Value.Kind = OdfValueKind.Error;
        cells[8].Value.Error = OdfFormulaError.DivisionByZero;
        cells[9].Value.Kind = OdfValueKind.Empty;
        cells[9].Value.ValueType = "";
        OdfDocument reread = WriteAndRead(document);
        OdfCell[] result = reread.Tables[0].Rows[0].Cells;

        Assert.Multiple(() =>
        {
            Assert.That(result[0].Value.StringValue, Is.EqualTo("written"));
            Assert.That(result[1].Value.NumericValue, Is.EqualTo(42.25));
            Assert.That(result[2].Value.Kind, Is.EqualTo(OdfValueKind.Percentage));
            Assert.That(result[2].Value.NumericValue, Is.EqualTo(0.375));
            Assert.That(result[3].Value.Currency, Is.EqualTo("EUR"));
            Assert.That(result[3].Value.NumericValue, Is.EqualTo(19.99));
            Assert.That(result[4].Value.Kind, Is.EqualTo(OdfValueKind.Date));
            Assert.That(result[4].Value.DateValue, Is.EqualTo("2027-01-03"));
            Assert.That(result[5].Value.Kind, Is.EqualTo(OdfValueKind.DateTime));
            Assert.That(result[6].Value.DurationValue, Is.EqualTo("P2DT3H"));
            Assert.That(result[7].Value.BooleanValue, Is.True);
            Assert.That(result[8].Formula, Is.EqualTo("of:=1/0"));
            Assert.That(result[8].Value.Kind, Is.EqualTo(OdfValueKind.Error));
            Assert.That(result[8].Value.IsCachedFormulaResult, Is.True);
            Assert.That(result[9].Value.Kind, Is.EqualTo(OdfValueKind.Empty));
            Assert.That(AttributeValue(result[9].Node!, "value-type"), Is.Empty);
        });
    }

    [Test]
    public void PreservesInvalidLexicalValuesUntilTheyAreExplicitlyEdited()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table-row>"
            + "<table:table-cell office:value-type='float' office:value='not-a-number'/>"
            + "<table:table-cell office:value='3'/>"
            + "<table:table-cell office:value-type='currency' office:value='4'/>"
            + "</table:table-row>");
        OdfCell[] original = document.Tables[0].Rows[0].Cells;

        Assert.Multiple(() =>
        {
            Assert.That(original[0].Value.Kind, Is.EqualTo(OdfValueKind.Float));
            Assert.That(original[0].Value.IsValid, Is.False);
            Assert.That(original[0].Value.LexicalValue, Is.EqualTo("not-a-number"));
            Assert.That(original[0].Value.Provenance,
                Is.EqualTo(OdfValueProvenance.InvalidPreserved));
            Assert.That(original[1].Value.IsValid, Is.False);
            Assert.That(original[2].Value.IsValid, Is.False);
        });

        OdfDocument reread = WriteAndRead(document);
        OdfCell[] result = reread.Tables[0].Rows[0].Cells;

        Assert.Multiple(() =>
        {
            Assert.That(result[0].Value.LexicalValue, Is.EqualTo("not-a-number"));
            Assert.That(AttributeValue(result[0].Node!, "value"),
                Is.EqualTo("not-a-number"));
            Assert.That(AttributeValue(result[1].Node!, "value"), Is.EqualTo("3"));
            Assert.That(result[2].Value.Currency, Is.Empty);
            Assert.That(result[2].Value.IsValid, Is.False);
        });
    }

    [Test]
    public void FormulaStateDeterminesWhetherTheTypedValueIsACache()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table-row><table:table-cell table:formula='of:=6*7' "
            + "office:value-type='float' office:value='42'/></table:table-row>");
        OdfCell cell = document.Tables[0].Rows[0].Cells[0];
        cell.Formula = "";

        OdfDocument reread = WriteAndRead(document);
        OdfCell result = reread.Tables[0].Rows[0].Cells[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Formula, Is.Empty);
            Assert.That(result.Value.Kind, Is.EqualTo(OdfValueKind.Float));
            Assert.That(result.Value.NumericValue, Is.EqualTo(42));
            Assert.That(result.Value.IsCachedFormulaResult, Is.False);
        });
    }

    [Test]
    public void ReplacingTheValueObjectClearsAnExistingTypedValue()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table-row><table:table-cell office:value-type='float' "
            + "office:value='5'/></table:table-row>");
        OdfCell cell = document.Tables[0].Rows[0].Cells[0];
        cell.Value = new OdfCellValue
        {
            Kind = OdfValueKind.Empty
        };

        OdfDocument reread = WriteAndRead(document);
        OdfCell result = reread.Tables[0].Rows[0].Cells[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Value.Kind, Is.EqualTo(OdfValueKind.Empty));
            Assert.That(AttributeValue(result.Node!, "value-type"), Is.Empty);
            Assert.That(AttributeValue(result.Node!, "value"), Is.Empty);
        });
    }

    private static string AttributeValue(OdfNode node, string localName)
    {
        OdfAttribute? attribute = node.Attributes.SingleOrDefault(
            item => item.Name.NamespaceName == OdfNamespaces.Office
                && item.Name.LocalName == localName);
        return attribute == null ? "" : attribute.Value;
    }

    private static OdfDocument ReadSpreadsheet(string rows)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:of='" + OdfFormulaNames.OpenFormulaNamespace + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:styles/><office:body><office:spreadsheet>"
            + "<table:table table:name='Values'>"
            + rows
            + "</table:table></office:spreadsheet></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "cell-values.fods",
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
            "cell-values.fods",
            new OdfLoadOptions());
    }
}
