namespace chart;
/// <summary>Defines substitutable chart label placement charts operations.</summary>
public interface IChartLabelPlacementCharts
{
    /// <summary>Formats numeric values for use by any label layout operation.</summary>
    /// <param name = "values">The finite numeric values.</param>
    /// <param name = "formatter">The value-label formatter.</param>
    /// <returns>One formatted label per input value.</returns>
    string[] FormatValueLabels(double[] values, IChartValueLabelFormatter formatter);
    /// <summary>
    /// Computes the layout for point labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "xValues">The horizontal data values; its length must match the related value arrays.</param>
    /// <param name = "yValues">The vertical data values; its length must match the related value arrays.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "offset">The offset.</param>
    /// <param name = "position">The position.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartPlacedLabel[] LayoutPointLabels(string idPrefix, string[] labels, double[] xValues, double[] yValues, double labelWidth, double labelHeight, double offset, ChartLabelPosition position, bool reduceCollisions);
    /// <summary>
    /// Computes the layout for bar labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "bars">The bars.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "offset">The offset.</param>
    /// <param name = "position">The position.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartPlacedLabel[] LayoutBarLabels(string idPrefix, string[] labels, ChartCartesianSeries bars, double labelWidth, double labelHeight, double offset, ChartLabelPosition position, bool reduceCollisions);
    /// <summary>
    /// Computes the layout for heatmap cell labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "xCenters">The x centers.</param>
    /// <param name = "yCenters">The y centers.</param>
    /// <param name = "cellWidth">The cell width.</param>
    /// <param name = "cellHeight">The cell height.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    ChartPlacedLabel[] LayoutHeatmapCellLabels(string idPrefix, string[] labels, double[] xCenters, double[] yCenters, double cellWidth, double cellHeight, double labelWidth, double labelHeight, bool reduceCollisions);
    /// <summary>
    /// Computes the layout for pie slice labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "layout">The layout.</param>
    /// <param name = "labelRadius">The label radius.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartPlacedLabel[] LayoutPieSliceLabels(string idPrefix, ChartPieLayout layout, double labelRadius, double labelWidth, double labelHeight, bool reduceCollisions);
    /// <summary>
    /// Computes the layout for contour labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "segments">The segments.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartPlacedLabel[] LayoutContourLabels(string idPrefix, string[] labels, ChartContourSegment[] segments, double labelWidth, double labelHeight, bool reduceCollisions);
    /// <summary>
    /// Computes the layout for annotation labels within the supplied bounds.
    /// </summary>
    /// <param name = "idPrefix">The prefix used to create stable identifiers for generated items.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "annotationBounds">The annotation bounds.</param>
    /// <param name = "labelWidth">The label width.</param>
    /// <param name = "labelHeight">The label height.</param>
    /// <param name = "offset">The offset.</param>
    /// <param name = "position">The position.</param>
    /// <param name = "reduceCollisions">The reduce collisions.</param>
    /// <returns>The resulting placed label array.</returns>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartPlacedLabel[] LayoutAnnotationLabels(string idPrefix, string[] labels, ChartRect[] annotationBounds, double labelWidth, double labelHeight, double offset, ChartLabelPosition position, bool reduceCollisions);
    /// <summary>
    /// Creates the requested label series from the supplied data and configuration.
    /// </summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    /// <returns>The newly created cartesian series.</returns>
    ChartCartesianSeries CreateLabelSeries(string id, ChartPlacedLabel[] labels, ChartRenderStyle style);
    /// <summary>
    /// Adds the supplied visible labels to scene to this object.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <param name = "style">The fill, stroke, text, opacity, and quality settings to apply.</param>
    void AddVisibleLabelsToScene(IChartRenderTarget scene, ChartPlacedLabel[] labels, ChartRenderStyle style);
    /// <summary>Adds visible labels and leader lines after collision visibility has been resolved.</summary>
    /// <param name = "scene">The retained scene that receives the labels and leaders.</param>
    /// <param name = "labels">The placed labels, including their source anchors and visibility.</param>
    /// <param name = "labelStyle">The label render style.</param>
    /// <param name = "leaderStyle">The leader-line render style.</param>
    void AddVisibleLabelsAndLeaderLinesToScene(IChartRenderTarget scene, ChartPlacedLabel[] labels, ChartRenderStyle labelStyle, ChartRenderStyle leaderStyle);
    /// <summary>
    /// Applies the reduce collisions operation to the supplied data and returns the computed result.
    /// </summary>
    /// <param name = "labels">The display labels in the same order as their corresponding values.</param>
    /// <returns>The resulting int.</returns>
    int ReduceCollisions(ChartPlacedLabel[] labels);
}
