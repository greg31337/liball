using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class TableTests
{
    [Test]
    public void Slide_AddTableCreatesCellsSizingAndTextThatRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        PptxTable table = slide.AddTable(2, 3, new PptxBounds(0L, 0L, 600L, 240L));
        table.SetText(0, 0, "A1");
        table.GetCell(1, 2).SetText("R2C3\nNext\tTab");
        table.SetColumnWidth(1, 250L);
        table.SetRowHeight(0, 140L);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<PptxTable> tables = reopened.GetSlide(0).GetTables();
        PptxTable reopenedTable = tables[0];

        Assert.That(table.Shape.IsTable(), Is.True);
        Assert.That(table.Shape.AsTable().ColumnCount(), Is.EqualTo(3));
        Assert.That(tables.Count, Is.EqualTo(1));
        Assert.That(reopenedTable.RowCount(), Is.EqualTo(2));
        Assert.That(reopenedTable.ColumnCount(), Is.EqualTo(3));
        Assert.That(reopenedTable.GetColumnWidth(0), Is.EqualTo(200L));
        Assert.That(reopenedTable.GetColumnWidth(1), Is.EqualTo(250L));
        Assert.That(reopenedTable.GetRowHeight(0), Is.EqualTo(140L));
        Assert.That(reopenedTable.GetRowHeight(1), Is.EqualTo(120L));
        Assert.That(reopenedTable.GetText(0, 0), Is.EqualTo("A1"));
        Assert.That(reopenedTable.GetText(1, 2), Is.EqualTo("R2C3\nNext\tTab"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Slide_LoadsExistingTableAndEditsCellsThroughModel()
    {
        PptxPresentation presentation = LoadTablePresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxTable table = slide.GetTable(0);

        table.GetCell(1, 1).SetText("Updated\nCell");
        table.SetColumnWidth(0, 333L);
        table.GetRow(1).SetHeight(444L);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxSlide reopenedSlide = reopened.GetSlide(0);
        PptxTable reopenedTable = reopenedSlide.GetTable(0);

        Assert.That(table.RowCount(), Is.EqualTo(2));
        Assert.That(table.ColumnCount(), Is.EqualTo(2));
        Assert.That(reopenedTable.Shape.IsTable(), Is.True);
        Assert.That(reopenedTable.GetText(0, 0), Is.EqualTo("A1"));
        Assert.That(reopenedTable.GetText(1, 1), Is.EqualTo("Updated\nCell"));
        Assert.That(reopenedSlide.GetTableCellText(1, 1), Is.EqualTo("Updated\nCell"));
        Assert.That(reopenedTable.GetColumnWidth(0), Is.EqualTo(333L));
        Assert.That(reopenedTable.GetColumnWidth(1), Is.EqualTo(222L));
        Assert.That(reopenedTable.GetRowHeight(0), Is.EqualTo(333L));
        Assert.That(reopenedTable.GetRowHeight(1), Is.EqualTo(444L));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Table_SetFormattingWritesStyleFlagsAndCellProperties()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxTable table = presentation.GetSlide(0).AddTable(1, 1, new PptxBounds(0L, 0L, 300L, 100L));
        PptxTableCell cell = table.GetCell(0, 0);

        table.SetStyleId("{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}");
        table.SetFirstRow(true);
        table.SetBandedRows(true);
        table.SetLastColumn(false);
        cell.SetFill(PptxFill.CreateSolidRgb("123456"));
        cell.SetBorder("left", PptxLine.CreateSolidRgb("ABCDEF", 12700L));
        cell.SetMargins(11L, 22L, 33L, 44L);
        cell.SetVerticalAlignment("middle");
        cell.SetTextDirection("vertical");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxTable reopenedTable = reopened.GetSlide(0).GetTable(0);
        PptxTableCell reopenedCell = reopenedTable.GetCell(0, 0);

        Assert.That(reopenedTable.GetStyleId(), Is.EqualTo("{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}"));
        Assert.That(reopenedTable.GetFirstRow(), Is.True);
        Assert.That(reopenedTable.GetBandedRows(), Is.True);
        Assert.That(reopenedTable.GetLastColumn(), Is.False);
        Assert.That(reopenedCell.GetFill().Type, Is.EqualTo(PptxFill.Solid));
        Assert.That(reopenedCell.GetFill().Color.Value, Is.EqualTo("123456"));
        Assert.That(reopenedCell.GetBorder("left").Type, Is.EqualTo(PptxLine.Solid));
        Assert.That(reopenedCell.GetBorder("left").Color.Value, Is.EqualTo("ABCDEF"));
        Assert.That(reopenedCell.GetBorder("left").Width, Is.EqualTo(12700L));
        Assert.That(reopenedCell.GetMarginLeft(), Is.EqualTo(11L));
        Assert.That(reopenedCell.GetMarginTop(), Is.EqualTo(22L));
        Assert.That(reopenedCell.GetMarginRight(), Is.EqualTo(33L));
        Assert.That(reopenedCell.GetMarginBottom(), Is.EqualTo(44L));
        Assert.That(reopenedCell.GetVerticalAlignment(), Is.EqualTo("ctr"));
        Assert.That(reopenedCell.GetTextDirection(), Is.EqualTo("vert"));
        Assert.That(reopenedCell.Element.OuterXml, Does.Contain("lnL"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Table_LoadsExistingFormattingAndReplacesCellFormatting()
    {
        PptxPresentation presentation = LoadFormattedTablePresentation();
        PptxTable table = presentation.GetSlide(0).GetTable(0);
        PptxTableCell cell = table.GetCell(0, 0);

        Assert.That(table.GetStyleId(), Is.EqualTo("{11111111-2222-3333-4444-555555555555}"));
        Assert.That(table.GetFirstColumn(), Is.True);
        Assert.That(table.GetStyleFlag("banded-columns"), Is.True);
        Assert.That(cell.GetFill().Color.Value, Is.EqualTo("EEEEEE"));
        Assert.That(cell.GetBorder("bottom").Color.Value, Is.EqualTo("111111"));
        Assert.That(cell.GetBorder("bottom").Width, Is.EqualTo(987L));
        Assert.That(cell.GetMarginLeft(), Is.EqualTo(101L));
        Assert.That(cell.GetMarginTop(), Is.EqualTo(202L));
        Assert.That(cell.GetVerticalAlignment(), Is.EqualTo("b"));
        Assert.That(cell.GetTextDirection(), Is.EqualTo("vert270"));

        table.SetStyleFlag("banded-columns", false);
        table.SetBandedRows(true);
        cell.SetFill(PptxFill.CreateNone());
        cell.SetBorder("bottom", PptxLine.CreateSolidRgb("222222", 654L));
        cell.SetMargin("right", 303L);
        cell.SetVerticalAlignment("top");
        cell.SetTextDirection("horizontal");
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        PptxTable reopenedTable = reopened.GetSlide(0).GetTable(0);
        PptxTableCell reopenedCell = reopenedTable.GetCell(0, 0);

        Assert.That(reopenedTable.GetStyleFlag("banded-columns"), Is.False);
        Assert.That(reopenedTable.GetBandedRows(), Is.True);
        Assert.That(reopenedCell.GetFill().Type, Is.EqualTo(PptxFill.None));
        Assert.That(reopenedCell.GetBorder("bottom").Color.Value, Is.EqualTo("222222"));
        Assert.That(reopenedCell.GetBorder("bottom").Width, Is.EqualTo(654L));
        Assert.That(reopenedCell.GetMarginRight(), Is.EqualTo(303L));
        Assert.That(reopenedCell.GetVerticalAlignment(), Is.EqualTo("t"));
        Assert.That(reopenedCell.GetTextDirection(), Is.EqualTo("horz"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Table_MergeAndSplitCellsRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxTable table = presentation.GetSlide(0).AddTable(3, 3, new PptxBounds(0L, 0L, 900L, 300L));

        table.SetData(CreateThreeByThreeData());
        table.MergeCells(0, 0, 2, 2);
        PptxPresentation merged = SaveReopenAndLoad(presentation);
        PptxTable mergedTable = merged.GetSlide(0).GetTable(0);
        List<List<string>> mergedData = mergedTable.ExtractData();

        Assert.That(mergedTable.GetCell(0, 0).IsMergeOrigin(), Is.True);
        Assert.That(mergedTable.GetCell(0, 0).GetColumnSpan(), Is.EqualTo(2));
        Assert.That(mergedTable.GetCell(0, 0).GetRowSpan(), Is.EqualTo(2));
        Assert.That(mergedTable.GetCell(0, 1).IsHorizontalMerge(), Is.True);
        Assert.That(mergedTable.GetCell(1, 0).IsVerticalMerge(), Is.True);
        Assert.That(mergedTable.GetCell(1, 1).IsHorizontalMerge(), Is.True);
        Assert.That(mergedTable.GetCell(1, 1).IsVerticalMerge(), Is.True);
        Assert.That(mergedData[0][0], Is.EqualTo("A1"));
        Assert.That(mergedData[0][1], Is.EqualTo(""));
        Assert.That(mergedData[1][0], Is.EqualTo(""));
        Assert.That(mergedData[1][1], Is.EqualTo(""));

        mergedTable.SplitCell(1, 1);
        PptxPresentation split = SaveReopenAndLoad(merged);
        PptxTable splitTable = split.GetSlide(0).GetTable(0);

        Assert.That(splitTable.GetCell(0, 0).IsMerged(), Is.False);
        Assert.That(splitTable.GetCell(0, 1).IsMerged(), Is.False);
        Assert.That(splitTable.GetCell(1, 0).IsMerged(), Is.False);
        Assert.That(splitTable.GetCell(1, 1).IsMerged(), Is.False);
        Assert.That(splitTable.GetCell(0, 0).GetText(), Is.EqualTo("A1"));
        Assert.That(splitTable.GetCell(1, 1).GetText(), Is.EqualTo("B2"));
        Assert.That(split.Package.Validate().Succeeded(), Is.True, split.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Table_ImportsAndExtractsRectangularData()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxTable table = presentation.GetSlide(0).AddTable(2, 3, new PptxBounds(0L, 0L, 600L, 200L));

        table.SetData(CreateTwoByThreeData());
        table.SetDataAt(1, 1, CreateOneByTwoData());
        PptxPresentation reopened = SaveReopenAndLoad(presentation);
        List<List<string>> data = reopened.GetSlide(0).GetTable(0).GetData();

        Assert.That(data.Count, Is.EqualTo(2));
        Assert.That(data[0].Count, Is.EqualTo(3));
        Assert.That(data[0][0], Is.EqualTo("North"));
        Assert.That(data[0][1], Is.EqualTo("South"));
        Assert.That(data[0][2], Is.EqualTo("East"));
        Assert.That(data[1][0], Is.EqualTo("West"));
        Assert.That(data[1][1], Is.EqualTo("Patch 1"));
        Assert.That(data[1][2], Is.EqualTo("Patch 2"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Table_RejectsNonRectangularData()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxTable table = presentation.GetSlide(0).AddTable(2, 3, new PptxBounds(0L, 0L, 600L, 200L));

        Assert.Throws<System.ArgumentException>(() => table.SetData(CreateNonRectangularData()));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => table.MergeCells(1, 2, 2, 2));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => table.MergeCells(0, 0, int.MaxValue, 1));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => table.MergeCells(0, 0, 1, int.MaxValue));
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation LoadTablePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateTableSlideXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation LoadFormattedTablePresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateFormattedTableSlideXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static string CreateTableSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"2\" name=\"Table\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">" +
            CreateTableXml() +
            "</a:graphicData></a:graphic>" +
            "</p:graphicFrame>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateFormattedTableSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            "<p:graphicFrame>" +
            "<p:nvGraphicFramePr><p:cNvPr id=\"2\" name=\"Formatted Table\"/><p:cNvGraphicFramePr/><p:nvPr/></p:nvGraphicFramePr>" +
            "<p:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"400\" cy=\"200\"/></p:xfrm>" +
            "<a:graphic><a:graphicData uri=\"http://schemas.openxmlformats.org/drawingml/2006/table\">" +
            CreateFormattedTableXml() +
            "</a:graphicData></a:graphic>" +
            "</p:graphicFrame>" +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateTableXml()
    {
        return
            "<a:tbl>" +
            "<a:tblPr/>" +
            "<a:tblGrid><a:gridCol w=\"111\"/><a:gridCol w=\"222\"/></a:tblGrid>" +
            "<a:tr h=\"333\">" +
            CreateCellXml("A1") +
            CreateCellXml("A2") +
            "</a:tr>" +
            "<a:tr h=\"222\">" +
            CreateCellXml("B1") +
            CreateCellXml("B2") +
            "</a:tr>" +
            "</a:tbl>";
    }

    static string CreateCellXml(string text)
    {
        return
            "<a:tc>" +
            "<a:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>" + text + "</a:t></a:r></a:p></a:txBody>" +
            "<a:tcPr/>" +
            "</a:tc>";
    }

    static string CreateFormattedTableXml()
    {
        return
            "<a:tbl>" +
            "<a:tblPr firstCol=\"1\" bandCol=\"1\"><a:tableStyleId>{11111111-2222-3333-4444-555555555555}</a:tableStyleId></a:tblPr>" +
            "<a:tblGrid><a:gridCol w=\"111\"/></a:tblGrid>" +
            "<a:tr h=\"333\">" +
            "<a:tc>" +
            "<a:txBody><a:bodyPr/><a:lstStyle/><a:p><a:r><a:t>A1</a:t></a:r></a:p></a:txBody>" +
            "<a:tcPr marL=\"101\" marT=\"202\" anchor=\"b\" vert=\"vert270\">" +
            "<a:solidFill><a:srgbClr val=\"EEEEEE\"/></a:solidFill>" +
            "<a:lnB w=\"987\"><a:solidFill><a:srgbClr val=\"111111\"/></a:solidFill></a:lnB>" +
            "</a:tcPr>" +
            "</a:tc>" +
            "</a:tr>" +
            "</a:tbl>";
    }

    static List<List<string>> CreateThreeByThreeData()
    {
        List<List<string>> data = new List<List<string>>();
        data.Add(CreateRow("A1", "A2", "A3"));
        data.Add(CreateRow("B1", "B2", "B3"));
        data.Add(CreateRow("C1", "C2", "C3"));
        return data;
    }

    static List<List<string>> CreateTwoByThreeData()
    {
        List<List<string>> data = new List<List<string>>();
        data.Add(CreateRow("North", "South", "East"));
        data.Add(CreateRow("West", "Center", "Far"));
        return data;
    }

    static List<List<string>> CreateOneByTwoData()
    {
        List<List<string>> data = new List<List<string>>();
        data.Add(CreateRow("Patch 1", "Patch 2"));
        return data;
    }

    static List<List<string>> CreateNonRectangularData()
    {
        List<List<string>> data = new List<List<string>>();
        data.Add(CreateRow("A", "B"));
        data.Add(CreateRow("C"));
        return data;
    }

    static List<string> CreateRow(string first)
    {
        List<string> row = new List<string>();
        row.Add(first);
        return row;
    }

    static List<string> CreateRow(string first, string second)
    {
        List<string> row = new List<string>();
        row.Add(first);
        row.Add(second);
        return row;
    }

    static List<string> CreateRow(string first, string second, string third)
    {
        List<string> row = new List<string>();
        row.Add(first);
        row.Add(second);
        row.Add(third);
        return row;
    }
}
