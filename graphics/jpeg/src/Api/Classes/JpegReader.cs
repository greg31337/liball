namespace jpeg;

/// <summary>Public service for decoding JPEG data into in-memory images.</summary>
public class JpegReader : IJpegReader
{
    /// <summary>Creates an independent JPEG reader.</summary>
    public JpegReader()
    {
    }

    /// <summary>Decodes an image-only abbreviated JPEG byte array with explicitly supplied tables.</summary>
    /// <param name="data">The complete image-only JPEG data.</param>
    /// <param name="tables">The tables required by the image.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadAbbreviatedImage(byte[] data, JpegTableSet tables)
    {
        return new JpegImageReader().ReadAbbreviatedImage(data, tables);
    }

    /// <summary>Decodes an image-only abbreviated JPEG byte array with explicitly supplied tables and options.</summary>
    /// <param name="data">The complete image-only JPEG data.</param>
    /// <param name="tables">The tables required by the image.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadAbbreviatedImageWithOptions(byte[] data, JpegTableSet tables, JpegReadOptions options)
    {
        return new JpegImageReader().ReadAbbreviatedImageWithOptions(data, tables, options);
    }

    /// <summary>Decodes a JPEG byte array.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage Read(byte[] data)
    {
        return new JpegImageReader().Read(data);
    }

    /// <summary>Decodes a JPEG byte array using explicit options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadWithOptions(byte[] data, JpegReadOptions options)
    {
        return new JpegImageReader().ReadWithOptions(data, options);
    }

    /// <summary>Decodes a JPEG byte array while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadWithCancellation(byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadWithCancellation(data, cancellationToken);
    }

    /// <summary>Decodes a JPEG byte array using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadWithOptionsAndCancellation(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Decodes JPEG data from a read-only span.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadSpan(System.ReadOnlySpan<byte> data)
    {
        return new JpegImageReader().ReadSpan(data);
    }

    /// <summary>Decodes JPEG data from a read-only span using explicit options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadSpanWithOptions(System.ReadOnlySpan<byte> data, JpegReadOptions options)
    {
        return new JpegImageReader().ReadSpanWithOptions(data, options);
    }

    /// <summary>Decodes JPEG data from a read-only span while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadSpanWithCancellation(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadSpanWithCancellation(data, cancellationToken);
    }

    /// <summary>Decodes JPEG data from a read-only span using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadSpanWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Decodes JPEG data from a file.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadFile(string path)
    {
        return new JpegImageReader().ReadFile(path);
    }

    /// <summary>Decodes JPEG data from a file using explicit options.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadFileWithOptions(string path, JpegReadOptions options)
    {
        return new JpegImageReader().ReadFileWithOptions(path, options);
    }

    /// <summary>Decodes JPEG data from a file while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadFileWithCancellation(string path, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadFileWithCancellation(path, cancellationToken);
    }

    /// <summary>Decodes JPEG data from a file using explicit options while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadFileWithOptionsAndCancellation(string path, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadFileWithOptionsAndCancellation(path, options, cancellationToken);
    }

    /// <summary>Decodes JPEG data from a stream.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadStream(System.IO.Stream stream)
    {
        return new JpegImageReader().ReadStream(stream);
    }

    /// <summary>Decodes stream data using explicit options.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadStreamWithOptions(System.IO.Stream stream, JpegReadOptions options)
    {
        return new JpegImageReader().ReadStreamWithOptions(stream, options);
    }

    /// <summary>Decodes JPEG data from a stream while observing cancellation.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadStreamWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadStreamWithCancellation(stream, cancellationToken);
    }

    /// <summary>Decodes JPEG data from a stream using explicit options while observing cancellation.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <param name="cancellationToken">The token checked during decoding.</param>
    /// <returns>A newly allocated decoded image.</returns>
    public JpegImage ReadStreamWithOptionsAndCancellation(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadStreamWithOptionsAndCancellation(stream, options, cancellationToken);
    }

    /// <summary>Decodes an image while publishing each packed output row through a reusable buffer.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <param name="callback">The decoded-row callback.</param>
    /// <param name="cancellationToken">The token checked around every callback.</param>
    /// <returns>The decoded image.</returns>
    public JpegImage ReadRowsWithOptionsAndCancellation(byte[] data, JpegReadOptions options, IJpegDecodedRowCallback callback, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadRowsWithOptionsAndCancellation(data, options, callback, cancellationToken);
    }

    /// <summary>Decodes a progressive image while publishing a preview after each scan.</summary>
    /// <param name="data">The complete progressive JPEG data.</param>
    /// <param name="options">Decoding and resource options.</param>
    /// <param name="callback">The progressive-preview callback.</param>
    /// <param name="cancellationToken">The token checked around every scan and callback.</param>
    /// <returns>The final decoded image.</returns>
    public JpegImage ReadProgressivePreviewsWithOptionsAndCancellation(byte[] data, JpegReadOptions options, IJpegProgressivePreviewCallback callback, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegImageReader().ReadProgressivePreviewsWithOptionsAndCancellation(data, options, callback, cancellationToken);
    }
}
