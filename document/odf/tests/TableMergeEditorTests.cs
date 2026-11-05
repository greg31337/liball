using System.Text;

namespace odf.Tests;

public class TableMergeEditorTests
{
    [Test]
    public void MaterializesMergeMatrixAndNestedTableStructure()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Outer'>"
            + "<table:table-row>"
            + "<table:table-cell table:number-columns-spanned='2' table:number-rows-spanned='2'/>"
            + "<table:covered-table-cell/>"
            + "<table:table-cell><table:table table:name='Inner'>"
            + "<table:table-row><table:table-cell/></table:table-row>"
            + "</table:table></table:table-cell>"
            + "</table:table-row>"
            + "<table:table-row><table:covered-table-cell/><table:covered-table-cell/>"
            + "<table:table-cell/></table:table-row>"
            + "</table:table>"
            + "<table:table table:name='Matrix'>"
            + "<table:table-row><table:table-cell table:formula='of:=SUM([.A1:.B2])' "
            + "table:number-matrix-columns-spanned='2' table:number-matrix-rows-spanned='2'/>"
            + "<table:table-cell/></table:table-row>"
            + "<table:table-row><table:table-cell table:number-columns-repeated='2'/></table:table-row>"
            + "</table:table>");
        OdfTable outer = document.Tables[0];
        OdfCell merged = TableDocumentModel.GetCell(outer, 0, 0)!;
        OdfCell nested = TableDocumentModel.GetCell(outer, 0, 2)!;
        OdfCell matrix = TableDocumentModel.GetCell(
            TableDocumentModel.FindTable(document, "Matrix")!,
            0,
            0)!;

        Assert.Multiple(() =>
        {
            Assert.That(merged.IsMergeOrigin, Is.True);
            Assert.That(merged.ColumnSpan, Is.EqualTo(2));
            Assert.That(merged.RowSpan, Is.EqualTo(2));
            Assert.That(merged.HasValidStructure, Is.True);
            Assert.That(nested.NestedTables, Has.Length.EqualTo(1));
            Assert.That(nested.NestedTables[0].ParentCell, Is.SameAs(nested));
            Assert.That(matrix.IsMatrixFormula, Is.True);
            Assert.That(matrix.MatrixColumnSpan, Is.EqualTo(2));
            Assert.That(matrix.MatrixRowSpan, Is.EqualTo(2));
            Assert.That(matrix.HasValidStructure, Is.True);
            Assert.That(
                TableDocumentModel.GetCell(
                    TableDocumentModel.FindTable(document, "Matrix")!,
                    1,
                    1)!.IsCovered,
                Is.False);
        });
    }

    [Test]
    public void MergesAndUnmergesRangesWhileRestoringCompactRuns()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'>"
            + "<table:table-row table:number-rows-repeated='4'>"
            + "<table:table-cell table:number-columns-repeated='4'/>"
            + "</table:table-row></table:table>");

        OdfTable merged = TableMergeEditor.MergeRange(
            document,
            document.Tables[0],
            1,
            1,
            2,
            2);
        OdfCell origin = TableDocumentModel.GetCell(merged, 1, 1)!;

        Assert.Multiple(() =>
        {
            Assert.That(origin.IsMergeOrigin, Is.True);
            Assert.That(origin.ColumnSpan, Is.EqualTo(2));
            Assert.That(origin.RowSpan, Is.EqualTo(2));
            Assert.That(TableDocumentModel.GetCell(merged, 1, 2)!.IsCovered,
                Is.True);
            Assert.That(TableDocumentModel.GetCell(merged, 2, 1)!.IsCovered,
                Is.True);
            Assert.That(TableDocumentModel.GetCell(merged, 2, 2)!.IsCovered,
                Is.True);
            Assert.That(merged.Rows, Has.Length.EqualTo(4));
        });

        OdfTable unmerged = TableMergeEditor.UnmergeRange(
            document,
            merged,
            1,
            1);
        OdfTable reread = WriteAndRead(document).Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(unmerged.Rows, Has.Length.EqualTo(1));
            Assert.That(unmerged.Rows[0].RepeatCount, Is.EqualTo(4));
            Assert.That(unmerged.Rows[0].Cells, Has.Length.EqualTo(1));
            Assert.That(unmerged.Rows[0].Cells[0].RepeatCount, Is.EqualTo(4));
            Assert.That(unmerged.Rows[0].Cells[0].IsCovered, Is.False);
            Assert.That(reread.Rows[0].RepeatCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void SetsAndClearsMatrixAndArrayFormulaRangesWithoutCoveredCells()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'>"
            + "<table:table-row table:number-rows-repeated='3'>"
            + "<table:table-cell table:number-columns-repeated='3'/>"
            + "</table:table-row></table:table>");

        OdfTable matrixTable = TableMergeEditor.SetMatrixFormula(
            document,
            document.Tables[0],
            0,
            0,
            2,
            2,
            "of:=SUM([.A1:.B2])");
        OdfCell matrix = TableDocumentModel.GetCell(matrixTable, 0, 0)!;
        SemanticValidationResult validMatrix = Validate(document);

        Assert.Multiple(() =>
        {
            Assert.That(matrix.IsMatrixFormula, Is.True);
            Assert.That(matrix.MatrixColumnSpan, Is.EqualTo(2));
            Assert.That(matrix.MatrixRowSpan, Is.EqualTo(2));
            Assert.That(TableDocumentModel.GetCell(matrixTable, 0, 1)!.IsCovered,
                Is.False);
            Assert.That(TableDocumentModel.GetCell(matrixTable, 1, 0)!.IsCovered,
                Is.False);
            Assert.That(validMatrix.Diagnostics.Any(
                item => item.Code == OdfDiagnosticCodes.InvalidMerge),
                Is.False);
        });

        OdfTable cleared = TableMergeEditor.ClearMatrixFormula(
            document,
            matrixTable,
            0,
            0);
        OdfTable arrayTable = TableMergeEditor.SetArrayFormula(
            document,
            cleared,
            1,
            1,
            2,
            2,
            "of:=[.A1:.B2]*2");
        OdfTable reread = WriteAndRead(document).Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(TableDocumentModel.GetCell(arrayTable, 0, 0)!.Formula,
                Is.Empty);
            Assert.That(TableDocumentModel.GetCell(arrayTable, 1, 1)!.IsMatrixFormula,
                Is.True);
            Assert.That(TableDocumentModel.GetCell(reread, 1, 1)!.Formula,
                Is.EqualTo("of:=[.A1:.B2]*2"));
            Assert.That(TableDocumentModel.GetCell(reread, 1, 1)!.MatrixRowSpan,
                Is.EqualTo(2));
        });
    }

    [Test]
    public void SynchronizesEditedStructuralCellFieldsThroughRoundTrip()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Sheet1'>"
            + "<table:table-row><table:table-cell table:number-columns-repeated='2'/></table:table-row>"
            + "<table:table-row><table:table-cell table:number-columns-repeated='2'/></table:table-row>"
            + "</table:table>");
        OdfCell origin = TableRepeatRunEditor.SplitCellForEdit(
            document.Tables[0],
            0,
            0);
        origin.IsMergeOrigin = true;
        origin.ColumnSpan = 2;
        origin.RowSpan = 2;
        TableRepeatRunEditor.SplitCellForEdit(document.Tables[0], 0, 1).IsCovered = true;
        TableRepeatRunEditor.SplitCellForEdit(document.Tables[0], 1, 0).IsCovered = true;
        TableRepeatRunEditor.SplitCellForEdit(document.Tables[0], 1, 1).IsCovered = true;

        OdfTable reread = WriteAndRead(document).Tables[0];

        Assert.Multiple(() =>
        {
            Assert.That(TableDocumentModel.GetCell(reread, 0, 0)!.IsMergeOrigin,
                Is.True);
            Assert.That(TableDocumentModel.GetCell(reread, 0, 0)!.ColumnSpan,
                Is.EqualTo(2));
            Assert.That(TableDocumentModel.GetCell(reread, 1, 1)!.IsCovered,
                Is.True);
            Assert.That(Validate(WriteAndRead(document)).IsValid, Is.True);
        });
    }

    [Test]
    public void RejectsNestedCoveredCellsAndOutOfBoundsMatrixRanges()
    {
        OdfDocument document = ReadSpreadsheet(
            "<table:table table:name='Outer'>"
            + "<table:table-row><table:covered-table-cell>"
            + "<table:table table:name='Inner'><table:table-row>"
            + "<table:table-cell/></table:table-row></table:table>"
            + "</table:covered-table-cell></table:table-row>"
            + "</table:table>"
            + "<table:table table:name='Matrix'><table:table-row>"
            + "<table:table-cell table:formula='of:=1' "
            + "table:number-matrix-columns-spanned='2' "
            + "table:number-matrix-rows-spanned='2'/>"
            + "</table:table-row></table:table>");

        SemanticValidationResult result = Validate(document);

        Assert.Multiple(() =>
        {
            Assert.That(document.Tables[0].Rows[0].Cells[0].HasValidStructure,
                Is.False);
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics.Count(
                item => item.Code == OdfDiagnosticCodes.InvalidMerge),
                Is.GreaterThanOrEqualTo(2));
        });
    }

    private static SemanticValidationResult Validate(OdfDocument document)
    {
        OdfValidationOptions options = new OdfValidationOptions();
        options.Mode = OdfValidationMode.Complete;
        options.Version = OdfVersion.Version14;
        return new SemanticDocumentValidator(options).ValidateDocument(document);
    }

    private static OdfDocument ReadSpreadsheet(string tables)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:of='" + OdfNamespaces.OpenFormula + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Spreadsheet + "'>"
            + "<office:body><office:spreadsheet>"
            + tables
            + "</office:spreadsheet></office:body></office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "table-structures.fods",
            new OdfLoadOptions());
    }

    private static OdfDocument WriteAndRead(OdfDocument document)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        return new OdfDocumentReader().ReadDocumentBytes(
            bytes,
            "table-structures.fods",
            new OdfLoadOptions());
    }
}
