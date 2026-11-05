namespace chart;
/// <summary>Provides the public chart export operations.</summary>
public class ChartExport : IChartExport
{
    /// <summary>Initializes a stateless chart export facade.</summary>
    public ChartExport()
    {
    }

    /// <summary>Exports a retained scene through the unified raw bitmap, BMP, PNG, or SVG workflow.</summary>
    /// <param name = "scene">The retained scene to export.</param>
    /// <param name = "options">The unified export options.</param>
    /// <returns>The format-specific export result.</returns>
    public ChartExportResult ExportScene(ChartRenderScene scene, ChartExportOptions options)
    {
        return UnifiedChartExportOperations.ExportScene(scene, options);
    }

    /// <summary>Exports every timeline frame independently in deterministic insertion order.</summary>
    /// <param name = "timeline">The ordered retained-scene snapshots.</param>
    /// <param name = "options">The unified export options applied to every frame.</param>
    /// <returns>One timed export result per frame.</returns>
    public ChartFrameExportResult[] ExportTimeline(ChartTimeline timeline, ChartExportOptions options)
    {
        return UnifiedChartExportOperations.ExportTimeline(timeline, options);
    }

    /// <summary>Renders and exports a document through the unified raw bitmap, BMP, PNG, or SVG workflow.</summary>
    /// <param name = "document">The document that supplies size, background, and default accessibility metadata.</param>
    /// <param name = "renderer">The renderer that supplies document drawing operations.</param>
    /// <param name = "options">The unified export options.</param>
    /// <returns>The format-specific export result.</returns>
    public ChartExportResult ExportDocument(ChartDocument document, IChartRenderer renderer, ChartExportOptions options)
    {
        return UnifiedChartExportOperations.ExportDocument(document, renderer, options);
    }

    /// <summary>Exports a retained scene to a stream using the configured stream ownership.</summary>
    /// <param name = "scene">The retained scene to export.</param>
    /// <param name = "stream">The writable destination stream.</param>
    /// <param name = "options">The unified export options.</param>
    public void WriteSceneExport(ChartRenderScene scene, System.IO.Stream stream, ChartExportOptions options)
    {
        UnifiedChartExportOperations.WriteSceneExport(scene, stream, options);
    }

    /// <summary>Renders and exports a document to a stream using the configured stream ownership.</summary>
    /// <param name = "document">The document to render and export.</param>
    /// <param name = "renderer">The renderer that supplies document drawing operations.</param>
    /// <param name = "stream">The writable destination stream.</param>
    /// <param name = "options">The unified export options.</param>
    public void WriteDocumentExport(ChartDocument document, IChartRenderer renderer, System.IO.Stream stream, ChartExportOptions options)
    {
        UnifiedChartExportOperations.WriteDocumentExport(document, renderer, stream, options);
    }

    /// <summary>Exports a retained scene to a file.</summary>
    /// <param name = "scene">The retained scene to export.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "options">The unified export options.</param>
    public void SaveSceneExport(ChartRenderScene scene, string path, ChartExportOptions options)
    {
        UnifiedChartExportOperations.SaveSceneExport(scene, path, options);
    }

    /// <summary>Renders and exports a document to a file.</summary>
    /// <param name = "document">The document to render and export.</param>
    /// <param name = "renderer">The renderer that supplies document drawing operations.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "options">The unified export options.</param>
    public void SaveDocumentExport(ChartDocument document, IChartRenderer renderer, string path, ChartExportOptions options)
    {
        UnifiedChartExportOperations.SaveDocumentExport(document, renderer, path, options);
    }

    /// <summary>
    /// Creates a deep copy of a raw bitmap, including its row stride and pixel format.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted raw bitmap.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public RawBitmap ToRawBitmap(RawBitmap bitmap)
    {
        return ChartExportOperations.ToRawBitmap(bitmap);
    }

    /// <summary>
    /// Copies the bitmap buffer without exposing subsequent changes to the bitmap.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted byte array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public byte[] ToRawBytes(RawBitmap bitmap)
    {
        return ChartExportOperations.ToRawBytes(bitmap);
    }

    /// <summary>
    /// Encodes a bitmap as a 32-bit, top-down BMP byte array.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted byte array.</returns>
    public byte[] ToBmpBytes(RawBitmap bitmap)
    {
        return ChartExportOperations.ToBmpBytes(bitmap);
    }

    /// <summary>
    /// Encodes a bitmap as a 24-bit or 32-bit BMP byte array with the requested row order.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name = "bitsPerPixel">The BMP pixel depth; supported values are 24 and 32.</param>
    /// <param name = "topDown"><c>true</c> to store rows in source order; <c>false</c> to store bottom-up BMP rows.</param>
    /// <returns>The converted byte array.</returns>
    public byte[] ToBmpBytesWithOptions(RawBitmap bitmap, int bitsPerPixel, bool topDown)
    {
        return ChartExportOperations.ToBmpBytes(bitmap, bitsPerPixel, topDown);
    }

    /// <summary>Encodes a bitmap using BMP bit-depth, orientation, and resolution options.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "options">The BMP encoder options.</param>
    /// <returns>The encoded BMP bytes.</returns>
    public byte[] ToBmpBytesUsingOptions(RawBitmap bitmap, ChartBmpOptions options)
    {
        return ChartExportOperations.ToBmpBytesUsingOptions(bitmap, options);
    }

    /// <summary>Writes an options-based BMP to a writable stream without closing it.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "stream">The destination stream.</param>
    /// <param name = "options">The BMP encoder options.</param>
    public void WriteBmp(RawBitmap bitmap, System.IO.Stream stream, ChartBmpOptions options)
    {
        ChartExportOperations.WriteBmp(bitmap, stream, options);
    }

