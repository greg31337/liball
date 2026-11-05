namespace barcode;

/// <summary>
/// Holds a localized barcode candidate and its geometric and format evidence.
/// </summary>
public class BarcodeCandidate
{
    /// <summary>
    /// The most likely barcode format.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The most likely dimensional organization.
    /// </summary>
    public BarcodeDimension Dimension;

    /// <summary>
    /// The candidate location in source-image coordinates.
    /// </summary>
    public BarcodeQuadrilateral? Location;

    /// <summary>
    /// The estimated clockwise orientation in degrees.
    /// </summary>
    public double AngleDegrees;

    /// <summary>
    /// The heuristic localization confidence from zero through one.
    /// </summary>
    public double Confidence;

    /// <summary>
    /// The estimated symbol polarity.
    /// </summary>
    public BarcodePolarity Polarity;

    /// <summary>
    /// The estimated mirrored orientation.
    /// </summary>
    public BarcodeMirrorMode MirrorMode;

    /// <summary>
    /// The diagnostics specifically associated with this candidate.
    /// </summary>
    public BarcodeDiagnostic[]? Diagnostics;
}
