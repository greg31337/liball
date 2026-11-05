namespace qrcode;

/// <summary>Represents a detection failure that should be reported as an exception.</summary>
public class QrDetectionException : global::System.Exception
{
    /// <summary>The machine-readable detection failure category.</summary>
    public QrFailureReason FailureReason;

    /// <summary>The number of candidates examined before failure.</summary>
    public int CandidateCount;

    /// <summary>The number of preprocessing or geometry attempts performed.</summary>
    public int AttemptCount;

    /// <summary>Initializes a detection exception with candidate diagnostics.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="failureReason">The machine-readable detection failure category.</param>
    /// <param name="candidateCount">The number of candidates examined.</param>
    /// <param name="attemptCount">The number of processing attempts performed.</param>
    public QrDetectionException(
        string message,
        QrFailureReason failureReason,
        int candidateCount,
        int attemptCount) : base(message)
    {
        FailureReason = failureReason;
        CandidateCount = candidateCount;
        AttemptCount = attemptCount;
    }
}
