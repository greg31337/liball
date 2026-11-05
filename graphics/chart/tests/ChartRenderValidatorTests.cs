namespace chart.Tests;

[TestFixture]
public class ChartRenderValidatorTests
{
    [Test]
    public void ValidateDocument_ReportsWarningsAndErrorsBeforeRendering()
    {
        ChartDocument document = new ChartDocument(100.0, 80.0);
        ChartPlot first = document.AddPlotWithBounds("plot", new ChartRect(0.0, 0.0, 40.0, 40.0));
        ChartPlot second = document.AddPlotWithBounds("other", new ChartRect(10.0, 10.0, 40.0, 40.0));
        first.SetMargins(new ChartPadding(30.0, 0.0, 30.0, 0.0));
        second.Id = "plot";
        second.Bounds.Width = 0.0;
        document.Width = 0.0;
        document.Margins.Left = -1.0;

        ChartValidationResult result = ChartRenderValidatorOperations.ValidateDocument(document);

        Assert.That(result.HasCode(ChartRenderValidatorOperations.InvalidDocumentSizeCode), Is.True);
        Assert.That(result.HasCode(ChartRenderValidatorOperations.InvalidMarginsCode), Is.True);
        Assert.That(result.HasCode(ChartRenderValidatorOperations.EmptyInnerBoundsCode), Is.True);
        Assert.That(result.HasCode(ChartRenderValidatorOperations.DuplicatePlotIdCode), Is.True);
        Assert.That(result.HasCode(ChartRenderValidatorOperations.InvalidPlotBoundsCode), Is.True);
        Assert.That(result.HasCode(ChartRenderValidatorOperations.MissingAxisCode), Is.True);
        Assert.That(result.HasErrors(), Is.True);
    }

    [Test]
    public void ValidatePlotAndCartesianRender_ReportsAxisDomainAndSeriesIssues()
    {
        ChartPlot plot = new ChartPlot("plot");
        ChartAxis xAxis = plot.AddBottomAxis(0.0, 10.0, 3);
        ChartAxis yAxis = plot.AddLeftAxis(0.0, 10.0, 3);
        ChartCartesianSeries series = ChartLineAreaChartsOperations.CreateLineSeries("line", new double[] { 0.0, 1.0 }, new double[] { 0.0, 1.0 }, ChartRenderStyle.CreateStroke(ChartColor.Black(), 1.0));
        xAxis.Domain.Maximum = xAxis.Domain.Minimum;
        yAxis.PlotArea.Height = 0.0;
        series.Commands[0].Points[1].X = double.NaN;

        ChartValidationResult plotResult = ChartRenderValidatorOperations.ValidatePlot(plot);
        ChartValidationResult renderResult = ChartRenderValidatorOperations.ValidateCartesianRender(plot, new ChartCartesianSeries[] { series, null! }, new ChartRange(0.0, 0.0), new ChartRange(0.0, 1.0));

        Assert.That(plotResult.HasCode(ChartRenderValidatorOperations.InvalidAxisDomainCode), Is.True);
        Assert.That(plotResult.HasCode(ChartRenderValidatorOperations.InvalidAxisPlotAreaCode), Is.True);
        Assert.That(renderResult.CountCode(ChartRenderValidatorOperations.InvalidAxisDomainCode), Is.GreaterThanOrEqualTo(2));
        Assert.That(renderResult.HasCode(ChartRenderValidatorOperations.InvalidSeriesCommandCode), Is.True);
        Assert.That(renderResult.HasErrors(), Is.True);
    }

    [Test]
    public void ValidateSeriesAndScene_CheckCommandsWithoutRendering()
    {
        ChartCartesianSeries emptySeries = new ChartCartesianSeries("empty", ChartCartesianSeriesKind.Line);
        ChartCartesianSeries invalidSeries = new ChartCartesianSeries("invalid", ChartCartesianSeriesKind.Mixed);
        invalidSeries.AddCommand(ChartRenderCommand.CreateRect("rect", 1.0, 2.0, 3.0, 4.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.Black())));
        invalidSeries.Commands[0].Width = -1.0;
        ChartRenderScene emptyScene = new ChartRenderScene(10, 10);
        ChartRenderScene invalidScene = new ChartRenderScene(10, 10);
        invalidScene.AddText("text", "label", 2.0, 3.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        invalidScene.Commands[0].Style = null!;
        invalidScene.Commands[0].Text = null!;

        ChartValidationResult emptySeriesResult = ChartRenderValidatorOperations.ValidateSeries(emptySeries);
        ChartValidationResult invalidSeriesResult = ChartRenderValidatorOperations.ValidateSeries(invalidSeries);
        ChartValidationResult emptySceneResult = ChartRenderValidatorOperations.ValidateScene(emptyScene);
        ChartValidationResult invalidSceneResult = ChartRenderValidatorOperations.ValidateScene(invalidScene);

        Assert.That(emptySeriesResult.HasCode(ChartRenderValidatorOperations.EmptySeriesCode), Is.True);
        Assert.That(invalidSeriesResult.HasCode(ChartRenderValidatorOperations.InvalidSeriesCommandCode), Is.True);
        Assert.That(emptySceneResult.GetIssue(0).Severity, Is.EqualTo(ChartValidationSeverity.Warning));
        Assert.That(emptySceneResult.HasCode(ChartRenderValidatorOperations.EmptySceneCode), Is.True);
        Assert.That(invalidSceneResult.CountCode(ChartRenderValidatorOperations.InvalidSceneCommandCode), Is.EqualTo(2));
    }

    [Test]
    public void ValidationResult_CanMergeResults()
    {
        ChartValidationResult first = new ChartValidationResult();
        ChartValidationResult second = new ChartValidationResult();
        first.AddWarning("Warn", "warning", "a", 0);
        second.AddError("Error", "error", "b", 1);

        first.AddResult(second);

        Assert.That(first.IssueCount, Is.EqualTo(2));
        Assert.That(first.HasErrors(), Is.True);
        Assert.That(first.GetIssue(1).Code, Is.EqualTo("Error"));
    }

    [Test]
    public void RenderValidator_ValidatesNullInputs()
    {
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderValidatorOperations.ValidateDocument(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderValidatorOperations.ValidatePlot(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderValidatorOperations.ValidateSeries(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderValidatorOperations.ValidateScene(null!));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderValidatorOperations.ValidateCartesianRender(null!, new ChartCartesianSeries[0], new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => ChartRenderValidatorOperations.ValidateCartesianRender(new ChartPlot("plot"), null!, new ChartRange(0.0, 1.0), new ChartRange(0.0, 1.0)));
        Assert.Throws<System.ArgumentNullException>(() => new ChartValidationResult().AddResult(null!));
    }
}