    /// <summary>
    /// Encodes a bitmap as a 32-bit, top-down BMP and writes it to a file.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name = "path">The destination file path.</param>
    public void SaveBmp(RawBitmap bitmap, string path)
    {
        ChartExportOperations.SaveBmp(bitmap, path);
    }

    /// <summary>
    /// Encodes a bitmap with the requested BMP pixel depth and row order, then writes it to a file.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "bitsPerPixel">The BMP pixel depth; supported values are 24 and 32.</param>
    /// <param name = "topDown"><c>true</c> to store rows in source order; <c>false</c> to store bottom-up BMP rows.</param>
    public void SaveBmpWithOptions(RawBitmap bitmap, string path, int bitsPerPixel, bool topDown)
    {
        ChartExportOperations.SaveBmp(bitmap, path, bitsPerPixel, topDown);
    }

    /// <summary>Encodes an options-based BMP and writes it to a file.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "options">The BMP encoder options.</param>
    public void SaveBmpUsingOptions(RawBitmap bitmap, string path, ChartBmpOptions options)
    {
        ChartExportOperations.SaveBmpUsingOptions(bitmap, path, options);
    }

    /// <summary>
    /// Encodes a bitmap as a PNG byte array using lossless DEFLATE compression.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted byte array.</returns>
    public byte[] ToPngBytes(RawBitmap bitmap)
    {
        return ChartExportOperations.ToPngBytes(bitmap);
    }

    /// <summary>Encodes a bitmap using PNG color, filter, compression, and metadata options.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "options">The PNG encoder options.</param>
    /// <returns>The encoded PNG bytes.</returns>
    public byte[] ToPngBytesWithOptions(RawBitmap bitmap, ChartPngOptions options)
    {
        return ChartExportOperations.ToPngBytesWithOptions(bitmap, options);
    }

    /// <summary>Writes an options-based PNG to a writable stream without closing it.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "stream">The destination stream.</param>
    /// <param name = "options">The PNG encoder options.</param>
    public void WritePng(RawBitmap bitmap, System.IO.Stream stream, ChartPngOptions options)
    {
        ChartExportOperations.WritePng(bitmap, stream, options);
    }

    /// <summary>
    /// Encodes a bitmap as PNG and writes the resulting bytes to a file.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name = "path">The destination file path.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SavePng(RawBitmap bitmap, string path)
    {
        ChartExportOperations.SavePng(bitmap, path);
    }

    /// <summary>Encodes an options-based PNG and writes it to a file.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "options">The PNG encoder options.</param>
    public void SavePngWithOptions(RawBitmap bitmap, string path, ChartPngOptions options)
    {
        ChartExportOperations.SavePngWithOptions(bitmap, path, options);
    }

    /// <summary>
    /// Replays a retained scene into SVG and embeds the supplied accessibility metadata.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "accessibility">The title, description, series details, and optional data-table sidecar to embed.</param>
    /// <returns>The converted string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public string ToSvg(ChartRenderScene scene, ChartAccessibilityMetadata accessibility)
    {
        return ChartExportOperations.ToSvg(scene, accessibility);
    }

    /// <summary>Replays a retained scene into SVG with accessibility metadata and serialization options.</summary>
    /// <param name = "scene">The retained scene that supplies drawing commands.</param>
    /// <param name = "accessibility">The title, descriptions, and semantic series and point metadata.</param>
    /// <param name = "options">The identifier, role, and CSS-class serialization options.</param>
    /// <returns>The complete SVG document.</returns>
    public string ToSvgWithOptions(ChartRenderScene scene, ChartAccessibilityMetadata accessibility, ChartSvgOptions options)
    {
        return ChartExportOperations.ToSvgWithOptions(scene, accessibility, options);
    }

    /// <summary>
    /// Creates an empty SVG document with the requested pixel dimensions.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <returns>The converted string.</returns>
    public string CreateEmptySvg(int width, int height)
    {
        return ChartExportOperations.ToSvg(width, height);
    }

    /// <summary>
    /// Encodes an SVG document as UTF-8 bytes without a byte-order mark.
    /// </summary>
    /// <param name = "svg">The complete SVG document text.</param>
    /// <returns>The converted byte array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public byte[] ToSvgBytes(string svg)
    {
        return ChartExportOperations.ToSvgBytes(svg);
    }

    /// <summary>
    /// Encodes SVG text as UTF-8 in a new readable memory stream positioned at the start.
    /// </summary>
    /// <param name = "svg">The complete SVG document text.</param>
    /// <returns>The converted memory stream.</returns>
    public System.IO.MemoryStream ToSvgStream(string svg)
    {
        return ChartExportOperations.ToSvgStream(svg);
    }

    /// <summary>
    /// Writes UTF-8 SVG bytes to a stream without closing the stream.
    /// </summary>
    /// <param name = "svg">The complete SVG document text.</param>
    /// <param name = "stream">The writable stream that receives encoded output; it remains open.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void WriteSvg(string svg, System.IO.Stream stream)
    {
        ChartExportOperations.WriteSvg(svg, stream);
    }

    /// <summary>
    /// Writes an SVG document to a UTF-8 file.
    /// </summary>
    /// <param name = "svg">The complete SVG document text.</param>
    /// <param name = "path">The destination file path.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public void SaveSvg(string svg, string path)
    {
        ChartExportOperations.SaveSvg(svg, path);
    }
}
