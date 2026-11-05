using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailResentBlock"/>.
/// </summary>
public interface IEmailResentBlockContract
{
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    int GetCount();
    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    EmailResentField GetAt(int index);
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    List<EmailResentField> GetAll();
    /// <summary>
    /// Gets the first.
    /// </summary>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current first.</returns>
    EmailResentField? GetFirst(EmailResentFieldKind kind);
    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "kind">The kind used by the operation.</param>
    /// <returns>The current all.</returns>
    List<EmailResentField> GetAllByKind(EmailResentFieldKind kind);
}
