namespace qrcode;

/// <summary>Contains an exception-less encode attempt and its capacity diagnostics.</summary>
public class QrEncodeAttemptResult
{
    /// <summary>Whether the encode attempt succeeded.</summary>
    public bool Success;

    /// <summary>The machine-readable failure reason, or <see cref="QrFailureReason.None"/> on success.</summary>
    public QrFailureReason FailureReason;

    /// <summary>The encoded symbol, or <c>null</c> for failure or structured append.</summary>
    public QrSymbol? Symbol;

    /// <summary>The encoded structured-append sequence, or <c>null</c> otherwise.</summary>
    public QrSymbolSequence? Sequence;

    /// <summary>The encoded matrix for a single-symbol result, or <c>null</c> otherwise.</summary>
    public QrMatrix? Matrix;

    /// <summary>The encoded matrices for a structured-append result.</summary>
    public QrMatrix[] SequenceMatrices = new QrMatrix[0];

    /// <summary>The number of data bits required by the request.</summary>
    public int RequiredBitCount;

    /// <summary>The number of data bits available in the selected symbol or sequence.</summary>
    public int AvailableBitCount;
}
