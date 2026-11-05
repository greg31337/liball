namespace barcode;

/// <summary>
/// Holds decoded structured-append segments and assembly policy.
/// </summary>
public class StructuredAppendRequest
{
    /// <summary>
    /// The expected structured-append format or application profile.
    /// </summary>
    public BarcodeFormat Format;

    /// <summary>
    /// The decoded segments in caller-supplied order.
    /// </summary>
    public BarcodeDecodeResult[]? Segments;

    /// <summary>
    /// Indicates whether assembly must fail when any segment is missing.
    /// </summary>
    public bool RequireComplete;

    /// <summary>
    /// Indicates whether identical duplicate segments are accepted.
    /// </summary>
    public bool AllowIdenticalDuplicates;
}
