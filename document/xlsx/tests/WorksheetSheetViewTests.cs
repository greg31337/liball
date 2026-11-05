using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetSheetViewTests
{
    [Test]
    public void ReadString_ParsesSheetViewPaneSelectionsAndUnknowns()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\">" +
            "<sheetViews><sheetView workbookViewId=\"0\" view=\"pageLayout\" showGridLines=\"0\" showRowColHeaders=\"0\" rightToLeft=\"1\" zoomScale=\"120\" x:view=\"v\">" +
            "<pane xSplit=\"1\" ySplit=\"2\" topLeftCell=\"C3\" activePane=\"bottomRight\" state=\"frozen\" x:pane=\"p\"><x:futurePane /></pane>" +
            "<selection pane=\"topRight\" activeCell=\"C1\" activeCellId=\"1\" sqref=\"C1:D3\" x:selection=\"s\"><x:futureSelection /></selection>" +
            "<selection pane=\"bottomRight\" activeCell=\"C3\" sqref=\"C3\" />" +
            "<x:futureView />" +
            "</sheetView></sheetViews><sheetData /></worksheet>";

        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(part.SheetViews.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].View, Is.EqualTo("pageLayout"));
        Assert.That(part.SheetViews[0].ShowGridLines, Is.False);
        Assert.That(part.SheetViews[0].ShowRowColHeaders, Is.False);
        Assert.That(part.SheetViews[0].RightToLeft, Is.True);
        Assert.That(part.SheetViews[0].ZoomScale, Is.EqualTo(120));
        Assert.That(part.SheetViews[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].HasPane, Is.True);
        Assert.That(part.SheetViews[0].Pane.XSplit, Is.EqualTo(1d).Within(0.0000001d));
        Assert.That(part.SheetViews[0].Pane.YSplit, Is.EqualTo(2d).Within(0.0000001d));
        Assert.That(part.SheetViews[0].Pane.TopLeftCell, Is.EqualTo("C3"));
        Assert.That(part.SheetViews[0].Pane.ActivePane, Is.EqualTo("bottomRight"));
        Assert.That(part.SheetViews[0].Pane.State, Is.EqualTo("frozen"));
        Assert.That(part.SheetViews[0].Pane.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].Pane.UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].Selections.Count, Is.EqualTo(2));
        Assert.That(part.SheetViews[0].Selections[0].Pane, Is.EqualTo("topRight"));
        Assert.That(part.SheetViews[0].Selections[0].ActiveCell, Is.EqualTo("C1"));
        Assert.That(part.SheetViews[0].Selections[0].ActiveCellId, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].Selections[0].Sqref, Is.EqualTo("C1:D3"));
        Assert.That(part.SheetViews[0].Selections[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].Selections[0].UnknownXml.Count, Is.EqualTo(1));
        Assert.That(part.SheetViews[0].Selections[1].Pane, Is.EqualTo("bottomRight"));
    }

    [Test]
    public void WriteString_WritesConstructedPaneSelectionsAndRoundTrips()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetView view = new XlsxWorksheetView();
        view.WorkbookViewId = 0;
        view.HasWorkbookViewId = true;
        view.View = "pageBreakPreview";
        view.ShowGridLines = false;
        view.HasShowGridLines = true;
        view.ShowRowColHeaders = true;
        view.HasShowRowColHeaders = true;
        view.RightToLeft = true;
        view.HasRightToLeft = true;
        view.ZoomScale = 140;
        view.HasZoomScale = true;
        view.Pane.XSplit = 3;
        view.Pane.HasXSplit = true;
        view.Pane.YSplit = 1;
        view.Pane.HasYSplit = true;
        view.Pane.TopLeftCell = "D2";
        view.Pane.ActivePane = "bottomRight";
        view.Pane.State = "split";
        view.HasPane = true;
        XlsxWorksheetSelection selection = new XlsxWorksheetSelection();
        selection.Pane = "bottomRight";
        selection.ActiveCell = "D2";
        selection.ActiveCellId = 0;
        selection.HasActiveCellId = true;
        selection.Sqref = "D2:E4 G7";
        view.Selections.Add(selection);
        part.SheetViews.Add(view);

        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(part);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.Xml, Does.Contain("<pane xSplit=\"3\" ySplit=\"1\" topLeftCell=\"D2\" activePane=\"bottomRight\" state=\"split\" />"));
        Assert.That(writeResult.Xml, Does.Contain("<selection pane=\"bottomRight\" activeCell=\"D2\" activeCellId=\"0\" sqref=\"D2:E4 G7\" />"));
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(roundTrip.SheetViews[0].View, Is.EqualTo("pageBreakPreview"));
        Assert.That(roundTrip.SheetViews[0].Pane.TopLeftCell, Is.EqualTo("D2"));
        Assert.That(roundTrip.SheetViews[0].Selections[0].Sqref, Is.EqualTo("D2:E4 G7"));
    }

    [Test]
    public void WriteBytes_RejectsInvalidSheetViewDefinitions()
    {
        XlsxWorksheetPart nullViews = new XlsxWorksheetPart();
        XlsxWorksheetPart nullView = new XlsxWorksheetPart();
        XlsxWorksheetPart badView = new XlsxWorksheetPart();
        XlsxWorksheetPart badPane = new XlsxWorksheetPart();
        XlsxWorksheetPart badSelection = new XlsxWorksheetPart();

        nullViews.SheetViews = null!;
        nullView.SheetViews.Add(null!);
        XlsxWorksheetView badViewItem = new XlsxWorksheetView();
        badViewItem.View = "custom";
        badViewItem.ZoomScale = 401;
        badViewItem.HasZoomScale = true;
        badView.SheetViews.Add(badViewItem);
        XlsxWorksheetView badPaneView = new XlsxWorksheetView();
        badPaneView.HasPane = true;
        badPaneView.Pane.XSplit = -1;
        badPaneView.Pane.HasXSplit = true;
        badPaneView.Pane.TopLeftCell = "XFE1";
        badPaneView.Pane.ActivePane = "middle";
        badPaneView.Pane.State = "unknown";
        badPane.SheetViews.Add(badPaneView);
        XlsxWorksheetView badSelectionView = new XlsxWorksheetView();
        XlsxWorksheetSelection selection = new XlsxWorksheetSelection();
        selection.Pane = "middle";
        selection.ActiveCell = "XFE1";
        selection.Sqref = "A1 XFE1";
        badSelectionView.Selections.Add(selection);
        badSelection.SheetViews.Add(badSelectionView);

        XlsxWorksheetPartWriter writer = new XlsxWorksheetPartWriter();
        XlsxWorksheetPartWriteResult nullViewsResult = writer.WriteBytes(nullViews);
        XlsxWorksheetPartWriteResult nullViewResult = writer.WriteBytes(nullView);
        XlsxWorksheetPartWriteResult badViewResult = writer.WriteBytes(badView);
        XlsxWorksheetPartWriteResult badPaneResult = writer.WriteBytes(badPane);
        XlsxWorksheetPartWriteResult badSelectionResult = writer.WriteBytes(badSelection);

        Assert.That(nullViewsResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullViewsResult, "worksheet.sheetViewsNull"), Is.True);
        Assert.That(nullViewResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullViewResult, "worksheet.sheetViewNull"), Is.True);
        Assert.That(badViewResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(badViewResult, "worksheet.sheetViewModeUnsupported"), Is.True);
        Assert.That(HasDiagnostic(badViewResult, "worksheet.sheetViewZoomRange"), Is.True);
        Assert.That(badPaneResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(badPaneResult, "worksheet.paneSplitRange"), Is.True);
        Assert.That(HasDiagnostic(badPaneResult, "worksheet.paneTopLeftCellInvalid"), Is.True);
        Assert.That(HasDiagnostic(badPaneResult, "worksheet.paneActivePaneInvalid"), Is.True);
        Assert.That(HasDiagnostic(badPaneResult, "worksheet.paneStateInvalid"), Is.True);
        Assert.That(badSelectionResult.IsValid, Is.False);
        Assert.That(HasDiagnostic(badSelectionResult, "worksheet.selectionPaneInvalid"), Is.True);
        Assert.That(HasDiagnostic(badSelectionResult, "worksheet.selectionActiveCellInvalid"), Is.True);
        Assert.That(HasDiagnostic(badSelectionResult, "worksheet.selectionSqrefInvalid"), Is.True);
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
