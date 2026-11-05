using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class DenseCellStoreTests
{
    [Test]
    public void InitializeSetGetAndGetOrCreate_UseDenseCoordinatesAndBounds()
    {
        XlsxDenseCellStore store = new XlsxDenseCellStore();
        XlsxWorksheetCell first = CreateCell("first");
        XlsxWorksheetCell second = CreateCell("second");

        XlsxCellStoreResult init = store.Initialize(10, 5);
        XlsxCellStoreResult firstResult = store.SetCell(1, 1, first);
        XlsxCellStoreResult secondResult = store.SetCellByReference("E10", second);
        XlsxCellStoreResult created = store.GetOrCreateCell(2, 3);

        Assert.That(init.IsValid, Is.True);
        Assert.That(firstResult.IsValid, Is.True);
        Assert.That(secondResult.IsValid, Is.True);
        Assert.That(created.IsValid, Is.True);
        Assert.That(store.IsInitialized, Is.True);
        Assert.That(store.RowCount, Is.EqualTo(10));
        Assert.That(store.ColumnCount, Is.EqualTo(5));
        Assert.That(store.CellCount, Is.EqualTo(3));
        Assert.That(store.MinRow, Is.EqualTo(1));
        Assert.That(store.MaxRow, Is.EqualTo(10));
        Assert.That(store.MinColumn, Is.EqualTo(1));
        Assert.That(store.MaxColumn, Is.EqualTo(5));
        Assert.That(store.GetCell(1, 1), Is.SameAs(first));
        Assert.That(store.GetCellByReference("E10"), Is.SameAs(second));
        Assert.That(store.GetCellByReference("C2"), Is.SameAs(created.Cell));
        Assert.That(created.Cell.Reference, Is.EqualTo("C2"));
        Assert.That(store.ContainsCell(2, 3), Is.True);
        Assert.That(store.ContainsCellByReference("E10"), Is.True);
    }

    [Test]
    public void Resize_PreservesCellsAndRemoveRecalculatesBounds()
    {
        XlsxDenseCellStore store = new XlsxDenseCellStore();
        store.Initialize(2, 2);
        store.SetCell(1, 1, CreateCell("a"));
        store.SetCell(2, 2, CreateCell("b"));

        XlsxCellStoreResult resize = store.Resize(10, 5);
        XlsxCellStoreResult removed = store.RemoveCellByReference("A1");
        XlsxCellStoreResult missing = store.RemoveCell(1, 1);

        Assert.That(resize.IsValid, Is.True);
        Assert.That(removed.IsValid, Is.True);
        Assert.That(removed.Removed, Is.True);
        Assert.That(missing.IsValid, Is.True);
        Assert.That(missing.Removed, Is.False);
        Assert.That(store.RowCount, Is.EqualTo(10));
        Assert.That(store.ColumnCount, Is.EqualTo(5));
        Assert.That(store.CellCount, Is.EqualTo(1));
        Assert.That(store.GetCell(1, 1), Is.Null);
        Assert.That(store.GetCell(2, 2)!.Value, Is.EqualTo("b"));
        Assert.That(store.MinRow, Is.EqualTo(2));
        Assert.That(store.MaxRow, Is.EqualTo(2));
        Assert.That(store.MinColumn, Is.EqualTo(2));
        Assert.That(store.MaxColumn, Is.EqualTo(2));
    }

    [Test]
    public void LoadWorksheetPart_ExpandsToReferencedCellsAndReportsInvalidCells()
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        row.Cells.Add(CreateReferencedCell("A1", "a"));
        row.Cells.Add(CreateReferencedCell("C3", "c"));
        row.Cells.Add(CreateReferencedCell("XFE1", "bad"));
        row.Cells.Add(CreateReferencedCell("", "missing"));
        part.Rows.Add(row);
        XlsxDenseCellStore store = new XlsxDenseCellStore();

        XlsxCellStoreResult result = store.LoadWorksheetPart(part);

        Assert.That(result.IsValid, Is.False);
        Assert.That(store.IsInitialized, Is.True);
        Assert.That(store.RowCount, Is.EqualTo(3));
        Assert.That(store.ColumnCount, Is.EqualTo(3));
        Assert.That(store.CellCount, Is.EqualTo(2));
        Assert.That(store.GetCell(1, 1)!.Value, Is.EqualTo("a"));
        Assert.That(store.GetCellByReference("C3")!.Value, Is.EqualTo("c"));
        Assert.That(store.MinRow, Is.EqualTo(1));
        Assert.That(store.MaxRow, Is.EqualTo(3));
        Assert.That(store.MinColumn, Is.EqualTo(1));
        Assert.That(store.MaxColumn, Is.EqualTo(3));
        Assert.That(HasDiagnostic(result, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(result, "denseCellStore.referenceMissing"), Is.True);
    }

    [Test]
    public void ClearCells_RemovesCellsButKeepsDimensions()
    {
        XlsxDenseCellStore store = new XlsxDenseCellStore();
        store.Initialize(4, 4);
        store.SetCell(2, 2, CreateCell("x"));

        store.ClearCells();

        Assert.That(store.IsInitialized, Is.True);
        Assert.That(store.RowCount, Is.EqualTo(4));
        Assert.That(store.ColumnCount, Is.EqualTo(4));
        Assert.That(store.CellCount, Is.EqualTo(0));
        Assert.That(store.HasCells, Is.False);
        Assert.That(store.GetCell(2, 2), Is.Null);
    }

    [Test]
    public void Mutations_AutoGrowAndReportInvalidCoordinatesNullCellsAndAllocationLimits()
    {
        XlsxDenseCellStore store = new XlsxDenseCellStore();
        XlsxDenseCellStore limited = new XlsxDenseCellStore();
        limited.MaxCellCount = 4;

        XlsxCellStoreResult autoInitialize = store.SetCell(1, 1, CreateCell("first"));
        XlsxCellStoreResult badSize = store.Initialize(0, 0);
        XlsxCellStoreResult tooLarge = limited.Initialize(3, 2);
        store.Initialize(2, 2);
        XlsxCellStoreResult autoGrow = store.SetCell(3, 1, CreateCell("grown"));
        XlsxCellStoreResult badCoordinate = store.SetCell(1048577, 1, CreateCell("bad"));
        XlsxCellStoreResult badReference = store.SetCellByReference("XFE1", CreateCell("bad"));
        XlsxCellStoreResult nullCell = store.SetCell(1, 1, null);

        Assert.That(autoInitialize.IsValid, Is.True);
        Assert.That(badSize.IsValid, Is.False);
        Assert.That(tooLarge.IsValid, Is.False);
        Assert.That(autoGrow.IsValid, Is.True);
        Assert.That(badCoordinate.IsValid, Is.False);
        Assert.That(badReference.IsValid, Is.False);
        Assert.That(nullCell.IsValid, Is.False);
        Assert.That(store.RowCount, Is.EqualTo(3));
        Assert.That(store.GetCell(3, 1)!.Value, Is.EqualTo("grown"));
        Assert.That(HasDiagnostic(badSize, "denseCellStore.rowCountRange"), Is.True);
        Assert.That(HasDiagnostic(badSize, "denseCellStore.columnCountRange"), Is.True);
        Assert.That(HasDiagnostic(tooLarge, "denseCellStore.sizeTooLarge"), Is.True);
        Assert.That(HasDiagnostic(badCoordinate, "denseCellStore.rowRange"), Is.True);
        Assert.That(HasDiagnostic(badReference, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(nullCell, "denseCellStore.cellNull"), Is.True);
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
