namespace molecule;


/// <summary>
/// Contains the outcome and diagnostics produced by format detection.
/// </summary>
public class FormatDetectionResult
{
    /// <summary>
    /// The kind.
    /// </summary>
    public FormatKind Kind;
    /// <summary>
    /// The confidence.
    /// </summary>
    public int Confidence;
    /// <summary>
    /// The reason.
    /// </summary>
    public string Reason;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormatDetectionResult"/> class.
    /// </summary>
    public FormatDetectionResult()
    {
        Kind = FormatKind.Unknown;
        Confidence = 0;
        Reason = "";
    }
}

