using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxTableFormatTests
{
    [Test]
    public void Parse_ReadsCommonTableProperties()
    {
        global::docx.DocxTableFormat format = ParseFormat("<w:tblPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblStyle w:val=\"TableGrid\" /><w:tblW w:w=\"5000\" w:type=\"pct\" /><w:jc w:val=\"center\" /><w:tblLayout w:type=\"fixed\" /><w:tblInd w:w=\"240\" w:type=\"dxa\" /><w:tblLook w:val=\"04A0\" w:firstRow=\"1\" w:lastRow=\"0\" w:firstColumn=\"true\" w:lastColumn=\"false\" w:noHBand=\"off\" w:noVBand=\"on\" /><w:tblCellMar><w:top w:w=\"120\" w:type=\"dxa\" /><w:left w:w=\"80\" w:type=\"dxa\" /><w:bottom w:w=\"140\" w:type=\"dxa\" /><w:right w:w=\"100\" w:type=\"dxa\" /></w:tblCellMar></w:tblPr>");

        Assert.That(format.HasAnyFormatting(), Is.True);
        Assert.That(format.StyleId, Is.EqualTo("TableGrid"));
        Assert.That(format.Width, Is.EqualTo("5000"));
        Assert.That(format.WidthType, Is.EqualTo("pct"));
        Assert.That(format.Alignment, Is.EqualTo("center"));
        Assert.That(format.LayoutType, Is.EqualTo("fixed"));
        Assert.That(format.IndentWidth, Is.EqualTo("240"));
        Assert.That(format.IndentType, Is.EqualTo("dxa"));
        Assert.That(format.LookValue, Is.EqualTo("04A0"));
        Assert.That(format.HasFirstRow, Is.True);
        Assert.That(format.FirstRow, Is.True);
        Assert.That(format.HasLastRow, Is.True);
        Assert.That(format.LastRow, Is.False);
        Assert.That(format.HasFirstColumn, Is.True);
        Assert.That(format.FirstColumn, Is.True);
        Assert.That(format.HasLastColumn, Is.True);
        Assert.That(format.LastColumn, Is.False);
        Assert.That(format.HasNoHorizontalBand, Is.True);
        Assert.That(format.NoHorizontalBand, Is.False);
        Assert.That(format.HasNoVerticalBand, Is.True);
        Assert.That(format.NoVerticalBand, Is.True);
        Assert.That(format.CellMarginTopWidth, Is.EqualTo("120"));
        Assert.That(format.CellMarginTopType, Is.EqualTo("dxa"));
        Assert.That(format.CellMarginLeftWidth, Is.EqualTo("80"));
        Assert.That(format.CellMarginLeftType, Is.EqualTo("dxa"));
        Assert.That(format.CellMarginBottomWidth, Is.EqualTo("140"));
        Assert.That(format.CellMarginBottomType, Is.EqualTo("dxa"));
        Assert.That(format.CellMarginRightWidth, Is.EqualTo("100"));
        Assert.That(format.CellMarginRightType, Is.EqualTo("dxa"));
    }

    [Test]
    public void TableParsing_ExposesFormatFromProperties()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:tblStyle w:val=\"LightShading\" /><w:jc w:val=\"right\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"2400\" /><w:gridCol w:w=\"1200\" /></w:tblGrid><w:tr><w:tc><w:p /></w:tc><w:tc><w:p /></w:tc></w:tr><w:tr><w:tc><w:p /></w:tc></w:tr></w:tbl>");

        Assert.That(table.HasProperties(), Is.True);
        Assert.That(table.HasFormat(), Is.True);
        Assert.That(table.Format.StyleId, Is.EqualTo("LightShading"));
        Assert.That(table.Format.Alignment, Is.EqualTo("right"));
        Assert.That(table.ChildCount(), Is.EqualTo(3));
        Assert.That(table.HasGrid(), Is.True);
        Assert.That(table.Grid.ColumnCount(), Is.EqualTo(2));
        Assert.That(table.Grid.GetColumnWidth(0), Is.EqualTo("2400"));
        Assert.That(table.Grid.GetColumnWidth(1), Is.EqualTo("1200"));
        Assert.That(table.RowCount(), Is.EqualTo(2));
        Assert.That(table.GetRow(0).CellElementCount(), Is.EqualTo(2));
        Assert.That(table.GetRow(0).CellCount(), Is.EqualTo(2));
        Assert.That(table.GetRow(0).GetCell(0).ParagraphCount(), Is.EqualTo(1));
        Assert.That(table.GetRow(1).CellElementCount(), Is.EqualTo(1));
        Assert.That(table.CountChildElements("tblGrid", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
        Assert.That(table.CountChildElements("tr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(2));
    }

    [Test]
    public void TableParsing_PreservesGridAndRowChildren()
    {
        global::docx.DocxTableGrid grid = ParseGrid("<w:tblGrid xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:gridCol w:w=\"1000\" /><w:custom marker=\"grid\" /></w:tblGrid>");
        global::docx.DocxTableRow row = ParseRow("<w:tr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:trPr /><w:tc><w:p /></w:tc><w:custom marker=\"row\" /></w:tr>");

        Assert.That(grid.HasXml(), Is.True);
        Assert.That(grid.ChildCount(), Is.EqualTo(2));
        Assert.That(grid.ColumnCount(), Is.EqualTo(1));
        Assert.That(grid.GetChildXml(1), Does.Contain("marker=\"grid\""));
        Assert.That(row.HasXml(), Is.True);
        Assert.That(row.HasProperties(), Is.True);
        Assert.That(row.ChildCount(), Is.EqualTo(2));
        Assert.That(row.CellElementCount(), Is.EqualTo(1));
        Assert.That(row.GetChildXml(1), Does.Contain("marker=\"row\""));
    }

    [Test]
    public void RowParsing_ReadsRowProperties()
    {
        global::docx.DocxTableRow row = ParseRow("<w:tr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:trPr><w:trHeight w:val=\"480\" w:hRule=\"exact\" /><w:cantSplit /><w:tblHeader w:val=\"0\" /></w:trPr><w:tc><w:p /></w:tc></w:tr>");

        Assert.That(row.HasProperties(), Is.True);
        Assert.That(row.HasAnyFormatting(), Is.True);
        Assert.That(row.Height, Is.EqualTo("480"));
        Assert.That(row.HeightRule, Is.EqualTo("exact"));
        Assert.That(row.HasCantSplit, Is.True);
        Assert.That(row.CantSplit, Is.True);
        Assert.That(row.HasTableHeader, Is.True);
        Assert.That(row.TableHeader, Is.False);
        Assert.That(row.CellElementCount(), Is.EqualTo(1));
    }

    [Test]
    public void CellParsing_ReadsCellPropertiesAndParagraphs()
    {
        global::docx.DocxTableCell cell = ParseCell("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tcPr><w:tcW w:w=\"2400\" w:type=\"dxa\" /><w:gridSpan w:val=\"2\" /><w:vAlign w:val=\"center\" /></w:tcPr><w:p><w:r><w:t>One</w:t></w:r></w:p><w:p><w:r><w:t>Two</w:t></w:r></w:p><w:custom marker=\"cell\" /></w:tc>");

        Assert.That(cell.HasProperties(), Is.True);
        Assert.That(cell.HasAnyFormatting(), Is.True);
        Assert.That(cell.Width, Is.EqualTo("2400"));
        Assert.That(cell.WidthType, Is.EqualTo("dxa"));
        Assert.That(cell.GridSpan, Is.EqualTo("2"));
        Assert.That(cell.VerticalAlignment, Is.EqualTo("center"));
        Assert.That(cell.ChildCount(), Is.EqualTo(3));
        Assert.That(cell.ParagraphCount(), Is.EqualTo(2));
        Assert.That(cell.GetParagraph(0).RunCount(), Is.EqualTo(1));
        Assert.That(cell.GetChildXml(2), Does.Contain("marker=\"cell\""));
    }

    [Test]
    public void CellMergeParsing_DetectsHorizontalAndVerticalMerges()
    {
        global::docx.DocxTableCell gridSpanCell = ParseCell("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tcPr><w:gridSpan w:val=\"3\" /><w:vMerge w:val=\"restart\" /></w:tcPr><w:p /></w:tc>");
        global::docx.DocxTableCell continuationCell = ParseCell("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tcPr><w:hMerge /><w:vMerge /></w:tcPr><w:p /></w:tc>");
        global::docx.DocxTableCell horizontalStartCell = ParseCell("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tcPr><w:hMerge w:val=\"restart\" /></w:tcPr><w:p /></w:tc>");
        global::docx.DocxTableCell plainCell = ParseCell("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p /></w:tc>");

        Assert.That(gridSpanCell.GridSpan, Is.EqualTo("3"));
        Assert.That(gridSpanCell.GetGridSpanCount(), Is.EqualTo(3));
        Assert.That(gridSpanCell.VerticalMerge, Is.EqualTo("restart"));
        Assert.That(gridSpanCell.HasHorizontalMerge(), Is.True);
        Assert.That(gridSpanCell.IsHorizontalMergeStart(), Is.True);
        Assert.That(gridSpanCell.IsHorizontalMergeContinuation(), Is.False);
        Assert.That(gridSpanCell.HasVerticalMerge(), Is.True);
        Assert.That(gridSpanCell.IsVerticalMergeStart(), Is.True);
        Assert.That(gridSpanCell.IsVerticalMergeContinuation(), Is.False);
        Assert.That(continuationCell.HorizontalMerge, Is.EqualTo("continue"));
        Assert.That(continuationCell.VerticalMerge, Is.EqualTo("continue"));
        Assert.That(continuationCell.HasHorizontalMerge(), Is.True);
        Assert.That(continuationCell.IsHorizontalMergeContinuation(), Is.True);
        Assert.That(continuationCell.HasVerticalMerge(), Is.True);
        Assert.That(continuationCell.IsVerticalMergeContinuation(), Is.True);
        Assert.That(horizontalStartCell.HorizontalMerge, Is.EqualTo("restart"));
        Assert.That(horizontalStartCell.IsHorizontalMergeStart(), Is.True);
        Assert.That(plainCell.HasHorizontalMerge(), Is.False);
        Assert.That(plainCell.HasVerticalMerge(), Is.False);
    }

    [Test]
    public void NestedTableParsing_ReadsTablesInsideCells()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc><w:p><w:r><w:t>Before</w:t></w:r></w:p><w:tbl><w:tblGrid><w:gridCol w:w=\"1200\" /></w:tblGrid><w:tr><w:tc><w:p><w:r><w:t>Nested</w:t></w:r></w:p></w:tc></w:tr></w:tbl><w:p><w:r><w:t>After</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");
        global::docx.DocxTableCell outerCell = table.GetRow(0).GetCell(0);
        global::docx.DocxTable nested = outerCell.GetTable(0);

        Assert.That(table.RowCount(), Is.EqualTo(1));
        Assert.That(outerCell.ChildCount(), Is.EqualTo(3));
        Assert.That(outerCell.ParagraphCount(), Is.EqualTo(2));
        Assert.That(outerCell.TableCount(), Is.EqualTo(1));
        Assert.That(nested.HasGrid(), Is.True);
        Assert.That(nested.Grid.GetColumnWidth(0), Is.EqualTo("1200"));
        Assert.That(nested.RowCount(), Is.EqualTo(1));
        Assert.That(nested.GetRow(0).CellCount(), Is.EqualTo(1));
        Assert.That(nested.GetRow(0).GetCell(0).ParagraphCount(), Is.EqualTo(1));
    }

    [Test]
    public void Parse_HandlesEmptyAndMissingTableProperties()
    {
        global::docx.DocxTable noProperties = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr /></w:tbl>");
        global::docx.DocxTableFormat emptyProperties = global::docx.DocxTableFormat.ParseXml("<w:tblPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />");
        global::docx.DocxTableFormat missingProperties = global::docx.DocxTableFormat.ParseXml("<root />");

        Assert.That(noProperties.HasProperties(), Is.False);
        Assert.That(noProperties.HasFormat(), Is.False);
        Assert.That(noProperties.ChildCount(), Is.EqualTo(1));
        Assert.That(noProperties.HasGrid(), Is.False);
        Assert.That(noProperties.RowCount(), Is.EqualTo(1));
        Assert.That(noProperties.GetRow(0).CellCount(), Is.EqualTo(0));
        Assert.That(emptyProperties.HasAnyFormatting(), Is.False);
        Assert.That(missingProperties.HasAnyFormatting(), Is.False);
    }

    [Test]
    public void SetPropertiesXml_UpdatesParsedTableFormat()
    {
        global::docx.DocxTable table = new global::docx.DocxTable("");

        table.SetPropertiesXml("<w:tblPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblW w:w=\"7200\" w:type=\"dxa\" /><w:tblLook w:firstRow=\"1\" /></w:tblPr>");

        Assert.That(table.HasFormat(), Is.True);
        Assert.That(table.Format.Width, Is.EqualTo("7200"));
        Assert.That(table.Format.WidthType, Is.EqualTo("dxa"));
        Assert.That(table.Format.HasFirstRow, Is.True);
        Assert.That(table.Format.FirstRow, Is.True);
    }

    [Test]
    public void WriteXml_WritesCommonTableProperties()
    {
        global::docx.DocxTableFormat format = new global::docx.DocxTableFormat();

        format.SetStyleId("TableGrid");
        format.SetWidth("5000", "pct");
        format.SetAlignment("center");
        format.SetLayoutType("fixed");
        format.SetIndent("240", "dxa");
        format.SetLookValue("04A0");
        format.SetFirstRow(true);
        format.SetLastRow(false);
        format.SetFirstColumn(true);
        format.SetLastColumn(false);
        format.SetNoHorizontalBand(false);
        format.SetNoVerticalBand(true);
        format.SetCellMargins("120", "dxa", "80", "dxa", "140", "dxa", "100", "dxa");

        global::docx.DocxTableFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.StyleId, Is.EqualTo("TableGrid"));
        Assert.That(parsed.Width, Is.EqualTo("5000"));
        Assert.That(parsed.WidthType, Is.EqualTo("pct"));
        Assert.That(parsed.Alignment, Is.EqualTo("center"));
        Assert.That(parsed.LayoutType, Is.EqualTo("fixed"));
        Assert.That(parsed.IndentWidth, Is.EqualTo("240"));
        Assert.That(parsed.IndentType, Is.EqualTo("dxa"));
        Assert.That(parsed.LookValue, Is.EqualTo("04A0"));
        Assert.That(parsed.FirstRow, Is.True);
        Assert.That(parsed.LastRow, Is.False);
        Assert.That(parsed.FirstColumn, Is.True);
        Assert.That(parsed.LastColumn, Is.False);
        Assert.That(parsed.NoHorizontalBand, Is.False);
        Assert.That(parsed.NoVerticalBand, Is.True);
        Assert.That(parsed.CellMarginTopWidth, Is.EqualTo("120"));
        Assert.That(parsed.CellMarginLeftWidth, Is.EqualTo("80"));
        Assert.That(parsed.CellMarginBottomWidth, Is.EqualTo("140"));
        Assert.That(parsed.CellMarginRightWidth, Is.EqualTo("100"));
    }

    [Test]
    public void TableWriting_WritesStructuredFormatBeforeChildren()
    {
        global::docx.DocxTable table = new global::docx.DocxTable("");

        table.Format.SetStyleId("LightShading");
        table.Format.SetAlignment("right");
        table.AddChild(new global::docx.DocxXmlBuffer("<w:tblGrid xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:gridCol w:w=\"2400\" /></w:tblGrid>"));
        table.AddChild(new global::docx.DocxXmlBuffer("<w:tr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tc><w:p /></w:tc></w:tr>"));

        string xml = table.WriteXml();
        global::docx.DocxTable parsed = ParseTable(xml);

        Assert.That(xml.IndexOf("<w:tblPr", StringComparison.Ordinal), Is.LessThan(xml.IndexOf("<w:tblGrid", StringComparison.Ordinal)));
        Assert.That(parsed.HasFormat(), Is.True);
        Assert.That(parsed.Format.StyleId, Is.EqualTo("LightShading"));
        Assert.That(parsed.Format.Alignment, Is.EqualTo("right"));
        Assert.That(parsed.ChildCount(), Is.EqualTo(2));
    }

    [Test]
    public void TableWriting_WritesTypedGridRowsAndUnknownChildren()
    {
        global::docx.DocxTable table = new global::docx.DocxTable("");
        global::docx.DocxTableGrid grid = global::docx.DocxTableGrid.Create();
        global::docx.DocxTableRow row = global::docx.DocxTableRow.Create();

        grid.AddColumnWidth("1200");
        grid.AddColumnWidth("2400");
        row.AddChild(new global::docx.DocxXmlBuffer("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p /></w:tc>"));
        row.AddChild(new global::docx.DocxXmlBuffer("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p /></w:tc>"));
        table.SetGrid(grid);
        table.AddChild(new global::docx.DocxXmlBuffer("<w:bookmarkStart xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:id=\"1\" w:name=\"TableMark\" />"));
        table.AddRow(row);

        global::docx.DocxTable parsed = ParseTable(table.WriteXml());

        Assert.That(parsed.HasGrid(), Is.True);
        Assert.That(parsed.Grid.ColumnCount(), Is.EqualTo(2));
        Assert.That(parsed.Grid.GetColumnWidth(0), Is.EqualTo("1200"));
        Assert.That(parsed.RowCount(), Is.EqualTo(1));
        Assert.That(parsed.GetRow(0).CellElementCount(), Is.EqualTo(2));
        Assert.That(parsed.GetRow(0).CellCount(), Is.EqualTo(2));
        Assert.That(parsed.CountChildElements("bookmarkStart", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.EqualTo(1));
    }

    [Test]
    public void GridWidthHandling_ReplacesColumnWidthsAndCalculatesTotal()
    {
        global::docx.DocxTableGrid grid = ParseGrid("<w:tblGrid xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:gridCol w:w=\"1000\" /><w:gridCol w:w=\"2000\" /><w:custom marker=\"grid\" /></w:tblGrid>");

        grid.SetColumnWidth(0, "1500");
        grid.SetColumnWidth(1, "2500");

        global::docx.DocxTableGrid parsed = ParseGrid(grid.WriteXml());

        Assert.That(grid.GetTotalWidth(), Is.EqualTo("4000"));
        Assert.That(parsed.ColumnCount(), Is.EqualTo(2));
        Assert.That(parsed.GetColumnWidth(0), Is.EqualTo("1500"));
        Assert.That(parsed.GetColumnWidth(1), Is.EqualTo("2500"));
        Assert.That(parsed.ChildCount(), Is.EqualTo(3));
        Assert.That(parsed.GetChildXml(2), Does.Contain("marker=\"grid\""));

        grid.SetColumnWidth(1, "auto");

        Assert.That(grid.GetTotalWidth(), Is.EqualTo(""));

        grid.SetColumnWidths(new List<string> { long.MaxValue.ToString(), "1" });

        Assert.That(grid.GetTotalWidth(), Is.EqualTo(""));

        grid.SetColumnWidths(new List<string> { long.MinValue.ToString(), "-1" });

        Assert.That(grid.GetTotalWidth(), Is.EqualTo(""));
    }

    [Test]
    public void TableWidthHandling_DerivesTableWidthAndAppliesGridWidthsToCells()
    {
        global::docx.DocxTable table = new global::docx.DocxTable("");
        global::docx.DocxTableRow firstRow = global::docx.DocxTableRow.Create();
        global::docx.DocxTableRow secondRow = global::docx.DocxTableRow.Create();

        firstRow.AddCell(global::docx.DocxTableCell.Create());
        firstRow.AddCell(global::docx.DocxTableCell.Create());
        secondRow.AddCell(global::docx.DocxTableCell.Create());
        secondRow.AddCell(global::docx.DocxTableCell.Create());
        table.AddRow(firstRow);
        table.AddRow(secondRow);
        table.SetGridColumnWidths(new List<string> { "1200", "2400" });
        table.SetWidthFromGrid("dxa");
        table.ApplyGridColumnWidthsToCells("dxa");

        global::docx.DocxTable parsed = ParseTable(table.WriteXml());

        Assert.That(parsed.Format.Width, Is.EqualTo("3600"));
        Assert.That(parsed.Format.WidthType, Is.EqualTo("dxa"));
        Assert.That(parsed.Grid.ColumnCount(), Is.EqualTo(2));
        Assert.That(parsed.Grid.GetColumnWidth(0), Is.EqualTo("1200"));
        Assert.That(parsed.Grid.GetColumnWidth(1), Is.EqualTo("2400"));
        Assert.That(parsed.GetRow(0).GetCell(0).Width, Is.EqualTo("1200"));
        Assert.That(parsed.GetRow(0).GetCell(0).WidthType, Is.EqualTo("dxa"));
        Assert.That(parsed.GetRow(0).GetCell(1).Width, Is.EqualTo("2400"));
        Assert.That(parsed.GetRow(1).GetCell(0).Width, Is.EqualTo("1200"));
        Assert.That(parsed.GetRow(1).GetCell(1).Width, Is.EqualTo("2400"));
    }

    [Test]
    public void TableInsertionHelpers_CreateAndInsertRowsCellsAndNestedTables()
    {
        global::docx.DocxTable table = global::docx.DocxTable.Create(1, 2);
        global::docx.DocxTableRow insertedRow = table.InsertRowWithCells(0, 3);
        global::docx.DocxTableRow appendedRow = table.AddRowWithCells(1);
        global::docx.DocxTableRow standaloneRow = global::docx.DocxTableRow.Create();
        global::docx.DocxTableCell appendedCell = standaloneRow.AddNewCell();
        global::docx.DocxTableCell insertedCell = standaloneRow.InsertNewCell(0);
        global::docx.DocxTableCell hostCell = global::docx.DocxTableCell.Create();

        insertedRow.GetCell(0).SetWidth("1000", "dxa");
        appendedRow.GetCell(0).SetWidth("3000", "dxa");
        insertedCell.SetWidth("4000", "dxa");
        appendedCell.SetWidth("5000", "dxa");
        hostCell.AddNewTable(1, 1);
        hostCell.InsertNewTable(0, 2, 1);

        global::docx.DocxTable parsedTable = ParseTable(table.WriteXml());
        global::docx.DocxTableRow parsedRow = ParseRow(standaloneRow.WriteXml());
        global::docx.DocxTableCell parsedHostCell = ParseCell(hostCell.WriteXml());

        Assert.That(parsedTable.RowCount(), Is.EqualTo(3));
        Assert.That(parsedTable.GetRow(0).CellCount(), Is.EqualTo(3));
        Assert.That(parsedTable.GetRow(0).GetCell(0).Width, Is.EqualTo("1000"));
        Assert.That(parsedTable.GetRow(1).CellCount(), Is.EqualTo(2));
        Assert.That(parsedTable.GetRow(2).CellCount(), Is.EqualTo(1));
        Assert.That(parsedTable.GetRow(2).GetCell(0).Width, Is.EqualTo("3000"));
        Assert.That(parsedRow.CellCount(), Is.EqualTo(2));
        Assert.That(parsedRow.GetCell(0).Width, Is.EqualTo("4000"));
        Assert.That(parsedRow.GetCell(1).Width, Is.EqualTo("5000"));
        Assert.That(parsedHostCell.TableCount(), Is.EqualTo(2));
        Assert.That(parsedHostCell.GetTable(0).RowCount(), Is.EqualTo(2));
        Assert.That(parsedHostCell.GetTable(1).RowCount(), Is.EqualTo(1));
    }

    [Test]
    public void TableTextExtraction_UsesConfiguredSeparatorsAndNestedTables()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc><w:p><w:r><w:t>A</w:t></w:r></w:p><w:p><w:r><w:t>B</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>C</w:t></w:r></w:p></w:tc></w:tr><w:tr><w:tc><w:tbl><w:tr><w:tc><w:p><w:r><w:t>N</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:tc><w:tc><w:p><w:r><w:t>D</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";
        options.TableCellSeparator = ",";
        options.TableRowSeparator = "/";

        Assert.That(table.GetTextWithOptions(options), Is.EqualTo("A|B,C/N,D"));
        Assert.That(table.GetRow(0).GetCell(0).GetTextWithOptions(options), Is.EqualTo("A|B"));
    }

    [Test]
    public void CellTextExtraction_UsesParsedChildOrderForParagraphsAndNestedTables()
    {
        global::docx.DocxTableCell cell = ParseCell("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p><w:r><w:t>Before</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>Nested</w:t></w:r></w:p></w:tc></w:tr></w:tbl><w:p><w:r><w:t>After</w:t></w:r></w:p></w:tc>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";

        Assert.That(cell.ChildCount(), Is.EqualTo(3));
        Assert.That(cell.ParagraphCount(), Is.EqualTo(2));
        Assert.That(cell.TableCount(), Is.EqualTo(1));
        Assert.That(cell.GetTextWithOptions(options), Is.EqualTo("Before|Nested|After"));
    }

    [Test]
    public void TableTextMatrix_UsesNestedTableTextWhenExpandingSpans()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc><w:tcPr><w:gridSpan w:val=\"2\" /></w:tcPr><w:p><w:r><w:t>Outer</w:t></w:r></w:p><w:tbl><w:tr><w:tc><w:p><w:r><w:t>Nested</w:t></w:r></w:p></w:tc></w:tr></w:tbl></w:tc><w:tc><w:p><w:r><w:t>Right</w:t></w:r></w:p></w:tc></w:tr><w:tr><w:tc><w:p><w:r><w:t>Bottom</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>Middle</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>End</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();

        options.ParagraphSeparator = "|";

        List<List<string>> matrix = table.ToTextMatrixWithOptions(options);

        Assert.That(matrix.Count, Is.EqualTo(2));
        Assert.That(matrix[0][0], Is.EqualTo("Outer|Nested"));
        Assert.That(matrix[0][1], Is.EqualTo("Outer|Nested"));
        Assert.That(matrix[0][2], Is.EqualTo("Right"));
        Assert.That(matrix[1][0], Is.EqualTo("Bottom"));
        Assert.That(matrix[1][1], Is.EqualTo("Middle"));
        Assert.That(matrix[1][2], Is.EqualTo("End"));
    }

    [Test]
    public void TableCreation_RoundTripsGridRowsCellsAndText()
    {
        global::docx.DocxTable table = global::docx.DocxTable.Create(2, 2);

        table.SetGridColumnWidths(new List<string> { "1200", "1800" });
        table.SetWidthFromGrid("dxa");
        table.GetRow(0).GetCell(0).AddParagraph(CreateParagraph("A"));
        table.GetRow(0).GetCell(1).AddParagraph(CreateParagraph("B"));
        table.GetRow(1).GetCell(0).AddParagraph(CreateParagraph("C"));
        table.GetRow(1).GetCell(1).AddParagraph(CreateParagraph("D"));

        global::docx.DocxTable parsed = ParseTable(table.WriteXml());

        Assert.That(parsed.Format.Width, Is.EqualTo("3000"));
        Assert.That(parsed.Grid.ColumnCount(), Is.EqualTo(2));
        Assert.That(parsed.RowCount(), Is.EqualTo(2));
        Assert.That(parsed.GetRow(0).CellCount(), Is.EqualTo(2));
        Assert.That(parsed.GetRow(1).CellCount(), Is.EqualTo(2));
        Assert.That(parsed.ToTextMatrix()[0][0], Is.EqualTo("A"));
        Assert.That(parsed.ToTextMatrix()[0][1], Is.EqualTo("B"));
        Assert.That(parsed.ToTextMatrix()[1][0], Is.EqualTo("C"));
        Assert.That(parsed.ToTextMatrix()[1][1], Is.EqualTo("D"));
    }

    [Test]
    public void TableMatrixProjection_ExpandsHorizontalSpansAndVerticalContinuations()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tr><w:tc><w:tcPr><w:gridSpan w:val=\"2\" /></w:tcPr><w:p><w:r><w:t>Header</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>R1C3</w:t></w:r></w:p></w:tc></w:tr><w:tr><w:tc><w:tcPr><w:vMerge w:val=\"restart\" /></w:tcPr><w:p><w:r><w:t>Top</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>B</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>C</w:t></w:r></w:p></w:tc></w:tr><w:tr><w:tc><w:tcPr><w:vMerge /></w:tcPr><w:p /></w:tc><w:tc><w:p><w:r><w:t>D</w:t></w:r></w:p></w:tc><w:tc><w:p><w:r><w:t>E</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");

        List<List<string>> matrix = table.ToTextMatrix();

        Assert.That(matrix.Count, Is.EqualTo(3));
        Assert.That(matrix[0].Count, Is.EqualTo(3));
        Assert.That(matrix[0][0], Is.EqualTo("Header"));
        Assert.That(matrix[0][1], Is.EqualTo("Header"));
        Assert.That(matrix[0][2], Is.EqualTo("R1C3"));
        Assert.That(matrix[1][0], Is.EqualTo("Top"));
        Assert.That(matrix[1][1], Is.EqualTo("B"));
        Assert.That(matrix[1][2], Is.EqualTo("C"));
        Assert.That(matrix[2][0], Is.EqualTo("Top"));
        Assert.That(matrix[2][1], Is.EqualTo("D"));
        Assert.That(matrix[2][2], Is.EqualTo("E"));
    }

    [Test]
    public void RowWriting_WritesStructuredRowProperties()
    {
        global::docx.DocxTableRow row = global::docx.DocxTableRow.Create();

        row.SetHeight("360", "atLeast");
        row.SetCantSplit(false);
        row.SetTableHeader(true);
        row.AddChild(new global::docx.DocxXmlBuffer("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:p /></w:tc>"));

        global::docx.DocxTableRow parsed = ParseRow(row.WriteXml());

        Assert.That(parsed.Height, Is.EqualTo("360"));
        Assert.That(parsed.HeightRule, Is.EqualTo("atLeast"));
        Assert.That(parsed.HasCantSplit, Is.True);
        Assert.That(parsed.CantSplit, Is.False);
        Assert.That(parsed.HasTableHeader, Is.True);
        Assert.That(parsed.TableHeader, Is.True);
        Assert.That(parsed.CellElementCount(), Is.EqualTo(1));
    }

    [Test]
    public void CellWriting_WritesStructuredPropertiesAndParagraphs()
    {
        global::docx.DocxTableCell cell = global::docx.DocxTableCell.Create();
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        paragraph.AddChild(new global::docx.DocxXmlBuffer("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Text</w:t></w:r>"));
        cell.SetWidth("3000", "dxa");
        cell.SetGridSpan("2");
        cell.SetVerticalAlignment("bottom");
        cell.AddParagraph(paragraph);

        global::docx.DocxTableCell parsed = ParseCell(cell.WriteXml());

        Assert.That(parsed.Width, Is.EqualTo("3000"));
        Assert.That(parsed.WidthType, Is.EqualTo("dxa"));
        Assert.That(parsed.GridSpan, Is.EqualTo("2"));
        Assert.That(parsed.VerticalAlignment, Is.EqualTo("bottom"));
        Assert.That(parsed.ParagraphCount(), Is.EqualTo(1));
        Assert.That(parsed.GetParagraph(0).RunCount(), Is.EqualTo(1));
    }

    [Test]
    public void CellMergeWriting_WritesHorizontalAndVerticalMergeProperties()
    {
        global::docx.DocxTableCell cell = global::docx.DocxTableCell.Create();

        cell.SetGridSpan("2");
        cell.SetHorizontalMerge("restart");
        cell.SetVerticalMerge("continue");

        global::docx.DocxTableCell parsed = ParseCell(cell.WriteXml());

        Assert.That(parsed.GridSpan, Is.EqualTo("2"));
        Assert.That(parsed.HorizontalMerge, Is.EqualTo("restart"));
        Assert.That(parsed.VerticalMerge, Is.EqualTo("continue"));
        Assert.That(parsed.HasHorizontalMerge(), Is.True);
        Assert.That(parsed.IsHorizontalMergeStart(), Is.True);
        Assert.That(parsed.HasVerticalMerge(), Is.True);
        Assert.That(parsed.IsVerticalMergeContinuation(), Is.True);

        parsed.ClearHorizontalMerge();
        parsed.ClearVerticalMerge();

        global::docx.DocxTableCell cleared = ParseCell(parsed.WriteXml());

        Assert.That(cleared.HorizontalMerge, Is.EqualTo(""));
        Assert.That(cleared.VerticalMerge, Is.EqualTo(""));
        Assert.That(cleared.HasVerticalMerge(), Is.False);
    }

    [Test]
    public void NestedTableWriting_WritesTypedTablesInsideCells()
    {
        global::docx.DocxTable table = new global::docx.DocxTable("");
        global::docx.DocxTableRow row = global::docx.DocxTableRow.Create();
        global::docx.DocxTableCell cell = global::docx.DocxTableCell.Create();
        global::docx.DocxParagraph before = new global::docx.DocxParagraph("");
        global::docx.DocxTable nested = new global::docx.DocxTable("");
        global::docx.DocxTableGrid nestedGrid = global::docx.DocxTableGrid.Create();
        global::docx.DocxTableRow nestedRow = global::docx.DocxTableRow.Create();
        global::docx.DocxTableCell nestedCell = global::docx.DocxTableCell.Create();
        global::docx.DocxParagraph nestedParagraph = new global::docx.DocxParagraph("");

        before.AddChild(new global::docx.DocxXmlBuffer("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Before</w:t></w:r>"));
        nestedParagraph.AddChild(new global::docx.DocxXmlBuffer("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>Nested</w:t></w:r>"));
        nestedGrid.AddColumnWidth("1800");
        nested.SetGrid(nestedGrid);
        nestedCell.AddParagraph(nestedParagraph);
        nestedRow.AddCell(nestedCell);
        nested.AddRow(nestedRow);
        cell.AddParagraph(before);
        cell.AddTable(nested);
        row.AddCell(cell);
        table.AddRow(row);

        global::docx.DocxTable parsed = ParseTable(table.WriteXml());
        global::docx.DocxTableCell parsedCell = parsed.GetRow(0).GetCell(0);
        global::docx.DocxTable parsedNested = parsedCell.GetTable(0);

        Assert.That(parsed.RowCount(), Is.EqualTo(1));
        Assert.That(parsedCell.ParagraphCount(), Is.EqualTo(1));
        Assert.That(parsedCell.TableCount(), Is.EqualTo(1));
        Assert.That(parsedNested.HasGrid(), Is.True);
        Assert.That(parsedNested.Grid.GetColumnWidth(0), Is.EqualTo("1800"));
        Assert.That(parsedNested.RowCount(), Is.EqualTo(1));
        Assert.That(parsedNested.GetRow(0).GetCell(0).ParagraphCount(), Is.EqualTo(1));
    }

    [Test]
    public void CellWriting_ReplacesRawPropertiesWhenMutated()
    {
        global::docx.DocxTableCell cell = ParseCell("<w:tc xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tcPr><w:tcW w:w=\"1000\" w:type=\"dxa\" /></w:tcPr><w:p /></w:tc>");

        cell.SetWidth("2000", "pct");
        cell.SetVerticalAlignment("center");

        global::docx.DocxTableCell parsed = ParseCell(cell.WriteXml());

        Assert.That(parsed.Width, Is.EqualTo("2000"));
        Assert.That(parsed.WidthType, Is.EqualTo("pct"));
        Assert.That(parsed.VerticalAlignment, Is.EqualTo("center"));
        Assert.That(parsed.ParagraphCount(), Is.EqualTo(1));
    }

    [Test]
    public void RowWriting_ReplacesRawPropertiesWhenMutated()
    {
        global::docx.DocxTableRow row = ParseRow("<w:tr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:trPr><w:trHeight w:val=\"240\" /></w:trPr><w:tc><w:p /></w:tc></w:tr>");

        row.SetHeight("720", "exact");

        global::docx.DocxTableRow parsed = ParseRow(row.WriteXml());

        Assert.That(parsed.Height, Is.EqualTo("720"));
        Assert.That(parsed.HeightRule, Is.EqualTo("exact"));
        Assert.That(parsed.CellElementCount(), Is.EqualTo(1));
    }

    [Test]
    public void TableWriting_PreservesParsedGridRowsWhenFormatIsMutated()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:jc w:val=\"center\" /></w:tblPr><w:tblGrid><w:gridCol w:w=\"1000\" /></w:tblGrid><w:tr><w:tc><w:p /></w:tc></w:tr></w:tbl>");
        global::docx.DocxTableFormat format = new global::docx.DocxTableFormat();

        format.SetStyleId("Grid");
        table.SetFormat(format);

        global::docx.DocxTable parsed = ParseTable(table.WriteXml());

        Assert.That(parsed.Format.StyleId, Is.EqualTo("Grid"));
        Assert.That(parsed.HasGrid(), Is.True);
        Assert.That(parsed.Grid.GetColumnWidth(0), Is.EqualTo("1000"));
        Assert.That(parsed.RowCount(), Is.EqualTo(1));
        Assert.That(parsed.GetRow(0).CellElementCount(), Is.EqualTo(1));
    }

    [Test]
    public void SetFormat_ReplacesRawPropertiesForWriting()
    {
        global::docx.DocxTable table = ParseTable("<w:tbl xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblPr><w:jc w:val=\"center\" /></w:tblPr><w:tr /></w:tbl>");
        global::docx.DocxTableFormat format = new global::docx.DocxTableFormat();

        format.SetStyleId("BodyTable");
        table.SetFormat(format);

        string xml = table.WriteXml();
        global::docx.DocxTable parsed = ParseTable(xml);

        Assert.That(xml, Does.Contain("<w:tblStyle"));
        Assert.That(xml, Does.Not.Contain("<w:jc"));
        Assert.That(parsed.Format.StyleId, Is.EqualTo("BodyTable"));
        Assert.That(parsed.Format.Alignment, Is.EqualTo(""));
        Assert.That(parsed.ChildCount(), Is.EqualTo(1));
    }

    [Test]
    public void ClearLookFormatting_RemovesLookFlagsFromWrittenProperties()
    {
        global::docx.DocxTableFormat format = new global::docx.DocxTableFormat();

        format.SetFirstRow(true);
        format.ClearFirstRow();
        format.SetLastRow(true);
        format.ClearLastRow();
        format.SetFirstColumn(true);
        format.ClearFirstColumn();
        format.SetLastColumn(true);
        format.ClearLastColumn();
        format.SetNoHorizontalBand(true);
        format.ClearNoHorizontalBand();
        format.SetNoVerticalBand(true);
        format.ClearNoVerticalBand();

        global::docx.DocxTableFormat parsed = ParseFormat(format.WriteXml());

        Assert.That(parsed.HasAnyFormatting(), Is.False);
    }

    [Test]
    public void PublicTableFormatPath_RoundTripsSpacingCaptionAndDescription()
    {
        global::docx.DocxTable table = ParseTable(
            "<w:tbl xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\">"
            + "<w:tblPr><w:tblStyle w:val=\"TableGrid\"/><w:tblW w:w=\"5000\" w:type=\"pct\"/><w:jc w:val=\"center\"/>"
            + "<w:tblCellSpacing w:w=\"40\" w:type=\"dxa\"/><w:tblLayout w:type=\"fixed\"/><w:tblCaption w:val=\"Sales totals\"/><w:tblDescription w:val=\"Quarterly sales by region\"/></w:tblPr>"
            + "<w:tr><w:tc><w:p/></w:tc></w:tr></w:tbl>");
        global::docx.IDocxTableModel model = table;
        global::docx.DocxTableFormat format = model.GetFormat();

        Assert.That(format.CellSpacingWidth, Is.EqualTo("40"));
        Assert.That(format.CellSpacingType, Is.EqualTo("dxa"));
        Assert.That(format.Caption, Is.EqualTo("Sales totals"));
        Assert.That(format.Description, Is.EqualTo("Quarterly sales by region"));

        format.CellSpacingWidth = "60";
        format.CellSpacingType = "dxa";
        format.Caption = "Updated caption";
        format.Description = "Updated accessible description";
        model.SetFormat(format);

        global::docx.DocxTableFormat parsed = ParseTable(table.WriteXml()).GetFormat();
        Assert.That(model.HasFormat(), Is.True);
        Assert.That(parsed.StyleId, Is.EqualTo("TableGrid"));
        Assert.That(parsed.Width, Is.EqualTo("5000"));
        Assert.That(parsed.Alignment, Is.EqualTo("center"));
        Assert.That(parsed.LayoutType, Is.EqualTo("fixed"));
        Assert.That(parsed.CellSpacingWidth, Is.EqualTo("60"));
        Assert.That(parsed.Caption, Is.EqualTo("Updated caption"));
        Assert.That(parsed.Description, Is.EqualTo("Updated accessible description"));
    }

    [Test]
    public void TableFormattingService_RoundTripsRowRepeatAndSplitFormatting()
    {
        global::docx.DocxTableRow row = ParseRow(
            "<w:tr xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\"><w:trPr>"
            + "<w:trHeight w:val=\"480\" w:hRule=\"atLeast\"/><w:cantSplit/><w:tblHeader w:val=\"0\"/>"
            + "</w:trPr><w:tc><w:p/></w:tc></w:tr>");
        global::docx.IDocxTableFormatting formatting = new global::docx.DocxTableFormatting();
        global::docx.DocxTableRowFormat format = formatting.GetRowFormat(row);

        Assert.That(formatting.HasRowFormat(row), Is.True);
        Assert.That(format.Height, Is.EqualTo("480"));
        Assert.That(format.HeightRule, Is.EqualTo("atLeast"));
        Assert.That(format.HasCantSplit && format.CantSplit, Is.True);
        Assert.That(format.HasRepeatHeader && !format.RepeatHeader, Is.True);

        format.Height = "720";
        format.HeightRule = "exact";
        format.HasCantSplit = true;
        format.CantSplit = false;
        format.HasRepeatHeader = true;
        format.RepeatHeader = true;
        formatting.SetRowFormat(row, format);

        global::docx.DocxTableRow written = ParseRow(row.WriteXml());
        global::docx.DocxTableRowFormat parsed = formatting.GetRowFormat(written);
        Assert.That(parsed.Height, Is.EqualTo("720"));
        Assert.That(parsed.HeightRule, Is.EqualTo("exact"));
        Assert.That(parsed.HasCantSplit && !parsed.CantSplit, Is.True);
        Assert.That(parsed.HasRepeatHeader && parsed.RepeatHeader, Is.True);
        Assert.That(written.CellElementCount(), Is.EqualTo(1));
    }

    [Test]
    public void TableFormattingService_RoundTripsCompleteCellFormatting()
    {
        global::docx.DocxTableCell cell = ParseCell(
            "<w:tc xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\"><w:tcPr>"
            + "<w:tcW w:w=\"2400\" w:type=\"dxa\"/><w:gridSpan w:val=\"2\"/><w:hMerge w:val=\"restart\"/><w:vMerge/>"
            + "<w:tcBorders><w:top w:val=\"single\" w:color=\"112233\" w:sz=\"8\"/></w:tcBorders>"
            + "<w:shd w:val=\"clear\" w:fill=\"EEEEEE\"/><w:tcMar><w:top w:w=\"80\" w:type=\"dxa\"/><w:left w:w=\"90\" w:type=\"dxa\"/><w:bottom w:w=\"100\" w:type=\"dxa\"/><w:right w:w=\"110\" w:type=\"dxa\"/></w:tcMar>"
            + "<w:textDirection w:val=\"tbRl\"/><w:vAlign w:val=\"center\"/>"
            + "</w:tcPr><w:p><w:r><w:t>Cell</w:t></w:r></w:p></w:tc>");
        global::docx.IDocxTableFormatting formatting = new global::docx.DocxTableFormatting();
        global::docx.DocxTableCellFormat format = formatting.GetCellFormat(cell);

        Assert.That(formatting.HasCellFormat(cell), Is.True);
        Assert.That(format.Width, Is.EqualTo("2400"));
        Assert.That(format.GridSpan, Is.EqualTo("2"));
        Assert.That(format.HorizontalMerge, Is.EqualTo("restart"));
        Assert.That(format.VerticalMerge, Is.EqualTo("continue"));
        Assert.That(format.Borders.TopVal, Is.EqualTo("single"));
        Assert.That(format.Shading.Fill, Is.EqualTo("EEEEEE"));
        Assert.That(format.MarginTopWidth, Is.EqualTo("80"));
        Assert.That(format.MarginLeftWidth, Is.EqualTo("90"));
        Assert.That(format.MarginBottomWidth, Is.EqualTo("100"));
        Assert.That(format.MarginRightWidth, Is.EqualTo("110"));
        Assert.That(format.TextDirection, Is.EqualTo("tbRl"));
        Assert.That(format.VerticalAlignment, Is.EqualTo("center"));

        format.Width = "3000";
        format.WidthType = "pct";
        format.GridSpan = "3";
        format.HorizontalMerge = "continue";
        format.VerticalMerge = "restart";
        format.VerticalAlignment = "bottom";
        format.TextDirection = "btLr";
        format.MarginTopWidth = "120";
        format.MarginTopType = "dxa";
        format.MarginLeftWidth = "130";
        format.MarginLeftType = "dxa";
        format.MarginBottomWidth = "140";
        format.MarginBottomType = "dxa";
        format.MarginRightWidth = "150";
        format.MarginRightType = "dxa";
        format.Borders.TopVal = "";
        format.Borders.BottomVal = "double";
        format.Borders.BottomColor = "445566";
        format.Borders.BottomSize = "12";
        format.Shading.Fill = "ABCDEF";
        formatting.SetCellFormat(cell, format);

        global::docx.DocxTableCell written = ParseCell(cell.WriteXml());
        global::docx.DocxTableCellFormat parsed = formatting.GetCellFormat(written);
        Assert.That(parsed.Width, Is.EqualTo("3000"));
        Assert.That(parsed.WidthType, Is.EqualTo("pct"));
        Assert.That(parsed.GridSpan, Is.EqualTo("3"));
        Assert.That(parsed.HorizontalMerge, Is.EqualTo("continue"));
        Assert.That(parsed.VerticalMerge, Is.EqualTo("restart"));
        Assert.That(parsed.VerticalAlignment, Is.EqualTo("bottom"));
        Assert.That(parsed.TextDirection, Is.EqualTo("btLr"));
        Assert.That(parsed.MarginTopWidth, Is.EqualTo("120"));
        Assert.That(parsed.MarginRightWidth, Is.EqualTo("150"));
        Assert.That(parsed.Borders.BottomVal, Is.EqualTo("double"));
        Assert.That(parsed.Shading.Fill, Is.EqualTo("ABCDEF"));
        Assert.That(written.GetText(), Is.EqualTo("Cell"));
    }

    [Test]
    public void TableAwareFormatting_PersistsRowAndCellChangesThroughContainingTable()
    {
        global::docx.DocxTable table = ParseTable(
            "<w:tbl xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl + "\"><w:tblPr><w:tblStyle w:val=\"Grid\"/></w:tblPr>"
            + "<w:tr><w:trPr><w:trHeight w:val=\"240\"/></w:trPr><w:tc><w:tcPr><w:tcW w:w=\"1000\" w:type=\"dxa\"/></w:tcPr><w:p><w:r><w:t>Value</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");
        global::docx.IDocxTableFormatting formatting = new global::docx.DocxTableFormatting();
        global::docx.DocxTableRowFormat rowFormat = formatting.GetTableRowFormat(table, 0);
        global::docx.DocxTableCellFormat cellFormat = formatting.GetTableCellFormat(table, 0, 0);
        rowFormat.Height = "600";
        rowFormat.HeightRule = "exact";
        rowFormat.HasRepeatHeader = true;
        rowFormat.RepeatHeader = true;
        cellFormat.Width = "2500";
        cellFormat.WidthType = "dxa";
        cellFormat.TextDirection = "tbRl";

        formatting.SetTableRowFormat(table, 0, rowFormat);
        formatting.SetTableCellFormat(table, 0, 0, cellFormat);

        global::docx.DocxTable written = ParseTable(table.WriteXml());
        Assert.That(formatting.GetTableRowFormat(written, 0).Height, Is.EqualTo("600"));
        Assert.That(formatting.GetTableRowFormat(written, 0).RepeatHeader, Is.True);
        Assert.That(formatting.GetTableCellFormat(written, 0, 0).Width, Is.EqualTo("2500"));
        Assert.That(formatting.GetTableCellFormat(written, 0, 0).TextDirection, Is.EqualTo("tbRl"));
        Assert.That(written.GetText(), Is.EqualTo("Value"));
    }

    [Test]
    public void TableEditing_CreatesEditsAndRoundTripsRowsCellsGridAndHeaders()
    {
        global::docx.IDocxTableEditing editing = new global::docx.DocxTableEditing();
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxTable table = editing.CreateTable(2, 2);
        global::docx.DocxTableRow inserted = editing.InsertRow(table, 1, 2);
        global::docx.DocxTableRow appended = editing.AppendRow(table, 1);
        global::docx.DocxTableCell insertedCell = editing.InsertCell(appended, 0);
        global::docx.DocxTableCell appendedCell = editing.AppendCell(appended);

        editing.RemoveCell(appended, 1);
        editing.MoveCell(appended, 1, 0);
        editing.MoveRow(table, 3, 2);
        editing.RemoveRow(table, 3);
        editing.SetColumnWidths(table, new List<string> { "1200", "2400" }, "dxa");
        editing.SetHeaderRow(table, 0, true);
        SetCellText(editing, factory, table.GetRow(0).GetCell(0), "Header");
        SetCellText(editing, factory, inserted.GetCell(0), "Middle");

        global::docx.DocxTable parsed = ParseTable(table.WriteXml());

        Assert.That(editing.GetRows(parsed).Count, Is.EqualTo(3));
        Assert.That(editing.GetCells(parsed.GetRow(2)).Count, Is.EqualTo(2));
        Assert.That(parsed.GetGrid().ColumnWidths, Is.EqualTo(new[] { "1200", "2400" }));
        Assert.That(parsed.GetFormat().Width, Is.EqualTo("3600"));
        Assert.That(parsed.GetRow(0).HasTableHeader, Is.True);
        Assert.That(parsed.GetRow(0).TableHeader, Is.True);
        Assert.That(parsed.GetRow(0).GetCell(0).Width, Is.EqualTo("1200"));
        Assert.That(parsed.GetRow(0).GetCell(1).Width, Is.EqualTo("2400"));
        Assert.That(editing.GetCellBlocks(insertedCell).Count, Is.EqualTo(1));
        Assert.That(editing.GetCellBlocks(appendedCell).Count, Is.EqualTo(1));
        Assert.That(parsed.GetText(), Does.Contain("Header"));
        Assert.That(parsed.GetText(), Does.Contain("Middle"));
    }

    [Test]
    public void TableEditing_InsertsAndDeletesTablesAtMixedBodyBlockPositions()
    {
        global::docx.IDocxTableEditing editing = new global::docx.DocxTableEditing();
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxBody body = factory.CreateBody();
        body.AddBlock(factory.CreateParagraphBlock(factory.CreateTextParagraph("Before")));
        body.AddBlock(factory.CreateParagraphBlock(factory.CreateTextParagraph("After")));

        global::docx.DocxTable inserted = editing.InsertTableIntoBody(body, 1, 1, 1);
        global::docx.DocxTable appended = editing.AppendTableToBody(body, 1, 1);
        SetCellText(editing, factory, inserted.GetRow(0).GetCell(0), "Inserted");
        SetCellText(editing, factory, appended.GetRow(0).GetCell(0), "Appended");

        Assert.That(body.GetBlock(0).IsParagraph(), Is.True);
        Assert.That(body.GetBlock(1).GetTable(), Is.SameAs(inserted));
        Assert.That(body.GetBlock(2).IsParagraph(), Is.True);
        Assert.That(body.GetBlock(3).GetTable(), Is.SameAs(appended));
        Assert.That(editing.RemoveTableFromBody(body, 1), Is.SameAs(inserted));

        global::docx.DocxBody parsed = new global::docx.DocxBody(body.WriteXml());
        Assert.That(parsed.BlockCount(), Is.EqualTo(3));
        Assert.That(parsed.GetBlock(0).GetText(), Is.EqualTo("Before"));
        Assert.That(parsed.GetBlock(1).GetText(), Is.EqualTo("After"));
        Assert.That(parsed.GetBlock(2).GetText(), Is.EqualTo("Appended"));
    }

    [Test]
    public void TableEditing_PreservesCellBlockOrderAndParsedNestedTableChanges()
    {
        global::docx.IDocxTableEditing editing = new global::docx.DocxTableEditing();
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxTable outer = editing.CreateTable(1, 1);
        global::docx.DocxTableCell cell = outer.GetRow(0).GetCell(0);
        editing.InsertParagraphIntoCell(cell, 0, factory.CreateTextParagraph("Before"));
        global::docx.DocxTable nested = editing.AppendNestedTable(cell, 1, 1);
        SetCellText(editing, factory, nested.GetRow(0).GetCell(0), "Nested");
        global::docx.DocxParagraph? finalParagraph = editing.GetCellBlocks(cell)[2].GetParagraph();
        Assert.That(finalParagraph, Is.Not.Null);
        finalParagraph!.InsertRun(0, factory.CreateTextRun("After"));

        global::docx.DocxTable parsedOuter = ParseTable(outer.WriteXml());
        global::docx.DocxTableCell parsedCell = parsedOuter.GetRow(0).GetCell(0);
        List<global::docx.DocxBlock> blocks = editing.GetCellBlocks(parsedCell);
        Assert.That(blocks.Count, Is.EqualTo(3));
        Assert.That(blocks[0].IsParagraph(), Is.True);
        Assert.That(blocks[1].IsTable(), Is.True);
        Assert.That(blocks[2].IsParagraph(), Is.True);
        Assert.That(parsedCell.GetText(), Is.EqualTo("Before\nNested\nAfter"));

        global::docx.DocxTable? parsedNested = blocks[1].GetTable();
        Assert.That(parsedNested, Is.Not.Null);
        editing.AppendRow(parsedNested!, 1);
        global::docx.DocxTable reparsedOuter = ParseTable(parsedOuter.WriteXml());
        Assert.That(editing.GetCellBlocks(reparsedOuter.GetRow(0).GetCell(0))[1].GetTable()!.RowCount(), Is.EqualTo(2));

        global::docx.DocxBlock removed = editing.RemoveCellBlock(parsedCell, 1);
        Assert.That(removed.IsTable(), Is.True);
        Assert.That(editing.GetCellBlocks(parsedCell).Count, Is.EqualTo(2));
    }

    [Test]
    public void TableEditing_MergesCellsAndProjectsMergeAwareTextMatrix()
    {
        global::docx.IDocxTableEditing editing = new global::docx.DocxTableEditing();
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxTable table = editing.CreateTable(3, 3);
        SetCellText(editing, factory, table.GetRow(0).GetCell(0), "A");
        SetCellText(editing, factory, table.GetRow(0).GetCell(1), "B");
        SetCellText(editing, factory, table.GetRow(0).GetCell(2), "C");
        SetCellText(editing, factory, table.GetRow(1).GetCell(0), "Top");
        SetCellText(editing, factory, table.GetRow(2).GetCell(0), "Hidden");

        editing.MergeCellsHorizontally(table, 0, 0, 2);
        editing.MergeCellsVertically(table, 0, 1, 2);
        List<List<string>> matrix = editing.ProjectTableToMatrix(table);

        Assert.That(table.GetRow(0).CellCount(), Is.EqualTo(2));
        Assert.That(table.GetRow(0).GetCell(0).GetGridSpanCount(), Is.EqualTo(2));
        Assert.That(table.GetRow(1).GetCell(0).VerticalMerge, Is.EqualTo("restart"));
        Assert.That(table.GetRow(2).GetCell(0).VerticalMerge, Is.EqualTo("continue"));
        Assert.That(matrix[0], Is.EqualTo(new[] { "A\nB", "A\nB", "C" }));
        Assert.That(matrix[1][0], Is.EqualTo("Top"));
        Assert.That(matrix[2][0], Is.EqualTo("Top"));

        global::docx.DocxTable parsed = ParseTable(table.WriteXml());
        Assert.That(editing.ProjectTableToMatrix(parsed)[2][0], Is.EqualTo("Top"));
    }

    [Test]
    public void TableEditing_MatrixKeepsEmptyVerticalMergeOriginEmpty()
    {
        global::docx.DocxTable table = ParseTable(
            "<w:tbl xmlns:w=\"" + global::docx.DocxConstants.NamespaceWordprocessingMl
            + "\"><w:tr><w:tc><w:tcPr><w:vMerge w:val=\"restart\"/></w:tcPr><w:p/></w:tc>"
            + "<w:tc><w:p><w:r><w:t>Top right</w:t></w:r></w:p></w:tc></w:tr>"
            + "<w:tr><w:tc><w:tcPr><w:vMerge/></w:tcPr><w:p><w:r><w:t>Hidden continuation</w:t></w:r></w:p></w:tc>"
            + "<w:tc><w:p><w:r><w:t>Bottom right</w:t></w:r></w:p></w:tc></w:tr></w:tbl>");
        global::docx.IDocxTableEditing editing = new global::docx.DocxTableEditing();

        List<List<string>> matrix = editing.ProjectTableToMatrix(table);

        Assert.That(matrix[0], Is.EqualTo(new string[] { "", "Top right" }));
        Assert.That(matrix[1], Is.EqualTo(new string[] { "", "Bottom right" }));
        Assert.That(editing.ExtractTableText(table), Does.Contain("Hidden continuation"));
    }

    [Test]
    public void TableEditing_ExtractsTextWithCallerSeparators()
    {
        global::docx.IDocxTableEditing editing = new global::docx.DocxTableEditing();
        global::docx.DocxContentFactory factory = new global::docx.DocxContentFactory();
        global::docx.DocxTable table = editing.CreateTable(2, 2);
        SetCellText(editing, factory, table.GetRow(0).GetCell(0), "A");
        SetCellText(editing, factory, table.GetRow(0).GetCell(1), "B");
        SetCellText(editing, factory, table.GetRow(1).GetCell(0), "C");
        SetCellText(editing, factory, table.GetRow(1).GetCell(1), "D");
        global::docx.DocxTextOptions options = new global::docx.DocxTextOptions();
        options.TableCellSeparator = " | ";
        options.TableRowSeparator = " / ";

        Assert.That(editing.ExtractTableText(table), Is.EqualTo("A\tB\nC\tD"));
        Assert.That(editing.ExtractTableTextWithOptions(table, options), Is.EqualTo("A | B / C | D"));
        Assert.That(editing.ProjectTableToMatrixWithOptions(table, options)[1], Is.EqualTo(new[] { "C", "D" }));
    }

    [Test]
    public void TableFormattingService_RejectsInvalidArguments()
    {
        global::docx.IDocxTableFormatting formatting = new global::docx.DocxTableFormatting();
        global::docx.DocxTable table = new global::docx.DocxTable("");
        global::docx.DocxTableRow row = global::docx.DocxTableRow.Create();
        global::docx.DocxTableCell cell = global::docx.DocxTableCell.Create();

        Assert.Throws<ArgumentNullException>(() => formatting.HasTableFormat(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetTableFormat(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.SetTableFormat(null!, new global::docx.DocxTableFormat()));
        Assert.Throws<ArgumentNullException>(() => formatting.SetTableFormat(table, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.HasRowFormat(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetRowFormat(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.SetRowFormat(null!, new global::docx.DocxTableRowFormat()));
        Assert.Throws<ArgumentNullException>(() => formatting.SetRowFormat(row, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.HasCellFormat(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetCellFormat(null!));
        Assert.Throws<ArgumentNullException>(() => formatting.SetCellFormat(null!, new global::docx.DocxTableCellFormat()));
        Assert.Throws<ArgumentNullException>(() => formatting.SetCellFormat(cell, null!));
        Assert.Throws<ArgumentNullException>(() => formatting.GetTableRowFormat(null!, 0));
        Assert.Throws<ArgumentNullException>(() => formatting.SetTableRowFormat(null!, 0, new global::docx.DocxTableRowFormat()));
        Assert.Throws<ArgumentNullException>(() => formatting.GetTableCellFormat(null!, 0, 0));
        Assert.Throws<ArgumentNullException>(() => formatting.SetTableCellFormat(null!, 0, 0, new global::docx.DocxTableCellFormat()));
    }

    [Test]
    public void TableFormatParsing_RejectsInvalidArguments()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxTableFormat format = new global::docx.DocxTableFormat();
        global::docx.DocxTable table = new global::docx.DocxTable("");

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableFormat.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableFormat.ParseXml(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadProperty(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadTableLook(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadCellMargins(null!));
        Assert.Throws<ArgumentNullException>(() => format.ReadCellMarginElement(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetStyleId(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetWidth(null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetWidth("", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetAlignment(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetLayoutType(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetIndent(null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetIndent("", null!));
        Assert.Throws<ArgumentNullException>(() => format.SetLookValue(null!));
        Assert.Throws<ArgumentNullException>(() => format.SetCellMargins(null!, "", "", "", "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetCellMargins("", null!, "", "", "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetCellMargins("", "", null!, "", "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetCellMargins("", "", "", null!, "", "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetCellMargins("", "", "", "", null!, "", "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetCellMargins("", "", "", "", "", null!, "", ""));
        Assert.Throws<ArgumentNullException>(() => format.SetCellMargins("", "", "", "", "", "", null!, ""));
        Assert.Throws<ArgumentNullException>(() => format.SetCellMargins("", "", "", "", "", "", "", null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(null!, "tblStyle", "val", "TableGrid"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), null!, "val", "TableGrid"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), "tblStyle", null!, "TableGrid"));
        Assert.Throws<ArgumentNullException>(() => format.WriteStringProperty(global::docx.DocxXmlWriter.Create(), "tblStyle", "val", null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteWidthTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteIndentTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteTableLookTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteCellMarginsTo(null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteCellMarginElement(null!, "top", "120", "dxa"));
        Assert.Throws<ArgumentNullException>(() => format.WriteCellMarginElement(global::docx.DocxXmlWriter.Create(), null!, "120", "dxa"));
        Assert.Throws<ArgumentNullException>(() => format.WriteCellMarginElement(global::docx.DocxXmlWriter.Create(), "top", null!, "dxa"));
        Assert.Throws<ArgumentNullException>(() => format.WriteCellMarginElement(global::docx.DocxXmlWriter.Create(), "top", "120", null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalAttribute(null!, "w", "120"));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "120"));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "w", null!));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalBoolAttribute(null!, "firstRow", true, true));
        Assert.Throws<ArgumentNullException>(() => format.WriteOptionalBoolAttribute(global::docx.DocxXmlWriter.Create(), null!, true, true));
        Assert.Throws<ArgumentNullException>(() => table.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => table.AddParsedChild(null!));
        Assert.Throws<ArgumentNullException>(() => table.AddRow(null!));
        Assert.Throws<ArgumentNullException>(() => table.InsertRow(0, null!));
        Assert.Throws<ArgumentNullException>(() => table.AddParsedRow(null!));
        Assert.Throws<ArgumentNullException>(() => table.SetGrid(null!));
        Assert.Throws<ArgumentNullException>(() => table.SetWidth(null!, ""));
        Assert.Throws<ArgumentNullException>(() => table.SetWidth("", null!));
        Assert.Throws<ArgumentNullException>(() => table.SetGridColumnWidths(null!));
        Assert.Throws<ArgumentNullException>(() => table.SetWidthFromGrid(null!));
        Assert.Throws<ArgumentNullException>(() => table.ApplyGridColumnWidthsToCells(null!));
        Assert.Throws<InvalidOperationException>(() => table.SetWidthFromGrid("dxa"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.DocxTableGridFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.DocxTableRowFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => table.SetPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => table.SetFormat(null!));
        Assert.Throws<ArgumentNullException>(() => table.CountChildElements(null!, ""));
        Assert.Throws<ArgumentNullException>(() => table.CountChildElements("tr", null!));
        Assert.Throws<ArgumentException>(() => table.CountChildElements("", ""));
        Assert.Throws<ArgumentNullException>(() => table.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => table.WritePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => table.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => table.WriteGridTo(null!));
        Assert.Throws<ArgumentNullException>(() => table.WriteRowsTo(null!));
        Assert.Throws<ArgumentNullException>(() => table.WriteUnknownChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => table.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => table.ToTextMatrixWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.GetRowText(null!, new global::docx.DocxTextOptions()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.GetRowText(global::docx.DocxTableRow.Create(), null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.ProjectRowText(null!, new global::docx.DocxTextOptions(), new List<string>()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.ProjectRowText(global::docx.DocxTableRow.Create(), null!, new List<string>()));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.ProjectRowText(global::docx.DocxTableRow.Create(), new global::docx.DocxTextOptions(), null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.IsManagedTableChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.IsTableGridChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.IsTableRowChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTable.ValidateCount(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxTable.Create(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => global::docx.DocxTable.Create(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.AddRowWithCells(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.InsertRowWithCells(0, -1));
        global::docx.DocxTableGrid grid = global::docx.DocxTableGrid.Create();
        global::docx.DocxTableRow row = global::docx.DocxTableRow.Create();
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTableGrid(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTableRow(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableGrid.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableRow.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddParsedChild(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddColumnWidth(null!));
        Assert.Throws<ArgumentNullException>(() => grid.AddParsedColumnWidth(null!));
        Assert.Throws<ArgumentNullException>(() => grid.SetColumnWidth(0, null!));
        Assert.Throws<ArgumentNullException>(() => grid.SetColumnWidths(null!));
        Assert.Throws<ArgumentNullException>(() => grid.SetColumnWidths(new List<string> { null! }));
        Assert.Throws<ArgumentNullException>(() => grid.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => grid.WriteColumnsTo(null!));
        Assert.Throws<ArgumentNullException>(() => grid.WriteUnknownChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableGrid.IsGridColumnChild(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddParsedChild(null!));
        Assert.Throws<ArgumentNullException>(() => row.AddCell(null!));
        Assert.Throws<ArgumentNullException>(() => row.InsertCell(0, null!));
        Assert.Throws<ArgumentNullException>(() => row.AddParsedCell(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableRow.DocxTableCellFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => row.ReadPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => row.ReadProperty(null!));
        Assert.Throws<ArgumentNullException>(() => row.SetPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => row.SetHeight(null!, ""));
        Assert.Throws<ArgumentNullException>(() => row.SetHeight("", null!));
        Assert.Throws<ArgumentNullException>(() => row.CountChildElements(null!, ""));
        Assert.Throws<ArgumentNullException>(() => row.CountChildElements("tc", null!));
        Assert.Throws<ArgumentException>(() => row.CountChildElements("", ""));
        Assert.Throws<ArgumentNullException>(() => row.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => row.WriteCellsTo(null!));
        Assert.Throws<ArgumentNullException>(() => row.WriteUnknownChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => row.WritePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => row.WriteHeightTo(null!));
        Assert.Throws<ArgumentNullException>(() => row.WriteToggleProperty(null!, "cantSplit", true, true));
        Assert.Throws<ArgumentNullException>(() => row.WriteToggleProperty(global::docx.DocxXmlWriter.Create(), null!, true, true));
        Assert.Throws<ArgumentNullException>(() => row.WriteOptionalAttribute(null!, "val", "240"));
        Assert.Throws<ArgumentNullException>(() => row.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "240"));
        Assert.Throws<ArgumentNullException>(() => row.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "val", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableRow.IsCellChild(null!));
        global::docx.DocxTableCell cell = global::docx.DocxTableCell.Create();
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxTableCell(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableCell.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => cell.ReadPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => cell.ReadProperty(null!));
        Assert.Throws<ArgumentNullException>(() => cell.SetPropertiesXml(null!));
        Assert.Throws<ArgumentNullException>(() => cell.SetWidth(null!, ""));
        Assert.Throws<ArgumentNullException>(() => cell.SetWidth("", null!));
        Assert.Throws<ArgumentNullException>(() => cell.SetVerticalAlignment(null!));
        Assert.Throws<ArgumentNullException>(() => cell.SetGridSpan(null!));
        Assert.Throws<ArgumentNullException>(() => cell.SetHorizontalMerge(null!));
        Assert.Throws<ArgumentNullException>(() => cell.SetVerticalMerge(null!));
        Assert.Throws<ArgumentNullException>(() => cell.AddChild(null!));
        Assert.Throws<ArgumentNullException>(() => cell.AddParsedChild(null!));
        Assert.Throws<ArgumentNullException>(() => cell.AddParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => cell.AddParsedParagraph(null!));
        Assert.Throws<ArgumentNullException>(() => cell.AddTable(null!));
        Assert.Throws<ArgumentNullException>(() => cell.InsertTable(0, null!));
        Assert.Throws<ArgumentNullException>(() => cell.AddParsedTable(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => cell.AddNewTable(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => cell.AddNewTable(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => cell.InsertNewTable(0, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => cell.InsertNewTable(0, 1, -1));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableCell.DocxParagraphFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableCell.DocxTableFromBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => cell.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => cell.WritePropertiesTo(null!));
        Assert.Throws<ArgumentNullException>(() => cell.WriteWidthTo(null!));
        Assert.Throws<ArgumentNullException>(() => cell.WriteParagraphsTo(null!));
        Assert.Throws<ArgumentNullException>(() => cell.WriteTablesTo(null!));
        Assert.Throws<ArgumentNullException>(() => cell.WriteUnknownChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => cell.GetTextWithOptions(null!));
        Assert.Throws<ArgumentNullException>(() => cell.WriteValueElement(null!, "vAlign", "center"));
        Assert.Throws<ArgumentNullException>(() => cell.WriteValueElement(global::docx.DocxXmlWriter.Create(), null!, "center"));
        Assert.Throws<ArgumentNullException>(() => cell.WriteValueElement(global::docx.DocxXmlWriter.Create(), "vAlign", null!));
        Assert.Throws<ArgumentNullException>(() => cell.WriteOptionalAttribute(null!, "w", "1000"));
        Assert.Throws<ArgumentNullException>(() => cell.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), null!, "1000"));
        Assert.Throws<ArgumentNullException>(() => cell.WriteOptionalAttribute(global::docx.DocxXmlWriter.Create(), "w", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableCell.ReadMergeValue(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableCell.IsParagraphChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxTableCell.IsTableChild(null!));
        Assert.Throws<FormatException>(() => new global::docx.DocxTableRow("<w:tr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:trPr><w:cantSplit w:val=\"maybe\" /></w:trPr></w:tr>"));
        Assert.Throws<FormatException>(() => global::docx.DocxTableFormat.ParseXml("<w:tblPr xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tblLook w:firstRow=\"maybe\" /></w:tblPr>"));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxTableFormat.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxTableGrid.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxTableRow.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxTableCell.Parse(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadTableLook(reader));
        Assert.Throws<InvalidOperationException>(() => format.ReadCellMargins(reader));
    }

    static global::docx.DocxTableFormat ParseFormat(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("tblPr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxTableFormat.Parse(reader);
    }

    static global::docx.DocxTable ParseTable(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("tbl", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxTable.Parse(reader);
    }

    static global::docx.DocxTableGrid ParseGrid(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("tblGrid", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxTableGrid.Parse(reader);
    }

    static global::docx.DocxTableRow ParseRow(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("tr", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxTableRow.Parse(reader);
    }

    static global::docx.DocxTableCell ParseCell(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("tc", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxTableCell.Parse(reader);
    }

    static global::docx.DocxParagraph CreateParagraph(string text)
    {
        global::docx.DocxParagraph paragraph = new global::docx.DocxParagraph("");

        paragraph.AddChild(new global::docx.DocxXmlBuffer("<w:r xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:t>" + text + "</w:t></w:r>"));

        return paragraph;
    }

    static void SetCellText(global::docx.IDocxTableEditing editing, global::docx.DocxContentFactory factory, global::docx.DocxTableCell cell, string text)
    {
        List<global::docx.DocxBlock> blocks = editing.GetCellBlocks(cell);
        global::docx.DocxParagraph? paragraph = blocks[blocks.Count - 1].GetParagraph();

        Assert.That(paragraph, Is.Not.Null);
        paragraph!.InsertRun(0, factory.CreateTextRun(text));
    }
}
