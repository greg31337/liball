namespace jpeg;

/// <summary>Public service for encoding in-memory images as JPEG data.</summary>
public class JpegWriter : IJpegWriter
{
    /// <summary>Creates an independent JPEG writer.</summary>
    public JpegWriter()
    {
    }

    /// <summary>Encodes an image-only abbreviated JPEG byte array with explicitly supplied tables.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="tables">The quantization and Huffman tables used by the encoder.</param>
    /// <returns>A newly allocated image-only JPEG byte array.</returns>
    public byte[] WriteAbbreviatedImage(JpegImage image, JpegTableSet tables)
    {
        return new JpegImageWriter().WriteAbbreviatedImage(image, tables);
    }

    /// <summary>Encodes an image-only abbreviated JPEG byte array with explicitly supplied tables and options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="tables">The quantization and Huffman tables used by the encoder.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>A newly allocated image-only JPEG byte array.</returns>
    public byte[] WriteAbbreviatedImageWithOptions(JpegImage image, JpegTableSet tables, JpegWriteOptions options)
    {
        return new JpegImageWriter().WriteAbbreviatedImageWithOptions(image, tables, options);
    }

    /// <summary>Encodes an image using default options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] Write(JpegImage image)
    {
        return new JpegImageWriter().Write(image);
    }

    /// <summary>Encodes an image using explicit options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] WriteWithOptions(JpegImage image, JpegWriteOptions options)
    {
        return new JpegImageWriter().WriteWithOptions(image, options);
    }

    /// <summary>Encodes an image and reports the concrete settings selected after preset and automatic-option resolution.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>The encoded bytes and resolved settings.</returns>
    public JpegWriteResult WriteWithResult(JpegImage image, JpegWriteOptions options)
    {
        return new JpegImageWriter().WriteWithResult(image, options);
    }

    /// <summary>Encodes an image while observing cancellation.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] WriteWithCancellation(JpegImage image, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageWriter().WriteWithCancellation(image, cancellationToken);
    }

    /// <summary>Encodes an image using explicit options while observing cancellation.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] WriteWithOptionsAndCancellation(JpegImage image, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageWriter().WriteWithOptionsAndCancellation(image, options, cancellationToken);
    }

    /// <summary>Encodes an image into a caller-provided span.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="destination">The destination for the complete JPEG data.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteToSpan(JpegImage image, System.Span<byte> destination)
    {
        return new JpegImageWriter().WriteToSpan(image, destination);
    }

    /// <summary>Encodes an image into a caller-provided span using explicit options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="destination">The destination for the complete JPEG data.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteToSpanWithOptions(JpegImage image, System.Span<byte> destination, JpegWriteOptions options)
    {
        return new JpegImageWriter().WriteToSpanWithOptions(image, destination, options);
    }

    /// <summary>Encodes an image into a caller-provided span while observing cancellation.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="destination">The destination for the complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteToSpanWithCancellation(JpegImage image, System.Span<byte> destination, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageWriter().WriteToSpanWithCancellation(image, destination, cancellationToken);
    }

    /// <summary>Encodes an image into a caller-provided span using explicit options while observing cancellation.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="destination">The destination for the complete JPEG data.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    /// <returns>The number of bytes written.</returns>
    public int WriteToSpanWithOptionsAndCancellation(JpegImage image, System.Span<byte> destination, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageWriter().WriteToSpanWithOptionsAndCancellation(image, destination, options, cancellationToken);
    }

    /// <summary>Encodes an image to a file using default options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    public void WriteFile(JpegImage image, string path)
    {
        new JpegImageWriter().WriteFile(image, path);
    }

    /// <summary>Encodes an image to a file using explicit options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">JPEG encoding options.</param>
    public void WriteFileWithOptions(JpegImage image, string path, JpegWriteOptions options)
    {
        new JpegImageWriter().WriteFileWithOptions(image, path, options);
    }

    /// <summary>Encodes an image to a file while observing cancellation.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    public void WriteFileWithCancellation(JpegImage image, string path, System.Threading.CancellationToken cancellationToken)
    {
        new JpegImageWriter().WriteFileWithCancellation(image, path, cancellationToken);
    }

    /// <summary>Encodes an image to a file using explicit options while observing cancellation.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    public void WriteFileWithOptionsAndCancellation(JpegImage image, string path, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        new JpegImageWriter().WriteFileWithOptionsAndCancellation(image, path, options, cancellationToken);
    }

    /// <summary>Encodes an image to a stream using default options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    public void WriteToStream(JpegImage image, System.IO.Stream stream)
    {
        new JpegImageWriter().WriteToStream(image, stream);
    }

    /// <summary>Encodes an image to a stream using explicit options.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">JPEG encoding options.</param>
    public void WriteToStreamWithOptions(JpegImage image, System.IO.Stream stream, JpegWriteOptions options)
    {
        new JpegImageWriter().WriteToStreamWithOptions(image, stream, options);
    }

    /// <summary>Encodes an image to a stream while observing cancellation.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    public void WriteToStreamWithCancellation(JpegImage image, System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        new JpegImageWriter().WriteToStreamWithCancellation(image, stream, cancellationToken);
    }

    /// <summary>Encodes an image to a stream using explicit options while observing cancellation.</summary>
    /// <param name="image">The image to encode.</param>
    /// <param name="stream">The writable destination stream.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <param name="cancellationToken">The token checked during encoding.</param>
    public void WriteToStreamWithOptionsAndCancellation(JpegImage image, System.IO.Stream stream, JpegWriteOptions options, System.Threading.CancellationToken cancellationToken)
    {
        new JpegImageWriter().WriteToStreamWithOptionsAndCancellation(image, stream, options, cancellationToken);
    }

    /// <summary>Requests tightly packed source rows through a reusable buffer and encodes them.</summary>
    /// <param name="width">The image width in pixels.</param>
    /// <param name="height">The image height in pixels.</param>
    /// <param name="pixelFormat">The packed source pixel layout.</param>
    /// <param name="colorSpace">The interpreted source color space.</param>
    /// <param name="options">JPEG encoding options.</param>
    /// <param name="callback">The callback that fills each requested row.</param>
    /// <param name="cancellationToken">The token checked around every callback.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] WriteRowsWithOptionsAndCancellation(int width, int height, JpegPixelFormat pixelFormat, JpegColorSpace colorSpace, JpegWriteOptions options, IJpegEncodeRowCallback callback, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageWriter().WriteRowsWithOptionsAndCancellation(width, height, pixelFormat, colorSpace, options, callback, cancellationToken);
    }
}
