using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetRowMetadataStoreTests
{
    [Test]
    public void SetGetCreateAndRemoveRows_TrackMetadataAndBounds()
    {
        XlsxWorksheetRowMetadataStore store = new XlsxWorksheetRowMetadataStore();
        XlsxWorksheetRow row10 = CreateRow(0, "10:12", 20d, 3, true, true);
        XlsxWorksheetRow row2 = CreateRow(0, "2:4", 14d, 1, false, false);

        XlsxWorksheetRowStoreResult set10 = store.SetRow(10, row10);
        XlsxWorksheetRowStoreResult set2 = store.SetRow(2, row2);
        XlsxWorksheetRowStoreResult created = store.GetOrCreateRow(5);
        XlsxWorksheetRowStoreResult removed = store.RemoveRow(2);

        Assert.That(set10.IsValid, Is.True);
        Assert.That(set2.IsValid, Is.True);
        Assert.That(created.IsValid, Is.True);
        Assert.That(removed.IsValid, Is.True);
        Assert.That(removed.Removed, Is.True);
        Assert.That(store.RowCount, Is.EqualTo(2));
        Assert.That(store.HasRows, Is.True);
        Assert.That(store.MinRow, Is.EqualTo(5));
        Assert.That(store.MaxRow, Is.EqualTo(10));
        Assert.That(row10.RowIndex, Is.EqualTo(10));
        Assert.That(row2.RowIndex, Is.EqualTo(2));
        Assert.That(created.Row.RowIndex, Is.EqualTo(5));
        Assert.That(store.GetRow(10), Is.SameAs(row10));
        Assert.That(store.GetRow(2), Is.Null);
        Assert.That(store.ContainsRow(5), Is.True);
    }

    [Test]
    public void LoadWorksheetPart_IndexesExistingRowsAndAssignsMissingIndexesBySequence()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        part.Rows.Add(CreateRow(3, "3:3", 15d, 2, false, true));
        part.Rows.Add(CreateRow(0, "4:4", 16d, 0, true, true));
        XlsxWorksheetRowMetadataStore store = new XlsxWorksheetRowMetadataStore();

        XlsxWorksheetRowStoreResult result = store.LoadWorksheetPart(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(store.RowCount, Is.EqualTo(2));
        Assert.That(store.GetRow(3), Is.SameAs(part.Rows[0]));
        Assert.That(store.GetRow(2), Is.SameAs(part.Rows[1]));
        Assert.That(part.Rows[1].RowIndex, Is.EqualTo(2));
        Assert.That(store.MinRow, Is.EqualTo(2));
        Assert.That(store.MaxRow, Is.EqualTo(3));
    }

    [Test]
    public void ApplyToWorksheetPart_WritesRowsInAscendingOrder()
    {
        XlsxWorksheetRowMetadataStore store = new XlsxWorksheetRowMetadataStore();
        store.SetRow(10, CreateRow(0, "", 0d, 0, false, false));
        store.SetRow(2, CreateRow(0, "", 0d, 0, false, false));
        store.SetRow(5, CreateRow(0, "", 0d, 0, false, false));
        XlsxWorksheetPart part = new XlsxWorksheetPart();

        XlsxWorksheetRowStoreResult result = store.ApplyToWorksheetPart(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(part.Rows.Count, Is.EqualTo(3));
        Assert.That(part.Rows[0].RowIndex, Is.EqualTo(2));
        Assert.That(part.Rows[1].RowIndex, Is.EqualTo(5));
        Assert.That(part.Rows[2].RowIndex, Is.EqualTo(10));
    }

    [Test]
    public void WorksheetReaderWriter_RoundTripsRowMetadata()
    {
        string xml = "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:x=\"urn:x\">" +
            "<sheetData><row r=\"7\" spans=\"1:3\" ht=\"18.5\" s=\"4\" hidden=\"1\" customHeight=\"1\" x:row=\"r\"><c r=\"A7\"><v>1</v></c></row></sheetData>" +
            "</worksheet>";
        XlsxWorksheetPart part = new XlsxWorksheetPartReader().ReadString(xml);
        XlsxWorksheetRowMetadataStore store = new XlsxWorksheetRowMetadataStore();
        store.LoadWorksheetPart(part);

        XlsxWorksheetPart writtenPart = new XlsxWorksheetPart();
        store.ApplyToWorksheetPart(writtenPart);
        XlsxWorksheetPartWriteResult writeResult = new XlsxWorksheetPartWriter().WriteString(writtenPart);
        XlsxWorksheetPart roundTrip = new XlsxWorksheetPartReader().ReadString(writeResult.Xml);

        Assert.That(part.IsValid, Is.True);
        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(roundTrip.IsValid, Is.True);
        Assert.That(store.GetRow(7)!.Spans, Is.EqualTo("1:3"));
        Assert.That(store.GetRow(7)!.Height, Is.EqualTo(18.5d).Within(0.0000001d));
        Assert.That(store.GetRow(7)!.Style, Is.EqualTo(4));
        Assert.That(store.GetRow(7)!.Hidden, Is.True);
        Assert.That(store.GetRow(7)!.CustomHeight, Is.True);
        Assert.That(store.GetRow(7)!.UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Rows.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Rows[0].RowIndex, Is.EqualTo(7));
        Assert.That(roundTrip.Rows[0].Spans, Is.EqualTo("1:3"));
        Assert.That(roundTrip.Rows[0].Height, Is.EqualTo(18.5d).Within(0.0000001d));
        Assert.That(roundTrip.Rows[0].Style, Is.EqualTo(4));
        Assert.That(roundTrip.Rows[0].Hidden, Is.True);
        Assert.That(roundTrip.Rows[0].CustomHeight, Is.True);
        Assert.That(roundTrip.Rows[0].UnknownAttributes.Count, Is.EqualTo(1));
        Assert.That(roundTrip.Rows[0].Cells.Count, Is.EqualTo(1));
    }

    [Test]
    public void Store_ReportsInvalidRowsAndInputs()
    {
        XlsxWorksheetRowMetadataStore store = new XlsxWorksheetRowMetadataStore();
        XlsxWorksheetPart nullRows = new XlsxWorksheetPart();
        nullRows.Rows = null!;
        XlsxWorksheetPart nullRow = new XlsxWorksheetPart();
        nullRow.Rows.Add(null!);

        XlsxWorksheetRowStoreResult badIndex = store.SetRow(0, CreateRow(0, "", 0d, 0, false, false));
        XlsxWorksheetRowStoreResult? nullRowSet = store.SetRow(1, null!);
        XlsxWorksheetRowStoreResult? nullPartLoad = store.LoadWorksheetPart(null!);
        XlsxWorksheetRowStoreResult nullRowsLoad = store.LoadWorksheetPart(nullRows);
        XlsxWorksheetRowStoreResult nullRowLoad = store.LoadWorksheetPart(nullRow);
        XlsxWorksheetRowStoreResult? nullPartApply = store.ApplyToWorksheetPart(null!);

        Assert.That(badIndex.IsValid, Is.False);
        Assert.That(nullRowSet.IsValid, Is.False);
        Assert.That(nullPartLoad.IsValid, Is.False);
        Assert.That(nullRowsLoad.IsValid, Is.False);
        Assert.That(nullRowLoad.IsValid, Is.False);
        Assert.That(nullPartApply.IsValid, Is.False);
        Assert.That(HasDiagnostic(badIndex, "rowMetadataStore.rowRange"), Is.True);
        Assert.That(HasDiagnostic(nullRowSet, "rowMetadataStore.rowNull"), Is.True);
        Assert.That(HasDiagnostic(nullPartLoad, "rowMetadataStore.partNull"), Is.True);
        Assert.That(HasDiagnostic(nullRowsLoad, "rowMetadataStore.rowsNull"), Is.True);
        Assert.That(HasDiagnostic(nullRowLoad, "rowMetadataStore.rowNull"), Is.True);
        Assert.That(HasDiagnostic(nullPartApply, "rowMetadataStore.partNull"), Is.True);
    }

    static XlsxWorksheetRow CreateRow(uint rowIndex, string spans, double height, uint style, bool hidden, bool customHeight)
    {
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = rowIndex;
        row.Spans = spans;
        if (height > 0)
        {
            row.Height = height;
            row.HasHeight = true;
        }

        if (style > 0)
        {
            row.Style = style;
            row.HasStyle = true;
        }

        row.Hidden = hidden;
        row.HasHidden = true;
        row.CustomHeight = customHeight;
        row.HasCustomHeight = true;
        return row;
    }

    static bool HasDiagnostic(XlsxWorksheetRowStoreResult result, string code)
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
