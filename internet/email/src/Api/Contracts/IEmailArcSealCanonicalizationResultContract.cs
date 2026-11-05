using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailArcSealCanonicalizationResult"/>.
/// </summary>
public interface IEmailArcSealCanonicalizationResultContract
{
    /// <summary>
    /// Gets the seal instance.
    /// </summary>
    /// <returns>The current seal instance.</returns>
    int GetSealInstance();
    /// <summary>
    /// Gets the canonicalized headers.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current canonicalized headers.</returns>
    byte[] GetCanonicalizedHeaders();
    /// <summary>
    /// Gets the canonicalized byte count.
    /// </summary>
    /// <returns>The current canonicalized byte count.</returns>
    long GetCanonicalizedByteCount();
    /// <summary>
    /// Gets the source byte count.
    /// </summary>
    /// <returns>The current source byte count.</returns>
    long GetSourceByteCount();
    /// <summary>
    /// Gets the selection count.
    /// </summary>
    /// <returns>The current selection count.</returns>
    int GetSelectionCount();
    /// <summary>
    /// Gets the selection.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The current selection.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailArcSealHeaderSelection GetSelection(int index);
    /// <summary>
    /// Gets the selections.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current selections.</returns>
    List<EmailArcSealHeaderSelection> GetSelections();
    /// <summary>
    /// Determines whether the current value or state uses failed chain scope.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses failed chain scope; otherwise, <c>false</c>.</returns>
    bool UsesFailedChainScope();
}
