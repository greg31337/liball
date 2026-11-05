namespace chart.Tests;

[TestFixture]
public class ChartConfigurationValidatorTests
{
    [Test]
    public void MissingModels_ReturnStableIssuesWithoutThrowing()
    {
        ChartConfigurationValidator validator = new ChartConfigurationValidator();

        AssertMissing(validator.ValidateSeriesData(null));
        AssertMissing(validator.ValidateDataTable(null));
        AssertMissing(validator.ValidateDataColumn(null));
        AssertMissing(validator.ValidateDataMatrix(null));
        AssertMissing(validator.ValidateScalePolicy(null));
        AssertMissing(validator.ValidateLinearScale(null));
        AssertMissing(validator.ValidateLogScale(null));
        AssertMissing(validator.ValidatePowerScale(null));
        AssertMissing(validator.ValidateSquareRootScale(null));
        AssertMissing(validator.ValidateSymmetricLogScale(null));
        AssertMissing(validator.ValidateTimeScale(null));
        AssertMissing(validator.ValidateUtcTimeScale(null));
        AssertMissing(validator.ValidateQuantileScale(null));
        AssertMissing(validator.ValidateQuantizeScale(null));
        AssertMissing(validator.ValidateThresholdScale(null));
        AssertMissing(validator.ValidateDivergingScale(null));
        AssertMissing(validator.ValidateRadialScale(null));
        AssertMissing(validator.ValidateAngleScale(null));
        AssertMissing(validator.ValidateOpacityScale(null));
        AssertMissing(validator.ValidateShapeScale(null));
        AssertMissing(validator.ValidateCategoryBandScale(null));
        AssertMissing(validator.ValidateCategoryPointScale(null));
        AssertMissing(validator.ValidateColorScale(null));
        AssertMissing(validator.ValidateOrdinalScale(null));
        AssertMissing(validator.ValidateSizeScale(null));
        AssertMissing(validator.ValidateAxis(null));
        AssertMissing(validator.ValidateGrid(null));
        AssertMissing(validator.ValidateLayout(null));
        AssertMissing(validator.ValidateStyle(null));
        AssertMissing(validator.ValidateRenderStyle(null));
        AssertMissing(validator.ValidateRenderCommand(null));
        AssertMissing(validator.ValidateBmpOptions(null));
        AssertMissing(validator.ValidatePngOptions(null));
        AssertMissing(validator.ValidateSvgOptions(null));
        AssertMissing(validator.ValidateAccessibility(null));
        AssertMissing(validator.ValidateExportOptions(null));
        AssertMissing(validator.ValidateTimeline(null));
        Assert.That(ChartConfigurationValidatorConstants.MissingRequiredValueCode, Is.EqualTo("MissingRequiredValue"));
        Assert.That(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode, Is.EqualTo("InvalidEncoderOption"));
    }

