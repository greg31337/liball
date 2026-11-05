namespace barcode;

/// <summary>
/// Holds one decoded payload with its exact format, location, orientation, and metadata.
/// </summary>
public class BarcodeDecodeResult
{
    /// <summary>
    /// The decoded payload.
    /// </summary>
    public BarcodePayload? Payload;

    /// <summary>
    /// The exact observed format or application profile.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The dimensional organization of the decoded symbol.
    /// </summary>
    public BarcodeDimension Dimension;

    /// <summary>
    /// The decoded symbol location in source-image coordinates.
    /// </summary>
    public BarcodeQuadrilateral? Location;

    /// <summary>
    /// The recovered right-angle rotation.
    /// </summary>
    public BarcodeRotation Rotation;

    /// <summary>
    /// The recovered clockwise orientation in degrees.
    /// </summary>
    public double OrientationDegrees;

    /// <summary>
    /// The estimated skew in degrees.
    /// </summary>
    public double SkewDegrees;

    /// <summary>
    /// The recovered symbol polarity.
    /// </summary>
    public BarcodePolarity Polarity;

    /// <summary>
    /// The recovered mirrored orientation.
    /// </summary>
    public BarcodeMirrorMode MirrorMode;

    /// <summary>
    /// The checksum, correction, identifier, confidence, and GS1 metadata.
    /// </summary>
    public BarcodeDecodeMetadata? Metadata;

    /// <summary>
    /// The diagnostics specifically associated with this result.
    /// </summary>
    public BarcodeDiagnostic[]? Diagnostics;
}
