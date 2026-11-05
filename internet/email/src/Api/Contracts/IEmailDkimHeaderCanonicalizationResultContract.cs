using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailDkimHeaderCanonicalizationResult"/>.
/// </summary>
public interface IEmailDkimHeaderCanonicalizationResultContract
{
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailDkimCanonicalizationKind GetKind();
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
    EmailDkimHeaderSelection GetSelection(int index);
    /// <summary>
    /// Gets the selections.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current selections.</returns>
    List<EmailDkimHeaderSelection> GetSelections();
    /// <summary>
    /// Gets the missing header count.
    /// </summary>
    /// <returns>The current missing header count.</returns>
    int GetMissingHeaderCount();
    /// <summary>
    /// Determines whether the operation or source value was signature value blanked.
    /// </summary>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool WasSignatureValueBlanked();
}