    [Test]
    public void ValidModels_AreAcceptedAcrossConfigurationCategories()
    {
        ChartConfigurationValidator validator = new ChartConfigurationValidator();
        ChartSeriesData data = new ChartSeriesData(2);
        data.SetXValues(new double[] { 1.0, 2.0 });
        ChartDataTable table = new ChartDataTable();
        ChartDataColumn column = table.AddNumberColumn("value", new double[] { 1.0, 2.0 });
        ChartDataMatrix matrix = new ChartDataMatrix(1, 2);
        ChartScalePolicy policy = new ChartScalePolicy();
        ChartLinearScale scale = new ChartLinearScale(0.0, 10.0, 0.0, 100.0);
        ChartAxis axis = new ChartAxis(
            ChartAxisKind.X,
            ChartAxisPlacement.Bottom,
            new ChartTick[] { new ChartTick(ChartTickKind.Numeric, 0.0, "0", 0) },
            new ChartRange(0.0, 10.0),
            new ChartRect(0.0, 0.0, 100.0, 50.0));
        ChartGrid grid = new ChartGrid(2, 2);
        ChartLayoutResult layout = new ChartLayoutResult(new ChartRect(0.0, 0.0, 100.0, 60.0), new ChartRect(5.0, 5.0, 90.0, 50.0), new ChartRect(10.0, 10.0, 80.0, 40.0));
        ChartStyle style = new ChartStyle();
        ChartRenderStyle renderStyle = new ChartRenderStyle();
        ChartRenderScene scene = new ChartRenderScene(20, 10);
        ChartRenderCommand command = scene.AddRect("rect", 1.0, 1.0, 5.0, 4.0, 0.0, 0.0, ChartRenderStyle.CreateFill(ChartColor.Black()));
        ChartBmpOptions bmp = new ChartBmpOptions();
        ChartPngOptions png = new ChartPngOptions();
        ChartSvgOptions svg = new ChartSvgOptions();
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();
        ChartExportOptions export = new ChartExportOptions();
        ChartTimeline timeline = new ChartTimeline();
        timeline.AddFrame(new ChartTimelineFrame("frame", scene, 100));

        Assert.That(validator.ValidateSeriesData(data).IsValid(), Is.True);
        Assert.That(validator.ValidateDataTable(table).IsValid(), Is.True);
        Assert.That(validator.ValidateDataColumn(column).IsValid(), Is.True);
        Assert.That(validator.ValidateDataMatrix(matrix).IsValid(), Is.True);
        Assert.That(validator.ValidateScalePolicy(policy).IsValid(), Is.True);
        Assert.That(validator.ValidateLinearScale(scale).IsValid(), Is.True);
        Assert.That(validator.ValidateAxis(axis).IsValid(), Is.True);
        Assert.That(validator.ValidateGrid(grid).IsValid(), Is.True);
        Assert.That(validator.ValidateLayout(layout).IsValid(), Is.True);
        Assert.That(validator.ValidateStyle(style).IsValid(), Is.True);
        Assert.That(validator.ValidateRenderStyle(renderStyle).IsValid(), Is.True);
        Assert.That(validator.ValidateRenderCommand(command).IsValid(), Is.True);
        Assert.That(validator.ValidateBmpOptions(bmp).IsValid(), Is.True);
        Assert.That(validator.ValidatePngOptions(png).IsValid(), Is.True);
        Assert.That(validator.ValidateSvgOptions(svg).IsValid(), Is.True);
        Assert.That(validator.ValidateAccessibility(accessibility).IsValid(), Is.True);
        Assert.That(validator.ValidateExportOptions(export).IsValid(), Is.True);
        Assert.That(validator.ValidateTimeline(timeline).IsValid(), Is.True);
    }

