namespace jpeg;

/// <summary>Default service for inspecting JPEG markers and quantized coefficients.</summary>
public class JpegInspectionService : IJpegInspector
{
    /// <summary>Creates an independent JPEG inspection service.</summary>
    public JpegInspectionService()
    {
    }

    /// <summary>Reads tables from an abbreviated table-only JPEG byte array.</summary>
    /// <param name="data">The complete table-only JPEG data.</param>
    /// <returns>The parsed quantization and entropy tables.</returns>
    public JpegTableSet ReadTableStream(byte[] data)
    {
        return new JpegTableSetCodec().ReadTableStream(data);
    }

    /// <summary>Writes tables as an abbreviated table-only JPEG byte array.</summary>
    /// <param name="tables">The quantization and entropy tables to write.</param>
    /// <returns>A newly allocated table-only JPEG byte array.</returns>
    public byte[] WriteTableStream(JpegTableSet tables)
    {
        return new JpegTableSetCodec().WriteTableStream(tables);
    }

    /// <summary>Extracts tables from complete or abbreviated JPEG data.</summary>
    /// <param name="data">Complete, table-only, or image-only JPEG data.</param>
    /// <returns>Independent copies of the parsed table definitions.</returns>
    public JpegTableSet ExtractTables(byte[] data)
    {
        return new JpegTableOperations().ExtractFromBytes(data);
    }

    /// <summary>Replaces table definitions without changing unrelated JPEG bytes.</summary>
    /// <param name="data">Complete, table-only, or image-only JPEG data.</param>
    /// <param name="tables">The replacement quantization and entropy tables.</param>
    /// <returns>JPEG data containing the replacement definitions.</returns>
    public byte[] InjectTables(byte[] data, JpegTableSet tables)
    {
        return new JpegTableOperations().InjectIntoBytes(data, tables);
    }

    /// <summary>Extracts independent copies of coefficient-image tables.</summary>
    /// <param name="image">The coefficient image containing tables.</param>
    /// <returns>The copied quantization and entropy tables.</returns>
    public JpegTableSet ExtractCoefficientTables(JpegCoefficientImage image)
    {
        return new JpegTableOperations().ExtractFromCoefficientImage(image);
    }

    /// <summary>Replaces coefficient-image tables while preserving all other image data.</summary>
    /// <param name="image">The coefficient image to update.</param>
    /// <param name="tables">The replacement quantization and entropy tables.</param>
    /// <returns>The updated coefficient image.</returns>
    public JpegCoefficientImage InjectCoefficientTables(JpegCoefficientImage image, JpegTableSet tables)
    {
        return new JpegTableOperations().InjectIntoCoefficientImage(image, tables);
    }

    /// <summary>Reads marker segments from a JPEG byte array.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>Marker descriptions in stream order.</returns>
    public JpegMarkerSegment[] ReadMarkers(byte[] data)
    {
        return JpegInspector.ReadMarkers(data);
    }

    /// <summary>Reads marker segments from a stream.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <returns>Marker descriptions in stream order.</returns>
    public JpegMarkerSegment[] ReadMarkersStream(System.IO.Stream stream)
    {
        return JpegInspector.ReadMarkers(stream);
    }

    /// <summary>Reads marker segments from a JPEG file.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>Marker descriptions in stream order.</returns>
    public JpegMarkerSegment[] ReadMarkersFile(string path)
    {
        return JpegInspector.ReadMarkersFile(path);
    }

    /// <summary>Inspects markers and parsed structure from a JPEG byte array.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkers(byte[] data)
    {
        return new JpegMarkerInspectionReader().InspectBytes(data, new JpegReadOptions(), default(System.Threading.CancellationToken));
    }

    /// <summary>Inspects a JPEG byte array using explicit read options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersWithOptions(byte[] data, JpegReadOptions options)
    {
        return new JpegMarkerInspectionReader().InspectBytes(data, options, default(System.Threading.CancellationToken));
    }

