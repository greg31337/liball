namespace iso20022;

/// <summary>Calculates and optionally verifies deterministic byte, canonical XML, or semantic message checksums.</summary>
public interface IMessageChecksumCalculator
{
    /// <summary>Calculates one bounded checksum and compares it with an expected value when supplied.</summary>
    /// <param name="request">The message representation, checksum mode, scope, algorithm, and limits.</param>
    /// <returns>The stable checksum result.</returns>
    ChecksumResult CalculateChecksum(ChecksumRequest? request);
}
