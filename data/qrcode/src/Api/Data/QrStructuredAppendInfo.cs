namespace qrcode;

/// <summary>Contains metadata that identifies a structured-append sequence part.</summary>
public class QrStructuredAppendInfo
{
    /// <summary>The zero-based position of this part in the sequence.</summary>
    public int Index;

    /// <summary>The declared total number of parts in the sequence.</summary>
    public int Total;

    /// <summary>The eight-bit parity value shared by the sequence.</summary>
    public int Parity;

    /// <summary>An optional application identity used to distinguish collected sequences.</summary>
    public string? ApplicationIdentifier;

    /// <summary>Whether all declared sequence parts are present and consistent.</summary>
    public bool IsComplete;
}