    /// <summary>Inspects a JPEG byte array while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersWithCancellation(byte[] data, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegMarkerInspectionReader().InspectBytes(data, new JpegReadOptions(), cancellationToken);
    }

    /// <summary>Inspects a JPEG byte array using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersWithOptionsAndCancellation(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegMarkerInspectionReader().InspectBytes(data, options, cancellationToken);
    }

    /// <summary>Inspects markers and parsed structure from a read-only span.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersSpan(System.ReadOnlySpan<byte> data)
    {
        return new JpegMarkerInspectionReader().InspectBytes(data.ToArray(), new JpegReadOptions(), default(System.Threading.CancellationToken));
    }

    /// <summary>Inspects a read-only span using explicit read options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersSpanWithOptions(System.ReadOnlySpan<byte> data, JpegReadOptions options)
    {
        return new JpegMarkerInspectionReader().InspectBytes(data.ToArray(), options, default(System.Threading.CancellationToken));
    }

    /// <summary>Inspects a read-only span while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersSpanWithCancellation(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new JpegMarkerInspectionReader().InspectBytes(data.ToArray(), new JpegReadOptions(), cancellationToken);
    }

    /// <summary>Inspects a read-only span using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new JpegMarkerInspectionReader().InspectBytes(data.ToArray(), options, cancellationToken);
    }

    /// <summary>Inspects markers and parsed structure from a stream.</summary>
    /// <param name="stream">A readable stream positioned at JPEG data.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersStream(System.IO.Stream stream)
    {
        return new JpegMarkerInspectionReader().InspectStream(stream, new JpegReadOptions(), default(System.Threading.CancellationToken));
    }

    /// <summary>Inspects a stream using explicit read options.</summary>
    /// <param name="stream">A readable stream positioned at JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersStreamWithOptions(System.IO.Stream stream, JpegReadOptions options)
    {
        return new JpegMarkerInspectionReader().InspectStream(stream, options, default(System.Threading.CancellationToken));
    }

    /// <summary>Inspects a stream while observing cancellation.</summary>
    /// <param name="stream">A readable stream positioned at JPEG data.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersStreamWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegMarkerInspectionReader().InspectStream(stream, new JpegReadOptions(), cancellationToken);
    }

    /// <summary>Inspects a stream using explicit options while observing cancellation.</summary>
    /// <param name="stream">A readable stream positioned at JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersStreamWithOptionsAndCancellation(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        return new JpegMarkerInspectionReader().InspectStream(stream, options, cancellationToken);
    }

    /// <summary>Inspects markers and parsed structure from a JPEG file.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersFile(string path)
    {
        byte[] data = new JpegIo().ReadFile(path, default(System.Threading.CancellationToken));
        return new JpegMarkerInspectionReader().InspectBytes(data, new JpegReadOptions(), default(System.Threading.CancellationToken));
    }

    /// <summary>Inspects a JPEG file using explicit read options.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersFileWithOptions(string path, JpegReadOptions options)
    {
        byte[] data = new JpegIo().ReadFile(path, default(System.Threading.CancellationToken));
        return new JpegMarkerInspectionReader().InspectBytes(data, options, default(System.Threading.CancellationToken));
    }

    /// <summary>Inspects a JPEG file while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersFileWithCancellation(string path, System.Threading.CancellationToken cancellationToken)
    {
        byte[] data = new JpegIo().ReadFile(path, cancellationToken);
        return new JpegMarkerInspectionReader().InspectBytes(data, new JpegReadOptions(), cancellationToken);
    }

    /// <summary>Inspects a JPEG file using explicit options while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    public JpegInspectionResult InspectMarkersFileWithOptionsAndCancellation(string path, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        byte[] data = new JpegIo().ReadFile(path, cancellationToken);
        return new JpegMarkerInspectionReader().InspectBytes(data, options, cancellationToken);
    }

    /// <summary>Reads quantized coefficients from a JPEG byte array.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>A mutable coefficient image.</returns>
    public JpegCoefficientImage ReadCoefficients(byte[] data)
    {
        return JpegInspector.ReadCoefficients(data);
    }

    /// <summary>Reads quantized coefficients using explicit options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <returns>A mutable coefficient image.</returns>
    public JpegCoefficientImage ReadCoefficientsWithOptions(byte[] data, JpegReadOptions options)
    {
        return JpegInspector.ReadCoefficients(data, options);
    }

    /// <summary>Reads quantized coefficients from a stream.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <returns>A mutable coefficient image.</returns>
    public JpegCoefficientImage ReadCoefficientsStream(System.IO.Stream stream)
    {
        return JpegInspector.ReadCoefficients(stream);
    }

    /// <summary>Reads stream coefficients using explicit options.</summary>
    /// <param name="stream">A readable JPEG stream.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <returns>A mutable coefficient image.</returns>
    public JpegCoefficientImage ReadCoefficientsStreamWithOptions(System.IO.Stream stream, JpegReadOptions options)
    {
        return JpegInspector.ReadCoefficients(stream, options);
    }

    /// <summary>Reads quantized coefficients from a JPEG file.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>A mutable coefficient image.</returns>
    public JpegCoefficientImage ReadCoefficientsFile(string path)
    {
        return JpegInspector.ReadCoefficientsFile(path);
    }

    /// <summary>Reads file coefficients using explicit options.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Parsing and resource options.</param>
    /// <returns>A mutable coefficient image.</returns>
    public JpegCoefficientImage ReadCoefficientsFileWithOptions(string path, JpegReadOptions options)
    {
        return JpegInspector.ReadCoefficientsFile(path, options);
    }

    /// <summary>Writes a coefficient image to a JPEG byte array.</summary>
    /// <param name="image">The coefficient image to write.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    public byte[] WriteCoefficients(JpegCoefficientImage image)
    {
        return JpegInspector.WriteCoefficients(image);
    }

    /// <summary>Writes a coefficient image to a stream.</summary>
    /// <param name="image">The coefficient image to write.</param>
    /// <param name="stream">The writable destination stream.</param>
    public void WriteCoefficientsStream(JpegCoefficientImage image, System.IO.Stream stream)
    {
        JpegInspector.WriteCoefficients(image, stream);
    }

    /// <summary>Writes a coefficient image to a JPEG file.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="image">The coefficient image to write.</param>
    public void WriteCoefficientsFile(string path, JpegCoefficientImage image)
    {
        JpegInspector.WriteCoefficientsFile(path, image);
    }
}
