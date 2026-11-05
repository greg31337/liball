namespace chart;
/// <summary>Defines substitutable chart export operations.</summary>
public interface IChartExport
{
    /// <summary>Exports a retained scene through the unified raw bitmap, BMP, PNG, or SVG workflow.</summary>
    /// <param name = "scene">The retained scene to export.</param>
    /// <param name = "options">The unified export options.</param>
    /// <returns>The format-specific export result.</returns>
    ChartExportResult ExportScene(ChartRenderScene scene, ChartExportOptions options);
    /// <summary>Exports every timeline frame independently in deterministic insertion order.</summary>
    /// <param name = "timeline">The ordered retained-scene snapshots.</param>
    /// <param name = "options">The unified export options applied to every frame.</param>
    /// <returns>One timed export result per frame.</returns>
    ChartFrameExportResult[] ExportTimeline(ChartTimeline timeline, ChartExportOptions options);
    /// <summary>Renders and exports a document through the unified raw bitmap, BMP, PNG, or SVG workflow.</summary>
    /// <param name = "document">The document that supplies size, background, and default accessibility metadata.</param>
    /// <param name = "renderer">The renderer that supplies document drawing operations.</param>
    /// <param name = "options">The unified export options.</param>
    /// <returns>The format-specific export result.</returns>
    ChartExportResult ExportDocument(ChartDocument document, IChartRenderer renderer, ChartExportOptions options);
    /// <summary>Exports a retained scene to a stream using the configured stream ownership.</summary>
    /// <param name = "scene">The retained scene to export.</param>
    /// <param name = "stream">The writable destination stream.</param>
    /// <param name = "options">The unified export options.</param>
    void WriteSceneExport(ChartRenderScene scene, System.IO.Stream stream, ChartExportOptions options);
    /// <summary>Renders and exports a document to a stream using the configured stream ownership.</summary>
    /// <param name = "document">The document to render and export.</param>
    /// <param name = "renderer">The renderer that supplies document drawing operations.</param>
    /// <param name = "stream">The writable destination stream.</param>
    /// <param name = "options">The unified export options.</param>
    void WriteDocumentExport(ChartDocument document, IChartRenderer renderer, System.IO.Stream stream, ChartExportOptions options);
    /// <summary>Exports a retained scene to a file.</summary>
    /// <param name = "scene">The retained scene to export.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "options">The unified export options.</param>
    void SaveSceneExport(ChartRenderScene scene, string path, ChartExportOptions options);
    /// <summary>Renders and exports a document to a file.</summary>
    /// <param name = "document">The document to render and export.</param>
    /// <param name = "renderer">The renderer that supplies document drawing operations.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "options">The unified export options.</param>
    void SaveDocumentExport(ChartDocument document, IChartRenderer renderer, string path, ChartExportOptions options);
    /// <summary>
    /// Creates a deep copy of a raw bitmap, including its row stride and pixel format.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted raw bitmap.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    RawBitmap ToRawBitmap(RawBitmap bitmap);
    /// <summary>
    /// Copies the bitmap buffer without exposing subsequent changes to the bitmap.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted byte array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    byte[] ToRawBytes(RawBitmap bitmap);
    /// <summary>
    /// Encodes a bitmap as a 32-bit, top-down BMP byte array.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted byte array.</returns>
    byte[] ToBmpBytes(RawBitmap bitmap);
    /// <summary>
    /// Encodes a bitmap as a 24-bit or 32-bit BMP byte array with the requested row order.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name = "bitsPerPixel">The BMP pixel depth; supported values are 24 and 32.</param>
    /// <param name = "topDown"><c>true</c> to store rows in source order; <c>false</c> to store bottom-up BMP rows.</param>
    /// <returns>The converted byte array.</returns>
    byte[] ToBmpBytesWithOptions(RawBitmap bitmap, int bitsPerPixel, bool topDown);
    /// <summary>Encodes a bitmap using BMP bit-depth, orientation, and resolution options.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "options">The BMP encoder options.</param>
    /// <returns>The encoded BMP bytes.</returns>
    byte[] ToBmpBytesUsingOptions(RawBitmap bitmap, ChartBmpOptions options);
    /// <summary>Writes an options-based BMP to a writable stream without closing it.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "stream">The destination stream.</param>
    /// <param name = "options">The BMP encoder options.</param>
    void WriteBmp(RawBitmap bitmap, System.IO.Stream stream, ChartBmpOptions options);
    /// <summary>
    /// Encodes a bitmap as a 32-bit, top-down BMP and writes it to a file.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name = "path">The destination file path.</param>
    void SaveBmp(RawBitmap bitmap, string path);
    /// <summary>
    /// Encodes a bitmap with the requested BMP pixel depth and row order, then writes it to a file.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "bitsPerPixel">The BMP pixel depth; supported values are 24 and 32.</param>
    /// <param name = "topDown"><c>true</c> to store rows in source order; <c>false</c> to store bottom-up BMP rows.</param>
    void SaveBmpWithOptions(RawBitmap bitmap, string path, int bitsPerPixel, bool topDown);
    /// <summary>Encodes an options-based BMP and writes it to a file.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "options">The BMP encoder options.</param>
    void SaveBmpUsingOptions(RawBitmap bitmap, string path, ChartBmpOptions options);
    /// <summary>
    /// Encodes a bitmap as a PNG byte array using lossless DEFLATE compression.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted byte array.</returns>
    byte[] ToPngBytes(RawBitmap bitmap);
    /// <summary>Encodes a bitmap using PNG color, filter, compression, and metadata options.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "options">The PNG encoder options.</param>
    /// <returns>The encoded PNG bytes.</returns>
    byte[] ToPngBytesWithOptions(RawBitmap bitmap, ChartPngOptions options);
    /// <summary>Writes an options-based PNG to a writable stream without closing it.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "stream">The destination stream.</param>
    /// <param name = "options">The PNG encoder options.</param>
    void WritePng(RawBitmap bitmap, System.IO.Stream stream, ChartPngOptions options);
    /// <summary>
    /// Encodes a bitmap as PNG and writes the resulting bytes to a file.
    /// </summary>
    /// <param name = "bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name = "path">The destination file path.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SavePng(RawBitmap bitmap, string path);
    /// <summary>Encodes an options-based PNG and writes it to a file.</summary>
    /// <param name = "bitmap">The source bitmap.</param>
    /// <param name = "path">The destination file path.</param>
    /// <param name = "options">The PNG encoder options.</param>
    void SavePngWithOptions(RawBitmap bitmap, string path, ChartPngOptions options);
    /// <summary>
    /// Replays a retained scene into SVG and embeds the supplied accessibility metadata.
    /// </summary>
    /// <param name = "scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name = "accessibility">The title, description, series details, and optional data-table sidecar to embed.</param>
    /// <returns>The converted string.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    string ToSvg(ChartRenderScene scene, ChartAccessibilityMetadata accessibility);
    /// <summary>Replays a retained scene into SVG with accessibility metadata and serialization options.</summary>
    /// <param name = "scene">The retained scene that supplies drawing commands.</param>
    /// <param name = "accessibility">The title, descriptions, and semantic series and point metadata.</param>
    /// <param name = "options">The identifier, role, and CSS-class serialization options.</param>
    /// <returns>The complete SVG document.</returns>
    string ToSvgWithOptions(ChartRenderScene scene, ChartAccessibilityMetadata accessibility, ChartSvgOptions options);
    /// <summary>
    /// Creates an empty SVG document with the requested pixel dimensions.
    /// </summary>
    /// <param name = "width">The width in pixels or chart units, which must be positive.</param>
    /// <param name = "height">The height in pixels or chart units, which must be positive.</param>
    /// <returns>The converted string.</returns>
    string CreateEmptySvg(int width, int height);
    /// <summary>
    /// Encodes an SVG document as UTF-8 bytes without a byte-order mark.
    /// </summary>
    /// <param name = "svg">The complete SVG document text.</param>
    /// <returns>The converted byte array.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    byte[] ToSvgBytes(string svg);
    /// <summary>
    /// Encodes SVG text as UTF-8 in a new readable memory stream positioned at the start.
    /// </summary>
    /// <param name = "svg">The complete SVG document text.</param>
    /// <returns>The converted memory stream.</returns>
    System.IO.MemoryStream ToSvgStream(string svg);
    /// <summary>
    /// Writes UTF-8 SVG bytes to a stream without closing the stream.
    /// </summary>
    /// <param name = "svg">The complete SVG document text.</param>
    /// <param name = "stream">The writable stream that receives encoded output; it remains open.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void WriteSvg(string svg, System.IO.Stream stream);
    /// <summary>
    /// Writes an SVG document to a UTF-8 file.
    /// </summary>
    /// <param name = "svg">The complete SVG document text.</param>
    /// <param name = "path">The destination file path.</param>
    /// <exception cref = "System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    void SaveSvg(string svg, string path);
}
