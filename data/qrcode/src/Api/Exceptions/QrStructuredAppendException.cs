namespace qrcode;

/// <summary>Represents an incomplete or conflicting structured-append sequence.</summary>
public class QrStructuredAppendException : global::System.Exception
{
    /// <summary>The machine-readable sequence failure category.</summary>
    public QrFailureReason FailureReason;

    /// <summary>The number of parts declared by the sequence.</summary>
    public int ExpectedPartCount;

    /// <summary>The number of distinct valid parts received.</summary>
    public int ActualPartCount;

    /// <summary>The sequence parity, or -1 when conflicting parity values were received.</summary>
    public int Parity;

    /// <summary>Initializes a structured-append exception with sequence diagnostics.</summary>
    /// <param name="message">The human-readable failure message.</param>
    /// <param name="failureReason">The machine-readable sequence failure category.</param>
    /// <param name="expectedPartCount">The declared number of parts.</param>
    /// <param name="actualPartCount">The received number of distinct valid parts.</param>
    /// <param name="parity">The sequence parity, or -1 when conflicting.</param>
    public QrStructuredAppendException(
        string message,
        QrFailureReason failureReason,
        int expectedPartCount,
        int actualPartCount,
        int parity) : base(message)
    {
        FailureReason = failureReason;
        ExpectedPartCount = expectedPartCount;
        ActualPartCount = actualPartCount;
        Parity = parity;
    }
}
