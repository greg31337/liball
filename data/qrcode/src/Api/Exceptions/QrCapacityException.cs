namespace qrcode;

/// <summary>Represents a payload that exceeds the selected symbol capacity.</summary>
public class QrCapacityException : global::System.Exception
{
    /// <summary>The machine-readable failure category.</summary>
    public QrFailureReason FailureReason = QrFailureReason.CapacityExceeded;

    /// <summary>The QR family whose capacity was evaluated.</summary>
    public QrSymbolKind SymbolKind;

    /// <summary>The number of data bits required by the payload.</summary>
    public int RequiredBitCount;

    /// <summary>The number of data bits available in the selected symbol or sequence.</summary>
    public int AvailableBitCount;

    /// <summary>Initializes a capacity exception with exact fit diagnostics.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="symbolKind">The QR family whose capacity was evaluated.</param>
    /// <param name="requiredBitCount">The number of required data bits.</param>
    /// <param name="availableBitCount">The number of available data bits.</param>
    public QrCapacityException(
        string message,
        QrSymbolKind symbolKind,
        int requiredBitCount,
        int availableBitCount) : base(message)
    {
        SymbolKind = symbolKind;
        RequiredBitCount = requiredBitCount;
        AvailableBitCount = availableBitCount;
    }
}
