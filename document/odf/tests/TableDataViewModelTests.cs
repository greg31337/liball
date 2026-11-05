using System.Text;

namespace odf.Tests;

public class TableDataViewModelTests
{
    [Test]
    public void MaterializesTableFormattingProtectionPrintAndTemplateViews()
    {
        OdfDocument document = ReadSpreadsheet(
            StyleDefinitions(),
            "<table:table table:name='Main' table:style-name='TableStyle' "
            + "table:template-name='Banding' table:use-first-row-styles='true' "
            + "table:use-banding-rows-styles='1' table:protected='true' "
            + "table:protection-key='encoded' "
            + "table:protection-key-digest-algorithm='urn:example:digest' "
            + "table:print='false' table:print-ranges='.A1:.B2 Other.C3:Other.D4'>"
            + "<table:title>Sales</table:title><table:desc>Quarterly data</table:desc>"
            + "<table:table-header-columns><table:table-column "
            + "table:number-columns-repeated='2' table:style-name='ColumnStyle' "
            + "table:default-cell-style-name='ColumnCell' table:visibility='filter'/>"
            + "</table:table-header-columns>"
            + "<table:table-header-rows><table:table-row "
            + "table:number-rows-repeated='2' table:style-name='RowStyle' "
            + "table:default-cell-style-name='RowCell' table:visibility='collapse'>"
            + "<table:table-cell table:protect='true'/><table:table-cell "
            + "table:style-name='DirectCell'/></table:table-row>"
            + "</table:table-header-rows></table:table>"
            + "<table:table table:name='Other'><table:table-row>"
            + "<table:table-cell/></table:table-row></table:table>");
        OdfTable table = document.Tables[0];
        OdfRow row = table.Rows[0];
        OdfColumn column = table.Columns[0];

        Assert.Multiple(() =>
        {
            Assert.That(table.Style!.Name, Is.EqualTo("TableStyle"));
            Assert.That(table.DefaultStyle!.Family, Is.EqualTo(OdfStyleFamily.Table));
            Assert.That(table.Template!.Name, Is.EqualTo("Banding"));
            Assert.That(table.UseFirstRowStyles, Is.True);
            Assert.That(table.UseBandingRowStyles, Is.True);
            Assert.That(table.IsProtected, Is.True);
            Assert.That(table.ProtectionKey, Is.EqualTo("encoded"));
            Assert.That(table.IsPrinted, Is.False);
            Assert.That(table.Title, Is.EqualTo("Sales"));
            Assert.That(table.Description, Is.EqualTo("Quarterly data"));
            Assert.That(table.PrintRanges, Has.Length.EqualTo(2));
            Assert.That(table.PrintRanges[0].StartColumnIndex, Is.Zero);
            Assert.That(table.PrintRanges[0].EndColumnIndex, Is.EqualTo(1));
            Assert.That(table.PrintRanges[0].EndRowIndex, Is.EqualTo(1));
            Assert.That(table.PrintRanges[0].Table, Is.SameAs(table));
            Assert.That(table.PrintRanges[1].StartTableName, Is.EqualTo("Other"));
            Assert.That(table.PrintRanges[1].Table, Is.SameAs(document.Tables[1]));
            Assert.That(table.PrintTitleRowStart, Is.Zero);
            Assert.That(table.PrintTitleRowCount, Is.EqualTo(2));
            Assert.That(table.PrintTitleColumnCount, Is.EqualTo(2));
            Assert.That(row.Style!.Name, Is.EqualTo("RowStyle"));
            Assert.That(row.DefaultStyle!.Family, Is.EqualTo(OdfStyleFamily.TableRow));
            Assert.That(row.DefaultCellStyle!.Name, Is.EqualTo("RowCell"));
            Assert.That(row.Visibility, Is.EqualTo("collapse"));
            Assert.That(row.IsVisible, Is.False);
            Assert.That(column.Style!.Name, Is.EqualTo("ColumnStyle"));
            Assert.That(column.DefaultCellStyle!.Name, Is.EqualTo("ColumnCell"));
            Assert.That(column.Visibility, Is.EqualTo("filter"));
            Assert.That(row.Cells[0].IsProtected, Is.True);
            Assert.That(row.Cells[0].EffectiveStyle!.Name, Is.EqualTo("RowCell"));
            Assert.That(row.Cells[1].EffectiveStyle!.Name, Is.EqualTo("DirectCell"));
            Assert.That(document.TableTemplates, Has.Length.EqualTo(1));
            Assert.That(document.TableTemplates[0].FirstRowStyleName,
                Is.EqualTo("DirectCell"));
            Assert.That(document.TableTemplates[0].BodyStyleName,
                Is.EqualTo("DefaultCell"));
            Assert.That(document.TableTemplates[0].BackgroundStyleName,
                Is.EqualTo("TableStyle"));
        });
    }

