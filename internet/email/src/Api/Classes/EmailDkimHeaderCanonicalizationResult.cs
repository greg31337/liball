using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a DKIM header canonicalization operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailDkimHeaderCanonicalizationResult : IEmailDkimHeaderCanonicalizationResultContract
{
    EmailDkimCanonicalizationKind kind;
    byte[] canonicalizedHeaders;
    List<EmailDkimHeaderSelection> selections;
    long sourceByteCount;
    int missingHeaderCount;
    bool signatureValueBlanked;
    /// <summary>
    /// Creates a new DKIM header canonicalization result.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <param name = "canonicalizedHeaders">The canonicalized headers used by the operation.</param>
    /// <param name = "selections">The selections used by the operation.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "missingHeaderCount">The number of items or bytes to process.</param>
    /// <param name = "signatureValueBlanked">Whether to enable the signature value blanked behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDkimHeaderCanonicalizationResult(EmailDkimCanonicalizationKind kind, byte[] canonicalizedHeaders, List<EmailDkimHeaderSelection> selections, long sourceByteCount, int missingHeaderCount, bool signatureValueBlanked)
    {
        if (kind != EmailDkimCanonicalizationKind.Simple && kind != EmailDkimCanonicalizationKind.Relaxed)
            throw new ArgumentOutOfRangeException("kind");
        if (canonicalizedHeaders == null || selections == null)
            throw new ArgumentNullException("canonicalizedHeaders");
        if (sourceByteCount < 0 || missingHeaderCount < 0 || missingHeaderCount > selections.Count)
            throw new ArgumentOutOfRangeException("sourceByteCount");
        this.kind = kind;
        this.canonicalizedHeaders = Copy(canonicalizedHeaders);
        this.selections = new List<EmailDkimHeaderSelection>(selections);
        this.sourceByteCount = sourceByteCount;
        this.missingHeaderCount = missingHeaderCount;
        this.signatureValueBlanked = signatureValueBlanked;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailDkimCanonicalizationKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the canonicalized headers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current canonicalized headers.</returns>
    public byte[] GetCanonicalizedHeaders()
    {
        return Copy(canonicalizedHeaders);
    }

    /// <summary>
    /// Gets the canonicalized byte count.
    /// </summary>
    /// <returns>The current canonicalized byte count.</returns>
    public long GetCanonicalizedByteCount()
    {
        return canonicalizedHeaders.LongLength;
    }

    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    public long GetSourceByteCount()
    {
        return sourceByteCount;
    }

    /// <summary>
    /// Gets the selection count.
    /// </summary>
    /// <returns>The current selection count.</returns>
    public int GetSelectionCount()
    {
        return selections.Count;
    }

    /// <summary>
    /// Gets the selection.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current selection.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailDkimHeaderSelection GetSelection(int index)
    {
        if (index < 0 || index >= selections.Count)
            throw new ArgumentOutOfRangeException("index");
        return selections[index];
    }

    /// <summary>
    /// Gets the selections.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current selections.</returns>
    public List<EmailDkimHeaderSelection> GetSelections()
    {
        return new List<EmailDkimHeaderSelection>(selections);
    }

    /// <summary>
    /// Gets the missing header count.
    /// </summary>
    /// <returns>The current missing header count.</returns>
    public int GetMissingHeaderCount()
    {
        return missingHeaderCount;
    }

    /// <summary>
    /// Determines whether the operation or source value was signature value blanked.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool WasSignatureValueBlanked()
    {
        return signatureValueBlanked;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
