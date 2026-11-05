namespace barcode;

/// <summary>
/// Holds a parsed GS1 message and its requested serialization.
/// </summary>
public class Gs1ProcessResult
{
    /// <summary>
    /// Indicates whether parsing and requested validation completed without errors.
    /// </summary>
    public bool Success;

    /// <summary>
    /// The parsed GS1 message.
    /// </summary>
    public Gs1Message? Message;

    /// <summary>
    /// The requested textual serialization when applicable.
    /// </summary>
    public string? Text;

    /// <summary>
    /// The requested byte serialization when applicable.
    /// </summary>
    public byte[]? Bytes;

    /// <summary>
    /// The syntax and semantic issues in stable source order.
    /// </summary>
    public BarcodeValidationIssue[]? Issues;
}