    [Test]
    public void WritesEditedTableViewsAndTemplateRegionsThroughRoundTrip()
    {
        OdfDocument document = ReadSpreadsheet(
            StyleDefinitions(),
            "<table:table table:name='Main' table:style-name='TableStyle' "
            + "table:template-name='Banding' table:protected='true' table:print='false' "
            + "table:print-ranges='.A1:.B2'>"
            + "<table:title>Old title</table:title><table:desc>Old description</table:desc>"
            + "<table:table-column table:style-name='ColumnStyle' "
            + "table:default-cell-style-name='ColumnCell' table:visibility='filter'/>"
            + "<table:table-row table:style-name='RowStyle' "
            + "table:default-cell-style-name='RowCell' table:visibility='collapse'>"
            + "<table:table-cell table:style-name='DirectCell' table:protected='true'/>"
            + "</table:table-row></table:table>");
        OdfTable table = document.Tables[0];
        OdfTableTemplate template = document.TableTemplates[0];
        table.Name = "Renamed";
        table.StyleName = "TableStyleTwo";
        table.UseLastRowStyles = true;
        table.IsProtected = false;
        table.ProtectionKey = "new-key";
        table.ProtectionKeyDigestAlgorithm = "urn:new:digest";
        table.IsPrinted = true;
        table.PrintRanges[0].StartColumnIndex = 1;
        table.PrintRanges[0].StartRowIndex = 1;
        table.PrintRanges[0].EndColumnIndex = 2;
        table.PrintRanges[0].EndRowIndex = 2;
        table.Title = "New title";
        table.Description = "New description";
        table.Rows[0].StyleName = "RowStyleTwo";
        table.Rows[0].DefaultCellStyleName = "RowCellTwo";
        table.Rows[0].IsVisible = true;
        table.Columns[0].StyleName = "ColumnStyleTwo";
        table.Columns[0].Visibility = "collapse";
        table.Rows[0].Cells[0].StyleName = "DirectCellTwo";
        table.Rows[0].Cells[0].IsProtected = false;
        template.FirstRowStyleName = "DirectCellTwo";
        template.EvenRowsStyleName = "RowCellTwo";
        OdfDocument reread = WriteAndRead(document);
        OdfTable result = reread.Tables[0];
        OdfTableTemplate resultTemplate = reread.TableTemplates[0];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Renamed"));
            Assert.That(result.Style!.Name, Is.EqualTo("TableStyleTwo"));
            Assert.That(result.UseLastRowStyles, Is.True);
            Assert.That(result.IsProtected, Is.False);
            Assert.That(result.ProtectionKey, Is.EqualTo("new-key"));
            Assert.That(result.ProtectionKeyDigestAlgorithm,
                Is.EqualTo("urn:new:digest"));
            Assert.That(result.IsPrinted, Is.True);
            Assert.That(result.PrintRangeText, Is.EqualTo(".B2:.C3"));
            Assert.That(result.PrintRanges[0].StartColumnIndex, Is.EqualTo(1));
            Assert.That(result.Title, Is.EqualTo("New title"));
            Assert.That(result.Description, Is.EqualTo("New description"));
            Assert.That(result.Rows[0].Style!.Name, Is.EqualTo("RowStyleTwo"));
            Assert.That(result.Rows[0].DefaultCellStyle!.Name,
                Is.EqualTo("RowCellTwo"));
            Assert.That(result.Rows[0].Visibility, Is.EqualTo("visible"));
            Assert.That(result.Columns[0].Style!.Name,
                Is.EqualTo("ColumnStyleTwo"));
            Assert.That(result.Columns[0].Visibility, Is.EqualTo("collapse"));
            Assert.That(result.Rows[0].Cells[0].Style!.Name,
                Is.EqualTo("DirectCellTwo"));
            Assert.That(result.Rows[0].Cells[0].IsProtected, Is.False);
            Assert.That(resultTemplate.FirstRowStyleName,
                Is.EqualTo("DirectCellTwo"));
            Assert.That(resultTemplate.EvenRowsStyleName,
                Is.EqualTo("RowCellTwo"));
        });
    }

    [Test]
    public void ResolvesCellStylePrecedenceAcrossExplicitRowColumnAndDefaultStyles()
    {
        OdfDocument document = ReadSpreadsheet(
            StyleDefinitions(),
            "<table:table table:name='Styles'>"
            + "<table:table-column table:default-cell-style-name='ColumnCell'/>"
            + "<table:table-column/>"
            + "<table:table-row table:default-cell-style-name='RowCell'>"
            + "<table:table-cell/><table:table-cell table:style-name='DirectCell'/>"
            + "</table:table-row><table:table-row>"
            + "<table:table-cell/><table:table-cell/>"
            + "</table:table-row></table:table>");
        OdfTable table = document.Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(table.Rows[0].Cells[0].EffectiveStyle!.Name,
                Is.EqualTo("RowCell"));
            Assert.That(table.Rows[0].Cells[1].EffectiveStyle!.Name,
                Is.EqualTo("DirectCell"));
            Assert.That(table.Rows[1].Cells[0].EffectiveStyle!.Name,
                Is.EqualTo("ColumnCell"));
            Assert.That(table.Rows[1].Cells[1].EffectiveStyle!.IsDefault,
                Is.True);
            Assert.That(table.Rows[1].Cells[1].EffectiveStyle!.Family,
                Is.EqualTo(OdfStyleFamily.TableCell));
        });
    }

    [Test]
    public void PreservesInvalidRangeAndVisibilityLexicalValuesWhenUnedited()
    {
        OdfDocument document = ReadSpreadsheet(
            StyleDefinitions(),
            "<table:table table:name='Invalid' table:print-ranges='not a range'>"
            + "<table:table-column table:visibility='vendor-hidden'/>"
            + "<table:table-row table:visibility='vendor-hidden'>"
            + "<table:table-cell/></table:table-row></table:table>");
        OdfTable table = document.Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(table.PrintRanges, Is.Empty);
            Assert.That(table.PrintRangeText, Is.EqualTo("not a range"));
            Assert.That(table.Rows[0].Visibility, Is.EqualTo("vendor-hidden"));
            Assert.That(table.Columns[0].Visibility, Is.EqualTo("vendor-hidden"));
        });

        OdfDocument reread = WriteAndRead(document);

        Assert.Multiple(() =>
        {
            Assert.That(reread.Tables[0].PrintRangeText,
                Is.EqualTo("not a range"));
            Assert.That(reread.Tables[0].Rows[0].Visibility,
                Is.EqualTo("vendor-hidden"));
            Assert.That(reread.Tables[0].Columns[0].Visibility,
                Is.EqualTo("vendor-hidden"));
        });
    }

    private static string StyleDefinitions()
    {
        return "<style:default-style style:family='table'/>"
            + "<style:default-style style:family='table-row'/>"
            + "<style:default-style style:family='table-column'/>"
            + "<style:default-style style:family='table-cell'/>"
            + NamedStyle("TableStyle", "table")
            + NamedStyle("TableStyleTwo", "table")
            + NamedStyle("RowStyle", "table-row")
            + NamedStyle("RowStyleTwo", "table-row")
            + NamedStyle("ColumnStyle", "table-column")
            + NamedStyle("ColumnStyleTwo", "table-column")
            + NamedStyle("DefaultCell", "table-cell")
            + NamedStyle("RowCell", "table-cell")
            + NamedStyle("RowCellTwo", "table-cell")
            + NamedStyle("ColumnCell", "table-cell")
            + NamedStyle("DirectCell", "table-cell")
            + NamedStyle("DirectCellTwo", "table-cell")
            + "<table:table-template table:name='Banding' "
            + "table:first-row-start-column='column' "
            + "table:first-row-end-column='column' "
            + "table:last-row-start-column='column' "
            + "table:last-row-end-column='column'>"
            + "<table:first-row table:style-name='DirectCell' "
            + "table:paragraph-style-name='ParagraphStyle'/>"
            + "<table:body table:style-name='DefaultCell'/>"
            + "<table:background table:style-name='TableStyle'/>"
            + "</table:table-template>";
    }

    private static string NamedStyle(string name, string family)
    {
        return "<style:style style:name='" + name
            + "' style:family='" + family + "'/>";
    }

    private static OdfDocument ReadSpreadsheet(
        string styles,
        string tables)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:styles>" + styles + "</office:styles>"
            + "<office:body><office:spreadsheet>" + tables
            + "</office:spreadsheet></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "table-views.fods",
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
            "table-views.fods",
            new OdfLoadOptions());
    }
}
