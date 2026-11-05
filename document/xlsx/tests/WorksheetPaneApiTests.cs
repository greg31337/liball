using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetPaneApiTests
{
    [Test]
    public void FreezePanes_ByCountsCreatesFrozenPaneAndSelection()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetPaneResult result = part.FreezePanesByCount(1, 2);
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CreatedView, Is.True);
        Assert.That(result.HasPane, Is.True);
        Assert.That(part.SheetViews.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].HasPane, Is.True);
        Assert.That(part.SheetViews[0].Pane.XSplit, Is.EqualTo(2d).Within(0.0000001d));
        Assert.That(part.SheetViews[0].Pane.YSplit, Is.EqualTo(1d).Within(0.0000001d));
        Assert.That(part.SheetViews[0].Pane.TopLeftCell, Is.EqualTo("C2"));
        Assert.That(part.SheetViews[0].Pane.ActivePane, Is.EqualTo("bottomRight"));
        Assert.That(part.SheetViews[0].Pane.State, Is.EqualTo("frozen"));
        Assert.That(part.SheetViews[0].Selections.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].Selections[0].Pane, Is.EqualTo("bottomRight"));
        Assert.That(part.SheetViews[0].Selections[0].ActiveCell, Is.EqualTo("C2"));
        Assert.That(part.SheetViews[0].Selections[0].Sqref, Is.EqualTo("C2"));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<pane xSplit=\"2\" ySplit=\"1\" topLeftCell=\"C2\" activePane=\"bottomRight\" state=\"frozen\" />"));
        Assert.That(writeResult.Xml, Does.Contain("<selection pane=\"bottomRight\" activeCell=\"C2\" sqref=\"C2\" />"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.SheetViews[0].Pane.TopLeftCell, Is.EqualTo("C2"));
    }

    [Test]
    public void FreezePanes_ByTopLeftCellAndClearPanesUpdatePrimaryView()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetPaneResult freeze = part.FreezePanes("B1");
        XlsxWorksheetPaneResult clear = part.ClearPanes();
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);

        Assert.That(freeze.IsValid, Is.True);
        Assert.That(freeze.Pane!.XSplit, Is.EqualTo(1d).Within(0.0000001d));
        Assert.That(freeze.Pane.HasYSplit, Is.False);
        Assert.That(freeze.Pane.TopLeftCell, Is.EqualTo("B1"));
        Assert.That(freeze.Pane.ActivePane, Is.EqualTo("topRight"));
        Assert.That(freeze.Selection!.Pane, Is.EqualTo("topRight"));
        Assert.That(clear.IsValid, Is.True);
        Assert.That(clear.Cleared, Is.True);
        Assert.That(part.SheetViews[0].HasPane, Is.False);
        Assert.That(part.SheetViews[0].Selections.Count, Is.EqualTo(0));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Not.Contain("<pane"));
        Assert.That(writeResult.Xml, Does.Not.Contain("<selection"));
    }

    [Test]
    public void SplitPanes_CreatesSplitPaneAndSelection()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetView existing = new XlsxWorksheetView();
        existing.View = "normal";
        part.SheetViews.Add(existing);

        XlsxWorksheetPaneResult result = part.SplitPanesAtCell(2000d, 3000d, "D5");
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.CreatedView, Is.False);
        Assert.That(result.Pane!.XSplit, Is.EqualTo(2000d).Within(0.0000001d));
        Assert.That(result.Pane.YSplit, Is.EqualTo(3000d).Within(0.0000001d));
        Assert.That(result.Pane.TopLeftCell, Is.EqualTo("D5"));
        Assert.That(result.Pane.ActivePane, Is.EqualTo("bottomRight"));
        Assert.That(result.Pane.State, Is.EqualTo("split"));
        Assert.That(result.Selection!.ActiveCell, Is.EqualTo("D5"));
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<pane xSplit=\"2000\" ySplit=\"3000\" topLeftCell=\"D5\" activePane=\"bottomRight\" state=\"split\" />"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.SheetViews[0].Pane.State, Is.EqualTo("split"));
        Assert.That(roundTrip.SheetViews[0].Selections[0].ActiveCell, Is.EqualTo("D5"));
    }

    [Test]
    public void PaneApis_ReportInvalidInputs()
    {
        XlsxWorksheetPart nullViews = new XlsxWorksheetPart();
        XlsxWorksheetPart emptyFreeze = new XlsxWorksheetPart();
        XlsxWorksheetPart rowRange = new XlsxWorksheetPart();
        XlsxWorksheetPart columnRange = new XlsxWorksheetPart();
        XlsxWorksheetPart badTopLeft = new XlsxWorksheetPart();
        XlsxWorksheetPart badSplit = new XlsxWorksheetPart();
        XlsxWorksheetPart emptySplit = new XlsxWorksheetPart();

        nullViews.SheetViews = null!;

        XlsxWorksheetPaneResult nullViewsResult = nullViews.FreezePanesByCount(1, 0);
        XlsxWorksheetPaneResult emptyFreezeResult = emptyFreeze.FreezePanesByCount(0, 0);
        XlsxWorksheetPaneResult rowRangeResult = rowRange.FreezePanesByCount(XlsxXmlValueParser.MaxSpreadsheetRows, 0);
        XlsxWorksheetPaneResult columnRangeResult = columnRange.FreezePanesByCount(0, XlsxXmlValueParser.MaxSpreadsheetColumns);
        XlsxWorksheetPaneResult badTopLeftResult = badTopLeft.FreezePanes("XFE1");
        XlsxWorksheetPaneResult badSplitResult = badSplit.SplitPanesAtCell(-1d, double.NaN, "A1");
        XlsxWorksheetPaneResult emptySplitResult = emptySplit.SplitPanesAtCell(0d, 0d, "A1");

        Assert.That(nullViewsResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullViewsResult, "worksheetPane.sheetViewsNull"), Is.True);
        Assert.That(emptyFreezeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(emptyFreezeResult, "worksheetPane.freezeEmpty"), Is.True);
        Assert.That(rowRangeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(rowRangeResult, "worksheetPane.frozenRowsRange"), Is.True);
        Assert.That(columnRangeResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(columnRangeResult, "worksheetPane.frozenColumnsRange"), Is.True);
        Assert.That(badTopLeftResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(badTopLeftResult, "worksheetPane.topLeftCellInvalid"), Is.True);
        Assert.That(HasDiagnostic(badTopLeftResult, "a1.cellReference.columnRange"), Is.True);
        Assert.That(badSplitResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(badSplitResult, "worksheetPane.splitRange"), Is.True);
        Assert.That(emptySplitResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(emptySplitResult, "worksheetPane.splitEmpty"), Is.True);
    }

    static bool HasDiagnostic(XlsxWorksheetPaneResult result, string code)
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
