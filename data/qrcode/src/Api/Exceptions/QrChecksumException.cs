namespace qrcode;

/// <summary>Represents error-correction data that could not be verified or repaired.</summary>
public class QrChecksumException : global::System.Exception
{
    /// <summary>The machine-readable failure category.</summary>
    public QrFailureReason FailureReason = QrFailureReason.ChecksumFailure;

    /// <summary>The number of error-correction codewords available to the block.</summary>
    public int ErrorCorrectionCodewordCount;

    /// <summary>The number of error locations found before failure.</summary>
    public int ErrorCount;

    /// <summary>The number of caller-supplied erasures.</summary>
    public int ErasureCount;

    /// <summary>Initializes a checksum exception with repair-limit diagnostics.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="errorCorrectionCodewordCount">The number of available error-correction codewords.</param>
    /// <param name="errorCount">The number of error locations found.</param>
    /// <param name="erasureCount">The number of supplied erasures.</param>
    public QrChecksumException(
        string message,
        int errorCorrectionCodewordCount,
        int errorCount,
        int erasureCount) : base(message)
    {
        ErrorCorrectionCodewordCount = errorCorrectionCodewordCount;
        ErrorCount = errorCount;
        ErasureCount = erasureCount;
    }
}
