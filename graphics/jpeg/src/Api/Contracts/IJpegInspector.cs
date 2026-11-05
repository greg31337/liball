namespace jpeg;

/// <summary>Provides advanced access to JPEG markers and quantized DCT coefficients.</summary>
public interface IJpegInspector
{
    /// <summary>Reads the tables from an abbreviated table-only JPEG byte array.</summary>
    /// <param name="data">The complete table-only JPEG data.</param>
    /// <returns>The parsed quantization and entropy tables.</returns>
    JpegTableSet ReadTableStream(byte[] data);

    /// <summary>Writes tables as an abbreviated table-only JPEG byte array.</summary>
    /// <param name="tables">The quantization and entropy tables to write.</param>
    /// <returns>A newly allocated table-only JPEG byte array.</returns>
    byte[] WriteTableStream(JpegTableSet tables);

    /// <summary>Extracts all quantization and entropy tables from complete or abbreviated JPEG data.</summary>
    /// <param name="data">Complete, table-only, or image-only JPEG data.</param>
    /// <returns>Independent copies of the parsed table definitions.</returns>
    JpegTableSet ExtractTables(byte[] data);

    /// <summary>Replaces table definitions in complete or abbreviated JPEG data without re-encoding entropy data.</summary>
    /// <param name="data">Complete, table-only, or image-only JPEG data.</param>
    /// <param name="tables">The replacement quantization and entropy tables.</param>
    /// <returns>JPEG data with the replacement definitions and all unrelated bytes preserved.</returns>
    byte[] InjectTables(byte[] data, JpegTableSet tables);

    /// <summary>Extracts independent copies of every table in a coefficient image.</summary>
    /// <param name="image">The coefficient image containing tables.</param>
    /// <returns>The copied quantization and entropy tables.</returns>
    JpegTableSet ExtractCoefficientTables(JpegCoefficientImage image);

    /// <summary>Replaces the tables in a coefficient image while preserving its coefficients and other data.</summary>
    /// <param name="image">The coefficient image to update.</param>
    /// <param name="tables">The replacement quantization and entropy tables.</param>
    /// <returns>The updated coefficient image.</returns>
    JpegCoefficientImage InjectCoefficientTables(JpegCoefficientImage image, JpegTableSet tables);

    /// <summary>Reads every marker segment from a JPEG byte array.</summary>
    /// <param name="data">The complete JPEG data to inspect.</param>
    /// <returns>Marker descriptions in stream order.</returns>
    JpegMarkerSegment[] ReadMarkers(byte[] data);

    /// <summary>Reads every marker segment from a stream.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>Marker descriptions in stream order.</returns>
    JpegMarkerSegment[] ReadMarkersStream(System.IO.Stream stream);

    /// <summary>Reads every marker segment from a JPEG file.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>Marker descriptions in stream order.</returns>
    JpegMarkerSegment[] ReadMarkersFile(string path);

    /// <summary>Inspects markers and parsed structure from a JPEG byte array.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkers(byte[] data);

    /// <summary>Inspects a JPEG byte array using explicit read options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersWithOptions(byte[] data, JpegReadOptions options);

    /// <summary>Inspects a JPEG byte array while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersWithCancellation(byte[] data, System.Threading.CancellationToken cancellationToken);

    /// <summary>Inspects a JPEG byte array using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersWithOptionsAndCancellation(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Inspects markers and parsed structure from a read-only span.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersSpan(System.ReadOnlySpan<byte> data);

    /// <summary>Inspects a read-only span using explicit read options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersSpanWithOptions(System.ReadOnlySpan<byte> data, JpegReadOptions options);

    /// <summary>Inspects a read-only span while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersSpanWithCancellation(System.ReadOnlySpan<byte> data, System.Threading.CancellationToken cancellationToken);

    /// <summary>Inspects a read-only span using explicit options while observing cancellation.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersSpanWithOptionsAndCancellation(System.ReadOnlySpan<byte> data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Inspects markers and parsed structure from a stream.</summary>
    /// <param name="stream">A readable stream positioned at JPEG data.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersStream(System.IO.Stream stream);

