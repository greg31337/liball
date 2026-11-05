namespace qrcode;

/// <summary>Contains an exception-less decode attempt and its available payload diagnostics.</summary>
public class QrAttemptResult
{
    /// <summary>Whether the decode attempt succeeded.</summary>
    public bool Success;

    /// <summary>The machine-readable failure reason, or <see cref="QrFailureReason.None"/> on success.</summary>
    public QrFailureReason FailureReason;

    /// <summary>The complete decoded result, or <c>null</c> when decoding did not succeed.</summary>
    public QrDecodeResult? Result;

    /// <summary>The decoded text, or <c>null</c> when unavailable or not representable.</summary>
    public string? Text;

    /// <summary>The lossless decoded payload bytes available from the attempt.</summary>
    public byte[] RawBytes = new byte[0];

    /// <summary>The decoded payload and control segments available from the attempt.</summary>
    public QrSegment[] Segments = new QrSegment[0];

    /// <summary>The number of candidate symbols examined.</summary>
    public int CandidateCount;

    /// <summary>The number of bounded recovery attempts performed.</summary>
    public int AttemptCount;

    /// <summary>The number of corrected codeword errors.</summary>
    public int CorrectedErrorCount;

    /// <summary>The number of corrected erasures.</summary>
    public int CorrectedErasureCount;
}
