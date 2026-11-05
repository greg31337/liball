using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Describes the outcome of a ARC seal canonicalization operation.
/// </summary>
/// <remarks>
/// Inspect presence, status, or success methods when available before consuming optional output. A successful or partial result can still include warnings and informational issues.
/// </remarks>
public class EmailArcSealCanonicalizationResult : IEmailArcSealCanonicalizationResultContract
{
    int sealInstance;
    byte[] canonicalizedHeaders;
    List<EmailArcSealHeaderSelection> selections;
    long sourceByteCount;
    bool failedChainScope;
    /// <summary>
    /// Creates a new ARC seal canonicalization result.
    /// </summary>
    /// <param name = "sealInstance">The seal instance used by the operation.</param>
    /// <param name = "canonicalizedHeaders">The canonicalized headers used by the operation.</param>
    /// <param name = "selections">The selections used by the operation.</param>
    /// <param name = "sourceByteCount">The number of items or bytes to process.</param>
    /// <param name = "failedChainScope">Whether to enable the failed chain scope behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailArcSealCanonicalizationResult(int sealInstance, byte[] canonicalizedHeaders, List<EmailArcSealHeaderSelection> selections, long sourceByteCount, bool failedChainScope)
    {
        if (sealInstance < 1 || sealInstance > 50)
            throw new ArgumentOutOfRangeException("sealInstance");
        if (canonicalizedHeaders == null || selections == null)
            throw new ArgumentNullException("canonicalizedHeaders");
        if (sourceByteCount < 0)
            throw new ArgumentOutOfRangeException("sourceByteCount");
        this.sealInstance = sealInstance;
        this.canonicalizedHeaders = Copy(canonicalizedHeaders);
        this.selections = new List<EmailArcSealHeaderSelection>(selections);
        this.sourceByteCount = sourceByteCount;
        this.failedChainScope = failedChainScope;
    }

    /// <summary>
    /// Gets the seal instance.
    /// </summary>
    /// <returns>The current seal instance.</returns>
    public int GetSealInstance()
    {
        return sealInstance;
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
    public EmailArcSealHeaderSelection GetSelection(int index)
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
    public List<EmailArcSealHeaderSelection> GetSelections()
    {
        return new List<EmailArcSealHeaderSelection>(selections);
    }

    /// <summary>
    /// Determines whether the current value or state uses failed chain scope.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses failed chain scope; otherwise, <c>false</c>.</returns>
    public bool UsesFailedChainScope()
    {
        return failedChainScope;
    }

    static byte[] Copy(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
