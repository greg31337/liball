using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailHeaderRegistry"/>.
/// </summary>
public interface IEmailHeaderRegistryContract
{
    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    int GetCount();
    /// <summary>
    /// Gets the name at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested name.</returns>
    string GetNameAt(int index);
    /// <summary>
    /// Gets the value kind at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value kind.</returns>
    EmailHeaderValueKind GetValueKindAt(int index);
    /// <summary>
    /// Gets the names.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current names.</returns>
    List<string> GetNames();
    /// <summary>
    /// Determines whether the collection contains the supplied value.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    bool Contains(string fieldName);
    /// <summary>
    /// Gets the value kind.
    /// </summary>
    /// <param name = "fieldName">The field name used by the operation.</param>
    /// <returns>The current value kind.</returns>
    EmailHeaderValueKind GetValueKind(string fieldName);
}
