namespace odf.Tests;

public class FormulaReferenceRebaserTests
{
    [Test]
    public void RebasesRelativeCoordinatesForFormulaCopies()
    {
        string result = FormulaReferenceRebaser.RebaseCopy(
            "of:=[.A1]+[.$B2]+[.C$3]+[.$D$4]+['Other'.E5]",
            "Sheet1",
            2,
            1);

        Assert.That(result, Is.EqualTo(
            "of:=[.B3]+[.$B4]+[.D$3]+[.$D$4]+['Other'.E5]"));
    }

    [Test]
    public void RebasesWholeRowAndColumnRanges()
    {
        string columns = FormulaReferenceRebaser.RebaseCopy(
            "of:=[.A:.C]",
            "Sheet1",
            2,
            1);
        string rows = FormulaReferenceRebaser.RebaseCopy(
            "of:=[.1:.3]",
            "Sheet1",
            2,
            1);

        Assert.Multiple(() =>
        {
            Assert.That(columns, Is.EqualTo("of:=[.B:.D]"));
            Assert.That(rows, Is.EqualTo("of:=[.3:.5]"));
        });
    }

    [Test]
    public void MakesReferencesFollowMovedCellsRegardlessOfAnchoring()
    {
        string result = FormulaReferenceRebaser.FollowMovedRange(
            "of:=[.A1]+[.B2]+[.$B$2]+['Other'.B2]",
            "Sheet1",
            "Sheet1",
            0,
            0,
            2,
            2,
            2,
            3);

        Assert.That(result, Is.EqualTo(
            "of:=[.D3]+[.E4]+[.$E$4]+['Other'.B2]"));
    }

    [Test]
    public void AdjustsRangesForRowAndColumnInsertionAndDeletion()
    {
        string rowsInserted =
            FormulaReferenceRebaser.AdjustInsertedOrDeletedRows(
                "of:=[.A1:.C3]",
                "Sheet1",
                "Sheet1",
                1,
                0,
                2);
        string columnsInserted =
            FormulaReferenceRebaser.AdjustInsertedOrDeletedColumns(
                rowsInserted,
                "Sheet1",
                "Sheet1",
                1,
                0,
                1);
        string deleted =
            FormulaReferenceRebaser.AdjustInsertedOrDeletedRows(
                "of:=[.A2]",
                "Sheet1",
                "Sheet1",
                1,
                1,
                0);

        Assert.Multiple(() =>
        {
            Assert.That(rowsInserted, Is.EqualTo("of:=[.A1:.C5]"));
            Assert.That(columnsInserted, Is.EqualTo("of:=[.A1:.D5]"));
            Assert.That(deleted, Is.EqualTo("of:=#REF!"));
        });
    }

    [Test]
    public void RenamesAndDeletesSheetQualifiedReferencesOnly()
    {
        string source = "of:=['Old Sheet'.A1:Keep.B2]"
            + "+'Old Sheet'.$$Local"
            + "+['book.ods'#'Old Sheet'.A1]";
        string renamed = FormulaReferenceRebaser.RenameSheet(
            source,
            "Calc",
            "Old Sheet",
            "New Sheet");
        string deleted = FormulaReferenceRebaser.DeleteSheet(
            renamed,
            "Calc",
            "New Sheet");

        Assert.Multiple(() =>
        {
            Assert.That(renamed, Is.EqualTo(
                "of:=['New Sheet'.A1:Keep.B2]"
                + "+'New Sheet'.$$Local"
                + "+['book.ods'#'Old Sheet'.A1]"));
            Assert.That(deleted, Is.EqualTo(
                "of:=#REF!+#REF!+['book.ods'#'Old Sheet'.A1]"));
        });
    }

    [Test]
    public void PreservesNameBasedReferencesAcrossSheetOrderChanges()
    {
        string source = "of:=[First.A1:Third.B2]";
        string inserted =
            FormulaReferenceRebaser.PreserveAfterSheetInsertion(
                source,
                "Second",
                1);
        string reordered =
            FormulaReferenceRebaser.PreserveAfterSheetReorder(
                inserted,
                "Third",
                0);

        Assert.That(reordered, Is.SameAs(source));
    }
}
