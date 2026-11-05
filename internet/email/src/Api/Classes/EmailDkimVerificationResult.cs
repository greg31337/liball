using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a DKIM verification operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDkimVerificationResult : IEmailDkimVerificationResultContract
{
    List<EmailDkimSignatureVerificationResult> signatures;
    /// <summary>
    /// Creates a new DKIM verification result.
    /// </summary>
    /// <param name = "signatures">The signatures used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailDkimVerificationResult(List<EmailDkimSignatureVerificationResult> signatures)
    {
        if (signatures == null)
            throw new ArgumentNullException("signatures");
        this.signatures = new List<EmailDkimSignatureVerificationResult>(signatures);
    }

    /// <summary>
    /// Gets the signature count.
    /// </summary>
    /// <returns>The current signature count.</returns>
    public int GetSignatureCount()
    {
        return signatures.Count;
    }

    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current signature.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDkimSignatureVerificationResult GetSignature(int index)
    {
        if (index < 0 || index >= signatures.Count)
            throw new ArgumentOutOfRangeException("index");
        return signatures[index];
    }

    /// <summary>
    /// Gets the signatures.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signatures.</returns>
    public List<EmailDkimSignatureVerificationResult> GetSignatures()
    {
        return new List<EmailDkimSignatureVerificationResult>(signatures);
    }

    /// <summary>
    /// Gets the passing signature count.
    /// </summary>
    /// <returns>The current passing signature count.</returns>
    public int GetPassingSignatureCount()
    {
        int count = 0;
        int index = 0;
        while (index < signatures.Count)
        {
            if (signatures[index].IsSuccess())
                count++;
            index++;
        }

        return count;
    }

    /// <summary>
    /// Determines whether the passing signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the passing signature is available; otherwise, <c>false</c>.</returns>
    public bool HasPassingSignature()
    {
        return GetPassingSignatureCount() > 0;
    }
}
