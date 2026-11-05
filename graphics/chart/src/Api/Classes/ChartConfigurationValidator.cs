namespace chart;
/// <summary>Provides exception-less validation for chart data, configuration, accessibility, and output models.</summary>
public class ChartConfigurationValidator : IChartConfigurationValidator
{
    /// <summary>Initializes a stateless configuration validator.</summary>
    public ChartConfigurationValidator()
    {
    }

    /// <summary>Validates row-aligned public series data.</summary>
    /// <param name = "data">The series data, or <c>null</c>.</param>
    /// <returns>Structured issues instead of expected validation exceptions.</returns>
    public ChartValidationResult ValidateSeriesData(ChartSeriesData? data)
    {
        return ChartConfigurationValidatorOperations.ValidateSeriesData(data);
    }

    /// <summary>Validates a column-oriented data table.</summary>
    /// <param name = "table">The data table, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateDataTable(ChartDataTable? table)
    {
        return ChartConfigurationValidatorOperations.ValidateDataTable(table);
    }

    /// <summary>Validates a typed data column.</summary>
    /// <param name = "column">The data column, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateDataColumn(ChartDataColumn? column)
    {
        return ChartConfigurationValidatorOperations.ValidateDataColumn(column);
    }

    /// <summary>Validates a numeric data matrix.</summary>
    /// <param name = "matrix">The data matrix, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateDataMatrix(ChartDataMatrix? matrix)
    {
        return ChartConfigurationValidatorOperations.ValidateDataMatrix(matrix);
    }

    /// <summary>Validates composed scale policies.</summary>
    /// <param name = "policy">The scale policy, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateScalePolicy(ChartScalePolicy? policy)
    {
        return ChartConfigurationValidatorOperations.ValidateScalePolicy(policy);
    }

    /// <summary>Validates numeric scale domains and ranges.</summary>
    /// <param name = "scale">The linear scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateLinearScale(ChartLinearScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateLinearScale(scale);
    }

    /// <summary>Validates a logarithmic scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateLogScale(ChartLogScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateLogScale(scale);
    }

    /// <summary>Validates a power scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidatePowerScale(ChartPowerScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidatePowerScale(scale);
    }

    /// <summary>Validates a square-root scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateSquareRootScale(ChartSquareRootScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateSquareRootScale(scale);
    }

    /// <summary>Validates a symmetric logarithmic scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateSymmetricLogScale(ChartSymmetricLogScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateSymmetricLogScale(scale);
    }

    /// <summary>Validates a local-time scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateTimeScale(ChartTimeScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateTimeScale(scale);
    }

    /// <summary>Validates a UTC time scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateUtcTimeScale(ChartUtcTimeScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateUtcTimeScale(scale);
    }

    /// <summary>Validates a quantile scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateQuantileScale(ChartQuantileScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateQuantileScale(scale);
    }

    /// <summary>Validates a quantize scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateQuantizeScale(ChartQuantizeScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateQuantizeScale(scale);
    }

    /// <summary>Validates a threshold scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateThresholdScale(ChartThresholdScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateThresholdScale(scale);
    }

    /// <summary>Validates a diverging scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateDivergingScale(ChartDivergingScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateDivergingScale(scale);
    }

    /// <summary>Validates a radial scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateRadialScale(ChartRadialScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateRadialScale(scale);
    }

    /// <summary>Validates an angle scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateAngleScale(ChartAngleScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateAngleScale(scale);
    }

    /// <summary>Validates an opacity scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateOpacityScale(ChartOpacityScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateOpacityScale(scale);
    }

    /// <summary>Validates a marker-shape scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateShapeScale(ChartShapeScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateShapeScale(scale);
    }

    /// <summary>Validates a category band scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateCategoryBandScale(ChartCategoryBandScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateCategoryBandScale(scale);
    }

    /// <summary>Validates a category point scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateCategoryPointScale(ChartCategoryPointScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateCategoryPointScale(scale);
    }

    /// <summary>Validates a continuous color scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateColorScale(ChartColorScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateColorScale(scale);
    }

    /// <summary>Validates an ordinal numeric scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateOrdinalScale(ChartOrdinalScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateOrdinalScale(scale);
    }

    /// <summary>Validates a continuous size scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateSizeScale(ChartSizeScale? scale)
    {
        return ChartConfigurationValidatorOperations.ValidateSizeScale(scale);
    }

    /// <summary>Validates axis geometry, styling, ticks, thresholds, and breaks.</summary>
    /// <param name = "axis">The axis, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateAxis(ChartAxis? axis)
    {
        return ChartConfigurationValidatorOperations.ValidateAxis(axis);
    }

    /// <summary>Validates a layout grid.</summary>
    /// <param name = "grid">The grid, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateGrid(ChartGrid? grid)
    {
        return ChartConfigurationValidatorOperations.ValidateGrid(grid);
    }

    /// <summary>Validates retained layout rectangles and items.</summary>
    /// <param name = "layout">The layout result, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateLayout(ChartLayoutResult? layout)
    {
        return ChartConfigurationValidatorOperations.ValidateLayout(layout);
    }

    /// <summary>Validates cascading chart style settings.</summary>
    /// <param name = "style">The chart style, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateStyle(ChartStyle? style)
    {
        return ChartConfigurationValidatorOperations.ValidateStyle(style);
    }

    /// <summary>Validates retained render style settings.</summary>
    /// <param name = "style">The render style, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateRenderStyle(ChartRenderStyle? style)
    {
        return ChartConfigurationValidatorOperations.ValidateRenderStyle(style);
    }

    /// <summary>Validates one retained render command and its style.</summary>
    /// <param name = "command">The render command, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateRenderCommand(ChartRenderCommand? command)
    {
        return ChartConfigurationValidatorOperations.ValidateRenderCommand(command);
    }

    /// <summary>Validates BMP encoder options.</summary>
    /// <param name = "options">The BMP options, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateBmpOptions(ChartBmpOptions? options)
    {
        return ChartConfigurationValidatorOperations.ValidateBmpOptions(options);
    }

    /// <summary>Validates PNG encoder options and textual metadata.</summary>
    /// <param name = "options">The PNG options, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidatePngOptions(ChartPngOptions? options)
    {
        return ChartConfigurationValidatorOperations.ValidatePngOptions(options);
    }

    /// <summary>Validates SVG encoder options.</summary>
    /// <param name = "options">The SVG options, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateSvgOptions(ChartSvgOptions? options)
    {
        return ChartConfigurationValidatorOperations.ValidateSvgOptions(options);
    }

    /// <summary>Validates accessibility descriptions and sidecars.</summary>
    /// <param name = "metadata">The accessibility metadata, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateAccessibility(ChartAccessibilityMetadata? metadata)
    {
        return ChartConfigurationValidatorOperations.ValidateAccessibility(metadata);
    }

    /// <summary>Validates unified export and nested encoder options.</summary>
    /// <param name = "options">The export options, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateExportOptions(ChartExportOptions? options)
    {
        return ChartConfigurationValidatorOperations.ValidateExportOptions(options);
    }

    /// <summary>Validates sequential frame storage, identifiers, timing, and dimensions.</summary>
    /// <param name = "timeline">The timeline, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    public ChartValidationResult ValidateTimeline(ChartTimeline? timeline)
    {
        return ChartConfigurationValidatorOperations.ValidateTimeline(timeline);
    }
}
