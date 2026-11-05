namespace chart;

/// <summary>
/// Converts rendered chart output to raw bytes, BMP, PNG, SVG, streams, or files.
/// </summary>
internal static class ChartExportOperations
{
    /// <summary>
    /// Creates a deep copy of a raw bitmap, including its row stride and pixel format.
    /// </summary>
    /// <param name="bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted raw bitmap.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static RawBitmap ToRawBitmap(RawBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        return bitmap.Copy();
    }

    /// <summary>
    /// Copies the bitmap buffer without exposing subsequent changes to the bitmap.
    /// </summary>
    /// <param name="bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted byte array.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static byte[] ToRawBytes(RawBitmap bitmap)
    {
        if (bitmap == null)
        {
            throw new System.ArgumentNullException("bitmap");
        }

        return bitmap.CopyBuffer();
    }

    /// <summary>
    /// Encodes a bitmap as a 32-bit, top-down BMP byte array.
    /// </summary>
    /// <param name="bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted byte array.</returns>
    public static byte[] ToBmpBytes(RawBitmap bitmap)
    {
        return ToBmpBytes(bitmap, 32, true);
    }

    /// <summary>
    /// Encodes a bitmap as a 24-bit or 32-bit BMP byte array with the requested row order.
    /// </summary>
    /// <param name="bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name="bitsPerPixel">The BMP pixel depth; supported values are 24 and 32.</param>
    /// <param name="topDown"><c>true</c> to store rows in source order; <c>false</c> to store bottom-up BMP rows.</param>
    /// <returns>The converted byte array.</returns>
    public static byte[] ToBmpBytes(RawBitmap bitmap, int bitsPerPixel, bool topDown)
    {
        return BmpEncoder.Encode(bitmap, bitsPerPixel, topDown);
    }

    /// <summary>Encodes a bitmap using BMP bit-depth, orientation, and resolution options.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <param name="options">The BMP encoder options.</param>
    /// <returns>The encoded BMP bytes.</returns>
    public static byte[] ToBmpBytesUsingOptions(RawBitmap bitmap, ChartBmpOptions options)
    {
        return BmpEncoder.EncodeWithOptions(bitmap, options);
    }

    /// <summary>Writes an options-based BMP to a writable stream without closing it.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">The BMP encoder options.</param>
    public static void WriteBmp(RawBitmap bitmap, System.IO.Stream stream, ChartBmpOptions options)
    {
        BmpEncoder.WriteStreamWithOptions(bitmap, stream, options);
    }

    /// <summary>
    /// Encodes a bitmap as a 32-bit, top-down BMP and writes it to a file.
    /// </summary>
    /// <param name="bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name="path">The destination file path.</param>
    public static void SaveBmp(RawBitmap bitmap, string path)
    {
        SaveBmp(bitmap, path, 32, true);
    }

    /// <summary>
    /// Encodes a bitmap with the requested BMP pixel depth and row order, then writes it to a file.
    /// </summary>
    /// <param name="bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="bitsPerPixel">The BMP pixel depth; supported values are 24 and 32.</param>
    /// <param name="topDown"><c>true</c> to store rows in source order; <c>false</c> to store bottom-up BMP rows.</param>
    public static void SaveBmp(RawBitmap bitmap, string path, int bitsPerPixel, bool topDown)
    {
        BmpEncoder.WriteFile(bitmap, path, bitsPerPixel, topDown);
    }

    /// <summary>Encodes an options-based BMP and writes it to a file.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The BMP encoder options.</param>
    public static void SaveBmpUsingOptions(RawBitmap bitmap, string path, ChartBmpOptions options)
    {
        BmpEncoder.WriteFileWithOptions(bitmap, path, options);
    }

    /// <summary>
    /// Encodes a bitmap as a PNG byte array using lossless DEFLATE compression.
    /// </summary>
    /// <param name="bitmap">The bitmap to read, write, render, or encode.</param>
    /// <returns>The converted byte array.</returns>
    public static byte[] ToPngBytes(RawBitmap bitmap)
    {
        return PngEncoder.Encode(bitmap);
    }

    /// <summary>Encodes a bitmap using PNG color, filter, compression, and metadata options.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <param name="options">The PNG encoder options.</param>
    /// <returns>The encoded PNG bytes.</returns>
    public static byte[] ToPngBytesWithOptions(RawBitmap bitmap, ChartPngOptions options)
    {
        return PngEncoder.EncodeWithOptions(bitmap, options);
    }

    /// <summary>Writes an options-based PNG to a writable stream without closing it.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="options">The PNG encoder options.</param>
    public static void WritePng(RawBitmap bitmap, System.IO.Stream stream, ChartPngOptions options)
    {
        PngEncoder.WriteStreamWithOptions(bitmap, stream, options);
    }

    /// <summary>
    /// Encodes a bitmap as PNG and writes the resulting bytes to a file.
    /// </summary>
    /// <param name="bitmap">The bitmap to read, write, render, or encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void SavePng(RawBitmap bitmap, string path)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        System.IO.File.WriteAllBytes(path, ToPngBytes(bitmap));
    }

    /// <summary>Encodes an options-based PNG and writes it to a file.</summary>
    /// <param name="bitmap">The source bitmap.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The PNG encoder options.</param>
    public static void SavePngWithOptions(RawBitmap bitmap, string path, ChartPngOptions options)
    {
        PngEncoder.WriteFileWithOptions(bitmap, path, options);
    }

    internal static string ToSvg(SvgWriter writer)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        return writer.ToSvgString();
    }

    internal static string ToSvg(SvgWriter writer, ChartAccessibilityMetadata accessibility)
    {
        if (writer == null)
        {
            throw new System.ArgumentNullException("writer");
        }

        if (accessibility == null)
        {
            throw new System.ArgumentNullException("accessibility");
        }

        if (!writer.Configured)
        {
            writer.Configure(new ChartSvgOptions(), accessibility);
        }
        else
        {
            writer.Accessibility = accessibility;
        }
        accessibility.ApplyToSvgWriter(writer);
        return writer.ToSvgString();
    }

    /// <summary>
    /// Replays a retained scene into SVG and embeds the supplied accessibility metadata.
    /// </summary>
    /// <param name="scene">The retained scene that receives or supplies drawing commands.</param>
    /// <param name="accessibility">The title, description, series details, and optional data-table sidecar to embed.</param>
    /// <returns>The converted string.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static string ToSvg(ChartRenderScene scene, ChartAccessibilityMetadata accessibility)
    {
        return ToSvgWithOptions(scene, accessibility, new ChartSvgOptions());
    }

    /// <summary>Replays a retained scene into SVG with accessibility metadata and serialization options.</summary>
    /// <param name="scene">The retained scene that supplies drawing commands.</param>
    /// <param name="accessibility">The title, descriptions, and semantic series and point metadata.</param>
    /// <param name="options">The identifier, role, and CSS-class serialization options.</param>
    /// <returns>The complete SVG document.</returns>
    public static string ToSvgWithOptions(ChartRenderScene scene, ChartAccessibilityMetadata accessibility, ChartSvgOptions options)
    {
        if (scene == null)
        {
            throw new System.ArgumentNullException("scene");
        }

        if (accessibility == null)
        {
            throw new System.ArgumentNullException("accessibility");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        SvgWriter writer = new SvgWriter(scene.Width, scene.Height);
        writer.Configure(options, accessibility);
        accessibility.ApplyToSvgWriter(writer);
        scene.RenderSvg(writer);
        return writer.ToSvgString();
    }

    /// <summary>
    /// Creates an empty SVG document with the requested pixel dimensions.
    /// </summary>
    /// <param name="width">The width in pixels or chart units, which must be positive.</param>
    /// <param name="height">The height in pixels or chart units, which must be positive.</param>
    /// <returns>The converted string.</returns>
    public static string ToSvg(int width, int height)
    {
        SvgWriter writer = new SvgWriter(width, height);
        return writer.ToSvgString();
    }

    internal static byte[] ToSvgBytes(SvgWriter writer)
    {
        return ToSvgBytes(ToSvg(writer));
    }

    /// <summary>
    /// Encodes an SVG document as UTF-8 bytes without a byte-order mark.
    /// </summary>
    /// <param name="svg">The complete SVG document text.</param>
    /// <returns>The converted byte array.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static byte[] ToSvgBytes(string svg)
    {
        if (svg == null)
        {
            throw new System.ArgumentNullException("svg");
        }

        return System.Text.Encoding.UTF8.GetBytes(svg);
    }

    internal static System.IO.MemoryStream ToSvgStream(SvgWriter writer)
    {
        return ToSvgStream(ToSvg(writer));
    }

    /// <summary>
    /// Encodes SVG text as UTF-8 in a new readable memory stream positioned at the start.
    /// </summary>
    /// <param name="svg">The complete SVG document text.</param>
    /// <returns>The converted memory stream.</returns>
    public static System.IO.MemoryStream ToSvgStream(string svg)
    {
        return new System.IO.MemoryStream(ToSvgBytes(svg));
    }

    internal static void WriteSvg(SvgWriter writer, System.IO.Stream stream)
    {
        WriteSvg(ToSvg(writer), stream);
    }

    /// <summary>
    /// Writes UTF-8 SVG bytes to a stream without closing the stream.
    /// </summary>
    /// <param name="svg">The complete SVG document text.</param>
    /// <param name="stream">The writable stream that receives encoded output; it remains open.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void WriteSvg(string svg, System.IO.Stream stream)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        byte[] bytes = ToSvgBytes(svg);
        stream.Write(bytes, 0, bytes.Length);
    }

    internal static void SaveSvg(SvgWriter writer, string path)
    {
        SaveSvg(ToSvg(writer), path);
    }

    /// <summary>
    /// Writes an SVG document to a UTF-8 file.
    /// </summary>
    /// <param name="svg">The complete SVG document text.</param>
    /// <param name="path">The destination file path.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static void SaveSvg(string svg, string path)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        System.IO.File.WriteAllBytes(path, ToSvgBytes(svg));
    }
}
