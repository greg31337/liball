using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetColumnMetadataStoreTests
{
    [Test]
    public void SetGetCreateAndRemoveColumns_TrackMetadataAndBounds()
    {
        XlsxWorksheetColumnMetadataStore store = new XlsxWorksheetColumnMetadataStore();
        XlsxWorksheetColumn wide = CreateColumn(0, 0, 20d, 3, true, true);
        XlsxWorksheetColumn narrow = CreateColumn(0, 0, 10d, 0, false, true);

        XlsxWorksheetColumnStoreResult wideResult = store.SetColumnRange(1, 3, wide);
        XlsxWorksheetColumnStoreResult narrowResult = store.SetColumn(5, narrow);
        XlsxWorksheetColumnStoreResult created = store.GetOrCreateColumn(7);
        XlsxWorksheetColumnStoreResult removed = store.RemoveColumn(2);

        Assert.That(wideResult.IsValid, Is.True);
        Assert.That(narrowResult.IsValid, Is.True);
        Assert.That(created.IsValid, Is.True);
        Assert.That(removed.IsValid, Is.True);
        Assert.That(removed.Removed, Is.True);
        Assert.That(store.ColumnCount, Is.EqualTo(4));
        Assert.That(store.HasColumns, Is.True);
        Assert.That(store.MinColumn, Is.EqualTo(1));
        Assert.That(store.MaxColumn, Is.EqualTo(7));
        Assert.That(store.GetColumn(1)!.Width, Is.EqualTo(20d).Within(0.0000001d));
        Assert.That(store.GetColumn(2), Is.Null);
        Assert.That(store.GetColumn(3)!.Style, Is.EqualTo(3));
        Assert.That(store.GetColumn(5)!.Width, Is.EqualTo(10d).Within(0.0000001d));
        Assert.That(created.Column.Min, Is.EqualTo(7));
        Assert.That(created.Column.Max, Is.EqualTo(7));
        Assert.That(store.ContainsColumn(7), Is.True);
    }

    [Test]
    public void ApplyToWorksheetPart_MergesEquivalentAdjacentColumnsAndSplitsRemovedColumns()
    {
        XlsxWorksheetColumnMetadataStore store = new XlsxWorksheetColumnMetadataStore();
        store.SetColumnRange(1, 3, CreateColumn(0, 0, 20d, 2, true, true));
        store.RemoveColumn(2);
        store.SetColumn(5, CreateColumn(0, 0, 12d, 0, false, true));
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetColumnStoreResult result = store.ApplyToWorksheetPart(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(part.Columns.Count, Is.EqualTo(3));
        Assert.That(part.Columns[0].Min, Is.EqualTo(1));
        Assert.That(part.Columns[0].Max, Is.EqualTo(1));
        Assert.That(part.Columns[1].Min, Is.EqualTo(3));
        Assert.That(part.Columns[1].Max, Is.EqualTo(3));
        Assert.That(part.Columns[2].Min, Is.EqualTo(5));
        Assert.That(part.Columns[2].Max, Is.EqualTo(5));
    }

    [Test]
    public void LoadWorksheetPart_ExpandsColumnRangesForLookup()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.Columns.Add(CreateColumn(1, 3, 20d, 2, true, true));
        part.Columns.Add(CreateColumn(6, 6, 12d, 0, false, true));
        XlsxWorksheetColumnMetadataStore store = new XlsxWorksheetColumnMetadataStore();

        XlsxWorksheetColumnStoreResult result = store.LoadWorksheetPart(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(store.ColumnCount, Is.EqualTo(4));
        Assert.That(store.GetColumn(1)!.Width, Is.EqualTo(20d).Within(0.0000001d));
        Assert.That(store.GetColumn(2)!.Hidden, Is.True);
        Assert.That(store.GetColumn(3)!.Style, Is.EqualTo(2));
        Assert.That(store.GetColumn(4), Is.Null);
        Assert.That(store.GetColumn(6)!.Width, Is.EqualTo(12d).Within(0.0000001d));
        Assert.That(store.MinColumn, Is.EqualTo(1));
        Assert.That(store.MaxColumn, Is.EqualTo(6));
    }

    [Test]
    public void WorksheetReaderWriter_RoundTripsColumnMetadata()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\">" +
            "<cols><col min=\"1\" max=\"2\" width=\"22.5\" style=\"4\" hidden=\"1\" bestFit=\"1\" customWidth=\"1\" outlineLevel=\"2\" collapsed=\"0\" x:col=\"c\" /></cols>" +
            "<sheetData />" +
            "</worksheet>";
        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(xml);
        XlsxWorksheetColumnMetadataStore store = new XlsxWorksheetColumnMetadataStore();
        store.LoadWorksheetPart(part);

        XlsxWorksheetPart writtenPart = new XlsxWorksheetPart();
        store.ApplyToWorksheetPart(writtenPart);
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(writtenPart);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(store.GetColumn(1)!.Width, Is.EqualTo(22.5d).Within(0.0000001d));
        Assert.That(store.GetColumn(2)!.Style, Is.EqualTo(4));
        Assert.That(store.GetColumn(2)!.Hidden, Is.True);
        Assert.That(store.GetColumn(2)!.BestFit, Is.True);
        Assert.That(store.GetColumn(2)!.OutlineLevel, Is.EqualTo(2));
        Assert.That(store.GetColumn(2)!.Collapsed, Is.False);
        Assert.That(store.GetColumn(2)!.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Columns.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Columns[0].Min, Is.EqualTo(1));
        Assert.That(roundTrip.Columns[0].Max, Is.EqualTo(2));
        Assert.That(roundTrip.Columns[0].Width, Is.EqualTo(22.5d).Within(0.0000001d));
        Assert.That(roundTrip.Columns[0].Style, Is.EqualTo(4));
        Assert.That(roundTrip.Columns[0].UnknownAttributes.Count, Is.EqualTo(1));
    }

    [Test]
    public void Store_ReportsInvalidColumnsAndInputs()
    {
        XlsxWorksheetColumnMetadataStore store = new XlsxWorksheetColumnMetadataStore();
        XlsxWorksheetPart nullColumns = new XlsxWorksheetPart();
        nullColumns.Columns = null!;
        XlsxWorksheetPart nullColumn = new XlsxWorksheetPart();
        nullColumn.Columns.Add(null!);

        XlsxWorksheetColumnStoreResult badIndex = store.SetColumn(0, CreateColumn(0, 0, 1d, 0, false, false));
        XlsxWorksheetColumnStoreResult badOrder = store.SetColumnRange(3, 2, CreateColumn(0, 0, 1d, 0, false, false));
        XlsxWorksheetColumnStoreResult? nullColumnSet = store.SetColumn(1, null!);
        XlsxWorksheetColumnStoreResult? nullPartLoad = store.LoadWorksheetPart(null!);
        XlsxWorksheetColumnStoreResult nullColumnsLoad = store.LoadWorksheetPart(nullColumns);
        XlsxWorksheetColumnStoreResult nullColumnLoad = store.LoadWorksheetPart(nullColumn);
        XlsxWorksheetColumnStoreResult? nullPartApply = store.ApplyToWorksheetPart(null!);

        Assert.That(badIndex.IsValid, Is.False);
        Assert.That(badOrder.IsValid, Is.False);
        Assert.That(nullColumnSet.IsValid, Is.False);
        Assert.That(nullPartLoad.IsValid, Is.False);
        Assert.That(nullColumnsLoad.IsValid, Is.False);
        Assert.That(nullColumnLoad.IsValid, Is.False);
        Assert.That(nullPartApply.IsValid, Is.False);
        Assert.That(HasDiagnostic(badIndex, "columnMetadataStore.columnRange"), Is.True);
        Assert.That(HasDiagnostic(badOrder, "columnMetadataStore.columnRangeOrder"), Is.True);
        Assert.That(HasDiagnostic(nullColumnSet, "columnMetadataStore.columnNull"), Is.True);
        Assert.That(HasDiagnostic(nullPartLoad, "columnMetadataStore.partNull"), Is.True);
        Assert.That(HasDiagnostic(nullColumnsLoad, "columnMetadataStore.columnsNull"), Is.True);
        Assert.That(HasDiagnostic(nullColumnLoad, "columnMetadataStore.columnNull"), Is.True);
        Assert.That(HasDiagnostic(nullPartApply, "columnMetadataStore.partNull"), Is.True);
    }

    static XlsxWorksheetColumn CreateColumn(uint min, uint max, double width, uint style, bool hidden, bool customWidth)
    {
        XlsxWorksheetColumn column = new XlsxWorksheetColumn();
        column.Min = min;
        column.Max = max;
        if (width > 0)
        {
            column.Width = width;
            column.HasWidth = true;
        }

        if (style > 0)
        {
            column.Style = style;
            column.HasStyle = true;
        }

        column.Hidden = hidden;
        column.HasHidden = true;
        column.CustomWidth = customWidth;
        column.HasCustomWidth = true;
        return column;
    }

    static bool HasDiagnostic(XlsxWorksheetColumnStoreResult result, string code)
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
