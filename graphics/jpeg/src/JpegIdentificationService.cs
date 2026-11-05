namespace jpeg;

/// <summary>Default service for identifying JPEG structure and metadata without decoding pixels.</summary>
internal class JpegIdentificationService : IJpegIdentifier
{
    /// <summary>Creates an independent JPEG identification service.</summary>
    internal JpegIdentificationService()
    {
    }

    /// <summary>Implements byte-array identification for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo Identify(byte[] data)
    {
        return JpegIdentifier.Identify(data);
    }

    /// <summary>Implements byte-array identification with options for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyWithOptions(byte[] data, JpegReadOptions options)
    {
        return JpegIdentifier.Identify(data, options);
    }

    /// <summary>Implements cancellable byte-array identification for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyWithCancellation(byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        return JpegIdentifier.IdentifyBytesWithCancellation(data, cancellationToken);
    }

    /// <summary>Implements cancellable byte-array identification with options for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyWithOptionsAndCancellation(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return JpegIdentifier.IdentifyBytesWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Implements span identification for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifySpan(System.ReadOnlySpan<byte> data)
    {
        return JpegIdentifier.Identify(data.ToArray());
    }

    /// <summary>Implements span identification with options for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifySpanWithOptions(System.ReadOnlySpan<byte> data, JpegReadOptions options)
    {
        return JpegIdentifier.Identify(data.ToArray(), options);
    }

    /// <summary>Implements cancellable span identification for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifySpanWithCancellation(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return JpegIdentifier.IdentifyBytesWithCancellation(data.ToArray(), cancellationToken);
    }

    /// <summary>Implements cancellable span identification with options for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the data.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifySpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        cancellationToken.ThrowIfCancellationRequested();
        return JpegIdentifier.IdentifyBytesWithOptionsAndCancellation(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Implements file identification for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="path">The path of the JPEG file to inspect.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyFile(string path)
    {
        byte[] data = new JpegIo().ReadFile(path, default(System.Threading.CancellationToken));
        return JpegIdentifier.Identify(data);
    }

    /// <summary>Implements file identification with options for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="path">The path of the JPEG file to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the file.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyFileWithOptions(string path, JpegReadOptions options)
    {
        ValidateOptions(options);
        byte[] data = new JpegIo().ReadFile(path, default(System.Threading.CancellationToken));
        return JpegIdentifier.Identify(data, options);
    }

    /// <summary>Implements cancellable file identification for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="path">The path of the JPEG file to inspect.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyFileWithCancellation(string path, System.Threading.CancellationToken cancellationToken)
    {
        byte[] data = new JpegIo().ReadFile(path, cancellationToken);
        return JpegIdentifier.IdentifyBytesWithCancellation(data, cancellationToken);
    }

    /// <summary>Implements cancellable file identification with options for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="path">The path of the JPEG file to inspect.</param>
    /// <param name="options">Limits and policies used while parsing the file.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyFileWithOptionsAndCancellation(string path, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        byte[] data = new JpegIo().ReadFile(path, cancellationToken);
        return JpegIdentifier.IdentifyBytesWithOptionsAndCancellation(data, options, cancellationToken);
    }

    /// <summary>Implements stream identification for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyStream(System.IO.Stream stream)
    {
        byte[] data = new JpegIo().ReadStream(stream, default(System.Threading.CancellationToken));
        return JpegIdentifier.Identify(data);
    }

    /// <summary>Implements stream identification with options for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Limits and policies used while parsing the stream.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyStreamWithOptions(System.IO.Stream stream, JpegReadOptions options)
    {
        ValidateOptions(options);
        byte[] data = new JpegIo().ReadStream(stream, default(System.Threading.CancellationToken));
        return JpegIdentifier.Identify(data, options);
    }

    /// <summary>Implements cancellable stream identification for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyStreamWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        byte[] data = new JpegIo().ReadStream(stream, cancellationToken);
        return JpegIdentifier.IdentifyBytesWithCancellation(data, cancellationToken);
    }

    /// <summary>Implements cancellable stream identification with options for <see cref="IJpegIdentifier"/>.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Limits and policies used while parsing the stream.</param>
    /// <param name="cancellationToken">The token checked during identification.</param>
    /// <returns>Information collected from the JPEG markers and headers.</returns>
    public JpegInfo IdentifyStreamWithOptionsAndCancellation(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        ValidateOptions(options);
        byte[] data = new JpegIo().ReadStream(stream, cancellationToken);
        return JpegIdentifier.IdentifyBytesWithOptionsAndCancellation(data, options, cancellationToken);
    }

    internal void ValidateOptions(JpegReadOptions options)
    {
        new JpegReadOptionsValidator().Validate(options);
    }
}
