using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class StreamingSpreadsheetProjectorTests
{
    [Test]
    public void ProjectsCompactRepeatedRowsAndCellsWithoutExpansion()
    {
        string rows = "<table:table-header-rows><table:table-row "
            + "table:number-rows-repeated='2' table:style-name='HeaderRow'>"
            + "<table:table-cell table:number-columns-repeated='5'/>"
            + "<table:table-cell table:number-columns-repeated='3' "
            + "office:value-type='float' office:value='12.5'/>"
            + "</table:table-row></table:table-header-rows>"
            + "<table:table-row-group><table:table-row "
            + "table:number-rows-repeated='4'>"
            + "<table:covered-table-cell table:number-columns-repeated='2'/>"
            + "<table:table-cell office:value-type='string'>"
            + "<text:p>value</text:p></table:table-cell>"
            + "</table:table-row></table:table-row-group>";
        using MemoryStream input = SpreadsheetInput(
            "<table:table table:name='Sheet1'>" + rows + "</table:table>");

        OdfTable[] tables = StreamingSpreadsheetProjector.ProjectXmlTables(
            input,
            OdfPartNames.Content,
            new OdfLoadOptions { LeaveInputOpen = true });

        OdfTable table = tables.Single();
        OdfRow first = table.Rows[0];
        OdfRow second = table.Rows[1];
        Assert.Multiple(() =>
        {
            Assert.That(table.Name, Is.EqualTo("Sheet1"));
            Assert.That(table.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(table.Rows, Has.Length.EqualTo(2));
            Assert.That(table.RowCount, Is.EqualTo(6));
            Assert.That(table.ColumnCount, Is.EqualTo(8));
            Assert.That(first.StartIndex, Is.Zero);
            Assert.That(first.RepeatCount, Is.EqualTo(2));
            Assert.That(first.IsHeader, Is.True);
            Assert.That(first.StyleName, Is.EqualTo("HeaderRow"));
            Assert.That(first.Cells, Has.Length.EqualTo(2));
            Assert.That(first.Cells[0].StartColumnIndex, Is.Zero);
            Assert.That(first.Cells[0].RepeatCount, Is.EqualTo(5));
            Assert.That(first.Cells[1].StartColumnIndex, Is.EqualTo(5));
            Assert.That(first.Cells[1].RepeatCount, Is.EqualTo(3));
            Assert.That(first.Cells[1].Value.NumericValue, Is.EqualTo(12.5));
            Assert.That(second.StartIndex, Is.EqualTo(2));
            Assert.That(second.RepeatCount, Is.EqualTo(4));
            Assert.That(second.GroupDepth, Is.EqualTo(1));
            Assert.That(second.Cells[0].IsCovered, Is.True);
            Assert.That(second.Cells[0].RepeatCount, Is.EqualTo(2));
            Assert.That(second.Cells[1].Value.StringValue,
                Is.EqualTo("value"));
            Assert.That(first.Node, Is.Null);
            Assert.That(first.Cells[1].Node, Is.Null);
            Assert.That(input.CanRead, Is.True);
        });
    }

    [Test]
    public void ProjectsTypedValuesFormulaCachesAndCellDisplayText()
    {
        string cells = "<table:table-row>"
            + "<table:table-cell office:value-type='string'>"
            + "<text:p>Hello<text:s text:c='2'/>world</text:p>"
            + "<text:p>again<text:tab/>tab</text:p></table:table-cell>"
            + "<table:table-cell office:value-type='float' office:value='2.5'/>"
            + "<table:table-cell office:value-type='percentage' office:value='0.25'/>"
            + "<table:table-cell office:value-type='currency' office:value='9.75' "
            + "office:currency='USD'/>"
            + "<table:table-cell office:value-type='date' "
            + "office:date-value='2026-09-02'/>"
            + "<table:table-cell office:value-type='date' "
            + "office:date-value='2026-09-02T13:14:15Z'/>"
            + "<table:table-cell office:value-type='time' "
            + "office:time-value='PT2H3M'/>"
            + "<table:table-cell office:value-type='boolean' "
            + "office:boolean-value='true'/>"
            + "<table:table-cell table:formula='of:=1/0' "
            + "office:value-type='string' office:string-value='#DIV/0!'/>"
            + "<table:table-cell office:value-type='float' "
            + "office:value='invalid'/>"
            + "</table:table-row>";
        using MemoryStream input = SpreadsheetInput(
            "<table:table table:name='Values'>" + cells + "</table:table>");

        OdfCell[] projected =
            StreamingSpreadsheetProjector.ProjectXmlTables(
                input,
                OdfPartNames.Content,
                new OdfLoadOptions { LeaveInputOpen = true })[0]
                .Rows[0].Cells;

        Assert.Multiple(() =>
        {
            Assert.That(projected[0].Value.Kind,
                Is.EqualTo(OdfValueKind.String));
            Assert.That(projected[0].Value.StringValue,
                Is.EqualTo("Hello  world\nagain\ttab"));
            Assert.That(projected[1].Value.Kind,
                Is.EqualTo(OdfValueKind.Float));
            Assert.That(projected[1].Value.NumericValue, Is.EqualTo(2.5));
            Assert.That(projected[2].Value.Kind,
                Is.EqualTo(OdfValueKind.Percentage));
            Assert.That(projected[2].Value.UnitKind,
                Is.EqualTo(OdfUnitKind.Percentage));
            Assert.That(projected[3].Value.Kind,
                Is.EqualTo(OdfValueKind.Currency));
            Assert.That(projected[3].Value.Currency, Is.EqualTo("USD"));
            Assert.That(projected[4].Value.Kind,
                Is.EqualTo(OdfValueKind.Date));
            Assert.That(projected[5].Value.Kind,
                Is.EqualTo(OdfValueKind.DateTime));
            Assert.That(projected[6].Value.Kind,
                Is.EqualTo(OdfValueKind.Duration));
            Assert.That(projected[7].Value.BooleanValue, Is.True);
            Assert.That(projected[8].Formula, Is.EqualTo("of:=1/0"));
            Assert.That(projected[8].Value.Kind,
                Is.EqualTo(OdfValueKind.Error));
            Assert.That(projected[8].Value.Error,
                Is.EqualTo(OdfFormulaError.DivisionByZero));
            Assert.That(projected[8].Value.IsCachedFormulaResult, Is.True);
            Assert.That(projected[8].Value.Cell, Is.SameAs(projected[8]));
            Assert.That(projected[9].Value.IsValid, Is.False);
            Assert.That(projected[9].Value.LexicalValue,
                Is.EqualTo("invalid"));
            Assert.That(projected[9].Value.Provenance,
                Is.EqualTo(OdfValueProvenance.InvalidPreserved));
        });
    }

    [Test]
    public void KeepsNestedTablesSparseAndOutOfOuterCellText()
    {
        string nested = "<table:table table:name='Outer'><table:table-row>"
            + "<table:table-cell office:value-type='string'><text:p>Before"
            + "<table:table table:name='Inner'><table:table-row>"
            + "<table:table-cell table:number-columns-repeated='10' "
            + "office:value-type='string'><text:p>Nested</text:p>"
            + "</table:table-cell></table:table-row></table:table>After</text:p>"
            + "</table:table-cell></table:table-row></table:table>";
        using MemoryStream input = SpreadsheetInput(nested);

        OdfTable[] tables = StreamingSpreadsheetProjector.ProjectXmlTables(
            input,
            OdfPartNames.Content,
            new OdfLoadOptions { LeaveInputOpen = true });

        OdfCell outerCell = tables[0].Rows[0].Cells[0];
        OdfTable inner = tables[1];
        Assert.Multiple(() =>
        {
            Assert.That(tables.Select(item => item.Name),
                Is.EqualTo(new[] { "Outer", "Inner" }));
            Assert.That(outerCell.Value.StringValue, Is.EqualTo("BeforeAfter"));
            Assert.That(outerCell.NestedTables.Single(), Is.SameAs(inner));
            Assert.That(inner.ParentTable, Is.SameAs(tables[0]));
            Assert.That(inner.ParentCell, Is.SameAs(outerCell));
            Assert.That(inner.ColumnCount, Is.EqualTo(10));
            Assert.That(inner.Rows[0].Cells[0].Value.StringValue,
                Is.EqualTo("Nested"));
        });
    }

    [Test]
    public void ProjectsOnlySelectedPackageParts()
    {
        byte[] package = CreatePackage();
        OdfLoadOptions options = new OdfLoadOptions
        {
            LeaveInputOpen = true,
            StreamSelection = new OdfStreamSelection
            {
                PartPaths = [OdfPartNames.Content]
            }
        };
        using MemoryStream input = new MemoryStream(package);

        OdfTable[] tables =
            StreamingSpreadsheetProjector.ProjectPackageTables(
                input,
                options);

        Assert.Multiple(() =>
        {
            Assert.That(tables, Has.Length.EqualTo(1));
            Assert.That(tables[0].Name, Is.EqualTo("PackageSheet"));
            Assert.That(tables[0].Rows[0].RepeatCount, Is.EqualTo(100));
            Assert.That(tables[0].Rows[0].Cells[0].RepeatCount,
                Is.EqualTo(200));
            Assert.That(input.CanRead, Is.True);
        });
    }

    static byte[] CreatePackage()
    {
        string table = "<table:table table:name='PackageSheet'>"
            + "<table:table-row table:number-rows-repeated='100'>"
            + "<table:table-cell table:number-columns-repeated='200' "
            + "office:value-type='boolean' office:boolean-value='false'/>"
            + "</table:table-row></table:table>";
        string content = SpreadsheetXml(table);
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(
            output,
            ZipArchiveMode.Create,
            true))
        {
            WriteEntry(
                archive,
                OdfPartNames.Mimetype,
                OdfMediaTypes.Spreadsheet,
                CompressionLevel.NoCompression);
            WriteEntry(
                archive,
                OdfPartNames.Styles,
                "unrelated malformed XML",
                CompressionLevel.Optimal);
            WriteEntry(
                archive,
                OdfPartNames.Content,
                content,
                CompressionLevel.Optimal);
        }
        return output.ToArray();
    }

    static void WriteEntry(
        ZipArchive archive,
        string path,
        string value,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        using Stream stream = entry.Open();
        byte[] data = Encoding.UTF8.GetBytes(value);
        stream.Write(data, 0, data.Length);
    }

    static MemoryStream SpreadsheetInput(string tableXml)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(
            SpreadsheetXml(tableXml)));
    }

    static string SpreadsheetXml(string tableXml)
    {
        return "<office:document-content xmlns:office='"
            + OdfNamespaces.Office + "' xmlns:table='"
            + OdfNamespaces.Table + "' xmlns:text='"
            + OdfNamespaces.Text + "'><office:body><office:spreadsheet>"
            + tableXml
            + "</office:spreadsheet></office:body></office:document-content>";
    }
}
