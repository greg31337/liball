namespace molecule;


/// <summary>
/// Holds an optional estimated descriptor value and a diagnostic when no validated estimate is available.
/// </summary>
public class DescriptorEstimate
{
    /// <summary>
    /// The estimated numeric value when available.
    /// </summary>
    public double Value;
    /// <summary>
    /// Whether a validated estimate is available.
    /// </summary>
    public bool Available;
    /// <summary>
    /// The stable diagnostic code explaining why the estimate is unavailable.
    /// </summary>
    public string DiagnosticCode;
    /// <summary>
    /// The human-readable diagnostic explaining why the estimate is unavailable.
    /// </summary>
    public string DiagnosticMessage;

    /// <summary>
    /// Initializes an unavailable descriptor estimate.
    /// </summary>
    public DescriptorEstimate()
    {
        Value = 0.0;
        Available = false;
        DiagnosticCode = "";
        DiagnosticMessage = "";
    }
}
