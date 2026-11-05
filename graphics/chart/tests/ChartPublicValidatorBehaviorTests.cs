namespace chart.Tests;

[TestFixture]
public class ChartPublicValidatorBehaviorTests
{
    [Test]
    public void DataValidatorFacade_ValidatesEverySupportedDataShape()
    {
        ChartDataValidator validator = new ChartDataValidator();
        ChartDataTable table = new ChartDataTable();
        ChartDataColumn column = table.AddNumberColumn("value", new double[] { 1.0, 2.0 });
        ChartDataMatrix matrix = new ChartDataMatrix(1, 2);

        Assert.That(validator.ValidateNumberSeries("series", new double[] { 1.0 }, new double[] { 2.0 }).IsValid(), Is.True);
        Assert.That(validator.ValidateTable(table).IsValid(), Is.True);
        Assert.That(validator.ValidateColumn(column).IsValid(), Is.True);
        Assert.That(validator.ValidateMatrix(matrix).IsValid(), Is.True);

        ChartValidationResult invalidSeries = validator.ValidateNumberSeries("series", new double[] { double.NaN }, new double[0]);
        matrix.SetValue(0, 0, double.PositiveInfinity);
        Assert.That(invalidSeries.HasCode(ChartDataValidatorConstants.MismatchedLengthCode), Is.True);
        Assert.That(invalidSeries.HasCode(ChartDataValidatorConstants.NaNCode), Is.True);
        Assert.That(validator.ValidateMatrix(matrix).HasCode(ChartDataValidatorConstants.InfinityCode), Is.True);

        Assert.Throws<System.ArgumentNullException>(() => validator.ValidateNumberSeries("series", null!, new double[0]));
        Assert.Throws<System.ArgumentNullException>(() => validator.ValidateTable(null!));
        Assert.Throws<System.ArgumentNullException>(() => validator.ValidateColumn(null!));
        Assert.Throws<System.ArgumentNullException>(() => validator.ValidateMatrix(null!));
    }

    [Test]
    public void RenderValidatorFacade_ValidatesDocumentsPlotsSeriesScenesAndComposedInput()
    {
        ChartRenderValidator validator = new ChartRenderValidator();
        ChartDocument document = new ChartDocument(100.0, 80.0);
        ChartPlot plot = document.AddPlotWithBounds("plot", new ChartRect(0.0, 0.0, 100.0, 80.0));
        plot.AddBottomAxis(0.0, 1.0, 2);
        plot.AddLeftAxis(0.0, 1.0, 2);
        ChartCartesianSeries series = new ChartCartesianSeries("series", ChartCartesianSeriesKind.Scatter);
        series.AddMarker("point", ChartMarkerShape.Circle, 0.5, 0.5, 4.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        ChartRenderScene scene = new ChartRenderScene(10, 10);
        scene.AddRect("rect", 0.0, 0.0, 2.0, 2.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.Black()));

        Assert.That(validator.ValidateDocument(document).HasErrors(), Is.False);
        Assert.That(validator.ValidatePlot(plot).HasErrors(), Is.False);
        Assert.That(validator.ValidateSeries(series).IsValid(), Is.True);
        Assert.That(validator.ValidateScene(scene).IsValid(), Is.True);
        Assert.That(validator.ValidateCartesianRender(plot, new ChartCartesianSeries[] { series }, new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)).HasErrors(), Is.False);

        scene.CommandAt(0).SetWidth(-1.0);
        Assert.That(validator.ValidateScene(scene).HasCode(ChartRenderValidatorConstants.InvalidSceneCommandCode), Is.True);
        Assert.Throws<System.ArgumentNullException>(() => validator.ValidateDocument(null!));
        Assert.Throws<System.ArgumentNullException>(() => validator.ValidatePlot(null!));
        Assert.Throws<System.ArgumentNullException>(() => validator.ValidateSeries(null!));
        Assert.Throws<System.ArgumentNullException>(() => validator.ValidateScene(null!));
        Assert.Throws<System.ArgumentNullException>(() => validator.ValidateCartesianRender(plot, null!, new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)));
    }

    [Test]
    public void ValidationResult_PublicMutationCopyQueriesAndDiagnosticsRemainConsistent()
    {
        ChartValidationResult result = new ChartValidationResult();
        result.AddIssue(ChartValidationSeverity.Warning, "W", "warning", "x", 0);
        result.AddError("E", "error", "y", 1);
        ChartValidationResult nested = new ChartValidationResult();
        nested.AddWarning("W", "second warning", "z", -1);
        result.AddResult(nested);

        Assert.That(result.GetIssueCount(), Is.EqualTo(3));
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.IsValid(), Is.False);
        Assert.That(result.HasCode("E"), Is.True);
        Assert.That(result.CountCode("W"), Is.EqualTo(2));
        Assert.That(result.DiagnosticText(), Does.Contain("Error E y[1]: error"));

        ChartValidationIssue[] copy = result.CopyIssues();
        copy[0].Code = "changed";
        Assert.That(result.GetIssue(0).Code, Is.EqualTo("W"));

        ChartValidationIssue replacement = new ChartValidationIssue(ChartValidationSeverity.Warning, "R", "replacement", "", -1);
        result.SetIssues(new ChartValidationIssue[] { replacement });
        result.SetIssueCount(1);
        Assert.That(result.GetIssues().Length, Is.EqualTo(1));
        Assert.That(result.GetIssueCount(), Is.EqualTo(1));
        Assert.That(result.GetIssue(0).Code, Is.EqualTo("R"));

        Assert.Throws<System.ArgumentNullException>(() => result.AddResult(null!));
        Assert.Throws<System.ArgumentNullException>(() => result.CountCode(null!));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => result.GetIssue(-1));
    }
}
