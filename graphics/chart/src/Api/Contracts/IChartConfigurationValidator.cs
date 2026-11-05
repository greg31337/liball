namespace chart;
/// <summary>Defines exception-less validation for chart data, configuration, accessibility, and output models.</summary>
public interface IChartConfigurationValidator
{
    /// <summary>Validates row-aligned public series data.</summary>
    /// <param name = "data">The series data, or <c>null</c>.</param>
    /// <returns>Structured issues instead of expected validation exceptions.</returns>
    ChartValidationResult ValidateSeriesData(ChartSeriesData? data);
    /// <summary>Validates a column-oriented data table.</summary>
    /// <param name = "table">The data table, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateDataTable(ChartDataTable? table);
    /// <summary>Validates a typed data column.</summary>
    /// <param name = "column">The data column, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateDataColumn(ChartDataColumn? column);
    /// <summary>Validates a numeric data matrix.</summary>
    /// <param name = "matrix">The data matrix, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateDataMatrix(ChartDataMatrix? matrix);
    /// <summary>Validates composed scale policies.</summary>
    /// <param name = "policy">The scale policy, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateScalePolicy(ChartScalePolicy? policy);
    /// <summary>Validates numeric scale domains and ranges.</summary>
    /// <param name = "scale">The linear scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateLinearScale(ChartLinearScale? scale);
    /// <summary>Validates a logarithmic scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateLogScale(ChartLogScale? scale);
    /// <summary>Validates a power scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidatePowerScale(ChartPowerScale? scale);
    /// <summary>Validates a square-root scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateSquareRootScale(ChartSquareRootScale? scale);
    /// <summary>Validates a symmetric logarithmic scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateSymmetricLogScale(ChartSymmetricLogScale? scale);
    /// <summary>Validates a local-time scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateTimeScale(ChartTimeScale? scale);
    /// <summary>Validates a UTC time scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateUtcTimeScale(ChartUtcTimeScale? scale);
    /// <summary>Validates a quantile scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateQuantileScale(ChartQuantileScale? scale);
    /// <summary>Validates a quantize scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateQuantizeScale(ChartQuantizeScale? scale);
    /// <summary>Validates a threshold scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateThresholdScale(ChartThresholdScale? scale);
    /// <summary>Validates a diverging scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateDivergingScale(ChartDivergingScale? scale);
    /// <summary>Validates a radial scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateRadialScale(ChartRadialScale? scale);
    /// <summary>Validates an angle scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateAngleScale(ChartAngleScale? scale);
    /// <summary>Validates an opacity scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateOpacityScale(ChartOpacityScale? scale);
    /// <summary>Validates a marker-shape scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateShapeScale(ChartShapeScale? scale);
    /// <summary>Validates a category band scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateCategoryBandScale(ChartCategoryBandScale? scale);
    /// <summary>Validates a category point scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateCategoryPointScale(ChartCategoryPointScale? scale);
    /// <summary>Validates a continuous color scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateColorScale(ChartColorScale? scale);
    /// <summary>Validates an ordinal numeric scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateOrdinalScale(ChartOrdinalScale? scale);
    /// <summary>Validates a continuous size scale.</summary>
    /// <param name = "scale">The scale, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateSizeScale(ChartSizeScale? scale);
    /// <summary>Validates axis geometry, styling, ticks, thresholds, and breaks.</summary>
    /// <param name = "axis">The axis, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateAxis(ChartAxis? axis);
    /// <summary>Validates a layout grid.</summary>
    /// <param name = "grid">The grid, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateGrid(ChartGrid? grid);
    /// <summary>Validates retained layout rectangles and items.</summary>
    /// <param name = "layout">The layout result, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateLayout(ChartLayoutResult? layout);
    /// <summary>Validates cascading chart style settings.</summary>
    /// <param name = "style">The chart style, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateStyle(ChartStyle? style);
    /// <summary>Validates retained render style settings.</summary>
    /// <param name = "style">The render style, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateRenderStyle(ChartRenderStyle? style);
    /// <summary>Validates one retained render command and its style.</summary>
    /// <param name = "command">The render command, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateRenderCommand(ChartRenderCommand? command);
    /// <summary>Validates BMP encoder options.</summary>
    /// <param name = "options">The BMP options, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateBmpOptions(ChartBmpOptions? options);
    /// <summary>Validates PNG encoder options and textual metadata.</summary>
    /// <param name = "options">The PNG options, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidatePngOptions(ChartPngOptions? options);
    /// <summary>Validates SVG encoder options.</summary>
    /// <param name = "options">The SVG options, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateSvgOptions(ChartSvgOptions? options);
    /// <summary>Validates accessibility descriptions and sidecars.</summary>
    /// <param name = "metadata">The accessibility metadata, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateAccessibility(ChartAccessibilityMetadata? metadata);
    /// <summary>Validates unified export and nested encoder options.</summary>
    /// <param name = "options">The export options, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateExportOptions(ChartExportOptions? options);
    /// <summary>Validates sequential frame storage, identifiers, timing, and dimensions.</summary>
    /// <param name = "timeline">The timeline, or <c>null</c>.</param>
    /// <returns>The structured validation result.</returns>
    ChartValidationResult ValidateTimeline(ChartTimeline? timeline);
}