    [Test]
    public void ConcreteScaleModels_AreValidatedAcrossEveryScaleFamily()
    {
        ChartConfigurationValidator validator = new ChartConfigurationValidator();
        System.DateTime localStart = new System.DateTime(2024, 1, 1);
        System.DateTime localEnd = new System.DateTime(2024, 1, 2);
        System.DateTime utcStart = new System.DateTime(2024, 1, 1, 0, 0, 0, System.DateTimeKind.Utc);
        System.DateTime utcEnd = new System.DateTime(2024, 1, 2, 0, 0, 0, System.DateTimeKind.Utc);
        string[] categories = new string[] { "a", "b" };

        Assert.That(validator.ValidateLogScale(new ChartLogScale(1.0, 10.0, 0.0, 1.0, 10.0)).IsValid(), Is.True);
        Assert.That(validator.ValidatePowerScale(new ChartPowerScale(-2.0, 2.0, 0.0, 1.0, 2.0)).IsValid(), Is.True);
        Assert.That(validator.ValidateSquareRootScale(new ChartSquareRootScale(0.0, 4.0, 0.0, 2.0)).IsValid(), Is.True);
        Assert.That(validator.ValidateSymmetricLogScale(new ChartSymmetricLogScale(-10.0, 10.0, -1.0, 1.0, 1.0)).IsValid(), Is.True);
        Assert.That(validator.ValidateTimeScale(new ChartTimeScale(localStart, localEnd, 0.0, 1.0)).IsValid(), Is.True);
        Assert.That(validator.ValidateUtcTimeScale(new ChartUtcTimeScale(utcStart, utcEnd, 0.0, 1.0)).IsValid(), Is.True);
        Assert.That(validator.ValidateQuantileScale(new ChartQuantileScale(new double[] { 1.0, 2.0, 3.0, 4.0 }, new double[] { 0.0, 1.0 })).IsValid(), Is.True);
        Assert.That(validator.ValidateQuantileScale(new ChartQuantileScale(new double[] { 1.0, 1.0 }, new double[] { 0.0, 1.0 })).IsValid(), Is.True);
        Assert.That(validator.ValidateQuantizeScale(new ChartQuantizeScale(0.0, 10.0, new double[] { 0.0, 1.0 })).IsValid(), Is.True);
        Assert.That(validator.ValidateThresholdScale(new ChartThresholdScale(new double[] { 5.0 }, new double[] { 0.0, 1.0 })).IsValid(), Is.True);
        Assert.That(validator.ValidateDivergingScale(new ChartDivergingScale(-1.0, 0.0, 1.0, 0.0, 0.5, 1.0)).IsValid(), Is.True);
        Assert.That(validator.ValidateRadialScale(new ChartRadialScale(0.0, 10.0, 1.0, 5.0)).IsValid(), Is.True);
        Assert.That(validator.ValidateAngleScale(new ChartAngleScale(0.0, 10.0, 0.0, System.Math.PI)).IsValid(), Is.True);
        Assert.That(validator.ValidateOpacityScale(new ChartOpacityScale(0.0, 10.0, 0.2, 1.0)).IsValid(), Is.True);
        Assert.That(validator.ValidateShapeScale(new ChartShapeScale(categories, new ChartMarkerShape[] { ChartMarkerShape.Circle, ChartMarkerShape.Square })).IsValid(), Is.True);
        Assert.That(validator.ValidateCategoryBandScale(new ChartCategoryBandScale(categories, 0.0, 100.0, 0.1, 0.1)).IsValid(), Is.True);
        Assert.That(validator.ValidateCategoryPointScale(new ChartCategoryPointScale(categories, 0.0, 100.0, 0.1)).IsValid(), Is.True);
        Assert.That(validator.ValidateColorScale(new ChartColorScale(0.0, 1.0, ChartColor.Black(), ChartColor.White())).IsValid(), Is.True);
        Assert.That(validator.ValidateOrdinalScale(new ChartOrdinalScale(categories, new double[] { 1.0, 2.0 })).IsValid(), Is.True);
        Assert.That(validator.ValidateSizeScale(new ChartSizeScale(0.0, 1.0, 1.0, 10.0)).IsValid(), Is.True);

        ChartLogScale invalidLog = new ChartLogScale(1.0, 10.0, 0.0, 1.0, 10.0);
        invalidLog.SetBaseValue(1.0);
        ChartCategoryBandScale invalidBand = new ChartCategoryBandScale(categories, 0.0, 100.0, 0.1, 0.1);
        invalidBand.SetCategories(new string[] { "duplicate", "duplicate" });
        ChartColorScale invalidColor = new ChartColorScale(0.0, 1.0, ChartColor.Black(), ChartColor.White());
        invalidColor.SetEndColor(null!);
        ChartSizeScale invalidSize = new ChartSizeScale(0.0, 1.0, 1.0, 10.0);
        invalidSize.SetSizeMinimum(double.NaN);

        Assert.That(validator.ValidateLogScale(invalidLog).HasCode(ChartConfigurationValidatorConstants.InvalidScaleCode), Is.True);
        Assert.That(validator.ValidateCategoryBandScale(invalidBand).HasCode(ChartConfigurationValidatorConstants.InvalidScaleCode), Is.True);
        Assert.That(validator.ValidateColorScale(invalidColor).HasCode(ChartConfigurationValidatorConstants.InvalidScaleCode), Is.True);
        Assert.That(validator.ValidateSizeScale(invalidSize).HasCode(ChartConfigurationValidatorConstants.InvalidScaleCode), Is.True);
    }

