namespace uspto;

/// <summary>
/// Reports that a downloaded or mirrored file failed checksum verification.
/// </summary>
public class ChecksumException : global::System.Exception
{
    /// <summary>The checksum algorithm used for verification.</summary>
    public ChecksumAlgorithm Algorithm;

    /// <summary>The normalized expected checksum text.</summary>
    public string? ExpectedChecksum;

    /// <summary>The normalized checksum calculated from local bytes.</summary>
    public string? ActualChecksum;

    /// <summary>The local file path associated with the failure.</summary>
    public string? FilePath;

    /// <summary>Initializes a checksum exception and its verification context.</summary>
    /// <param name="message">The checksum failure message.</param>
    /// <param name="algorithm">The algorithm used for verification.</param>
    /// <param name="expectedChecksum">The normalized expected checksum.</param>
    /// <param name="actualChecksum">The normalized calculated checksum.</param>
    /// <param name="filePath">The local file path associated with the failure.</param>
    /// <param name="innerException">The lower-level cause, or <c>null</c>.</param>
    public ChecksumException(string message, ChecksumAlgorithm algorithm, string? expectedChecksum, string? actualChecksum, string? filePath, global::System.Exception? innerException)
        : base(message, innerException)
    {
        Algorithm = algorithm;
        ExpectedChecksum = expectedChecksum;
        ActualChecksum = actualChecksum;
        FilePath = filePath;
    }
}
