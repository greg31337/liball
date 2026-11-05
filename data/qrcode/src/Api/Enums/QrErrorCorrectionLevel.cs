namespace qrcode;

/// <summary>Identifies the error-correction level selected for a symbol.</summary>
public enum QrErrorCorrectionLevel
{
    /// <summary>No error-correction level applies.</summary>
    None = -1,

    /// <summary>Low error correction.</summary>
    Low = 0,

    /// <summary>Medium error correction.</summary>
    Medium = 1,

    /// <summary>Quartile error correction.</summary>
    Quartile = 2,

    /// <summary>High error correction.</summary>
    High = 3
}
