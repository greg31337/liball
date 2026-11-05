using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class WorksheetStorageSelectorTests
{
    [Test]
    public void SelectWorksheetPart_ChoosesDenseForCompactFilledWorksheets()
    {
        XlsxWorksheetPart part = CreateWorksheet("A1", "B1", "A2", "B2");
        XlsxWorksheetStorageSelector selector = new XlsxWorksheetStorageSelector();

        XlsxWorksheetStorageSelectionResult result = selector.SelectWorksheetPart(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Kind, Is.EqualTo(XlsxWorksheetStorageKind.Dense));
        Assert.That(result.HasStore, Is.True);
        Assert.That(result.IsStreaming, Is.False);
        Assert.That(result.Metrics.CellCount, Is.EqualTo(4));
        Assert.That(result.Metrics.DenseCellCount, Is.EqualTo(4));
        Assert.That(result.Metrics.FillRatio, Is.EqualTo(1d).Within(0.0000001d));
        Assert.That(result.Store!.GetCellByReference("B2")!.Value, Is.EqualTo("B2"));
    }

    [Test]
    public void SelectWorksheetPart_ChoosesSparseForSparseLargeWorksheetExtents()
    {
        XlsxWorksheetPart part = CreateWorksheet("A1", "XFD1048576");
        XlsxWorksheetStorageSelector selector = new XlsxWorksheetStorageSelector();

        XlsxWorksheetStorageSelectionResult result = selector.SelectWorksheetPart(part);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Kind, Is.EqualTo(XlsxWorksheetStorageKind.Sparse));
        Assert.That(result.HasStore, Is.True);
        Assert.That(result.Metrics.CellCount, Is.EqualTo(2));
        Assert.That(result.Metrics.MaxRow, Is.EqualTo(1048576));
        Assert.That(result.Metrics.MaxColumn, Is.EqualTo(16384));
        Assert.That(result.Store!.GetCellByReference("XFD1048576")!.Value, Is.EqualTo("XFD1048576"));
    }

    [Test]
    public void SelectWorksheetPart_ChoosesStreamingWhenCellCountCrossesThreshold()
    {
        XlsxWorksheetPart part = CreateWorksheet("A1", "A2", "A3");
        XlsxWorksheetStorageOptions options = new XlsxWorksheetStorageOptions();
        options.StreamingCellCountThreshold = 3;
        XlsxWorksheetStorageSelector selector = new XlsxWorksheetStorageSelector();

        XlsxWorksheetStorageSelectionResult result = selector.SelectWorksheetPartWithOptions(part, options);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Kind, Is.EqualTo(XlsxWorksheetStorageKind.Streaming));
        Assert.That(result.IsStreaming, Is.True);
        Assert.That(result.HasStore, Is.False);
        Assert.That(result.Metrics.CellCount, Is.EqualTo(3));
    }

    [Test]
    public void SelectWorksheetPart_HonorsForceSparseAndForceDense()
    {
        XlsxWorksheetPart compact = CreateWorksheet("A1", "B1", "A2", "B2");
        XlsxWorksheetStorageOptions sparseOptions = new XlsxWorksheetStorageOptions();
        sparseOptions.ForceSparse = true;
        XlsxWorksheetStorageOptions denseOptions = new XlsxWorksheetStorageOptions();
        denseOptions.ForceDense = true;
        denseOptions.MaxDenseCellCount = 4;
        XlsxWorksheetStorageSelector selector = new XlsxWorksheetStorageSelector();

        XlsxWorksheetStorageSelectionResult sparse = selector.SelectWorksheetPartWithOptions(compact, sparseOptions);
        XlsxWorksheetStorageSelectionResult dense = selector.SelectWorksheetPartWithOptions(compact, denseOptions);

        Assert.That(sparse.IsValid, Is.True);
        Assert.That(sparse.Kind, Is.EqualTo(XlsxWorksheetStorageKind.Sparse));
        Assert.That(sparse.HasStore, Is.True);
        Assert.That(dense.IsValid, Is.True);
        Assert.That(dense.Kind, Is.EqualTo(XlsxWorksheetStorageKind.Dense));
        Assert.That(dense.HasStore, Is.True);
    }

    [Test]
    public void SelectWorksheetPart_ReportsOptionAndForcedDenseAllocationDiagnostics()
    {
        XlsxWorksheetPart part = CreateWorksheet("A1", "C3");
        XlsxWorksheetStorageOptions denseOptions = new XlsxWorksheetStorageOptions();
        denseOptions.ForceDense = true;
        denseOptions.MaxDenseCellCount = 4;
        XlsxWorksheetStorageOptions conflictOptions = new XlsxWorksheetStorageOptions();
        conflictOptions.ForceDense = true;
        conflictOptions.ForceSparse = true;
        XlsxWorksheetStorageSelector selector = new XlsxWorksheetStorageSelector();

        XlsxWorksheetStorageSelectionResult dense = selector.SelectWorksheetPartWithOptions(part, denseOptions);
        XlsxWorksheetStorageSelectionResult conflict = selector.SelectWorksheetPartWithOptions(part, conflictOptions);
        XlsxWorksheetStorageSelectionResult? nullPart = selector.SelectWorksheetPart(null!);

        Assert.That(dense.IsValid, Is.False);
        Assert.That(dense.Kind, Is.EqualTo(XlsxWorksheetStorageKind.Dense));
        Assert.That(HasDiagnostic(dense, "denseCellStore.sizeTooLarge"), Is.True);
        Assert.That(conflict.IsValid, Is.False);
        Assert.That(HasDiagnostic(conflict, "worksheetStorage.forceConflict"), Is.True);
        Assert.That(nullPart.IsValid, Is.False);
        Assert.That(HasDiagnostic(nullPart, "worksheetStorage.partNull"), Is.True);
    }

    [Test]
    public void SelectWorksheetPart_PropagatesStoreLoadDiagnostics()
    {
        XlsxWorksheetPart part = CreateWorksheet("A1", "XFE1", "");
        XlsxWorksheetStorageOptions options = new XlsxWorksheetStorageOptions();
        options.ForceSparse = true;
        XlsxWorksheetStorageSelector selector = new XlsxWorksheetStorageSelector();

        XlsxWorksheetStorageSelectionResult result = selector.SelectWorksheetPartWithOptions(part, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Kind, Is.EqualTo(XlsxWorksheetStorageKind.Sparse));
        Assert.That(result.Store!.GetCellCount(), Is.EqualTo(1));
        Assert.That(HasDiagnostic(result, "a1.cellReference.columnRange"), Is.True);
        Assert.That(HasDiagnostic(result, "sparseCellStore.referenceMissing"), Is.True);
    }

    static XlsxWorksheetPart CreateWorksheet(string reference1, string reference2, string reference3, string reference4)
    {
        XlsxWorksheetPart part = CreateWorksheet(reference1, reference2, reference3);
        AddCell(part.Rows[0], reference4);
        return part;
    }

    static XlsxWorksheetPart CreateWorksheet(string reference1, string reference2, string reference3)
    {
        XlsxWorksheetPart part = CreateWorksheet(reference1, reference2);
        AddCell(part.Rows[0], reference3);
        return part;
    }

    static XlsxWorksheetPart CreateWorksheet(string reference1, string reference2)
    {
        XlsxWorksheetPart part = CreateWorksheet(reference1);
        AddCell(part.Rows[0], reference2);
        return part;
    }

    static XlsxWorksheetPart CreateWorksheet(string reference1)
    {
        XlsxWorksheetPart part = new XlsxWorksheetPart();
        XlsxWorksheetRow row = new XlsxWorksheetRow();
        part.Rows.Add(row);
        AddCell(row, reference1);
        return part;
    }

    static void AddCell(XlsxWorksheetRow row, string reference)
    {
        XlsxWorksheetCell cell = new XlsxWorksheetCell();
        cell.Reference = reference;
        cell.Value = reference;
        row.Cells.Add(cell);
    }

    static bool HasDiagnostic(XlsxWorksheetStorageSelectionResult result, string code)
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
