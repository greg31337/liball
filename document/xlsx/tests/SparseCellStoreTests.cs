using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class SparseCellStoreTests
{
    [Test]
    public void SetCell_GetCellAndGetOrCreateCell_UseSparseCoordinatesAndBounds()
    {
        XlsxSparseCellStore store = new XlsxSparseCellStore();
        XlsxWorksheetCell first = CreateCell("first");
        XlsxWorksheetCell second = CreateCell("second");

        XlsxCellStoreResult firstResult = store.SetCell(1, 1, first);
        XlsxCellStoreResult secondResult = store.SetCell(10, 5, second);
        XlsxCellStoreResult created = store.GetOrCreateCell(2, 3);

        Assert.That(firstResult.IsValid, Is.True);
        Assert.That(secondResult.IsValid, Is.True);
        Assert.That(created.IsValid, Is.True);
        Assert.That(store.CellCount, Is.EqualTo(3));
        Assert.That(store.HasCells, Is.True);
        Assert.That(store.MinRow, Is.EqualTo(1));
        Assert.That(store.MaxRow, Is.EqualTo(10));
        Assert.That(store.MinColumn, Is.EqualTo(1));
        Assert.That(store.MaxColumn, Is.EqualTo(5));
        Assert.That(store.GetCell(1, 1), Is.SameAs(first));
        Assert.That(store.GetCell(10, 5), Is.SameAs(second));
        Assert.That(store.GetCellByReference("C2"), Is.SameAs(created.Cell));
        Assert.That(created.Cell.Reference, Is.EqualTo("C2"));
        Assert.That(store.ContainsCell(2, 3), Is.True);
        Assert.That(store.ContainsCellByReference("E10"), Is.True);
    }

    [Test]
    public void SetCell_OverwritesExistingCellWithoutIncreasingCount()
    {
        XlsxSparseCellStore store = new XlsxSparseCellStore();
        XlsxWorksheetCell first = CreateCell("first");
        XlsxWorksheetCell second = CreateCell("second");

        store.SetCell(1, 1, first);
        XlsxCellStoreResult result = store.SetCell(1, 1, second);

        Assert.That(result.IsValid, Is.True);
        Assert.That(store.CellCount, Is.EqualTo(1));
        Assert.That(store.GetCell(1, 1), Is.SameAs(second));
        Assert.That(store.GetCellByReference("A1")!.Value, Is.EqualTo("second"));
    }

    [Test]
    public void RemoveCell_RemovesCellsAndRecalculatesBounds()
    {
        XlsxSparseCellStore store = new XlsxSparseCellStore();
        store.SetCell(1, 1, CreateCell("a"));
        store.SetCell(10, 5, CreateCell("b"));

        XlsxCellStoreResult removed = store.RemoveCellByReference("A1");
        XlsxCellStoreResult missing = store.RemoveCell(1, 1);

        Assert.That(removed.IsValid, Is.True);
        Assert.That(removed.Removed, Is.True);
        Assert.That(removed.HasCell, Is.True);
        Assert.That(missing.IsValid, Is.True);
        Assert.That(missing.Removed, Is.False);
        Assert.That(store.CellCount, Is.EqualTo(1));
        Assert.That(store.MinRow, Is.EqualTo(10));
        Assert.That(store.MaxRow, Is.EqualTo(10));
        Assert.That(store.MinColumn, Is.EqualTo(5));
        Assert.That(store.MaxColumn, Is.EqualTo(5));
        Assert.That(store.GetCell(1, 1), Is.Null);
        Assert.That(store.GetCell(10, 5), Is.Not.Null);
    }

    [Test]
    public void LoadWorksheetPart_LoadsReferencedCellsAndReportsInvalidCells()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.RowIndex = 1;
        row.Cells.Add(CreateReferencedCell("A1", "a"));
        row.Cells.Add(CreateReferencedCell("C3", "c"));
        row.Cells.Add(CreateReferencedCell("XFE1", "bad"));
        row.Cells.Add(CreateReferencedCell("", "missing"));
        part.Rows.Add(row);
        XlsxSparseCellStore store = new XlsxSparseCellStore();

        XlsxCellStoreResult result = store.LoadWorksheetPart(part);

        Assert.That(result.IsValid, Is.False);
        Assert.That(store.CellCount, Is.EqualTo(2));
        Assert.That(store.GetCell(1, 1)!.Value, Is.EqualTo("a"));
        Assert.That(store.GetCellByReference("C3")!.Value, Is.EqualTo("c"));
        Assert.That(store.MinRow, Is.EqualTo(1));
        Assert.That(store.MaxRow, Is.EqualTo(3));
        Assert.That(store.MinColumn, Is.EqualTo(1));
        Assert.That(store.MaxColumn, Is.EqualTo(3));
        Assert.That(HasDiagnostic(result, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(result, "sparseCellStore.referenceMissing"), Is.True);
    }

    [Test]
    public void Mutations_ReportInvalidCoordinatesReferencesAndNullCells()
    {
        XlsxSparseCellStore store = new XlsxSparseCellStore();

        XlsxCellStoreResult badCoordinate = store.SetCell(0, 0, CreateCell("bad"));
        XlsxCellStoreResult badReference = store.SetCellByReference("XFE1", CreateCell("bad"));
        XlsxCellStoreResult nullCell = store.SetCell(1, 1, null);
        XlsxCellStoreResult badRemove = store.RemoveCell(1048577, 1);

        Assert.That(badCoordinate.IsValid, Is.False);
        Assert.That(badReference.IsValid, Is.False);
        Assert.That(nullCell.IsValid, Is.False);
        Assert.That(badRemove.IsValid, Is.False);
        Assert.That(store.CellCount, Is.EqualTo(0));
        Assert.That(HasDiagnostic(badCoordinate, "sparseCellStore.rowRange"), Is.True);
        Assert.That(HasDiagnostic(badCoordinate, "sparseCellStore.columnRange"), Is.True);
        Assert.That(HasDiagnostic(badReference, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(nullCell, "sparseCellStore.cellNull"), Is.True);
        Assert.That(HasDiagnostic(badRemove, "sparseCellStore.rowRange"), Is.True);
    }

    static XlsxWorksheetCell CreateCell(string value)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Value = value;
        return cell;
    }

    static XlsxWorksheetCell CreateReferencedCell(string reference, string value)
    {
        XlsxWorksheetCell cell = CreateCell(value);
        cell.Reference = reference;
        return cell;
    }

    static bool HasDiagnostic(XlsxCellStoreResult result, string code)
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
