namespace qrcode;

/// <summary>Represents a payload or symbol decoding failure.</summary>
public class QrDecodeException : global::System.Exception
{
    /// <summary>The machine-readable decode failure category.</summary>
    public QrFailureReason FailureReason;

    /// <summary>The zero-based detection candidate index, or -1 for direct matrix decoding.</summary>
    public int CandidateIndex;

    /// <summary>The number of bounded recovery attempts performed.</summary>
    public int RecoveryAttemptCount;

    /// <summary>Initializes a decode exception with candidate and recovery diagnostics.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="failureReason">The machine-readable decode failure category.</param>
    /// <param name="candidateIndex">The detection candidate index, or -1.</param>
    /// <param name="recoveryAttemptCount">The number of recovery attempts performed.</param>
    public QrDecodeException(
        string message,
        QrFailureReason failureReason,
        int candidateIndex,
        int recoveryAttemptCount) : base(message)
    {
        FailureReason = failureReason;
        CandidateIndex = candidateIndex;
        RecoveryAttemptCount = recoveryAttemptCount;
    }
}
