namespace jpeg;

/// <summary>Public service for identifying JPEG structure and metadata without decoding pixels.</summary>
public class JpegIdentifierService : IJpegIdentifier
{
    /// <summary>Creates an independent JPEG identification service.</summary>
    public JpegIdentifierService()
    {
    }

    /// <summary>Reads JPEG structure and metadata from a byte array.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo Identify(byte[] data)
    {
        return new JpegIdentificationService().Identify(data);
    }

    /// <summary>Reads JPEG structure and metadata using explicit options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyWithOptions(byte[] data, JpegReadOptions options)
    {
        return new JpegIdentificationService().IdentifyWithOptions(data, options);
    }

    /// <summary>Reads JPEG structure and metadata from a byte array while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyWithCancellation(byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIdentificationService().IdentifyWithCancellation(data, cancellationToken);
    }

    /// <summary>Reads JPEG structure and metadata from a byte array using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyWithOptionsAndCancellation(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIdentificationService().IdentifyWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Reads JPEG structure and metadata from a read-only span.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifySpan(System.ReadOnlySpan<byte> data)
    {
        return new JpegIdentificationService().IdentifySpan(data);
    }

    /// <summary>Reads JPEG structure and metadata from a read-only span using explicit options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifySpanWithOptions(System.ReadOnlySpan<byte> data, JpegReadOptions options)
    {
        return new JpegIdentificationService().IdentifySpanWithOptions(data, options);
    }

    /// <summary>Reads JPEG structure and metadata from a read-only span while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifySpanWithCancellation(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIdentificationService().IdentifySpanWithCancellation(data, cancellationToken);
    }

    /// <summary>Reads JPEG structure and metadata from a read-only span using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifySpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIdentificationService().IdentifySpanWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Reads JPEG structure and metadata from a file.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyFile(string path)
    {
        return new JpegIdentificationService().IdentifyFile(path);
    }

    /// <summary>Reads JPEG structure and metadata from a file using explicit options.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyFileWithOptions(string path, JpegReadOptions options)
    {
        return new JpegIdentificationService().IdentifyFileWithOptions(path, options);
    }

    /// <summary>Reads JPEG structure and metadata from a file while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyFileWithCancellation(string path, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIdentificationService().IdentifyFileWithCancellation(path, cancellationToken);
    }

    /// <summary>Reads JPEG structure and metadata from a file using explicit options while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyFileWithOptionsAndCancellation(string path, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIdentificationService().IdentifyFileWithOptionsAndCancellation(path, options, cancellationToken);
    }

    /// <summary>Reads JPEG structure and metadata from a stream.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyStream(System.IO.Stream stream)
    {
        return new JpegIdentificationService().IdentifyStream(stream);
    }

    /// <summary>Reads stream structure and metadata using explicit options.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyStreamWithOptions(System.IO.Stream stream, JpegReadOptions options)
    {
        return new JpegIdentificationService().IdentifyStreamWithOptions(stream, options);
    }

    /// <summary>Reads JPEG structure and metadata from a stream while observing cancellation.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyStreamWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIdentificationService().IdentifyStreamWithCancellation(stream, cancellationToken);
    }

    /// <summary>Reads JPEG structure and metadata from a stream using explicit options while observing cancellation.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG stream.</returns>
    public JpegInfo IdentifyStreamWithOptionsAndCancellation(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegIdentificationService().IdentifyStreamWithOptionsAndCancellation(stream, options, cancellationToken);
    }
}