    /// <summary>Inspects a stream using explicit read options.</summary>
    /// <param name="stream">A readable stream positioned at JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersStreamWithOptions(System.IO.Stream stream, JpegReadOptions options);

    /// <summary>Inspects a stream while observing cancellation.</summary>
    /// <param name="stream">A readable stream positioned at JPEG data.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersStreamWithCancellation(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken);

    /// <summary>Inspects a stream using explicit options while observing cancellation.</summary>
    /// <param name="stream">A readable stream positioned at JPEG data.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersStreamWithOptionsAndCancellation(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Inspects markers and parsed structure from a JPEG file.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersFile(string path);

    /// <summary>Inspects a JPEG file using explicit read options.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersFileWithOptions(string path, JpegReadOptions options);

    /// <summary>Inspects a JPEG file while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersFileWithCancellation(string path, System.Threading.CancellationToken cancellationToken);

    /// <summary>Inspects a JPEG file using explicit options while observing cancellation.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Options controlling parsing, diagnostics, metadata, and limits.</param>
    /// <param name="cancellationToken">The token checked while reading markers.</param>
    /// <returns>The ordered markers and parsed inspection result.</returns>
    JpegInspectionResult InspectMarkersFileWithOptionsAndCancellation(string path, JpegReadOptions options, System.Threading.CancellationToken cancellationToken);

    /// <summary>Reads quantized DCT coefficients from a JPEG byte array.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <returns>A mutable coefficient image.</returns>
    JpegCoefficientImage ReadCoefficients(byte[] data);

    /// <summary>Reads quantized DCT coefficients using explicit options.</summary>
    /// <param name="data">The complete JPEG data.</param>
    /// <param name="options">Options controlling parsing, metadata, and limits.</param>
    /// <returns>A mutable coefficient image.</returns>
    JpegCoefficientImage ReadCoefficientsWithOptions(byte[] data, JpegReadOptions options);

    /// <summary>Reads quantized DCT coefficients from a stream.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <returns>A mutable coefficient image.</returns>
    JpegCoefficientImage ReadCoefficientsStream(System.IO.Stream stream);

    /// <summary>Reads quantized DCT coefficients from a stream using explicit options.</summary>
    /// <param name="stream">A readable stream positioned at the start of JPEG data.</param>
    /// <param name="options">Options controlling parsing, metadata, and limits.</param>
    /// <returns>A mutable coefficient image.</returns>
    JpegCoefficientImage ReadCoefficientsStreamWithOptions(System.IO.Stream stream, JpegReadOptions options);

    /// <summary>Reads quantized DCT coefficients from a JPEG file.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <returns>A mutable coefficient image.</returns>
    JpegCoefficientImage ReadCoefficientsFile(string path);

    /// <summary>Reads quantized DCT coefficients from a JPEG file using explicit options.</summary>
    /// <param name="path">The path of the JPEG file.</param>
    /// <param name="options">Options controlling parsing, metadata, and limits.</param>
    /// <returns>A mutable coefficient image.</returns>
    JpegCoefficientImage ReadCoefficientsFileWithOptions(string path, JpegReadOptions options);

    /// <summary>Writes a coefficient image as a complete JPEG byte array.</summary>
    /// <param name="image">A coefficient image previously read by this inspector.</param>
    /// <returns>A newly allocated JPEG byte array.</returns>
    byte[] WriteCoefficients(JpegCoefficientImage image);

    /// <summary>Writes a coefficient image to a stream.</summary>
    /// <param name="image">A coefficient image previously read by this inspector.</param>
    /// <param name="stream">The writable destination stream.</param>
    void WriteCoefficientsStream(JpegCoefficientImage image, System.IO.Stream stream);

    /// <summary>Writes a coefficient image to a JPEG file.</summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="image">A coefficient image previously read by this inspector.</param>
    void WriteCoefficientsFile(string path, JpegCoefficientImage image);
}