    [Test]
    public void InvalidDataScaleAxisLayoutStyleAndCommand_ReturnCategoryCodes()
    {
        ChartConfigurationValidator validator = new ChartConfigurationValidator();
        ChartSeriesData data = new ChartSeriesData(2);
        data.XValues = new double[] { double.NaN };
        ChartDataColumnFactory columnFactory = new ChartDataColumnFactory();
        ChartDataColumn duplicateColumn = columnFactory.CreateNumber("duplicate", new double[] { 1.0 });
        ChartDataTable table = new ChartDataTable();
        table.SetColumns(new ChartDataColumn[] { duplicateColumn, duplicateColumn });
        table.SetColumnCount(2);
        table.SetRowCount(1);
        ChartDataColumn textColumn = columnFactory.CreateText("text", new string? [] { "value" });
        textColumn.SetLength(2);
        ChartDataMatrix matrix = new ChartDataMatrix(1, 1);
        matrix.SetRows(2);
        ChartScalePolicy policy = new ChartScalePolicy();
        policy.DomainPadding = -1.0;
        policy.Sharing = (ChartScaleSharing)99;
        ChartLinearScale scale = new ChartLinearScale(0.0, 1.0, 0.0, 1.0);
        scale.SetDomainMinimum(double.NaN);
        ChartAxis axis = new ChartAxis(ChartAxisKind.X, ChartAxisPlacement.Bottom, new ChartTick[0], new ChartRange(0.0, 1.0), new ChartRect(0.0, 0.0, 10.0, 10.0));
        axis.SetDomain(new ChartRange(0.0, 0.0));
        ChartGrid grid = new ChartGrid(1, 1);
        grid.SetRows(0);
        grid.SetRowGap(double.NaN);
        ChartLayoutResult layout = new ChartLayoutResult(new ChartRect(0.0, 0.0, 10.0, 10.0), new ChartRect(0.0, 0.0, 10.0, 10.0), new ChartRect(0.0, 0.0, 10.0, 10.0));
        layout.AddItem(ChartLayoutItemKind.Plot, "duplicate", new ChartRect(0.0, 0.0, 2.0, 2.0), ChartSide.Left);
        layout.AddItem(ChartLayoutItemKind.Plot, "duplicate", new ChartRect(2.0, 0.0, 2.0, 2.0), ChartSide.Right);
        ChartStyle style = new ChartStyle();
        style.SetHasOpacity(true);
        style.Opacity = 2.0;
        ChartRenderStyle renderStyle = new ChartRenderStyle();
        renderStyle.SetFontSize(0);
        renderStyle.SetLineCap((ChartLineCap)99);
        ChartRenderCommand command = ChartRenderCommand.CreateRect("bad", 0.0, 0.0, 1.0, 1.0, 0.0, 0.0, new ChartRenderStyle());
        command.SetWidth(0.0);

        Assert.That(validator.ValidateSeriesData(data).HasCode(ChartConfigurationValidatorConstants.InvalidDataCode), Is.True);
        Assert.That(validator.ValidateDataTable(table).HasCode(ChartConfigurationValidatorConstants.DuplicateIdentifierCode), Is.True);
        Assert.That(validator.ValidateDataColumn(textColumn).HasCode(ChartDataValidatorConstants.InvalidDimensionsCode), Is.True);
        Assert.That(validator.ValidateDataMatrix(matrix).HasCode(ChartDataValidatorConstants.InvalidDimensionsCode), Is.True);
        Assert.That(validator.ValidateScalePolicy(policy).CountCode(ChartConfigurationValidatorConstants.InvalidScaleCode), Is.EqualTo(2));
        Assert.That(validator.ValidateLinearScale(scale).HasCode(ChartConfigurationValidatorConstants.InvalidScaleCode), Is.True);
        Assert.That(validator.ValidateAxis(axis).HasCode(ChartConfigurationValidatorConstants.InvalidAxisCode), Is.True);
        Assert.That(validator.ValidateGrid(grid).HasCode(ChartConfigurationValidatorConstants.InvalidLayoutCode), Is.True);
        Assert.That(validator.ValidateLayout(layout).HasCode(ChartConfigurationValidatorConstants.DuplicateIdentifierCode), Is.True);
        Assert.That(validator.ValidateStyle(style).HasCode(ChartConfigurationValidatorConstants.InvalidStyleCode), Is.True);
        Assert.That(validator.ValidateRenderStyle(renderStyle).HasCode(ChartConfigurationValidatorConstants.InvalidStyleCode), Is.True);
        Assert.That(validator.ValidateRenderCommand(command).HasCode(ChartConfigurationValidatorConstants.InvalidRenderCommandCode), Is.True);
    }

