namespace qrcode;

/// <summary>Contains a complete structured-append assembly or sequence failure diagnostics.</summary>
public class QrStructuredAppendAttemptResult
{
    /// <summary>Whether the sequence was assembled successfully.</summary>
    public bool Success;

    /// <summary>The sequence failure category, or <see cref="QrFailureReason.None"/> on success.</summary>
    public QrFailureReason FailureReason;

    /// <summary>The complete assembled payload, or <c>null</c> on failure. Partial payloads are never returned.</summary>
    public QrDecodeResult? Result;

    /// <summary>The declared part count when one sequence identity is known, or zero otherwise.</summary>
    public int ExpectedPartCount;

    /// <summary>The number of distinct received positions for that identity, including positions with conflicting observations.</summary>
    public int ActualPartCount;

    /// <summary>The parity for the identified sequence, or -1 when the identity is unavailable or ambiguous.</summary>
    public int Parity = -1;

    /// <summary>Missing zero-based positions in ascending order, when part ordering was possible.</summary>
    public int[] MissingIndices = new int[0];

    /// <summary>Zero-based positions with incompatible duplicate payloads in ascending order.</summary>
    public int[] ConflictingIndices = new int[0];

    /// <summary>The number of additional observations at already received positions, including conflicting observations.</summary>
    public int DuplicateCount;
}
