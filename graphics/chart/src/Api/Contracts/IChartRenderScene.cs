namespace chart;
/// <summary>Defines the public operations exposed by <see cref = "ChartRenderScene"/>.</summary>
public interface IChartRenderScene
{
    /// <summary>
    /// Adds the supplied command to this object.
    /// </summary>
    /// <param name = "command">The command.</param>
    /// <returns>The resulting render command.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    ChartRenderCommand AddCommand(ChartRenderCommand command);
    /// <summary>
    /// Returns the requested command.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The resulting render command.</returns>
    /// <exception cref = "System.ArgumentOutOfRangeException">Thrown when an argument falls outside its supported range.</exception>
    ChartRenderCommand CommandAt(int index);
    /// <summary>
    /// Creates an independent copy of the commands.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartRenderCommand[] CopyCommands();
    /// <summary>
    /// Creates a deep copy that can be modified independently.
    /// </summary>
    /// <returns>A new value that can be modified independently of the source.</returns>
    ChartRenderScene Copy();
    /// <summary>
    /// Replays the retained scene into a new top-down RGBA bitmap.
    /// </summary>
    /// <returns>The converted raw bitmap.</returns>
    RawBitmap ToRawBitmap();
    /// <summary>
    /// Replays the retained scene into a complete SVG document string.
    /// </summary>
    /// <returns>The converted string.</returns>
    string ToSvgString();
    /// <summary>Adds a complete public text block with wrapping, alignment, rotation, clipping, direction, and rich spans.</summary>
    /// <param name = "id">A stable identifier used for retained commands and SVG elements.</param>
    /// <param name = "textBlock">The configured public text block.</param>
    /// <param name = "x">The horizontal anchor coordinate.</param>
    /// <param name = "y">The vertical anchor coordinate.</param>
    /// <returns>The retained text command.</returns>
    ChartRenderCommand AddTextBlock(string id, ChartTextBlock textBlock, double x, double y);
    /// <summary>
    /// Creates a deterministic text trace of the retained drawing commands.
    /// </summary>
    /// <returns>The converted string.</returns>
    string ToTraceString();
    /// <summary>
    /// Renders raster using the supplied data and configuration.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <exception cref = "System.ArgumentException">Thrown when the supplied values are inconsistent or unsupported.</exception>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void RenderRaster(RawBitmap bitmap);
    /// <summary>Replaces the current width.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetWidth(int value);
    /// <summary>Replaces the current height.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetHeight(int value);
    /// <summary>Returns the current commands.</summary>
    /// <returns>The current field value.</returns>
    ChartRenderCommand[] GetCommands();
    /// <summary>Replaces the current commands.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCommands(ChartRenderCommand[] value);
    /// <summary>Returns the current command count.</summary>
    /// <returns>The current field value.</returns>
    int GetCommandCount();
    /// <summary>Replaces the current command count.</summary>
    /// <param name = "value">The replacement field value.</param>
    void SetCommandCount(int value);
}
