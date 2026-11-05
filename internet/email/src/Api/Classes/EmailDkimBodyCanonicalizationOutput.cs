using System;

namespace email;
/// <summary>
/// Represents a DKIM body canonicalization output.
/// </summary>
public class EmailDkimBodyCanonicalizationOutput : IEmailDkimBodyCanonicalizationOutputContract
{
    byte[] canonicalizedBody;
    EmailDkimBodyCanonicalizationResult result;
    /// <summary>
    /// Creates a new DKIM body canonicalization output.
    /// </summary>
    /// <param name = "canonicalizedBody">The canonicalized body used by the operation.</param>
    /// <param name = "result">The result used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimBodyCanonicalizationOutput(byte[] canonicalizedBody, EmailDkimBodyCanonicalizationResult result)
    {
        if (canonicalizedBody == null)
            throw new ArgumentNullException("canonicalizedBody");
        if (result == null)
            throw new ArgumentNullException("result");
        if (canonicalizedBody.LongLength != result.GetCanonicalizedByteCount())
            throw new ArgumentException("The canonicalized body length does not match the result metadata.", "canonicalizedBody");
        this.canonicalizedBody = Copy(canonicalizedBody);
        this.result = result;
    }

    /// <summary>
    /// Gets the canonicalized body.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current canonicalized body.</returns>
    public byte[] GetCanonicalizedBody()
    {
        return Copy(canonicalizedBody);
    }

    /// <summary>
    /// Gets the result.
    /// </summary>
    /// <returns>The current result.</returns>
    public EmailDkimBodyCanonicalizationResult GetResult()
    {
        return result;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