    [Test]
    public void InvalidEncoderAccessibilityExportAndTimeline_ReturnNestedStableCodes()
    {
        ChartConfigurationValidator validator = new ChartConfigurationValidator();
        ChartBmpOptions bmp = new ChartBmpOptions();
        bmp.BitDepth = (ChartBmpBitDepth)7;
        bmp.HorizontalPixelsPerMeter = -1;
        ChartPngOptions png = new ChartPngOptions();
        png.Filter = (ChartPngFilter)99;
        png.Metadata.Title = null!;
        ChartSvgOptions svg = new ChartSvgOptions();
        svg.IdPrefix = null!;

        ChartSeriesAccessibility first = new ChartSeriesAccessibility("duplicate", "first");
        first.AddPointDescription("point", "point", 0).SetRowIndex(-1);
        ChartSeriesAccessibility second = new ChartSeriesAccessibility("duplicate", "second");
        ChartAccessibilityMetadata accessibility = new ChartAccessibilityMetadata();
        accessibility.SetSeries(new ChartSeriesAccessibility[] { first, second });
        accessibility.SetSeriesCount(2);

        ChartExportOptions export = new ChartExportOptions();
        export.Format = (ChartOutputFormat)99;
        export.Bmp = bmp;
        export.Png = png;
        export.Svg = svg;
        export.Accessibility = accessibility;

        ChartRenderScene scene = new ChartRenderScene(2, 2);
        ChartTimelineFrame frame = new ChartTimelineFrame("same", scene, 10);
        ChartTimeline timeline = new ChartTimeline();
        timeline.AddFrame(frame);
        timeline.Frames[1] = frame;
        timeline.FrameCount = 2;

        Assert.That(validator.ValidateBmpOptions(bmp).CountCode(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode), Is.EqualTo(2));
        Assert.That(validator.ValidatePngOptions(png).CountCode(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode), Is.EqualTo(2));
        Assert.That(validator.ValidateSvgOptions(svg).HasCode(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode), Is.True);
        Assert.That(validator.ValidateAccessibility(accessibility).HasCode(ChartConfigurationValidatorConstants.InvalidAccessibilityCode), Is.True);
        Assert.That(validator.ValidateAccessibility(accessibility).HasCode(ChartConfigurationValidatorConstants.DuplicateIdentifierCode), Is.True);
        ChartValidationResult exportResult = validator.ValidateExportOptions(export);
        Assert.That(exportResult.HasCode(ChartConfigurationValidatorConstants.InvalidExportOptionCode), Is.True);
        Assert.That(exportResult.HasCode(ChartConfigurationValidatorConstants.InvalidEncoderOptionCode), Is.True);
        Assert.That(exportResult.HasCode(ChartConfigurationValidatorConstants.InvalidAccessibilityCode), Is.True);
        Assert.That(validator.ValidateTimeline(timeline).HasCode(ChartConfigurationValidatorConstants.DuplicateIdentifierCode), Is.True);
    }

    [Test]
    public void DiagnosticText_IsDeterministicAndIncludesStableLocations()
    {
        ChartValidationResult result = new ChartValidationResult();
        result.AddWarning("W001", "Check this setting.", "style.opacity", 2);
        result.AddError("E001", "Required value is missing.", "export.png", -1);

        Assert.That(
            result.DiagnosticText(),
            Is.EqualTo("Warning W001 style.opacity[2]: Check this setting.\nError E001 export.png: Required value is missing."));
        Assert.That(new ChartValidationResult().DiagnosticText(), Is.Empty);
    }

    static void AssertMissing(ChartValidationResult result)
    {
        Assert.That(result.HasErrors(), Is.True);
        Assert.That(result.HasCode(ChartConfigurationValidatorConstants.MissingRequiredValueCode), Is.True);
        Assert.That(result.GetIssue(0).Severity, Is.EqualTo(ChartValidationSeverity.Error));
    }
}
