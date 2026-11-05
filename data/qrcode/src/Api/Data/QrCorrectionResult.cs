namespace qrcode;

/// <summary>Contains repaired codewords and error-correction diagnostics.</summary>
public class QrCorrectionResult
{
    /// <summary>Whether error correction completed and the repaired block was verified.</summary>
    public bool Success;

    /// <summary>The machine-readable failure reason, or <see cref="QrFailureReason.None"/> on success.</summary>
    public QrFailureReason FailureReason;

    /// <summary>The repaired codewords, or the original codewords when no repair was required.</summary>
    public byte[] Codewords = new byte[0];

    /// <summary>The number of parity codewords used for this Reed-Solomon block.</summary>
    public int ErrorCorrectionCodewordCount;

    /// <summary>The number of corrected errors at previously unknown locations.</summary>
    public int CorrectedErrorCount;

    /// <summary>The number of corrected caller-supplied erasures.</summary>
    public int CorrectedErasureCount;

    /// <summary>The calculated syndromes in polynomial order.</summary>
    public int[] Syndromes = new int[0];

    /// <summary>Whether a final syndrome calculation verified the repaired codewords.</summary>
    public bool Verified;
}
