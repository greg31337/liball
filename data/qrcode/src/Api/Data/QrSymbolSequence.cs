namespace qrcode;

/// <summary>Contains the symbols and combined payload metadata for a structured-append sequence.</summary>
public class QrSymbolSequence
{
    /// <summary>The symbols in structured-append order.</summary>
    public QrSymbol[] Symbols = new QrSymbol[0];

    /// <summary>The shared structured-append metadata, or <c>null</c> when unavailable.</summary>
    public QrStructuredAppendInfo? StructuredAppend;

    /// <summary>The lossless combined payload bytes.</summary>
    public byte[] RawBytes = new byte[0];

    /// <summary>The combined text, or <c>null</c> when unavailable or not representable.</summary>
    public string? Text;

    /// <summary>Whether every declared part is present and consistent.</summary>
    public bool IsComplete;

    /// <summary>The machine-readable sequence failure, or <see cref="QrFailureReason.None"/> when valid.</summary>
    public QrFailureReason FailureReason;
}
