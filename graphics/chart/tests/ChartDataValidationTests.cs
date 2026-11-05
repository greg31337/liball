namespace chart.Tests;

[TestFixture]
public class ChartDataValidationTests
{
    [Test]
    public void ValidateNumberSeries_ReportsEmptyMismatchedNaNAndInfinity()
    {
        ChartValidationResult empty = ChartDataValidatorOperations.ValidateNumberSeries("empty", new double[0], new double[0]);
        ChartValidationResult invalid = ChartDataValidatorOperations.ValidateNumberSeries(
            "bad",
            new double[] { 1.0, double.NaN, double.PositiveInfinity },
            new double[] { 2.0, double.NegativeInfinity });

        Assert.That(empty.HasCode(ChartDataValidatorOperations.EmptySeriesCode), Is.True);
        Assert.That(empty.IsValid(), Is.False);
        Assert.That(invalid.HasCode(ChartDataValidatorOperations.MismatchedLengthCode), Is.True);
        Assert.That(invalid.CountCode(ChartDataValidatorOperations.NaNCode), Is.EqualTo(1));
        Assert.That(invalid.CountCode(ChartDataValidatorOperations.InfinityCode), Is.EqualTo(2));
        Assert.That(invalid.IsValid(), Is.False);
    }

    [Test]
    public void ValidateColumn_ReportsMissingCategoriesAndNumericValues()
    {
        ChartDataColumn category = ChartDataColumn.CreateCategory("group", new string?[] { "A", "", null, "B" });
        ChartDataColumn number = ChartDataColumn.CreateNumber("value", new double[] { 1.0, double.NaN, double.NegativeInfinity });

        ChartValidationResult categoryResult = ChartDataValidatorOperations.ValidateColumn(category);
        ChartValidationResult numberResult = ChartDataValidatorOperations.ValidateColumn(number);

        Assert.That(categoryResult.CountCode(ChartDataValidatorOperations.MissingCategoryCode), Is.EqualTo(2));
        Assert.That(categoryResult.GetIssue(0).ColumnName, Is.EqualTo("group"));
        Assert.That(categoryResult.GetIssue(0).RowIndex, Is.EqualTo(1));
        Assert.That(numberResult.CountCode(ChartDataValidatorOperations.NaNCode), Is.EqualTo(1));
        Assert.That(numberResult.CountCode(ChartDataValidatorOperations.InfinityCode), Is.EqualTo(1));
    }

    [Test]
    public void ValidateTable_ReportsMismatchedLengthsAndMutatedInvalidState()
    {
        ChartDataTable table = new ChartDataTable();
        table.AddNumberColumn("x", new double[] { 1.0, 2.0 });
        table.AddCategoryColumn("group", new string[] { "A", "B" });
        table.GetColumn("group").Length = 1;

        ChartValidationResult result = ChartDataValidatorOperations.ValidateTable(table);

        Assert.That(result.HasCode(ChartDataValidatorOperations.MismatchedLengthCode), Is.True);
        Assert.That(result.GetIssue(0).Severity, Is.EqualTo(ChartValidationSeverity.Error));
        Assert.That(result.IsValid(), Is.False);
    }

    [Test]
    public void ValidateTable_ReportsEmptyAndInvalidDimensions()
    {
        ChartValidationResult empty = ChartDataValidatorOperations.ValidateTable(new ChartDataTable());
        ChartDataTable invalid = new ChartDataTable();
        invalid.ColumnCount = 2;

        ChartValidationResult invalidResult = ChartDataValidatorOperations.ValidateTable(invalid);

        Assert.That(empty.HasCode(ChartDataValidatorOperations.EmptySeriesCode), Is.True);
        Assert.That(invalidResult.HasCode(ChartDataValidatorOperations.InvalidDimensionsCode), Is.True);
    }

    [Test]
    public void ValidateMatrix_ReportsInvalidDimensionsAndNumericValues()
    {
        ChartDataMatrix matrix = ChartDataMatrix.FromValues(2, 2, new double[] { 1.0, double.NaN, double.PositiveInfinity, 4.0 });
        ChartDataMatrix invalid = new ChartDataMatrix(1, 1);

        invalid.Rows = int.MaxValue;
        invalid.Columns = 2;

        ChartValidationResult matrixResult = ChartDataValidatorOperations.ValidateMatrix(matrix);
        ChartValidationResult invalidResult = ChartDataValidatorOperations.ValidateMatrix(invalid);

        Assert.That(matrixResult.CountCode(ChartDataValidatorOperations.NaNCode), Is.EqualTo(1));
        Assert.That(matrixResult.CountCode(ChartDataValidatorOperations.InfinityCode), Is.EqualTo(1));
        Assert.That(invalidResult.HasCode(ChartDataValidatorOperations.InvalidDimensionsCode), Is.True);
    }

    [Test]
    public void ChartValidationResult_StoresIssuesAndCopiesIssueList()
    {
        ChartValidationResult result = new ChartValidationResult();

        result.AddWarning("Warn", "warning", "x", 1);
        result.AddError("Error", "error", "y", 2);
        ChartValidationIssue[] issues = result.CopyIssues();
        issues[0] = new ChartValidationIssue(ChartValidationSeverity.Error, "Changed", "changed", "", -1);

        Assert.That(result.IssueCount, Is.EqualTo(2));
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.GetIssue(0).Severity, Is.EqualTo(ChartValidationSeverity.Warning));
        Assert.That(result.GetIssue(1).Code, Is.EqualTo("Error"));
        Assert.That(result.HasCode("Changed"), Is.False);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => result.GetIssue(2));
    }

    [Test]
    public void ChartDataValidator_ValidatesNullInputs()
    {
        Assert.Throws<System.ArgumentNullException>(() => ChartDataValidatorOperations.ValidateNumberSeries("x", null!, new double[0]));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataValidatorOperations.ValidateNumberSeries("x", new double[0], null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataValidatorOperations.ValidateTable(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataValidatorOperations.ValidateColumn(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartDataValidatorOperations.ValidateMatrix(null!));
        Assert.Throws<System.ArgumentNullException>(() => new ChartValidationIssue(ChartValidationSeverity.Error, null!, "message", "", -1));
        Assert.Throws<System.ArgumentNullException>(() => new ChartValidationResult().CountCode(null!));
    }
}
